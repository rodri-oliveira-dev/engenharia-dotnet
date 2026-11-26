# Guia de estilo editorial

Este guia transforma as diretrizes do projeto em regras práticas para autoria e
revisão. Em caso de dúvida de escopo, prevalecem o
[mapa editorial](planejamento/mapa-editorial.md) e as
[diretrizes originais](planejamento/diretrizes-editoriais.md).

## Idioma e tom

- Escreva o conteúdo autoral em português do Brasil.
- Prefira frases diretas, voz ativa e parágrafos com uma ideia central.
- Preserve termos técnicos consolidados em inglês quando a tradução reduzir a
  precisão; explique-os na primeira ocorrência.
- Expanda siglas na primeira ocorrência e use um termo consistente por conceito.
- Evite atribuir maturidade ou senioridade a alguém pelo uso de uma técnica.
- Evite absolutos como “sempre”, “nunca”, “melhor” e “mais rápido” sem condições
  e evidência.

## Estrutura e títulos

- Use um único título de nível 1 (`#`) por arquivo.
- Organize seções sem saltar níveis de títulos.
- Escolha títulos descritivos, em estilo de frase, sem numeração manual.
- Use o [template de capítulo](templates/capitulo-tecnico.md) como ponto de
  partida e adapte seções somente quando o assunto exigir.
- Comece pelo problema e pelo contexto antes de apresentar soluções.
- Declare nível, objetivo, pré-requisitos, escopo e última revisão.

## Continuidade e progressão

O eixo da obra é a engenharia de software como disciplina de construir, evoluir
e operar sistemas por meio de decisões conscientes, evidências, responsabilidade
técnica e compreensão de trade-offs. Preserve esse eixo sem transformar o texto
em narrativa ficcional ou motivacional.

- Retome apenas o conhecimento anterior necessário ao problema atual.
- Mostre qual limite da solução conhecida exige um novo conceito.
- Prepare aplicações posteriores sem prometer conteúdo ou forçar links.
- Varie as transições; evite fórmulas repetidas sobre capítulo anterior e próximo.
- Reutilize cenários quando isso reduzir carga cognitiva, mantendo laboratórios
  independentes e contratos de negócio consistentes.
- Preserve contexto suficiente para leitura não linear.

Identificador, ordem de implementação, publicação e leitura são dimensões
distintas. O ID `CXX` permanece estável; a posição recomendada é definida nos
índices e no mapa editorial a partir das dependências conceituais.

## Nomes e organização de arquivos

- Use nomes em português, sem acentos e em `kebab-case`.
- Mantenha capítulos na área definida pelo mapa editorial.
- Nomeie capítulos como `cXX-titulo-descritivo.md`, preservando o identificador
  estável do planejamento.
- Não crie arquivos vazios ou placeholders. Enquanto um capítulo não existe, seu
  título permanece como texto no roadmap, sem link.
- `README.md`, `CONTRIBUTING.md` e `LICENSE` são exceções por serem convenções da
  plataforma.

## Formatação em Markdown

- Use Markdown compatível com a renderização nativa do GitHub.
- Deixe uma linha em branco ao redor de títulos, listas, tabelas e blocos de
  código.
- Informe a linguagem em blocos de código, como `csharp`, `bash`, `json` ou
  `text`.
- Use crases para nomes de tipos, membros, comandos, arquivos e valores literais.
- Prefira listas para itens equivalentes; use tabelas apenas quando a comparação
  por colunas trouxer ganho real.
- Use links relativos para arquivos do repositório e textos de link descritivos.
- Não use HTML quando Markdown nativo resolver o problema.
- Não adicione elementos visuais decorativos, badges sem resultado verificável
  ou imagens sem texto alternativo.

## Acessibilidade do conteúdo

- Não use cor ou símbolos como única forma de comunicar informação.
- Forneça descrição textual para diagramas e imagens informativas, incluindo
  as relações essenciais quando o texto alternativo curto não for suficiente.
- Prefira links descritivos e tabelas simples; apresente em texto as conclusões
  relevantes de comparações visuais.
- Consulte a [declaração de acessibilidade](../ACCESSIBILITY.md) para conhecer
  os objetivos e as limitações do projeto.

## Código e exemplos

- Exemplos devem demonstrar um comportamento, risco ou decisão observável.
- Snippets precisam ser autocontidos no contexto do texto; não use reticências
  que escondam a parte difícil.
- Código executável deve seguir a
  [estratégia de exemplos compiláveis](exemplos-compilaveis.md).
- Mostre cancelamento, falhas e descarte de recursos quando forem relevantes.
- Não inclua credenciais, endpoints privados ou dependências sem função didática.
- Declare simplificações que seriam inadequadas em produção.
- Não faça alegações de performance com medições improvisadas; registre baseline,
  ambiente e metodologia.

## Trade-offs e recomendações

Toda recomendação relevante deve informar contexto, restrições, benefício, custo,
modos de falha e alternativa mais simples. Quando pertinente, inclua impacto em
desempenho e operação e um sinal que justifique reavaliar a decisão.

Padrões e ferramentas não são objetivos. Cite-os quando nomearem uma solução com
precisão e ajudarem a discutir consequências.

## Referências

Priorize, nesta ordem:

1. documentação oficial;
2. especificações e padrões de organizações mantenedoras;
3. materiais técnicos primários de autores ou equipes responsáveis;
4. livros e artigos reconhecidos, com contexto e data.

Abra e confira cada fonte antes da publicação. Informe versão quando o
comportamento depender dela e data de acesso para conteúdo sujeito a mudança. A
planilha de origem orienta a pauta, mas não substitui uma fonte técnica.

## Links e conteúdo canônico

Um conceito deve possuir um capítulo canônico. Outros capítulos resumem apenas o
necessário e apontam para ele. Adicione links bidirecionais quando a relação for
material e nunca copie uma seção inteira para evitar navegação.

Antes de publicar, confira todos os links locais e não crie links para capítulos
apenas planejados.

## Revisão final

Confirme que o texto:

- respeita o escopo e os pré-requisitos do mapa;
- contém fontes verificáveis para afirmações técnicas relevantes;
- explicita alternativas, limitações e quando evitar;
- não duplica conteúdo canônico sem justificativa;
- possui exemplos compiláveis e testes aprovados quando aplicável;
- considera riscos de segurança e operação;
- está integralmente revisado em português e, quando houver alterações,
  passa pelo [verificador ortográfico pt-BR](ortografia.md);
- passa pela validação automatizada de Markdown e links internos.

Mudanças estruturais também devem passar pela skill
`governar-evolucao-editorial` e ser conferidas no GitHub, PDF e EPUB.
