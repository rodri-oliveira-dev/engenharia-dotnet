# Relatório de governança e continuidade editorial

> **Data da revisão:** 26 de novembro de 2026
>
> **Branch:** `docs/c13-design-patterns-practical-examples`
>
> **Escopo:** macroestrutura, mesoestrutura, microestrutura, governança e
> composição do GitHub, PDF e EPUB

## Resumo executivo

O eixo que já emergia do conteúdo foi confirmado: engenharia de software é
tratada como a disciplina de construir, evoluir e operar sistemas por decisões
conscientes, evidências, responsabilidade técnica e compreensão de trade-offs.
Esse eixo preserva a voz autoral, centrada em problemas e consequências, e não
exige personagens, motivação artificial nem simplificação do conteúdo técnico.

Os oito agrupamentos continuam adequados. A principal ruptura não estava na
taxonomia, mas na comunicação da jornada: o índice central podia sugerir ordem
numérica, os índices de área quase não explicavam propósito e progressão, e o
gerador descartava suas introduções no PDF e EPUB. Dependências avançadas de C25
e C36 também atravessam agrupamentos sem justificar uma reorganização global.

A revisão preservou IDs, agrupamentos e laboratórios. Os índices agora funcionam
como introduções de partes nos três formatos; a distinção entre identificação,
implementação, publicação e leitura tornou-se explícita; referências que ainda
tratavam capítulos publicados como futuros foram atualizadas; e uma skill passou
a governar expansões posteriores.

## Diagnóstico inicial

### Macroestrutura

- As oito partes cobrem uma progressão defensável: semântica da linguagem,
  aplicação, domínio, distribuição, confiança, operação, prática e síntese.
- A ordem real do livro já vinha dos índices das áreas, não da numeração. O
  índice central, entretanto, exibia o catálogo numérico sem explicar essa
  diferença.
- As páginas de abertura das partes existiam no PDF e EPUB apenas como títulos.
  O propósito, os pré-requisitos e a ponte narrativa disponíveis no GitHub não
  chegavam aos artefatos.
- C25 depende de C27 e C36 depende de C17. São aprofundamentos que cruzam áreas,
  não evidência de que Qualidade ou Cache estejam nos agrupamentos errados.

### Mesoestrutura

- C10 já estava corretamente antes de C08 e C09 no índice da área, embora seu ID
  pudesse induzir leitura posterior ao ORM.
- C19 e C20 possuem IDs herdados da implementação editorial, mas ocupam posições
  coerentes: C19 consolida decisões de arquitetura; C20 aplica documentação e
  entrega à infraestrutura.
- Não foram encontradas dependências obrigatórias circulares. Relações de volta,
  como C27 fornecer observabilidade a C25, são aplicações complementares e não
  ciclos de pré-requisito.
- Diversos capítulos ainda anunciavam como futuros C13, C15, C16, C17, C24, C29
  e C32, enfraquecendo a sensação de obra concluída e os links bidirecionais.

### Microestrutura

Os 36 capítulos foram examinados quanto a objetivo, pré-requisitos, introdução,
seções, relações e referências. A voz predominante já parte de tensões reais e
explicita limites; portanto, não seria útil impor uma estrutura narrativa única.
As correções diretas ficaram concentradas nas transições obsoletas e nas
conexões materiais. C13 manteve o aprofundamento de Strategy, Factory, Decorator,
Adapter e Observer e ganhou uma ponte explícita do Observer local para C15/C16.

## Papel dos agrupamentos

| Agrupamento | Entrada e competência desenvolvida | Relação com a jornada | Decisão |
|---|---|---|---|
| Linguagem e runtime | Sem pré-requisito; contratos de tipos, falhas, execução, concorrência e medição | Fornece a semântica usada por todas as partes | Mantido; C05 antes de C04 na leitura |
| Aplicações e dados | C02/C05; composição, HTTP, persistência, ORM e consistência | Transforma contratos locais em fronteiras e estado durável | Mantido; C10 antes de C08/C09; C36 como retorno após C17 |
| Domínio e arquitetura | C01/C02 e fundamentos de aplicação; invariantes, políticas, abstrações e decisões | Define o que os mecanismos precisam preservar | Mantido; C19 encerra e documenta as decisões |
| Sistemas distribuídos | C07, C10 e C12–C13; entrega, duplicidade, compensação e limites | Expõe contratos a rede e falhas parciais | Mantido; sequência C15 → C16 → C17 → C18 |
| Qualidade e segurança | Falhas, APIs e abstrações; evidência por risco e proteção de fronteiras | Constrói confiança sobre o sistema integrado | Mantido; C25 recomendado depois de C27 |
| Operação, entrega e plataforma | Distribuição, testes e segurança; diagnóstico, confiabilidade, entrega e drift | Usa evidência de produção para controlar evolução | Mantido; C20 encerra após C29 |
| IA e prática profissional | C19, C23 e segurança; autoridade, avaliação, revisão e decisão | Aplica os mesmos critérios a sistemas probabilísticos e equipes | Mantido; C30 funciona como síntese |
| Estudos de caso | Capítulos canônicos conforme o caso; investigação e decisão integrada | Testa a capacidade de combinar conceitos sem resposta universal | Mantido como encerramento da obra |

## Alterações implementadas

### Posicionamento e navegação

- O mapa define separadamente ID estável, ordem de implementação, publicação,
  leitura e dependência conceitual.
- O índice central identifica sua lista numérica como catálogo, não percurso.
- Os oito índices passaram a declarar propósito, conhecimentos necessários,
  competências, encadeamento e ponte para a parte seguinte.
