# Checklist de code review orientado a risco

Use perguntas proporcionais ao impacto da mudança. O checklist apoia julgamento;
não transforma aprovação em contagem de itens.

## Contexto e risco

- [ ] O propósito, o escopo e o risco estão claros?
- [ ] A mudança é pequena o suficiente para ser compreendida e revertida?
- [ ] Contratos públicos, dados, segurança ou operação foram afetados?

## Correção e legibilidade

- [ ] Fluxos principal, de erro, cancelamento e concorrência estão corretos?
- [ ] Nomes e estrutura comunicam intenção sem exigir conhecimento oculto?
- [ ] As suposições importantes estão expressas por tipos, validações ou texto?

## Evidências

- [ ] Os testes cobrem comportamentos e riscos alterados?
- [ ] Há evidência adequada para performance, migração ou compatibilidade?
- [ ] Logs, métricas e traces permitem observar a mudança em operação?

## Proteção e evolução

- [ ] Entradas, autorização, segredos e dados sensíveis foram considerados?
- [ ] Dependências novas são necessárias, mantidas e avaliadas?
- [ ] Deploy, convivência de versões, rollback ou rollforward são seguros?

## Colaboração e decisão

- [ ] Comentários distinguem bloqueios, sugestões e dúvidas?
- [ ] Bloqueios explicam o risco e propõem uma direção verificável?
- [ ] A aprovação registra riscos aceitos ou acompanhamentos necessários?
