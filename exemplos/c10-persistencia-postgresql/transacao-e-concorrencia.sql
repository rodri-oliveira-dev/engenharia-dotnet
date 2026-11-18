\set ON_ERROR_STOP on
SET search_path TO c10_lab;

\echo 'Transação revertida: os saldos permanecem iguais'
BEGIN;
UPDATE conta SET saldo = saldo - 100.00 WHERE id = 1 AND saldo >= 100.00;
UPDATE conta SET saldo = saldo + 100.00 WHERE id = 2;
INSERT INTO transferencia (id, origem_id, destino_id, valor)
VALUES ('018f8d35-4e7a-7a61-bb88-f4d9dfb29110', 1, 2, 100.00);
ROLLBACK;
TABLE conta;

\echo 'Controle otimista: a primeira atualização afeta uma linha; a obsoleta, zero'
UPDATE produto
SET nome = 'Produto atualizado', preco = 129.90, versao = versao + 1
WHERE id = 42 AND versao = 1;

UPDATE produto
SET nome = 'Escrita obsoleta', preco = 109.90, versao = versao + 1
WHERE id = 42 AND versao = 1;
TABLE produto;

\echo 'Deadlock exige duas sessões. Copie os blocos abaixo na ordem descrita no C10.'
\echo 'A: BEGIN; UPDATE c10_lab.conta SET saldo=saldo-10 WHERE id=1;'
\echo 'B: BEGIN; UPDATE c10_lab.conta SET saldo=saldo-20 WHERE id=2;'
\echo 'A: UPDATE c10_lab.conta SET saldo=saldo+10 WHERE id=2;'
\echo 'B: UPDATE c10_lab.conta SET saldo=saldo+20 WHERE id=1;'
\echo 'Finalize a sessão vencedora com ROLLBACK para não alterar os dados do laboratório.'
