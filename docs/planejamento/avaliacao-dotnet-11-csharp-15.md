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
- A execução [44429396050](https://github.com/rodri-oliveira-dev/engenharia-dotnet/actions/runs/44429396050)
  aprovou 138 testes em 15 projetos, sem falhas ou testes ignorados, com
  zero avisos e erros de compilação. Inclui os testes adicionados para a
  mudança de binding do ASP.NET Core 11 e os quatro exemplos P1.
- As checagens de Markdown/links internos, segredos, dependências e IaC
  passaram no GitHub Actions. Esse resultado não substitui testes
  direcionados às mudanças de comportamento não cobertas por esses casos.

## Matriz de aplicabilidade

| Tema e novidade | Ponto existente no repositório | Ganho e teste proposto | Prioridade | Complexidade |
|---|---|---|---|---|
| **ASP.NET Core 11: filtros após falha de binding** | `Pipeline.cs` e `AplicacaoTests.cs` (C06/C07) | **Implementado:** filtros preservam HTTP 400 em falhas de binding; testes distinguem JSON malformado, body ausente, literal `null`, mídia incompatível e query inválida. O tratador global preserva `BadHttpRequestException.StatusCode`. | **P0** | Baixa/média |
| **C# 15: tipos de união** | C02, `Cadastro.cs` e `CadastroTests.cs` | **Implementado:** `ResultadoRegistro` distingue cadastro criado e e-mail já cadastrado, com switch exaustivo e round-trip JSON por classificador estrutural. Entrada inválida mantém exceção; nenhum contrato HTTP foi alterado. | **P1** | Média |
| **C# 15: hierarquias fechadas (`closed`)** | C11, `DominioPedidos.cs` e `DominioPedidosTests.cs` | **Implementado:** etapas de separação fechadas, descendentes selados, `switch` exaustivo e testes de transição, sem modificar o agregado existente. | **P1** | Média |
| **LINQ .NET 11: `FullJoin`** | C03, `ColecoesLinq.cs` e testes | **Implementado:** conciliação em memória com clientes/pedidos órfãos, duplicatas, comparação com `Join` e `LeftJoin` e fontes vazias. Não afirma tradução SQL. | **P1** | Baixa/média |
| **C# 15: argumentos em expressões de coleção (`with(...)`)** | C03, `ColecoesLinq.cs` e testes | **Implementado:** comparador OrdinalIgnoreCase no `HashSet` e capacidade inicial da `List`, com testes de igualdade, reserva e ordem, sem alegar performance não medida. | **P1** | Baixa |
| **EF Core 11: SQL mais enxuto e melhorias em `GroupBy`** | C08, `NovidadesEfCore11.cs` e testes | **Implementado no laboratório:** agrupamento por navegação, SQL inspecionado e split query com referência sem join redundante na consulta da coleção. Não há benchmark de ganho de desempenho. | **P2** | Média |
| **EF Core 11 + SQLite: ordenação em `group_concat`** | C08, `NovidadesEfCore11.cs` e testes | **Implementado no laboratório:** `string.Join` com `OrderBy` de coleção relacionada e asserções de SQL/ordem. Requer SQLite **3.44+**. | **P2** | Média |
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

A hipótese inicial sobre `ValidarProdutoFilter` mostrou um defeito real.
Erros de **leitura** de JSON malformado ou mídia não suportada não passam
pelo filter. Porém, corpo ausente ou literal JSON `null` podem levar à
execução do filtro com argumentos não vinculados: o uso anterior de
`Single()` lançava exceção e convertia uma entrada inválida em HTTP 500.
O filtro agora observa o status antes de ler os argumentos e valida a
ausência do DTO sem lançar exceção.

Por precaução, o `TratadorGlobalDeExcecoes` também preserva
`BadHttpRequestException.StatusCode` e retorna Problem Details quando
essa exceção chega à fronteira global. As regras de validação de campos
do produto permanecem intactas. O tratamento específico da paginação
`GET /produtos` demonstra a outra modalidade de falha de binding.

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

### P1: implementação dos quatro exemplos

Os quatro tópicos P1 foram adicionados aos laboratórios existentes, com
testes unitários específicos e explicações nos capítulos C02, C03 e C11:

- União: `ResultadoRegistro`, conversões de casos, switch exaustivo e
  classificação estrutural ao serializar/desserializar JSON.
- Hierarquia fechada: `EtapaSeparacao` e `FluxoSeparacao`, com
  descendentes selados e regras explícitas de transição.
- LINQ: `ConciliarClientesPedidos` usa `FullJoin` em memória para
  preservar correspondências e registros órfãos.
- Expressões de coleção: `with(...)` informa comparer de `HashSet`
  e capacidade inicial de `List`.

Nenhum exemplo substituiu automaticamente os contratos anteriores, e os
ganhos de memória ou tempo de CPU continuam condicionados a medição.

### P2: experimentar com prova, não pressupor ganhos

EF Core 11 introduz otimizações de tradução SQL; o laboratório agora
inclui exemplos direcionados, mas os benefícios de performance continuam
dependendo do modelo e da consulta. O capítulo C08 já testa `AsSplitQuery`,
N+1 e compiled queries. Antes de modificar `Consultas.cs`, capturar SQL,
verificar plano e número de comandos. Para `group_concat` ordenado, fixar
ou verificar a versão de SQLite antes de afirmar suporte.

No runtime, comparar benchmarks com SDK/runtime 10 e 11 separadamente,
incluindo aquecimento, número de iterações, ambiente e desvio. O projeto
continuará com `TreatWarningsAsErrors=true`.

### Extensão didática de EF Core 11

Um laboratório adicional cobre `FullJoin` relacional, `MaxByAsync`,
`MinByAsync`, `GroupBy` com navegação, agregação ordenada do SQLite,
split queries sem join redundante e `JSON_PATH_EXISTS` (somente SQL
gerado). O teste de `MigrateAsync` sem migrations documenta uma
quebra de comportamento. Para migração de sistemas existentes, veja
[compatibilidade EF Core 11](../aplicacoes-e-dados/ef-core-11-compatibilidade.md).

## Guias complementares de compatibilidade

A trilha [Compatibilidade e evolução do .NET](../compatibilidade/indice.md)
organiza os breaking changes de ASP.NET Core 11, runtime/BCL/Hosting,
SDK/NuGet/containers e C# 15. Os guias diferenciam o que é verificável
por testes automatizados no repositório do que requer infraestrutura
real ou comparação de versões. Os documentos oficiais permanecem
a referência primária; a lista de mudanças do .NET 11 não é exaustiva.

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

## Revisão pré-merge do PR #9

A revisão do commit que implementou os P1 confirmou:

- **Escopo:** migração de SDK/TFM, pacotes da linha ASP.NET Core/EF Core 11,
  correções P0 e quatro exercícios P1. Os P2/P3 são oportunidades de pesquisa
  posteriores, não critérios de aceite desta migração.
- **Compatibilidade:** o SDK fica fixado em `11.0.100` com
  `rollForward: latestPatch`, e os projetos usam `net11.0`. Desenvolvedores
  e ambientes de build precisam instalar SDK/runtime compatíveis.
- **Suporte:** .NET 11 é STS, com suporte previsto até novembro de 2028;
  .NET 10 é LTS, também com suporte até novembro de 2028. Para um repositório
  de estudos de recursos recentes, a escolha por 11 é deliberada. Não é uma
  recomendação de migração automática para aplicações de produção.
- **CI:** a execução
  [44429396050](https://github.com/rodri-oliveira-dev/engenharia-dotnet/actions/runs/44429396050)
  comprovou 138/138 testes, build Release sem warnings e quatro jobs aprovados.
  A análise de pacotes não reportou vulnerabilidades nas fontes disponíveis.
- **Limites da cobertura:** o CI executa em `ubuntu-latest`, sem matriz Windows,
  macOS, arquiteturas alternativas ou benchmarks comparativos. O job de
  auditoria agora **reprova** dependências vulneráveis diretas ou transitivas,
  inclusive de severidade baixa. Testes simulados validam a política de
  reprovação; o resultado real depende das fontes NuGet consultadas.
- **Revisão:** não havia reviews humanos registrados na consulta do PR; a
  consulta automatizada às regras de proteção da `main` retornou 403.
  A ausência de problemas na validação técnica não substitui políticas de
  aprovação/revisão aplicáveis ao repositório.

Apenas o registro técnico e a descrição do PR foram normalizados nesta
revisão final. Este relatório não declara novos testes de compatibilidade
multiplataforma nem substitui a avaliação dos P2.

Referência: [ciclo de suporte do .NET](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support).

### Auditoria NuGet bloqueante

O job `Analisar dependências` realiza o restore com `NuGetAudit=true`,
`NuGetAuditMode=all` e `NuGetAuditLevel=low`. Em seguida, executa
`dotnet package list --vulnerable --include-transitive --format json --output-version 1`
e passa o relatório ao `scripts/verificar-vulnerabilidades-nuget.py`.

O verificador reprova pacotes vulneráveis **diretos ou transitivos**
(código 1). Relatório ausente, inválido ou sem projetos também reprova
(código 2). O job executa testes com relatórios simulados que cobrem
achados diretos, transitivos, em outro framework e dados inválidos,
sem instalar pacotes intencionalmente vulneráveis.

Referências: [auditoria NuGet](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
e [relatório JSON da CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-list).

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
