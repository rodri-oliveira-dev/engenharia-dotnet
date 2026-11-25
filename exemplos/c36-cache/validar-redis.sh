#!/usr/bin/env bash
set -euo pipefail

# Container descartavel, sem portas publicadas e sem rede externa.
imagem=redis:7.4-alpine
docker pull "$imagem"
docker image inspect --format '{{join .RepoDigests ", "}}' "$imagem"
container=$(docker run --pull never --rm -d --network none "$imagem")
trap 'docker rm -f "$container" >/dev/null 2>&1 || true' EXIT

redis() { docker exec "$container" redis-cli -e --raw "$@"; }
for tentativa in {1..30}; do
  if [[ "$(redis PING 2>/dev/null || true)" == PONG ]]; then
    break
  fi
  sleep 1
done
[[ "$(redis PING)" == PONG ]]

# O ponteiro guarda a chave completa, nao um identificador independente.
# As geracoes de oito digitos permitem comparacao lexical sem perda numerica.
ponteiro='{catalogo}:ativo'
a='{catalogo}:00000001'
b='{catalogo}:00000002'
c='{catalogo}:00000003'
redis HSET "$a" 1 antigo 2 antigo >/dev/null
redis SET "$ponteiro" "$a" >/dev/null
redis HSET "$b" 1 novo >/dev/null

publicar='
local formato = "^%{catalogo%}:%d%d%d%d%d%d%d%d$"
if KEYS[1] ~= "{catalogo}:ativo" then return 0 end
if not string.match(ARGV[1], formato) or not string.match(KEYS[2], formato) then
    return 0
end
if KEYS[2] <= ARGV[1] then return 0 end
if redis.call("GET", KEYS[1]) ~= ARGV[1] then return 0 end
local quantidade = tonumber(ARGV[2])
if not quantidade or quantidade < 1 or quantidade ~= math.floor(quantidade) then
    return 0
end
if redis.call("HLEN", KEYS[2]) ~= quantidade then return 0 end
redis.call("SET", KEYS[1], KEYS[2])
return 1
'

trocar() { redis EVAL "$publicar" 2 "$ponteiro" "$2" "$1" "$3"; }

# Carga incompleta ou inexistente nao publica.
[[ "$(trocar "$a" "$b" 2)" == 0 ]]
[[ "$(trocar "$a" "$c" 2)" == 0 ]]
[[ "$(trocar "$a" "$c" 0)" == 0 ]]
[[ "$(redis GET "$ponteiro")" == "$a" ]]
redis HSET "$b" 2 novo >/dev/null

# Uma chave de outro conjunto, mesmo completa, nao pode ser publicada.
redis HSET '{outro}:00000002' 1 outro 2 outro >/dev/null
[[ "$(trocar "$a" '{outro}:00000002' 2)" == 0 ]]
[[ "$(redis GET "$ponteiro")" == "$a" ]]

# Leitor captura a chave antes da troca.
leitor=$(redis GET "$ponteiro")
[[ "$(trocar "$a" "$b" 2)" == 1 ]]
[[ "$(redis GET "$ponteiro")" == "$b" ]]
[[ "$(redis HGET "$leitor" 2)" == antigo ]]
[[ "$(redis HGET "$b" 2)" == novo ]]

# Publicador obsoleto e tentativa de reutilizar uma geracao anterior.
redis HSET "$c" 1 outro 2 outro >/dev/null
[[ "$(trocar "$a" "$c" 2)" == 0 ]]
[[ "$(trocar "$b" "$a" 2)" == 0 ]]
[[ "$(redis GET "$ponteiro")" == "$b" ]]
[[ "$(trocar "$b" "$c" 2)" == 1 ]]
[[ "$(redis GET "$ponteiro")" == "$c" ]]
echo "Publicacao condicional validada."
