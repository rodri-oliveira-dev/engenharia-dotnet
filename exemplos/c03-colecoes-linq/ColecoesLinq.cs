namespace EngenhariaDotnet.ColecoesLinq;

public sealed record Pedido(int Id, int ClienteId, decimal Total);

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
