# Laboratório: EF Core 11 (C08 e C09)

Este projeto contém cenários curtos de EF Core 11 executados com SQLite
relacional real. O objetivo é confrontar **código LINQ**, **SQL gerado**
e **comportamento observado**, sem tratar apenas a compilação como prova.

## Execução

No diretório raiz do repositório, com o SDK .NET 11 disponível:

```bash
dotnet test exemplos/c08-c09-ef-core/C08C09.EfCore.Tests.csproj
```

O projeto usa `net11.0` e versão centralizada
`Microsoft.EntityFrameworkCore.Sqlite 11.0.0`; o projeto de testes
referencia também `Microsoft.EntityFrameworkCore.SqlServer 11.0.0`
para validar exclusivamente a tradução do `JsonPathExists`.
Não requer um servidor SQL Server para rodar os testes atuais.

## Cenários

- **C08:** tracking, N+1, split queries, paginação, plano de índice
  e compiled query assíncrona, no arquivo `PersistenciaTests.cs`.
- **EF Core 11 / FullJoin:** preserva o cadastro de cliente sem pedido
  e o pedido sem cliente, usando `FULL JOIN` no SQLite.
- **EF Core 11 / MaxByAsync e MinByAsync:** encontra entidades por
  contagem de itens, inspecionando o comando SQL enviado ao provider.
- **EF Core 11 / GroupBy:** soma e conta itens por status do pedido.
- **EF Core 11 / group_concat:** ordenação de marcadores dentro do
  `string.Join` traduzida para SQLite, que deve ser 3.44+.
- **EF Core 11 / split query:** `Categoria` (referência) e `Itens`
  (coleção), inspecionando as duas consultas e os joins.
- **EF Core 11 / SQL Server JSON:** `ToQueryString()` verifica
  `JSON_PATH_EXISTS`, sem abrir conexão nem executar a consulta.
- **EF Core 11 / migrations:** detecta a nova exceção ao chamar
  `MigrateAsync` sem migrations.
- **C09:** concorrência otimista, comparação de valores e merge após
  conflito de `DbUpdateConcurrencyException`.

As entidades `ClienteExterno` e `PedidoExterno` não possuem relação de
chave estrangeira intencionalmente: o exercício de conciliação deve
preservar os dois tipos de órfão. Cada teste SQLite usa um arquivo
temporário próprio e remove o banco ao terminar.

## Limites dos testes

- `ToQueryString()` prova tradução de consulta, não execução em
  produção nem desempenho.
- O SQLite não reproduz locks, `rowversion` e planos SQL Server.
- A concatenação ordenada precisa de SQLite 3.44.0 ou superior.
- SQL Server JSON é testado apenas por tradução; exigiria instância
  SQL Server 2022+ para execução real.
- O teste de `MigrateAsync` sem migrations cobre uma falha
  proposital; o laboratório usa `EnsureCreatedAsync`, não um
  workflow de migrations para produção.

Veja a [documentação de compatibilidade](../../docs/aplicacoes-e-dados/ef-core-11-compatibilidade.md)
e os capítulos [C08](../../docs/aplicacoes-e-dados/c08-consultas-ef-core.md)
e [C09](../../docs/aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md).
