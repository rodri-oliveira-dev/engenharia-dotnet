# Validação ortográfica em português brasileiro

O repositório usa **CSpell 10.4.0** e o dicionário
[`@cspell/dict-pt-br`](https://www.npmjs.com/package/@cspell/dict-pt-br),
com configuração em [`cspell.json`](../cspell.json) e vocabulário
técnico em [`.cspell/termos-tecnicos.txt`](../.cspell/termos-tecnicos.txt).

## Comportamento do pipeline

No PR, o job **Validar português (pt-BR)** compara o commit de destino
com o commit da branch e analisa apenas as **linhas novas ou alteradas**
de arquivos `.md`. O script mantém os números originais das linhas
para facilitar a identificação de avisos. Trechos entre crases,
blocos de código, URLs, nomes de arquivos em links, HTML de metadados e caminhos são ignorados,
mas textos visíveis de links são revisados.

Ao executar em `main`, usa-se o commit anterior ao push como base.
Linhas removidas não são analisadas. O verificador é **bloqueante**
para palavras desconhecidas no trecho novo. Ele **não detecta**
automaticamente todos os erros de concordância, pontuação ou gramática.
A revisão editorial continua necessária.

## Execução local

Requer Node.js 22 e Python 3.13:

```bash
npm install
python -m unittest discover -s scripts/tests -p "test_validar_portugues_pr.py"
python scripts/validar-portugues-pr.py --base main --head HEAD
```

Opcionalmente, para verificar o conjunto completo de arquivos, inclusive
erros anteriores:

```bash
npm run ortografia
```

As dependências diretas estão fixadas em `package.json`. A instalação
do CI não executa scripts de pacotes npm. A resolução transitiva deve
ser reavaliada e travada por um arquivo de lock em uma evolução futura.

## Manutenção do dicionário

1. Verifique se o termo apontado é um erro real de grafia.
2. Se for vocabulário técnico consolidado, adicione uma palavra por linha
   ao arquivo `.cspell/termos-tecnicos.txt`.
3. Preserve a grafia correta de termos portugueses no texto, sem
   transformá-los em exceções do dicionário.
4. Não adicione nomes de variáveis, URLs, tokens ou identificadores
   que devam ficar entre crases no Markdown.
5. Evite ignorar um arquivo inteiro apenas para fazer o CI passar.

O job contém um teste positivo (palavra correta) e negativo (erro
intencional) para garantir que o CSpell está ativo e que um erro
ortográfico consegue reprovar a verificação.

## Diagnóstico e limitações

A lista técnica não é uma fonte normativa de escrita; ela aceita
termos presentes nos conteúdos do projeto, inclusive nomes próprios
de produtos. O suporte bilíngue pt-BR/inglês reduz falsos positivos
de termos técnicos, mas não garante que a ferramenta perceba uma
palavra portuguesa incorreta que também exista em inglês.

O GitHub Actions verifica a documentação atualizada em cada PR; a
[política de proteção de branch](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
deve exigir o status **Validar português (pt-BR)** para efetivamente
impedir merge. A existência do job sozinha não altera a proteção da
branch.
