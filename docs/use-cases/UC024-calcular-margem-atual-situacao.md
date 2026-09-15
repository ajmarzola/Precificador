# UC024 — Calcular margem atual e situação

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC012 e UC022
- **Base de revalidação:** UC012 e UC022 concluídas
- **Schema:** não
- **Persistência do resultado:** não
- **Regras principais:** RN017, RN019, RN022, RN023, RN026 e RN027

## Objetivo

Calcular, para um Produto da **Empresa Ativa**, a **Margem atual** e a **Situação frente à Margem-alvo** usando exclusivamente estado corrente:

~~~text
CustoUnitarioProduto atual    => UC022
PrecoPrateleiraAtual          => registro atual selecionado pela UC012
MargemAlvo atual              => Produto.MargemAlvo
~~~

A UC024 não reinterpreta o histórico comercial e não usa os snapshots `CustoReferencia` ou `MargemReferencia` para calcular a situação atual.

## Definição de "atual"

A UC024 combina três valores atuais, cada um com sua própria origem normativa.

### Custo unitário atual

Usar o `CustoUnitarioProduto` calculado sob demanda pela orquestração atual UC018–UC022.

Mudanças em preços vigentes de Insumos, Ficha Técnica, perdas, rendimento, mão de obra, energia e configurações relacionadas ao custo devem refletir no próximo cálculo.

### Preço de prateleira atual

Usar o `RegistroPrecoProduto` selecionado pela UC012:

~~~text
DataReferencia DESC
Id DESC
~~~

O valor relevante é:

~~~text
PrecoPrateleiraAtual = registroAtual.PrecoPrateleira
~~~

A DataReferencia do registro atual continua disponível para apresentação.

Não usar `PrecoPrateleira` informado em request e não persistir cópia do preço atual no Produto.

### Margem-alvo atual

Usar sempre:

~~~text
Produto.MargemAlvo
~~~

Não usar:

- `RegistroPrecoProduto.MargemReferencia`;
- `ConfiguracaoPrecificacaoEmpresa.MargemPadrao`.

`MargemReferencia` preserva a decisão histórica do UC011; `MargemAlvo` atual representa a meta estratégica vigente para avaliar o Produto hoje.

## Fórmula da Margem atual

Aplicar RN022:

~~~text
MargemAtual =
    (PrecoPrateleiraAtual - CustoUnitarioProduto)
    / PrecoPrateleiraAtual
~~~

Pré-condições para cálculo:

~~~text
CustoUnitarioProduto != null
PrecoPrateleiraAtual != null
~~~

`PrecoPrateleiraAtual`, quando existente, é sempre maior que zero pela invariante do UC011.

## Situação frente à margem

Aplicar RN023/RN027.

Estados mínimos e normativos:

~~~text
Incompleto
AbaixoDaMargem
DentroDaMargem
~~~

### Incompleto

~~~text
CustoUnitarioProduto == null
OU
PrecoPrateleiraAtual == null
~~~

Resultado:

~~~text
MargemAtual = null
Situacao = Incompleto
~~~

### AbaixoDaMargem

Com cálculo completo:

~~~text
MargemAtual < MargemAlvo
=> Situacao = AbaixoDaMargem
~~~

### DentroDaMargem

Com cálculo completo:

~~~text
MargemAtual >= MargemAlvo
=> Situacao = DentroDaMargem
~~~

A igualdade exata pertence a `DentroDaMargem`.

Não criar estado separado "Na margem" neste UC.

## Null x zero

### Custo zero

Custo atual conhecido igual a zero é válido:

~~~text
CustoUnitarioProduto = 0
PrecoPrateleiraAtual > 0

=> MargemAtual = 1
=> 100%
~~~

Como `MargemAlvo < 1`, a situação é `DentroDaMargem`.

### Preço ausente

Ausência de Preço de prateleira não equivale a zero:

~~~text
PrecoPrateleiraAtual = null
=> MargemAtual = null
=> Incompleto
~~~

### Margem negativa

É válida como resultado matemático quando custo atual supera o preço atual:

~~~text
CustoUnitarioProduto > PrecoPrateleiraAtual
=> MargemAtual < 0
=> AbaixoDaMargem
~~~

