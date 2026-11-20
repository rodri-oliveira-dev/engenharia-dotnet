using Microsoft.EntityFrameworkCore;

namespace EngenhariaDotnet.Persistencia;

public sealed record LinhaConciliacao(string Codigo, string? Cliente, string? Pedido);

public sealed record ResumoMarcadores(int PedidoId, string Marcadores);

/// <summary>
/// Consultas didáticas do EF Core 11. As traduções dependem do provider;
/// os testes executam SQLite real e validam SQL Server sem abrir conexão.
/// </summary>
public static class NovidadesEfCore11
{
    // Os dados não têm FK de propósito: há clientes e pedidos sem par.
    // FullJoin é traduzido para FULL JOIN; não é um join em memória.
    public static IQueryable<LinhaConciliacao> Conciliar(LojaContext context) =>
        context.ClientesExternos.FullJoin(
            context.PedidosExternos,
            cliente => cliente.Codigo,
            pedido => pedido.CodigoCliente,
            (cliente, pedido) => new LinhaConciliacao(
                cliente != null ? cliente.Codigo : pedido!.CodigoCliente,
                cliente != null ? cliente.Nome : null,
                pedido != null ? pedido.Descricao : null));

    // O SQLite 3.44+ suporta ORDER BY dentro de group_concat.
    public static IQueryable<ResumoMarcadores> MarcadoresOrdenados(LojaContext context) =>
        context.Marcadores
            .GroupBy(marcador => marcador.PedidoId)
            .Select(grupo => new ResumoMarcadores(
                grupo.Key,
                string.Join(", ", grupo
                    .OrderBy(marcador => marcador.Nome)
                    .Select(marcador => marcador.Nome))));

    // GroupBy com navegação de referência (ItemPedido -> Pedido).
    // O agrupamento e a agregação devem ser feitos pelo banco.
    public static IQueryable<ResumoPorStatus> AgruparItensPorStatus(LojaContext context) =>
        context.Itens
            .GroupBy(item => item.Pedido.Status)
            .Select(grupo => new ResumoPorStatus(
                grupo.Key,
                grupo.Count(),
                grupo.Sum(item => item.Quantidade)));
}

public sealed record ResumoPorStatus(string Status, int NumeroDeItens, int QuantidadeTotal);
