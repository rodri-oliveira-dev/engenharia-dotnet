# C02 — Nulabilidade, falhas e exceções

> **Nível:** Fundamental
>
> **Objetivo:** expressar ausência e falha nos contratos, usar a análise de
> nulabilidade como feedback de design e reservar exceções para fronteiras
> adequadas.
>
> **Pré-requisitos:** [C01 — Tipos, identidade e modelagem em C#](c01-tipos-identidade-modelagem.md).
>
> **Última revisão:** 2026-11-18

Falhas de referência nula raramente começam na linha que lança
`NullReferenceException`. Elas começam quando uma API não informa se um valor
pode estar ausente, quando uma fronteira aceita entrada inválida ou quando um
aviso do compilador é silenciado sem que o estado tenha sido provado.

Nullable Reference Types (NRT) permitem declarar e verificar parte desse
contrato durante a compilação. Eles não validam dados externos, não impedem
`null` em runtime e não substituem uma estratégia de falhas. Este capítulo liga
as anotações de nulabilidade a validação, resultados esperados e exceções.

## Ausência precisa fazer parte do contrato

Considere uma busca de cliente. Três contratos diferentes são plausíveis:

```csharp
Cliente Obter(Guid id);             // promete um cliente ou não retorna normalmente
Cliente? Encontrar(Guid id);        // ausência é um resultado previsto
bool TentarObter(Guid id, out Cliente? cliente); // ausência é comunicada por bool
```

Nenhuma assinatura é universalmente correta. `Obter` pode lançar uma exceção ou
usar outro mecanismo quando o contrato for violado. `Encontrar` obriga o
chamador a tratar a ausência. `TentarObter` é útil quando o padrão `Try` torna o
fluxo mais claro e pode incluir atributos que refinam o estado do parâmetro.

O problema aparece quando a implementação retorna `null` sob uma assinatura que
promete `Cliente`, ou quando toda operação retorna `Cliente?` apenas para evitar
decidir o contrato. NRT torna essa inconsistência visível mais cedo.

## O que Nullable Reference Types faz

Com `<Nullable>enable</Nullable>`, uma referência sem `?`, como `string`, é
anotada como não anulável; `string?` admite `null`. O compilador acompanha o
*null-state* de expressões como **not-null** ou **maybe-null** e emite warnings
quando atribuições ou desreferências contradizem o contrato.

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

```csharp
static int Tamanho(string texto) => texto.Length;
static int TamanhoOpcional(string? texto) => texto?.Length ?? 0;
```

As anotações não criam um novo tipo em runtime. Para manter compatibilidade com
código anterior, um consumidor ainda pode passar `null` por reflection, código
com NRT desabilitado, interoperabilidade, desserialização ou pelo operador `!`.
Por isso, APIs públicas devem validar argumentos quando `null` vindo de fora é
um risco real, mesmo que a assinatura seja não anulável.

NRT foi introduzido em C# 8. Projetos modernos normalmente o habilitam, mas cada
projeto e arquivo pode ter contexto diferente. Ao migrar legado, ative análise
por partes em vez de adicionar centenas de supressões sem investigar.

## Anotações e operadores no contexto correto

### `?`: ausência permitida

Em `string?`, o sufixo anota uma referência que pode ser nula. Em `int?`, ele é
uma forma abreviada de `Nullable<int>`, um tipo por valor que representa um
inteiro ou ausência. A sintaxe é parecida, mas o mecanismo de runtime é
diferente.

Use `?` quando a ausência tiver significado no domínio ou na fronteira. Não o
adicione automaticamente a todo membro que gera warning: isso amplia o contrato
e transfere tratamento para todos os consumidores.

### `?.` e `?[]`: acesso condicional

O operador condicional nulo interrompe a cadeia e produz `null` se o receptor
for nulo:

```csharp
int? quantidade = pedido?.Itens?.Count;
```

Ele é apropriado quando a ausência deve se propagar. Pode esconder um contrato
quebrado se o objeto deveria obrigatoriamente existir; nesse caso, validar ou
corrigir a origem é mais informativo.

### `??` e `??=`: fallback explícito

O operador de coalescência escolhe o operando direito quando o esquerdo é nulo.
O operador `??=` atribui um fallback somente nessa condição.

```csharp
var nomeDeExibicao = apelido?.Trim() is { Length: > 0 } valor
    ? valor
    : nome;

cache ??= new Dictionary<string, Cliente>();
```

Um fallback precisa ser semanticamente válido. Trocar uma moeda ausente por
`"BRL"`, por exemplo, pode mascarar um dado incompleto; rejeitar a entrada pode
ser o contrato correto.

### `!`: supressão, não validação

O operador null-forgiving informa ao compilador que a expressão deve ser
tratada como não nula. Ele não executa verificação e não altera o valor:

```csharp
string? entrada = null;
Console.WriteLine(entrada!.Length); // compila; falha em runtime
```

Use-o somente quando houver uma prova que a análise não consegue representar,
por exemplo em um teste que deliberadamente passa `null`, ou na integração com
um framework cujo ciclo de inicialização é conhecido e documentado. Deixe a
prova próxima da supressão. Repetir `!` para “limpar” warnings destrói o valor da
análise e desloca falhas para produção.

## Análise de fluxo e refinamento de estado

O compilador refina o estado depois de verificações reconhecidas:

```csharp
static string Normalizar(string? texto)
{
    if (string.IsNullOrWhiteSpace(texto))
        throw new ArgumentException("Texto obrigatório.", nameof(texto));

    return texto.Trim(); // aqui o compilador conhece texto como não nulo
}
```

Guard clauses, pattern matching, `is null`, `is not null`, `?? throw` e alguns
métodos anotados da Base Class Library participam dessa análise. A análise é
conservadora: mutação, aliases, propriedades sobrescrevíveis e métodos que o
compilador não conhece podem impedir o refinamento.

Evite contornar isso lendo repetidamente uma propriedade mutável. Capture uma
vez e valide a variável local:

```csharp
var endereco = pedido.EnderecoDeEntrega;
if (endereco is null)
    return;

EnviarPara(endereco);
```

## Atributos para contratos mais precisos

`System.Diagnostics.CodeAnalysis` oferece atributos quando `?` sozinho não
expressa a relação entre entrada, saída e retorno:

- `[NotNullWhen(true)]` informa que um argumento não será nulo quando o método
  retornar `true`;
- `[MaybeNull]` informa que uma saída pode ser nula mesmo quando seu tipo genérico
  não permite escrever `?` de forma útil;
- `[NotNull]` e `[AllowNull]` separam estados de entrada e saída;
- `[MemberNotNull]` informa quais membros foram inicializados por um método;
- `[DoesNotReturn]` descreve um método que nunca retorna normalmente.

```csharp
using System.Diagnostics.CodeAnalysis;

static bool TentarNormalizar(
    string? valor,
    [NotNullWhen(true)] out string? normalizado)
{
    normalizado = valor?.Trim();
    return normalizado is { Length: > 0 };
}

if (TentarNormalizar(entrada, out var texto))
    Console.WriteLine(texto.Length); // contrato prova que texto não é nulo
```

O atributo deve descrever o comportamento real em todos os caminhos. Uma
anotação incorreta é tão perigosa quanto `!`: o compilador passa a confiar em
uma promessa falsa.

## Inicialização e propriedades não anuláveis

Um membro não anulável deve estar inicializado quando a construção termina.
Prefira construtores, parâmetros posicionais ou propriedades `required`:

```csharp
public sealed class ServicoEmail
{
    private readonly IClienteEmail _cliente;

    public ServicoEmail(IClienteEmail cliente) =>
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
}
```

`required` exige que o inicializador informe o membro, mas não impede que código
sem análise de nulabilidade atribua `null`. Desserializadores e Object-Relational
Mappers (ORMs) podem ter ciclos próprios. Configure suas integrações e valide a
fronteira em vez de inicializar membros com `null!` indiscriminadamente.

## Warnings são divergências de contrato

Trate um warning pela causa, nesta ordem:

1. confirme se ausência é válida para o modelo;
2. ajuste a assinatura (`T` ou `T?`) para declarar o contrato verdadeiro;
3. valide dados vindos de HTTP, banco, arquivo, mensagem ou interoperabilidade;
4. reorganize o fluxo para que o compilador reconheça a prova;
5. use um atributo de análise quando houver uma relação legítima mais rica;
6. use `!` apenas se a prova externa estiver documentada e testada.

Corrigir o warning com uma checagem redundante pode ser tão ruim quanto
suprimi-lo. Se um método privado recebe `string` de todos os chamadores e a
invariante é confiável, adicionar `if (texto is null)` em cada linha não melhora
o desenho. Valide nas fronteiras e mantenha o núcleo com contratos fortes.

Em bibliotecas, warnings de nulabilidade fazem parte da experiência de consumo.
Alterar `T` para `T?` ou o inverso pode ser uma quebra de contrato para análise
estática mesmo quando a assinatura do Common Language Runtime (CLR) permanece
compatível. Revise isso como mudança de API.

## Ausência, falha esperada e exceção

Nulabilidade representa ausência, não todo tipo de falha. Separe três situações:

- **ausência esperada:** `Cliente?`, `TryGetValue` ou um tipo de opção podem ser
  suficientes;
- **resultado de negócio recusado:** um resultado explícito com código e detalhes
  permite ao chamador reagir sem exceção como controle de fluxo;
- **operação que não pode cumprir o contrato:** exceção comunica que o caminho
  normal não produziu resultado.

```csharp
public abstract record ResultadoCadastro
{
    public sealed record Sucesso(Guid ClienteId) : ResultadoCadastro;
    public sealed record EmailJaUtilizado(string Email) : ResultadoCadastro;
}
```

Um resultado explícito aumenta tipos e ramificações, mas torna falhas previstas
exaustivas e visíveis. Para uma consulta simples, `T?` pode ser a alternativa
mais clara. Exceções continuam apropriadas para argumento inválido, falha de I/O
ou violação de uma promessa que impede o método de concluir.

### C# 15: resultado de negócio com union

O C# 15 permite declarar uma união de casos existentes. Em vez de aceitar
`Cadastro?` como se ausência e e-mail duplicado fossem a mesma coisa, um
registro pode devolver explicitamente sucesso ou recusa:

```csharp
public sealed record CadastroRegistrado(Cadastro Cadastro);
public sealed record EmailEmUso(string Email);

public union ResultadoRegistro(CadastroRegistrado, EmailEmUso);

string Descrever(ResultadoRegistro resultado) => resultado switch
{
    CadastroRegistrado(var cadastro) => cadastro.Email,
    EmailEmUso(var email) => $"Já cadastrado: {email}"
};
```

A conversão de cada caso para a união é implícita; o `switch` exige cobrir
os casos declarados. O laboratório implementa `RegistroComResultado.Registrar`,
sem alterar o contrato anterior de `CadastroFactory.Criar`: entrada inválida
continua lançando `ArgumentException`, enquanto e-mail já registrado é uma
recusa de negócio. O custo é criar tipos e exigir tratamento explícito;
para uma ausência simples, `T?` ainda costuma ser suficiente.

**Serialização:** os dois casos são objetos JSON. Para desserializá-los,
o laboratório usa `[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]`
e propriedades de raiz distintas (`Cadastro` e `Email`). Sem um discriminador
ou classificador adequado, dois objetos JSON parecidos podem ser ambíguos.
`default(ResultadoRegistro)` não representa sucesso: a união gerada é um
`struct` cujo valor padrão não contém um caso ativo. Não publique essa
representação como contrato HTTP sem especificar como erros e versões
são representados.

Referências: [Tipos de união no C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union)
e [serialização de uniões no System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types).

### Preserve a informação da exceção

Capture onde for possível acrescentar contexto, recuperar ou traduzir na
fronteira. Se precisar relançar a mesma exceção, use `throw;`, que preserva o
stack trace; `throw ex;` redefine sua origem observável.

```csharp
try
{
    await repositorio.SalvarAsync(cliente, cancellationToken);
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    throw; // preserva o stack trace e a semântica de cancelamento
}
```

Não capture `Exception` apenas para registrar e relançar em todas as camadas:
isso tende a duplicar logs. Registre na fronteira que conhece a operação e a
resposta; preserve a exceção original como `InnerException` ao traduzi-la.

### Trate na fronteira que pode decidir

Uma captura é útil quando existe uma decisão concreta: recuperar com uma
alternativa segura, traduzir uma falha de infraestrutura para um contrato da
aplicação, liberar recursos ou formar a resposta da fronteira. Capturar todas as
exceções em uma camada intermediária e retornar `null`, `false` ou sucesso apaga
a diferença entre ausência prevista, cancelamento e corrupção de estado.

Na fronteira HTTP, worker ou comando, registre uma vez com identificador de
correlação, operação, dependência e contexto não sensível. Preserve tipo, stack
trace e `InnerException`; métricas devem distinguir falha, timeout e
cancelamento solicitado pelo chamador. Logs sem a exceção ou com mensagens
recriadas perdem a evidência necessária para diagnóstico.

`CancellationToken` faz parte desse contrato. Encaminhe o token até o I/O e não
capture `OperationCanceledException` indiscriminadamente como erro. Quando a
exceção corresponde ao token esperado, a fronteira pode representar
cancelamento; quando outro timeout ou token interno disparou, a política pode
ser diferente. Cancelamento não desfaz efeitos já confirmados.

## Exemplo de fronteira validada

O laboratório recebe dados anuláveis, pois simula uma fronteira externa, e cria
um record com propriedades não anuláveis somente depois da validação:

```csharp
public sealed record Cadastro(string Nome, string Email);

public static Cadastro Criar(string? nome, string? email)
{
    var nomeValido = Normalizar(nome, "nome");
    var emailValido = Normalizar(email, "email");
    return new Cadastro(nomeValido, emailValido);
}
```

O núcleo não precisa carregar `string?` depois que a fronteira estabeleceu a
invariante. Isso reduz verificações espalhadas e torna estados inválidos menos
representáveis.

## Quando aplicar e quando evitar

Habilite NRT em código novo e trate warnings como defeitos de contrato. Em um
legado grande, migre por projeto ou arquivo, começando pelas fronteiras e APIs
compartilhadas. Uma ativação global que apenas adiciona `?` e `!` pode criar uma
falsa sensação de segurança.

Use `T?` quando ausência for válida e o consumidor precisar reconhecê-la. Use
fallback com `??` quando existir um padrão correto para o negócio. Use atributos
quando uma API `Try` ou genérica tiver relações que a sintaxe básica não captura.
Evite `!` quando uma guarda, assinatura ou anotação puder expressar a prova.

Use resultados explícitos para falhas previstas que orientam decisões do
chamador. Evite criar uma hierarquia de resultados para uma ausência trivial.
Use exceções quando o método não puder cumprir seu contrato; evite-as como
ramificação rotineira em um fluxo esperado e frequente.

## Armadilhas comuns

- Acreditar que `string` não pode ser `null` em runtime.
- Marcar toda referência como `?` para silenciar warnings.
- Usar `!` depois de desserialização ou resolução de dependência sem validar a
  configuração real.
- Usar `?.` em uma invariante obrigatória e converter uma falha em silêncio.
- Escolher um fallback que muda o significado do dado ausente.
- Anotar `[NotNullWhen]` sem cumprir a promessa em todos os retornos.
- Expor propriedades não inicializadas com `null!` e depender de uma ferramenta
  externa sem teste de integração.
- Capturar, registrar e relançar a mesma exceção em várias camadas.
- Usar `throw ex;` e perder a origem original do stack trace.
- Converter qualquer exceção em `null` ou `Result` genérico, perdendo causa,
  pilha e distinção entre falha e ausência.
- Registrar cancelamento cooperativo como erro interno sem verificar o token que
  originou a interrupção.

## Como verificar

O laboratório em `exemplos/c02-nulabilidade/` habilita NRT e transforma warnings
em erros. Os testes verificam validação da fronteira, coalescência e o refinamento
produzido por `[NotNullWhen(true)]`.

```bash
dotnet test exemplos/c02-nulabilidade/C02.Nulabilidade.Tests.csproj
```

O projeto usa .NET 10, não acessa serviços externos e testa apenas contratos
locais. Desserializadores, ORMs e APIs HTTP exigem testes de integração próprios.

## Relação com outros capítulos

- **Pré-requisito:** [C01 — Tipos, identidade e modelagem em C#](c01-tipos-identidade-modelagem.md)
  estabelece tipos por valor e referência, records e imutabilidade.
- **Aprofundamentos:** [C03 — Coleções, LINQ e execução diferida](c03-colecoes-linq-execucao-diferida.md)
  trata ausência em coleções e consultas;
  [C05 — Assincronicidade e concorrência controlada](c05-assincronicidade-concorrencia-controlada.md)
  aplica propagação de falha e cancelamento a `Task`, `WhenAll` e pipelines.
- **Aplicações:** [C06 — Composição e pipeline no ASP.NET Core](../aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md)
  aplica validação na composição; [C07 — APIs HTTP e integrações resilientes](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md)
  traduz falhas para contratos HTTP;
  [C09](../aplicacoes-e-dados/c09-fronteiras-concorrencia-ef-core.md) trata
  fronteiras de persistência.

## Autoavaliação

Teste seu entendimento com as
[questões de autoavaliação do C02](../autoavaliacao/c02-nulabilidade-falhas-excecoes.md),
que aplicam Nullable Reference Types, atributos de análise, fallback, resultados
explícitos e tratamento de exceções a cenários concretos, com gabarito
comentado.

## Referências

- [Nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [Referência de tipos de referência anuláveis](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/nullable-reference-types)
  — referência de C#, acesso em 18 novembro 2026.
- [Warnings de referências anuláveis](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/nullable-warnings)
  — referência de C#, acesso em 18 novembro 2026.
- [Atributos para análise estática de estado nulo](https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/nullable-analysis)
  — referência de C#, acesso em 18 novembro 2026.
- [Operadores `??` e `??=`](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/null-coalescing-operator)
  — referência de C#, acesso em 18 novembro 2026.
- [Operadores `?.` e `?[]`](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-)
  — referência de C#, acesso em 18 novembro 2026.
- [Exceções e boas práticas](https://learn.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions)
  — documentação .NET, acesso em 18 novembro 2026.
