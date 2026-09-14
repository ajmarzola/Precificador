# F004 — Precificação

## Objetivo

Calcular e explicar o custo atual, a margem atual e o preço necessário para atender a margem-alvo.

## Resultado esperado por produto

Quando a ficha estiver completa, apresentar pelo menos:

- custo de ingredientes;
- custo de embalagens/consumíveis;
- custo de perdas;
- custo de mão de obra;
- custo de energia;
- custo do lote;
- rendimento;
- custo unitário;
- margem-alvo;
- preço teórico;
- preço sugerido;
- preço de venda atual;
- margem atual;
- situação frente à margem.

Quando o cálculo estiver incompleto, apresentar claramente quais dados impedem a precificação.

## UC018 — Custo atual dos Itens

A primeira fatia do motor de custo calcula cada Item pela RN011 usando o preço vigente do Insumo na data operacional da Empresa.

A consulta apresenta custos individuais conhecidos, mas só apresenta `CustoBaseItens` quando a Ficha possui ao menos um Item e todos os Itens possuem preço vigente.

Item sem preço vigente e Ficha vazia tornam esse componente indisponível; nunca são convertidos silenciosamente em custo zero.

Os cálculos são derivados em tempo de consulta e não são persistidos. Perdas, mão de obra, energia e custo total pertencem aos UCs posteriores.

## Regras relacionadas

RN004, RN006, RN007, RN009 a RN027.

## Princípio de explicabilidade

O usuário deve conseguir rastrear os componentes usados no resultado. A interface não deve exibir somente um número final sem detalhamento de origem.
