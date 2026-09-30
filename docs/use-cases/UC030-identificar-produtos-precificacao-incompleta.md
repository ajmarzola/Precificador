# UC030 — Identificar Produtos com precificação incompleta

- **Funcionalidade principal:** F005 — Dashboard de Margens
- **Superfície relacionada:** F002 / UC008 — listagem de Produtos
- **Dependências funcionais:** UC017, UC023, UC024, UC028 e UC029
- **Estado:** Concluído
- **Alteração de schema:** não
- **Persistência de resultado:** não
- **Natureza:** consulta somente leitura
- **Regras principais:** RN017, RN022, RN023, RN026, RN027, RN036 e RN037

## Objetivo

Identificar, de forma explícita e acionável, Produtos ativos cuja **precificação atual global** não pode ser formada completamente conforme RN017.

A UC030 conclui o recorte operacional iniciado por UC028/UC029 e disponibiliza o mesmo conceito em:

- Dashboard da Empresa;
- listagem de Produtos.

A UC030 não redefine `SituacaoMargem`. Ela introduz um conceito distinto:

~~~text
precificação completa / precificação incompleta
~~~

A classificação global considera a cadeia atual de precificação como um todo e deve informar **por que** um Produto está incompleto.

## Distinção obrigatória entre margem e precificação

`SituacaoMargemProduto.Incompleto` continua significando somente:

~~~text
Margem atual indisponível
~~~

Isso ocorre quando a Margem atual não pode ser calculada por ausência de Custo unitário atual e/ou Preço de prateleira atual, conforme RN027.

Já **precificação incompleta** significa que pelo menos um resultado necessário da cadeia atual de precificação não pode ser formado conforme RN017.

Consequência importante:

- todo Produto com Margem indisponível é candidato natural a precificação incompleta;
- porém um Produto pode ter Margem atual calculável e ainda assim possuir precificação global incompleta.

Exemplo obrigatório:

~~~text
Custo unitário atual        disponível
Preço de prateleira atual   disponível
Margem atual                disponível
Incremento comercial        ausente
Preço sugerido              indisponível

=> SituacaoMargem pode ser DentroDaMargem ou AbaixoDaMargem
=> Precificação global = Incompleta
~~~

Portanto, **não derivar precificação incompleta a partir de `SituacaoMargem.Incompleto`**.

## Superfícies

A UC030 evolui as mesmas duas superfícies operacionais da UC029:

~~~text
GET /Dashboard
GET /Produtos
~~~

Não criar nova página.

O Dashboard continua protegido pela policy `EmpresaAtiva` e permanece a Home operacional da Empresa.

`/Produtos` continua tenant-aware e preserva a listagem completa de Produtos ativos/inativos quando nenhum recorte operacional estiver selecionado.

## Critério de completude global

Para a fotografia atual do Produto, considerar a precificação **completa** somente quando todos os resultados finais abaixo forem determináveis:

~~~text
CustoUnitarioProduto
PrecoSugerido
PrecoPrateleiraAtual
MargemAtual
~~~

Em termos conceituais:

~~~text
PrecificacaoCompleta =
    CustoProdutoCompleto
    AND PrecoProdutoCompleto
    AND PrecoPrateleiraAtual != null
    AND MargemAtual != null
~~~

A implementação pode materializar isso como booleano, enum ou value object, desde que a semântica acima permaneça explícita e testada.

Zero conhecido continua sendo valor válido. Apenas ausência/indisponibilidade caracteriza incompletude.

## Motivos estruturados de incompletude

Toda precificação classificada como incompleta deve possuir ao menos um motivo estruturado.

Conjunto mínimo da UC030:

~~~text
FichaTecnicaAusente
FichaTecnicaSemItens
InsumoSemPrecoVigente
ConfiguracaoPrecificacaoAusente
TarifaEnergiaNaoConfigurada
IncrementoComercialNaoConfigurado
PrecoPrateleiraNaoDefinido
~~~

Rótulos sugeridos para UI:

~~~text
Ficha técnica não cadastrada
Ficha técnica sem itens
Há item(ns) sem preço vigente
Configurações de precificação não encontradas
Tarifa de energia não configurada
Incremento comercial não configurado
Preço de prateleira não definido
~~~

