using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EngenhariaDotnet.Arquitetura;

public sealed class ArquiteturaTests
{
    [Fact]
    public void Strategy_e_factory_selecionam_variacao_real()
    {
        var politica = PoliticaDescontoFactory.Criar("recorrencia");
        Assert.Equal(5m, politica.Calcular(new ContextoDesconto(100m, true)));
    }

    [Fact]
    public void Adapter_traduz_unidades_do_contrato_legado()
    {
        var legado = new FreteLegadoFake();
        var adapter = new FreteLegadoAdapter(legado);

        Assert.Equal(12.34m, adapter.Calcular("01001-000", 1.5m));
        Assert.Equal(1_500, legado.UltimoPesoEmGramas);
    }

    [Fact]
    public async Task Decorator_preserva_resultado_e_adiciona_auditoria()
    {
        var id = Guid.NewGuid();
        var auditados = new List<Guid>();
        var consulta = new ConsultaPedidoComAuditoria(new ConsultaPedidoFake(), auditados.Add);

        var resultado = await consulta.ObterAsync(id, CancellationToken.None);

        Assert.Equal("pedido", resultado);
        Assert.Equal([id], auditados);
    }

    [Fact]
    public async Task Observer_notifica_todos_os_assinantes_locais()
    {
        var primeiro = new ObservadorFake();
        var segundo = new ObservadorFake();
        var notificador = new NotificadorPedido([primeiro, segundo]);

        await notificador.NotificarAsync(new PedidoConfirmado(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(1, primeiro.Chamadas);
        Assert.Equal(1, segundo.Chamadas);
    }

    [Fact]
    public async Task Command_persiste_pedido_e_outbox_e_query_projeta_leitura()
    {
        await using var banco = await BancoTeste.CriarAsync();
        var id = Guid.NewGuid();
        await new CriarPedidoHandler(banco.Db, TimeProvider.System)
            .HandleAsync(new CriarPedido(id, "cliente-1", 42m), CancellationToken.None);

        var resumo = await new ConsultarPedido(banco.Db).PorIdAsync(id, CancellationToken.None);

        Assert.NotNull(resumo);
        Assert.Equal(42m, resumo.Total);
        Assert.Equal(1, await banco.Db.Outbox.CountAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Falha_de_publicacao_mantem_outbox_para_retry()
    {
        await using var banco = await BancoTeste.CriarAsync();
        await new CriarPedidoHandler(banco.Db, TimeProvider.System)
            .HandleAsync(new CriarPedido(Guid.NewGuid(), "cliente-1", 10m), CancellationToken.None);
        var publicador = new PublicadorOutbox(banco.Db, new BusSempreFalha(), TimeProvider.System);

        await Assert.ThrowsAsync<FalhaTransitoriaException>(
            () => publicador.PublicarPendentesAsync(CancellationToken.None));

        var evento = await banco.Db.Outbox.SingleAsync(CancellationToken.None);
        Assert.Null(evento.PublicadoEm);
        Assert.Equal(1, evento.Tentativas);
    }

    [Fact]
    public async Task Consumidor_ignora_mensagem_duplicada_sem_repetir_efeito()
    {
        await using var banco = await BancoTeste.CriarAsync();
        var mensagemId = Guid.NewGuid();
        var pedidoId = Guid.NewGuid();
        var consumidor = new ConsumidorPedidoCriado(banco.Db, TimeProvider.System);

        Assert.True(await consumidor.ProcessarAsync(mensagemId, pedidoId, CancellationToken.None));
        Assert.False(await consumidor.ProcessarAsync(mensagemId, pedidoId, CancellationToken.None));

        Assert.Equal(1, (await banco.Db.PedidosProjetados.SingleAsync(CancellationToken.None)).Notificacoes);
    }

    [Fact]
    public async Task Poison_message_vai_para_dlq_sem_retries_inuteis()
    {
        var tratador = new TratadorPermanente();
        var processador = new ProcessadorResiliente(tratador, new EsperaImediata());

        var morta = await processador.ProcessarAsync(
            new Mensagem(Guid.NewGuid(), "desconhecido", "{}"),
            CancellationToken.None);

        Assert.NotNull(morta);
        Assert.Equal(1, morta.Tentativas);
        Assert.Equal(1, tratador.Chamadas);
    }

    [Fact]
    public void Compensacao_de_saga_e_uma_nova_transacao_e_pode_falhar()
    {
        var saga = new SagaPedido(Guid.NewGuid(), Guid.NewGuid());
        saga.EstoqueReservado();
        saga.IniciarCompensacao("pagamento recusado");
        saga.ExigirIntervencao("falha ao liberar estoque");

        Assert.Equal(EstadoSagaPedido.FalhaManual, saga.Estado);
        Assert.Equal("falha ao liberar estoque", saga.UltimoErro);
    }

    private sealed class BusSempreFalha : IBusEventos
    {
        public Task PublicarAsync(Guid mensagemId, string tipo, string payload, CancellationToken cancellationToken) =>
            throw new FalhaTransitoriaException("broker indisponível");
    }

    private sealed class FreteLegadoFake : IFreteLegado
    {
        public int UltimoPesoEmGramas { get; private set; }
        public int CotarEmCentavos(string codigoPostal, int pesoEmGramas)
        {
            UltimoPesoEmGramas = pesoEmGramas;
            return 1_234;
        }
    }

    private sealed class ConsultaPedidoFake : IConsultaPedido
    {
        public Task<string> ObterAsync(Guid pedidoId, CancellationToken cancellationToken) =>
            Task.FromResult("pedido");
    }

    private sealed class ObservadorFake : IObservadorPedido
    {
        public int Chamadas { get; private set; }
        public Task AoConfirmarAsync(PedidoConfirmado evento, CancellationToken cancellationToken)
        {
            Chamadas++;
            return Task.CompletedTask;
        }
    }

    private sealed class TratadorPermanente : ITratadorMensagem
    {
        public int Chamadas { get; private set; }
        public Task ProcessarAsync(Mensagem mensagem, CancellationToken cancellationToken)
        {
            Chamadas++;
            throw new FalhaPermanenteException("schema inválido");
        }
    }

    private sealed class EsperaImediata : IEsperaRetry
    {
        public Task AguardarAsync(TimeSpan atraso, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

internal sealed class BancoTeste : IAsyncDisposable
{
    private readonly SqliteConnection conexao;
    private BancoTeste(SqliteConnection conexao, PedidosDbContext db) => (this.conexao, Db) = (conexao, db);
    public PedidosDbContext Db { get; }

    public static async Task<BancoTeste> CriarAsync()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();
        var options = new DbContextOptionsBuilder<PedidosDbContext>().UseSqlite(conexao).Options;
        var db = new PedidosDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return new BancoTeste(conexao, db);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await conexao.DisposeAsync();
    }
}
