# Regras de Negócio

Este documento contém regras normativas do Precificador.

## Insumos e preços

### RN001 — Unidade base

Cada insumo possui unidade base. O conjunto inicial `g`, `ml`, `un` será generalizado no UC001B para atender os dois negócios; até lá permanece válido para o código existente.

### RN002 — Quantidade de compra válida

Quantidade de compra > 0.

### RN003 — Preço de compra válido

Preço pago > 0.

### RN004 — Custo unitário do insumo

`CustoUnitario = PrecoCompra / QuantidadeCompra`, preservando precisão intermediária.

### RN005 — Histórico de preços

Novo preço não sobrescreve anterior.

### RN006 — Preço atual

Preço atual é o registro vigente mais recente do insumo; datas futuras não vigem e, em empate de data, prevalece o criado por último.

### RN007 — Insumo sem preço

Custo desconhecido nunca é zero implícito.

### RN008 — Desativação de insumo

Insumo é desativado, não excluído no fluxo normal. Referências históricas permanecem legíveis.

### RN028 — Nome do insumo

Nome obrigatório, máximo 120 após normalização; trim externo, whitespace interno colapsado e capitalização preservada para exibição. Comparação usa maiúsculas invariáveis sem remover acentos.

### RN029 — Unicidade do insumo

A unicidade do Insumo é sempre limitada à Empresa proprietária.

Após FT002 e antes de UC001A: `(EmpresaId, NomeNormalizado)`.

Após UC001A: `(EmpresaId, NomeNormalizado, MarcaNormalizada)`.

Registros de empresas diferentes podem possuir o mesmo nome/marca.

### RN030 — Categoria do insumo

O conjunto atual Ingrediente/Embalagem/Consumível permanece tecnicamente válido até o UC001B, que deverá generalizar o vocabulário para múltiplos negócios sem perda semântica.

### RN031 — Situação inicial do insumo

Novo Insumo nasce ativo.

### RN032 — Marca do insumo

Marca opcional, máximo 80, normalizada para comparação e participante da identidade do Insumo dentro da Empresa após UC001A.

### RN033 — Observação do insumo

Observação técnica global opcional, máximo 1000, não participa da identidade.

### RN034 — Observação contextual da ficha

A justificativa de escolha de um Insumo em uma ficha pertence ao item da ficha e não é sobrescrita pela observação global do Insumo.

## Multiempresa e acesso

### RN035 — Propriedade por empresa

Dados operacionais tenant-owned pertencem exatamente a uma Empresa por `EmpresaId` obrigatório.

### RN036 — Isolamento de empresa

Operações comuns só podem ler ou alterar dados tenant-owned da Empresa Ativa. Ausência de Empresa Ativa não autoriza acesso implícito a qualquer empresa.

### RN037 — Empresa ativa

Usuário autenticado trabalha no contexto de uma única Empresa Ativa por vez. Só pode ativar empresa ativa para a qual possua vínculo ativo.

### RN038 — Vínculo usuário-empresa

Um usuário pode possuir vínculos com múltiplas empresas. Vínculo inativo ou empresa inativa não concede acesso operacional.

### RN039 — Configurações por empresa

Configurações de precificação pertencem a uma Empresa e não afetam cálculos de outra.

## Produtos e ficha técnica

### RN009 — Rendimento

Toda ficha precificável possui rendimento > 0 em unidades de venda por lote/execução.

### RN010 — Quantidade na ficha

Quantidade de item > 0 na unidade base do Insumo.

### RN011 — Custo dos itens

`CustoItem = QuantidadeUtilizada × CustoUnitarioAtualDoInsumo`.

### RN012 — Perdas de material/processo

Perdas são opcionais e devem ser aplicadas somente quando fizerem sentido ao material/processo. O desenho exato será fechado antes do UC019; não assumir percentual obrigatório exclusivo de panificação no Produto.

### RN013 — Mão de obra

`CustoMaoDeObraLote = (TempoAtivoMinutos / 60) × ValorHoraDaEmpresa`.

### RN014 — Energia de equipamento

Quando houver equipamento mensurável: `CustoEnergia = PotenciaEquipamentoKw × (TempoUsoMinutos / 60) × TarifaKwhDaEmpresa`. O custo do lote soma os usos aplicáveis. Forno é um tipo de equipamento, não um conceito universal.

### RN015 — Custo do lote

Custo do lote soma itens, perdas aplicáveis, mão de obra e usos de recursos/equipamentos aplicáveis.

### RN016 — Custo unitário

`CustoUnitarioProduto = CustoLote / Rendimento`.

### RN017 — Precificação incompleta

Dado obrigatório ausente/inválido torna a precificação incompleta; nunca substituir por zero silenciosamente.

### RN018 — Desativação de produto

Produto é desativado, não excluído no fluxo normal.

## Margem e preço

### RN019 — Margem-alvo válida

`0 <= MargemAlvo < 100%`.

### RN020 — Preço teórico

`PrecoTeorico = CustoUnitarioProduto / (1 - MargemAlvo)`.

### RN021 — Arredondamento

Preço sugerido é arredondado para cima ao incremento comercial da Empresa.

### RN022 — Margem atual

`MargemAtual = (PrecoVendaAtual - CustoUnitarioProduto) / PrecoVendaAtual`, para preço > 0.

### RN023 — Abaixo da margem

Produto completo está abaixo da meta quando `MargemAtual < MargemAlvo`.

### RN024 — Histórico de preço de venda

Alteração de preço preserva histórico.

## Configurações

### RN025 — Configurações de precificação

Cada Empresa possui, no mínimo, valor/hora, tarifa de energia, margem padrão e incremento de arredondamento. Potência pertence ao Equipamento quando aplicável.

## Precisão

### RN026 — Arredondamento intermediário

Não arredondar cálculos intermediários prematuramente.

## Status

### RN027 — Estados mínimos

`Incompleto`, `AbaixoDaMargem`, `DentroDaMargem`.
