using System.Buffers;
using System.Globalization;

namespace EngenhariaDotnet.MemoriaPerformance;

public readonly record struct Medicao(int SensorId, decimal Valor, bool Valida);

public static class Processamento
{
    public static Medicao InterpretarComStrings(string linha)
    {
        var partes = linha.Split('|');
        if (partes.Length != 3)
            throw new FormatException("A linha deve ter três campos.");

        return new Medicao(
            int.Parse(partes[0], CultureInfo.InvariantCulture),
            decimal.Parse(partes[1], CultureInfo.InvariantCulture),
            bool.Parse(partes[2]));
    }

    public static Medicao InterpretarComSpan(ReadOnlySpan<char> linha)
    {
        var primeiroSeparador = linha.IndexOf('|');
        var ultimoSeparador = linha.LastIndexOf('|');

        if (primeiroSeparador <= 0 || ultimoSeparador <= primeiroSeparador)
            throw new FormatException("A linha deve ter três campos.");

        var id = linha[..primeiroSeparador];
        var valor = linha[(primeiroSeparador + 1)..ultimoSeparador];
        var valida = linha[(ultimoSeparador + 1)..];

        return new Medicao(
            int.Parse(id, CultureInfo.InvariantCulture),
            decimal.Parse(valor, CultureInfo.InvariantCulture),
            bool.Parse(valida));
    }

    public static int Somar(ReadOnlySpan<int> valores)
    {
        var soma = 0;
        foreach (var valor in valores)
            soma += valor;

        return soma;
    }

    public static async Task<int> SomarDepoisDeAguardarAsync(
        ReadOnlyMemory<int> valores,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return Somar(valores.Span);
    }

    public static async Task<byte> LerCabecalhoAntesDeAguardarAsync(
        ReadOnlyMemory<byte> dados,
        CancellationToken cancellationToken = default)
    {
        byte cabecalho;
        {
            ReadOnlySpan<byte> trechoSincrono = dados.Span;
            cabecalho = trechoSincrono.Length > 0
                ? trechoSincrono[0]
                : throw new ArgumentException("Buffer vazio.", nameof(dados));
        }

        await Task.Delay(1, cancellationToken);
        return cabecalho;
    }

    public static int SomarComBufferAlugado(ReadOnlySpan<int> origem)
    {
        var buffer = ArrayPool<int>.Shared.Rent(origem.Length);
        try
        {
            origem.CopyTo(buffer);
            return Somar(buffer.AsSpan(0, origem.Length));
        }
        finally
        {
            ArrayPool<int>.Shared.Return(buffer, clearArray: true);
        }
    }
}
