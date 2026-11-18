using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace EngenhariaDotnet.AspNetCore;

public static class Aplicacao
{
    public static WebApplication Criar(
        string[]? args = null,
        Action<WebApplicationBuilder>? configurarBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        configurarBuilder?.Invoke(builder);

        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        builder.Services
            .AddOptions<IntegracaoExternaOptions>()
            .Bind(builder.Configuration.GetSection(IntegracaoExternaOptions.Secao))
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps,
                "IntegracaoExterna:BaseUrl deve ser uma URL HTTPS absoluta.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<CacheOptions>()
            .Bind(builder.Configuration.GetSection(CacheOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddTransient<OperacaoTransient>();
        builder.Services.AddScoped<ContextoDaRequisicao>();
        builder.Services.AddSingleton<EstadoGlobalSeguro>();
        builder.Services.AddSingleton<ExecutorComEscopo>();

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.HttpContext.Items.TryGetValue(
                    CorrelationIdMiddleware.ItemKey,
                    out var correlationId))
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
            };
        });
        builder.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSingleton<IRepositorioProdutos, RepositorioProdutos>();
        builder.Services.AddSingleton<CacheAsideProdutos>();

        RegistrarClienteExterno(builder.Services);

        var app = builder.Build();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            SuppressDiagnosticsCallback = context =>
                context.Exception is RecursoNaoEncontradoException
        });

        app.MapEndpointsDoLaboratorio();
        return app;
    }

    public static IHttpClientBuilder RegistrarClienteExterno(
        IServiceCollection services,
        Func<HttpMessageHandler>? primaryHandlerFactory = null,
        TimeSpan? retryDelay = null,
        bool useJitter = true)
    {
        var clientBuilder = services.AddHttpClient<ApiExternaClient>((provider, client) =>
        {
            var options = provider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegracaoExternaOptions>>()
                .Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        if (primaryHandlerFactory is not null)
            clientBuilder.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);

        clientBuilder.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = retryDelay ?? TimeSpan.FromMilliseconds(200);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = useJitter;
            options.Retry.DisableForUnsafeHttpMethods();
        });

        return clientBuilder;
    }
}
