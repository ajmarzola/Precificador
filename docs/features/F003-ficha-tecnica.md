# F003 — Ficha Técnica

## Objetivo

Definir os recursos necessários para produzir um lote/execução de cada Produto, sem assumir um processo produtivo específico.

## Capacidades

- informar rendimento do lote;
- adicionar e remover insumos da ficha;
- informar quantidade de cada insumo na unidade base;
- registrar observação contextual opcional por item da ficha técnica;
- informar tempo ativo de trabalho por lote;
- registrar perdas de material/processo quando aplicáveis;
- registrar uso de equipamentos/recursos quando aplicável;
- apresentar composição de custo por item.

## Regras relacionadas

RN009 a RN017, RN034 e regras multiempresa aplicáveis.

## Modelo

A ficha técnica representa um lote/execução. O rendimento transforma o custo do lote em custo por unidade de venda.

A ficha não deve ser sinônimo de receita de panificação. `TempoForno` não será tratado como campo universal; a direção aprovada é modelar forno, impressora, laminadora e outros recursos como equipamentos quando esse domínio for detalhado.

Cada item da ficha referencia um Insumo específico da mesma Empresa. Como Marca faz parte da identidade econômica do Insumo após UC001A, selecionar `Farinha de Trigo Branca / Renata` é diferente de selecionar `Farinha de Trigo Branca / Caputo`.

A observação contextual do item registra a razão de uso naquela ficha e é independente da observação técnica global do Insumo.

Insumos desativados já existentes em uma ficha continuam visíveis para preservar a consistência histórica, mas não podem ser adicionados a novas composições.

Produto, Ficha Técnica, itens, Insumos e futuros equipamentos utilizados na mesma composição devem pertencer à mesma Empresa.

## Fora do escopo do MVP

Preparações intermediárias reutilizáveis como itens compostos continuam pós-MVP até nova autorização.
