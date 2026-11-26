# Mapa editorial

## Taxonomia

O repositório será organizado por **trilhas de conhecimento**, não pelas categorias da entrevista. Cada capítulo possui um identificador estável, nível, dependências e um escopo que explicita também o que fica de fora.

- **Fundamental:** estabelece vocabulário, modelos mentais e contratos necessários a outros capítulos.
- **Intermediário:** combina fundamentos para resolver problemas recorrentes de produção.
- **Avançado:** trata otimização, sistemas distribuídos, operação ou decisões com múltiplas forças e consequências.
- **Exemplo .NET — sim:** o capítulo deve ter projeto, teste ou benchmark executável.
- **Exemplo .NET — parcial:** snippets verificáveis ou um laboratório pequeno são úteis, mas o assunto não é predominantemente código .NET.
- **Exemplo .NET — não:** diagramas, registros de decisão ou exemplos de configuração comunicam melhor o objetivo.

## Capítulos propostos

| Ordem | ID | Trilha | Capítulo e escopo | Nível | Depende de | Exemplo .NET |
|---:|---|---|---|---|---|---|
| 1 | C01 | Linguagem e runtime | **Tipos, identidade e modelagem em C#** — classes, records, structs, semântica de valor/referência, cópia, mutabilidade e extensão por herança. | Fundamental | — | Sim |
| 2 | C02 | Linguagem e runtime | **Nulabilidade, falhas e exceções** — contratos de ausência, nullable reference types, resultados explícitos, exceções e fronteiras de tratamento. | Fundamental | C01 | Sim |
| 3 | C03 | Linguagem e runtime | **Coleções, LINQ e execução diferida** — escolha de estruturas, iteradores, múltipla enumeração, materialização e streaming. | Fundamental | C01 | Sim |
| 4 | C05 | Linguagem e runtime | **Assincronicidade e concorrência controlada** — `Task`, `ValueTask`, contexto, cancelamento, limites de concorrência e backpressure. | Intermediário | C02, C03 | Sim |
| 5 | C04 | Linguagem e runtime | **Memória, runtime e engenharia de performance** — GC, boxing, buffers, `Span`, JIT/AOT, profiling e otimização baseada em evidência. | Avançado | C01, C03 | Sim |
| 6 | C06 | Aplicações .NET | **Composição e pipeline no ASP.NET Core** — DI e lifetimes, middleware, filters, Options e validação de configuração. | Fundamental | C02 | Sim |
| 7 | C07 | Aplicações .NET | **APIs HTTP e integrações resilientes** — recursos, semântica HTTP, cache distribuído, clientes externos, timeout, retry e cancelamento. | Intermediário | C05, C06 | Sim |
| 8 | C10 | Dados | **Fundamentos de persistência SQL e NoSQL** — critérios de escolha, ACID, índices, transações e locks otimistas/pessimistas. | Fundamental | C01 | Parcial |
| 9 | C08 | Dados | **Consultas eficientes com EF Core** — tracking, projeção, N+1, split queries, SQL gerado, planos e compiled queries. | Intermediário | C03, C10 | Sim |
| 10 | C09 | Dados | **Fronteiras e concorrência no EF Core** — unidade de trabalho, ciclo de vida, `IQueryable`, tokens de concorrência e resolução de conflitos. | Intermediário | C02, C06, C10 | Sim |
| 11 | C11 | Domínio | **Entidades, value objects e agregados** — identidade, igualdade, invariantes, primitive obsession e modelo anêmico. | Fundamental | C01, C02 | Sim |
| 12 | C12 | Domínio | **Serviços de domínio e integração entre modelos** — application/domain services e Anti-Corruption Layer. | Intermediário | C11 | Sim |
| 13 | C13 | Arquitetura | **Dependências, abstrações e padrões com propósito** — regra de dependência, SOLID contextual, custo da indireção e seleção de design patterns. | Fundamental | C01, C11 | Sim |
| 14 | C14 | Arquitetura | **CQRS e modelos orientados ao uso** — separação de comandos e consultas, consistência, complexidade e sinais para evitar. | Avançado | C08, C11, C13 | Sim |
| 15 | C19 | Arquitetura | **Documentação arquitetural viva** — C4 por público e necessidade; ADRs com contexto, alternativas, consequências e supersessão. | Fundamental | C13 | Não |
| 16 | C15 | Sistemas distribuídos | **Comunicação entre serviços e mensageria** — síncrono/assíncrono, acoplamento temporal, ordering, poison messages, retries e DLQ. | Intermediário | C07, C13 | Sim |
| 17 | C16 | Sistemas distribuídos | **Idempotência e entrega confiável** — idempotency keys, consumidores idempotentes, deduplicação, Outbox e circuit breaker no conjunto de resiliência. | Avançado | C09, C10, C15 | Sim |
| 18 | C17 | Sistemas distribuídos | **Consistência eventual e sagas** — transações locais, compensações, orquestração/coreografia e limites operacionais. | Avançado | C11, C16 | Sim |
| 19 | C18 | Sistemas distribuídos | **Limites de microserviços e evolução de monólitos** — autonomia, seleção de padrões, Strangler Fig e prevenção do monólito distribuído. | Avançado | C12, C13, C15 | Parcial |
| 20 | C23 | Qualidade | **Testes orientados a comportamento e risco** — unitário versus integração, dublês, matchers e redução de acoplamento à implementação. | Fundamental | C02, C13 | Sim |
| 21 | C24 | Qualidade | **Integração real e testes incrementais de legado** — Testcontainers, testes de caracterização, seams e cobertura progressiva de fluxos críticos. | Intermediário | C08, C23 | Sim |
| 22 | C26 | Segurança | **Segurança nas fronteiras da aplicação** — fundamentos, identidade, autorização por objeto, APIs, vulnerabilidades, segredos, dados, dependências, pipelines e fronteiras de IA. | Intermediário | C06, C07 | Sim |
| 23 | C27 | Operação | **Telemetria e diagnóstico distribuído** — logs estruturados, métricas, traces, correlação, cardinalidade e proteção de dados. | Intermediário | C07, C15, C26 | Sim |
| 24 | C25 | Qualidade | **Cobertura, E2E e testes de carga** — qualidade de asserts, metas, ambiente, latência, throughput, saturação e limites da cobertura. | Avançado | C07, C23, C27 | Sim |
| 25 | C28 | Operação | **Confiabilidade de serviço e resposta a incidentes** — SLIs, SLOs, error budgets, burn rate, triagem, mitigação e aprendizado. | Avançado | C27 | Parcial |
| 26 | C29 | Entrega | **Entrega compatível e gestão de risco no pipeline** — expand-migrate-contract, contratos, canary/flags, rollback e triagem de vulnerabilidades. | Avançado | C10, C23, C26, C27 | Parcial |
| 27 | C20 | Plataforma | **Infraestrutura como código e controle de drift** — provisionamento versus configuração, estado, revisão, detecção e reconciliação. | Intermediário | C19, C29 | Não |
| 28 | C21 | IA aplicada | **Fundamentos de LLM, MCP e RAG** — contexto, recuperação, permissões, auditoria, avaliação e limites contra alucinação. | Intermediário | C19, C23 | Parcial |
| 29 | C22 | IA aplicada | **Agentes, orquestração e desenvolvimento orientado por especificações** — decomposição, estado, validação, custo e revisão humana. | Avançado | C21, C23 | Parcial |
| 30 | C30 | Prática profissional | **Decisão, revisão e maturidade técnica** — evidências, trade-offs, governança, code review, mentoria, dívida e autonomia progressiva. | Fundamental | C19, C23 | Não |
| 31 | C31 | Estudos de caso | **API de pagamentos com operações duplicadas** — investigação temporal e composição de idempotência, restrição única, transação, Outbox e retries. | Avançado | C09, C16, C27, C28 | Sim |
| 32 | C32 | Estudos de caso | **API com aumento de latência após deploy** — métricas, tracing, rollback, profiling e investigação causal. | Avançado | C04, C27, C28, C29 | Parcial |
| 33 | C33 | Estudos de caso | **Sistema legado com baixa testabilidade** — caracterização, seams, refatoração incremental e mitigação de risco. | Intermediário | C13, C23, C24 | Parcial |
| 34 | C34 | Estudos de caso | **Migração gradual de monólito** — limites, Strangler Fig, contratos, dados e observabilidade durante a convivência. | Avançado | C12, C18, C27, C29 | Parcial |
| 35 | C35 | Estudos de caso | **Pipeline com dependência vulnerável** — alcance, segurança, políticas, atualização e decisão de liberação. | Avançado | C26, C29, C30 | Não |
| 36 | C36 | Aplicações e dados | **Arquitetura de cache e consistência de dados** — estratégias, níveis, invalidação, publicação por versões, segurança e capacidade. | Avançado | C05, C07, C10, C17 | Sim |