Não truncar a margem em zero.

## Independência de Preço sugerido e Incremento comercial

A Margem atual não depende de:

- `PrecoTeorico`;
- `PrecoSugerido`;
- `IncrementoComercial`;
- `ReservaComercialDesconto`;
- `DescontoReferencia`.

Consequência obrigatória:

~~~text
Custo atual conhecido
+ Preço de prateleira atual conhecido
+ MargemAlvo válida
+ IncrementoComercial = null

=> MargemAtual calculável
=> Situação AbaixoDaMargem ou DentroDaMargem
~~~

Não usar `PrecoProdutoCompleto` da UC023 como critério para a completude da Margem atual.

## Calculadora pura

Criar componente em `Precificador.Core.Precificacao`, preferencialmente:

~~~text
CalculadoraMargemAtual
~~~

Entrada conceitual:

~~~text
decimal? custoUnitarioProduto
decimal? precoPrateleiraAtual
decimal margemAlvo
~~~

Saída conceitual:

~~~text
decimal? MargemAtual
SituacaoMargemProduto Situacao
~~~

Enum sugerido:

~~~text
SituacaoMargemProduto
- Incompleto
- AbaixoDaMargem
- DentroDaMargem
~~~

Nomes equivalentes claros são aceitáveis.

### Validações defensivas

~~~text
0 <= MargemAlvo < 1
custo conhecido >= 0
preco conhecido > 0
~~~

Se custo ou preço estiverem `null`, não lançar; retornar `Incompleto`.

Se valores conhecidos violarem suas invariantes, lançar `ArgumentOutOfRangeException`.

A calculadora:

- não acessa EF;
- não acessa HTTP;
- não acessa tenant;
- não consulta histórico;
- não consulta Ficha/configuração;
- não formata;
- não persiste.

## Precisão

Aplicar RN026.

Não arredondar antes de:

- subtrair custo do preço;
- dividir pelo Preço de prateleira;
- comparar `MargemAtual` com `MargemAlvo`.

Exemplo de fronteira:

~~~text
MargemAlvo = 30%

MargemAtual real = 29,999999%
=> AbaixoDaMargem

MargemAtual real = 30%
=> DentroDaMargem
~~~

Arredondamento para exibição não pode alterar a classificação.

## Orquestração atual

Evoluir `PrecificacaoProdutoAtual` para compor UC024 com os resultados já existentes.

Ela deve passar a retornar, no mesmo resultado corrente:

~~~text
CustoUnitarioProduto
MargemAlvo
PrecoPrateleiraAtual
DataReferenciaPrecoAtual
MargemAtual
SituacaoMargem
~~~

Além dos campos UC018–UC023 já existentes.

### Seleção do preço

Usar obrigatoriamente a consulta reutilizável criada pela UC012:

~~~text
RegistroPrecoProdutoConsultas.SelecionarAtualAsync(...)
~~~

Não duplicar a ordenação `DataReferencia DESC, Id DESC`.

### Preço deve sobreviver à incompletude do custo

O registro comercial atual deve ser selecionado independentemente da existência/completude da Ficha.

Exemplos:

~~~text
Produto sem Ficha + histórico existente
=> PrecoPrateleiraAtual conhecido
=> CustoUnitarioProduto null
=> MargemAtual null
=> Incompleto
~~~

~~~text
Configuração inesperadamente ausente + histórico existente
=> PrecoPrateleiraAtual conhecido
=> MargemAtual incompleta
=> preço continua visível
~~~

Portanto, os retornos antecipados atuais da orquestração não podem apagar o preço comercial já existente.

## Separação de completudes

A aplicação passa a ter pelo menos duas noções distintas:

~~~text
PrecoProdutoCompleto   => UC023
SituacaoMargem         => UC024
~~~

`SituacaoMargem` descreve exclusivamente a capacidade de avaliar o Produto **frente à MargemAlvo atual**. Ela não é sinônimo do indicador global de precificação completa e não substitui `PrecoProdutoCompleto`.

O futuro UC030 poderá combinar sinais de incompletude de outras dimensões; não deve inferir a completude global apenas de `SituacaoMargem`.

