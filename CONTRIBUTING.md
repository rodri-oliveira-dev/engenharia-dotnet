# Como contribuir

Obrigado por contribuir com o Engenharia .NET. O projeto valoriza conteúdo
técnico verificável, decisões contextualizadas e uma experiência de leitura
consistente no GitHub.

## Antes de começar

1. Consulte o [índice](docs/indice.md), o
   [mapa editorial](docs/planejamento/mapa-editorial.md) e o
   [inventário](docs/planejamento/inventario.md).
2. Leia o [guia de estilo](docs/guia-de-estilo.md).
3. Verifique se já existe um capítulo canônico ou uma proposta para o assunto.
4. Para mudanças grandes de escopo, abra uma issue antes de escrever para alinhar
   lacuna, área, dependências e sobreposições.

## Ortografia em português

Os PRs com documentação passam pelo [guia de validação pt-BR](docs/ortografia.md).
Antes de enviar o PR, execute `npm install` e
`python scripts/validar-portugues-pr.py --base main --head HEAD`.
Para uma revisão geral, execute `npm run ortografia`. Termos técnicos
necessários devem entrar em `.cspell/termos-tecnicos.txt` após revisão;
não adicione grafias incorretas apenas para silenciar o job.

## Licenciamento das contribuições

Ao contribuir, disponibilize seu conteúdo original sob a licença aplicável:
[MIT para código](LICENSE-CODE) e
[CC BY-NC 4.0 para conteúdo editorial](LICENSE).
Confirme que possui os direitos necessários e identifique materiais de terceiros
com suas fontes, licenças e créditos. Não inclua conteúdo incompatível com
as condições de distribuição do projeto.

## Novos capítulos

- Escolha um capítulo já aprovado no mapa editorial ou justifique a nova lacuna.
- Copie o [template de capítulo](docs/templates/capitulo-tecnico.md) e nomeie o
  arquivo como `cXX-titulo-descritivo.md` na área correspondente.
- Preserve o ID, nível, escopo, dependências e ordem definidos no mapa.
- Linke apenas pré-requisitos publicados. Mantenha capítulos futuros como texto.
- Atualize o índice da área, o índice central quando necessário e as relações
  bidirecionais relevantes.
- Não transforme perguntas da fonte em formato de entrevista ou FAQ disfarçada.

## Revisões técnicas

Separe comentários de revisão por natureza: correção factual, clareza, escopo ou
preferência. Para divergências arquiteturais, explicite contexto, evidências,
alternativas e consequências.

Confirme especialmente:

- coerência com versões declaradas de C# e .NET;
- distinção entre comportamento especificado e detalhe de implementação;
- validade de exemplos, comandos e modos de falha;
- implicações de segurança, desempenho e operação;
- presença de critérios para usar, evitar, testar, medir ou observar a solução.

## Exemplos em C\#

- Use snippets quando forem suficientes para demonstrar o comportamento.
- Adicione projetos apenas conforme a
  [estratégia de exemplos compiláveis](docs/exemplos-compilaveis.md).
- Classifique cada fence C# conforme o
  [inventário de snippets](docs/planejamento/inventario-snippets-csharp.md).
- Habilite execução apenas para snippets locais, determinísticos e seguros, com
  saída ou exceção esperada declarada no manifesto.
- Mantenha exemplos pequenos, completos e sem credenciais.
- Habilite nulabilidade e trate cancelamento, falha e descarte de recursos quando
  pertinentes.
- Inclua testes determinísticos para comportamentos verificáveis.
- Declare qualquer simplificação inadequada para produção.
- Não use benchmark improvisado nem provider em memória para provar semântica
  relacional.

## Referências externas

- Priorize documentação oficial, especificações e fontes primárias.
- Abra e confira cada URL antes de enviar a contribuição.
- Use texto de link descritivo e informe versão e data de acesso quando a fonte
  puder mudar.
- Resuma com suas palavras e respeite licenças e direitos autorais.
- Não use a planilha de pauta como autoridade sobre comportamento técnico.

## Validar dependências e executar o CI localmente

Os laboratórios usam o SDK definido em [`global.json`](global.json)
(.NET 11) e o target `net11.0`. Antes de contribuir, instale o SDK
correspondente e execute na raiz do repositório:

```sh
dotnet restore exemplos/EngenhariaDotnet.slnx
dotnet build exemplos/EngenhariaDotnet.slnx --configuration Release --no-restore
dotnet test exemplos/EngenhariaDotnet.slnx --configuration Release --no-build
```

