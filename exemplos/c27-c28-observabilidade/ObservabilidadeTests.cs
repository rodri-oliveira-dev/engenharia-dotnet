using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                ["Telemetry:ConsoleExporter"] = "false",
                ["Telemetry:SamplingRatio"] = "1.0"
            })));
}
