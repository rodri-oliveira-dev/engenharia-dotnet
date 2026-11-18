# C25 — Cobertura, E2E e testes de carga

> **Nível:** Avançado
>
> **Objetivo:** interpretar evidências de cobertura e validar fluxos e capacidade
> sem confundir métricas de atividade com ausência de risco.
>
> **Pré-requisitos:** [C07 — APIs HTTP e integrações
> resilientes](../aplicacoes-e-dados/c07-apis-http-integracoes-resilientes.md) e
> [C23 — Testes orientados a comportamento e risco](c23-testes-comportamento-risco.md).
> C27 — Telemetria e diagnóstico distribuído permanece planejado.
>
> **Última revisão:** 2026-11-18

Cobertura informa qual código uma execução atravessou; E2E informa se uma jornada
implantada funcionou; carga informa como o sistema se comportou sob um perfil de
tráfego. Nenhuma dessas evidências, isoladamente, prova correção. Este capítulo
mostra como conectá-las a risco, oráculos e comparação reproduzível.

## Contexto

Métricas fáceis de contar tendem a virar meta. Uma equipe persegue 100% de linhas,
centenas de E2E ou um pico de requisições por segundo e ainda não testa uma
constraint, uma falha de dependência ou a cauda de latência. O problema não é a
métrica; é usá-la sem declarar a pergunta, o ambiente e o oráculo.

Qualidade de teste combina: cenário relevante, estímulo controlado, observação
capaz de detectar erro e resultado reproduzível. Cobertura ajuda a localizar
lacunas. Mutation Testing desafia asserts. E2E cobre integração implantada. Carga
mede capacidade e saturação. Cada técnica vê uma dimensão diferente.

## Cobertura de código

**Line coverage** é a proporção de linhas instrumentadas executadas. **Branch
coverage** observa alternativas de decisão, como lados de um `if`, braços de
pattern matching e curto-circuitos. Uma linha com `condicao ? A : B` pode estar
coberta enquanto apenas um ramo foi exercitado.

Cobertura não informa se:

- o resultado foi assertado;
- a asserção distingue correto de incorreto;
- entradas representam limites e cenários negativos;
- dependências reais são compatíveis;
- há corridas, falhas de configuração ou requisitos ausentes;
- código inexistente, mas necessário, deveria ter sido escrito.

Um teste pode chamar todo método e terminar sem assert. Ele produz cobertura alta
e confiança quase nula. Também pode validar apenas `NotNull` quando total, moeda e
destinatário estão errados. Por isso, 100% de cobertura não significa ausência de
bugs: execução não é especificação, todos os inputs não foram enumerados e bugs
podem estar nos requisitos, ambiente ou interações.

### Interpretação e metas

Use cobertura como mapa de investigação:

1. filtre código gerado e infraestrutura sem decisão com justificativa;
2. observe diff coverage para não introduzir lacunas em código novo;
3. procure ramos de erro, cancelamento, autorização e transação;
4. relacione arquivos descobertos ao impacto do domínio;
5. leia os testes e seus asserts antes de exigir um número.

Um limiar de pipeline evita regressão grosseira, mas não deve incentivar testes
sem propósito. Código crítico pode exigir análise de risco e branches negativos
mesmo acima da meta; DTO trivial pode não merecer teste isolado. Não compare
percentuais produzidos por ferramentas/configurações diferentes como se fossem a
mesma medida.

O laboratório não adiciona coletor de cobertura: isso evitar introduzir pacote
sem necessidade didática e, principalmente, evita publicar um número não medido.
Quando configurado no projeto, execute a ferramenta escolhida, arquive formato e
versão, e registre commit e filtros junto do resultado.

## Qualidade das asserções e Mutation Testing

Mutation Testing altera temporariamente o código — troca comparação, remove
chamada, muda constante — e executa a suíte. Se os testes continuam verdes, o
mutante sobreviveu: ou o cenário não alcança a mudança, ou o oráculo não percebe o
erro. Mutantes mortos oferecem evidência sobre a sensibilidade dos testes, não
uma prova de correção.

Use Mutation Testing em regras estáveis e críticas, começando por escopo pequeno.
Ele multiplica builds e execuções, pode gerar mutantes equivalentes e custa CPU.
Não imponha score global sem triagem. Sobreviventes orientam revisão de asserts e
cenários; não justificam testar detalhes irrelevantes.

Cenários negativos exercitam o que mais escapa do caminho feliz: valores no
limite, entrada inválida, ausência de permissão, duplicidade, timeout, cancelamento,
constraint, mensagem fora de ordem e dependência degradada. Escolha-os pelo modo
de falha e pelo impacto, não para inflar branch coverage.

**Cobertura de riscos** é uma matriz qualitativa entre risco e evidência:

| Risco | Evidência mínima útil |
| --- | --- |
| desconto incorreto no limite | teoria unitária em ambos os lados e mutante de comparação morto |
| total negativo persistido | teste de constraint no engine real |
| quebra de JSON | teste de API/contrato com status, headers e corpo |
| pagamento duplicado | integração concorrente/idempotente e E2E crítico |
| degradação sob pico | carga com percentis, erro e saturação |

