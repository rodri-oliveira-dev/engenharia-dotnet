#!/usr/bin/env bash
set -euo pipefail

# Container descartavel, sem portas publicadas e sem rede externa.
container=$(docker run --rm -d --network none redis:7.4-alpine)
trap 'docker rm -f "$container" >/dev/null 2>&1 || true' EXIT

redis() { docker exec "$container" redis-cli --raw "$@"; }
for tentativa in {1..30}; do
  if [[ "$(redis PING 2>/dev/null || true)" == PONG ]]; then
    break
  fi
  sleep 1
done
[[ "$(redis PING)" == PONG ]]

# O ponteiro e os metadados de prontidao usam a mesma hash tag.
# Dados pequenos em hashes apenas para demonstracao.
redis HSET '{catalogo}:A' 1 antigo 2 antigo >/dev/null
redis SET '{catalogo}:ativo' A >/dev/null
redis HSET '{catalogo}:B' 1 novo >/dev/null

publicar='
if redis.call("GET", KEYS[1]) ~= ARGV[1] then return 0 end
if redis.call("HLEN", KEYS[2]) ~= tonumber(ARGV[3]) then return 0 end
redis.call("SET", KEYS[1], ARGV[2])
return 1
'

# Carga incompleta nao publica.
[[ "$(redis EVAL "$publicar" 2 '{catalogo}:ativo' '{catalogo}:B' A B 2)" == 0 ]]
[[ "$(redis GET '{catalogo}:ativo')" == A ]]
redis HSET '{catalogo}:B' 2 novo >/dev/null

# Leitor captura o ponteiro antes da troca.
leitor=$(redis GET '{catalogo}:ativo')
[[ "$(redis EVAL "$publicar" 2 '{catalogo}:ativo' '{catalogo}:B' A B 2)" == 1 ]]
[[ "$(redis GET '{catalogo}:ativo')" == B ]]
[[ "$(redis HGET "{catalogo}:$leitor" 2)" == antigo ]]
[[ "$(redis HGET '{catalogo}:B' 2)" == novo ]]

# Publicador obsoleto: a expectativa A ja nao e valida.
redis HSET '{catalogo}:C' 1 outro 2 outro >/dev/null
[[ "$(redis EVAL "$publicar" 2 '{catalogo}:ativo' '{catalogo}:C' A C 2)" == 0 ]]
[[ "$(redis GET '{catalogo}:ativo')" == B ]]
echo "Publicacao condicional validada."
