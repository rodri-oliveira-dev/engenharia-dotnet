# C23 — Testes orientados a comportamento e risco

> **Nível:** Fundamental
>
> **Objetivo:** projetar uma estratégia de testes confiável, econômica e
> orientada ao comportamento que realmente reduz risco.
>
> **Pré-requisitos:** [C02 — Nulabilidade, falhas e
> exceções](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md) e
> [C13 — Dependências, abstrações e padrões com
> propósito](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md).
>
> **Última revisão:** 2026-11-18

Uma suíte não é boa porque contém muitos testes. Ela é boa quando detecta
regressões relevantes cedo, explica a falha e permanece barata o suficiente para
ser executada. Este capítulo conecta níveis de teste, risco, xUnit e dublês. C24
aprofunda infraestrutura e legado; C25 trata cobertura, E2E e carga.

## Contexto

Cada teste escolhe uma fronteira. Uma fronteira estreita costuma executar rápido
e localizar bem a falha, mas não comprova configuração, serialização, SQL ou
integrações. Uma fronteira ampla aumenta a fidelidade e também o número de causas
possíveis, o tempo e a instabilidade. A estratégia combina fronteiras, em vez de
procurar um único tipo de teste para tudo.

Testar custa implementação, infraestrutura, execução, investigação e manutenção.
Não testar transfere o custo para incidentes e validação manual. O critério é o
risco: probabilidade de falha multiplicada pelo impacto, modulado pela capacidade
do teste de revelar o defeito.

## Fundamentos da estratégia

Os nomes abaixo descrevem escopo e propósito; não são definidos pela ferramenta:

| Tipo | Fronteira e pergunta | Custo e limitações |
| --- | --- | --- |
| unitário | uma regra ou unidade de comportamento funciona sob entradas controladas? | muito rápido; não prova infraestrutura |
| componente | um serviço ou módulo funciona pela interface pública, com dependências externas substituídas? | bom equilíbrio; pode mascarar incompatibilidades |
| integração | aplicação e banco, broker, HTTP ou framework cooperam de verdade? | mais fiel; setup e diagnóstico custam mais |
| contrato | produtor e consumidor continuam compatíveis em forma e semântica? | detecta quebra de fronteira; não prova jornada completa |
| end-to-end (E2E) | um fluxo crítico atravessa o sistema implantado? | alta confiança sistêmica; lento e sujeito a flakiness |

Um teste HTTP com `TestServer`, aplicação e serialização é integração de
componente, não E2E de produção: ele não atravessa proxy, DNS, TLS, processo nem
banco externo. A classificação deve revelar o que ficou fora.

### Pirâmide, Test Trophy e risco

A **pirâmide de testes** recomenda base larga de testes unitários, menos testes de
integração e poucos E2E. Ela protege feedback rápido e baixo custo. Não prescreve
percentuais, nem justifica testar detalhes privados apenas para alargar a base.

O **Test Trophy** dá mais peso a testes de integração: em aplicações cujo risco
está na composição de componentes, testar a interface pública com dependências
realistas pode oferecer melhor confiança por teste. A alternativa não elimina
unitários nem E2E; muda o investimento conforme a natureza das falhas.

Uma carteira orientada ao risco pergunta:

1. quais fluxos causariam perda financeira, exposição, indisponibilidade ou
   irreversibilidade;
2. onde já ocorreram defeitos e onde a mudança é frequente;
3. qual é a menor fronteira capaz de revelar cada modo de falha;
4. quanto tempo o teste pode consumir em commit, pull request e pipeline tardio;
5. quão confiável é um resultado vermelho ou verde.

Regras de preço pedem muitos unitários. Mapeamento relacional e constraints pedem
integração real. Compatibilidade de mensagens pede contrato e integração com o
serializer. Um pagamento principal merece poucos E2E; combinações de desconto
continuam abaixo dessa camada.

## Testes unitários com xUnit

Uma unidade útil é um comportamento observável, não necessariamente uma classe.
O teste controla entradas, executa sem rede, disco ou relógio real e verifica
saídas, estado ou efeitos contratados. Isolamento significa ausência de estado
compartilhado acidental; não significa simular cada colaborador.

No xUnit, `[Fact]` representa um cenário fixo e `[Theory]` recebe casos por
`[InlineData]`, `MemberData` ou outra fonte. Organize a suíte pelo comportamento
do sistema sob teste. Classes aninhadas ou contextos separados podem tornar
pré-condições visíveis sem uma hierarquia de fixtures genéricas.

O padrão **Arrange, Act, Assert (AAA)** separa preparação, ação e observação. Ele
é uma ferramenta de leitura, não uma obrigação de comentários. Se o Arrange
domina o teste, a API ou o fixture talvez esteja complexo. Se há várias ações,
talvez existam vários comportamentos misturados.

