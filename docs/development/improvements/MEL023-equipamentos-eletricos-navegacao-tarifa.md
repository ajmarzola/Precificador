# MEL023 — Refinar equipamentos elétricos e navegação de tarifa na Ficha Técnica

- **Origem:** segunda rodada de testes manuais.
- **Classificação:** UX / semântica de energia / navegação.
- **Estado:** Planejado.
- **Prioridade:** alta.
- **Dependência sugerida:** MEL022.
- **Alteração de regra de negócio:** não intencional; consolidar regra já implementada de zero equipamentos.
- **Alteração de schema:** não prevista.

## Problema observado

A Ficha apresenta a seção como **Equipamentos** e solicita potência elétrica obrigatória.

Isso induz o usuário a interpretar que todo instrumento usado no processo deve ser cadastrado.

Exemplo:

```text
descanso de panela em crochê
-> agulha de crochê
-> não possui potência elétrica
```

O modelo atual de `UsoEquipamentoFicha`, entretanto, foi criado especificamente para custo de energia.

A calculadora já possui a regra correta:

```text
zero usos
=> CustoEnergiaLote = 0
=> Completo = true
```

inclusive quando a tarifa não está configurada.

Portanto o problema principal é de semântica/UX, não de fórmula.

## Direção aprovada

Tratar a seção explicitamente como:

```text
Equipamentos elétricos (opcional)
```

ou redação equivalente.

Um instrumento sem consumo elétrico, como:

- agulha de crochê;
- tesoura manual;
- régua;
- espátula;

não precisa ser cadastrado como `UsoEquipamentoFicha`.

### Potência

Se o usuário decidir cadastrar um equipamento elétrico:

```text
PotenciaKw > 0
TempoUsoMinutos > 0
```

continuam obrigatórios.

Não criar registros com potência zero apenas para representar ferramenta manual.

### Sem equipamento elétrico

```text
nenhum equipamento elétrico cadastrado
=> Custo de energia = 0
=> componente completo
=> TarifaEnergiaKwh não é necessária
```

A UI deve tornar essa opcionalidade inequívoca.

## Tarifa de energia não configurada

Quando houver ao menos um equipamento elétrico e a tarifa estiver ausente:

```text
Tarifa de energia não configurada.
[Configurar tarifa de energia]
```

A ação deve levar à edição das Configurações de Precificação.

## Salvar antes de sair da Ficha

O atalho para Configurações não deve descartar alterações válidas ainda não salvas na base da Ficha.

Fluxo esperado:

1. usuário altera os campos da Ficha;
2. aciona `Configurar tarifa de energia`;
3. o POST da Ficha valida e salva o estado atual;
4. em caso válido, redireciona para Configurações;
5. em caso inválido, permanece na Ficha com os erros;
6. depois de salvar a configuração, deve ser possível retornar à Ficha de origem.

Após MEL022, a tendência é que a base editável da Ficha contenha apenas Rendimento; por isso esta melhoria deve ser implementada depois da remodelagem de mão de obra.

A navegação de Configurações pode adotar `returnUrl` local/seguro ou mecanismo equivalente.

Nunca aceitar redirect externo aberto.

## Relação com o pedido de valor/hora

O pedido equivalente para:

```text
Valor da hora de trabalho não configurado.
```

não será implementado porque a MEL022 substitui ValorHora por PercentualMaoDeObra com default de 10%.

Não criar UX transitória que será removida na melhoria imediatamente anterior.

## Impactos esperados

Revisar:

- Ficha Técnica;
- botão `Adicionar equipamento`;
- rótulos de seção;
- mensagens de energia;
- página de Configurações / retorno à origem;
- UC021;
- UC027 apenas quanto à navegação;
- testes Web de Ficha/energia.

## Fora do escopo

- cadastrar ferramentas manuais;
- custo de aquisição/depreciação de equipamento;
- catálogo global de equipamentos;
- potência zero em `UsoEquipamentoFicha`;
- água/gás;
- alterar fórmula de energia;
- tornar tarifa obrigatória para fichas sem equipamento elétrico.
