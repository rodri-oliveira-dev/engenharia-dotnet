# C35 — Pipeline com dependência vulnerável

> **Nível:** Avançado
>
> **Objetivo:** transformar um alerta de dependência em decisão de liberação
> baseada em severidade, alcançabilidade, exposição e opções de mitigação.
>
> **Pré-requisitos:** [C26 — Segurança nas fronteiras](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md),
> [C29 — Entrega compatível](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md)
> e [C30 — Decisão e maturidade técnica](../ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md).
>
> **Última revisão:** 2026-11-18

Este cenário didático fictício não descreve um CVE específico. Severidade e
prazo não devem ser inventados: consulte o advisory aplicável e o contexto do
sistema.

## Cenário

O job de Software Composition Analysis (SCA) encontra vulnerabilidade em pacote
transitivo durante uma correção funcional urgente. Há versão corrigida, mas a
atualização pode alterar comportamento. A equipe precisa decidir se bloqueia,
atualiza, mitiga ou aceita temporariamente.

## Contexto técnico

A aplicação .NET gera inventário de pacotes no pipeline. O serviço é exposto à
rede, mas ainda não se sabe se a API vulnerável é carregada ou alcançável por
entrada não confiável. O pacote chega por uma dependência direta e existe owner
para a aplicação, não para a biblioteca externa.

## Sintomas ou problema

O pipeline fica vermelho com identificador, pacote e intervalo afetado. Um alerta
não prova exploração; ignorá-lo por ser transitivo também é incorreto. A decisão
funcional agora disputa tempo com análise de segurança.

## Restrições

- a correção pendente reduz outro risco conhecido;
- atualização ampla pode introduzir regressão;
- não há credenciais para consultar fontes privadas em pull request não confiável;
- exceções precisam de validade e responsável;
- a organização possui critérios de risco e segregação de aprovação.

## Hipóteses

1. O código vulnerável é alcançável por entrada externa no runtime publicado.
2. O pacote aparece apenas em build/teste ou caminho não carregado.
3. A versão corrigida é compatível e uma atualização focal resolve o alerta.
4. A dependência direta restringe a versão transitiva e também precisa subir.
5. Configuração ou remoção de funcionalidade reduz exposição temporariamente.
6. O scanner usa advisory desatualizado ou identificou o pacote errado.

## Processo de investigação

Confirme advisory em fonte primária, versões afetadas, pré-condições, impacto e
correção. Gere o grafo restaurado e o artefato publicado; descubra qual pacote
traz a dependência e se ela chega ao runtime. Procure uso estático e valide
carregamento/chamada em execução quando necessário. Modele se uma identidade não
confiável satisfaz as pré-condições e quais controles já limitam impacto.

Em branch isolada, atualize a menor dependência responsável, leia notas de
compatibilidade e execute build, testes, análise e smoke do caminho afetado. Se
não houver correção imediata, teste a mitigação proposta e registre evidência.
Evite publicar exploit contra sistemas reais; reprodução deve ser autorizada e
isolada.

## Alternativas

- bloquear release até atualização;
- atualizar pacote direto ou dependência que o traz;
- remover funcionalidade/dependência;
- aplicar configuração, WAF ou redução de privilégio como mitigação temporária;
- liberar apenas a correção urgente com exceção formal e prazo curto;
- manter versão anterior do serviço;
- fazer fork/backport, assumindo manutenção e cadeia de suprimentos própria.

## Trade-offs

Bloqueio elimina exposição nova e pode prolongar outro defeito crítico. Atualizar
rapidamente remove versão afetada e pode quebrar contrato. Mitigação reduz
probabilidade ou impacto, mas pode ser incompleta. Exceção conserva fluxo e cria
risco residual e trabalho obrigatório. Fork dá controle imediato e transfere
manutenção contínua para a equipe.

## Estratégia de solução

Classifique com severidade do advisory, alcançabilidade, exposição, privilégio,
impacto dos dados, controles compensatórios e detectabilidade. Prefira atualização
focal com versão explícita, revisão do lock/grafo, testes e canary. Não altere
várias dependências sem necessidade durante resposta urgente.

Se a atualização não for segura no prazo, compare o risco vulnerável ao risco de
adiar a correção funcional. Uma exceção deve registrar justificativa, escopo,
owner, expiração, mitigação, telemetria e condição de revogação. A aprovação segue
a autoridade definida pela política; o pipeline não deve aceitar texto mágico no
commit para silenciar o gate.

## Validação

- restore e inventário confirmam ausência da versão afetada no artefato;
- build, unitários, integração e smoke cobrem a atualização;
- SAST/SCA são reexecutados sem ocultar alertas diferentes;
- teste de contrato verifica mudança relevante da biblioteca;
- canary observa erro, latência e sinal de abuso definido;
- exceção, quando necessária, falha automaticamente após expiração;
- rollback/rollforward consideram schema e compatibilidade.

## Riscos remanescentes

Reachability analysis pode produzir falso negativo por reflexão ou configuração.
O advisory pode evoluir. Controles compensatórios podem ser removidos. A versão
corrigida pode trazer outra vulnerabilidade ou regressão. Imagens e ambientes
antigos podem continuar contendo o pacote mesmo após corrigir a branch.

## Aprendizados

SCA inicia uma investigação; política transforma resultado em resposta
consistente. A decisão de liberar compara riscos concretos, não “segurança versus
negócio”. Inventário, ownership, atualização pequena, expiração de exceção e
telemetria reduzem tanto exposição quanto tempo de resposta.

## Referências para capítulos relacionados

- [C26 — Segurança](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md)
- [C29 — Pipeline e governança](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md)
- [C30 — Decisão e dívida](../ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md)
- [Checklist de code review](../templates/checklist-code-review.md)

## Referências

- [NuGet audit](https://learn.microsoft.com/nuget/concepts/auditing-packages)
  — Microsoft Learn, acesso em 18 novembro 2026.
- [GitHub Advisory Database](https://github.com/advisories)
  — GitHub, acesso em 18 novembro 2026.
- [Open Source Vulnerability format](https://ossf.github.io/osv-schema/)
  — OpenSSF, acesso em 18 novembro 2026.
