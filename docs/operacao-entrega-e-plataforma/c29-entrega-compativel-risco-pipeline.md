# C29 — Entrega compatível e gestão de risco no pipeline

> **Nível:** Avançado
>
> **Objetivo:** desenhar um fluxo de entrega que produza evidências rápidas,
> artefatos rastreáveis e mudanças compatíveis, com risco proporcionalmente
> controlado.
>
> **Pré-requisitos:** [C10 — Fundamentos de persistência SQL e NoSQL](../aplicacoes-e-dados/c10-fundamentos-persistencia-sql-nosql.md),
> [C23 — Testes orientados a comportamento e risco](../qualidade-e-seguranca/c23-testes-comportamento-risco.md),
> [C26 — Segurança nas fronteiras da aplicação](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md),
> [C27 — Telemetria e diagnóstico distribuído](c27-telemetria-diagnostico-distribuido.md)
> e [C28 — Confiabilidade de serviço e resposta a incidentes](c28-confiabilidade-resposta-incidentes.md).
>
> **Última revisão:** 2026-11-18

Entregar software não é transportar arquivos até um servidor. É transformar uma
alteração em um artefato identificável, acumular evidências sobre ele e controlar
sua exposição. Quanto maior o intervalo entre mudança e feedback, mais contexto
se perde; quanto mais controles indiscriminados, maior o incentivo para
contorná-los. Este capítulo integra CI/CD, qualidade, segurança, estratégias de
deploy e evolução de contratos. Os fundamentos de testes, telemetria e resposta
a incidentes permanecem nos capítulos canônicos indicados acima.

## Integração, entrega e deployment contínuos

**Continuous Integration (CI)** é a prática de integrar mudanças pequenas com
frequência e verificar automaticamente se o repositório continua saudável. Ela
depende de branch de vida curta, build confiável e falha visível; uma execução
noturna sobre mudanças acumuladas é automação, mas oferece integração tardia.

**Continuous Delivery** mantém cada alteração aprovada em condição de ser
implantada, embora a decisão de liberar possa ser humana. **Continuous
Deployment** automatiza também a promoção até produção após os controles. A
diferença é o último ato, não a ausência de governança. Sistemas regulados podem
usar delivery contínuo com segregação de funções; serviços de baixo impacto
podem usar deployment contínuo com canary e interrupção automática.

Um pipeline é a representação executável dessa política:

```text
mudança → lint/build/testes rápidos → análises → artefato imutável
        → validações de maior fidelidade → promoção → observação → decisão
```

O mesmo artefato deve atravessar ambientes. Recompilar para produção permite que
fonte, dependências ou ferramenta mudem depois da validação. Identifique o
artefato por commit e digest, mantenha metadados de origem e promova por
referência. Versionamento semântico pode comunicar compatibilidade de biblioteca;
serviços implantados continuamente também precisam de identidade imutável e
histórico, mesmo que a versão pública seja outra.

### Builds reproduzíveis e proveniência

Um build reproduzível permite reconstruir saídas equivalentes a partir das
mesmas entradas. Fixe SDK, dependências diretas e ferramentas; use fontes de
pacote explícitas, ambiente efêmero e configuração versionada. Lock files fixam
o grafo transitivo quando o custo de atualização explícita é aceitável. Imagens
por digest reduzem ambiguidade. Timestamps, caminhos absolutos, geração aleatória
e downloads não fixados prejudicam a reprodução.

Reprodução não prova legitimidade. Proveniência responde quem ou qual workflow
produziu o artefato, a partir de qual commit e com quais entradas. Assinatura,
atestação e Software Bill of Materials (SBOM) podem complementar o controle,
conforme o risco da cadeia de suprimentos. Segredos não pertencem ao artefato,
ao log nem ao código; credenciais temporárias e identidades do workload reduzem
o impacto de vazamento.

## Qualidade organizada por custo e informação

Um gate deve bloquear um risco definido e explicar como agir. Uma sequência
prática separa feedback por custo:

| Momento | Verificações | Intenção e limite |
| --- | --- | --- |
| local/commit | formatação, lint, compilação incremental, unitários focados | segundos; não prova integrações |
| pull request | build limpo, unitários, análise estática, SAST e testes de componente | feedback obrigatório antes de integrar |
| paralelo no PR | integração, contrato, SCA e cobertura da mudança | aumenta confiança sem serializar tudo |
| artefato candidato | smoke, migração, E2E crítico e políticas | valida a unidade que será promovida |
| após deploy | health, erro, latência, saturação e sinais de negócio | detecta comportamento no ambiente real |

