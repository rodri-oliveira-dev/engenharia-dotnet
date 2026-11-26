using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EngenhariaDotnet.Arquitetura;

public sealed class ArquiteturaTests
{
    [Fact]
    public void Strategy_aplica_politica_quando_cliente_e_elegivel()
    {
        IPoliticaDesconto politica = new DescontoClienteRecorrente();

        Assert.Equal(5m, politica.Calcular(new ContextoDesconto(100m, true)));
    }

    [Fact]
    public void Strategy_nao_aplica_politica_quando_cliente_nao_e_elegivel()
    {
        IPoliticaDesconto politica = new DescontoClienteRecorrente();

        Assert.Equal(0m, politica.Calcular(new ContextoDesconto(100m, false)));
    }

    [Theory]
    [InlineData(199, 0)]
    [InlineData(200, 20)]
    public void Strategy_respeita_limite_da_campanha_sazonal(decimal total, decimal desconto)
    {
        IPoliticaDesconto politica = new DescontoCampanhaSazonal();

        Assert.Equal(desconto, politica.Calcular(new ContextoDesconto(total, false)));
    }

    [Theory]
    [InlineData("recorrencia", 100, true, 5)]
    [InlineData("sazonal", 200, false, 20)]
    [InlineData("nenhuma", 100, true, 0)]
    public void Factory_seleciona_politica_conhecida(
        string campanha,
        decimal total,
        bool clienteRecorrente,
        decimal descontoEsperado)
    {
        var politica = PoliticaDescontoFactory.Criar(campanha);

        Assert.Equal(
            descontoEsperado,
            politica.Calcular(new ContextoDesconto(total, clienteRecorrente)));
    }

    [Fact]
    public void Factory_rejeita_campanha_desconhecida()
    {
        var excecao = Assert.Throws<ArgumentOutOfRangeException>(
            () => PoliticaDescontoFactory.Criar("inexistente"));

        Assert.Equal("campanha", excecao.ParamName);
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
    public void Adapter_arredonda_fracao_para_o_grama_mais_proximo()
    {
        var legado = new FreteLegadoFake();
        var adapter = new FreteLegadoAdapter(legado);

        adapter.Calcular("01001-000", 1.2345m);

        Assert.Equal(1_235, legado.UltimoPesoEmGramas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Adapter_rejeita_peso_nao_positivo(decimal pesoEmKg)
    {
        var adapter = new FreteLegadoAdapter(new FreteLegadoFake());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => adapter.Calcular("01001-000", pesoEmKg));
    }

    [Fact]
    public void Adapter_rejeita_peso_que_nao_cabe_no_contrato_legado()
    {
        var adapter = new FreteLegadoAdapter(new FreteLegadoFake());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => adapter.Calcular("01001-000", int.MaxValue / 1_000m + 0.001m));
    }

    [Fact]
    public void Adapter_rejeita_peso_positivo_que_arredonda_para_zero_grama()
    {
        var adapter = new FreteLegadoAdapter(new FreteLegadoFake());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => adapter.Calcular("01001-000", 0.0004m));
    }

    [Fact]
    public void Adapter_rejeita_cep_vazio()
    {
        var adapter = new FreteLegadoAdapter(new FreteLegadoFake());

        Assert.Throws<ArgumentException>(() => adapter.Calcular(" ", 1m));
    }

    [Fact]
    public void Adapter_rejeita_valor_negativo_retornado_pelo_legado()
    {
        var adapter = new FreteLegadoAdapter(new FreteLegadoFake(-1));

        Assert.Throws<InvalidOperationException>(
            () => adapter.Calcular("01001-000", 1m));
    }

    [Fact]
    public async Task Decorator_preserva_resultado_e_adiciona_auditoria()
    {
        var id = Guid.NewGuid();
        var auditados = new List<Guid>();
        var inner = new ConsultaPedidoFake();
        var consulta = new ConsultaPedidoComAuditoria(inner, auditados.Add);

        var resultado = await consulta.ObterAsync(id, CancellationToken.None);

        Assert.Equal("pedido", resultado);
        Assert.Equal([id], auditados);
        Assert.Equal(id, inner.UltimoPedidoId);
    }

