using C21C22.Ia;
using Xunit;

namespace C21C22.Ia.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public async Task WorkflowValidaSaidaEEncaminhaBaixaConfianca()
    {
        var modelo = new GeradorTextoFalso("""{"categoria":"Incidente","confianca":0.72}""");
        var workflow = new WorkflowTriagem(modelo);

        var resultado = await workflow.ExecutarAsync(new Chamado("CH-1", "API indisponível"));

        Assert.Equal(CategoriaChamado.Incidente, resultado.Categoria);
        Assert.True(resultado.RevisaoHumana);
        Assert.Equal(0, modelo.UltimaSolicitacao?.Temperatura);
    }

    [Fact]
    public async Task WorkflowRejeitaCategoriaForaDoContrato()
    {
        var workflow = new WorkflowTriagem(
            new GeradorTextoFalso("""{"categoria":"Outro","confianca":0.9}"""));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => workflow.ExecutarAsync(new Chamado("CH-2", "pedido")));
    }
}

public sealed class RagTests
{
    [Fact]
    public void RecuperacaoAplicaAclAntesDoRankingEMontaCitacao()
    {
        var rag = new PipelineRagLocal(new EmbeddingHashLocal(), palavrasPorTrecho: 8, sobreposicao: 2);
        rag.Indexar([
            new DocumentoFonte("publico", "v1", "retry usa espera limitada e cancelamento", new HashSet<string> { "todos" }),
            new DocumentoFonte("restrito", "v3", "segredo financeiro retry cancelamento", new HashSet<string> { "financas" })
        ]);

        var resultados = rag.Recuperar("retry cancelamento", new HashSet<string> { "todos" });
        var contexto = PipelineRagLocal.MontarContexto(resultados);

        var item = Assert.Single(resultados);
        Assert.Equal("publico", item.DocumentoId);
        Assert.Contains("[publico@v1#0]", contexto, StringComparison.Ordinal);
    }
}

public sealed class AgenteTests
{
    [Fact]
    public async Task AgenteNaoExecutaFerramentaForaDaAllowlist()
    {
        var agente = new AgenteLimitado(
            new PlanejadorRoteirizado(DecisaoAgente.Usar("shell", "remove tudo")),
            [],
            new ConfirmacaoFixa(false));

        await Assert.ThrowsAsync<InvalidOperationException>(() => agente.ExecutarAsync("diagnosticar"));
    }

    [Fact]
    public async Task AgenteExigeConfirmacaoAntesDeAlteracao()
    {
        var executou = false;
        var ferramenta = new FerramentaLimitada(
            "fechar_chamado",
            RiscoFerramenta.AlteracaoReversivel,
            (argumento, _) =>
            {
                executou = true;
                return Task.FromResult("fechado");
            });
        var agente = new AgenteLimitado(
            new PlanejadorRoteirizado(
                DecisaoAgente.Usar("fechar_chamado", "CH-1"),
                DecisaoAgente.Finalizar("Encaminhado para revisão humana.")),
            [ferramenta],
            new ConfirmacaoFixa(false));

        var resposta = await agente.ExecutarAsync("resolver CH-1");

        Assert.False(executou);
        Assert.Contains("revisão humana", resposta, StringComparison.Ordinal);
    }
}

public sealed class EspecificacaoTests
{
    [Fact] // AC-01
    public void Ac01_TesteFalhouTemPrecedencia() => Assert.Equal(
        EncaminhamentoRevisao.CorrigirTestes,
        PoliticaRevisao.Encaminhar(new EntradaRevisao(2, AlteraAutorizacao: true, TestesPassaram: false)));

    [Fact] // AC-02
    public void Ac02_AutorizacaoExigeRevisaoEspecializada() => Assert.Equal(
        EncaminhamentoRevisao.RevisaoSeguranca,
        PoliticaRevisao.Encaminhar(new EntradaRevisao(1, AlteraAutorizacao: true, TestesPassaram: true)));

    [Fact] // AC-03
    public void Ac03_MudancaComumSegueRevisaoPadrao() => Assert.Equal(
        EncaminhamentoRevisao.RevisaoPadrao,
        PoliticaRevisao.Encaminhar(new EntradaRevisao(4, AlteraAutorizacao: false, TestesPassaram: true)));

    [Fact] // AC-04
    public void Ac04_QuantidadeNegativaEhInvalida() => Assert.Throws<ArgumentOutOfRangeException>(
        () => PoliticaRevisao.Encaminhar(new EntradaRevisao(-1, false, true)));

    [Fact]
    public void ServidorMcpExpoeSomenteTopicosPermitidos()
    {
        Assert.Contains("fluxo explícito", FerramentasArquitetura.ConsultarDecisao("workflow"), StringComparison.Ordinal);
        Assert.Contains("não encontrado", FerramentasArquitetura.ConsultarDecisao("segredo"), StringComparison.Ordinal);
    }
}
