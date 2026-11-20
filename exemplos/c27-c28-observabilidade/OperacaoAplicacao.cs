using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EngenhariaDotnet.Observabilidade;

public sealed class EstadoOperacional
{
    private int encerramentoSolicitado;

    public bool EncerramentoSolicitado => Volatile.Read(ref encerramentoSolicitado) == 1;

    public void SinalizarEncerramento() => Interlocked.Exchange(ref encerramentoSolicitado, 1);
}

public sealed class CicloVidaObservavel(
    IHostApplicationLifetime applicationLifetime,
    EstadoOperacional estado,
    ILogger<CicloVidaObservavel> logger) : IHostedService, IDisposable
{
    private CancellationTokenRegistration registroEncerramento;
    private CancellationTokenRegistration registroEncerrado;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        registroEncerramento = applicationLifetime.ApplicationStopping.Register(() =>
        {
            estado.SinalizarEncerramento();
            logger.LogWarning(Eventos.EncerramentoSolicitado,
                "Encerramento gracioso solicitado; tarefas em andamento receberam o sinal da aplicação");
        });
        registroEncerrado = applicationLifetime.ApplicationStopped.Register(() =>
            logger.LogInformation(Eventos.EncerramentoConcluido,
                "Aplicação concluiu o encerramento gracioso"));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        registroEncerramento.Dispose();
        registroEncerrado.Dispose();
    }
}

public sealed class ProntidaoEncerramentoHealthCheck(EstadoOperacional estado) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(estado.EncerramentoSolicitado
            ? HealthCheckResult.Unhealthy("Encerramento gracioso em andamento")
            : HealthCheckResult.Healthy());
}

public sealed class ProcessamentoOperacional(
    IHostApplicationLifetime applicationLifetime,
    ILogger<ProcessamentoOperacional> logger)
{
    public async Task ExecutarAsync(TimeSpan duracao, CancellationToken requestAborted)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            requestAborted,
            applicationLifetime.ApplicationStopping);

        logger.LogInformation(Eventos.OperacaoIniciada,
            "Operação local iniciada com duração configurada de {DuracaoMs} ms", duracao.TotalMilliseconds);

        try
        {
            await Task.Delay(duracao, cancellation.Token);
            logger.LogInformation(Eventos.OperacaoConcluida, "Operação local concluída");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            logger.LogWarning(Eventos.OperacaoCancelada,
                "Operação local cancelada; ApplicationStopping={ApplicationStopping}; RequestAborted={RequestAborted}",
                applicationLifetime.ApplicationStopping.IsCancellationRequested,
                requestAborted.IsCancellationRequested);
            throw;
        }
    }
}
