using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EngenhariaDotnet.AspNetCore.Tests;

public sealed class AplicacaoTests
{
    [Fact]
    public async Task Lifetimes_respeitam_fronteiras_de_resolucao()
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();

        var primeira = await client.GetFromJsonAsync<DiagnosticoLifetimes>(
            "/diagnostico/lifetimes");
        var segunda = await client.GetFromJsonAsync<DiagnosticoLifetimes>(
            "/diagnostico/lifetimes");

        Assert.NotNull(primeira);
        Assert.NotNull(segunda);
        Assert.NotEqual(primeira.Transient1, primeira.Transient2);
        Assert.NotEqual(primeira.Scoped, segunda.Scoped);
        Assert.Equal(primeira.ContagemSingleton + 1, segunda.ContagemSingleton);
    }

    [Fact]
    public void Validacao_do_container_rejeita_scoped_capturado_por_singleton()
    {
        var services = new ServiceCollection();
        services.AddScoped<ContextoDaRequisicao>();
        services.AddSingleton<SingletonCapturandoScoped>();

        var exception = Assert.Throws<AggregateException>(() =>
            services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            }));

        Assert.Contains("scoped", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Excecao_conhecida_vira_problem_details_com_correlation_id()
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "teste-123");

        using var response = await client.GetAsync("/produtos/999");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("teste-123", response.Headers.GetValues(
            CorrelationIdMiddleware.HeaderName).Single());
        Assert.Equal("teste-123", document.RootElement
            .GetProperty("correlationId").GetString());
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Configuracao_invalida_impede_startup()
    {
        await using var app = CriarAplicacaoDeTeste(new Dictionary<string, string?>
        {
            ["IntegracaoExterna:BaseUrl"] = "http://inseguro.invalid/"
        });

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => app.StartAsync());

        Assert.Contains("HTTPS", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cliente_externo_repete_get_transiente_com_limite()
    {
        var handler = new HandlerSequencial(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.OK);
        using var provider = CriarProviderDoCliente(handler);
        var client = provider.GetRequiredService<ApiExternaClient>();

        var produto = await client.ObterProdutoAsync(1);

        Assert.NotNull(produto);
        Assert.Equal(3, handler.Chamadas);
    }

    [Fact]
    public async Task Cliente_externo_nao_repete_post_nao_idempotente()
    {
        var handler = new HandlerSequencial(HttpStatusCode.ServiceUnavailable);
        using var provider = CriarProviderDoCliente(handler);
        var client = provider.GetRequiredService<ApiExternaClient>();

        using var response = await client.CriarProdutoAsync(
            new Produto(10, "Novo", 10m));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task Cliente_externo_propaga_cancelamento_do_chamador()
    {
        var handler = new HandlerAteCancelar();
        using var provider = CriarProviderDoCliente(handler);
        var client = provider.GetRequiredService<ApiExternaClient>();
        using var cancellationTokenSource = new CancellationTokenSource();

        var chamada = client.ObterProdutoAsync(1, cancellationTokenSource.Token);
        await handler.RequisicaoRecebida;
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => chamada);
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task Cache_indisponivel_faz_fallback_para_fonte_de_verdade()
    {
        var cacheAside = new CacheAsideProdutos(
            new CacheComFalha(),
            new RepositorioProdutos(),
            Options.Create(new CacheOptions
            {
                TtlSeconds = 60,
                ExpirationJitterSeconds = 0
            }),
            NullLogger<CacheAsideProdutos>.Instance);

        var produto = await cacheAside.ObterAsync(1);

        Assert.NotNull(produto);
        Assert.Equal("Teclado", produto.Nome);
    }

    [Fact]
    public async Task Paginacao_valida_retorna_metadados_e_entrada_invalida_retorna_400()
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var success = await client.GetAsync("/produtos?pagina=2&tamanho=2");
        var page = await success.Content.ReadFromJsonAsync<Pagina<Produto>>();
        using var invalid = await client.GetAsync("/produtos?pagina=0&tamanho=100");

        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(2, page.Itens.Count);
        Assert.Equal(5, page.Total);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Endpoint_filter_retorna_validation_problem()
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync(
            "/produtos",
            new CriarProdutoRequest("", 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/produtos?pagina=abc&tamanho=2")]
    [InlineData("/produtos?pagina=2&tamanho=abc")]
    [InlineData("/produtos?pagina=2")]
    [InlineData("/produtos?tamanho=2")]
    public async Task Falha_de_binding_da_paginacao_preserva_400_com_problem_details(
        string endereco)
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var response = await client.GetAsync(endereco);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.Contains("paginação", document.RootElement.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"nome\":\"Produto\",\"preco\":\"invalido\"}")]
    [InlineData("null")]
    public async Task Corpo_json_invalido_retorna_400_sem_mascarar_falha_de_leitura(
        string json)
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/produtos", content);

        // Falhas de leitura do body são tratadas antes de executar endpoint filters.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Corpo_obrigatorio_ausente_retorna_400()
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/produtos");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tipo_de_conteudo_incompativel_retorna_415()
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var content = new StringContent("nome=Produto", Encoding.UTF8, "text/plain");

        using var response = await client.PostAsync("/produtos", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_filter_permite_produto_valido_e_retorna_201()
    {
        await using var app = CriarAplicacaoDeTeste();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync(
            "/produtos", new CriarProdutoRequest("Mouse", 50m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static WebApplication CriarAplicacaoDeTeste(
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["IntegracaoExterna:BaseUrl"] = "https://api.example.invalid/",
            ["IntegracaoExterna:TimeoutSeconds"] = "5",
            ["Cache:TtlSeconds"] = "60",
            ["Cache:ExpirationJitterSeconds"] = "0"
        };

        if (overrides is not null)
        {
            foreach (var item in overrides)
                settings[item.Key] = item.Value;
        }

        return Aplicacao.Criar(configurarBuilder: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(settings);
        });
    }

    private static ServiceProvider CriarProviderDoCliente(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<IntegracaoExternaOptions>>(
            Options.Create(new IntegracaoExternaOptions
            {
                BaseUrl = "https://api.example.invalid/",
                TimeoutSeconds = 5
            }));
        Aplicacao.RegistrarClienteExterno(
            services,
            () => handler,
            retryDelay: TimeSpan.Zero,
            useJitter: false);
        return services.BuildServiceProvider();
    }

    private sealed class SingletonCapturandoScoped(ContextoDaRequisicao scoped)
    {
        public ContextoDaRequisicao Scoped { get; } = scoped;
    }

    private sealed class HandlerSequencial(params HttpStatusCode[] statuses)
        : HttpMessageHandler
    {
        private int _chamadas;

        public int Chamadas => Volatile.Read(ref _chamadas);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var call = Interlocked.Increment(ref _chamadas);
            var status = statuses[Math.Min(call - 1, statuses.Length - 1)];
            var response = new HttpResponseMessage(status);

            if (status == HttpStatusCode.OK)
            {
                response.Content = new StringContent(
                    JsonSerializer.Serialize(new Produto(1, "Remoto", 10m)),
                    Encoding.UTF8,
                    "application/json");
            }

            return Task.FromResult(response);
        }
    }

    private sealed class CacheComFalha : IDistributedCache
    {
        public byte[]? Get(string key) => throw new InvalidOperationException("cache offline");
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromException<byte[]?>(new InvalidOperationException("cache offline"));
        public void Refresh(string key) => throw new InvalidOperationException("cache offline");
        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.FromException(new InvalidOperationException("cache offline"));
        public void Remove(string key) => throw new InvalidOperationException("cache offline");
        public Task RemoveAsync(string key, CancellationToken token = default) =>
            Task.FromException(new InvalidOperationException("cache offline"));
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            throw new InvalidOperationException("cache offline");
        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default) =>
            Task.FromException(new InvalidOperationException("cache offline"));
    }

    private sealed class HandlerAteCancelar : HttpMessageHandler
    {
        private readonly TaskCompletionSource _requisicaoRecebida = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _chamadas;

        public Task RequisicaoRecebida => _requisicaoRecebida.Task;
        public int Chamadas => Volatile.Read(ref _chamadas);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _chamadas);
            _requisicaoRecebida.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Trecho inalcançável.");
        }
    }
}
