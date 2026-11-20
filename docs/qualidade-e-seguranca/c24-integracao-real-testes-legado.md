# C24 — Integração real e testes incrementais de legado

> **Nível:** Intermediário
>
> **Objetivo:** validar fronteiras reais de infraestrutura e proteger sistemas
> legados enquanto sua testabilidade evolui incrementalmente.
>
> **Pré-requisitos:** [C08 — Consultas eficientes com EF
> Core](../aplicacoes-e-dados/c08-consultas-ef-core.md) e
> [C23 — Testes orientados a comportamento e risco](c23-testes-comportamento-risco.md).
>
> **Última revisão:** 2026-11-18

Testes de integração encontram defeitos que dublês não representam: SQL inválido,
collation, constraint, serialização, configuração do pipeline, protocolo e ciclo
de vida. Este capítulo mostra como obter fidelidade sem transformar a suíte em um
ambiente compartilhado e imprevisível, e como usar caracterização para evoluir
código legado sem reescrita total.

## Contexto

Uma unidade pode estar correta e a aplicação falhar porque o banco arredonda de
outro modo, a API muda o JSON, o broker redelivera, uma credencial expirou ou o
provider traduz uma consulta de forma diferente. O teste precisa atravessar a
fronteira onde o risco existe.

“Real” não significa usar produção. Significa executar o componente relevante —
engine relacional, servidor HTTP, serializer ou broker — em ambiente controlado,
isolado e descartável. Serviços de terceiros sem sandbox confiável permanecem
atrás de um adaptador; um servidor stub local valida nosso cliente e poucos testes
de contrato podem rodar contra a sandbox fora do caminho de commit.

## Fundamentos de integração

Defina explicitamente o sistema sob teste, as dependências reais e as
substituídas. Exemplos:

- repositório + driver + PostgreSQL valida SQL, tipos, constraints e transação;
- API ASP.NET Core + `TestServer` valida roteamento, binding, filtros,
  serialização e contrato HTTP, mas não proxy/TLS;
- consumidor + broker real valida exchange/tópico, ack, redelivery e headers;
- cliente HTTP + servidor stub controlado valida URL, método, headers, timeout e
  parsing sem depender do terceiro;
- teste de contrato valida compatibilidade de payload entre produtor e consumidor
  sem precisar de toda a topologia.

APIs externas reais trazem rate limit, custo, indisponibilidade e dados difíceis
de limpar. Reserve-as para smoke tests autorizados e nunca use credenciais de
produção. Brokers e bancos normalmente podem ser efêmeros em container.

## Ambiente, dados e isolamento

Reprodutibilidade exige versão fixada, schema versionado e configuração conhecida.
Uma tag `latest`, um banco compartilhado ou dados “que já estão lá” tornam o
resultado dependente do tempo.

Escolha uma estratégia de isolamento coerente com o mecanismo:

| Estratégia | Benefício | Limite |
| --- | --- | --- |
| container por teste | isolamento máximo | startup e recursos altos |
| container por classe/coleção | amortiza startup | exige limpar ou particionar dados |
| database/schema por teste | paralelismo com engine compartilhada | criação e permissões |
| transação com rollback | rápida | não cobre múltiplas conexões/commit real |
| identificador único por cenário | simples em APIs/brokers | limpeza e consultas precisam respeitar o escopo |

Dados de teste devem ser mínimos e expressar o cenário. Builders ajudam a mostrar
apenas diferenças relevantes; dumps de produção aumentam risco de privacidade e
dificultam o diagnóstico. Se for necessário realismo estatístico, gere dados
sintéticos versionados e registre a seed.

Limpeza pertence ao lifecycle: finalize consumidores, descarte conexões, remova
schemas/filas e permita que o resource reaper do Testcontainers remova recursos.
Uma falha de teste não pode impedir o `finally` ou `DisposeAsync`.

## Persistência real e providers InMemory

