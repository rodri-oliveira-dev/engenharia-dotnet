using System.Diagnostics;

namespace EngenhariaDotnet.Microservicos;

public interface IAtraso
{
    Task AguardarAsync(TimeSpan duracao, CancellationToken cancellationToken);
}

public sealed class AtrasoReal : IAtraso
{
    public Task AguardarAsync(TimeSpan duracao, CancellationToken cancellationToken) =>
        Task.Delay(duracao, cancellationToken);
}

public sealed class RetryControlado
{
    private readonly IAtraso _atraso;
    private readonly int _maximoTentativas;
    private readonly Func<int, TimeSpan> _calcularAtraso;

    public RetryControlado(
        IAtraso atraso,
        int maximoTentativas,
        Func<int, TimeSpan> calcularAtraso)
    {
        ArgumentNullException.ThrowIfNull(atraso);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximoTentativas, 1);
        ArgumentNullException.ThrowIfNull(calcularAtraso);

        _atraso = atraso;
        _maximoTentativas = maximoTentativas;
        _calcularAtraso = calcularAtraso;
    }

    public async Task ExecutarAsync(
        Func<CancellationToken, Task> operacao,
        Func<Exception, bool> falhaTransitoria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        ArgumentNullException.ThrowIfNull(falhaTransitoria);

        for (var tentativa = 1; ; tentativa++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await operacao(cancellationToken);
                return;
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException &&
                tentativa < _maximoTentativas &&
                falhaTransitoria(ex))
            {
                await _atraso.AguardarAsync(_calcularAtraso(tentativa), cancellationToken);
            }
        }
    }
}

public static class ContextoTrace
{
    public const string TraceParent = "traceparent";
    public const string TraceState = "tracestate";

    public static void InjetarAtual(IDictionary<string, string> metadados)
    {
        var atual = Activity.Current;
        if (atual is null) return;

        metadados[TraceParent] = atual.Id!;
        if (!string.IsNullOrWhiteSpace(atual.TraceStateString))
            metadados[TraceState] = atual.TraceStateString;
    }

    public static Activity? IniciarConsumo(
        ActivitySource fonte,
        IReadOnlyDictionary<string, string> metadados,
        string nome)
    {
        var traceParent = metadados.GetValueOrDefault(TraceParent);
        var traceState = metadados.GetValueOrDefault(TraceState);

        return ActivityContext.TryParse(traceParent, traceState, out var pai)
            ? fonte.StartActivity(nome, ActivityKind.Consumer, pai)
            : fonte.StartActivity(nome, ActivityKind.Consumer);
    }
}
