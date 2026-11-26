# Livro Engenharia .NET

O livro reúne os capítulos publicados e os materiais complementares em um
livro nos formatos PDF e EPUB 3. Os arquivos Markdown continuam sendo a fonte de verdade.

## Distribuição e licença

A edição oficial é gratuita. O conteúdo editorial original e as edições PDF e EPUB seguem
[CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/deed.pt).
Os trechos de código mantêm a [licença MIT](../LICENSE-CODE).
Após a capa, seguem os direitos autorais, a dedicatória, a epígrafe e o sumário.
O histórico de edições e a seção de autenticidade ficam após os créditos
e antes da licença de código. A
[política de licenciamento](../LICENSE) preserva permissões anteriores e
licenças de terceiros.

## Capa

A capa usa uma composição vetorial inspirada em desenho arquitetural, com
textos em português, identificação da edição e revisão obtidas do commit.
No PDF, o cabeçalho e o rodapé mantêm o nome e o endereço do projeto.
O EPUB usa uma capa vertical com a mesma identidade visual.

## Experiência de leitura

Após o sumário, a página Sobre o autor apresenta a trajetória profissional,
a formação e os canais públicos do autor. A introdução explica os percursos
de estudo. O texto principal usa corpo de
11 pontos e os blocos de código usam 9,5 pontos. Os metadados dos capítulos
recebem tratamento visual próprio, e as tabelas se ajustam ao conteúdo.
O EPUB usa diagramação fluida, com fontes ajustáveis pelo leitor e sumário
navegável. Os exemplos de código usam uma paleta clara inspirada em IDE.
No EPUB, leitores compatíveis com a preferência de tema escuro recebem
uma paleta escura; os demais usam a paleta clara de contraste definido.
O código permanece texto selecionável. O PDF inclui marcação estrutural e o fluxo gera amostras das primeiras dez
páginas para revisão visual. A marcação não comprova conformidade com PDF/UA.

## Decisão arquitetural

Consulte o [registro da decisão multiformato](planejamento/adr-publicacao-multiformato.md).

## Organização

O índice central define as áreas. Os índices de cada área definem a ordem
de leitura dos capítulos. A geração verifica a cobertura de todos os arquivos
de capítulo e inclui os materiais complementares, guias de compatibilidade
e a trilha de estudos como apêndices. Os documentos de planejamento,
contribuição e manutenção ficam no repositório.

As referências entre capítulos apontam para o próprio livro em ambos os formatos. Referências a
arquivos não incluídos apontam para o commit de origem no GitHub.
Os diagramas Mermaid são convertidos em imagens de alta resolução.
Os comentários internos não fazem parte da edição publicada.

## Gerar e validar

Em Linux, instale Python 3.13 (versão usada no CI), Pandoc, Node.js 22,
Chrome ou Chromium, as fontes DejaVu e as bibliotecas nativas do WeasyPrint
68.0. Instale também `poppler-utils`, que fornece `pdftotext` e `pdfinfo`,
`epubcheck` e `librsvg2-bin`, que fornece `rsvg-convert`. O arquivo
[de validação](../.github/workflows/validar-livro.yml) registra os comandos
usados no CI, incluindo as versões das ferramentas de renderização.

Instale as ferramentas Python e Node.js usadas na renderização. O segundo
comando instala o `mmdc` em um diretório temporário; o terceiro o adiciona ao
`PATH` da sessão atual:

~~~sh
python -m pip install "weasyprint==68.0"
npm install --prefix /tmp/engenharia-dotnet-livro-tools --no-audit --no-fund "@mermaid-js/mermaid-cli@11.4.2"
export PATH="/tmp/engenharia-dotnet-livro-tools/node_modules/.bin:$PATH"
~~~

Depois, execute a partir da raiz do repositório:

~~~sh
python -m unittest discover -s scripts/tests -p "test_gerar_livro.py" -v
python scripts/gerar-livro.py --format both
python scripts/livro/validar_pdf.py dist/livro/engenharia-dotnet.pdf
~~~

Os resultados ficam em `dist/livro/engenharia-dotnet.pdf` e
`dist/livro/engenharia-dotnet.epub`. Use `--format pdf` ou `--format epub`
para gerar somente um formato. A geração do EPUB verifica as âncoras e
executa o EPUBCheck antes de promover o arquivo final.
O fluxo de validação disponibiliza os livros gerados como artefatos de cada PR
relevante, mesmo se a validação editorial posterior falhar. O artefato de
revisão não equivale a uma edição aprovada: a publicação exige sucesso de
todas as verificações. Confira a capa, o sumário, as tabelas, os blocos de código e os
diagramas antes de aprovar uma mudança editorial.

