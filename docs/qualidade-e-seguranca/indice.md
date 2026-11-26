# Qualidade e segurança

Esta parte pergunta como obter confiança sem confundir quantidade de testes com
qualidade e como preservar propriedades do sistema diante de erro, abuso e ação
adversária. O percurso começa pela seleção de evidências, aproxima os testes da
infraestrutura real e integra segurança às fronteiras já construídas.

Pressupõe contratos de falha, APIs e abstrações dos capítulos anteriores. Ao
final, o leitor deve conseguir escolher testes por risco, controlar fidelidade e
isolamento e compor autenticação, autorização, dados, segredos e cadeia de
suprimentos. Segurança aparece antes do aprofundamento de carga porque protege
as mesmas fronteiras exercitadas pelos testes sistêmicos.

C25 depende da telemetria apresentada em C27. Seu ID e seu agrupamento são
preservados, mas sua leitura avançada é recomendada depois de C27; até lá, C23 e
C24 formam um percurso completo de fundamentos de teste. As evidências desta
parte alimentam entrega e operação.

## Capítulos publicados

- **C23 · Fundamental · Publicado:** [Testes orientados a comportamento e
  risco](c23-testes-comportamento-risco.md).
- **C24 · Intermediário · Publicado:** [Integração real e testes incrementais de
  legado](c24-integracao-real-testes-legado.md).
- **C26 · Intermediário · Publicado:** [Segurança nas fronteiras da
  aplicação](c26-seguranca-fronteiras-aplicacao.md).
- **C25 · Avançado · Publicado:** [Cobertura, E2E e testes de
  carga](c25-cobertura-e2e-carga.md).

## Material complementar

- [Acessibilidade em aplicações .NET](acessibilidade-aplicacoes-dotnet.md).

Consulte o [mapa editorial](../planejamento/mapa-editorial.md) para escopo,
dependências e estratégia de exemplos. Volte ao [índice central](../indice.md).
