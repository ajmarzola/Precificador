# Regras de Negócio

Este documento contém regras normativas do Precificador. Casos de uso devem referenciar os identificadores abaixo em vez de copiar fórmulas.

## Insumos e preços

### RN001 — Unidade base

Cada insumo deve possuir uma unidade base. No MVP, as unidades previstas são `g`, `ml` e `un`.

### RN002 — Quantidade de compra válida

A quantidade informada em um registro de preço deve ser maior que zero.

### RN003 — Preço de compra válido

O preço pago deve ser maior que zero.

### RN004 — Custo unitário do insumo

`CustoUnitario = PrecoCompra / QuantidadeCompra`.

O cálculo deve preservar precisão decimal suficiente para que insumos baratos por grama/ml não sejam arredondados prematuramente.

### RN005 — Histórico de preços

Registrar um novo preço não deve sobrescrever o registro anterior. Cada registro pertence a um Insumo específico; marcas diferentes do mesmo item possuem históricos independentes por serem Insumos distintos.

### RN006 — Preço atual

O preço atual é o registro de preço vigente mais recente para o insumo. Registros com data futura não são considerados vigentes antes de sua data de referência. Em empate de data, prevalece o registro criado por último.

### RN007 — Insumo sem preço

Insumo sem preço vigente possui custo desconhecido. O sistema não deve tratá-lo como custo zero.

### RN008 — Desativação de insumo

Insumos são desativados, não excluídos fisicamente pelo fluxo normal. Um insumo desativado não pode ser adicionado a novas fichas técnicas, mas referências já existentes devem permanecer legíveis.

### RN028 — Nome do insumo

Todo insumo deve possuir nome válido.

O nome:

- é obrigatório;
- deve possuir no máximo 120 caracteres após normalização de espaços;
- deve ter espaços em branco removidos do início e do fim;
- deve ter sequências internas de whitespace reduzidas a um único espaço;
- preserva para exibição a capitalização informada pelo usuário após a limpeza de espaços.

Para comparação determinística, o sistema mantém uma representação normalizada do nome em maiúsculas com regra invariável. A normalização de comparação não remove acentos.

### RN029 — Unicidade do insumo por nome e marca

Não podem existir dois insumos com a mesma combinação de `NomeNormalizado` e `MarcaNormalizada`, independentemente de estarem ativos ou inativos.

Marcas diferentes do mesmo Nome representam insumos distintos e podem coexistir.

Quando a Marca não for informada, `MarcaNormalizada` deve assumir string vazia como representação técnica, garantindo que dois insumos sem marca e com o mesmo Nome também sejam considerados duplicados.

A integridade deve ser protegida pelo banco com índice/restrição única composta, além da validação funcional usada para apresentar mensagem amigável ao usuário.

### RN030 — Categoria do insumo

Todo insumo deve pertencer exatamente a uma das categorias do MVP:

- Ingrediente;
- Embalagem;
- Consumível.

Nenhum valor indefinido/zero é considerado categoria funcional válida.

### RN031 — Situação inicial do insumo

Todo novo insumo é criado como ativo. A situação inicial não é escolhida pelo usuário no cadastro.

### RN032 — Marca do insumo

A Marca identifica opcionalmente a variação comercial de um insumo.

A Marca:

- é opcional;
- possui no máximo 80 caracteres após normalização;
- remove espaços externos e reduz sequências internas de whitespace a um único espaço;
- preserva a capitalização informada para exibição;
- possui representação `MarcaNormalizada` em maiúsculas com regra invariável;
- não remove acentos durante a normalização de comparação;
- quando ausente, é armazenada como `null`, enquanto `MarcaNormalizada` usa string vazia.

Marcas diferentes do mesmo Nome representam insumos economicamente distintos e podem possuir preços/custos diferentes.

### RN033 — Observação do insumo

A Observação do Insumo é uma anotação técnica global e opcional, como força W de uma farinha ou característica relevante de embalagem.

A observação:

- possui no máximo 1000 caracteres;
- remove apenas whitespace externo;
- preserva conteúdo interno e quebras de linha;
- se vazia ou composta apenas por whitespace, é armazenada como `null`;
- não participa da identidade ou unicidade do Insumo.

### RN034 — Observação contextual do item da ficha técnica

