namespace EngenhariaDotnet.ColecoesLinq;

public sealed record Pedido(int Id, int ClienteId, decimal Total);

public sealed record Cliente(int Id, string Nome);

public sealed record ConciliacaoClientePedido(int ClienteId, string? NomeCliente, int? PedidoId);

public static class Consultas
{
    public static IReadOnlyList<Pedido> BuscarRepetidamente(
        IReadOnlyList<Pedido> pedidos,
        IEnumerable<int> ids)
    {
        var encontrados = new List<Pedido>();

        foreach (var id in ids)
        {
            var pedido = pedidos.FirstOrDefault(item => item.Id == id);
            if (pedido is not null)
                encontrados.Add(pedido);
        }

        return encontrados;
    }

    public static IReadOnlyList<Pedido> BuscarComIndice(
        IReadOnlyCollection<Pedido> pedidos,
        IEnumerable<int> ids)
    {
        var porId = pedidos.ToDictionary(pedido => pedido.Id);
        var encontrados = new List<Pedido>();

        foreach (var id in ids)
        {
            if (porId.TryGetValue(id, out var pedido))
                encontrados.Add(pedido);
        }

        return encontrados;
    }

    public static IReadOnlyList<Pedido> BuscarClientesPermitidos(
        IEnumerable<Pedido> pedidos,
        IEnumerable<int> clientesPermitidos)
    {
        var permitidos = clientesPermitidos.ToHashSet();
        return pedidos.Where(pedido => permitidos.Contains(pedido.ClienteId)).ToArray();
    }

    public static ILookup<int, Pedido> AgruparPorCliente(IEnumerable<Pedido> pedidos) =>
        pedidos.ToLookup(pedido => pedido.ClienteId);

    // Inclui clientes sem pedidos e pedidos sem cliente cadastrado.
    public static IReadOnlyList<ConciliacaoClientePedido> ConciliarClientesPedidos(
        IEnumerable<Cliente> clientes,
        IEnumerable<Pedido> pedidos)
    {
        ArgumentNullException.ThrowIfNull(clientes);
        ArgumentNullException.ThrowIfNull(pedidos);

        return clientes.FullJoin(
                pedidos,
                cliente => cliente.Id,
                pedido => pedido.ClienteId,
                (cliente, pedido) => new ConciliacaoClientePedido(
                    cliente?.Id ?? pedido?.ClienteId
                        ?? throw new InvalidOperationException("Par sem chave."),
                    cliente?.Nome,
                    pedido?.Id))
            .ToArray();
    }

    // O comparer faz parte da semantica de igualdade, nao da normalizacao.
    public static HashSet<string> CriarEtiquetasSemDuplicidade(IEnumerable<string> etiquetas)
    {
        ArgumentNullException.ThrowIfNull(etiquetas);
        HashSet<string> resultado = [with(StringComparer.OrdinalIgnoreCase), .. etiquetas];
        return resultado;
    }

    public static List<Pedido> CriarLoteComCapacidade(
        IReadOnlyCollection<Pedido> pedidos,
        int reserva)
    {
        ArgumentNullException.ThrowIfNull(pedidos);
        ArgumentOutOfRangeException.ThrowIfNegative(reserva);

        var capacidade = checked(pedidos.Count + reserva);
        List<Pedido> resultado = [with(capacity: capacidade), .. pedidos];
        return resultado;
    }

}

public static class Enumeracao
{
    public static IEnumerable<int> Gerar(int quantidade, Action<int> aoProduzir)
    {
        for (var numero = 0; numero < quantidade; numero++)
        {
            aoProduzir(numero);
            yield return numero;
        }
    }

    public static (int Quantidade, int Soma) EnumerarDuasVezes(IEnumerable<int> fonte) =>
        (fonte.Count(), fonte.Sum());

    public static (int Quantidade, int Soma) MaterializarUmaVez(IEnumerable<int> fonte)
    {
        var valores = fonte.ToArray();
        return (valores.Length, valores.Sum());
    }

    public static IEnumerable<int> Pares(IEnumerable<int> fonte) =>
        fonte.Where(numero => numero % 2 == 0);
}
