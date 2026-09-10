# Modelo de Precificação

## Contexto de empresa

Todo cálculo é realizado dentro de uma Empresa Ativa. Insumos, preços, produtos, fichas, equipamentos e configurações utilizados no cálculo pertencem à mesma empresa.

## Unidade de cálculo

A Ficha Técnica representa um lote/execução produtiva e seu rendimento em unidades de venda.

## Composição do custo

### Materiais/insumos

```text
custo_item = quantidade_utilizada × custo_unitario_atual
```

### Perdas

Perdas são opcionais e representam material/processo desperdiçado quando aplicável. A regra detalhada será fechada antes do UC019 para atender processos distintos sem impor semântica de panificação.

### Mão de obra

```text
custo_mao_de_obra_lote = tempo_ativo_em_horas × valor_hora_da_empresa
```

### Equipamentos/energia

Forno deixa de ser conceito universal. Quando um equipamento tiver consumo relevante:

```text
custo_energia_uso = potencia_equipamento_kW × tempo_uso_h × tarifa_kWh_da_empresa
```

Somar os usos aplicáveis ao lote.

### Custo do lote

```text
custo_lote = materiais + perdas_aplicaveis + mao_de_obra + recursos_aplicaveis
```

### Custo unitário

```text
custo_unitario_produto = custo_lote / rendimento
```

## Formação do preço

```text
preco_teorico = custo_unitario / (1 - margem_alvo)
```

Preço sugerido é arredondado para cima pelo incremento comercial configurado da Empresa.

## Margem atual

```text
margem_atual = (preco_venda - custo_unitario) / preco_venda
```

## Ausência de dados

Dado obrigatório ausente torna a precificação incompleta. Nunca substituir por zero.

## Histórico

Preço de Insumo e preço de venda permanecem históricos. Custo atual é derivado sob demanda.

## Golden cases

Haverá casos canônicos de diferentes processos/empresas de referência, sem misturar dados entre tenants.
