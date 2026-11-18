# C27 — Telemetria e diagnóstico distribuído

> **Nível:** Intermediário
>
> **Objetivo:** instrumentar aplicações .NET com sinais úteis e investigar falhas
> distribuídas por evidências, controlando custo, volume, cardinalidade e exposição
> de dados.
>
> **Pré-requisitos:** [C07 — APIs HTTP e integrações resilientes](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md),
> [C15 — Comunicação entre serviços e mensageria resiliente](../sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md)
> e [C26 — Segurança nas fronteiras da aplicação](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md).
>
> **Última revisão:** 2026-11-18

Monitoramento responde perguntas conhecidas: a taxa de erro ultrapassou o limite?
Há fila acumulada? Observabilidade é a capacidade de investigar estados internos a
partir das evidências emitidas pelo sistema, inclusive quando a pergunta ainda não
era conhecida. Um painel não torna um sistema observável; é preciso contexto que
permita sair do sintoma, formular hipóteses e distinguir causas concorrentes.

Logs, métricas e traces são sinais complementares. Métricas mostram tendência e
amplitude, traces preservam o caminho causal de uma operação e logs registram
eventos discretos com detalhes. Deploys, flags, configuração, ownership, topologia
e objetivos de serviço formam o **contexto operacional** sem o qual os sinais podem
ser tecnicamente corretos e ainda induzir a decisão errada.

## De perguntas operacionais à telemetria

Instrumentar tudo produz custo, não compreensão. Comece por perguntas:

- qual jornada do usuário está degradada, desde quando e em quais regiões ou versões;
- se o tempo foi gasto na aplicação, fila, banco ou dependência externa;
- se a falha acompanha um deploy, flag, volume, tenant ou tipo de operação;
- se CPU, memória, conexões, threads ou capacidade de downstream estão saturados;
- qual evidência confirmaria ou refutaria a hipótese mais provável.

Uma telemetria útil tem owner, propósito, unidade, semântica estável, dimensões
limitadas, política de retenção e ação possível. O evento “algo falhou” sem nome da
operação, resultado e correlação pouco ajuda. Em sentido oposto, registrar payloads,
headers e objetos inteiros aumenta volume e risco sem garantir diagnóstico.

```mermaid
flowchart LR
    app["ASP.NET Core\nILogger + Activity + Meter"] -->|OTLP| collector["OpenTelemetry Collector"]
    collector --> processor["batch, limites e transformação"]
    processor --> traces[(Backend de traces)]
    processor --> metrics[(Backend de métricas)]
    processor --> logs[(Backend de logs)]
    deploy["deploy, versão, ambiente e flags"] -. contexto .-> app
    traces --> investigation["consulta e investigação"]
    metrics --> investigation
    logs --> investigation
```

O OpenTelemetry (OTel) padroniza APIs, SDK, convenções e protocolo de exportação;
não é o banco nem a interface de consulta. Na aplicação, a instrumentação produz
telemetria, o SDK agrega, amostra e exporta, e um **exporter** envia o sinal. Um
Collector entre aplicações e backends concentra batching, retry, filtragem,
enriquecimento e roteamento. Isso reduz acoplamento, mas adiciona componente que
precisa de capacidade, health, atualização e proteção de rede.

## Logs estruturados que preservam significado

Um log estruturado mantém mensagem e propriedades separadas. Com `ILogger<T>`, o
template é constante e os valores viram campos consultáveis:

```csharp
logger.LogInformation(
    new EventId(2702, "ConsultaConcluida"),
    "Consulta do pedido concluída com {QuantidadeItens} item(ns)",
    quantidade);
```

Interpolar `$"Pedido {id}"` transforma valores em texto, piora agrupamento e pode
formatar dados mesmo quando o nível está desabilitado. Para caminhos muito quentes,
source-generated logging com `LoggerMessage` reduz alocações; primeiro confirme a
necessidade por profiling.

### Níveis, eventos e contexto

