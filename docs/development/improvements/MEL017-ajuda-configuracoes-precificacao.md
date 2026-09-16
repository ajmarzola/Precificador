# MEL017 — Explicar configurações de precificação na interface

- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Classificação:** UX / explicabilidade
- **Prioridade:** baixa
- **Dependência:** UC026, UC027
- **Estado:** Planejado

## Problema

Os nomes das configurações são tecnicamente corretos, mas não são necessariamente autoexplicativos para o usuário final.

O caso mais evidente é:

~~~text
Incremento comercial de arredondamento
~~~

Sem contexto, o usuário pode não compreender como o valor interfere no Preço sugerido.

## Objetivo

Adicionar ajuda contextual curta e acessível para todas as configurações de precificação.

## Configurações mínimas

Explicar:

- Valor da hora de trabalho;
- Tarifa de energia;
- Margem padrão para novos produtos;
- Incremento comercial de arredondamento;
- Reserva comercial para desconto.

## Incremento comercial — conteúdo mínimo

A explicação deve comunicar, em linguagem de usuário, que o sistema arredonda o Preço teórico para o próximo múltiplo configurado.

Exemplo:

~~~text
Incremento = R$ 0,50
Preço teórico = R$ 12,13
Preço sugerido = R$ 12,50
~~~

Se o preço já for múltiplo exato, ele é mantido.

## Forma de apresentação

Preferência:

- tooltip/ícone de ajuda junto ao rótulo;
- acessível por teclado;
- conteúdo disponível também para tecnologias assistivas.

É aceitável complementar com texto de ajuda abaixo do campo quando isso for mais claro.

Não depender apenas de atributo HTML `title` se isso prejudicar acessibilidade/mobile.

## Relação com Manual do Usuário

MEL014 deve conter explicação mais completa das mesmas configurações.

MEL017 resolve a dúvida no ponto de uso; MEL014 documenta o processo de forma abrangente.
