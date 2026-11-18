using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EngenhariaDotnet.Observabilidade;

public sealed class CatalogoClient(HttpClient httpClient)
{
    public async Task<string> ConsultarAsync(int atrasoMs, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"/dependencia-simulada?atrasoMs={Math.Clamp(atrasoMs, 0, 2_000)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task VerificarAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("/dependencia-simulada?atrasoMs=0", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class CatalogoHealthCheck(CatalogoClient catalogo) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await catalogo.VerificarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Catálogo simulado indisponível");
        }
    }
}
