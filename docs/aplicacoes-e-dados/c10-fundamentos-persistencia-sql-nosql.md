# C10 — Fundamentos de persistência SQL e NoSQL

> **Nível:** Fundamental
>
> **Objetivo:** compreender garantias, estruturas e custos dos bancos para
> escolher, modelar e diagnosticar a persistência a partir do acesso e das
> invariantes do sistema.
>
> **Pré-requisitos:** [C01 — Tipos, identidade e modelagem em C#](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md).
>
> **Última revisão:** 2026-11-18

Persistir não é apenas serializar objetos. O banco precisa representar
identidade e relações, rejeitar estados inválidos, coordenar acessos concorrentes,
recuperar-se de falhas e responder às consultas dentro de limites operacionais.
A escolha entre SQL e NoSQL, o desenho de um índice ou o nível de isolamento são
decisões sobre essas garantias e custos.

Este capítulo apresenta os mecanismos que sustentam essas decisões. O uso do
Entity Framework Core para tradução, projeção e tracking está no
[C08](c08-consultas-ef-core.md); unidade de trabalho e tokens de concorrência no
[C09](c09-fronteiras-concorrencia-ef-core.md). Saga, entrega de mensagens e
implementação completa de Outbox terão capítulos próprios.

## Comece por invariantes e padrões de acesso

Antes de escolher uma categoria de banco, registre:

- quais dados são lidos e alterados juntos;
- quais invariantes precisam valer após cada commit;
- quais consultas exigem filtro, ordenação, agregação ou travessia de relações;
- volume atual, crescimento, distribuição das chaves e picos de concorrência;
- latência, disponibilidade, perda de dados e defasagem aceitáveis;
- necessidade de consultas ad hoc, auditoria e evolução do schema;
- capacidade da equipe para operar, observar, fazer backup e restaurar a solução.

O modelo lógico e o modelo de acesso influenciam um ao outro. Normalização reduz
duplicidade e anomalias de atualização; desnormalização pode economizar junções e
round trips, mas cria cópias a sincronizar. “Modelar orientado ao acesso” não
significa ignorar o domínio: significa materializar relações e agregados de modo
compatível com consultas, alterações e garantias reais.

## Relacional, documento, chave-valor, colunar e grafo

“NoSQL” agrupa modelos com contratos muito diferentes. A comparação útil não é
SQL contra uma tecnologia genérica, e sim capacidades concretas para uma carga.

| Modelo | Unidade natural | Favorece | Custo e risco recorrentes |
| --- | --- | --- | --- |
| Relacional | linha, tabela e relações declaradas | integridade referencial, consultas ad hoc, joins e transações entre várias linhas | schema e migrações exigem disciplina; escala horizontal pode demandar particionamento e coordenação |
| Documento | documento e campos aninhados | agregado lido junto, estrutura variável e evolução localizada | duplicação, documentos crescentes e consultas entre agregados podem complicar consistência |
| Chave-valor | valor opaco localizado por chave | acesso direto previsível, cache, sessão e estado simples | consultas secundárias e relações precisam de estruturas adicionais ou da aplicação |
| Colunar distribuído (*wide-column*) | linha particionada por chave, com famílias/colunas | escrita distribuída e consultas conhecidas por chave/partição | modelagem por consulta, duplicação e pouca flexibilidade para novos acessos |
| Grafo | vértices, arestas e propriedades | travessias de profundidade variável e relações como conceito central | operação especializada; agregados simples e relatórios tabulares podem não justificar o modelo |

Armazenamento analítico colunar, como *columnstore*, não é sinônimo de banco
NoSQL *wide-column*. O primeiro organiza valores por coluna para varrer e
comprimir grandes conjuntos analíticos; o segundo é um modelo distribuído de
acesso por chave de partição.

### Comparação contextual: catálogo de produtos

Um catálogo com preço, estoque, fornecedor e pedidos possui relações fortes.
Se preço válido, reserva de estoque e item do pedido precisam mudar atomicamente,
um modelo relacional com `FOREIGN KEY`, `CHECK`, `UNIQUE` e transação expressa
bem as invariantes. Variações de produto podem usar tabelas próprias ou uma
coluna JSON quando apenas parte dos atributos é flexível.

Um catálogo de leitura no qual cada produto tem atributos diferentes e a página
sempre carrega descrição, variações e resumo de avaliações em conjunto pode
usar um documento por produto:

```json
{
  "_id": "produto-42",
  "nome": "Tênis",
  "atributos": { "tamanho": [38, 39, 40], "material": "couro" },
  "avaliacoesRecentes": [
    { "nota": 5, "texto": "Confortável" }
  ]
}
```

O documento evita joins nesse acesso, mas o resumo duplicado precisa de uma
política de atualização. Embutir avaliações sem limite também pode criar
crescimento indesejado; separar histórico e manter apenas as recentes é uma
decisão de acesso. Não há vencedor universal: pedidos podem permanecer no
relacional enquanto uma projeção documental atende o catálogo, desde que a
equipe assuma sincronização e operação de dois armazenamentos.

### Alto volume não implica NoSQL

Volume isolado não informa formato das consultas, distribuição de chaves,
contenção, latência, invariantes nem competência operacional. Bancos relacionais
podem usar índices, particionamento, réplicas de leitura, processamento em lote e
escala vertical; bancos NoSQL também podem sofrer com partições quentes, índices
secundários caros, amplificação de escrita e coordenação entre nós.

Trocar de modelo apenas porque “há muitos dados” pode remover constraints e
joins sem remover as relações do negócio: elas reaparecem como código,
duplicidade e reconciliação. Meça primeiro o gargalo. Considere outro modelo
quando o padrão de acesso e as garantias combinarem com ele e quando as opções
mais simples tiverem limites demonstrados.

### Critérios de escolha e custo operacional

Considere conjuntamente:

- **consistência:** unidade atômica, isolamento, constraints e defasagem aceita;
- **acesso:** chave, faixa, texto, agregação, join, travessia e consulta ad hoc;
- **escala:** throughput, tamanho, distribuição, hotspots e estratégia de shard;
- **operação:** alta disponibilidade, upgrade, backup, restauração testada,
  observabilidade, segurança e resposta a incidentes;
- **ecossistema:** drivers, ferramentas, conhecimento e portabilidade;
- **custo total:** infraestrutura, licenças, desenvolvimento, migrações,
  reconciliação e plantão.

Comece com a tecnologia mais simples que satisfaça as garantias. Persistência
poliglota é válida quando cada banco resolve uma necessidade comprovada; ela
também multiplica modos de falha e trabalho operacional.

## ACID é um conjunto de garantias, não uma regra de negócio

Uma transação local delimita operações que o banco tenta confirmar como uma
unidade:

- **Atomicidade:** todas as alterações da transação são confirmadas ou nenhuma
  é. Rollback não desfaz um e-mail ou uma chamada HTTP já enviados.
- **Consistência:** a transação leva o banco de um estado válido para outro
  segundo constraints e mecanismos implementados. Regras não declaradas ao
  banco continuam responsabilidade da aplicação e do desenho concorrente.
- **Isolamento:** transações concorrentes observam efeitos conforme o nível e a
  implementação escolhidos; não significa ausência de concorrência.
- **Durabilidade:** depois do commit reconhecido, o banco preserva o resultado
  conforme sua configuração de log, replicação e armazenamento. Confirme opções
  que relaxam `fsync`, *write concern* ou confirmação síncrona.

Constraints são a última linha de defesa compartilhada. Use `NOT NULL`,
`UNIQUE`, `CHECK`, chaves estrangeiras e tipos adequados para regras que o banco
consegue expressar. A aplicação ainda precisa validar cedo, autorizar a operação
e traduzir violações esperadas. Uma constraint única, por exemplo, é mais segura
contra corrida que “consultar se existe e depois inserir”.

### Múltiplas operações e rollback no PostgreSQL

O exemplo transfere saldo e registra o lançamento na mesma transação local:

```sql
BEGIN;

UPDATE conta
SET saldo = saldo - 100.00
WHERE id = 1
  AND saldo >= 100.00;

-- A aplicação deve exigir exatamente uma linha alterada.
UPDATE conta
SET saldo = saldo + 100.00
WHERE id = 2;

INSERT INTO transferencia (id, origem_id, destino_id, valor)
VALUES ('018f8d35-4e7a-7a61-bb88-f4d9dfb29110', 1, 2, 100.00);

-- Se qualquer validação falhar:
ROLLBACK;
-- Somente após sucesso de todas as etapas seria usado COMMIT.
```

O `ROLLBACK` desfaz as três alterações no PostgreSQL. Ele não cobre outro banco
nem efeitos externos. Uma transferência real também exige constraints de valor,
identidade idempotente e tratamento do resultado de cada `UPDATE`.

## Isolamento transacional e anomalias

Os nomes vêm do padrão SQL, mas a implementação varia entre SGBDs e até pela
configuração do mesmo produto. As anomalias principais são:

- **dirty read:** ler dado escrito por uma transação ainda não confirmada;
- **non-repeatable read:** reler uma linha e encontrar valor confirmado diferente;
- **phantom read:** repetir um predicado e encontrar outro conjunto de linhas;
- **lost update:** duas operações derivam uma escrita do mesmo estado e uma
  sobrescreve a outra sem detectar o conflito.

| Nível conceitual | Proteção mínima esperada | Trade-off típico |
| --- | --- | --- |
| `READ UNCOMMITTED` | permite leitura suja segundo o padrão | baixa proteção; resultados podem nunca ter existido como estado confirmado |
| `READ COMMITTED` | não lê dados não confirmados | cada comando pode observar um snapshot/estado confirmado diferente |
| `REPEATABLE READ` | preserva linhas já lidas | tratamento de phantoms e conflitos depende do SGBD |
| `SERIALIZABLE` | efeito equivalente a alguma execução serial | mais bloqueios ou abortos; a aplicação deve repetir a transação inteira |
| `SNAPSHOT` | visão versionada consistente, quando oferecida | mantém versões e pode abortar conflitos de escrita; não equivale automaticamente a serializável |

Essa tabela não prova que `READ COMMITTED` evita lost update. Um comando atômico
como `SET saldo = saldo - 10` é diferente de ler o saldo, calculá-lo na aplicação
e depois gravar um valor absoluto. Proteja a segunda forma com versão, lock ou
isolamento apropriado.

### PostgreSQL

PostgreSQL usa controle de concorrência multiversão (MVCC). `READ UNCOMMITTED`
se comporta como `READ COMMITTED`. Neste último, cada comando recebe um novo
snapshot; duas leituras na mesma transação podem divergir. O `REPEATABLE READ`
usa snapshot da transação e, na implementação atual, também impede phantom
reads, embora ainda permita anomalias de serialização. `SERIALIZABLE` usa
isolamento serializável por snapshot e pode abortar com `serialization_failure`;
o cliente precisa repetir toda a unidade com backoff limitado.

`SELECT ... FOR UPDATE` bloqueia linhas selecionadas para alterações
concorrentes. Use-o somente quando a decisão realmente depender do estado
travado e mantenha a transação curta. Um lock não deve ficar aberto enquanto o
usuário pensa ou enquanto a aplicação chama um serviço remoto.

### SQL Server

No SQL Server, o `READ COMMITTED` padrão pode usar shared locks quando
`READ_COMMITTED_SNAPSHOT` está desabilitado ou versionamento por linha quando a
opção está habilitada. `REPEATABLE READ` mantém proteção das linhas lidas, mas
inserções no intervalo ainda podem produzir phantoms. `SERIALIZABLE` pode usar
key-range locks. `SNAPSHOT` é habilitado por configuração e mantém visão no nível
da transação; escritas concorrentes podem causar conflito de atualização.

Portanto, `NOLOCK` não é uma correção genérica para bloqueio: ele corresponde a
proteção fraca de leitura e pode retornar dados não confirmados, ausentes ou
duplicados. Investigue a transação bloqueadora, o plano, os índices e o nível
necessário.

Outros bancos atribuem semânticas próprias aos mesmos nomes. MySQL/InnoDB, Oracle
e bancos distribuídos devem ser avaliados pela documentação da versão em uso,
não por uma matriz copiada de outro produto.

## Índices são estruturas de acesso com custo

Um índice mantém chaves organizadas e referências às linhas. Em PostgreSQL,
`B-tree` é o padrão e atende igualdade, faixas e ordenação; `Hash` atende
igualdade; `GIN` é comum para elementos de arrays, documentos e busca textual;
`GiST`/`SP-GiST` suportam classes especializadas; `BRIN` resume intervalos de
blocos e pode ser eficiente em tabelas grandes fisicamente correlacionadas.
SQL Server oferece rowstore B+ tree, índices clustered/nonclustered e columnstore,
entre outras variantes.

Cada índice consome armazenamento, cache, I/O de criação e trabalho em
`INSERT`, `UPDATE`, `DELETE`, vacuum/reorganização e backup. Índices redundantes
podem custar mais que a leitura que tentam acelerar. Índice único também aplica
uma regra; não o trate apenas como otimização.

### Seletividade, predicados e ordem

Seletividade descreve quanto um predicado reduz o conjunto. Uma coluna booleana
isolada frequentemente seleciona muitas linhas, então o otimizador pode preferir
varrer a tabela. Um índice parcial, quando suportado, pode representar somente o
subconjunto relevante:

```sql
-- PostgreSQL: útil se a aplicação consulta poucos pedidos ainda abertos.
CREATE INDEX ix_pedido_aberto_criado
    ON pedido (criado_em DESC)
    WHERE status = 'aberto';
```

O predicado da consulta precisa ser compatível com a estrutura. Envolver a
coluna em função pode impedir o acesso direto por um índice simples:

```sql
-- O índice comum em criado_em pode atender esta faixa.
WHERE criado_em >= TIMESTAMPTZ '2026-11-18 00:00:00-03'
  AND criado_em <  TIMESTAMPTZ '2026-11-19 00:00:00-03'

-- Esta transformação pode exigir índice de expressão específico.
WHERE criado_em::date = DATE '2026-11-18'
```

Índice composto não é a soma de índices simples. Para o B-tree PostgreSQL
`(tenant_id, status, criado_em DESC)`, igualdades nas colunas iniciais e faixa na
primeira coluna sem igualdade delimitam melhor o trecho percorrido:

```sql
CREATE INDEX ix_pedido_tenant_status_criado
    ON pedido (tenant_id, status, criado_em DESC);

SELECT id, criado_em, total
FROM pedido
WHERE tenant_id = 42
  AND status = 'aberto'
  AND criado_em >= TIMESTAMPTZ '2026-11-01 00:00:00Z'
ORDER BY criado_em DESC
LIMIT 50;
```

Esse índice também pode ajudar uma consulta apenas por `tenant_id`; uma consulta
apenas por `criado_em` tende a aproveitá-lo menos. PostgreSQL recente pode usar
*skip scan* em alguns casos, portanto “só funciona pelo prefixo esquerdo” é uma
heurística, não uma lei universal. Escolha ordem a partir de predicados reais,
igualdade/faixa, ordenação e distribuição; confirme no plano.

### Antes e depois de um índice adequado

O laboratório PostgreSQL cria dados sintéticos e permite observar o plano sem
fixar resultados. Execute primeiro:

```sql
ANALYZE pedido;

EXPLAIN (ANALYZE, BUFFERS)
SELECT id, criado_em, total
FROM pedido
WHERE tenant_id = 42
  AND status = 'aberto'
ORDER BY criado_em DESC
LIMIT 50;
```

Depois crie o índice composto anterior, atualize estatísticas e repita exatamente
o comando. Compare nós, estimativa e linhas reais, buffers, sort e tempo naquele
ambiente. Em tabela pequena, baixa seletividade ou cache favorável, um
`Seq Scan` pode continuar sendo a escolha correta. O exemplo não promete um
`Index Scan` nem publica números como se fossem universais.

## Ler planos antes de tentar otimizar

`EXPLAIN` mostra a decisão estimada; no PostgreSQL, `EXPLAIN ANALYZE` executa o
comando e acrescenta medições. Use `ANALYZE` com cuidado em escrita: ele realmente
modifica dados, salvo quando envolvido em transação revertida. No SQL Server,
distinga plano estimado de plano de execução real.

Termos variam. PostgreSQL mostra `Seq Scan`, `Index Scan`, `Index Only Scan` e
`Bitmap Heap Scan`. SQL Server costuma mostrar `Table Scan`, `Index Scan` e
`Index Seek`. “Full Table Scan” é uma descrição genérica, não o nome de todo nó.
Um scan não é automaticamente ruim: ler grande parte da tabela sequencialmente
pode ser mais barato que muitos acessos aleatórios pelo índice.

Analise de baixo para cima:

- **estimativa de cardinalidade:** diferenças grandes entre linhas estimadas e
  reais podem indicar estatísticas desatualizadas, correlação ou parâmetros;
- **acesso e filtros:** veja quantas linhas foram lidas e descartadas, não só as
  retornadas;
- **joins:** confirme cardinalidade de cada lado e se nested loop, hash ou merge
  combinam com o volume real;
- **ordenação/agregação:** procure sort, spill para disco, memória e índices que
  possam fornecer ordem útil;
- **paginação:** `OFFSET` grande ainda percorre/descarta linhas; paginação por
  cursor com chave única e estável escala melhor para navegação sequencial;
- **bloqueios:** uma consulta rápida isolada pode passar tempo esperando lock;
- **rede e materialização:** plano eficiente não compensa retornar milhões de
  linhas ou fazer muitos round trips.

Atualizar estatísticas pode corrigir estimativas; criar índice não corrige
predicado incorreto, N+1, join cartesiano, consulta que retorna dados demais,
contenção, rede lenta ou schema incompatível. Meça tempo no servidor e no
cliente, volume, waits e repetição antes de mudar.

## Concorrência pessimista e otimista

Controle pessimista adquire locks antes da decisão, assumindo que o conflito é
provável ou caro. Controle otimista lê uma versão, tenta gravar condicionada a
ela e rejeita se outro ator venceu.

| Estratégia | Boa candidata | Custo principal |
| --- | --- | --- |
| Pessimista | disputa curta por recurso escasso; decisão precisa do estado mais recente | espera, contenção, deadlock e redução de throughput |
| Otimista | conflitos raros; interação humana ou trabalho não deve reter lock | detectar, recarregar, mesclar ou pedir nova decisão |

### Atualização otimista explícita no PostgreSQL

```sql
UPDATE produto
SET nome = 'Novo nome',
    preco = 129.90,
    versao = versao + 1
WHERE id = 42
  AND versao = 7;
```

Se a contagem afetada for zero, a linha não existe mais ou outra transação mudou
a versão. A aplicação não deve tratar isso como sucesso: recarrega e aplica uma
política do caso de uso — abandonar, apresentar conflito, mesclar campos seguros
ou tentar novamente. O [C09](c09-fronteiras-concorrencia-ef-core.md) implementa
esse mecanismo com token no EF Core.

Retry automático só é seguro quando a operação inteira é idempotente ou quando
efeitos anteriores são conhecidos. Use limite de tentativas, backoff com jitter,
cancelamento e métricas. Não repita indefinidamente nem reutilize estado obsoleto.

### Contenção e deadlock

Transações longas retêm locks e versões por mais tempo. No exemplo PostgreSQL,
execute cada bloco em uma sessão, avançando na ordem indicada:

```sql
-- Sessão A
BEGIN;
UPDATE conta SET saldo = saldo - 10 WHERE id = 1;
-- aguarde a sessão B atualizar id = 2
UPDATE conta SET saldo = saldo + 10 WHERE id = 2;
```

```sql
-- Sessão B
BEGIN;
UPDATE conta SET saldo = saldo - 20 WHERE id = 2;
-- depois que A atualizou id = 1
UPDATE conta SET saldo = saldo + 20 WHERE id = 1;
```

A segura a linha 1 e espera a 2; B segura a 2 e espera a 1. PostgreSQL detecta o
ciclo e aborta uma transação. Faça `ROLLBACK` na sessão abortada. A prevenção
principal é acessar recursos na mesma ordem, reduzir a unidade e evitar I/O
externo dentro dela. Ainda assim trate deadlock como falha transitória e repita a
transação completa quando for seguro. Contenção sem ciclo apenas espera; timeouts
e observabilidade distinguem os casos.

## Dados em sistemas distribuídos

Uma transação local termina na fronteira de um banco. Se o commit ocorrer e a
publicação de um evento falhar, os estados divergem; se a mensagem for repetida,
o consumidor pode aplicar o efeito duas vezes. Transação distribuída existe em
alguns ambientes, mas aumenta acoplamento e nem todo participante a suporta.

Projete explicitamente:

- **consistência eventual:** qual estado intermediário é aceitável, por quanto
  tempo e como o usuário o percebe;
- **idempotência:** chave estável e resultado para que repetição não duplique o
  efeito; `UNIQUE` costuma fechar a corrida;
- **Outbox:** grave mudança de negócio e registro a publicar na mesma transação
  local; um processo posterior publica e marca progresso;
- **deduplicação:** consumidores registram mensagens processadas ou usam uma
  operação naturalmente idempotente;
- **compensação:** nova ação de negócio corrige um efeito confirmado; não é
  rollback técnico e pode exigir aprovação;
- **recuperação:** retry limitado, fila de falhas, reconciliação, replay
  controlado, alertas e procedimento manual testado.

Outbox reduz a janela entre banco e publicação, mas admite repetição se houver
falha após publicar e antes de marcar. Portanto, não substitui idempotência. A
implementação de mensageria, Saga e ordenação fica fora deste capítulo.

## Laboratório PostgreSQL

Os scripts em [`exemplos/c10-persistencia-postgresql`](../../exemplos/c10-persistencia-postgresql/README.md)
cobrem schema, carga sintética, comparação de índice, transação, controle
otimista e deadlock. Requerem PostgreSQL 16 ou posterior e `psql`; não exigem
.NET. Os comandos e limitações estão no README do laboratório.

Os planos são medidos localmente porque dependem de versão, estatísticas,
configuração, hardware, cache e dados. O laboratório não contém benchmark. Os
exemplos .NET dos capítulos C08/C09 usam SQLite e não validam MVCC, locks ou
planos do PostgreSQL.

## Checklist de decisão e diagnóstico

1. Explicite invariantes, unidade atômica e tolerância a defasagem.
2. Liste consultas e escritas críticas com cardinalidade e distribuição reais.
3. Escolha o modelo que represente essas garantias com menor custo total.
4. Coloque constraints compartilhadas no banco e regras contextuais no caso de uso.
5. Mantenha transações curtas e não inclua interação humana ou rede externa.
6. Defina política para conflito, deadlock, timeout e repetição.
7. Inspecione plano real, estatísticas, waits, rede e volume retornado.
8. Adicione índices para acessos comprovados e revise o custo de escrita.
9. Teste backup, restauração e recuperação de falhas, não apenas o caminho feliz.
10. Reavalie quando volume, distribuição ou garantias mudarem.

## Relação com outros capítulos

- [C08](c08-consultas-ef-core.md) aplica projeção, tracking, SQL gerado e
  diagnóstico de consultas no EF Core.
- [C09](c09-fronteiras-concorrencia-ef-core.md) aplica unidade de trabalho,
  transações e concorrência otimista no EF Core.
- [C07](c07-apis-http-integracoes-resilientes.md) diferencia retry HTTP,
  idempotência e cancelamento nas integrações.
- [C11](../dominio-e-arquitetura/c11-entidades-value-objects-agregados.md)
  define invariantes e limites de consistência dos agregados que a persistência
  precisa sustentar.
- C16 aprofundará idempotência, consumidores e Outbox; C17 aprofundará
  consistência eventual e Sagas; C24 tratará testes com banco real; C29,
  evolução compatível de schema.

## Referências

- [Transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html)
  — PostgreSQL 18, diferenças entre níveis e implementação MVCC, acesso em 18 novembro 2026.
- [Explicit locking](https://www.postgresql.org/docs/current/explicit-locking.html)
  — PostgreSQL 18, locks e deadlocks, acesso em 18 novembro 2026.
- [Constraints](https://www.postgresql.org/docs/current/ddl-constraints.html)
  — PostgreSQL 18, integridade declarativa, acesso em 18 novembro 2026.
- [Index types](https://www.postgresql.org/docs/current/indexes-types.html) e
  [multicolumn indexes](https://www.postgresql.org/docs/current/indexes-multicolumn.html)
  — PostgreSQL 18, estruturas e ordem das colunas, acesso em 18 novembro 2026.
- [Using `EXPLAIN`](https://www.postgresql.org/docs/current/using-explain.html)
  — PostgreSQL 18, planos estimados e medidos, acesso em 18 novembro 2026.
- [Transaction locking and row versioning guide](https://learn.microsoft.com/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide)
  — SQL Server, locks, níveis e versionamento, acesso em 18 novembro 2026.
- [Indexes](https://learn.microsoft.com/sql/relational-databases/indexes/indexes)
  — SQL Server, estruturas rowstore e columnstore, acesso em 18 novembro 2026.
- [Data modeling in MongoDB](https://www.mongodb.com/docs/manual/data-modeling/)
  — modelo documental orientado ao acesso, acesso em 18 novembro 2026.
- [Transactions](https://www.mongodb.com/docs/manual/core/transactions/)
  — garantias e limites de transações multidocumento no MongoDB, acesso em 18 novembro 2026.
