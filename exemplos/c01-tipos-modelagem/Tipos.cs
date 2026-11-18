namespace C01.TiposModelagem;

public sealed class Cliente
{
    public Cliente(Guid id, string nome) => (Id, Nome) = (id, nome);

    public Guid Id { get; }
    public string Nome { get; }
}

public sealed record Dinheiro(decimal Quantia, string Moeda);

public readonly record struct Coordenada(decimal Latitude, decimal Longitude);

public struct Contador
{
    public int Valor { get; private set; }

    public void Incrementar() => Valor++;
}

public interface ITemComCodigo
{
    int Codigo { get; }
}

public readonly record struct ItemComCodigo(int Codigo) : ITemComCodigo;

public static class Operacoes
{
    public static void IncrementarCopia(Contador contador) => contador.Incrementar();

    public static void IncrementarOriginal(ref Contador contador) => contador.Incrementar();

    public static bool MesmoValor<T>(T esquerda, T direita) =>
        EqualityComparer<T>.Default.Equals(esquerda, direita);
}
