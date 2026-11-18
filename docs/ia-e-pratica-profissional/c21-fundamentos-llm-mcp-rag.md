# C21 — Fundamentos de LLM, MCP e RAG

> **Nível:** Intermediário
>
> **Objetivo:** projetar aplicações com modelos de linguagem, integrações MCP e
> recuperação de conhecimento com contratos, avaliação e limites de segurança
> explícitos.
>
> **Pré-requisitos:** [C13 — Dependências, abstrações e padrões com
> propósito](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md),
> [C19 — Documentação arquitetural viva](../dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md)
> e [C23 — Testes orientados a comportamento e
> risco](../qualidade-e-seguranca/c23-testes-comportamento-risco.md).
>
> **Última revisão:** 2026-11-18

Uma aplicação com Large Language Model (LLM) continua sendo uma aplicação: tem
entradas não confiáveis, dependências, orçamento, latência, autorização e modos
de falha. A diferença é que parte do comportamento é probabilístico e difícil de
especificar somente com exemplos unitários. Prompts não substituem contratos;
modelos não substituem políticas; uma resposta fluente não constitui evidência.

Este capítulo estabelece a base. O [C22](c22-agentes-orquestracao-sdd.md) trata
agentes, padrões de orquestração e desenvolvimento orientado por especificações.
Segurança geral de fronteiras, resiliência HTTP e estratégia de testes permanecem
em seus capítulos canônicos.

## Do modelo à aplicação

Um modelo de linguagem estima uma distribuição sobre possíveis próximos tokens
dado o contexto recebido. Durante a **inferência**, pesos já treinados são usados
para produzir uma saída; a aplicação prepara mensagens, seleciona opções de
amostragem, envia a requisição e interpreta o resultado. O modelo não consulta
automaticamente dados atuais, não conhece a autorização do usuário e não garante
verdade factual.

### Tokens e janela de contexto

Tokens são as unidades processadas pelo modelo. Podem corresponder a palavras,
partes de palavras, pontuação ou bytes, conforme o tokenizador. Contar caracteres
ou palavras é apenas aproximação. Entrada, histórico, definições de ferramentas,
trechos recuperados e saída disputam o orçamento da **context window**.

Uma janela maior não significa memória ilimitada nem uso uniforme de toda a
entrada. Conteúdo pode ser truncado, informação relevante pode se perder em meio
a ruído e mais tokens elevam custo e latência. A aplicação deve definir:

- política de truncamento e resumo, sem ocultar fatos obrigatórios;
- limite de saída e reserva para respostas e chamadas de ferramenta;
- precedência entre instruções, dados do usuário e contexto recuperado;
- identificação de origem para cada fragmento inserido;
- comportamento quando o material não cabe ou a evidência é insuficiente.

Histórico de conversa é dado da aplicação, não memória intrínseca do modelo. Se
uma decisão precisa sobreviver a requisições, persista estado estruturado fora do
prompt e controle retenção e acesso.

### Temperatura, amostragem e variabilidade

A temperatura altera a distribuição usada na escolha de tokens: em geral,
valores menores concentram probabilidade nas opções mais prováveis; valores
maiores aumentam diversidade e risco de desvio. Outros controles, como top-p,
seed e penalidades, são específicos da API e não devem ser presumidos como
equivalentes entre modelos.

Temperatura zero reduz variabilidade, mas não garante determinismo. Mudanças de
modelo, infraestrutura, paralelismo, contexto, versão do serviço ou desempate
numérico podem mudar a saída. Para regras obrigatórias, use código convencional;
para saída gerada, valide forma e invariantes.

### Alucinação, ou confabulação

Uma alucinação é conteúdo incorreto, inventado ou incompatível com a entrada,
apresentado de modo plausível. É consequência possível da geração estatística,
não uma exceção de transporte. RAG, prompt engineering e modelos maiores podem
reduzir alguns erros, mas não eliminam o fenômeno. Até citações podem ser
inventadas ou não sustentar a afirmação.

Mitigações se combinam:

