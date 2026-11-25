# Autoavaliação — C02: Nulabilidade, falhas e exceções

Questões de fixação do capítulo
[C02 — Nulabilidade, falhas e exceções](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md).
Cada questão tem uma única alternativa correta. Tente responder antes de
consultar o [gabarito comentado](#gabarito-comentado), no fim desta página.

## Questões

### Questão 1

Um projeto tem `<Nullable>enable</Nullable>` e expõe o método público abaixo
numa biblioteca usada por outros times. O que é correto afirmar sobre o
parâmetro `titulo` em tempo de execução?

<!-- snippet-csharp: id=aval-c02-q01; categoria=contexto -->
```csharp
public static string Normalizar(string titulo) => titulo.Trim();
```

- A. O runtime lança `ArgumentNullException` automaticamente se alguém passar
  `null`, porque o tipo foi declarado sem `?`.
- B. O compilador gera um tipo diferente de `string` para referências não
  anuláveis, e esse tipo rejeita `null`.
- C. A anotação não adiciona validação automática pelo runtime; um chamador
  com NRT desabilitado, reflection ou o operador `!` ainda pode entregar `null`.
- D. Como o projeto tem NRT habilitado, nenhum código, de nenhum projeto,
  consegue compilar uma chamada que passe `null`.

### Questão 2

O que acontece com o código abaixo em um projeto com NRT habilitado?

<!-- snippet-csharp: id=aval-c02-q02; categoria=isolado -->
```csharp
string? entrada = null;
Console.WriteLine(entrada!.Length);
```

- A. O compilador emite um erro, porque `!` não pode ser usado em uma variável
  declarada como `string?`.
- B. O operador `!` verifica o valor e, por ser nulo, faz a expressão retornar
  zero.
- C. O operador `!` converte `null` em string vazia, e o programa imprime `0`.
- D. O código compila sem warning de nulabilidade nessa linha, mas lança
  `NullReferenceException` ao executar.

### Questão 3

No método abaixo, o compilador não emite warning na linha do `return`, embora
`texto` seja `string?`. Por quê?

<!-- snippet-csharp: id=aval-c02-q03; categoria=contexto -->
```csharp
static string Normalizar(string? texto)
{
    if (string.IsNullOrWhiteSpace(texto))
        throw new ArgumentException("Texto obrigatório.", nameof(texto));

    return texto.Trim();
}
```

- A. Porque o compilador ignora a nulabilidade de parâmetros em métodos
  `static`.
- B. Porque `string.IsNullOrWhiteSpace` é anotado na BCL para indicar que o
  argumento não é nulo quando o retorno é `false`, e a análise de fluxo refina
  o estado de `texto`.
- C. Porque `Trim()` aceita receptores nulos e retorna string vazia nesse caso.
- D. Porque qualquer chamada de método que recebe a variável como argumento faz
  o compilador considerá-la não nula dali em diante.

### Questão 4

Um método segue o padrão `Try`. Quando retorna `true`, o parâmetro de saída
sempre contém um texto não vazio. Qual anotação permite que o chamador use
`texto.Length` dentro do `if` sem warning e sem `!`?

<!-- snippet-csharp: id=aval-c02-q04; categoria=invalido -->
```csharp
string? entrada = " Ada ";

static bool TentarNormalizar(string? valor, /* anotação */ out string? normalizado)
{
    normalizado = valor?.Trim();
    return normalizado is { Length: > 0 };
}

if (TentarNormalizar(entrada, out var texto))
    Console.WriteLine(texto.Length);
```

- A. `[AllowNull]`
- B. `[MaybeNull]`
- C. `[DoesNotReturn]`
- D. `[NotNullWhen(true)]`

### Questão 5

Um método `TentarObterCliente` foi anotado com
`[NotNullWhen(true)] out Cliente? cliente`. Em um dos caminhos, porém, ele
retorna `true` e deixa `cliente` como `null`. Qual é a consequência?

- A. O compilador confia na anotação, não emite warning quando o chamador usa
  `cliente` após um retorno `true`, e o erro aparece em runtime.
- B. O runtime verifica a anotação na saída do método e lança uma exceção
  específica de contrato violado.
- C. O compilador detecta a contradição em todos os casos e impede a compilação
  do método.
- D. A anotação é ignorada sempre que existe algum caminho inconsistente, e o
  compilador volta a exigir verificação de `null` no chamador.

### Questão 6

O método auxiliar `Falhar` sempre lança uma exceção. Ainda assim, o compilador
emite warning na linha do `return`. Qual alteração resolve o warning
descrevendo corretamente o comportamento de `Falhar`?

<!-- snippet-csharp: id=aval-c02-q06; categoria=invalido -->
```csharp
Console.WriteLine(Obter("Ada"));

static void Falhar(string mensagem) => throw new InvalidOperationException(mensagem);

static string Obter(string? valor)
{
    if (valor is null)
        Falhar("Valor ausente.");

    return valor;
}
```

- A. Declarar o retorno de `Obter` como `string?`.
- B. Aplicar `[DoesNotReturn]` ao método `Falhar`.
- C. Trocar `return valor;` por `return valor!;`.
- D. Aplicar `[MaybeNull]` ao parâmetro `valor`.

### Questão 7

Uma API recebe pedidos de vários países. A moeda é obrigatória no contrato, mas
às vezes chega ausente por erro do cliente. Um desenvolvedor propõe
`var moeda = dto.Moeda ?? "BRL";`. Qual avaliação está de acordo com o uso
correto de fallback?

- A. Como a moeda é obrigatória, a ausência indica dado incompleto; rejeitar a
  entrada na fronteira preserva o significado, enquanto o fallback pode
  registrar pedidos com a moeda errada.
- B. O fallback é sempre preferível, porque evita exceções e mantém a API
  disponível.
- C. O correto é usar `dto.Moeda!`, porque a moeda é obrigatória e o contrato
  garante sua presença.
- D. O correto é usar `dto?.Moeda`, que propaga a ausência sem alterar o
  significado do pedido.

### Questão 8

No cadastro de usuários, o e-mail já utilizado é um caso frequente, e o
chamador precisa reagir de forma diferente (exibir mensagem, sugerir
recuperação de senha). Entrada malformada continua sendo erro do chamador. Qual
desenho separa melhor essas situações?

- A. Retornar `Cadastro?`, usando `null` tanto para e-mail já utilizado quanto
  para entrada inválida.
- B. Lançar uma exceção específica para e-mail já utilizado e capturá-la no
  chamador para decidir o fluxo.
- C. Retornar um resultado explícito com os casos de sucesso e de e-mail já
  utilizado, e lançar `ArgumentException` para entrada inválida.
- D. Capturar qualquer exceção dentro do método e retornar `false` para o
  chamador.

### Questão 9

Considere o trecho abaixo, em que `cancellationToken` é o token recebido do
chamador e o repositório também pode ser interrompido por um timeout interno.
Qual afirmação descreve corretamente o efeito do filtro `when`?

<!-- snippet-csharp: id=aval-c02-q09; categoria=contexto -->
```csharp
try
{
    await repositorio.SalvarAsync(cliente, cancellationToken);
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    throw;
}
catch (OperationCanceledException ex)
{
    throw new TimeoutException("O salvamento excedeu o tempo limite.", ex);
}
```

- A. Desfazer automaticamente as alterações que já tinham sido confirmadas no
  banco antes do cancelamento.
- B. Converter qualquer timeout em cancelamento solicitado pelo chamador.
- C. O primeiro `catch` só é executado quando o token do chamador já está
  cancelado; uma `OperationCanceledException` que ocorra com esse token ainda
  ativo, como a de um timeout interno, segue para o segundo `catch` e recebe
  outra política.
- D. Impedir que a exceção chegue à fronteira, transformando o cancelamento em
  sucesso.

### Questão 10

Um repositório captura uma exceção do driver do banco e precisa traduzi-la para
uma exceção da aplicação sem perder a evidência necessária para diagnóstico.
Hoje, cada camada também captura, registra e relança a mesma exceção. Qual
abordagem segue as boas práticas?

- A. Em cada camada, capturar `Exception`, registrar a mensagem e lançar uma
  nova exceção apenas com o texto original.
- B. Lançar `new FalhaPersistenciaException("Falha ao salvar cliente.", ex)` no
  repositório e registrar uma única vez na fronteira que conhece a operação.
- C. Capturar a exceção no repositório e retornar `null` para que o chamador
  decida o que fazer.
- D. Registrar a exceção no repositório e usar `throw ex;` para que o stack
  trace aponte para a camada de dados.

## Gabarito comentado

### Questão 1

**Resposta: C.** Nullable Reference Types não criam um novo tipo em runtime:
`string` e `string?` são o mesmo `System.String` para o CLR. A anotação vira
metadado, que bibliotecas como o EF Core podem ler, mas o runtime não valida o
argumento por causa dela. Código sem NRT, reflection, desserialização,
interoperabilidade ou `!` ainda podem passar `null`. Por isso uma API pública
deve validar o argumento quando `null` vindo de fora for um risco real. Veja
[O que Nullable Reference Types faz](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#o-que-nullable-reference-types-faz).

### Questão 2

**Resposta: D.** O operador null-forgiving apenas informa ao compilador que a
expressão deve ser tratada como não nula. Ele não executa verificação nem
altera o valor: o warning some, mas o acesso a `Length` continua acontecendo
sobre `null`. Use-o somente quando houver uma prova que a análise não consegue
representar. Veja
[Anotações e operadores no contexto correto](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#anotações-e-operadores-no-contexto-correto).

### Questão 3

**Resposta: B.** A análise de fluxo reconhece guard clauses, pattern matching,
`is null`, `?? throw` e métodos anotados da BCL. `string.IsNullOrWhiteSpace`
indica que o argumento não é nulo quando retorna `false`; como o ramo `true`
lança exceção, depois do `if` o compilador conhece `texto` como não nulo. Veja
[Análise de fluxo e refinamento de estado](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#análise-de-fluxo-e-refinamento-de-estado).

### Questão 4

**Resposta: D.** `[NotNullWhen(true)]` informa que o argumento não será nulo
quando o método retornar `true`. `[MaybeNull]` indica que a saída pode ser
nula, `[AllowNull]` trata da entrada e `[DoesNotReturn]` descreve métodos que
nunca retornam normalmente. Veja
[Atributos para contratos mais precisos](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#atributos-para-contratos-mais-precisos).

### Questão 5

**Resposta: A.** Atributos de análise são promessas feitas ao compilador, não
verificações de runtime. Uma anotação incorreta é tão perigosa quanto `!`: o
compilador passa a confiar numa promessa falsa. O atributo precisa descrever o
comportamento real em todos os caminhos. Veja
[Atributos para contratos mais precisos](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#atributos-para-contratos-mais-precisos).

### Questão 6

**Resposta: B.** O compilador não analisa o corpo de `Falhar` para concluir que
ele nunca retorna. `[DoesNotReturn]` descreve exatamente isso. Mudar o retorno
para `string?` amplia o contrato sem necessidade, e `!` suprime o warning sem
expressar a prova. Veja
[Atributos para contratos mais precisos](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#atributos-para-contratos-mais-precisos).

### Questão 7

**Resposta: A.** Um fallback com `??` precisa ser semanticamente válido. Trocar
uma moeda ausente por `"BRL"` mascara um dado incompleto e muda o significado
do pedido; quando o dado é obrigatório, o contrato correto é rejeitar a
entrada. Veja
[Anotações e operadores no contexto correto](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#anotações-e-operadores-no-contexto-correto).

### Questão 8

**Resposta: C.** Ausência esperada, recusa de negócio e impossibilidade de
cumprir o contrato são situações diferentes. Um resultado explícito torna a
recusa prevista visível ao chamador, sem usar exceção como controle de fluxo
num caso frequente; argumentos inválidos continuam sendo exceção. Veja
[Ausência, falha esperada e exceção](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#ausência-falha-esperada-e-exceção).

### Questão 9

**Resposta: C.** O filtro avalia o estado do token do chamador no momento da
exceção: se ele está cancelado, o primeiro `catch` relança com `throw;`,
preservando o stack trace e a semântica de cancelamento; caso contrário, a
exceção segue para o segundo `catch`, que a traduz em timeout. O filtro não
prova a origem da exceção: se um token interno disparar no mesmo momento em
que o chamador cancelar, o primeiro `catch` também a aceita. Distinguir as
causas com precisão exige um contrato explícito entre os tokens. Cancelamento
também não desfaz efeitos já confirmados. Veja
[Ausência, falha esperada e exceção](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#ausência-falha-esperada-e-exceção).

### Questão 10

**Resposta: B.** Ao traduzir uma falha de infraestrutura, preserve a exceção
original como `InnerException`. Registrar em todas as camadas duplica logs; o
ideal é registrar uma vez na fronteira que conhece a operação. Retornar `null`
apaga a diferença entre ausência e falha, e `throw ex;` redefine a origem
observável da exceção. Veja
[Ausência, falha esperada e exceção](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md#ausência-falha-esperada-e-exceção).

---

Volte ao [capítulo C02](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md)
ou ao [índice central](../indice.md).
