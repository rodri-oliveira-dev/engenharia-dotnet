# C11 — Entidades, value objects e agregados

> **Nível:** Fundamental
>
> **Objetivo:** modelar conceitos do negócio, proteger invariantes e escolher
> limites transacionais sem transformar DDD em uma coleção obrigatória de
> padrões.
>
> **Pré-requisitos:** [C01 — Tipos, identidade e modelagem em C#](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md)
> e [C02 — Nulabilidade, falhas e exceções](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md).
>
> **Última revisão:** 2026-11-18

Domain-Driven Design (DDD) é uma abordagem para desenvolver software em torno
do conhecimento do negócio. Seu valor não está em renomear classes como
`Entity` ou `Repository`, mas em aproximar conversas, decisões e código das
regras que diferenciam o sistema. Entidades, value objects e agregados são
ferramentas táticas; antes deles vêm linguagem, limites e prioridades.

Este capítulo estabelece os fundamentos estratégicos e desenvolve o modelo de
Pedidos usado no laboratório. [C12](c12-servicos-dominio-integracao-modelos.md)
separa serviços de aplicação e de domínio e mostra uma Anti-Corruption Layer.
Mensageria, entrega confiável e implementação completa de Outbox pertencem a
C15 e C16.

## Comece pelo problema, não pelos padrões

Um mesmo termo pode ter significados incompatíveis. “Cliente” pode ser a pessoa
autenticada no contexto de Identidade, o contratante em Faturamento e o
destinatário em Entregas. Um modelo único para a empresa inteira acumula campos,
condicionais e regras conflitantes. DDD estratégico torna essas diferenças
explícitas.

- **Domínio** é a área de atividade e conhecimento que o software atende, como
  comércio eletrônico.
- **Subdomínio** é uma parte coerente do problema. Catálogo, Pedidos, Pagamentos
  e Entregas podem ser subdomínios distintos.
- **Core Domain** é a capacidade que gera diferenciação ou vantagem e merece o
  maior investimento de modelagem. Ele é uma decisão estratégica, não sinônimo
  de “tudo que é importante”.
- **Supporting Subdomain** sustenta o core e contém regra específica da empresa,
  mas não é sua diferenciação principal. Uma política interna de expedição pode
  ser um exemplo.
- **Generic Subdomain** resolve problema conhecido e pouco diferenciador, como
  autenticação ou envio de e-mail. Comprar, adotar uma solução pronta ou usar um
  modelo simples costuma ser preferível a investir em inovação local.

Essa classificação muda com a estratégia. Pagamentos podem ser genéricos para
uma loja e o core para uma adquirente. A consequência prática é alocar atenção:
modelagem colaborativa e especialistas no core; soluções adequadas e econômicas
nos demais subdomínios.

### Linguagem ubíqua e modelagem orientada ao negócio

A **Ubiquitous Language** (linguagem ubíqua) é o vocabulário compartilhado por
especialistas e equipe técnica dentro de um limite. Ela aparece em conversas,
exemplos, código, testes e documentação. Se o negócio diz “confirmar pedido”, um
método `Confirmar` comunica melhor que `UpdateStatus(2)`.

Linguagem ubíqua não é um glossário congelado. Contradições reveladas durante a
implementação devem voltar à conversa: “remover um item confirmado” é correção,
cancelamento parcial ou devolução? Cada resposta implica estados e invariantes
diferentes. Renomear código quando o entendimento muda faz parte da modelagem.

Evite transportar nomes técnicos para o negócio. `PedidoRepository`, `DTO` e
`rowversion` descrevem solução; “pedido rascunho”, “preço acordado” e “limite de
itens” descrevem o domínio. Os dois vocabulários são necessários, mas cumprem
papéis diferentes.

### Bounded Context

Um **Bounded Context** delimita onde um modelo e sua linguagem são consistentes.
Dentro de Pedidos, `ProdutoId` pode ser apenas a referência ao item comprado e o
preço fica congelado no momento da compra; em Catálogo, Produto pode possuir
descrição, variantes e regras de publicação. Não se deve compartilhar a entidade
completa apenas porque os dois contextos usam a palavra “produto”.

Subdomínio pertence ao espaço do problema; bounded context pertence à solução.
Eles podem se alinhar um para um, mas isso não é obrigatório. Um contexto pode
cobrir dois subdomínios simples ou um subdomínio complexo pode exigir mais de um
contexto. Tampouco bounded context implica microsserviço: módulos de um monólito
podem preservar limites de linguagem e dependência com custo operacional menor.

As relações entre contextos devem declarar direção e tradução. Compartilhar
contratos, adotar o modelo do fornecedor ou construir uma Anti-Corruption Layer
são decisões diferentes; [C12](c12-servicos-dominio-integracao-modelos.md)
desenvolve essa fronteira.

### DDD estratégico e tático

| Perspectiva | Pergunta principal | Ferramentas frequentes |
| --- | --- | --- |
| Estratégica | Onde cada modelo vale, qual capacidade diferencia e como contextos se relacionam? | subdomínios, Core Domain, linguagem ubíqua, bounded contexts e context maps |
| Tática | Como expressar e proteger as regras dentro de um contexto? | entidades, value objects, agregados, serviços e eventos de domínio |

É possível usar modelagem estratégica sem todos os padrões táticos. Também é
possível criar um value object útil em uma aplicação simples sem declarar que
toda a organização “adota DDD”. O problema governa a técnica.

### Quando DDD agrega valor

DDD tende a compensar quando regras mudam e interagem, existem exceções reais,
o vocabulário é ambíguo, decisões dependem de especialistas e erros violam
dinheiro, contrato ou operação. O sinal não é o número de tabelas, mas a
complexidade do comportamento e do conhecimento.

Uma tela administrativa que apenas cadastra uma lista de países raramente
precisa de agregados ricos, eventos e serviços de domínio. CRUD com validações de
entrada e constraints no banco pode ser mais claro. DDD também perde valor sem
acesso contínuo ao conhecimento do negócio: padrões no código não substituem
descoberta. Use a solução mais simples que proteja as regras atuais e evolua
quando surgirem sinais concretos.

## Entidades: continuidade por identidade

Uma **entidade** é reconhecida pela identidade ao longo do tempo, mesmo que seu
estado mude. Dois pedidos com os mesmos itens e total continuam sendo pedidos
distintos; o mesmo pedido antes e depois da confirmação conserva sua identidade.
O identificador pode ser atribuído pela aplicação, banco ou domínio, desde que
seja estável no ciclo de vida relevante.

Estado sem comportamento deixa regras espalhadas. No exemplo, `Pedido` não
expõe setters públicos nem sua lista mutável. Operações nomeadas validam a
transição antes de alterar o estado:

```csharp
public void Confirmar(DateTimeOffset ocorridoEm)
{
    ExigirRascunho();
    if (_itens.Count == 0)
        throw new RegraDeNegocioException("Um pedido sem itens não pode ser confirmado.");
    if (Total.Quantia == 0)
        throw new RegraDeNegocioException("Um pedido de valor zero não pode ser confirmado.");

    Estado = EstadoPedido.Confirmado;
    _eventos.Add(new PedidoConfirmado(Id, Total, ocorridoEm));
}
```

Uma **invariante** é uma condição que deve permanecer verdadeira ao final de
cada operação consistente. No modelo do laboratório:

- apenas rascunhos aceitam alteração de itens e desconto;
- quantidades ficam entre 1 e 100 por item;
- um produto aparece uma vez e seu preço não muda implicitamente;
- todos os itens usam a moeda do pedido;
- desconto não supera 30%;
- confirmação exige itens e total positivo.

O objeto rejeita a operação antes de publicar estado inválido. A exceção de
domínio representa uma solicitação que viola uma regra; falhas esperadas também
poderiam usar um tipo `Result`. A escolha deve ser consistente na fronteira da
aplicação. Nullable ou um `bool` isolado não explica por que a operação falhou.

### Ciclo de vida e transições

Construtores ou factories devem produzir um estado válido. Métodos representam
transições permitidas; reidratação pela persistência precisa restaurar um estado
que já era válido, não contornar regras para aceitar dados arbitrários. No
exemplo, o ciclo é:

```text
Rascunho --Confirmar--> Confirmado --Cancelar--> Cancelado
```

Não há transição de volta. Se o negócio permitir correção após confirmação, ela
deve ganhar nome, autorização e efeito próprios. Um setter de `Estado` permitiria
saltos que nunca foram discutidos.

### Modelo anêmico e entidade rica

Um modelo anêmico contém dados e deixa regras em services procedurais. Isso é
problemático quando vários casos de uso conseguem alterar a mesma estrutura e
precisam se lembrar das mesmas validações. A entidade rica mantém junto do
estado o comportamento que só ela pode proteger.

“Rica” não significa concentrar tudo em uma classe. Enviar e-mail, consultar
estoque e abrir transação são orquestração ou infraestrutura. Cálculos sem estado
podem viver em value objects ou, quando realmente envolvem vários conceitos, em
um serviço de domínio. Um registro CRUD sem comportamento relevante pode ser
deliberadamente simples; fabricar métodos que apenas copiam setters não melhora
o modelo.

### Entidade de domínio não é entidade de persistência

“Entidade” no EF Core significa um tipo mapeado e rastreado; em DDD significa um
conceito definido por identidade. Os conjuntos podem coincidir, mas não são
sinônimos. Tabela de junção, registro de Outbox e projeção de leitura podem ser
entidades de persistência sem identidade no domínio. Um agregado pode ser
mapeado em várias tabelas.

Configuração Fluent permite persistir campos e coleções privados sem colocar
attributes do ORM no domínio. Em um sistema simples, usar a mesma classe para
domínio e persistência pode reduzir tradução. Separe modelos quando o schema
impuser formas que corrompem a linguagem, quando leitura e escrita divergirem ou
quando o acoplamento ao ORM bloquear invariantes. Não duplique classes por
princípio: tradução também tem custo e pode perder dados.

## Value objects: o valor define a igualdade

Um **value object** não possui identidade relevante. `Dinheiro(10, "BRL")` é
igual a outro valor com a mesma quantia e moeda; trocar um valor significa
substituí-lo, não acompanhar seu ciclo de vida. Imutabilidade torna essa
semântica segura para compartilhamento e evita que uma alteração lateral invalide
uma entidade.

O laboratório implementa três formas:

- `Dinheiro` mantém quantia e moeda juntas, impede moeda inválida, rejeita somas
  entre moedas e define arredondamento ao aplicar percentual;
- `Email` valida e normaliza o endereço uma vez;
- `Percentual` restringe o intervalo e expõe a conversão para fração.

`record` e `record struct` fornecem igualdade estrutural, mas não fornecem regras
de domínio. Um record posicional `record Dinheiro(decimal Quantia, string
Moeda)` ainda aceitaria moeda vazia e combinações inválidas. O construtor precisa
estabelecer o contrato.

```csharp
public readonly record struct Percentual
{
    public Percentual(decimal valor)
    {
        if (valor is < 0 or > 100)
            throw new RegraDeNegocioException("O percentual deve estar entre 0 e 100.");
        Valor = valor;
    }

    public decimal Valor { get; }
    public decimal Fracao => Valor / 100m;
}
```

### Integração com tipos de C\#

`readonly record struct` é adequado para valores pequenos, imutáveis e usados
com frequência, mas cópias de structs grandes custam. `sealed record` é útil
quando semântica de referência, tamanho ou hierarquia de dados favorecem uma
classe. Conversões implícitas para primitivos devem ser raras: se `Email` vira
`string` automaticamente em qualquer lugar, é fácil perder sua fronteira. Exponha
`Valor`, formatação e conversões explícitas nos adapters.

Serializadores e ORMs podem precisar de converters, construtores ou mapeamento
de propriedades. Esse é um problema da infraestrutura. Não enfraqueça o
construtor público com um estado inválido apenas para satisfazer uma ferramenta.

### Quando não criar um value object

O benefício deve superar nomes e conversões adicionais. Um `NomeDaRua` sem
regra, operação, ambiguidade ou risco de troca talvez seja apenas `string` dentro
de `Endereco`. Criar wrapper para cada primitivo produz cerimônia e dificulta
integração sem proteger nada. Extraia um tipo quando ele:

- combina valores que só fazem sentido juntos;
- centraliza validação ou operação repetida;
- impede troca de argumentos semanticamente diferentes;
- torna unidade, moeda, formato ou ausência explícitos.

## Primitive obsession: regra dissolvida em tipos genéricos

**Primitive obsession** ocorre quando `string`, `decimal`, `int` ou `Guid`
carregam conceitos distintos sem proteção. Um método
`Adicionar(string produto, int quantidade, decimal preco, string moeda)` permite
trocas, unidades inválidas e regras duplicadas. `ProdutoId`, `Dinheiro` e
`Percentual` restringem o espaço de estados representáveis.

Nem todo primitivo é obsessão. A quantidade do item permanece `int` porque sua
regra é local, simples e protegida por `ItemPedido`; um `QuantidadeDeItem` não
adicionaria valor suficiente neste exemplo. Se a quantidade ganhasse unidade de
medida, conversão e política de fracionamento, um tipo próprio passaria a ser
justificado. Tipos específicos são uma resposta a risco observado, não uma meta
de cobertura.

## Agregados: o limite da consistência imediata

Um **agregado** é um conjunto de entidades e value objects tratado como unidade
para alterações. Sua **Aggregate Root** (raiz) é a única porta pública para
mutação. No laboratório, `Pedido` é a raiz e `ItemPedido` é interno ao agregado:
consumidores não instanciam nem alteram um item diretamente.

```csharp
private readonly List<ItemPedido> _itens = [];
private readonly ReadOnlyCollection<ItemPedido> _itensSomenteLeitura;

public IReadOnlyList<ItemPedido> Itens => _itensSomenteLeitura;

public void AdicionarItem(ProdutoId produtoId, int quantidade, Dinheiro precoUnitario)
{
    ExigirRascunho();
    ExigirMoeda(precoUnitario);
    // localiza o item, valida preço e quantidade, então altera a coleção
}
```

Retornar `IReadOnlyList` não basta se os elementos tiverem setters públicos. A
coleção é somente leitura e os métodos mutadores de `ItemPedido` são internos;
a raiz coordena as duas proteções.

### Limites de consistência e invariantes transacionais

Tudo que precisa ficar consistente no mesmo commit para uma operação pertence
ao mesmo limite transacional ou precisa de uma garantia equivalente. Quantidade,
preço, desconto, estado e total do pedido mudam juntos, por isso estão no mesmo
agregado. Cadastro completo do cliente, catálogo e estoque têm ciclos de vida e
contenção próprios; o pedido guarda apenas referências ou valores capturados.

Referencie outro agregado por identidade, não por um grafo mutável. Isso evita
carregar e bloquear uma rede de objetos e torna explícito que não se pode alterar
o outro agregado pela raiz atual. Se uma regra exige informação externa, a
aplicação obtém um snapshot antes da operação ou coordena agregados. Se exige
atomicidade permanente entre ambos, reavalie o limite: talvez sejam um único
agregado, ou talvez a regra aceite consistência posterior.

### Tamanho e controle de alterações

Agregados grandes parecem convenientes porque permitem validar tudo em memória,
mas aumentam leitura, escrita, tracking e conflitos. Um agregado `Cliente` que
contém todos os pedidos faz duas compras independentes disputarem a mesma versão
do cliente. Limites menores reduzem contenção e permitem evolução independente.

O extremo oposto também falha: se cada item for uma raiz independente, confirmar
o pedido exigirá coordenar várias escritas para provar total e existência de
itens. Modele pelo conjunto de invariantes, não pelo desenho das telas nem por
uma tabela por classe.

Métodos da raiz devem ser operações de negócio, não `SetX`. O repositório carrega
e persiste a raiz; a unidade de trabalho define o commit. Não é necessário chamar
`Update` em cada item quando o mecanismo de persistência já rastreia o grafo. O
[C09](../aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) detalha
tracking, repositórios e unidade de trabalho.

### Concorrência

Encapsulamento protege uma instância em memória, não duas requisições concorrentes.
Duas cópias do mesmo pedido podem validar sobre a mesma versão e a última escrita
apagar a primeira. A persistência deve usar token de versão, constraint ou
isolamento compatível com a invariante. Um conflito otimista deve recarregar o
estado e reaplicar a intenção, pedir decisão ao usuário ou rejeitar; retry cego
repete uma decisão feita sobre dados antigos.

O agregado determina o que precisa ser consistente; o banco implementa a
garantia sob concorrência. Testes unitários não substituem testes de integração
do mapeamento, token e transação.

## Eventos de domínio: fatos relevantes dentro do contexto

Um **Domain Event** registra em passado algo significativo que aconteceu no
domínio, como `PedidoConfirmado`. “Linha atualizada” é detalhe de persistência,
não evento do negócio. O agregado altera seu estado e acumula o fato sem chamar
broker, e-mail ou repositório:

```csharp
public sealed record PedidoConfirmado(
    Guid PedidoId,
    Dinheiro Total,
    DateTimeOffset OcorridoEm) : IEventoDeDominio;
```

Handlers podem executar efeitos internos ou coordenar outro agregado. Eles não
devem ser necessários para tornar o próprio `Pedido` válido: as invariantes do
agregado precisam valer antes de o método terminar. Execução imediata dentro do
método esconde efeitos, complica testes e pode chamar infraestrutura antes do
commit.

Evento de domínio e **evento de integração** podem representar o mesmo fato, mas
têm contratos e garantias diferentes. O primeiro pertence ao modelo interno e
pode ser despachado em processo. O segundo cruza bounded contexts, precisa ser
versionado e só pode anunciar um estado confirmado. Não exponha diretamente um
tipo interno como contrato público por conveniência.

O serviço do laboratório persiste primeiro e só então publica os eventos
internos. Assim, falha ao salvar impede efeitos colaterais. Ainda existe uma
janela: o processo pode cair depois do commit e antes da publicação. Quando a
entrega externa precisar ser confiável, grave uma mensagem de Outbox na mesma
transação e publique-a posteriormente. C16 tratará deduplicação, reprocessamento
e Outbox; este capítulo não implementa mensageria distribuída.

Eventos não são obrigatórios. Uma chamada direta é mais clara quando há um único
efeito estável e local. Eventos agregam valor para fatos importantes com número
variável de reações; também introduzem ordem, tratamento de falha e rastreamento
mais difíceis.

## Erros recorrentes e alternativas

- Começar pelo banco e chamar cada tabela de agregado. Comece pelas invariantes
  e padrões de alteração; depois mapeie-os.
- Aceitar DTO externo diretamente no domínio. Traduza na fronteira e invoque
  operações válidas, evitando over-posting e estados impossíveis.
- Expor `List<T>`, setters ou entidades filhas mutáveis. A raiz deixa de ser o
  ponto de controle.
- Carregar todos os conceitos relacionados no mesmo agregado. Relação de negócio
  não implica consistência transacional.
- Usar evento para esconder fluxo sequencial ou corrigir um agregado inválido.
- Criar base classes `Entity`, `ValueObject` e barramento antes de existir
  repetição. C# já oferece composição, records e igualdade; abstraia apenas o que
  o modelo pede.
- Exigir arquitetura em camadas ou microsserviços para “ser DDD”. Limites
  conceituais podem existir em poucos projetos e no mesmo processo.

## Laboratório executável

O laboratório [c11-c12-ddd](../../exemplos/c11-c12-ddd/) contém o agregado
`Pedido`, `ItemPedido`, os value objects `Dinheiro`, `Email` e `Percentual`, o
serviço de aplicação e uma ACL. Não depende de framework de DDD nem de banco.

```bash
dotnet test exemplos/c11-c12-ddd/C11C12.Ddd.Tests.csproj
```

Os testes cobrem igualdade e validação dos value objects, adição e remoção
controladas, moeda e preço, confirmação, congelamento de alterações, desconto e
ordem entre persistência e publicação. A ausência de banco é deliberada: estes
testes verificam regras em memória; concorrência e mapeamento exigiriam testes
de integração específicos da infraestrutura.

## Como decidir

Descubra a linguagem com exemplos reais, identifique regras que precisam valer
juntas e dê a cada alteração uma porta explícita. Use entidade para continuidade
por identidade, value object para conceitos definidos pelo valor e agregado para
o menor limite que protege invariantes imediatas. Mantenha referências externas
por identidade e trate concorrência também na persistência.

Se o comportamento for CRUD simples, tipos comuns, validação na fronteira e
constraints podem ser suficientes. Adote cada padrão porque ele remove uma
ambiguidade ou impede uma falha concreta.

## Relação com outros capítulos

- [C01](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md) fundamenta
  classes, records, structs, identidade e igualdade.
- [C02](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md) compara
  exceções, resultados explícitos e estados ausentes.
- [C09](../aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) cobre
  unidade de trabalho, tracking e concorrência otimista na persistência.
- [C10](../aplicacoes-e-dados/c10-fundamentos-persistencia-sql-nosql.md) trata
  transações, constraints e isolamento.
- [C12](c12-servicos-dominio-integracao-modelos.md) aplica serviços de aplicação,
  serviços de domínio e Anti-Corruption Layer.
- C15 aprofundará comunicação e mensageria; C16, Outbox e entrega confiável.

## Referências

- [DDD Reference](https://www.domainlanguage.com/ddd/reference/) — Eric Evans,
  resumo dos padrões e definições de DDD, acesso em 18 novembro 2026.
- [Bounded Context](https://martinfowler.com/bliki/BoundedContext.html) — Martin
  Fowler, linguagem e limites de modelo, acesso em 18 novembro 2026.
- [Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html) —
  Martin Fowler, separação indevida entre dados e comportamento, acesso em 18
  novembro 2026.
- [Designing a microservice domain model](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model)
  — .NET Architecture, entidades, value objects e agregados; os conceitos
  independem de microsserviços, acesso em 18 novembro 2026.
- [Domain events: design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
  — .NET Architecture, eventos internos e de integração, acesso em 18 novembro
  2026.
- [Records em C#](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/records)
  — documentação da linguagem, igualdade por valor e imutabilidade, acesso em
  18 novembro 2026.
