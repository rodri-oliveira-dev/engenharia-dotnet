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
    [Fact]
    public void FullJoin_inclui_clientes_e_pedidos_sem_correspondencia_e_preserva_duplicatas()
    {
        Cliente[] clientes = [new(1, "Ada"), new(2, "Bia")];
        Pedido[] pedidos = [new(10, 1, 20m), new(11, 1, 30m), new(12, 3, 40m)];

        var inner = clientes.Join(pedidos, cliente => cliente.Id, pedido => pedido.ClienteId);
        var left = clientes.LeftJoin(pedidos, cliente => cliente.Id, pedido => pedido.ClienteId);
        var completo = Consultas.ConciliarClientesPedidos(clientes, pedidos);

        Assert.Equal(2, inner.Count());
        Assert.Equal(3, left.Count());
        Assert.Equal(4, completo.Count);
        Assert.Equal(2, completo.Count(item => item.ClienteId == 1));
        Assert.Contains(completo, item =>
            item.ClienteId == 2 && item.NomeCliente == "Bia" && item.PedidoId is null);
        Assert.Contains(completo, item =>
            item.ClienteId == 3 && item.NomeCliente is null && item.PedidoId == 12);
    }

    [Fact]
    public void FullJoin_cobre_fontes_vazias_sem_perder_registros_orfaos()
    {
        Assert.Empty(Consultas.ConciliarClientesPedidos([], []));
        var semPedidos = Assert.Single(Consultas.ConciliarClientesPedidos(
            [new Cliente(1, "Ada")], []));
        var semClientes = Assert.Single(Consultas.ConciliarClientesPedidos(
            [], [new Pedido(10, 3, 40m)]));

        Assert.Null(semPedidos.PedidoId);
        Assert.Equal("Ada", semPedidos.NomeCliente);
        Assert.Null(semClientes.NomeCliente);
        Assert.Equal(10, semClientes.PedidoId);
    }

    [Fact]
    public void Colecao_com_comparer_remove_duplicatas_independentemente_de_caixa()
    {
        var etiquetas = Consultas.CriarEtiquetasSemDuplicidade(
            ["Backend", "backend", "Arquitetura"]);

        Assert.Equal(2, etiquetas.Count);
        Assert.Contains("BACKEND", etiquetas);
        Assert.Contains("arquitetura", etiquetas);
        Assert.Same(StringComparer.OrdinalIgnoreCase, etiquetas.Comparer);
    }

    [Fact]
    public void Colecao_com_reserva_mantem_ordem_e_capacidade()
    {
        var pedidos = new[] { Pedidos[2], Pedidos[0] };
        var lote = Consultas.CriarLoteComCapacidade(pedidos, reserva: 3);

        Assert.Equal(pedidos, lote);
        Assert.Equal(5, lote.Capacity);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Consultas.CriarLoteComCapacidade(pedidos, -1));
    }

}
