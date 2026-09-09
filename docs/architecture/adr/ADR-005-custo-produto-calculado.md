# ADR-005 — Custo atual do produto é calculado

- **Status:** Aceito
- **Data:** 2026-09-09

## Contexto

Persistir `CustoAtual` no produto criaria duplicação de informação e risco de ficar desatualizado quando um preço de insumo ou configuração global mudar.

## Decisão

O custo atual do produto não será um atributo persistido do cadastro do produto. Será calculado a partir da ficha técnica, preços vigentes dos insumos, rendimento e configurações de produção.

## Consequências

- uma alteração de insumo reflete nos produtos sem rotina de sincronização;
- não existe risco de um campo de custo ficar divergente da ficha técnica;
- consultas de dashboard precisarão compor os dados necessários ao cálculo;
- snapshots históricos podem ser adicionados em eventos específicos sem transformar o valor atual em dado redundante.
