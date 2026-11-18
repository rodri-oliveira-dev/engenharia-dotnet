# C15 — Comunicação entre serviços e mensageria resiliente

> **Nível:** Intermediário
>
> **Objetivo:** distinguir falhas transitórias e permanentes e operar retries,
> DLQ, replay, idempotência e ordenação sem amplificar incidentes.
>
> **Pré-requisitos:** [C07 — APIs HTTP e integrações resilientes](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md)
> e [C13 — Dependências, abstrações e padrões com propósito](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md).
>
> **Última revisão:** 2026-11-18

Mensageria reduz acoplamento temporal: o produtor não precisa aguardar o
consumidor. Em troca, o sistema passa a conviver com backlog, redelivery,
duplicidade, ordenação limitada e operação de mensagens que nunca terão sucesso.
Fila não transforma uma operação frágil em confiável por si só.

## Comunicação e garantias

HTTP síncrono é adequado quando o chamador precisa da resposta para continuar e
o orçamento de latência comporta a dependência. Mensagem assíncrona é adequada
para desacoplar tempo, nivelar carga ou distribuir fatos. Ela não elimina
acoplamento de contrato nem garante disponibilidade do resultado.

Em recepção com lock/ack após processamento, uma queda antes do ack causa nova
entrega. Esse modelo **at-least-once** prefere duplicidade a perda. Remover a
mensagem antes do efeito oferece risco oposto. Não descreva nenhum deles como
exactly-once do efeito de negócio: broker, consumidor e banco são fronteiras
distintas.

## Retry com limite e backoff

Classifique antes de repetir:

| Falha | Exemplo | Ação inicial |
| --- | --- | --- |
| transitória | timeout, conexão interrompida, `503`, lock breve | retry limitado com backoff e jitter |
| permanente da mensagem | schema inválido, campo obrigatório ausente | DLQ imediata |
| permanente de negócio | cliente inexistente após janela prevista | estado/rejeição de negócio, não loop técnico |
| sistêmica persistente | credencial revogada, bug, banco fora por horas | interromper/pausar, alertar e corrigir |

Backoff exponencial abre espaço para recuperação; jitter evita que consumidores
sincronizados repitam juntos. Defina máximo de tentativas ou tempo total. Retry
infinito bloqueia partição, consome capacidade e esconde poison messages. Conte
tentativas entre redeliveries do broker, reinícios e retries internos; camadas
independentes podem multiplicá-las.

O `ProcessadorResiliente` do laboratório repete somente
`FalhaTransitoriaException`, usa atrasos 1s, 2s... e envia falha permanente à
representação de DLQ na primeira tentativa. Em produção, use a capacidade do
broker/pipeline, persista contagem e acrescente jitter.

## Poison message e Dead Letter Queue

Uma **poison message** falha de forma determinística para o consumidor atual.
Após classificação ou limite, mova-a para a **Dead Letter Queue** (DLQ) com
identificador, tipo/versão, motivo, primeira/última falha, contagem e correlação.
A DLQ é uma fila operacional, não lixeira nem arquivo definitivo.

Um procedimento de reprocessamento deve:

1. corrigir código, configuração ou dado de referência;
2. selecionar mensagens por causa e janela, sem despejar toda a DLQ;
3. preservar `MessageId`/chave idempotente e registrar quem iniciou o replay;
4. limitar taxa e observar dependências;
5. confirmar resultado e política de retenção.

Editar payload manualmente cria um novo fato e exige trilha de auditoria. Replay
sem idempotência pode repetir cobrança, e-mail ou atualização.

## Idempotência e ordenação

Idempotência faz repetições da mesma intenção produzirem um único efeito de
negócio. O consumidor grava `MessageId` (ou chave de negócio) com a alteração na
mesma transação local. “Consultar, executar, depois registrar” deixa janela de
duplicidade. O C16 implementa a transação e discute retenção das chaves.

Ordenação global reduz paralelismo e raramente é necessária. Quando o requisito é
“eventos do mesmo pedido em ordem”, particione/sessione por `PedidoId` e inclua
sequência ou versão. Mesmo assim, retry e DLQ abrem buracos: bloquear a chave,
adiar eventos posteriores ou aceitar reconciliação é uma decisão de negócio.
Timestamp de máquinas diferentes não prova ordem causal.

## Estratégias operacionais

Observe taxa de entrada/saída, backlog, idade da mensagem mais antiga, tentativas,
tempo de processamento, locks perdidos, DLQ por motivo e saturação downstream.
Alertar apenas pelo tamanho ignora uma fila grande saudável e uma única mensagem
antiga travada.

Defina runbooks para pausar consumidor, reduzir concorrência, drenar, replay e
descartar conforme política autorizada. Proteja payloads sensíveis e acesso à
DLQ. Versione contratos de forma tolerante e mantenha consumidores capazes de
lidar com versões ainda retidas.

## Trade-offs e quando evitar

Mensageria adiciona durabilidade e absorção de picos, mas custa broker, schema,
observabilidade, consistência eventual e suporte 24/7. Para uma chamada local ou
fluxo curto que precisa resposta imediata, método/HTTP pode ser mais claro.
Fila usada para esconder serviço lento apenas desloca o incidente para backlog.

Não introduza retry para erro de validação, DLQ sem processo de recuperação,
ordenação global por conveniência ou concorrência maior que a capacidade da
dependência. Backpressure e limites do [C05](../linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md)
continuam necessários.

## Laboratório executável

O teste `Poison_message_vai_para_dlq_sem_retries_inuteis` prova classificação e
limite de tentativas:

```bash
dotnet test exemplos/c13-c17-arquitetura/C13C17.Arquitetura.Tests.csproj
```

O laboratório não implementa broker; por isso não afirma comportamento de lock,
partição ou throughput. Esses pontos exigem testes de integração com o produto e
configuração reais.

## Relação com outros capítulos

- [C07](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md) cobre
  timeout, retry e orçamento de tentativas em HTTP.
- [C16](c16-idempotencia-entrega-confiavel.md) cobre Outbox, deduplicação e
  circuit breaker; [C17](c17-consistencia-eventual-sagas.md) compõe mensagens em
  transações distribuídas de negócio.
- [C09](../aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) cobre a
  transação local usada pelo consumidor.

## Referências

- [Prevent message loss and duplicate processing in Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-message-loss-and-duplicates)
  — documentação Azure Service Bus, entrega, redelivery e idempotência, acesso
  em 18 novembro 2026.
- [Service Bus dead-letter queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
  — documentação Azure Service Bus, motivos e contagem máxima, acesso em 18
  novembro 2026.
- [Duplicate message detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)
  — documentação Azure Service Bus, janela e limites, acesso em 18 novembro
  2026.
