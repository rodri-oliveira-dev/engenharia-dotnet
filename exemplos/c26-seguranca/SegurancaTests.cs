using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EngenhariaDotnet.Seguranca.Tests;

public sealed class SegurancaTests : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RsaSecurityKey _key;

    public SegurancaTests()
    {
        _key = new RsaSecurityKey(_rsa) { KeyId = "chave-efemera-de-teste" };
    }

    [Fact]
    public async Task Endpoint_protegido_rejeita_requisicao_sem_token()
    {
        await using var app = await CriarAplicacaoAsync();
        using var client = app.GetTestClient();

        using var response = await client.GetAsync(
            "/tenants/tenant-a/documentos/doc-1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Policy_exige_permission_especifica()
    {
        await using var app = await CriarAplicacaoAsync();
        using var client = CriarCliente(app, CriarToken("user-a", "tenant-a"));

        using var forbidden = await client.GetAsync("/relatorios");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CriarToken("user-a", "tenant-a", AplicacaoSegura.LerRelatorios));
        using var allowed = await client.GetAsync("/relatorios");

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Proprietario_acessa_objeto_sem_expor_campos_internos()
    {
        await using var app = await CriarAplicacaoAsync();
        using var client = CriarCliente(app, CriarToken("user-a", "tenant-a"));

        using var response = await client.GetAsync(
            "/tenants/tenant-a/documentos/doc-1");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Plano defensivo", json, StringComparison.Ordinal);
        Assert.DoesNotContain("proprietario", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("user-b", "tenant-a", "doc-1")]
    [InlineData("user-a", "tenant-b", "doc-1")]
    public async Task BOLA_e_cross_tenant_retornam_not_found(
        string subject,
        string tenant,
        string documentId)
    {
        await using var app = await CriarAplicacaoAsync();
        using var client = CriarCliente(app, CriarToken(subject, tenant));

        using var response = await client.GetAsync(
            $"/tenants/tenant-a/documentos/{documentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DTO_de_entrada_nao_permite_mass_assignment()
    {
        await using var app = await CriarAplicacaoAsync();
        using var client = CriarCliente(
            app,
            CriarToken(
                "user-a",
                "tenant-a",
                AplicacaoSegura.EditarDocumentos));

        using var update = await client.PatchAsJsonAsync(
            "/tenants/tenant-a/documentos/doc-1",
            new
            {
                titulo = "Título permitido",
                proprietarioId = "attacker",
                tenantId = "tenant-b"
            });
        using var read = await client.GetAsync(
            "/tenants/tenant-a/documentos/doc-1");
        var document = await read.Content.ReadFromJsonAsync<DocumentoResponse>();

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal("Título permitido", document?.Titulo);
    }

    [Fact]
    public async Task Consulta_parametrizada_trata_payload_como_dado()
    {
        await using var app = await CriarAplicacaoAsync();
        var repository = app.Services.GetRequiredService<RepositorioDocumentos>();

        var injected = await repository.ObterAsync(
            "tenant-a",
            "doc-1' OR 1=1 --");
        var valid = await repository.ObterAsync("tenant-a", "doc-1");

        Assert.Null(injected);
        Assert.NotNull(valid);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("signature")]
    public async Task Validacao_de_JWT_rejeita_token_invalido(string defect)
    {
        await using var app = await CriarAplicacaoAsync();
        var token = defect switch
        {
            "issuer" => CriarToken("user-a", "tenant-a", issuer: "https://evil.invalid"),
            "audience" => CriarToken("user-a", "tenant-a", audience: "outra-api"),
            "expired" => CriarToken(
                "user-a",
                "tenant-a",
                expires: DateTime.UtcNow.AddMinutes(-2)),
            "signature" => CriarTokenComOutraAssinatura(),
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        using var client = CriarCliente(app, token);

        using var response = await client.GetAsync(
            "/tenants/tenant-a/documentos/doc-1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Erro_nao_expoe_excecao_ou_segredo()
    {
        await using var app = await CriarAplicacaoAsync();
        using var client = CriarCliente(app, CriarToken("user-a", "tenant-a"));

        using var response = await client.GetAsync("/falha-controlada");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(json.RootElement.ToString().Contains(
            "segredo-interno",
            StringComparison.OrdinalIgnoreCase));
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
    }

    public void Dispose() => _rsa.Dispose();

    private async Task<WebApplication> CriarAplicacaoAsync()
    {
        var app = AplicacaoSegura.Criar(
            _key,
            builder => builder.WebHost.UseTestServer(),
            protegerTransporte: false);
        await app.StartAsync();
        return app;
    }

    private string CriarToken(
        string subject,
        string tenant,
        string? permission = null,
        string? issuer = null,
        string? audience = null,
        DateTime? expires = null)
    {
        var claims = new List<Claim>
        {
            new("sub", subject),
            new("tenant_id", tenant),
            new("jti", Guid.NewGuid().ToString("N"))
        };
        if (permission is not null)
            claims.Add(new Claim("permission", permission));

        var expiration = expires ?? DateTime.UtcNow.AddMinutes(5);
        var notBefore = expiration < DateTime.UtcNow
            ? expiration.AddMinutes(-1)
            : DateTime.UtcNow.AddMinutes(-1);
        var token = new JwtSecurityToken(
            issuer ?? AplicacaoSegura.Issuer,
            audience ?? AplicacaoSegura.Audience,
            claims,
            notBefore: notBefore,
            expires: expiration,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CriarTokenComOutraAssinatura()
    {
        using var rsa = RSA.Create(2048);
        var otherKey = new RsaSecurityKey(rsa);
        var token = new JwtSecurityToken(
            AplicacaoSegura.Issuer,
            AplicacaoSegura.Audience,
            [new Claim("sub", "user-a"), new Claim("tenant_id", "tenant-a")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(otherKey, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpClient CriarCliente(WebApplication app, string token)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
