using C11C12.Ddd;
using Xunit;

namespace C11C12.Ddd.Tests;

public sealed class DominioPedidosTests
{
    private static readonly ProdutoId Livro = new("livro-ddd");

    [Fact]
    public void Value_objects_validam_normalizam_e_comparam_por_valor()
    {
        Assert.Equal(new Email("Pessoa@Exemplo.com"), new Email("pessoa@exemplo.com"));
        Assert.Equal(new Dinheiro(10, "brl"), new Dinheiro(10, "BRL"));
        Assert.Equal(90m, new Dinheiro(100, "BRL").Aplicar(new Percentual(10)).Quantia);
        Assert.Throws<RegraDeNegocioException>(() => new Percentual(101));
        Assert.Throws<RegraDeNegocioException>(() => new Dinheiro(10, "R$"));
        Assert.Throws<RegraDeNegocioException>(() => new Email("email inválido"));
    }

    [Fact]
    public void Adicionar_mesmo_produto_acumula_quantidade_sem_expor_mutacao()
    {
        var pedido = NovoPedido();

        pedido.AdicionarItem(Livro, 1, new Dinheiro(50, "BRL"));
        pedido.AdicionarItem(Livro, 2, new Dinheiro(50, "BRL"));

        var item = Assert.Single(pedido.Itens);
        Assert.Equal(3, item.Quantidade);
        Assert.Equal(new Dinheiro(150, "BRL"), pedido.Total);
        Assert.IsAssignableFrom<IReadOnlyList<ItemPedido>>(pedido.Itens);
    }

    [Fact]
    public void Pedido_rejeita_preco_implicito_moeda_e_quantidade_invalidos()
    {
        var pedido = NovoPedido();
        pedido.AdicionarItem(Livro, 1, new Dinheiro(50, "BRL"));

        Assert.Throws<RegraDeNegocioException>(
            () => pedido.AdicionarItem(Livro, 1, new Dinheiro(60, "BRL")));
        Assert.Throws<RegraDeNegocioException>(
            () => pedido.AdicionarItem(new ProdutoId("outro"), 1, new Dinheiro(10, "USD")));
        Assert.Throws<RegraDeNegocioException>(() => pedido.RemoverItem(Livro, 2));
        Assert.Equal(1, Assert.Single(pedido.Itens).Quantidade);
    }

    [Fact]
    public void Remocao_controlada_reduz_quantidade_e_remove_item_sem_estado_intermediario_invalido()
    {
        var pedido = NovoPedido();
        pedido.AdicionarItem(Livro, 3, new Dinheiro(50, "BRL"));

        pedido.RemoverItem(Livro, 2);
        Assert.Equal(1, Assert.Single(pedido.Itens).Quantidade);

        pedido.RemoverItem(Livro, 1);
        Assert.Empty(pedido.Itens);
    }

    [Fact]
    public void Regra_do_agregado_restringe_desconto_mais_que_o_value_object_generico()
    {
        var pedido = NovoPedido();

        Assert.Throws<RegraDeNegocioException>(() => pedido.AplicarDesconto(new Percentual(31)));
        Assert.Equal(new Percentual(0), pedido.Desconto);
    }

    [Fact]
    public void Confirmacao_exige_itens_congela_alteracoes_e_registra_evento()
    {
        var vazio = NovoPedido();
        Assert.Throws<RegraDeNegocioException>(() => vazio.Confirmar(DateTimeOffset.UtcNow));

        var instante = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var pedido = NovoPedido();
        pedido.AdicionarItem(Livro, 2, new Dinheiro(100, "BRL"));
        pedido.AplicarDesconto(new Percentual(10));
        pedido.Confirmar(instante);

        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
        Assert.Equal(new Dinheiro(180, "BRL"), pedido.Total);
        var evento = Assert.IsType<PedidoConfirmado>(Assert.Single(pedido.EventosDeDominio));
        Assert.Equal(pedido.Id, evento.PedidoId);
        Assert.Equal(instante, evento.OcorridoEm);
        Assert.Throws<RegraDeNegocioException>(
            () => pedido.AdicionarItem(new ProdutoId("novo"), 1, new Dinheiro(10, "BRL")));
    }

