# C01 — Tipos, identidade e modelagem em C\#

> **Nível:** Fundamental
>
> **Objetivo:** escolher entre classes, records e structs a partir da identidade,
> da igualdade, da cópia e dos pontos de extensão exigidos pelo modelo.
>
> **Pré-requisitos:** nenhum.
>
> **Última revisão:** 2026-11-18

Uma escolha de tipo parece local, mas define o que significa copiar, comparar,
alterar e estender um valor em toda a aplicação. Um identificador de pedido, uma
posição geográfica e uma conexão aberta têm necessidades diferentes. Modelá-los
com a mesma semântica pode introduzir aliases mutáveis, chaves instáveis em
dicionários, cópias caras ou hierarquias que não preservam invariantes.

Este capítulo apresenta classes, `record class`, structs e `record struct`, a
diferença entre tipos por referência e por valor, o controle de herança com
`sealed` e as conversões de boxing e unboxing. Garbage Collector (GC), profiling
e otimização sistemática pertencem ao futuro C04 — Memória, runtime e engenharia
de performance; entidades e value objects de domínio serão aprofundados em C11.

## O problema começa pela semântica

Antes de escolher uma sintaxe, responda:

- o objeto representa uma entidade contínua ou um valor substituível;
- duas instâncias com os mesmos componentes devem ser iguais;
- uma atribuição deve compartilhar o mesmo objeto ou produzir uma cópia;
- o estado pode mudar e, se puder, quem pode observar essa mudança;
- consumidores devem especializar o comportamento por herança;
- o valor é pequeno e será copiado com frequência;
- ele atravessará APIs baseadas em `object` ou interfaces.

Essas respostas são mais estáveis do que regras como “dados usam record” ou
“struct evita alocação”. O tipo deve comunicar o modelo correto antes de ser uma
tentativa de otimização.

## Tipos por referência e tipos por valor

Uma variável de tipo por valor contém o próprio valor. Atribuir essa variável,
passá-la por valor a um método ou retorná-la copia o valor. `struct`,
`record struct`, enums e os tipos numéricos internos são tipos por valor.

Uma variável de tipo por referência contém uma referência para um objeto.
Copiar a variável copia a referência: ambas podem alcançar o mesmo objeto.
`class`, `record class`, arrays, delegates e `string` são tipos por referência.

```csharp
var origem = new EstadoDaEntrega { Status = "Criado" };
var alias = origem;
alias.Status = "Pago";

Console.WriteLine(origem.Status); // Pago: os dois nomes chegam ao mesmo objeto.

var ponto = new PontoMutavel { X = 1, Y = 2 };
var copia = ponto;
copia.X = 10;

Console.WriteLine(ponto.X); // 1: o struct foi copiado.

public sealed class EstadoDaEntrega
{
    public required string Status { get; set; }
}

public struct PontoMutavel
{
    public int X { get; set; }
    public int Y { get; set; }
}
```

O `PontoMutavel` serve para tornar a cópia visível, não como recomendação de
design. Structs imutáveis evitam a surpresa de alterar apenas uma cópia.

### Cópias podem continuar compartilhando objetos internos

Copiar um struct copia cada campo. Se um campo for uma referência, a referência
é copiada, não o objeto referenciado. As duas cópias continuam vendo a mesma
lista, array ou outro objeto mutável.

```csharp
public struct Lote
{
    public int Numero;
    public List<string> Etiquetas;
}

var primeiro = new Lote { Numero = 1, Etiquetas = ["urgente"] };
var segundo = primeiro;
segundo.Numero = 2;
segundo.Etiquetas.Add("revisar");

Console.WriteLine(primeiro.Numero);                 // 1
Console.WriteLine(string.Join(", ", primeiro.Etiquetas)); // urgente, revisar
```

Imutabilidade rasa também afeta records: propriedades `init` impedem nova
atribuição depois da construção, mas não congelam os objetos guardados nelas.

### Métodos, parâmetros e coleções

O padrão é passagem por valor tanto para classes quanto para structs. O que muda
é o valor copiado: a referência, no primeiro caso; todos os campos do valor, no
segundo. `ref`, `in` e `out` alteram essa regra e devem comunicar uma necessidade
real, pois tornam aliasing e mutação menos locais.

```csharp
static void IncrementarCopia(Contador contador) => contador.Incrementar();
static void IncrementarOriginal(ref Contador contador) => contador.Incrementar();

public struct Contador
{
    public int Valor { get; private set; }
    public void Incrementar() => Valor++;
}
```

