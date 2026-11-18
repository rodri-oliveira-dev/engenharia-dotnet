# C05 — Assincronicidade e concorrência controlada

> **Nível:** Intermediário
>
> **Objetivo:** compor operações assíncronas com cancelamento, falhas observáveis
> e limites explícitos, distinguindo responsividade, paralelismo, rate limiting e
> backpressure.
>
> **Pré-requisitos:** [C02 — Nulabilidade, falhas e exceções](c02-nulabilidade-falhas-excecoes.md)
> e [C03 — Coleções, LINQ e execução diferida](c03-colecoes-linq-execucao-diferida.md).
>
> **Última revisão:** 2026-11-18

## Contexto

Esperar banco de dados, rede ou arquivo sem bloquear uma thread permite que um
servidor use essa thread para outra requisição. Isso pode elevar o throughput
de uma aplicação limitada por I/O, mas não reduz a duração da dependência e não
protege o banco de milhares de chamadas simultâneas. Assincronismo resolve a
espera; controle de concorrência resolve a pressão sobre recursos finitos.

Este capítulo trata do modelo Task-based Asynchronous Pattern (TAP),
cancelamento, composição, concorrência limitada, produtor/consumidor e streams
assíncronos. O contrato geral de ausência e falha pertence ao
[C02](c02-nulabilidade-falhas-excecoes.md); iteradores síncronos e os fundamentos
de execução diferida pertencem ao
[C03](c03-colecoes-linq-execucao-diferida.md).

## Fundamentos: assincronismo não é paralelismo

Quatro mecanismos respondem a problemas diferentes:

| Mecanismo | Pergunta respondida | Limite relevante |
|---|---|---|
| Assincronismo | Como não bloquear uma thread enquanto a operação espera? | Operações pendentes, memória e dependência de I/O |
| Concorrência | Quantas operações podem estar em progresso ao mesmo tempo? | Capacidade do recurso compartilhado |
| Paralelismo | Quantos trabalhos executam CPU simultaneamente? | Núcleos, ThreadPool e contenção |
| Rate limiting | Quantas admissões são aceitas por intervalo ou cota? | Política temporal ou orçamento |
| Backpressure | Como o produtor desacelera quando o consumidor não acompanha? | Capacidade da fila e política de excesso |

Concorrência pode existir em uma única thread por intercalação. Paralelismo exige
execução simultânea. Um `await` de I/O normalmente não mantém uma thread ocupada;
`Task.Run` agenda trabalho no ThreadPool e não transforma uma API síncrona de I/O
em I/O assíncrono escalável.

Rate limiting e limite de concorrência também não são equivalentes. Dez
operações simultâneas podem terminar e dar lugar a outras dez milhares de vezes
por segundo. Um limite de 100 requisições por segundo pode, por sua vez, admitir
100 chamadas lentas que permanecem concorrentes. Sistemas reais frequentemente
precisam das duas políticas.

## Funcionamento de `async` e `await`

Um método marcado `async` é transformado pelo compilador em uma máquina de
estados. A execução começa de forma síncrona e avança até encontrar um `await`
cujo awaitable ainda não terminou. Nesse ponto, o método registra uma
continuação, devolve ao chamador uma `Task` ou `Task<T>` incompleta e libera a
thread para outro trabalho. Quando a operação termina, a continuação retoma a
máquina de estados.

Se o awaitable já estiver concluído, o método pode continuar sincronamente. A
linguagem define a semântica de `await`; tamanho da máquina de estados,
alocações, inlining e estratégia de agendamento são detalhes de implementação
que podem mudar entre versões do compilador e do runtime.

`Task` representa conclusão futura sem resultado. `Task<T>` representa conclusão
com um valor `T`. Ambas podem terminar em três estados: sucesso, falha ou
cancelamento. Uma task não é necessariamente uma thread e criá-la não garante
paralelismo.

```csharp
static async Task<string> LerAsync(
    HttpClient cliente,
    string endereco,
    CancellationToken cancellationToken)
{
    using var resposta = await cliente.GetAsync(endereco, cancellationToken);
    resposta.EnsureSuccessStatusCode();
    return await resposta.Content.ReadAsStringAsync(cancellationToken);
}
```