    [Fact]
    public async Task Servico_publica_evento_somente_depois_de_persistir()
    {
        var pedido = NovoPedido();
        pedido.AdicionarItem(Livro, 1, new Dinheiro(50, "BRL"));
        var chamadas = new List<string>();
        var repositorio = new RepositorioFake(pedido, chamadas);
        var publicador = new PublicadorFake(chamadas);
        var servico = new ServicoDePedidos(repositorio, publicador, new RelogioFake());

        await servico.ConfirmarAsync(pedido.Id, CancellationToken.None);

        Assert.Equal(["salvar", "publicar"], chamadas);
        Assert.Empty(pedido.EventosDeDominio);
    }

    [Fact]
    public async Task Falha_de_persistencia_impede_publicacao()
    {
        var pedido = NovoPedido();
        pedido.AdicionarItem(Livro, 1, new Dinheiro(50, "BRL"));
        var chamadas = new List<string>();
        var repositorio = new RepositorioFake(pedido, chamadas) { FalharAoSalvar = true };
        var servico = new ServicoDePedidos(repositorio, new PublicadorFake(chamadas), new RelogioFake());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servico.ConfirmarAsync(pedido.Id, CancellationToken.None));

        Assert.Equal(["salvar"], chamadas);
        Assert.Single(pedido.EventosDeDominio);
    }

    [Fact]
    public async Task Acl_traduz_contrato_externo_e_rejeita_semantica_invalida()
    {
        var acl = new ConsultaDeClienteLegado(
            new ClienteLegadoFake(new("CLI-1", "cliente@exemplo.com", false, false)));

        var cliente = await acl.ObterElegivelAsync("CLI-1", CancellationToken.None);

        Assert.Equal("CLI-1", cliente.ClienteId);
        Assert.Equal(new Email("cliente@exemplo.com"), cliente.Email);

        var bloqueado = new ConsultaDeClienteLegado(
            new ClienteLegadoFake(new("CLI-2", "cliente@exemplo.com", false, true)));
        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => bloqueado.ObterElegivelAsync("CLI-2", CancellationToken.None));
    }

    [Fact]
    public void Hierarquia_fechada_descreve_cada_estado_sem_fallback()
    {
        Assert.Equal("Aguardando separacao", FluxoSeparacao.Descrever(new AguardandoSeparacao()));
        Assert.Equal("Separando: 2 itens", FluxoSeparacao.Descrever(new Separando(2)));
        Assert.Equal("Separacao concluida", FluxoSeparacao.Descrever(new SeparacaoConcluida()));
    }

    [Fact]
    public void Hierarquia_fechada_explicita_transicoes_validas_e_invalidas()
    {
        var concluida = FluxoSeparacao.Concluir(new Separando(3));

        Assert.IsType<SeparacaoConcluida>(concluida);
        Assert.Throws<RegraDeNegocioException>(() =>
            FluxoSeparacao.Concluir(new AguardandoSeparacao()));
        Assert.Throws<RegraDeNegocioException>(() =>
            FluxoSeparacao.Concluir(new SeparacaoConcluida()));
    }

    private static Pedido NovoPedido() =>
        new(Guid.NewGuid(), new Email("cliente@exemplo.com"), "BRL");

    private sealed class RepositorioFake(Pedido pedido, List<string> chamadas) : IRepositorioDePedidos
    {
        public bool FalharAoSalvar { get; init; }
        public Task<Pedido?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult<Pedido?>(pedido);
        public Task AdicionarAsync(Pedido novoPedido, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct)
        {
            chamadas.Add("salvar");
            return FalharAoSalvar
                ? Task.FromException(new InvalidOperationException("Falha simulada."))
                : Task.CompletedTask;
        }
    }

    private sealed class PublicadorFake(List<string> chamadas) : IPublicadorDeEventosDeDominio
    {
        public Task PublicarAsync(IEventoDeDominio evento, CancellationToken ct)
        {
            chamadas.Add("publicar");
            return Task.CompletedTask;
        }
    }

    private sealed class RelogioFake : IRelogio
    {
        public DateTimeOffset Agora { get; } = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class ClienteLegadoFake(CadastroClienteLegado cadastro) : IClienteLegado
    {
        public Task<CadastroClienteLegado?> BuscarAsync(string codigo, CancellationToken ct) =>
            Task.FromResult<CadastroClienteLegado?>(cadastro);
    }
}
