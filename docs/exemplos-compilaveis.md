# Estratégia para exemplos compiláveis

Exemplos executáveis serão adicionados sob demanda, junto ao primeiro capítulo
que necessitar deles. Esta estratégia evita projetos vazios e mantém cada
laboratório proporcional ao conceito demonstrado.

## Estrutura prevista

Quando existirem exemplos, a raiz `exemplos/` terá uma única solução para
descoberta e execução em conjunto. Cada laboratório ficará isolado em um
diretório `cXX-nome-do-exemplo/` e poderá conter um projeto de produção e um de
testes apenas quando ambos tiverem função didática.

```text
exemplos/
├── EngenhariaDotnet.slnx
└── cXX-nome-do-exemplo/
    ├── CXX.NomeDoExemplo.csproj
    └── CXX.NomeDoExemplo.Tests.csproj
```

Essa árvore é uma convenção futura, não representa arquivos já publicados.

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
laboratório e seus testes no mesmo pull request. A validação de build será
adicionada ao CI somente quando o primeiro projeto executável existir.