Um fake em memória não implementa SQL, transações, constraints, collation, tipos,
locks nem tradução do provider. O provider EF Core InMemory é útil para alguns
testes do modelo de aplicação, mas não é um banco relacional e não comprova a
consulta que irá para produção. Até SQLite difere de PostgreSQL ou SQL Server em
tipos, funções e concorrência.

Use uma gradação consciente:

1. regra pura sem provider;
2. SQLite real quando a semântica exercitada é comum e o feedback local importa;
3. o mesmo engine e provider de produção para consultas, migrations, constraints,
   concorrência e transações críticas.

O exemplo D abre um SQLite relacional real em memória, cria uma tabela com chave
e `CHECK`, persiste e relê um pedido. Ele valida ADO.NET, parâmetros e mapping, mas
não afirma equivalência com PostgreSQL. O exemplo E repete o princípio no engine
de produção e prova a constraint específica.

<!-- snippet-csharp: id=c24-01; categoria=contexto -->
```csharp
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
// A conexão permanece aberta para preservar esse database temporário.
var repositorio = new RepositorioPedidos(connection);
await repositorio.AdicionarAsync(pedido, CancellationToken.None);
Assert.Equal(pedido, await repositorio.ObterAsync(
    pedido.Id, CancellationToken.None));
```

## Testcontainers

Testcontainers cria containers programaticamente para testes. A imagem fixa o
serviço; o builder configura credenciais descartáveis, portas aleatórias e wait
strategy; o fixture inicia antes do teste e descarta ao final. Isso reduz drift
entre máquina e CI, mas ainda requer runtime compatível com Docker.

O laboratório usa `Testcontainers.PostgreSql` porque o módulo encapsula conexão e
prontidão do PostgreSQL. A imagem está fixada em `postgres:17.6-alpine`; atualizar
a versão é uma mudança deliberada, validada no CI.

<!-- snippet-csharp: id=c24-02; categoria=contexto -->
```csharp
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } =
        new PostgreSqlBuilder("postgres:17.6-alpine")
            .WithDatabase("engenharia_testes")
            .WithUsername("testes")
            .WithPassword("senha-local-descartavel")
            .Build();

    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}
```

`IClassFixture` compartilha o container dentro da classe. Compartilhar reduz
tempo, mas os testes devem criar schema/dados exclusivos. Para execução paralela,
evite nomes globais, portas fixas e tabelas recriadas por classes concorrentes;
use databases/schemas únicos ou coleções não paralelas justificadas.

### Lifecycle, recursos e CI

Container consome CPU, memória, disco, rede e tempo de pull/startup. Mantenha a
imagem pequena, faça cache do registry quando permitido e limite paralelismo pela
capacidade do runner. Falta de recursos pode parecer flakiness da aplicação.
Colete logs do container em falhas e defina timeout de inicialização explícito.

No CI, o agente precisa acessar um daemon Docker e baixar a imagem. O teste está
marcado com `[Trait("Category", "Docker")]`: a etapa rápida exclui a categoria e
uma etapa de integração com Docker a inclui. Não faça o teste retornar sucesso
silencioso quando Docker estiver ausente; ausência do requisito deve impedir essa
etapa ou deixá-la explicitamente não executada.

Containers são desnecessários quando um objeto real local e determinístico cobre
o risco (regra pura, parser, `TimeProvider`, serializer), quando um servidor em
processo é a própria implementação suportada, ou quando a dependência externa não
pode ser legal/técnicamente containerizada. Não use container por estética.

## Teste de API ASP.NET Core

O exemplo F usa `WebApplicationFactory<Program>`. A requisição atravessa host de
teste, roteamento, binding do enum, endpoint e serialização JSON:

<!-- snippet-csharp: id=c24-03; categoria=contexto -->
```csharp
var response = await client.GetAsync(
    "/descontos/250?categoria=Fidelidade");

Assert.Equal(HttpStatusCode.OK, response.StatusCode);
var body = await response.Content.ReadFromJsonAsync<RespostaDesconto>();
Assert.Equal(new RespostaDesconto(250m, 25m, 225m), body);
```

