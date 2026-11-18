# C06 — Composição e pipeline no ASP.NET Core

> **Nível:** Fundamental
>
> **Objetivo:** compor serviços, configuração e preocupações transversais com
> ciclos de vida explícitos e falhas detectadas antes de receber tráfego.
>
> **Pré-requisitos:** [C02 — Nulabilidade, falhas e exceções](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md).
>
> **Última revisão:** 2026-11-18

Uma aplicação ASP.NET Core não é apenas um conjunto de endpoints. O container de
injeção de dependência define quanto tempo objetos e recursos permanecem vivos;
o pipeline HTTP define quais preocupações cercam cada requisição; e a
configuração define se a aplicação consegue operar no ambiente atual. Erros
nessas três fronteiras aparecem como estado compartilhado indevido, vazamento de
recursos, respostas inconsistentes ou falhas tardias em produção.

Este capítulo trata composição, lifetimes, middleware, filters, Problem Details
e Options. Semântica REST, cache distribuído e resiliência de clientes HTTP são
canônicos no [C07](c07-apis-http-integracoes-resilientes.md). Autorização e
proteção de dados pertencem ao C26.

## Lifetimes são contratos de propriedade

Registrar um serviço não responde apenas como construí-lo. Também define quem
possui a instância, quando ela pode ser compartilhada e quando será descartada.

| Lifetime | Criação | Descarte típico | Uso apropriado |
|---|---|---|---|
| `Transient` | a cada resolução | ao encerrar o escopo que o capturou | operações pequenas e sem estado compartilhado |
| `Scoped` | uma vez por escopo | ao encerrar a requisição ou escopo explícito | unidade de trabalho e estado da operação |
| `Singleton` | uma vez no container raiz | no encerramento da aplicação | estado global deliberado ou serviço thread-safe |

Em aplicações HTTP, o framework cria um escopo por requisição. Duas resoluções
do mesmo serviço scoped dentro da mesma requisição devolvem a mesma instância;
requisições diferentes recebem instâncias diferentes. Isso não significa que o
escopo acompanha automaticamente tasks iniciadas e abandonadas. Trabalho que
sobrevive à resposta precisa de um proprietário, como um serviço hospedado.

Transient não significa que o chamador deve executar `Dispose`. Quando o
container cria uma dependência descartável, o container a rastreia e descarta no
fim do escopo correspondente. Resolver transients descartáveis repetidamente no
container raiz pode retê-los até o shutdown e transformar uma escolha local em
pressão de memória.

Singletons permanecem alcançáveis por toda a aplicação. Qualquer estado mutável
precisa suportar acesso concorrente. Imutabilidade, `Interlocked`, locks ou
coleções concorrentes podem proteger operações específicas, mas a invariante
completa precisa continuar válida. Um singleton não deve guardar `HttpContext`,
DTOs mutáveis da requisição nem buffers reutilizados sem sincronização.

## Resolução e composição

Prefira constructor injection: dependências obrigatórias ficam visíveis, o tipo
não precisa localizar serviços e testes podem fornecer implementações explícitas.
Agrupe registros por responsabilidade em métodos de extensão quando o
`Program.cs` perder a função de mapa da aplicação.

```csharp
builder.Services.AddTransient<OperacaoTransient>();
builder.Services.AddScoped<ContextoDaRequisicao>();
builder.Services.AddSingleton<EstadoGlobalSeguro>();
```

Evite chamar `BuildServiceProvider` durante o registro. Isso cria um segundo
container, com singletons, descarte e opções independentes. Evite também usar
`IServiceProvider` como service locator no domínio. Resolução dinâmica é
adequada em infraestrutura que realmente precisa escolher implementações ou
criar escopos, não para esconder dependências comuns.

Registros duplicados têm semântica própria: resolver um serviço único geralmente
retorna o último registro, enquanto `IEnumerable<T>` contém todos. A ordem pode
ser importante para handlers e estratégias; torne-a deliberada e teste a
composição final, não apenas cada classe isolada.

