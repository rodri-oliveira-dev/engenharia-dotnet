using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using EngenhariaDotnet.AssincronismoConcorrencia;
using Xunit;

namespace EngenhariaDotnet.AssincronismoConcorrencia.Tests;

public sealed class AssincronismoConcorrenciaTests
{
    [Fact]
    public async Task Cliente_http_encaminha_cancelamento_a_operacao_externa()
    {
        var manipulador = new ManipuladorAguardandoCancelamento();
        using var cliente = new HttpClient(manipulador);
        using var cancellationTokenSource = new CancellationTokenSource();

        var leitura = ProcessamentoAssincrono.LerApiExternaAsync(
            cliente,
            "https://exemplo.invalid/recurso",
            cancellationTokenSource.Token);

        await manipulador.RequisicaoRecebida;
        cancellationTokenSource.Cancel();

        var excecao = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => leitura);
        Assert.True(manipulador.TokenRecebido.CanBeCanceled);
        Assert.True(manipulador.TokenRecebido.IsCancellationRequested);
        Assert.Equal(cancellationTokenSource.Token, excecao.CancellationToken);
    }

    [Fact]
    public async Task Cancelamento_cooperativo_interrompe_o_processamento()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var excecao = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ProcessamentoAssincrono.ProcessarEmSequenciaAsync(
                new[] { 1, 2, 3 },
                static (_, _) => Task.CompletedTask,
                cancellationTokenSource.Token));

        Assert.Equal(cancellationTokenSource.Token, excecao.CancellationToken);
    }

    [Fact]
    public async Task Limite_impede_mais_operacoes_que_o_configurado()
    {
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var limiteAtingido = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var emExecucao = 0;
        var maximoObservado = 0;

        var processamento = ProcessamentoAssincrono.ProcessarComLimiteAsync(
            Enumerable.Range(0, 8),
            limite: 2,
            async (_, cancellationToken) =>
            {
                var atual = Interlocked.Increment(ref emExecucao);
                AtualizarMaximo(ref maximoObservado, atual);
                if (atual == 2)
                    limiteAtingido.TrySetResult();

                try
                {
                    await liberar.Task.WaitAsync(cancellationToken);
                }
                finally
                {
                    Interlocked.Decrement(ref emExecucao);
                }
            });

        await limiteAtingido.Task;
        Assert.Equal(2, Volatile.Read(ref maximoObservado));

        liberar.SetResult();
        await processamento;
        Assert.Equal(2, maximoObservado);
    }

    [Fact]
    public async Task Falha_ao_enumerar_nao_inicia_operacoes_parciais()
    {
        var operacoesIniciadas = 0;

        static IEnumerable<int> ItensComFalha()
        {
            yield return 1;
            throw new InvalidOperationException("falha durante a enumeração");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProcessamentoAssincrono.ProcessarComLimiteAsync(
                ItensComFalha(),
                limite: 2,
                (_, _) =>
                {
                    Interlocked.Increment(ref operacoesIniciadas);
                    return Task.CompletedTask;
                }));

        Assert.Equal(0, operacoesIniciadas);
    }

    [Fact]
    public async Task WhenAll_sem_limite_inicia_todo_o_lote()
    {
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var todosIniciados = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var iniciados = 0;

        var processamento = ProcessamentoAssincrono.ProcessarSemLimiteAsync(
            Enumerable.Range(0, 8),
            async (_, cancellationToken) =>
            {
                if (Interlocked.Increment(ref iniciados) == 8)
                    todosIniciados.TrySetResult();

                await liberar.Task.WaitAsync(cancellationToken);
            });

        await todosIniciados.Task;
        Assert.Equal(8, Volatile.Read(ref iniciados));

        liberar.SetResult();
        await processamento;
    }

    [Fact]
    public async Task WhenAll_preserva_todas_as_falhas_na_task_agregada()
    {
        var primeira = Task.FromException(new InvalidOperationException("primeira"));
        var segunda = Task.FromException(new ArgumentException("segunda"));
        var todas = Task.WhenAll(primeira, segunda);

        await Assert.ThrowsAnyAsync<Exception>(() => todas);

        Assert.NotNull(todas.Exception);
        Assert.Equal(2, todas.Exception.InnerExceptions.Count);
    }

    [Fact]
    public async Task WhenAny_devolve_a_task_concluida_e_exige_observar_sua_falha()
    {
        var pendente = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var falha = Task.FromException(new InvalidOperationException("primeira"));

        var concluida = await Task.WhenAny(pendente.Task, falha);

        Assert.Same(falha, concluida);
        Assert.False(pendente.Task.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await concluida);

        pendente.SetResult();
        await pendente.Task;
    }

    [Fact]
    public async Task Channel_limitado_aplica_backpressure_ao_produtor()
    {
        var canal = Channel.CreateBounded<int>(1);
        await canal.Writer.WriteAsync(1);

        var segundaEscrita = canal.Writer.WriteAsync(2);
        Assert.False(segundaEscrita.IsCompleted);

        Assert.Equal(1, await canal.Reader.ReadAsync());
        await segundaEscrita;
        Assert.Equal(2, await canal.Reader.ReadAsync());
    }

    [Fact]
    public async Task Consumidores_concorrentes_podem_publicar_resultados_na_ordem_de_entrada()
    {
        var resultados = await ProcessamentoAssincrono.ProcessarComChannelAsync(
            Enumerable.Range(1, 5),
            capacidade: 2,
            consumidores: 2,
            static (item, _) => ValueTask.FromResult(item * 10));

        Assert.Equal(new[] { 10, 20, 30, 40, 50 }, resultados);
    }

    [Fact]
    public async Task Falha_do_consumidor_interrompe_o_produtor_sem_deixar_backlog_bloqueado()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProcessamentoAssincrono.ProcessarComChannelAsync<int, int>(
                Enumerable.Range(1, 100),
                capacidade: 1,
                consumidores: 1,
                static (_, _) => throw new InvalidOperationException("falha do consumidor")));
    }

    [Fact]
    public async Task Stream_assincrono_observa_cancelamento_entre_elementos()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var recebidos = new List<int>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in ProcessamentoAssincrono.GerarSobDemandaAsync(
                10,
                cancellationTokenSource.Token))
            {
                recebidos.Add(item);
                cancellationTokenSource.Cancel();
            }
        });

        Assert.Equal(new[] { 0 }, recebidos);
    }

    [Fact]
    public async Task Throw_preserva_a_origem_da_excecao()
    {
        static Task OrigemDaFalha() => throw new InvalidOperationException("falha");

        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProcessamentoAssincrono.ExecutarPreservandoPilhaAsync(OrigemDaFalha));

        Assert.Contains(nameof(OrigemDaFalha), excecao.StackTrace);
    }

    [Fact]
    public async Task ValueTask_completa_sincronamente_quando_o_cache_responde()
    {
        var chamadasRemotas = new ConcurrentBag<int>();
        var catalogo = new CatalogoComCache(
            new Dictionary<int, string> { [1] = "cache" },
            (id, _) =>
            {
                chamadasRemotas.Add(id);
                return Task.FromResult("remoto");
            });

        var consulta = catalogo.ObterAsync(1);

        Assert.True(consulta.IsCompletedSuccessfully);
        Assert.Equal("cache", await consulta);
        Assert.Empty(chamadasRemotas);
    }

    private static void AtualizarMaximo(ref int destino, int candidato)
    {
        int observado;
        do
        {
            observado = Volatile.Read(ref destino);
            if (candidato <= observado)
                return;
        }
        while (Interlocked.CompareExchange(ref destino, candidato, observado) != observado);
    }

    private sealed class ManipuladorAguardandoCancelamento : HttpMessageHandler
    {
        private readonly TaskCompletionSource _requisicaoRecebida = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task RequisicaoRecebida => _requisicaoRecebida.Task;
        public CancellationToken TokenRecebido { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            TokenRecebido = cancellationToken;
            _requisicaoRecebida.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new UnreachableException();
        }
    }
}
