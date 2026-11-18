using EngenhariaDotnet.ColecoesLinq;
using Xunit;

namespace EngenhariaDotnet.ColecoesLinq.Tests;

public sealed class ColecoesLinqTests
{
    private static readonly Pedido[] Pedidos =
    [
        new(1, 10, 25m),
        new(2, 20, 50m),
        new(3, 10, 75m)
    ];

    [Fact]
    public void Lista_e_indice_retornam_o_mesmo_resultado()
    {
        var ids = new[] { 3, 1, 99 };

        var porLista = Consultas.BuscarRepetidamente(Pedidos, ids);
        var porIndice = Consultas.BuscarComIndice(Pedidos, ids);

        Assert.Equal(porLista, porIndice);
    }

    [Fact]
    public void Indice_exige_chaves_unicas()
    {
        var duplicados = new[] { Pedidos[0], Pedidos[0] with { Total = 100m } };

        Assert.Throws<ArgumentException>(() =>
            Consultas.BuscarComIndice(duplicados, new[] { 1 }));
    }

    [Fact]
    public void Reenumerar_repete_a_producao_e_os_efeitos_colaterais()
    {
        var producoes = 0;
        var fonte = Enumeracao.Gerar(3, _ => producoes++);

        var resultado = Enumeracao.EnumerarDuasVezes(fonte);

        Assert.Equal((3, 3), resultado);
        Assert.Equal(6, producoes);
    }

    [Fact]
    public void Materializar_estabiliza_o_resultado_para_multiplos_consumos()
    {
        var producoes = 0;
        var fonte = Enumeracao.Gerar(3, _ => producoes++);

        var resultado = Enumeracao.MaterializarUmaVez(fonte);

        Assert.Equal((3, 3), resultado);
        Assert.Equal(3, producoes);
    }

    [Fact]
    public void Execucao_diferida_processa_somente_o_necessario()
    {
        var producoes = 0;
        var fonte = Enumeracao.Gerar(1_000_000, _ => producoes++);

        var primeiros = Enumeracao.Pares(fonte).Take(3).ToArray();

        Assert.Equal(new[] { 0, 2, 4 }, primeiros);
        Assert.Equal(5, producoes);
    }

    [Fact]
    public void HashSet_e_lookup_expressam_consulta_de_pertinencia_e_um_para_muitos()
    {
        var permitidos = Consultas.BuscarClientesPermitidos(Pedidos, new[] { 10 });
        var porCliente = Consultas.AgruparPorCliente(Pedidos);

        Assert.Equal(2, permitidos.Count);
        Assert.Equal(new[] { 1, 3 }, porCliente[10].Select(pedido => pedido.Id));
        Assert.Empty(porCliente[999]);
    }
}
