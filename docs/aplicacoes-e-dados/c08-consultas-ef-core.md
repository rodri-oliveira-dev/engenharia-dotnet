# C08 — Consultas eficientes com EF Core

> **Nível:** Intermediário
>
> **Objetivo:** construir e diagnosticar consultas EF Core a partir do trabalho
> realizado no banco, dos dados transferidos e dos objetos materializados.
>
> **Pré-requisitos:** [C03 — Coleções, LINQ e execução diferida](../linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md).
> [C10 — Fundamentos de persistência SQL e NoSQL](c10-fundamentos-persistencia-sql-nosql.md).
>
> **Última revisão:** 2026-11-18

Uma expressão LINQ curta pode significar uma consulta seletiva, dezenas de
round trips ou uma junção que multiplica linhas. O código C# não revela sozinho
o custo: o provider traduz uma árvore de expressões, o banco escolhe um plano e
o EF Core materializa o resultado. A unidade de diagnóstico é esse caminho
completo, não a quantidade de métodos encadeados.

Este capítulo não repete execução diferida e materialização de `IEnumerable<T>`,
tratadas no [C03](../linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md).
Aqui a pergunta é onde cada operação executa, qual SQL ela produz e qual volume
atravessa as fronteiras. Ciclo de vida do contexto, estados, `SaveChanges` e
concorrência são canônicos no [C09](c09-fronteiras-concorrencia-ef-core.md).

## `IQueryable<T>` é uma descrição de consulta

`DbSet<T>` implementa `IQueryable<T>`. Operadores como `Where`, `OrderBy` e
`Select` compõem uma árvore de expressões; eles ainda não consultam o banco.
Um operador terminal assíncrono, como `ToListAsync`, `SingleAsync` ou
`CountAsync`, pede ao provider que traduza e execute a expressão.

<!-- snippet-csharp: id=c08-01; categoria=contexto -->
```csharp
IQueryable<Pedido> consulta = context.Pedidos;

if (somenteAbertos)
    consulta = consulta.Where(pedido => pedido.Status == "aberto");

var resumos = await consulta
    .OrderBy(pedido => pedido.Id)
    .Select(pedido => new { pedido.Id, pedido.Descricao })
    .Take(50)
    .ToListAsync(cancellationToken);
```

A composição permite que filtro, ordenação, limite e projeção cheguem juntos ao
banco. Ela também significa que uma extensão aparentemente comum precisa ser
traduzível pelo provider. Providers diferentes suportam operações e tipos
diferentes; uma consulta que funciona em SQLite não prova o mesmo SQL ou plano
em SQL Server ou PostgreSQL.

### A fronteira para `IEnumerable<T>`

`AsEnumerable`, `ToList` e equivalentes mudam onde o restante do pipeline roda.
Depois de `AsEnumerable`, operadores de `Enumerable` executam em .NET. Depois de
`ToList`, todo o resultado anterior já foi transferido e armazenado.

<!-- snippet-csharp: id=c08-02; categoria=contexto -->
```csharp
// Filtra e limita no banco.
var corretos = await context.Pedidos
    .Where(pedido => pedido.Status == status)
    .Take(100)
    .ToListAsync();

// Materializa todos e só então filtra em memória.
var todos = await context.Pedidos.ToListAsync();
var caros = todos.Where(pedido => pedido.Total >= minimo).Take(100);
```

A segunda forma pode ser deliberada para uma entrada comprovadamente pequena e
uma função sem tradução, mas a fronteira precisa ser visível. Não use
`AsEnumerable` para apenas silenciar erro de tradução.

EF Core admite avaliação no cliente na projeção de nível superior. Uma função
C# no último `Select` pode rodar após a leitura das colunas necessárias. Uma
expressão não traduzível em filtro, ordenação ou outra parte da consulta gera
exceção, evitando que um filtro caro seja transferido silenciosamente para a
memória. Inspecione e teste cada consulta relevante com o provider de produção.

## Projete o que o consumidor precisa

Carregar uma entidade completa cria o objeto, lê todas as colunas mapeadas e,
por padrão, registra a entidade no rastreamento. Isso é correto quando a unidade
de trabalho modificará aquela entidade. Para uma tela ou resposta somente de
leitura, um `Select` para um modelo de leitura costuma expressar melhor o
contrato e reduzir transferência e materialização.

