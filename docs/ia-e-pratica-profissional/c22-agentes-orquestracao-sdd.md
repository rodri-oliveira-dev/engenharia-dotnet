# C22 — Agentes, orquestração e desenvolvimento orientado por especificações

> **Nível:** Avançado
>
> **Objetivo:** escolher entre workflow e agente, limitar autonomia e usar
> especificações para orientar implementação, testes e revisão sem terceirizar a
> responsabilidade de engenharia ao modelo.
>
> **Pré-requisitos:** [C21 — Fundamentos de LLM, MCP e
> RAG](c21-fundamentos-llm-mcp-rag.md), [C13 — Dependências, abstrações e padrões
> com propósito](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md)
> e [C23 — Testes orientados a comportamento e
> risco](../qualidade-e-seguranca/c23-testes-comportamento-risco.md).
>
> **Última revisão:** 2026-11-18

Um agente escolhe ações durante a execução para perseguir um objetivo. Essa
flexibilidade ajuda quando a próxima etapa depende de evidência ainda não
conhecida, mas reduz previsibilidade e amplia custo, superfície de ataque e modos
de falha. Muitas tarefas chamadas de “agentes” são workflows com uma etapa de
modelo; manter essa distinção melhora projeto, teste e operação.

Este capítulo não apresenta múltiplos agentes nem Spec-Driven Development (SDD)
como evolução obrigatória. O desenho mais simples que satisfaz o requisito e pode
ser verificado continua preferível.

## O que torna um sistema agente

Um agente combina objetivo, modelo de decisão, estado e ferramentas em um laço:

1. observa objetivo e estado disponível;
2. seleciona próximo passo;
3. solicita uma ferramenta ou produz uma resposta;
4. valida e executa a ação permitida;
5. registra o resultado como nova observação;
6. verifica conclusão, erro ou limite e repete quando necessário.

Uma chamada única para resumir texto não é agente. Um pipeline fixo
`validar → classificar com LLM → validar saída → persistir` também não: a
aplicação definiu todas as transições. O sistema se torna agente quando delega ao
modelo a escolha dinâmica de ferramenta, ordem ou decomposição.

### Ferramentas são fronteiras privilegiadas

Uma ferramenta é um contrato executável, não uma função genérica para o modelo.
Prefira operações estreitas como `ObterPedido(id)` ou
`ProporCancelamento(id, motivo)` a `ExecutarSql(texto)` ou `ExecutarShell(comando)`.
O contrato deve declarar:

- schema de entrada e saída, tamanhos e enumerações;
- identidade e escopos exigidos;
- se lê, altera, envia, cobra ou remove;
- idempotência, reversibilidade e timeout;
- dados sensíveis aceitos ou retornados;
- erros esperados e se retry é seguro;
- auditoria e confirmação exigidas.

Descrições ajudam o modelo a escolher; autorização e validação continuam no
código. Nunca entregue ao modelo credencial de administrador para depois pedir
que “use apenas quando apropriado”. Reduza a capacidade real.

### Estado explícito e limitado

Separe quatro tipos de estado:

| Estado | Exemplos | Tratamento |
| --- | --- | --- |
| execução | passo, tentativas, deadline, orçamento | estrutura durável e versionada |
| negócio | chamado, pedido, aprovação | sistema de registro e invariantes do domínio |
| conversação | mensagens e resumos | retenção, truncamento e classificação de dados |
| evidência | resultados e citações | origem, validade, ACL e versão |

Não use histórico textual como banco de dados. Estado estruturado permite
retomar, deduplicar, auditar e migrar. Em execução concorrente, aplique controle
de versão e idempotência como em qualquer processo distribuído; o modelo não
resolve corrida, entrega duplicada ou efeito parcialmente confirmado.

### Planejamento, execução e verificação

Planejamento pode ser uma escolha de próxima ação ou um plano completo. Planos
longos ficam obsoletos quando ferramentas revelam novos fatos. Planejar um passo,
executar e reavaliar reduz suposições, mas aumenta chamadas e latência.

Separe ao menos logicamente:

- **planner:** propõe passo dentro do catálogo permitido;
- **policy enforcement:** valida identidade, ferramenta, parâmetros e orçamento;
- **executor:** executa sem reinterpretar instruções em texto;
- **verifier:** confere pós-condições por fonte independente;
- **controller:** encerra, tenta alternativa limitada ou escala a uma pessoa.