- permitir resposta “não sei” e exigir evidência para afirmações factuais;
- restringir a tarefa e solicitar saída estruturada;
- recuperar fontes autorizadas e incluir identificadores verificáveis;
- validar schemas, referências e regras fora do modelo;
- encaminhar baixa confiança ou alto impacto para revisão humana;
- avaliar continuamente casos reais, adversariais e de regressão.

### Custo e latência como requisitos

O custo total não é apenas preço por token. Inclui embeddings, armazenamento,
recuperação, re-ranking, chamadas repetidas, observabilidade, avaliação e revisão
humana. Meça por caso de uso e resultado útil, não apenas por requisição.

A latência combina fila, rede, processamento de entrada, geração de saída,
ferramentas e eventuais tentativas. Streaming melhora tempo até o primeiro token,
mas não reduz necessariamente tempo total e dificulta validar antes de exibir.
Defina timeout e cancelamento ponta a ponta; retry cego pode duplicar custo e
ações. O [C07](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md)
mantém o tratamento canônico de resiliência HTTP.

## Contratos ao redor do modelo

Isole a integração por capacidade da aplicação, não por nome comercial. Um
contrato útil explicita entrada, limite de saída, cancelamento e metadados de uso.
A resposta do modelo atravessa uma fronteira não confiável:

1. valide tamanho, encoding e conteúdo permitido da entrada;
2. construa instruções com dados delimitados e proveniência;
3. chame o modelo com timeout, orçamento e correlação;
4. faça parse com schema estrito;
5. valide regras de domínio e autorização fora do modelo;
6. registre decisão, versões e métricas sem armazenar segredos por padrão;
7. recuse, repita de forma limitada ou encaminhe a uma pessoa.

Saída estruturada aumenta a confiabilidade sintática, não a correção semântica.
Um JSON válido ainda pode conter uma categoria inexistente ou um valor proibido.

### Avaliação de resultados

Testes tradicionais continuam cobrindo parsing, autorização, fallback e
orquestração. O comportamento probabilístico pede um conjunto versionado de casos
com entrada, contexto, resposta esperada ou rubrica e severidade. Separe:

| Dimensão | Pergunta e exemplos de métrica |
| --- | --- |
| tarefa | a resposta resolve o caso? acurácia, F1, sucesso do fluxo |
| fundamentação | afirmações são sustentadas pelos trechos? precisão de citação |
| recuperação | a evidência relevante apareceu? recall@k, MRR, nDCG |
| segurança | recusou ações ou dados indevidos? taxa por cenário adversarial |
| operação | cabe no orçamento? tokens, custo, p50/p95, falhas e retries |
| experiência | é útil e calibrada? avaliação humana com rubrica |

Avaliação por outro modelo pode escalar triagem, mas também erra e compartilha
vieses. Calibre o juiz contra avaliação humana, fixe versões e preserve amostras.
Uma média global pode esconder falhas graves; use cortes por idioma, grupo,
categoria e impacto. Mudança de prompt, modelo, índice ou ferramenta exige
regressão comparativa antes de promoção.

## Model Context Protocol como protocolo de integração

Model Context Protocol (MCP) padroniza como uma aplicação de IA descobre e usa
contexto e capacidades externas. Ele não é um modelo, framework de agentes,
orquestrador ou mecanismo de autorização corporativa completo. Um workflow
convencional pode consumir MCP, e um agente pode operar sem MCP.

Esta revisão usa a especificação oficial `2026-07-28`. Ela introduziu núcleo
stateless: cada requisição é autocontida e leva versão e capacidades. O SDK C#
oficial `2.4.0`, fixado no laboratório, implementa essa revisão e mantém
compatibilidade com revisões anteriores documentadas pelo projeto.

### Host, client e server

| Papel | Responsabilidade |
| --- | --- |
| host | contém a aplicação e o LLM, cria clients, agrega contexto, aplica consentimento e políticas |
| client | conector gerenciado pelo host, com relação 1:1 com um server |
| server | expõe capacidades focadas e recebe somente o contexto necessário |

O host é a fronteira de confiança principal. Ele decide quais servidores podem
ser conectados, o que chega ao modelo e quando uma ação pede confirmação. Um
server não deve enxergar toda a conversa nem outros servidores por padrão.

