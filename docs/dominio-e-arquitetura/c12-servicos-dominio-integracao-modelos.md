# C12 — Serviços de domínio e integração entre modelos

> **Nível:** Intermediário
>
> **Objetivo:** separar orquestração de casos de uso, regras de negócio e
> adaptação de integrações sem criar services que esvaziam o modelo.
>
> **Pré-requisitos:** [C11 — Entidades, value objects e agregados](c11-entidades-value-objects-agregados.md).
>
> **Última revisão:** 2026-11-18

Nem toda operação cabe em uma entidade. Um caso de uso precisa localizar um
agregado, controlar a unidade de trabalho, chamar integrações e traduzir o
resultado para sua fronteira. Uma regra também pode depender de vários conceitos
sem pertencer naturalmente a um deles. Chamar ambos de “service” apaga uma
distinção importante: coordenação técnica e política de negócio mudam por motivos
diferentes.

Este capítulo separa Application Services, Domain Services e infraestrutura e
usa uma Anti-Corruption Layer (ACL) para proteger Pedidos de um cadastro legado.
O exemplo permanece no mesmo processo e não introduz microsserviços ou
mensageria.

## Três responsabilidades, três razões para mudar

| Componente | Responsabilidade | Pode depender de | Evite colocar |
| --- | --- | --- | --- |
| Application Service | executar um caso de uso e delimitar sua unidade de trabalho | domínio e portas para persistência, relógio, identidade e integrações | cálculo ou decisão que especialistas reconhecem como regra de negócio |
| Domain Service | representar uma política do domínio que não pertence naturalmente a uma entidade/value object | tipos e portas de domínio; idealmente sem infraestrutura | transação, serialização, HTTP, logging e fluxo de interface |
| Infraestrutura | realizar I/O e adaptar tecnologia | SDKs, banco, rede e contratos técnicos, implementando portas internas | decidir invariantes do agregado |

Essas são responsabilidades lógicas. Uma aplicação pequena pode mantê-las no
mesmo assembly; criar um projeto por linha da tabela não melhora a separação por
si só. A direção das dependências e o vocabulário são mais importantes que o
número de pastas.

## Application Service: orquestração do caso de uso

Um **Application Service** define o que a aplicação oferece. Ele recebe dados já
traduzidos da fronteira, carrega o agregado, invoca comportamento, confirma a
persistência e dispara efeitos na ordem definida. Não deve recalcular regras que
o agregado pode proteger.

O laboratório implementa `ServicoDePedidos`:

<!-- snippet-csharp: id=c12-01; categoria=contexto -->
```csharp
public async Task ConfirmarAsync(Guid pedidoId, CancellationToken cancellationToken)
{
    var pedido = await ObterPedidoAsync(pedidoId, cancellationToken);
    pedido.Confirmar(relogio.Agora);

    await repositorio.SalvarAlteracoesAsync(cancellationToken);
    foreach (var evento in pedido.EventosDeDominio.ToArray())
        await publicador.PublicarAsync(evento, cancellationToken);
    pedido.LimparEventosDeDominio();
}
```

O método coordena; `Pedido.Confirmar` decide se a transição é válida. O relógio é
uma dependência porque o instante precisa ser determinístico em testes. O
repositório e o publicador são portas pequenas exigidas pelo caso de uso, não
wrappers genéricos de toda a infraestrutura.

Autenticação, autorização do caso de uso, idempotência da requisição, transação,
telemetria e cancelamento normalmente pertencem à aplicação ou às suas
fronteiras. A regra “desconto máximo é 30%” pertence ao domínio. “Este usuário
pode operar este pedido” pode ser regra de aplicação ou domínio conforme a
linguagem: autorização técnica por papel é uma preocupação da aplicação; uma
alçada comercial que determina quem aprova desconto é política do negócio.

### O que um Application Service não deve fazer

Este código duplicaria e fragilizaria o agregado:

