using Microsoft.Data.Sqlite;

namespace EngenhariaDotnet.Microservicos;

public sealed class ConsumidorPedidoCriado(SqliteConnection conexao)
{
    public static async Task CriarSchemaAsync(
        SqliteConnection conexao,
        CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS inbox (
                consumidor TEXT NOT NULL,
                mensagem_id TEXT NOT NULL,
                processada_em TEXT NOT NULL,
                PRIMARY KEY (consumidor, mensagem_id)
            );

            CREATE TABLE IF NOT EXISTS pedidos_recebidos (
                pedido_id TEXT PRIMARY KEY,
                total TEXT NOT NULL
            );
            """;

        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ProcessarAsync(
        PedidoCriado evento,
        CancellationToken cancellationToken)
    {
        await using var transacao = await conexao.BeginTransactionAsync(cancellationToken);

        await using var registrar = conexao.CreateCommand();
        registrar.Transaction = (SqliteTransaction)transacao;
        registrar.CommandText = """
            INSERT INTO inbox (consumidor, mensagem_id, processada_em)
            VALUES ($consumidor, $mensagemId, $processadaEm)
            ON CONFLICT (consumidor, mensagem_id) DO NOTHING;
            """;
        registrar.Parameters.AddWithValue("$consumidor", nameof(ConsumidorPedidoCriado));
        registrar.Parameters.AddWithValue("$mensagemId", evento.MensagemId.ToString("D"));
        registrar.Parameters.AddWithValue("$processadaEm", DateTimeOffset.UtcNow.ToString("O"));

        if (await registrar.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            await transacao.RollbackAsync(cancellationToken);
            return false;
        }

        await using var aplicarEfeito = conexao.CreateCommand();
        aplicarEfeito.Transaction = (SqliteTransaction)transacao;
        aplicarEfeito.CommandText = """
            INSERT INTO pedidos_recebidos (pedido_id, total)
            VALUES ($pedidoId, $total);
            """;
        aplicarEfeito.Parameters.AddWithValue("$pedidoId", evento.PedidoId.ToString("D"));
        aplicarEfeito.Parameters.AddWithValue("$total", evento.Total.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await aplicarEfeito.ExecuteNonQueryAsync(cancellationToken);

        await transacao.CommitAsync(cancellationToken);
        return true;
    }
}