Verifique status, headers e corpo relevantes. Substitua autenticação e serviços
externos de forma explícita no factory; não remova silenciosamente middlewares que
fazem parte do contrato. Para banco, injete uma instância isolada por fixture.

`TestServer` não abre socket real. Testes de deploy ainda precisam validar
configuração, health check, TLS/proxy e dependências no ambiente implantado.

## Brokers, HTTP e serviços externos

Para broker, cubra publicação, binding, headers, ack/nack, retry, dead-letter e
duplicidade. Aguarde por sinal com timeout diagnóstico, não `sleep`. Use nomes de
fila/tópico únicos e encerre consumidores antes da limpeza.

Para cliente HTTP, um servidor stub local é preferível a mockar `HttpClient`: ele
exercita serialização e transporte HTTP controlado. Valide request recebida e
responda com sucesso, erro, payload inválido, latência controlada e desconexão.
Contrato não deve duplicar a implementação do terceiro; derive casos de uma
especificação versionada.

Sandbox externa serve para poucos testes de aceitação do adaptador. Limite taxa,
use conta dedicada, dados sintéticos e limpeza idempotente. Uma indisponibilidade
externa deve ser distinguível de regressão local no relatório.

## Testes em sistemas legados

Código legado frequentemente combina regra, relógio, estático, banco e UI. A
reescrita total remove simultaneamente comportamento conhecido e observabilidade;
o risco migra para um projeto longo sem oráculo.

**Testes de caracterização** registram o comportamento atual, inclusive
peculiaridades, antes da mudança. Eles não afirmam que esse comportamento é o
ideal. Dê nomes que expressem “histórico”, documente anomalias e só altere a
expectativa com decisão consciente.

O exemplo H fixa dois detalhes de `CalculadoraFreteLegada`: comparação sensível a
maiúsculas e arredondamento para longe de zero. Isso cria uma rede de proteção
para refatorar sem mudar resultados acidentalmente.

<!-- snippet-csharp: id=c24-04; categoria=contexto -->
```csharp
[Theory]
[InlineData(0, "SUL", 7.01)]
[InlineData(2, "SUL", 9.31)]
[InlineData(2, "sul", 9.71)]
public void Calcular_CaracterizaComportamentoExistente(
    double peso, string regiao, double resultadoHistorico)
{
    var resultado = new CalculadoraFreteLegada()
        .Calcular((decimal)peso, regiao);
    Assert.Equal((decimal)resultadoHistorico, resultado);
}
```

### Refatoração incremental e seams

Um **seam** é um ponto em que o comportamento pode ser substituído sem editar o
núcleo exercitado. Interfaces em adaptadores, método virtual temporário, função
injetada, wrapper de API estática e `TimeProvider` são opções. Crie o menor ponto
de isolamento necessário; não redesenhe todo o sistema antes do primeiro teste.

Sequência segura:

1. escolha um fluxo valioso e observável;
2. capture entradas e saídas representativas, removendo dados sensíveis;
3. escreva caracterização na fronteira disponível;
4. extraia um seam preservando comportamento;
5. mova uma regra para uma unidade testável;
6. compare antes/depois e entregue uma alteração pequena;
7. repita, adicionando cenários negativos encontrados em produção.

Para código difícil, comece acima: endpoint, comando CLI ou banco. Golden master
pode comparar saída grande, desde que normalize timestamps/IDs e permita revisar
diferenças. Approval/snapshot sem revisão vira um botão de “aceitar tudo”. Para
concorrência, caracterize invariantes e eventos, não uma ordem de scheduling.

## Como executar o laboratório

O SDK é o .NET 10 fixado em `global.json`. Os exemplos A–H estão em
`exemplos/c23-c25-testes/`. NSubstitute existe somente para demonstrar matchers e
interações; SQLite/Npgsql exercitam bancos; `WebApplicationFactory` hospeda a API;
Testcontainers gerencia PostgreSQL. Nenhum serviço externo é chamado.

Restauração, build e testes sem Docker:

