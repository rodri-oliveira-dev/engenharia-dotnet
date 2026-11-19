# C03 — Coleções, LINQ e execução diferida

> **Nível:** Fundamental
>
> **Objetivo:** escolher estruturas e estratégias de enumeração considerando
> semântica, complexidade, memória e frequência das operações.
>
> **Pré-requisitos:** [C01 — Tipos, identidade e modelagem em C#](c01-tipos-identidade-modelagem.md).
>
> **Última revisão:** 2026-11-18

Uma coleção não é apenas um recipiente. Ela define quais operações são naturais,
quais invariantes precisam ser mantidas e quanto trabalho e memória uma consulta
pode consumir. Uma lista pequena percorrida uma vez pode ser mais simples e mais
adequada do que um índice. A mesma busca linear repetida para milhares de chaves
pode transformar um fluxo aparentemente inocente em trabalho quadrático.

Este capítulo conecta `List<T>`, `HashSet<T>`, `Dictionary<TKey, TValue>`, filas,
pilhas e LINQ a critérios observáveis. O objetivo não é eleger uma coleção
universal, mas reconhecer o padrão de uso. Detalhes de Garbage Collector (GC),
buffers e medição são tratados em
[C04 — Memória, runtime e engenharia de performance](c04-memoria-runtime-performance.md).

## Coleções representam contratos diferentes

Antes de escolher a estrutura, identifique:

- se a ordem de inserção ou o acesso por posição fazem parte do contrato;
- se duplicatas são válidas;
- se existe uma chave única ou uma relação de um para muitos;
- quais operações dominam: anexar, remover, buscar, testar pertinência ou
  consumir nas extremidades;
- quantos elementos e quantas consultas existem no caminho real;
- por quanto tempo o índice será reutilizado;
- se uma visão atualizada da fonte ou um snapshot estável é necessário.

Complexidade assintótica descreve como o trabalho cresce, não o tempo exato.
Constantes, qualidade do hash, localidade de memória, tamanho da entrada e custo
de construir a estrutura continuam relevantes. `O(1)` também não significa
“custo zero”, e `O(n)` pode ser a decisão correta para uma entrada pequena.

## Estruturas fundamentais

### `List<T>`: sequência contígua e acesso por índice

`List<T>` mantém os elementos em um array interno redimensionável. O indexador e
a leitura de `Count` são `O(1)`. Anexar é `O(1)` amortizado: quando a capacidade
acaba, uma inserção precisa alocar outro armazenamento e copiar os elementos,
portanto aquela operação é `O(n)`. Inserir ou remover no meio também desloca
elementos e custa `O(n)`.

Uma busca por valor ou predicado é linear. Ela pode parar cedo, mas no pior caso
examina todos os elementos. Essa característica combina bem com iteração
sequencial, acesso por posição, entradas pequenas e buscas ocasionais.

`Count` é a quantidade lógica; `Capacity` é o espaço disponível antes de outro
redimensionamento. Informar uma capacidade conhecida pode evitar realocações,
mas superestimar reserva memória que talvez não seja usada. `TrimExcess` também
tem custo de cópia e não deve ser chamado automaticamente após cada alteração.

### `HashSet<T>`: unicidade e pertinência

`HashSet<T>` representa um conjunto: cada valor aparece no máximo uma vez de
acordo com o `IEqualityComparer<T>` adotado. `Contains`, adição e remoção têm
custo esperado próximo de `O(1)`, sujeito à distribuição dos hashes e a
redimensionamentos.

Ele é apropriado para remover duplicatas, testar permissão e executar operações
de conjunto. Não oferece acesso por posição e seu armazenamento inclui a
estrutura de hash além dos próprios elementos. Se o código apenas percorrerá
uma sequência uma vez, construir o conjunto pode consumir CPU e memória sem
benefício.

### `Dictionary<TKey, TValue>`: uma chave para um valor

`Dictionary<TKey, TValue>` mantém chaves únicas e associa cada uma a um valor.
`TryGetValue` evita uma consulta de existência seguida por outra consulta para
obter o valor. A busca se aproxima de `O(1)` em condições normais; construir o
dicionário custa `O(n)` e mantém buckets e entradas adicionais.

A igualdade e o hash da chave fazem parte do contrato. Alterar uma chave depois
de inseri-la pode torná-la inalcançável pela busca. O capítulo
[C01](c01-tipos-identidade-modelagem.md) detalha igualdade, mutabilidade e
componentes usados em `GetHashCode`.

Um dicionário não é automaticamente superior a uma lista. Para poucos elementos,
uma única busca ou quando a ordem e duplicatas são essenciais, a lista pode ser
mais clara e evitar o custo de indexação. Um índice se justifica quando a chave é
estável, o conjunto é grande o suficiente e as buscas são repetidas ou o índice
é reutilizado.

### `Queue<T>` e `Stack<T>`: acesso restrito com intenção

`Queue<T>` implementa first in, first out (FIFO): `Enqueue` adiciona ao fim e
`Dequeue` remove do início. `Stack<T>` implementa last in, first out (LIFO):
`Push` e `Pop` trabalham no topo. Remover da extremidade adequada é `O(1)`;
inserções que exigem aumentar o array interno têm custo `O(n)` naquela operação.

Use fila para trabalho pendente em ordem de chegada e pilha para desfazer,
percorrer profundidade ou processar contextos aninhados. Uma `List<T>` pode
simular ambos, mas permite operações que enfraquecem o contrato. Essas coleções
genéricas não fornecem segurança para acesso concorrente por si mesmas.

### Comparação orientativa

| Necessidade dominante | Estrutura inicial | Custo relevante | Custo de memória |
|---|---|---|---|
| ordem e índice | `List<T>` | busca/remoção no meio `O(n)` | array com capacidade ociosa possível |
| unicidade/pertinência | `HashSet<T>` | busca esperada próxima de `O(1)` | tabela de hash adicional |
| chave única para valor | `Dictionary<TKey, TValue>` | construção `O(n)`; busca esperada próxima de `O(1)` | buckets, chaves e valores |
| consumir na chegada | `Queue<T>` | extremidades `O(1)` fora de resize | array circular e capacidade |
| consumir em ordem inversa | `Stack<T>` | topo `O(1)` fora de resize | array e capacidade |

Os custos descrevem operações típicas da implementação documentada do .NET 11.
Não substituem medição do caso concreto.

## LINQ e o contrato de `IEnumerable<T>`

`IEnumerable<T>` representa algo que pode fornecer um enumerador. Ele não
promete coleção em memória, contagem barata, repetibilidade nem ausência de
efeitos colaterais. A fonte pode ser um array, um iterador com `yield return`,
uma leitura de arquivo ou um adaptador sobre outro sistema.

Muitos operadores LINQ que retornam `IEnumerable<T>` têm execução diferida. A
chamada a `Where` normalmente monta a consulta; o predicado roda quando alguém
enumera. Operadores terminais como `Count`, `Sum`, `First` e `ToArray` iniciam a
enumeração. Alguns operadores diferidos precisam de trabalho antecipado quando
a enumeração começa: `OrderBy`, por exemplo, precisa conhecer e ordenar a fonte
antes de entregar o primeiro resultado.

```csharp
var consulta = pedidos.Where(pedido => pedido.Total >= 100m);

// Alterações na fonte antes da enumeração podem aparecer no resultado.
pedidos.Add(new Pedido(10, 5, 150m));

foreach (var pedido in consulta)
    Console.WriteLine(pedido.Id);
```

Esse comportamento permite pipelines e processamento sob demanda. Também pode
surpreender quando o consumidor esperava um snapshot.

## Exemplo A: busca repetida em lista versus dicionário

O padrão abaixo faz uma busca linear para cada identificador:

```csharp
foreach (var id in idsProcurados)
{
    var pedido = pedidos.FirstOrDefault(item => item.Id == id);
    if (pedido is not null)
        Processar(pedido);
}
```

Com `n` pedidos e `m` identificadores, o pior caso é `O(n × m)`. Se ambos crescem
na mesma proporção, o comportamento é quadrático. Um loop simples não é
automaticamente `O(n)`: o trabalho dentro dele importa, inclusive chamadas LINQ.
Da mesma forma, loops aninhados sugerem multiplicação de trabalho, mas o limite
real depende de quantas iterações internas ocorrem e de saídas antecipadas. Conte
as operações em função da entrada em vez de classificar apenas pela aparência.

Pré-indexar muda o perfil:

```csharp
var porId = pedidos.ToDictionary(pedido => pedido.Id);

foreach (var id in idsProcurados)
{
    if (porId.TryGetValue(id, out var pedido))
        Processar(pedido);
}
```

O índice custa `O(n)` para construir e usa memória adicional; as `m` consultas
passam a ter custo esperado próximo de `O(m)`. A alternativa também impõe chaves
únicas: `ToDictionary` lança `ArgumentException` se dois pedidos tiverem o mesmo
ID segundo o comparador.

O ganho é uma hipótese até ser medido. Para uma única busca em dez itens, o
índice pode apenas adicionar trabalho. Ele se justifica com buscas numerosas,
dados suficientemente grandes e uma chave estável. Se apenas a pertinência
importa, `HashSet<int>` armazena somente as chaves. Para vários valores por
chave, `ToLookup` cria uma relação um-para-muitos e evita decidir artificialmente
qual duplicata vencerá.

## Exemplo B: reenumeração e efeitos colaterais

Um iterador executa seu corpo a cada enumeração:

```csharp
static IEnumerable<int> Gerar(Action aoProduzir)
{
    for (var numero = 0; numero < 3; numero++)
    {
        aoProduzir();
        yield return numero;
    }
}

var fonte = Gerar(() => Console.WriteLine("produziu"));
var quantidade = fonte.Count(); // produz três vezes
var soma = fonte.Sum();          // produz outras três vezes
```

O problema não é apenas CPU. Uma fonte pode consultar dados mutáveis, ler I/O,
registrar telemetria ou falhar de maneira diferente na segunda passagem. Efeitos
colaterais em iteradores e predicados tornam a quantidade e a ordem das execuções
parte oculta do comportamento.

Se os dois consumidores precisam do mesmo snapshot, materialize uma vez:

```csharp
var valores = fonte.ToArray();
var quantidade = valores.Length;
var soma = valores.Sum();
```

O benefício é repetibilidade e uma única leitura da fonte. A limitação é alocar
e reter todos os elementos. Em APIs, prefira receber `IReadOnlyCollection<T>`
quando múltiplas passagens e contagem fizerem parte do contrato; não presuma que
qualquer `IEnumerable<T>` tem essas propriedades.

## Exemplo C: materialização antecipada de uma coleção grande

`ToList` e `ToArray` executam a consulta imediatamente e armazenam todos os
resultados. Fazer isso antes de filtrar ou limitar pode ampliar memória, latência
até o primeiro item e pressão sobre o GC:

```csharp
// Materializa todos os eventos antes de selecionar os necessários.
var todos = LerEventos().ToList();
var primeirosErros = todos.Where(e => e.Erro).Take(100).ToArray();
```

Quando a fonte permite streaming, preserve a execução diferida até o limite:

```csharp
var primeirosErros = LerEventos()
    .Where(e => e.Erro)
    .Take(100)
    .ToArray();
```

A segunda forma pode parar após encontrar 100 itens, mas isso depende do
provider. Em `IQueryable`, por exemplo, é preciso inspecionar a consulta
traduzida; sem um `OrderBy`, “primeiros” também pode não ter ordem estável.

Materializar é correto quando o resultado precisa atravessar uma fronteira de
ciclo de vida, ser enumerado várias vezes, permanecer estável diante de mutação,
liberar um recurso associado à fonte ou permitir acesso por índice. Evite quando
o consumidor usa poucos itens, a fonte é muito grande ou potencialmente infinita,
ou um único percurso resolve o problema.

Escolha `ToArray` para um snapshot de tamanho fixo e indexável. Escolha `ToList`
quando a lista será alterada ou APIs exigirem esse contrato. A diferença de
performance entre ambos não deve ser presumida como critério principal sem um
caminho medido.

## Novidades em .NET 11 e C# 15: conciliação e construção de coleções

### FullJoin reconcilia duas fontes sem descartar registros órfãos

Em conciliações, o `Join` tradicional entrega apenas pares com chaves
correspondentes; `LeftJoin` conserva clientes sem pedidos; `FullJoin`
inclui **também pedidos cujo cliente não consta da fonte recebida**.

```csharp
var resultado = clientes.FullJoin(
    pedidos,
    cliente => cliente.Id,
    pedido => pedido.ClienteId,
    (cliente, pedido) => new
    {
        Cliente = cliente?.Nome,
        PedidoId = pedido?.Id
    });
```

O laboratório `Consultas.ConciliarClientesPedidos` projeta um identificador
de cliente, nome possivelmente ausente e identificador de pedido possivelmente
ausente. O teste compara `Join`, `LeftJoin` e `FullJoin`, verificando
correspondências repetidas, clientes sem pedidos, pedidos órfãos e fontes
vazias. É uma conciliação **em memória**: para `IQueryable` e EF Core,
consulte o SQL gerado e as capacidades do provider antes de extrapolar
o resultado. Chaves duplicadas podem multiplicar as linhas, portanto
não use `FullJoin` como substituto de deduplicação.

### with(...) informa parâmetros do construtor da coleção

O C# 15 aceita `with(...)` **como primeiro elemento** de uma expressão
de coleção. Assim, o código pode declarar explicitamente um comparador
ou uma reserva de capacidade:

```csharp
HashSet<string> categorias =
    [with(StringComparer.OrdinalIgnoreCase), "API", "api", "Dados"];

List<string> etapas =
    [with(capacity: 6), "validar", "persistir"];
```

O `HashSet` contém duas categorias, pois a igualdade ignora diferenças
de maiúsculas/minúsculas. Já a capacidade informa espaço inicial da
`List`, sem mudar a contagem lógica de elementos. Os exemplos
`CriarEtiquetasSemDuplicidade` e `CriarLoteComCapacidade` testam essas
propriedades e preservam o comportamento dos demais métodos. O comparador
é parte da **semântica de igualdade**, não uma otimização universal;
reservar capacidade pode evitar realocações quando a demanda é conhecida,
mas não comprova ganho sem medir o caso real.

Referências: [LINQ no .NET 11](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#linq-join-improvements)
e [expressões de coleção no C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions#collection-expression-arguments).

## Armadilhas comuns

- Escolher por Big O sem considerar construção, tamanho real e memória.
- Usar `Contains` ou `FirstOrDefault` em uma lista dentro de outro loop sem
  calcular o trabalho total.
- Criar um dicionário por item processado em vez de uma vez por lote reutilizável.
- Ignorar duplicatas ou igualdade ao trocar lista por hash.
- Chamar `ContainsKey` e depois o indexador quando `TryGetValue` faz uma busca e
  expressa ausência.
- Presumir que `IEnumerable<T>` pode ser repetido com o mesmo resultado.
- Colocar escrita, I/O ou logging dentro de `Where` e depender de quantas vezes
  o predicado roda.
- Espalhar `ToList` defensivo por cada camada, criando cópias e retenção sem
  definir quem precisa do snapshot.
- Retornar uma consulta diferida sobre recurso já descartado.

## Como decidir

Comece pela estrutura mais simples que preserve o contrato. Registre volume,
quantidade de buscas, frequência de reconstrução e memória disponível. Conte as
operações dominantes antes de alterar o código. Se o caminho for relevante,
compare uma baseline e a alternativa conforme o processo do C04.

## Quando utilizar e quando evitar

Use `List<T>` como ponto de partida para sequências ordenadas, acesso por índice
e poucas buscas. Adote `HashSet<T>` quando unicidade ou pertinência dominar, e
`Dictionary<TKey, TValue>` quando uma chave estável sustentar buscas repetidas.
Use fila ou pilha quando a ordem de consumo fizer parte do contrato. Materialize
quando precisar de snapshot, múltiplos percursos ou uma fronteira de lifetime.

Evite índices de hash descartáveis para entradas pequenas consultadas uma vez,
materialização de fontes grandes consumidas parcialmente e efeitos colaterais em
pipelines diferidos. Se volume e frequência não justificarem a estrutura mais
complexa, mantenha a alternativa simples e registre o sinal que motivaria rever
a decisão.

## Como verificar

O laboratório em `exemplos/c03-colecoes-linq/` verifica equivalência entre busca
linear e índice, duplicidade de chave, efeitos da reenumeração, snapshot por
materialização, parada antecipada e relações com `HashSet` e `Lookup`.

```bash
dotnet test exemplos/c03-colecoes-linq/C03.ColecoesLinq.Tests.csproj
```

Ele usa .NET 10 e não mede velocidade. Os testes provam comportamento e
pré-condições; a decisão de performance requer dados do ambiente de destino.

## Relação com outros capítulos

- **Pré-requisito:** [C01 — Tipos, identidade e modelagem em C#](c01-tipos-identidade-modelagem.md)
  explica igualdade, mutabilidade e armazenamento de valores em coleções.
- **Aprofundamento:** [C04 — Memória, runtime e engenharia de performance](c04-memoria-runtime-performance.md)
  trata alocações, GC, profiling e benchmarks.
- **Aplicação:** [C05 — Assincronicidade e concorrência controlada](c05-assincronicidade-concorrencia-controlada.md)
  usa filas e streaming em concorrência.
- **Paginação:** [C07 — APIs HTTP e integrações resilientes](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md)
  aplica materialização limitada e ordenação estável em contratos HTTP.
- **Aplicação:** [C08 — Consultas eficientes com EF Core](../aplicacoes-e-dados/c08-consultas-ef-core.md)
  distingue LINQ to Objects de consultas traduzidas pelo provider.

## Referências

- [Coleções e estruturas de dados](https://learn.microsoft.com/dotnet/standard/collections/)
  — documentação .NET, acesso em 18 novembro 2026.
- [`List<T>.Capacity`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1.capacity?view=net-10.0)
  — API .NET 10, acesso em 18 novembro 2026.
- [`List<T>.Find`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1.find?view=net-10.0)
  — busca linear e complexidade, acesso em 18 novembro 2026.
- [`HashSet<T>.Contains`](https://learn.microsoft.com/dotnet/api/system.collections.generic.hashset-1.contains?view=net-10.0)
  — API .NET 10, acesso em 18 novembro 2026.
- [`Dictionary<TKey, TValue>.ContainsKey`](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2.containskey?view=net-10.0)
  — API .NET 10, acesso em 18 novembro 2026.
- [`Queue<T>.Dequeue`](https://learn.microsoft.com/dotnet/api/system.collections.generic.queue-1.dequeue?view=net-10.0)
  e [`Stack<T>.Pop`](https://learn.microsoft.com/dotnet/api/system.collections.generic.stack-1.pop?view=net-10.0)
  — APIs .NET 10, acesso em 18 novembro 2026.
- [Introdução a consultas LINQ](https://learn.microsoft.com/dotnet/csharp/linq/get-started/introduction-to-linq-queries)
  — execução imediata e diferida, acesso em 18 novembro 2026.
- [Execução diferida e avaliação preguiçosa](https://learn.microsoft.com/dotnet/standard/linq/deferred-execution-lazy-evaluation)
  — documentação .NET, acesso em 18 novembro 2026.
- [`Enumerable.ToLookup`](https://learn.microsoft.com/dotnet/api/system.linq.enumerable.tolookup?view=net-10.0)
  — relação um-para-muitos, acesso em 18 novembro 2026.
