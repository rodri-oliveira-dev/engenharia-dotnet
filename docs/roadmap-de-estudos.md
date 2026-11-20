# Roadmap de estudos: livros e maturidade em engenharia .NET

Uma trilha complementar aos [35 capítulos do repositório](indice.md), organizada pela **complexidade das decisões** que cada etapa ajuda a enfrentar. Os 14 livros estão disponíveis em português: 13 têm links de **busca na Amazon Brasil** e um direciona para o **site oficial do Refactoring.Guru**. Ofertas, formatos e disponibilidade podem mudar.

> **Como usar:** siga a ordem proposta como referência, não como regra de senioridade, cargo ou prazo. Quem já domina uma etapa pode ir diretamente ao desafio de conclusão. Ler um livro não comprova competência: aplique e revise as decisões em código, documentação e conversas técnicas.

## Visão geral

| Etapa | Maturidade de estudo | Foco | Livros |
| --- | --- | --- | --- |
| 1 | Fundamental | Fundamentos de C# e autonomia no desenvolvimento | 2 |
| 2 | Fundamental → intermediário | Qualidade, testes e responsabilidade profissional | 4 |
| 3 | Intermediário | Desenho de aplicações e modelagem do domínio | 4 |
| 4 | Intermediário → avançado | Arquitetura e evolução de sistemas | 3 |
| 5 | Avançado | Decisões complexas e influência técnica | 1 |

## Sequência de leitura

### 1. Fundamentos de C# e autonomia no desenvolvimento

**Nível da trilha:** Fundamental. Construa uma base consistente de linguagem e tome decisões simples com clareza antes de adotar abstrações.

| Ordem | Livro e compra | Aplicação do estudo | Conexão com o repositório |
| --- | --- | --- | --- |
| 1 | **Use a Cabeça! C#, 4ª edição** — Andrew Stellman e Jennifer Greene. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Use%20a%20Cabe%C3%A7a!%20C%23%2C%204%C2%AA%20edi%C3%A7%C3%A3o%20Andrew%20Stellman%20e%20Jennifer%20Greene) | Revisar orientação a objetos, tipos, coleções e fluxo do código. | [C01](linguagem-e-runtime/c01-tipos-identidade-modelagem.md), [C02](linguagem-e-runtime/c02-nulabilidade-falhas-excecoes.md) e [C03](linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md) |
| 2 | **Produtividade em C#** — Cláudio Ralha. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Produtividade%20em%20C%23%20Cl%C3%A1udio%20Ralha) | Comparar soluções expressivas com alternativas mais explícitas, sem confundir concisão com legibilidade. | [C03](linguagem-e-runtime/c03-colecoes-linq-execucao-diferida.md), [C04](linguagem-e-runtime/c04-memoria-runtime-performance.md) e [C05](linguagem-e-runtime/c05-assincronicidade-concorrencia-controlada.md) |

**Prática sugerida:** Reescreva uma rotina de C# de duas maneiras. Inclua nulabilidade, tratamento de falhas e testes simples. Explique por que escolheu uma das versões.

**Critério para avançar:** Consegue justificar escolhas de tipos, coleções e tratamento de erros, sem depender apenas da preferência por sintaxe.

### 2. Qualidade, testes e responsabilidade profissional

**Nível da trilha:** Fundamental → intermediário. Aprenda a preservar comportamento e a melhorar código com evidências, critérios de revisão e comunicação responsável.

| Ordem | Livro e compra | Aplicação do estudo | Conexão com o repositório |
| --- | --- | --- | --- |
| 3 | **O Programador Pragmático, 2ª edição** — David Thomas e Andrew Hunt. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=O%20Programador%20Pragm%C3%A1tico%2C%202%C2%AA%20edi%C3%A7%C3%A3o%20David%20Thomas%20e%20Andrew%20Hunt) | Desenvolver autonomia, aprendizado contínuo e responsabilidade pelas decisões. | [C30](ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md) |
| 4 | **TDD: Desenvolvimento Guiado por Testes** — Kent Beck. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=TDD%3A%20Desenvolvimento%20Guiado%20por%20Testes%20Kent%20Beck) | Praticar ciclos curtos de teste, implementação e refatoração. | [C23](qualidade-e-seguranca/c23-testes-comportamento-risco.md) |
| 5 | **Refatoração, 2ª edição** — Martin Fowler. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Refatora%C3%A7%C3%A3o%2C%202%C2%AA%20edi%C3%A7%C3%A3o%20Martin%20Fowler) | Melhorar o desenho interno preservando contratos e comportamento observável. | [C24](qualidade-e-seguranca/c24-integracao-real-testes-legado.md) e [C33](estudos-de-caso/c33-legado-baixa-testabilidade.md) |
| 6 | **O Codificador Limpo** — Robert C. Martin. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=O%20Codificador%20Limpo%20Robert%20C.%20Martin) | Discutir estimativas, acordos profissionais, limites e comunicação sob pressão. | [C30](ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md) e [C28](operacao-entrega-e-plataforma/c28-confiabilidade-resposta-incidentes.md) |

