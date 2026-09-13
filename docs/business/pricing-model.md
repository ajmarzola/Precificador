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

A margem é tratada como margem sobre o preço, não como simples multiplicador sobre custo.

```text
preço_teórico = custo_unitário / (1 - margem_alvo)
```

O **Preço sugerido** é o preço teórico arredondado para cima para o incremento comercial configurado da Empresa. Ele representa a referência economicamente saudável calculada pelo sistema.

O **Preço de prateleira** é a decisão comercial efetivamente informada pelo usuário. Pode ser igual, superior ou inferior ao Preço sugerido. Valor inferior é permitido, mas deve ser evidenciado na apresentação como decisão abaixo da referência saudável.

### Desconto de referência

Enquanto não existir parametrização própria, a reserva comercial é fixa em **10 pontos percentuais**.

O Desconto de referência só é aplicável quando o Preço de prateleira estiver pelo menos **11% acima** do Preço sugerido.

Conceitualmente:

```text
percentual_acima_sugerido = (preço_prateleira / preço_sugerido) - 1

se percentual_acima_sugerido < 11%
    desconto_referencia = não aplicável
senão
    desconto_referencia = percentual_acima_sugerido - 10 p.p.
```

Se o Preço de prateleira for inferior ao Preço sugerido, não existe percentual de desconto de referência; a condição é apresentada como preço abaixo do sugerido.

O Desconto de referência é derivado e não deve ser persistido como dado redundante.

## Margem atual

```text
margem_atual = (preço_prateleira_atual - custo_unitário) / preço_prateleira_atual
```

A margem atual usa o Preço de prateleira vigente e o custo calculado com preços e configurações vigentes da mesma Empresa.

## Ausência de dados

Se um dado obrigatório estiver ausente ou inválido, o cálculo é **incompleto**. Valores faltantes não devem ser substituídos por zero.

## Histórico

### Insumos

O histórico registra compras/preços conhecidos do Insumo e é append-only.

Cada registro contém, no mínimo:

- EmpresaId;
- InsumoId;
- QuantidadeCompra na Unidade base do Insumo;
- PrecoCompra total da quantidade;
- DataReferencia.

O custo unitário não é persistido:

`CustoUnitario = PrecoCompra / QuantidadeCompra`.

Não há arredondamento intermediário para centavos.

Datas futuras podem ser registradas, mas não são vigentes antes da DataReferencia.

Para seleção do preço atual:

1. considerar somente `DataReferencia <= dataOperacionalEmpresa`;
2. ordenar por DataReferencia decrescente;
3. em empate, ordenar por Id decrescente;
4. selecionar o primeiro registro.

O histórico completo permanece ordenado por `DataReferencia DESC, Id DESC`, inclusive com futuros no topo quando aplicável. A apresentação deve distinguir **Vigente**, **Anterior** e **Futuro** sem persistir esses estados.

Se só existirem preços futuros, o custo atual continua desconhecido conforme RN007.

A situação Ativo/Inativo do Insumo não impede registro de preço. Registrar preço não reativa o Insumo e não muda sua elegibilidade operacional.

A interpretação histórica depende da estabilidade cadastral definida pela RN040: depois do primeiro preço, Nome, Marca e Unidade base do Insumo não podem ser alterados. Isso evita que um preço antigo passe a aparentar pertencer a outra identidade econômica ou que a quantidade de compra histórica mude de significado por troca de unidade.

No MVP, os registros de preço referenciam o Insumo por `InsumoId` e **não precisam duplicar snapshots de Nome, Marca e Unidade base**. Se no futuro houver requisito de auditoria que exija reproduzir exatamente a apresentação cadastral de cada momento, snapshots/versionamento deverão ser modelados explicitamente em vez de relaxar a RN040.

### Produtos

O histórico comercial do Produto é append-only e registra snapshots da decisão de precificação.

Cada novo registro deve conter, no mínimo:

- EmpresaId;
- ProdutoId;
- DataReferencia determinada pelo sistema pela data operacional da Empresa;
- CustoReferencia = custo unitário vigente calculado pelo sistema;
- MargemReferencia = MargemAlvo do Produto usada naquele cálculo;
- PrecoSugerido calculado pelo UC023 a partir dessas referências e do arredondamento comercial;
- PrecoPrateleira informado pelo usuário.

O usuário informa somente o Preço de prateleira. Os demais valores são derivados do estado da precificação no momento do registro e ficam congelados como snapshot histórico.

Não são permitidas datas futuras. Múltiplos registros na mesma DataReferencia são permitidos para correções sem edição/exclusão do histórico. O registro vigente é o de maior DataReferencia e, em empate, maior Id.

Produto inativo pode receber novo registro comercial; isso não o reativa.

O custo atual continua calculado sob demanda para a visão corrente. O CustoReferencia persistido no histórico existe para preservar a decisão tomada naquele momento e não substitui o cálculo atual.

## Golden cases

O motor de cálculo deve possuir cenários canônicos derivados de produtos conferidos nos negócios de referência. Os golden cases devem validar a composição completa do custo e respeitar o contexto da Empresa.
