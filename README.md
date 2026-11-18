# Engenharia .NET

Base pública de conhecimento sobre engenharia de software no ecossistema .NET,
escrita em português e projetada para consulta direta no GitHub.

O projeto organiza fundamentos, práticas e decisões de engenharia que ajudam a
desenvolver, evoluir e operar sistemas .NET. O foco está em construir modelos
mentais, explicitar trade-offs e conectar escolhas técnicas a problemas reais —
não em oferecer respostas prontas para entrevistas ou um curso linear.

## Propósito e público-alvo

Este guia é destinado a pessoas que desenvolvem, projetam, revisam ou operam
sistemas com C# e .NET. Ele pode ser usado tanto para consolidar fundamentos
quanto para apoiar decisões em aplicações, dados, arquitetura, sistemas
distribuídos, qualidade, segurança e operação.

O leitor deve conseguir entrar por um problema concreto, entender os mecanismos
relevantes, comparar alternativas e identificar como verificar a decisão.

## Filosofia editorial

- Conteúdo autoral exclusivamente em português, com termos técnicos em inglês
  explicados quando necessário.
- Recomendações contextualizadas por restrições, benefícios, custos, riscos e
  alternativas mais simples.
- Um capítulo canônico para cada conceito, conectado aos demais por links.
- Afirmações técnicas sustentadas preferencialmente por documentação oficial,
  especificações e fontes primárias verificadas.
- Exemplos que demonstram comportamento ou decisões, sem complexidade
  ornamental.
- Evolução incremental: arquivos são criados quando há conteúdo útil, não para
  simular capítulos ainda não escritos.

As regras completas estão no [guia de estilo editorial](docs/guia-de-estilo.md).
O [planejamento editorial](docs/planejamento/mapa-editorial.md) registra o escopo,
as dependências e a ordem de implementação dos capítulos.

## Como utilizar o guia

Há três formas recomendadas de navegação:

1. Consulte o [índice central](docs/indice.md) para escolher uma área.
2. Entre pelo índice de uma área para conhecer seus capítulos e a ordem
   sugerida.
3. Use os pré-requisitos e capítulos relacionados para formar uma trilha própria
   conforme o problema que deseja resolver.

O mapa não impõe leitura sequencial. Os índices de área oferecem uma visão
compacta do conteúdo publicado e de sua ordem editorial.

## Áreas de conhecimento

- [Linguagem e runtime](docs/linguagem-e-runtime/indice.md)
  — tipos, nulabilidade, coleções, LINQ, memória, assincronismo, concorrência e
  performance.
- [Aplicações e dados](docs/aplicacoes-e-dados/indice.md)
  — ASP.NET Core, APIs HTTP, Entity Framework Core e
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

## Índice completo

| Área | Capítulos |
|---|---|
| Linguagem e runtime | [C01](docs/linguagem-e-runtime/c01-tipos-identidade-modelagem.md) · [C02](docs/linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md) · [C03](docs/linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md) · [C04](docs/linguagem-e-runtime/c04-memoria-runtime-performance.md) · [C05](docs/linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md) |
| Aplicações e dados | [C06](docs/aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md) · [C07](docs/aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md) · [C08](docs/aplicacoes-e-dados/c08-consultas-ef-core.md) · [C09](docs/aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) · [C10](docs/aplicacoes-e-dados/c10-fundamentos-persistencia-sql-nosql.md) |
| Domínio e arquitetura | [C11](docs/dominio-e-arquitetura/c11-entidades-value-objects-agregados.md) · [C12](docs/dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md) · [C13](docs/dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md) · [C14](docs/dominio-e-arquitetura/c14-cqrs-modelos-orientados-uso.md) · [C19](docs/dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md) |
| Sistemas distribuídos | [C15](docs/sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md) · [C16](docs/sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md) · [C17](docs/sistemas-distribuidos/c17-consistencia-eventual-sagas.md) · [C18](docs/sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md) |
| Qualidade e segurança | [C23](docs/qualidade-e-seguranca/c23-testes-comportamento-risco.md) · [C24](docs/qualidade-e-seguranca/c24-integracao-real-testes-legado.md) · [C25](docs/qualidade-e-seguranca/c25-cobertura-e2e-carga.md) · [C26](docs/qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md) |
| Operação, entrega e plataforma | [C27](docs/operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md) · [C28](docs/operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md) · [C29](docs/operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md) · [C20](docs/operacao-entrega-e-plataforma/c20-infraestrutura-como-codigo-drift.md) |
| IA e prática profissional | [C21](docs/ia-e-pratica-profissional/c21-fundamentos-llm-mcp-rag.md) · [C22](docs/ia-e-pratica-profissional/c22-agentes-orquestracao-sdd.md) · [C30](docs/ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md) |
| Estudos de caso | [C31](docs/estudos-de-caso/c31-pagamentos-operacoes-duplicadas.md) · [C32](docs/estudos-de-caso/c32-latencia-apos-deploy.md) · [C33](docs/estudos-de-caso/c33-legado-baixa-testabilidade.md) · [C34](docs/estudos-de-caso/c34-migracao-gradual-monolito.md) · [C35](docs/estudos-de-caso/c35-pipeline-dependencia-vulneravel.md) |

O [índice central](docs/indice.md) apresenta os títulos completos e a
documentação de apoio.

## Níveis de conhecimento

- **Fundamental:** estabelece vocabulário, modelos mentais e contratos usados
  por outros capítulos. Não pressupõe experiência com a técnica específica.
- **Intermediário:** combina fundamentos explícitos para resolver problemas
  recorrentes de produção.
- **Avançado:** discute forças concorrentes, falhas parciais, custo operacional e
  decisões orientadas por evidência. O nível não justifica complexidade gratuita.

Cada capítulo declara seu nível e seus pré-requisitos. A dificuldade decorre do
problema tratado, não da quantidade de padrões ou ferramentas citados.

## Exemplos de código

Trechos em C# devem ser pequenos, verificáveis e coerentes com a versão de .NET
declarada pelo repositório. Quando um comportamento exige compilação, testes,
benchmark ou infraestrutura, o exemplo é mantido como laboratório isolado e
reproduzível. Os projetos existentes correspondem a necessidades didáticas dos
capítulos publicados.

A convenção completa está na
[estratégia para exemplos compiláveis](docs/exemplos-compilaveis.md).

## Contribuição

Contribuições são bem-vindas. Antes de propor um capítulo, correção técnica ou
exemplo, leia o [guia de contribuição](CONTRIBUTING.md), o
[mapa editorial](docs/planejamento/mapa-editorial.md) e o
[guia de estilo](docs/guia-de-estilo.md).

## Licença

O conteúdo deste repositório é disponibilizado sob a [licença MIT](LICENSE).
