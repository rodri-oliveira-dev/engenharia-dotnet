namespace EngenhariaDotnet.Testes;

public enum CategoriaCliente
{
    Regular,
    Fidelidade
}

public static class PoliticaDesconto
{
    public static decimal Calcular(decimal total, CategoriaCliente categoria)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        var percentual = categoria == CategoriaCliente.Fidelidade && total >= 200m
            ? 0.10m
            : 0m;

        return decimal.Round(total * percentual, 2, MidpointRounding.ToEven);
    }
}
