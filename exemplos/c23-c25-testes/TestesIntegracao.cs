using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EngenhariaDotnet.Testes;

public sealed class RepositorioPedidosSqliteTests
{
    [Fact]
    public async Task AdicionarEObter_PreservaPedidoNoBancoRelacional()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(CancellationToken.None);
        await using (var schema = connection.CreateCommand())
        {
            schema.CommandText = """
                CREATE TABLE pedidos (
                    id TEXT PRIMARY KEY,
                    total NUMERIC NOT NULL CHECK (total >= 0)
                );
                """;
            await schema.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var sut = new RepositorioPedidos(connection);
        var pedido = new PedidoPersistido("pedido-42", 125.50m);

        await sut.AdicionarAsync(pedido, CancellationToken.None);
        var persistido = await sut.ObterAsync(
            pedido.Id,
            CancellationToken.None);

        Assert.Equal(pedido, persistido);
    }
}

public sealed class ApiDescontosTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiDescontosTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetDesconto_ExecutaPipelineERetornaContratoJson()
    {
        var response = await _client.GetAsync(
            "/descontos/250?categoria=Fidelidade",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RespostaDesconto>(
            CancellationToken.None);
        Assert.Equal(new RespostaDesconto(250m, 25m, 225m), body);
    }
}
