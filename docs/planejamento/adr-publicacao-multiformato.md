# ADR: publicação do livro em múltiplos formatos

## Contexto

O livro consolida capítulos e materiais complementares em uma edição
gratuita. O PDF preserva a paginação, mas a leitura em telas pequenas
precisa de conteúdo que se adapte ao tamanho da fonte e da tela.

## Decisão

Gerar PDF e EPUB 3 a partir da mesma estrutura intermediária do Pandoc,
com seleção de capítulos, resolução de referências e renderização de
diagramas compartilhadas. Cada formato usa seu próprio estilo.

O PDF continua sendo renderizado pelo WeasyPrint. O EPUB usa o escritor
EPUB 3 do Pandoc, capa própria derivada da arte existente e validação com
EPUBCheck. O EPUB incorpora os recursos locais, sem depender de acesso
à rede durante a leitura dos capítulos.

A publicação manual exige a validação dos dois formatos e distribui
ambos na mesma edição identificada pelo commit. O conteúdo editorial
continua sob CC BY-NC 4.0 e o código sob MIT.

## Alternativas

Converter o PDF em EPUB perderia a estrutura original e poderia
prejudicar a ordem de leitura, os links, as tabelas e o código.

Manter dois conjuntos de arquivos editoriais aumentaria o risco de
divergência entre formatos.

## Consequências

O fluxo inclui dependências adicionais para a capa e a validação do EPUB.
Os diagramas são preparados uma única vez por execução conjunta.
A paginação do PDF não corresponde à do EPUB, que depende do leitor.
Validação estrutural não substitui revisão visual ou testes de
acessibilidade em leitores digitais.
