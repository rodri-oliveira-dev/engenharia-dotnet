using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;

namespace EngenhariaDotnet.Seguranca;

public static class AplicacaoSegura
{
    public const string Issuer = "https://identity.example.invalid";
    public const string Audience = "engenharia-dotnet-api";
    public const string LerRelatorios = "relatorios:ler";
    public const string EditarDocumentos = "documentos:editar";

    public static WebApplication Criar(
        SecurityKey signingKey,
        Action<WebApplicationBuilder>? configurarBuilder = null,
        bool protegerTransporte = true)
    {
        var builder = WebApplication.CreateBuilder();
        configurarBuilder?.Invoke(builder);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(LerRelatorios, policy =>
                policy.RequireClaim("permission", LerRelatorios));

        builder.Services.AddSingleton<IAuthorizationHandler, DocumentoAuthorizationHandler>();
        builder.Services.AddExceptionHandler<TratadorSeguroDeExcecoes>();
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        });

        builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
            policy.WithOrigins("https://app.example.invalid")
                .WithMethods("GET", "PATCH")
                .WithHeaders("Authorization", "Content-Type")));

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("api", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.FindFirstValue("sub")
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        var databaseName = $"c26-{Guid.NewGuid():N}";
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";
        var anchor = new SqliteConnection(connectionString);
        anchor.Open();
        InicializarBanco(anchor);
        builder.Services.AddSingleton(anchor);
        builder.Services.AddSingleton(new RepositorioDocumentos(connectionString));

        var app = builder.Build();

        if (protegerTransporte)
        {
            if (!app.Environment.IsDevelopment())
                app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseExceptionHandler();
        app.UseCors("frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapGet("/relatorios", () => Results.Ok(new { total = 3 }))
            .RequireAuthorization(LerRelatorios)
            .RequireRateLimiting("api");

        app.MapGet(
            "/tenants/{tenantId}/documentos/{id}",
            ObterDocumentoAsync)
            .RequireRateLimiting("api");

        app.MapPatch(
            "/tenants/{tenantId}/documentos/{id}",
            AtualizarDocumentoAsync)
            .RequireRateLimiting("api");

        app.MapGet("/falha-controlada", FalharDeFormaControlada);

        return app;
    }

    private static IResult FalharDeFormaControlada() =>
        throw new InvalidOperationException("segredo-interno-nao-deve-vazar");

    private static async Task<IResult> ObterDocumentoAsync(
        string tenantId,
        string id,
        ClaimsPrincipal user,
        RepositorioDocumentos repository,
        IAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        if (!PertenceAoTenant(user, tenantId))
            return Results.NotFound();

        var document = await repository.ObterAsync(tenantId, id, cancellationToken);
        if (document is null)
            return Results.NotFound();

        var authorization = await authorizationService.AuthorizeAsync(
            user,
            document,
            DocumentoOperacoes.Ler);

        return authorization.Succeeded
            ? Results.Ok(new DocumentoResponse(document.Id, document.Titulo))
            : Results.NotFound();
    }

    private static async Task<IResult> AtualizarDocumentoAsync(
        string tenantId,
        string id,
        AtualizarDocumentoRequest request,
        ClaimsPrincipal user,
        RepositorioDocumentos repository,
        IAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        if (!user.HasClaim("permission", EditarDocumentos))
            return Results.Forbid();
        if (!PertenceAoTenant(user, tenantId))
            return Results.NotFound();
        if (string.IsNullOrWhiteSpace(request.Titulo) || request.Titulo.Length > 120)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Titulo)] = ["O título deve ter entre 1 e 120 caracteres."]
            });

        var document = await repository.ObterAsync(tenantId, id, cancellationToken);
        if (document is null)
            return Results.NotFound();

        var authorization = await authorizationService.AuthorizeAsync(
            user,
            document,
            DocumentoOperacoes.Editar);
        if (!authorization.Succeeded)
            return Results.NotFound();

        await repository.AtualizarTituloAsync(
            tenantId,
            id,
            request.Titulo.Trim(),
            cancellationToken);
        return Results.NoContent();
    }

    private static bool PertenceAoTenant(ClaimsPrincipal user, string tenantId) =>
        string.Equals(
            user.FindFirstValue("tenant_id"),
            tenantId,
            StringComparison.Ordinal);

    private static void InicializarBanco(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE documentos (
                id TEXT NOT NULL,
                tenant_id TEXT NOT NULL,
                proprietario_id TEXT NOT NULL,
                titulo TEXT NOT NULL,
                PRIMARY KEY (tenant_id, id)
            );
            INSERT INTO documentos (id, tenant_id, proprietario_id, titulo)
            VALUES
                ('doc-1', 'tenant-a', 'user-a', 'Plano defensivo'),
                ('doc-2', 'tenant-a', 'user-b', 'Registro privado'),
                ('doc-1', 'tenant-b', 'user-c', 'Mesmo ID, outro tenant');
            """;
        command.ExecuteNonQuery();
    }
}

public sealed record AtualizarDocumentoRequest(string Titulo);
public sealed record DocumentoResponse(string Id, string Titulo);
public sealed record Documento(string Id, string TenantId, string ProprietarioId, string Titulo);

public static class DocumentoOperacoes
{
    public static OperationAuthorizationRequirement Ler { get; } = new() { Name = "ler" };
    public static OperationAuthorizationRequirement Editar { get; } = new() { Name = "editar" };
}

public sealed class DocumentoAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, Documento>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Documento resource)
    {
        var sameTenant = string.Equals(
            context.User.FindFirstValue("tenant_id"),
            resource.TenantId,
            StringComparison.Ordinal);
        var isOwner = string.Equals(
            context.User.FindFirstValue("sub"),
            resource.ProprietarioId,
            StringComparison.Ordinal);

        if (sameTenant && isOwner &&
            (requirement.Name is "ler" or "editar"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class RepositorioDocumentos(string connectionString)
{
    public async Task<Documento?> ObterAsync(
        string tenantId,
        string id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, tenant_id, proprietario_id, titulo
            FROM documentos
            WHERE tenant_id = $tenantId AND id = $id
            """;
        command.Parameters.AddWithValue("$tenantId", tenantId);
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new Documento(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3))
            : null;
    }

    public async Task AtualizarTituloAsync(
        string tenantId,
        string id,
        string titulo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE documentos SET titulo = $titulo
            WHERE tenant_id = $tenantId AND id = $id
            """;
        command.Parameters.AddWithValue("$titulo", titulo);
        command.Parameters.AddWithValue("$tenantId", tenantId);
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class TratadorSeguroDeExcecoes(
    ILogger<TratadorSeguroDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Falha não tratada. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Não foi possível concluir a operação.",
            Detail = "Use o identificador de rastreamento ao contatar o suporte."
        };
        problem.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
