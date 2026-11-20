# Compatibilidade: runtime, BCL e Hosting no .NET 11

> **Nível:** Avançado
>
> **Objetivo:** reconhecer mudanças comportamentais que passam
> pelo build e podem afetar dados, execução ou operação.
>
> **Última revisão:** 2026-11-20

Este guia complementa [C02 — Falhas e exceções](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md),
[C04 — Runtime e performance](../linguagem-e-runtime/c04-memoria-runtime-performance.md),
[C05 — Assincronicidade](../linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md)
e [C27 — Telemetria](../operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md).

## Matriz de mudanças

| Mudança | Tipo | Risco | Validação |
| --- | --- | --- | --- |
| `DateOnly`/`TimeOnly.TryParse*` rejeitam estilos e formatos inválidos com exceção | comportamento | antes retornava `false`; configuração defeituosa passa a interromper fluxo | teste de argumentos inválidos e de dado malformado |
| Configuração encadeada preserva valor `""` | comportamento | substitui valor anterior mesmo quando vazio | teste de precedência com `AddConfiguration` |
| Falha em `BackgroundService` propaga exceção no host | comportamento | `RunAsync`/`StopAsync` deixam de completar com sucesso | serviço de teste que falha e verificação da exceção |
| ZIP verifica CRC32 ao ler entradas | comportamento | arquivos corrompidos podem falhar em leitura | validar tratamento de dados danificados |
| TAR verifica checksum de cabeçalhos | comportamento | entrada antes tolerada pode ser rejeitada | fixture TAR inválida controlada |
| Deflate/GZip emitem cabeçalhos/rodapés para payload vazio | comportamento | binário de saída muda | teste de round-trip e tamanho |
| `http.client.open_connections`/`active_requests` passam a observable counters | comportamento | leitores `MeterListener` de eventos push não recebem incrementos | chamar `RecordObservableInstruments()` |
| `SslStream` servidor deixa de baixar intermediários AIA por padrão | comportamento | autenticação mTLS sem cadeia completa pode falhar | teste TLS com CA/cadeia de certificados |
| Pacotes `Microsoft.Extensions.*` no shared framework | comportamento | resolução de assemblies pode usar a versão do runtime 11 | inventário de referências e integração real |

## Exemplo 1: TryParse não valida a programação do chamador

**Até .NET 10**, um `DateTimeStyles` inválido podia provocar
`false` sem exceção mesmo que o valor textual estivesse correto.

**No .NET 11:**

```csharp
using System.Globalization;

bool entradaInvalida = DateOnly.TryParse(
    "não é uma data",
    CultureInfo.InvariantCulture,
    DateTimeStyles.None,
    out _); // false: dado inválido, argumentos válidos

// Programação incorreta: agora lança ArgumentException.
_ = DateOnly.TryParseExact(
    "2026-11-20",
    "yyyy-MM-dd",
    CultureInfo.InvariantCulture,
    (DateTimeStyles)999,
    out _);
```

A correção é garantir **estilo e formatos válidos** no código,
não engolir toda exceção e tratar como entrada inválida.
O teste de regressão está em
[CompatibilidadeRuntime11Tests.cs](../../exemplos/c05-assincronismo-concorrencia/CompatibilidadeRuntime11Tests.cs).

## Exemplo 2: configuração encadeada e string vazia

**Antes, .NET 10:** um valor vazio adicionado via
`AddConfiguration(inner)` era tratado como ausente; o provider
anterior podia prevalecer.

**Agora, .NET 11:**

```csharp
using Microsoft.Extensions.Configuration;

IConfiguration inner = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Endpoint"] = ""
    })
    .Build();

IConfiguration outer = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Endpoint"] = "https://servico.exemplo"
    })
    .AddConfiguration(inner)
    .Build();

// Agora é string vazia, não o endereço anterior.
Console.WriteLine(outer["Endpoint"]);
```

A correção, caso o sistema dependesse de fallback, é remover a chave
do provider de maior prioridade ou representá-la como ausente;
a mudança não tem um switch de compatibilidade. Verifique também
binding de números e opções, pois string vazia pode causar erro de
conversão. Teste em
[CompatibilidadeDotnet11Tests.cs](../../exemplos/c06-c07-aspnet-core/CompatibilidadeDotnet11Tests.cs).

## Exemplo 3: BackgroundService e código de saída

**Antes, .NET 10:** se `BackgroundService` falhasse e
`HostOptions.BackgroundServiceExceptionBehavior` fosse
`StopHost`, os métodos de espera/parada podiam concluir sem exceção,
resultando em código de saída de sucesso.

**Agora, .NET 11:** `RunAsync`, `StopAsync` e
`WaitForShutdownAsync` propagam falha; múltiplas falhas podem
ser agrupadas. Isso permite que orchestrators percebam o erro.

```csharp
try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Falha do host");
    throw; // preserva falha para supervisor/process manager
}
```

Não capture a exceção apenas para forçar exit code 0. O
[teste de regressão](../../exemplos/c05-assincronismo-concorrencia/CompatibilidadeRuntime11Tests.cs)
usa um worker propositalmente defeituoso para provar a propagação
no runtime 11.

## Casos que exigem infraestrutura específica

- **ZIP e TAR:** use corpus de arquivos legítimos e corrompidos,
  observando quando a exceção surge; não relaxe a validação de checksums.
- **TLS/mTLS:** forneça cadeia intermediária completa no cliente
  ou na `X509ChainPolicy.ExtraStore`. Não reabilite downloads AIA
  indiscriminadamente, porque isso impacta latência e segurança.
- **Métricas HttpClient:** leitores manuais via `MeterListener`
  devem chamar `RecordObservableInstruments()`; SDKs OpenTelemetry
  baseados em coleta periódica normalmente já o fazem.
- **Runtime async e JIT:** melhorias de performance não são
  breaking changes presumidos. Meça CPU, alocação e latência
  em condições comparáveis antes de refatorar.

## Checklist

1. Executar testes focados no parser e na configuração efetiva.
2. Verificar o exit code de serviços que falham e os alertas do supervisor.
3. Validar arquivos compactados de fronteira e cadeias mTLS reais.
4. Inspecionar cardinalidade e comportamento das métricas após o deploy.
5. Separar o que foi **testado** do que exige infraestrutura não presente no CI.

## Referências

- [Breaking changes .NET 11](https://learn.microsoft.com/en-us/dotnet/core/compatibility/11)
- [DateOnly e TimeOnly](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/11/dateonly-timeonly-tryparse-argumentexception)
- [Configuração encadeada](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/11/chained-configuration-empty-string)
- [BackgroundService e host](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/11/ihost-runasync-stopasync-throw-backgroundservice-failure)
- [Métricas HttpClient](https://learn.microsoft.com/en-us/dotnet/core/compatibility/networking/11/http-metrics-observable-counters)
- [SslStream AIA](https://learn.microsoft.com/en-us/dotnet/core/compatibility/networking/11/sslstream-aia-downloads-disabled)

[Voltar à trilha](indice.md).
