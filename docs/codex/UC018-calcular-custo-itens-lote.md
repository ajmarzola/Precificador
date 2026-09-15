# Instrução Codex — UC018: Calcular custo atual dos itens do lote

## Tarefa

Implementar integralmente a UC018 conforme `docs/use-cases/UC018-calcular-custo-itens-lote.md`.

Branch obrigatória:

~~~text
feat/uc018-custo-itens-lote
~~~

Não editar/commitar/push direto em `master`. Não fazer merge da própria implementação.

## Precondições

Antes de alterar arquivos:

1. atualizar `master`;
2. criar/trocar para a branch obrigatória;
3. confirmar branch != master;
4. ler `AGENTS.md`;
5. ler `docs/development/backlog.md` e confirmar UC018 = `Pronto`;
6. ler UC018, F004 e RN004/RN006/RN007/RN011/RN017/RN026;
7. inspecionar `PrecoInsumo`, `PrecoInsumoConsultas`, `IDataOperacionalEmpresa`, `FichaTecnicaModel`, `FichaTecnica.cshtml`, `PrecoInsumoFormatacao` e testes atuais de preço/Ficha.

Se UC018 não estiver `Pronto`, não implementar.

## Escopo funcional

Calcular custo atual de cada Item da Ficha:

~~~text
CustoItem = Quantidade × CustoUnitarioDoPrecoVigente
~~~

onde o preço vigente é selecionado pela RN006 e:

~~~text
CustoUnitario = PrecoInsumo.CustoUnitario
~~~

Não duplicar a fórmula `PrecoCompra / QuantidadeCompra` na PageModel.

Quando todos os Itens possuírem preço vigente:

~~~text
CustoBaseItens = soma dos CustoItem
~~~

Se qualquer Item estiver sem preço vigente:

- custos individuais conhecidos continuam disponíveis;
- `CustoBaseItens` fica indisponível;
- não apresentar total parcial como total confiável.

Ficha sem Itens também deixa `CustoBaseItens` indisponível, nunca zero.

## Core — cálculo puro

Criar um componente puro/reutilizável em namespace de Precificação no Core.

Não depender de:

- EF;
- Web;
- tenant/contexto;
- relógio/data operacional;
- strings formatadas.

Entrada conceitual por Item:

~~~text
ItemId
Quantidade decimal
CustoUnitario decimal?
~~~

Saída conceitual:

~~~text
ItemId
CustoUnitario decimal?
CustoItem decimal?
~~~

Resultado agregado:

~~~text
Itens
CustoBaseItens decimal?
Completo bool
~~~

`Completo` neste UC significa somente:

~~~text
há pelo menos 1 Item
E
todos os Itens possuem custo unitário conhecido
~~~

Não implementar status final RN027.

## Precisão

Usar `decimal` em todo o cálculo.

Não arredondar:

- CustoUnitario;
- CustoItem;
- CustoBaseItens.

Arredondamento/formatação somente na apresentação.

## Infrastructure — preço vigente em lote

O código atual possui somente `SelecionarVigenteAsync` para um único Insumo.

Adicionar operação em lote em `PrecoInsumoConsultas` para todos os `InsumoId` da Ficha.

Requisitos:

- uma única ida lógica ao banco para o conjunto de Insumos;
- sem chamada individual `SelecionarVigenteAsync` em loop;
- sem N+1;
- GQF normal;
- sem `IgnoreQueryFilters`;
- filtrar `DataReferencia <= dataOperacionalEmpresa`;
- desempatar dentro de cada Insumo por `DataReferencia DESC`, depois `Id DESC`;
- retornar no máximo um preço vigente por Insumo;
- Insumo sem vigente simplesmente não aparece/retorna null no mapeamento;
- manter `SelecionarVigenteAsync` existente sem mudar semântica.

Nome sugerido:

~~~text
SelecionarVigentesAsync(...)
~~~

A forma exata de retorno pode ser lista/dicionário/projeção, desde que seja simples e testável.

Não carregar preços futuros para cálculo.

## Data operacional

Injetar `IDataOperacionalEmpresa` em `FichaTecnicaModel`.

Usar exclusivamente:

~~~text
dataOperacionalEmpresa.Hoje
~~~

Não usar DateTime.Now, UtcNow, DateOnly.FromDateTime(DateTime.Now) ou equivalente.

`CustomWebApplicationFactory` já suporta data operacional fixa; usar isso nos testes determinísticos.

## Não usar ConfiguracaoPrecificacaoEmpresa

UC018 não depende de UC026/UC027 em runtime.

Não consultar/injetar:

- ValorHoraTrabalho;
- TarifaEnergiaKwh;
- MargemPadrao;
- IncrementoComercial;
- ReservaComercialDesconto.

Esses parâmetros pertencem aos UCs posteriores.

## Web — carregamento da Ficha

Evoluir `/Produtos/FichaTecnica/{id:int}`.

Preservar integralmente:

