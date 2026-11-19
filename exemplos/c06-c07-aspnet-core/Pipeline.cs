using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EngenhariaDotnet.AspNetCore;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].ToString();
        var correlationId = IsValid(supplied)
            ? supplied
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            [ItemKey] = correlationId
        }))
        {
            await next(context);
        }
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= 64
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}

public sealed class RecursoNaoEncontradoException(string recurso)
    : Exception($"{recurso} não foi encontrado.");

public sealed class TratadorGlobalDeExcecoes(
    IProblemDetailsService problemDetailsService,
    ILogger<TratadorGlobalDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var conhecido = exception is RecursoNaoEncontradoException;
        var status = exception switch
        {
            RecursoNaoEncontradoException => StatusCodes.Status404NotFound,
            // Falha de binding/leitura do corpo é erro do cliente, não erro do servidor.
            BadHttpRequestException invalidRequest => invalidRequest.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        if (conhecido)
            logger.LogInformation("Recurso solicitado não existe: {Message}", exception.Message);
        else if (exception is BadHttpRequestException)
            logger.LogInformation("Requisição HTTP inválida: status {StatusCode}", status);
        else
            logger.LogError(exception, "Falha não tratada no pipeline HTTP");

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    StatusCodes.Status404NotFound => "Recurso não encontrado",
                    StatusCodes.Status400BadRequest => "Requisição inválida",
                    StatusCodes.Status415UnsupportedMediaType => "Tipo de mídia não suportado",
                    _ => "Erro interno"
                },
                Detail = conhecido ? exception.Message : null,
                Type = $"https://httpstatuses.com/{status}"
            },
            Exception = exception
        });
    }
}

public sealed record CriarProdutoRequest(string Nome, decimal Preco);

// No .NET 11, erros de binding de query podem chegar aos filtros antes do handler.
// Nessa situação, argumentos ainda não são confiáveis: observar primeiro o status.
public sealed class ValidarBindingPaginacaoFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var status = context.HttpContext.Response.StatusCode;
        if (status >= StatusCodes.Status400BadRequest)
        {
            return Results.Problem(
                title: "Não foi possível interpretar os parâmetros de paginação.",
                statusCode: status);
        }

        return await next(context);
    }
}

public sealed class ValidarProdutoFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Binding pode falhar antes que exista um DTO válido no contexto.
        var status = context.HttpContext.Response.StatusCode;
        if (status >= StatusCodes.Status400BadRequest)
        {
            return Results.Problem(
                title: "Não foi possível vincular o corpo da requisição.",
                statusCode: status);
        }

        var request = context.Arguments.OfType<CriarProdutoRequest>().SingleOrDefault();
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["produto"] = ["O corpo da requisição é obrigatório e deve ser válido."]
            });
        }

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nome))
            errors[nameof(request.Nome)] = ["Nome é obrigatório."];
        if (request.Preco <= 0)
            errors[nameof(request.Preco)] = ["Preço deve ser maior que zero."];

        return errors.Count > 0
            ? Results.ValidationProblem(errors)
            : await next(context);
    }
}

public static class Endpoints
{
    public static IEndpointRouteBuilder MapEndpointsDoLaboratorio(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/diagnostico/lifetimes", (
            IServiceProvider provider,
            ContextoDaRequisicao scoped,
            EstadoGlobalSeguro singleton) =>
        {
            var transient1 = provider.GetRequiredService<OperacaoTransient>();
            var transient2 = provider.GetRequiredService<OperacaoTransient>();

            return Results.Ok(new DiagnosticoLifetimes(
                transient1.Id,
                transient2.Id,
                scoped.Id,
                singleton.RegistrarRequisicao()));
        });

        endpoints.MapGet("/produtos", (
            int pagina,
            int tamanho,
            IRepositorioProdutos repositorio) =>
        {
            if (pagina < 1 || tamanho is < 1 or > 50)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["paginacao"] = ["Página deve ser >= 1 e tamanho deve estar entre 1 e 50."]
                });
            }

            var todos = repositorio.Listar();
            var itens = todos.Skip((pagina - 1) * tamanho).Take(tamanho).ToArray();
            return Results.Ok(new Pagina<Produto>(itens, pagina, tamanho, todos.Count));
        }).AddEndpointFilter<ValidarBindingPaginacaoFilter>();

        endpoints.MapGet("/produtos/{id:int}", async (
            int id,
            CacheAsideProdutos cache,
            CancellationToken cancellationToken) =>
        {
            var produto = await cache.ObterAsync(id, cancellationToken);
            return produto ?? throw new RecursoNaoEncontradoException($"Produto {id}");
        });

        endpoints.MapPost("/produtos", (CriarProdutoRequest request) =>
                Results.Created($"/produtos/6", new Produto(6, request.Nome, request.Preco)))
            .AddEndpointFilter<ValidarProdutoFilter>();

        return endpoints;
    }
}

public sealed record DiagnosticoLifetimes(
    Guid Transient1,
    Guid Transient2,
    Guid Scoped,
    long ContagemSingleton);

public sealed record Pagina<T>(
    IReadOnlyList<T> Itens,
    int PaginaAtual,
    int Tamanho,
    int Total);
