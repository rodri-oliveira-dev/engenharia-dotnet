namespace EngenhariaDotnet.Testes;

public sealed class ProcessadorAssincrono
{
    public async Task ProcessarAsync(
        Func<CancellationToken, Task> manipulador,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manipulador);
        cancellationToken.ThrowIfCancellationRequested();
        await manipulador(cancellationToken).ConfigureAwait(false);
    }
}
