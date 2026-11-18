using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EngenhariaDotnet.AspNetCore;

public sealed class OperacaoTransient
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class ContextoDaRequisicao
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class EstadoGlobalSeguro
{
    private long _requisicoes;

    public long RegistrarRequisicao() => Interlocked.Increment(ref _requisicoes);
}

public sealed class ExecutorComEscopo(IServiceScopeFactory scopeFactory)
{
    public Guid Executar()
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ContextoDaRequisicao>().Id;
    }
}

public sealed class IntegracaoExternaOptions
{
    public const string Secao = "IntegracaoExterna";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(1, 30)]
    public int TimeoutSeconds { get; init; }
}

public sealed class CacheOptions
{
    public const string Secao = "Cache";

    [Range(1, 86_400)]
    public int TtlSeconds { get; init; }

    [Range(0, 3_600)]
    public int ExpirationJitterSeconds { get; init; }
}

public sealed record Produto(int Id, string Nome, decimal Preco);

public interface IRepositorioProdutos
{
    ValueTask<Produto?> ObterAsync(int id, CancellationToken cancellationToken);
    IReadOnlyList<Produto> Listar();
}

public sealed class RepositorioProdutos : IRepositorioProdutos
{
    private static readonly Produto[] Produtos =
    [
        new(1, "Teclado", 250m),
        new(2, "Mouse", 120m),
        new(3, "Monitor", 1_500m),
        new(4, "Webcam", 300m),
        new(5, "Headset", 400m)
    ];

    public ValueTask<Produto?> ObterAsync(
        int id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Produtos.SingleOrDefault(produto => produto.Id == id));
    }

    public IReadOnlyList<Produto> Listar() => Produtos;
}

public sealed class CacheAsideProdutos(
    IDistributedCache cache,
    IRepositorioProdutos repositorio,
    IOptions<CacheOptions> options,
    ILogger<CacheAsideProdutos> logger)
{
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

    public async Task<Produto?> ObterAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var key = $"produto:{id}";
        var cached = await TryReadAsync(key, cancellationToken);
        if (cached is not null)
            return cached;

        var gate = _locks.GetOrAdd(id, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            cached = await TryReadAsync(key, cancellationToken);
            if (cached is not null)
                return cached;

            var produto = await repositorio.ObterAsync(id, cancellationToken);
            if (produto is null)
                return null;

            await TryWriteAsync(key, produto, cancellationToken);
            return produto;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<Produto?> TryReadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await cache.GetAsync(key, cancellationToken);
            return bytes is null ? null : JsonSerializer.Deserialize<Produto>(bytes);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao ler a chave {CacheKey}", key);
            return null;
        }
    }

    private async Task TryWriteAsync(
        string key,
        Produto produto,
        CancellationToken cancellationToken)
    {
        try
        {
            var configured = options.Value;
            var jitter = configured.ExpirationJitterSeconds == 0
                ? 0
                : Random.Shared.Next(configured.ExpirationJitterSeconds + 1);
            var expiration = TimeSpan.FromSeconds(configured.TtlSeconds + jitter);

            await cache.SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(produto),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                },
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao escrever a chave {CacheKey}", key);
        }
    }
}

public sealed class ApiExternaClient(
    HttpClient client,
    IOptions<IntegracaoExternaOptions> options)
{
    public async Task<Produto?> ObterProdutoAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CreateTimeout(cancellationToken);
        return await client.GetFromJsonAsync<Produto>($"produtos/{id}", timeout.Token);
    }

    public async Task<HttpResponseMessage> CriarProdutoAsync(
        Produto produto,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CreateTimeout(cancellationToken);
        return await client.PostAsJsonAsync("produtos", produto, timeout.Token);
    }

    private CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        return timeout;
    }
}
