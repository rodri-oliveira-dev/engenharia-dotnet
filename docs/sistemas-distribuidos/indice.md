# Sistemas distribuídos

Distribuir um sistema troca chamadas locais e transações únicas por comunicação,
entrega repetida, consistência temporal e operação de vários componentes. Esta
parte acompanha essa mudança em ordem: primeiro o transporte e seus modos de
falha, depois a entrega confiável, a coordenação de processos e, por fim, a
decisão econômica de criar ou extrair limites de serviço.

Pressupõe contratos HTTP, persistência e limites de domínio, especialmente C07,
C10 e C12–C13. Ao concluir o percurso, o leitor deve conseguir modelar retries,
duplicidade, Outbox, compensações e migração incremental sem prometer garantias
que a rede não oferece.

As falhas parciais introduzidas aqui criam a necessidade de evidência, proteção
e operação tratada nas duas partes seguintes. C17 também fornece o contexto de
consistência recomendado antes do aprofundamento opcional C36.

## Capítulos publicados

- **C15 · Intermediário · Publicado:** [Comunicação entre serviços e mensageria
  resiliente](c15-comunicacao-mensageria-resiliente.md).
- **C16 · Avançado · Publicado:** [Idempotência e entrega
  confiável](c16-idempotencia-entrega-confiavel.md).
- **C17 · Avançado · Publicado:** [Consistência eventual e
  sagas](c17-consistencia-eventual-sagas.md).
- **C18 · Avançado · Publicado:** [Limites de microserviços e evolução de
  monólitos](c18-limites-microservicos-evolucao-monolitos.md).

Consulte o [mapa editorial](../planejamento/mapa-editorial.md) para escopo,
dependências e estratégia de exemplos. Volte ao [índice central](../indice.md).