Coleções genéricas armazenam o `T` de acordo com sua categoria. Um `List<Ponto>`
mantém os valores inline em seu array interno; um `List<Cliente>` mantém
referências. Ler um struct pelo indexador de `List<T>` devolve uma cópia. Alterar
essa cópia não atualiza o elemento: é preciso substituí-lo ou escolher uma API
que exponha uma referência controlada, se o cenário realmente justificar isso.

### Stack e heap não definem a categoria do tipo

“Struct fica na stack e classe fica no heap” não é um modelo correto. A
categoria define semântica, não um endereço fixo:

- um struct pode estar como campo inline de um objeto ou como elemento de array
  no heap gerenciado;
- um valor submetido a boxing passa a fazer parte de um novo objeto no heap;
- uma variável local pode ocupar stack, registrador ou até ser eliminada pelas
  otimizações do compilador Just-in-Time (JIT);
- a referência local e o objeto referenciado são coisas distintas.

A localização concreta é detalhe da implementação e do contexto de execução.
Use a semântica de cópia para modelar; use ferramentas de diagnóstico para
investigar memória.

## Classes, records e structs

### `class`: identidade e estado compartilhado

Uma classe comum usa igualdade por referência por padrão. Duas instâncias com os
mesmos dados continuam diferentes, salvo se o tipo sobrescrever `Equals`,
`GetHashCode` e, quando apropriado, os operadores de igualdade.

Classes combinam bem com identidade e ciclo de vida: um pedido continua sendo o
mesmo pedido enquanto seu estado muda. Também são adequadas quando o objeto é
grande, precisa ser compartilhado ou participa de polimorfismo por herança.

### `record class`: referência com igualdade por valor

`record` e `record class` são sinônimos. O compilador sintetiza igualdade por
valor, `GetHashCode`, representação textual, suporte a desconstrução em records
posicionais e cópia não destrutiva com `with`.

```csharp
public sealed class Cliente
{
    public Cliente(Guid id, string nome) => (Id, Nome) = (id, nome);
    public Guid Id { get; }
    public string Nome { get; }
}

public sealed record Dinheiro(decimal Quantia, string Moeda);

var id = Guid.NewGuid();
var cliente1 = new Cliente(id, "Ada");
var cliente2 = new Cliente(id, "Ada");
var dinheiro1 = new Dinheiro(10m, "BRL");
var dinheiro2 = new Dinheiro(10m, "BRL");

Console.WriteLine(cliente1 == cliente2); // False: identidade por padrão.
Console.WriteLine(dinheiro1 == dinheiro2); // True: componentes iguais.
Console.WriteLine(ReferenceEquals(dinheiro1, dinheiro2)); // False.
```

Igualdade gerada é feita a partir dos membros relevantes, usando a igualdade de
cada componente. Um array, por exemplo, continua usando igualdade por referência
por padrão. Um record também pode declarar propriedades mutáveis:

```csharp
public record Preferencias
{
    public string Tema { get; set; } = "claro";
}
```

Portanto, `record` não é sinônimo de imutabilidade. Record classes posicionais
geram propriedades `init`, mas referências contidas ainda podem apontar para
objetos mutáveis. Se o record for chave de `Dictionary` ou membro de `HashSet`,
alterar um componente usado no hash pode tornar o item logicamente inalcançável.

### `struct`: valor copiado

Structs representam valores pequenos e autocontidos. Não suportam herança de
outra classe ou struct, embora possam implementar interfaces. Por padrão,
`ValueType.Equals` oferece igualdade baseada nos campos, mas uma struct comum
não recebe operadores `==` e `!=` automaticamente. Para contratos públicos,
implementar `IEquatable<T>` ou usar `record struct` torna a intenção mais clara.

Structs grandes merecem cautela: atribuições, parâmetros e retornos podem copiar
todos os seus campos. `in` e `ref readonly` podem reduzir cópias em caminhos
medidos, mas aumentam a complexidade da API e não corrigem uma modelagem ruim.
Não há um limite universal de tamanho; frequência de cópia, layout, boxing e
resultado de profiling importam mais do que uma regra numérica isolada.

`readonly struct` impede a mutação dos campos de instância depois da construção
e ajuda o compilador a evitar cópias defensivas em alguns contextos:

```csharp
public readonly struct Percentual
{
    public Percentual(decimal valor)
    {
        if (valor is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(valor));

        Valor = valor;
    }

    public decimal Valor { get; }
}
```

### `record struct`: cópia por valor com igualdade gerada

`record struct` mantém a semântica de valor do struct e adiciona os membros
gerados de record. Um record struct posicional é mutável por padrão;
`readonly record struct` expressa imutabilidade do próprio valor.

```csharp
public readonly record struct Coordenada(decimal Latitude, decimal Longitude);

var origem = new Coordenada(-23.55m, -46.63m);
var destino = origem with { Longitude = -43.17m };

Console.WriteLine(origem == destino); // False.
```

