using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EngenhariaDotnet.Persistencia;

public sealed class PersistenciaTests
{
    [Fact]
    public async Task Tracking_e_identity_resolution_possuem_custos_distintos()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        var comTracking = await context.Itens.Select(item => item.Pedido).ToListAsync();
        Assert.Same(comTracking[0], comTracking[1]);
        Assert.NotEmpty(context.ChangeTracker.Entries());

        context.ChangeTracker.Clear();
        var semTracking = await context.Itens.AsNoTracking().Select(item => item.Pedido).ToListAsync();
        Assert.NotSame(semTracking[0], semTracking[1]);
        Assert.Empty(context.ChangeTracker.Entries());

        var identidadeSemTracking = await context.Itens
            .AsNoTrackingWithIdentityResolution()
            .Select(item => item.Pedido)
            .ToListAsync();
        Assert.Same(identidadeSemTracking[0], identidadeSemTracking[1]);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Projecao_corrige_N_mais_1_com_uma_consulta()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        banco.Contador.Zerar();
        var pedidos = await context.Pedidos.AsNoTracking().ToListAsync();
        foreach (var pedido in pedidos)
        {
            _ = await context.Itens.CountAsync(item => item.PedidoId == pedido.Id);
        }

        Assert.Equal(1 + pedidos.Count, banco.Contador.Quantidade);

        banco.Contador.Zerar();
        var resumos = await context.Pedidos
            .AsNoTracking()
            .Select(pedido => new { pedido.Id, Quantidade = pedido.Itens.Count })
            .ToListAsync();

        Assert.Equal(pedidos.Count, resumos.Count);
        Assert.Equal(1, banco.Contador.Quantidade);
    }

    [Fact]
    public async Task SplitQuery_troca_um_join_por_uma_consulta_para_cada_colecao()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        banco.Contador.Zerar();
        _ = await context.Pedidos
            .Include(pedido => pedido.Itens)
            .Include(pedido => pedido.Marcadores)
            .AsSingleQuery()
            .ToListAsync();
        Assert.Equal(1, banco.Contador.Quantidade);

        context.ChangeTracker.Clear();
        banco.Contador.Zerar();
        var pedidos = await context.Pedidos
            .Include(pedido => pedido.Itens)
            .Include(pedido => pedido.Marcadores)
            .AsSplitQuery()
            .ToListAsync();

        Assert.Equal(3, banco.Contador.Quantidade);
        Assert.All(pedidos, pedido => Assert.NotEmpty(pedido.Itens));
        Assert.All(pedidos, pedido => Assert.NotEmpty(pedido.Marcadores));
    }

    [Fact]
    public async Task Paginacao_limita_e_projeta_no_SQL()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();

        var consulta = context.Pedidos
            .AsNoTracking()
            .OrderBy(pedido => pedido.Id)
            .Skip(1)
            .Take(1)
            .Select(pedido => new { pedido.Id, pedido.Descricao });
        var sql = consulta.ToQueryString();
        var pagina = await Consultas.PaginarAsync(context, pagina: 2, tamanho: 1);

        Assert.Single(pagina);
        Assert.DoesNotContain("Observacoes", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Conflito_otimista_pode_ser_mesclado_com_valores_do_banco()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var primeiro = banco.NovoContexto();
        await using var segundo = banco.NovoContexto();

        var pedidoA = await primeiro.Pedidos.SingleAsync(pedido => pedido.Id == 1);
        var pedidoB = await segundo.Pedidos.SingleAsync(pedido => pedido.Id == 1);

        pedidoA.Descricao = "alterado pelo primeiro editor";
        await primeiro.SaveChangesAsync();

        pedidoB.Status = "separando";
        var conflito = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => segundo.SaveChangesAsync());

        var entry = conflito.Entries.Single();
        var statusDoCliente = entry.CurrentValues.GetValue<string>(nameof(Pedido.Status));
        var valoresDoBanco = await entry.GetDatabaseValuesAsync();
        Assert.NotNull(valoresDoBanco);

        entry.CurrentValues.SetValues(valoresDoBanco);
        entry.CurrentValues[nameof(Pedido.Status)] = statusDoCliente;
        entry.OriginalValues.SetValues(valoresDoBanco);
        await segundo.SaveChangesAsync();

        await using var verificacao = banco.NovoContexto();
        var persistido = await verificacao.Pedidos.AsNoTracking().SingleAsync(pedido => pedido.Id == 1);
        Assert.Equal("alterado pelo primeiro editor", persistido.Descricao);
        Assert.Equal("separando", persistido.Status);
    }

    [Fact]
    public async Task Plano_do_SQLite_confirma_uso_do_indice_do_filtro()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();
        var sql = context.Pedidos.Where(pedido => pedido.Status == "aberto").ToQueryString();

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN SELECT Id FROM Pedidos WHERE Status = $status";
        command.Parameters.Add(new SqliteParameter("$status", "aberto"));
        await context.Database.OpenConnectionAsync();
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var detalhe = reader.GetString(3);

        Assert.Contains("IX_Pedidos_Status", detalhe, StringComparison.Ordinal);
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Compiled_query_assincrona_preserva_projecao_e_parametro()
    {
        await using var banco = await BancoDeTeste.CriarAsync();
        await using var context = banco.NovoContexto();
        var resumos = new List<ResumoPedido>();

        await foreach (var resumo in Consultas.PorStatusCompilada(context, "aberto"))
        {
            resumos.Add(resumo);
        }

        Assert.Equal(2, resumos.Count);
        Assert.All(resumos, resumo => Assert.Equal(2, resumo.QuantidadeDeItens));
        Assert.Empty(context.ChangeTracker.Entries());
    }
}

internal sealed class BancoDeTeste : IAsyncDisposable
{
    private readonly string _arquivo;
    private readonly DbContextOptions<LojaContext> _options;

    private BancoDeTeste(string arquivo, ContadorDeComandos contador)
    {
        _arquivo = arquivo;
        Contador = contador;
        _options = new DbContextOptionsBuilder<LojaContext>()
            .UseSqlite($"Data Source={arquivo}")
            .AddInterceptors(contador)
            .Options;
    }

    public ContadorDeComandos Contador { get; }

    public static async Task<BancoDeTeste> CriarAsync()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), $"engenharia-dotnet-{Guid.NewGuid():N}.db");
        var banco = new BancoDeTeste(arquivo, new ContadorDeComandos());
        await using var context = banco.NovoContexto();
        await context.Database.EnsureCreatedAsync();
        context.Pedidos.AddRange(
            CriarPedido("primeiro", "aberto", 120m),
            CriarPedido("segundo", "aberto", 80m));
        await context.SaveChangesAsync();
        return banco;
    }

    public LojaContext NovoContexto() => new(_options);

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_arquivo))
        {
            File.Delete(_arquivo);
        }

        return ValueTask.CompletedTask;
    }

    private static Pedido CriarPedido(string descricao, string status, decimal total)
    {
        var pedido = new Pedido
        {
            Descricao = descricao,
            Status = status,
            Total = total,
            Observacoes = new string('x', 1_000)
        };
        pedido.Itens.Add(new ItemPedido { Produto = "teclado", Quantidade = 1 });
        pedido.Itens.Add(new ItemPedido { Produto = "mouse", Quantidade = 2 });
        pedido.Marcadores.Add(new Marcador { Nome = "prioridade" });
        pedido.Marcadores.Add(new Marcador { Nome = "varejo" });
        return pedido;
    }
}
