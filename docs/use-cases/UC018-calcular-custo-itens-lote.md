# UC018 — Calcular custo atual dos itens do lote

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC005, UC006, UC014 e UC017
- **Alteração de schema:** não
- **Persistência de resultado:** não

## Objetivo

Calcular, para a composição atual da Ficha Técnica, o custo corrente de cada Item a partir da Quantidade utilizada e do custo unitário do preço vigente do Insumo.

A UC018 também calcula o **Custo base dos itens** quando todos os Itens possuem custo determinável.

Esta é a primeira fatia do motor de custo. Não inclui perdas, mão de obra, energia/equipamentos, custo total do lote, custo unitário do Produto, margem ou preço sugerido.

## Fórmula normativa

Aplicar RN011:

~~~text
CustoItem = QuantidadeUtilizada × CustoUnitarioAtualDoInsumo
~~~

onde:

~~~text
CustoUnitarioAtualDoInsumo = PrecoVigente.PrecoCompra / PrecoVigente.QuantidadeCompra
~~~

e `PrecoVigente` é selecionado exclusivamente pela RN006 usando a data operacional da Empresa.

Quando todos os Itens possuem custo determinável:

~~~text
CustoBaseItens = Σ CustoItem
~~~

## Preço vigente

Para cada Insumo da Ficha, selecionar o preço vigente conforme RN006:

1. considerar somente registros com `DataReferencia <= DataOperacionalEmpresa`;
2. ordenar por `DataReferencia DESC`;
3. em empate, usar `Id DESC`;
4. selecionar o primeiro.

Não usar:

- maior Id sem considerar DataReferencia;
- preço futuro;
- último registro inserido se ele ainda for futuro;
- preço de outro tenant.

A UC018 deve reutilizar/estender `PrecoInsumoConsultas`; não duplicar a regra RN006 dentro da PageModel.

## Data operacional

Usar:

~~~text
IDataOperacionalEmpresa.Hoje
~~~

A data do servidor/UTC não substitui a data operacional da Empresa.

## Vários Itens e eficiência

A Ficha pode conter vários Insumos.

A implementação deve resolver os preços vigentes **em lote**. Não executar uma consulta de preço por Item.

O código real pós-UC027 possui apenas `PrecoInsumoConsultas.SelecionarVigenteAsync` para um único Insumo. Portanto, UC018 deve ampliar `PrecoInsumoConsultas` com uma operação para múltiplos `InsumoId`, preservando RN006.

Essa consulta em lote deve:

- realizar uma única ida lógica ao banco para o conjunto de Insumos da Ficha;
- ser tenant-aware pelo GQF normal;
- não usar `IgnoreQueryFilters`;
- filtrar `DataReferencia <= dataOperacionalEmpresa`;
- preservar desempate por `DataReferencia DESC, Id DESC` dentro de cada Insumo;
- retornar no máximo um preço vigente por Insumo;
- permitir ausência de resultado para Insumo sem preço vigente;
- não alterar a semântica de `SelecionarVigenteAsync` existente;
- ser coberta por testes de infraestrutura.

Nome sugerido, não obrigatório:

~~~text
SelecionarVigentesAsync(
    IQueryable<PrecoInsumo> precos,
    IReadOnlyCollection<int> insumoIds,
    DateOnly dataOperacionalEmpresa)
~~~

A implementação interna pode usar projeção/agrupamento compatível com SQLite/EF Core, desde que não introduza N+1.

## Precisão e arredondamento

Aplicar RN026.

Não arredondar para centavos:

- `CustoUnitario`;
- `CustoItem`;
- soma `CustoBaseItens`

durante o cálculo.

Exemplo:

~~~text
Quantidade = 37
CustoUnitario = 0,013579
CustoItem = 0,502423
~~~

O valor interno continua com precisão decimal disponível; formatação é responsabilidade da apresentação.

## Insumo sem preço vigente

Aplicar RN007 e RN017.

Um Item não possui custo determinável quando seu Insumo:

- não possui qualquer registro de preço; ou
- possui apenas preços futuros em relação à data operacional.

Nesses casos:

~~~text
CustoUnitario = indisponível
CustoItem = indisponível
~~~

Nunca usar:

~~~text
0
0,00
preço futuro
último preço histórico não vigente
~~~

como substituto silencioso.

## Total parcial é proibido

Se pelo menos um Item não possuir preço vigente, os Itens que possuem preço podem continuar exibindo seus custos individuais conhecidos.

Porém:

~~~text
CustoBaseItens = indisponível
~~~

Não somar somente os Itens conhecidos e apresentar o resultado como `Custo base dos itens`.