<!-- snippet-csharp: id=c12-02; categoria=fragmento -->
```csharp
// Evite: outro caso de uso pode esquecer uma dessas condições.
if (pedido.Estado == EstadoPedido.Rascunho && pedido.Itens.Count > 0)
{
    pedido.Estado = EstadoPedido.Confirmado;
    await repositorio.SalvarAlteracoesAsync(cancellationToken);
}
```

Além de exigir setter público, ele transforma a aplicação na proprietária de
uma invariante. A forma preferível é uma operação atômica no objeto que controla
o estado. Isso reduz a quantidade de caminhos que precisam lembrar da regra.

Um Application Service também não precisa ser uma classe “Service” grande. Um
command handler por caso de uso é outra forma da mesma responsabilidade. A
escolha entre classe agrupada e handlers menores depende de coesão, pipeline e
volume; não muda onde ficam as regras.

## Domain Service: política sem dono natural

Um **Domain Service** nomeia comportamento importante do negócio que envolve
conceitos distintos e não pertence naturalmente a uma entidade ou value object.
Exemplos possíveis são uma política de câmbio que compara ofertas válidas ou um
cálculo de rota que combina restrições de múltiplos trechos. Seu contrato e
resultado usam a linguagem ubíqua.

Antes de criar um, tente responder:

1. a regra protege o estado de um agregado específico? Então deve entrar pela
   raiz;
2. a operação é definida pelo valor e não usa identidade? Pode ser método de um
   value object;
3. o código apenas busca, salva, envia ou traduz? É aplicação ou infraestrutura;
4. resta uma política de negócio coesa, sem dono natural? Um Domain Service pode
   ser apropriado.

O exemplo de Pedidos **não** cria um Domain Service. Somar itens, aplicar desconto
e confirmar pertencem ao agregado; persistir e publicar pertencem ao Application
Service. Inventar `PedidoDomainService` para chamar esses métodos adicionaria
indireção sem representar novo conceito.

Quando uma política precisa de informação externa, seu contrato pode depender
de uma porta expressa no domínio, como `ICotacaoCambial`, e a infraestrutura a
implementa. Ainda assim, evite tornar a regra uma sequência de I/O difícil de
testar. A aplicação pode obter um snapshot e entregar valores ao serviço de
domínio. Isso explicita o instante da decisão e mantém a política determinística.

### Estado e dependências

Domain Services costumam ser stateless: recebem entidades e valores e retornam
uma decisão ou resultado. Estado durável pertence a entidades; caches, conexões
e clientes pertencem à infraestrutura. Stateless não significa classe estática:
uma interface pode representar uma política substituível, mas só é útil quando
há fronteira ou variação real. Função ou método concreto é suficiente para uma
regra estável.

## Limites entre domínio, aplicação e infraestrutura

Uma direção simples é:

```text
API/UI -> Aplicação -> Domínio
              ^          ^
              |          |
        Infraestrutura implementa portas
```

O domínio não conhece controller, `DbContext`, HTTP, broker ou formato legado.
A aplicação conhece as interfaces necessárias para o caso de uso e compõe a
sequência. A infraestrutura implementa I/O e tradução. O composition root liga
implementações a interfaces, como explicado no
[C06](../aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md).

Uma dependência abstrata não se torna automaticamente de domínio. Uma interface
`IEmailSender` continua representando efeito de infraestrutura, mesmo colocada
na pasta Domain. Declare portas perto do consumidor e dê a elas contratos na
linguagem daquele consumidor.

Transações locais envolvem a alteração persistida. Chamada HTTP ou e-mail não é
desfeita por rollback. No laboratório, eventos internos são publicados após
`SalvarAlteracoesAsync`; uma falha no save impede a publicação e é coberta por
teste. A queda depois do commit ainda pode perder o efeito. Outbox resolve essa
janela quando entrega confiável é requisito, como aprofundado no
[C16](../sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md).

## Anti-Corruption Layer: tradução entre linguagens

Uma **Anti-Corruption Layer** protege o modelo local de outro modelo com
semântica diferente. Ela pode ser um adapter no mesmo processo; não precisa ser
um serviço de rede. Seu trabalho é traduzir estruturas, unidades, estados,
identidades, erros e pressupostos.