## Captive dependency

Uma captive dependency ocorre quando um objeto de vida longa captura outro de
vida curta. O caso clássico é um singleton recebendo um serviço scoped no
construtor:

```csharp
builder.Services.AddScoped<ContextoDaRequisicao>();
builder.Services.AddSingleton<ServicoGlobalInvalido>();

public sealed class ServicoGlobalInvalido(ContextoDaRequisicao contexto);
```

O singleton é construído uma vez. O scoped capturado deixa de acompanhar
requisições, mantém o primeiro estado e só é descartado com o container raiz.
Com um `DbContext`, isso pode misturar unidades de trabalho, provocar acesso
concorrente a um tipo não thread-safe e reter conexões ou entidades.

A primeira solução é alinhar lifetimes: frequentemente o consumidor também deve
ser scoped. Se um singleton ou `BackgroundService` precisa executar uma unidade
de trabalho independente, injete `IServiceScopeFactory`, crie e descarte um
escopo por operação e não deixe objetos resolvidos escaparem:

```csharp
public sealed class Executor(IServiceScopeFactory scopeFactory)
{
    public async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var operacao = scope.ServiceProvider.GetRequiredService<OperacaoScoped>();
        await operacao.ExecutarAsync(cancellationToken);
    }
}
```

Criar um escopo não transforma a dependência em thread-safe. Cada execução
concorrente precisa de seu próprio escopo, e a task precisa ser aguardada antes
do descarte.

### Validação do container

`ValidateScopes` detecta resolução de scoped pelo container raiz e injeção de
scoped em singleton. `ValidateOnBuild` tenta construir os grafos registrados
quando o provider é criado, encontrando dependências ausentes e parte dos ciclos
antes da primeira requisição.

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

O ambiente Development habilita validações úteis por padrão, mas depender do
ambiente permite que um erro chegue a produção. Habilitar explicitamente torna o
contrato uniforme. A validação não executa todas as factories, não prova thread
safety e não alcança objetos criados manualmente; testes de startup continuam
necessários.

## O pipeline HTTP é ordenado

Middleware envolve a próxima etapa. A ordem de registro é a ordem de entrada e
se inverte na saída. Um middleware pode executar antes e depois do próximo,
encerrar o pipeline ou transformar a resposta.

```text
requisição
  → correlation e contexto de log
    → tratamento global de exceções
      → roteamento, autenticação e autorização
        → filtros do endpoint
          → handler
resposta ← etapas na ordem inversa
```

Ordem é comportamento. Tratamento de exceções precisa cercar as etapas cujas
falhas deve capturar. Autenticação deve preceder autorização. CORS, cache,
compressão e arquivos estáticos possuem restrições próprias. Não copie uma lista
universal: declare quais garantias cada componente precisa antes e depois dele.

### Middleware, filters e endpoint filters

Use middleware para uma preocupação transversal a muitos endpoints e baseada em
HTTP: correlação, tratamento global, headers, autenticação ou telemetria. O
middleware convencional é singleton; injete serviços scoped em `InvokeAsync`,
ou use factory-based middleware para injeção no construtor por requisição.

Filters MVC executam dentro do pipeline de actions e conhecem controllers,
model binding, action results e exceções MVC. São apropriados para convenções
específicas de controllers. Exception filters não capturam falhas ocorridas
antes do MVC e não substituem tratamento global.

Endpoint filters envolvem handlers de Minimal APIs. Podem inspecionar argumentos
já vinculados, validar DTOs ou aplicar uma política a um grupo de rotas. Como são
mais locais que middleware, evitam que uma regra de um endpoint afete tráfego
não relacionado. Regras de domínio continuam no serviço de aplicação, não no
filter.

No MVC, authorization filters vêm primeiro; resource filters envolvem model
binding; action filters cercam a action; exception filters observam parte das
falhas; result filters cercam a execução do resultado. Dentro de uma categoria,
escopo global, controller e action influenciam a ordem de entrada, invertida na
saída. Endpoint filters também formam uma cadeia: o primeiro registrado executa
antes dos seguintes e termina depois deles. Dependência acidental dessa ordem
merece teste de integração.