Essa matriz revela riscos sem teste mesmo quando a cobertura de linhas é alta.

## Testes end-to-end

E2E exercita um fluxo pela interface externa em um ambiente implantado, usando as
dependências que definem a jornada. Ele deve responder “o cliente consegue concluir
esta ação crítica?”, não “cada combinação de regra funciona?”.

Selecione cenários por criticidade e diferença de caminho:

- smoke de disponibilidade e configuração;
- jornada principal de criação/consulta;
- uma rejeição de alto impacto;
- autenticação/autorização relevante;
- integração assíncrona cujo estado final precisa ser comprovado.

Não replique a matriz inteira de unitários. Se vinte E2E diferem somente em um
percentual, mantenha um E2E representativo e teste a regra abaixo.

### Ambiente, dados e dependências

O ambiente precisa de versão identificável, configuração próxima do destino e
dados isolados. Gere um ID de correlação por cenário, crie dados pela API pública
quando isso fizer parte da jornada e limpe de modo idempotente. Nunca dependa da
ordem ou de uma conta mutável compartilhada.

Dependências externas podem tornar o E2E não determinístico. Use sandbox dedicada
quando a integração real é o risco; para pipeline frequente, virtualize o terceiro
na fronteira e mantenha poucos testes de aceitação separados. Declare o que foi
substituído: um E2E do nosso sistema com provedor fake não prova o provedor.

Flakiness é resultado variável sem mudança relevante. Causas comuns incluem
espera por tempo, seletor de UI instável, dados compartilhados, eventual
consistency sem polling limitado, rede e recursos saturados. Reexecução automática
pode coletar evidência, mas não converte falha em sucesso. Registre taxa de retry,
preserve artefatos e atribua owner/prazo.

Para estado assíncrono, faça polling por condição observável com deadline e
intervalo controlado. Ao expirar, reporte último estado, IDs, logs e trace. Um
`sleep(30s)` é lento quando o sistema termina cedo e insuficiente quando termina
tarde.

### E2E em CI/CD

Separe estágios:

1. unitários e componentes em cada commit;
2. integrações reais no pull request;
3. poucos E2E smoke após deploy em ambiente efêmero ou homologação;
4. jornadas mais caras em agenda ou antes de promoção de alto risco.

Uma falha deve bloquear somente quando o ambiente e o oráculo são confiáveis.
Quarentena não pode virar cemitério: mantenha visibilidade, owner e critério de
saída. Preserve screenshot, vídeo, request/response permitido, versão do deploy e
correlação, respeitando dados sensíveis.

Evite excesso de E2E quando a mesma falha pode ser detectada abaixo, quando o
cenário não representa fluxo crítico, quando dados não podem ser isolados ou
quando o custo de diagnóstico domina o feedback. O objetivo é máxima confiança
por tempo de pipeline, não máxima quantidade.

## Testes de carga e performance

Teste de carga não é um loop local improvisado. Ele exige hipótese, workload,
ambiente, duração, aquecimento, observabilidade e critério. Tipos comuns incluem
baseline de carga esperada, stress até o limite, spike, soak prolongado e teste de
capacidade por componente.

Métricas essenciais:

- **throughput:** operações concluídas por unidade de tempo, separado por tipo;
- **latência:** duração da operação; use distribuição e percentis, não só média;
- **p50/p95/p99:** valor abaixo do qual 50%, 95% ou 99% das observações caem;
- **taxa de erro:** falhas por total, classificadas por timeout, 4xx/5xx e domínio;
- **saturação:** recurso sem folga — pool, fila, threads, conexões, CPU ou I/O;
- **concorrência:** trabalho simultâneo; não é sinônimo de throughput.

Percentis precisam de volume e método de agregação conhecidos. A média pode ficar
estável enquanto p99 degrada. Comparar p99 de amostras pequenas ou agregar
percentis de instâncias é enganoso; preserve histogramas ou observações adequadas.

### Diagnóstico de gargalos

Correlacione resultado externo com:

- CPU por processo/container e throttling;
- memória, alocação, garbage collection e paginação;
- pool de conexões, locks, planos, I/O e duração no banco;
- tamanho/idade de filas, partições e taxa de consumo;
- thread pool, sockets, filas internas e rate limit;
- latência, erro e limite de cada dependência downstream.

CPU baixa não significa capacidade: o sistema pode esperar banco, lock, fila ou
serviço externo. Aumentar réplicas não corrige um banco saturado. Throughput
estável com latência e fila crescentes indica que chegadas excedem a taxa de
serviço; o sistema acumula trabalho.

### Baseline e comparação

Uma baseline registra commit, hardware/limites, topologia, versões, configuração,
dataset, script, perfil de chegada, duração, aquecimento e ruído conhecido. Sem
isso, “ficou 20% mais rápido” não é reproduzível.