| Nível | Uso operacional | Erro frequente |
| --- | --- | --- |
| `Trace`/`Debug` | diagnóstico temporário e detalhe de desenvolvimento | permanecer ligado globalmente em produção |
| `Information` | marcos esperados e mudanças relevantes de estado | registrar cada linha ou cada retry |
| `Warning` | anomalia recuperada ou degradação que merece atenção | tratar toda resposta 4xx como falha do servidor |
| `Error` | operação falhou e exige investigação ou ação | registrar e relançar em todas as camadas |
| `Critical` | processo ou capacidade crítica sem condição segura | usar como sinônimo de qualquer exceção |

`EventId` dá identidade estável ao tipo de evento; a mensagem pode evoluir. Escopos
acrescentam propriedades comuns durante uma operação. O runtime já correlaciona
logs com `TraceId` e `SpanId` quando o provider preserva scopes; não invente outro
identificador para substituir o trace. Um `Correlation-ID` de negócio pode coexistir
quando sua vida ultrapassa uma requisição, mas precisa de validação, tamanho máximo
e geração interna se vier de uma fronteira não confiável.

Capture exceção no argumento próprio de `ILogger`, preservando tipo e stack trace.
Registre-a na fronteira que sabe o resultado operacional; registrar em repositório,
serviço e middleware cria três eventos para uma falha. Cancelamento solicitado pelo
cliente não é automaticamente erro. Mensagens de domínio esperadas, falhas
transitórias e bugs precisam de níveis e resultados distintos.

### Dados sensíveis e excesso

Não registre senha, token, cookie, chave, connection string, documento, cartão,
payload completo ou header de autorização. “Mascarar depois” é uma defesa adicional,
não autorização para capturar. Prefira allowlist de propriedades, pseudonimização
quando necessária, acesso restrito, criptografia, retenção curta e teste de ausência.
IDs podem ser dados pessoais e ainda causam alto volume; avalie finalidade.

Logs excessivos consomem CPU, I/O, rede, indexação e atenção. Probes, retries e
sucessos de alta frequência são candidatos a filtro, agregação em métrica ou
amostragem. Limitar por nível sem conhecer categorias pode ocultar a única evidência
útil; faça mudanças reversíveis e com janela de diagnóstico definida.

## Métricas: tendências com dimensões controladas

Métricas são medições numéricas ao longo do tempo. Contadores representam totais
monotônicos, gauges valores atuais e histogramas distribuições. Em .NET, bibliotecas
e aplicações usam `System.Diagnostics.Metrics`; ASP.NET Core, `HttpClient` e runtime
já publicam instrumentos que podem ser coletados pelo SDK OTel ou por ferramentas
de diagnóstico.

Os quatro sinais de ouro ajudam a começar:

- **throughput:** requisições, mensagens ou operações por segundo;
- **latência:** distribuição de duração, não apenas média;
- **erros:** proporção de resultados que não cumpriram o contrato do usuário;
- **saturação:** proximidade do limite de CPU, memória, ThreadPool, conexões, filas
  ou concorrência do downstream.

Percentis respondem “quanto demorou para uma fração das operações”. `p95 = 400 ms`
significa que 95% das observações ficaram em até 400 ms na janela e agregação
considerada; não significa que um usuário específico sempre verá isso. A média
esconde caudas, mas percentis também dependem de buckets, janela, volume e método de
agregação. Não calcule média de percentis de instâncias.

Métricas técnicas incluem CPU, working set, heap, taxa e pausas de Garbage Collector
(GC), fila do ThreadPool, conexões ativas, pool de banco, backlog, I/O e duração de
dependências. Métricas de negócio representam resultados como pedidos aceitos,
pagamentos autorizados e idade de processos pendentes. Uma não substitui a outra:
CPU normal não prova que clientes conseguem comprar, e queda de vendas pode não ser
falha técnica.

### Cardinalidade e custo

Cada combinação de valores de atributos pode criar uma série. `metodo=GET` e
`resultado=sucesso` têm conjunto limitado; `usuario.id`, URL bruta, `pedido.id` ou
mensagem de exceção podem criar uma série por operação. O produto de dimensões
explode rapidamente memória, armazenamento e custo de consulta.