A interface deve deixar explícito que existem Itens sem preço vigente.

## Ficha sem Itens

Preservar a decisão da UC013:

- Ficha vazia é válida estruturalmente;
- não equivale a custo zero;
- não torna a precificação completa.

Portanto, para Ficha existente sem Itens:

~~~text
CustoBaseItens = indisponível
~~~

Não exibir `R$ 0,00` como custo dos itens.

## Produto sem Ficha

Produto sem Ficha continua na superfície de criação da base conforme UC013/UC017.

Não existe cálculo de custo dos Itens enquanto não existir Ficha.

GET não cria Ficha nem resultado de cálculo.

## Insumo inativo

Um Insumo já pertencente à Ficha continua participando do cálculo mesmo se estiver inativo.

A situação cadastral não invalida seu preço vigente nem remove o Item da composição.

Se houver preço vigente, calcular normalmente.

Se não houver preço vigente, aplicar RN007/RN017.

Não reativar o Insumo.

## Produto inativo

Produto inativo pode ter o custo atual da Ficha consultado.

O cálculo não reativa o Produto.

## Escopo de apresentação

Evoluir a rota canônica já existente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Não criar nova tela de Precificação completa neste UC.

Na tabela da composição, acrescentar:

- `Custo unitário`;
- `Custo do item`.

Preservar as colunas e ações da UC017.

### Item com preço vigente

Exibir custo unitário e custo do Item.

### Item sem preço vigente

Na coluna `Custo unitário`, usar texto explícito:

~~~text
Sem preço vigente
~~~

Na coluna `Custo do item`, usar:

~~~text
—
~~~

ou apresentação equivalente que não sugira valor zero.

## Resumo do custo base

Quando a Ficha possui pelo menos um Item e todos possuem preço vigente, mostrar:

~~~text
Custo base dos itens: <valor>
~~~

Quando houver Item sem preço vigente, mostrar:

~~~text
Custo base dos itens: indisponível
Há item(ns) sem preço vigente.
~~~

Quando a Ficha estiver vazia, mostrar:

~~~text
Custo base dos itens: indisponível
A ficha não possui itens.
~~~

A redação pode ser ajustada minimamente na implementação, desde que a semântica seja inequívoca.

## Formatação

Reutilizar a convenção pt-BR já existente.

Recomendação:

- custo unitário: até 6 casas decimais;
- custo do Item: até 4 casas decimais;
- custo base dos Itens: até 4 casas decimais.

Não alterar o valor de cálculo para produzir essa representação.

É aceitável ampliar `PrecoInsumoFormatacao` ou criar helper de apresentação específico de custo, evitando formatação monetária duplicada em Razor.

## Modelo de cálculo

O cálculo não deve ficar acoplado a strings formatadas da PageModel.

Não converter `Quantidade` formatada de volta para decimal.

Usar valores `decimal` reais para cálculo.

Recomendação arquitetural KISS:

1. carregar composição atual com Quantidade decimal;
2. resolver preços vigentes dos Insumos em lote;
3. aplicar um cálculo puro/reutilizável para `CustoItem` e `CustoBaseItens`;
4. formatar somente na projeção Web.

Como UC018 inicia o motor de custo, é recomendável criar um componente puro em `Precificador.Core` sob namespace de Precificação em vez de manter a fórmula dentro da Razor Page.

Esse componente não acessa EF, relógio, tenant ou HTTP.

Exemplo conceitual:

~~~text
Entrada:
- ItemId
- Quantidade
- CustoUnitario? 

Saída por Item:
- ItemId
- CustoUnitario?
- CustoItem?

Resultado:
- Itens
- CustoBaseItens?
- Completo
~~~

O nome concreto das classes pode ser definido na implementação.

## Estado incompleto

A UC018 pode expor no resultado técnico que o cálculo dos Itens está completo/incompleto.

Não persistir `StatusPrecificacao` no Produto.

Não implementar ainda os estados finais RN027 (`Incompleto`, `AbaixoDaMargem`, `DentroDaMargem`) como fluxo de negócio completo.

Neste UC, `Completo` significa apenas:

~~~text
Ficha possui >= 1 Item
E
todos os Itens possuem preço vigente
~~~

Isso não significa que a precificação completa do Produto esteja pronta, pois perdas, mão de obra, energia e demais componentes ainda virão nos UCs seguintes.

## Recalculo atual

O custo é derivado em tempo de consulta.

Alterações em:

- Quantidade do Item;
- composição da Ficha;
- preço vigente do Insumo;
- data operacional da Empresa

devem refletir naturalmente no próximo cálculo.

