---
name: revisar-conteudo-tecnico
description: >-
  Revisar capítulos, documentação e autoavaliações do Engenharia .NET
  quanto a precisão conceitual, fontes primárias, didática, exemplos,
  links, gabaritos e coerência editorial. Use para revisões de Markdown.
---

# Revisar conteúdo técnico

Consulte `AGENTS.md`, `docs/guia-de-estilo.md`, o mapa editorial,
o capítulo canônico e a issue pertinente antes de revisar.

## Exatidão e rastreabilidade

1. Delimite conceito, versão C#/.NET e nível de conhecimento esperado.
2. Confirme afirmações com especificação, fonte primária ou exemplo
   observável; não use apenas a planilha de origem como autoridade.
3. Diferencie garantia, detalhe de implementação e recomendação
   contextual. Identifique simplificações que podem induzir a erro.
4. Verifique snippets pela skill `validar-exemplos-csharp` quando
   necessário; Markdown validado não implica código compilado.
5. Confirme referências e links relativos aos capítulos publicados.

## Questões e gabaritos

- O enunciado deve conter informações suficientes e não revelar a solução.
- Deve haver exatamente uma alternativa defensável como correta.
- O gabarito deve corresponder à semântica real do código e justificar
  a correta; explique distratores relevantes quando for útil.
- Investigue condições em que outra opção também seria válida.
- Prefira cenários aplicados, não perguntas de definição ou entrevista.
- Compare quantidade, estrutura e entrega com a issue: desvios
  editoriais são decisões a alinhar, não bugs automáticos.
- Quando o formato acordado solicitar, use respostas recolhíveis
  com `<details>`, observando a renderização nativa do GitHub.

## Linguagem e revisão final

Mantenha português brasileiro, termos consistentes, trade-offs
explícitos, links funcionais e conteúdo canônico sem duplicação.

Reporte erro factual, clareza e preferência separadamente. Cada
achado deve citar local, impacto didático e correção sugerida.
Dê prioridade a gabaritos errados, comportamento inexato e
exemplos inválidos; não mascare isso com sugestões de estilo.

Informe o que foi testado e quais verificações não foram executadas.
