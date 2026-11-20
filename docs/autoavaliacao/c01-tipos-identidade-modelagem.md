# Autoavaliação — C01: Tipos, identidade e modelagem em C\#

Questões de fixação do capítulo
[C01 — Tipos, identidade e modelagem em C#](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md).
Cada questão tem uma única alternativa correta. Tente responder antes de
consultar o [gabarito comentado](#gabarito-comentado), no fim desta página.

## Questões

### Questão 1

Considere que `Pedido` é uma classe e `Ponto` é um struct, ambos com
propriedades mutáveis. Qual será a saída?

<!-- snippet-csharp: id=aval-c01-q01; categoria=isolado -->
```csharp
var pedido = new Pedido { Status = "Criado" };
var outroPedido = pedido;
outroPedido.Status = "Pago";

var ponto = new Ponto { X = 1 };
var outroPonto = ponto;
outroPonto.X = 10;

Console.WriteLine($"{pedido.Status} {ponto.X}");

public sealed class Pedido
{
    public string Status { get; set; } = "";
}

public struct Ponto
{
    public int X { get; set; }
}
```

- A. `Criado 1`
- B. `Pago 1`
- C. `Pago 10`
- D. `Criado 10`

### Questão 2

Qual será a saída do código a seguir?

<!-- snippet-csharp: id=aval-c01-q02; categoria=isolado -->
```csharp
var primeiro = new Lote { Numero = 1, Etiquetas = ["urgente"] };
var segundo = primeiro;
segundo.Numero = 2;
segundo.Etiquetas.Add("revisar");

Console.WriteLine($"{primeiro.Numero} {primeiro.Etiquetas.Count}");

public struct Lote
{
    public int Numero;
    public List<string> Etiquetas;
}
```

- A. `2 2`
- B. `1 1`
- C. `2 1`
- D. `1 2`

### Questão 3

Durante uma revisão, alguém justifica a troca de uma classe por um struct com
o argumento de que "struct fica na stack e classe fica no heap". Qual
afirmação está correta?

- A. A categoria do tipo define semântica de cópia, não um endereço fixo: um
  struct pode estar inline em um objeto ou array no heap, e o boxing cria um
  novo objeto no heap.
- B. Structs são sempre alocados na stack, independentemente de onde forem
  declarados.
- C. Classes são sempre alocadas na stack quando usadas como variáveis locais.
- D. A localização na memória é a principal razão para escolher entre classe e
  struct.

### Questão 4

`Ponto` é um struct mutável e `pontos` é um `List<Ponto>` com um elemento cujo
`X` vale 1. Após o trecho abaixo, `pontos[0].X` continua valendo 1. Por quê?

<!-- snippet-csharp: id=aval-c01-q04; categoria=contexto -->
```csharp
var ponto = pontos[0];
ponto.X = 5;
```

- A. Porque `List<T>` torna seus elementos imutáveis.
- B. Porque structs não podem ter propriedades com `set`.
- C. Porque ler um struct pelo indexador devolve uma cópia; alterar a cópia não
  atualiza o elemento, que precisa ser substituído na lista.
- D. Porque o compilador desfaz automaticamente alterações em variáveis locais.

### Questão 5

`Cliente` é uma classe comum sem sobrescrita de igualdade, e `Dinheiro` é
declarado como `public sealed record Dinheiro(decimal Quantia, string Moeda);`.
Qual será a saída?

<!-- snippet-csharp: id=aval-c01-q05; categoria=isolado -->
```csharp
var id = Guid.NewGuid();
var cliente1 = new Cliente(id, "Ada");
var cliente2 = new Cliente(id, "Ada");
var dinheiro1 = new Dinheiro(10m, "BRL");
var dinheiro2 = new Dinheiro(10m, "BRL");

Console.WriteLine(cliente1 == cliente2);
Console.WriteLine(dinheiro1 == dinheiro2);
Console.WriteLine(ReferenceEquals(dinheiro1, dinheiro2));

public sealed class Cliente(Guid id, string nome)
{
    public Guid Id { get; } = id;
    public string Nome { get; } = nome;
}

public sealed record Dinheiro(decimal Quantia, string Moeda);
```

- A. `True`, `True`, `True`
- B. `False`, `True`, `True`
- C. `True`, `True`, `False`
- D. `False`, `True`, `False`

### Questão 6

Qual será o resultado da comparação abaixo?

<!-- snippet-csharp: id=aval-c01-q06; categoria=isolado -->
```csharp
var a = new Pedido("P1", ["caderno"]);
var b = new Pedido("P1", ["caderno"]);

Console.WriteLine(a == b);

public sealed record Pedido(string Codigo, List<string> Itens);
```

- A. `False`, porque a igualdade gerada compara cada componente com a
  igualdade dele, e `List<string>` usa igualdade por referência.
- B. `True`, porque records sempre comparam o conteúdo das coleções.
- C. `True`, porque records são imutáveis em profundidade.
- D. O código não compila, porque records não aceitam coleções como componentes.

### Questão 7

Um record com uma propriedade `set` é usado como chave de um `Dictionary`.
Depois de inserido, o valor dessa propriedade é alterado, e `ContainsKey` com
a mesma instância passa a retornar `false`. Qual é a explicação?

- A. Records não podem ser usados como chave de dicionário.
- B. O `Dictionary` remove automaticamente chaves alteradas.
- C. A propriedade alterada participa do `GetHashCode` gerado; com o hash
  diferente, o item fica logicamente inalcançável no dicionário.
- D. O `Dictionary` compara chaves apenas por referência, e a alteração criou
  outra instância.

### Questão 8

Sobre igualdade em structs, qual afirmação está correta?

- A. Todo struct recebe automaticamente os operadores `==` e `!=` gerados pelo
  compilador.
- B. `ValueType.Equals` oferece igualdade baseada nos campos, mas um struct
  comum não recebe `==` e `!=` automaticamente; `record struct` gera esses
  operadores.
- C. Structs só podem ser comparados com `ReferenceEquals`.
- D. `record struct` usa igualdade por referência, como uma classe comum.

### Questão 9

Uma classe base chama um membro virtual no construtor e mantém estado
`protected` sensível. A equipe discute marcá-la como `sealed`. Qual avaliação
está de acordo com o capítulo?

- A. `sealed` deve ser aplicado principalmente como micro-otimização, porque
  sempre torna as chamadas mais rápidas.
- B. Selar a classe impede que ela implemente interfaces.
- C. `sealed` deve ser evitado sempre que a classe tiver métodos virtuais.
- D. Selar é uma decisão de contrato: reduz a superfície de extensão quando
  subclasses poderiam violar invariantes; ganhos de desempenho, se existirem,
  precisam de medição.

### Questão 10

O que acontece ao executar a última linha?

<!-- snippet-csharp: id=aval-c01-q10; categoria=isolado -->
```csharp
int quantidade = 42;
object caixa = quantidade;
short valor = (short)caixa;
```

- A. Lança `InvalidCastException`: o unboxing para `short` exige que a caixa
  contenha um `short`; seria preciso `(short)(int)caixa`.
- B. `valor` recebe 42, porque o unboxing converte entre tipos numéricos.
- C. `valor` recebe 0, porque a conversão falha silenciosamente.
- D. O código não compila, porque não é possível converter `object` em `short`.

## Gabarito comentado

### Questão 1

**Resposta: B.** `Pedido` é uma classe: copiar a variável copia a referência, e
os dois nomes chegam ao mesmo objeto, por isso o status vira `Pago`. `Ponto` é
um struct: a atribuição copia o valor, e alterar a cópia não afeta o original.
Veja [Tipos por referência e tipos por valor](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md#tipos-por-referência-e-tipos-por-valor).

### Questão 2

**Resposta: D.** Copiar um struct copia cada campo. `Numero` é copiado como
valor, então `primeiro.Numero` continua 1. `Etiquetas` é uma referência: as
duas cópias apontam para a mesma lista, que passa a ter dois itens. Veja
[Cópias podem continuar compartilhando objetos internos](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md#cópias-podem-continuar-compartilhando-objetos-internos).

### Questão 3

**Resposta: A.** "Struct fica na stack e classe fica no heap" não é um modelo
correto. A categoria define a semântica; a localização concreta é detalhe da
implementação e do contexto de execução. Use a semântica de cópia para modelar
e ferramentas de diagnóstico para investigar memória. Veja
[Stack e heap não definem a categoria do tipo](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md#stack-e-heap-não-definem-a-categoria-do-tipo).

### Questão 4

**Resposta: C.** Um `List<Ponto>` guarda os valores inline no array interno, e
o indexador devolve uma cópia. Para alterar o elemento, é preciso substituí-lo
(por exemplo, `pontos[0] = ponto;`) ou rever a modelagem; structs imutáveis
evitam essa surpresa. Veja
[Métodos, parâmetros e coleções](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md#métodos-parâmetros-e-coleções).

### Questão 5

**Resposta: D.** A classe usa igualdade por referência por padrão, então dois
objetos com os mesmos dados são diferentes. O record sintetiza igualdade por
valor, então os componentes iguais tornam `dinheiro1 == dinheiro2` verdadeiro,
mas continuam sendo instâncias distintas. Veja
[`record class`: referência com igualdade por valor](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md).

### Questão 6

**Resposta: A.** A igualdade gerada usa a igualdade de cada componente. Como
`List<string>` compara por referência, duas listas com o mesmo conteúdo tornam
os records diferentes. `record` também não é sinônimo de imutabilidade
profunda. Veja a seção sobre `record class` no
[C01](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md).

### Questão 7

**Resposta: C.** Alterar um componente usado no hash depois de inserir a chave
em `Dictionary` ou `HashSet` pode tornar o item logicamente inalcançável. Chaves
estáveis são uma das vantagens de tipos imutáveis. Veja
[Armadilhas comuns](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md#armadilhas-comuns).

### Questão 8

**Resposta: B.** Structs comuns têm igualdade baseada nos campos via
`ValueType.Equals`, sem operadores gerados. Para contratos públicos,
implementar `IEquatable<T>` ou usar `record struct` torna a intenção mais clara.
Veja a seção sobre `struct` e a comparação resumida no
[C01](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md#comparação-resumida).

### Questão 9

**Resposta: D.** Se uma classe chama membros virtuais durante a construção ou
depende de estado `protected`, subclasses podem violar invariantes que a base
não controla. Selar reduz essa superfície. O runtime pode aproveitar a ausência
de subclasses, mas isso não garante melhoria sem medição. Veja a seção sobre
`sealed`, herança e pontos de extensão no
[C01](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md).

### Questão 10

**Resposta: A.** Boxing copia o valor para um objeto no heap. O unboxing
verifica se o objeto contém o tipo de valor pedido (ou, no caso de `int?`, o
`int` correspondente); ele não converte entre tipos numéricos. Para obter um `short`, primeiro faça o unboxing para `int` e
depois converta. Veja
[Boxing e unboxing](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md#boxing-e-unboxing).

---

Volte ao [capítulo C01](../linguagem-e-runtime/c01-tipos-identidade-modelagem.md)
ou ao [índice central](../indice.md).
