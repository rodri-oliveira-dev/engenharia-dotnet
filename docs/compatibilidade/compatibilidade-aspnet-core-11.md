# Compatibilidade: ASP.NET Core 10 → 11

> **Nível:** Avançado
>
> **Objetivo:** preservar contratos HTTP, segurança e observabilidade
> durante a atualização de aplicações web.
>
> **Última revisão:** 2026-11-20

Este documento complementa [C06 — Pipeline](../aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md),
[C07 — APIs HTTP](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md)
e [C26 — Segurança](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md).
As mudanças abaixo estão nas notas oficiais do ASP.NET Core 11; não
se deve assumir que toda aplicação usa os componentes afetados.

## Matriz de mudanças

| Mudança | Tipo | Impacto | Como verificar |
| --- | --- | --- | --- |
| `Microsoft.AspNetCore.ConcurrencyLimiter` removido | fonte/binário | API `UseConcurrencyLimiter` indisponível na linha 11 | procurar uso legado e compilar com `Microsoft.AspNetCore.RateLimiting` |
| `Microsoft.OpenApi` 2.x → 3.x | fonte | transformers customizados podem deixar de compilar | compilar transformers e comparar schema/contrato publicado |
| Kestrel mais estrito em HTTP/1.1, 2 e 3 | comportamento | proxies/clientes com headers inválidos podem falhar | teste HTTP com proxy real e requisições de fronteira |
| Blazor SSR delega antiforgery ao middleware | comportamento | formulários dependem da configuração do pipeline | POST com token válido, ausente e inválido |
| Hosting emite tags de convenções semânticas HTTP | comportamento | duplicidade/cardinalidade em dashboards customizados | comparar atributos e volume de telemetria |
| `OpenApiVersion` adota default 3.2 | comportamento | ferramentas consumidoras podem rejeitar documento | validar consumidor e versão explicitamente |
| `ResponseCompression` emite `Vary: Accept-Encoding` mesmo sem comprimir | comportamento | altera a chave de cache também para respostas não comprimidas | testar `Accept-Encoding: gzip` e `identity`, inspecionando `Vary`, `Content-Encoding` e cache intermediário |

As categorias e detalhes devem ser confirmados nas notas completas
antes de atualizar um serviço que usa endpoints ou middlewares
específicos.

## Exemplo 1: substituir ConcurrencyLimiter

**Antes, ASP.NET Core até 10:**

<!-- snippet-csharp: id=compat-asp11-01; categoria=fragmento -->
```csharp
builder.Services.AddQueuePolicy(options =>
{
    options.MaxConcurrentRequests = 2;
    options.RequestQueueLimit = 25;
});

app.UseConcurrencyLimiter();
```

**Depois, ASP.NET Core 11:**

<!-- snippet-csharp: id=compat-asp11-02; categoria=contexto -->
```csharp
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddConcurrencyLimiter("politica", limiter =>
    {
        limiter.PermitLimit = 2;
        limiter.QueueLimit = 25;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

app.UseRateLimiter();
app.MapGet("/processamento", () => Results.Ok())
   .RequireRateLimiting("politica");
```

O exemplo **não é uma substituição literal** de todas as políticas
antigas. Valide semântica da fila, timeout, status de rejeição
e comportamento sob concorrência. A política nomeada associada por
`RequireRateLimiting("politica")` limita **somente os endpoints marcados**;
as demais rotas não recebem essa política. Já
`RateLimiterOptions.GlobalLimiter` aplica a limitação a **todas as
requisições**. Se a intenção é preservar o alcance global do antigo
`UseConcurrencyLimiter()`, configure `GlobalLimiter` com parâmetros
equivalentes e revise os endpoints afetados.

**Ordem do middleware:** no exemplo com hospedagem mínima, mantenha o
pipeline mostrado. Em aplicações que chamam `UseRouting()` de modo
explícito, registre `UseRateLimiter()` **depois de `UseRouting()`**
quando usar políticas por endpoint, como `RequireRateLimiting("politica")`.
Se houver apenas `GlobalLimiter`, ele pode ser colocado antes do
roteamento. Verifique também o status HTTP de rejeição e a prioridade
de outras regras de limitação.

## Exemplo 2: OpenAPI 3.x

Se a aplicação apenas expõe endpoints comuns, a migração pode exigir
pouco código. Se implementa `IOpenApiDocumentTransformer`,
`IOpenApiOperationTransformer` ou `IOpenApiSchemaTransformer`,
as interfaces continuam, mas os tipos `OpenApiDocument`,
`OpenApiOperation` e `OpenApiSchema` do pacote 3.x mudaram.
Não substitua nomes de propriedades às cegas.