O verificador não deve confiar apenas na frase “concluído” do agente. Após criar
uma issue, consulte o identificador retornado; após editar arquivo, faça parse e
execute testes; após propor pagamento, consulte estado no sistema de registro.
Usar o mesmo modelo para gerar e julgar ajuda em alguns casos, mas não é prova
independente.

## Autonomia, falhas e intervenção humana

Autonomia é um orçamento, não um booleano. Limite passos, tokens, tempo, custo,
concorrência, ferramentas, registros afetados e destinos de rede. Defina também
uma condição de parada sem sucesso. Um agente que não encontra resposta deve
encerrar com evidência e incerteza, não continuar até consumir todo o orçamento.

### Human-in-the-loop

Revisão humana pode ocorrer antes, durante ou depois:

- **antes:** aprovar objetivo, escopo e plano para tarefa de alto impacto;
- **durante:** confirmar ação sensível com parâmetros finais;
- **depois:** revisar diff, citação ou resultado antes da publicação;
- **por exceção:** assumir quando confiança, custo ou falha excede limite.

Confirmação útil informa ação, alvo, consequência, reversibilidade e dados que
serão enviados. A aprovação deve expirar e ficar vinculada a esses parâmetros;
qualquer alteração pede nova decisão. Pessoas não devem receber volume impossível
de revisar nem ser usadas como controle decorativo.

### Tratamento de falhas

Classifique falhas para evitar loops:

| Falha | Resposta típica |
| --- | --- |
| entrada inválida | corrigir uma vez com erro estruturado ou encerrar |
| timeout/transiente | retry limitado somente se operação for segura |
| autorização negada | não contornar; explicar e encerrar ou pedir acesso formal |
| ferramenta desconhecida | rejeitar na allowlist |
| pós-condição ausente | reconciliar estado antes de tentar novamente |
| plano repetido | detectar ciclo e interromper |
| orçamento esgotado | produzir checkpoint e escalar |
| contexto malicioso | isolar fonte, não executar instruções e registrar evento |

Compensação não equivale a rollback, como detalha o
[C17](../sistemas-distribuidos/c17-consistencia-eventual-sagas.md). Para ações
irreversíveis, prefira proposta e aprovação a compensação posterior.

### Custo e observabilidade

Meça por execução e por resultado:

- chamadas, tokens e custo por modelo, ferramenta e etapa;
- latência total e por dependência, incluindo fila e revisão humana;
- passos até conclusão, loops, retries e taxa de abandono;
- ferramentas propostas, negadas, confirmadas e executadas;
- sucesso verificado, reversões, violações de política e incidentes;
- versões de prompt, modelo, catálogo, política e dados recuperados.

Trace deve correlacionar decisão, tool call e efeito, mas não registrar prompt,
segredo ou retorno integral por padrão. Métrica de “tarefas concluídas” sem
verificação incentiva o agente a declarar sucesso. Métrica de linhas geradas não
mede produtividade nem qualidade.

## Padrões de orquestração

Orquestração decide onde o fluxo fica explícito e onde existe escolha dinâmica.
Os padrões podem ser combinados, mas cada camada adicional cria estado, falha e
custo.

| Padrão | Funcionamento | Use quando | Custo e risco |
| --- | --- | --- | --- |
| sequencial | etapas em ordem fixa | dependências e política são conhecidas | simples; uma etapa lenta bloqueia o fluxo |
| paralelo | tarefas independentes executam juntas | latência domina e resultados podem ser agregados | concorrência, rate limit, custo simultâneo |
| router | classifica e escolhe um caminho conhecido | categorias são limitadas e especializadas | erro de rota; precisa fallback |
| supervisor | componente delega e acompanha workers | subtarefas variam, mas controle central é útil | gargalo e estado central complexo |
| planner/executor | plano é proposto e outro componente executa | decomposição depende do objetivo | plano inválido, chamadas extras, drift |
| determinístico | máquina de estados ou código define transições | compliance, repetibilidade e regras estáveis | menos flexível para casos abertos |
| multiagente | vários agentes com contexto ou papel próprio | isolamento real ou trabalho paralelo mensurável | coordenação, conflitos, custo e depuração |

### Workflow sequencial e paralelo

Um workflow sequencial é a baseline para etapas dependentes. Use paralelismo só
para trabalhos realmente independentes e limite concorrência. Três modelos
avaliando a mesma entrada não garantem diversidade: podem compartilhar dados e
vieses. Agregação por maioria também não torna verdadeiro um resultado.

### Router

