using C02.Nulabilidade;
using Xunit;

namespace C02.Nulabilidade.Tests;

public sealed class CadastroTests
{
    [Fact]
    public void Cria_cadastro_depois_de_validar_a_fronteira()
    {
        var cadastro = CadastroFactory.Criar("  Ada  ", " ada@example.test ");

        Assert.Equal("Ada", cadastro.Nome);
        Assert.Equal("ada@example.test", cadastro.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejeita_nome_ausente(string? nome)
    {
        Assert.Throws<ArgumentException>(
            () => CadastroFactory.Criar(nome, "ada@example.test"));
    }

    [Fact]
    public void Contrato_not_null_when_refina_o_estado_do_compilador()
    {
        string? entrada = "  conteúdo  ";

        if (!CadastroFactory.TentarNormalizar(entrada, out var normalizado))
        {
            throw new Xunit.Sdk.XunitException("A entrada deveria ser válida.");
        }

        Assert.Equal(8, normalizado.Length);
    }

    [Fact]
    public void Coalescencia_fornece_fallback_sem_suprimir_analise()
    {
        Assert.Equal("Ada", CadastroFactory.NomeDeExibicao(null, "Ada"));
        Assert.Equal("Condessa", CadastroFactory.NomeDeExibicao(" Condessa ", "Ada"));
    }
}
