using EngenhariaDotnet.MemoriaPerformance;
using Xunit;

namespace EngenhariaDotnet.MemoriaPerformance.Tests;

public sealed class MemoriaPerformanceTests
{
    [Theory]
    [InlineData("42|10.50|true")]
    [InlineData("7|-3.25|false")]
    public void Parser_com_span_preserva_o_comportamento(string linha)
    {
        var convencional = Processamento.InterpretarComStrings(linha);
        var comSpan = Processamento.InterpretarComSpan(linha.AsSpan());

        Assert.Equal(convencional, comSpan);
    }

    [Fact]
    public async Task Memory_pode_ser_preservado_durante_suspensao()
    {
        ReadOnlyMemory<int> dados = new[] { 1, 2, 3 };

        var soma = await Processamento.SomarDepoisDeAguardarAsync(dados);

        Assert.Equal(6, soma);
    }

    [Fact]
    public async Task Span_pode_ser_usado_em_bloco_que_nao_cruza_await_no_CSharp_moderno()
    {
        ReadOnlyMemory<byte> dados = new byte[] { 0x2A, 0x01 };

        var cabecalho = await Processamento.LerCabecalhoAntesDeAguardarAsync(dados);

        Assert.Equal(0x2A, cabecalho);
    }

    [Fact]
    public void Buffer_alugado_tem_o_mesmo_resultado_do_processamento_direto()
    {
        var dados = new[] { 1, 2, 3, 4 };

        Assert.Equal(Processamento.Somar(dados), Processamento.SomarComBufferAlugado(dados));
    }
}
