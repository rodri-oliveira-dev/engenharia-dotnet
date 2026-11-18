namespace C11C12.Ddd;

public interface IRepositorioDePedidos
{
    Task<Pedido?> ObterAsync(Guid id, CancellationToken cancellationToken);
    Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken);
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}

public interface IPublicadorDeEventosDeDominio
{
    Task PublicarAsync(IEventoDeDominio evento, CancellationToken cancellationToken);
}

public interface IRelogio
{
    DateTimeOffset Agora { get; }
}

public sealed class ServicoDePedidos(
    IRepositorioDePedidos repositorio,
    IPublicadorDeEventosDeDominio publicador,
    IRelogio relogio)
{
    public async Task<Guid> CriarAsync(
        Email emailCliente,
        string moeda,
        CancellationToken cancellationToken)
    {
        var pedido = new Pedido(Guid.NewGuid(), emailCliente, moeda);
        await repositorio.AdicionarAsync(pedido, cancellationToken);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return pedido.Id;
    }

    public async Task AdicionarItemAsync(
        Guid pedidoId,
        ProdutoId produtoId,
        int quantidade,
        Dinheiro precoUnitario,
        CancellationToken cancellationToken)
    {
        var pedido = await ObterPedidoAsync(pedidoId, cancellationToken);
        pedido.AdicionarItem(produtoId, quantidade, precoUnitario);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task ConfirmarAsync(Guid pedidoId, CancellationToken cancellationToken)
    {
        var pedido = await ObterPedidoAsync(pedidoId, cancellationToken);
        pedido.Confirmar(relogio.Agora);

        // Só despacha efeitos internos depois que o estado foi persistido.
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        foreach (var evento in pedido.EventosDeDominio.ToArray())
            await publicador.PublicarAsync(evento, cancellationToken);
        pedido.LimparEventosDeDominio();
    }

    private async Task<Pedido> ObterPedidoAsync(Guid id, CancellationToken cancellationToken) =>
        await repositorio.ObterAsync(id, cancellationToken)
        ?? throw new RegraDeNegocioException("Pedido não encontrado.");
}

// Contrato local, expresso na linguagem do contexto de Pedidos.
public sealed record ClienteElegivel(string ClienteId, Email Email);

public interface IConsultaDeCliente
{
    Task<ClienteElegivel> ObterElegivelAsync(string clienteId, CancellationToken cancellationToken);
}

// Contrato imposto pelo sistema legado.
public sealed record CadastroClienteLegado(string Codigo, string Correio, bool Inativo, bool Bloqueado);

public interface IClienteLegado
{
    Task<CadastroClienteLegado?> BuscarAsync(string codigo, CancellationToken cancellationToken);
}

// ACL: traduz estrutura e semântica; o domínio não conhece o DTO legado.
public sealed class ConsultaDeClienteLegado(IClienteLegado clienteLegado) : IConsultaDeCliente
{
    public async Task<ClienteElegivel> ObterElegivelAsync(
        string clienteId,
        CancellationToken cancellationToken)
    {
        var cadastro = await clienteLegado.BuscarAsync(clienteId, cancellationToken)
            ?? throw new RegraDeNegocioException("Cliente não encontrado no cadastro externo.");

        if (cadastro.Inativo || cadastro.Bloqueado)
            throw new RegraDeNegocioException("O cliente não está elegível para realizar pedidos.");

        return new ClienteElegivel(cadastro.Codigo, new Email(cadastro.Correio));
    }
}