Use rota normalizada em vez de path, classe de resultado em vez de texto livre e
dependência lógica em vez de host efêmero. Mantenha IDs nos traces/logs quando forem
necessários para investigação e permitidos pela política. Antes de adicionar uma
dimensão, estime valores possíveis, combinações, taxa, retenção e consulta que ela
habilita. O custo de instrumentação inclui também SDK, serialização, buffers,
exportação, Collector, backend, dashboards, alertas e manutenção semântica.

## Distributed tracing e propagação de contexto

Um **trace** representa uma operação distribuída. Cada **span** representa uma
unidade de trabalho e contém tempo, status, atributos e eventos. `TraceId` liga todos
os spans; `SpanId` identifica um span. A relação pai-filho descreve causalidade e
permite decompor latência; links modelam relações não hierárquicas, úteis em fan-in
e processamento assíncrono.

```mermaid
sequenceDiagram
    participant C as Cliente
    participant P as API Pedidos
    participant H as HttpClient
    participant D as Catálogo
    C->>P: GET /pedidos (traceparent)
    activate P
    Note over P: span servidor<br/>TraceId T, SpanId A
    P->>H: consultar produto
    Note over H: span cliente<br/>TraceId T, SpanId B, pai A
    H->>D: HTTP + traceparent de B
    Note over D: span servidor<br/>TraceId T, SpanId C, pai B
    D-->>H: resposta
    H-->>P: duração/status da dependência
    P-->>C: resposta + TraceId para suporte
    deactivate P
```

O W3C Trace Context define `traceparent` e `tracestate`. Instrumentações de ASP.NET
Core e `HttpClient` criam spans e propagam contexto automaticamente. Não copie
headers manualmente quando a biblioteca já o faz. Mensageria exige injetar/extrair
contexto conforme a instrumentação e preservar a origem confiável; baggage atravessa
processos e não deve carregar segredo ou dado volumoso.

Uma chamada lenta pode ser localizada comparando duração do span servidor, spans de
dependência e lacunas. Um trace mostra uma amostra causal, não prova tendência;
confirme com métricas. Erros precisam de status coerente e atributos de baixa
cardinalidade. Grave exceção apenas quando contribui para diagnosticar a falha e
evite duplicá-la em cada span.

### Sampling

Head sampling decide no início do trace e é barato, mas pode descartar a falha rara.
Parent-based sampling mantém a decisão ao longo da árvore. Tail sampling decide
depois de observar spans no Collector e pode preservar erros ou traces lentos, ao
custo de buffer, latência e capacidade. Sampling reduz volume de traces; não torna
seguro coletar dados sensíveis e não deve distorcer métricas, que normalmente são
agregadas separadamente.

Calibre taxa por volume e valor diagnóstico. Registre a política, monitore perda do
exporter e preserve propagação mesmo quando um span não é gravado. “100% em produção”
é decisão de capacidade, não requisito de correlação.

## OpenTelemetry em ASP.NET Core

O laboratório [C27/C28](../../exemplos/c27-c28-observabilidade/) usa OpenTelemetry
.NET 1.21.0 e .NET 10. Ele demonstra:

- logs JSON estruturados com `EventId`, scopes e exceção sem payload sensível;
- spans automáticos de ASP.NET Core e `HttpClient`, além de span de domínio;
- propagação W3C em uma chamada HTTP para dependência simulada;
- métricas HTTP/runtime e contador/histograma de negócio com tags limitadas;
- resource attributes de serviço, versão e ambiente;
- exporter de console para aprendizado e OTLP opcional para Collector;
- liveness independente e readiness com uma dependência deliberadamente escolhida.

Semantic conventions estabilizam nomes e unidades entre instrumentações. Atributos
de resource descrevem o produtor (`service.name`, versão e ambiente), enquanto
atributos do span ou ponto métrico descrevem a operação. Não coloque `trace.id` como
resource nem IDs de instância como dimensão de negócio indiscriminadamente.

O exporter de console é didático e não é pipeline de produção. OTLP envia ao
Collector sem acoplar a aplicação a um backend. Quando o endpoint OTLP está
configurado, o exemplo também exporta logs estruturados, mantendo scopes; o console
JSON continua disponível para a execução local. O arquivo `otel-collector.yaml` usa
apenas receiver OTLP, limites de memória, batch e exporter `debug`; ele demonstra os
três pipelines, não persistência, alta disponibilidade ou segurança de produção.