O router pode ser regra, classificador tradicional ou LLM. Rotas com impacto
devem ter limiar de confiança e fallback. Se palavras-chave resolvem a tarefa com
qualidade comparável, elas são mais baratas e explicáveis. Avalie a matriz de
confusão, sobretudo rotas de segurança.

### Supervisor e planner/executor

Supervisor mantém objetivo, orçamento e estado, delega tarefas e verifica
respostas. Ele não deve conceder automaticamente a workers todas as próprias
permissões. Planner/executor reduz o acoplamento entre raciocínio e efeito: o
executor aceita apenas um plano tipado e validado. Em ambos, um controller
determinístico deve impor limites.

### Multiagentes

Múltiplos agentes se justificam quando há isolamento de contexto ou credencial,
especialização avaliada, ownership separado ou paralelismo útil. Dar personas
diferentes ao mesmo modelo não cria automaticamente conhecimento independente.
Antes de adotar, compare contra um agente com tools focadas e contra um workflow.

Defina protocolo de handoff, schema, prazo, orçamento, autoridade sobre o estado
e regra para conflitos. Evite conversa livre ilimitada entre agentes. Falha de um
worker não pode deixar o supervisor assumindo que sua ação ocorreu.

## Quando o workflow convencional é melhor

Prefira workflow determinístico quando:

- etapas, decisões e exceções são enumeráveis;
- ação tem impacto financeiro, regulatório ou irreversível;
- auditoria precisa explicar exatamente por que uma transição ocorreu;
- throughput, latência e custo precisam de limites estreitos;
- regras mudam por versionamento controlado;
- testes precisam reproduzir todas as transições;
- uma busca, regra ou state machine resolve o problema.

Considere agente quando o espaço de ações é limitado, mas a ordem depende de
evidência descoberta durante a execução: investigação de incidente, exploração
de codebase ou coleta de dados heterogêneos. Mesmo nesses casos, mantenha efeitos
sensíveis em workflows explícitos. Um desenho híbrido costuma ser adequado: o
agente pesquisa e propõe; um workflow valida, aprova e executa.

## Comparação para o mesmo problema

Considere triagem e resolução de chamados:

| Aspecto | Workflow determinístico | Agente limitado |
| --- | --- | --- |
| fluxo | validar, classificar, rotear | escolher consulta, observar e propor próximo passo |
| modelo | uma classificação estruturada | uma ou mais decisões dinâmicas |
| ferramentas | chamadas definidas pelo código | catálogo permitido e policy gate |
| estado | status e resultado por etapa | histórico, orçamento e observações |
| teste | transições enumeráveis | cenários, limites, tool calls e avaliação |
| custo/latência | previsíveis | variáveis e normalmente maiores |
| melhor uso | categorias e regras conhecidas | diagnóstico com evidência desconhecida |

Se o chamado só precisa ser classificado, o workflow vence. Se investigar exige
consultar logs diferentes conforme o primeiro resultado, um agente pode reduzir
scripts especializados. Ele ainda não deve fechar chamado, alterar produção ou
notificar cliente sem validação e autoridade explícitas.

## Spec-Driven Development

Spec-Driven Development, neste capítulo, significa tornar a especificação um
artefato versionado que antecede e orienta implementação, testes e revisão. Não é
uma metodologia única nem garantia de qualidade. Uma especificação errada apenas
torna o erro mais rastreável.

### Conteúdo mínimo de uma especificação útil

- problema, objetivo e não objetivos;
- atores, premissas e termos do domínio;
- requisitos funcionais e qualidades mensuráveis;
- contratos de entrada, saída, erros e compatibilidade;
- critérios de aceite com exemplos e precedência;
- segurança, privacidade, observabilidade e operação;
- restrições, alternativas e decisões abertas;
- estratégia de rollout, migração e reversão;
- identificadores estáveis para rastreabilidade.

Requisito “a API deve ser rápida” não gera teste útil. “p95 abaixo de 300 ms sob
200 req/s no ambiente X, payload Y e dataset Z” explicita método e limite. Para
comportamento de IA, acrescente dataset versionado, rubrica, limiares por classe,
modelo de ameaça e orçamento.

### Da especificação ao código

Um fluxo proporcional é:

1. humano e partes interessadas definem problema e decisões irreversíveis;
2. IA pode apontar ambiguidades, gerar exemplos e propor critérios;
3. equipe aprova contrato, ameaças e critérios de aceite;
4. IA pode gerar esqueleto, implementação pequena e testes candidatos;
5. compilador, analyzers, testes e avaliações produzem evidência;
6. pessoa revisa comportamento, segurança, manutenção e aderência à spec;
7. spec, código e testes evoluem juntos, com mudança rastreável.