## Publicar

Após o merge e a conferência do PDF, execute manualmente o fluxo
[Publicar livro PDF e EPUB](../.github/workflows/publicar-livro.yml) na branch main.
Também é possível iniciar pelo GitHub CLI:

~~~sh
gh workflow run publicar-livro.yml --ref main
~~~

O histórico consulta releases públicas com a tag do livro e os dois arquivos.
Versões de código, testes, rascunhos e publicações incompletas não são
considerados edições oficiais. Gerações locais sem o JSON de releases
informam que a consulta não ocorreu. A revisão atual é identificada
separadamente, sem antecipar a data de publicação.

A edição recebe uma tag específica do commit. A publicação não substitui
edições anteriores nem altera a release principal de código. O PDF e o EPUB ficam
disponíveis nos arquivos anexos da edição na página de releases.
Se a edição já existir, a publicação falha em vez de sobrescrever o arquivo.

## Autenticidade e procedência

O fluxo oficial de publicação na branch main gera uma atestação de
proveniência SLSA para cada formato com GitHub Artifact Attestations,
usando identidade OIDC e Sigstore. A geração e a validação dos livros
não recebem permissões de assinatura ou publicação. Um job separado
atesta os arquivos validados sem executar o conteúdo dos livros;
outro job verifica novamente as atestações antes de publicar.

A release inclui cinco arquivos: PDF, EPUB, duas atestações
(`engenharia-dotnet.pdf.sigstore.json` e `engenharia-dotnet.epub.sigstore.json`)
e `SHA256SUMS`.

Para verificar a origem pela API do GitHub, use o GitHub CLI atualizado.
Autentique-o com `gh auth login` ou configure `GH_TOKEN` no ambiente. Depois,
substitua o valor de `REVISAO` pelo SHA completo da tag da edição, sem o
prefixo `livro-`:

~~~sh
gh auth login
REVISAO="SHA_COMPLETO_DA_EDICAO"
REPO="rodri-oliveira-dev/engenharia-dotnet"
gh attestation verify engenharia-dotnet.pdf \
  --repo "$REPO" \
  --signer-workflow "$REPO/.github/workflows/publicar-livro.yml" \
  --source-ref refs/heads/main --source-digest "$REVISAO" \
  --deny-self-hosted-runners
~~~

Repita o comando para `engenharia-dotnet.epub`. Para verificar sem rede,
execute antes, em uma máquina conectada e autenticada,
`gh attestation trusted-root > trusted_root.jsonl`. Transfira para o ambiente
isolado o artefato, a atestação distribuída e esse arquivo de raízes
confiáveis. Então, execute:

~~~sh
gh attestation verify engenharia-dotnet.pdf \
  --repo "$REPO" \
  --signer-workflow "$REPO/.github/workflows/publicar-livro.yml" \
  --source-ref refs/heads/main --source-digest "$REVISAO" \
  --deny-self-hosted-runners \
  --bundle engenharia-dotnet.pdf.sigstore.json \
  --custom-trusted-root trusted_root.jsonl
~~~

Use os nomes correspondentes para verificar o EPUB. Gere uma raiz atualizada
sempre que importar novo material assinado para o ambiente sem rede.

Em Linux, confira as somas com `sha256sum --check SHA256SUMS`. A soma
isolada não autentica a origem: verifique também a assinatura.

Consulte a [documentação do GitHub sobre atestações](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations),
o [guia de verificação offline](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/verify-attestations-offline),
os manuais de [`gh attestation verify`](https://cli.github.com/manual/gh_attestation_verify)
e [`gh attestation trusted-root`](https://cli.github.com/manual/gh_attestation_trusted-root)
e o [registro da decisão](planejamento/adr-proveniencia-livro.md).

As atestações não são assinaturas pessoais do autor, não equivalem a
certificados ICP-Brasil e não demonstram a titularidade de direitos
autorais. Artefatos de PR e gerações locais não são atestados.

## Limitações

A geração não comprova conformidade com PDF/UA nem acessibilidade integral
do livro. O EPUBCheck verifica a conformidade estrutural do EPUB, não a
qualidade visual em todos os dispositivos. O Markdown continua disponível como alternativa. Os diagramas são
imagens, acompanhadas pelas explicações dos capítulos. A validação automática
não substitui uma revisão visual. O ambiente do sistema e as dependências
transitivas podem mudar, portanto não há garantia de igualdade binária
entre gerações do mesmo commit.
