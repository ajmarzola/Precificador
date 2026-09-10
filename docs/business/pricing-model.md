# Modelo de Precificação

## Contexto de empresa

Todo cálculo é realizado dentro de uma Empresa Ativa. Insumos, preços, produtos, fichas e configurações usados em um cálculo devem pertencer à mesma Empresa.

## Unidade de cálculo

A ficha técnica representa **um lote/execução produtiva**. O produto é vendido em uma ou mais unidades derivadas desse lote.

## Composição do custo

### 1. Itens da ficha técnica

Para cada insumo:

```text
custo_item = quantidade_utilizada × custo_unitario_atual
```

### 2. Perdas

Perdas são opcionais e representam material/processo desperdiçado quando aplicável. A regra exata será revalidada antes do UC019 para não impor uma semântica exclusiva de panificação.

### 3. Mão de obra

```text
custo_mao_de_obra_lote = tempo_ativo_em_horas × valor_hora_da_empresa
```

### 4. Equipamentos/energia

Forno deixa de ser conceito universal. Quando o consumo de um equipamento for relevante:

```text
custo_energia_uso = potencia_equipamento_kW × tempo_uso_h × tarifa_kWh_da_empresa
```

O custo de energia do lote é a soma dos usos aplicáveis.

### 5. Custo do lote

```text
custo_lote =
    custo_itens
  + perdas_aplicaveis
  + custo_mao_de_obra
  + recursos_e_equipamentos_aplicaveis
```

### 6. Custo unitário

```text
custo_unitario_produto = custo_lote / rendimento
```

## Formação do preço

A margem continua sendo margem sobre preço de venda:

```text
preco_teorico = custo_unitario / (1 - margem_alvo)
```

Preço sugerido é o preço teórico arredondado para cima pelo incremento comercial configurado da Empresa.

## Margem atual

```text
margem_atual = (preco_venda - custo_unitario) / preco_venda
```

## Ausência de dados

Se um dado obrigatório estiver ausente, o cálculo é **incompleto**. Valores faltantes não são substituídos por zero.

## Histórico

Preço de Insumo e preço de venda preservam histórico. O custo atual continua calculado sob demanda.

## Golden cases

O motor terá cenários canônicos conferidos dos processos de referência, sempre respeitando o contexto de Empresa.
