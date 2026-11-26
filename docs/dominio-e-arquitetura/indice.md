# Domínio e arquitetura

Esta parte desloca a atenção dos mecanismos para as decisões que o software
precisa preservar. Tipos e persistência passam a servir à linguagem do negócio;
serviços, padrões e modelos de leitura são avaliados pelas forças de mudança, e
as escolhas relevantes ganham documentação que pode evoluir com o sistema.

O percurso pressupõe C01 e C02 e se beneficia dos fundamentos de aplicações e
dados. Ao final, o leitor deve conseguir delimitar invariantes, separar
orquestração de política, justificar abstrações, reconhecer o custo de CQRS e
registrar decisões sem congelá-las. C19 foi implementado depois dos C11–C14, mas
seu ID não determina posição: ele encerra a parte porque documenta decisões já
discutidas.

Os limites e contratos construídos aqui serão submetidos a rede, duplicidade e
falhas parciais em Sistemas distribuídos. Os cenários de Pedidos são retomados
quando ajudam a manter continuidade, sem criar dependência entre laboratórios.

## Capítulos publicados

- **C11 · Fundamental · Publicado:** [Entidades, value objects e agregados](c11-entidades-value-objects-agregados.md).
- **C12 · Intermediário · Publicado:** [Serviços de domínio e integração entre
  modelos](c12-servicos-dominio-integracao-modelos.md).
- **C13 · Fundamental · Publicado:** [Dependências, abstrações e padrões com
  propósito](c13-dependencias-abstracoes-padroes.md).
- **C14 · Avançado · Publicado:** [CQRS e modelos orientados ao
  uso](c14-cqrs-modelos-orientados-uso.md).
- **C19 · Fundamental · Publicado:** [Documentação arquitetural
  viva](c19-documentacao-arquitetural-viva.md).

Consulte o [mapa editorial](../planejamento/mapa-editorial.md) para escopo,
dependências e estratégia de exemplos. Volte ao [índice central](../indice.md).
