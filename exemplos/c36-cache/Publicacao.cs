using System.Collections.Frozen;

namespace EngenhariaDotnet.C36;

public sealed record Produto(int Id, string Nome);

public sealed class VersaoCatalogo
{
    private readonly FrozenDictionary<int, Produto> produtos;

    public long Geracao { get; }
    public int Quantidade => produtos.Count;

    public VersaoCatalogo(long geracao, IEnumerable<Produto> itens)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(geracao);
        Geracao = geracao;
        produtos = itens.ToFrozenDictionary(item => item.Id);
    }

    public Produto? Buscar(int id) => produtos.GetValueOrDefault(id);
}

// Modelo local da publicação: a referência capturada pelo leitor é imutável.
public sealed class CatalogoVersionado(VersaoCatalogo inicial)
{
    private VersaoCatalogo ativa = inicial;

    public VersaoCatalogo Capturar() => Volatile.Read(ref ativa);

    public bool TentarPublicar(VersaoCatalogo candidata, long geracaoEsperada, int quantidadeEsperada)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidadeEsperada);
        if (candidata.Quantidade != quantidadeEsperada)
            return false;

        var anterior = Capturar();
        if (anterior.Geracao != geracaoEsperada || candidata.Geracao <= anterior.Geracao)
            return false;

        // Um concorrente que mudar o ponteiro invalida esta tentativa.
        return ReferenceEquals(Interlocked.CompareExchange(ref ativa, candidata, anterior), anterior);
    }
}