Os motivos devem ser representados internamente por identificadores estruturados; **não usar o texto renderizado da UI para decidir completude ou filtrar Produtos**.

### Regras dos motivos

#### Ficha técnica ausente

Aplicar quando o Produto não possui Ficha Técnica atual.

#### Ficha técnica sem itens

Aplicar quando existe Ficha Técnica, mas ela não possui Itens.

Conforme RN017, isso não equivale a custo zero.

#### Insumo sem preço vigente

Aplicar quando pelo menos um Item da Ficha não possui preço de Insumo vigente na Data Operacional da Empresa.

Não é necessário listar nesta UC cada Insumo faltante; o motivo agregado é suficiente.

#### Configuração de precificação ausente

Aplicar quando a Empresa Ativa não possui `ConfiguracaoPrecificacaoEmpresa`.

Nesse caso, não duplicar artificialmente motivos que só poderiam ser avaliados dentro da configuração ausente.

#### Tarifa de energia não configurada

Aplicar somente quando:

~~~text
existem usos de equipamento elétrico na Ficha
AND TarifaEnergiaKwh == null
~~~

Se não houver usos de equipamento elétrico, tarifa nula continua válida para o cálculo de energia, cujo custo é zero e completo.

#### Incremento comercial não configurado

Aplicar quando:

~~~text
IncrementoComercial == null
~~~

Esse motivo torna `PrecoSugerido` incompleto, mas não invalida automaticamente Custo unitário nem Margem atual.

#### Preço de prateleira não definido

Aplicar quando não existe registro atual de Preço de prateleira para o Produto.

Esse motivo impede Margem atual, mas não invalida por si só Custo unitário nem Preço sugerido.

### Múltiplos motivos

Motivos independentes devem coexistir.

Exemplo:

~~~text
Ficha técnica sem itens
Incremento comercial não configurado
Preço de prateleira não definido
~~~

=> precificação incompleta com três motivos.

A apresentação deve evitar duplicidade e manter ordem determinística.

Ordem sugerida:

1. Ficha técnica/configuração;
2. Itens/preços de Insumo;
3. Energia;
4. Incremento comercial;
5. Preço de prateleira.

## Classificação compartilhada

A regra de completude e os motivos não devem ser implementados de forma divergente entre:

~~~text
PrecificacaoProdutoAtual
ResumoPrecificacaoProdutosAtual
~~~

Preferir extração de uma classificação pura/compartilhada, ou outro mecanismo que garanta equivalência sem executar o serviço individual Produto a Produto.

Objetivo arquitetural:

~~~text
mesmos fatos de entrada
=> mesma situação global
=> mesmos motivos
~~~

Não duplicar regras por texto em PageModels.

## Evolução de ResumoPrecificacaoProdutosAtual

O serviço em lote deve passar a expor, para cada Produto, no mínimo:

~~~text
PrecificacaoCompleta
MotivosPrecificacaoIncompleta
~~~

além dos valores já existentes:

~~~text
CustoUnitarioProduto
PrecoPrateleiraAtual
PrecoSugerido
MargemAtual
SituacaoMargem
~~~

Nome exato das propriedades/tipos pode variar, mas a classificação deve ser estruturada.

O cálculo permanece em lote. É proibido resolver os motivos chamando:

~~~text
foreach produto
    await PrecificacaoProdutoAtual.CalcularAsync(produto.Id)
~~~

## Filtros

A UC030 acrescenta um segundo valor válido ao parâmetro `filtro` criado pela UC029.

Valores suportados após esta UC:

~~~text
abaixo-da-margem
precificacao-incompleta
~~~

Ausência ou valor vazio preserva o modo padrão de cada superfície.

### Dashboard

~~~text
/Dashboard
/Dashboard?filtro=
=> Todos os Produtos ativos

/Dashboard?filtro=abaixo-da-margem
=> somente Produtos ativos com SituacaoMargem.AbaixoDaMargem

/Dashboard?filtro=precificacao-incompleta
=> somente Produtos ativos com precificação global incompleta
~~~

Controles visíveis:

~~~text
Todos os ativos
Abaixo da margem
Precificação incompleta
~~~

O estado selecionado deve continuar semanticamente identificável, por exemplo com `aria-current="page"`, sem depender apenas de cor.

### Lista de Produtos

~~~text
/Produtos
/Produtos?filtro=
=> comportamento normal da UC008/MEL024, incluindo Produtos inativos

