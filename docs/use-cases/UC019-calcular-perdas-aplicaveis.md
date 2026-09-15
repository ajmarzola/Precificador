# UC019 — Calcular perdas aplicáveis

- **Funcionalidades:** F003 — Ficha Técnica; F004 — Precificação
- **Dependência funcional:** UC018
- **Base de revalidação:** UC020 e UC021 concluídas
- **Schema:** sim
- **Persistência do custo:** não

## Objetivo

Permitir registrar perda esperada de material por Item da Ficha e calcular o custo adicional correspondente, sem voltar ao antigo percentual global específico de panificação.

A regra deve funcionar igualmente para matéria-prima, embalagem ou consumível quando houver desperdício esperado. A Categoria do Insumo não ativa perda automaticamente.

## Modelo

A perda pertence ao `ItemFichaTecnica`:

~~~text
PercentualPerda : decimal
~~~

Semântica interna:

~~~text
0,10 = 10%
0    = sem perda adicional
~~~

Regra:

~~~text
0 <= PercentualPerda < 1
~~~

O campo é obrigatório na persistência, com default zero. Não usar null para "não aplicável".

## Quantidade base x perda

`ItemFichaTecnica.Quantidade` continua sendo a quantidade base necessária para o lote, antes da perda adicional esperada.

`PercentualPerda` representa acréscimo esperado sobre essa base.

~~~text
Quantidade base = 100 g
PercentualPerda = 0,10
Quantidade adicional esperada = 10 g
~~~

O UC019 não persiste a quantidade adicional calculada.

## Perda de material x perda de rendimento

Não usar PercentualPerda para representar unidades finais defeituosas ou redução de saída vendável.

Quando um processo produz menos unidades vendáveis por lote, isso deve aparecer em:

~~~text
FichaTecnica.Rendimento
~~~

Exemplos:

- aparas de papel, sobra de vinil ou material descartado mantendo o mesmo número de unidades vendáveis => perda no Item;
- unidades defeituosas, pães perdidos ou produtos finais não vendáveis => reduzir o Rendimento esperado.

Isso evita dupla contagem:

- perda de material aumenta o custo dos materiais;
- menor Rendimento distribui todos os custos do lote por menos unidades no UC022.

## Fórmulas

O UC018 já calcula:

~~~text
CustoItemBase = Quantidade × CustoUnitarioAtual
~~~

O UC019 calcula:

~~~text
CustoPerdaItem = CustoItemBase × PercentualPerda
CustoPerdasLote = soma(CustoPerdaItem)
~~~

UC019 não altera `CustoBaseItens`.

## Null x zero

### Perda zero

~~~text
PercentualPerda = 0
=> CustoPerdaItem = 0
~~~

Isso vale mesmo se `CustoItemBase` estiver indisponível.

### Perda positiva sem custo base

~~~text
PercentualPerda > 0
E CustoItemBase = indisponível
=> CustoPerdaItem = indisponível
=> CustoPerdasLote = indisponível
=> Completo = false
~~~

Custos de perda conhecidos dos demais Itens podem ser exibidos individualmente, mas não como total parcial.

### Ficha sem Itens

~~~text
CustoPerdasLote = 0
Completo = true
~~~

Isso não torna a precificação completa: UC018 continua considerando a Ficha sem Itens como custo base indisponível.

### Todos os Itens com perda zero

Mesmo com UC018 incompleta por preço ausente:

~~~text
CustoPerdasLote = 0
Completo = true
~~~

## Precisão

Aplicar RN026.

Não arredondar PercentualPerda, CustoPerdaItem ou CustoPerdasLote.

Persistência:

~~~text
decimal(9,6)
~~~

Exemplo de round-trip:

~~~text
12,3456% <=> 0,123456
~~~

## Domínio

Evoluir `ItemFichaTecnica`.

Criação deve aceitar PercentualPerda com default zero.

Edição passa a alterar:

- Quantidade;
- Observacao;
- PercentualPerda.

Preservar Id, EmpresaId, FichaTecnicaId e InsumoId.

Validar todos os candidatos antes de mutar a entidade.

## Calculadora pura

Criar componente em `Precificador.Core.Precificacao`, preferencialmente `CalculadoraCustoPerdas`.

Entrada conceitual:

~~~text
ItemId
PercentualPerda
CustoItemBase : decimal?
~~~

Saída:

~~~text
ItemId
PercentualPerda
CustoPerdaItem : decimal?
~~~

Resultado:

~~~text
Itens
CustoPerdasLote : decimal?
Completo : bool
~~~

Completude:

~~~text
não existe Item com
PercentualPerda > 0
e CustoItemBase indisponível
~~~

Coleção vazia é completa com total zero.

A calculadora não acessa EF, Web, tenant, relógio ou formatação e rejeita percentuais <0 ou >=1.

## Migration

Adicionar a `ItensFichaTecnica`:

~~~text
PercentualPerda decimal(9,6) NOT NULL DEFAULT 0
~~~

Migration sugerida:

~~~text
AddPercentualPerdaItemFichaTecnica
~~~

A migration deve preservar Itens existentes, preenchê-los com zero e não alterar Quantidade/Observacao nem migrations históricas.

Não criar nova entidade ou DbSet.

## Entrada Web

Evoluir Novo e Editar Item com:

~~~text
Perda esperada (%)
~~~

A UI usa percentual; domínio/persistência usam fração.