<!-- snippet-csharp: id=c08-03; categoria=contexto -->
```csharp
var pagina = await context.Pedidos
    .AsNoTracking()
    .OrderBy(pedido => pedido.Id) // ordenação estável antes de Skip/Take
    .Skip((numeroDaPagina - 1) * tamanho)
    .Take(tamanho)
    .Select(pedido => new ResumoPedido(
        pedido.Id,
        pedido.Descricao,
        pedido.Total,
        pedido.Itens.Count))
    .ToListAsync(cancellationToken);
```

O banco calcula o `Count`, aplica paginação e devolve apenas as colunas do DTO.
`AsNoTracking` reforça a intenção, embora uma projeção composta somente por
valores escalares já não contenha entidades para rastrear. Se o `Select` inclui
uma entidade em algum campo, essa entidade ainda pode ser rastreada.

Paginação por deslocamento (`Skip`/`Take`) é simples e permite ir a uma página
arbitrária, mas páginas distantes podem exigir que o banco percorra muitas
linhas e mudanças concorrentes podem deslocar resultados. Paginação por cursor
ou chave — por exemplo, `Id > ultimoId`, com ordenação única e índice compatível
— é alternativa para navegação sequencial. O limite e a ordenação pertencem ao
contrato; não aceite tamanhos ilimitados.

## Tracking, identidade e memória

Uma consulta com tracking registra cada entidade no `ChangeTracker` e realiza
identity resolution: para uma mesma chave, o contexto mantém uma única instância.
Isso sustenta correção do grafo e atualizações posteriores, mas guarda entradas,
snapshots e relacionamentos até o contexto ser limpo ou descartado.

| Modo | Identity resolution | Entra no contexto | Uso típico |
|---|---|---|---|
| tracking, padrão | sim, pelo contexto | sim | ler, alterar e salvar na mesma unidade de trabalho |
| `AsNoTracking()` | não | não | leitura curta sem entidades repetidas relevantes |
| `AsNoTrackingWithIdentityResolution()` | sim, em tracker temporário | não | grafo somente leitura com repetição de entidades |

<!-- snippet-csharp: id=c08-04; categoria=contexto -->
```csharp
var paraEditar = await context.Pedidos.SingleAsync(p => p.Id == id);
paraEditar.Status = "pago";
await context.SaveChangesAsync();

var paraExibir = await context.Pedidos
    .AsNoTracking()
    .Select(p => new { p.Id, p.Status })
    .ToListAsync();
```

No-tracking elimina o estado duradouro de rastreamento, mas não torna toda
consulta automaticamente mais barata. Se uma junção repete a mesma entidade,
`AsNoTracking` pode criar várias instâncias; tracking ou
`AsNoTrackingWithIdentityResolution` pode economizar objetos ao custo de manter
um mapa de identidade durante a materialização. Projeção pode evitar ambos.

Não transforme `NoTracking` no padrão global sem examinar os fluxos de escrita.
Também não mantenha um contexto longo para tentar reutilizar seu identity map:
o estado cresce, pode ficar obsoleto e aumenta o intervalo para conflitos. Meça
alocação e quantidade de entidades materializadas quando memória for o problema.

## N+1: uma consulta inicial mais uma por elemento

N+1 aparece quando o código carrega `N` pais e busca dados relacionados dentro
de um loop. Lazy loading torna esse acesso implícito: ler uma navegação pode
abrir uma consulta. Explicit loading dentro do loop tem o mesmo perfil, apenas
mais visível.

<!-- snippet-csharp: id=c08-05; categoria=contexto -->
```csharp
var pedidos = await context.Pedidos.AsNoTracking().ToListAsync(); // 1

foreach (var pedido in pedidos)
{
    var quantidade = await context.Itens                         // + N
        .CountAsync(item => item.PedidoId == pedido.Id);
}
```

Para uma listagem que precisa somente da contagem, projete a relação:

<!-- snippet-csharp: id=c08-06; categoria=contexto -->
```csharp
var pedidos = await context.Pedidos
    .AsNoTracking()
    .Select(pedido => new
    {
        pedido.Id,
        Quantidade = pedido.Itens.Count
    })
    .ToListAsync(); // uma consulta no laboratório SQLite
```