| Necessidade | Mecanismo preferido |
|---|---|
| todas as requisições HTTP | middleware |
| convenção de controllers/actions | filter MVC |
| argumentos ou resultado de Minimal API | endpoint filter |
| invariante de negócio reutilizada fora do HTTP | serviço de aplicação/domínio |

## Erros, validação e contexto operacional

`AddProblemDetails` registra escrita consistente no formato Problem Details.
`IExceptionHandler`, disponível no ASP.NET Core 8 ou posterior, centraliza o
mapeamento de exceções conhecidas e desconhecidas; `UseExceptionHandler` ativa o
middleware que chama esses handlers.

Uma resposta de erro deve expor contrato, não detalhes internos. Inclua status,
título, tipo estável, erros de validação e um identificador de correlação. Não
retorne stack trace, SQL, caminhos locais ou segredo. Exceções esperadas podem
virar `404`, `409` ou outro status definido; falhas inesperadas permanecem `500`
e são registradas uma vez na fronteira.

No .NET 10, exceções que um `IExceptionHandler` considera tratadas têm seus
diagnósticos suprimidos por padrão. `SuppressDiagnosticsCallback` permite decidir
quais continuam gerando logs e métricas. Isso evita duplicação, mas pode ocultar
falhas se o mapeamento for amplo demais.

Correlation ID conecta resposta, logs e chamadas downstream. Prefira o contexto
de trace padronizado quando disponível; aceite um header externo apenas após
validar tamanho e caracteres. Coloque o valor em um logging scope e devolva-o na
resposta final. O laboratório usa `Response.OnStarting` porque o middleware de
exceção pode limpar headers escritos antes da falha.

Validação de formato pertence à fronteira. Controllers com `[ApiController]`
oferecem validação de modelo integrada. Minimal APIs exigem endpoint filter,
binder ou validação explícita. Uma requisição sintaticamente válida ainda pode
violar uma invariante; essa regra pertence à aplicação ou ao domínio e pode
resultar em conflito, não apenas em `400`.

## Configuração tipada e falha no startup

`IConfiguration` combina providers em uma árvore chave-valor. É útil na raiz de
composição, mas espalhá-lo pela aplicação transforma chaves textuais em uma API
implícita. O Options Pattern cria classes coesas, validação e dependências
declaradas.

| Interface | Lifetime | Atualização | Uso típico |
|---|---|---|---|
| `IOptions<T>` | singleton | valor estável após criação | configuração fixa |
| `IOptionsSnapshot<T>` | scoped | recalculada por requisição | consumidores scoped que aceitam reload |
| `IOptionsMonitor<T>` | singleton | valor atual e callbacks | singleton que precisa observar reload |

`IOptionsSnapshot<T>` não pode ser capturado por singleton. Use
`IOptionsMonitor<T>` quando o singleton realmente suporta mudança segura. Reload
de configuração não garante que conexões, clientes e outros recursos já criados
sejam reconstruídos de forma atômica.

```csharp
builder.Services.AddOptions<IntegracaoOptions>()
    .Bind(builder.Configuration.GetSection("Integracao"))
    .ValidateDataAnnotations()
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "BaseUrl precisa ser absoluta.")
    .ValidateOnStart();
```

Sem `ValidateOnStart`, a primeira resolução pode descobrir a configuração ruim
somente durante uma requisição. Falhar no startup impede uma instância inválida
de entrar no balanceador. A validação deve conferir formato, faixa e combinações
locais; testar disponibilidade de um serviço externo no startup acopla deploy à
saúde momentânea da dependência e precisa de decisão operacional explícita.

### Configuração não é sinônimo de segredo

Arquivos `appsettings.json` podem conter URLs, limites e nomes não sensíveis.
Variáveis de ambiente sobrescrevem arquivos e usam `__` para hierarquia em
plataformas onde `:` não funciona. Elas são convenientes, mas podem aparecer em
diagnósticos, consoles e inventários; não são um cofre por si mesmas.

