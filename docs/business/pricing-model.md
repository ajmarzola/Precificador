# Modelo de Precificação

## Unidade de cálculo

A ficha técnica representa **um lote**. O produto é vendido em uma ou mais unidades derivadas desse lote. Todos os tempos e quantidades da ficha são informados para o lote.

## Composição do custo

### 1. Itens da ficha técnica

Para cada insumo:

```text
custo_item = quantidade_utilizada × custo_unitário_atual
```

Os itens podem ser ingredientes, embalagens ou consumíveis.

### 2. Perdas

A perda percentual é aplicada ao custo dos ingredientes do lote:

```text
custo_perda = custo_ingredientes × percentual_perda
```

### 3. Mão de obra

```text
custo_mão_de_obra_lote = tempo_ativo_em_horas × valor_hora
```

### 4. Energia

```text
custo_energia_lote = potência_forno_kW × tempo_forno_h × tarifa_kWh
```

### 5. Custo do lote

```text
custo_lote =
    custo_itens
  + custo_perda
  + custo_mão_de_obra_lote
  + custo_energia_lote
```

### 6. Custo unitário

```text
custo_unitário_produto = custo_lote / rendimento
```

## Formação do preço

A margem é tratada como margem sobre o preço de venda, não como simples multiplicador sobre custo.

```text
preço_teórico = custo_unitário / (1 - margem_alvo)
```

O preço sugerido é o preço teórico arredondado para cima para o incremento comercial configurado.

## Margem atual

```text
margem_atual = (preço_venda - custo_unitário) / preço_venda
```

A margem atual sempre usa o custo calculado com preços e configurações vigentes.

## Ausência de dados

Se um insumo da ficha não possuir preço vigente ou outro parâmetro obrigatório estiver ausente, o cálculo é **incompleto**. Valores faltantes não devem ser substituídos por zero.

## Histórico

### Insumos

O histórico registra as compras/preços conhecidos. O custo atual deriva do registro vigente mais recente.

### Produtos

O preço de venda praticado possui histórico próprio. O custo atual continua calculado sob demanda. Quando snapshots históricos de custo forem necessários para relatórios futuros, deverão ser modelados explicitamente, sem transformar o custo atual em campo redundante.

## Golden cases

O motor de cálculo deve possuir testes de referência derivados de produtos conferidos na planilha original. Os golden cases devem validar a composição completa do custo e não apenas fórmulas isoladas.

Os valores exatos dos casos canônicos serão congelados no momento da implementação do motor, depois de conferência final da ficha de referência.
