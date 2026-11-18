using System.Globalization;
using System.Text;

namespace C21C22.Ia;

public sealed record DocumentoFonte(
    string Id,
    string Versao,
    string Texto,
    IReadOnlySet<string> GruposPermitidos);

public sealed record TrechoIndexado(
    string DocumentoId,
    string Versao,
    int Posicao,
    string Texto,
    IReadOnlySet<string> GruposPermitidos,
    float[] Vetor);

public sealed record ResultadoRecuperacao(
    string DocumentoId,
    string Versao,
    int Posicao,
    string Texto,
    double Pontuacao)
{
    public string Citacao => $"[{DocumentoId}@{Versao}#{Posicao}]";
}

public interface IGeradorEmbedding
{
    float[] Gerar(string texto);
}

/// <summary>
/// Embedding local por hashing. É determinístico e didático, não semântico nem adequado à produção.
/// </summary>
public sealed class EmbeddingHashLocal(int dimensoes = 128) : IGeradorEmbedding
{
    public float[] Gerar(string texto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        var vetor = new float[dimensoes];

        foreach (var termo in Tokenizar(texto))
        {
            var indice = (int)(HashEstavel(termo) % (uint)dimensoes);
            vetor[indice] += 1;
        }

        var norma = Math.Sqrt(vetor.Sum(valor => valor * valor));
        if (norma > 0)
        {
            for (var i = 0; i < vetor.Length; i++)
            {
                vetor[i] = (float)(vetor[i] / norma);
            }
        }

        return vetor;
    }

    private static IEnumerable<string> Tokenizar(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var atual = new StringBuilder();

        foreach (var caractere in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caractere))
            {
                atual.Append(char.ToLowerInvariant(caractere));
            }
            else if (atual.Length > 0)
            {
                yield return atual.ToString();
                atual.Clear();
            }
        }

        if (atual.Length > 0)
        {
            yield return atual.ToString();
        }
    }

    private static uint HashEstavel(string valor)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var caractere in valor)
        {
            hash ^= caractere;
            hash *= prime;
        }

        return hash;
    }
}

public sealed class PipelineRagLocal(IGeradorEmbedding embeddings, int palavrasPorTrecho = 40, int sobreposicao = 8)
{
    private readonly List<TrechoIndexado> _indice = [];

    public void Indexar(IEnumerable<DocumentoFonte> documentos)
    {
        foreach (var documento in documentos)
        {
            _indice.RemoveAll(item => item.DocumentoId == documento.Id);
            var palavras = documento.Texto.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var passo = Math.Max(1, palavrasPorTrecho - sobreposicao);
            for (var inicio = 0; inicio < palavras.Length; inicio += passo)
            {
                var texto = string.Join(' ', palavras.Skip(inicio).Take(palavrasPorTrecho));
                _indice.Add(new TrechoIndexado(
                    documento.Id,
                    documento.Versao,
                    inicio,
                    texto,
                    documento.GruposPermitidos,
                    embeddings.Gerar(texto)));
            }
        }
    }

    public IReadOnlyList<ResultadoRecuperacao> Recuperar(
        string consulta,
        IReadOnlySet<string> gruposDoUsuario,
        int limite = 3)
    {
        var vetorConsulta = embeddings.Gerar(consulta);

        return _indice
            .Where(trecho => trecho.GruposPermitidos.Overlaps(gruposDoUsuario))
            .Select(trecho => new ResultadoRecuperacao(
                trecho.DocumentoId,
                trecho.Versao,
                trecho.Posicao,
                trecho.Texto,
                ProdutoEscalar(vetorConsulta, trecho.Vetor)))
            .Where(resultado => resultado.Pontuacao > 0)
            .OrderByDescending(resultado => resultado.Pontuacao)
            .ThenBy(resultado => resultado.DocumentoId, StringComparer.Ordinal)
            .Take(limite)
            .ToArray();
    }

    public static string MontarContexto(IEnumerable<ResultadoRecuperacao> resultados) =>
        string.Join(Environment.NewLine, resultados.Select(item => $"{item.Citacao} {item.Texto}"));

    private static double ProdutoEscalar(float[] esquerda, float[] direita)
    {
        if (esquerda.Length != direita.Length)
        {
            throw new ArgumentException("Vetores devem ter a mesma dimensão.");
        }

        double total = 0;
        for (var i = 0; i < esquerda.Length; i++)
        {
            total += esquerda[i] * direita[i];
        }

        return total;
    }
}