MCP usa mensagens JSON-RPC 2.0. Na revisão atual, `server/discover` permite
descobrir versões, identidade e capacidades antes de outra operação. Clients
declaram capacidades em cada requisição; servers anunciam as suas na descoberta.
Listas também podem mudar, ser paginadas e variar conforme a autorização.

### Tools, resources e prompts

- **tools** são funções com nome, descrição e JSON Schema de entrada, destinadas
  à invocação controlada pelo modelo ou pela aplicação;
- **resources** são dados endereçáveis por URI, como arquivos, schemas ou
  registros, incorporados ao contexto segundo decisão do host;
- **prompts** são templates de mensagens reutilizáveis, normalmente selecionados
  pelo usuário ou pela aplicação.

Essas primitivas descrevem integração, não confiança. Descrições, anotações,
conteúdo de resources e resultados de tools são entradas não confiáveis. O host
precisa validar schemas e política mesmo que o server declare uma operação como
segura.

### Transporte e comunicação

Os transportes padrão são:

- **stdio:** mensagens delimitadas por nova linha entre host e subprocesso local;
  stdout é reservado ao protocolo e logs devem ir para stderr;
- **Streamable HTTP:** cada mensagem usa POST em um endpoint MCP; a resposta é
  JSON ou stream SSE restrito à requisição.

O transporte não muda a semântica do protocolo. Stdio reduz exposição de rede,
mas executar um binário local continua sendo execução de código com as permissões
do processo. HTTP exige TLS, autenticação, proteção contra SSRF, CORS e validação
de origem quando aplicável, rate limiting e isolamento por identidade.

Revisões até `2025-11-25` usavam handshake `initialize`, sessão e podiam manter
streams bidirecionais. A revisão `2026-07-28` carrega metadados por requisição e
remove sessão do núcleo. Não projete estado de negócio escondido na conexão:
use identificadores explícitos, persistência e autorização por operação.

### Segurança, autorização e auditoria

A autorização MCP para HTTP é opcional no protocolo; quando usada, trata o
server protegido como resource server OAuth e o client como OAuth client. Isso
não dispensa políticas do domínio. Stdio não usa esse fluxo: credenciais locais
devem vir do ambiente ou de armazenamento seguro, nunca de argumentos visíveis,
prompt ou repositório.

Uma integração corporativa deve:

- autenticar usuário, workload e server; não reutilizar token destinado a outra
  audiência;
- conceder escopos mínimos por operação e filtrar a própria descoberta;
- manter credenciais fora do contexto do modelo e impedir token passthrough;
- executar server local em sandbox e com filesystem/rede mínimos;
- separar leitura, alteração reversível e ação sensível;
- pedir confirmação vinculada aos parâmetros finais para efeitos relevantes;
- registrar identidade, ferramenta, argumentos saneados, decisão de política,
  resultado, duração e correlação;
- limitar tamanho, frequência, concorrência, tempo e custo;
- revogar acesso e investigar chamadas sem depender de logs do modelo.

Auditoria não deve copiar prompts e resultados integrais indiscriminadamente.
Classifique dados, aplique minimização e retenção, mas preserve evidência suficiente
para atribuir uma ação. Consentimento não corrige permissão excessiva: o server
deve ser incapaz de executar o que o usuário e a aplicação não autorizaram.

## Retrieval-Augmented Generation

Retrieval-Augmented Generation (RAG) recupera conteúdo externo e o fornece ao
modelo no momento da geração. Resolve principalmente atualização, proveniência e
acesso a conhecimento fora dos parâmetros do modelo. Não treina automaticamente
o modelo, não substitui busca comum e não elimina alucinações.

### Pipeline de ingestão

1. **Aquisição:** conectores leem fontes autorizadas e preservam origem, versão,
   proprietário, classificação e ACL.
2. **Normalização:** parsing remove ruído sem perder estrutura relevante.
3. **Chunking:** documentos viram trechos com tamanho e sobreposição adequados à
   semântica; títulos e relações podem ser repetidos como metadados.
4. **Embeddings:** um modelo transforma conteúdo em vetores cuja proximidade
   aproxima similaridade, com limitações por domínio e idioma.
5. **Indexação:** vetores, texto e metadados são armazenados para busca; exclusão
   e nova versão precisam propagar.