O job **Analisar dependências**, no workflow
[`validar-documentacao.yml`](.github/workflows/validar-documentacao.yml),
executa auditoria NuGet para dependências diretas e transitivas. O comando
de listagem pode retornar sucesso mesmo quando há achados; por isso,
o relatório JSON é analisado por um verificador que decide o status do CI.

Para reproduzir localmente a verificação de segurança:

```sh
dotnet restore exemplos/EngenhariaDotnet.slnx -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=low
dotnet package list --project exemplos/EngenhariaDotnet.slnx --vulnerable --include-transitive --no-restore --format json --output-version 1 > nuget-audit.json
python scripts/verificar-vulnerabilidades-nuget.py nuget-audit.json
python -m unittest discover -s scripts/tests -p "test_verificar_vulnerabilidades_nuget.py" -v
```

Use Python 3 para o verificador e remova o `nuget-audit.json` gerado
após a conferência; ele não deve fazer parte do commit.

A interpretação é explícita: **0** significa relatório válido sem achados,
**1** indica uma ou mais vulnerabilidades de qualquer severidade e **2**
indica relatório inválido, ausente ou inconclusivo. Os oito testes simulam
esses resultados sem adicionar dependências vulneráveis ao repositório.
Verifique também a disponibilidade das fontes de auditoria: resultado
limpo não demonstra cobertura completa quando uma fonte está indisponível.

Este é o bloqueio conservador definido **para este repositório educacional**.
Em um sistema corporativo, o tratamento de exceções de segurança deve seguir
a governança, o risco e as aprovações da organização, sem simplesmente
ignorar os achados. Veja o [estudo de caso C35](docs/estudos-de-caso/c35-pipeline-dependencia-vulneravel.md)
e a [avaliação do .NET 11](docs/planejamento/avaliacao-dotnet-11-csharp-15.md).

## Agentes e skills de revisão

O [AGENTS.md](AGENTS.md) fornece as regras persistentes para agentes,
incluindo a seção `Code Review Rules` usada pelo Codex no GitHub.

Três skills locais aprofundam procedimentos específicos:

- [Revisão de PRs](.agents/skills/revisar-pr-dotnet/SKILL.md):
  falhas, contratos, segurança e testes.
- [Validação de C#](.agents/skills/validar-exemplos-csharp/SKILL.md):
  compilação, comportamento e resultados esperados.
- [Revisão editorial técnica](.agents/skills/revisar-conteudo-tecnico/SKILL.md):
  conceitos, fontes, questões e gabaritos.

Agentes compatíveis podem carregar essas skills conforme a tarefa.
Não pressuponha que todas sejam carregadas automaticamente durante
um review no GitHub. Instruções de IA não substituem CI nem revisão humana.

## Modelos de pull request

O GitHub carrega automaticamente o
[modelo padrão](.github/PULL_REQUEST_TEMPLATE.md) ao criar um PR.
Para mudanças com necessidades específicas, estão disponíveis os modelos
de [conteúdo técnico](.github/PULL_REQUEST_TEMPLATE/conteudo-tecnico.md),
[exemplos .NET](.github/PULL_REQUEST_TEMPLATE/exemplos-dotnet.md) e
[automações e CI](.github/PULL_REQUEST_TEMPLATE/automacoes-ci.md).

Para escolher um modelo específico, adicione o parâmetro
`?template=conteudo-tecnico.md`, `?template=exemplos-dotnet.md` ou
`?template=automacoes-ci.md` à URL de criação do pull request.
Se a URL já contiver `?`, use `&template=...`.
O GitHub não apresenta uma seleção automática desses modelos por tipo de alteração.

Em qualquer modelo, descreva a motivação, o escopo, os riscos e as
validações realmente executadas. Deixe sem marcar os itens não realizados
e explique o motivo. Os modelos não substituem o CI nem a revisão humana.

## Qualidade e consistência

Antes de abrir o pull request:

1. releia o texto integralmente em português;
2. confirme que todos os links internos apontam para arquivos existentes;
3. execute `npx markdownlint-cli2@0.20.0` na raiz;
4. execute `python scripts/validar-snippets-csharp.py` ao alterar fences C#;
5. compile e teste os exemplos alterados, quando existirem;
6. revise o diff para evitar arquivos gerados, mudanças fora do escopo ou
   alterações acidentais na licença.

O pull request deve resumir a motivação, as decisões editoriais, os arquivos
afetados e as validações realizadas. Prefira contribuições pequenas e coerentes,
com documentação e código de exemplo evoluindo juntos.
