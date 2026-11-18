using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EngenhariaDotnet.AssincronismoConcorrencia;

public static class ProcessamentoAssincrono
{
    public static async Task<string> LerApiExternaAsync(
        HttpClient cliente,
        string endereco,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentException.ThrowIfNullOrWhiteSpace(endereco);

        using var resposta = await cliente.GetAsync(endereco, cancellationToken);
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadAsStringAsync(cancellationToken);
    }

    public static async Task ProcessarEmSequenciaAsync<T>(
        IEnumerable<T> itens,
        Func<T, CancellationToken, Task> processarAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(processarAsync);

        foreach (var item in itens)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await processarAsync(item, cancellationToken);
        }
    }

    public static Task ProcessarSemLimiteAsync<T>(
        IEnumerable<T> itens,
        Func<T, CancellationToken, Task> processarAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(processarAsync);

        return Task.WhenAll(itens.Select(item => processarAsync(item, cancellationToken)));
    }

    public static async Task ProcessarComLimiteAsync<T>(
        IEnumerable<T> itens,
        int limite,
        Func<T, CancellationToken, Task> processarAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(processarAsync);
        ArgumentOutOfRangeException.ThrowIfLessThan(limite, 1);

        using var semaforo = new SemaphoreSlim(limite, limite);

        var tarefas = itens.Select(async item =>
        {
            await semaforo.WaitAsync(cancellationToken);
            try
            {
                await processarAsync(item, cancellationToken);
            }
            finally
            {
                semaforo.Release();
            }
        });

        await Task.WhenAll(tarefas);
    }

    public static async Task<IReadOnlyList<TResult>> ProcessarComChannelAsync<T, TResult>(
        IEnumerable<T> itens,
        int capacidade,
        int consumidores,
        Func<T, CancellationToken, ValueTask<TResult>> processarAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(processarAsync);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacidade, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(consumidores, 1);

        var canal = Channel.CreateBounded<(int Ordem, T Item)>(
            new BoundedChannelOptions(capacidade)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true
            });
        var resultados = new ConcurrentDictionary<int, TResult>();
        using var encerramento = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        async Task ProduzirAsync()
        {
            var ordem = 0;
            try
            {
                foreach (var item in itens)
                {
                    await canal.Writer.WriteAsync((ordem++, item), encerramento.Token);
                }

                canal.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                canal.Writer.TryComplete(ex);
                throw;
            }
        }

        async Task ConsumirAsync()
        {
            try
            {
                await foreach (var (ordem, item) in canal.Reader.ReadAllAsync(
                    encerramento.Token))
                {
                    resultados[ordem] = await processarAsync(item, encerramento.Token);
                }
            }
            catch
            {
                await encerramento.CancelAsync();
                throw;
            }
        }

        var produtor = ProduzirAsync();
        var tarefasDosConsumidores = Enumerable.Range(0, consumidores)
            .Select(_ => ConsumirAsync());

        await Task.WhenAll(tarefasDosConsumidores.Prepend(produtor));
        return resultados.OrderBy(par => par.Key).Select(par => par.Value).ToArray();
    }

    public static async IAsyncEnumerable<int> GerarSobDemandaAsync(
        int quantidade,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidade);

        for (var valor = 0; valor < quantidade; valor++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return valor;
        }
    }

    public static async Task ExecutarPreservandoPilhaAsync(Func<Task> operacaoAsync)
    {
        ArgumentNullException.ThrowIfNull(operacaoAsync);

        try
        {
            await operacaoAsync();
        }
        catch
        {
            throw;
        }
    }
}

public sealed class CatalogoComCache
{
    private readonly IReadOnlyDictionary<int, string> _cache;
    private readonly Func<int, CancellationToken, Task<string>> _carregarAsync;

    public CatalogoComCache(
        IReadOnlyDictionary<int, string> cache,
        Func<int, CancellationToken, Task<string>> carregarAsync)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _carregarAsync = carregarAsync ?? throw new ArgumentNullException(nameof(carregarAsync));
    }

    public ValueTask<string> ObterAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _cache.TryGetValue(id, out var valor)
            ? ValueTask.FromResult(valor)
            : new ValueTask<string>(_carregarAsync(id, cancellationToken));
    }

    // Sempre há I/O e não existe caminho síncrono frequente: Task deixa o contrato
    // mais simples e pode ser aguardada mais de uma vez por consumidores legítimos.
    public static Task<string> ObterRemotamenteAsync(
        int id,
        Func<int, CancellationToken, Task<string>> carregarAsync,
        CancellationToken cancellationToken = default) =>
        carregarAsync(id, cancellationToken);
}
