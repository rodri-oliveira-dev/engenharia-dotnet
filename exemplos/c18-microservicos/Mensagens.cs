namespace EngenhariaDotnet.Microservicos;

public interface IEventoIntegracao
{
    Guid MensagemId { get; }
    DateTimeOffset OcorridoEm { get; }
}

public sealed record PedidoCriado(
    Guid MensagemId,
    DateTimeOffset OcorridoEm,
    Guid PedidoId,
    decimal Total) : IEventoIntegracao;

public interface IPublicadorEventos
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : IEventoIntegracao;
}

public sealed class NotificadorPedido(IPublicadorEventos publicador, TimeProvider relogio)
{
    public Task PublicarCriacaoAsync(Guid pedidoId, decimal total, CancellationToken cancellationToken)
    {
        var evento = new PedidoCriado(
            Guid.NewGuid(), relogio.GetUtcNow(), pedidoId, total);

        return publicador.PublicarAsync(evento, cancellationToken);
    }
}
