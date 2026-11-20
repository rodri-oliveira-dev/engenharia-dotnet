# Compatibilidade: SDK, MSBuild, NuGet e containers no .NET 11

> **Nível:** Avançado
>
> **Objetivo:** evitar falhas de restore, build, teste e publicação
> durante migrações de pipelines .NET.
>
> **Última revisão:** 2026-11-20

Relaciona-se a [C29 — Entrega compatível](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md).
O SDK, o compilador e o runtime não são a mesma coisa:
migrações devem registrar explicitamente as versões utilizadas.

## Matriz de mudanças

| Mudança | Tipo | Impacto principal | Verificação |
| --- | --- | --- | --- |
| Restore ignora cache global ao buscar a próxima versão disponível | fonte/restore | seleção muda ou erro NU1102 | restore limpo e inspeção do lock/assets |
| VSTest requer base gravável de artefatos em containers | comportamento | warning/attachments não processados; Nano Server pode falhar | rodar testes no container com usuário real |
| NativeAOT produz bibliotecas Unix com prefixo `lib` | comportamento | scripts de deploy podem procurar nome antigo | publicar e verificar nome do arquivo |
| `dnx` scripts não usam seleção de SDK via `global.json` | comportamento | ferramenta pode usar SDK não esperado | conferir SDK resolvido na execução |
| NuGet `pack` adverte sobre identificadores restritos | comportamento | novos warnings podem bloquear builds que tratam warning como erro | executar `dotnet pack` |
| Verificação de assinatura de modelos NuGet baixados de feeds | comportamento | instalação pode falhar segundo SO e política NuGet; não se aplica a `.nupkg` ou pasta local | revisar assinatura, `signatureValidationMode` e `allRepositorySigned` nos feeds utilizados |
| Runtime local de containers prefere ferramentas nativas da plataforma | comportamento | ferramenta de container diferente da prevista | verificar seleção no ambiente de CI |

Essas mudanças não justificam alterar todo o pipeline quando
o recurso afetado não é usado.

## Exemplo 1: restore reproduzível

**Até o SDK 10:** ao pedir uma versão indisponível, o restore
podia escolher uma versão superior encontrada somente no cache
global de pacotes.

**SDK 11:** o fallback para a próxima versão superior considera
**fontes de pacotes**, não o cache global. Uma versão exatamente
solicitada ainda pode ser aproveitada do cache.

```bash
dotnet --info
PACOTES_LIMPOS="$(mktemp -d)"
dotnet restore exemplos/EngenhariaDotnet.slnx --force \
  --packages "$PACOTES_LIMPOS" \
  --configfile NuGet.Config
dotnet build exemplos/EngenhariaDotnet.slnx -c Release --no-restore
rm -rf "$PACOTES_LIMPOS"
```

O exemplo usa Bash, cria uma pasta de pacotes inicialmente vazia e
passa o caminho explicitamente por `--packages`. O arquivo
`NuGet.Config` determina as fontes autorizadas; alternativamente,
use `--source` para definir um feed específico. A opção `--force`
recalcula os assets, mas **não limpa o cache global por si só**.
Mantenha a pasta isolada até o fim do `build --no-restore`.

Ação: fixar versões que existam nas fontes autorizadas e
centralizar dependências. Para demonstrar a diferença de resolução
entre .NET 10 e 11, use feed NuGet **local** com versões controladas,
cache isolado por execução e inspeção do `project.assets.json`;
não dependa da disponibilidade momentânea da Internet.

### Assinaturas de modelos NuGet

O `dotnet new install` verifica a assinatura dos modelos **baixados
de fontes NuGet** quando a verificação está habilitada. Ela é sempre
habilitada no Windows; no Linux, é habilitada por padrão e pode ser
desativada por `DOTNET_NUGET_SIGNATURE_VERIFICATION=false`; no
macOS, é **desabilitada por padrão** e pode ser ativada com
`DOTNET_NUGET_SIGNATURE_VERIFICATION=true`.

