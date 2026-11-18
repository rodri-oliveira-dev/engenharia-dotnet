# Laboratório Terraform do C20

Este root module lê `modelo.txt` e planeja um arquivo local em `saida/`. Não usa
cloud, credenciais ou recursos cobrados. O backend local é intencional somente
para o laboratório e não representa uma configuração de equipe.

Valide sem executar `apply`:

```bash
terraform fmt -check
terraform init -backend=false
terraform validate
terraform plan -out=tfplan
terraform show tfplan
```

O diretório `.terraform`, o plan, o state e `saida/` são artefatos locais e estão
ignorados pelo Git. Mesmo neste laboratório sem segredos, trate plan e state como
potencialmente sensíveis.
