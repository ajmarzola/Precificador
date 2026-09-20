# UC025 — Consultar detalhamento da precificação atual

> **Nota MEL022:** referências históricas a Tempo ativo/Valor da hora neste documento foram substituídas no modelo ativo por Percentual de mão de obra e Base da mão de obra = `CustoBaseItens`.

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC023 e UC024
- **Base de revalidação:** UC017 a UC024 concluídas
- **Schema:** não
- **Persistência do resultado:** não
- **Natureza:** consulta analítica somente leitura

## Objetivo

Permitir consultar, em uma única tela, **como a precificação atual do Produto foi formada**, reunindo a composição econômica já calculada pelos UCs anteriores:

- custo dos Itens;
- perdas;
- mão de obra;
- energia/equipamentos;
- custo do lote;
- rendimento;
- custo unitário;
- Margem-alvo;
- Preço teórico;
- Preço sugerido;
- Preço de prateleira atual;
- Margem atual;
- Situação frente à margem;
- impedimentos da precificação, quando houver.

UC025 **não cria nova fórmula**. A página é uma projeção explicativa do estado corrente produzido por `PrecificacaoProdutoAtual`.

## Princípios

1. a tela é somente leitura;
2. todos os valores calculados vêm da mesma fotografia corrente;
3. fórmulas permanecem exclusivamente nas calculadoras Core existentes;
4. `PrecificacaoProdutoAtual` é a fonte numérica compartilhada;
5. a página não recalcula UC018–UC024 por conta própria;
6. valores conhecidos permanecem visíveis mesmo quando outros componentes estiverem incompletos;
7. total/custo unitário/preços dependentes continuam `indisponível` quando suas entradas forem incompletas;
8. não usar snapshots históricos para explicar o custo atual;
9. histórico completo continua pertencendo à UC012;
10. edição da Ficha continua pertencendo à rota de Ficha Técnica;
11. nenhuma informação derivada é persistida;
12. Produto ativo ou inativo pode ser consultado.

## Rota

Criar Razor Page somente leitura:

~~~text
/Produtos/Precificacao/{id:int}
~~~

O `id` identifica o Produto da Empresa Ativa.

Não criar formulário POST nesta página.

## Fonte única dos valores calculados

Executar exatamente uma vez:

~~~text
PrecificacaoProdutoAtual.CalcularAsync(produtoId)
~~~

A página deve consumir desse resultado:

~~~text
CustoBaseItens
CustoPerdasLote
CustoMaoDeObraLote
CustoEnergiaLote
CustoLote
CustoUnitarioProduto
PrecoTeorico
PrecoSugerido
MargemAlvo
PrecoPrateleiraAtual
DataReferenciaPrecoAtual
MargemAtual
SituacaoMargem
CustoProdutoCompleto
PrecoProdutoCompleto
Impedimentos
ImpedimentosMargemAtual
Itens
Usos
~~~

Não executar novamente:

- CalculadoraCustoItens;
- CalculadoraCustoPerdas;
- CalculadoraCustoMaoDeObra;
- CalculadoraCustoEnergia;
- CalculadoraCustoProduto;
- CalculadoraPrecoProduto;
- CalculadoraMargemAtual.

Não consultar novamente os preços vigentes para refazer os cálculos.

## Entradas explicativas da fotografia corrente

Para que a tela explique os resultados sem realizar consultas paralelas de configuração, evoluir `ResultadoPrecificacaoProdutoAtual` para expor as entradas correntes já utilizadas pela própria orquestração:

~~~text
DataOperacional
Rendimento
PercentualMaoDeObra
TarifaEnergiaKwh
IncrementoComercial
~~~

É aceitável usar nomes equivalentes claros.

### Semântica

- `DataOperacional`: data usada para selecionar preços vigentes dos Insumos;
- `Rendimento`: rendimento atual da Ficha;
- `PercentualMaoDeObra`: configuração corrente usada por UC020;
- `TarifaEnergiaKwh`: configuração corrente usada por UC021;
- `IncrementoComercial`: configuração corrente usada por UC023.