A validação **não se aplica à instalação direta** de um arquivo
`.nupkg` local nem de um diretório. Para modelos baixados,
a configuração `signatureValidationMode=require` ou a exigência
do feed `allRepositorySigned=true` rejeita pacotes sem assinatura;
com `accept` e sem essa exigência, pacotes não assinados podem ser
aceitos. O sinalizador `--force` não ignora a verificação de uma
assinatura. Uma assinatura válida não garante a segurança do
código gerado pelo modelo. Se houver falha, use
`dotnet new install --diagnostics` e inspecione a configuração
NuGet aplicável em vez de desabilitar a segurança indiscriminadamente.

## Exemplo 2: VSTest dentro de containers

O comportamento começou no SDK 11.0.100 **e também em
atualizações de manutenção das linhas 8, 9 e 10**.
Com VSTest, o runner precisa criar uma pasta privada para anexos.
Em Linux sem a pasta padrão, pode haver warning e perda do
pós-processamento; em Nano Server pode ocorrer falha.

**Antes:** Dockerfile/teste sem base de artefatos explicitada
dependia do ambiente.

**Depois:** em imagens Linux personalizadas, configure
um diretório gravável:

```dockerfile
ENV VSTEST_ARTIFACTS_BASE_DIRECTORY=/tmp
```

```bash
dotnet test exemplos/EngenhariaDotnet.slnx -c Release
```

Configure **`VSTEST_ARTIFACTS_BASE_DIRECTORY`**, não
`VSTEST_ARTIFACTS_DIRECTORY`. As imagens oficiais
`mcr.microsoft.com/dotnet/sdk` já definem a variável.
Não há necessidade de adicionar a configuração a todo Dockerfile
de produção que não executa testes.

## Exemplo 3: Native AOT como biblioteca Unix

**Até .NET 10:** uma biblioteca nativa publicada como
`mylib.so` podia ser esperada pelo consumidor.

**No .NET 11:** a saída de biblioteca NativeAOT Unix usa
`libmylib.so` (e `libmylib.a`) por padrão.
Não se aplica ao nome de executáveis gerados normalmente.

```xml
<PropertyGroup>
  <!-- Use somente para compatibilidade com consumidor legado. -->
  <UseNativeLibPrefix>false</UseNativeLibPrefix>
</PropertyGroup>
```

Prefira atualizar consumidores e scripts para a convenção `lib`;
a opção de compatibilidade deve ter justificativa e prazo.
Verifique nomes de arquivos e a capacidade de carregamento no
sistema de destino. O repositório não compila bibliotecas NativeAOT
no CI atual.

## Checklist do pipeline

1. Verificar `global.json`, `TargetFramework`, `Directory.Packages.props`,
   `NuGet.Config` e versões efetivas dos SDKs nos runners.
2. Validar restore com cache limpo e feed correto, sem mudar pacotes
   silenciosamente para versões não aprovadas.
3. Rodar testes com o mesmo usuário/permissões do container final,
   preservando TRX, cobertura e artifacts.
4. Conferir runtime identifiers, publish, saídas NativeAOT,
   dependências nativas e scripts que referenciam arquivos.
5. Reavaliar scans de vulnerabilidades, assinatura de templates e
   warnings de `pack`.
6. Comparar ambiente da PR com ambiente de publicação; não
   considerar apenas `dotnet build` suficiente.

**Evidência neste repositório:** o workflow
[validar-documentacao.yml](../../.github/workflows/validar-documentacao.yml)
já faz restore, build, testes e auditoria. Os casos de cache com feed
local, Nano Server e NativeAOT são **roteiros de validação**,
não testes automatizados adicionados neste PR.

## Referências

- [Breaking changes .NET 11](https://learn.microsoft.com/en-us/dotnet/core/compatibility/11)
- [Restore e pasta global de pacotes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/11/restore-global-packages-folder-version-search)
- [VSTest em containers](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/11/vstest-artifacts-base-directory)
- [Prefixo NativeAOT em Unix](https://learn.microsoft.com/en-us/dotnet/core/compatibility/interop/11/nativeaot-lib-prefix)
- [Verificação de assinaturas na instalação de modelos NuGet](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/11/template-package-signature-verification)

[Voltar à trilha](indice.md).
