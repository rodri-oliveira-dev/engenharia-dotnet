using C02.Nulabilidade;
using Xunit;
using System.Text.Json;

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
    [Fact]
    public void Uniao_representa_cadastro_valido_sem_excecao_de_fluxo()
    {
        var resultado = RegistroComResultado.Registrar(
            " Ada ", " ADA@example.test ", []);

        var sucesso = Assert.IsType<CadastroRegistrado>(resultado.Value);
        Assert.Equal("Ada", sucesso.Cadastro.Nome);
        Assert.Equal("ADA@example.test", sucesso.Cadastro.Email);
        Assert.Equal("Cadastro criado: ADA@example.test",
            RegistroComResultado.Descrever(resultado));
    }

    [Fact]
    public void Uniao_representa_email_ja_utilizado_sem_diferenca_de_caixa()
    {
        var resultado = RegistroComResultado.Registrar(
            "Ada", "ADA@example.test", ["ada@EXAMPLE.test"]);

        var falha = Assert.IsType<EmailEmUso>(resultado.Value);
        Assert.Equal("ADA@example.test", falha.Email);
        Assert.Equal("E-mail ja utilizado: ADA@example.test",
            RegistroComResultado.Descrever(resultado));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Uniao_preserva_o_caso_ao_serializar_e_desserializar(bool duplicado)
    {
        var resultado = RegistroComResultado.Registrar(
            "Ada", "ada@example.test", duplicado ? ["ada@example.test"] : []);

        var json = JsonSerializer.Serialize(resultado);
        var reconstruido = JsonSerializer.Deserialize<ResultadoRegistro>(json);

        if (duplicado)
            Assert.Equal(new EmailEmUso("ada@example.test"),
                Assert.IsType<EmailEmUso>(reconstruido.Value));
        else
            Assert.Equal(new CadastroRegistrado(new Cadastro("Ada", "ada@example.test")),
                Assert.IsType<CadastroRegistrado>(reconstruido.Value));
    }

    [Fact]
    public void Uniao_nao_mascara_entrada_invalida_como_erro_de_negocio()
    {
        Assert.Throws<ArgumentException>(() =>
            RegistroComResultado.Registrar(" ", "ada@example.test", []));
    }

}