`Include` é eager loading de entidades relacionadas e resolve outros casos:
editar um agregado ou devolver um grafo delimitado. Ele não é correção universal
para N+1. Pode trazer colunas e linhas desnecessárias; projeção é preferível
quando o consumidor precisa de poucos campos, agregações ou um DTO.

Lazy loading pode ser habilitado com proxies ou `ILazyLoader`, mas transforma o
acesso a uma propriedade em I/O implícito e exige que o contexto ainda esteja
vivo. Serialização e loops tornam-se pontos fáceis para N+1. Quando adotado por
uma necessidade específica, delimite o grafo, monitore comandos e não dependa
dele fora da unidade de trabalho. O laboratório não habilita lazy loading: as
consultas adicionais do exemplo são explícitas para deixar o custo observável.
Eager loading por `Include`, explicit loading por `Entry(...).Collection(...).Load`
e projeção são alternativas com diferentes formas e volumes de resultado.

### Como detectar

Registre comandos do EF Core com duração e correlação da operação, sem dados
sensíveis. Procure o mesmo formato SQL repetido com parâmetros distintos.
Interceptors podem contar comandos em testes, como faz o laboratório. Em
produção, combine logs amostrados, tracing e métricas de duração e quantidade de
round trips; não habilite `EnableSensitiveDataLogging` indiscriminadamente.

Um teste que afirma “uma consulta” é útil para uma consulta crítica, mas acopla
o comportamento ao provider e à estratégia escolhida. Use-o quando a regressão
para N+1 for risco maior que esse acoplamento.

## Uma consulta ou consultas divididas

Ao incluir duas coleções irmãs em uma single query, o SQL usa `JOIN`. Se um
pedido tem dez itens e dez marcadores, o resultado relacional pode conter cem
combinações para aquele pedido. Essa explosão cartesiana aumenta linhas,
transferência e trabalho de materialização. Mesmo com uma coleção, colunas
grandes do pai podem ser duplicadas em cada linha.

<!-- snippet-csharp: id=c08-07; categoria=contexto -->
```csharp
var pedidos = await context.Pedidos
    .Include(pedido => pedido.Itens)
    .Include(pedido => pedido.Marcadores)
    .AsSplitQuery()
    .ToListAsync();
```

`AsSplitQuery` emite uma consulta para a raiz e outra para cada coleção incluída.
Evita o produto cartesiano, mas adiciona round trips e pode precisar manter
resultados intermediários em memória. Entre as consultas, outra transação pode
alterar dados; o grafo observado não possui automaticamente a consistência de
uma única instrução. Uma transação com isolamento apropriado pode reduzir essa
janela, com custos de locks, versões ou contenção dependentes do banco.

`AsSingleQuery` força a forma com joins. Ela é adequada para volume pequeno,
uma coleção limitada ou rede de alta latência quando as linhas duplicadas não
são o gargalo. `AsSplitQuery` é candidata quando múltiplas coleções geram grande
multiplicação e os round trips cabem no orçamento. Projeções podem produzir uma
forma ainda melhor que ambas.

Não escolha pela quantidade de `Include` isoladamente. Compare cardinalidade
real, bytes transferidos, número de comandos, latência, memória e consistência
exigida. Se configurar split globalmente, `AsSingleQuery` documenta exceções;
se mantiver o padrão single, seja explícito nas consultas com múltiplas coleções.

## Diagnóstico de uma consulta lenta

“O EF está lento” é uma hipótese ampla. Separe o tempo de composição e
compilação, espera de conexão, execução no servidor, transferência, round trips
e materialização. Investigue nesta ordem:

1. Capture a operação lenta com duração, volume e correlação. Confirme se é uma
   consulta lenta, muitas consultas ou espera por conexão/lock.
2. Obtenha o SQL com logs ou `ToQueryString()` e confira parâmetros. Use
   `TagWith` para ligar uma consulta importante à telemetria. `ToQueryString`
   serve para diagnóstico e não substitui o comando final de toda operação.
