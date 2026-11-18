using Microsoft.EntityFrameworkCore;

namespace EngenhariaDotnet.Arquitetura;

public sealed class Pedido
{
    private Pedido() { }

    public Pedido(Guid id, string clienteId, decimal total)
    {
        if (id == Guid.Empty) throw new ArgumentException("O pedido precisa de identidade.", nameof(id));
        if (string.IsNullOrWhiteSpace(clienteId)) throw new ArgumentException("Cliente é obrigatório.", nameof(clienteId));
        if (total <= 0) throw new ArgumentOutOfRangeException(nameof(total));
        Id = id;
        ClienteId = clienteId;
        Total = total;
        Status = "Criado";
    }

    public Guid Id { get; private set; }
    public string ClienteId { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public string Status { get; private set; } = string.Empty;
}

public sealed class EventoOutbox
{
    private EventoOutbox() { }

    public EventoOutbox(Guid id, string tipo, string payload, DateTimeOffset ocorridoEm)
    {
        Id = id;
        Tipo = tipo;
        Payload = payload;
        OcorridoEm = ocorridoEm;
    }

    public Guid Id { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OcorridoEm { get; private set; }
    public DateTimeOffset? PublicadoEm { get; private set; }
    public int Tentativas { get; private set; }

    public void RegistrarTentativa() => Tentativas++;
    public void MarcarPublicado(DateTimeOffset instante) => PublicadoEm = instante;
}

public sealed class MensagemProcessada
{
    private MensagemProcessada() { }
    public MensagemProcessada(Guid mensagemId, DateTimeOffset processadaEm)
    {
        MensagemId = mensagemId;
        ProcessadaEm = processadaEm;
    }

    public Guid MensagemId { get; private set; }
    public DateTimeOffset ProcessadaEm { get; private set; }
}

public sealed class PedidoProjetado
{
    private PedidoProjetado() { }
    public PedidoProjetado(Guid pedidoId, int notificacoes) => (PedidoId, Notificacoes) = (pedidoId, notificacoes);
    public Guid PedidoId { get; private set; }
    public int Notificacoes { get; private set; }
    public void RegistrarNotificacao() => Notificacoes++;
}

public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<EventoOutbox> Outbox => Set<EventoOutbox>();
    public DbSet<MensagemProcessada> MensagensProcessadas => Set<MensagemProcessada>();
    public DbSet<PedidoProjetado> PedidosProjetados => Set<PedidoProjetado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>().HasKey(x => x.Id);
        modelBuilder.Entity<EventoOutbox>().HasKey(x => x.Id);
        modelBuilder.Entity<MensagemProcessada>().HasKey(x => x.MensagemId);
        modelBuilder.Entity<PedidoProjetado>().HasKey(x => x.PedidoId);
    }
}

public sealed record CriarPedido(Guid PedidoId, string ClienteId, decimal Total);
public sealed record PedidoResumo(Guid Id, string ClienteId, decimal Total, string Status);

// Command side: estado e evento entram no mesmo SaveChanges/transação local.
public sealed class CriarPedidoHandler(PedidosDbContext db, TimeProvider relogio)
{
    public async Task HandleAsync(CriarPedido comando, CancellationToken cancellationToken)
    {
        var pedido = new Pedido(comando.PedidoId, comando.ClienteId, comando.Total);
        var evento = new EventoOutbox(
            Guid.NewGuid(),
            "PedidoCriado.v1",
            $"{pedido.Id}|{pedido.Total}",
            relogio.GetUtcNow());

        db.Pedidos.Add(pedido);
        db.Outbox.Add(evento);
        await db.SaveChangesAsync(cancellationToken);
    }
}

// Query side: projeção própria, sem carregar o agregado para leitura.
public sealed class ConsultarPedido(PedidosDbContext db)
{
    public Task<PedidoResumo?> PorIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Pedidos.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PedidoResumo(x.Id, x.ClienteId, x.Total, x.Status))
            .SingleOrDefaultAsync(cancellationToken);
}

public interface IBusEventos
{
    Task PublicarAsync(Guid mensagemId, string tipo, string payload, CancellationToken cancellationToken);
}

public sealed class PublicadorOutbox(PedidosDbContext db, IBusEventos bus, TimeProvider relogio)
{
    public async Task<int> PublicarPendentesAsync(CancellationToken cancellationToken)
    {
        var pendentes = (await db.Outbox
            .Where(x => x.PublicadoEm == null)
            .ToListAsync(cancellationToken))
            .OrderBy(x => x.OcorridoEm)
            .ThenBy(x => x.Id)
            .ToList();

        foreach (var evento in pendentes)
        {
            evento.RegistrarTentativa();
            await db.SaveChangesAsync(cancellationToken);
            await bus.PublicarAsync(evento.Id, evento.Tipo, evento.Payload, cancellationToken);
            evento.MarcarPublicado(relogio.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }

        return pendentes.Count;
    }
}

public sealed class ConsumidorPedidoCriado(PedidosDbContext db, TimeProvider relogio)
{
    public async Task<bool> ProcessarAsync(Guid mensagemId, Guid pedidoId, CancellationToken cancellationToken)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.MensagensProcessadas.AnyAsync(x => x.MensagemId == mensagemId, cancellationToken))
            return false;

        var projecao = await db.PedidosProjetados.SingleOrDefaultAsync(x => x.PedidoId == pedidoId, cancellationToken);
        if (projecao is null)
            db.PedidosProjetados.Add(new PedidoProjetado(pedidoId, 1));
        else
            projecao.RegistrarNotificacao();

        db.MensagensProcessadas.Add(new MensagemProcessada(mensagemId, relogio.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
        return true;
    }
}