### I/O-bound e CPU-bound

Em trabalho **I/O-bound**, o tempo dominante é a espera por outro componente.
APIs assíncronas reais permitem que a thread volte ao pool enquanto o sistema
operacional ou a dependência progride. Isso melhora escalabilidade quando há
muitas esperas sobrepostas, mas aumenta máquinas de estados, caminhos de falha e
necessidade de cancelamento. Em ferramenta simples, baixa concorrência ou API
que só existe de forma síncrona, o custo conceitual pode superar o benefício.

Em trabalho **CPU-bound**, não há espera a liberar. Para manter uma interface de
usuário responsiva, `Task.Run` pode mover o cálculo para o ThreadPool. Para
reduzir o tempo do cálculo, é preciso paralelismo justificável e divisível, por
exemplo com APIs de `Parallel`. Em servidores ASP.NET Core, envolver cálculo em
`Task.Run` apenas troca uma thread do pool por outra e pode piorar a saturação.

O critério é o gargalo observado: `async` ajuda a escalar espera de I/O; não cria
mais CPU, não acelera automaticamente cada requisição e não compensa uma
dependência saturada.

## Cancelamento é cooperativo

`CancellationToken` comunica uma solicitação; não encerra código à força. Cada
camada precisa aceitar e encaminhar o token às APIs chamadas, observar
`IsCancellationRequested`, chamar `ThrowIfCancellationRequested` ou registrar
uma callback quando apropriado. Depois que efeitos irreversíveis começaram, o
método deve definir se ainda é seguro cancelar ou se precisa concluir uma etapa
atômica.

```csharp
static async Task ProcessarAsync(
    IEnumerable<int> itens,
    Func<int, CancellationToken, Task> salvarAsync,
    CancellationToken cancellationToken)
{
    foreach (var item in itens)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await salvarAsync(item, cancellationToken);
    }
}
```

`OperationCanceledException` sinaliza o resultado cancelado quando associada ao
token esperado. Não a converta automaticamente em erro interno nem a engula
para retornar sucesso. Também não crie um token novo no meio da cadeia: isso
rompe a capacidade do chamador de interromper a operação. `CancelAfter` e tokens
vinculados podem representar timeout e encerramento da aplicação, desde que a
fronteira consiga distinguir as causas relevantes.

## Exceções atravessam `Task`

Uma exceção lançada em um método assíncrono fica armazenada na task retornada e
é observada quando o consumidor aplica `await`. O `await` relança uma exceção
com a pilha assíncrona preservada. Métodos `async void` não oferecem uma task ao
chamador e, fora de event handlers, tornam composição, teste e observação de
falha frágeis; prefira `Task`.

`Task.WhenAll` termina somente quando todas as tasks fornecidas terminam:

- conclui com sucesso se todas tiverem sucesso;
- fica faulted se pelo menos uma falhar;
- fica cancelada se nenhuma falhar e ao menos uma for cancelada;
- preserva todas as falhas na propriedade `Exception` da task agregada.

Ao aplicar `await` a uma `WhenAll` com várias falhas, uma exceção é relançada. Se
o diagnóstico precisa de todas, mantenha a referência da task agregada e examine
`Exception.InnerExceptions` depois da conclusão. Não interprete `WhenAll` como
fail-fast: as demais operações já iniciadas continuam até seus próprios
términos.

```csharp
var todas = Task.WhenAll(tarefas);
try
{
    await todas;
}
catch
{
    foreach (var falha in todas.Exception?.InnerExceptions ?? [])
        observador.Registrar(falha);
    throw;
}
```

`Task.WhenAny` resolve outro problema: a task externa termina quando a primeira
das tasks fornecidas termina e devolve essa task. A task externa termina com
sucesso mesmo quando a vencedora falhou ou foi cancelada; é preciso aguardar a
task devolvida para observar seu resultado. As demais não são canceladas nem
aguardadas automaticamente.

```csharp
var pendentes = endpoints
    .Select(endpoint => ConsultarAsync(endpoint, cancellationToken))
    .ToList();

while (pendentes.Count > 0)
{
    var concluida = await Task.WhenAny(pendentes);
    pendentes.Remove(concluida);

    var resposta = await concluida; // Observa falha ou cancelamento.
    await PublicarAsync(resposta, cancellationToken);
}
```

