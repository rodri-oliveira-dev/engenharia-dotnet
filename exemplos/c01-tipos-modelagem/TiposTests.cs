using C01.TiposModelagem;
using Xunit;

namespace C01.TiposModelagem.Tests;

public sealed class TiposTests
{
    [Fact]
    public void Classe_compara_identidade_e_record_compara_valores()
    {
        var id = Guid.NewGuid();
        var cliente1 = new Cliente(id, "Ada");
        var cliente2 = new Cliente(id, "Ada");
        var dinheiro1 = new Dinheiro(10m, "BRL");
        var dinheiro2 = new Dinheiro(10m, "BRL");

        Assert.NotEqual(cliente1, cliente2);
        Assert.Equal(dinheiro1, dinheiro2);
        Assert.False(ReferenceEquals(dinheiro1, dinheiro2));
    }

    [Fact]
    public void Atribuicao_e_parametro_por_valor_copiam_struct()
    {
        var original = new Contador();
        var copia = original;

        copia.Incrementar();
        Operacoes.IncrementarCopia(original);

        Assert.Equal(0, original.Valor);
        Assert.Equal(1, copia.Valor);

        Operacoes.IncrementarOriginal(ref original);
        Assert.Equal(1, original.Valor);
    }

    [Fact]
    public void Leitura_de_struct_em_lista_produz_uma_copia()
    {
        var contadores = new List<Contador> { new() };
        var copia = contadores[0];

        copia.Incrementar();

        Assert.Equal(0, contadores[0].Valor);
        Assert.Equal(1, copia.Valor);
    }

    [Fact]
    public void Boxing_cria_uma_copia_independente_do_valor()
    {
        var valor = new ItemComCodigo(42);
        object caixa = valor;
        ITemComCodigo interfaceCaixa = valor;
        valor = new ItemComCodigo(7);

        Assert.Equal(42, ((ItemComCodigo)caixa).Codigo);
        Assert.Equal(42, interfaceCaixa.Codigo);
        Assert.Equal(7, valor.Codigo);
        Assert.True(Operacoes.MesmoValor(new ItemComCodigo(1), new ItemComCodigo(1)));
    }
}
