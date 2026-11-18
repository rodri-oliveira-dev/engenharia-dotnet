using Microsoft.EntityFrameworkCore;

namespace EngenhariaDotnet.Persistencia;

public sealed record ResumoPedido(int Id, string Descricao, decimal Total, int QuantidadeDeItens);

public static class Consultas
{
    private static readonly Func<LojaContext, string, IAsyncEnumerable<ResumoPedido>>
        PedidosPorStatusCompilada = EF.CompileAsyncQuery(
            (LojaContext context, string status) => context.Pedidos
                .AsNoTracking()
                .Where(pedido => pedido.Status == status)
                .OrderBy(pedido => pedido.Id)
                .Select(pedido => new ResumoPedido(
                    pedido.Id,
                    pedido.Descricao,
                    pedido.Total,
                    pedido.Itens.Count)));

    public static Task<List<ResumoPedido>> PaginarAsync(
        LojaContext context,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pagina, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanho, 100);

        return context.Pedidos
            .AsNoTracking()
            .OrderBy(pedido => pedido.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(pedido => new ResumoPedido(
                pedido.Id,
                pedido.Descricao,
                pedido.Total,
                pedido.Itens.Count))
            .ToListAsync(cancellationToken);
    }

    public static IAsyncEnumerable<ResumoPedido> PorStatusCompilada(
        LojaContext context,
        string status) => PedidosPorStatusCompilada(context, status);
}