**Lint** encontra desvios mecânicos. **Análise estática** procura defeitos no
código sem executá-lo; **Static Application Security Testing (SAST)** prioriza
classes de vulnerabilidade. **Software Composition Analysis (SCA)** inventaria
componentes e vulnerabilidades conhecidas. SAST não substitui modelagem de
ameaças; SCA não decide sozinho se uma vulnerabilidade é alcançável.

Cobertura informa o que a suíte executou, não se os asserts protegem o risco. Um
gate de cobertura deve observar tendência ou código alterado e aceitar exceção
justificada, em vez de premiar testes sem valor. C23–C25 detalham níveis, legado,
cobertura e testes de carga.

Jobs independentes podem rodar em paralelo; passos que reutilizam a mesma saída
ficam juntos ou consomem o artefato imutável. Separe falha da aplicação de falha
da infraestrutura, defina timeout e torne logs diagnósticos. Cache acelera
downloads, mas não deve ser a única cópia de uma saída nem aceitar conteúdo de
origem não confiável. Proteja workflows: permissões mínimas, ações fixadas de
forma compatível com a política, revisão de mudanças e nenhum segredo em código
executado por contribuição não confiável.

O workflow em `.github/workflows/validar-documentacao.yml` demonstra os exemplos
A e B: checkout somente leitura, SDK definido por `global.json`, restore, build,
testes e inventário de dependências, sem publicar artefato nem implantar. Em um
repositório com política mais rigorosa, fixe actions por SHA e automatize a
renovação dessas referências.

### Velocidade e proteção

Executar todo teste em série a cada tecla reduz risco em teoria e destrói
feedback na prática. Selecione pelo raio de impacto, paralelize o que for
independente e mantenha uma trilha periódica mais ampla. Não silencie teste
instável: coloque-o em quarentena com responsável e prazo, preservando a
visibilidade do risco. Dados históricos de duração e falhas ajudam a mover um
controle para o estágio adequado.

Governança útil define propriedade, evidência, validade da exceção e trilha de
auditoria. Aprovação manual sem informação apenas adiciona fila. Um bypass
emergencial precisa ser explícito, restrito, auditado e seguido por revisão; se
ele vira rotina, o gate ou a capacidade da equipe está mal desenhado.

## Estratégias de deployment

Deployment altera a versão em execução; release torna uma capacidade disponível
ao usuário. Feature flags podem separar os dois. As estratégias combinam-se:

| Estratégia | Mecânica | Benefício | Risco dominante |
| --- | --- | --- | --- |
| rolling | substitui instâncias gradualmente no mesmo ambiente | baixo custo adicional | versões convivem e a capacidade cai durante a rotação |
| blue-green | prepara ambiente novo e troca o tráfego | troca e retorno rápidos | duplicação temporária e estado externo compartilhado |
| canary | expõe pequena parcela e amplia por etapas | limita raio de impacto | amostra ou métricas podem não representar o risco |
| feature flag | decide comportamento em runtime | separa código de ativação | dívida de flags e combinações de estado |

Escolha por tolerância à indisponibilidade, capacidade de coexistência, custo de
ambiente, reversibilidade de dados, segmentação de tráfego e qualidade dos sinais.
Rolling é simples quando versões são compatíveis. Blue-green ajuda em troca
atômica, mas não reverte uma escrita destrutiva. Canary exige volume suficiente,
janelas e critérios definidos; para evento raro e crítico, testes e aprovação
podem informar mais que uma pequena amostra.

Feature flag não é autorização. Avalie a flag em um ponto explícito, monitore
cada variante, defina proprietário e data de remoção. O laboratório em
`exemplos/c29-entrega/` mantém os dois comportamentos testáveis e mostra a decisão
por contexto. Execute com:

```bash
dotnet test exemplos/c29-entrega/C29.Entrega.Tests.csproj
```

O exemplo usa uma fonte de flags em memória e não cobre distribuição de
configuração, consistência entre instâncias nem auditoria; essas responsabilidades
precisam de desenho próprio em produção.

### Rollback, rollforward e observação

Rollback restaura código anterior quando seu artefato ainda funciona com o
estado atual. Rollforward entrega uma correção compatível quando desfazer é mais
arriscado, por exemplo após dados já transformados. Defina antes do deploy:

- sinais de sucesso e guardrails de erro, latência, saturação e negócio;
- janela de observação e responsável pela decisão;
- limiar para pausar, voltar ou avançar;
- versão e comandos de recuperação ensaiados;
- tratamento de filas, caches, jobs e migrations em andamento.

Health check verde apenas mostra a condição programada. Compare baseline e
canary, segmente por versão e acompanhe efeitos atrasados. Ao detectar dano,
primeiro limite o impacto; preserve evidências sem atrasar a mitigação, conforme
C28.

## Evolução de esquemas e contratos