    [Fact]
    public async Task Decorator_nao_audita_quando_consulta_falha()
    {
        var auditados = new List<Guid>();
        var consulta = new ConsultaPedidoComAuditoria(
            new ConsultaPedidoFake(new InvalidOperationException("banco indisponível")),
            auditados.Add);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consulta.ObterAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Empty(auditados);
    }

    [Fact]
    public async Task Decorator_propaga_falha_da_auditoria_apos_consulta_bem_sucedida()
    {
        var inner = new ConsultaPedidoFake();
        var consulta = new ConsultaPedidoComAuditoria(
            inner,
            _ => throw new InvalidOperationException("auditoria indisponível"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consulta.ObterAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(1, inner.Chamadas);
    }

    [Fact]
    public async Task Decorator_propaga_token_para_consulta_decorada()
    {
        using var origem = new CancellationTokenSource();
        var inner = new ConsultaPedidoFake();
        var consulta = new ConsultaPedidoComAuditoria(inner, _ => { });

        await consulta.ObterAsync(Guid.NewGuid(), origem.Token);

        Assert.Equal(origem.Token, inner.UltimoCancellationToken);
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
    public async Task Observer_aceita_ausencia_de_assinantes()
    {
        var notificador = new NotificadorPedido([]);

        await notificador.NotificarAsync(
            new PedidoConfirmado(Guid.NewGuid()),
            CancellationToken.None);
    }

    [Fact]
    public async Task Observer_interrompe_sequencia_e_propaga_falha()
    {
        var primeiro = new ObservadorFake(new InvalidOperationException("falha local"));
        var segundo = new ObservadorFake();
        var notificador = new NotificadorPedido([primeiro, segundo]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => notificador.NotificarAsync(
                new PedidoConfirmado(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(1, primeiro.Chamadas);
        Assert.Equal(0, segundo.Chamadas);
    }

    [Fact]
    public async Task Observer_cancelado_nao_inicia_notificacoes()
    {
        using var origem = new CancellationTokenSource();
        origem.Cancel();
        var observador = new ObservadorFake();
        var notificador = new NotificadorPedido([observador]);

        var excecao = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => notificador.NotificarAsync(
                new PedidoConfirmado(Guid.NewGuid()),
                origem.Token));

        Assert.Equal(origem.Token, excecao.CancellationToken);
        Assert.Equal(0, observador.Chamadas);
    }

    [Fact]
    public async Task Observer_cancelado_entre_assinantes_interrompe_sequencia()
    {
        using var origem = new CancellationTokenSource();
        var primeiro = new ObservadorFake(aoChamar: origem.Cancel);
        var segundo = new ObservadorFake();
        var notificador = new NotificadorPedido([primeiro, segundo]);

        var excecao = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => notificador.NotificarAsync(
                new PedidoConfirmado(Guid.NewGuid()),
                origem.Token));

        Assert.Equal(origem.Token, excecao.CancellationToken);
        Assert.Equal(1, primeiro.Chamadas);
        Assert.Equal(0, segundo.Chamadas);
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

    private sealed class FreteLegadoFake(int valorEmCentavos = 1_234) : IFreteLegado
    {
        public int UltimoPesoEmGramas { get; private set; }
        public int CotarEmCentavos(string codigoPostal, int pesoEmGramas)
        {
            UltimoPesoEmGramas = pesoEmGramas;
            return valorEmCentavos;
        }
    }

    private sealed class ConsultaPedidoFake(Exception? falha = null) : IConsultaPedido
    {
        public int Chamadas { get; private set; }
        public Guid UltimoPedidoId { get; private set; }
        public CancellationToken UltimoCancellationToken { get; private set; }

        public Task<string> ObterAsync(Guid pedidoId, CancellationToken cancellationToken)
        {
            Chamadas++;
            UltimoPedidoId = pedidoId;
            UltimoCancellationToken = cancellationToken;
            return falha is null ? Task.FromResult("pedido") : Task.FromException<string>(falha);
        }
    }

    private sealed class ObservadorFake(
        Exception? falha = null,
        Action? aoChamar = null) : IObservadorPedido
    {
        public int Chamadas { get; private set; }
        public Task AoConfirmarAsync(PedidoConfirmado evento, CancellationToken cancellationToken)
        {
            Chamadas++;
            aoChamar?.Invoke();
            return falha is null ? Task.CompletedTask : Task.FromException(falha);
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
