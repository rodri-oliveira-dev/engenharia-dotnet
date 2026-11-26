# Diretrizes editoriais

## Propósito e público

Este repositório é um guia público de referência para profissionais que desenvolvem, projetam e operam sistemas com C# e .NET. Não é curso linear, banco de perguntas, preparação para entrevistas nem catálogo de definições.

O leitor deve conseguir chegar por um problema real, compreender os fundamentos, avaliar alternativas e sair com critérios de decisão. Todo o conteúdo autoral será escrito em português. Termos técnicos consolidados em inglês podem ser preservados, com explicação em português na primeira ocorrência.

## Unidade editorial

Cada capítulo deve responder, em prosa técnica, às seguintes dimensões:

1. **Problema e contexto:** qual tensão de engenharia motivou o conceito.
2. **Modelo mental:** o conceito, seu vocabulário e as garantias que oferece.
3. **Funcionamento técnico:** mecanismos relevantes, sem esconder detalhes que alteram decisões.
4. **Aplicação:** cenários realistas, pré-condições e exemplos em C# quando pertinentes.
5. **Decisão:** quando usar, quando evitar e alternativas plausíveis.
6. **Riscos:** erros comuns, modos de falha, limitações e custo operacional.
7. **Verificação:** como testar, medir ou observar a solução.
8. **Conexões:** pré-requisitos e capítulos relacionados.
9. **Referências:** fontes oficiais e materiais primários que sustentam afirmações importantes.

Essas dimensões não impõem títulos idênticos a todos os capítulos. A estrutura deve servir ao assunto, mantendo previsibilidade suficiente para consulta.

## Estrutura recomendada de capítulo

```text
# Título orientado ao conceito

Resumo curto
## O problema
## Modelo mental e funcionamento
## Exemplo ou cenário
## Alternativas e trade-offs
## Riscos e erros comuns
## Quando usar e quando evitar
## Como verificar em produção ou em testes
## Relações com outros capítulos
## Referências
```

Para capítulos com laboratório, acrescentar **Como executar** e **Limitações do exemplo**. Para estudos de caso, começar pelos sintomas e evidências, separar investigação de correção e apontar os capítulos que contêm os fundamentos.

## Profundidade e progressão

- **Fundamental:** definir termos, desfazer ambiguidades e estabelecer contratos. Não pressupor experiência com a técnica específica.
- **Intermediário:** pressupor os fundamentos explicitamente listados e mostrar composição em cenários de produção.
- **Avançado:** apresentar forças concorrentes, falhas parciais, custo operacional e critérios baseados em medição. Não usar “avançado” como licença para complexidade gratuita.

Cada capítulo declara seu nível e pré-requisitos conforme o mapa editorial. A dificuldade decorre do problema, não da quantidade de padrões ou ferramentas citadas.

## Exemplos em C# e .NET

Um exemplo precisa demonstrar uma decisão, risco ou comportamento observável. Deve:

- compilar na versão de .NET declarada pelo repositório;
- ser pequeno o bastante para leitura e completo o bastante para não esconder a parte difícil;
- incluir testes quando o comportamento puder ser verificado automaticamente;
- mostrar cancelamento, falhas e descarte de recursos quando relevantes;
- evitar credenciais, endpoints privados e dependências sem função didática;
- declarar simplificações que seriam inadequadas em produção;
- acompanhar o texto: mudanças conceituais exigem atualização do exemplo.

Não usar benchmark improvisado para alegações de performance. Registrar ambiente, baseline e metodologia. Não substituir banco relacional por provider em memória quando o objetivo for validar tradução SQL ou semântica relacional.

## Linguagem e tom

- Preferir frases diretas, termos precisos e exemplos concretos.
- Diferenciar garantia, convenção, recomendação e preferência.
- Evitar “sempre”, “nunca”, “melhor” e “mais rápido” sem condições e evidência.
- Explicar siglas na primeira ocorrência e manter um único termo para cada conceito.
- Usar voz impessoal ou primeira pessoa do plural apenas quando melhora a leitura.
- Não conservar a formulação interrogativa da planilha como estrutura do texto.
- Não atribuir maturidade, senioridade ou qualidade a uma pessoa com base no uso de uma técnica.

