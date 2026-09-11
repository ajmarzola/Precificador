# Modelo de Precificação

## Contexto de empresa

Todo cálculo é realizado dentro de uma Empresa Ativa. Insumos, preços, produtos, fichas técnicas, configurações e futuros recursos/equipamentos usados em um cálculo devem pertencer à mesma Empresa.

## Unidade de cálculo

A ficha técnica representa **um lote/execução produtiva**. O produto é vendido em uma ou mais unidades derivadas desse lote. Todos os tempos e quantidades da ficha são informados para o lote/execução.

## Composição do custo

### 1. Itens da ficha técnica

Para cada insumo:

```text
custo_item = quantidade_utilizada × custo_unitário_atual
```

Os itens podem representar materiais/ingredientes, embalagens ou consumíveis conforme a classificação vigente do domínio.

### 2. Perdas

Perdas são opcionais e representam material/processo desperdiçado quando aplicável.

A regra originalmente definida apenas para ingredientes de panificação será revalidada antes do UC019 para atender diferentes processos produtivos sem tornar perda um atributo obrigatório de todos os Produtos.

### 3. Mão de obra

```text
custo_mão_de_obra_lote = tempo_ativo_em_horas × valor_hora_da_empresa
```

### 4. Equipamentos e energia

`TempoForno` e `PotenciaFornoKw` deixam de ser conceitos universais. Quando o consumo de um equipamento for relevante:

```text
custo_energia_uso = potência_equipamento_kW × tempo_uso_h × tarifa_kWh_da_empresa
```

O custo de energia do lote é a soma dos usos de equipamentos aplicáveis. Forno é apenas um possível equipamento.

### 5. Custo do lote

Conceitualmente:

```text
custo_lote =
    custo_itens
  + perdas_aplicáveis
  + custo_mão_de_obra_lote
  + custos_de_recursos_e_equipamentos_aplicáveis
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

O preço sugerido é o preço teórico arredondado para cima para o incremento comercial configurado da Empresa.

## Margem atual

```text
margem_atual = (preço_venda - custo_unitário) / preço_venda
```

A margem atual sempre usa o custo calculado com preços e configurações vigentes da mesma Empresa.

## Ausência de dados

Se um dado obrigatório estiver ausente ou inválido, o cálculo é **incompleto**. Valores faltantes não devem ser substituídos por zero.

## Histórico

### Insumos

O histórico registra compras/preços conhecidos do Insumo. O custo atual deriva do registro vigente mais recente e permanece isolado pela Empresa proprietária do Insumo.

A interpretação histórica depende da estabilidade cadastral definida pela RN040: depois do primeiro preço, Nome, Marca e Unidade base do Insumo não podem ser alterados. Isso evita que um preço antigo passe a aparentar pertencer a outra identidade econômica ou que a quantidade de compra histórica mude de significado por troca de unidade.

No MVP, os registros de preço referenciam o Insumo por `InsumoId` e **não precisam duplicar snapshots de Nome, Marca e Unidade base**. Se no futuro houver requisito de auditoria que exija reproduzir exatamente a apresentação cadastral de cada momento, snapshots/versionamento deverão ser modelados explicitamente em vez de relaxar a RN040.

### Produtos

O preço de venda praticado possui histórico próprio. O custo atual continua calculado sob demanda. Quando snapshots históricos de custo forem necessários, deverão ser modelados explicitamente.

## Golden cases

O motor de cálculo deve possuir cenários canônicos derivados de produtos conferidos nos negócios de referência. Os golden cases devem validar a composição completa do custo e respeitar o contexto da Empresa.
