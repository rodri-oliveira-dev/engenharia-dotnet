using System.Diagnostics.CodeAnalysis;

namespace C02.Nulabilidade;

public sealed record Cadastro(string Nome, string Email);

public static class CadastroFactory
{
    public static Cadastro Criar(string? nome, string? email)
    {
        var nomeValido = Normalizar(nome, "nome");
        var emailValido = Normalizar(email, "email");
        return new Cadastro(nomeValido, emailValido);
    }

    public static string NomeDeExibicao(string? apelido, string nome) =>
        apelido?.Trim() is { Length: > 0 } valor ? valor : nome;

    public static bool TentarNormalizar(
        string? valor,
        [NotNullWhen(true)] out string? normalizado)
    {
        normalizado = valor?.Trim();
        return normalizado is { Length: > 0 };
    }

    private static string Normalizar(string? valor, string parametro)
    {
        if (!TentarNormalizar(valor, out var normalizado))
        {
            throw new ArgumentException("O valor é obrigatório.", parametro);
        }

        return normalizado;
    }
}
