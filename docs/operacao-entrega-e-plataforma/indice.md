# Operação, entrega e plataforma

Esta parte acompanha o sistema depois que ele encontra produção. Sinais
observáveis sustentam objetivos de confiabilidade e resposta a incidentes; essas
evidências orientam exposição, rollback e evolução de contratos; por fim, a
infraestrutura passa a ser tratada como estado versionado e reconciliável.

Pressupõe aplicações integradas, modos de falha distribuídos, testes e segurança.
Ao concluir o percurso, o leitor deve conseguir ligar telemetria a decisões,
definir objetivos acionáveis, reduzir risco de entrega e controlar drift sem
confundir automação com ausência de responsabilidade operacional.

C20 tem identificador anterior porque foi planejado em outra fase, mas encerra
a leitura desta parte: sua aplicação depende das decisões de documentação e
entrega de C19 e C29. C27 também completa o contexto necessário para retornar ao
aprofundamento C25. A governança dessas decisões conduz à prática profissional.

## Capítulos publicados

- **C27 · Intermediário · Publicado:** [Telemetria e diagnóstico
  distribuído](c27-telemetria-diagnostico-distribuido.md).
- **C28 · Avançado · Publicado:** [Confiabilidade de serviço e resposta a
  incidentes](c28-confiabilidade-resposta-incidentes.md).
- **C29 · Avançado · Publicado:** [Entrega compatível e gestão de risco no
  pipeline](c29-entrega-compativel-risco-pipeline.md).
- **C20 · Intermediário · Publicado:** [Infraestrutura como código e controle de
  drift](c20-infraestrutura-como-codigo-drift.md).

## Compatibilidade de build e publicação

- [SDK, NuGet, containers e NativeAOT no .NET 11](../compatibilidade/compatibilidade-sdk-build-11.md).

Consulte o [mapa editorial](../planejamento/mapa-editorial.md) para escopo,
dependências e estratégia de exemplos. Volte ao [índice central](../indice.md).