Um item de ficha técnica pode possuir observação contextual própria para registrar a justificativa de uso daquele Insumo na receita, como a razão para escolher uma marca específica.

Essa observação é independente da Observação global do Insumo e deve permanecer associada ao item da ficha. Alterações posteriores na Observação do Insumo não devem sobrescrever a justificativa registrada na receita.

A implementação desta regra pertence ao UC014/UC015, não ao UC001A ou UC002.

## Produtos e ficha técnica

### RN009 — Rendimento

Toda ficha técnica precificável deve possuir rendimento maior que zero, expresso em unidades de venda por lote.

### RN010 — Quantidade na ficha

A quantidade de cada item da ficha técnica deve ser maior que zero e estar expressa na unidade base do insumo.

### RN011 — Custo dos itens do lote

Para cada item: `CustoItem = QuantidadeUtilizada × CustoUnitarioAtualDoInsumo`.

O custo base dos itens é a soma dos custos dos itens da ficha.

### RN012 — Perda de ingredientes

O percentual de perda do produto incide apenas sobre itens categorizados como ingrediente. Embalagens e consumíveis não recebem essa perda automaticamente.

`CustoPerda = CustoIngredientes × PercentualPerda`.

### RN013 — Mão de obra

O tempo ativo é informado para o lote.

`CustoMaoDeObraLote = (TempoAtivoMinutos / 60) × ValorHoraTrabalho`.

### RN014 — Energia do forno

O tempo de forno é informado para o lote.

`CustoEnergiaLote = PotenciaFornoKw × (TempoFornoMinutos / 60) × TarifaKwh`.

### RN015 — Custo do lote

`CustoLote = CustoItens + CustoPerda + CustoMaoDeObraLote + CustoEnergiaLote`.

### RN016 — Custo unitário do produto

`CustoUnitarioProduto = CustoLote / Rendimento`.

### RN017 — Precificação incompleta

Se qualquer dado obrigatório para o cálculo estiver ausente ou inválido — incluindo preço vigente de um insumo — o produto deve ser marcado como precificação incompleta. Não deve ser exibido custo total ou margem como se fossem confiáveis.

### RN018 — Desativação de produto

Produtos são desativados, não excluídos fisicamente pelo fluxo normal.

## Margem e preço

### RN019 — Margem-alvo válida

A margem-alvo deve ser maior ou igual a zero e menor que 100%.

### RN020 — Preço teórico

`PrecoTeorico = CustoUnitarioProduto / (1 - MargemAlvo)`.

### RN021 — Arredondamento do preço sugerido

O preço sugerido deve ser arredondado **para cima** para o próximo múltiplo do incremento comercial configurado, garantindo que o arredondamento não reduza a margem abaixo da margem-alvo.

Exemplo com incremento de R$ 0,50: R$ 25,08 resulta em R$ 25,50.

### RN022 — Margem atual

Para preço de venda maior que zero:

`MargemAtual = (PrecoVendaAtual - CustoUnitarioProduto) / PrecoVendaAtual`.

### RN023 — Produto abaixo da margem

Um produto com precificação completa está abaixo da margem quando `MargemAtual < MargemAlvo`.

### RN024 — Preço de venda e histórico

Alterar o preço de venda deve preservar o valor anteriormente praticado em histórico com data da alteração.

## Configurações

### RN025 — Configurações globais

O MVP possui configurações globais para, no mínimo:

- valor/hora de trabalho;
- tarifa de energia por kWh;
- potência do forno em kW;
- margem padrão para novos produtos;
- incremento comercial de arredondamento.

Alterações de configuração afetam imediatamente cálculos atuais que dependam delas.

## Precisão

### RN026 — Arredondamento intermediário

Cálculos intermediários não devem ser arredondados para centavos. O arredondamento monetário de apresentação ocorre apenas nas fronteiras definidas pelo modelo de precificação.

## Status de cálculo

### RN027 — Estados mínimos

Para fins de acompanhamento, um produto ativo pode estar pelo menos em um dos estados:

- `Incompleto`: não pode ser precificado com segurança;
- `AbaixoDaMargem`: cálculo válido e margem atual inferior à meta;
- `DentroDaMargem`: cálculo válido e margem atual igual ou superior à meta.
