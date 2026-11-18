using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EngenhariaDotnet.Observabilidade;

public sealed class TelemetriaAplicacao : IDisposable
{
    public const string Nome = "EngenhariaDotnet.Observabilidade";
    public static readonly ActivitySource Fonte = new(Nome);

    private readonly Meter meter;
    private readonly Counter<long> consultas;
    private readonly Histogram<double> duracao;

    public TelemetriaAplicacao(IMeterFactory meterFactory)
    {
        meter = meterFactory.Create(Nome);
        consultas = meter.CreateCounter<long>("pedidos.consultas", "{consulta}",
            "Consultas de pedidos concluídas");
        duracao = meter.CreateHistogram<double>("pedidos.consulta.duracao", "s",
            "Duração da consulta de pedido");
    }

    public void RegistrarConsulta(string resultado, TimeSpan tempo)
    {
        var tags = new TagList { { "resultado", resultado } };
        consultas.Add(1, tags);
        duracao.Record(tempo.TotalSeconds, tags);
    }

    public void Dispose() => meter.Dispose();
}

public static class Eventos
{
    public static readonly EventId ConsultaIniciada = new(2701, nameof(ConsultaIniciada));
    public static readonly EventId ConsultaConcluida = new(2702, nameof(ConsultaConcluida));
    public static readonly EventId DependenciaIndisponivel = new(2703, nameof(DependenciaIndisponivel));
    public static readonly EventId FalhaNaoTratada = new(2799, nameof(FalhaNaoTratada));
}

public static class Configuracao
{
    public static IServiceCollection AdicionarAplicacao(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton<TelemetriaAplicacao>();
        services.AddHttpClient<CatalogoClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Dependencias:Catalogo"] ?? "http://localhost:5080");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddHealthChecks()
            .AddCheck("processo", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<CatalogoHealthCheck>("catalogo-simulado", tags: ["ready"]);

        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "engenharia-dotnet-observabilidade";
        var amostragem = configuration.GetValue("Telemetry:SamplingRatio", environment.IsDevelopment() ? 1.0 : 0.25);
        var usarConsole = configuration.GetValue("Telemetry:ConsoleExporter", environment.IsDevelopment());
        var usarOtlp = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (usarOtlp)
        {
            services.AddLogging(logging => logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.AddOtlpExporter();
            }));
        }

        var openTelemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment.name", environment.EnvironmentName)
                ]))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(amostragem)))
                    .AddSource(TelemetriaAplicacao.Nome)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation();
                if (usarConsole) tracing.AddConsoleExporter();
                if (usarOtlp) tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(
                    TelemetriaAplicacao.Nome,
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http",
                    "System.Runtime");
                if (usarConsole) metrics.AddConsoleExporter();
                if (usarOtlp) metrics.AddOtlpExporter();
            });

        _ = openTelemetry;
        return services;
    }
}
