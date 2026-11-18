# C34 — Migração gradual de monólito

> **Nível:** Avançado
>
> **Objetivo:** investigar limites de domínio e aplicar Strangler Fig sem criar
> contratos, dados e operação acoplados entre monólito e novo serviço.
>
> **Pré-requisitos:** [C12 — Serviços de domínio e integração](../dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md),
> [C18 — Limites de microserviços e evolução de monólitos](../sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md),
> [C27 — Telemetria](../operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md)
> e [C29 — Entrega compatível](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md).
>
> **Última revisão:** 2026-11-18

Este cenário didático fictício não presume que microserviços sejam o destino
correto. A investigação pode recomendar manter ou modularizar o monólito.

## Cenário

Um monólito atende pedidos, catálogo, estoque e faturamento. Deploys coordenam
equipes e uma área muda com frequência diferente. A organização considera extrair
uma capacidade por vez sem interromper o produto.

## Contexto técnico

O processo compartilha banco e modelos internos. Jobs atualizam várias tabelas em
uma transação. Há integrações que conhecem IDs e estados do monólito. Telemetria
mostra rotas, mas não dependências entre módulos nem autoridade sobre dados.

## Sintomas ou problema

Mudanças em uma capacidade exigem regressão ampla e release conjunto. Isso pode
resultar de limite mal definido, dependência de dados, pipeline lento ou ownership
difuso; separar processos sem diagnóstico pode apenas adicionar rede.

## Restrições

- invariantes financeiras e de estoque não podem ser quebradas;
- clientes existentes precisam continuar operando;
- não haverá migração integral em uma janela única;
- monólito e serviço coexistirão por meses;
- a equipe precisa operar a topologia intermediária;
- replicar dados pessoais amplia obrigações de proteção.

## Hipóteses

1. A capacidade candidata possui linguagem, regras e cadência próprias.
2. O banco compartilhado é o maior acoplamento e impede deploy independente.
3. A maioria das mudanças ainda atravessa capacidades; extração não dará
   autonomia.
4. Uma fachada e modularização interna capturam benefício com menor custo.
5. Tráfego síncrono entre limites criará uma cadeia mais frágil que o monólito.

## Processo de investigação

Mapeie jornadas, decisões de negócio, invariantes, tabelas lidas/escritas,
transações, chamadas e frequência de mudança conjunta. Converse com quem opera e
com quem entende regras. Use tracing temporário e análise de repositório para
validar o mapa; diagrama sozinho não prova dependência.

Escolha uma fatia por coesão e baixo risco, não pela classe mais fácil de mover.
Defina contrato, autoridade de dados, SLO e modelo de falha. Faça um experimento
com roteamento sombreado ou leitura sem efeito para medir latência, divergência e
capacidade operacional. Compare também a alternativa de módulo interno.

## Alternativas

- manter monólito e impor módulos e ownership de dados;
- extrair capacidade por Strangler Fig através de fachada/roteador;
- extrair somente processamento assíncrono com contrato de evento;
- criar serviço que ainda lê o banco compartilhado como etapa transitória;
- reescrever o produto e fazer troca única;
- comprar serviço externo e manter uma Anti-Corruption Layer.

## Trade-offs

Monólito modular preserva transação e simplicidade operacional, mas mantém
deploy conjunto. Strangler entrega valor em fatias e cria coexistência longa.
Banco compartilhado acelera transição e impede autonomia, além de permitir
acoplamento novo. Cópia de dados reduz chamadas e introduz consistência eventual,
reconciliação e proteção adicional. Chamada síncrona oferece resultado imediato
e transfere disponibilidade para o caminho crítico.

## Estratégia de solução

Primeiro torne o limite explícito dentro do monólito: API interna, ownership e
testes de contrato. Coloque fachada na entrada e direcione somente a fatia
selecionada. O novo serviço recebe identificadores e termos do próprio contexto,
traduzidos por Anti-Corruption Layer.

Escolha uma fonte de verdade por dado. Migre com snapshot e mudanças incrementais
ou eventos de Outbox, com checkpoint e reconciliação; evite dual write ingênuo.
Durante a convivência, contratos são aditivos e consumers toleram versões. Use
feature flag/roteamento para ampliar tráfego, compare resultados quando isso não
duplica efeitos e mantenha caminho de retorno compatível. Remova código e tabelas
antigos somente depois de provar ausência de uso.

## Validação

- testes de contrato cobrem semântica, erros e compatibilidade;
- reconciliação mede divergência e possui procedimento de reparo;
- traces atravessam fachada, monólito, serviço e mensagens;
- dashboards separam versão, rota e ownership, com SLO definido;
- ensaios cobrem indisponibilidade, atraso e redelivery;
- uma mudança representativa é implantada sem release coordenado;
- custo operacional e fluxo de desenvolvimento são comparados à baseline.

## Riscos remanescentes

Jornadas podem continuar atravessando limites e exigir saga. A camada temporária
pode tornar-se permanente. Eventos antigos podem não conter dados necessários.
Reconciliação pode revelar regras implícitas. Duas equipes podem disputar
ownership. Mais unidades implantáveis aumentam plantão, segurança e custo.

## Aprendizados

Extração bem-sucedida é transferência de autoridade, contrato e responsabilidade,
não movimentação de código. Strangler Fig reduz risco ao permitir fatias e
retorno, mas a topologia intermediária precisa ser tratada como sistema real.
Modularizar pode ser a decisão final quando não há autonomia econômica a ganhar.

## Referências para capítulos relacionados

- [C12 — Integração entre modelos](../dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md)
- [C15 — Comunicação](../sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md)
- [C17 — Sagas](../sistemas-distribuidos/c17-consistencia-eventual-sagas.md)
- [C18 — Evolução de monólitos](../sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md)
- [C29 — Compatibilidade](../operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md)

## Referências

- [Strangler Fig application](https://martinfowler.com/bliki/StranglerFigApplication.html)
  — Martin Fowler, acesso em 18 novembro 2026.
