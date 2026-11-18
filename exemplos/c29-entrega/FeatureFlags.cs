namespace EngenhariaDotnet.Entrega;

public interface IFeatureFlags
{
    bool Habilitada(string nome, ContextoAvaliacao contexto);
}

public sealed record ContextoAvaliacao(string ClienteId);

public sealed record ResultadoFrete(decimal Valor, string Calculadora);

public sealed class CalculadoraFrete(IFeatureFlags flags)
{
    public const string NovaPolitica = "frete-nova-politica";

    public ResultadoFrete Calcular(decimal subtotal, ContextoAvaliacao contexto)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(subtotal);

        return flags.Habilitada(NovaPolitica, contexto)
            ? new ResultadoFrete(Math.Max(0m, 18m - subtotal * 0.02m), "nova")
            : new ResultadoFrete(subtotal >= 200m ? 0m : 20m, "estavel");
    }
}