Não persistir snapshot na Ficha ou no Item.

Snapshots comerciais pertencem ao UC011.

## Multiempresa e segurança

Preservar FT002.

- Produto/Ficha/Item/Insumo/PrecoInsumo respeitam GQF;
- não usar `IgnoreQueryFilters` no cálculo Web;
- preços de outro tenant não podem participar;
- `EmpresaId` nunca vem do request;
- cálculo não realiza escrita.

## Ausência de efeitos colaterais

GET da Ficha com cálculo:

- não cria/edita Ficha;
- não cria/edita Item;
- não cria/edita Insumo;
- não cria/edita PrecoInsumo;
- não altera `IdentidadeConsolidada`;
- não persiste custos.

## Relação com UC026 e UC027

A revalidação pós-UC027 confirmou que as configurações introduzidas por UC026/UC027 não participam do custo dos Itens da Ficha.

UC018 **não deve consultar** `ConfiguracaoPrecificacaoEmpresa`.

Em particular, não usar neste UC:

- ValorHoraTrabalho;
- TarifaEnergiaKwh;
- MargemPadrao;
- IncrementoComercial;
- ReservaComercialDesconto.

A única informação temporal da Empresa necessária é `IDataOperacionalEmpresa.Hoje`, usada para selecionar preço vigente pela RN006.

Essas configurações entram apenas nos UCs posteriores correspondentes.

## Relação com UC019

UC018 calcula apenas o custo base dos Itens sem perdas.

Não aplicar percentual de perda nem quantidade adicional.

UC019 será revalidada depois desta implementação para modelar perdas de forma genérica entre os diferentes negócios.

## Relação com UC022

`CustoBaseItens` é um componente futuro de `CustoLote`.

UC018 não deve chamar o resultado de:

- Custo do lote;
- Custo total;
- Custo unitário do Produto.

Esses conceitos pertencem ao UC022.

## Critérios de aceitação

### CA01

Cada Item com preço vigente possui `CustoItem = Quantidade × CustoUnitario`.

### CA02

O preço vigente usado respeita RN006 e a data operacional da Empresa.

### CA03

Empate de DataReferencia usa maior Id.

### CA04

Preço futuro não participa do cálculo antes de sua data.

### CA05

Item sem preço vigente não recebe custo zero.

### CA06

Quando todos os Itens possuem preço vigente, `CustoBaseItens` é a soma exata dos custos dos Itens.

### CA07

Se qualquer Item não possui preço vigente, `CustoBaseItens` fica indisponível; não há total parcial apresentado como total.

### CA08

Ficha sem Itens não apresenta custo base zero.

### CA09

Insumo inativo existente na Ficha continua participando do cálculo.

### CA10

Produto inativo continua calculável sem reativação.

### CA11

Cálculos intermediários não são arredondados para centavos.

### CA12

Custos não são persistidos em Item, Ficha ou Produto.

### CA13

Preços de outro tenant não participam do cálculo.

### CA14

A página da Ficha preserva composição e ações da UC017 e acrescenta custos.

### CA15

GET de cálculo não muta estado.

## Matriz de testes

### Unitários — cálculo puro

- U1: calcula `Quantidade × CustoUnitario`;
- U2: preserva precisão sem arredondamento intermediário;
- U3: soma todos os Itens quando completos;
- U4: um Item sem custo unitário torna `CustoBaseItens` indisponível sem apagar custos conhecidos dos demais;
- U5: coleção vazia resulta em cálculo dos Itens incompleto e total indisponível.

### Infraestrutura — seleção em lote de preços vigentes

- P1: seleciona preço mais recente com DataReferencia <= data operacional;
- P2: desempata mesma DataReferencia por maior Id;
- P3: ignora preço futuro;
- P4: Insumo com apenas preço futuro não recebe vigente;
- P5: vários Insumos são resolvidos corretamente no mesmo fluxo;
- P6: consulta respeita tenant e não retorna preço de outra Empresa;
- P7: resolução de vários Insumos ocorre por operação em lote, sem chamada individual de `SelecionarVigenteAsync` por Item.

Os testes existentes de `SelecionarVigenteAsync` continuam válidos como proteção da RN006, mas UC018 deve adicionar cobertura própria para a nova consulta em lote.

### Web

