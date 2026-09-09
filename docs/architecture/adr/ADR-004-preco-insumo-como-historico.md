# ADR-004 — Preço de insumo como histórico

- **Status:** Aceito
- **Data:** 2026-09-09

## Contexto

Sobrescrever o preço no cadastro do insumo impede explicar a evolução de custos e dificulta identificar quando uma alteração impactou a margem dos produtos.

## Decisão

O preço não será armazenado como um único campo mutável do insumo. Cada atualização será um registro de preço contendo, no mínimo, data de referência, quantidade comprada e preço pago.

O custo unitário será derivado desses dados e o preço atual será o registro vigente mais recente segundo as regras de negócio.

## Consequências

- histórico de preços é preservado;
- atualizar um insumo altera automaticamente o custo atual dos produtos relacionados;
- o sistema pode explicar a origem do custo vigente;
- correções não devem apagar silenciosamente a história por meio do fluxo normal de atualização.
