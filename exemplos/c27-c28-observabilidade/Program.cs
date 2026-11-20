using System.Diagnostics;
using EngenhariaDotnet.Observabilidade;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AdicionarAplicacao(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("FalhasNaoTratadas");
    logger.LogError(Eventos.FalhaNaoTratada, "Falha não tratada no processamento HTTP");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { erro = "falha_interna", traceId = Activity.Current?.TraceId.ToString() });
}));

app.MapGet("/pedidos/{pedidoId:guid}", async (
    Guid pedidoId,
    int? atrasoMs,
    CatalogoClient catalogo,
    TelemetriaAplicacao telemetria,
    IHostApplicationLifetime applicationLifetime,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    using var escopo = logger.BeginScope(new Dictionary<string, object>
    {
        ["pedido.id"] = pedidoId,
        ["trace.id"] = Activity.Current?.TraceId.ToString() ?? string.Empty
    });
    using var atividade = TelemetriaAplicacao.Fonte.StartActivity("pedidos.consultar");
    atividade?.SetTag("pedido.id", pedidoId.ToString());

    logger.LogInformation(Eventos.ConsultaIniciada, "Consulta do pedido iniciada");
    var inicio = Stopwatch.GetTimestamp();
    using var operacaoCancellation = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken,
        applicationLifetime.ApplicationStopping);

    try
    {
        var produto = await catalogo.ConsultarAsync(atrasoMs ?? 120, operacaoCancellation.Token);
        telemetria.RegistrarConsulta("sucesso", Stopwatch.GetElapsedTime(inicio));
        logger.LogInformation(Eventos.ConsultaConcluida, "Consulta do pedido concluída com {QuantidadeItens} item(ns)", 1);
        return Results.Ok(new { pedidoId, produto, traceId = Activity.Current?.TraceId.ToString() });
    }
    catch (HttpRequestException ex)
    {
        telemetria.RegistrarConsulta("falha_dependencia", Stopwatch.GetElapsedTime(inicio));
        atividade?.SetStatus(ActivityStatusCode.Error, "dependencia_indisponivel");
        logger.LogWarning(Eventos.DependenciaIndisponivel, ex,
            "Dependência {Dependencia} indisponível", "catalogo-simulado");
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Dependência indisponível", extensions: new Dictionary<string, object?>
            {
                ["traceId"] = Activity.Current?.TraceId.ToString()
            });
    }
    catch (OperationCanceledException ex) when (
        applicationLifetime.ApplicationStopping.IsCancellationRequested ||
        cancellationToken.IsCancellationRequested)
    {
        telemetria.RegistrarConsulta("cancelada", Stopwatch.GetElapsedTime(inicio));
        logger.LogWarning(Eventos.OperacaoCancelada, ex,
            "Consulta cancelada; ApplicationStopping={ApplicationStopping}; RequestAborted={RequestAborted}",
            applicationLifetime.ApplicationStopping.IsCancellationRequested,
            cancellationToken.IsCancellationRequested);

        return applicationLifetime.ApplicationStopping.IsCancellationRequested
            ? Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Encerramento em andamento")
            : Results.StatusCode(499);
    }
    catch (OperationCanceledException ex)
    {
        telemetria.RegistrarConsulta("timeout_dependencia", Stopwatch.GetElapsedTime(inicio));
        atividade?.SetStatus(ActivityStatusCode.Error, "timeout_dependencia");
        logger.LogWarning(Eventos.DependenciaTimeout, ex,
            "Tempo limite excedido ao consultar a dependência {Dependencia}", "catalogo-simulado");
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Tempo limite da dependência excedido", extensions: new Dictionary<string, object?>
            {
                ["traceId"] = Activity.Current?.TraceId.ToString()
            });
    }
});

// Dependência deliberadamente simulada: não representa resultado de performance.
app.MapGet("/dependencia-simulada", async (int? atrasoMs, bool? falhar, CancellationToken cancellationToken) =>
{
    var atraso = Math.Clamp(atrasoMs ?? 120, 0, 2_000);
    await Task.Delay(atraso, cancellationToken);
    return falhar is true
        ? Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Falha simulada")
        : Results.Ok(new { produtoId = "produto-demonstracao", atrasoSimuladoMs = atraso });
});

// Atraso local controlado: demonstra cancelamento, não representa trabalho ou infraestrutura real.
app.MapGet("/operacoes/demorada", async (
    int? duracaoMs,
    ProcessamentoOperacional processamento,
    IHostApplicationLifetime applicationLifetime,
    CancellationToken requestAborted) =>
{
    var duracao = TimeSpan.FromMilliseconds(Math.Clamp(duracaoMs ?? 5_000, 0, 30_000));

    try
    {
        await processamento.ExecutarAsync(duracao, requestAborted);
        return Results.Ok(new { resultado = "concluida", duracaoMs = duracao.TotalMilliseconds });
    }
    catch (OperationCanceledException) when (applicationLifetime.ApplicationStopping.IsCancellationRequested)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Encerramento em andamento");
    }
    catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
    {
        return Results.StatusCode(499);
    }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).ShortCircuit();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).ShortCircuit();

app.Run();

public partial class Program;