Esse padrão processa resultados na ordem de conclusão. Ele não cria limite de
concorrência: todas as consultas do exemplo já foram iniciadas. Também não deve
ser usado como timeout abandonando a operação perdedora. Para um timeout
cancelável, vincule tokens, solicite cancelamento e aguarde o encerramento das
operações iniciadas.

Capturar `Exception` indiscriminadamente em cada camada, registrar e seguir em
frente converte falhas em estados parcialmente válidos e duplica telemetria.
Capture onde seja possível recuperar, traduzir com `InnerException`, acrescentar
contexto útil ou produzir a resposta da fronteira. Use `throw;` ao relançar a
mesma exceção; `throw ex;` redefine a origem observável da pilha.

## Bloqueio sobre código assíncrono

`.Result` e `.Wait()` bloqueiam a thread. Em ambientes com um
`SynchronizationContext` exclusivo, como certas aplicações de interface de
usuário, a continuação pode precisar do mesmo contexto que está bloqueado,
produzindo deadlock. Mesmo sem esse contexto, bloquear consome uma thread por
espera, reduz throughput e pode causar starvation do ThreadPool. `.Wait()` e
`.Result` ainda expõem falhas por `AggregateException`, tornando o contrato
diferente de `await`.

Propague assincronismo até a fronteira. Se uma integração estritamente síncrona
for inevitável, trate-a como uma fronteira explícita, não como padrão espalhado.
`GetAwaiter().GetResult()` muda a forma da exceção, mas continua bloqueando e não
resolve starvation.

### `SynchronizationContext` e `ConfigureAwait(false)`

Por padrão, `await` tenta retomar no contexto capturado quando existe. Isso é
necessário quando uma continuação atualiza controles de UI que exigem sua thread.
Bibliotecas que não dependem desse contexto podem usar `ConfigureAwait(false)`
para não solicitar a retomada nele.

ASP.NET Core não instala um `SynchronizationContext` por requisição por padrão.
Consequentemente, `ConfigureAwait(false)` normalmente não altera onde uma
continuação da aplicação web executa e não deve ser vendido como otimização
universal. Bibliotecas reutilizáveis ainda podem adotá-lo por não conhecerem o
ambiente consumidor. Ele tampouco torna estado compartilhado thread-safe nem
garante uma thread diferente após o `await`.

## Structured concurrency como disciplina

Structured concurrency é a disciplina de manter o ciclo de vida do trabalho
concorrente dentro de um escopo léxico: o método que inicia operações filhas
também as aguarda, propaga cancelamento e observa suas falhas antes de retornar.
Isso torna propriedade, encerramento e erro visíveis na árvore de chamadas.

`Task.WhenAll`, `await using`, tokens vinculados, `Parallel.ForEachAsync` e o
encerramento explícito de producers e consumers permitem aplicar parte dessa
disciplina. As APIs gerais de `Task`, porém, também permitem guardar tasks em
estado global, iniciar trabalho sem proprietário ou retornar antes das filhas.
Elas não impõem um task group léxico que cancele irmãs e aguarde todas ao sair.

Portanto, trate structured concurrency como uma propriedade do desenho, não
como garantia automática de `async`/`await`: cada task iniciada deve ter um
proprietário, e todo caminho de sucesso, falha ou cancelamento deve chegar a um
`await` de encerramento. Trabalho que precisa sobreviver à requisição pertence a
um serviço hospedado ou fila com ciclo de vida próprio, e não a fire-and-forget.

## `WhenAll` não é um limitador

O padrão abaixo enumera a fonte e inicia uma chamada para cada item antes de
aguardar o conjunto:

```csharp
await Task.WhenAll(ids.Select(id => cliente.ObterAsync(id, cancellationToken)));
```

Com milhares de itens, isso pode manter milhares de tasks, sockets, buffers e
respostas na memória; esgotar o pool de conexões; ultrapassar cotas; aumentar
timeouts e retries; e deslocar a fila para uma dependência menos observável. A
latência individual pode até piorar quando todas as operações disputam o mesmo
recurso.

