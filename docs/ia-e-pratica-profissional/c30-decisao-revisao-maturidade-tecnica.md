# C30 — Decisão, revisão e maturidade técnica

> **Nível:** Fundamental
>
> **Objetivo:** conduzir decisões e revisões proporcionais ao risco, colaborar
> em discordâncias, comunicar escolhas e riscos, exercer autonomia com limites
> claros e tornar dívida técnica e compromissos de qualidade visíveis.
>
> **Pré-requisitos:** [C19 — Documentação arquitetural viva](../dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md)
> e [C23 — Testes orientados a comportamento e risco](../qualidade-e-seguranca/c23-testes-comportamento-risco.md).
>
> **Última revisão:** 2026-11-20

Engenharia madura não elimina incerteza nem discordância. Ela torna problema,
evidências, restrições, decisão e consequências examináveis. Isso exige técnica
e colaboração: uma solução correta que ninguém consegue operar ou questionar
não é uma boa decisão de equipe. Este capítulo apresenta um processo adaptável,
não um ritual obrigatório nem uma definição de senioridade.

## Senioridade e liderança técnica sem cargo de gestão

Senioridade não exige ser gestor de pessoas nem arquiteto de software.
Liderança técnica também se manifesta pela influência positiva, pela autonomia e pela capacidade de ajudar a equipe a entregar soluções melhores.

Essa influência não depende de autoridade hierárquica. Ela aparece quando alguém
expõe riscos com clareza, compartilha critérios de decisão, melhora o trabalho
coletivo e assume a parte da entrega que está sob sua responsabilidade.
Não significa decidir por outras pessoas, centralizar aprovações ou responder
sozinho por todo o sistema. A autoridade e a responsabilidade precisam ser
compatíveis com o papel, a equipe e o contexto.

Competências comportamentais devem ser observadas em ações e resultados,
não em traços de personalidade, tempo de carreira ou quantidade de reuniões.

| Competência | Comportamento observável | Evidência possível |
| --- | --- | --- |
| Comunicação técnica e com o negócio | explica impacto, alternativas e limites sem esconder incerteza | decisão registrada e entendimento confirmado com os envolvidos |
| Descoberta de requisitos | identifica regras ambíguas, dependências e exceções antes de implementar | exemplos de entrada, saída e critérios de aceite acordados |
| Negociação de escopo e prazo | apresenta opções e explicita risco residual e custo de atraso | escopo ajustado com riscos e responsáveis registrados |
| Autonomia na entrega | conduz a solução com validação, alinhamento e acompanhamento proporcionais | mudança observada após publicação e impedimentos sinalizados |
| Feedback e gestão de conflitos | discute consequências do código, ouve objeções e busca critérios comuns | revisão concluída com justificativa e sem ataques pessoais |
| Mentoria e colaboração | distribui contexto e aumenta a autonomia de outras pessoas | colega consegue executar ou manter um fluxo sem dependência constante |
| Comunicação em incidentes | diferencia fato, hipótese, impacto e próximo passo | atualização útil para decisão, sem causa raiz inventada |

A tabela não é uma pontuação automática para promoção. As evidências são
exemplos, e a expectativa precisa considerar oportunidades, apoio disponível,
complexidade das demandas e acordos de cada equipe.

### Traduzir problemas para públicos diferentes

Considere uma API que começou a apresentar respostas lentas depois de uma
publicação. A mensagem deve preservar os mesmos fatos, mas mudar o foco:

- **Para desenvolvedores:** qual endpoint piorou, quais métricas mudaram,
  hipótese em análise, experimento proposto e risco de reversão.
- **Para produto e negócio:** qual jornada foi afetada, impacto observado,
  alternativas de mitigação e decisões que precisam de participação.
- **Para operação e suporte:** sinais para monitorar, medida de contenção,
  condições de escalonamento e onde consultar o estado atual.

Não prometa prazo de recuperação sem evidência. Confirme com o interlocutor
se a mensagem permitiu compreender o risco e tomar a decisão necessária.

### Descobrir requisitos antes de transformar pedidos em código

Um pedido como "a consulta precisa ser rápida" não basta para escolher cache,
índice ou novo serviço. A pessoa responsável pela análise pode esclarecer:

1. Quem realiza a consulta e qual decisão depende da resposta?
2. Que volumes e condições de pico são conhecidos ou apenas estimados?
3. Quais dados devem estar atualizados e qual atraso é aceitável?
4. Como serão tratados acesso indevido, falha da dependência e ausência de dados?
5. Qual indicador e cenário de teste demonstram que o resultado foi alcançado?