/Produtos?filtro=abaixo-da-margem
=> somente Produtos ativos abaixo da margem

/Produtos?filtro=precificacao-incompleta
=> somente Produtos ativos com precificação global incompleta
~~~

O recorte continua combinando por AND com filtros cadastrais:

~~~text
/Produtos?q=agenda&categoria=3&filtro=precificacao-incompleta

=> q AND categoria AND precificacao-incompleta
~~~

Selecionar/remover somente o recorte operacional deve preservar `q` e `categoria`.

`Limpar filtros` remove `q`, `categoria` e `filtro`.

### Filtros não são categorias mutuamente exclusivas

`abaixo-da-margem` e `precificacao-incompleta` representam recortes diferentes.

Um mesmo Produto pode pertencer aos dois conjuntos.

Exemplo:

~~~text
Margem atual abaixo da meta
Incremento comercial ausente
~~~

=> aparece em `abaixo-da-margem`
=> aparece em `precificacao-incompleta`

Como `filtro` é single-value no MVP, o usuário visualiza um recorte por vez.

Não introduzir combinação:

~~~text
filtro=abaixo-da-margem,precificacao-incompleta
~~~

nem múltiplos parâmetros `filtro`.

## Valor de filtro inválido

Preservar a semântica da UC029.

Valor não suportado em qualquer superfície deve:

- responder HTTP 200;
- exibir zero linhas;
- não ampliar silenciosamente para a visão padrão;
- informar filtro inválido/não suportado;
- oferecer recuperação:
  - **Mostrar todos** no Dashboard;
  - **Limpar filtros** em `/Produtos`.

Normalizar whitespace externo e comparar os valores suportados sem sensibilidade a maiúsculas/minúsculas.

Links/option values gerados devem usar os valores canônicos em minúsculas.

## Apresentação da completude

A UC030 deve tornar a razão do recorte compreensível ao usuário.

Adicionar informação de **Precificação** às tabelas do Dashboard e da lista de Produtos.

Apresentação mínima:

### Produto completo

~~~text
Completa
~~~

### Produto incompleto

~~~text
Incompleta
- Ficha técnica sem itens
- Incremento comercial não configurado
~~~

Pode usar badges/lista/linhas compactas, desde que:

- o estado não dependa apenas de cor;
- todos os motivos aplicáveis sejam acessíveis;
- não seja necessário inferir o motivo a partir de campos `indisponível`;
- os rótulos venham dos identificadores estruturados.

### Dashboard — colunas após UC030

Preservar as colunas da UC028/UC029 e acrescentar:

~~~text
Precificação
~~~

Tabela:

~~~text
Produto
Custo unitário atual
Preço de prateleira atual
Margem atual
Margem-alvo
Preço sugerido
Situação da margem
Precificação
Consultar
~~~

### Lista de Produtos — colunas após UC030

Preservar todas as colunas existentes e acrescentar:

~~~text
Precificação
~~~

Tabela:

~~~text
Nome
Categoria
Custo unitário atual
Preço de prateleira vigente
Margem atual
Margem-alvo
Situação
Precificação
Consultar
~~~

A coluna **Situação** continua significando exclusivamente Ativo/Inativo.

## Indicadores do Dashboard

Os cards existentes continuam com a semântica da UC028/UC029:

~~~text
Produtos ativos
Insumos ativos
Abaixo da margem
Dentro da margem
Margem indisponível
~~~

A UC030 **não renomeia** `Margem indisponível` para `Precificação incompleta`.

Também não altera o significado das contagens existentes.

Nesta UC não é obrigatório criar um sexto card para precificação incompleta; a identificação global ocorre pelo estado/coluna e pelo recorte específico.

Se um Produto tiver Margem disponível, mas Preço sugerido incompleto, ele:

- continua sendo contado como abaixo/dentro da margem conforme sua Margem atual;
- pode aparecer simultaneamente no recorte de precificação incompleta.

## Estratégia de cálculo

### Dashboard

Fluxo conceitual:

~~~text
1. carregar Produtos ativos da Empresa Ativa
2. ResumoPrecificacaoProdutosAtual.CalcularAsync(ids) uma única vez
3. montar cards globais de margem sobre o universo completo
4. materializar completude + motivos de cada Produto
5. aplicar em memória o recorte selecionado:
   - nenhum => todos
   - abaixo-da-margem => SituacaoMargem.AbaixoDaMargem
   - precificacao-incompleta => !PrecificacaoCompleta
