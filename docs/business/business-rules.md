# Regras de Negócio

Este documento contém regras normativas do Precificador. Casos de uso devem referenciar os identificadores abaixo em vez de copiar fórmulas.

## Insumos e preços

### RN001 — Unidade base

Cada insumo deve possuir uma unidade base. No código atual, as unidades previstas são `g`, `ml` e `un`.

A adequação desse conjunto para diferentes tipos de negócio será revalidada antes do UC002; a FT002 não altera unidades.

### RN002 — Quantidade de compra válida

A quantidade informada em um registro de preço deve ser maior que zero.

### RN003 — Preço de compra válido

O preço pago deve ser maior que zero.

### RN004 — Custo unitário do insumo

`CustoUnitario = PrecoCompra / QuantidadeCompra`.

O cálculo deve preservar precisão decimal suficiente para que insumos baratos por unidade base não sejam arredondados prematuramente.

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

O nome é obrigatório, possui no máximo 120 caracteres após normalização de espaços, remove whitespace externo, reduz sequências internas a um espaço e preserva capitalização para exibição.

Para comparação determinística, o sistema mantém representação normalizada em maiúsculas invariáveis sem remover acentos.

### RN029 — Unicidade do insumo por empresa, nome e marca

A identidade funcional de Insumo é limitada à Empresa proprietária.

Após FT002 e antes do UC001A, não podem existir dois Insumos da mesma Empresa com o mesmo `NomeNormalizado`.

Após UC001A, a unicidade passa a ser `EmpresaId + NomeNormalizado + MarcaNormalizada`.

Empresas diferentes podem cadastrar a mesma combinação Nome/Marca. A unicidade continua incluindo registros inativos.

### RN030 — Categoria do insumo

O código atual prevê Ingrediente, Embalagem e Consumível. O vocabulário será revalidado antes do UC002 para suportar negócios não alimentícios; nenhum valor indefinido/zero é válido.

### RN031 — Situação inicial do insumo

Todo novo insumo é criado como ativo. A situação inicial não é escolhida pelo usuário no cadastro.

### RN032 — Marca do insumo

A Marca identifica opcionalmente a variação comercial de um insumo.

A Marca é opcional, possui no máximo 80 caracteres após normalização, preserva capitalização para exibição, mantém `MarcaNormalizada` em maiúsculas invariáveis e não remove acentos. Quando ausente, `Marca = null` e `MarcaNormalizada = ""`.

Marcas diferentes do mesmo Nome representam insumos economicamente distintos e podem possuir preços/custos diferentes dentro da mesma Empresa.

### RN033 — Observação do insumo

A Observação é anotação técnica global opcional, máximo 1000 caracteres, com trim externo e preservação do conteúdo interno. Whitespace-only é armazenado como `null` e não participa da identidade.

### RN034 — Observação contextual do item da ficha técnica

Um item de ficha técnica pode possuir observação contextual própria para registrar a justificativa de uso daquele Insumo naquela ficha. Essa observação é independente da Observação global do Insumo.

A implementação pertence aos UCs de Ficha Técnica, não ao UC001A/UC002.

## Multiempresa e acesso

### RN035 — Propriedade por empresa

Dados operacionais tenant-owned pertencem exatamente a uma Empresa por `EmpresaId` obrigatório.

### RN036 — Isolamento de empresa

Operações comuns só podem ler ou alterar dados tenant-owned da Empresa Ativa. Ausência de Empresa Ativa não concede acesso implícito a qualquer empresa.

### RN037 — Empresa ativa

Usuário autenticado trabalha no contexto de uma única Empresa Ativa por vez e só pode ativar empresa para a qual possua vínculo ativo e cuja situação esteja ativa.

### RN038 — Vínculo usuário-empresa

Um usuário pode possuir vínculos com múltiplas empresas. Vínculo inativo ou empresa inativa não concede acesso operacional.

### RN039 — Configurações por empresa

Configurações de precificação pertencem a uma Empresa e alterações não afetam cálculos de outra Empresa.

## Produtos e ficha técnica

### RN009 — Rendimento

Toda ficha técnica precificável deve possuir rendimento maior que zero, expresso em unidades de venda por lote/execução.

### RN010 — Quantidade na ficha

A quantidade de cada item da ficha técnica deve ser maior que zero e estar expressa na unidade base do insumo.

### RN011 — Custo dos itens do lote

Para cada item: `CustoItem = QuantidadeUtilizada × CustoUnitarioAtualDoInsumo`.

O custo base dos itens é a soma dos custos dos itens da ficha.

### RN012 — Perdas

Perda deixa de ser assumida como atributo obrigatório específico de panificação. Quando aplicável, representa perda de material/processo e seu modelo exato será fechado antes do UC019.

Até esse detalhamento, não implementar nova fórmula além das já aprovadas para o código inexistente.

### RN013 — Mão de obra

O tempo ativo é informado para o lote/execução.

`CustoMaoDeObraLote = (TempoAtivoMinutos / 60) × ValorHoraTrabalhoDaEmpresa`.

### RN014 — Energia de equipamento

Quando houver equipamento mensurável, a fórmula geral é:

`CustoEnergiaUso = PotenciaEquipamentoKw × (TempoUsoMinutos / 60) × TarifaKwhDaEmpresa`.

Forno é um equipamento possível, não conceito obrigatório de toda Ficha Técnica. O modelo de equipamentos será detalhado antes do UC021.

### RN015 — Custo do lote

O custo do lote soma itens, perdas aplicáveis, mão de obra e recursos/equipamentos aplicáveis.

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

O preço sugerido deve ser arredondado **para cima** para o próximo múltiplo do incremento comercial configurado da Empresa, garantindo que o arredondamento não reduza a margem abaixo da margem-alvo.

### RN022 — Margem atual

Para preço de venda maior que zero:

`MargemAtual = (PrecoVendaAtual - CustoUnitarioProduto) / PrecoVendaAtual`.

### RN023 — Produto abaixo da margem

Um produto com precificação completa está abaixo da margem quando `MargemAtual < MargemAlvo`.

### RN024 — Preço de venda e histórico

Alterar o preço de venda deve preservar o valor anteriormente praticado em histórico com data da alteração.

## Configurações

### RN025 — Configurações de precificação

Cada Empresa possui configurações para, no mínimo:

- valor/hora de trabalho;
- tarifa de energia por kWh;
- margem padrão para novos produtos;
- incremento comercial de arredondamento.

Potência passa a pertencer ao Equipamento quando esse domínio for implementado. Alterações afetam imediatamente apenas cálculos atuais da mesma Empresa.

## Precisão

### RN026 — Arredondamento intermediário

Cálculos intermediários não devem ser arredondados para centavos. O arredondamento monetário de apresentação ocorre apenas nas fronteiras definidas pelo modelo de precificação.

## Status de cálculo

### RN027 — Estados mínimos

Para fins de acompanhamento, um produto ativo pode estar pelo menos em um dos estados:

- `Incompleto`;
- `AbaixoDaMargem`;
- `DentroDaMargem`.
