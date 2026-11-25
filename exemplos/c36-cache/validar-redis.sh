#!/usr/bin/env bash
set -euo pipefail

# Container descartavel, sem portas publicadas e sem rede externa.
imagem=redis:7.4-alpine@sha256:7f7d30b7fdd09467bac58198a1f166dd69789d348d4fe859cccb20e0cf287fbe
docker pull "$imagem"
docker image inspect --format '{{join .RepoDigests ", "}}' "$imagem"
container=$(docker run --pull never --rm -d --network none "$imagem")
temporario=$(mktemp -d)
limpar() {
  docker rm -f "$container" >/dev/null 2>&1 || true
  rm -rf -- "$temporario"
}
trap limpar EXIT

redis() { docker exec "$container" redis-cli -e --raw "$@"; }
falhar() {
  printf 'FALHA [%s]: esperado <%s>; obtido <%s>\n' "$1" "$2" "$3" >&2
  exit 1
}
igual() {
  local nome=$1 esperado=$2 obtido=$3
  [[ "$obtido" == "$esperado" ]] || falhar "$nome" "$esperado" "$obtido"
  printf 'OK [%s]\n' "$nome"
}
verificar() {
  local nome=$1 esperado=$2 obtido
  shift 2
  if ! obtido=$("$@" 2>&1); then
    falhar "$nome" "$esperado" "$obtido"
  fi
  igual "$nome" "$esperado" "$obtido"
}
for tentativa in {1..30}; do
  if [[ "$(redis PING 2>/dev/null || true)" == PONG ]]; then
    break
  fi
  sleep 1
done
verificar "Redis pronto" PONG redis PING

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
if redis.call("TYPE", KEYS[1]).ok ~= "string" then return 0 end
if redis.call("GET", KEYS[1]) ~= ARGV[1] then return 0 end
local quantidade = tonumber(ARGV[2])
if not quantidade or quantidade < 1 or quantidade ~= math.floor(quantidade) then
    return 0
end
if redis.call("TYPE", KEYS[2]).ok ~= "hash" then return 0 end
if redis.call("HLEN", KEYS[2]) ~= quantidade then return 0 end
redis.call("SET", KEYS[1], KEYS[2])
return 1
'

trocar() { redis EVAL "$publicar" 2 "$ponteiro" "$2" "$1" "$3"; }

estado() {
  local nome=$1 chave=$2 valor=$3
  verificar "$nome: ponteiro" "$chave" redis GET "$ponteiro"
  verificar "$nome: quantidade" 2 redis HLEN "$chave"
  verificar "$nome: campo 1" "$valor" redis HGET "$chave" 1
  verificar "$nome: campo 2" "$valor" redis HGET "$chave" 2
}
rejeitar() {
  local nome=$1 base=$2 candidata=$3 quantidade=$4 ativa=$5 valor=$6
  verificar "$nome: retorno" 0 trocar "$base" "$candidata" "$quantidade"
  estado "$nome" "$ativa" "$valor"
}

rejeitar "Carga incompleta" "$a" "$b" 2 "$a" antigo
rejeitar "Carga inexistente" "$a" "$c" 2 "$a" antigo
rejeitar "Quantidade zero" "$a" "$c" 0 "$a" antigo
redis HSET "$b" 2 novo >/dev/null
rejeitar "Quantidade superior" "$a" "$b" 1 "$a" antigo
rejeitar "Quantidade negativa" "$a" "$b" -1 "$a" antigo
rejeitar "Quantidade fracionaria" "$a" "$b" 1.5 "$a" antigo
rejeitar "Quantidade invalida" "$a" "$b" texto "$a" antigo
rejeitar "Geracao igual" "$a" "$a" 2 "$a" antigo

redis HSET '{outro}:00000002' 1 outro 2 outro >/dev/null
rejeitar "Outro conjunto" "$a" '{outro}:00000002' 2 "$a" antigo
redis HSET '{catalogo}:0007' 1 outro 2 outro >/dev/null
rejeitar "Formato de candidata invalido" "$a" '{catalogo}:0007' 2 "$a" antigo
rejeitar "Formato de base invalido" '{catalogo}:0007' "$b" 2 "$a" antigo
redis SET '{catalogo}:00000006' texto >/dev/null
rejeitar "Tipo de candidata invalido" "$a" '{catalogo}:00000006' 2 "$a" antigo

leitor=$(redis GET "$ponteiro")
verificar "Publicacao de B" 1 trocar "$a" "$b" 2
estado "Versao B ativa" "$b" novo
verificar "Leitor antigo: campo 1" antigo redis HGET "$leitor" 1
verificar "Leitor antigo: campo 2" antigo redis HGET "$leitor" 2

redis HSET "$c" 1 outro 2 outro >/dev/null
rejeitar "Publicador obsoleto" "$a" "$c" 2 "$b" novo
rejeitar "Geracao anterior" "$b" "$a" 2 "$b" novo
verificar "Publicacao de C" 1 trocar "$b" "$c" 2
estado "Versao C ativa" "$c" outro

# Dois clientes independentes competem pela mesma referencia observada.
d='{catalogo}:00000004'
e='{catalogo}:00000005'
redis HSET "$d" 1 quarto 2 quarto >/dev/null
redis HSET "$e" 1 quinto 2 quinto >/dev/null
trocar "$c" "$d" 2 >"$temporario/d" 2>&1 &
pid_d=$!
trocar "$c" "$e" 2 >"$temporario/e" 2>&1 &
pid_e=$!
status_d=0
status_e=0
wait "$pid_d" || status_d=$?
wait "$pid_e" || status_e=$?
igual "Cliente D executado" 0 "$status_d"
igual "Cliente E executado" 0 "$status_e"
resultado_d=$(cat "$temporario/d")
resultado_e=$(cat "$temporario/e")
case "$resultado_d:$resultado_e" in
  1:0)
    vencedora=$d; valor=quarto; perdedora=$e; valor_perdedora=quinto ;;
  0:1)
    vencedora=$e; valor=quinto; perdedora=$d; valor_perdedora=quarto ;;
  *) falhar "Publicacao concorrente" "1:0 ou 0:1" "$resultado_d:$resultado_e" ;;
esac
printf 'OK [Publicacao concorrente: D=%s E=%s]\n' "$resultado_d" "$resultado_e"
estado "Versao vencedora" "$vencedora" "$valor"
verificar "Versao anterior preservada: campo 1" outro redis HGET "$c" 1
verificar "Versao anterior preservada: campo 2" outro redis HGET "$c" 2
verificar "Candidata rejeitada preservada: campo 1" "$valor_perdedora" redis HGET "$perdedora" 1
verificar "Candidata rejeitada preservada: campo 2" "$valor_perdedora" redis HGET "$perdedora" 2
rejeitar "Nova tentativa com base obsoleta" "$c" "$perdedora" 2 "$vencedora" "$valor"

echo "Publicacao condicional validada."
