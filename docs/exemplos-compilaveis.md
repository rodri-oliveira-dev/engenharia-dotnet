# Estratégia para exemplos compiláveis

Exemplos executáveis são adicionados sob demanda, junto ao capítulo que necessita
deles. Esta estratégia evita projetos vazios e mantém cada laboratório
proporcional ao conceito demonstrado.

## Estrutura prevista

Cada laboratório fica isolado em um
diretório `cXX-nome-do-exemplo/` e poderá conter um projeto de produção e um de
testes apenas quando ambos tiverem função didática.

```text
exemplos/
├── EngenhariaDotnet.slnx
├── c01-tipos-modelagem/
│   ├── C01.TiposModelagem.csproj
│   └── C01.TiposModelagem.Tests.csproj
└── c02-nulabilidade/
    ├── C02.Nulabilidade.csproj
    └── C02.Nulabilidade.Tests.csproj
```

Capítulos fortemente relacionados podem compartilhar um laboratório, como
`c06-c07-aspnet-core/`, quando o mesmo host é necessário para testar composição,
pipeline e contratos HTTP sem duplicar infraestrutura.

O laboratório `c23-c25-testes/` reúne regras unitárias, dublês, API,
persistência, Testcontainers e caracterização de legado. Testes com a trait
`Category=Docker` exigem daemon Docker ativo; os demais não acessam serviços
externos.

O laboratório `c21-c22-ia/` reúne workflow com modelo falso, RAG local, agente
limitado, especificação rastreável e server MCP stdio. Os testes não acessam
modelos nem serviços externos; somente o projeto do server usa o SDK C# oficial
do MCP com versão fixada.

O SDK está fixado em `global.json`; projetos novos devem usar o mesmo target ou
documentar por que precisam de uma versão diferente.

O laboratório `c27-c28-observabilidade/` reúne uma aplicação ASP.NET Core com
logs estruturados, métricas, traces, propagação HTTP, OpenTelemetry e health
checks. A dependência e sua latência são simuladas; o exemplo não publica
resultados de performance nem exige backend comercial.

O laboratório `c29-entrega/` demonstra uma feature flag com os caminhos estável
e novo isolados e testados. A fonte de configuração é deliberadamente local: o
exemplo não simula propagação, auditoria ou consistência de um serviço de flags.

## Quando criar um projeto

Crie um laboratório quando compilação, testes, benchmark ou integração forem
necessários para verificar a afirmação do capítulo. Para APIs pequenas e sem
comportamento oculto, prefira um snippet completo no próprio Markdown.

Não crie um projeto por seção, variação ou ferramenta. Amplie um laboratório
existente quando as partes compartilharem o mesmo objetivo e as mesmas
dependências; separe quando a execução conjunta introduzir infraestrutura
irrelevante.

## Contrato de cada laboratório

Cada exemplo executável deve:

- usar a versão do SDK fixada pelo repositório quando os primeiros projetos forem
  adicionados;
- compilar sem avisos novos e habilitar nullable reference types;
- oferecer comandos de restauração, build, teste e execução no capítulo;
- manter testes determinísticos para o comportamento central;
- declarar serviços externos, portas, variáveis e forma de limpeza;
- evitar segredos e dependências sem função didática;
- documentar simplificações e diferenças em relação a produção.

## Infraestrutura e dados

Banco, broker ou serviço externo só deve ser introduzido quando fizer parte do
comportamento estudado. O ambiente precisa ser reproduzível e isolado. Testes de
semântica relacional não devem substituir o banco real por um provider em
memória que altere o comportamento observado.

Containers e fixtures compartilhadas pertencem à infraestrutura comum somente
depois de uma segunda necessidade comprovada. Até lá, ficam junto ao laboratório
que os utiliza.

## Performance

Benchmarks pertencem ao capítulo C04. Eles devem registrar ferramenta, baseline,
ambiente e metodologia. Outros capítulos podem consumir os resultados, mas não
devem duplicar a infraestrutura de medição.

## Evolução

O código faz parte da documentação: uma mudança conceitual exige atualizar o
laboratório e seus testes no mesmo pull request. O CI restaura, compila e testa a
solução, além de validar os exemplos de Terraform e a sintaxe do playbook Ansible.

## Snippets em Markdown

Os blocos C# publicados nos documentos usam metadados explícitos para separar
código isolado, código dependente de contexto, fragmentos ilustrativos e erros
deliberados. O formato, os critérios de compilação e execução, o inventário e as
limitações estão no
[inventário de snippets C#](planejamento/inventario-snippets-csharp.md).

O CI verifica a cobertura e a consistência dos metadados, compila a seleção
declarada no manifesto e executa somente os exemplos classificados
explicitamente como determinísticos e seguros. Essa verificação não substitui
os testes dos laboratórios.
