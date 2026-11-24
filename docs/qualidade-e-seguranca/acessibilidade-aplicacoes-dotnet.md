# Acessibilidade em aplicações .NET

- **Nível:** intermediário.
- **Objetivo:** incorporar acessibilidade aos requisitos, à implementação e à
  estratégia de testes de aplicações web.
- **Pré-requisitos:** [ASP.NET Core](../aplicacoes-e-dados/c06-composicao-pipeline-aspnet-core.md)
  e [testes orientados a risco](c23-testes-comportamento-risco.md).
- **Escopo:** interfaces web em ASP.NET Core e Blazor; não abrange
  conformidade legal ou interfaces nativas de todas as plataformas .NET.
- **Última revisão:** 2026-11-23.

## O problema

Uma aplicação pode responder corretamente a uma requisição HTTP e ainda impedir
que uma pessoa conclua uma operação. Um formulário que informa erros apenas
pela cor ou uma janela que aprisiona o foco são exemplos de barreiras que
testes de regras de negócio isolados não identificam.

Acessibilidade é um atributo de qualidade que precisa de critérios observáveis.
As WCAG 2.2 organizam requisitos para conteúdo web em quatro princípios:
perceptível, operável, compreensível e robusto. O nível AA inclui os critérios
de nível A e AA aplicáveis, não apenas uma pontuação de ferramenta.

## Implementação em ASP.NET Core e Blazor

Em MVC e Razor Pages, os Tag Helpers podem gerar associações entre rótulos e
campos e mensagens de validação. Inspecione o HTML efetivamente renderizado:
o uso de um framework não garante nomes acessíveis, contraste ou foco adequado.

Em Blazor, considere também mudanças dinâmicas de estado e navegação entre
rotas. O componente `FocusOnNavigate` permite direcionar o foco a um elemento
da página após a navegação; o seletor escolhido precisa corresponder à
estrutura real das páginas. Atualizações de estado que não recebem foco podem
necessitar de regiões vivas para comunicar resultados sem interromper
desnecessariamente a leitura.

Por exemplo, um campo com erro precisa de um rótulo associado, uma mensagem
textual que explique como corrigir o problema e uma associação programática
entre campo e mensagem. O atributo `aria-describedby` pode estabelecer essa
associação, e `aria-invalid` deve refletir o estado real de validação.
A validação no servidor continua necessária independentemente da apresentação.

Prefira controles HTML nativos. Um botão nativo já possui semântica e
comportamento de teclado; reproduzi-los em um elemento genérico aumenta a
responsabilidade de implementação e manutenção.

## Critérios de aceite

Considere um fluxo de cadastro de usuário. Além do resultado funcional, a
equipe pode estabelecer critérios como:

- concluir o fluxo apenas com teclado, sem perder o foco;
- identificar campos e mensagens de erro por tecnologia assistiva;
- manter informação disponível com ampliação e reorganização do conteúdo;
- apresentar foco visível e não totalmente oculto por outros componentes;
- atender ao contraste aplicável e ao tamanho mínimo de alvos interativos,
  considerando as exceções previstas na especificação.

Esses exemplos não representam uma lista completa dos critérios WCAG.

## Estratégia de testes

| Camada | Evidência | Limitação |
|---|---|---|
| Testes unitários | Estado de validação e regras de apresentação | Não avaliam o navegador real |
| Integração | HTML e respostas gerados pelo servidor | Não comprovam interação assistiva |
| Testes de ponta a ponta | Navegação, foco e estados interativos | Cobertura depende dos cenários |
| Análise automatizada | Violações detectáveis por regras | Não comprova conformidade |
| Avaliação manual | Teclado, leitor de tela, ampliação e fluxos completos | Exige ambiente e avaliação registrados |

Ferramentas como axe podem integrar os testes de navegador. Defina o escopo,
as regras executadas e a política de tratamento de achados no pipeline.
Uma execução sem violações detectadas não equivale à ausência de barreiras.

A avaliação manual deve registrar versão da aplicação, navegador, tecnologia
assistiva, cenário, resultado e limitações. Sempre que possível, envolva
pessoas com deficiência na avaliação.

## Decisões e consequências

Centralizar componentes acessíveis pode reduzir repetição, mas uma biblioteca
de componentes não elimina a necessidade de testar a composição real.
Controles personalizados podem ser justificados por necessidades específicas;
o custo inclui reproduzir interações de teclado, semântica, foco e estados.

Priorize os fluxos essenciais e corrija causas compartilhadas. Registre
requisitos aplicáveis e evidências junto aos critérios de aceite. Evite
converter uma pontuação de auditoria em garantia de conformidade.

## Prática sugerida

Escolha um formulário existente em ASP.NET Core ou Blazor. Identifique os
critérios aplicáveis, percorra o fluxo com teclado e leitor de tela, execute
uma análise automatizada e registre pelo menos uma evidência por critério
avaliado. Corrija uma barreira identificada e adicione uma regressão quando
o comportamento puder ser verificado automaticamente. Se nenhuma barreira
for encontrada, documente os limites da avaliação em vez de inventar uma.

## Referências

- [W3C: WCAG 2.2](https://www.w3.org/TR/WCAG22/).
- [W3C: avaliação de acessibilidade web](https://www.w3.org/WAI/test-evaluate/).
- [Microsoft Learn: formulários no ASP.NET Core 11](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-11.0).
- [Microsoft Learn: navegação no Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing).

Volte ao [índice de qualidade e segurança](indice.md).