`MargemAlvo` já existe no resultado.

`ReservaComercialDesconto` não precisa ser exibida na visão de precificação atual porque não participa de UC018–UC024. O histórico de desconto pertence à UC012.

### Estado sem Ficha/configuração

Os retornos incompletos da orquestração devem preencher o que for conhecido:

- Produto sempre mantém `MargemAlvo`;
- Preço de prateleira/DataReferencia permanecem disponíveis conforme UC024;
- `DataOperacional` pode permanecer conhecida;
- dados inexistentes de Ficha/configuração ficam `null`.

Não inventar zero para campo ausente.

## Metadados estruturais da Ficha

A página precisa associar os resultados numéricos de `Itens` e `Usos` aos seus rótulos estruturais.

É permitido realizar consultas **somente de metadados**, sem refazer cálculo:

### Ficha

- existência da Ficha;
- Id.

Os valores de Rendimento/Tempo usados na explicação devem preferencialmente vir do resultado da orquestração, garantindo a mesma fotografia do cálculo.

### Itens

Consultar:

- ItemId;
- InsumoId;
- Nome;
- Marca;
- Quantidade;
- Unidade base;
- PercentualPerda;
- Situação Ativo/Inativo do Insumo.

Associar pelo `ItemId` ao resultado:

~~~text
ItemPrecificacaoAtual
- CustoUnitario
- CustoItem
- CustoPerdaItem
~~~

Não recalcular esses valores.

### Equipamentos

Consultar:

- UsoId;
- NomeEquipamento;
- PotenciaKw;
- TempoUsoMinutos.

Associar pelo `UsoId` ao resultado:

~~~text
UsoPrecificacaoAtual
- ConsumoKwh
- CustoEnergiaUso
~~~

Não recalcular consumo/custo.

## Estrutura da página

### 1. Identificação do Produto

Exibir:

- Nome;
- Categoria;
- Situação Ativo/Inativo.

### 2. Estado da precificação

Exibir em destaque:

- Situação da margem;
- Margem-alvo;
- Margem atual;
- Preço de prateleira atual;
- Data de referência do preço atual;
- Preço sugerido.

Rótulos:

~~~text
Incompleto
Abaixo da margem
Dentro da margem
~~~

A ausência de Preço de prateleira continua:

~~~text
Preço de prateleira atual: não definido.
~~~

### 3. Parâmetros atuais usados

Exibir:

- Data operacional;
- Rendimento;
- Percentual de mão de obra;
- Tarifa de energia;
- Incremento comercial;
- Margem-alvo.

Campos opcionais ausentes:

~~~text
Não configurado
~~~

Campos de Ficha inexistentes:

~~~text
Não disponível
~~~

Zero configurado deve continuar sendo exibido como zero, nunca como `Não configurado`.

### 4. Itens e perdas

Quando existir Ficha, exibir tabela com:

1. Insumo;
2. Marca;
3. Situação;
4. Quantidade;
5. Unidade;
6. Custo unitário atual do Insumo;
7. Custo base do Item;
8. Perda esperada;
9. Custo da perda.

Ordenação:

~~~text
NomeNormalizado
MarcaNormalizada
~~~

Sem Itens:

~~~text
Nenhum insumo adicionado.
~~~

Item sem preço vigente:

- Custo unitário: **Sem preço vigente**;
- Custo do Item: **indisponível**;
- Custo da perda conforme semântica UC019.

Não exibir ações Editar/Remover nesta página.

Após tabela, exibir:

- Custo base dos Itens;
- Custo de perdas do lote.

### 5. Mão de obra

Exibir:

- Base da mão de obra = Custo base dos Itens;
- Percentual de mão de obra;
- Custo de mão de obra do lote.

Se `CustoBaseItens` estiver indisponível:

~~~text
Custo de mão de obra do lote: indisponível
~~~

Se `CustoBaseItens` for conhecido e `PercentualMaoDeObra = 0`:

~~~text
Custo de mão de obra = 0
~~~