É permitido:

~~~text
PrecoProdutoCompleto = false
SituacaoMargem = DentroDaMargem
~~~

por exemplo quando `IncrementoComercial = null`, mas custo atual e Preço de prateleira atual estão conhecidos.

Não inferir situação da margem a partir da lista geral de impedimentos da precificação.

## Impedimentos da Margem atual

Quando `SituacaoMargem = Incompleto`, a apresentação deve explicar o motivo.

Motivos mínimos:

- **Preço de prateleira não definido.**
- impedimentos que tornem o **custo unitário atual** indisponível.

Não tratar `IncrementoComercial não configurado` como impedimento da Margem atual.

É aceitável evoluir o resultado compartilhado para expor uma coleção específica, por exemplo:

~~~text
ImpedimentosMargemAtual
~~~

ou estruturar os impedimentos por componente.

Evitar lógica de UI baseada em texto para decidir completude.

## Integração Web

Não criar nova página neste UC.

Evoluir:

~~~text
/Produtos/Detalhes/{id:int}
~~~

para apresentar um resumo da situação atual.

### Resumo mínimo

Exibir:

- Custo unitário atual;
- Preço de prateleira atual;
- Data de referência do Preço atual, se existir;
- Margem-alvo atual;
- Margem atual;
- Situação.

Rótulos de situação:

~~~text
Incompleto
Abaixo da margem
Dentro da margem
~~~

### Estado completo

Exemplo:

~~~text
Custo unitário atual: 10,00
Preço de prateleira atual: 15,00
Margem-alvo: 30%
Margem atual: 33,33%
Situação: Dentro da margem
~~~

### Sem preço atual

~~~text
Preço de prateleira atual: não definido.
Margem atual: indisponível
Situação: Incompleto
Preço de prateleira não definido.
~~~

### Custo incompleto

Manter Preço de prateleira e DataReferencia visíveis.

~~~text
Custo unitário atual: indisponível
Margem atual: indisponível
Situação: Incompleto
<impedimentos de custo aplicáveis>
~~~

Não esconder o histórico comercial porque a produção atual está incompleta.

## Reuso em Detalhes

`DetalhesModel` não deve continuar carregando o registro atual de preço em uma consulta independente e, em paralelo, executar outra orquestração para margem.

Após UC024:

- usar uma única execução da orquestração corrente para preço/custo/margem/situação;
- a seleção de preço continua sendo a regra compartilhada da UC012;
- evitar fotografias divergentes entre Preço de prateleira exibido e Preço usado no cálculo da Margem atual.

Consultas cadastrais do Produto podem permanecer separadas para apresentação.

## Ficha Técnica

A página de Ficha Técnica já consome `PrecificacaoProdutoAtual`.

Ela deve continuar funcionando sem regressão após a ampliação do resultado.

UC024 não exige que Margem atual/Situação sejam exibidas na Ficha; a superfície mínima deste UC é Detalhes do Produto.

A apresentação detalhada da precificação corrente pertence ao UC025.

## Produto inativo

Produto inativo continua calculável.

Ativo/Inativo não altera a fórmula da margem nem a seleção do preço atual.

Detalhes de Produto inativo deve apresentar a mesma situação corrente quando os dados forem suficientes.

UC024 não reativa Produto.

## Mudanças correntes e recalculo

Margem atual e situação são derivadas em consulta e devem reagir imediatamente, sem novo snapshot comercial, a mudanças em:

- preço vigente de Insumo;
- Ficha Técnica;
- perdas;
- rendimento;
- valor/hora;
- energia;
- qualquer outra dependência do custo UC022;
- `Produto.MargemAlvo`.

Novo `RegistroPrecoProduto` altera o Preço de prateleira atual e portanto recalcula Margem atual.

### Alteração de MargemAlvo

Se a meta muda depois de um registro comercial:

- `MargemReferencia` histórica não muda;
- Preço de prateleira atual não muda;
- MargemAtual matemática não muda se custo/preço não mudarem;
- **Situação** pode mudar porque a comparação usa a `MargemAlvo` atual.