6. renderizar
~~~

Não reduzir o universo antes do resumo, pois os cards continuam globais.

### Lista de Produtos

Fluxo conceitual:

~~~text
1. aplicar q/categoria no banco
2. se houver recorte operacional, restringir candidatos a Produtos ativos
3. calcular ResumoPrecificacaoProdutosAtual uma única vez para os candidatos
4. aplicar em memória:
   - abaixo-da-margem
   - OU precificacao-incompleta
5. preservar ordenação por NomeNormalizado
~~~

Sem `filtro`, preservar a listagem atual com Produtos ativos e inativos.

Filtro inválido pode encerrar com coleção vazia sem cálculo desnecessário.

## Relação com PrecificacaoProdutoAtual

`PrecificacaoProdutoAtual` já possui informações como:

~~~text
CustoProdutoCompleto
PrecoProdutoCompleto
Impedimentos
ImpedimentosMargemAtual
~~~

A UC030 deve alinhar esses conceitos ao novo classificador global, sem quebrar a UC025.

É aceitável refatorar os impedimentos atuais para reutilizar identificadores estruturados e converter para texto somente na apresentação, desde que:

- os textos atuais da UC025 continuem equivalentes;
- os resultados financeiros não mudem;
- não seja criada persistência;
- os dois caminhos (individual e lote) permaneçam semanticamente equivalentes.

## Estado vazio

### Dashboard — nenhum incompleto

Com Produtos ativos, mas nenhum com precificação incompleta:

~~~text
Nenhum Produto ativo possui precificação incompleta.
~~~

Cards globais permanecem visíveis.

### Produtos — nenhum incompleto

Com `filtro=precificacao-incompleta` e zero resultado após `q/categoria`:

~~~text
Nenhum Produto ativo com precificação incompleta foi encontrado para os filtros informados.
~~~

ou texto equivalente que preserve o conceito de resultado filtrado vazio.

Não confundir com catálogo vazio.

## Multiempresa

Preservar FT002/RN036/RN037:

- todos os fatos usados na classificação pertencem à Empresa Ativa;
- não receber `EmpresaId` por query/form;
- não usar `IgnoreQueryFilters`;
- preço de Insumo vigente, Ficha, configuração, Categoria e preço do Produto permanecem tenant-aware;
- nenhum motivo pode revelar dados de outra Empresa;
- filtros não alteram tenant.

## Somente leitura

GET `/Dashboard` e GET `/Produtos` continuam sem persistência.

A UC030 não pode:

- gravar situação de completude;
- gravar motivos;
- registrar preço;
- alterar configuração;
- alterar Ficha;
- criar preço de Insumo;
- persistir seleção do filtro.

Não criar migration.

## Critérios de aceitação

