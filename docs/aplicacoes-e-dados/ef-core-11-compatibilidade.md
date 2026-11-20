# EF Core 11: recursos, provedores e compatibilidade

> **Nível:** Avançado
>
> **Objetivo:** demonstrar mudanças da versão 11, distinguir SQL traduzido
> de comportamento executado e planejar uma atualização sem regressões.
>
> **Pré-requisitos:** [C08](c08-consultas-ef-core.md) e
> [C09](c09-fronteiras-concorrencia-ef-core.md).
>
> **Última revisão:** 2026-11-20

Este guia complementa os fundamentos do EF Core: o alvo é identificar o que
mudou ao migrar da linha 10 para a linha 11, em quais provedores a mudança
funciona e quais hipóteses exigem testes no banco de produção.

## Matriz de evidências do laboratório

| Cenário | Provider | Evidência automatizada | Limitação |
| --- | --- | --- | --- |
| FullJoin e órfãos de ambos os lados | SQLite relacional | FULL JOIN no SQL, uma consulta, três resultados | SQLite requer FULL OUTER JOIN (3.39+) |
| MaxByAsync/MinByAsync | SQLite relacional | primeiro/último por contagem de itens e ORDER BY | empates exigem desempate explícito no domínio |
| GroupBy por navegação de referência | SQLite relacional | JOIN, GROUP BY, SUM e totais | SQL diferente em outros provedores |
| string.Join ordenado | SQLite | group_concat e ordenação dos nomes | SQLite 3.44+ |
| Split query com referência e coleção | SQLite | dois comandos; join de Categoria ausente no segundo | não é benchmark ou teste de consistência concorrente |
| JsonPathExists | SQL Server | SQL gerado contém JSON_PATH_EXISTS | **sem executar em SQL Server**; exige SQL Server 2022+ |
| MigrateAsync sem migrations | SQLite | exceção no EF Core 11 | não ensina a aplicar migrations em produção |

O código está em
[NovidadesEfCore11.cs](../../exemplos/c08-c09-ef-core/NovidadesEfCore11.cs)
e os testes em
[NovidadesEfCore11Tests.cs](../../exemplos/c08-c09-ef-core/NovidadesEfCore11Tests.cs).
O [README do laboratório](../../exemplos/c08-c09-ef-core/README.md)
explica como executar.

## O que muda na tradução SQL

### FullJoin, extremos e agrupamento

