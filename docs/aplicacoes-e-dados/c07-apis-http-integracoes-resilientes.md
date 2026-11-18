# C07 — APIs HTTP e integrações resilientes

> **Nível:** Intermediário
>
> **Objetivo:** projetar contratos HTTP evolutivos e proteger dependências
> externas com cache e resiliência sem ocultar falhas ou duplicar efeitos.
>
> **Pré-requisitos:** [C05 — Assincronicidade e concorrência controlada](../linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md)
> e [C06 — Composição e pipeline no ASP.NET Core](c06-composicao-pipeline-aspnet-core.md).
>
> **Última revisão:** 2026-11-18

Uma API é um contrato observado por clientes que evoluem em ritmos diferentes.
Uma integração externa é uma fronteira onde latência, indisponibilidade e
limites de capacidade escapam ao controle da aplicação. Status vagos, DTOs
acoplados ao banco, retries indiscriminados e cache tratado como verdade tornam
essas fronteiras frágeis justamente durante incidentes.

Este capítulo trata design HTTP, evolução, cache distribuído e clientes
resilientes. O pipeline que aplica Problem Details, validação e correlação é
canônico no [C06](c06-composicao-pipeline-aspnet-core.md). Idempotência
distribuída e entrega confiável serão aprofundadas no C16.

## REST não é apenas HTTP com JSON

Representational State Transfer (REST) é um estilo arquitetural orientado a
recursos, interface uniforme, mensagens autocontidas, ausência de sessão de
aplicação no servidor e possibilidade de cache. JSON é apenas uma representação;
usar verbos HTTP sobre rotas que espelham métodos, como `/executarCadastro`, não
estabelece por si só uma API REST.

Comece pelo recurso e seu ciclo de vida. `/pedidos/{id}` identifica um pedido;
`/pedidos/{id}/itens` representa sua coleção de itens. Uma ação que não cabe em
CRUD pode ser modelada como mudança de estado ou novo recurso, por exemplo
`POST /pedidos/{id}/cancelamentos`, desde que o contrato expresse resultado,
repetição e concorrência.

DTOs de entrada e saída protegem o contrato da entidade persistida. Eles evitam
overposting, permitem validação específica e deixam campos internos fora da
representação. Compartilhar a mesma classe entre banco, domínio e HTTP parece
simples no início, mas transforma qualquer refatoração interna em possível
breaking change.

## Semântica dos métodos e idempotência

| Método | Semântica principal | Seguro | Idempotente |
|---|---|---:|---:|
| `GET` | obter representação | sim | sim |
| `HEAD` | obter metadados sem corpo | sim | sim |
| `POST` | criar subordinado ou executar comando | não | não por padrão |
| `PUT` | substituir estado no URI conhecido | não | sim |
| `PATCH` | aplicar alteração parcial | não | depende do formato/operação |
| `DELETE` | remover o recurso | não | sim quanto ao estado desejado |

Seguro significa não solicitar alteração de estado; idempotente significa que
repetir a mesma requisição produz o mesmo efeito pretendido. Isso não proíbe
logs, métricas ou mudança de timestamp interno. Também não torna toda repetição
gratuita: um `PUT` pode disparar integrações mal projetadas mais de uma vez.

POST pode receber idempotency key quando duplicidade é um risco e o servidor
mantém resultado associado à chave e ao conteúdo. Apenas adicionar o header sem
persistência atômica não oferece garantia. Até C16 publicar o contrato completo,
trate essa técnica como requisito explícito da operação, não como decoração.

## Status codes e respostas consistentes

- `200 OK` retorna representação ou resultado concluído.
- `201 Created` informa criação e deve fornecer `Location` quando possível.
- `202 Accepted` significa aceito para processamento, não concluído; ofereça um
  recurso de status.
- `204 No Content` confirma sucesso sem representação.
- `400 Bad Request` cobre mensagem ou parâmetros inválidos.
- `401 Unauthorized` significa autenticação ausente ou inválida; `403 Forbidden`
  significa identidade conhecida sem permissão.
- `404 Not Found` evita afirmar um recurso inexistente ou não visível.
- `409 Conflict` representa conflito com o estado atual.
- `412 Precondition Failed` é apropriado para precondição como ETag inválida.
- `422 Unprocessable Content` pode representar semântica inválida quando essa
  distinção faz parte do padrão da API.