O `with` também faz cópia rasa. Ele não clona objetos referenciados pelos
componentes.

### Comparação resumida

| Forma | Categoria | Igualdade padrão | Herança | Mutabilidade |
|---|---|---|---|---|
| `class` | referência | identidade | suporta | definida pelo autor |
| `record class` | referência | valores dos componentes | entre records | possível; não é proibida |
| `struct` | valor | campos via `Equals`; sem `==` gerado | não suporta | possível; prefira imutável |
| `record struct` | valor | valores dos componentes, com operadores | não suporta | posicional é mutável por padrão |

Essa tabela descreve padrões da linguagem. Qualquer tipo pode personalizar parte
do contrato de igualdade, respeitando consistência entre `Equals`, `GetHashCode`
e operadores.

## Imutabilidade reduz estados acidentais

Um objeto imutável não muda depois de construído. Isso permite compartilhar a
mesma instância sem coordenar escritas, torna a validação concentrada na criação
e mantém chaves de coleções estáveis. `with` permite criar uma nova versão sem
alterar a anterior.

O custo é produzir novas instâncias e tratar estruturas internas mutáveis. Para
agregados com muitas mudanças incrementais, um objeto mutável com encapsulamento
forte pode ser mais simples e eficiente. A decisão não é “imutável em todos os
casos”, e sim tornar mutação explícita e preservar invariantes.

## `sealed`, herança e pontos de extensão

Uma classe `sealed` não pode ser usada como classe base. Um membro
`sealed override` encerra apenas aquele ponto de sobrescrita na hierarquia.

```csharp
public abstract class PoliticaDeFrete
{
    public abstract decimal Calcular(decimal peso);
}

public sealed class FreteFixo : PoliticaDeFrete
{
    public override decimal Calcular(decimal peso) => peso <= 10 ? 20m : 35m;
}
```

Selar é uma decisão de contrato. Se uma classe chama membros virtuais durante a
construção, mantém estado `protected` sensível ou depende de uma ordem específica
de operações, subclasses podem violar invariantes que a classe base não consegue
controlar. Torná-la `sealed` reduz a superfície de extensão e permite evoluir
detalhes internos com menor risco de quebrar subclasses externas.

Composição costuma oferecer um ponto de variação mais explícito:

```csharp
public interface IPoliticaDeDesconto
{
    decimal Aplicar(decimal subtotal);
}

public sealed class CalculadoraDeTotal(IPoliticaDeDesconto politica)
{
    public decimal Calcular(decimal subtotal) => politica.Aplicar(subtotal);
}
```

A dependência pode ser substituída sem herdar estado, métodos protegidos ou
regras de ciclo de vida. Herança ainda é adequada para uma relação substituível
e deliberadamente projetada, com contrato documentado, poucos membros virtuais
e testes para implementações derivadas. Frameworks e bibliotecas que oferecem
extensibilidade por subclasses não devem ser selados sem preservar uma
alternativa equivalente.

A especificação observa que conhecer a ausência de subclasses pode possibilitar
desvirtualização e outras otimizações do runtime. Isso não garante melhoria em
um programa específico: o JIT já usa informações de tipo em vários contextos.
Não aplique `sealed` como micro-otimização sem medição; aplique-o primeiro para
expressar o contrato de extensão.

## Boxing e unboxing

Boxing converte um tipo por valor em `object` ou em uma interface implementada
por ele. O runtime cria um objeto no heap gerenciado e copia o valor para esse
objeto. Unboxing verifica se o objeto contém exatamente o tipo esperado e copia
o valor para uma variável daquele tipo.

```csharp
int quantidade = 42;
object caixa = quantidade;      // boxing e alocação
int copia = (int)caixa;         // unboxing

// Não converte durante unboxing:
// short invalido = (short)caixa; // InvalidCastException
short valido = (short)(int)caixa;
```

Converter um struct para uma interface também pode fazer boxing:

```csharp
public interface IComCodigo { int Codigo { get; } }
public readonly record struct Item(int Codigo) : IComCodigo;

var item = new Item(7);
IComCodigo contrato = item; // boxing
```

Generics preservam o tipo concreto e frequentemente evitam boxing:

```csharp
static bool MesmoValor<T>(T esquerda, T direita) =>
    EqualityComparer<T>.Default.Equals(esquerda, direita);
```

Isso não significa que toda chamada genérica seja livre de alocações. Uma
conversão explícita para `object` ou interface dentro do método, delegates,
closures e outras operações podem alocar. Também há chamadas de interface sobre
parâmetros genéricos restritos que o runtime pode executar sem boxing; confirme
o caminho concreto em vez de inferir pelo nome da API.