`WhenAll` é adequado para um conjunto conhecido e pequeno de operações
independentes. Para entradas grandes ou variáveis, derive o limite da capacidade
da dependência e valide-o com métricas de latência, erros, filas, conexões e
throughput.

## Limitação de concorrência

### `SemaphoreSlim`

`SemaphoreSlim` representa um número de permissões no processo. `WaitAsync`
adquire uma; `Release` deve devolvê-la em `finally`. Ele é útil para adaptar uma
API existente, mas não armazena itens, não distribui trabalho entre processos,
não implementa cota temporal e não oferece, por si só, uma política de fila.

```csharp
using var semaforo = new SemaphoreSlim(limite, limite);

var tarefas = itens.Select(async item =>
{
    await semaforo.WaitAsync(cancellationToken);
    try
    {
        await ProcessarAsync(item, cancellationToken);
    }
    finally
    {
        semaforo.Release();
    }
});

await Task.WhenAll(tarefas);
```

Esse padrão limita a região crítica, embora ainda crie uma task por item. Para
fluxos extensos ou contínuos, uma fila limitada evita representar todo o backlog
como tasks pendentes.

### `Parallel.ForEachAsync`

`Parallel.ForEachAsync` integra iteração, cancelamento e grau máximo de
paralelismo. Sem `ParallelOptions`, a API limita a execução a
`Environment.ProcessorCount`; isso é um ponto inicial voltado a CPU, não uma
medida da capacidade de um banco ou endpoint. Para I/O, configure
`MaxDegreeOfParallelism` com base na dependência.

```csharp
await Parallel.ForEachAsync(
    itens,
    new ParallelOptions
    {
        MaxDegreeOfParallelism = limite,
        CancellationToken = cancellationToken
    },
    async (item, token) => await ProcessarAsync(item, token));
```

Use-o para um lote enumerável quando a política é apenas um grau máximo. Quando
for necessário buffer limitado, múltiplos estágios, produtores independentes ou
política para excesso, `Channel<T>` expressa melhor o pipeline.

## `Channel<T>` e backpressure

Um channel separa produtores de consumidores sem exigir uma task por item. Um
channel limitado possui capacidade máxima. Com `BoundedChannelFullMode.Wait`,
`WriteAsync` permanece pendente quando o buffer está cheio: o produtor sente a
pressão dos consumidores e desacelera.

```csharp
var canal = Channel.CreateBounded<Mensagem>(
    new BoundedChannelOptions(capacidade)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleWriter = true
    });

await canal.Writer.WriteAsync(mensagem, cancellationToken);

await foreach (var item in canal.Reader.ReadAllAsync(cancellationToken))
    await consumidor.ProcessarAsync(item, cancellationToken);
```

As opções `DropNewest`, `DropOldest` e `DropWrite` descartam dados conforme a
política escolhida. Só são corretas quando perda é parte explícita do contrato e
é observável por métricas. Um channel ilimitado desacopla velocidades apenas
temporariamente: se o produtor continuar mais rápido, memória passa a ser a
fila.

Ao concluir, o produtor deve chamar `TryComplete`, incluindo a exceção quando
falhar. Consumidores precisam ser aguardados no encerramento. O laboratório usa
vários consumidores e associa um número de sequência a cada entrada, porque
concorrência não preserva automaticamente a ordem de conclusão.

## Rate limiting e admissão

Um limite de concorrência controla quantas operações permanecem em andamento;
rate limiting controla quantas recebem permissão em uma janela ou orçamento.
No .NET, `System.Threading.RateLimiting` oferece limitadores como token bucket,
fixed window, sliding window e concorrência. `AcquireAsync` devolve um
`RateLimitLease`: o chamador deve verificar `IsAcquired`, respeitar cancelamento
e definir se uma admissão recusada espera, retorna erro ou será tentada depois.

A fila interna também precisa ser limitada. Uma fila grande mascara sobrecarga,
aumenta latência e pode entregar trabalho já obsoleto. Em várias réplicas, um
limitador local aplica uma cota por processo; uma cota global exige coordenação
externa ou divisão explícita do orçamento. Rate limiting não substitui
backpressure: a primeira política decide admissão no tempo, enquanto a segunda
faz a produção acompanhar a capacidade real do pipeline.

