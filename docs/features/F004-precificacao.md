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

## UC020 — Custo de mão de obra

O custo de mão de obra do lote aplica RN013 usando o `TempoAtivoMinutos` persistido na Ficha e o `ValorHoraTrabalho` atual da configuração da Empresa.

Tempo ativo zero representa ausência explícita de trabalho humano ativo e resulta em custo zero mesmo se o valor/hora não estiver configurado. Para tempo maior que zero, valor/hora ausente torna apenas esse componente indisponível; zero configurado continua sendo um valor válido.

O componente é derivado em tempo de consulta, independente do custo dos Itens e não é persistido. A soma dos componentes pertence ao UC022.

## UC021 — Custo de energia/equipamentos

A Ficha registra usos atuais de equipamentos elétricos por Nome, Potência em kW e Tempo de uso em minutos.

O custo aplica RN014 usando a TarifaEnergiaKwh atual da Empresa. Sem usos, o componente vale zero mesmo se a tarifa estiver ausente. Com usos e tarifa ausente, consumos em kWh permanecem explicáveis, mas custos ficam indisponíveis.

O componente é independente de Itens e mão de obra, derivado em tempo de consulta e não persistido. UC022 fará a composição final.

## Regras relacionadas

RN004, RN006, RN007, RN009 a RN027, RN055 e RN056.

## Princípio de explicabilidade

O usuário deve conseguir rastrear os componentes usados no resultado. A interface não deve exibir somente um número final sem detalhamento de origem.
