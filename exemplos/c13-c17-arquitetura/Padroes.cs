namespace EngenhariaDotnet.Arquitetura;

public sealed record ContextoDesconto(decimal Total, bool ClienteRecorrente);

// Strategy: variação real de política. Um switch local seria melhor com uma única regra estável.
public interface IPoliticaDesconto
{
    decimal Calcular(ContextoDesconto contexto);
}

public sealed class DescontoClienteRecorrente : IPoliticaDesconto
{
    public decimal Calcular(ContextoDesconto contexto) =>
        contexto.ClienteRecorrente ? contexto.Total * 0.05m : 0m;
}

public sealed class DescontoCampanhaSazonal : IPoliticaDesconto
{
    public decimal Calcular(ContextoDesconto contexto) =>
        contexto.Total >= 200m ? contexto.Total * 0.10m : 0m;
}

public sealed class SemDesconto : IPoliticaDesconto
{
    public decimal Calcular(ContextoDesconto contexto) => 0m;
}

// Simple Factory: concentra a seleção e a criação sem usar Factory Method ou Abstract Factory.
public static class PoliticaDescontoFactory
{
    public static IPoliticaDesconto Criar(string campanha) => campanha switch
    {
        "recorrencia" => new DescontoClienteRecorrente(),
        "sazonal" => new DescontoCampanhaSazonal(),
        "nenhuma" => new SemDesconto(),
        _ => throw new ArgumentOutOfRangeException(nameof(campanha), campanha, "Campanha desconhecida.")
    };
}

public interface ICalculadorFrete
{
    decimal Calcular(string cep, decimal pesoEmKg);
}

// Adapter: traduz contrato e unidade de um fornecedor legado.
public interface IFreteLegado
{
    int CotarEmCentavos(string codigoPostal, int pesoEmGramas);
}

public sealed class FreteLegadoAdapter(IFreteLegado legado) : ICalculadorFrete
{
    public decimal Calcular(string cep, decimal pesoEmKg)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cep);

        if (pesoEmKg <= 0m || pesoEmKg > int.MaxValue / 1_000m)
            throw new ArgumentOutOfRangeException(
                nameof(pesoEmKg),
                pesoEmKg,
                "O peso deve ser positivo e representável em gramas.");

        var pesoEmGramas = decimal.ToInt32(
            decimal.Round(pesoEmKg * 1_000m, 0, MidpointRounding.AwayFromZero));

        if (pesoEmGramas == 0)
            throw new ArgumentOutOfRangeException(
                nameof(pesoEmKg),
                pesoEmKg,
                "O peso deve resultar em pelo menos um grama.");

        var valorEmCentavos = legado.CotarEmCentavos(cep, pesoEmGramas);

        if (valorEmCentavos < 0)
            throw new InvalidOperationException("O fornecedor legado retornou um valor de frete negativo.");

        return valorEmCentavos / 100m;
    }
}

public interface IConsultaPedido
{
    Task<string> ObterAsync(Guid pedidoId, CancellationToken cancellationToken);
}

// Decorator: adiciona comportamento ortogonal preservando o contrato.
public sealed class ConsultaPedidoComAuditoria(
    IConsultaPedido inner,
    Action<Guid> registrarConsulta) : IConsultaPedido
{
    public async Task<string> ObterAsync(Guid pedidoId, CancellationToken cancellationToken)
    {
        var resultado = await inner.ObterAsync(pedidoId, cancellationToken);
        registrarConsulta(pedidoId);
        return resultado;
    }
}

// Observer no mesmo processo. Para entrega entre processos, use mensageria durável.
public sealed record PedidoConfirmado(Guid PedidoId);

public interface IObservadorPedido
{
    Task AoConfirmarAsync(PedidoConfirmado evento, CancellationToken cancellationToken);
}

public sealed class NotificadorPedido(IEnumerable<IObservadorPedido> observadores)
{
    public async Task NotificarAsync(PedidoConfirmado evento, CancellationToken cancellationToken)
    {
        foreach (var observador in observadores)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await observador.AoConfirmarAsync(evento, cancellationToken);
        }
    }
}