- `429 Too Many Requests` informa limitação e pode incluir `Retry-After`.
- `500` indica falha inesperada; `502`, `503` e `504` descrevem classes distintas
  de falha de dependência ou gateway.

Não retorne `200` com `{ sucesso: false }`: isso impede caches, clientes,
telemetria e intermediários de entenderem o resultado HTTP. Use Problem Details
para uma forma comum de erro, com `type` estável, `status`, título, erros de
campo e correlação. Não use `detail` como canal para stack traces.

## Paginação é parte do contrato

Uma coleção sem limite transfere custo e risco para servidor e cliente.
Paginação por offset (`pagina`/`tamanho` ou `skip`/`take`) é simples e permite
acesso aproximado a páginas, mas inserções e remoções entre chamadas podem
duplicar ou omitir itens, e offsets altos podem ser caros no armazenamento.

Cursor ou keyset pagination usa uma chave estável da última posição. Escala
melhor para navegação sequencial e reduz inconsistência, mas não permite salto
arbitrário e torna o cursor parte do contrato. Em ambos os casos:

- imponha tamanho máximo e ordenação determinística;
- documente filtros, ordenação e estabilidade;
- forneça próximo link ou cursor;
- trate página vazia válida como coleção vazia, não necessariamente `404`;
- só calcule total quando o custo e a consistência forem aceitáveis.

O laboratório usa offset e retorna itens, página, tamanho e total. É adequado ao
conjunto pequeno do exemplo; não prova eficiência de consulta.

## Evolução e compatibilidade

Mudanças aditivas, como novo campo opcional, costumam ser compatíveis quando os
clientes ignoram membros desconhecidos. Renomear ou remover campo, alterar tipo,
tornar opcional obrigatório, mudar significado ou restringir valores pode
quebrar consumidores sem alterar a rota.

Antes de criar versão, considere novo recurso, novo campo opcional ou negociação
de capacidade. Quando houver quebra real, escolha uma estratégia consistente:
versão no caminho, query, header ou media type. Caminho é visível e fácil de
rotear, mas mistura identidade com representação; headers preservam URI, mas
dificultam descoberta e cache. Não há estratégia gratuita.

Defina política de suporte, depreciação, telemetria de uso e data de remoção.
Testes de contrato devem proteger versões suportadas. Compatibilidade também
inclui comportamento: ordem, status, defaults, paginação e interpretação de
ausência podem quebrar um cliente mesmo quando o JSON continua desserializando.

## Controllers ou Minimal APIs

Minimal APIs reduzem cerimônia e funcionam bem para serviços pequenos, endpoints
verticais e handlers com dependências explícitas. Controllers oferecem
convenções maduras, model validation com `[ApiController]`, filters MVC e uma
estrutura familiar para superfícies amplas.

Nenhum dos dois define arquitetura. Um endpoint Minimal pode delegar a uma
aplicação bem separada; um controller pode acumular regras, persistência e HTTP
na mesma action. Organize por capacidade ou recurso, mantenha DTOs na fronteira,
deixe handlers curtos e teste políticas fora do transporte quando possível.

Evite introduzir controller base, mediator, repository genérico e várias camadas
apenas para mover uma chamada de lugar. Adicione abstração quando há política,
substituição real, isolamento de tecnologia ou teste que se beneficia dela.

## Cache local e distribuído

Cache local tem baixa latência e nenhuma chamada de rede, mas cada réplica vê
estado diferente e perde conteúdo no restart. Cache distribuído oferece uma
visão compartilhada e sobrevivência independente do processo, ao custo de rede,
serialização, operação e uma nova dependência. Redis é um provider comum de
`IDistributedCache`, não uma justificativa automática para cache.

Cache é uma cópia derivada. A fonte de verdade continua sendo banco ou serviço
responsável, salvo uma arquitetura explicitamente diferente. Escolha o que
acontece quando o cache está lento ou indisponível: bypass controlado, dado
stale, erro ou limitação de carga. Bypass ilimitado pode derrubar a fonte.

### Cache-aside, TTL e invalidação

No cache-aside, a aplicação consulta o cache; em miss, lê a fonte e grava a
cópia. Escritas atualizam a fonte e invalidam ou atualizam o cache. A janela
entre operações cria consistência eventual.

