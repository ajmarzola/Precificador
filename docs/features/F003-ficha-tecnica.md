# F003 — Ficha Técnica

## Objetivo

Definir os materiais e recursos necessários para uma execução/lote de um Produto, independentemente do tipo de negócio.

## Capacidades previstas

- informar rendimento;
- adicionar/remover insumos;
- quantidade por item;
- observação contextual por item;
- tempo ativo de trabalho;
- registrar perdas de material/processo quando aplicáveis;
- registrar uso de equipamentos quando aplicável;
- apresentar composição de custo.

## Modelo

Ficha Técnica não é sinônimo de receita de panificação.

Insumo referenciado é específico da Empresa e Marca. Observação contextual explica decisões daquela ficha.

`TempoForno` não será campo universal; forno será tratado como equipamento. Impressora, laminadora e outros recursos poderão usar o mesmo mecanismo.

A regra detalhada de perdas e equipamentos será fechada antes dos respectivos UCs.

## Isolamento

Produto, ficha, itens, insumos e equipamentos de uma ficha devem pertencer à mesma Empresa.

## Fora do escopo do MVP

Preparações intermediárias reutilizáveis continuam pós-MVP até nova autorização.
