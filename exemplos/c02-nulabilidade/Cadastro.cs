using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

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

// Resultado de negocio esperado; argumentos invalidos seguem o contrato de Criar.
public sealed record CadastroRegistrado(Cadastro Cadastro);
public sealed record EmailEmUso(string Email);

// Dois objetos JSON com propriedades de raiz distintas: Cadastro ou Email.
[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union ResultadoRegistro(CadastroRegistrado, EmailEmUso);

public static class RegistroComResultado
{
    public static ResultadoRegistro Registrar(
        string? nome,
        string? email,
        IEnumerable<string> emailsJaCadastrados)
    {
        ArgumentNullException.ThrowIfNull(emailsJaCadastrados);
        var cadastro = CadastroFactory.Criar(nome, email);

        return emailsJaCadastrados.Contains(
            cadastro.Email, StringComparer.OrdinalIgnoreCase)
            ? new EmailEmUso(cadastro.Email)
            : new CadastroRegistrado(cadastro);
    }

    public static string Descrever(ResultadoRegistro resultado) => resultado switch
    {
        CadastroRegistrado(var cadastro) => $"Cadastro criado: {cadastro.Email}",
        EmailEmUso(var email) => $"E-mail ja utilizado: {email}"
    };
}
