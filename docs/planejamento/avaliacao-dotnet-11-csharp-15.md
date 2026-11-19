# Avaliação técnica: .NET 11 e C# 15

> **Data:** 18 de novembro de 2026
>
> **Contexto:** PR #9, migração de `net10.0` para `net11.0` com SDK
> `11.0.100`. Este documento é um levantamento de oportunidades, não uma
> afirmação de que todas as funcionalidades abaixo já foram implementadas.

## Objetivo e critério de decisão

O Engenharia .NET serve como material de estudo de engenharia de software,
não como catálogo indiscriminado de sintaxe nova. Uma novidade só deve ser
adotada quando demonstrar pelo menos um benefício verificável: melhorar um
contrato, explicitar uma invariante, reduzir trabalho ou alocação medidos, ou
ensinar uma decisão com alternativas e limitações.

**Prioridade:** P0 = risco de regressão/contrato a verificar primeiro;
P1 = oportunidade educacional forte e aplicável; P2 = oportunidade
condicional; P3 = registrar ou postergar. **Complexidade** é uma estimativa
de implementação com testes e explicação, não apenas da alteração de código.
**Ganho** indica benefício esperado, não melhoria de desempenho medida.

## Estado verificado

- O PR #9 já fixa o SDK `11.0.100` em `global.json` e `net11.0` em
  `exemplos/Directory.Build.props`.
- Versões do ASP.NET Core e do EF Core usadas nos exemplos foram alinhadas
  à linha 11. A configuração de NuGet permanece centralizada em
  `exemplos/Directory.Packages.props`.
