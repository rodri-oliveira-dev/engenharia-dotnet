\set ON_ERROR_STOP on

DROP SCHEMA IF EXISTS c10_lab CASCADE;
CREATE SCHEMA c10_lab;
SET search_path TO c10_lab;

CREATE TABLE conta (
    id bigint PRIMARY KEY,
    saldo numeric(14, 2) NOT NULL CHECK (saldo >= 0),
    versao integer NOT NULL DEFAULT 1 CHECK (versao > 0)
);

CREATE TABLE transferencia (
    id uuid PRIMARY KEY,
    origem_id bigint NOT NULL REFERENCES conta (id),
    destino_id bigint NOT NULL REFERENCES conta (id),
    valor numeric(14, 2) NOT NULL CHECK (valor > 0),
    CHECK (origem_id <> destino_id)
);

CREATE TABLE produto (
    id bigint PRIMARY KEY,
    nome text NOT NULL,
    preco numeric(12, 2) NOT NULL CHECK (preco >= 0),
    versao integer NOT NULL DEFAULT 1 CHECK (versao > 0)
);

CREATE TABLE pedido (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id integer NOT NULL,
    status text NOT NULL CHECK (status IN ('aberto', 'pago', 'cancelado')),
    criado_em timestamptz NOT NULL,
    total numeric(12, 2) NOT NULL CHECK (total >= 0)
);

INSERT INTO conta (id, saldo) VALUES (1, 1000.00), (2, 1000.00);
INSERT INTO produto (id, nome, preco) VALUES (42, 'Produto inicial', 99.90);

INSERT INTO pedido (tenant_id, status, criado_em, total)
SELECT
    1 + (n % 200)::integer,
    CASE WHEN (n / 200) % 20 = 0 THEN 'aberto'
         WHEN (n / 200) % 5 = 0 THEN 'cancelado'
         ELSE 'pago' END,
    TIMESTAMPTZ '2026-01-01 00:00:00Z' + n * INTERVAL '1 minute',
    (10 + (n % 5000))::numeric(12, 2)
FROM generate_series(1, 100000) AS serie(n);

ANALYZE;