User Secrets evita versionar credenciais de desenvolvimento, mas não criptografa
o arquivo e não é solução de produção. Produção deve usar um secret manager, por
exemplo Azure Key Vault ou serviço equivalente, com identidade da workload,
auditoria, rotação e menor privilégio. Nunca registre o grafo completo de
configuração nem devolva valores secretos em endpoints de diagnóstico.

## Exemplo executável

O laboratório compartilhado em `exemplos/c06-c07-aspnet-core/` demonstra:

- os três lifetimes e validação de captive dependency;
- uso de `IServiceScopeFactory` sem fazer scoped escapar;
- correlation ID, logging scope, `IExceptionHandler` e Problem Details;
- endpoint filter para validação;
- Options validadas com falha no startup.

```bash
dotnet test exemplos/c06-c07-aspnet-core/C06C07.AspNetCore.Tests.csproj
dotnet run --project exemplos/c06-c07-aspnet-core/C06C07.AspNetCore.csproj
```

O projeto usa .NET 10. O teste de integração usa `TestServer`, sem abrir porta e
sem serviço externo. `AddDistributedMemoryCache` existe apenas como substituto
didático da abstração; não oferece distribuição entre processos.

## Boas práticas e armadilhas

- Mantenha `Program.cs` como mapa de composição e extraia grupos coesos, não uma
  cadeia de extensões que esconda toda a aplicação.
- Valide o provider e Options no startup em testes e no host real.
- Não injete scoped no construtor de middleware convencional ou singleton.
- Não crie escopo apenas para fazer a exceção desaparecer; defina a unidade de
  trabalho e aguarde seu término.
- Não capture `HttpContext` fora da requisição.
- Registre uma falha uma vez na fronteira que decide a resposta.
- Não use exception filter como tratamento global de todo o pipeline.
- Não exponha mensagens arbitrárias de exceção ao cliente.

## Trade-offs e critérios de uso

Transient reduz compartilhamento acidental, mas aumenta construções e descarte.
Scoped alinha estado à operação HTTP, mas exige escopo explícito fora dela.
Singleton economiza construção e permite estado global, mas aumenta retenção,
contenção e exigência de thread safety.

Middleware oferece abrangência e ordem explícita, ao custo de afetar todo o
tráfego. Filters têm contexto rico do framework, ao custo de acoplamento a MVC
ou Minimal APIs. Options tipadas adicionam classes e validação, mas removem
chaves textuais dispersas e falhas tardias.

Use os mecanismos quando seu alcance coincide com o problema. Evite criar uma
camada, filter ou interface para cada função pequena: composição simples e
testável é preferível a indireção sem política ou substituição real.

## Relação com outros capítulos

- **Pré-requisito:** [C02](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md)
  define falhas esperadas e fronteiras de exceção.
- **Continuação:** [C07 — APIs HTTP e integrações resilientes](c07-apis-http-integracoes-resilientes.md)
  aplica o pipeline a contratos, cache e clientes externos.
- **Concorrência:** [C05](../linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md)
  fundamenta cancelamento, thread safety e ciclo de vida de tasks.
- **Aplicações:** [C09 — Fronteiras e concorrência no EF Core](c09-fronteiras-concorrencia-ef-core.md)
  usa scoped para unidade de trabalho; C26 trata autorização e segredos; C27
  aprofunda telemetria distribuída.

## Referências

- [Lifetimes de serviços](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/service-lifetimes)
  — documentação .NET, acesso em 18 novembro 2026.
- [Diretrizes de injeção de dependência](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-guidelines)
  — documentação .NET, acesso em 18 novembro 2026.
- [Middleware no ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
- [Filters de Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
- [Filters no ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/controllers/filters?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
- [Tratamento de erros no ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
- [Options Pattern](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
- [Configuração no ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0)
  — ASP.NET Core 10, acesso em 18 novembro 2026.