- A execução [44425625140](https://github.com/rodri-oliveira-dev/engenharia-dotnet/actions/runs/44425625140)
  aprovou 117 testes em 15 projetos, sem falhas ou testes ignorados, com
  zero avisos e erros de compilação. O ajuste de precisão dos testes de frete
  consta do próprio PR.
- Resultado dos testes não substitui testes direcionados a mudanças de
  comportamento introduzidas pelo framework.

## Matriz de aplicabilidade

| Tema e novidade | Ponto existente no repositório | Ganho e teste proposto | Prioridade | Complexidade |
|---|---|---|---|---|
| **ASP.NET Core 11: filtros após falha de binding** | `Pipeline.cs` e `AplicacaoTests.cs` (C06/C07) | **Implementado:** filtro de paginação observa status 400 antes de acessar argumentos; testes HTTP cobrem query inválida/ausente e leitura de JSON inválida (que não executa filtros). | **P0** | Baixa/média |
| **C# 15: tipos de união** | C02, C12, C14; resultados de aplicação e possíveis respostas HTTP | **Modelagem:** distinguir sucesso, falha de negócio e entrada inválida sem exceções de fluxo. Testar correspondência exaustiva, serialização e contrato HTTP; não converter todas as APIs indiscriminadamente. | **P1** | Média |
| **C# 15: hierarquias fechadas (`closed`)** | C01, C11 e `exemplos/c11-c12-ddd/` | **Segurança de modelagem:** exemplificar conjunto finito de estados/eventos com `switch` exaustivo. Testar transições válidas e avaliar efeito sobre extensibilidade entre assemblies. | **P1** | Média |
| **LINQ .NET 11: `FullJoin`** | C03 e `exemplos/c03-colecoes-linq/` | **Didática:** comparar inner, left e full join em memória com casos sem correspondência em ambos os lados. Testar valores nulos, cardinalidade e duplicatas. EF Core 11 também pode traduzir `FullJoin` em SQL; validar `ToQueryString()` e resultados no provider SQLite antes de ensinar o caso relacional. | **P1** | Baixa/média |
| **C# 15: argumentos em expressões de coleção (`with(...)`)** | C03, `BuscarClientesPermitidos`; C04 | **Clareza e controle:** demonstrar comparer em `HashSet` e capacidade em `List`. Testar deduplicação case-insensitive; discutir alocação sem alegar ganho sem benchmark. | **P1** | Baixa |
| **EF Core 11: SQL mais enxuto e melhorias em `GroupBy`** | C08, `Consultas.cs` e `PersistenciaTests.cs` | **Diagnóstico:** comparar SQL gerado para cenários de inclusão, referência e agrupamento. Usar `ToQueryString()` e medir quando relevante; nem toda otimização da documentação se manifesta no modelo atual. | **P2** | Média |
| **EF Core 11 + SQLite: ordenação em `group_concat`** | C08, contexto SQLite dos testes | **Tradução SQL:** criar caso com `string.Join` e `OrderBy` em coleção relacionada, validar SQL e sequência resultante. Requer SQLite **3.44+** e teste do provider usado no CI. | **P2** | Média |
| **.NET 11: validação assíncrona em DataAnnotations/Minimal APIs** | C06/C07, `Aplicacao.cs`, validação de opções e requisições | **I/O não bloqueante:** exemplo de validação de unicidade ou regra externa com cancelamento. Separar validação estrutural de política de negócio; evitar I/O remoto real nos testes. | **P2** | Média/alta |
| **.NET 11: runtime async, JIT e melhorias de alocação** | C04 e C05, benchmarks de parsing e fluxos com `Task`/`ValueTask` | **Observabilidade de performance:** estabelecer baseline .NET 10 vs. 11 em mesmo hardware e configuração, medir alocação, throughput, latência e warm-up. Melhorias automáticas não autorizam refatorações sem perfil. | **P2** | Alta |
| **ASP.NET Core 11: CSRF automático** | C06 e C26 | **Segurança:** verificar endpoints que aceitam formulário, cookies e origem. O `MapPost` JSON atual não recebe a mesma mudança de comportamento; documentar sem habilitar/desabilitar proteção indiscriminadamente. | **P2** | Média |
| **Observabilidade: métricas nativas e atributos de trace HTTP** | C27/C28, `TelemetriaAplicacao.cs` | **Custo operacional:** verificar duplicidade de instrumentação de métricas e atributos, contratos de dashboards e cardinalidade antes de alterar pipeline. | **P2** | Média |
| **C# 15: indexadores de extensão** | C03 | **Uso condicional:** explicar conveniência e custo oculto de `ElementAt` em `IEnumerable<T>`. Não introduzir indexador que faça varredura O(n) parecendo acesso direto O(1). | **P3** | Baixa |
| **C# 15: `break`/`continue` com rótulos** | C03 e C05 | **Legibilidade situacional:** útil para loops aninhados existentes; não há caso convincente para inserir a sintaxe artificialmente nos exemplos atuais. | **P3** | Baixa |
| **C# 15: segurança de memória (preview)** | C04 | **Pesquisa apenas:** sem `unsafe` no escopo didático identificado. Não ativar `LangVersion=preview` nem flags experimentais no repositório inteiro. | **P3** | Alta |
| **.NET 11: template `dotnet new mcpserver`** | C21/C22, servidor MCP stdio existente | **Tooling:** documentar disponibilidade do template e comparar o resultado gerado, sem recriar o laboratório funcional ou adicionar dependências desnecessárias. | **P3** | Baixa |

## Decisões específicas

### P0: cobertura de regressão HTTP

Em ASP.NET Core 11, falhas de parsing em rota, query string e headers
podem chegar ao endpoint filter, que deve observar o status HTTP antes de
acessar argumentos parcialmente vinculados. O filtro específico da paginação
C06/C07 agora responde com Problem Details preservando o HTTP 400.

A hipótese inicial sobre `ValidarProdutoFilter` precisa de uma distinção:
falhas de **leitura do body** não executam endpoint filters. Os testes
de `POST /produtos` cobrem JSON malformado, tipo incompatível, body ausente
e mídia não suportada. Os primeiros testes detectaram um defeito real:
o `TratadorGlobalDeExcecoes` mapeava `BadHttpRequestException` para HTTP 500.
O tratamento foi ajustado para preservar `StatusCode` e devolver Problem
Details sem expor detalhes internos. A validação de campos do produto
permaneceu intacta. A implementação também demonstra o comportamento
documentado em `GET /produtos` sem alterar contratos de domínio.

### P1: exemplos pequenos em vez de migração cosmética

- **Tipos de união e hierarquias fechadas:** escolher um caso de domínio
  representativo, documentar alternativas (`Result<T>` próprio, herança
  tradicional ou exceções) e testar exaustividade/serialização.
- **`FullJoin`:** limitar o exemplo inicial a coleções em memória. O
  EF Core 11 traduz `FullJoin` em `FULL JOIN` em provedores relacionais;
  SQLite suporta `FULL OUTER JOIN` desde a versão 3.39.0. O exercício
  relacional deve validar a versão de SQLite, a tradução gerada e os resultados.
- **Expressões de coleção com argumentos:** comparar com construtores
  explícitos e explicar por que `StringComparer.OrdinalIgnoreCase` muda
  semântica, enquanto capacidade inicial depende do volume esperado.

### P2: experimentar com prova, não pressupor ganhos

EF Core 11 introduz otimizações de tradução SQL, mas os benefícios
dependem do modelo e da consulta. O capítulo C08 já testa `AsSplitQuery`,
N+1 e compiled queries. Antes de modificar `Consultas.cs`, capturar SQL,
verificar plano e número de comandos. Para `group_concat` ordenado, fixar
ou verificar a versão de SQLite antes de afirmar suporte.

No runtime, comparar benchmarks com SDK/runtime 10 e 11 separadamente,
incluindo aquecimento, número de iterações, ambiente e desvio. O projeto
continuará com `TreatWarningsAsErrors=true`.

## Critérios de aceite para a fase de implementação

1. Resolver ou descartar **com evidência** a hipótese P0 dos filtros HTTP.
2. Escolher no máximo duas a quatro melhorias P1 que acrescentem
   compreensão e comportamento observável, sem reescrever exemplos.
3. Para cada melhoria, apresentar problema, alternativa anterior,
   abordagem atual, restrições e testes determinísticos.
4. Não adicionar pacote, flag experimental ou infraestrutura sem
   necessidade didática verificável.
5. Preservar contratos HTTP e de domínio. Não converter silenciosamente
   API pública ou tipo de retorno só para usar sintaxe recente.
6. Executar restore, build Release (0 warnings/0 errors), testes
   completos e checagens de dependências, Markdown e IaC no PR #9.
7. Atualizar os capítulos C01/C02/C03/C06/C08/C11 relevantes junto
   aos exemplos efetivamente implementados.

## Dívida documental identificada

- `docs/linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md`
  ainda atribui custos de implementação à versão ".NET 10". Rever essa
  redação quando o capítulo receber o exemplo LINQ 11.
- `docs/planejamento/relatorio-final.md` é um registro histórico
  da auditoria de 18/11/2026 e menciona `net10.0`; **não** reescrever
  retrospectivamente um relatório histórico como se validasse .NET 11.
- A referência ao laboratório externo de observabilidade no `README.md`
  descreve corretamente aquele projeto como .NET 10; ele não faz parte
  deste PR e não deve ser atualizado por associação.

## Referências oficiais

- [Novidades no .NET 11](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview)
- [Novidades no C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)
- [Novidades no ASP.NET Core 11](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11)
- [Migração ASP.NET Core 10 para 11](https://learn.microsoft.com/en-us/aspnet/core/migration/100-to-110)
- [Novidades no EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)
- [Breaking changes no EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/breaking-changes)
- [SQLite 3.39.0: suporte a FULL OUTER JOIN](https://www.sqlite.org/releaselog/3_39_0.html)

A documentação oficial fundamenta a existência dos recursos. Sua
aplicabilidade e a prioridade aqui são avaliações específicas deste
repositório, não recomendações universais.