No cadastro legado do laboratório, os flags `Inativo` e `Bloqueado` precisam ser
interpretados para formar o conceito local positivo `ClienteElegivel`:

<!-- snippet-csharp: id=c12-03; categoria=contexto -->
```csharp
public sealed record CadastroClienteLegado(
    string Codigo,
    string Correio,
    bool Inativo,
    bool Bloqueado);

public sealed class ConsultaDeClienteLegado(IClienteLegado clienteLegado)
    : IConsultaDeCliente
{
    public async Task<ClienteElegivel> ObterElegivelAsync(
        string clienteId,
        CancellationToken cancellationToken)
    {
        var cadastro = await clienteLegado.BuscarAsync(clienteId, cancellationToken)
            ?? throw new RegraDeNegocioException("Cliente não encontrado no cadastro externo.");

        if (cadastro.Inativo || cadastro.Bloqueado)
            throw new RegraDeNegocioException("O cliente não está elegível para realizar pedidos.");

        return new ClienteElegivel(cadastro.Codigo, new Email(cadastro.Correio));
    }
}
```

Pedidos depende de `IConsultaDeCliente` e recebe tipos locais. O nome do campo
`Correio`, os flags e o DTO imposto pelo fornecedor ficam na ACL. Validar o
`Email` durante a tradução impede que dados externos inválidos contaminem o
modelo.

### ACL não é apenas mapeamento de propriedades

Copiar `Nome` para `Nome` pode ser feito por um adapter simples. A ACL se
justifica quando há distância semântica: “ativo” tem regras diferentes,
centavos viram moeda decimal, fusos mudam a interpretação de datas ou um estado
externo representa vários estados locais. Ela também traduz falhas externas para
resultados que o caso de uso entende, sem esconder indisponibilidade como
“cliente inexistente”.

Mantenha regra local no domínio. A ACL decide o significado do contrato externo
e o adapta; ela não deve virar um segundo modelo de Pedidos nem orquestrar o caso
de uso inteiro. Testes de contrato devem detectar mudanças do fornecedor e
testes unitários devem cobrir cada tradução semântica.

### Custos e quando evitar

Uma ACL adiciona código, testes, observabilidade e atraso de evolução. Se dois
componentes compartilham deliberadamente o mesmo modelo estável e a mesma equipe
controla ambos, um contrato compartilhado ou adapter fino pode bastar. Criar uma
camada que apenas renomeia todas as propriedades sem proteger significado é
cerimônia.

Use ACL quando o sistema externo muda de forma independente, o contrato é
hostil ao modelo local, uma migração será gradual ou o vocabulário tem diferenças
que já causam condicionais espalhadas. Registre propriedade e ciclo de vida da
camada: uma ACL de migração pode ser descartada; uma fronteira com fornecedor
externo tende a ser permanente.

## Alternativas e trade-offs

| Decisão | Benefício | Custo e sinal de alerta |
| --- | --- | --- |
| regra na entidade/value object | invariante junto do estado, chamada direta | classe cresce sem coesão quando recebe I/O ou políticas sem dono |
| Domain Service | nomeia política transversal testável | vira depósito procedural e produz modelo anêmico |
| Application Service | caso de uso e ordem de efeitos explícitos | duplica regra se toma decisões que o agregado deveria proteger |
| ACL | isola mudança e semântica externa | tradução, testes e operação adicionais sem valor quando modelos já coincidem |
| usar diretamente SDK/DTO externo | menor código inicial | espalha contrato externo e aumenta custo de substituição ou mudança |

Não separe camadas para perseguir “pureza”. Separe decisões que mudam por razões
diferentes. Um endpoint pequeno pode chamar um Application Service concreto; um
sistema com poucos casos de uso não precisa de mediator, base handler, factory e
repository genérico.

## Erros comuns

- Sufixar qualquer classe com `DomainService` sem que ela expresse política do
  negócio.
- Colocar validações do agregado no controller ou Application Service.
- Injetar repositórios em entidades, tornando comportamento dependente de I/O e
  ciclo de vida do container.