Compare sob a mesma carga e ambiente, com repetições suficientes. Apresente
throughput, percentis, erros e saturação juntos. Uma versão com throughput maior
que também dobra p99 e erros pode ser pior. Defina critério antes da execução,
por exemplo: 300 req/s por 20 minutos, erro < 0,1%, p95 < 250 ms, p99 < 600 ms e
CPU < 75%, sem crescimento contínuo de backlog.

Não execute carga destrutiva em produção sem autorização, janela, limites e plano
de interrupção. Dados devem ser sintéticos; efeitos como e-mail e cobrança ficam
desativados ou direcionados a fakes. O gerador também pode saturar: monitore-o e,
se necessário, distribua a carga.

## Exemplo integrado e limites

O laboratório de C23–C25 oferece uma API pequena e testes de regras, dublês,
persistência, container, componente assíncrono e legado. Ele **não** inclui uma
ferramenta de carga nem browser E2E: adicionar dependências apenas para produzir
tráfego ornamental contrariaria o escopo. O endpoint pode ser alvo de k6, NBomber
ou ferramenta organizacional quando existir ambiente controlado e hipótese.

Um plano mínimo para esse endpoint seria:

```text
Hipótese: 95% de GET /descontos/250?categoria=Fidelidade abaixo de 100 ms
Workload: taxa constante de 100 req/s, aquecimento de 2 min, medição de 10 min
Critérios: erro < 0,1%; p95 < 100 ms; p99 < 200 ms; sem crescimento de memória
Evidência: script versionado, commit, limites de CPU/memória e histogramas
```

Esse texto não é resultado de benchmark. Nenhuma medição foi executada e nenhum
número de performance ou cobertura é alegado.

## Dicas de engenharia

- Faça cobertura apontar perguntas; não transforme o percentual em objetivo
  isolado.
- Revise sobreviventes de mutação em regras críticas e descarte equivalentes com
  justificativa.
- Mantenha E2E pequeno, orientado a jornada e rico em diagnóstico.
- Controle relógio e dados; espere condições observáveis em assíncrono.
- Versione script de carga junto da aplicação e registre o ambiente.
- Compare percentis, erro e saturação, não somente média ou throughput.
- Pare o teste se erros ou saturação ultrapassarem limite seguro.

## Armadilhas comuns

- afirmar ausência de bugs por 100% de cobertura;
- adicionar asserts triviais somente para matar mutantes;
- executar E2E em ambiente compartilhado sem isolamento;
- tratar retry verde como teste aprovado na primeira tentativa;
- medir performance no notebook com processos concorrentes e chamar de baseline;
- observar somente CPU e ignorar banco, filas, memória e dependências;
- comparar versões com datasets, carga ou limites diferentes;
- gerar carga maior que o gerador suporta e atribuir o limite ao servidor.

## Trade-offs

Cobertura é barata e ampla, mas semântica limitada. Mutation Testing avalia a
força do oráculo e custa múltiplas execuções. E2E oferece confiança na composição
implantada e diagnóstico caro. Carga revela curvas e saturação, mas exige ambiente
representativo e observabilidade. Metas rígidas simplificam governança e podem
incentivar comportamento adverso; avaliação por risco exige mais julgamento e
produz evidência mais útil.

## Quando utilizar

Use line/branch coverage para encontrar código não exercitado e proteger diffs.
Use mutação em regras críticas. Use E2E para jornadas que dependem do deploy e de
múltiplas fronteiras. Use carga antes de mudanças de capacidade, lançamentos,
otimizações e quando SLOs não têm margem conhecida.

## Quando evitar

Evite perseguir 100% sem relação com risco. Evite E2E para permutações de lógica.
Evite carga em ambiente não autorizado ou sem telemetria. Se a hipótese pode ser
respondida por unitário, integração focal ou benchmark de componente, use a
evidência menor e mais diagnosticável.

## Relação com outros capítulos

- [C23](c23-testes-comportamento-risco.md) define comportamento, dublês e
  carteira orientada a risco.
- [C24](c24-integracao-real-testes-legado.md) fornece ambientes reproduzíveis e
  isolamento para integrações.
- [C04](../linguagem-e-runtime/c04-memoria-runtime-performance.md) trata benchmark,
  alocação e diagnóstico de runtime.
- [C18](../sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md)
  relaciona filas, dependências e observabilidade distribuída.
- C27 — Telemetria e diagnóstico distribuído aprofundará métricas, traces e
  cardinalidade quando publicado.

## Referências

- [Use code coverage for unit testing](https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage)
  — Microsoft, coleta e relatório de cobertura, acesso em 18 novembro 2026.
- [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) —
  Stryker Mutator, conceitos de Mutation Testing para .NET, acesso em 18 novembro
  2026.
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/testing/integration-testing)
  — Microsoft, fronteira e custo de testes de integração, acesso em 18 novembro
  2026.
- [Load testing websites](https://grafana.com/docs/k6/latest/testing-guides/load-testing-websites/)
  — Grafana k6, testes por protocolo, browser e componente, acesso em 18 novembro
  2026.
- [OpenTelemetry metrics](https://opentelemetry.io/docs/concepts/signals/metrics/)
  — OpenTelemetry, métricas, agregações e histogramas, acesso em 18 novembro 2026.
