# C09 — Fronteiras e concorrência no EF Core

> **Nível:** Intermediário
>
> **Objetivo:** delimitar unidades de trabalho, persistir mudanças com segurança
> e resolver conflitos sem esconder as capacidades ou os custos do EF Core.
>
> **Pré-requisitos:** [C02 — Nulabilidade, falhas e exceções](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md),
> [C06 — Composição e pipeline no ASP.NET Core](c06-composicao-pipeline-aspnet-core.md)
> e [C08 — Consultas eficientes com EF Core](c08-consultas-ef-core.md).
>
> **Última revisão:** 2026-11-18

Um `DbContext` é ao mesmo tempo sessão com o modelo, identity map, change tracker
e coordenador de persistência. Esconder todos esses papéis atrás de uma abstração
genérica pode perder semântica; deixar um contexto viver indefinidamente acumula
estado e conflitos. A fronteira correta é uma unidade de trabalho curta,
alinhada a uma operação de negócio e com uma política explícita para falhas.

## Ciclo de vida: uma unidade de trabalho

O ciclo típico é criar o contexto, consultar ou anexar entidades, modificar o
grafo, chamar `SaveChangesAsync` e descartar o contexto. Em ASP.NET Core,
`AddDbContext` registra o tipo como scoped por padrão e uma requisição costuma
ser uma fronteira adequada. Não é uma regra absoluta: um worker, circuito Blazor
ou requisição com várias operações independentes pode precisar de
`IDbContextFactory<TContext>` e um contexto por unidade.

`DbContext` não é thread-safe. Não execute duas operações paralelas na mesma
instância; aguarde cada chamada assíncrona ou crie contextos independentes. Um
contexto também não é cache de aplicação. Quanto mais tempo vive, mais entidades
e snapshots retém, maior a chance de dados obsoletos e maior a janela entre
leitura e escrita.

<!-- snippet-csharp: id=c09-01; categoria=contexto -->
```csharp
await using var context = await contextFactory.CreateDbContextAsync(ct);
var pedido = await context.Pedidos.SingleAsync(p => p.Id == id, ct);
pedido.Status = "pago";
await context.SaveChangesAsync(ct);
```

Descarte libera recursos possuídos pelo contexto; a conexão normalmente é
aberta apenas durante uma operação e devolvida ao pool do provider. Pooling de
conexões e pooling de `DbContext` são mecanismos diferentes. Context pooling
reutiliza instâncias resetadas e exige cuidado com estado mutável por requisição;
só deve ser adotado após medição.

## Identity map e Change Tracker

O identity map garante uma única instância rastreada por tipo e chave dentro do
contexto. Sem essa regra, duas representações da mesma linha poderiam ter valores
e relacionamentos conflitantes. Tentar anexar outra instância com a mesma chave
enquanto a primeira é rastreada normalmente falha; prefira consultar e aplicar
valores na instância existente.

O `ChangeTracker` mantém entradas, valores originais, valores atuais, estados e
relacionamentos. No rastreamento por snapshot, o EF Core compara valores atuais
com o snapshot original. `DetectChanges` ocorre automaticamente em pontos como
`SaveChanges`, `Entries` e `HasChanges`; chamá-lo manualmente normalmente é
desnecessário. Desabilitar detecção automática pode ajudar em operações massivas
medidas, mas transfere ao código a responsabilidade de executá-la no momento
correto.

| Estado | Significado | Efeito usual em `SaveChanges` |
|---|---|---|
| `Detached` | fora do tracker | nenhum |
| `Unchanged` | existe e não mudou | nenhum comando de escrita |
| `Added` | novo | `INSERT` |
| `Modified` | propriedades marcadas mudaram | `UPDATE` |
| `Deleted` | deve ser removido | `DELETE` |

Após sucesso, `SaveChanges` aceita as mudanças por padrão: adicionados e
modificados tornam-se `Unchanged`; removidos deixam de ser rastreados. O método
detecta mudanças, ordena comandos para respeitar relacionamentos e, se o provider
relacional suportar transações, aplica atomicamente as alterações de uma chamada
que exijam múltiplos comandos. Isso não torna várias chamadas, mensagens externas
ou outros bancos uma única transação.

