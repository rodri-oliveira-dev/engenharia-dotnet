# Compatibilidade: C# 14 → C# 15

> **Nível:** Avançado
>
> **Objetivo:** distinguir novos recursos opcionais de
> incompatibilidades reais do compilador e da linguagem.
>
> **Última revisão:** 2026-11-20

C# 15 é a linguagem padrão para `net11.0` no SDK 11.
Não significa que todo código C# 14 deva ser reescrito: mudanças
de compilador podem afetar código existente mesmo sem adoção
explícita de novidades. Os exemplos práticos de union e closed
já aparecem em [C02](../linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md)
e [C11](../dominio-e-arquitetura/c11-entidades-value-objects-agregados.md).

## Matriz de compatibilidade

| Mudança | Tipo | Cenário | Ação |
| --- | --- | --- | --- |
| `with(...)` em expressão de coleção passa a ser argumento de construção | fonte | antes podia invocar método chamado `with` | escrever `@with(...)` quando for chamada |
| `closed` contextual em declaração de tipo | fonte | tipo/alias chamado `closed` | renomear ou usar `@closed` |
| `union` contextual em declaração de tipo | fonte | tipo chamado `union` | renomear ou usar `@union` |
| Contexto seguro de expressão de coleção `Span<T>`/`ReadOnlySpan<T>` ajustado | fonte | `Span` pode escapar de escopo anteriormente aceito | ajustar escopo ou converter via array |
| `ref readonly` e delegates sintetizados exigem `InAttribute` | fonte | targets de referência customizados podem ter CS0518 | garantir assembly com `InAttribute` |
| Interfaces com operadores lógicos e `dynamic` | fonte | lógica antes compilava mas falhava em runtime | usar tipagem explícita ou revisar a sobrecarga |

As notas oficiais do compilador incluem entradas de outras
versões do C# e múltiplos SDKs. **Não classifique entradas
explicitamente marcadas como C# 16 como breaking changes
de C# 15.**

## Exemplo 1: `with` em coleção

**Antes (C# 14):** `with(x, y)` dentro da coleção podia
invocar uma função com esse nome.

<!-- snippet-csharp: id=compat-cs15-01; categoria=contexto -->
```csharp
int with(int a, int b) => a + b;

// Quando a intenção é a chamada do método:
int[] valores = [@with(2, 3), 10];
```

**Agora (C# 15):** `with(...)` no início de uma
expressão de coleção representa parâmetros de construção;
a chamada literal do método requer `@with(...)`.

<!-- snippet-csharp: id=compat-cs15-02; categoria=contexto -->
```csharp
HashSet<string> nomes =
    [with(StringComparer.OrdinalIgnoreCase), "Ana", "ANA"];
```

O segundo exemplo demonstra o **novo recurso**, não um
breaking change por si só. Valide comparadores, capacidades
e semântica antes de adotar em código de produção.

## Exemplo 2: nomes `closed` e `union`

**Antes:** identificadores com esses nomes podiam aparecer
em posições que agora são interpretadas como declarações.

**Depois:**

<!-- snippet-csharp: id=compat-cs15-03; categoria=contexto -->
```csharp
class @closed { }
class @union { }

class Exemplo
{
    private @closed? _estado;
    private @union? _alternativa;
}
```

Se bibliotecas públicas expõem esses identificadores, avalie
API e compatibilidade binária antes de renomear. Escapar o
identificador é a menor alteração de código-fonte.

## Exemplo 3: segurança de referência em spans

O compilador corrigiu o escopo seguro de expressões de coleção
alvo `Span<T>`/`ReadOnlySpan<T>`: atribuições que deixam
um valor escapar do bloco podem passar a ser rejeitadas.

**Antes:** certos valores temporários compilavam apesar do
escopo inseguro em termos das regras da linguagem.

**Depois:** prefira conter o `Span` em seu escopo seguro ou,
quando a retenção fora do bloco é legítima, use array
com tempo de vida explícito:

<!-- snippet-csharp: id=compat-cs15-04; categoria=contexto -->
```csharp
int[] dados = [1, 2, 3];
ReadOnlySpan<int> leitura = dados;
Console.WriteLine(leitura[0]);
```

Não resolva problemas de escopo simplesmente adicionando
`unsafe`. Reavalie a propriedade de memória e o contrato
de tempo de vida.

## Recursos que não são breaking changes por padrão

C# 15 também introduz tipos de união, hierarquias fechadas,
indexadores de extensão, `break`/`continue` com rótulos
e membros estáticos não virtuais de interfaces em runtimes
anteriores. Eles são oportunidades de modelagem, não
correções de migração obrigatórias. A evolução de segurança
de memória permanece **preview** e requer configuração
explícita; o repositório não habilita preview globalmente.

## Plano de verificação

1. Fixar o SDK 11.0.100 em `global.json`; confirmar
   versão de linguagem efetiva na compilação.
2. Comparar build de uma branch de referência em C# 14
   e da branch `net11.0`, observando novos erros/warnings.
3. Procurar métodos chamados `with` usados em
   expressões de coleção e identificadores `closed`/`union`.
4. Inspecionar uso de `Span<T>` que possa escapar de escopo.
5. Avaliar erros `CS0518` e `CS7083` apenas se surgirem
   nos projetos e nas plataformas utilizadas.
6. Não inserir código intencionalmente inválido em testes de
   compilação normais: exemplos negativos devem rodar em
   um projeto isolado que espere falha e confira diagnóstico.

**Evidência no repositório:** C02, C03 e C11 já
exercitam recursos novos de C# 15 em testes de sucesso.
Este guia não declara que o pipeline atual compara dois
compiladores; essa comparação deve ocorrer em matriz
se houver necessidade real.

## Referências

- [O que há de novo no C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)
- [Breaking changes do compilador entre SDK 10 e 11](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011)
- [Regras de ref safety](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters)

[Voltar à trilha](indice.md).
