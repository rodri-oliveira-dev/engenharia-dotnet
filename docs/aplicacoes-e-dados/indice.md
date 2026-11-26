# Aplicações e dados

Esta parte transforma contratos da linguagem em aplicações que recebem trabalho,
coordenam dependências e preservam dados. O percurso parte da composição do host
e das fronteiras HTTP, apresenta as garantias da persistência antes do uso do
ORM e termina com decisões que combinam concorrência, consistência e capacidade.

Pressupõe principalmente C02 e C05; cada capítulo declara as dependências
adicionais. C10 aparece antes de C08 e C09 porque SQL, transações, índices e
isolamento explicam o comportamento que o EF Core apenas intermedeia. Ao final,
o leitor deve conseguir distinguir contrato HTTP, unidade de trabalho, garantia
do banco e detalhe do provider.

C36 é um aprofundamento opcional adicionado depois dos demais capítulos. Seu ID
permanece estável, mas a leitura é recomendada após C17, que desenvolve
consistência distribuída. Quem seguir o livro linearmente pode retornar a C36
depois da parte de sistemas distribuídos. A modelagem das regras que esses
mecanismos sustentam começa em Domínio e arquitetura.

## Capítulos publicados

- **C06 · Fundamental · Publicado:**
  [Composição e pipeline no ASP.NET Core](c06-composicao-pipeline-aspnet-core.md).
- **C07 · Intermediário · Publicado:**
  [APIs HTTP e integrações resilientes](c07-apis-http-integracoes-resilientes.md).
- **C10 · Fundamental · Publicado:**
  [Fundamentos de persistência SQL e NoSQL](c10-fundamentos-persistencia-sql-nosql.md).
- **C08 · Intermediário · Publicado:**
  [Consultas eficientes com EF Core](c08-consultas-ef-core.md).
- **C09 · Intermediário · Publicado:**
  [Fronteiras e concorrência no EF Core](c09-fronteiras-concorrencia-ef-core.md).
- **C36 · Avançado · Publicado:**
  [Arquitetura de cache e consistência de dados](c36-arquitetura-cache-consistencia.md).

## Compatibilidade do ASP.NET Core

- [Atualização e breaking changes do ASP.NET Core 11](../compatibilidade/compatibilidade-aspnet-core-11.md).

## Atualização para EF Core 11

- [Novidades, testes e compatibilidade do EF Core 11](ef-core-11-compatibilidade.md).

Consulte o [mapa editorial](../planejamento/mapa-editorial.md) para escopo,
dependências e estratégia de exemplos. Volte ao [índice central](../indice.md).
