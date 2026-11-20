# Testes arquiteturais

Este projeto transforma em verificações executáveis apenas as fronteiras que a
solução de exemplos declara. Ele lê `EngenhariaDotnet.slnx` e os `.csproj`
referenciados, sem adicionar uma biblioteca de análise arquitetural.

Os laboratórios de C11–C12 e C13–C17 agrupam responsabilidades diferentes em
seus respectivos assemblies. Os testes não inventam projetos para simular uma
separação física que hoje não existe. Quando uma fronteira precisa ser aplicada,
o projeto a declara com `ArchitectureLayer`; nomes de projeto e namespace não
determinam seu papel.

## Regras e justificativas

| Regra | O que reprova | Justificativa |
| --- | --- | --- |
| `project-reference-outside-solution` | `ProjectReference` cujo alvo não pertence à solução | Um projeto fora do `.slnx` não participa necessariamente do build e do gate usados pelo repositório. |
| `production-to-test` | Projeto com `IsTestProject=false` referenciando um projeto com `IsTestProject=true` | Código de produção não deve depender de fixtures, runners ou contratos pertencentes à verificação. |
| `test-package-in-production` | Uso direto de xUnit, NSubstitute, Test SDK ou Testcontainers por produção | Dependências exclusivas de teste aumentam superfície e podem levar utilitários de teste ao artefato publicado. |
| `test-project-target` | Projeto de teste comum sem nenhum `ProjectReference` | Na solução atual, cada suíte deve explicitar o laboratório que verifica. O projeto arquitetural é identificado separadamente porque inspeciona a solução. |
| `unknown-architecture-layer` | Valor desconhecido em `ArchitectureLayer` | Um erro de configuração não pode desativar silenciosamente as demais regras. |
| `layer-dependency-direction` | `Domain -> Application/Infrastructure/EntryPoint`, `Application -> Infrastructure/EntryPoint` ou `Infrastructure -> EntryPoint` | Políticas internas não devem conhecer mecanismos externos ou o ponto de composição. |
| `core-project-reference` | Projeto marcado como `Core` referenciando outro projeto | O núcleo DDD de C11–C12 é um laboratório autocontido; separar assemblies exige uma decisão explícita, não uma referência incidental. |
| `core-infrastructure-package` | Projeto `Core` usando diretamente ASP.NET Core, EF Core, providers de dados, OpenTelemetry ou MCP | O modelo e os casos de uso do laboratório devem continuar executáveis sem servidor, banco, telemetria ou integração externa. |

`C11C12.Ddd.csproj` declara `ArchitectureLayer=Core`. As camadas `Domain`,
`Application`, `Infrastructure` e `EntryPoint` estão disponíveis para projetos
que realmente adotarem essas fronteiras. A classificação é explícita e não
depende de sufixos como `.Domain`, `.Api` ou `.Tests`.

## Evidências positivas e negativas

`ArchitectureTests.cs` contém três tipos de evidência:

- a solução real deve produzir zero violações;
- fixtures positivas usam nomes neutros, aceitam uma dependência comum no
  núcleo, múltiplos projetos consumidores e projetos de teste com nomes livres;
- fixtures negativas introduzem cada direção proibida, referência ausente,
  pacote de teste em produção, pacote de infraestrutura no núcleo e configuração
  inválida, verificando os identificadores e as mensagens resultantes.

As fixtures são modelos em memória. Elas não entram na solução nem criam
projetos vazios, mas percorrem o mesmo mecanismo usado para validar os `.csproj`
reais.

## Relação com os capítulos

O [C13](../../docs/dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md)
explica por que dependências de código devem apontar para políticas internas e
por que o número de projetos não define uma arquitetura limpa. Estes testes
automatizam somente a parte observável dessa decisão no grafo de projetos.

O [C19](../../docs/dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md)
defende manter decisões próximas do código e validá-las no mesmo fluxo. A
propriedade `ArchitectureLayer`, este documento e os testes evoluem juntos para
reduzir divergência entre intenção e estrutura.

## Execução e CI

Execute isoladamente com:

```bash
dotnet test exemplos/testes-arquiteturais/EngenhariaDotnet.Architecture.Tests.csproj
```

O projeto também pertence a `EngenhariaDotnet.slnx`. O workflow existente é
acionado por mudanças em `.cs`, `.csproj`, `.slnx` e Markdown; seu job
`exemplos-dotnet` restaura, compila e testa a solução completa. Assim, as regras
arquiteturais participam do mesmo gate dos outros exemplos sem um job paralelo.

## Limitações

A análise é estática e intencionalmente pequena. Ela:

- lê referências e propriedades declaradas diretamente nos `.csproj`, sem
  avaliar condições, imports ou o grafo MSBuild resultante;
- verifica dependências diretas, não o conteúdo de pacotes transitivos;
- não inspeciona dependências entre tipos ou namespaces dentro do mesmo
  assembly;
- não detecta acoplamento criado em execução por DI, reflection, configuração,
  serialização, banco, HTTP ou mensageria;
- não prova coesão, qualidade das abstrações ou correção da arquitetura;
- depende de revisão humana para decidir quando uma nova fronteira merece
  `ArchitectureLayer` ou quando uma exceção é justificada.

Build, testes de comportamento, auditoria de dependências e revisão continuam
necessários. Um teste verde comprova apenas as regras declaradas acima.