## Trade-offs e decisões

Toda recomendação relevante deve explicitar:

- contexto e restrições;
- benefício esperado;
- custo de implementação e manutenção;
- impacto em desempenho e operação, quando houver;
- modos de falha;
- alternativa mais simples;
- sinal que justificaria reavaliar a decisão.

Padrões não são objetivos. Citar um padrão apenas quando ele nomeia com precisão uma solução e ajuda a discutir consequências. Tecnologias só entram quando sustentam um conceito do mapa, um exemplo necessário ou uma comparação justificada; a introdução deve registrar essa justificativa.

## Fontes e referências

Priorizar, nesta ordem:

1. documentação oficial da linguagem, runtime, framework ou protocolo;
2. especificações e padrões publicados por suas organizações mantenedoras;
3. artigos técnicos primários dos autores ou equipes responsáveis;
4. livros e artigos reconhecidos, usados com contexto e data.

Antes de publicar, abrir e conferir cada referência. Não citar uma URL por memória. Informar data de acesso para conteúdo sujeito a mudança e versão quando o comportamento depender dela. Fontes secundárias podem enriquecer uma análise, mas não devem ser a única base de afirmações sobre comportamento do runtime ou de APIs.

A planilha é fonte de pauta e rastreabilidade, não autoridade técnica. O vínculo entre pergunta e tema permanece em `inventario.md`; o capítulo publicado usa documentação técnica atual e não reproduz respostas de entrevista.

## Redundância e links

Um conceito possui um capítulo canônico. Outros capítulos devem resumir apenas o necessário e criar link para a explicação completa. Antes de redigir:

1. consultar o inventário e o mapa editorial;
2. identificar o capítulo canônico de cada fundamento;
3. registrar novas sobreposições percebidas;
4. preferir links com contexto a copiar seções;
5. atualizar links bidirecionais quando uma relação for material.

Exemplos: idempotência é canônica em C16 e aplicada em C31; telemetria é canônica em C27 e consumida por C25, C28, C31 e C32; índices e transações pertencem a C10 e são referenciados pelo conteúdo de EF Core.

## Revisão técnica e editorial

Um capítulo está pronto para revisão quando:

- o escopo corresponde ao mapa e os pré-requisitos estão linkados;
- afirmações técnicas relevantes têm fonte verificável;
- exemplos compilam e testes passam;
- alternativas, limitações e quando evitar estão presentes;
- não há perguntas e respostas disfarçadas;
- não duplica conteúdo canônico sem justificativa;
- termos, títulos e links seguem a taxonomia;
- riscos de segurança e operação foram considerados;
- o texto foi revisado integralmente em português.

Na revisão, separar comentários de correção factual, clareza, escopo e preferência. Divergências arquiteturais devem ser resolvidas com contexto, evidências e registro de trade-offs, não por autoridade.

## Manutenção

Cada capítulo deve declarar no histórico de mudanças quando uma atualização altera comportamento, recomendação ou compatibilidade de versão. Conteúdo obsoleto não deve ser silenciosamente reescrito quando a decisão anterior ainda for relevante: registrar a mudança e, quando necessário, um ADR editorial.

O mapa editorial é vivo. Novos capítulos exigem demonstração de lacuna, fonte ou demanda real, relação com a taxonomia e análise de sobreposição. Novas perguntas da planilha devem primeiro entrar no inventário com linha, grupo, tema normalizado e destino editorial.

Propostas e mudanças estruturais seguem a skill
`governar-evolucao-editorial`. O registro da decisão pode permanecer na issue ou
no pull request; seu propósito é demonstrar posicionamento consciente, não criar
documentação cerimonial.

## Fora de escopo

- respostas modelo para entrevistas;
- listas extensas de definições sem aplicação;
- tutoriais sequenciais que dependam da leitura integral do repositório;
- comparações de ferramentas sem problema concreto;
- conteúdo gerado para atingir quantidade de páginas;
- referências não verificadas ou alegações de performance sem medição;
- reprodução de conteúdo protegido ou de fontes sem atribuição.