Manipular `EntityState.Modified` em uma entidade desconectada marca propriedades
sem conhecer um snapshot confiável e pode sobrescrever colunas. Para updates
parciais, carregue a entidade e altere campos permitidos, ou anexe uma instância
e marque explicitamente apenas propriedades validadas. Nunca aplique cegamente
um DTO de entrada ao grafo persistido.

Consultas de leitura e custos de `AsNoTracking` são detalhados no
[C08](c08-consultas-ef-core.md). No-tracking não é apropriado quando o fluxo
espera alterar a instância e deixar `SaveChanges` detectar a mudança.

## Concorrência otimista protege contra lost update

Duas unidades de trabalho podem ler a mesma versão. Sem controle, a última
gravação pode apagar silenciosamente a primeira. Concorrência otimista não
bloqueia a linha durante toda a interação; ela inclui um token original no
`WHERE` do `UPDATE` ou `DELETE`:

```sql
UPDATE Pedidos
SET Status = @status, Versao = @novaVersao
WHERE Id = @id AND Versao = @versaoOriginal;
```

Se nenhuma linha for afetada, o EF Core lança `DbUpdateConcurrencyException`.
Configure o token por `[ConcurrencyCheck]`, `.IsConcurrencyToken()` ou mecanismo
específico do provider. No SQL Server, uma propriedade `byte[]` configurada com
`.IsRowVersion()` usa `rowversion` gerado pelo banco e protege a linha inteira.
`rowversion` não é timestamp de data e não é portável para SQLite.

SQLite não possui um tipo autoatualizado equivalente. O laboratório usa `Guid`
gerenciado pela aplicação e renovado antes de inserts e updates:

<!-- snippet-csharp: id=c09-02; categoria=contexto -->
```csharp
modelBuilder.Entity<Pedido>()
    .Property(pedido => pedido.Versao)
    .IsConcurrencyToken();
```

Um token gerenciado permite decidir quais mudanças o renovam, mas esquecer de
renová-lo elimina a proteção. Interceptor ou override de `SaveChanges` pode
centralizar a regra. Triggers e colunas geradas pelo banco são alternativas
dependentes do provider. Um token único por linha detecta conflito, não decide
qual versão está semanticamente correta.

## Resolver um conflito é uma decisão de negócio

Ao capturar `DbUpdateConcurrencyException`, existem três conjuntos de valores:

- **originais:** lidos quando a entidade entrou no contexto;
- **atuais:** mudanças propostas pelo cliente;
- **do banco:** estado persistido depois da alteração concorrente.

Há três políticas principais.

### Rejeitar e pedir nova decisão

Para preço, saldo ou aprovação, rejeitar costuma ser mais seguro. Retorne um
conflito de aplicação — em HTTP, frequentemente `409 Conflict` — com a versão
atual apropriada ao contrato. `ReloadAsync` descarta mudanças locais e atualiza a
entidade; descartar o contexto também encerra a tentativa. Não converta conflito
em sucesso silencioso.

### Banco vence

Substitua valores atuais pelos valores do banco quando a intenção puder ser
abandonada. Isso é uma forma explícita de rejeição local, útil para edição
informativa. Avise o chamador quando a alteração dele não foi aplicada.

### Cliente vence ou merge

Para cliente vence, atualize os valores originais com a versão do banco e tente
novamente mantendo os atuais. Isso pode sobrescrever a alteração concorrente;
só é aceitável quando a regra permite. Merge escolhe campo a campo ou recalcula
a operação sobre o estado recente:

<!-- snippet-csharp: id=c09-03; categoria=contexto -->
```csharp
catch (DbUpdateConcurrencyException exception)
{
    var entry = exception.Entries.Single();
    var banco = await entry.GetDatabaseValuesAsync(ct);

    if (banco is null)
        return Resultado.Rejeitado("O pedido foi removido.");

    var statusProposto = entry.CurrentValues[nameof(Pedido.Status)];
    entry.CurrentValues.SetValues(banco);       // preserva demais campos recentes
    entry.CurrentValues[nameof(Pedido.Status)] = statusProposto;
    entry.OriginalValues.SetValues(banco);      // nova condição de concorrência

    await context.SaveChangesAsync(ct);
}
```

