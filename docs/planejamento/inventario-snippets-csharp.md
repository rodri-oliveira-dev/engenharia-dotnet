# Inventário de snippets C# em Markdown

Este inventário registra a preparação para validar automaticamente os blocos C#
publicados em Markdown. Ele descreve o estado em 20 de novembro de 2026 sem
reproduzir o conteúdo dos capítulos.

## Classificação e metadados

Todo fence C# deve ser precedido imediatamente por um comentário no formato:

```markdown
<!-- snippet-csharp: id=c01-01; categoria=isolado -->
```

O ID é estável, único no repositório e não depende do número da linha. As
categorias têm estes significados:

- `isolado`: o conteúdo do fence forma uma unidade de compilação sem tipos,
  variáveis ou membros definidos pelo capítulo;
- `contexto`: o código pode ser compilado quando imports, tipos, membros,
  dependências ou um invólucro auxiliar forem fornecidos explicitamente;
- `fragmento`: o trecho omite partes deliberadamente e serve para explicar uma
  decisão; tentar completá-lo automaticamente poderia mudar seu significado;
- `invalido`: o erro de compilação é o comportamento demonstrado e a validação
  futura deve conferir diagnósticos esperados.

O validador `scripts/validar-snippets-csharp.py` reprova fences C# sem metadado,
metadados órfãos ou malformados e IDs duplicados antes de criar qualquer projeto
temporário.

## Inventário atual

| Arquivo | Total | Isolado | Contexto | Fragmento | Inválido |
|---|---:|---:|---:|---:|---:|
| `docs/aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md` | 6 | 0 | 6 | 0 | 0 |
| `docs/aplicacoes-e-dados/c08-consultas-ef-core.md` | 8 | 0 | 8 | 0 | 0 |
| `docs/aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md` | 4 | 0 | 4 | 0 | 0 |
| `docs/autoavaliacao/c01-tipos-identidade-modelagem.md` | 6 | 5 | 1 | 0 | 0 |
| `docs/autoavaliacao/c02-nulabilidade-falhas-excecoes.md` | 6 | 1 | 3 | 0 | 2 |
| `docs/dominio-e-arquitetura/c11-entidades-value-objects-agregados.md` | 5 | 0 | 4 | 1 | 0 |
| `docs/dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md` | 3 | 0 | 2 | 1 | 0 |
| `docs/dominio-e-arquitetura/c14-cqrs-modelos-orientados-uso.md` | 1 | 0 | 1 | 0 | 0 |
| `docs/ia-e-pratica-profissional/c21-fundamentos-llm-mcp-rag.md` | 1 | 0 | 1 | 0 | 0 |
| `docs/linguagem-e-runtime/c01-tipos-identidade-modelagem.md` | 12 | 5 | 7 | 0 | 0 |
| `docs/linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md` | 13 | 2 | 11 | 0 | 0 |
| `docs/linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md` | 9 | 0 | 9 | 0 | 0 |
| `docs/linguagem-e-runtime/c04-memoria-runtime-performance.md` | 6 | 0 | 6 | 0 | 0 |
| `docs/linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md` | 10 | 0 | 10 | 0 | 0 |
| `docs/operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md` | 1 | 0 | 1 | 0 | 0 |
| `docs/qualidade-e-seguranca/c23-testes-comportamento-risco.md` | 4 | 0 | 2 | 2 | 0 |
| `docs/qualidade-e-seguranca/c24-integracao-real-testes-legado.md` | 4 | 0 | 4 | 0 | 0 |
| `docs/qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md` | 5 | 1 | 4 | 0 | 0 |
| `docs/sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md` | 2 | 0 | 2 | 0 | 0 |
| `docs/sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md` | 1 | 0 | 1 | 0 | 0 |
| **Total** | **107** | **14** | **87** | **4** | **2** |

## Critérios de compilação e execução

A compilação deve usar o SDK `11.0.100`, `net11.0`, C# 15, nullable habilitado
e avisos tratados como erros. O ambiente temporário deve ser criado fora da
árvore versionada e removido ao final; nenhum projeto permanente será criado
apenas para validar um fence.

Para `isolado`, o validador deve copiar o conteúdo sem reescrevê-lo para uma
unidade de compilação mínima. Imports implícitos precisam ser fixos e
documentados. Para `contexto`, cada snippet precisará de um invólucro declarado,
com imports, tipos auxiliares, pacotes e posição de inserção explícitos. O
invólucro não pode alterar a semântica do trecho. `Fragmento` fica fora da
compilação; `invalido` deve falhar somente com os diagnósticos cadastrados.

Compilar não autoriza executar. A execução é opt-in e limitada a programas
determinísticos, sem rede, credenciais, containers, produção, operações
destrutivas ou serviços reais. Exemplos cobertos por laboratórios existentes
continuam validados pela solução, sem duplicação em projetos gerados.

As expectativas ficam nos metadados de validação do manifesto. O formato exige
classificação explícita e exatamente uma expectativa:

```json
{
  "execution": {
    "classification": "deterministic-safe",
    "expected_stdout": "resultado\n",
    "timeout_seconds": 5
  }
}
```

Para uma falha deliberada em runtime, `expected_stdout` é substituído por, por
exemplo, `"expected_exception": "System.InvalidOperationException"`. Saída
padrão é comparada exatamente após normalizar quebras de linha. A exceção exige
código de saída diferente de zero e o nome qualificado do tipo no diagnóstico.
O timeout padrão é cinco segundos e o manifesto aceita no máximo 30 segundos.
Um invólucro auxiliar pode capturar a exceção somente na fronteira do programa
gerado, registrar seu tipo e devolver falha, sem reescrever o fence nem delegar
o tratamento ao mecanismo de crash do sistema operacional.