Chunk fixo é uma baseline, não regra universal. Código, tabelas, políticas e
transcrições têm fronteiras diferentes. Trechos pequenos perdem contexto; grandes
ocupam janela e reduzem precisão. Embeddings não devem ser tratados como
anonimização: podem revelar informação e permanecem sujeitos à política da fonte.

### Pipeline de consulta

1. autentique o principal e derive os filtros de acesso fora do texto da busca;
2. normalize ou expanda a consulta sem mudar sua intenção;
3. recupere candidatos por busca lexical, vetorial ou híbrida;
4. aplique ACL antes que conteúdo proibido chegue ao ranking ou ao modelo;
5. faça ranking ou re-ranking e remova duplicações;
6. monte contexto dentro do orçamento, com delimitadores e proveniência;
7. gere a resposta instruindo o modelo a usar evidência e admitir insuficiência;
8. valide citações contra os trechos realmente enviados.

Busca vetorial ajuda em paráfrases; busca lexical é forte para identificadores,
nomes e termos exatos. A forma híbrida costuma ser uma boa candidata, mas precisa
ser medida. Re-ranking melhora ordenação à custa de latência e custo.

Uma citação deve apontar para fonte estável, versão e localização. O backend deve
associá-la ao trecho recuperado; não peça ao modelo para inventar URLs. Antes de
exibir uma fonte, revalide o acesso do usuário, pois autorização pode mudar entre
indexação e leitura.

### Atualização, qualidade e limitações

Defina SLA de atualização e exclusão. Use eventos, varredura incremental ou
reconciliação periódica; compare versão da origem com a do índice. Quando uma
fonte muda, remova trechos antigos de forma atômica ou torne a versão antiga
inelegível. Conteúdo órfão é falha de segurança e correção.

Avalie recuperação e geração separadamente. Um caso pode falhar porque a fonte
não foi ingerida, o chunk perdeu contexto, o retriever não trouxe a evidência, o
ranking a descartou ou o modelo ignorou o trecho. Sem separar etapas, trocar o
modelo mascara o defeito.

RAG também herda conteúdo incorreto, conflitante, malicioso ou desatualizado.
Prompt injection indireta em um documento pode tentar redefinir instruções e
acionar ferramentas. Trate o contexto como dados citáveis, nunca como instruções
privilegiadas. Para respostas determinísticas sobre dados estruturados, uma
consulta tipada e template pode ser superior a RAG.

## Segurança de aplicações com IA

**Prompt injection** é entrada que tenta alterar instruções ou obter ações fora
da intenção. Pode ser direta, no pedido do usuário, ou indireta, em páginas,
issues, documentos e resultados de ferramentas. Não existe delimitador de prompt
que sozinho a elimine. A defesa efetiva reduz o impacto possível:

| Risco | Controle arquitetural |
| --- | --- |
| vazamento e exfiltração | minimização, ACL antes do contexto, DLP, destinos de rede permitidos |
| ferramenta indevida | allowlist, schema estrito, autorização por chamada e credencial mínima |
| escalada de permissão | identidade do usuário propagada, policy enforcement fora do modelo |
| ação sensível | preview, confirmação vinculada, idempotência e reversibilidade |
| contexto malicioso | proveniência, separação instrução/dado, sanitização e sem ferramentas por padrão |
| saída insegura | encode por destino, validação de schema, consultas parametrizadas, nunca `eval` |
| investigação impossível | trilha de auditoria, versão de modelo/prompt/índice e correlação |

O modelo nunca deve decidir sozinho se o usuário possui permissão. Também não
deve construir comandos shell, SQL ou HTML e executá-los sem parser, allowlist e
validação apropriada ao destino. Confirmação deve mostrar efeito, alvo e dados
relevantes; “permitir que o agente prossiga?” é consentimento ambíguo.

## Laboratório executável

O diretório `exemplos/c21-c22-ia/` contém três demonstrações deste capítulo:

- `WorkflowTriagem`: workflow determinístico que chama `IGeradorTexto`, exige
  JSON, valida enum e confiança e encaminha baixa confiança à revisão;
- `PipelineRagLocal`: ingestão, chunking, embedding por hashing, índice em memória,
  ACL antes do ranking e citações verificáveis;
