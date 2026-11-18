using System.Collections.ObjectModel;
using System.Net.Mail;

namespace C11C12.Ddd;

public sealed class RegraDeNegocioException(string mensagem) : Exception(mensagem);

public sealed record Dinheiro
{
    public Dinheiro(decimal quantia, string moeda)
    {
        if (quantia < 0)
            throw new RegraDeNegocioException("A quantia não pode ser negativa.");
        var moedaNormalizada = moeda?.Trim().ToUpperInvariant();
        if (moedaNormalizada is null || moedaNormalizada.Length != 3 ||
            moedaNormalizada.Any(caractere => caractere is < 'A' or > 'Z'))
            throw new RegraDeNegocioException("A moeda deve usar um código de três letras.");

        Quantia = quantia;
        Moeda = moedaNormalizada;
    }

    public decimal Quantia { get; }
    public string Moeda { get; }

    public static Dinheiro Zero(string moeda) => new(0, moeda);

    public Dinheiro Somar(Dinheiro parcela)
    {
        ExigirMesmaMoeda(parcela);
        return new Dinheiro(Quantia + parcela.Quantia, Moeda);
    }

    public Dinheiro Multiplicar(int multiplicador)
    {
        if (multiplicador < 0)
            throw new RegraDeNegocioException("O multiplicador não pode ser negativo.");
        return new Dinheiro(Quantia * multiplicador, Moeda);
    }

    public Dinheiro Aplicar(Percentual percentual) =>
        new(decimal.Round(Quantia * (1 - percentual.Fracao), 2, MidpointRounding.ToEven), Moeda);

    private void ExigirMesmaMoeda(Dinheiro outro)
    {
        if (!StringComparer.Ordinal.Equals(Moeda, outro.Moeda))
            throw new RegraDeNegocioException("Não é possível operar valores de moedas diferentes.");
    }
}

public sealed record Email
{
    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new RegraDeNegocioException("O e-mail é obrigatório.");

        try
        {
            var endereco = new MailAddress(valor.Trim());
            if (!StringComparer.OrdinalIgnoreCase.Equals(endereco.Address, valor.Trim()))
                throw new FormatException();
            Valor = endereco.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new RegraDeNegocioException("O e-mail informado é inválido.");
        }
    }

    public string Valor { get; }
    public override string ToString() => Valor;
}

public readonly record struct Percentual
{
    public Percentual(decimal valor)
    {
        if (valor is < 0 or > 100)
            throw new RegraDeNegocioException("O percentual deve estar entre 0 e 100.");
        Valor = valor;
    }

    public decimal Valor { get; }
    public decimal Fracao => Valor / 100m;
}

public sealed record ProdutoId
{
    public ProdutoId(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new RegraDeNegocioException("O produto é obrigatório.");
        Valor = valor.Trim().ToUpperInvariant();
    }

    public string Valor { get; }
    public override string ToString() => Valor;
}

public enum EstadoPedido { Rascunho, Confirmado, Cancelado }

public sealed class ItemPedido
{
    internal ItemPedido(ProdutoId produtoId, int quantidade, Dinheiro precoUnitario)
    {
        ProdutoId = produtoId;
        PrecoUnitario = precoUnitario;
        AlterarQuantidade(quantidade);
    }

    public ProdutoId ProdutoId { get; }
    public int Quantidade { get; private set; }
    public Dinheiro PrecoUnitario { get; }
    public Dinheiro Subtotal => PrecoUnitario.Multiplicar(Quantidade);

    internal void Acrescentar(int quantidade) => AlterarQuantidade(checked(Quantidade + quantidade));

    internal void Remover(int quantidade) => AlterarQuantidade(Quantidade - quantidade);

    private void AlterarQuantidade(int quantidade)
    {
        if (quantidade is < 1 or > 100)
            throw new RegraDeNegocioException("A quantidade de um item deve estar entre 1 e 100.");
        Quantidade = quantidade;
    }
}

public interface IEventoDeDominio;

public sealed record PedidoConfirmado(
    Guid PedidoId,
    Dinheiro Total,
    DateTimeOffset OcorridoEm) : IEventoDeDominio;

public sealed class Pedido
{
    private const int MaximoDeItensDistintos = 20;
    private static readonly Percentual DescontoMaximo = new(30);
    private readonly List<ItemPedido> _itens = [];
    private readonly List<IEventoDeDominio> _eventos = [];
    private readonly ReadOnlyCollection<ItemPedido> _itensSomenteLeitura;
    private readonly ReadOnlyCollection<IEventoDeDominio> _eventosSomenteLeitura;