O exemplo simplifica a política: mantém o status proposto e aceita os outros
campos do banco. Uma interface de edição real deve comparar original, atual e
banco e validar novamente invariantes. Se a linha foi removida,
`GetDatabaseValuesAsync` retorna `null`.

### Retry não é repetição cega

Repita somente depois de obter estado novo e reaplicar uma operação ainda válida.
Limite tentativas e encerre sob contenção persistente. Uma strategy de retry para
falhas transitórias de rede não resolve conflito de concorrência: o mesmo token
obsoleto continuará falhando. Reexecutar toda a unidade pode duplicar efeitos
externos; idempotência e Outbox pertencem a fronteiras posteriores do guia.

Tokens otimistas não substituem constraints, transações ou isolamento. Uma
restrição única protege invariantes de unicidade; o nível de isolamento regula o
que transações observam; locks pessimistas podem ser necessários em casos raros
de alta contenção e exigem suporte e análise do banco.

## Limites arquiteturais

### Não exponha `IQueryable<T>` indiscriminadamente

Retornar `IQueryable` de um repositório permite que qualquer camada acrescente
filtros, includes, client evaluation, materialização e custos imprevisíveis. A
consulta executa depois, talvez quando o contexto já tenha sido descartado. A
assinatura também vaza capacidades e limitações do provider.

Isso não torna `IQueryable` proibido. Dentro da camada de persistência ele é uma
ferramenta forte de composição. Em fronteiras internas controladas, uma
especificação limitada pode ser apropriada. Em uma API pública entre camadas,
prefira operações orientadas ao caso de uso que retornem DTOs materializados:

<!-- snippet-csharp: id=c09-04; categoria=contexto -->
```csharp
public interface IConsultaDePedidos
{
    Task<IReadOnlyList<ResumoPedido>> ListarAbertosAsync(
        int limite,
        CancellationToken cancellationToken);
}
```

O nome expõe a intenção; a implementação mantém SQL, projeção, paginação e
tracking coesos e testáveis.

### Repository e Unit of Work: avalie o valor adicional

`DbSet<T>` já oferece coleção consultável e operações de persistência;
`DbContext` já rastreia mudanças e coordena uma unidade de trabalho. Um
`GenericRepository<T>` com `GetAll`, `Add`, `Update` e `Delete` frequentemente
repete a API, esconde projeção e ainda não abstrai diferenças entre bancos.

Um repositório específico agrega valor quando protege a fronteira de um agregado,
centraliza uma consulta complexa, aplica política de acesso ou apresenta uma
porta exigida pela arquitetura. Uma unidade de trabalho própria pode coordenar
vários repositórios sobre o mesmo contexto ou controlar commit, mas não deve
chamar `SaveChanges` em cada repositório e destruir atomicidade.

Não crie abstração apenas para poder mockar EF Core. Testes de domínio devem
operar sobre regras sem persistência. Testes de consultas, mapeamentos,
constraints, transações e concorrência precisam do provider relacional real.
Fakes podem testar orquestração de uma porta específica, mas não comprovam SQL.

### Domínio e persistência

O domínio não precisa depender de `DbContext`, attributes do EF ou
`IQueryable`. Configuração Fluent pode manter mapeamento na infraestrutura.
Entretanto, insistir em um modelo completamente ignorante do EF a ponto de criar
cópias e repositórios sem comportamento também tem custo. Escolha conforme a
complexidade do domínio, estabilidade do banco e necessidade real de isolamento.

Entidades persistidas não devem atravessar diretamente contratos HTTP. DTOs
protegem versionamento, autorização por campo e over-posting. A camada de
aplicação define a unidade de trabalho; o domínio mantém invariantes; a
infraestrutura traduz consultas e persistência. Essas responsabilidades podem
estar no mesmo assembly em um sistema pequeno sem deixarem de ser distintas.

## Diagnóstico e manutenção