TTL limita quanto tempo um valor pode permanecer stale e controla ocupação, mas
não corrige uma invalidação incorreta. TTL curto aumenta misses e carga; longo
aumenta defasagem. Jitter adiciona variação ao vencimento para evitar que muitas
chaves expirem juntas.

Invalidação por evento reduz defasagem, mas exige entrega, idempotência e
observabilidade. Chaves precisam incluir tenant, versão do schema, cultura e
outros componentes que alteram o valor. Nunca componha chaves de modo que dados
de contextos diferentes colidam.

### Stampede e falhas

Quando uma chave popular expira, muitas requisições podem buscar a fonte ao
mesmo tempo. Um lock por chave no processo reduz duplicação local; não coordena
réplicas. Lock distribuído, stale-while-revalidate ou `HybridCache` podem oferecer
proteção mais ampla, cada qual com custo e modo de falha.

Meça hits, misses, latência, erros, evictions, bytes, conexões e carga transferida
à fonte. Uma taxa de hit alta não prova benefício se serialização e rede custam
mais que a consulta original. Não registre valores sensíveis nem chaves com alta
cardinalidade sem controle.

O exemplo captura falhas de leitura e escrita, preserva cancelamento e consulta
a fonte. Esse fallback é adequado para leitura pequena; em sobrecarga real seria
necessário limitar concorrência e talvez servir dado stale.

## Clientes HTTP e gestão de conexões

Criar e descartar `HttpClient` por chamada pode descartar handlers e impedir o
reuso eficiente de conexões. `IHttpClientFactory` centraliza configuração,
logging e pipelines e gerencia o lifetime de `HttpMessageHandler`. Typed clients
encapsulam base address, serialização e contrato da dependência.

Pooling de handler não elimina limites de sockets, DNS, HTTP/2 streams ou cotas
da dependência. Observe conexões, fila, latência e erros. Cookies no handler
pooled podem vazar estado entre consumidores; avalie cliente dedicado quando o
cookie jar fizer parte do contrato.

Propague `CancellationToken` da requisição até `SendAsync` e leitura do corpo.
Separe cancelamento do chamador, orçamento total da operação e timeout por
tentativa na telemetria. `HttpClient.Timeout` combinado com outro timeout pode
tornar a causa ambígua; o laboratório o deixa infinito, cria um token vinculado
para o orçamento configurado da operação e mantém guardas por tentativa no
resilience pipeline.

## Resilience pipelines sem amplificação

`Microsoft.Extensions.Http.Resilience`, disponível para .NET 8 ou posterior,
compõe rate limiter, timeout total, retry, circuit breaker e timeout por
tentativa. Ordem altera comportamento: timeout total deve incluir tentativas;
circuit breaker observa falhas em uma janela; rate limiter limita trabalho
admitido.

Retry é apropriado apenas para falha transitória e operação repetível. Use poucas
tentativas, exponential backoff, jitter e `Retry-After` quando confiável. Não
repita validação `4xx` comum, autenticação inválida ou operação não idempotente
sem protocolo de deduplicação.

O laboratório limita a dois retries e chama
`DisableForUnsafeHttpMethods`, portanto `POST`, `PATCH`, `PUT`, `DELETE` e
`CONNECT` não são repetidos automaticamente. A escolha conservadora pode ser
relaxada por operação quando a idempotência estiver comprovada.

### Retry storm

Se mil requisições falham e cada camada faz três tentativas, a dependência pode
receber múltiplos da carga original enquanto tenta se recuperar. API, gateway,
SDK e worker podem multiplicar retries sem conhecer uns aos outros. Backoff sem
jitter ainda sincroniza clientes; timeout longo mantém recursos ocupados; fila
ilimitada entrega tráfego velho após a recuperação.

Defina um orçamento de tentativas ponta a ponta. Combine retry limitado com
circuit breaker, limite de concorrência e timeout total. Circuit breaker falha
rápido quando a taxa de falhas ultrapassa o limiar; não cura a dependência e
precisa de volume e janela coerentes. Rate limiting protege orçamento temporal;
concorrência limita operações simultâneas. São políticas complementares.

