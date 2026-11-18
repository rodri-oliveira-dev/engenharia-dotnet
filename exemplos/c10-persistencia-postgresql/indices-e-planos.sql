\set ON_ERROR_STOP on
SET search_path TO c10_lab;

\echo 'Plano medido antes do índice composto'
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, criado_em, total
FROM pedido
WHERE tenant_id = 42
  AND status = 'aberto'
ORDER BY criado_em DESC
LIMIT 50;

CREATE INDEX ix_pedido_tenant_status_criado
    ON pedido (tenant_id, status, criado_em DESC);
ANALYZE pedido;

\echo 'Plano medido depois do índice composto'
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, criado_em, total
FROM pedido
WHERE tenant_id = 42
  AND status = 'aberto'
ORDER BY criado_em DESC
LIMIT 50;

\echo 'Apenas criado_em não possui a coluna inicial do índice'
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, criado_em, total
FROM pedido
WHERE criado_em >= TIMESTAMPTZ '2026-03-01 00:00:00Z'
ORDER BY criado_em DESC
LIMIT 50;
