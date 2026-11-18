using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EngenhariaDotnet.Microservicos;

public sealed class MicroservicosTests
{
    [Fact]
    public async Task Publicacao_explicita_propaga_evento_e_cancelamento()
    {
        var publicador = new PublicadorCaptura();
        using var cancelamento = new CancellationTokenSource();
        var servico = new NotificadorPedido(publicador, TimeProvider.System);

        await servico.PublicarCriacaoAsync(Guid.NewGuid(), 125m, cancelamento.Token);

        Assert.IsType<PedidoCriado>(publicador.Evento);
        Assert.Equal(cancelamento.Token, publicador.CancellationToken);
    }

    [Fact]
    public async Task Inbox_e_efeito_na_mesma_transacao_ignoram_duplicidade()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();
        await ConsumidorPedidoCriado.CriarSchemaAsync(conexao, CancellationToken.None);
        var consumidor = new ConsumidorPedidoCriado(conexao);
        var evento = new PedidoCriado(
            Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), 49.90m);

        Assert.True(await consumidor.ProcessarAsync(evento, CancellationToken.None));
        Assert.False(await consumidor.ProcessarAsync(evento, CancellationToken.None));

        await using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) FROM pedidos_recebidos";
        Assert.Equal(1L, await comando.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Retry_repete_so_falha_classificada_e_respeita_limite()
    {
        var atrasos = new AtrasoCaptura();
        var retry = new RetryControlado(atrasos, 3, tentativa => TimeSpan.FromSeconds(tentativa));
        var chamadas = 0;

        await Assert.ThrowsAsync<TimeoutException>(() => retry.ExecutarAsync(
            _ =>
            {
                chamadas++;
                throw new TimeoutException("dependencia indisponivel");
            },
            ex => ex is TimeoutException,
            CancellationToken.None));

        Assert.Equal(3, chamadas);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], atrasos.Valores);
    }

    [Fact]
    public async Task Retry_nao_repete_falha_permanente()
    {
        var retry = new RetryControlado(new AtrasoCaptura(), 3, _ => TimeSpan.Zero);
        var chamadas = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() => retry.ExecutarAsync(
            _ =>
            {
                chamadas++;
                throw new InvalidDataException("contrato invalido");
            },
            ex => ex is TimeoutException,
            CancellationToken.None));

        Assert.Equal(1, chamadas);
    }

    [Fact]
    public void Retry_rejeita_configuracao_invalida_na_construcao()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RetryControlado(new AtrasoCaptura(), 0, _ => TimeSpan.Zero));
        Assert.Throws<ArgumentNullException>(
            () => new RetryControlado(null!, 1, _ => TimeSpan.Zero));
        Assert.Throws<ArgumentNullException>(
            () => new RetryControlado(new AtrasoCaptura(), 1, null!));
    }

    [Fact]
    public async Task Cancelamento_interrompe_retry_sem_nova_tentativa()
    {
        using var cancelamento = new CancellationTokenSource();
        var retry = new RetryControlado(
            new AtrasoQueCancela(cancelamento), 3, _ => TimeSpan.FromSeconds(1));
        var chamadas = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => retry.ExecutarAsync(
            _ =>
            {
                chamadas++;
                throw new TimeoutException();
            },
            ex => ex is TimeoutException,
            cancelamento.Token));

        Assert.Equal(1, chamadas);
    }

    [Fact]
    public void Trace_context_mantem_trace_id_entre_produtor_e_consumidor()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var produtorFonte = new ActivitySource("Pedidos");
        using var consumidorFonte = new ActivitySource("Pagamentos");
        using var produtor = produtorFonte.StartActivity("publicar", ActivityKind.Producer);
        Assert.NotNull(produtor);
        var metadados = new Dictionary<string, string>();
        ContextoTrace.InjetarAtual(metadados);

        using var consumidor = ContextoTrace.IniciarConsumo(
            consumidorFonte, metadados, "processar");

        Assert.NotNull(consumidor);
        Assert.Equal(produtor.TraceId, consumidor.TraceId);
        Assert.Equal(produtor.SpanId, consumidor.ParentSpanId);
    }

    private sealed class PublicadorCaptura : IPublicadorEventos
    {
        public IEventoIntegracao? Evento { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken)
            where TEvento : IEventoIntegracao
        {
            Evento = evento;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class AtrasoCaptura : IAtraso
    {
        public List<TimeSpan> Valores { get; } = [];
        public Task AguardarAsync(TimeSpan duracao, CancellationToken cancellationToken)
        {
            Valores.Add(duracao);
            return Task.CompletedTask;
        }
    }

    private sealed class AtrasoQueCancela(CancellationTokenSource cancelamento) : IAtraso
    {
        public Task AguardarAsync(TimeSpan duracao, CancellationToken cancellationToken)
        {
            cancelamento.Cancel();
            return Task.FromCanceled(cancellationToken);
        }
    }
}