Observe tentativas por chamada, motivo, status final, duração total, estado do
circuito, rejeições e saturação. Um retry que termina em sucesso ainda é sinal de
degradação e deve aparecer em métricas sem gerar um log de erro por tentativa.

## Exemplo executável

O laboratório `exemplos/c06-c07-aspnet-core/` contém:

- **D:** typed client com `IHttpClientFactory`, timeout total e por tentativa,
  cancelamento, retry exponencial limitado e jitter;
- **E:** cache-aside com TTL, jitter, proteção local contra stampede e fallback;
- **F:** endpoints paginados, `201 Created`, `404`, `400` e Problem Details.

Os testes comprovam que duas falhas `503` em `GET` são repetidas até sucesso,
enquanto `POST` não é repetido; cache indisponível consulta a fonte; paginação
inválida retorna `400`; e recurso inexistente retorna Problem Details com
correlação.

```bash
dotnet test exemplos/c06-c07-aspnet-core/C06C07.AspNetCore.Tests.csproj
```

O cache em memória implementa `IDistributedCache`, mas não é distribuído. O fake
HTTP não mede latência, sockets ou comportamento de DNS. Não há benchmark nem
alegação de ganho de performance. Redis e uma API real exigiriam infraestrutura
e testes de integração separados. O registro didático de semáforos por chave não
faz eviction; um conjunto de chaves sem limite exigiria `HybridCache`, limpeza
segura ou outra estratégia que também limitasse os metadados locais.

## Boas práticas e armadilhas

- Modele recursos e efeitos antes de escolher rotas e verbos.
- Versione apenas quebras reais e mantenha telemetria de versões antigas.
- Limite paginação e imponha ordenação estável.
- Preserve DTOs como contrato; não exponha entidades persistidas diretamente.
- Trate cache como otimização descartável e teste o caminho sem ele.
- Não engula cancelamento como falha de cache ou rede.
- Não faça retry em toda exceção, todo status ou toda camada.
- Não configure circuit breaker sem volume, janela e métricas coerentes.
- Não use cache ou retry para esconder uma dependência permanentemente lenta.

## Trade-offs e quando evitar

REST e semântica HTTP melhoram interoperabilidade, cache e observabilidade, mas
nem toda operação precisa fingir ser CRUD. RPC explícito pode ser mais claro
quando o domínio é comando intensivo, desde que ainda respeite contratos HTTP.

Cache reduz carga e latência em leituras repetidas, mas adiciona inconsistência,
serialização e operação. Evite-o quando a fonte já é rápida, a taxa de reuso é
baixa ou dado stale é inaceitável sem invalidação confiável.

Retry aumenta chance de superar falha breve, mas consome capacidade e aumenta a
latência de cauda. Evite-o em efeito não idempotente, erro permanente, orçamento
de tempo curto ou dependência saturada. Circuit breaker e rate limiting também
rejeitam trabalho; consumidores precisam entender a degradação resultante.

## Relação com outros capítulos

- **Pré-requisitos:** [C05](../linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md)
  cobre cancelamento, concorrência e rate limiting; [C06](c06-composicao-pipeline-aspnet-core.md)
  cobre DI, pipeline, configuração e Problem Details.
- **Contratos de falha:** [C02](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md)
  diferencia ausência, resultado esperado e exceção.
- **Streaming e paginação:** [C03](../linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md)
  explica materialização e execução diferida.
- **Aplicações planejadas:** C15 trata comunicação e mensageria; C16 aprofunda
  idempotência; C26 trata autorização; C27 trata telemetria distribuída.

## Referências

- [Boas práticas de design de APIs Web REST](https://learn.microsoft.com/azure/architecture/best-practices/api-design)
  — Azure Architecture Center, acesso em 18 novembro 2026.
- [Boas práticas de implementação de APIs REST](https://learn.microsoft.com/azure/architecture/best-practices/api-implementation)
  — Azure Architecture Center, acesso em 18 novembro 2026.
- [`IHttpClientFactory` no ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
- [Resiliência para clientes HTTP](https://learn.microsoft.com/dotnet/core/resilience/http-resilience)
  — documentação .NET, acesso em 18 novembro 2026.
- [Cache distribuído no ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
- [Formatação e Problem Details](https://learn.microsoft.com/aspnet/core/web-api/advanced/formatting?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
