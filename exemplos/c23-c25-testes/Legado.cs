namespace EngenhariaDotnet.Testes;

public sealed class CalculadoraFreteLegada
{
    // Comportamento histórico: a região é comparada sem normalização e valores
    // terminados em 0,005 são arredondados para longe de zero.
    public decimal Calcular(decimal pesoEmQuilos, string regiao)
    {
        var tarifa = regiao == "SUL" ? 1.15m : 1.35m;
        return Math.Round(7.005m + pesoEmQuilos * tarifa, 2,
            MidpointRounding.AwayFromZero);
    }
}
