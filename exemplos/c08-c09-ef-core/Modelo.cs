using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EngenhariaDotnet.Persistencia;

public sealed class LojaContext(DbContextOptions<LojaContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> Itens => Set<ItemPedido>();
    public DbSet<Marcador> Marcadores => Set<Marcador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(pedido =>
        {
            pedido.Property(x => x.Descricao).HasMaxLength(200);
            pedido.Property(x => x.Status).HasMaxLength(30);
            pedido.Property(x => x.Versao).IsConcurrencyToken();
            pedido.HasIndex(x => x.Status);
            pedido.HasMany(x => x.Itens).WithOne(x => x.Pedido).HasForeignKey(x => x.PedidoId);
            pedido.HasMany(x => x.Marcadores).WithOne(x => x.Pedido).HasForeignKey(x => x.PedidoId);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AtualizarTokens();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AtualizarTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AtualizarTokens()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries<Pedido>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.Versao = Guid.NewGuid();
        }
    }
}

public sealed class Pedido
{
    public int Id { get; set; }
    public required string Descricao { get; set; }
    public required string Status { get; set; }
    public decimal Total { get; set; }
    public string? Observacoes { get; set; }
    public Guid Versao { get; set; }
    public List<ItemPedido> Itens { get; } = [];
    public List<Marcador> Marcadores { get; } = [];
}

public sealed class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public required string Produto { get; set; }
    public int Quantidade { get; set; }
    public Pedido Pedido { get; set; } = null!;
}

public sealed class Marcador
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public required string Nome { get; set; }
    public Pedido Pedido { get; set; } = null!;
}

public sealed class ContadorDeComandos : DbCommandInterceptor
{
    public int Quantidade { get; private set; }

    public void Zerar() => Quantidade = 0;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Quantidade++;
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Quantidade++;
        return ValueTask.FromResult(result);
    }
}