Testes derivados da especificação verificam exemplos e propriedades, mas não
provam que a especificação cobre o mundo real. Inclua contraexemplos, limites,
falhas e testes independentes do código gerado. Se o mesmo modelo escreve spec,
código e testes sem revisão, um mal-entendido pode se repetir nas três camadas.

### Rastreabilidade e evolução

Requisitos estáveis como `REQ-AUTH-03` e critérios `AC-01` podem aparecer em
testes, ADRs e evidências de release. Evite matriz burocrática para mudança
trivial; automatize links quando o risco justificar. Um requisito modificado deve
registrar motivo, compatibilidade, dados afetados e testes a atualizar.

Spec não deve congelar descoberta. Use questões abertas e hipóteses em vez de
inventar certeza. Prototipação pode preceder a spec para reduzir incerteza, desde
que o protótipo não seja promovido silenciosamente a produção.

### Limitações e riscos de SDD

- especificações extensas podem ficar desatualizadas e atrasar feedback;
- linguagem natural mantém ambiguidades;
- geração otimiza o texto fornecido, inclusive premissas ruins;
- critérios fáceis de medir podem substituir resultados importantes;
- rastreabilidade aparente pode criar falsa confiança;
- código gerado aumenta volume de revisão e dependências não necessárias;
- propriedade intelectual e dados sensíveis podem sair do limite autorizado.

Use a quantidade de especificação proporcional a impacto, irreversibilidade e
coordenação. Para uma função interna, exemplos e tipos podem bastar; para um
contrato público ou fluxo financeiro, compatibilidade e falhas merecem detalhe.

## IA no ciclo de engenharia

IA é útil como acelerador de busca, transformação e crítica, desde que cada uso
tenha entrada autorizada e evidência verificável.

| Atividade | Uso responsável | Verificação necessária |
| --- | --- | --- |
| refinamento | listar ambiguidades, cenários e riscos | decisão com produto e domínio |
| código | gerar esqueleto ou mudança delimitada | build, testes, análise e revisão do diff |
| code review | procurar padrões de defeito e explicar impacto | revisor confirma no contexto completo |
| testes | sugerir partições, limites e propriedades | mutation/risk review e independência da implementação |
| documentação | resumir contratos e decisões existentes | validar contra código e fontes |
| refatoração | propor passos pequenos e equivalência | caracterização, testes e medição |
| incidentes | correlacionar sinais e construir hipóteses | telemetria primária e aprovação para mitigação |
| arquitetura | comparar forças e registrar alternativas | ADR, dados operacionais e responsáveis |

Não envie repositório, logs, dados de cliente ou segredos a um sistema sem
contrato e configuração compatíveis com sua classificação. Remova segredos antes
do prompt, restrinja retenção e treinamento e preserve licença e atribuição.

### Medição de produtividade e qualidade

Linhas geradas, sugestões aceitas e número de prompts são métricas de atividade.
Meça resultado em períodos comparáveis:

- lead time e tempo de ciclo por tipo de mudança;
- taxa de retrabalho, rollback e defeitos escapados;
- tempo de review e tamanho de lotes;
- cobertura de risco, confiabilidade do pipeline e incidentes;
- satisfação, carga cognitiva e capacidade de manutenção;
- custo total de ferramenta, inferência, revisão e operação.

Controle seleção: equipes escolhem IA para tarefas diferentes, e novidade pode
alterar comportamento. Combine telemetria quantitativa com entrevistas e
revisões amostrais. Ganho local de digitação pode virar custo de compreensão e
manutenção.

### Dependência excessiva

Sinais incluem aceitar código não compreendido, perder habilidade de depurar,
usar citação não aberta, ampliar escopo para aproveitar uma sugestão e substituir
conversa de domínio por texto gerado. Mitigue com revisão ativa, pairing,
exercícios sem assistência em competências críticas, documentação de decisões e
ownership humano claro. A pessoa responsável precisa conseguir explicar,
operar, alterar e reverter o resultado.

## Segurança operacional de agentes

Agentes combinam os riscos do [C21](c21-fundamentos-llm-mcp-rag.md) com efeitos
reais. A política deve estar fora do prompt:

