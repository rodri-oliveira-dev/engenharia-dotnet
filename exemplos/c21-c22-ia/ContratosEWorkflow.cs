using System.Text.Json;

namespace C21C22.Ia;

public sealed record SolicitacaoModelo(
    string Instrucao,
    string Entrada,
    double Temperatura,
    int LimiteTokensSaida);

public sealed record RespostaModelo(string Conteudo, int TokensEntrada, int TokensSaida);

public interface IGeradorTexto
{
    Task<RespostaModelo> GerarAsync(
        SolicitacaoModelo solicitacao,
        CancellationToken cancellationToken = default);
}

public enum CategoriaChamado
{
    Acesso,
    Cobranca,
    Incidente
}

public sealed record Chamado(string Id, string Descricao);

public sealed record Triagem(CategoriaChamado Categoria, double Confianca, bool RevisaoHumana);

/// <summary>
/// Workflow determinístico: o modelo participa de uma etapa, mas não decide o fluxo.
/// </summary>
public sealed class WorkflowTriagem(IGeradorTexto modelo)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Triagem> ExecutarAsync(
        Chamado chamado,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chamado.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(chamado.Descricao);

        var solicitacao = new SolicitacaoModelo(
            "Classifique em Acesso, Cobranca ou Incidente. " +
            "Responda somente JSON: {\"categoria\":string,\"confianca\":number}.",
            chamado.Descricao,
            Temperatura: 0,
            LimiteTokensSaida: 80);

        var resposta = await modelo.GerarAsync(solicitacao, cancellationToken);
        var saida = JsonSerializer.Deserialize<SaidaTriagem>(resposta.Conteudo, JsonOptions)
            ?? throw new InvalidOperationException("O modelo retornou JSON vazio.");

        if (!Enum.TryParse<CategoriaChamado>(saida.Categoria, ignoreCase: true, out var categoria))
        {
            throw new InvalidOperationException("O modelo retornou uma categoria fora do contrato.");
        }

        if (saida.Confianca is < 0 or > 1)
        {
            throw new InvalidOperationException("A confiança deve estar entre zero e um.");
        }

        return new Triagem(categoria, saida.Confianca, RevisaoHumana: saida.Confianca < 0.80);
    }

    private sealed record SaidaTriagem(string Categoria, double Confianca);
}

/// <summary>Fake configurável: substitui um provedor externo nos testes.</summary>
public sealed class GeradorTextoFalso(string resposta) : IGeradorTexto
{
    public SolicitacaoModelo? UltimaSolicitacao { get; private set; }

    public Task<RespostaModelo> GerarAsync(
        SolicitacaoModelo solicitacao,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UltimaSolicitacao = solicitacao;
        return Task.FromResult(new RespostaModelo(resposta, 24, 12));
    }
}
