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

## Qualidade e consistência

Antes de abrir o pull request:

1. releia o texto integralmente em português;
2. confirme que todos os links internos apontam para arquivos existentes;
3. execute `npx markdownlint-cli2@0.20.0` na raiz;
4. compile e teste os exemplos alterados, quando existirem;
5. revise o diff para evitar arquivos gerados, mudanças fora do escopo ou
   alterações acidentais na licença.

O pull request deve resumir a motivação, as decisões editoriais, os arquivos
afetados e as validações realizadas. Prefira contribuições pequenas e coerentes,
com documentação e código de exemplo evoluindo juntos.
