# Laboratório C36: publicação de cache por versões

O [capítulo C36](../../docs/aplicacoes-e-dados/c36-arquitetura-cache-consistencia.md)
discute estratégias de cache. Este exemplo verifica a regra de publicação
sem depender de um serviço externo nos testes .NET.

## Testes .NET

Use o SDK fixado no arquivo [global.json](../../global.json). Da raiz:

```bash
dotnet restore exemplos/EngenhariaDotnet.slnx
dotnet build exemplos/EngenhariaDotnet.slnx --configuration Release --no-restore
dotnet test exemplos/c36-cache/C36.Cache.Tests.csproj --configuration Release --no-build
```

O catálogo mantém uma referência para um conjunto imutável. A operação
condicional troca a referência se a geração observada continua ativa. Os
testes verificam a permanência da visão de um leitor, carga incompleta,
chaves duplicadas, geração obsoleta e competição entre duas publicações.

A contagem é um critério didático de validação, não uma prova de integridade.
Um sistema real deve validar identidade, conteúdo e versão da origem.
O modelo não implementa TTL, recuperação de processo, evicção, bloqueios
distribuídos nem retenção limitada; a memória antiga permanece enquanto
houver referências vivas.

## Experimento opcional com Redis real

Pré-requisitos: Bash, Docker em execução e acesso para obter a imagem
`redis:7.4-alpine`. Da raiz, execute:

```bash
bash exemplos/c36-cache/validar-redis.sh
```

O script usa um container isolado, sem portas publicadas, com dados
fictícios. A imagem usa uma etiqueta de versão menor; para experimentos
comparativos, registre também o digest obtido. O container é removido ao
final, inclusive em caso de falha após sua criação.

A troca condicional por Lua valida o ponteiro esperado e a quantidade de
campos. O script demonstra publicação após carga completa, preservação
de dados da geração anterior e rejeição de uma expectativa obsoleta.

O experimento pressupõe um único Redis, versões não reutilizadas e dados
imutáveis depois da preparação. Não valida concorrência real entre
processos, Redis Cluster, durabilidade, pressão de memória ou recuperação
após troca de primário. A identidade da versão e o vínculo entre a chave
dos dados e o valor do ponteiro dependem do publicador neste exercício.

## Próximos experimentos

Compare a consulta direta, o cache local e o cache distribuído com uma
origem controlada. Use a metodologia de carga do
[C25](../../docs/qualidade-e-seguranca/c25-cobertura-e2e-carga.md) e
registre latência, taxa de acertos, carga na origem e idade dos dados.
Não há resultados de desempenho publicados neste laboratório.