- **CA01:** UC030 atua em `/Dashboard` e `/Produtos`, sem criar nova página.
- **CA02:** precificação incompleta é conceito distinto de `SituacaoMargem.Incompleto`.
- **CA03:** classificação global não é derivada exclusivamente de `SituacaoMargem`.
- **CA04:** precificação completa exige Custo unitário, Preço sugerido, Preço de prateleira e Margem atual determináveis.
- **CA05:** zero conhecido é aceito como valor válido; somente indisponibilidade torna a etapa incompleta.
- **CA06:** Produto incompleto possui ao menos um motivo estruturado.
- **CA07:** Ficha ausente gera `FichaTecnicaAusente`.
- **CA08:** Ficha sem Itens gera `FichaTecnicaSemItens`.
- **CA09:** Item sem preço vigente gera `InsumoSemPrecoVigente`.
- **CA10:** configuração ausente gera `ConfiguracaoPrecificacaoAusente`.
- **CA11:** tarifa nula com uso elétrico gera `TarifaEnergiaNaoConfigurada`.
- **CA12:** tarifa nula sem uso elétrico não torna a precificação incompleta por energia.
- **CA13:** Incremento comercial nulo gera `IncrementoComercialNaoConfigurado`.
- **CA14:** Preço de prateleira ausente gera `PrecoPrateleiraNaoDefinido`.
- **CA15:** múltiplos motivos independentes coexistem sem duplicidade.
- **CA16:** motivos possuem ordem determinística.
- **CA17:** `ResumoPrecificacaoProdutosAtual` expõe completude e motivos em lote.
- **CA18:** classificação individual e em lote é semanticamente equivalente.
- **CA19:** `filtro=precificacao-incompleta` é aceito nas duas superfícies.
- **CA20:** Dashboard oferece Todos os ativos, Abaixo da margem e Precificação incompleta.
- **CA21:** estado selecionado no Dashboard é semanticamente identificável e não depende só de cor.
- **CA22:** Dashboard com recorte incompleto exibe somente Produtos ativos incompletos.
- **CA23:** `/Produtos?filtro=precificacao-incompleta` exibe somente Produtos ativos incompletos.
- **CA24:** `/Produtos` sem filtro continua exibindo ativos e inativos.
- **CA25:** `q + categoria + filtro` continuam combinando por AND.
- **CA26:** selecionar/remover recorte preserva `q` e `categoria`.
- **CA27:** Limpar filtros remove todos os filtros em `/Produtos`.
- **CA28:** filtro inválido preserva comportamento seguro da UC029.
- **CA29:** Produto pode pertencer simultaneamente aos conjuntos abaixo-da-margem e precificação-incompleta.
- **CA30:** a coluna Precificação é exibida nas duas tabelas.
- **CA31:** Produto completo é identificado por texto/semântica como Completa.
- **CA32:** Produto incompleto exibe todos os motivos aplicáveis de forma acessível.
- **CA33:** coluna Situação de `/Produtos` continua significando Ativo/Inativo.
- **CA34:** cards existentes do Dashboard mantêm semântica e contagens globais.
- **CA35:** `Margem indisponível` não é renomeada para Precificação incompleta.
- **CA36:** filtro incompleto sem resultado possui mensagem específica.
- **CA37:** ordenação existente por Nome normalizado é preservada.
- **CA38:** isolamento tenant-aware permanece.
- **CA39:** nenhuma escrita é realizada pelos GETs.
- **CA40:** nenhuma migration/ModelSnapshot é alterado.
- **CA41:** `ResumoPrecificacaoProdutosAtual.CalcularAsync` é chamado no máximo uma vez por request/superfície.
- **CA42:** não existe chamada individual de `PrecificacaoProdutoAtual` em loop.
- **CA43:** não existe segundo cálculo em lote após o recorte.
- **CA44:** fórmulas financeiras existentes não são duplicadas nem alteradas.
- **CA45:** build Release fica verde.
- **CA46:** unitários completos ficam verdes.
- **CA47:** integração SQL Server/Web fica verde.

## Matriz mínima de testes

### Classificador de completude

- tudo disponível => Completa, zero motivos;
- Ficha ausente => Incompleta + FichaTecnicaAusente;
- Ficha sem Itens => Incompleta + FichaTecnicaSemItens;
- um Item sem preço vigente => Incompleta + InsumoSemPrecoVigente;
- configuração ausente => Incompleta + ConfiguracaoPrecificacaoAusente;
- uso elétrico + tarifa nula => Incompleta + TarifaEnergiaNaoConfigurada;
- sem uso elétrico + tarifa nula => não gerar motivo de tarifa;
- incremento nulo, demais dados completos => Incompleta + IncrementoComercialNaoConfigurado;
- preço de prateleira ausente => Incompleta + PrecoPrateleiraNaoDefinido;
- múltiplas faltas => todos os motivos independentes, sem duplicidade e em ordem estável.

### Distinção margem x completude

Caso obrigatório:

~~~text
custo conhecido
preço de prateleira conhecido
margem abaixo da meta
incremento comercial nulo
~~~

Esperado:

~~~text
SituacaoMargem = AbaixoDaMargem
PrecificacaoCompleta = false
motivo = IncrementoComercialNaoConfigurado
~~~

O Produto deve aparecer tanto no recorte abaixo da margem quanto no recorte precificação incompleta, em requests separados.

### Dashboard

- sem filtro => todos os ativos;
- abaixo-da-margem => comportamento UC029 preservado;
- precificacao-incompleta => somente ativos incompletos;
- completo não aparece no recorte incompleto;
- inativo incompleto não aparece no Dashboard;
- filtro incompleto sem resultados => mensagem específica;
- filtro inválido => zero linhas + mensagem + Mostrar todos;
- controles possuem estado semântico correto;
- cards permanecem idênticos entre todos os recortes;
- coluna Precificação apresenta estado/motivos.

