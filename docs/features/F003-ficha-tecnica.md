# F003 — Ficha Técnica

## Objetivo

Definir os recursos necessários para produzir um lote de cada produto.

## Capacidades

- informar rendimento do lote;
- adicionar e remover insumos da ficha;
- informar quantidade de cada insumo na unidade base;
- registrar observação contextual opcional por item da ficha técnica;
- informar tempo ativo de trabalho por lote;
- informar tempo de forno por lote;
- apresentar composição de custo por item.

## Regras relacionadas

RN009 a RN017 e RN034.

## Modelo

A ficha técnica representa um lote. O rendimento transforma o custo do lote em custo por unidade de venda.

Cada item da ficha referencia um Insumo específico. Como Marca faz parte da identidade econômica do Insumo, selecionar `Farinha de Trigo Branca / Renata` é diferente de selecionar `Farinha de Trigo Branca / Caputo`.

A futura observação contextual do item registra a razão de uso naquela receita, por exemplo por que determinada marca foi escolhida. Essa informação é independente da observação técnica global do Insumo.

Insumos desativados já existentes em uma ficha continuam visíveis para preservar a consistência do cadastro, mas não podem ser adicionados a novas composições.

## Fora do escopo do MVP

Preparações intermediárias reutilizáveis como itens compostos. Essa evolução está reconhecida no escopo pós-MVP.