### Alteração do custo

Se o custo atual muda:

- snapshots históricos não mudam;
- MargemAtual muda no próximo GET;
- Situação pode mudar.

## Multiempresa

Preservar FT002:

- Produto e Ficha por GQF;
- preços de Insumo por GQF;
- configuração por GQF;
- `RegistroPrecoProduto` atual por GQF;
- sem `IgnoreQueryFilters` no fluxo comum;
- Produto cross-tenant => 404;
- dados da Empresa B nunca entram na Margem atual da Empresa A.

A calculadora pura não recebe `EmpresaId`.

## Persistência

Nenhuma alteração de schema.

Não persistir:

- MargemAtual;
- SituacaoMargem;
- PrecoPrateleiraAtual;
- CustoUnitarioProduto;
- qualquer cache da análise atual.

Não criar migration e não alterar `PrecificadorDbContextModelSnapshot`.

## Critérios de aceitação

- **CA01:** MargemAtual segue RN022.
- **CA02:** PrecoPrateleiraAtual vem da seleção UC012 por DataReferencia/Id.
- **CA03:** Custo atual vem de UC022, não de CustoReferencia histórico.
- **CA04:** comparação usa Produto.MargemAlvo atual, não MargemReferencia.
- **CA05:** margem abaixo da meta resulta AbaixoDaMargem.
- **CA06:** margem igual à meta resulta DentroDaMargem.
- **CA07:** margem acima da meta resulta DentroDaMargem.
- **CA08:** custo zero produz MargemAtual = 100%.
- **CA09:** custo maior que preço admite margem negativa.
- **CA10:** custo indisponível resulta Incompleto.
- **CA11:** preço atual ausente resulta Incompleto.
- **CA12:** preço ausente nunca vira zero.
- **CA13:** incremento comercial ausente não impede MargemAtual se custo e preço estiverem disponíveis.
- **CA14:** PrecoProdutoCompleto não controla SituacaoMargem.
- **CA15:** nenhum arredondamento intermediário altera a situação.
- **CA16:** alteração de custo recalcula margem/situação sem alterar histórico.
- **CA17:** alteração de MargemAlvo pode alterar situação sem novo preço.
- **CA18:** novo registro comercial altera o preço atual usado na margem.
- **CA19:** empate de DataReferencia usa maior Id conforme UC012.
- **CA20:** preço atual permanece visível quando custo está incompleto.
- **CA21:** Produto sem Ficha + preço atual => Incompleto, preservando preço.
- **CA22:** Produto inativo é calculável.
- **CA23:** Detalhes mostra custo/preço/meta/margem/situação.
- **CA24:** Detalhes usa uma única composição corrente para preço e margem.
- **CA25:** cross-tenant retorna 404 e não mistura dados.
- **CA26:** GET não persiste MargemAtual/Situacao.
- **CA27:** nenhuma migration/schema é alterado.
- **CA28:** Ficha Técnica mantém comportamento UC018–UC023.
- **CA29:** UC025 não é antecipado com detalhamento completo nesta tela.

## Matriz de testes

### Unitários — CalculadoraMargemAtual

- U1: margem acima da meta => DentroDaMargem.
- U2: margem abaixo da meta => AbaixoDaMargem.
- U3: margem exatamente igual à meta => DentroDaMargem.
- U4: custo zero => margem 1 / DentroDaMargem.
- U5: custo maior que preço => margem negativa / AbaixoDaMargem.
- U6: custo null => Incompleto.
- U7: preço null => Incompleto.
- U8: custo e preço null => Incompleto.
- U9: MargemAlvo zero é válida.
- U10: fronteira 29,999999% vs meta 30% continua abaixo sem arredondamento.
- U11: MargemAlvo negativa é rejeitada.
- U12: MargemAlvo >= 1 é rejeitada.
- U13: custo conhecido negativo é rejeitado.
- U14: preço conhecido <= 0 é rejeitado.

### Integração — orquestração

