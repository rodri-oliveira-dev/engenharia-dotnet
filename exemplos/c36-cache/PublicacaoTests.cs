using Xunit;

namespace EngenhariaDotnet.C36;

public sealed class PublicacaoTests
{
    private static VersaoCatalogo Versao(long geracao, string nome) =>
        new(geracao, [new Produto(1, nome), new Produto(2, nome)]);

    [Fact]
    public void LeitorCapturadoNaoMisturaGeracoes()
    {
        var catalogo = new CatalogoVersionado(Versao(1, "A"));
        var leitor = catalogo.Capturar();

        Assert.True(catalogo.TentarPublicar(Versao(2, "B"), leitor, 2));

        Assert.Equal("A", leitor.Buscar(1)?.Nome);
        Assert.Equal("A", leitor.Buscar(2)?.Nome);
        Assert.Equal("B", catalogo.Capturar().Buscar(1)?.Nome);
    }

    [Fact]
    public void CargaIncompletaNaoSubstituiVersaoAtiva()
    {
        var catalogo = new CatalogoVersionado(Versao(1, "A"));
        var incompleta = new VersaoCatalogo(2, [new Produto(1, "B")]);

        Assert.False(catalogo.TentarPublicar(incompleta, catalogo.Capturar(), 2));
        Assert.Equal(1L, catalogo.Capturar().Geracao);
    }

    [Fact]
    public void GeracaoAntigaOuExpectativaObsoletaNaoPublica()
    {
        var catalogo = new CatalogoVersionado(Versao(2, "B"));

        Assert.False(catalogo.TentarPublicar(Versao(1, "A"), catalogo.Capturar(), 2));
        Assert.False(catalogo.TentarPublicar(Versao(3, "C"), Versao(1, "A"), 2));
        Assert.Equal(2L, catalogo.Capturar().Geracao);
    }

    [Fact]
    public async Task DuasPublicacoesDaMesmaBaseNaoPodemVencer()
    {
        var catalogo = new CatalogoVersionado(Versao(1, "A"));
        // Ambos os publicadores observam exatamente a mesma referência antes da troca.
        var baseComum = catalogo.Capturar();
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> Publicar(VersaoCatalogo candidata)
        {
            await inicio.Task;
            return catalogo.TentarPublicar(candidata, baseComum, 2);
        }

        var primeira = Publicar(Versao(2, "B"));
        var segunda = Publicar(Versao(3, "C"));
        inicio.SetResult();
        var resultados = await Task.WhenAll(primeira, segunda);

        Assert.Single(resultados, resultado => resultado);
        Assert.Equal(resultados[0] ? 2L : 3L, catalogo.Capturar().Geracao);
    }

    [Fact]
    public void ReferenciaDeOutraPublicacaoNaoEquivaleAoPonteiroAtivo()
    {
        var catalogo = new CatalogoVersionado(Versao(1, "A"));
        var mesmaGeracaoOutraReferencia = Versao(1, "A");

        Assert.False(catalogo.TentarPublicar(Versao(2, "B"), mesmaGeracaoOutraReferencia, 2));
        Assert.Equal(1L, catalogo.Capturar().Geracao);
    }

    [Fact]
    public void ChavesDuplicadasSaoRejeitadasAntesDaPublicacao()
    {
        Assert.Throws<ArgumentException>(() =>
            new VersaoCatalogo(2, [new Produto(1, "A"), new Produto(1, "B")]));
    }
}
