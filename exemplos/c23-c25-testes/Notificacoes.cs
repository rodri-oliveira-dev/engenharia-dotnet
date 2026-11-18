namespace EngenhariaDotnet.Testes;

public interface IEmailSender
{
    Task EnviarAsync(
        string destinatario,
        string assunto,
        CancellationToken cancellationToken);
}

public sealed class CadastroClienteComDefeito(IEmailSender emailSender)
{
    public Task CadastrarAsync(string email, CancellationToken cancellationToken) =>
        emailSender.EnviarAsync(
            "administrativo@exemplo.test",
            "Cadastro confirmado",
            cancellationToken);
}

public sealed class CadastroCliente(IEmailSender emailSender)
{
    public Task CadastrarAsync(string email, CancellationToken cancellationToken) =>
        emailSender.EnviarAsync(email, "Cadastro confirmado", cancellationToken);
}
