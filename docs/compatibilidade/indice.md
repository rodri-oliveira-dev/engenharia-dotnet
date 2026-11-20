# Compatibilidade e evolução do .NET

Guias de atualização de **.NET 10 para .NET 11**, em português. São
documentos complementares aos capítulos, não capítulos numerados novos:
cada um descreve as diferenças relevantes, o impacto, a correção e
como verificar a compatibilidade. A lista oficial de breaking changes
é dinâmica e declaradamente não exaustiva.

## Guias

| Guia | Assuntos e risco principal |
| --- | --- |
| [ASP.NET Core 11](compatibilidade-aspnet-core-11.md) | pipeline, OpenAPI, rate limiting, Kestrel, antiforgery e contratos HTTP |
| [Runtime, BCL e Hosting 11](compatibilidade-runtime-bcl-11.md) | parsing, ZIP, configurações, trabalhadores, TLS e métricas |
| [SDK, build, NuGet e containers 11](compatibilidade-sdk-build-11.md) | restore, CI, VSTest, containers e Native AOT |
| [C# 15 e compilador](compatibilidade-csharp-15.md) | mudanças de sintaxe, ref safety e verificação de código-fonte |
| [EF Core 11](../aplicacoes-e-dados/ef-core-11-compatibilidade.md) | tradução LINQ, provedores, migrations e SQL |

## Como ler e aplicar

1. Inventarie o SDK efetivo, runtime, versões dos pacotes e provedores.
2. Distinga **incompatibilidade de código-fonte**, **binária** e
   **comportamental**; ausência de erro de build não prova compatibilidade.
3. Para cada cenário aplicável, compare comportamento antigo e novo
   com testes focados no contrato, na infraestrutura e na execução.
4. Migre com rollback preparado, monitore métricas e erros após o deploy.
5. Registre explicitamente cenários não cobertos em vez de marcar tudo
   como compatível apenas por passar nos testes atuais.

## Evidência automatizada

Os casos [CompatibilidadeDotnet11Tests.cs](../../exemplos/c06-c07-aspnet-core/CompatibilidadeDotnet11Tests.cs)
e [CompatibilidadeRuntime11Tests.cs](../../exemplos/c05-assincronismo-concorrencia/CompatibilidadeRuntime11Tests.cs)
demonstram comportamento do **runtime 11** para configuração, parsing
e falhas de serviço em segundo plano. Um teste no runtime 11
**não substitui** a execução comparativa em .NET 10 nem demonstra a
compatibilidade de Kestrel, produção, containers ou Native AOT.

```bash
dotnet test exemplos/c06-c07-aspnet-core/C06C07.AspNetCore.Tests.csproj
dotnet test exemplos/c05-assincronismo-concorrencia/C05.AssincronismoConcorrencia.Tests.csproj
```

Os testes fazem parte da solution e passam pelo mesmo gate de CI.
Não são utilizados serviços externos nesses casos.

## Fontes

- [Breaking changes no .NET 11](https://learn.microsoft.com/en-us/dotnet/core/compatibility/11)
- [Breaking changes no ASP.NET Core 11](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/overview)
- [Mudanças no compilador C# até o SDK 11.0.100](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011)

**Revisão das fontes:** 20 de novembro de 2026.