- Produto/Nome/Categoria/Situação;
- Rendimento/TempoAtivo editáveis;
- Insumo;
- Marca;
- Quantidade;
- Unidade;
- Observação contextual;
- Situação;
- Editar/Remover;
- ordenação NomeNormalizado/MarcaNormalizada.

Adicionar por Item:

- `Custo unitário`;
- `Custo do item`.

Adicionar resumo:

- `Custo base dos itens`.

## Quantidade

Hoje `ItemFichaResumo` recebe Quantidade já formatada.

Para calcular, não converter string formatada de volta para decimal.

Carregar/projetar a Quantidade decimal real e formatá-la separadamente para a UI.

É aceitável o resumo do Item carregar ambos:

- Quantidade decimal interna;
- propriedade formatada para apresentação.

## Item com preço vigente

Exibir custo unitário e custo do Item.

Reutilizar `PrecoInsumoFormatacao.CustoUnitario` quando adequado.

Para custo do Item e total, usar helper de apresentação centralizado, preferencialmente com até 4 casas decimais conforme a especificação.

Não espalhar `.ToString(...)` monetário pela Razor.

## Item sem preço vigente

Exibir:

~~~text
Custo unitário: Sem preço vigente
Custo do item: —
~~~

Não exibir 0, `0,00` ou preço futuro.

## Resumo

Ficha com todos os Itens completos:

~~~text
Custo base dos itens: <valor>
~~~

Ficha com algum Item sem preço:

~~~text
Custo base dos itens: indisponível
Há item(ns) sem preço vigente.
~~~

Ficha sem Itens:

~~~text
Custo base dos itens: indisponível
A ficha não possui itens.
~~~

## GET e POST inválido

O carregamento de composição + custos deve ser centralizado no mesmo fluxo usado por:

- GET;
- retorno de POST inválido da base da Ficha.

Em POST inválido:

- preservar Input digitado/erros da base;
- preservar tabela completa;
- preservar custos individuais;
- preservar resumo de custo;
- não persistir mutação.

Não sobrescrever Input inválido com valores persistidos.

## Produto/Ficha/Insumo inativos

- Produto inativo continua consultável/calculável;
- Insumo inativo já existente na Ficha continua participando do cálculo;
- nenhum deles é reativado.

## Ausência de Ficha

Produto sem Ficha:

- continua mostrando superfície de criação da base;
- não mostra cálculo de Itens;
- não cria Ficha no GET;
- não cria qualquer resultado persistido.

## Tenant

Preservar GQF de Produto/Ficha/Item/Insumo/PrecoInsumo.

Não usar `IgnoreQueryFilters` no código de produção.

Preço de outro tenant nunca participa.

Request não controla EmpresaId.

## Persistência

Não criar:

- migration;
- novo DbSet;
- coluna de custo;
- snapshot de custo;
- status persistido.

GET/calculadora não chamam SaveChanges.

## Não antecipar UCs seguintes

Não implementar:

- perdas (UC019);
- mão de obra (UC020);
- energia (UC021);
- CustoLote/CustoTotal/CustoUnitarioProduto (UC022);
- PrecoTeorico/PrecoSugerido (UC023);
- margem atual;
- RegistroPrecoProduto;
- ReservaComercialReferencia;
- desconto de referência.

## Testes obrigatórios

### Core U1–U5

- multiplicação;
- precisão;
- soma completa;
- Item sem custo torna total indisponível sem apagar custos conhecidos;
- coleção vazia incompleta.

### Infrastructure P1–P7

Adicionar testes da seleção em lote:

- vigente pela data operacional;
- desempate mesma data por maior Id;
- futuro ignorado;
- apenas futuro => sem vigente;
- vários Insumos no mesmo fluxo;
- isolamento tenant;
- prova de operação em lote/ausência de chamada individual por Item.

Os testes existentes de `SelecionarVigenteAsync` devem continuar verdes.

### Web W1–W15

Atender integralmente a matriz da UC018.

Ênfases:

- custo unitário e custo do Item;
- total completo;
- futuro;
- ausência de preço;
- apenas futuro;
- total parcial proibido;
- Ficha vazia;
- Produto sem Ficha;
- Insumo inativo;
- Produto inativo;
- tenant;
- GET sem mutação;
- preservação das colunas/ações UC017;
- pt-BR/precisão;
- POST inválido preservando custos e sem mutação.

Para W3/W5, usar factory com data operacional fixa, não data corrente do ambiente.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Também executar `git diff --check`.

## Documentação pós-implementação

Na PR de implementação:

- alterar UC018 de `Pronto` para `Concluído` somente no backlog;
- não alterar MEL009;
- não criar migration;
- não reintroduzir Status em documentos individuais.

## Retorno obrigatório

Informar:

1. branch;
2. arquivos alterados;
3. componente de cálculo criado;
4. estratégia de seleção vigente em lote;
5. uso da data operacional;
6. comportamento sem preço/total parcial;
7. mudanças na Ficha;
8. testes U/P/W;
9. build/test;
10. URL da PR.

Commit sugerido:

~~~text
feat: calcula custo atual dos itens da ficha
~~~
