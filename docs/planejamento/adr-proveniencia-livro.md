# ADR: proveniência das edições do livro

## Contexto

O livro é distribuído gratuitamente em PDF e EPUB. A autoria declarada
nos metadados pode ser copiada; uma soma de verificação não assinada
também pode ser substituída. O leitor precisa conseguir verificar a
origem técnica de cada arquivo e detectar alterações.

## Decisão

Usar GitHub Artifact Attestations com a identidade OIDC do workflow
oficial de publicação e proveniência SLSA. Gerar uma atestação
por arquivo, manter os arquivos de assinatura e as somas SHA-256
na mesma edição e registrar o commit de origem.

Separar as permissões entre geração, atestação e publicação.
A atestação só ocorre após a validação dos dois formatos, em execução
manual na branch main do repositório oficial, sem obter ou executar
código do PR no job de assinatura. A publicação verifica os arquivos
novamente antes de criar a release. A verificação restringe a identidade
ao repositório, ao workflow, à branch e ao commit esperado.

## Alternativas

- Metadados e somas sem assinatura: não comprovam a origem por si sós.
- Certificados pessoais ou chaves persistentes: exigem gestão do ciclo
  de vida, armazenamento seguro e procedimentos de revogação.
- Assinatura embutida no PDF: não oferece um mecanismo uniforme para
  PDF e EPUB e modifica o próprio arquivo.

## Consequências

O leitor pode verificar os mesmos bytes distribuídos pelo projeto sem
instalar certificados pessoais do autor. A verificação depende das
raízes de confiança e da identidade do GitHub e do Sigstore.
A atestação não garante correção editorial, reprodução binária da
geração ou titularidade de direitos autorais.

A segurança depende também da proteção da branch main e dos workflows.
A configuração de publicação não autoriza alterações administrativas
automáticas. A primeira atestação oficial ocorrerá na publicação manual
após a incorporação desta decisão.