**Prática sugerida:** Escolha um módulo legado. Escreva testes de caracterização, faça uma refatoração pequena e registre no pull request o risco, a evidência de preservação de comportamento e o plano de reversão.

**Critério para avançar:** Consegue entregar uma melhoria de código com testes relevantes e comunicar riscos e limites de modo verificável.

### 3. Desenho de aplicações e modelagem do domínio

**Nível da trilha:** Intermediário. Aprenda a reconhecer fronteiras, representar regras de negócio e escolher abstrações em função do problema.

| Ordem | Livro e compra | Aplicação do estudo | Conexão com o repositório |
| --- | --- | --- | --- |
| 7 | **Arquitetura Limpa** — Robert C. Martin. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Arquitetura%20Limpa%20Robert%20C.%20Martin) | Avaliar separação de responsabilidades, inversão de dependências e acoplamento. | [C13](dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md) |
| 8 | **Mergulho nos Padrões de Projeto** — Alexander Shvets. [Consultar no Refactoring.Guru](https://refactoring.guru/pt-br/design-patterns/book) | Estudar a intenção, a aplicação e os custos de padrões de projeto; reconhecer quando uma solução direta é suficiente. | [C13](dominio-e-arquitetura/c13-dependencias-abstracoes-padroes.md) e [C30](ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md) |
| 9 | **Storytelling de Domínio** — Stefan Hofer e Henning Schwentner. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Storytelling%20de%20Dom%C3%ADnio%20Stefan%20Hofer%20e%20Henning%20Schwentner) | Descobrir fluxos e vocabulário com pessoas do negócio antes de escolher a estrutura técnica. | [C11](dominio-e-arquitetura/c11-entidades-value-objects-agregados.md) e [C12](dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md) |
| 10 | **Implementando Domain-Driven Design** — Vaughn Vernon. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Implementando%20Domain-Driven%20Design%20Vaughn%20Vernon) | Aplicar agregados, eventos, serviços de domínio e contextos delimitados com critérios claros. | [C11](dominio-e-arquitetura/c11-entidades-value-objects-agregados.md), [C12](dominio-e-arquitetura/c12-servicos-dominio-integracao-modelos.md) e [C14](dominio-e-arquitetura/c14-cqrs-modelos-orientados-uso.md) |

**Prática sugerida:** Modele um fluxo real com participantes e decisões, delimite dois contextos, proponha agregados e explique quais regras devem ser consistentes na mesma transação. Para uma regra do fluxo, compare uma implementação simples com uma alternativa que empregue um padrão de projeto, avaliando benefícios e custos. Considere também a opção de manter um monólito modular.

**Critério para avançar:** Consegue defender as fronteiras do domínio e explicar onde a abstração agrega valor ou apenas complexidade.

### 4. Arquitetura e evolução de sistemas

**Nível da trilha:** Intermediário → avançado. Compare estilos arquiteturais e estratégias de evolução considerando qualidade, operação, custo e risco.

| Ordem | Livro e compra | Aplicação do estudo | Conexão com o repositório |
| --- | --- | --- | --- |
| 11 | **Fundamentos da Arquitetura de Software, 2ª edição** — Mark Richards e Neal Ford. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Fundamentos%20da%20Arquitetura%20de%20Software%2C%202%C2%AA%20edi%C3%A7%C3%A3o%20Mark%20Richards%20e%20Neal%20Ford) | Relacionar características de qualidade, estilos, decisões e governança. | [C19](dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md), [C27](operacao-entrega-e-plataforma/c27-telemetria-diagnostico-distribuido.md) e [C29](operacao-entrega-e-plataforma/c29-entrega-compativel-risco-pipeline.md) |
| 12 | **Criando Microsserviços, 2ª edição** — Sam Newman. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Criando%20Microsservi%C3%A7os%2C%202%C2%AA%20edi%C3%A7%C3%A3o%20Sam%20Newman) | Avaliar limites de serviços, integração, implantação e custo operacional da distribuição. | [C15](sistemas-distribuidos/c15-comunicacao-mensageria-resiliente.md), [C16](sistemas-distribuidos/c16-idempotencia-entrega-confiavel.md) e [C18](sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md) |
| 13 | **Migrando Sistemas Monolíticos para Microsserviços** — Sam Newman. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Migrando%20Sistemas%20Monol%C3%ADticos%20para%20Microsservi%C3%A7os%20Sam%20Newman) | Planejar migração incremental, convivência entre modelos e reversibilidade. | [C18](sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md) e [C34](estudos-de-caso/c34-migracao-gradual-monolito.md) |

