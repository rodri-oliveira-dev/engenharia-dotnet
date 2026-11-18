using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace EngenhariaDotnet.MemoriaPerformance.Benchmarks;

[MemoryDiagnoser]
public class ParserBenchmarks
{
    private const string Linha = "42|10.50|true";

    [Benchmark(Baseline = true)]
    public Medicao ComStrings() => Processamento.InterpretarComStrings(Linha);

    [Benchmark]
    public Medicao ComSpan() => Processamento.InterpretarComSpan(Linha.AsSpan());
}

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