Registre exemplos de sucesso e falha, critérios verificáveis e dúvidas ainda
abertas. Investigar não dá a uma pessoa licença para criar regras de negócio
sem validação com quem responde por elas.

### Negociar alcance, prazo e risco com transparência

Quando a data é rígida, apresente opções que permitam uma escolha consciente:
reduzir o escopo, entregar por etapas ou adiar uma funcionalidade. Indique
o que permanece protegido (por exemplo, autorização e integridade de dados),
o que não será entregue, quais verificações serão executadas e como recuperar
o serviço se a mudança falhar.

Autonomia não é executar silenciosamente uma escolha de alto risco.
O profissional sênior sinaliza cedo um bloqueio, recomenda uma alternativa
e leva a decisão para quem tem autoridade quando o impacto excede sua alçada.
Depois, acompanha o resultado e comunica desvios, mesmo que a implementação
esteja tecnicamente concluída.

### Colaborar e comunicar incidentes sem buscar culpados

Durante uma falha, informe primeiro o que foi observado, quem foi afetado,
quais medidas foram tomadas e qual é a próxima verificação. Separe hipóteses
de causas confirmadas. Em retrospectiva, identifique mudanças no sistema,
na documentação e no processo que possam reduzir recorrência, em vez de
avaliar a competência individual de quem participou do incidente.