Percentuais maiores que 100% são válidos conforme UC020/RN013.

### 6. Energia/equipamentos

Exibir tabela:

1. Equipamento;
2. Potência (kW);
3. Tempo de uso (min);
4. Consumo (kWh);
5. Custo de energia.

Ordenar por `NomeEquipamentoNormalizado`.

Sem usos:

~~~text
Nenhum equipamento adicionado.
Custo de energia do lote: 0
~~~

quando aplicável pela semântica UC021.

Exibir também:

- Tarifa de energia atual;
- Custo de energia do lote.

Não exibir ações Editar/Remover.

### 7. Consolidação do custo

Exibir:

- Custo base dos Itens;
- Custo de perdas;
- Custo de mão de obra;
- Custo de energia;
- Custo total do lote;
- Rendimento;
- Custo unitário do Produto.

Se algum componente obrigatório estiver indisponível:

- componentes conhecidos continuam visíveis;
- `CustoLote` e `CustoUnitarioProduto` ficam **indisponível**;
- não apresentar soma parcial como total.

### 8. Formação do preço

Exibir:

- Custo unitário;
- Margem-alvo;
- Preço teórico;
- Incremento comercial;
- Preço sugerido.

Se custo estiver incompleto:

~~~text
Preço teórico: indisponível
Preço sugerido: indisponível
~~~

Se custo estiver conhecido e IncrementoComercial = null:

~~~text
Preço teórico: <valor>
Incremento comercial: Não configurado
Preço sugerido: indisponível
~~~

Não inventar incremento.

### 9. Situação comercial atual

Exibir:

- Preço de prateleira atual;
- Data de referência;
- Margem atual;
- Situação.

A Margem atual continua usando o custo atual e o preço atual conforme UC024.

Não exibir `CustoReferencia` ou `MargemReferencia` do snapshot nessa seção.

Para consultar a decisão histórica, fornecer link para UC012.

## Impedimentos

A página deve apresentar um bloco:

~~~text
Pendências da precificação
~~~

Quando `Impedimentos.Count > 0`, listar todos os impedimentos gerais já fornecidos pela orquestração, por exemplo:

- A ficha técnica não foi cadastrada.
- A ficha não possui itens.
- Há item(ns) sem preço vigente.
- Tarifa de energia não configurada.
- Incremento comercial não configurado.

### Preço de prateleira ausente

`Preço de prateleira não definido.` pertence aos impedimentos de Margem atual, mas não torna UC023/custo incompletos.

Na tela detalhada:

- listar a ausência de preço em um bloco de situação/margem;
- não misturá-la como causa de `PrecoProdutoCompleto = false`.

### Sem pendências gerais

Se `Impedimentos.Count == 0`:

~~~text
Nenhuma pendência de cálculo.
~~~

Mesmo nesse caso, a Margem pode estar `Incompleto` por ausência de Preço de prateleira.

## Produto sem Ficha

A página continua acessível.

Exibir:

- Produto;
- Preço de prateleira atual/Data, se existirem;
- Margem-alvo;
- Situação `Incompleto`;
- Ficha Técnica: não cadastrada;
- custos/preços dependentes como indisponíveis;
- impedimento específico.

Não criar Ficha automaticamente.

Oferecer link:

~~~text
Cadastrar/editar Ficha técnica
~~~

## Configuração ausente

A página continua acessível.

Preservar dados conhecidos de Produto e Preço de prateleira.

Exibir:

~~~text
As configurações de precificação não foram encontradas.
~~~

Não criar configuração no GET.

## Produto inativo

Produto inativo possui a mesma consulta analítica.

A situação cadastral não altera os cálculos.

Não reativar Produto.

## Navegação

### Em Detalhes do Produto

Adicionar:

~~~text
Detalhar precificação
~~~

para Produto ativo e inativo.

Destino:

~~~text
/Produtos/Precificacao/{id}
~~~

### Na página de precificação

Exibir links:

