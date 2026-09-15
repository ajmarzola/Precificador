# Glossário

## Insumo

Item consumido ou utilizado na produção de um produto. No MVP, pode representar ingrediente, embalagem ou consumível. Quando houver marca comercial relevante, cada combinação de item + marca representa um insumo economicamente distinto.

## Marca do insumo

Identificação comercial opcional do insumo. Marcas diferentes de um mesmo item podem possuir históricos de preço e custos distintos.

## Observação do insumo

Anotação técnica global do cadastro, como força W de uma farinha, teor de proteína ou característica de embalagem. Não representa justificativa específica de uma receita.

## Observação do item da ficha técnica

Anotação contextual associada ao uso de um insumo em uma receita, destinada a registrar razões como a escolha de uma marca específica. É independente da observação global do insumo.

## Ingrediente

Termo legado para matéria-prima alimentícia. No domínio atual, perda não é automática por Categoria; quando aplicável, é configurada no Item da Ficha.

## Embalagem

Insumo utilizado para acondicionar a unidade vendida ou o lote. É precificado como qualquer outro Insumo e pode possuir perda esperada quando o processo justificar; nenhuma Categoria recebe perda automaticamente.

## Consumível

Insumo de produção associado ao produto e consumido no processo, sem necessariamente compor o alimento.

## Unidade base

Unidade em que o insumo é usado nas fichas técnicas. O escopo atual trabalha com `g`, `ml`, `m` e `un`.

## Registro de preço

Evento que informa quanto foi pago por determinada quantidade de um insumo em uma data de referência. O registro pertence a um Insumo específico e, portanto, distingue marcas diferentes.

## Custo unitário do insumo

Preço da compra dividido pela quantidade da embalagem, expresso na unidade base do insumo.

## Preço atual do insumo

Registro de preço vigente mais recente segundo as regras de negócio. Não é um atributo sobrescrito no cadastro do insumo.

## Produto

Item comercializado cuja precificação é calculada pelo sistema.

No cadastro inicial, Produto possui identidade por Empresa + Nome, Categoria opcional, Margem-alvo e situação. Preço de prateleira e snapshots de precificação possuem histórico próprio e não são campos obrigatórios do cadastro inicial. Produção/composição pertence à Ficha Técnica.

## Categoria do produto

Texto livre opcional usado para organização do catálogo de Produtos dentro de uma Empresa. Não participa da identidade nem altera regras de cálculo.

## Ficha técnica

Definição produtiva atual de um lote/execução de Produto. A base introduzida pela UC013 contém Rendimento e Tempo ativo; composição por Insumos e demais recursos são acrescentados em UCs posteriores. No MVP existe no máximo uma Ficha Técnica atual por Produto.

## Lote

Quantidade produzida por uma execução da ficha técnica.

## Rendimento

Quantidade decimal de unidades de venda obtidas por lote. Deve ser maior que zero.

## Perda esperada de material

Percentual adicional configurado em um Item da Ficha para representar desperdício esperado de material no lote. É aplicado sobre a quantidade/custo base do Item. Perdas que reduzem unidades finais vendáveis pertencem ao Rendimento, evitando dupla contagem.

## Tempo ativo

Tempo de trabalho humano ativo necessário para executar o lote, informado em minutos inteiros. Pode ser zero quando explicitamente não houver trabalho ativo.

## Custo do lote

Somatório dos itens e recursos necessários para produzir o lote.

## Custo unitário do produto

Custo do lote dividido pelo rendimento.

## Margem-alvo

Percentual de margem desejado para um produto. Na persistência/domínio é representado como fração decimal: 30% = 0,30.

## Margem atual

Margem obtida usando o Preço de prateleira vigente e o custo unitário atual.

## Preço teórico

Menor preço matemático, antes da política comercial de arredondamento, necessário para atingir exatamente a margem-alvo.

## Preço sugerido

Preço teórico após a política configurada de arredondamento.

## Precificação incompleta

Estado em que o sistema não dispõe de dados suficientes para calcular com segurança o custo ou a margem. Um valor ausente nunca é convertido silenciosamente em zero.

## Golden case

Cenário de referência com entradas e saída esperada conhecidas, utilizado para validar o motor de precificação contra cálculos previamente conferidos.
