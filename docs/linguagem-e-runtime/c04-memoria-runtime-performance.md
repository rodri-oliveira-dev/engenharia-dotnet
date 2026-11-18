# C04 — Memória, runtime e engenharia de performance

> **Nível:** Avançado
>
> **Objetivo:** investigar pressão de memória e gargalos, aplicar buffers e
> spans quando adequados e validar otimizações por medição reproduzível.
>
> **Pré-requisitos:** [C01 — Tipos, identidade e modelagem em C#](c01-tipos-identidade-modelagem.md)
> e [C03 — Coleções, LINQ e execução diferida](c03-colecoes-linq-execucao-diferida.md).
>
> **Última revisão:** 2026-11-18

Otimização útil começa por uma restrição observada: latência fora do objetivo,
CPU saturada, taxa de alocação elevada, pausas do Garbage Collector (GC) ou
memória retida. Reescrever código com pooling ou `Span<T>` sem esse diagnóstico
pode aumentar o risco de corrupção e o custo de manutenção sem alterar a métrica
que importa.

Este capítulo apresenta o GC geracional, retenção, Large Object Heap (LOH),
buffers, `Span<T>`, `Memory<T>`, profiling e microbenchmarks. Boxing e semântica
de valor já são explicados no [C01](c01-tipos-identidade-modelagem.md); execução
diferida e materialização pertencem ao
[C03](c03-colecoes-linq-execucao-diferida.md).

## O GC administra memória, não o ciclo de vida da aplicação

O heap gerenciado permite alocar objetos com baixo custo inicial. O GC encontra
objetos alcançáveis a partir de raízes, como variáveis locais ativas, campos
estáticos e referências mantidas pelo runtime. Objetos que deixaram de ser
alcançáveis podem ter seu espaço recuperado em uma coleta futura.

Isso não equivale a descarte determinístico. Arquivos, sockets e outros recursos
devem seguir seus contratos de `IDisposable` ou `IAsyncDisposable`. Também não
impede vazamentos lógicos: um cache, evento, timer, closure ou coleção estática
que ainda referencia um objeto faz com que ele permaneça vivo, mesmo que o
programa não volte a usá-lo.

Uma coleta envolve localizar objetos vivos e, conforme a região, mover e
compactar sobreviventes. O custo depende do volume alocado, do que sobrevive e
do tamanho do conjunto alcançável; somente observar o tamanho final do heap não
explica a causa.

## Gerações e pressão de alocação

O heap gerenciado organiza objetos por gerações com base na hipótese de que a
maioria morre jovem:

- **geração 0:** recebe a maioria dos objetos pequenos e é coletada com maior
  frequência;
- **geração 1:** funciona como transição para objetos que sobreviveram;
- **geração 2:** contém objetos de vida longa; uma coleta dessa geração também
  examina as gerações mais jovens e é chamada de coleta completa;
- **LOH:** recebe objetos grandes e é coletado junto com a geração 2.

Objetos temporários não são defeitos por definição. Alocação gerenciada é
otimizada, e muitos temporários morrem baratos na geração 0. O problema aparece
quando a taxa de alocação aciona coletas com frequência, quando muitos objetos
sobrevivem e são promovidos, ou quando as pausas e CPU do GC afetam o objetivo
do serviço.

Exemplos de pressão evitável em caminhos intensos incluem `Split` criando várias
strings, projeções materializadas e descartadas, concatenação repetida e arrays
grandes por operação. Antes de mudar o código, atribua as alocações a stacks e
tipos com um profiler; uma taxa global não indica qual linha é responsável.

### Large Object Heap

Por padrão, objetos com pelo menos 85.000 bytes são considerados grandes. Arrays
atingem esse limite com frequência. O valor é um detalhe documentado e pode ser
configurado em runtimes atuais; o código não deve depender dele como fronteira
de correção.

O conteúdo de uma nova alocação precisa ser limpo. Além disso, o LOH normalmente
não é compactado em toda coleta porque mover blocos grandes custa caro. Alocar e
descartar grandes buffers repetidamente pode aumentar coletas de geração 2 e
fragmentação. A resposta não é chamar `GC.Collect`: o GC ajusta seus limites, e
coletas forçadas em código comum podem piorar throughput e latência.

Primeiro reduza o tamanho ou a frequência dos buffers, processe em blocos quando
o protocolo permitir e meça novamente. Reutilização e pooling entram somente
quando o perfil mostra alocações recorrentes relevantes e há propriedade clara.

## Retenção é diferente de alocação

Uma aplicação pode alocar pouco e ainda crescer porque mantém referências. Ao
investigar retenção, compare snapshots de heap depois de um período estável e
analise caminhos até as raízes. Alguns padrões frequentes são:

- cache sem limite, expiração ou política de tamanho;
- inscrição em evento sem cancelamento;
- tarefas, closures ou timers capturando grafos maiores do que o necessário;
- coleção de requisições anteriores mantida por telemetria ou diagnóstico;
- buffer grande guardado por um objeto pequeno de vida longa.

Trocar `List<T>` por array ou aplicar `Span<T>` não remove uma raiz. O reparo é
encerrar a propriedade ou limitar a retenção.

## `Span<T>` e regiões contíguas

`Span<T>` é um `ref struct` que representa uma região contígua de memória. Ele
pode referenciar um array, memória obtida com `stackalloc` ou memória não
gerenciada. `ReadOnlySpan<T>` oferece a mesma visão sem permitir escrita por
meio dela; uma `string` também pode ser exposta como `ReadOnlySpan<char>`.

Um span contém referência e comprimento. `Slice` e os operadores de intervalo
criam outra visão para o mesmo armazenamento: não copiam os elementos.

```csharp
ReadOnlySpan<char> linha = "42|10.50|true";
var separador = linha.IndexOf('|');
var id = linha[..separador];

var sensorId = int.Parse(id, CultureInfo.InvariantCulture);
```

Isso é útil em parsing, protocolos e processamento de buffers quando a versão
convencional cria substrings ou arrays temporários. A ausência de cópia não
torna o algoritmo automaticamente mais rápido: busca, validação e conversão
continuam existindo, e APIs baseadas em span podem aumentar a complexidade.

### Lifetime e segurança de referência

Como `Span<T>` e `ReadOnlySpan<T>` são `ref struct`, o compilador impede que uma
referência escape do armazenamento que ela aponta. Entre outras restrições, um
span não pode ser campo de uma classe, sofrer boxing ou ser capturado por lambda.

A regra para métodos assíncronos depende da versão da linguagem:

- antes do C# 13, uma variável local de `ref struct` não podia ser declarada em
  um método `async`;
- desde C# 13, ela pode existir em um trecho que não atravesse `await`. O
  compilador rejeita usos cujo lifetime cruza um ponto de suspensão.

Portanto, não é correto dizer que `Span<T>` é proibido em qualquer método
`async`. O padrão abaixo é válido em C# moderno porque o span deixa de estar vivo
antes do `await`:

```csharp
static async Task<byte> LerAsync(ReadOnlyMemory<byte> dados)
{
    byte cabecalho;
    {
        ReadOnlySpan<byte> trechoSincrono = dados.Span;
        cabecalho = trechoSincrono[0];
    }

    await Task.Yield();
    return cabecalho;
}
```

O laboratório compila com o SDK .NET 10 e a versão de C# correspondente. Uma
biblioteca que também suporta compiladores anteriores precisa manter código
compatível com a menor versão declarada.

## `Memory<T>` atravessa fronteiras assíncronas

`Memory<T>` e `ReadOnlyMemory<T>` também representam regiões contíguas, mas não
são `ref struct` e podem ser armazenados no heap e preservados por uma operação
assíncrona. Use `ReadOnlyMemory<T>` quando o consumidor não deve escrever.

```csharp
static async Task<int> SomarAsync(
    ReadOnlyMemory<int> valores,
    CancellationToken cancellationToken)
{
    await Task.Delay(1, cancellationToken);
    return Somar(valores.Span); // span existe somente no trecho síncrono
}
```

`Memory<T>` resolve o transporte, não a propriedade. Se o buffer veio de um pool
ou pertence a outro componente, ele precisa permanecer válido até a `Task`
terminar. Escrever no mesmo buffer concorrentemente também exige coordenação.

Como regra de API, prefira `Span<T>`/`ReadOnlySpan<T>` para consumo síncrono e
`Memory<T>`/`ReadOnlyMemory<T>` quando a região precisa ser preservada. Para
dados pequenos ou código de negócio sem pressão medida, `string`, arrays e
coleções convencionais costumam comunicar melhor a intenção.

## Exemplo D: parsing convencional versus `ReadOnlySpan<char>`

O parser convencional é direto:

```csharp
var partes = linha.Split('|');
return new Medicao(
    int.Parse(partes[0], CultureInfo.InvariantCulture),
    decimal.Parse(partes[1], CultureInfo.InvariantCulture),
    bool.Parse(partes[2]));
```

`Split` cria o array de partes e strings para os campos. A alternativa localiza
os separadores e passa slices para overloads de `Parse` que aceitam spans:

```csharp
ReadOnlySpan<char> dados = linha;
var primeiro = dados.IndexOf('|');
var ultimo = dados.LastIndexOf('|');

var id = dados[..primeiro];
var valor = dados[(primeiro + 1)..ultimo];
var valida = dados[(ultimo + 1)..];

return new Medicao(
    int.Parse(id, CultureInfo.InvariantCulture),
    decimal.Parse(valor, CultureInfo.InvariantCulture),
    bool.Parse(valida));
```

O problema abordado são substrings e array temporários em um parser executado
com alta frequência. Os slices referenciam a linha original e reduzem essas
alocações intermediárias. Em contrapartida, o código precisa validar posições e
fica mais detalhado; o ganho depende do volume, do formato e do custo restante.

A alternativa se justifica após um perfil apontar esse parser como fonte
relevante de alocações ou CPU. Para configuração lida uma vez, `Split` pode ser a
opção mais legível. Os testes verificam equivalência funcional; somente o
benchmark separado compara tempo e alocação.

## Exemplo E: reduzir alocações e reutilizar buffers

A primeira estratégia é evitar criar o objeto: consumir slices, escrever em um
buffer fornecido pelo chamador, usar overloads `TryFormat`/`TryParse` e não
materializar consultas antes da hora. Isso preserva propriedade simples.

Se um caminho medido cria e descarta arrays relevantes, `ArrayPool<T>.Shared`
pode amortizar alocações:

```csharp
var buffer = ArrayPool<byte>.Shared.Rent(tamanhoMinimo);
try
{
    var regiao = buffer.AsSpan(0, tamanhoMinimo);
    Preencher(regiao);
    Consumir(regiao);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
}
```

`Rent` garante um array com **pelo menos** o tamanho pedido; use somente a região
lógica. O conteúdo pode conter dados anteriores. Depois de `Return`, o chamador
perde a propriedade e não pode continuar usando o array nem devolvê-lo outra
vez. `clearArray: true` reduz risco de exposição quando há dados sensíveis ou
referências, mas limpar também consome CPU. O pool pode reter memória e introduz
caminhos de falha mais perigosos do que uma alocação comum.

Pooling é apropriado para buffers recorrentes, com lifetime curto e propriedade
inequívoca, quando a medição mostra redução relevante na pressão do GC. Evite-o
para objetos baratos, uso esporádico, buffers mantidos por tempo imprevisível ou
quando a equipe não consegue garantir retorno em `finally`. Muitas APIs de I/O
já fazem pooling internamente; uma camada adicional pode não ajudar.

O método de pooling do laboratório é deliberadamente didático: copiar uma única
entrada apenas para somá-la é pior do que processar o span diretamente. Ele
demonstra o contrato de aluguel e devolução, não uma recomendação para esse
algoritmo.

## JIT, Native AOT e trimming

Projetos .NET normalmente compilam C# para Common Intermediate Language (CIL) e
metadados. No modelo convencional, o runtime carrega os assemblies e o
compilador Just-in-Time (JIT) gera código de máquina quando um método precisa
executar. Isso permite gerar código para a arquitetura e o ambiente atuais. Com
compilação em camadas, o runtime pode produzir código inicial rapidamente e
recompilar métodos quentes com otimizações adicionais.

Essa descrição é um modelo operacional, não uma garantia de que cada método
será compilado isoladamente ou apenas uma vez. Inlining, generics, ReadyToRun,
profile-guided optimization e versões do runtime alteram o código produzido.
Não baseie correção em uma decisão do JIT; use disassembly e métricas quando a
forma do código nativo for relevante para performance.

O custo do JIT aparece durante a execução, sobretudo no startup e na primeira
passagem por caminhos ainda não compilados. Em aplicações de longa duração, a
otimização adaptativa pode beneficiar throughput. Em funções curtas, ferramentas
de linha de comando ou cenários com muitas instâncias efêmeras, startup e
memória por processo podem ter peso maior.

### Native AOT

Native Ahead-of-Time (AOT) compila IL para código nativo durante a publicação. O
artefato é self-contained e específico para um Runtime Identifier (RID), como
`linux-x64`; ele não usa JIT durante a execução. Isso pode reduzir tempo de
inicialização e memória em workloads compatíveis, mas aumenta o trabalho de
build, exige publicação por plataforma e perde parte da adaptação dinâmica que
o JIT pode fazer durante uma execução longa.

O modelo de publicação Native AOT suportado pelo SDK foi introduzido no .NET 7,
inicialmente concentrado em aplicações console; o suporte do ASP.NET Core foi
adicionado no .NET 8 e continua sujeito ao subconjunto compatível do framework.
O laboratório fixa .NET 10, portanto essas versões mínimas servem para avaliar
backports, não para alterar seu target atual.

Native AOT não é uma opção superior por definição. Uma API ASP.NET Core com
startup crítico e conjunto de recursos compatível pode se beneficiar. Uma
aplicação extensível por plugins, dependente de geração dinâmica de código ou de
bibliotecas não preparadas pode exigir reengenharia ou permanecer no modelo JIT.
Compare o artefato publicado, startup, conjunto de trabalho, throughput,
diagnóstico e custo do pipeline no ambiente de destino.

```bash
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

### Reflection, geração de código e compatibilidade

Native AOT não oferece carregamento dinâmico irrestrito nem geração de código
com `System.Reflection.Emit`. Reflection conhecida pelo analisador pode
funcionar, mas descoberta aberta de tipos por nome, serializers baseados em
reflection, proxies dinâmicos e sistemas de plugins são fontes comuns de
incompatibilidade. Source generators e metadados explícitos tornam dependências
visíveis no build, mas também alteram desenho e manutenção.

Compatibilidade precisa ser avaliada no grafo completo de dependências. Marcar o
projeto principal como compatível não corrige uma biblioteca que depende de
código dinâmico. Habilite os analisadores, trate os warnings e teste o binário
publicado; executar apenas o build JIT não valida o modelo AOT.

### Trimming não é apenas remoção de arquivos

O trimmer analisa código alcançável e remove partes que parecem não usadas para
reduzir a publicação. Reflection e carregamento dinâmico podem ocultar essa
alcançabilidade. Um build que ignora warnings pode publicar com sucesso e falhar
somente quando um caminho removido for ativado em produção.

Native AOT implica trimming, mas trimming também pode ser usado sem Native AOT.
Anotações como `DynamicallyAccessedMembers`, descritores e supressões devem
expressar uma prova específica; suprimir todos os warnings apenas transfere o
risco para runtime. Prefira APIs e bibliotecas declaradamente compatíveis e
exercite serialização, configuração, DI e caminhos pouco frequentes no artefato
final.

### Critérios de deployment

Escolha o modelo a partir das restrições:

- **JIT/framework-dependent:** artefato menor e portável entre ambientes com o
  runtime instalado, atualização de runtime separada e maior compatibilidade
  dinâmica;
- **self-contained com JIT:** runtime incluído e deployment mais isolado, ao
  custo de artefato maior e publicação por plataforma;
- **Native AOT:** startup previsível e ausência de JIT, com build mais caro,
  restrições dinâmicas, trimming obrigatório e diagnóstico parcialmente
  diferente.

Não extrapole números de uma aplicação para outra. Publique cada candidato com
a mesma configuração, valide funcionalidade e meça as métricas operacionais que
motivaram a decisão.

## Performance orientada por medição

Uma investigação reproduzível segue uma sequência:

1. Defina a métrica e o objetivo: percentil de latência, throughput, CPU,
   memória retida, bytes alocados ou tempo em GC.
2. Reproduza carga e dados representativos no mesmo runtime, arquitetura e modo
   de build usados no destino.
3. Use métricas para localizar a classe do problema e um profiler para atribuir
   custo a métodos, stacks e tipos.
4. Registre a baseline, altere uma hipótese por vez e repita nas mesmas condições.
5. Verifique correção, regressões, variabilidade e impacto no sistema inteiro.
6. Mantenha a otimização somente se o benefício justificar complexidade e risco.

`dotnet-counters` é útil para triagem de CPU, heap, taxa de alocação e coletas.
`dotnet-trace` captura eventos e amostras para análise posterior. Dumps e
snapshots ajudam a investigar retenção. Ferramentas comerciais ou específicas
da plataforma também podem ser apropriadas; o ponto é atribuir custo, não apenas
observar que o processo usa memória.

JIT tiering, warmup, Debug versus Release, arquitetura, configuração de GC e
otimizações Ahead-of-Time (AOT) alteram resultados. Registre essas condições e
compare o modo que representa a produção.

## BenchmarkDotNet e limites do microbenchmark

BenchmarkDotNet automatiza aquecimento, várias execuções e estatísticas. O
projeto `C04.MemoriaPerformance.Benchmarks.csproj` compara os dois parsers com
uma baseline e `[MemoryDiagnoser]`:

```bash
dotnet run -c Release \
  --project exemplos/c04-memoria-performance/C04.MemoriaPerformance.Benchmarks.csproj
```

O repositório não publica números desse benchmark. Eles dependem da máquina e
da execução; o exemplo é uma infraestrutura reproduzível para o leitor gerar e
registrar seus próprios resultados.

Um microbenchmark responde a uma pergunta estreita. Ele pode ocultar contenção,
I/O, cache, distribuição de entradas e efeitos do GC no serviço completo. Evite
trabalho que o compilador consiga eliminar, valide os resultados retornados,
use dados representativos, compare alocações além da média de tempo e não
generalize pequenas diferenças dentro do ruído. Depois, valide a mudança com
profiling ou carga no fluxo integrado.

## Armadilhas comuns

- Otimizar por intuição antes de definir qual métrica está fora do objetivo.
- Tratar toda alocação como defeito e trocar código claro por estado reutilizado.
- Concluir que memória crescente é GC lento sem examinar raízes de retenção.
- Chamar `GC.Collect` em código de aplicação para “liberar memória”.
- Criar `stackalloc` de tamanho controlado por entrada sem limite; stacks são
  finitas e buffers grandes devem usar outra estratégia.
- Retornar um span para memória cujo lifetime terminou ou tentar armazená-lo em
  objeto; o compilador bloqueia muitos, mas não todos os riscos em código unsafe.
- Manter `Span<T>` vivo através de `await`, ou afirmar que nenhuma utilização em
  método `async` é possível em C# 13 ou posterior.
- Usar buffer alugado depois de devolvê-lo, devolver duas vezes ou assumir que
  ele veio zerado e com tamanho exato.
- Manter buffers alugados indefinidamente, reduzindo a disponibilidade do pool.
- Publicar um resultado de microbenchmark sem ambiente, baseline, dispersão e
  versão do runtime.
- Adotar Native AOT apenas por expectativa genérica de performance, sem validar
  compatibilidade, warnings, startup, memória e throughput do artefato final.
- Suprimir warnings de trimming sem demonstrar como membros acessados
  dinamicamente permanecem alcançáveis.

## Quando utilizar e quando evitar

Use spans em APIs síncronas que operam sobre regiões contíguas e em parsers ou
pipelines nos quais cópias e alocações aparecem no perfil. Use `Memory<T>` quando
a região precisa atravessar uma suspensão ou ser guardada por um componente com
lifetime explícito. Considere pooling somente para pressão recorrente e medida.

Evite essas abstrações em código de baixa frequência, fluxos dominados por I/O,
regras de negócio nas quais strings e coleções são mais claras ou equipes sem um
contrato seguro de propriedade. Uma melhoria pequena de microbenchmark não
compensa automaticamente uma API mais difícil de manter.

## Como verificar

O laboratório em `exemplos/c04-memoria-performance/` testa equivalência entre os
parsers, passagem de `ReadOnlyMemory<T>` por `await`, uso de span em trecho que
não cruza suspensão e o contrato básico do exemplo com `ArrayPool<T>`.

```bash
dotnet test exemplos/c04-memoria-performance/C04.MemoriaPerformance.Tests.csproj
```

Os testes demonstram correção, não performance. Execute o benchmark em Release
fora de outros processos intensivos e registre CPU, sistema operacional, runtime,
configuração, commit e saída completa ao usar os dados numa decisão.

## Relação com outros capítulos

- **Pré-requisitos:** [C01 — Tipos, identidade e modelagem em C#](c01-tipos-identidade-modelagem.md)
  cobre tipos por valor, cópia e boxing; [C03 — Coleções, LINQ e execução diferida](c03-colecoes-linq-execucao-diferida.md)
  cobre materialização e complexidade.
- **Aplicação:** [C05 — Assincronicidade e concorrência controlada](c05-assincronicidade-concorrencia-controlada.md)
  aprofunda lifetime e buffers em código assíncrono.
- **Aplicações planejadas:** C25 aplica métricas a testes de carga; C27 trata
  telemetria.

## Referências

- [Fundamentos de garbage collection](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals)
  — documentação .NET, acesso em 18 novembro 2026.
- [Large Object Heap](https://learn.microsoft.com/dotnet/standard/garbage-collection/large-object-heap)
  — documentação .NET, acesso em 18 novembro 2026.
- [Garbage collection e performance](https://learn.microsoft.com/dotnet/standard/garbage-collection/performance)
  — documentação .NET, acesso em 18 novembro 2026.
- [Tipos relacionados a memória e span](https://learn.microsoft.com/dotnet/standard/memory-and-spans/)
  — documentação .NET, acesso em 18 novembro 2026.
- [Diretrizes de uso de `Memory<T>` e `Span<T>`](https://learn.microsoft.com/dotnet/standard/memory-and-spans/memory-t-usage-guidelines)
  — documentação .NET, acesso em 18 novembro 2026.
- [Tipos `ref struct`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/ref-struct)
  — referência de C#, incluindo mudanças do C# 13, acesso em 18 novembro 2026.
- [Novidades do C# 13](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-13#ref-and-unsafe-in-iterators-and-async-methods)
  — referência da linguagem, acesso em 18 novembro 2026.
- [`ArrayPool<T>.Rent`](https://learn.microsoft.com/dotnet/api/system.buffers.arraypool-1.rent?view=net-10.0)
  e [`ArrayPool<T>.Return`](https://learn.microsoft.com/dotnet/api/system.buffers.arraypool-1.return?view=net-10.0)
  — APIs .NET 10, acesso em 18 novembro 2026.
- [Visão geral das ferramentas de diagnóstico .NET](https://learn.microsoft.com/dotnet/core/diagnostics/)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [Compilação gerenciada](https://learn.microsoft.com/dotnet/standard/managed-execution-process)
  e [compilação em camadas](https://learn.microsoft.com/dotnet/core/runtime-config/compilation)
  — modelo JIT e configurações do runtime, acesso em 18 novembro 2026.
- [Deployment com Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
  — benefícios, requisitos e limitações, acesso em 18 novembro 2026.
- [Preparar bibliotecas para trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
  e [incompatibilidades conhecidas](https://learn.microsoft.com/dotnet/core/deploying/trimming/incompatibilities)
  — documentação .NET, acesso em 18 novembro 2026.
- [Suporte do ASP.NET Core a Native AOT](https://learn.microsoft.com/aspnet/core/fundamentals/native-aot?view=aspnetcore-10.0)
  — matriz de recursos do ASP.NET Core 10, acesso em 18 novembro 2026.
- [`dotnet-counters`](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-counters)
  e [`dotnet-trace`](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace)
  — ferramentas .NET, acesso em 18 novembro 2026.
- [Getting started do BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet/blob/master/docs/articles/guides/getting-started.md)
  — documentação oficial do projeto, acesso em 18 novembro 2026.