- **Voltar ao produto**;
- **Editar Ficha técnica**;
- **Registrar preço de prateleira**;
- **Histórico de precificação**.

`Registrar preço de prateleira` continua sujeito às regras do UC011.

## Relação com a Ficha Técnica

A Ficha Técnica continua sendo a superfície de **edição** da composição.

UC025 não deve remover funcionalidades atuais da Ficha.

A nova página pode reutilizar componentes/formatadores de apresentação, mas não deve:

- embutir formulário de edição;
- duplicar handlers POST;
- criar ações de Item/equipamento;
- substituir a rota canônica da Ficha.

Após UC025:

~~~text
Ficha Técnica
=> manter/editar composição

Detalhamento da precificação
=> explicar economicamente a composição corrente
~~~

## Relação com Detalhes do Produto

O resumo introduzido pelo UC024 permanece em Detalhes.

UC025 não deve remover:

- Custo unitário atual;
- Preço de prateleira atual;
- Margem-alvo;
- Margem atual;
- Situação.

Detalhes recebe somente o novo link para a análise completa.

## Relação com histórico

UC025 é estado **corrente**.

Não listar registros históricos.

Não derivar `DescontoReferencia` nessa página.

Não exibir snapshots `CustoReferencia`, `MargemReferencia`, `ReservaComercialReferencia` como se fossem estado atual.

Linkar para:

~~~text
/Produtos/Precos/Historico/{id}
~~~

quando o usuário quiser histórico.

## Recalculo

Como a página usa `PrecificacaoProdutoAtual`, o próximo GET deve refletir alterações válidas em:

- preço vigente de Insumo;
- Quantidade;
- PercentualPerda;
- Rendimento;
- PercentualMaoDeObra;
- equipamentos;
- PotenciaKw;
- TempoUsoMinutos;
- TarifaEnergiaKwh;
- MargemAlvo;
- IncrementoComercial;
- novo Preço de prateleira;
- Data operacional quando afetar preço vigente.

Nenhum resultado é cacheado/persistido.

## Consistência de fotografia

Na mesma requisição, valores estruturais e resultados calculados não podem representar versões logicamente divergentes.

Requisitos:

1. cálculos vêm de uma única execução de `PrecificacaoProdutoAtual`;
2. metadados de Itens/equipamentos são usados apenas para rotulagem;
3. associações numéricas são feitas por Id;
4. não realizar segunda execução da orquestração;
5. não recalcular com valores de formulário;
6. não consultar outra Empresa.

Não é necessário introduzir transação snapshot/database isolation apenas para a tela. O objetivo é eliminar duplicação lógica de cálculo.

## Multiempresa

Preservar FT002:

- Produto por GQF;
- Ficha por GQF;
- Itens/Insumos por GQF;
- usos de equipamentos por GQF;
- preços/configuração/registro atual já isolados pela orquestração;
- sem `IgnoreQueryFilters` no fluxo comum;
- cross-tenant => HTTP 404;
- dados da Empresa B não aparecem em nenhuma seção.

Nenhum `EmpresaId` vem do request.

## Precisão e apresentação

Aplicar RN026.

UC025 não arredonda valores antes de cálculo; recebe resultados já calculados.

Formatação é apenas visual.

### Valores monetários

Reutilizar formatadores existentes de custo/preço.

### Percentuais

Reutilizar formatadores existentes para:

- PercentualPerda;
- MargemAlvo;
- MargemAtual.

Margem negativa deve permanecer negativa na apresentação.

### Valores técnicos

Preservar precisão útil já adotada para:

- quantidade;
- potência;
- consumo;
- rendimento.

Não converter `null` para zero.

## Persistência

Nenhuma alteração de schema.

Não criar migration.

Não alterar `PrecificadorDbContextModelSnapshot`.

Não persistir:

- resultados de custos;
- PrecoTeorico;
- PrecoSugerido;
- PrecoPrateleiraAtual;
- MargemAtual;
- SituacaoMargem;
- inputs correntes da configuração em Produto/Ficha.

## Critérios de aceitação

