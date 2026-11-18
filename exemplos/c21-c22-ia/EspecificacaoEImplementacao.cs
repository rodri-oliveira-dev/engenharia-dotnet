namespace C21C22.Ia;

public sealed record EntradaRevisao(int ArquivosAlterados, bool AlteraAutorizacao, bool TestesPassaram);

public enum EncaminhamentoRevisao
{
    RevisaoPadrao,
    RevisaoSeguranca,
    CorrigirTestes
}

/// <summary>
/// Implementa os critérios AC-01 a AC-03 de especificacao-triagem.md.
/// A ordem explicita a precedência definida pela especificação.
/// </summary>
public static class PoliticaRevisao
{
    public static EncaminhamentoRevisao Encaminhar(EntradaRevisao entrada)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(entrada.ArquivosAlterados);

        if (!entrada.TestesPassaram)
        {
            return EncaminhamentoRevisao.CorrigirTestes;
        }

        return entrada.AlteraAutorizacao
            ? EncaminhamentoRevisao.RevisaoSeguranca
            : EncaminhamentoRevisao.RevisaoPadrao;
    }
}
