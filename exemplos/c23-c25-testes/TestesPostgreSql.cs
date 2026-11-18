using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace EngenhariaDotnet.Testes;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:17.6-alpine")
        .WithDatabase("engenharia_testes")
        .WithUsername("testes")
        .WithPassword("senha-local-descartavel")
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class RepositorioPedidosPostgreSqlTests(
    PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Docker")]
    public async Task RestricaoDoPostgreSql_RejeitaTotalNegativo()
    {
        await using var connection = new NpgsqlConnection(fixture.Container.GetConnectionString());
        await connection.OpenAsync(CancellationToken.None);
        await using (var schema = connection.CreateCommand())
        {
            schema.CommandText = """
                CREATE TABLE pedidos (
                    id text PRIMARY KEY,
                    total numeric(12, 2) NOT NULL CHECK (total >= 0)
                );
                """;
            await schema.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var sut = new RepositorioPedidos(connection);

        await Assert.ThrowsAsync<PostgresException>(() => sut.AdicionarAsync(
            new PedidoPersistido("pedido-invalido", -0.01m),
            CancellationToken.None));
    }
}