Para falha de escrita, registre tipo da operação, entidade, quantidade de
entradas e correlação, nunca valores sensíveis por padrão. Em conflito, meça a
taxa por caso de uso; crescimento pode indicar contexto longo, tela obsoleta ou
contenção estrutural. Inspecione o SQL para confirmar que chave e token aparecem
no `WHERE`.

Evite registrar o contexto como singleton ou guardar entidades rastreadas em
cache. Monitore crescimento de entradas em processos longos. `ChangeTracker.Clear`
remove todas com mais eficiência que desanexar uma a uma, mas muitas chamadas
podem indicar que a unidade de trabalho está ampla demais.

Fundamentos de constraints, transações e isolamento estão no
[C10](c10-fundamentos-persistencia-sql-nosql.md); migrações e deploy compatível
serão tratados no capítulo de entrega. A estratégia de concorrência faz parte
do schema: alterar token ou trigger precisa ser versionado e testado junto com
a aplicação.

## Laboratório executável

O mesmo laboratório do C08 usa EF Core 10.0.12 e SQLite em arquivos temporários.
O exemplo **E** abre dois contextos, provoca um conflito real, captura
`DbUpdateConcurrencyException` e faz merge preservando uma mudança concorrente.
Outros testes validam identity map e ausência de tracking.

```bash
dotnet test exemplos/c08-c09-ef-core/C08C09.EfCore.Tests.csproj
```

SQLite é adequado para provar o token gerenciado e o SQL relacional do exemplo.
Ele não prova `rowversion`, locks ou planos do SQL Server. Para comportamentos
específicos do banco de produção, use seu provider e uma instância efêmera, por
exemplo com Testcontainers. O provider InMemory não substitui essa validação.

## Como decidir

Defina primeiro a operação de negócio e seu limite atômico. Use um contexto
curto por unidade, mantenha consultas e alterações relacionadas na mesma
instância e torne a política de conflito parte do caso de uso. Exponha consultas
específicas quando a fronteira precisar controlar custo. Adicione repositórios
ou unidade de trabalho próprios somente quando expressarem uma regra que
`DbContext` e `DbSet` não tornam clara por si mesmos.

## Relação com outros capítulos

- [C06](c06-composicao-pipeline-aspnet-core.md) explica scoped lifetime e
  descarte no container de DI.
- [C08](c08-consultas-ef-core.md) cobre tradução, tracking de leitura, N+1 e
  diagnóstico de SQL.
- [C10](c10-fundamentos-persistencia-sql-nosql.md) trata transações, índices,
  constraints e isolamento.
- [C11](../dominio-e-arquitetura/c11-entidades-value-objects-agregados.md) trata
  entidades, agregados e invariantes; C13 tratará dependências e abstrações.
- C16 tratará idempotência e Outbox em efeitos distribuídos.

## Referências

- [Lifetime e configuração de `DbContext`](https://learn.microsoft.com/ef/core/dbcontext-configuration/)
  — EF Core, acesso em 18 novembro 2026.
- [Change tracking](https://learn.microsoft.com/ef/core/change-tracking/)
  — estados e ciclo básico, acesso em 18 novembro 2026.
- [Identity resolution](https://learn.microsoft.com/ef/core/change-tracking/identity-resolution)
  — EF Core, acesso em 18 novembro 2026.
- [Detecção de mudanças](https://learn.microsoft.com/ef/core/change-tracking/change-detection)
  — snapshots e `DetectChanges`, acesso em 18 novembro 2026.
- [Transações](https://learn.microsoft.com/ef/core/saving/transactions)
  — comportamento de `SaveChanges`, acesso em 18 novembro 2026.
- [Concorrência otimista](https://learn.microsoft.com/ef/core/saving/concurrency)
  — tokens e resolução de conflitos, acesso em 18 novembro 2026.
- [Testes sem o sistema de banco de dados](https://learn.microsoft.com/ef/core/testing/testing-without-the-database)
  — limites de mocks, SQLite e InMemory, acesso em 18 novembro 2026.
- [Testes com o banco de produção](https://learn.microsoft.com/ef/core/testing/testing-with-the-database)
  — estratégia de integração, acesso em 18 novembro 2026.
