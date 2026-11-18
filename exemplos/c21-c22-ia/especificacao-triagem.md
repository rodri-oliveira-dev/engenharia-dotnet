# Especificação: encaminhamento de revisão

## Objetivo

Encaminhar uma mudança para a revisão adequada sem permitir que uma sugestão de
IA substitua as regras obrigatórias do pipeline.

## Contrato

Entrada: quantidade não negativa de arquivos alterados, indicador de alteração
de autorização e resultado dos testes. Saída: `RevisaoPadrao`,
`RevisaoSeguranca` ou `CorrigirTestes`.

## Critérios de aceite

- **AC-01:** se os testes falharam, retornar `CorrigirTestes`, inclusive quando a
  mudança altera autorização.
- **AC-02:** com testes aprovados e alteração de autorização, retornar
  `RevisaoSeguranca`.
- **AC-03:** com testes aprovados e sem alteração de autorização, retornar
  `RevisaoPadrao`.
- **AC-04:** rejeitar quantidade negativa de arquivos.

## Rastreabilidade

`PoliticaRevisao` implementa AC-01 a AC-04. Os testes `Ac01` a `Ac04` verificam
cada critério. Alterar a precedência exige atualizar especificação, implementação
e testes na mesma revisão.