**Antes:** compile contra `Microsoft.OpenApi` 2.x, armazene a
descrição OpenAPI publicada e valide clientes gerados.

**Depois:** atualize pacotes, adapte os transformers ao novo
modelo de objetos, compile e compare `paths`, `schemas`,
`security`, status codes e versão emitida. O arquivo gerado
deve ser validado contra o consumidor real.

## Exemplo 3: Kestrel, proxy e headers

O ASP.NET Core 11 passa a rejeitar outros headers de conexão
em HTTP/2/3, além de fechar a conexão HTTP/1.1 após requisição
que inclua simultaneamente `Content-Length` e
`Transfer-Encoding`. É uma mudança de conformidade de protocolo,
não uma recomendação para afrouxar o servidor.

**Antes:** alguns clientes malformados conseguiam continuar
a conexão após esses headers.

**Depois:** atualize o cliente ou proxy e confirme com testes
que exercitem HTTP/1.1, HTTP/2 e, quando aplicável, HTTP/3 no
ambiente onde Kestrel realmente termina a conexão.
`TestServer` não substitui esses testes de rede.

## Exemplo 4: compactação de respostas e cache HTTP

No ASP.NET Core 10, o middleware `ResponseCompression` acrescentava
`Vary: Accept-Encoding` apenas quando compactava efetivamente a
resposta. No ASP.NET Core 11, o cabeçalho também é emitido em
**respostas não comprimidas** que atravessam o middleware.

Para comparar o comportamento das versões, use o mesmo endpoint
com conteúdo elegível para compactação e duas requisições:

1. Envie `Accept-Encoding: gzip`. Verifique a presença de
   `Content-Encoding: gzip` quando a compactação for aplicada e do
   cabeçalho `Vary: Accept-Encoding`.
2. Envie `Accept-Encoding: identity`. Verifique que **não** houve
   compactação (`Content-Encoding` ausente) e que
   **`Vary: Accept-Encoding` ainda está presente** no .NET 11.
   Esse segundo cenário é o que distingue o comportamento anterior.

No proxy reverso ou CDN, confirme que as variantes comprimidas e não
comprimidas não sejam compartilhadas incorretamente e que
`Accept-Encoding` seja normalizado para evitar multiplicação de
entradas de cache. Caso o endpoint ou outro middleware já adicione
`Vary`, verifique que `Accept-Encoding` não seja duplicado.
O teste deve usar a compressão realmente habilitada, sem assumir que
`TestServer` reproduz o comportamento de um cache intermediário.

## Exemplo 5: proteção de formulários

Blazor SSR no ASP.NET Core 11 usa o resultado registrado por middleware
antiforgery, em vez de validar sempre no endpoint.
`UseAntiforgery()` mantém validação baseada em token;
a proteção automática de CSRF introduzida no pipeline de
`WebApplication` não deve ser confundida com geração de token
em todos os formulários. Compare a configuração efetiva do
aplicativo, especialmente quando há cookies.

**Critério de aceite:** operações autenticadas com cookie
devem rejeitar requisições cross-site não autorizadas sem bloquear
o fluxo legítimo do formulário.

## Checklist de migração

1. Atualizar SDK/TFM, pacotes ASP.NET Core e componentes OpenAPI em conjunto.
2. Compilar e executar testes HTTP com `TestServer` para
   binding, handlers e contratos já cobertos pelo [laboratório
   C06/C07](../../exemplos/c06-c07-aspnet-core/).
3. Rodar testes reais em Kestrel para protocolo, TLS e proxies.
4. Testar formulários Blazor e antiforgery conforme o modelo de autenticação.
5. Comparar esquema OpenAPI e o resultado de SDKs gerados.
6. Comparar respostas comprimidas e não comprimidas, cabeçalhos e cache HTTP.
7. Verificar métricas e dashboards após a publicação.

**Evidência no repositório:** testes de binding do ASP.NET Core 11
já existem em `AplicacaoTests.cs`; esta seção de compatibilidade
não introduz um servidor de rede ou uma instalação de navegador no CI.

## Referências

- [Breaking changes ASP.NET Core 11](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/overview)
- [Migração 10 para 11](https://learn.microsoft.com/en-us/aspnet/core/migration/100-to-110)
- [ConcurrencyLimiter removido](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/concurrencylimiter-removed)
- [Kestrel mais estrito](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/kestrel-strict-protocol-compliance)
- [Microsoft.OpenApi 3.x](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/microsoft-openapi-3x)
- [Blazor SSR e antiforgery](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/blazor-server-side-rendering-deferred-cross-site-request-forgery-protection)
- [Middleware de rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [ResponseCompression e o cabeçalho Vary no ASP.NET Core 11](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/response-compression-always-vary)

[Voltar à trilha](indice.md).
