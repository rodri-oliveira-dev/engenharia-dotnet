# C31 — API de pagamentos com operações duplicadas

> **Nível:** Avançado
>
> **Objetivo:** investigar duplicidade de efeitos e combinar idempotência,
> persistência, mensageria e retry sem presumir uma causa única.
>
> **Pré-requisitos:** [C09 — Fronteiras e concorrência no EF Core](../aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md),
> [C16 — Idempotência e entrega confiável](../sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md),
> [C27 — Telemetria e diagnóstico distribuído](../operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md)
> e [C28 — Confiabilidade de serviço e resposta a incidentes](../operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md).
>
> **Última revisão:** 2026-11-18

Este é um cenário didático fictício. Ele descreve um processo de raciocínio, não
um incidente real nem uma solução universal.

## Cenário

Uma API aceita uma intenção de pagamento, persiste a tentativa e publica uma
mensagem para autorização. O cliente pode repetir a requisição após timeout e o
broker oferece entrega pelo menos uma vez. O efeito financeiro não pode depender
da esperança de que cada chamada ocorra uma única vez.

## Contexto técnico

Há API HTTP, banco relacional, worker, broker e provedor externo. O fluxo cruza
fronteiras transacionais: confirmar no provedor, gravar estado e reconhecer a
mensagem não formam uma transação ACID única. Cada fronteira pode observar uma
falha diferente.

## Sintomas ou problema

Suporte relata duas autorizações associadas ao mesmo pedido. Logs possuem IDs de
requisição, mas não há chave estável da intenção de negócio em todas as etapas.
Antes de corrigir, é preciso distinguir requisição repetida, entrega duplicada,
concorrência, retry após resposta perdida e duplicidade apenas na projeção.

## Restrições

- pagamentos legítimos distintos para o mesmo pedido podem existir;
- o provedor pode confirmar e a resposta se perder;
- mensagens podem chegar novamente e fora de ordem;
- pausar toda autorização amplia impacto;
- dados financeiros precisam de trilha de auditoria; exclusão não é correção.

## Hipóteses

1. O cliente criou novas chaves após timeout e a API tratou cada chamada como
   intenção nova.
2. Duas instâncias passaram por um `if not exists` não atômico.
3. O worker repetiu a chamada externa depois de timeout sem consultar o estado.
4. A autorização foi única, mas evento ou projeção duplicou a visualização.
5. O Outbox publicou mais de uma vez, comportamento esperado sem inbox no
   consumidor.

As hipóteses não são mutuamente exclusivas. Corrigir primeiro o ponto mais
visível pode deixar outro caminho de duplicidade ativo.

## Processo de investigação

Primeiro limite o dano: considere suspender retries automáticos do efeito externo
ou reduzir concorrência, preservando processamento manual seguro. Depois monte
uma linha temporal por chave de intenção, pedido, mensagem, tentativa, ID do
provedor, trace e instância. Não correlacione por timestamp apenas.

Consulte registros autoritativos: constraints e transações do banco, resposta de
consulta do provedor, offsets e redeliveries do broker. Verifique se a resposta
HTTP perdida ocorreu antes ou depois do commit e se o ack ocorreu antes ou depois
do efeito. Reproduza em teste controlado: duas chamadas concorrentes com a mesma
chave, falha após confirmação externa, falha após commit e redelivery.

A investigação termina quando a sequência explica todos os efeitos observados e
um teste de falha reproduz o mecanismo, não quando um log “parece suspeito”.

## Alternativas

- chave de idempotência persistida com resultado completo e constraint única;
- uma intenção de pagamento como agregado, com estado explícito e reconciliação;
- inbox transacional no consumidor e Outbox no produtor;
- consulta ao provedor por chave estável antes de repetir estado incerto;
- serialização por chave, lock ou partição para reduzir concorrência;
- compensação/estorno quando o provedor não oferece idempotência.

## Trade-offs

Constraint única resolve corrida local, mas não desfaz dois efeitos externos já
aceitos. Lock distribuído reduz concorrência e acrescenta expiração e modo de
falha. Guardar resposta idempotente exige política de retenção e validação de que
a mesma chave não seja reutilizada com payload diferente. Outbox evita a lacuna
entre commit e publicação, mas aceita publicação repetida; consumers ainda
precisam ser idempotentes. Retry aumenta disponibilidade em falha transitória e
amplia dano quando o resultado é desconhecido.

## Estratégia de solução

Defina uma chave da intenção, fornecida ou derivada de identidade de negócio, e
insira-a atomicamente com a tentativa. A mesma chave e o mesmo payload retornam o
resultado registrado; payload diferente é conflito. Persista transição e evento
de Outbox juntos. O consumidor registra mensagem processada e efeito local na
mesma transação.

Ao chamar o provedor, envie a chave idempotente se o contrato suportar. Diante de
timeout após envio, marque resultado desconhecido e consulte/reconcilie antes de
nova autorização. Classifique erros permanentes e transitórios, limite retry e
use backoff com jitter. Modele estorno como novo evento auditável, não como
deleção da autorização.

## Validação

- teste concorrente prova uma linha autoritativa por chave;
- fault injection cobre quedas antes/depois de commit, publish, chamada e ack;
- teste de contrato verifica semântica idempotente do provedor;
- replay de mensagem não repete efeito local;
- dashboard segmenta tentativas, deduplicações, estados desconhecidos e
  reconciliações por versão;
- runbook ensaia consulta, contenção e recuperação sem alterar evidência.

## Riscos remanescentes

Um consumidor desconhecido pode não deduplicar. Retenção curta pode permitir
reuso tardio. Reconciliação pode atrasar resposta ao usuário. O provedor pode não
garantir idempotência no mesmo horizonte. Falha humana em compensação permanece
possível e exige dupla verificação conforme impacto.

## Aprendizados

“Exactly once” ponta a ponta não nasce de um único broker ou lock. O efeito
seguro resulta da identidade estável, atomicidade local, deduplicação em cada
fronteira, reconciliação e observabilidade. Retry é uma decisão de domínio quando
o primeiro resultado pode ter produzido efeito.

## Referências para capítulos relacionados

- [C15 — Comunicação e mensageria](../sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md)
- [C16 — Idempotência e entrega confiável](../sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md)
- [C17 — Consistência eventual e sagas](../sistemas-distribuidos/c17-consistencia-eventual-sagas.md)
- [C27 — Telemetria](../operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md)
- [C28 — Resposta a incidentes](../operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md)

## Referências

- [The Idempotency-Key HTTP Header Field](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)
  — IETF HTTPAPI Working Group, acesso em 18 novembro 2026.
