using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace EngenhariaDotnet.Observabilidade.Tests;

public sealed class ObservabilidadeTests
{
    [Fact]
    public async Task Liveness_nao_depende_do_catalogo()
    {
        await using var factory = CriarFactory("http://127.0.0.1:1");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_detecta_dependencia_indisponivel()
    {
        await using var factory = CriarFactory("http://127.0.0.1:1");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Consulta_retorna_503_quando_dependencia_falha()
    {
        await using var factory = CriarFactory("http://127.0.0.1:1");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/pedidos/11111111-1111-1111-1111-111111111111?atrasoMs=0");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Consulta_retorna_503_quando_dependencia_excede_timeout()
    {
        await using var factory = CriarFactoryComDependenciaLenta();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/pedidos/11111111-1111-1111-1111-111111111111?atrasoMs=0");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Sinal_de_encerramento_altera_prontidao()
    {
        using var lifetime = new ApplicationLifetimeTeste();
        var estado = new EstadoOperacional();
        using var ciclo = new CicloVidaObservavel(
            lifetime,
            estado,
            NullLogger<CicloVidaObservavel>.Instance);
        await ciclo.StartAsync(CancellationToken.None);
        var healthCheck = new ProntidaoEncerramentoHealthCheck(estado);

        lifetime.StopApplication();
        var resultado = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.True(estado.EncerramentoSolicitado);
        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
    }

    [Fact]
    public async Task Processamento_observa_encerramento_da_aplicacao()
    {
        using var lifetime = new ApplicationLifetimeTeste();
        var processamento = new ProcessamentoOperacional(
            lifetime,
            NullLogger<ProcessamentoOperacional>.Instance);
        var operacao = processamento.ExecutarAsync(TimeSpan.FromMinutes(1), CancellationToken.None);

        lifetime.StopApplication();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operacao.WaitAsync(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Processamento_observa_cancelamento_da_requisicao()
    {
        using var lifetime = new ApplicationLifetimeTeste();
        using var requestCancellation = new CancellationTokenSource();
        var processamento = new ProcessamentoOperacional(
            lifetime,
            NullLogger<ProcessamentoOperacional>.Instance);
        var operacao = processamento.ExecutarAsync(TimeSpan.FromMinutes(1), requestCancellation.Token);

        await requestCancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operacao.WaitAsync(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Logs_warning_ou_superior_sao_configurados_para_stderr()
    {
        await using var factory = CriarFactory("http://127.0.0.1:1");
        var options = factory.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;

        Assert.Equal(LogLevel.Warning, options.LogToStandardErrorThreshold);
    }

    [Fact]
    public async Task Timeout_de_shutdown_e_configuravel()
    {
        await using var factory = CriarFactory("http://127.0.0.1:1");
        var options = factory.Services.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(8), options.ShutdownTimeout);
    }

    [Fact]
    public void Metrica_usa_resultado_de_baixa_cardinalidade()
    {
        using var meterProvider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var factory = meterProvider.GetRequiredService<IMeterFactory>();
        using var telemetria = new TelemetriaAplicacao(factory);
        KeyValuePair<string, object?>[]? tagsObservadas = null;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == TelemetriaAplicacao.Nome) meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => tagsObservadas = tags.ToArray());
        listener.Start();

        telemetria.RegistrarConsulta("sucesso", TimeSpan.FromMilliseconds(10));

        Assert.Contains(tagsObservadas!, tag => tag.Key == "resultado" && Equals(tag.Value, "sucesso"));
        Assert.DoesNotContain(tagsObservadas!, tag => tag.Key.Contains("pedido", StringComparison.OrdinalIgnoreCase));
    }

    private static WebApplicationFactory<Program> CriarFactory(string catalogo) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Dependencias:Catalogo"] = catalogo,
                ["Dependencias:CatalogoTimeout"] = "00:00:00.2",
                ["Telemetry:ConsoleExporter"] = "false",
                ["Telemetry:SamplingRatio"] = "1.0"
            })));

    private static WebApplicationFactory<Program> CriarFactoryComDependenciaLenta() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Dependencias:Catalogo"] = "http://catalogo-simulado",
                    ["Dependencias:CatalogoTimeout"] = "00:00:00.05",
                    ["Telemetry:ConsoleExporter"] = "false",
                    ["Telemetry:SamplingRatio"] = "1.0"
                }));
            builder.ConfigureTestServices(services => services
                .AddHttpClient<CatalogoClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new HandlerLento()));
        });

    private sealed class HandlerLento : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class ApplicationLifetimeTeste : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource started = new();
        private readonly CancellationTokenSource stopping = new();
        private readonly CancellationTokenSource stopped = new();

        public CancellationToken ApplicationStarted => started.Token;
        public CancellationToken ApplicationStopping => stopping.Token;
        public CancellationToken ApplicationStopped => stopped.Token;

        public void StopApplication() => stopping.Cancel();

        public void Dispose()
        {
            started.Dispose();
            stopping.Dispose();
            stopped.Dispose();
        }
    }
}