- P1: orquestração usa registro atual selecionado pela UC012.
- P2: empate de DataReferencia usa maior Id.
- P3: preço atual é retornado mesmo quando Produto não possui Ficha.
- P4: custo atual, e não CustoReferencia, alimenta a calculadora.
- P5: MargemAlvo atual, e não MargemReferencia, alimenta a situação.
- P6: IncrementoComercial null não torna margem incompleta se custo/preço forem conhecidos.
- P7: GQF impede usar registro comercial de outra Empresa.
- P8: execução não persiste resultado derivado.

### Integração — Web

- W1: Detalhes completo mostra CustoUnitario, PrecoPrateleiraAtual, DataReferencia, MargemAlvo, MargemAtual e DentroDaMargem.
- W2: margem abaixo da meta mostra Abaixo da margem.
- W3: margem exatamente igual à meta mostra Dentro da margem.
- W4: sem registro comercial mostra preço não definido, Margem atual indisponível e Incompleto.
- W5: com preço atual e custo incompleto mantém preço/data visíveis e mostra Incompleto.
- W6: Produto sem Ficha + preço atual preserva preço e mostra motivo de custo incompleto.
- W7: IncrementoComercial null + custo/preço completos continua calculando margem/situação.
- W8: CustoReferencia histórico diferente do custo atual não contamina MargemAtual.
- W9: MargemReferencia histórica diferente da MargemAlvo atual não contamina Situação.
- W10: alteração de preço vigente de Insumo recalcula MargemAtual no próximo GET.
- W11: alteração de MargemAlvo recalcula Situação sem novo RegistroPrecoProduto.
- W12: novo preço de prateleira recalcula MargemAtual.
- W13: dois registros na mesma data usam maior Id.
- W14: custo zero produz 100% sem virar indisponível.
- W15: custo maior que preço apresenta margem negativa.
- W16: Produto inativo continua exibindo cálculo.
- W17: Produto cross-tenant retorna 404.
- W18: configuração/preço de outra Empresa não influencia cálculo.
- W19: GET não persiste resultado nem cria configuração/Ficha.
- W20: ausência inesperada da configuração preserva Preço atual e resulta Incompleto quando custo não puder ser determinado.
- W21: Ficha Técnica mantém custos, PrecoTeorico e PrecoSugerido existentes após expansão da orquestração.
- W22: Detalhes não apresenta composição detalhada de itens/perdas/mão de obra/energia reservada ao UC025.

## Alterações esperadas

### Core

Adicionar:

- calculadora pura de Margem atual;
- enum/resultado de Situação.

### Web / orquestração

Evoluir `PrecificacaoProdutoAtual` para:

- buscar registro comercial atual via helper UC012;
- preservar preço/data mesmo com custo incompleto;
- calcular MargemAtual/Situacao separadamente de UC023;
- expor impedimentos específicos da Margem atual.

Evoluir `Produtos/Detalhes` para consumir esse resultado compartilhado.

### Infrastructure

Reutilizar `RegistroPrecoProdutoConsultas.SelecionarAtualAsync`.

Nenhuma nova abstração de persistência é necessária.

### Banco

Nenhuma alteração.

## Documentação

Atualizar:

- catálogo;
- backlog;
- F002;
- F004;
- modelo de precificação;
- RN022/RN023/RN027, se necessário para explicitar origem corrente e completude.

## Fora do escopo

- histórico completo — UC012;
- DescontoReferencia histórico;
- registrar/editar Preço de prateleira;
- detalhamento completo da precificação atual — UC025;
- dashboard/resumo da Empresa — UC028;
- filtro abaixo da margem — UC029;
- lista de Produtos incompletos — UC030;
- persistir MargemAtual/Situação;
- impostos, comissão, frete ou novos componentes de custo;
- preço promocional;
- alteração de MargemAlvo;
- nova página de precificação.

## Gate

UC012 está concluída e fornece seleção reutilizável do Preço de prateleira atual.

UC022 está concluída e fornece CustoUnitarioProduto atual.

Produto já possui MargemAlvo válida.

Não existe alteração estrutural pendente.

**UC024 está liberada para implementação após o merge desta documentação.**

## Branch sugerida

~~~text
feat/uc024-margem-atual-situacao
~~~

## Commit sugerido

~~~text
feat: calcula margem atual e situacao
~~~