## Dependências conceituais

O fluxo principal é:

```text
C# e contratos (C01–C05)
├── Aplicações .NET (C06–C07)
├── Dados (C10 → C08/C09)
├── Domínio e arquitetura (C11 → C12/C13 → C14/C19)
└── Testes fundamentais (C23)
     ↓
Sistemas distribuídos (C15 → C16 → C17/C18)
     ↓
Segurança, telemetria e operação (C26 → C27 → C28)
     ↓
Entrega e plataforma (C29 → C20)
     ↓
IA aplicada (C21 → C22) e estudos integrados (C31–C35)
     ↓
Aprofundamento de dados (C36)
```

Essa é uma ordem de implementação editorial, não uma sequência obrigatória de leitura. Links de pré-requisito permitem ao leitor entrar pelo problema que precisa resolver.

## Agrupamentos principais

1. **Linguagem e runtime (C01–C05):** base para código correto e eficiente.
2. **Aplicações e dados (C06–C10, C36):** fronteiras HTTP, composição, persistência e concorrência.
3. **Domínio e arquitetura (C11–C14, C19):** modelos, dependências, decisões e documentação.
4. **Sistemas distribuídos (C15–C18):** comunicação, entrega, consistência e decomposição.
5. **Qualidade e segurança (C23–C26):** confiança no comportamento e proteção de fronteiras.
6. **Operação, entrega e plataforma (C27–C29, C20):** diagnóstico, confiabilidade, releases e infraestrutura.
7. **IA e prática profissional (C21, C22, C30):** uso responsável de IA e maturidade de equipe.
8. **Estudos de caso (C31–C35):** síntese prática sem duplicar os fundamentos.