3. Execute com parâmetros representativos e examine o plano real quando o banco
   oferecer essa distinção. Busque scans inesperados, estimativas de
   cardinalidade divergentes, sorts, lookups, spills e esperas.
4. Verifique filtros seletivos, conversões, funções sobre colunas e índices cuja
   ordem e inclusão atendam ao acesso. Índice acelera certas leituras, mas ocupa
   espaço e encarece escrita; sua sintaxe e eficácia dependem do banco.
5. Reduza colunas e linhas: projete, filtre antes de materializar, limite páginas
   e evite entidades completas ou includes sem consumidor.
6. Conte round trips e procure N+1. Compare single e split apenas para o grafo e
   as cardinalidades reais.
7. Meça materialização e tracking separadamente se o SQL já for rápido. Um plano
   bom não impede custo alto para criar centenas de milhares de objetos.

Um profiler do banco, Query Store ou observabilidade do serviço ajuda a localizar
as operações dominantes. Colete taxa, latência por percentil, linhas, bytes,
comandos por operação, conexões/locks e erros; médias isoladas escondem caudas.
Controle a cardinalidade das dimensões de telemetria: não use SQL completo, IDs
ou valores de parâmetros como labels de métrica. O profiler localiza candidatos;
o plano e as medidas do caso representativo explicam o gargalo.

SQLite `EXPLAIN QUERY PLAN`, usado no exemplo F, confirma que o índice do
laboratório é elegível. Ele não prevê o plano de outro provider nem demonstra
ganho de tempo. Estatísticas, distribuição dos valores, parâmetros, cache,
concorrência e versão do banco influenciam estimativas e escolhas. Use as
ferramentas nativas — por exemplo, planos reais e Query Store no SQL Server ou
`EXPLAIN (ANALYZE, BUFFERS)` no PostgreSQL — com cuidado para não executar uma
consulta perigosa em produção.

## Compiled queries são uma otimização de nicho

O EF Core já mantém cache da tradução por formato da árvore de expressão.
Valores capturados como parâmetros preservam o formato e favorecem o cache; uma
construção dinâmica que embute constantes pode criar formatos e planos distintos.

`EF.CompileQuery` e `EF.CompileAsyncQuery` produzem um delegate e evitam até a
comparação da árvore com o cache em cada execução:

<!-- snippet-csharp: id=c08-08; categoria=contexto -->
```csharp
private static readonly Func<LojaContext, string,
    IAsyncEnumerable<ResumoPedido>> PorStatus =
    EF.CompileAsyncQuery(
        (LojaContext db, string status) => db.Pedidos
            .AsNoTracking()
            .Where(pedido => pedido.Status == status)
            .Select(pedido => new ResumoPedido(
                pedido.Id, pedido.Descricao, pedido.Total,
                pedido.Itens.Count)));
```

O delegate pode ser compartilhado e invocado com contextos distintos do mesmo
modelo. Parâmetros devem ser escalares simples, e a consulta fica menos flexível
para composição dinâmica. Use essa opção somente para consultas muito frequentes
cujo profiling atribua custo relevante à preparação no cliente. Antes disso,
corrija N+1, SQL, índices, volume e rede. Compilar uma consulta não muda o SQL,
o plano, as linhas lidas nem os objetos materializados.

## Como decidir

Comece pelo resultado mínimo e por uma consulta composta até a materialização.
Use tracking quando salvará as mesmas entidades; use projeção e, quando houver
entidades, no-tracking para leituras. Para relacionamentos, escolha entre
projeção, eager loading delimitado e carregamento explícito a partir do que o
caso precisa. Só então compare single/split e otimizações de preparação.

Não recomende `AsNoTracking`, `Include`, `AsSplitQuery` ou compiled queries por
convenção. Registre o volume, cardinalidade, consistência, quantidade de comandos
e evidência que justificam a escolha.

## Novidades da versão 11

O [laboratório EF Core 11](../../exemplos/c08-c09-ef-core/README.md) amplia
os exemplos de consulta sem substituir os fundamentos anteriores:

- **FullJoin relacional:** concilia cadastros de clientes e pedidos externos
  sem chave estrangeira entre as fontes, preservando registros órfãos de
  ambos os lados. Diferentemente do exemplo em memória do C03, aqui o
  provider precisa traduzir a expressão em um `FULL JOIN`.