As seções de [code review](#code-review-é-gestão-compartilhada-de-risco),
[colaboração em decisões difíceis](#colaboração-em-decisões-difíceis)
e [mentoria](#mentoria-e-conhecimento-compartilhado) aprofundam como
dar feedback, lidar com discordâncias e distribuir conhecimento. O
[C28 — Confiabilidade e resposta a incidentes](../operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md)
descreve a resposta operacional.

## Começar pelo problema

Uma discussão que começa em “microserviços ou monólito?” já reduziu o espaço de
solução antes de declarar o problema. Um enquadramento útil registra:

- resultado funcional e pessoas ou sistemas afetados;
- estado atual, lacuna observável e urgência;
- requisitos funcionais que precisam ser preservados;
- requisitos não funcionais como disponibilidade, latência, segurança,
  recuperabilidade, custo e capacidade de mudança;
- restrições de prazo, regulação, equipe, plataforma e compatibilidade;
- fatos, suposições e questões ainda abertas.

Requisito não funcional precisa de contexto e forma de verificação. “Escalável”
é ambíguo; “sustentar a carga prevista sem ultrapassar o SLO, verificado pelo
ensaio definido” orienta alternativas. Um número inventado apenas parece mais
rigoroso: use baseline, previsão identificada como tal ou experimento.

## Um ciclo de decisão proporcional

O ciclo abaixo serve tanto para uma biblioteca quanto para uma mudança de
arquitetura; a profundidade varia com alcance e reversibilidade.

1. **Definir:** formular problema, resultado, restrições e critérios.
2. **Descobrir:** reunir alternativas, incluindo manter o estado atual e a opção
   mais simples.
3. **Investigar:** buscar documentação primária, dados internos e experiência
   relevante; separar evidência de opinião.
4. **Experimentar:** reduzir a incerteza mais importante com spike, teste de
   carga, protótipo descartável ou mudança limitada.
5. **Comparar:** avaliar benefícios, custos, riscos, reversibilidade e custo de
   oportunidade sob os mesmos critérios.
6. **Decidir:** registrar escolha, responsáveis, consequências e dissensos
   relevantes.
7. **Observar:** verificar resultados e revisar quando fatos ou restrições mudam.

Experimento útil possui hipótese e critério de término. “Testar Kafka” não diz
o que aprender; “verificar se a equipe consegue operar a ordenação exigida sob o
perfil de falha X” delimita aprendizado. Protótipo não prova operação prolongada
e não deve entrar em produção por inércia.

### Evidência, risco e reversibilidade

Documentação oficial sustenta comportamento contratado; benchmark próprio
responde ao workload local; incidente anterior revela modos de falha, mas não
prova causalidade em todo contexto. Registre qualidade e limite da evidência.

Risco combina probabilidade e impacto, mas também detectabilidade e tempo de
recuperação. Uma decisão facilmente reversível pode começar pequena e aprender
em produção com guardrails. Uma migração destrutiva ou compromisso público pede
mais evidência antecipada. *Timebox* limita custo de análise, não autoriza ignorar
risco crítico.

O [modelo de decisão técnica](../templates/registro-decisao-tecnica.md) oferece o
mínimo necessário. Para decisões arquiteturais duráveis, C19 aprofunda
Architecture Decision Records (ADRs). Registros preservam contexto; não congelam
a escolha. Ao revisar, mantenha o histórico e ligue a decisão sucessora.

## Code review é gestão compartilhada de risco

Code review busca reduzir defeitos, distribuir contexto e melhorar a capacidade
de evolução. Não substitui testes, automação, design colaborativo nem
responsabilidade de quem escreveu. O revisor deve entender intenção e risco
antes de discutir estilo já coberto por ferramenta.

### Dimensões da revisão

- **Correção:** fluxos principal, de falha, concorrência, cancelamento e bordas
  preservam contratos e invariantes?
- **Legibilidade:** nomes, estrutura e tipos comunicam a intenção para quem dará
  manutenção?
- **Testabilidade:** o comportamento relevante pode ser controlado e observado
  sem acoplamento desnecessário à implementação?
- **Segurança:** entrada, autenticação, autorização, dados, segredos, dependências
  e abuso foram considerados?
- **Performance:** existe risco plausível no caminho ou volume afetado, e a
  alegação está apoiada por medida adequada?
- **Manutenibilidade:** a solução local aumenta duplicação, acoplamento,
  dependências ou custo operacional futuro?
- **Entrega:** contratos, migrations, telemetria e estratégia de recuperação
  permitem convivência e diagnóstico?

Revisão baseada em risco investe atenção onde uma falha causa maior dano ou é
difícil de reverter. Uma alteração mecânica validada automaticamente merece
menos escrutínio que autorização, cobrança, concorrência ou schema. Arquivos
gerados podem ser verificados pela origem; código crítico pede revisores com
contextos complementares. Pull requests menores diminuem carga cognitiva, mas
uma mudança artificialmente fragmentada não deve esconder a invariante completa.

### Feedback que permite ação

Um comentário útil localiza a observação, explica consequência e convida a uma
ação verificável. Diferencie:

- **bloqueio:** risco de correção, segurança, contrato ou operação que precisa de
  resolução antes da integração;
- **sugestão:** melhoria valiosa, mas não condição de aprovação;
- **dúvida:** pedido de contexto, não acusação disfarçada;
- **acompanhamento:** trabalho aceito fora do escopo, com responsável e registro.

Prefira “esta escrita pode ocorrer duas vezes após timeout; podemos proteger por
chave idempotente?” a julgamento sobre a pessoa. Quem recebe deve responder ao
argumento, trazer evidência ou ajustar a mudança; concordar por hierarquia não
produz segurança. Se o debate excede a mídia assíncrona, converse de forma
síncrona e registre a conclusão no review.

### Critério de aprovação

Aprovar significa que o risco residual é aceitável para o escopo e que as
evidências combinadas são suficientes, não que o revisor escreveria igual.
Bloqueios foram resolvidos, testes relevantes passam, políticas são atendidas e
acompanhamentos estão explícitos. Preferência pessoal sem efeito demonstrável não
é bloqueio. O [checklist orientado a risco](../templates/checklist-code-review.md)
pode ser adaptado por equipe; não use cada item indiscriminadamente.

## Colaboração em decisões difíceis

Discordância técnica pode revelar restrições ocultas. Antes de defender solução,
repita o problema, encontre o ponto exato de divergência e pergunte qual evidência
mudaria cada posição. Compare opções por critérios acordados. Quando valores
entram em conflito — prazo, segurança, custo ou autonomia — quem possui a decisão
deve estar explícito e ouvir quem arcará com as consequências.

Métodos de decisão variam:

- consenso funciona quando compromisso coletivo é essencial e o tempo permite;
- *consent* aceita uma proposta sem objeção fundamentada em dano;
- responsável único decide após consulta quando velocidade e clareza importam;
- experimento resolve divergência factual mensurável;
- escalonamento é apropriado quando risco excede a autoridade do grupo.

Nenhum método justifica apagar dissenso. Registre risco aceito e critério de
revisão. “Discordar e comprometer-se” exige segurança ética e operacional; não se
aplica a violação conhecida de segurança, lei ou responsabilidade profissional.

### Mentoria e conhecimento compartilhado

Mentoria desenvolve capacidade de decisão, não dependência do mentor. Torne o
raciocínio visível, faça perguntas graduais, ofereça contexto e devolva autonomia
com limites seguros. Pairing e mobbing ajudam em trabalho ambíguo ou de alto
risco; revisão assíncrona preserva foco e escala conhecimento. Rotação de
responsabilidades, documentos curtos, demonstrações e retrospectivas reduzem
concentração de informação.

Ownership significa autoridade para mudar e dever de observar consequências.
Não deve significar “única pessoa que entende”. Uma equipe responsável mantém
runbooks, telemetria, sucessão e fronteiras de suporte. Incidentes devem produzir
aprendizado sobre sistema e processo, não busca de culpado; C28 trata o ciclo de
resposta.

## Dívida técnica como decisão econômica

Dívida técnica é uma escolha ou condição que reduz capacidade futura e cobra
juros em mudança, falha ou operação. Pode ser deliberada para antecipar valor,
acidental por aprendizado ou herdada. Nem todo código antigo é dívida, e “eu
prefiro outro padrão” não demonstra dívida.

Identifique por efeito observável:

- mudanças rotineiras exigem coordenação ou retrabalho desproporcional;
- defeitos e incidentes concentram-se na mesma área;
- build, testes ou deploy atrasam feedback;
- dependência obsoleta amplia exposição ou bloqueia evolução;
- conhecimento concentrado torna manutenção e plantão frágeis;
- solução impede requisito provável ou objetivo de confiabilidade.

Registre principal, juros e risco: qual atalho existe, quais atividades encarece,
com que frequência, qual impacto operacional e qual evidência sustenta isso.
Evite converter estimativas frágeis em precisão financeira. Uma descrição como
“cada alteração de preço exige três equipes e duas releases coordenadas” é mais
acionável que “arquitetura ruim”.

### Priorizar e negociar

Compare dívida com produto e risco usando critérios comuns: impacto, frequência,
probabilidade, urgência, custo de atraso, esforço, reversibilidade e alinhamento
com trabalho próximo. Segurança crítica ou risco de perda de dados pode exigir
ação imediata; um módulo estável e isolado pode conviver com imperfeições.

Opções de tratamento incluem:

- refatorar incrementalmente ao tocar a área, protegido por testes;
- reservar uma intervenção curta para remover um gargalo conhecido;
- encapsular a dívida atrás de uma fronteira e limitar propagação;
- substituir componente quando custo total justificar;
- aceitar conscientemente, com sinal e data de revisão.

Grandes reescritas adiam valor e podem recriar comportamentos desconhecidos. O
*Strangler Fig* e testes de caracterização permitem mudança em fatias; C18 e C24
detalham esses mecanismos. A regra do escoteiro ajuda em melhorias locais, mas
não resolve risco sistêmico sem ownership e prioridade explícita.

Ao negociar prazo e qualidade, declare a proteção que será reduzida, a
consequência possível e a recuperação. Escopo pode ser cortado; integridade de
dados, autorização ou capacidade básica de recuperar não são preferências. Se um
atalho for aceito, crie acompanhamento com responsável e condição objetiva. Um
backlog infinito sem revisão apenas arquiva o problema.

### Acompanhar resultado, não volume

Contar itens de dívida fechados incentiva fragmentação. Observe tempo de mudança,
falhas recorrentes, dependências bloqueadas, esforço de suporte e concentração de
conhecimento. Após a intervenção, compare com a hipótese original. Se o efeito
não mudou, atualize o diagnóstico em vez de declarar sucesso pela refatoração.

## Armadilhas e sinais de revisão

- decidir por autoridade ou popularidade sem explicitar critérios;
- pedir números precisos onde só existem suposições não testadas;
- usar review para redesenhar indefinidamente uma mudança já adequada;
- confundir padronização útil com preferência estética;
- celebrar velocidade transferindo validação e recuperação para quem opera;
- chamar todo incômodo de dívida ou todo legado de erro;
- criar ação sem responsável, prazo ou sinal que provoque revisão.

Reavalie o processo quando decisões se repetem sem histórico, reviews acumulam,
as mesmas pessoas bloqueiam o fluxo, exceções viram permanentes ou a equipe não
consegue relacionar trabalho técnico a risco e resultado.

## Relação com outros capítulos

- [C19](../dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md)
  aprofunda ADRs e documentação viva.
- [C23](../qualidade-e-seguranca/c23-testes-comportamento-risco.md) orienta
  evidências de comportamento; C24 aplica caracterização a legado.
- [C18](../sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md)
  trata migração incremental e limites de ownership.
- [C29](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md)
  aplica governança a pipeline, deploy e contratos.
- C31–C35 demonstram o processo de investigação e decisão em cenários didáticos.

## Referências

- [Architecture Decision Records](https://adr.github.io/)
  — comunidade ADR, acesso em 18 novembro 2026.
- [Google Engineering Practices: Code Review](https://google.github.io/eng-practices/review/)
  — Google, acesso em 18 novembro 2026.
- [Code review guidelines](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/development-process-for-azure#code-reviews)
  — Microsoft, acesso em 18 novembro 2026.
- [Blameless postmortem culture](https://sre.google/sre-book/postmortem-culture/)
  — Google SRE Book, acesso em 18 novembro 2026.
