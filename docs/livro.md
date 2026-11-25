# Livro Engenharia .NET

O livro reúne os capítulos publicados e os materiais complementares em um
único PDF. Os arquivos Markdown continuam sendo a fonte de verdade.

## Organização

O índice central define as áreas. Os índices de cada área definem a ordem
de leitura dos capítulos. A geração verifica a cobertura de todos os arquivos
de capítulo e inclui os materiais complementares, guias de compatibilidade
e a trilha de estudos como apêndices. Os documentos de planejamento,
contribuição e manutenção ficam no repositório.

As referências entre capítulos apontam para o próprio PDF. Referências a
arquivos não incluídos apontam para o commit de origem no GitHub.
Os diagramas Mermaid são convertidos em imagens de alta resolução.
Os comentários internos não fazem parte da edição publicada.

## Gerar e validar

Em Linux, instale Python 3, Pandoc, Node.js 22, Chrome ou Chromium, as fontes
DejaVu e as bibliotecas nativas do WeasyPrint. O arquivo
[de validação](../.github/workflows/validar-livro.yml) registra os comandos
usados no CI, incluindo as versões das ferramentas de renderização.

Execute a partir da raiz do repositório:

~~~sh
python -m unittest discover -s scripts/tests -p "test_gerar_livro.py" -v
python scripts/gerar-livro.py
~~~

O resultado fica em `dist/livro/engenharia-dotnet.pdf`.
O fluxo de validação disponibiliza o mesmo PDF como artefato de cada PR
relevante. Confira a capa, o sumário, as tabelas, os blocos de código e os
diagramas antes de aprovar uma mudança editorial.

## Publicar

Após o merge e a conferência do PDF, execute manualmente o fluxo
[Publicar livro PDF](../.github/workflows/publicar-livro.yml) na branch main.
Também é possível iniciar pelo GitHub CLI:

~~~sh
gh workflow run publicar-livro.yml --ref main
~~~

A edição recebe uma tag específica do commit. A publicação não substitui
edições anteriores nem altera a release principal de código. O PDF fica
disponível nos arquivos anexos da edição na página de releases.
Se a edição já existir, a publicação falha em vez de sobrescrever o arquivo.

## Limitações

A geração não comprova conformidade com PDF/UA nem acessibilidade integral
do PDF. O Markdown continua disponível como alternativa. Os diagramas são
imagens, acompanhadas pelas explicações dos capítulos. A validação automática
não substitui uma revisão visual. O ambiente do sistema e as dependências
transitivas podem mudar, portanto não há garantia de igualdade binária
entre gerações do mesmo commit.