## Race conditions, thread safety e consistência

Uma race condition ocorre quando o resultado depende da ordem imprevisível de
operações concorrentes. `contador++` é leitura, cálculo e escrita, não uma
transação atômica. Coleções comuns como `List<T>` e `Dictionary<TKey,TValue>` não
aceitam mutações concorrentes arbitrárias.

Thread safety é propriedade de um contrato sob operações concorrentes, não uma
etiqueta absoluta do objeto. Ferramentas incluem imutabilidade, confinamento a
um consumidor, `Interlocked`, locks e coleções concorrentes. Escolha pela
invariante inteira: tornar o incremento atômico não torna atômicas duas
atualizações relacionadas.

Ordem de entrada, início, conclusão e publicação são garantias diferentes. Um
`SemaphoreSlim(4)` limita quatro trabalhos, mas qualquer um pode terminar
primeiro. Quando ordem for requisito, alternativas incluem um único consumidor,
particionamento por chave, números de sequência e reordenação antes da
publicação. Locks locais não oferecem consistência entre processos; persistência
e sistemas distribuídos exigem mecanismos próprios.

## `Task<T>` versus `ValueTask<T>`

`Task<T>` é uma referência a um resultado futuro. Pode ser armazenada, aguardada
mais de uma vez e compartilhada entre consumidores. É o retorno padrão porque o
contrato é simples e o runtime já otimiza vários casos.

`ValueTask<T>` é uma struct que pode conter um resultado já disponível, envolver
uma `Task<T>` ou usar uma fonte especializada como `IValueTaskSource<T>`. Ela
pode evitar uma alocação quando uma operação completa sincronamente com muita
frequência, mas a struct é maior que uma referência e sua cópia também tem
custo. Se o caminho for realmente assíncrono, ela normalmente apenas envolve a
task existente.

O consumidor deve, em geral, aguardar uma `ValueTask<T>` uma única vez e não
usar simultaneamente `await`, `AsTask` e acesso ao awaiter. Implementações
baseadas em fonte reutilizável podem produzir comportamento indefinido quando a
instância é consumida mais de uma vez. Se for preciso compartilhar ou aguardar
repetidamente, converta uma vez com `AsTask` ou exponha `Task<T>` desde a API.

Um cache em caminho crítico, medido e com alta taxa de acertos é candidato:

```csharp
public ValueTask<string> ObterAsync(int id, CancellationToken token) =>
    _cache.TryGetValue(id, out var valor)
        ? ValueTask.FromResult(valor)
        : new ValueTask<string>(_origem.ObterAsync(id, token));
```

Uma chamada que sempre faz rede é um cenário inadequado: não há conclusão
síncrona frequente a aproveitar e o contrato mais restritivo se espalha pelos
consumidores. `ValueTask<T>` também não deve substituir `Task<T>` por intuição;
meça alocações e throughput no fluxo real antes de mudar uma API pública.

## Iteradores e streaming sob demanda

Um método com `yield return` produz um `IEnumerable<T>` cujo corpo executa a
cada enumeração e avança somente quando `MoveNext` é chamado. Reenumerar repete
leitura, cálculo e efeitos colaterais. Esses contratos e a decisão de
materializar são detalhados no [C03](c03-colecoes-linq-execucao-diferida.md).

`IAsyncEnumerable<T>` leva a produção sob demanda a fontes nas quais obter o
próximo elemento pode esperar I/O. `await foreach` aguarda cada `MoveNextAsync` e
descarta o enumerador ao sair. Isso reduz tempo até o primeiro item e evita
materializar todo o conjunto, mas mantém recursos pelo tempo da enumeração e não
impõe buffer ou paralelismo automaticamente.

```csharp
static async IAsyncEnumerable<int> GerarAsync(
    int quantidade,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    for (var i = 0; i < quantidade; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return i;
    }
}

await foreach (var item in GerarAsync(100, cancellationToken))
    await ConsumirAsync(item, cancellationToken);
```

