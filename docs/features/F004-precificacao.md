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

## UC019 — Perdas aplicáveis

UC019 calcula perda adicional de material por Item da Ficha, sem percentual global no Produto.

`PercentualPerda` é uma fração entre 0 e 1 aplicada sobre o CustoItem base já calculado pelo UC018. Perda zero custa zero mesmo se o Item estiver sem preço; perda positiva depende de custo base conhecido.

`CustoPerdasLote` só é apresentado quando todas as perdas positivas são determináveis. A ausência de Itens resulta em perda zero, mas não torna o custo base da Ficha completo.

Perdas de saída que reduzam unidades vendáveis pertencem ao Rendimento e não ao PercentualPerda.

O componente é derivado em consulta e não persistido.

## UC020 — Custo de mão de obra

O custo de mão de obra do lote aplica RN013 usando o `TempoAtivoMinutos` persistido na Ficha e o `ValorHoraTrabalho` atual da configuração da Empresa.

Tempo ativo zero representa ausência explícita de trabalho humano ativo e resulta em custo zero mesmo se o valor/hora não estiver configurado. Para tempo maior que zero, valor/hora ausente torna apenas esse componente indisponível; zero configurado continua sendo um valor válido.

O componente é derivado em tempo de consulta, independente do custo dos Itens e não é persistido. A soma dos componentes pertence ao UC022.

## UC021 — Custo de energia/equipamentos

A Ficha registra usos atuais de equipamentos elétricos por Nome, Potência em kW e Tempo de uso em minutos.

O custo aplica RN014 usando a TarifaEnergiaKwh atual da Empresa. Sem usos, o componente vale zero mesmo se a tarifa estiver ausente. Com usos e tarifa ausente, consumos em kWh permanecem explicáveis, mas custos ficam indisponíveis.

O componente é independente de Itens e mão de obra, derivado em tempo de consulta e não persistido. UC022 fará a composição final.

## UC022 — Custo total e custo unitário

UC022 compõe os resultados já calculados por UC018, UC019, UC020 e UC021 para obter o custo total do lote pela RN015 e o custo unitário do Produto pela RN016.

A composição só é considerada completa quando todos os componentes são determináveis. Zero conhecido é valor válido; componente indisponível nunca é convertido em zero e impede a apresentação de soma parcial como custo total confiável, conforme RN017.

O custo unitário divide o custo completo do lote pelo Rendimento atual da Ficha. Os resultados são derivados em consulta, sem arredondamento intermediário e sem persistência.

## UC023 — Preço teórico e sugerido

UC023 usa o `CustoUnitarioProduto` calculado pelo UC022 e a `MargemAlvo` persistida no Produto para obter o `PrecoTeorico` pela RN020.

O `PrecoSugerido` aplica RN021 usando o `IncrementoComercial` vigente da Empresa, sempre para o menor múltiplo maior ou igual ao preço teórico. Se o preço teórico já for múltiplo exato do incremento, ele é preservado.

Quando o custo unitário estiver indisponível, ambos os preços ficam indisponíveis. Quando somente o incremento comercial estiver ausente, o preço teórico continua explicável, mas o preço sugerido permanece indisponível e a precificação continua incompleta.

`MargemPadrao` não substitui a margem do Produto e `ReservaComercialDesconto` não participa do cálculo de UC023. Os resultados são derivados em consulta, sem persistência e sem arredondamento intermediário.

## UC011 — Preço de prateleira e snapshot comercial

UC011 registra a decisão comercial append-only e congela `CustoReferencia`, `MargemReferencia`, `PrecoSugerido`, `PrecoPrateleira` e `ReservaComercialReferencia` no momento do POST. O registro atual é derivado do histórico, não armazenado diretamente no Produto.

## UC012 — Histórico de precificação

UC012 consulta os snapshots comerciais em `DataReferencia DESC, Id DESC`; o primeiro registro é o Preço de prateleira atual. O `DescontoReferencia` é calculado em consulta usando exclusivamente `PrecoSugerido`, `PrecoPrateleira` e `ReservaComercialReferencia` do próprio registro.

Configuração, custo, margem e Ficha Técnica atuais não reinterpretam decisões antigas. `DescontoReferencia` permanece derivado e não persistido. Se `PrecoSugerido = 0`, o desconto é não aplicável porque a razão percentual é indefinida. UC012 não calcula `MargemAtual` nem situação — esses conceitos permanecem no UC024.

## UC024 — Margem atual e situação

UC024 combina três valores correntes: `CustoUnitarioProduto` atual da UC022, `PrecoPrateleiraAtual` selecionado pela UC012 e `Produto.MargemAlvo` atual.

~~~text
MargemAtual = (PrecoPrateleiraAtual - CustoUnitarioProduto) / PrecoPrateleiraAtual
~~~

Sem custo atual ou sem Preço de prateleira atual, a situação é `Incompleto` e a Margem atual fica indisponível. Com ambos conhecidos, `MargemAtual < MargemAlvo` resulta `AbaixoDaMargem`; igualdade ou valor superior resulta `DentroDaMargem`.

O cálculo não usa `CustoReferencia`/`MargemReferencia` históricos, Preço sugerido, Incremento comercial, Reserva comercial ou Desconto de referência. Em especial, `IncrementoComercial = null` não impede Margem atual quando custo e preço corrente são conhecidos.

Margem atual e situação são derivadas em consulta, sem arredondamento intermediário e sem persistência. Produto inativo permanece calculável.

## Regras relacionadas

RN004, RN006, RN007, RN009 a RN027, RN055 e RN056.

## Princípio de explicabilidade

O usuário deve conseguir rastrear os componentes usados no resultado. A interface não deve exibir somente um número final sem detalhamento de origem.