- **MaxByAsync/MinByAsync:** selecionam as entidades com maior e menor número
  de itens, com ordenação/limite executados pelo banco. Sem critério secundário,
  empates não possuem identidade determinística.
- **GroupBy com navegação:** agrupa itens pelo status do pedido relacionado;
  o teste inspeciona `JOIN`, `GROUP BY`, `SUM` e os resultados efetivos.
- **string.Join com ordenação:** concatena marcadores via `group_concat`
  ordenado, tradução disponível em SQLite 3.44.0 ou superior.
- **Split query com referência:** inclui `Categoria` e `Itens` para
  demonstrar o join de referência no SQL principal e sua remoção na
  consulta separada da coleção, sem alegar ganho de tempo não medido.
- **JSON no SQL Server:** verifica a tradução de
  `EF.Functions.JsonPathExists` para `JSON_PATH_EXISTS` sem conexão;
  execução requer SQL Server 2022 ou mais recente.

A comparação de SQL é específica do provider. Os resultados do SQLite
não garantem os mesmos planos, índices e custos no SQL Server. A análise
das mudanças de compatibilidade está no
[guia de atualização para EF Core 11](ef-core-11-compatibilidade.md).

## Laboratório executável

O projeto `exemplos/c08-c09-ef-core/` usa EF Core 11.0.0 e SQLite real em um
arquivo temporário por teste. Ele cobre:

- **A:** tracking, `AsNoTracking` e identity resolution;
- **B:** N+1 explícito e correção por projeção, contando comandos;
- **C:** duas coleções com `AsSingleQuery` e `AsSplitQuery`;
- **D:** paginação ordenada e projeção sem a coluna grande;
- **F:** SQL gerado e `EXPLAIN QUERY PLAN` com índice;
- compiled query assíncrona no código de exemplo.

```bash
dotnet test exemplos/c08-c09-ef-core/C08C09.EfCore.Tests.csproj
```

Os testes provam tradução e comportamento no SQLite, não performance. O provider
InMemory não é usado porque não reproduz SQL, joins, índices nem concorrência
relacional. Para validar o banco de produção, mantenha uma suíte adicional com
esse provider e, quando necessário, uma instância efêmera via Testcontainers.

## Relação com outros capítulos

- [C03](../linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md) define
  execução diferida e materialização em LINQ to Objects.
- [C04](../linguagem-e-runtime/c04-memoria-runtime-performance.md) define o
  processo de profiling, baseline e benchmark.
- [C09](c09-fronteiras-concorrencia-ef-core.md) trata `DbContext`, escrita,
  concorrência otimista e limites arquiteturais.
- [C10](c10-fundamentos-persistencia-sql-nosql.md) trata índices, transações,
  isolamento e locks como fundamentos de banco.

## Referências

- [Consultas eficientes](https://learn.microsoft.com/ef/core/performance/efficient-querying)
  — EF Core, acesso em 18 novembro 2026.
- [Tracking e no-tracking](https://learn.microsoft.com/ef/core/querying/tracking)
  — EF Core, acesso em 18 novembro 2026.
- [Avaliação no cliente e no servidor](https://learn.microsoft.com/ef/core/querying/client-eval)
  — EF Core, acesso em 18 novembro 2026.
- [Carregamento de dados relacionados](https://learn.microsoft.com/ef/core/querying/related-data/)
  — EF Core, acesso em 18 novembro 2026.
- [Single e split queries](https://learn.microsoft.com/ef/core/querying/single-split-queries)
  — EF Core, acesso em 18 novembro 2026.
- [Diagnóstico e logging](https://learn.microsoft.com/ef/core/logging-events-diagnostics/)
  — EF Core, acesso em 18 novembro 2026.
- [Tópicos avançados de performance](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics)
  — cache e compiled queries, acesso em 18 novembro 2026.
- [Provider SQLite do EF Core](https://learn.microsoft.com/ef/core/providers/sqlite/)
  — limitações e configuração, acesso em 18 novembro 2026.
- [Microsoft.EntityFrameworkCore.Sqlite 10.0.12](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.12)
  — pacote usado pelo laboratório, acesso em 18 novembro 2026.