```bash
dotnet restore exemplos/EngenhariaDotnet.slnx
dotnet build exemplos/EngenhariaDotnet.slnx --no-restore
dotnet test exemplos/c23-c25-testes/C23C25.Testes.Tests.csproj \
  --no-build --filter "Category!=Docker"
```

Teste E com Docker:

```bash
docker version
dotnet test exemplos/c23-c25-testes/C23C25.Testes.Tests.csproj \
  --no-build --filter "Category=Docker"
```

O daemon deve estar ativo e permitir baixar `postgres:17.6-alpine`. Portas são
aleatórias; credenciais são locais e descartáveis; o container é removido pelo
fixture. Em CI, execute as categorias em etapas distintas. O teste SQLite não
substitui o teste PostgreSQL.

## Dicas de engenharia

- Fixe versões de imagem e pacote; atualize com revisão e execução.
- Registre qual recurso cada teste usa e qual semântica ele comprova.
- Mantenha migrations e schema do teste no mesmo fluxo da aplicação quando o
  objetivo for validá-los.
- Colete logs, query/constraint e IDs do cenário ao falhar.
- Teste cancelamento, timeout e falha parcial sem depender de serviço público.
- Meça duração por categoria e limite paralelismo pela capacidade real.

## Armadilhas comuns

- chamar provider InMemory de prova relacional;
- usar uma base compartilhada e depender da ordem da suíte;
- fixar porta ou tag `latest`;
- mockar o próprio SQL/HTTP e confirmar apenas o mock;
- limpar dados somente no caminho de sucesso;
- ocultar ausência de Docker retornando sucesso;
- congelar bug de legado sem documentar se ele deve ser preservado ou corrigido;
- introduzir muitas interfaces sem um modo de falha testável.

## Trade-offs

Fidelidade custa startup, recursos e diagnóstico. Container por teste maximiza
isolamento; fixture compartilhada reduz custo com disciplina de dados. SQLite dá
feedback rápido, mas não prova o engine de produção. Caracterização acelera
mudança segura, mas pode perpetuar comportamento indesejado se não houver uma
decisão posterior. Seams reduzem acoplamento testável e podem adicionar indireção.

## Quando utilizar

Use integração real para SQL e migrations, serialização, pipeline HTTP, broker,
cache e adapters cujo risco depende do protocolo. Use Testcontainers quando a
dependência tem imagem confiável e a reprodução vale o custo. Use caracterização
antes de modificar código importante sem especificação executável.

## Quando evitar

Evite infraestrutura para regra pura. Evite terceiros reais no pipeline rápido.
Evite container quando o mecanismo não faz parte da afirmação. Não crie uma
abstração apenas para satisfazer um mock; primeiro identifique o comportamento e
a fronteira que precisam de controle.

## Relação com outros capítulos

- [C23](c23-testes-comportamento-risco.md) define a carteira de testes e dublês.
- [C25](c25-cobertura-e2e-carga.md) usa essas fronteiras em E2E e métricas.
- [C09](../aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) detalha
  concorrência e transações a validar no banco real.
- [C15](../sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md) define
  modos de falha de broker que os testes de integração devem cobrir.

## Referências

- [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/testing/integration-testing)
  — Microsoft, `TestServer` e `WebApplicationFactory`, acesso em 18 novembro 2026.
- [Choosing a testing strategy](https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy)
  — EF Core, banco real e limites de dublês, acesso em 18 novembro 2026.
- [Testing with xUnit.net](https://dotnet.testcontainers.org/test_frameworks/xunit_net/)
  — Testcontainers for .NET, lifecycle isolado e compartilhado, acesso em 18
  novembro 2026.
- [PostgreSQL module](https://dotnet.testcontainers.org/modules/postgres/) —
  Testcontainers for .NET, builder e conexão, acesso em 18 novembro 2026.
- [Working Effectively with Legacy Code](https://www.pearson.com/en-us/subject-catalog/p/Feathers-FEATHERS-WORK-EFFECT-LEG-CODE-p-1/P200000008984)
  — Michael Feathers, seams e caracterização, acesso em 18 novembro 2026.
