using System.Globalization;
using NSubstitute;
using NSubstitute.Exceptions;
using Xunit;

namespace EngenhariaDotnet.Testes;

public sealed class PoliticaDescontoTests
{
    [Fact]
    public void ClienteFidelidade_ComTotalElegivel_RecebeDezPorCento()
    {
        // Arrange
        const decimal total = 250m;

        // Act
        var desconto = PoliticaDesconto.Calcular(total, CategoriaCliente.Fidelidade);

        // Assert
        Assert.Equal(25m, desconto);
    }

    [Theory]
    [InlineData(199.99, CategoriaCliente.Fidelidade, 0)]
    [InlineData(200, CategoriaCliente.Fidelidade, 20)]
    [InlineData(500, CategoriaCliente.Regular, 0)]
    public void Calcular_ParaCenariosDeElegibilidade_RetornaDescontoEsperado(
        double total,
        CategoriaCliente categoria,
        double esperado)
    {
        var desconto = PoliticaDesconto.Calcular((decimal)total, categoria);

        Assert.Equal((decimal)esperado, desconto);
    }
}

public sealed class DublesTests
{
    [Fact]
    public async Task ArgAny_AceitaChamadaComDestinatarioErrado_EGeraFalsoPositivo()
    {
        var sender = Substitute.For<IEmailSender>();
        var sut = new CadastroClienteComDefeito(sender);

        await sut.CadastrarAsync("cliente@exemplo.test", CancellationToken.None);

        await sender.Received(1).EnviarAsync(
            Arg.Any<string>(),
            "Cadastro confirmado",
            CancellationToken.None);
    }

    [Fact]
    public async Task ValidacaoDoArgumento_RevelaDestinatarioErrado()
    {
        var sender = Substitute.For<IEmailSender>();
        var sut = new CadastroClienteComDefeito(sender);

        await sut.CadastrarAsync("cliente@exemplo.test", CancellationToken.None);

        await Assert.ThrowsAsync<ReceivedCallsException>(() => sender.Received(1).EnviarAsync(
            "cliente@exemplo.test",
            "Cadastro confirmado",
            CancellationToken.None));
    }

    [Fact]
    public async Task ImplementacaoCorrigida_EnviaAoClienteInformado()
    {
        var sender = Substitute.For<IEmailSender>();
        var sut = new CadastroCliente(sender);

        await sut.CadastrarAsync("cliente@exemplo.test", CancellationToken.None);

        await sender.Received(1).EnviarAsync(
            Arg.Is<string>(email => email == "cliente@exemplo.test"),
            "Cadastro confirmado",
            CancellationToken.None);
    }
}

public sealed class ProcessadorAssincronoTests
{
    [Fact]
    public async Task ProcessarAsync_SoConcluiDepoisDoManipulador()
    {
        var iniciou = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = new ProcessadorAssincrono();

        var processamento = sut.ProcessarAsync(async cancellationToken =>
        {
            iniciou.SetResult();
            await liberar.Task.WaitAsync(cancellationToken);
        }, CancellationToken.None);

        await iniciou.Task.WaitAsync(CancellationToken.None);
        Assert.False(processamento.IsCompleted);

        liberar.SetResult();
        await processamento;
    }
}

public sealed class CalculadoraFreteLegadaTests
{
    [Theory]
    // InlineData nao aceita literais decimais: usar texto evita conversoes imprecisas de double.
    [InlineData(0, "SUL", "7.01")]
    [InlineData(2, "SUL", "9.31")]
    [InlineData(2, "sul", "9.71")]
    public void Calcular_CaracterizaComportamentoExistente(
        int peso,
        string regiao,
        string resultadoHistorico)
    {
        var sut = new CalculadoraFreteLegada();

        var resultado = sut.Calcular(peso, regiao);

        Assert.Equal(decimal.Parse(resultadoHistorico, CultureInfo.InvariantCulture), resultado);
    }
}