- `C21.McpServer`: server MCP stdio com uma única tool de leitura e allowlist.

O embedding por hashing é deliberadamente simples: permite executar sem modelo
ou serviço pago, mas não representa qualidade semântica de produção. O gerador é
um fake; conectar um modelo real exige implementar `IGeradorTexto`, configurar
credencial fora do código e acrescentar avaliações e testes de integração.

O server MCP usa o SDK C# oficial:

```xml
<PackageReference Include="ModelContextProtocol" Version="2.4.0" />
```

```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
```

Execute:

```powershell
dotnet test exemplos/c21-c22-ia/C21C22.Ia.Tests.csproj --configuration Release
dotnet run --project exemplos/c21-c22-ia/C21.McpServer.csproj
```

O segundo comando espera um client MCP em stdin e escreve protocolo em stdout;
não é uma interface de terminal interativa. Não há chave, rede ou serviço pago.
Para produção, faltam autenticação quando remota, isolamento do processo, quotas,
telemetria, catálogo versionado e testes de conformidade com o host escolhido.

## Critérios de decisão

- Use chamada direta ao modelo quando a tarefa é restrita e a aplicação controla
  o fluxo.
- Use busca ou consulta tipada sem geração quando o usuário precisa do dado, não
  de uma síntese.
- Use RAG quando fontes atualizáveis e citáveis realmente melhorarem a tarefa.
- Use MCP quando interoperabilidade entre hosts e capacidades justificar o
  protocolo; uma interface C# local continua mais simples dentro do processo.
- Não exponha uma capacidade como tool apenas porque ela existe: avalie impacto,
  identidade, reversibilidade, orçamento e auditoria.

Comece com baseline simples e casos de avaliação. Complexidade só se justifica
quando uma métrica de tarefa ou uma restrição operacional demonstra o ganho.

## Relação com outros capítulos

- [C07](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md) cobre
  timeouts, retry, contratos HTTP e idempotência.
- [C13](../dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md) orienta
  abstrações por capacidade e adapters de fornecedores.
- [C19](../dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md) mantém
  decisões, limites e ameaças visíveis.
- [C23](../qualidade-e-seguranca/c23-testes-comportamento-risco.md) define a
  estratégia de testes; avaliação de IA a complementa.
- [C26](../qualidade-e-seguranca/c26-seguranca-fronteiras-aplicacao.md) define
  autenticação, autorização por recurso, segredos e proteção de dados reutilizados
  por RAG e tools.
- [C22](c22-agentes-orquestracao-sdd.md) usa esta base em agentes e workflows.

## Referências

- [MCP Specification 2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28)
  — arquitetura, protocolo e princípios de segurança, acesso em 18 novembro 2026.
- [MCP architecture](https://modelcontextprotocol.io/specification/2026-07-28/architecture)
  — host, client, server e descoberta de capacidades, acesso em 18 novembro 2026.
- [MCP transports](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports)
  — stdio e Streamable HTTP, acesso em 18 novembro 2026.
- [MCP authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization)
  — autorização para HTTP e escopos, acesso em 18 novembro 2026.
- [MCP security best practices](https://modelcontextprotocol.io/docs/2026-07-28/tutorials/security/security_best_practices)
  — ameaças e mitigações, acesso em 18 novembro 2026.
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) — SDK oficial;
  versão `2.4.0` validada em 18 novembro 2026.
- [Retrieval-Augmented Generation for Knowledge-Intensive NLP
  Tasks](https://arxiv.org/abs/2005.11401) — trabalho original sobre RAG.
- [NIST AI 600-1](https://doi.org/10.6028/NIST.AI.600-1) — perfil de riscos de
  IA generativa, incluindo confabulação e avaliação.
- [OWASP LLM01:2025 Prompt Injection](https://genai.owasp.org/llmrisk/llm01-prompt-injection/)
  — injeção direta e indireta e seus impactos, acesso em 18 novembro 2026.
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/core/extensions/artificial-intelligence)
  — abstrações .NET opcionais e independentes de provedor, acesso em 18 novembro
  2026. O laboratório usa contrato próprio mínimo para evitar dependência sem
  função didática.