Conversão:

~~~text
10      => 0,10
10,5    => 0,105
12.3456 => 0,123456
~~~

Parsing:

- vazio/whitespace => 0;
- vírgula => pt-BR;
- caso contrário => invariant;
- 0 <= percentual < 100;
- texto inválido => erro.

Centralizar parse/formatação em `ItemFichaTecnicaFormulario` ou helper equivalente.

GET Editar deve preservar:

~~~text
0,123456 <=> 12,3456
~~~

POST inválido preserva o texto informado e não muta parcialmente o Item.

## Página da Ficha

Na tabela existente, acrescentar:

- Perda esperada (%);
- Custo da perda.

Não substituir Custo unitário, Custo do item ou Custo base dos itens.

Perda zero:

~~~text
0%
Custo da perda: 0
~~~

Perda positiva com custo conhecido: mostrar percentual e custo.

Perda positiva sem custo base:

~~~text
Custo da perda: —
Custo de perdas do lote: indisponível
Há perda(s) sem custo base determinável.
~~~

Quando completo:

~~~text
Custo de perdas do lote: <valor>
~~~

## Reuso do UC018

UC019 não consulta preços novamente.

Usar o `CustoItem` já calculado pelo fluxo do UC018 para alimentar a calculadora de perdas.

Não introduzir nova seleção de preço, consulta por Item ou N+1.

## Independência

Perdas são independentes de mão de obra e energia. Não somar componentes nesta UC.

Produto inativo continua calculável. Item de Insumo inativo mantém perda, pode ser editado e não reativa o Insumo.

## Multiempresa

PercentualPerda faz parte de ItemFichaTecnica, que já é tenant-owned.

Preservar GQF, guard central e cadeia Produto/Ficha/Item/Insumo. Ownership não vem do request.

## Recalculo

Alterações em Quantidade, PercentualPerda, preço vigente ou data operacional refletem no próximo GET.

Nenhum custo de perda é persistido.

## Critérios essenciais

- perda opcional por Item, default zero;
- Categoria não ativa perda automaticamente;
- Quantidade é base antes da perda;
- 10% significa acréscimo de 10% sobre custo/quantidade base;
- perda de unidades vendáveis pertence ao Rendimento;
- PercentualPerda em [0,1);
- migration preserva Itens existentes com zero;
- perda zero custa zero mesmo sem preço;
- perda positiva sem custo base torna total indisponível;
- total parcial nunca é apresentado como total;
- nenhum custo de perda é persistido;
- UC018/RN006 são reutilizados sem nova consulta.

## Matriz de testes

### Domínio

- D1: novo Item defaulta PercentualPerda = 0;
- D2: criação aceita percentual válido;
- D3: percentual negativo ou >=1 é rejeitado;
- D4: edição válida altera Quantidade/Observacao/Perda preservando ownership;
- D5: edição inválida é atômica.

### Cálculo

- U1: 10% sobre CustoItemBase 100 => perda 10;
- U2: múltiplos Itens somam exatamente;
- U3: perda zero + custo base null => perda 0/completo;
- U4: perda positiva + custo base null => perda null/total incompleto;
- U5: custo conhecido permanece visível quando outro torna total indisponível;
- U6: coleção vazia => total 0/completo;
- U7: precisão sem arredondamento intermediário;
- U8: percentual inválido rejeitado.

### Persistência

- P1: migration sobre banco anterior preserva Itens existentes;
- P2: Itens existentes recebem PercentualPerda = 0;
- P3: PercentualPerda faz round-trip com 6 casas;
- P4: FKs/índices/constraints existentes de Item permanecem intactos.

### Web

- W1: Novo com perda vazia persiste zero;
- W2: Novo aceita percentual pt-BR/invariant e persiste fração correta;
- W3: Editar faz round-trip 0,123456 <=> 12,3456;
- W4: percentual inválido preserva entrada, retorna erro e não muta Item;
- W5: perda positiva + custo conhecido mostra percentual/custo;
- W6: múltiplas perdas mostram total correto;
- W7: perda zero + Item sem preço mostra custo da perda zero;
- W8: perda positiva + Item sem preço mostra — e total indisponível;
- W9: total parcial não é apresentado quando uma perda positiva está sem custo;
- W10: Ficha sem Itens mostra perdas 0, mantendo custo base indisponível;
- W11: Categoria do Insumo não ativa/bloqueia perda;
- W12: Produto/Insumo inativos preservam comportamento existente;
- W13: outro tenant não acessa/muta PercentualPerda;
- W14: POST inválido da base recalcula perdas pelo estado persistido;
- W15: GET não persiste custos nem muta Item;
- W16: UC020/UC021 permanecem independentes e visíveis.

## Fora do escopo

- percentual global no Produto;
- perda automática por Categoria;
- histórico/versionamento de Ficha;
- estoque ou apontamento de perda real;
- quantidade adicional persistida;
- custo total/unitário — UC022;
- preço sugerido — UC023;
- snapshots comerciais.

## Gate

Revalidação concluída após UC018, UC020 e UC021.

O estado real confirma que UC018 já fornece CustoItem, ItemFichaTecnica é o ponto natural para perda material específica, Rendimento já representa saída vendável e não existe modelo antigo de perda a migrar.

UC019 está liberada para implementação.

## Branch sugerida

~~~text
feat/uc019-perdas-itens
~~~
