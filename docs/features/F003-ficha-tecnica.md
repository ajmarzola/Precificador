# F003 — Ficha Técnica

## Objetivo

Definir os recursos necessários para produzir um lote/execução de cada Produto de forma aplicável a diferentes negócios artesanais.

## Capacidades

- informar rendimento do lote;
- adicionar e remover insumos da ficha;
- informar quantidade de cada insumo na unidade base;
- registrar observação contextual opcional por item;
- informar tempo ativo de trabalho por lote;
- registrar perdas quando aplicáveis;
- registrar uso de equipamentos/recursos quando aplicável;
- apresentar composição de custo por item.

## Modelo

A ficha técnica representa um lote/execução. O rendimento transforma o custo do lote em custo por unidade de venda.

Ela não deve assumir panificação como semântica universal. `TempoForno` será reavaliado como uso de equipamento genérico antes dos UCs correspondentes.

Cada item referencia um Insumo específico da mesma Empresa. A observação contextual do item é independente da observação global do Insumo.

Insumos desativados já referenciados continuam visíveis para preservar consistência histórica.

## Regras relacionadas

RN009 a RN017, RN034 e regras multiempresa aplicáveis.

## Fora do escopo do MVP

Preparações intermediárias reutilizáveis continuam pós-MVP até nova autorização.