`[EnumeratorCancellation]` permite combinar o token do argumento com aquele
passado por `WithCancellation`. O produtor deve encaminhá-lo ao I/O entre
elementos. Abandonar a enumeração não desfaz efeitos já realizados; o `finally`
do iterador e `DisposeAsync` são os locais para liberar recursos.

Evite async stream quando o consumidor precisa do conjunto inteiro de qualquer
forma, quando a paginação exige consistência global impossível durante o fluxo
ou quando uma coleção pequena é um contrato mais simples. Para múltiplos
produtores e consumidores com buffer, prefira `Channel<T>`; um
`IAsyncEnumerable<T>` modela pull, enquanto channel também modela uma fila push.

## Exemplos compiláveis

O laboratório em `exemplos/c05-assincronismo-concorrencia/` contém:

- **Exemplo A:** `Task.WhenAll` sem limite versus adaptação com `SemaphoreSlim`;
- **Exemplo B:** consumo de API externa com `HttpClient` e
  `CancellationToken` propagado;
- **Exemplo C:** pipeline produtor/consumidor com `Channel<T>` limitado;
- **Exemplo D:** relançamento com `throw;` e teste da origem da pilha;
- **Exemplo E:** streaming cancelável com `IAsyncEnumerable<T>`;
- **Exemplo F:** cache com conclusão síncrona apropriada para `ValueTask<T>` e
  operação remota que permanece `Task<T>`.

O projeto usa o SDK .NET 10 fixado no `global.json`, não requer serviços externos
e pode ser verificado com:

```bash
dotnet test exemplos/c05-assincronismo-concorrencia/C05.AssincronismoConcorrencia.Tests.csproj
```

Os testes coordenam concorrência com sinais e capacidade de fila, sem depender
de atrasos arbitrários. Eles comprovam contratos funcionais, não throughput ou
superioridade de performance.

### Compatibilidade das APIs

O laboratório usa .NET 10. Para projetos em versões anteriores, os pontos de
corte relevantes são:

- `IAsyncEnumerable<T>` e as interfaces de async streams fazem parte do .NET
  Standard 2.1 e do .NET Core 3.0, com suporte de linguagem a partir do C# 8;
- `System.Threading.Channels` integra o shared framework desde o .NET Core 3.0 e
  também está disponível como pacote para outros targets compatíveis;
- `Parallel.ForEachAsync` está disponível a partir do .NET 6;
- as APIs `System.Threading.RateLimiting` e o middleware correspondente do
  ASP.NET Core chegaram ao conjunto de APIs do .NET 7 e também podem ser usados
  pelo pacote compatível com o target declarado.

Confirme o target e a versão do pacote no projeto consumidor; disponibilidade
de uma API não garante que uma biblioteca ou framework ao redor preserve a
mesma semântica operacional.

## Dicas de engenharia

- Propague `CancellationToken` da fronteira até a operação bloqueante e teste o
  cancelamento antes e durante o fluxo.
- Observe tarefas criadas; operações fire-and-forget precisam de proprietário,
  lifetime, política de erro e encerramento explícitos.
- Dimensione concorrência por dependência, não apenas por CPU; registre fila,
  utilização do pool, throttling, timeout, erros e percentis de latência.
- Prefira dados imutáveis ou propriedade exclusiva por estágio. Estado
  compartilhado exige uma invariante documentada e sincronização correspondente.
- Faça o limite configurável dentro de uma faixa segura e teste sob falhas e
  encerramento, não apenas no caminho feliz.
- Preserve correlação, operação, dependência e status de cancelamento na
  telemetria; registre uma falha na fronteira responsável pela resposta.

## Armadilhas comuns

- Usar `Task.Run` para cada chamada de I/O síncrona e chamar isso de I/O
  assíncrono escalável.
- Criar milhares de operações com `WhenAll` sem considerar conexões, memória,
  cotas e retries.
- Esquecer `Release` fora de `finally`, vazando permissões do semáforo.
- Usar channel ilimitado e apenas transferir o backlog para a memória.
- Confundir cancelamento solicitado com rollback de efeitos já confirmados.
- Bloquear com `.Result` ou `.Wait()` em uma cadeia assíncrona.
- Usar `async void` fora do contrato de eventos.
- Supor que concorrência preserva a ordem de conclusão.
- Compartilhar `DbContext`, coleções ou objetos mutáveis que não prometem thread
  safety entre operações concorrentes.
