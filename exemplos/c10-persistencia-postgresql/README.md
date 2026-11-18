# Laboratório C10 — PostgreSQL

Este laboratório permite observar índices, planos, transações e concorrência do
C10 em uma instância real de PostgreSQL. Ele não é benchmark e não fixa um plano
esperado: custos e nós variam com versão, configuração, estatísticas e ambiente.

## Dependências

- PostgreSQL 16 ou posterior;
- cliente `psql` com acesso a uma base descartável;
- permissão para criar schema, tabelas e índices.

Defina a conexão pelos mecanismos normais do `psql` (`PGHOST`, `PGPORT`,
`PGDATABASE`, `PGUSER` e prompt de senha). Não grave credenciais no repositório.

## Execução

```bash
psql -v ON_ERROR_STOP=1 -f schema-e-dados.sql
psql -v ON_ERROR_STOP=1 -f indices-e-planos.sql
psql -v ON_ERROR_STOP=1 -f transacao-e-concorrencia.sql
```

O primeiro script recria somente o schema `c10_lab` e gera 100.000 pedidos
sintéticos com `generate_series`. O segundo executa `EXPLAIN (ANALYZE, BUFFERS)`
antes e depois do índice; compare a saída produzida na sua máquina. O terceiro
executa exemplos seguros de rollback e controle otimista e imprime os blocos que
devem ser copiados para duas sessões a fim de provocar deadlock.

Não rode em uma base compartilhada. A remoção do schema no início apaga dados de
uma execução anterior desse laboratório. Para limpar ao final:

```sql
DROP SCHEMA c10_lab CASCADE;
```