- W1: Ficha com Item/preço vigente mostra Custo unitário e Custo do Item corretos;
- W2: múltiplos Itens completos mostram `CustoBaseItens` correto;
- W3: preço futuro não entra no cálculo Web;
- W4: Item sem qualquer preço mostra `Sem preço vigente` e não mostra zero;
- W5: Item com somente preço futuro também mostra ausência de vigente;
- W6: um Item incompleto + um completo mantém custo individual do completo, mas não exibe total parcial;
- W7: Ficha vazia mostra custo base indisponível, não zero;
- W8: Produto sem Ficha não calcula custos nem cria Ficha;
- W9: Insumo inativo com preço vigente é calculado sem reativação;
- W10: Produto inativo é calculado sem reativação;
- W11: outro tenant não vaza preço/custo;
- W12: GET não persiste/muta estado;
- W13: tabela mantém Observação/Situação/Editar/Remover da UC017;
- W14: valores de apresentação seguem pt-BR e não alteram o cálculo interno;
- W15: POST inválido da base da Ficha preserva composição, custos individuais e resumo de custo calculado, sem mutação persistida.

## Alterações esperadas

### Core

- adicionar modelo/calculadora pura e reutilizável para custo dos Itens;
- sem dependência de EF/Web.

### Infrastructure

- ampliar `PrecoInsumoConsultas` com resolução em lote dos preços vigentes de múltiplos Insumos;
- manter `SelecionarVigenteAsync` existente sem mudança de semântica;
- manter RN006 centralizada;
- não introduzir consulta por Item/N+1.

### Web

- injetar `IDataOperacionalEmpresa` em `FichaTecnicaModel`;
- não injetar `ConfiguracaoPrecificacaoEmpresa` nem consultar suas configurações neste UC;
- carregar Quantidade decimal para cálculo;
- compor custos na projeção da Ficha;
- usar `PrecoInsumo.CustoUnitario` como fonte do custo unitário do preço selecionado, evitando duplicar a fórmula RN004 na PageModel;
- adicionar colunas `Custo unitário` e `Custo do item`;
- adicionar resumo `Custo base dos itens`;
- garantir que o mesmo carregamento de composição/custos seja usado no GET e no retorno de POST inválido;
- preservar UC013–UC017.

### Persistência

- nenhuma migration;
- nenhum novo DbSet;
- nenhum campo de custo persistido.

## Fora do escopo

- perdas (UC019);
- mão de obra (UC020);
- energia/equipamentos (UC021);
- custo total do lote (UC022);
- custo unitário do Produto;
- preço teórico/sugerido (UC023);
- preço de prateleira;
- margem;
- snapshot comercial;
- persistência de custo;
- estoque;
- conversão de unidade;
- média de preços;
- custo médio ponderado;
- seleção manual de preço;
- filtros/pesquisa;
- status final RN027 persistido.

## Revalidação pós-UC027

Revalidação concluída contra a implementação real mergeada da UC027.

Foi confirmado:

1. UC027 está mergeada e UC026/UC027 não alteraram RN004, RN006, RN007, RN011, RN017 ou RN026;
2. `ConfiguracaoPrecificacaoEmpresa` não participa do custo dos Itens e não deve ser consultada pela UC018;
3. `IDataOperacionalEmpresa.Hoje` permanece a fonte normativa da data operacional;
4. `PrecoInsumo.CustoUnitario` continua calculando `PrecoCompra / QuantidadeCompra`;
5. `PrecoInsumoConsultas.SelecionarVigenteAsync` continua filtrando por data operacional e desempata por `DataReferencia DESC, Id DESC`;
6. a infraestrutura atual ainda não possui seleção em lote, portanto UC018 deve adicioná-la sem N+1;
7. a rota canônica continua `/Produtos/FichaTecnica/{id:int}`;
8. a Ficha atual carrega Item + Insumo e preserva ordenação, observação, situação e ações da UC017;
9. o POST inválido da base já recarrega a composição, então UC018 deve recarregar também os custos nesse mesmo fluxo;
10. `PrecoInsumoFormatacao` já oferece formatação pt-BR de custo unitário até 6 casas, podendo ser reutilizado/ampliado;
11. `CustomWebApplicationFactory` já suporta `IDataOperacionalEmpresa` fixa, permitindo testes Web determinísticos de preço vigente/futuro;
12. nenhuma migration, novo DbSet ou persistência de custo é necessária.

Não houve mudança material na regra funcional da UC018.

Os refinamentos desta revalidação são apenas técnicos/de cobertura:

- seleção vigente em lote passa a ser obrigatória;
- N+1 deixa de ser alternativa aceitável;
- POST inválido da Ficha deve preservar custos calculados na renderização;
- UC018 não deve depender das configurações de precificação criadas em UC026/UC027.

A UC018 está liberada para implementação.

## Branch sugerida

~~~text
feat/uc018-custo-itens-lote
~~~

## Commit sugerido futuro

~~~text
feat: calcula custo atual dos itens da ficha
~~~