- Fazer `SaveChanges` em cada repositório e quebrar a unidade de trabalho.
- Confundir ACL com gateway HTTP: o cliente lida com protocolo; a ACL lida com
  semântica. Eles podem estar na mesma classe pequena, mas são responsabilidades
  distintas.
- Tratar timeout do fornecedor como ausência de entidade.
- Publicar evento de integração antes do commit ou supor que um evento em memória
  oferece entrega confiável.
- Criar interfaces para toda classe, mesmo sem fronteira, substituição ou teste
  que se beneficie delas.

## Como verificar

Testes unitários do domínio exercitam regras sem mock de banco. Testes do
Application Service usam fakes orientados à porta para verificar a ordem dos
efeitos e que falha ao persistir impede publicação. Testes da ACL cobrem tradução,
estados externos, dados malformados e erros distintos de indisponibilidade.

Na infraestrutura real, acrescente testes de integração para mapeamento e
transação e testes de contrato para o serviço externo. Observe latência, taxa e
motivo de falhas da ACL sem registrar dados pessoais. Meça conflitos de
concorrência por caso de uso; eles podem revelar agregado amplo ou unidade de
trabalho longa.

## Laboratório executável

O laboratório compartilhado com C11 está em
[c11-c12-ddd](../../exemplos/c11-c12-ddd/). Execute:

```bash
dotnet test exemplos/c11-c12-ddd/C11C12.Ddd.Tests.csproj
```

Ele demonstra portas específicas, relógio controlável, publicação após
persistência e tradução do cadastro legado. Os fakes não pretendem provar
comportamento de banco ou rede. Não há mediator, container de DI, ORM ou broker,
porque nenhum deles é necessário para observar as decisões deste capítulo.

## Como decidir

Coloque a invariante no objeto que controla o estado. Use Application Service
para sequência, transação e integrações. Só crie Domain Service quando houver
uma política reconhecível pelo negócio sem dono natural. Traduza um modelo
externo em uma ACL quando a diferença semântica ameaçar se espalhar; use um
adapter simples quando a diferença for apenas mecânica.

Reavalie quando services começarem a concentrar condicionais do negócio,
entidades virarem bolsas de propriedades ou alterações externas exigirem mudanças
em muitos casos de uso. Esses são sinais de que a fronteira ou a alocação de
comportamento não está protegendo o modelo.

## Relação com outros capítulos

- [C11](c11-entidades-value-objects-agregados.md) define linguagem, entidades,
  value objects, agregados, invariantes e eventos de domínio.
- [C06](../aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md) cobre
  composição, injeção de dependências e lifetimes.
- [C07](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md) trata
  clientes HTTP, timeout, retry e contratos de API.
- [C09](../aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) desenvolve
  repositórios, unidade de trabalho e concorrência no EF Core.
- [C13](c13-dependencias-abstracoes-padroes.md) trata regra de dependência e
  custo de abstrações; [C15](../sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md)
  e [C16](../sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md)
  aprofundam mensageria, idempotência e Outbox.

## Referências

- [DDD Reference](https://www.domainlanguage.com/ddd/reference/) — Eric Evans,
  serviços, camadas e Anti-Corruption Layer, acesso em 18 novembro 2026.
- [Designing a DDD-oriented application](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)
  — .NET Architecture, responsabilidades das camadas; os limites lógicos não
  exigem microsserviços, acesso em 18 novembro 2026.
- [Tactical DDD](https://learn.microsoft.com/azure/architecture/microservices/model/tactical-ddd)
  — Azure Architecture Center, agregados, serviços de aplicação e eventos,
  acesso em 18 novembro 2026.
- [Anti-Corruption Layer pattern](https://learn.microsoft.com/azure/architecture/patterns/anti-corruption-layer)
  — Azure Architecture Center, tradução, custos e critérios de uso, acesso em 18
  novembro 2026.
- [Domain events: design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
  — .NET Architecture, handlers, persistência e eventos de integração, acesso
  em 18 novembro 2026.