Boxing isolado em configuração, logging ocasional ou uma operação dominada por
I/O raramente justifica complicar o design. Em loops intensos ou caminhos com
alta taxa de eventos, alocações repetidas podem aumentar a pressão sobre o GC.
Use um profiler de alocação, `dotnet-trace`, `dotnet-counters` ou um benchmark
controlado para localizar e quantificar o custo. Não use uma única leitura de
memória ou um cronômetro improvisado como evidência.

## Critérios de escolha

Use uma classe quando identidade, estado compartilhado, tamanho variável ou
polimorfismo por herança fizerem parte do modelo. Use `record class` quando o
tipo continuar sendo referência, mas igualdade estrutural e cópia com `with`
representarem corretamente dados ou mensagens.

Considere `readonly struct` ou `readonly record struct` quando o conceito for um
valor pequeno, autocontido, frequentemente embutido ou armazenado em coleções e
quando cópia independente for desejada. Prefira `record struct` se a igualdade
gerada fizer parte do contrato. Volte para uma classe se o valor crescer,
precisar de identidade, for mutável, sofrer boxing frequente ou se cópias
começarem a dominar um caminho medido.

Use herança quando consumidores precisarem de uma relação “é um” substituível e
o tipo tiver sido projetado para isso. Use composição para variar uma
responsabilidade sem expor o estado interno. Use `sealed` quando derivação não
fizer parte do contrato ou puder quebrar invariantes; evite-o em pontos de
extensão públicos e intencionais.

## Armadilhas comuns

- Confundir igualdade de record com identidade de domínio. Duas entidades com o
  mesmo estado ainda podem ter histórias e identidades distintas.
- Colocar coleções mutáveis dentro de um record e presumir imutabilidade profunda
  ou comparação por conteúdo.
- Alterar campos usados por `GetHashCode` depois de inserir uma chave em
  `Dictionary` ou `HashSet`.
- Criar structs grandes e mutáveis; cópias implícitas ficam caras e difíceis de
  perceber.
- Alterar uma cópia lida de uma coleção esperando atualizar o elemento original.
- Escolher struct com a justificativa genérica de “ficar na stack”.
- Converter valores repetidamente para `object` ou interfaces em um caminho
  crítico sem observar alocações.
- Abrir herança por padrão e descobrir depois que subclasses dependem de detalhes
  internos; ou selar uma API cujo propósito era ser estendida.

## Como verificar

O laboratório em `exemplos/c01-tipos-modelagem/` contém testes para igualdade de
classe e record, cópia de structs em atribuições, parâmetros e listas, passagem
por `ref`, boxing para `object` e interface e comparação genérica.

```bash
dotnet test exemplos/c01-tipos-modelagem/C01.TiposModelagem.Tests.csproj
```

Ele usa .NET 10, não possui serviços externos e não mede performance. Os testes
demonstram semântica; eventuais custos devem ser medidos no contexto real.

## Relação com outros capítulos

- **Próximo fundamento:** [C02 — Nulabilidade, falhas e exceções](c02-nulabilidade-falhas-excecoes.md)
  adiciona contratos de ausência às categorias de tipo apresentadas aqui.
- **Aprofundamentos planejados:** C03 trata o comportamento em coleções e
  execução diferida; C04 trata GC, profiling, boxing e otimização baseada em
  evidência.
- **Aplicações planejadas:** C11 usa identidade e igualdade para distinguir
  entidades e value objects; C13 aprofunda abstrações, composição e herança.

## Referências

- [Sistema de tipos de C#](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [Tipos por valor](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/value-types)
  — referência de C#, acesso em 18 novembro 2026.
- [Tipos por referência](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/reference-types)
  — referência de C#, acesso em 18 novembro 2026.
- [Tipos record em C#](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/records)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [Tipos struct](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/struct)
  — referência de C#, acesso em 18 novembro 2026.
- [Comparações de igualdade em C#](https://learn.microsoft.com/dotnet/csharp/fundamentals/expressions/equality)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [Classes e membros abstratos e selados](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/abstract-and-sealed-classes-and-class-members)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [Especificação de C#: classes seladas](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/classes#15223-sealed-classes)
  — especificação da linguagem, acesso em 18 novembro 2026.
- [Boxing e unboxing](https://learn.microsoft.com/dotnet/csharp/programming-guide/types/boxing-and-unboxing)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [`GC.GetAllocatedBytesForCurrentThread`](https://learn.microsoft.com/dotnet/api/system.gc.getallocatedbytesforcurrentthread)
  — API .NET, acesso em 18 novembro 2026.