### Como executar

```powershell
dotnet restore exemplos/c27-c28-observabilidade/C27C28.Observabilidade.csproj
$env:ASPNETCORE_URLS = "http://localhost:5080"
dotnet run --project exemplos/c27-c28-observabilidade/C27C28.Observabilidade.csproj
```

Em outro terminal:

```powershell
Invoke-RestMethod http://localhost:5080/pedidos/11111111-1111-1111-1111-111111111111?atrasoMs=350
Invoke-WebRequest http://localhost:5080/health/live
Invoke-WebRequest http://localhost:5080/health/ready
```

Os `350 ms`, o produto e a dependência são **simulados** para tornar o caminho
visível. Não são benchmark nem evidência de desempenho. Para usar um Collector
local compatível com OTLP, desabilite console e configure o endpoint:

```powershell
$env:Telemetry__ConsoleExporter = "false"
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
dotnet run --project exemplos/c27-c28-observabilidade/C27C28.Observabilidade.csproj
```

Valide o comportamento central:

```powershell
dotnet test exemplos/c27-c28-observabilidade/C27C28.Observabilidade.Tests.csproj --configuration Release
```

O teste confirma que liveness não herda a disponibilidade do catálogo, readiness
detecta indisponibilidade e a métrica de negócio não usa ID de pedido como dimensão.

## Health checks como contrato do orquestrador

**Liveness** responde se o processo deve ser reiniciado. Ela deve ser barata e não
falhar porque banco, DNS ou API externa estão indisponíveis; reiniciar todas as
réplicas durante falha do banco amplia o incidente. **Readiness** responde se a
instância deve receber novo tráfego e pode verificar apenas dependências necessárias
àquela capacidade. **Startup** dá tempo a inicializações legitimamente longas sem
confundi-las com travamento; não deve esconder boot ilimitado.

Probes frequentes geram carga e logs. Defina timeout, período, limiar e resposta sem
detalhes sensíveis. Uma dependência degradada pode permitir tráfego parcial ou
resposta cacheada; retirar toda instância pode ser pior. Health check não substitui
SLI: um endpoint verde sintético não prova que jornadas reais funcionam.

## Troubleshooting orientado por hipóteses

Investigar não é abrir painéis aleatoriamente. Preserve evidência e mantenha uma
linha do tempo:

1. defina impacto em usuários, capacidades, regiões, versões e duração;
2. alinhe relógios e marque o início, mudanças recentes, flags e eventos externos;
3. compare taxa, erro, latência e saturação com baseline e grupo não afetado;
4. formule hipótese falsificável, evidência esperada e consulta de menor custo;
5. encontre traces representativos e decomponha latência por dependência;
6. use `TraceId`, `EventId` e propriedades para buscar logs relacionados;
7. verifique pool, queries, locks e planos no banco sem assumir que “banco lento” é causa;
8. verifique timeouts, rate limits, DNS, conexões e saúde de dependências;
9. mitigue quando o impacto exigir, meça o efeito e atualize a hipótese;
10. registre lacunas de telemetria sem alterar dezenas de sinais durante o incidente.

Correlação temporal não prova causalidade. Um deploy próximo é hipótese forte, mas
volume, certificado, mudança de downstream ou tarefa agendada podem coincidir. Um
rollback que recupera o serviço é evidência importante, não necessariamente a causa
raiz completa.

## Performance em produção

Uma baseline precisa registrar versão, configuração, carga, distribuição de entrada,
ambiente e janela. Compare antes/depois sob condições equivalentes e instrumente a
mudança. Não anuncie ganho a partir de uma única requisição ou do laboratório deste
capítulo.

Para CPU alta, confirme processo e janela com métricas e colete profile pelo menor
tempo que responda à hipótese. Para memória, diferencie working set, heap gerenciado,
taxa de alocação, gerações, Large Object Heap, pausas e memória nativa; crescimento
não é vazamento até que retenção indevida seja demonstrada. Para latência com CPU
baixa, verifique I/O, conexões, locks, filas e dependências.

