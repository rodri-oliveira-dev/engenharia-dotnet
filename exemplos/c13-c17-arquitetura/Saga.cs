namespace EngenhariaDotnet.Arquitetura;

public enum EstadoSagaPedido
{
    Iniciada,
    EstoqueReservado,
    PagamentoAutorizado,
    Concluida,
    Compensando,
    Compensada,
    FalhaManual
}

public sealed class SagaPedido(Guid id, Guid pedidoId)
{
    public Guid Id { get; } = id;
    public Guid PedidoId { get; } = pedidoId;
    public EstadoSagaPedido Estado { get; private set; } = EstadoSagaPedido.Iniciada;
    public string? UltimoErro { get; private set; }

    public void EstoqueReservado() => Transicionar(EstadoSagaPedido.Iniciada, EstadoSagaPedido.EstoqueReservado);
    public void PagamentoAutorizado() => Transicionar(EstadoSagaPedido.EstoqueReservado, EstadoSagaPedido.PagamentoAutorizado);
    public void Concluir() => Transicionar(EstadoSagaPedido.PagamentoAutorizado, EstadoSagaPedido.Concluida);

    public void IniciarCompensacao(string erro)
    {
        if (Estado is EstadoSagaPedido.Concluida or EstadoSagaPedido.Compensada)
            throw new InvalidOperationException("A saga já terminou.");
        UltimoErro = erro;
        Estado = EstadoSagaPedido.Compensando;
    }

    public void ConfirmarCompensacao() => Transicionar(EstadoSagaPedido.Compensando, EstadoSagaPedido.Compensada);

    public void ExigirIntervencao(string erro)
    {
        UltimoErro = erro;
        Estado = EstadoSagaPedido.FalhaManual;
    }

    private void Transicionar(EstadoSagaPedido esperado, EstadoSagaPedido destino)
    {
        if (Estado != esperado)
            throw new InvalidOperationException($"Transição inválida: {Estado} -> {destino}.");
        Estado = destino;
    }
}
