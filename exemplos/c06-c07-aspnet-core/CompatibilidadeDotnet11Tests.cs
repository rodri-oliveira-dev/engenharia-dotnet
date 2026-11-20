using Microsoft.Extensions.Configuration;
using Xunit;

namespace EngenhariaDotnet.AspNetCore.Tests;

public sealed class CompatibilidadeDotnet11Tests
{
    [Fact]
    public void AddConfiguration_no_dotnet11_preserva_string_vazia_como_override()
    {
        // No .NET 10 o valor vazio do provider encadeado nao prevalecia.
        IConfiguration configuracaoInterna = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Endpoint"] = ""
            })
            .Build();

        IConfiguration configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Endpoint"] = "https://exemplo.invalid"
            })
            .AddConfiguration(configuracaoInterna)
            .Build();

        Assert.Equal("", configuracao["Endpoint"]);
        Assert.True(configuracao.GetSection("Endpoint").Exists());
    }

    [Fact]
    public void AddConfiguration_nao_substitui_valor_anterior_quando_chave_e_nula()
    {
        IConfiguration configuracaoInterna = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Endpoint"] = null
            })
            .Build();

        IConfiguration configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Endpoint"] = "https://exemplo.invalid"
            })
            .AddConfiguration(configuracaoInterna)
            .Build();

        Assert.Equal("https://exemplo.invalid", configuracao["Endpoint"]);
    }
}
