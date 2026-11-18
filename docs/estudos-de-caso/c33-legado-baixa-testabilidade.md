# C33 — Sistema legado com baixa testabilidade

> **Nível:** Intermediário
>
> **Objetivo:** criar proteção de comportamento e abrir seams para refatorar um
> legado em fatias compatíveis com o risco.
>
> **Pré-requisitos:** [C13 — Dependências e abstrações](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md),
> [C23 — Testes orientados a comportamento](../qualidade-e-seguranca/c23-testes-comportamento-risco.md)
> e [C24 — Integração real e testes incrementais de legado](../qualidade-e-seguranca/c24-integracao-real-testes-legado.md).
>
> **Última revisão:** 2026-11-18

Este cenário é didático e fictício. “Legado” descreve software valioso sob
restrições e conhecimento incompleto; não é julgamento sobre quem o construiu.

## Cenário

Um serviço central mistura regras, SQL, relógio, arquivos e chamadas HTTP em
métodos extensos. Mudanças pequenas exigem validação manual e geram receio. A
equipe precisa alterar uma regra sem financiar uma reescrita completa.

## Contexto técnico

A aplicação .NET possui poucos testes, ambiente compartilhado e comportamento
parcialmente documentado por tickets. Algumas saídas estranhas podem ser
contratos dos consumidores. Introduzir interfaces em toda classe criaria muita
mudança sem evidência de benefício.

## Sintomas ou problema

Tempo de mudança e regressões concentram-se no módulo. Testes tentados exigem
banco, rede, data atual e ordem específica. Não está claro quais comportamentos
são intencionais, acidentes tolerados ou defeitos conhecidos.

## Restrições

- o serviço continua recebendo correções;
- produção não pode ser usada como massa de teste;
- banco e integrações têm semântica que mocks não reproduzem;
- a nova regra tem prazo, mas perda de comportamento seria mais cara;
- conhecimento está distribuído entre código, suporte e usuários.

## Hipóteses

1. A principal fragilidade vem de dependências globais e tempo não controlado.
2. O risco está na persistência/serialização, não na regra candidata.
3. Testes de alto nível podem congelar defeitos irrelevantes e impedir melhoria.
4. Um seam na fronteira da nova regra basta; refatoração ampla não é necessária.
5. Falhas recorrentes apontam um fluxo crítico que merece caracterização primeiro.

## Processo de investigação

Mapeie chamadas e dados do fluxo alterado, consulte incidentes e pessoas que o
operam e escolha exemplos reais anonimizados. Execute o comportamento atual em
ambiente isolado, capture entradas e saídas observáveis e confirme com o negócio
o que precisa ser preservado. Marque explicitamente anomalias conhecidas.

Adicione testes de caracterização na menor fronteira estável: API pública ou
componente com banco real isolado quando SQL importa. Controle tempo e serviço
externo por seam estreito. Faça uma transformação estrutural por vez e mantenha
testes verdes antes de mudar a regra. Review pequeno e telemetria do caminho novo
reduzem o raio de impacto.

## Alternativas

- testar o fluxo inteiro sem alterar código;
- extrair função pura para a nova regra, protegida por caracterização externa;
- envolver dependência externa com adapter e fake controlado;
- substituir módulo gradualmente por Strangler Fig;
- reescrever tudo em paralelo;
- manter código e reforçar somente validação operacional.

## Trade-offs

Caracterização cria uma rede inicial rapidamente, mas preserva comportamento sem
julgar sua correção. Extração incremental limita risco e pode deixar design
temporariamente desigual. Teste com infraestrutura real é fiel e mais lento.
Mocks executam rápido, mas podem codificar a implementação atual. Reescrita oferece
liberdade e perde conhecimento incorporado, além de adiar feedback.

## Estratégia de solução

Escolha o fluxo pelo risco da mudança. Crie um teste de caracterização de entrada
e saída e um teste de integração para a semântica persistente relevante. Insira
um seam somente no relógio ou cliente que impede determinismo. Extraia a regra
para função ou serviço pequeno sem alterar resultado; depois introduza o novo
comportamento com testes de domínio.

Libere atrás de flag se for necessário comparar variantes, observando resultado
sem duplicar efeitos. Remova o caminho antigo apenas após validação e atualize a
documentação. Registre dívidas adjacentes pelo impacto, sem ampliar o escopo
automaticamente.

## Validação

- golden master ou tabela de exemplos possui revisão de negócio;
- testes falham quando a regra relevante é perturbada;
- banco/serializer real cobre o contrato que doubles não representam;
- execução é determinística e não acessa produção;
- rollout observa erro e resultado funcional por variante;
- cada refatoração pode ser revisada separadamente da mudança comportamental.

## Riscos remanescentes

Casos não observados permanecem. O comportamento caracterizado pode conter
defeito. Teste amplo pode ser lento ou frágil. A arquitetura antiga continua fora
da fatia tratada. A flag pode virar dívida se não houver remoção planejada.

## Aprendizados

Testabilidade pode ser conquistada em incrementos. Caracterizar antes de mudar
preserva conhecimento; seams devem nascer de uma necessidade concreta. A meta
não é alcançar um desenho ideal em uma etapa, mas reduzir risco e custo da próxima
mudança com evidência.

## Referências para capítulos relacionados

- [C13 — Dependências](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md)
- [C23 — Estratégia de testes](../qualidade-e-seguranca/c23-testes-comportamento-risco.md)
- [C24 — Legado e integração](../qualidade-e-seguranca/c24-integracao-real-testes-legado.md)
- [C30 — Dívida e colaboração](../ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md)

## Referências

- [Working Effectively with Legacy Code](https://www.oreilly.com/library/view/working-effectively-with/0131177052/)
  — Michael Feathers, acesso em 18 novembro 2026.