Starvation do ThreadPool aparece como fila e crescimento lento de threads, muitas
vezes por bloqueio síncrono sobre operações assíncronas. Contenção exige stacks e
tempo bloqueado, não apenas contagem de threads. GC frequente pode ser efeito de alta
alocação; aumentar heap sem entender retenção apenas desloca o limite.

Ferramentas abertas do SDK incluem `dotnet-counters` para triagem, `dotnet-trace`
para profile/eventos, `dotnet-gcdump` para heap e `dotnet-dump` para snapshot. Coleta
pode pausar, consumir CPU, gerar arquivos grandes e capturar dados; limite acesso,
duração, armazenamento e transferência. Observabilidade sempre ativa orienta a
hipótese; diagnóstico especializado coleta detalhe temporário.

## Armadilhas e decisões

- dashboard sem pergunta, owner ou ação não é observabilidade;
- alerta por CPU isolada tende a notificar sintoma sem impacto;
- URL, usuário ou erro como label de métrica cria cardinalidade sem controle;
- logar e relançar em toda camada multiplica um único erro;
- trace sem propagação vira ilhas e correlação por timestamp vira aproximação;
- sampling sem política pode eliminar exatamente a classe de falha procurada;
- readiness que consulta todas as dependências cria falha em cascata;
- Collector único e sem buffer pode se tornar ponto de perda ou pressão;
- instrumentação automática não conhece resultado de negócio; complemente-a com
  poucos spans e métricas de domínio.

Comece com telemetria nativa do framework, jornadas críticas e convenções comuns.
Adicione sinal customizado quando uma pergunta real permanecer sem resposta. Remova
sinais sem uso e trate mudanças de nome, unidade e atributo como evolução de contrato.

## Relação com outros capítulos

- [C18](../sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md)
  aplica correlação e traces a limites de microserviços.
- [C25](../qualidade-e-seguranca/c25-cobertura-e2e-carga.md) usa throughput,
  latência e saturação em testes de carga controlados.
- [C26](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md) define a
  proteção de segredos, dados e fronteiras usada pela telemetria.
- [C28](c28-confiabilidade-resposta-incidentes.md) transforma sinais em SLIs,
  alertas, decisões de entrega e resposta a incidentes.
- [C04](../linguagem-e-runtime/c04-memoria-runtime-performance.md) aprofunda
  runtime, memória, profiling e benchmarks.

## Referências

- [Diagnostics tools overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/)
  — Microsoft, sinais, coleta e ferramentas de diagnóstico .NET, acesso em 18 novembro 2026.
- [Logging in C# and .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview)
  — Microsoft, `ILogger`, logging estruturado e providers, acesso em 18 novembro 2026.
- [Creating metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation)
  — Microsoft, `System.Diagnostics.Metrics` e `IMeterFactory`, acesso em 18 novembro 2026.
- [Adding distributed tracing instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
  — Microsoft, `ActivitySource`, `Activity` e instrumentação, acesso em 18 novembro 2026.
- [Health checks in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)
  — Microsoft, endpoints, filtros e custo de probes, acesso em 18 novembro 2026.
- [Getting started with traces — ASP.NET Core](https://opentelemetry.io/docs/languages/dotnet/traces/getting-started-aspnetcore/)
  — OpenTelemetry, SDK e instrumentação ASP.NET Core, acesso em 18 novembro 2026.
- [OpenTelemetry .NET exporters](https://opentelemetry.io/docs/languages/dotnet/exporters/)
  — OpenTelemetry, console e OTLP exporters, acesso em 18 novembro 2026.
- [Collector configuration](https://opentelemetry.io/docs/collector/configuration/)
  — OpenTelemetry, receivers, processors, exporters e pipelines, acesso em 18 novembro 2026.
- [Sampling](https://opentelemetry.io/docs/concepts/sampling/)
  — OpenTelemetry, head, tail e parent-based sampling, acesso em 18 novembro 2026.
- [Semantic conventions for .NET](https://opentelemetry.io/docs/specs/semconv/dotnet/)
  — OpenTelemetry, convenções de traces e métricas .NET, acesso em 18 novembro 2026.
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
  — W3C Recommendation, `traceparent` e `tracestate`, acesso em 18 novembro 2026.