**Prática sugerida:** Compare monólito modular, arquitetura baseada em serviços e microsserviços para um mesmo sistema. Registre uma decisão técnica com requisitos, riscos operacionais, estratégia incremental e critérios de reversão.

**Critério para avançar:** Consegue escolher um estilo arquitetural pelo contexto, e não pela popularidade, e construir um plano de evolução verificável.

### 5. Decisões complexas e influência técnica

**Nível da trilha:** Avançado. Aprofunde decisões sob incerteza, com falhas parciais, consistência, granularidade e consequências organizacionais.

| Ordem | Livro e compra | Aplicação do estudo | Conexão com o repositório |
| --- | --- | --- | --- |
| 14 | **Arquitetura de Software: As Partes Difíceis** — Neal Ford, Mark Richards, Pramod Sadalage e Zhamak Dehghani. [Buscar na Amazon Brasil](https://www.amazon.com.br/s?k=Arquitetura%20de%20Software%3A%20As%20Partes%20Dif%C3%ADceis%20Neal%20Ford%2C%20Mark%20Richards%2C%20Pramod%20Sadalage%20e%20Zhamak%20Dehghani) | Analisar trade-offs de granularidade, decomposição, consistência de dados e transações distribuídas. | [C17](sistemas-distribuidos/c17-consistencia-eventual-sagas.md), [C18](sistemas-distribuidos/c18-limites-microservicos-evolucao-monolitos.md), [C19](dominio-e-arquitetura/c19-documentacao-arquitetural-viva.md) e [C30](ia-e-pratica-profissional/c30-decisao-revisao-maturidade-tecnica.md) |

**Prática sugerida:** Simule uma mudança que atravessa serviços e exige consistência entre operações. Compare transação local, compensação e consistência eventual. Documente hipóteses, modos de falha e sinais para reavaliar a decisão; conduza uma revisão com o time.

**Critério para avançar:** Consegue explicitar consequências, negociar prioridades e ajudar outras pessoas a tomar melhores decisões, inclusive quando a solução mais simples é suficiente.

## Acompanhamento do aprendizado

Use a lista como registro pessoal; não é uma exigência para concluir os capítulos do repositório.

- [ ] Etapa 1: Fundamentos de C# e autonomia no desenvolvimento. Demonstrei o critério de conclusão e guardei as evidências da prática.
- [ ] Etapa 2: Qualidade, testes e responsabilidade profissional. Demonstrei o critério de conclusão e guardei as evidências da prática.
- [ ] Etapa 3: Desenho de aplicações e modelagem do domínio. Demonstrei o critério de conclusão e guardei as evidências da prática.
- [ ] Etapa 4: Arquitetura e evolução de sistemas. Demonstrei o critério de conclusão e guardei as evidências da prática.
- [ ] Etapa 5: Decisões complexas e influência técnica. Demonstrei o critério de conclusão e guardei as evidências da prática.

Para cada etapa, registre **problema, alternativas, decisão, evidência e o que faria mudar de ideia**. O [modelo de decisão técnica](templates/registro-decisao-tecnica.md) e o [checklist de revisão](templates/checklist-code-review.md) ajudam a estruturar esse exercício.

## Limites e fontes complementares

- Os livros são material conceitual e prático; exemplos de edições anteriores podem não usar os recursos atuais de C# e .NET.
- Para comportamento específico do .NET 11, C# 15, ASP.NET Core 11 e EF Core 11, consulte os [guias de compatibilidade](compatibilidade/indice.md) e a documentação oficial vinculada a eles.
- Alguns exemplos dos livros usam linguagens diferentes de C#. O objetivo é transferir critérios de engenharia, não copiar sintaxe sem adaptação.
- As buscas da Amazon **não confirmam estoque, preço, idioma ou edição do anúncio**. Antes de comprar, confirme a edição em português e os dados do produto. O livro do Refactoring.Guru tem um link direto para a página oficial da editora, fora da Amazon.
- Os livros selecionados não cobrem com a mesma profundidade todos os temas do repositório, como segurança, infraestrutura como código, observabilidade e IA. Para esses assuntos, use os capítulos específicos e suas referências primárias.

**Última revisão:** 20 de novembro de 2026.
