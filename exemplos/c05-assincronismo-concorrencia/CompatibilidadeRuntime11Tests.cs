using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace EngenhariaDotnet.AssincronismoConcorrencia.Tests;

public sealed class CompatibilidadeRuntime11Tests
{
    [Fact]
    public void DateOnly_TryParseExact_com_estilo_invalido_lanca_excecao()
    {
        Assert.Throws<ArgumentException>(() =>
            DateOnly.TryParseExact(
                "2026-11-20",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                (DateTimeStyles)999,
                out _));
    }

    [Fact]
    public void TimeOnly_TryParseExact_com_estilo_invalido_lanca_excecao()
    {
        Assert.Throws<ArgumentException>(() =>
            TimeOnly.TryParseExact(
                "13:30",
                "HH:mm",
                CultureInfo.InvariantCulture,
                (DateTimeStyles)999,
                out _));
    }

    [Fact]
    public void DateOnly_com_texto_invalido_e_estilo_valido_retorna_falso()
    {
        var resultado = DateOnly.TryParse(
            "texto que nao e data",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);

        Assert.False(resultado);
    }

    [Fact]
    public async Task RunAsync_propagara_falha_do_background_service_no_dotnet11()
    {
        using var host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.Configure<HostOptions>(options =>
                    options.BackgroundServiceExceptionBehavior =
                        BackgroundServiceExceptionBehavior.StopHost);
                services.AddHostedService<ServicoQueFalha>();
            })
            .Build();

        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.RunAsync());

        Assert.Equal("Falha controlada do worker", excecao.Message);
    }

    private sealed class ServicoQueFalha : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("Falha controlada do worker");
        }
    }
}