    public Pedido(Guid id, Email emailCliente, string moeda)
    {
        if (id == Guid.Empty)
            throw new RegraDeNegocioException("O identificador do pedido é obrigatório.");

        Id = id;
        EmailCliente = emailCliente;
        Moeda = Dinheiro.Zero(moeda).Moeda;
        Desconto = new Percentual(0);
        Estado = EstadoPedido.Rascunho;
        _itensSomenteLeitura = _itens.AsReadOnly();
        _eventosSomenteLeitura = _eventos.AsReadOnly();
    }

    public Guid Id { get; }
    public Email EmailCliente { get; }
    public string Moeda { get; }
    public EstadoPedido Estado { get; private set; }
    public Percentual Desconto { get; private set; }
    public IReadOnlyList<ItemPedido> Itens => _itensSomenteLeitura;
    public IReadOnlyList<IEventoDeDominio> EventosDeDominio => _eventosSomenteLeitura;

    public Dinheiro TotalBruto => _itens.Aggregate(
        Dinheiro.Zero(Moeda),
        (total, item) => total.Somar(item.Subtotal));

    public Dinheiro Total => TotalBruto.Aplicar(Desconto);

    public void AdicionarItem(ProdutoId produtoId, int quantidade, Dinheiro precoUnitario)
    {
        ExigirRascunho();
        ExigirMoeda(precoUnitario);
        if (quantidade is < 1 or > 100)
            throw new RegraDeNegocioException("A quantidade adicionada deve estar entre 1 e 100.");

        var existente = _itens.SingleOrDefault(item => item.ProdutoId == produtoId);
        if (existente is not null)
        {
            if (existente.PrecoUnitario != precoUnitario)
                throw new RegraDeNegocioException("O preço de um item existente não pode mudar implicitamente.");
            existente.Acrescentar(quantidade);
            return;
        }

        if (_itens.Count == MaximoDeItensDistintos)
            throw new RegraDeNegocioException("O pedido aceita no máximo 20 itens distintos.");
        _itens.Add(new ItemPedido(produtoId, quantidade, precoUnitario));
    }

    public void RemoverItem(ProdutoId produtoId, int quantidade)
    {
        ExigirRascunho();
        if (quantidade <= 0)
            throw new RegraDeNegocioException("A quantidade removida deve ser positiva.");

        var item = _itens.SingleOrDefault(i => i.ProdutoId == produtoId)
            ?? throw new RegraDeNegocioException("O item não pertence ao pedido.");

        if (quantidade > item.Quantidade)
            throw new RegraDeNegocioException("Não é possível remover mais unidades do que o pedido contém.");
        if (quantidade == item.Quantidade)
            _itens.Remove(item);
        else
            item.Remover(quantidade);
    }

    public void AplicarDesconto(Percentual desconto)
    {
        ExigirRascunho();
        if (desconto.Valor > DescontoMaximo.Valor)
            throw new RegraDeNegocioException("O desconto de um pedido não pode superar 30%.");
        Desconto = desconto;
    }

    public void Confirmar(DateTimeOffset ocorridoEm)
    {
        ExigirRascunho();
        if (_itens.Count == 0)
            throw new RegraDeNegocioException("Um pedido sem itens não pode ser confirmado.");
        if (Total.Quantia == 0)
            throw new RegraDeNegocioException("Um pedido de valor zero não pode ser confirmado.");

        Estado = EstadoPedido.Confirmado;
        _eventos.Add(new PedidoConfirmado(Id, Total, ocorridoEm));
    }

    public void Cancelar()
    {
        if (Estado != EstadoPedido.Confirmado)
            throw new RegraDeNegocioException("Somente um pedido confirmado pode ser cancelado.");
        Estado = EstadoPedido.Cancelado;
    }

    public void LimparEventosDeDominio() => _eventos.Clear();

    private void ExigirRascunho()
    {
        if (Estado != EstadoPedido.Rascunho)
            throw new RegraDeNegocioException("Somente um pedido em rascunho pode ser alterado.");
    }

    private void ExigirMoeda(Dinheiro dinheiro)
    {
        if (!StringComparer.Ordinal.Equals(Moeda, dinheiro.Moeda))
            throw new RegraDeNegocioException("Todos os itens devem usar a moeda do pedido.");
    }
}