Nomes como
`ClienteFidelidade_ComTotalElegivel_RecebeDezPorCento` registram condição e
resultado. Evite nomes que repetem somente o método, como `CalcularTest`, ou que
prometem internals. O nome deve ajudar a entender a regressão sem abrir o corpo.

### Exemplo de regra de domínio e teoria

O laboratório em `exemplos/c23-c25-testes/` contém os exemplos A e C. A regra é
pura e o teste verifica a decisão de negócio:

```csharp
[Fact]
public void ClienteFidelidade_ComTotalElegivel_RecebeDezPorCento()
{
    const decimal total = 250m; // Arrange

    var desconto = PoliticaDesconto.Calcular( // Act
        total, CategoriaCliente.Fidelidade);

    Assert.Equal(25m, desconto); // Assert
}
```

A teoria cobre partições e limites sem duplicar estrutura:

```csharp
[Theory]
[InlineData(199.99, CategoriaCliente.Fidelidade, 0)]
[InlineData(200, CategoriaCliente.Fidelidade, 20)]
[InlineData(500, CategoriaCliente.Regular, 0)]
public void Calcular_ParaCenariosDeElegibilidade_RetornaDescontoEsperado(
    double total, CategoriaCliente categoria, double esperado)
{
    var desconto = PoliticaDesconto.Calcular((decimal)total, categoria);
    Assert.Equal((decimal)esperado, desconto);
}
```

Os casos representam abaixo do limite, no limite e categoria não elegível. Uma
lista extensa de números sem partições explícitas apenas aumenta manutenção.

## Comportamento, implementação e fragilidade

Teste de **comportamento** usa a interface pública e falha quando o contrato muda.
Teste de **implementação** observa ordem interna, método privado ou quantidade de
chamadas sem relevância externa; ele falha em refatorações equivalentes. Algumas
interações são comportamento: cobrar uma vez, publicar um evento ou não enviar
e-mail após rejeição. Verificá-las é apropriado quando representam efeitos.

Um teste frágil depende de detalhes voláteis: texto completo de log, ordem de
coleção sem contrato, timestamp exato, `Task.Delay`, porta fixa ou chamadas
internas. Fragilidade reduz confiança; a equipe passa a reexecutar falhas em vez
de investigá-las. Remova a dependência ou torne o contrato explícito.

Determinismo exige controlar aleatoriedade, tempo, cultura, fuso e concorrência.
Injete `TimeProvider`, use sementes registradas quando aleatoriedade fizer parte
do cenário e espere sinais (`TaskCompletionSource`, canal ou evento), não um
tempo presumido. O exemplo G prova que o componente assíncrono não conclui antes
do manipulador, sem `sleep` arbitrário.

## Dublês de teste

**Test double** é o termo geral:

- **stub** fornece respostas programadas;
- **fake** implementa uma versão funcional simplificada, como armazenamento
  local, sem afirmar equivalência com a infraestrutura real;
- **spy** registra chamadas para inspeção posterior;
- **mock** define e verifica expectativas de interação;
- um objeto pode cumprir mais de um papel; o propósito importa mais que a classe.

NSubstitute foi usado no exemplo B porque torna explícitos `Received`, `Arg.Any<T>`
e `Arg.Is<T>`. Ele é uma dependência de teste; não entra no código de produção.

```csharp
await sender.Received(1).EnviarAsync(
    Arg.Any<string>(),
    "Cadastro confirmado",
    CancellationToken.None);
```

Esse teste passa mesmo quando a implementação envia ao destinatário errado. O
substituto recebeu uma chamada e `Arg.Any<string>()` aceita **qualquer** texto;
nenhuma asserção relaciona o argumento à entrada `cliente@exemplo.test`. O verde
prova apenas uma interação permissiva, não a intenção de negócio.

A correção valida o argumento significativo:

```csharp
await sender.Received(1).EnviarAsync(
    Arg.Is<string>(email => email == "cliente@exemplo.test"),
    "Cadastro confirmado",
    CancellationToken.None);
```

Valide quantidade e ordem somente quando importarem. Para uma operação idempotente,
`Received(1)` pode ser contrato. Para telemetria auxiliar, verificar chamadas
torna o teste ruidoso. Capture argumentos quando a asserção sobre o objeto inteiro
for mais clara que muitos matchers.

### Excesso de mocks e falsos positivos

Uma rede de mocks pode reproduzir a implementação em expectativas: o teste sabe
quem chama quem, mas não sabe se o resultado é correto. Configurações permissivas
retornam valores padrão e criam falsos positivos; configurações excessivamente
estritas criam falhas em refatorações. Ambos são acoplamento à implementação.

