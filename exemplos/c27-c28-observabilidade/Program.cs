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

    try
    {
        var produto = await catalogo.ConsultarAsync(atrasoMs ?? 120, cancellationToken);
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
