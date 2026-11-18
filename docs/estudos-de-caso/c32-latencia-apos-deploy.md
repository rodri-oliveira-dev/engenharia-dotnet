# C32 — API com aumento de latência após deploy

> **Nível:** Avançado
>
> **Objetivo:** correlacionar mudança e degradação, mitigar com segurança e usar
> métricas, tracing e profiling para explicar a causa.
>
> **Pré-requisitos:** [C04 — Memória, runtime e performance](../linguagem-e-runtime/c04-memoria-runtime-performance.md),
> [C27 — Telemetria e diagnóstico distribuído](../operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md),
> [C28 — Confiabilidade e resposta a incidentes](../operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md)
> e [C29 — Entrega compatível](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md).
>
> **Última revisão:** 2026-11-18

Este cenário didático fictício não atribui métricas a uma empresa real. Valores
e limiares devem ser definidos pelo SLO e pela baseline do sistema analisado.

## Cenário

Uma nova versão da API foi implantada gradualmente. Pouco depois, o percentil de
cauda de algumas rotas sobe e o consumo de recursos muda. A coincidência temporal
é evidência para priorizar a investigação, não prova de causalidade.

## Contexto técnico

A API ASP.NET Core possui várias instâncias, banco e cliente HTTP. Métricas têm
rótulo de versão, traces amostram requisições e logs carregam trace ID. O deploy
foi rolling, portanto versões antiga e nova convivem durante a observação.

## Sintomas ou problema

Usuários percebem lentidão intermitente. A média muda pouco, enquanto p95/p99 e
timeouts aumentam em uma operação. CPU agregada não explica sozinha se há espera
em banco, lock, rede, GC, thread pool ou trabalho adicional.

## Restrições

- disponibilidade deve ser preservada durante diagnóstico;
- profiling tem overhead e pode capturar dados sensíveis;
- rollback de código pode ser incompatível após migration ou mensagens novas;
- tráfego, dependências e infraestrutura também mudam;
- amostragem pode esconder a cauda rara.

## Hipóteses

1. Nova consulta perdeu seletividade ou trouxe dados além do necessário.
2. Uma chamada antes paralela tornou-se sequencial ou passou a sofrer retry.
3. Alocação adicional aumentou pausas ou pressão do GC.
4. Bloqueio ou starvation da thread pool aparece apenas sob concorrência.
5. Cache frio durante rolling explica efeito transitório.
6. Dependência ou nó de infraestrutura degradou simultaneamente.

## Processo de investigação

Declare impacto e compare séries por rota, status, versão e instância, evitando
cardinalidade não controlada. Verifique início do deploy, mudança de tráfego,
erros e saturação. Um canary antigo versus novo sob tráfego comparável ajuda a
separar versão de ambiente.

Nos traces lentos, decomponha tempo em spans: fila, aplicação, SQL e HTTP. Compare
traces rápidos e lentos, planos de execução e contadores do runtime. Procure
correlação, não apenas o span mais longo: ele pode ser vítima de contenção.
Somente após localizar CPU, alocação ou bloqueio capture profile curto e
controlado na instância afetada, com autorização e política de dados.

Reproduza a hipótese em ambiente representativo ou canary limitado. Meça baseline
e variante com mesma carga e dados; uma execução isolada não sustenta causalidade.

## Alternativas

- pausar o rollout e manter versões atuais;
- desabilitar o caminho novo por feature flag;
- rollback do artefato, se estado e contratos permitirem;
- rollforward pequeno para consulta, timeout ou alocação identificada;
- reduzir carga, concorrência ou funcionalidade para ganhar margem;
- escalar temporariamente enquanto se investiga, sem tratá-lo como correção.

## Trade-offs

Rollback mitiga rápido, mas remove evidência e pode falhar com schema novo.
Feature flag reduz o caminho afetado sem trocar binário, porém não ajuda se a
inicialização ou migration causou o problema. Escalar reduz saturação e custo por
fila, mas pode pressionar uma dependência compartilhada. Profiling aumenta
visibilidade e também overhead e exposição de dados.

## Estratégia de solução

Separe mitigação de causa. Se guardrail for violado, pause e escolha flag,
rollback ou redução de tráfego pela reversibilidade real. Preserve versão, traces
representativos, planos e contadores necessários. Formule uma alteração mínima
para a causa sustentada por evidência e promova como canary com critérios
predefinidos.

Caso migration impeça retorno, faça rollforward compatível e mantenha o schema
expandido. Se a causa estiver fora da versão, encerre a correlação inicial e
coordene a dependência sem forçar uma narrativa de “deploy ruim”.

## Validação

- compara versão candidata, anterior e baseline no mesmo recorte;
- verifica latência de cauda, erro, throughput e saturação, não uma métrica só;
- confirma o mecanismo em trace, plano ou profile e em teste controlado;
- observa tempo suficiente para cache, GC e efeito atrasado;
- executa smoke de rotas críticas e confirma ausência de regressão adjacente;
- registra linha temporal, decisão de mitigação e evidência causal.

## Riscos remanescentes

Baixo tráfego pode atrasar confiança. Um perfil curto pode não capturar evento
raro. Dados e carga de teste podem divergir de produção. A correção local pode
deslocar pressão para banco ou serviço downstream. A causa pode envolver duas
mudanças concorrentes.

## Aprendizados

Deploy fornece uma fronteira temporal útil, não um veredito. Métricas localizam
quem e quando; traces decompõem caminho; profiles explicam consumo interno. A
melhor mitigação é a que reduz dano dentro da compatibilidade real, enquanto a
análise causal exige comparação controlada.

## Referências para capítulos relacionados

- [C04 — Performance](../linguagem-e-runtime/c04-memoria-runtime-performance.md)
- [C25 — Testes de carga](../qualidade-e-seguranca/c25-cobertura-e2e-carga.md)
- [C27 — Telemetria](../operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md)
- [C28 — Incidentes](../operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md)
- [C29 — Deploy e rollback](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md)

## Referências

- [Diagnose performance issues in .NET](https://learn.microsoft.com/dotnet/core/diagnostics/debug-highcpu)
  — Microsoft Learn, acesso em 18 novembro 2026.