Prefira estado e saída observáveis. Use um fake pequeno quando múltiplos cenários
precisarem de comportamento coerente. Use mock nas fronteiras onde a interação é
o resultado. Complete com testes de integração para descobrir divergências entre
o dublê e a dependência real.

## Qualidade da suíte

Testes de qualidade são:

- determinísticos e independentes de ordem;
- isolados por dados, transação, schema ou instância;
- rápidos o suficiente para a etapa em que executam;
- claros sobre entrada, ação e assert relevante;
- diagnósticos: mensagens e nomes apontam a diferença observada;
- concorrentes apenas quando concorrência faz parte do risco;
- reproduzíveis localmente com o mesmo seed, imagem e configuração do CI.

Independência não proíbe fixture compartilhada cara, mas exige que cada teste não
dependa da mutação anterior. Paralelismo pode revelar estado global e também criar
contenção artificial; configure-o por coleção quando o recurso não suportar
isolamento. Nunca “corrija” corrida com `Thread.Sleep`.

Uma asserção relevante verifica resultado, invariantes e efeitos necessários.
`Assert.NotNull(resposta)` é fraco se o contrato exige status, total e moeda.
Snapshots grandes podem esconder mudança importante no ruído. Asserções demais
podem misturar comportamentos; asserções coerentes sobre uma única resposta são
aceitáveis.

## Dicas de engenharia

- Mantenha a suíte rápida no commit e promova testes mais caros em etapas claras.
- Registre duração e flakiness; quarentena deve ter owner e prazo, não ocultar
  regressão indefinidamente.
- Teste cenários negativos: entrada inválida, timeout, cancelamento, duplicidade e
  concorrência conforme o domínio.
- Revise testes junto com produção; duplicação nos testes também é custo, mas uma
  abstração de fixture não deve esconder o cenário.
- Se a falha não explica qual contrato quebrou, melhore nome, dados e assert.

## Armadilhas comuns

- confundir quantidade de testes com cobertura de risco;
- testar cada método privado e bloquear refatoração;
- usar `Arg.Any<T>` em todo argumento e não verificar dados importantes;
- simular banco, serializer e broker e chamar o resultado de integração;
- compartilhar dados mutáveis entre testes paralelos;
- tolerar teste intermitente porque “passa na segunda execução”;
- usar teste E2E para todas as combinações que um unitário cobriria melhor.

## Trade-offs

Unitários oferecem velocidade e diagnóstico, mas baixa fidelidade de integração.
Integrações aumentam confiança nas fronteiras e consomem recursos. Contratos dão
feedback focado sobre compatibilidade sem provar disponibilidade. E2E prova a
jornada implantada e tem maior custo e superfície de falha. A carteira saudável
usa a camada mais baixa capaz de revelar o risco e reserva camadas altas para
falhas que só existem nelas.

## Quando utilizar

Use unitários para regras, transições e partições de entrada; componente para uma
API de módulo; integração para infraestrutura e framework; contrato para evolução
independente; E2E para poucos fluxos críticos. Reavalie a distribuição quando
defeitos escaparem repetidamente por uma fronteira não exercitada.

## Quando evitar

Evite mock quando um objeto real simples é mais claro. Evite integração para
permutar lógica pura. Evite E2E como prova de cada regra. Não automatize um teste
sem oráculo estável ou cujo custo de manutenção exceda o risco; primeiro torne o
comportamento observável e a fronteira controlável.

## Relação com outros capítulos

- [C24](c24-integracao-real-testes-legado.md) aplica integração real,
  Testcontainers e caracterização de legado.
- [C25](c25-cobertura-e2e-carga.md) aprofunda cobertura, Mutation Testing, E2E e
  testes de carga.
- [C05](../linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md)
  fundamenta cancelamento e coordenação assíncrona.
- [C07](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md) define os
  contratos HTTP exercitados por testes de API.

## Referências

- [Testing in .NET](https://learn.microsoft.com/dotnet/core/testing/) — Microsoft,
  tipos e execução de testes, acesso em 18 novembro 2026.
- [Unit testing best practices with .NET](https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices)
  — Microsoft, isolamento, nomenclatura e legibilidade, acesso em 18 novembro
  2026.
- [Getting started with xUnit.net v2](https://xunit.net/docs/getting-started/v2/getting-started)
  — xUnit.net, fatos, teorias e execução, acesso em 18 novembro 2026.
- [Argument matchers](https://nsubstitute.github.io/help/argument-matchers/) —
  NSubstitute, `Arg.Any`, `Arg.Is` e limites de uso, acesso em 18 novembro 2026.
- [Mocks Aren't Stubs](https://martinfowler.com/articles/mocksArentStubs.html) —
  Martin Fowler, distinção entre verificação de estado e comportamento, acesso em
  18 novembro 2026.
