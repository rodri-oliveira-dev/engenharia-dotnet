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
condicional troca a referência somente se o conjunto observado continua ativo. Os
testes verificam a permanência da visão de um leitor, carga incompleta,
chaves duplicadas, geração obsoleta, referência de outra publicação e
competição entre dois publicadores que capturam a mesma base. Também cobrem
limites de geração, quantidade inválida, excesso de registros e publicação
intencional de um catálogo vazio no modelo local.

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
fictícios. A imagem é fixada pelo digest registrado na execução bem-sucedida
[45714467427](https://github.com/rodri-oliveira-dev/engenharia-dotnet/actions/runs/45714467427).
O script registra o digest utilizado. Atualizações da imagem devem ser
revisadas, incluindo correções de segurança, e validadas novamente pelo CI.
A execução local é opcional; o CI executa o script em um job próprio.
O container é removido ao final, inclusive em caso de falha após sua criação.

A troca condicional por Lua valida o ponteiro esperado, a geração crescente,
o espaço de chaves e a quantidade de campos. O ponteiro recebe exatamente
a chave que foi validada; não há um segundo identificador de destino.
O script verifica cargas incompletas, inexistentes, quantidades inválidas,
chaves de outro conjunto, formato e tipo de chave, preservação da visão
anterior e rejeição de gerações antigas. Cada rejeição verifica o ponteiro
e os dados ativos imediatamente. Dois clientes independentes disputam a
mesma base; somente um deve publicar. As asserções registram o cenário e,
em caso de falha, os valores esperado e obtido.

O experimento pressupõe um único Redis, versões não reutilizadas e dados
imutáveis depois da preparação. Não valida uma aplicação distribuída completa,
Redis Cluster, durabilidade, pressão de memória ou recuperação
após troca de primário. A contagem não valida a integridade dos registros,
e a extração consistente da origem permanece fora do exemplo. O formato de
oito dígitos restringe as gerações do exercício; não é um contrato universal.
Ao contrário do modelo .NET, a carga vazia é rejeitada deliberadamente no
Redis: um hash sem campos não permanece como chave. Sistemas que aceitem
catálogos vazios precisam de um manifesto de prontidão separado.

## Próximos experimentos

Compare a consulta direta, o cache local e o cache distribuído com uma
origem controlada. Use a metodologia de carga do
[C25](../../docs/qualidade-e-seguranca/c25-cobertura-e2e-carga.md) e
registre latência, taxa de acertos, carga na origem e idade dos dados.
Não há resultados de desempenho publicados neste laboratório.
