---
name: governar-evolucao-editorial
description: >-
  Governar a arquitetura editorial, o storytelling, a progressão de
  conhecimento e a integração de novos capítulos e agrupamentos ao livro
  Engenharia .NET, preservando sua identidade autoral, coerência narrativa e
  rigor técnico. Use em propostas, criações ou ampliações substanciais de
  capítulos, novas áreas, mudanças de ordem, mapa editorial ou composição do
  PDF e EPUB.
---

# Governar a evolução editorial

Consulte `AGENTS.md`, `docs/indice.md`, `docs/guia-de-estilo.md`,
`docs/planejamento/mapa-editorial.md` e os índices e capítulos afetados. Esta
skill decide **por que, onde e como** o conteúdo pertence à obra. Use
`revisar-conteudo-tecnico` para precisão e didática, `validar-exemplos-csharp`
para comportamento executável e `revisar-pr-dotnet` para o risco do diff.

## A. Avaliar a relevância

Identifique o problema educacional, a lacuna, a competência desenvolvida e a
contribuição para a proposta do livro. Compare três alternativas: capítulo
independente, ampliação de capítulo canônico e material complementar. Não aceite
popularidade, tendência ou lançamento de ferramenta como justificativa isolada.

## B. Definir o posicionamento

Determine agrupamento, nível, pré-requisitos obrigatórios, leituras
complementares, conhecimentos introduzidos, capítulos relacionados, posição de
leitura, limites e responsabilidade canônica. Preserve o identificador estável;
não coloque conteúdo novo no fim apenas por ser novo.

## C. Avaliar um novo agrupamento

Crie uma área somente quando houver eixo de conhecimento e objetivos próprios,
baixa compatibilidade com as áreas atuais, volume ou profundidade suficientes e
potencial coerente de expansão. Avalie os impactos em capítulos, índices,
navegação, PDF e EPUB. Um capítulo ou uma tecnologia isolada normalmente não
justificam uma área.

## D. Planejar as conexões

Antes de redigir, registre quais conhecimentos serão retomados, qual limite dos
conceitos anteriores motiva o novo assunto, quais cenários existentes podem ser
reutilizados e quais capítulos posteriores se beneficiam. Indique ajustes nos
capítulos relacionados. Prefira conexões bidirecionais quando melhorarem a
compreensão; não adicione links sem função conceitual nem torne laboratórios
artificialmente dependentes.

## E. Implementar

Somente depois da decisão editorial, crie ou atualize o capítulo, mapa, índices,
relações, pré-requisitos, laboratórios, autoavaliações e mecanismos de publicação
pertinentes. Mantenha leitura independente com contexto local suficiente e
evite duplicar o tratamento canônico de outro capítulo.

## F. Validar a integração

Verifique GitHub, PDF e EPUB: ordem, sumário, introduções das partes, referências,
hierarquia, código, diagramas, acessibilidade e elementos editoriais. Confirme
que o conteúdo parece pertencer à obra, contribui para o conjunto e continua
útil como consulta isolada. Registre evidências e limitações; qualidade local do
capítulo não encerra a integração editorial.

## Registro da decisão

Use o [modelo de decisão editorial](../../../docs/templates/registro-decisao-editorial.md)
na issue ou no pull request. Um arquivo separado só é necessário quando a
decisão precisar de histórico próprio. Preencha o que altera a decisão, não o
formulário por obrigação.