- autentique cada tool call e autorize objeto e operação;
- forneça credencial curta, específica e vinculada à execução;
- não propague texto recuperado como instrução de maior prioridade;
- bloqueie destinos de exfiltração e aplique egress control;
- valide argumentos depois da decisão do modelo e antes da execução;
- use preview e confirmação em alterações, envio, compra e exclusão;
- limite passos, tempo, custo, volume e concorrência;
- encode saída conforme shell, SQL, HTML ou linguagem de destino;
- mantenha audit log resistente a adulteração e sem segredos;
- teste prompt injection, ferramenta comprometida e resposta parcialmente falha.

Um agente de código não deve alterar pipeline, permissões ou arquivos fora do
workspace só porque a tarefa textual sugere. Um agente de incidentes pode ler
telemetria e propor comandos; acesso de escrita em produção deve ficar em fluxo
separado, autenticado e confirmado.

## Laboratório executável

O laboratório compartilhado em `exemplos/c21-c22-ia/` demonstra:

- `AgenteLimitado`: catálogo imutável, limite de passos, tamanho de argumento e
  confirmação para qualquer ferramenta que não seja somente leitura;
- `WorkflowTriagem`: alternativa determinística para o mesmo domínio de chamados;
- `especificacao-triagem.md`, `PoliticaRevisao` e testes `Ac01` a `Ac04`:
  especificação, implementação e rastreabilidade dos critérios.

O planner do teste é roteirizado, não um LLM. Isso isola o que importa: nenhum
planejador, por mais convincente, atravessa allowlist ou confirmação. Para ligar
um modelo real, implemente `IPlanejadorAgente` e mantenha os mesmos gates. A
demonstração não é runtime de agentes pronto para produção: faltam persistência,
recuperação após falha, autenticação, quotas distribuídas e telemetria.

Execute todo o laboratório:

```powershell
dotnet test exemplos/c21-c22-ia/C21C22.Ia.Tests.csproj --configuration Release
```

Os testes não acessam rede, não usam chave e não exigem serviço pago. Eles
verificam saída inválida do modelo, ACL do RAG, tool fora da allowlist, confirmação
negada, critérios da especificação e a tool MCP por chamada direta.

## Como decidir

1. Escreva o resultado, impacto e evidência de sucesso.
2. Tente regra, busca, template ou workflow determinístico.
3. Delimite o que realmente precisa de interpretação por LLM.
4. Se a próxima etapa depender de evidência desconhecida, experimente agente com
   catálogo somente leitura e orçamento curto.
5. Compare qualidade, custo, latência e falhas com a baseline.
6. Acrescente efeitos apenas com autorização, confirmação e verificação.
7. Use multiagentes somente após medir uma limitação concreta do desenho simples.

O critério não é quão “autônomo” o sistema parece. É se ele entrega resultado
verificável dentro de risco e custo aceitáveis e pode ser compreendido e operado.

## Relação com outros capítulos

- [C05](../linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md)
  cobre paralelismo, cancelamento e backpressure.
- [C13](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md) ajuda a
  manter planner, policy e executor em responsabilidades verificáveis.
- [C16](../sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md) e
  [C17](../sistemas-distribuidos/c17-consistencia-eventual-sagas.md) tratam
  idempotência, entrega e compensação sem atribuí-las ao modelo.
- [C19](../dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md) conecta
  specs, ADRs e limites arquiteturais.
- [C23](../qualidade-e-seguranca/c23-testes-comportamento-risco.md) orienta testes
  por risco e revisão independente.

## Referências

- [MCP Specification 2026-07-28 — Tools](https://modelcontextprotocol.io/specification/2026-07-28/server/tools)
  — schemas, descoberta, invocação e human-in-the-loop, acesso em 18 novembro
  2026.
- [MCP Architecture](https://modelcontextprotocol.io/specification/2026-07-28/architecture)
  — separação entre host, clients e servers, acesso em 18 novembro 2026.
- [NIST AI 600-1](https://doi.org/10.6028/NIST.AI.600-1) — gestão de riscos de
  IA generativa e dependência humana inadequada.
- [OWASP LLM06:2025 Excessive Agency](https://genai.owasp.org/llmrisk/llm062025-excessive-agency/)
  — funcionalidade, permissão e autonomia excessivas, acesso em 18 novembro 2026.
- [OWASP LLM01:2025 Prompt Injection](https://genai.owasp.org/llmrisk/llm01-prompt-injection/)
  — injeção direta e indireta, acesso em 18 novembro 2026.
- [Microsoft.Extensions.AI evaluation libraries](https://learn.microsoft.com/dotnet/ai/conceptual/evaluation-libraries)
  — opções .NET para avaliação automática; alguns evaluators dependem de modelos
  ou serviços externos e não são usados no laboratório, acesso em 18 novembro
  2026.