- Trocar todo `Task<T>` por `ValueTask<T>` sem medição e impor restrições inúteis
  a todos os consumidores.
- Capturar todas as exceções, registrar em cada camada e retornar sucesso ou
  `null`, ocultando uma operação incompleta.

## Trade-offs

Use assincronismo para esperas de I/O quando a aplicação precisa manter
responsividade ou sustentar muitas operações pendentes. Evite introduzi-lo em
um caminho estritamente síncrono e simples sem benefício operacional.

Limites protegem dependências e tornam consumo previsível, mas podem aumentar o
tempo total do lote e criar fila local. Channels desacoplam estágios e expressam
backpressure, ao custo de lifecycle e tratamento de conclusão mais elaborados.
Paralelismo pode reduzir trabalho CPU-bound divisível, mas também aumenta
contenção e competição com outras requisições.

## Quando utilizar e quando evitar

Use `WhenAll` para poucos trabalhos independentes e conhecidos. Use
`SemaphoreSlim` ou `Parallel.ForEachAsync` para lotes com grau máximo. Use
channel limitado quando backlog, desacoplamento de estágios e backpressure fazem
parte do problema. Use rate limiter quando a política é temporal, por cliente ou
por cota; combine mecanismos quando as restrições forem diferentes.

Mais concorrência pode reduzir o tempo de um lote até o ponto de saturação.
Depois disso, aumenta filas, contenção e falhas. O valor adequado é uma decisão
operacional revisável, não uma constante universal.

## Relação com outros capítulos

- **Pré-requisitos:** [C02](c02-nulabilidade-falhas-excecoes.md) define ausência,
  resultados e fronteiras de exceção; [C03](c03-colecoes-linq-execucao-diferida.md)
  define iteração sob demanda e reenumeração.
- **Runtime:** [C04 — Memória, runtime e engenharia de performance](c04-memoria-runtime-performance.md)
  trata máquinas de estado sob a perspectiva de alocação, medição, JIT e AOT.
- **Aplicações planejadas:** C07 aplicará cancelamento a HTTP e resiliência; C15
  tratará filas externas e entrega; C32 investigará backlog operacional.

## Referências

- [Programação assíncrona com `async` e `await`](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/)
  — documentação de C#, acesso em 18 novembro 2026.
- [Modelo assíncrono baseado em tarefas](https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)
  — documentação .NET, acesso em 18 novembro 2026.
- [`Task.WhenAll`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.whenall?view=net-10.0)
  — API .NET 10, acesso em 18 novembro 2026.
- [`Task.WhenAny`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.whenany?view=net-10.0)
  — API .NET 10, acesso em 18 novembro 2026.
- [Cancelamento em threads gerenciadas](https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads)
  — documentação .NET, acesso em 18 novembro 2026.
- [`ConfigureAwait` FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
  — equipe .NET, acesso em 18 novembro 2026.
- [Regra CA2007 e comportamento no ASP.NET Core](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2007)
  — análise de código .NET, acesso em 18 novembro 2026.
- [`SemaphoreSlim.WaitAsync`](https://learn.microsoft.com/dotnet/api/system.threading.semaphoreslim.waitasync?view=net-10.0)
  — API .NET 10, acesso em 18 novembro 2026.
- [`Parallel.ForEachAsync`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.parallel.foreachasync?view=net-10.0)
  — API .NET 10, acesso em 18 novembro 2026.
- [Channels no .NET](https://learn.microsoft.com/dotnet/core/extensions/channels)
  — documentação .NET, acesso em 18 novembro 2026.
- [Rate limiting em um cliente HTTP](https://learn.microsoft.com/dotnet/core/extensions/http-ratelimiter)
  — documentação .NET, acesso em 18 novembro 2026.
- [`ValueTask<T>`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1?view=net-10.0)
  e [entendendo quando usá-la](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)
  — API e equipe .NET, acesso em 18 novembro 2026.
- [Gerar e consumir streams assíncronos](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream)
  — documentação de C#, acesso em 18 novembro 2026.
