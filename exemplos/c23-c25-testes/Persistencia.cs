using System.Data.Common;

namespace EngenhariaDotnet.Testes;

public sealed record PedidoPersistido(string Id, decimal Total);

public sealed class RepositorioPedidos(DbConnection connection)
{
    public async Task AdicionarAsync(
        PedidoPersistido pedido,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO pedidos (id, total) VALUES (@id, @total)";
        AdicionarParametro(command, "@id", pedido.Id);
        AdicionarParametro(command, "@total", pedido.Total);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PedidoPersistido?> ObterAsync(
        string id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, total FROM pedidos WHERE id = @id";
        AdicionarParametro(command, "@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new PedidoPersistido(reader.GetString(0), reader.GetDecimal(1));
    }

    private static void AdicionarParametro(
        DbCommand command,
        string nome,
        object valor)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = nome;
        parameter.Value = valor;
        command.Parameters.Add(parameter);
    }
}
