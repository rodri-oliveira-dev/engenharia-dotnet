namespace C21C22.Ia;

public enum RiscoFerramenta
{
    Leitura,
    AlteracaoReversivel,
    AcaoSensivel
}

public sealed record ChamadaFerramenta(string Nome, string Argumento);

public sealed record DecisaoAgente(ChamadaFerramenta? Chamada, string? RespostaFinal)
{
    public static DecisaoAgente Usar(string nome, string argumento) =>
        new(new ChamadaFerramenta(nome, argumento), null);

    public static DecisaoAgente Finalizar(string resposta) => new(null, resposta);
}

public sealed record FerramentaLimitada(
    string Nome,
    RiscoFerramenta Risco,
    Func<string, CancellationToken, Task<string>> ExecutarAsync);

public interface IPlanejadorAgente
{
    Task<DecisaoAgente> ProximoPassoAsync(
        string objetivo,
        IReadOnlyList<string> historico,
        CancellationToken cancellationToken = default);
}

public interface IConfirmacaoHumana
{
    Task<bool> ConfirmarAsync(
        ChamadaFerramenta chamada,
        CancellationToken cancellationToken = default);
}

public sealed class AgenteLimitado(
    IPlanejadorAgente planejador,
    IEnumerable<FerramentaLimitada> ferramentas,
    IConfirmacaoHumana confirmacao,
    int maximoPassos = 4)
{
    private readonly IReadOnlyDictionary<string, FerramentaLimitada> _ferramentas = ferramentas
        .ToDictionary(item => item.Nome, StringComparer.Ordinal);

    public async Task<string> ExecutarAsync(
        string objetivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objetivo);
        var historico = new List<string>();

        for (var passo = 1; passo <= maximoPassos; passo++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var decisao = await planejador.ProximoPassoAsync(objetivo, historico, cancellationToken);

            if (!string.IsNullOrWhiteSpace(decisao.RespostaFinal))
            {
                return decisao.RespostaFinal;
            }

            var chamada = decisao.Chamada
                ?? throw new InvalidOperationException("O planejador não finalizou nem solicitou ferramenta.");

            if (!_ferramentas.TryGetValue(chamada.Nome, out var ferramenta))
            {
                throw new InvalidOperationException($"Ferramenta não permitida: {chamada.Nome}.");
            }

            if (chamada.Argumento.Length > 500)
            {
                throw new InvalidOperationException("Argumento excede o limite da ferramenta.");
            }

            if (ferramenta.Risco != RiscoFerramenta.Leitura &&
                !await confirmacao.ConfirmarAsync(chamada, cancellationToken))
            {
                historico.Add($"passo={passo}; ferramenta={chamada.Nome}; resultado=negado");
                continue;
            }

            var resultado = await ferramenta.ExecutarAsync(chamada.Argumento, cancellationToken);
            historico.Add($"passo={passo}; ferramenta={chamada.Nome}; resultado={resultado}");
        }

        throw new InvalidOperationException("Agente interrompido pelo limite de passos.");
    }
}

public sealed class PlanejadorRoteirizado(params DecisaoAgente[] decisoes) : IPlanejadorAgente
{
    private int _indice;

    public Task<DecisaoAgente> ProximoPassoAsync(
        string objetivo,
        IReadOnlyList<string> historico,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_indice >= decisoes.Length)
        {
            throw new InvalidOperationException("Roteiro do teste terminou sem resposta final.");
        }

        return Task.FromResult(decisoes[_indice++]);
    }
}

public sealed class ConfirmacaoFixa(bool permitir) : IConfirmacaoHumana
{
    public Task<bool> ConfirmarAsync(
        ChamadaFerramenta chamada,
        CancellationToken cancellationToken = default) => Task.FromResult(permitir);
}
