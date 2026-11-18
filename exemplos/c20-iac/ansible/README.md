# Laboratório Ansible do C20

O playbook usa somente `localhost` com conexão local e escreve no diretório
informado por `output_dir`. Não requer privilégio elevado, credenciais ou hosts
externos.

Em uma máquina de controle Linux com `ansible-core`:

```bash
ansible-playbook -i inventory.ini playbook.yml --syntax-check
ansible-playbook -i inventory.ini playbook.yml --check --diff
```

Uma execução real pode ser feita apenas em laboratório autorizado. Execute duas
vezes e confirme `changed=0` na segunda para verificar idempotência. Check mode é
uma simulação e não garante o resultado da execução real.