- **CA01:** rota exige autenticação e Empresa Ativa.
- **CA02:** Produto inexistente/cross-tenant retorna 404.
- **CA03:** Produto ativo e inativo são consultáveis.
- **CA04:** página executa `PrecificacaoProdutoAtual` uma única vez.
- **CA05:** nenhuma fórmula UC018–UC024 é duplicada na PageModel.
- **CA06:** identificação do Produto é exibida.
- **CA07:** Data operacional e parâmetros atuais usados são exibidos.
- **CA08:** Item associa metadados estruturais aos resultados numéricos pelo ItemId.
- **CA09:** Item sem preço vigente é evidenciado sem virar custo zero.
- **CA10:** CustoBaseItens e CustoPerdasLote seguem os resultados da orquestração.
- **CA11:** mão de obra exibe base, percentual e custo.
- **CA12:** equipamento associa metadados aos resultados por UsoId.
- **CA13:** energia exibe tarifa, consumos e custos conhecidos.
- **CA14:** consolidação não apresenta soma parcial como CustoLote.
- **CA15:** Preço teórico/sugerido reproduzem UC023.
- **CA16:** IncrementoComercial null mantém PreçoTeorico conhecido e sugerido indisponível.
- **CA17:** Preço de prateleira/Margem/Situação reproduzem UC024.
- **CA18:** ausência de Preço de prateleira não invalida custo/Preço teórico/sugerido.
- **CA19:** Produto sem Ficha continua acessível e incompleto.
- **CA20:** configuração ausente não é criada e preserva dados conhecidos.
- **CA21:** impedimentos gerais são exibidos sem inferência textual de estado.
- **CA22:** impedimentos de margem permanecem semanticamente separados.
- **CA23:** histórico não é listado nem reinterpretado.
- **CA24:** snapshots históricos não entram nos cálculos correntes.
- **CA25:** Detalhes mantém resumo UC024 e ganha link para UC025.
- **CA26:** Ficha Técnica mantém fluxos de edição existentes.
- **CA27:** navegação para Ficha, registro de preço, histórico e Produto funciona.
- **CA28:** GET não persiste resultado nem cria Ficha/configuração.
- **CA29:** nenhuma migration/schema é alterado.
- **CA30:** nenhuma informação de outra Empresa aparece.
- **CA31:** mudanças nas dependências correntes refletem no próximo GET.
- **CA32:** UC028/dashboard não é antecipado.

## Matriz de testes

UC025 não introduz fórmula de domínio nova; não é necessária uma nova calculadora Core.

### Integração — orquestração / projeção

- **P1:** resultado expõe DataOperacional, Rendimento e TempoAtivo usados.
- **P2:** resultado expõe ValorHora, TarifaEnergia e Incremento usados.
- **P3:** zero configurado permanece zero e é distinto de null.
- **P4:** estado sem Ficha mantém Produto/preço atual e inputs de Ficha indisponíveis.
- **P5:** configuração ausente mantém preço atual e parâmetros de configuração null.
- **P6:** expansão do resultado não altera valores UC018–UC024.
- **P7:** Itens e Usos permanecem associados aos mesmos Ids/resultados.
- **P8:** execução não persiste os novos dados explicativos.

### Integração — Web

