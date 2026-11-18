using Xunit;

namespace EngenhariaDotnet.Entrega;

public sealed class FeatureFlagsTests
{
    [Fact]
    public void Flag_desabilitada_preserva_comportamento_estavel()
    {
        var calculadora = new CalculadoraFrete(new FlagsFixas(false));

        var resultado = calculadora.Calcular(100m, new ContextoAvaliacao("cliente-1"));

        Assert.Equal(new ResultadoFrete(20m, "estavel"), resultado);
    }

    [Fact]
    public void Flag_habilitada_seleciona_nova_politica()
    {
        var calculadora = new CalculadoraFrete(new FlagsFixas(true));

        var resultado = calculadora.Calcular(100m, new ContextoAvaliacao("cliente-1"));

        Assert.Equal(new ResultadoFrete(16m, "nova"), resultado);
    }

    private sealed class FlagsFixas(bool habilitada) : IFeatureFlags
    {
        public bool Habilitada(string nome, ContextoAvaliacao contexto)
        {
            Assert.Equal(CalculadoraFrete.NovaPolitica, nome);
            Assert.NotEmpty(contexto.ClienteId);
            return habilitada;
        }
    }
}
