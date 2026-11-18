namespace EngenhariaDotnet.Testes;

public static class Aplicacao
{
    public static WebApplication Criar(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        var app = builder.Build();

        app.MapGet("/descontos/{total:decimal}", (
            decimal total,
            CategoriaCliente categoria) =>
        {
            if (total < 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(total)] = ["O total não pode ser negativo."]
                });
            }

            var desconto = PoliticaDesconto.Calcular(total, categoria);
            return Results.Ok(new RespostaDesconto(total, desconto, total - desconto));
        });

        return app;
    }
}

public sealed record RespostaDesconto(decimal Total, decimal Desconto, decimal Liquido);
