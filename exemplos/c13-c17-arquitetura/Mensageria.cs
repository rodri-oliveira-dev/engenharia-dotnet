using System.Diagnostics;

namespace EngenhariaDotnet.Arquitetura;

public sealed record Mensagem(Guid Id, string Tipo, string Corpo);
public sealed record MensagemMorta(Mensagem Mensagem, string Motivo, int Tentativas);

public interface ITratadorMensagem
{
    Task ProcessarAsync(Mensagem mensagem, CancellationToken cancellationToken);
}

public interface IEsperaRetry
{
    Task AguardarAsync(TimeSpan atraso, CancellationToken cancellationToken);
}

public sealed class ProcessadorResiliente(
    ITratadorMensagem tratador,
    IEsperaRetry espera,
    int maximoDeTentativas = 3)
{
    public async Task<MensagemMorta?> ProcessarAsync(Mensagem mensagem, CancellationToken cancellationToken)
    {
        if (maximoDeTentativas < 1) throw new InvalidOperationException("Configure ao menos uma tentativa.");

        for (var tentativa = 1; tentativa <= maximoDeTentativas; tentativa++)
        {
            try
            {
                await tratador.ProcessarAsync(mensagem, cancellationToken);
                return null;
            }
            catch (FalhaPermanenteException ex)
            {
                return new MensagemMorta(mensagem, ex.Message, tentativa);
            }
            catch (FalhaTransitoriaException ex) when (tentativa == maximoDeTentativas)
            {
                return new MensagemMorta(mensagem, ex.Message, tentativa);
            }
            catch (FalhaTransitoriaException)
            {
                // Exponencial determinístico no laboratório; produção deve adicionar jitter.
                await espera.AguardarAsync(TimeSpan.FromSeconds(Math.Pow(2, tentativa - 1)), cancellationToken);
            }
        }

        throw new UnreachableException();
    }
}

public sealed class FalhaTransitoriaException(string message) : Exception(message);
public sealed class FalhaPermanenteException(string message) : Exception(message);
