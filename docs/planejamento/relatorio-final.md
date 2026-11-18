# Relatório final de auditoria

> **Data da auditoria:** 18 de novembro de 2026
> **Branch:** `docs/assincronismo-concorrencia-runtime`
> **Escopo:** entregas editoriais e técnicas dos Prompts 1 a 17

## Resumo executivo

O mapa editorial foi reconciliado com a implementação. Os 35 capítulos
planejados possuem conteúdo substancial, referências e navegação; não foram
encontrados capítulos vazios, IDs duplicados ou parágrafos longos duplicados entre
capítulos. A cobertura está distribuída nas oito áreas previstas.

Os exemplos incluem 17 laboratórios: 15 laboratórios .NET, um laboratório SQL e
um laboratório de Infrastructure as Code. A solução .NET contém 32 projetos,
dos quais 15 são projetos de teste. A compilação Release passou sem avisos no
conjunto de regras configurado pelo repositório.

O status geral é **parcialmente validado**: conteúdo, compilação, testes,
Terraform, Ansible, Markdown, links internos, dependências e segredos foram
validados localmente ou no CI. O daemon Docker e o executável Ansible não estavam
disponíveis na máquina local; essas verificações foram concluídas pelo workflow em
runner Linux. Uma referência externa oficial permanece inacessível ao verificador
automático e está explicitamente classificada como não validada.

## Cobertura editorial

| Dimensão | Resultado | Status |
|---|---:|---|
| Capítulos planejados | 35 | Validado |
| Capítulos implementados com conteúdo | 35 | Validado |
| Cobertura do mapa editorial | 35 de 35 (100%) | Validado |
| Áreas de conhecimento | 8 de 8 | Validado |
| Menor capítulo, em contagem automatizada | 764 palavras | Validado |
| Capítulos vazios | 0 | Validado |
| IDs de capítulo duplicados | 0 | Validado |
| Parágrafos normalizados duplicados com 180 ou mais caracteres | 0 | Validado |

As áreas implementadas são:

1. Linguagem e runtime;
2. aplicações e dados;
3. domínio e arquitetura;
4. sistemas distribuídos;
5. qualidade e segurança;
6. operação, entrega e plataforma;
7. IA e prática profissional;
8. estudos de caso.

A existência do arquivo não foi usada isoladamente como critério de cobertura.
A auditoria também verificou extensão mínima observada, seções, referências,
links, relação com outros capítulos, exemplos associados e presença nos
índices.

## Exemplos executáveis

| Item | Quantidade | Status |
|---|---:|---|
| Laboratórios .NET | 15 | Validado |
| Projetos .NET | 32 | Validado |
| Projetos de teste .NET | 15 | Validado |
| Laboratório PostgreSQL em SQL | 1 | Validado |
| Laboratório de IaC, com Terraform e Ansible | 1 | Validado |

Todos os projetos usam `net10.0`, nullable reference types e avisos como erros.
O SDK está fixado em `10.0.404` pelo `global.json`. Os exemplos que abrem
conexões, respostas HTTP, documentos JSON, fontes de telemetria ou outros recursos
descartáveis foram revisados quanto a `using` ou `await using`. Operações
assíncronas expõem e propagam `CancellationToken` onde o cancelamento faz parte
do contrato demonstrado.

## Validações executadas

| Validação | Resultado | Status |
|---|---|---|
| `dotnet restore exemplos/EngenhariaDotnet.slnx --force-evaluate` | 32 projetos restaurados | Validado |
| `dotnet build ... --configuration Release --no-restore` | 0 avisos e 0 erros | Validado |
| Suíte sem o teste marcado `Category=Docker` | 116 aprovados, 0 falhas | Validado |
| Suíte completa no CI, incluindo PostgreSQL/Testcontainers | 117 aprovados, 0 falhas | Validado |
| Teste direcionado do laboratório C18 após a correção | 7 aprovados, 0 falhas | Validado |
| Markdown lint 0.24.0 / markdownlint 0.42.0 | 59 arquivos, 0 ocorrências | Validado |
| Links internos | 494 destinos, 0 ausentes | Validado |
| Links externos distintos em Markdown | 228; 227 acessíveis pelo verificador | Parcialmente validado |
| Link da LGPD no Planalto | o cliente automatizado retornou status 0; URL oficial preservada | Não validado |
| `dotnet package list --vulnerable --include-transitive` | nenhum pacote vulnerável nos 32 projetos | Validado |
| Varredura básica de segredos em arquivos versionados | nenhuma ocorrência | Validado |
| `terraform fmt -check`, `init -backend=false` e `validate` | configuração válida com Terraform local 1.15.5 | Validado |
| `ansible-playbook --syntax-check` | aprovado no runner Linux com `ansible-core==2.20.1` | Validado |
| Workflow do GitHub Actions | quatro jobs aprovados na execução `44388873410` | Validado |

