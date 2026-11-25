# Engenharia .NET

[![CI](https://github.com/rodri-oliveira-dev/engenharia-dotnet/actions/workflows/validar-documentacao.yml/badge.svg?branch=main)](https://github.com/rodri-oliveira-dev/engenharia-dotnet/actions/workflows/validar-documentacao.yml)
![.NET 11](https://img.shields.io/badge/.NET-11.0-512BD4?logo=dotnet&logoColor=white)
![C# 15](https://img.shields.io/badge/C%23-15-239120)
![Português](https://img.shields.io/badge/idioma-pt--BR-blue)
[![Licença MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-green)](LICENSE)

Trabalho com C# e .NET há bastante tempo e sei que algumas dúvidas merecem mais do que uma resposta rápida. Criei este espaço para reunir explicações sobre a linguagem, o runtime e as decisões de engenharia que aparecem quando desenvolvemos, evoluímos e mantemos sistemas.

A ideia não é decorar padrões ou acumular respostas prontas para entrevistas. É entender o que acontece por trás do código, quais alternativas existem, onde cada escolha pode falhar e o que vale a pena testar ou medir.

Todo o conteúdo está em português e pode ser consultado diretamente pelo GitHub. Você não precisa ler tudo em sequência: pode começar pelo assunto que faz sentido para você agora.

## Por onde começar

Se você chegou com uma dúvida específica, vá direto ao assunto. Se quiser explorar, os índices ajudam a encontrar uma sequência de leitura:

1. Abra o [índice central](docs/indice.md) para ter uma visão de todos os temas.
2. Escolha uma área e consulte os capítulos disponíveis.
3. Siga os pré-requisitos e os links entre assuntos quando quiser se aprofundar.

Os capítulos indicam o nível de conhecimento e os pré-requisitos. Assim, quem está começando consegue construir uma base e quem já trabalha com .NET pode usar o repositório como material de consulta.

## O que você vai encontrar

Os assuntos vão dos fundamentos de C# a problemas de arquitetura e operação. Cada área tem seu próprio índice:

- [Linguagem e runtime](docs/linguagem-e-runtime/indice.md)
  — tipos, nulabilidade, coleções, LINQ, memória, assincronismo, concorrência e
  performance.
- [Aplicações e dados](docs/aplicacoes-e-dados/indice.md)
  — ASP.NET Core, APIs HTTP, Entity Framework Core,
  [arquitetura de cache](docs/aplicacoes-e-dados/c36-arquitetura-cache-consistencia.md) e
  [fundamentos de persistência SQL e NoSQL](docs/aplicacoes-e-dados/c10-fundamentos-persistencia-sql-nosql.md).
- [Domínio e arquitetura](docs/dominio-e-arquitetura/indice.md)
  — DDD estratégico, [entidades, value objects e agregados](docs/dominio-e-arquitetura/c11-entidades-value-objects-agregados.md),
  [serviços e integração entre modelos](docs/dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md) e
  [documentação arquitetural viva](docs/dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md).
- [Sistemas distribuídos](docs/sistemas-distribuidos/indice.md)
  — comunicação, entrega confiável, consistência e
  [evolução de monólitos para microserviços](docs/sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md).
- [Qualidade e segurança](docs/qualidade-e-seguranca/indice.md)
  — [testes orientados a comportamento e risco](docs/qualidade-e-seguranca/c23-testes-comportamento-risco.md),
  [integração real e evolução segura de legado](docs/qualidade-e-seguranca/c24-integracao-real-testes-legado.md),
  [cobertura, E2E e carga](docs/qualidade-e-seguranca/c25-cobertura-e2e-carga.md) e
  [segurança nas fronteiras da aplicação](docs/qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md).
- [Operação, entrega e plataforma](docs/operacao-entrega-e-plataforma/indice.md)
  — [telemetria e diagnóstico distribuído](docs/operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md),
  [confiabilidade e resposta a incidentes](docs/operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md),
  [entrega compatível e gestão de risco](docs/operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md)
  e [infraestrutura como código e controle de
  drift](docs/operacao-entrega-e-plataforma/c20-infraestrutura-como-codigo-drift.md).
- [IA e prática profissional](docs/ia-e-pratica-profissional/indice.md)
  — [fundamentos de LLM, MCP e RAG](docs/ia-e-pratica-profissional/c21-fundamentos-llm-mcp-rag.md)
  e [agentes, orquestração e desenvolvimento orientado por
  especificações](docs/ia-e-pratica-profissional/c22-agentes-orquestracao-sdd.md),
  além de [decisão, revisão e maturidade técnica](docs/ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md).
- [Estudos de caso](docs/estudos-de-caso/indice.md)
  — problemas de produção analisados a partir do contexto, das opções e das consequências.

## Todos os capítulos

| Área | Capítulos |
|---|---|
| Linguagem e runtime | [C01](docs/linguagem-e-runtime/c01-tipos-identidade-modelagem.md) · [C02](docs/linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md) · [C03](docs/linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md) · [C04](docs/linguagem-e-runtime/c04-memoria-runtime-performance.md) · [C05](docs/linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md) |
| Aplicações e dados | [C06](docs/aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md) · [C07](docs/aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md) · [C08](docs/aplicacoes-e-dados/c08-consultas-ef-core.md) · [C09](docs/aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) · [C10](docs/aplicacoes-e-dados/c10-fundamentos-persistencia-sql-nosql.md) · [C36](docs/aplicacoes-e-dados/c36-arquitetura-cache-consistencia.md) |
| Domínio e arquitetura | [C11](docs/dominio-e-arquitetura/c11-entidades-value-objects-agregados.md) · [C12](docs/dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md) · [C13](docs/dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md) · [C14](docs/dominio-e-arquitetura/c14-cqrs-modelos-orientados-uso.md) · [C19](docs/dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md) |
| Sistemas distribuídos | [C15](docs/sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md) · [C16](docs/sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md) · [C17](docs/sistemas-distribuidos/c17-consistencia-eventual-sagas.md) · [C18](docs/sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md) |
| Qualidade e segurança | [C23](docs/qualidade-e-seguranca/c23-testes-comportamento-risco.md) · [C24](docs/qualidade-e-seguranca/c24-integracao-real-testes-legado.md) · [C25](docs/qualidade-e-seguranca/c25-cobertura-e2e-carga.md) · [C26](docs/qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md) |
| Operação, entrega e plataforma | [C27](docs/operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md) · [C28](docs/operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md) · [C29](docs/operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md) · [C20](docs/operacao-entrega-e-plataforma/c20-infraestrutura-como-codigo-drift.md) |
| IA e prática profissional | [C21](docs/ia-e-pratica-profissional/c21-fundamentos-llm-mcp-rag.md) · [C22](docs/ia-e-pratica-profissional/c22-agentes-orquestracao-sdd.md) · [C30](docs/ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md) |
| Estudos de caso | [C31](docs/estudos-de-caso/c31-pagamentos-operacoes-duplicadas.md) · [C32](docs/estudos-de-caso/c32-latencia-apos-deploy.md) · [C33](docs/estudos-de-caso/c33-legado-baixa-testabilidade.md) · [C34](docs/estudos-de-caso/c34-migracao-gradual-monolito.md) · [C35](docs/estudos-de-caso/c35-pipeline-dependencia-vulneravel.md) |

Para ver os títulos completos e a documentação de apoio, consulte o
[índice central](docs/indice.md).

## Como escrevo os conteúdos

Procuro explicar os conceitos com a mesma preocupação que teria ao discutir uma solução com outra pessoa do time: clareza, contexto e espaço para questionar as escolhas.

Na prática, isso significa:

- Começar pelo problema e mostrar por que o assunto importa.
- Comparar alternativas, benefícios, limitações e **trade-offs**, sem tratar uma técnica como resposta universal.
- Usar exemplos que ajudem a observar um comportamento ou entender uma decisão, sem complexidade desnecessária.
- Priorizar documentação oficial, especificações e outras fontes primárias para sustentar afirmações técnicas.
- Manter cada conceito em um capítulo de referência e conectar os assuntos por links, em vez de repetir a mesma explicação.
- Publicar conteúdo aos poucos, conforme os capítulos ficam úteis de verdade, sem criar páginas vazias só para completar o índice.

Esses critérios estão detalhados no [guia de estilo editorial](docs/guia-de-estilo.md). O [mapa editorial](docs/planejamento/mapa-editorial.md) mostra os temas previstos e como eles se relacionam.

## Níveis dos capítulos

Cada capítulo indica seu nível e os conhecimentos que ajudam a acompanhá-lo:

- **Fundamental:** apresenta conceitos e comportamentos importantes, mesmo que você ainda não tenha trabalhado com o tema.
- **Intermediário:** conecta esses fundamentos a problemas comuns no desenvolvimento e na manutenção de aplicações.
- **Avançado:** discute decisões com mais variáveis, como falhas parciais, concorrência, custo operacional e riscos de produção.

Avançado não significa mais padrões ou mais ferramentas. Muitas vezes, a melhor decisão técnica é justamente manter a solução simples.

## Roadmap de estudos e livros em português

O [roadmap de estudos por maturidade](docs/roadmap-de-estudos.md) organiza 14
livros de C#, qualidade, DDD, arquitetura e sistemas distribuídos em cinco etapas,
dos fundamentos às decisões arquiteturais mais complexas. Cada obra tem um link de busca na Amazon Brasil ou para o site oficial do
Refactoring.Guru, indicação dos capítulos relacionados e uma prática
com critérios para avaliar o aprendizado.

A ordem é uma sugestão de aprofundamento, não um requisito de senioridade. Quem
já domina um assunto pode começar pela atividade prática da etapa correspondente.

## Código e exemplos

Quando o código ajuda a explicar um conceito, uso exemplos pequenos em C#, com comportamento verificável e compatíveis com a versão de .NET indicada no repositório.

Nem tudo cabe em um trecho de Markdown. Quando o assunto exige compilação, testes, benchmarks ou infraestrutura, a ideia é ter um laboratório separado e reproduzível. Os exemplos são adicionados conforme a necessidade dos capítulos, não apenas para aumentar a quantidade de projetos.

Os detalhes estão na [estratégia para exemplos compiláveis](docs/exemplos-compilaveis.md).
Os exemplos deste repositório usam **.NET 11 / C# 15** e são validados por
build, testes e auditoria de dependências no GitHub Actions. O
[guia de contribuição](CONTRIBUTING.md#validar-dependências-e-executar-o-ci-localmente)
explica como executar os mesmos comandos localmente e interpretar o gate de
vulnerabilidades.

## Qualidade editorial e ortografia

O pipeline possui uma etapa de [validação ortográfica em pt-BR](docs/ortografia.md)
com CSpell, dicionário brasileiro e [vocabulário técnico](.cspell/termos-tecnicos.txt).
Nos PRs, são verificadas somente as linhas novas de prosa Markdown, sem
bloquear mudanças por erros anteriores. A checagem não substitui revisão
gramatical nem valida exemplos de código.

## Compatibilidade e evolução do .NET 11

Além dos capítulos de fundamentos, o [guia de compatibilidade e breaking changes](docs/compatibilidade/indice.md)
reúne as mudanças do ASP.NET Core 11, runtime/BCL/Hosting, SDK/NuGet/containers,
C# 15 e EF Core 11. Cada trilha diferencia incompatibilidades de código-fonte,
binárias e comportamentais e apresenta testes e critérios de migração.

## Entity Framework Core 11

Os capítulos [Consultas eficientes com EF Core](docs/aplicacoes-e-dados/c08-consultas-ef-core.md)
e [Fronteiras e concorrência](docs/aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md)
têm exemplos compiláveis de EF Core 11: `FullJoin` relacional,
`MaxByAsync`/`MinByAsync`, `GroupBy` com navegação, `group_concat`
ordenado e split queries. O laboratório valida SQLite real e a tradução
SQL Server de `JsonPathExists`. Os riscos de atualização constam do
[guia de compatibilidade do EF Core 11](docs/aplicacoes-e-dados/ef-core-11-compatibilidade.md).

## Testes

Os capítulos de [Qualidade e segurança](docs/qualidade-e-seguranca/indice.md) abordam estratégia de testes, integração e cobertura. Para explorar esses conceitos em um projeto executável, consulte o laboratório [Pirâmide de testes e cobertura em .NET](https://github.com/rodri-oliveira-dev/dotnet-code-coverage).

O projeto demonstra testes unitários, de integração HTTP e de ponta a ponta (E2E), além de métricas de cobertura, *mutation testing* e validações automatizadas de qualidade (*quality gates*) no CI. A proposta é mostrar como combinar camadas de teste e interpretar métricas sem tratar cobertura alta como sinônimo de testes eficazes.

## Acessibilidade

O [guia de acessibilidade em aplicações .NET](docs/qualidade-e-seguranca/acessibilidade-aplicacoes-dotnet.md)
explora requisitos de qualidade, ASP.NET Core, Blazor e estratégias de testes.
A [declaração de acessibilidade](ACCESSIBILITY.md) descreve os objetivos e
limitações do próprio material educacional.

## Observabilidade

Os capítulos de [Operação, entrega e plataforma](docs/operacao-entrega-e-plataforma/indice.md) abordam telemetria, rastreamento distribuído e diagnóstico de sistemas. Para acompanhar esses conceitos em um ambiente executável, consulte o laboratório [Observabilidade em sistemas distribuídos com .NET](https://github.com/rodri-oliveira-dev/dotnet-observability-lab).

O projeto utiliza .NET 10, .NET Aspire e OpenTelemetry para explorar *traces*, logs estruturados e métricas em APIs e workers de processamento assíncrono. Também apresenta cenários de falha, duplicidade e recuperação em um fluxo com RabbitMQ, PostgreSQL e padrões Outbox/Inbox, relacionando os sinais de telemetria ao comportamento do sistema e às decisões de arquitetura.

## Quer contribuir?

Encontrou um erro, acha que algum trecho poderia ser mais claro ou quer sugerir um tema? Correções, discussões técnicas e contribuições são bem-vindas.

Antes de abrir um pull request, dê uma olhada no [guia de contribuição](CONTRIBUTING.md), no [mapa editorial](docs/planejamento/mapa-editorial.md) e no [guia de estilo](docs/guia-de-estilo.md). Eles ajudam a manter os capítulos conectados e as explicações consistentes.

## Licença

O conteúdo deste repositório é disponibilizado sob a [licença MIT](LICENSE).