- **W1:** autenticação e Empresa Ativa são obrigatórias.
- **W2:** cross-tenant retorna 404.
- **W3:** Produto ativo e inativo abrem a página.
- **W4:** Detalhes contém link `Detalhar precificação` para ativo e inativo.
- **W5:** Produto completo exibe todos os blocos principais.
- **W6:** parâmetros atuais exibem DataOperacional/Rendimento/TempoAtivo/configuração corretos.
- **W7:** tabela de Itens exibe Nome/Marca/Quantidade/Unidade/perda e resultados UC018/019.
- **W8:** Item sem preço mostra Sem preço vigente e não inventa custo.
- **W9:** Ficha vazia exibe estado vazio e custo incompleto.
- **W10:** mão de obra completa exibe base, percentual e custo.
- **W11:** custo base indisponível torna mão de obra indisponível.
- **W12:** percentual zero com custo base conhecido preserva custo zero.
- **W13:** tabela de equipamentos exibe potência/tempo/consumo/custo.
- **W14:** tarifa null mantém consumo conhecido e custo indisponível.
- **W15:** zero equipamentos preserva energia zero.
- **W16:** consolidação exibe componentes, total, rendimento e custo unitário corretos.
- **W17:** custo incompleto mantém componentes conhecidos e total/unitário indisponíveis.
- **W18:** preço completo exibe MargemAlvo, PrecoTeorico, Incremento e PrecoSugerido.
- **W19:** incremento null mantém PrecoTeorico e sugerido indisponível.
- **W20:** preço de prateleira ausente mantém UC023 visível e margem incompleta.
- **W21:** preço de prateleira presente exibe DataReferencia, MargemAtual e Situação.
- **W22:** custo atual diferente do snapshot histórico continua usando estado atual.
- **W23:** MargemAlvo atual diferente da MargemReferencia histórica continua usando meta atual.
- **W24:** Produto sem Ficha preserva preço comercial conhecido e exibe impedimento.
- **W25:** configuração ausente não cria configuração e mantém página consultável.
- **W26:** pendências gerais são listadas integralmente.
- **W27:** Incremento ausente não aparece como impedimento de MargemAtual quando custo/preço existem.
- **W28:** navegação para Ficha, Novo preço, Histórico e Produto está presente.
- **W29:** UC025 não exibe lista histórica nem DescontoReferencia.
- **W30:** GET repetido não persiste/cria estado.
- **W31:** alteração de custo/configuração/meta/preço reflete no próximo GET.
- **W32:** nenhuma informação de outra Empresa aparece.
- **W33:** Ficha Técnica continua com POST/ações de edição sem regressão.
- **W34:** Detalhes mantém resumo UC024 sem incorporar composição completa.
- **W35:** página não contém formulário POST de edição.

## Alterações esperadas

### Web

Adicionar:

~~~text
Pages/Produtos/Precificacao.cshtml
Pages/Produtos/Precificacao.cshtml.cs
~~~

ou estrutura equivalente mantendo rota:

~~~text
/Produtos/Precificacao/{id:int}
~~~

### Orquestração

Ampliar `ResultadoPrecificacaoProdutoAtual` com os inputs explicativos usados no cálculo, sem mover fórmulas para Web.

### Core

Nenhuma nova fórmula esperada.

Não criar nova calculadora apenas para apresentação.

### Infrastructure

Nenhuma alteração de schema.

Consultas adicionais são apenas para metadados estruturais de Ficha/Itens/Equipamentos.

## Documentação

Atualizar:

- backlog;
- catálogo;
- F002;
- F004;
- modelo de precificação.

Não é necessária nova RN se a implementação permanecer como projeção das regras existentes.

## Fora do escopo

- editar Ficha Técnica;
- criar/editar/remover Itens ou equipamentos;
- registrar Preço de prateleira dentro da mesma página;
- listar histórico completo;
- DescontoReferencia histórico;
- gráficos;
- dashboard/resumo multi-Produto — UC028;
- filtro abaixo da margem — UC029;
- identificação consolidada de incompletos — UC030;
- exportação/impressão;
- persistência de cálculo;
- cache;
- nova API REST;
- impostos, comissão, frete ou novos componentes econômicos.

## Gate

UC023 está concluída e fornece Preço teórico/sugerido.

UC024 está concluída e fornece Preço de prateleira atual, Margem atual e Situação no mesmo resultado compartilhado.

`PrecificacaoProdutoAtual` já centraliza UC018–UC024 e fornece resultados por Item/Uso.

Não existe alteração estrutural pendente.

**UC025 está liberada para implementação após o merge desta documentação.**

## Branch sugerida

~~~text
feat/uc025-detalhamento-precificacao
~~~

## Commit sugerido

~~~text
feat: consulta detalhamento da precificacao
~~~
