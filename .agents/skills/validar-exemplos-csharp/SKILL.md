---
name: validar-exemplos-csharp
description: >-
  Validar compilação, nulabilidade, semântica, exceções e saídas de
  exemplos C# e laboratórios .NET do Engenharia .NET. Use ao criar
  ou revisar snippets em capítulos, questionários e arquivos .cs.
---

# Validar exemplos de C# e .NET

Siga `AGENTS.md` e `docs/exemplos-compilaveis.md`.

## Entenda o contexto

1. Identifique qual afirmação, questão ou comportamento o trecho ilustra.
2. Classifique-o como programa completo, fragmento contextualizado,
   pseudocódigo ou erro deliberado.
3. Liste tipos auxiliares, imports, estrutura de programa e versão
   necessários. Não presuma que um fragmento compila isolado.
4. Ao testar fragmentos, use um invólucro mínimo e documente o que
   foi acrescentado; não altere a semântica original.

## Compilação e execução

- Confira `global.json` e `TargetFramework` antes de escolher o SDK.
- Se seguro e viável, compile em projeto temporário isolado; não
  crie um laboratório permanente apenas para conferir um snippet.
- Verifique Nullable Reference Types, avisos e erros separadamente.
- Confira ordem de top-level statements em relação a tipos
  (`CS8803`), resolução de nomes, genéricos, sintaxe, conversões
  e APIs disponíveis na versão definida.
- Compare saída, exceção, retorno e efeitos com o que o texto afirma.
- Cubra casos importantes: `null`, igualdade, referências compartilhadas,
  mutabilidade, boxing, coleções vazias e limites numéricos.
- Em async, diferencie cancelamento externo, interno e timeout; o
  estado de um token não identifica necessariamente a causa da exceção.
- Não deduza comportamento específico de provider ou performance
  sem experimentação adequada.
- Só execute exemplos seguros e isolados; não use rede, produção,
  credenciais, operações destrutivas ou serviços reais sem autorização.
- Para mudanças de código, prefira testes determinísticos nos
  laboratórios existentes.

## Relatório por exemplo

Registre local, contexto/invólucro, estado da compilação, saída
observada ou motivo de não executar, divergência e ajuste proposto.

Se `dotnet` não estiver disponível, faça revisão estática e declare
expressamente que não houve compilação. Não alegue ter validado
todos os trechos quando apenas uma amostra foi verificada.