O operador LINQ \`FullJoin\` do .NET 11 pode ser traduzido pelo EF Core 11
para \`FULL JOIN\` no banco. O exemplo concilia duas fontes independentes
sem FK para expor a diferença entre um par completo e os órfãos de
cada lado. Não confundir com \`FullJoin\` em memória do capítulo C03.

\`MaxByAsync\` e \`MinByAsync\` selecionam a **entidade** com valor
extremo de uma chave. Em consultas relacionais, o provider ordena a
chave no SQL e limita a quantidade de linhas. Um \`Max()\` tradicional
seleciona apenas o maior escalar, o que é uma operação diferente.
Empates devem ser tratados com uma ordenação determinística quando
a identidade do vencedor afeta o negócio.

\`GroupBy\` com navegações ganhou traduções e correções na versão 11.
O exemplo conta e soma itens agrupados pelo status do pedido,
usando a navegação \`ItemPedido.Pedido\`. Testar SQL e resultado é
mais confiável do que afirmar que qualquer \`GroupBy\` ficou mais rápido.

### group_concat e ordenação

No provider SQLite, \`string.Join\` com \`OrderBy\` sobre uma coleção
relacionada é traduzido para \`group_concat\` com ordenação dentro da
agregação. Isso depende da biblioteca SQLite 3.44.0 ou superior. A
ordem não é garantida por um \`OrderBy\` externo sobre o resultado.
Confirme a versão nativa efetivamente carregada no ambiente de CI
antes de atribuir divergências ao LINQ.

### Joins de referência em split queries

Com \`AsSplitQuery()\`, o EF11 elimina joins redundantes de
navegações de referência nas consultas separadas de coleções.
O laboratório usa \`Pedido.Categoria\` como referência e
\`Pedido.Itens\` como coleção: a primeira instrução consulta Categoria,
a segunda consulta os Itens sem incluir novamente Categoria.

A melhora de SQL **não garante** melhora universal de latência.
Mensure round trips, cardinalidade, plano, CPU e bytes transferidos.
A operação continua sujeita a inconsistência entre instruções na
presença de modificações concorrentes.

### JSON_PATH_EXISTS no SQL Server

\`EF.Functions.JsonPathExists\` traduz para \`JSON_PATH_EXISTS\`
no SQL Server 2022 ou superior. O teste usa \`ToQueryString()\`:
não abre conexão e não comprova execução no servidor. Para um
serviço real, validar a versão e o nível de compatibilidade, dados JSON
válidos, índices, nulos e plano de execução.

## Breaking changes que exigem atenção

| Mudança | Risco | Conduta de atualização |
| --- | --- | --- |
| \`UseSqlServer\` passa a usar nível 160 por padrão | SQL gerado incompatível com SQL Server 2019 ou bases com nível menor | identificar versão/compatibilidade real; se necessário, configurar \`UseCompatibilityLevel(150)\` |
| \`Microsoft.Data.SqlClient\` 7.0 separa dependências Entra ID | falha de autenticação em cenários Active Directory | adicionar \`Microsoft.Data.SqlClient.Extensions.Azure\` quando a autenticação utilizada exigir |
| \`MigrateAsync\` sem migrations agora falha | inicialização/deploy antes silencioso passa a abortar | verificar assembly e pipeline de migrations; não silenciar sem justificativa |
| Split queries detectam correlação impossível após alteração concorrente | \`DbQueryConcurrencyException\` chega à aplicação | retry com limite e idempotência ou isolamento transacional apropriado |
| \`Property\` não configura mais coleções primitivas da mesma maneira | erro/configuração do modelo | migrar configuração explícita para \`PrimitiveCollection\` |
| \`EFOptimizeContext\` removida | geração não ocorre no pipeline esperado | avaliar \`EFScaffoldModelStage\` e \`EFPrecompileQueriesStage\` |
| Cosmos deixa de preservar propriedades JSON não mapeadas | perda de dados no round-trip de documentos | mapear campos necessários ou usar SDK diretamente |
| Cosmos remove I/O síncrono | falhas ao chamar APIs síncronas | migrar chamadas para APIs assíncronas |
| SQLite 11 deixa de suportar .NET Framework | projetos legados não podem atualizar provider | migrar para .NET compatível ou permanecer temporariamente em linha anterior |

Outras mudanças em Cosmos, coleções JSON, tipos complexos e recursos
experimentais também merecem revisão se forem usados pelo sistema. Esta
lista não substitui o inventário completo das notas oficiais.

## Checklist antes de atualizar um sistema real

1. Fixar versões do SDK, runtime, EF Core e providers; evitar misturar
   majors incompatíveis. Inventariar SqlClient, SQLite e extensões usadas.
2. Levantar versão do banco, compatibilidade, tipos JSON, índices e
   autenticação. Em SQL Server, não presumir nível de compatibilidade 160.
3. Gerar e revisar SQL para as consultas críticas, principalmente as que
   usam \`Include\`, \`AsSplitQuery\`, agregações, ordenação e filtros JSON.
4. Revisar migrations em código e em banco, conferir schema e ensaiar
   rollback. \`EnsureCreated\` não é substituto de migrations.
5. Rodar testes de integração no **mesmo provider de produção**,
   com dados de fronteira, concorrência, cancelamento e falhas.
6. Observar planos, métricas, regressões de latência e diagnósticos antes
   e depois; não afirmar ganhos só a partir de mudanças na string SQL.

## Referências primárias

- [What's new in EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)
- [Breaking changes do EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/breaking-changes)
- [Funções traduzidas pelo provider SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/functions)
- [Documentação SQL Server de JSON_PATH_EXISTS](https://learn.microsoft.com/en-us/sql/t-sql/functions/json-path-exists-transact-sql)
