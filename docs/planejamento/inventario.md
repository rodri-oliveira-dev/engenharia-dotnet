# Inventário de temas

## Escopo e rastreabilidade

Este inventário foi produzido a partir da aba **Entrevista v2 - Sênior .NET** da planilha [Entrevista Técnica - Desenvolvedor Sênior .NET](https://docs.google.com/spreadsheets/d/1V10vyDSPYv97EJRIOpTpa37Vi4tncZIYbVEy7rDrBEQ/edit), consultada em 18 de novembro de 2026. A aba informa um banco de 93 questões. Conforme o recorte solicitado, as seis questões do grupo inicial **Programação** (linhas 5 a 10) foram excluídas. Restaram 87 questões, das linhas 11 a 97.

Os números de linha abaixo são os da planilha e constituem a chave de rastreabilidade. O texto foi convertido de pergunta para tema; não é uma transcrição das respostas esperadas. Os códigos `C01` a `C32` apontam para os capítulos definidos em [mapa-editorial.md](mapa-editorial.md).

## Temas derivados das perguntas

| Linha | Grupo de origem | Tema normalizado | Capítulo |
|---:|---|---|---|
| 11 | C# | Semântica, identidade e critérios de escolha entre classes, records e structs | C01 |
| 12 | C# | Controle de herança com classes seladas e desenho consciente de pontos de extensão | C01 |
| 13 | C# | Tipos por valor e por referência: cópia, identidade, nulabilidade e custo | C01 |
| 14 | C# | Processamento de memória contígua com `Span<T>` e alternativas assíncronas | C04 |
| 15 | C# | Captura de contexto em código assíncrono e uso contextual de `ConfigureAwait` | C05 |
| 16 | C# | Iteradores, execução diferida e produção sob demanda com `yield return` | C03 |
| 17 | C# | Compilação JIT e AOT: inicialização, otimização, compatibilidade e tamanho | C04 |
| 18 | C# | Contratos de nulabilidade e uso responsável de nullable reference types | C02 |
| 19 | C# | Estratégia de exceções, preservação de stack trace e tratamento nas fronteiras | C02 |
| 20 | C# | Modelagem explícita de ausência, falha esperada e condição excepcional | C02 |
| 21 | C# | Boxing, alocações e impacto em caminhos críticos | C04 |
| 22 | C# | Múltipla enumeração de sequências e efeitos da execução diferida | C03 |
| 23 | C# | Materialização, streaming e custo de `ToList` em grandes volumes | C03 |
| 24 | C# | Escolha de coleções por semântica, acesso e custo de memória | C03 |
| 25 | C# | Concorrência limitada, backpressure e riscos de `Task.WhenAll` em massa | C05 |
| 26 | C# | Garbage Collector, pressão de alocação, LOH, buffers e pooling | C04 |
| 27 | C# | Uso especializado de `ValueTask<T>` e seus custos de complexidade | C05 |
| 28 | C# | Diagnóstico de desempenho orientado por baseline, profiling e medição | C04 |
| 29 | ASP.NET Core | Lifetimes de injeção de dependência e captive dependency | C06 |
| 30 | ASP.NET Core | Design de APIs orientadas a recursos e semântica HTTP | C07 |
| 31 | ASP.NET Core | Pipeline HTTP, middleware, filters e preocupações transversais | C06 |
| 32 | ASP.NET Core | Gestão de segredos por ambiente | C26 |
| 33 | ASP.NET Core | Configuração tipada, validação no startup e Options Pattern | C06 |
| 34 | ASP.NET Core | Cache distribuído: invalidação, TTL, stampede e degradação | C07 |
| 35 | ASP.NET Core | Consumo resiliente de APIs: clientes HTTP, timeout, cancelamento e retry | C07 |
| 36 | EF Core | Consultas somente leitura e custo do Change Tracker | C08 |
| 37 | EF Core | Consultas divididas e explosão cartesiana | C08 |
| 38 | EF Core | Compiled queries como otimização de nicho e orientada por medição | C08 |
| 39 | EF Core | Detecção e prevenção de N+1 | C08 |
| 40 | EF Core | Diagnóstico sistemático de consultas lentas | C08 |
| 41 | EF Core | Projeção de modelos de leitura e redução de materialização | C08 |
| 42 | EF Core | Limites arquiteturais para exposição de `IQueryable<T>` | C09 |
| 43 | EF Core | Concorrência otimista e resolução de conflitos de atualização | C09 |
| 44 | DB / SQL / NoSQL | Escolha entre modelos relacionais e não relacionais | C10 |
| 45 | DB / SQL / NoSQL | Propriedades ACID e consequências para a aplicação | C10 |
| 46 | DB / SQL / NoSQL | Índices: benefício de leitura e custos de escrita e armazenamento | C10 |
| 47 | DB / SQL / NoSQL | Locks otimistas e pessimistas e critérios de escolha | C10 |
| 48 | DDD | Entidades e identidade no modelo de domínio | C11 |
| 49 | DDD | Agregados, raízes e proteção de invariantes transacionais | C11 |
| 50 | DDD | Modelo anêmico e alocação adequada de comportamento | C11 |
| 51 | DDD | Value objects, igualdade e imutabilidade | C11 |
| 52 | DDD | Primitive obsession e tipos de domínio proporcionais ao problema | C11 |
| 53 | DDD | Application services e domain services | C12 |
| 54 | DDD | Anti-Corruption Layer e isolamento entre modelos | C12 |
| 55 | Arquitetura e Padrões | Clean Architecture, responsabilidades e regra de dependência | C13 |
| 56 | Arquitetura e Padrões | SOLID sem abstrações cerimoniais | C13 |
| 57 | Arquitetura e Padrões | Padrões de projeto como resposta contextual a forças e consequências | C13 |
| 58 | Arquitetura e Padrões | CQRS, separação de modelos e limite de aplicabilidade | C14 |
| 59 | Arquitetura e Padrões | Outbox transacional e entrega confiável de eventos | C16 |
| 60 | Arquitetura e Padrões | Circuit breaker e prevenção de falhas em cascata | C16 |
| 61 | Arquitetura e Padrões | Sagas, passos locais e compensações | C17 |
| 62 | Arquitetura e Padrões | Poison messages, retry limitado, DLQ e replay operacional | C15 |
| 63 | Arquitetura e Padrões | Critérios econômicos para criação de abstrações | C13 |
| 64 | Microserviços | Comunicação síncrona e assíncrona e acoplamento temporal | C15 |
| 65 | Microserviços | Diagnóstico distribuído com logs, métricas, correlação e traces | C27 |
| 66 | Microserviços | Consistência eventual entre banco e publicação de eventos | C16 |
| 67 | Microserviços | Idempotência de APIs e persistência da intenção | C16 |
| 68 | Microserviços | Seleção de padrões de microserviços orientada pelo problema | C18 |
| 69 | Microserviços | Migração incremental de monólitos com limites autônomos | C18 |
| 70 | Microserviços | Entrega duplicada, consumidores idempotentes e deduplicação | C16 |
| 71 | Documentação | Níveis C4 escolhidos conforme público e necessidade | C19 |
| 72 | Documentação | ADRs, histórico, alternativas, consequências e supersessão | C19 |
| 73 | IaC | Provisionamento declarativo e configuração: papéis de Terraform e Ansible | C20 |
| 74 | IaC | Drift, rastreabilidade e reconciliação da infraestrutura | C20 |
| 75 | IA | MCP, ferramentas, contexto, autorização e auditoria | C21 |
| 76 | IA | RAG, recuperação de conhecimento, avaliação e controle de acesso | C21 |
| 77 | IA | Orquestração de agentes, estado, validação e human-in-the-loop | C22 |
| 78 | IA | Desenvolvimento orientado por especificações com revisão e validação | C22 |
| 79 | Testes | Matchers precisos e validação de argumentos em mocks | C23 |
| 80 | Testes | Fronteira entre testes unitários e de integração orientada por risco | C23 |
| 81 | Testes | Custo do excesso de mocks e escolha de dublês | C23 |
| 82 | Testes | Testes de comportamento versus acoplamento à implementação | C23 |
| 83 | Testes | Cobertura como indicador, não como medida isolada de qualidade | C25 |
| 84 | Testes | Testes de carga e end-to-end com metas e observabilidade | C25 |
| 85 | Testes | Infraestrutura real e reproduzível com Testcontainers | C24 |
| 86 | Cenário integrado | Pagamentos duplicados: investigação e desenho idempotente ponta a ponta | C31 |
| 87 | Segurança | Autorização em nível de objeto e isolamento por usuário ou tenant | C26 |
| 88 | Segurança | Resposta a segredos expostos, rotação e prevenção | C26 |
| 89 | Segurança | Minimização e proteção de dados sensíveis em telemetria | C26 |
| 90 | Operação e observabilidade | Mitigação de incidentes pós-deploy e análise posterior | C28 |
| 91 | Operação e observabilidade | SLIs, SLOs, error budget e alertas por burn rate | C28 |
| 92 | CI/CD | Evolução compatível de banco e API, implantação gradual e rollback | C29 |
| 93 | CI/CD | Triagem contextual de vulnerabilidades em dependências | C29 |
| 94 | Colaboração e senioridade | Decisão técnica sob restrições, evidências e governança | C30 |
| 95 | Colaboração e senioridade | Mentoria por review e pairing com autonomia progressiva | C30 |
| 96 | Testes | Estratégia incremental de testes para legado crítico | C24 |
| 97 | Cenário integrado | Filas com backlog: diagnóstico de throughput e gargalos externos | C32 |

## Consolidação e sobreposições

As perguntas não devem originar 87 artigos independentes. As principais consolidações são:

- **Performance em C# (linhas 14, 17, 21, 26 e 28):** memória, runtime e medição formam uma única narrativa; otimizações específicas só fazem sentido depois do diagnóstico.
- **Execução diferida (linhas 16, 22 e 23):** `yield`, múltipla enumeração e materialização são manifestações do mesmo modelo de execução.
- **Concorrência e assincronicidade (linhas 15, 25 e 27):** APIs diferentes, mas um mesmo eixo de custo, contexto e controle de concorrência.
- **EF Core e banco (linhas 36 a 47):** o diagnóstico de consulta depende de SQL, plano, índices e concorrência. A divisão entre C08, C09 e C10 evita repetir fundamentos do banco em cada técnica do ORM.
- **Abstrações (linhas 55 a 58 e 63):** SOLID, Clean Architecture, design patterns e CQRS se sobrepõem quando tratados como receitas. C13 estabelece critérios; C14 aprofunda uma aplicação específica.
- **Confiabilidade distribuída (linhas 35, 59 a 62, 64, 66, 67 e 70):** timeout, retry, circuit breaker, outbox, idempotência, DLQ e saga são complementares. C15–C17 separam transporte, entrega confiável e transações de longa duração.
- **Observabilidade (linhas 65, 89 a 91):** telemetria sustenta troubleshooting e SRE, mas privacidade pertence também ao eixo de segurança. C26 define limites de dados; C27 define instrumentação; C28 define operação.
- **Qualidade de testes (linhas 79 a 85 e 96):** mocks, tipos de teste, cobertura, carga e legado compartilham estratégia baseada em risco, distribuída em C23–C25 por objetivo.
- **Cenários integrados (linhas 86 e 97):** reutilizam conceitos anteriores e não devem reexplicá-los. Funcionam como síntese com links para os capítulos de base.

## Cobertura das áreas e lacunas de fonte

| Área solicitada | Evidência na planilha | Situação editorial |
|---|---|---|
| C# e fundamentos | Linhas 11–28 | Cobertura ampla; ausência de perguntas específicas sobre generics, pattern matching e sistema de tipos avançado não autoriza presumir conteúdo da fonte. |
| ASP.NET Core | Linhas 29–35 | Cobertura de pipeline, DI, configuração, APIs e integrações; autenticação e hosting não aparecem como perguntas próprias. |
| Entity Framework Core | Linhas 36–43 | Boa cobertura de leitura e desempenho; migrações aparecem apenas indiretamente em CI/CD. |
| Bancos SQL e NoSQL | Linhas 44–47 | Fundamentos presentes; modelagem, isolamento de transações e operação de bancos não têm perguntas próprias. |
| Domain-Driven Design | Linhas 48–54 | Building blocks centrais presentes; bounded contexts e linguagem ubíqua não aparecem explicitamente. |
| Arquitetura e padrões | Linhas 55–63 | Cobertura forte, com sobreposição distribuída tratada na consolidação. |
| Microserviços | Linhas 64–70 | Comunicação, consistência e migração presentes; descoberta de serviços e governança de contratos não aparecem isoladamente. |
| Documentação | Linhas 71–72 | Fonte limitada a C4 e ADR. |
| Infraestrutura como código | Linhas 73–74 | Fonte limitada a papéis de ferramentas e drift. |
| IA aplicada | Linhas 75–78 | MCP, RAG, agentes e especificações presentes; avaliação, segurança e custos aparecem apenas dentro desses temas. |
| Testes | Linhas 79–85 e 96 | Estratégia, mocks, integração, carga, E2E e legado presentes. |
| Segurança | Linhas 32, 87–89 e 93 | Autorização por objeto, segredos, privacidade de telemetria e dependências; ameaças mais amplas não estão na fonte. |
| Observabilidade e operação | Linhas 65, 89–91 e 97 | Instrumentação, incidentes e SRE presentes. |
| CI/CD | Linhas 92–93 | Fonte limitada a entrega compatível e vulnerabilidades. |
| Colaboração e maturidade | Linhas 94–95 | Fonte limitada a decisão técnica e mentoria. |
| Estudos de caso | Linhas 86 e 97 | Dois casos integrados disponíveis. |

Essas lacunas são registradas para evolução futura, mas não são tratadas como perguntas existentes nem recebem capítulos adicionais sem nova fonte ou decisão editorial explícita.