Cada expectativa roda duas vezes. Código de saída, stdout e stderr precisam ser
idênticos nas duas observações antes da comparação com o valor esperado. Um
resultado naturalmente variável não deve receber `execution`; ele permanece
somente compilado ou é coberto por um laboratório apropriado.

## Seleção automatizada

O manifesto `scripts/snippets-csharp.json` escolhe os snippets e declara somente
o contexto externo necessário. Seus campos permitem definir imports, prefixo e
sufixo do invólucro, fontes auxiliares e `OutputType`. O código dentro do fence
é copiado sem reescrita e recebe uma diretiva `#line` com o arquivo e a linha do
Markdown original.

Cada snippet selecionado recebe um projeto isolado com `net11.0`, C# 15,
nullable habilitado e warnings como erros. O validador copia `global.json` para
a raiz temporária, executa `dotnet build` a partir dela e remove projetos,
resultados de build e cache NuGet ao terminar, inclusive quando há falha.

As execuções opt-in usam diretamente o assembly compilado, stdin fechado,
processo separado, diretório de trabalho e perfil temporários por tentativa,
ambiente reduzido sem variáveis arbitrárias e diagnósticos do runtime
desabilitados. O processo recebe o timeout declarado. Esse isolamento não é um
sandbox de rede do sistema operacional; por isso, somente código revisado como
local, seguro e sem efeitos destrutivos pode ser classificado para execução.

A seleção inicial valida estes 11 IDs:

- isolados: `c01-01`, `c01-05`, `c01-06`, `c01-08`, `c01-09`, `c02-05`,
  `c02-10` e `c26-04`;
- com contexto declarado: `c02-01`, `c04-01` e `c11-03`.

Destes, `c01-01` valida a saída padrão, `c02-05` valida uma
`System.NullReferenceException` esperada e `c04-01` valida execução bem-sucedida
sem saída. Os outros oito são apenas compilados.

As autoavaliações acrescentam dez IDs ao manifesto:

- isolados: `aval-c01-q01`, `aval-c01-q02`, `aval-c01-q05`, `aval-c01-q06`,
  `aval-c01-q10` e `aval-c02-q02`;
- com contexto declarado: `aval-c01-q04`, `aval-c02-q01`, `aval-c02-q03` e
  `aval-c02-q09`.

Oito deles conferem o comportamento que o gabarito afirma: seis pela saída
padrão e dois pela exceção esperada (`System.InvalidCastException` em
`aval-c01-q10` e `System.NullReferenceException` em `aval-c02-q02`).
`aval-c02-q01` e `aval-c02-q09` são apenas compilados.

Ainda não são compilados:

- C01: `c01-02`, `c01-03`, `c01-04`, `c01-07`, `c01-10`, `c01-11`, `c01-12`;
- C02: `c02-02`, `c02-03`, `c02-04`, `c02-06`, `c02-07`, `c02-08`, `c02-09`,
  `c02-11`, `c02-12`, `c02-13`;
- C03: `c03-01` a `c03-09`;
- C04: `c04-02` a `c04-06`;
- C05: `c05-01` a `c05-10`;
- C06: `c06-01` a `c06-06`;
- C08: `c08-01` a `c08-08`;
- C09: `c09-01` a `c09-04`;
- C11: `c11-01`, `c11-02` e `c11-05`;
- C12: `c12-01` e `c12-03`;
- C14: `c14-01`;
- C16: `c16-01` e `c16-02`;
- C18: `c18-01`;
- C21: `c21-01`;
- C23: `c23-01` e `c23-02`;
- C24: `c24-01` a `c24-04`;
- C26: `c26-01`, `c26-02`, `c26-03` e `c26-05`;
- C27: `c27-01`.

Os fragmentos `c11-04`, `c12-02`, `c23-03` e `c23-04` são excluídos. Os
snippets `invalido` `aval-c02-q04` (CS8602) e `aval-c02-q06` (CS8603) também
ficam fora da compilação até o validador conferir diagnósticos esperados. O
comando abaixo
recalcula e imprime as três listas a partir do Markdown e do manifesto:

```sh
python scripts/validar-snippets-csharp.py --list-only
```

Para executar os testes do extrator, gerador e relatório de falhas:

```sh
python -m unittest discover -s scripts/tests -p "test_validar_snippets_csharp.py" -v
```

## Limitações desta etapa

- A classificação foi feita por revisão estática e pelo contexto dos capítulos.
- Com o SDK 11.0.100, os 11 snippets selecionados compilaram sem falhas; três
  tiveram o comportamento executado duas vezes e conferido. Isso cobre somente
  os IDs listados, não os 80 snippets ainda pendentes.
- O manifesto inicial cobre três dos 83 snippets de contexto; os demais exigem
  invólucros ou dependências proporcionais ao exemplo antes de serem incluídos.
- A suíte .NET local aprovou 137 testes sem Docker. A execução integral encontrou
  uma única falha no teste `Category=Docker`, porque não havia daemon disponível;
  portanto, esse teste não foi validado localmente.
- Os dez snippets das autoavaliações cadastrados no manifesto foram compilados
  localmente com o SDK 9, `TreatWarningsAsErrors` e nullable habilitado,
  reproduzindo o invólucro do manifesto, e os oito com `execution` também foram
  executados. Os dois `invalido` foram compilados à parte só para confirmar que
  falham com CS8602 e CS8603. A confirmação com o SDK 11.0.100 depende do CI.
- Metadados em comentários HTML preservam a renderização do GitHub, mas outras
  ferramentas Markdown podem optar por exibi-los ao editar a fonte.
