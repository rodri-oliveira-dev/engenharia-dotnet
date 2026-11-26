# Linguagem e runtime

Esta parte estabelece a semântica sobre a qual o restante da obra raciocina:
tipos, ausência, coleções, execução, memória e concorrência. O objetivo não é
colecionar recursos da linguagem, mas compreender contratos que permanecem
válidos quando o código passa a atender requisições, persistir dados e operar sob
carga.

Não há pré-requisito obrigatório. Ao concluir o percurso, o leitor deve conseguir
explicar identidade e igualdade, representar falhas, reconhecer execução
diferida, controlar trabalho concorrente e medir custos do runtime. C05 antecede
C04 na leitura porque concorrência e assincronismo são necessárias em várias
partes da obra, enquanto performance exige uma base mais ampla e evidência.

Esses fundamentos reaparecem nas fronteiras HTTP e de dados da próxima parte.
Quem consultar um capítulo isoladamente pode seguir apenas os pré-requisitos
indicados nele.

## Capítulos publicados

- **C01 · Fundamental · Publicado:**
  [Tipos, identidade e modelagem em C#](c01-tipos-identidade-modelagem.md).
- **C02 · Fundamental · Publicado:**
  [Nulabilidade, falhas e exceções](c02-nulabilidade-falhas-excecoes.md).
- **C03 · Fundamental · Publicado:**
  [Coleções, LINQ e execução diferida](c03-colecoes-linq-execucao-diferida.md).
- **C05 · Intermediário · Publicado:**
  [Assincronicidade e concorrência controlada](c05-assincronicidade-concorrencia-controlada.md).
- **C04 · Avançado · Publicado:**
  [Memória, runtime e engenharia de performance](c04-memoria-runtime-performance.md).

## Compatibilidade de versões

- [Runtime, BCL e Hosting no .NET 11](../compatibilidade/compatibilidade-runtime-bcl-11.md).
- [C# 15 e mudanças do compilador](../compatibilidade/compatibilidade-csharp-15.md).

Consulte o [mapa editorial](../planejamento/mapa-editorial.md) para escopo,
dependências e estratégia de exemplos. Volte ao [índice central](../indice.md).
