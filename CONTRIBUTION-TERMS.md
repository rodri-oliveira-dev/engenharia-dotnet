# Termos de contribuição

Versão 1.0 · 25/11/2026

A aceitação destes termos é condição para incorporar contribuições ao
Engenharia .NET.

## Declarações do contribuinte

Ao aceitar estes termos e enviar material ao projeto, você declara que:

- Possui os direitos e autorizações necessários para a contribuição e
  identifica material de terceiros, suas fontes e respectivas licenças.
- Disponibiliza suas contribuições originais de código sob a licença
  [MIT](LICENSE-CODE).
- Disponibiliza suas contribuições editoriais originais sob a licença
  [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/deed.pt).
- Autoriza a inclusão, adaptação, revisão e distribuição gratuita de suas
  contribuições nas edições digitais do livro, nos limites das licenças
  aplicáveis e com atribuição.
- Quando envia material de outra pessoa, possui autorização para fazê-lo
  sob os termos aplicáveis e identifica sua autoria.

A contribuição não implica transferência da titularidade dos direitos
autorais, exclusividade ou expectativa de remuneração. Não se concede
autorização comercial adicional para conteúdo editorial. O código pode
ser usado comercialmente nos termos da MIT.

Os direitos de terceiros, as permissões anteriormente concedidas e as
exceções legais continuam preservados. A autoria será reconhecida no
[registro de colaboradores](CONTRIBUTORS.md) e nas edições do livro.

## Como registrar o aceite

Leia os termos e a [política de licenciamento](LICENSE). Marque as duas
declarações do modelo de PR e publique, pela conta autora do PR, um comentário
contendo exatamente a declaração abaixo, em uma única linha:

> Li e concordo com os Termos de Contribuição do Engenharia .NET v1.0 e declaro possuir os direitos necessários.

Depois do comentário, salve a descrição do PR com as duas declarações
marcadas. Se a verificação já tiver sido executada, solicite uma nova execução
do fluxo de validação. O comentário isolado não dispara esse fluxo.

O aceite fica associado à conta GitHub, ao número do PR, à versão dos termos,
à data do comentário e ao respectivo endereço. Cada PR requer um aceite.
Não altere nem exclua o comentário de aceite. Uma mudança de versão exige
nova declaração com a versão vigente.

O mantenedor deve verificar a autoria de materiais enviados por terceiros
e a autorização dos demais autores quando houver contribuições conjuntas.
O aceite do autor do PR não comprova o consentimento de todos os coautores.

## Automação e revisão

O fluxo de validação verifica a declaração na descrição e o comentário
do autor do PR. A execução usa a definição da branch de destino confiável
(`pull_request_target` na `main`), com permissões somente de leitura e sem
obter ou executar arquivos do PR. Na implantação inicial, o fluxo passa a
executar após sua incorporação à `main`. Até lá, o mantenedor deve conferir
manualmente o aceite deste PR. Após a implantação, configure a verificação
obrigatória e proteja mudanças nos fluxos de trabalho contra alterações
não revisadas. O nome do check, isoladamente, não garante sua procedência. A automação oficial do Dependabot é dispensada da declaração
pessoal, mas somente para alterações em arquivos de dependências autorizados.
Outras automações precisam de revisão e procedimento de aceite humano.

A verificação automatizada é uma evidência de aceite, não uma certificação
jurídica nem uma transferência de direitos. O mantenedor deve preservar
os registros e revisar casos de contribuição de terceiros.

Para mudanças futuras destes termos, atualize a versão e a declaração em
todos os modelos de PR e na verificação. Não aplique mudanças retroativamente.