## Fases de implementação

| Fase | Capítulos | Resultado editorial verificável |
|---|---|---|
| 1 — fundações | C01, C02, C03, C06, C10, C11, C13, C19, C23 | Vocabulário comum, templates estabilizados e links fundamentais disponíveis. |
| 2 — aplicação | C05, C07, C08, C09, C12, C24, C26, C30 | Exemplos de aplicação, persistência, segurança e testes executáveis. |
| 3 — distribuição e operação | C15, C16, C17, C18, C27, C28 | Narrativa coerente de sistemas distribuídos, resiliência e operação. |
| 4 — aprofundamentos | C04, C14, C25, C29, C20 | Performance, CQRS, qualidade sistêmica e entrega segura apoiados nas bases. |
| 5 — IA e síntese | C21, C22, C31–C35 | Temas emergentes e estudos integrados apoiados em capítulos já publicados. |
| 6 — aprofundamento de dados | C36 | Decisões de cache e publicação por versões com comportamento verificável. |

Dentro de cada fase, a ordem da tabela de capítulos prevalece. Um capítulo só entra em redação quando seus pré-requisitos estiverem publicados ou tiverem um resumo provisório claramente marcado.

## Estratégia para exemplos executáveis

Os capítulos marcados como **Sim** devem compartilhar uma solução .NET de laboratórios, dividida por projetos independentes para que um exemplo não dependa de infraestrutura irrelevante. Cada laboratório deve incluir comando de execução, versão alvo do SDK, testes determinísticos e observação sobre recursos externos.

Exemplos de banco, mensageria ou serviços externos devem ser reproduzíveis e isolados. Benchmarks pertencem ao C04; testes de integração e containers pertencem ao C24; os demais capítulos devem referenciar esses mecanismos em vez de repetir sua explicação. Capítulos **Parcial** podem usar pseudocódigo, diagramas e trechos compiláveis, conforme o problema. Nenhum exemplo deve existir apenas para ornamentar o texto.