- C25 e C36 receberam orientação de leitura transversal sem mudança de ID ou
  agrupamento.

### Continuidade narrativa

Foram atualizadas transições em C05, C09, C10, C11, C12, C13, C20, C26 e C28.
Referências futuras a capítulos já publicados foram substituídas por relações no
presente e links contextuais. Não houve tentativa de adicionar frases de ligação
em todos os capítulos: conexões só foram incluídas quando alteram a compreensão
do leitor.

O cenário de Pedidos continua atravessando domínio, padrões e distribuição onde
reduz carga cognitiva. Os laboratórios permanecem independentes, sem nova
dependência de build ou infraestrutura.

### Publicação

O gerador agora incorpora a introdução de cada índice de área imediatamente
após o título da parte. O mesmo conteúdo alimenta PDF e EPUB, evitando versões
editoriais divergentes. Estilos discretos foram adicionados para legibilidade.

A revisão também corrigiu a composição de caminhos internos do EPUB para usar
separadores POSIX. Isso elimina a falha no Windows que procurava
`EPUB\\nav.xhtml` dentro do ZIP e preserva o comportamento no Linux.

### Governança

A skill `governar-evolucao-editorial` implementa as etapas de relevância,
posicionamento, avaliação de agrupamento, conexões, implementação e validação. O
modelo de decisão pode ser preenchido na issue ou no PR. `AGENTS.md`, o guia de
contribuição, o guia de estilo, o template de capítulo e os formulários tornam os
gatilhos explícitos.

A separação de responsabilidades ficou documentada:

- governança editorial: posição, narrativa e coerência da obra;
- revisão técnica: precisão, didática, fontes e gabaritos;
- validação de exemplos: compilação e comportamento;
- revisão de PR: integridade do diff e regressões.

## Decisões editoriais

| Critério | Decisão |
|---|---|
| Assunto proposto | Continuidade narrativa e governança evolutiva |
| Problema educacional | A ordem pedagógica existia, mas não era comunicada igualmente nos três formatos |
| Justificativa para um capítulo independente | Não aplicável; o problema pertence à arquitetura editorial |
| Agrupamento escolhido | Transversal aos oito agrupamentos |
| Alternativas de posicionamento | Renumerar capítulos, mover C25/C36 ou explicitar percursos |
| Necessidade de novo agrupamento | Não; não existe novo eixo de conhecimento |
| Nível de conhecimento | Do fundamental ao avançado, conforme cada parte |
| Pré-requisitos | Mapa, índices, capítulos e mecanismos de publicação existentes |
| Capítulos relacionados | Todos; ajustes diretos em C05, C09–C13, C20, C26 e C28 |
| Posição de leitura | Índices de área são canônicos; catálogo numérico é apenas referência |
| Responsabilidade canônica | Mapa governa posição; capítulos governam conteúdo; índices governam percurso |
| Conexões narrativas | Introduções de parte, dependências explícitas e links presentes |
| Impactos editoriais | GitHub, sumário, páginas de parte, PDF, EPUB, templates e contribuição |

## Evidências de validação

- Skill validada pelo `quick_validate.py` do criador de skills.
- 18 testes do gerador do livro aprovados com Pandoc portátil 3.14.
- Markdownlint 0.20.0: 97 arquivos, nenhuma ocorrência.
- Links internos: 719 destinos, nenhuma falha.
- Verificação de segredos: nenhuma ocorrência.
- Validador ortográfico: 724 linhas novas em 33 arquivos, nenhuma ocorrência;
  11 testes do seletor aprovados.
- O script de links foi restringido a Markdown versionado ou novo e não ignorado,
  evitando validar documentação de `node_modules`.

## Limitações e oportunidades futuras

### Não concluído neste ambiente

- O SDK 11.0.100 não está instalado; restore, build, testes .NET e validador de
  snippets dependem do CI configurado pelo repositório.
- A geração visual completa de PDF e EPUB requer Mermaid CLI, WeasyPrint,
  librsvg e EPUBCheck. A estrutura e a portabilidade do gerador foram cobertas
  por testes, mas a inspeção visual dos artefatos permanece para o CI Linux.
- O comando de varredura integral `npm run ortografia` usa aspas POSIX no glob e
  não funciona integralmente no Windows. O validador das linhas novas foi
  corrigido para selecionar o shim do CSpell por plataforma e passou localmente;
  a repetição oficial permanece no CI.

### Oportunidades, não correções desta revisão

- Reconciliar menções históricas a laboratórios .NET 10 nos capítulos com a
  migração para .NET 11, respeitando o registro já existente em
  `avaliacao-dotnet-11-csharp-15.md`.
- Observar uso real dos novos percursos antes de considerar qualquer divisão ou
  renomeação de agrupamento.
- Avaliar novas recorrências de cenário somente quando reduzirem carga cognitiva;
  não converter a obra inteira em um único estudo de caso.

## Parecer das cinco revisões

- **Livro técnico:** a voz foi preservada e as partes agora possuem função clara.
- **Storytelling educacional:** a progressão e as exceções de leitura estão
  explícitas sem transições mecânicas.
- **Arquitetura de software:** os agrupamentos continuam alinhados às
  responsabilidades canônicas e aos trade-offs.
- **QA:** links, Markdown, skill e regras do gerador foram testados; validações
  dependentes do SDK 11 permanecem declaradas.
- **Publicação:** as introduções passaram a compor PDF e EPUB e a falha de
  separador interno do EPUB foi corrigida; a revisão visual depende do artefato
  do CI.
