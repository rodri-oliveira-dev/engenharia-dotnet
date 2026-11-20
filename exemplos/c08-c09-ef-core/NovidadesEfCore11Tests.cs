using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EngenhariaDotnet.Persistencia;

public sealed class NovidadesEfCore11Tests
{
    [Fact]
    public async Task FullJoin_preserva_clientes_e_pedidos_sem_correspondencia()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        context.ClientesExternos.AddRange(
            new ClienteExterno { Codigo = "001", Nome = "Ana" },
            new ClienteExterno { Codigo = "002", Nome = "Beto" });
        context.PedidosExternos.AddRange(
            new PedidoExterno { CodigoCliente = "001", Descricao = "Compra A" },
            new PedidoExterno { CodigoCliente = "003", Descricao = "Compra órfã" });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var consulta = NovidadesEfCore11.Conciliar(context).OrderBy(linha => linha.Codigo);
        var sql = consulta.ToQueryString();
        banco.Contador.Zerar();
        var linhas = await consulta.ToListAsync();

        Assert.Contains("FULL JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, banco.Contador.Quantidade);
        Assert.Collection(linhas,
            linha =>
            {
                Assert.Equal("001", linha.Codigo);
                Assert.Equal("Ana", linha.Cliente);
                Assert.Equal("Compra A", linha.Pedido);
            },
            linha =>
            {
                Assert.Equal("002", linha.Codigo);
                Assert.Equal("Beto", linha.Cliente);
                Assert.Null(linha.Pedido);
            },
            linha =>
            {
                Assert.Equal("003", linha.Codigo);
                Assert.Null(linha.Cliente);
                Assert.Equal("Compra órfã", linha.Pedido);
            });
    }

    [Fact]
    public async Task MaxBy_e_MinBy_selecionam_entidades_e_executam_no_banco()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        var primeiro = await context.Pedidos
            .Include(pedido => pedido.Itens)
            .SingleAsync(pedido => pedido.Descricao == "primeiro");
        primeiro.Itens.Add(new ItemPedido { Produto = "monitor", Quantidade = 1 });
        context.Pedidos.Add(new Pedido
        {
            Descricao = "vazio",
            Status = "fechado",
            Total = 0m
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        banco.Contador.Zerar();

        var comMaisItens = await context.Pedidos
            .AsNoTracking()
            .MaxByAsync(pedido => pedido.Itens.Count);
        var sqlMax = banco.Contador.Comandos.Single();
        banco.Contador.Zerar();

        var comMenosItens = await context.Pedidos
            .AsNoTracking()
            .MinByAsync(pedido => pedido.Itens.Count);
        var sqlMin = banco.Contador.Comandos.Single();

        Assert.NotNull(comMaisItens);
        Assert.NotNull(comMenosItens);
        Assert.Equal("primeiro", comMaisItens.Descricao);
        Assert.Equal("vazio", comMenosItens.Descricao);
        Assert.Contains("ORDER BY", sqlMax, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DESC", sqlMax, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT", sqlMax, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sqlMin, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT", sqlMin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GroupBy_com_navegacao_e_agregados_e_traduzido_para_SQL()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        var fechado = new Pedido { Descricao = "terceiro", Status = "fechado", Total = 10m };
        fechado.Itens.Add(new ItemPedido { Produto = "caderno", Quantidade = 4 });
        context.Pedidos.Add(fechado);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var consulta = NovidadesEfCore11.AgruparItensPorStatus(context)
            .OrderBy(resumo => resumo.Status);
        var sql = consulta.ToQueryString();
        banco.Contador.Zerar();
        var dados = await consulta.ToListAsync();

        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SUM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, banco.Contador.Quantidade);
        Assert.Collection(dados,
            aberto =>
            {
                Assert.Equal("aberto", aberto.Status);
                Assert.Equal(4, aberto.NumeroDeItens);
                Assert.Equal(6, aberto.QuantidadeTotal);
            },
            encerrado =>
            {
                Assert.Equal("fechado", encerrado.Status);
                Assert.Equal(1, encerrado.NumeroDeItens);
                Assert.Equal(4, encerrado.QuantidadeTotal);
            });
    }

    [Fact]
    public async Task StringJoin_ordenado_usa_group_concat_no_SQLite()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        var consulta = NovidadesEfCore11.MarcadoresOrdenados(context);
        var sql = consulta.ToQueryString();
        banco.Contador.Zerar();
        var resumos = await consulta.ToListAsync();

        Assert.Contains("group_concat(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, banco.Contador.Quantidade);
        Assert.Equal(2, resumos.Count);
        Assert.All(resumos, resumo => Assert.Equal("prioridade, varejo", resumo.Marcadores));
    }

    [Fact]
    public async Task SplitQuery_elimina_join_de_referencia_na_consulta_de_colecao()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        var categoria = new CategoriaPedido { Nome = "varejo" };
        var pedido = await context.Pedidos.SingleAsync(p => p.Descricao == "primeiro");
        pedido.Categoria = categoria;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        banco.Contador.Zerar();
        var pedidos = await context.Pedidos
            .Include(p => p.Categoria)
            .Include(p => p.Itens)
            .AsSplitQuery()
            .ToListAsync();

        Assert.Equal(2, banco.Contador.Quantidade);
        Assert.Contains("Categorias", banco.Contador.Comandos[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Itens", banco.Contador.Comandos[1], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Categorias", banco.Contador.Comandos[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(pedidos, p => p.Categoria?.Nome == "varejo" && p.Itens.Count == 2);
    }

    [Fact]
    public void JsonPathExists_gera_SQL_Server_2022_sem_acessar_banco()
    {
        // Este teste só verifica a tradução SQL. Não valida execução, índices ou plano.
        var opcoes = new DbContextOptionsBuilder<LojaContext>()
            .UseSqlServer("Server=localhost;Database=Ef11Laboratorio;Integrated Security=true;TrustServerCertificate=true")
            .Options;
        using var context = new LojaContext(opcoes);

        var sql = context.Pedidos
            .Where(p => p.Observacoes != null
                && EF.Functions.JsonPathExists(p.Observacoes, "$.prioridade"))
            .Select(p => p.Id)
            .ToQueryString();

        Assert.Contains("JSON_PATH_EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("$.prioridade", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MigrateAsync_sem_migrations_falha_explicitamente_no_EF11()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Database.MigrateAsync());
    }
}