Deploy independente implica convivência de versões. Compatibilidade retroativa
permite que um consumidor existente entenda a nova saída; compatibilidade para
frente permite que um leitor mais antigo tolere elementos adicionais. Contrato
inclui semântica, defaults, erros, ordenação e tempo, não apenas formato.

O padrão **expand-migrate-contract** divide uma mudança destrutiva:

1. **Expandir:** adicionar coluna, campo, endpoint ou evento opcional; leitores
   antigos continuam válidos.
2. **Migrar:** publicar produtores capazes de preencher o novo formato, fazer
   backfill observável e mover consumidores gradualmente. Dual write requer uma
   estratégia para falha parcial; quando possível, uma escrita autoritativa e
   projeção evitam divergência.
3. **Contrair:** depois de provar que nenhum consumidor usa o contrato antigo,
   remover leitura, escrita e estrutura obsoletas em outro deploy.

Exemplo conceitual: em vez de renomear `nome` para `nome_completo` na mesma
release, adicione `nome_completo` anulável, faça a aplicação ler o novo campo com
fallback, preencha lotes pequenos com checkpoint, passe a escrever o novo valor,
observe usos antigos e só então remova `nome`. Cada etapa deve poder parar sem
corromper o estado.

Migrations de banco devem considerar locks, duração, crescimento de log,
replicação e capacidade de recuperação. Alterações de metadados podem ser baratas
em uma versão do banco e reescrever tabela em outra; ensaie com volume
representativo. DDL transacional ajuda, mas não torna reversível uma perda de
dados. Backups só protegem se restauração e objetivo de recuperação forem
testados.

Em APIs, prefira adições compatíveis e política explícita de depreciação. Crie
nova versão quando a semântica não puder coexistir; manter versões indefinidas
também tem custo. Em eventos, catalogue consumers, preserve campos antigos até a
migração comprovada e evite reutilizar nome com significado novo. Consumer-driven
contract tests oferecem evidência, mas não descobrem consumidores desconhecidos;
telemetria e registro de ownership completam a visão.

## Critério integrado de liberação

Uma decisão de liberar combina probabilidade, impacto e capacidade de detectar e
recuperar. Antes de promover, responda:

1. qual comportamento e quais contratos mudaram;
2. quais evidências cobrem os modos de falha relevantes;
3. quais versões coexistirão e por quanto tempo;
4. como limitar exposição e observar sucesso;
5. qual estado impede rollback e qual rollforward está preparado;
6. quem decide e como registrar exceções.

Nem todo alerta de dependência bloqueia uma correção urgente, e nem toda suíte
verde autoriza uma mudança irreversível. A decisão deve registrar severidade,
alcançabilidade, exposição, compensações e prazo. O C35 aplica esse raciocínio a
uma dependência vulnerável.

## Armadilhas e sinais de revisão

- reconstruir por ambiente e perder a identidade do artefato;
- usar long-lived branches e descobrir conflitos no fim;
- colocar testes lentos antes de feedback diagnóstico barato;
- tratar cobertura, scanner ou aprovação como prova isolada;
- reverter código sem considerar schema, mensagens e efeitos já emitidos;
- manter flags e contratos antigos sem proprietário nem critério de remoção;
- executar migration destrutiva junto da primeira versão que exige o novo schema.

Reavalie o pipeline quando tempo de feedback cresce, bypasses aumentam, falhas
chegam repetidamente à produção ou a equipe não consegue explicar por que um gate
existe. O objetivo não é maximizar etapas, mas reduzir risco com informação útil.

## Relação com outros capítulos

- C23–C25 detalham estratégia de testes, integração, cobertura, E2E e carga.
- [C26](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md)
  fundamenta proteção do pipeline e dependências.
- [C27](c27-telemetria-diagnostico-distribuido.md) e
  [C28](c28-confiabilidade-resposta-incidentes.md) sustentam observação e reação.
- [C18](../sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md)
  trata limites, Strangler Fig e independência de deploy.

## Referências

- [Continuous integration](https://martinfowler.com/articles/continuousIntegration.html)
  — Martin Fowler, acesso em 18 novembro 2026.
- [Deployment strategies](https://learn.microsoft.com/azure/architecture/framework/devops/release-engineering-cd#safe-deployment-practices)
  — Microsoft Azure Well-Architected Framework, acesso em 18 novembro 2026.
- [Workflow syntax for GitHub Actions](https://docs.github.com/actions/reference/workflows-and-actions/workflow-syntax)
  — GitHub Docs, acesso em 18 novembro 2026.
- [Secure use reference](https://docs.github.com/actions/reference/security/secure-use)
  — GitHub Docs, acesso em 18 novembro 2026.
- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)
  — Microsoft, acesso em 18 novembro 2026.