O comando completo local confirmou a indisponibilidade de
`npipe://./pipe/docker_engine`, em vez de uma falha de asserção do teste
PostgreSQL. A execução seguinte, sem Docker, aprovou 116 testes. No CI, onde o
daemon estava disponível, a suíte completa aprovou os 117 casos, incluindo o
teste PostgreSQL.

## Segurança e dependências

- Nenhuma chave privada, credencial de provedor ou token com formato conhecido
  foi encontrada em arquivo versionado.
- A senha `senha-local-descartavel` pertence exclusivamente ao container
  efêmero do teste PostgreSQL e não concede acesso a um serviço externo.
- Os exemplos JWT validam assinatura, emissor, audiência e expiração; a
  autorização também verifica recurso e tenant.
- Consultas SQL com entrada variável usam parâmetros. O SQL fixo usado para
  schema, explicação de plano e contagem não incorpora entrada do usuário.
- O workflow declara `contents: read`, não recebe segredos e agora fixa actions
  externas por SHA completo, com a versão documentada ao lado.
- Foram adicionadas validações automatizadas e locais para links internos e
  formatos comuns de segredos.

## Problemas identificados e correções

| Problema | Correção | Status |
|---|---|---|
| README não apresentava todos os capítulos diretamente | Adicionado índice completo por área | Corrigido |
| Índices ainda descreviam o conteúdo publicado como roadmap | Atualizados títulos e textos de navegação | Corrigido |
| Guia de exemplos dizia que o CI seria criado no futuro | Descrição reconciliada com o workflow existente | Corrigido |
| Referência de consistência da Microsoft retornava 404 | Substituída pela documentação oficial atual | Corrigido |
| Link comercial da O'Reilly retornava 403 | Substituído pela página da editora Pearson | Corrigido |
| Configuração inválida do retry falhava somente na primeira execução | Validação movida para o construtor, com guardas de nulo e teste | Corrigido |
| CI não validava links internos nem segredos básicos | Adicionados scripts sem dependências externas e etapas no workflow | Corrigido |
| Actions usavam tags principais mutáveis | Referências fixadas por SHA completo | Corrigido |

## Limitações e pendências

- **Limitação local:** Docker e Ansible não estavam disponíveis nesta máquina;
  as duas verificações foram aprovadas no CI em runner Linux.
- **Não validado:** disponibilidade automatizada do texto compilado da LGPD no
  Planalto. A referência oficial foi mantida, pois a falha observada foi de
  acesso do cliente, não evidência de um novo endereço canônico.
- **Parcialmente validado:** snippets que existem apenas no Markdown foram
  revisados editorialmente, mas não compõem todos um projeto compilável.
- **Parcialmente validado:** exemplos de mensageria, OpenTelemetry Collector e
  autenticação são laboratórios controlados; não comprovam comportamento de um
  broker, collector ou provedor de identidade de produção.

## Recomendações futuras

1. Manter o teste `Category=Docker` e o `syntax-check` Ansible no CI para evitar
   regressões que a validação local sem esses pré-requisitos não detecta.
2. Renovar periodicamente as versões fixadas das actions e repetir a auditoria de
   dependências.
3. Revisar referências externas em ciclos editoriais, sem tornar o CI dependente
   da disponibilidade de 228 sites externos.
4. Adicionar laboratórios somente quando uma afirmação nova exigir verificação
   executável; a cobertura numérica não deve orientar expansão superficial.

## Legenda de status

- **Validado:** a verificação foi executada e produziu evidência conclusiva no
  escopo declarado.
- **Parcialmente validado:** parte relevante foi verificada, mas existe uma
  fronteira de ambiente, execução ou abrangência explicitada.
- **Não validado:** a verificação não alcançou o comportamento pretendido.
- **Não aplicável:** o item não existe ou não corresponde ao tipo de artefato;
  nenhum resultado desta auditoria precisou dessa classificação.