### Lista de Produtos

- sem filtro => ativos e inativos;
- abaixo-da-margem => regressão UC029;
- precificacao-incompleta => somente ativos incompletos;
- Produto inativo incompleto fica fora do recorte;
- q + categoria + incompleto => interseção AND;
- remover só o recorte preserva q/categoria;
- Limpar filtros remove tudo;
- coluna Situação permanece Ativo/Inativo;
- coluna Precificação apresenta estado/motivos;
- filtro inválido => zero resultados + Limpar filtros.

### Equivalência individual x lote

Para cenários representativos, comparar o estado derivado por:

~~~text
PrecificacaoProdutoAtual
ResumoPrecificacaoProdutosAtual
~~~

e confirmar mesma completude/motivos.

### Multiempresa

- Empresa A e B com Produtos incompletos por motivos distintos;
- Dashboard/Produtos de A classificam somente dados de A;
- preços/configurações/Fichas de B não resolvem pendências de A.

### Estrutural/performance

Revisar explicitamente:

- uma única chamada do resumo em lote por request;
- nenhuma chamada individual por Produto;
- nenhum segundo resumo após filtrar;
- nenhum N+1 introduzido para motivos;
- nenhuma escrita;
- nenhuma migration.

## Arquivos esperados

~~~text
src/Precificador.Core/Precificacao/... (classificador/motivos, se extração compartilhada for adotada)
src/Precificador.Web/Precificacao/PrecificacaoProdutoAtual.cs
src/Precificador.Web/Precificacao/ResumoPrecificacaoProdutosAtual.cs
src/Precificador.Web/Pages/Dashboard/Index.cshtml.cs
src/Precificador.Web/Pages/Dashboard/Index.cshtml
src/Precificador.Web/Pages/Produtos/Index.cshtml.cs
src/Precificador.Web/Pages/Produtos/Index.cshtml
tests/Precificador.Tests.Unit/Precificacao/...
tests/Precificador.Tests.Integration/Infrastructure/PrecificacaoProdutoAtualTests.cs
tests/Precificador.Tests.Integration/Web/DashboardPageTests.cs
tests/Precificador.Tests.Integration/Web/ListarConsultarProdutosPageTests.cs
docs/features/F005-dashboard-margens.md
docs/features/F002-produtos.md
docs/use-cases/UC008-listar-consultar-produtos.md
docs/development/backlog.md
docs/use-cases/catalog.md
~~~

Não há alteração esperada de schema.

## Fora do escopo

- persistir situação de completude;
- persistir motivos;
- nova página de Dashboard;
- novo card obrigatório de Precificação incompleta;
- combinar simultaneamente múltiplos valores de `filtro`;
- filtro por motivo específico;
- pesquisar texto dos motivos;
- listar nominalmente todos os Insumos sem preço no Dashboard/listagem;
- corrigir automaticamente pendências;
- criar Ficha/configuração/preço automaticamente;
- alterar fórmulas de custo, preço ou margem;
- paginação;
- ordenação configurável;
- exportação;
- alertas;
- monitoramento em background;
- Continuous Deployment.

## Definition of Done

UC030 está concluída quando:

- completude global está explicitamente definida e distinta de Situação da margem;
- motivos estruturados cobrem as causas atuais de incompletude;
- classificação individual e em lote permanece equivalente;
- Dashboard oferece o terceiro recorte Precificação incompleta;
- `/Produtos` oferece o mesmo recorte sem perder `q`/Categoria;
- ambas as tabelas tornam estado/motivos compreensíveis;
- filtros anteriores continuam funcionando;
- cards de margem mantêm a semântica global existente;
- multiempresa permanece isolada;
- processamento permanece em lote, sem N+1;
- GETs continuam sem persistência;
- nenhuma migration é criada;
- documentação está alinhada;
- build/testes completos estão verdes;
- backlog marca UC030 como Concluído após implementação.

## Branch de implementação sugerida

~~~text
feat/uc030-precificacao-incompleta
~~~

## Commit sugerido

~~~text
feat: identifica precificacao incompleta
~~~
