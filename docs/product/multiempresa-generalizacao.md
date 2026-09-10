# Generalização multiempresa e de produção

## Objetivo

Registrar a evolução do Precificador para atender empresas de produção artesanal com processos diferentes, sem transformar o sistema em ERP nem duplicar motores de cálculo.

## Multiempresa

Dados operacionais pertencem a uma Empresa. Usuários acessam somente empresas com vínculo ativo e trabalham no contexto de uma Empresa Ativa.

Entidades tenant-owned previstas incluem Insumo, histórico de preço, Produto, Ficha Técnica, preço de venda, configurações e equipamentos/recursos quando modelados.

## Ficha Técnica genérica

A Ficha Técnica deve representar composição e recursos necessários para uma execução/lote de um produto, e não especificamente uma receita de panificação.

Conceitos comuns permanecem: rendimento, insumos/quantidades, observação contextual por item e tempo ativo de trabalho.

Perdas devem ser modeladas como conceito de material/processo quando aplicável, sem obrigar produtos que não possuem essa característica.

`TempoForno` e `PotenciaFornoKw` não devem ser conceitos universais. A direção aprovada é modelar futuramente uso de equipamento de forma genérica — forno, impressora, laminadora ou outro recurso — com potência/tempo quando isso for relevante ao custo.

O desenho exato será fechado antes dos UCs de Ficha Técnica e energia.

## Insumos

A generalização revelou que `Ingrediente` como categoria e apenas `g/ml/un` como unidades podem ser insuficientes para negócios não alimentícios.

Essa alteração não pertence à FT002. Antes do UC002, será feito um inventário real dos tipos de insumo dos dois negócios e então será documentado um ajuste específico, sem inventar unidades antecipadamente.

## Configurações

Configurações de precificação passam a pertencer a uma Empresa. Parâmetros específicos de equipamento pertencem ao equipamento quando esse domínio for implementado.

## Compatibilidade

As regras específicas de panificação atualmente documentadas permanecem como referência histórica até serem revalidadas antes de seus UCs de implementação. Nenhuma tabela de Produto/Ficha Técnica existe hoje, portanto essa generalização pode ser concluída documentalmente sem migration ou retrabalho de código neste momento.
