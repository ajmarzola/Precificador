# UC029 — Filtrar Produtos abaixo da margem

- **Funcionalidade principal:** F005 — Dashboard de Margens
- **Superfície relacionada:** F002 / UC008 — listagem de Produtos
- **Dependências funcionais:** UC028 e MEL024
- **Dependências operacionais concluídas:** MEL028
- **Estado:** Concluído
- **Alteração de schema:** não
- **Persistência de resultado:** não
- **Natureza:** consulta somente leitura
- **Regras principais:** RN022, RN023, RN027, RN036 e RN037

## Objetivo

Disponibilizar o mesmo recorte operacional de **Produtos ativos cuja Margem atual está abaixo da Margem-alvo** nas duas superfícies em que o usuário acompanha Produtos:

- Dashboard da Empresa;
- listagem de Produtos.

No Dashboard, o recorte preserva os indicadores globais da Empresa Ativa. Em `/Produtos`, ele se combina com a pesquisa por Nome e o filtro por Categoria já existentes.

A UC029 não cria novo cálculo financeiro. O filtro usa exclusivamente a `SituacaoMargem` já derivada pela UC024/UC028:

~~~text
SituacaoMargem = AbaixoDaMargem
~~~

## Superfícies

A UC029 evolui duas páginas existentes:

~~~text
GET /Dashboard
GET /Produtos
~~~

Não criar nova página.

O Dashboard continua protegido pela policy:

~~~text
EmpresaAtiva
~~~

e continua sendo a Home da Empresa.

`/Produtos` preserva o comportamento tenant-aware da UC008/MEL024 e continua sendo a listagem operacional completa, inclusive com Produtos inativos quando nenhum recorte de margem estiver selecionado.

## Modos de listagem

A partir da UC029, o Dashboard e a listagem de Produtos oferecem o recorte **Abaixo da margem**.

### Dashboard

Possui dois modos:

~~~text
Todos os ativos
Abaixo da margem
~~~

**Todos os ativos** é o comportamento padrão da UC028 e exibe todos os Produtos ativos da Empresa Ativa.

### Lista de Produtos

`/Produtos` preserva sua visão completa quando o parâmetro `filtro` estiver ausente/vazio: Produtos ativos e inativos continuam listáveis conforme UC008/MEL024.

Quando `filtro=abaixo-da-margem` estiver ativo, a lista mostra somente Produtos **ativos** classificados como abaixo da margem. Isso mantém o mesmo significado funcional do recorte nas duas superfícies.

### Abaixo da margem

Exibe somente Produtos ativos cujo resumo atual possua:

~~~text
SituacaoMargem == SituacaoMargemProduto.AbaixoDaMargem
~~~

Não incluir nesse filtro:

~~~text
DentroDaMargem
Incompleto
Produtos inativos
~~~

A igualdade exata:

~~~text
MargemAtual == MargemAlvo
~~~

continua sendo `DentroDaMargem` conforme RN023 e, portanto, não aparece no filtro abaixo da margem.

Margem negativa é válida e, quando inferior à Margem-alvo, aparece normalmente.

## Query string

Usar o mesmo parâmetro GET nas duas superfícies:

~~~text
filtro
~~~

Valor canônico da UC029:

~~~text
abaixo-da-margem
~~~

Semântica no Dashboard:

~~~text
/Dashboard
/Dashboard?filtro=
=> Todos os Produtos ativos

/Dashboard?filtro=abaixo-da-margem
=> somente Produtos ativos com SituacaoMargem.AbaixoDaMargem
~~~

Semântica na listagem de Produtos:

~~~text
/Produtos
/Produtos?filtro=
=> comportamento atual da UC008/MEL024

/Produtos?filtro=abaixo-da-margem
=> somente Produtos ativos com SituacaoMargem.AbaixoDaMargem
~~~

O parâmetro deve compor com os filtros já existentes:

~~~text
/Produtos?q=agenda&categoria=3&filtro=abaixo-da-margem
=> q AND categoria AND filtro
~~~

`q` continua pesquisando somente Nome e `categoria` mantém exatamente a semântica da MEL024.

Normalizar whitespace externo e comparar o valor de `filtro` sem sensibilidade a maiúsculas/minúsculas, mas sempre gerar links com o valor canônico em minúsculas.

### Valor inválido

Qualquer valor diferente dos suportados, em `/Dashboard` ou `/Produtos`, deve:

- responder HTTP 200;
- não ampliar silenciosamente para a visão sem filtro;
- exibir zero linhas;
- informar que o filtro é inválido ou não suportado;
- oferecer ação de recuperação:
  - **Mostrar todos** no Dashboard;
  - **Limpar filtros** em `/Produtos`.

Em `/Produtos`, `q` e `categoria` permanecem preenchidos enquanto o filtro de margem for válido. A ação **Limpar filtros** remove todos os filtros e retorna a `/Produtos`.

Esse comportamento deixa a evolução futura explícita e evita que erros de URL sejam confundidos com a visão completa.

A UC030 poderá acrescentar um novo valor ao mesmo parâmetro; não antecipá-lo aqui.

## Controle visual do filtro

### Dashboard

Acima da tabela, disponibilizar controles explícitos:

~~~text
Todos os ativos
Abaixo da margem
~~~

Destino:

~~~text
Todos os ativos
=> /Dashboard

Abaixo da margem
=> /Dashboard?filtro=abaixo-da-margem
~~~

O card **Abaixo da margem** pode também funcionar como atalho, mas isso é opcional.

### Lista de Produtos

Adicionar o recorte de margem junto aos controles existentes de pesquisa/Categoria, sem remover nem substituir esses controles.

Pode ser seletor, link ou botão, desde que:

- permita alternar entre visão sem recorte e **Abaixo da margem**;
- preserve `q` e `categoria` ao selecionar/remover apenas o recorte de margem;
- identifique o estado selecionado por texto/semântica, não apenas por cor;
- participe de `TemFiltros`/estado equivalente;
- **Limpar filtros** continue retornando a `/Produtos` sem `q`, `categoria` ou `filtro`.

Não adicionar coluna de `SituacaoMargem` à listagem nesta UC. A coluna **Situação** continua significando Ativo/Inativo.

## Indicadores do Dashboard não são filtrados

Os cards da UC028 continuam representando o universo completo de Produtos ativos da Empresa Ativa.

Aplicar filtro **não altera**:

~~~text
Produtos ativos
Insumos ativos
Abaixo da margem
Dentro da margem
Margem indisponível
~~~

Exemplo:

~~~text
Produtos ativos        10
Abaixo da margem        3

filtro = abaixo-da-margem

cards continuam:
Produtos ativos        10
Abaixo da margem        3

tabela:
3 Produtos
~~~

O filtro altera apenas a coleção exibida na tabela.

Isso preserva o Dashboard como visão-resumo e evita que os próprios indicadores mudem de significado após o usuário selecionar um recorte.

## Estratégia de cálculo

Preservar o processamento em lote introduzido pela MEL024 e reutilizado pela UC028.

### Dashboard

Fluxo conceitual:

~~~text
1. carregar Produtos ativos da Empresa Ativa
2. calcular ResumoPrecificacaoProdutosAtual uma única vez para todos
3. montar os indicadores globais
4. aplicar filtro em memória sobre SituacaoMargem
5. renderizar a lista filtrada
~~~

Os IDs não podem ser reduzidos pelo filtro de margem antes do resumo, porque os cards precisam continuar representando o universo global de Produtos ativos.

### Lista de Produtos

Preservar primeiro os filtros cadastrais já existentes:

~~~text
q
categoria
~~~

Depois:

~~~text
1. carregar os Produtos candidatos da Empresa Ativa conforme q/categoria
2. se filtro=abaixo-da-margem, excluir inativos do conjunto candidato
3. calcular ResumoPrecificacaoProdutosAtual uma única vez para os candidatos
4. aplicar SituacaoMargem.AbaixoDaMargem em memória
5. renderizar mantendo a ordenação existente
~~~

Sem `filtro`, o comportamento/performance da MEL024 permanece: o resumo em lote alimenta os indicadores da listagem, inclusive para Produtos inativos.

Filtro inválido pode encerrar com coleção vazia sem cálculo desnecessário.

Em nenhuma superfície executar um segundo cálculo em lote para o subconjunto já classificado.

Também permanece proibido:

~~~text
foreach produto
    await PrecificacaoProdutoAtual.CalcularAsync(produto.Id)
~~~

A UC029 não deve introduzir consulta SQL proporcional ao número de Produtos.

## Ordenação

Preservar a ordenação definida na UC028:

~~~text
NomeNormalizado ASC
~~~

O filtro não altera a ordenação.

## Colunas

### Dashboard

Preservar integralmente a tabela da UC028:

~~~text
Produto
Custo unitário atual
Preço de prateleira atual
Margem atual
Margem-alvo
Preço sugerido
Situação da margem
Consultar
~~~

### Lista de Produtos

Preservar integralmente a tabela da UC008/MEL024:

~~~text
Nome
Categoria
Custo unitário atual
Preço de prateleira vigente
Margem atual
Margem-alvo
Situação
Consultar
~~~

Não adicionar/remover colunas nesta UC.

## Estado vazio

### Empresa sem Produtos ativos

Sem Produtos ativos e sem filtro:

~~~text
Não há Produtos ativos para acompanhar.
~~~

ou texto equivalente já existente.

Os cards ficam zerados conforme UC028.

### Filtro abaixo da margem sem resultados

Quando existem Produtos ativos, mas nenhum está abaixo da margem:

~~~text
Nenhum Produto ativo está abaixo da margem.
~~~

Não usar a mensagem de catálogo vazio.

Os indicadores globais permanecem visíveis.

### Filtro inválido

No Dashboard, exibir mensagem distinta, por exemplo:

~~~text
Filtro do Dashboard inválido.
~~~

e ação **Mostrar todos**.

Em `/Produtos`, exibir mensagem de filtro inválido/não suportado e ação **Limpar filtros**.

### Lista de Produtos sem resultado

Quando `q`, `categoria` e/ou `filtro=abaixo-da-margem` produzirem zero resultados, preservar o conceito de estado vazio filtrado da MEL024, sem confundir com catálogo vazio.

## Multiempresa

Preservar FT002/RN036/RN037.

- filtro opera somente sobre Produtos já carregados sob GQF da Empresa Ativa;
- não receber `EmpresaId`;
- não usar `IgnoreQueryFilters`;
- contagens globais continuam tenant-aware;
- lista filtrada nunca inclui Produto de outra Empresa;
- query string não pode selecionar tenant.

## Leitura somente

GET `/Dashboard` e GET `/Produtos` continuam sem persistência.

O filtro não pode:

- alterar Produto;
- alterar Margem-alvo;
- registrar preço;
- criar configuração;
- persistir situação;
- persistir seleção do filtro no banco.

Não criar migration.

## Relação com UC030

UC030 adicionará a identificação da **precificação globalmente incompleta** e o terceiro recorte do F005.

A UC029 não deve:

- interpretar `SituacaoMargem.Incompleto` como precificação globalmente incompleta;
- adicionar filtro `precificacao-incompleta`;
- calcular motivos de incompletude;
- alterar a semântica de `Margem indisponível`.

Após UC029, o recorte de margem disponível nas duas superfícies é somente:

~~~text
Abaixo da margem
~~~

No Dashboard, ele aparece ao lado de **Todos os ativos**. Em `/Produtos`, a ausência de `filtro` preserva a listagem normal da UC008/MEL024.

## Critérios de aceitação

- **CA01:** UC029 atua em `/Dashboard` e `/Produtos`, sem criar nova página.
- **CA02:** Dashboard continua exigindo Empresa Ativa.
- **CA03:** sem `filtro`, todos os Produtos ativos são exibidos.
- **CA04:** `filtro=abaixo-da-margem` exibe somente `SituacaoMargem.AbaixoDaMargem`.
- **CA05:** Produto `DentroDaMargem` não aparece no filtro.
- **CA06:** Produto `Incompleto` não aparece no filtro.
- **CA07:** Produto inativo não aparece em nenhum modo.
- **CA08:** igualdade `MargemAtual == MargemAlvo` não entra no filtro.
- **CA09:** margem negativa abaixo da meta entra no filtro.
- **CA10:** valor vazio de `filtro` equivale à visão completa.
- **CA11:** comparação do valor suportado tolera caixa/whitespace externo.
- **CA12:** links gerados usam `abaixo-da-margem` canônico.
- **CA13:** filtro inválido responde 200 sem ampliar para Todos.
- **CA14:** filtro inválido exibe mensagem e opção Mostrar todos.
- **CA15:** controles Todos os ativos/Abaixo da margem estão visíveis.
- **CA16:** estado selecionado é identificável sem depender apenas de cor.
- **CA17:** cards continuam com valores globais quando o filtro está ativo.
- **CA18:** `Produtos ativos` não passa a contar somente a lista filtrada.
- **CA19:** `Abaixo da margem` continua representando todos os Produtos ativos abaixo da margem.
- **CA20:** demais cards também permanecem globais.
- **CA21:** `ResumoPrecificacaoProdutosAtual.CalcularAsync` é chamado no máximo uma vez por request.
- **CA22:** o cálculo em lote recebe o universo de Produtos ativos, não somente o subconjunto filtrado.
- **CA23:** não existe chamada individual de `PrecificacaoProdutoAtual` por Produto.
- **CA24:** ordenação por Nome normalizado é preservada.
- **CA25:** colunas/valores atuais da UC028 são preservados.
- **CA26:** zero Produtos ativos mantém estado vazio da UC028.
- **CA27:** filtro abaixo da margem sem resultados usa mensagem específica.
- **CA28:** isolamento tenant-aware permanece.
- **CA29:** GET não persiste alterações.
- **CA30:** nenhuma migration/ModelSnapshot é alterado.
- **CA31:** UC030 não é antecipada.
- **CA32:** build Release fica verde.
- **CA33:** unitários completos ficam verdes.
- **CA34:** integração SQL Server/Web fica verde.
- **CA35:** `/Produtos` sem `filtro` preserva a listagem atual, inclusive Produtos inativos.
- **CA36:** `/Produtos?filtro=abaixo-da-margem` exibe somente Produtos ativos com `SituacaoMargem.AbaixoDaMargem`.
- **CA37:** Produto inativo abaixo da margem permanece fora desse recorte.
- **CA38:** `q`, `categoria` e `filtro` combinam por AND.
- **CA39:** selecionar/remover somente o recorte de margem preserva `q` e `categoria`.
- **CA40:** `filtro` participa do estado de filtros de `/Produtos` e **Limpar filtros** remove todos.
- **CA41:** filtro inválido em `/Produtos` responde 200, retorna zero linhas e oferece **Limpar filtros**.
- **CA42:** a tabela de `/Produtos` mantém colunas e significado de **Situação** definidos pela MEL024.
- **CA43:** `/Produtos` usa o resumo em lote sem chamada individual por Produto.

## Matriz mínima de testes

### Filtro — Dashboard

- sem parâmetro => todos os ativos;
- `filtro=` => todos os ativos;
- `filtro=abaixo-da-margem` => somente abaixo;
- caixa diferente + whitespace externo => abaixo;
- valor inválido => zero linhas + mensagem + Mostrar todos.

### Filtro — Lista de Produtos

- sem `filtro` => comportamento atual da UC008/MEL024, inclusive inativos;
- `filtro=` => comportamento atual;
- `filtro=abaixo-da-margem` => somente ativos abaixo;
- Produto inativo abaixo => fora do recorte;
- `q + categoria + filtro` => interseção por AND;
- remover apenas filtro de margem preserva `q/categoria`;
- filtro inválido => zero linhas + mensagem + Limpar filtros;
- catálogo vazio sem filtros continua distinto de resultado filtrado vazio.

### Classificação

Em uma mesma Empresa:

- Produto abaixo da margem => aparece;
- Produto dentro da margem => não aparece;
- igualdade com meta => não aparece;
- margem negativa abaixo da meta => aparece;
- custo ausente => `Incompleto`, não aparece;
- preço de prateleira ausente => `Incompleto`, não aparece;
- Produto inativo abaixo da margem => não aparece.

### Indicadores globais

Com filtro ativo:

- Produtos ativos mantém total global;
- Abaixo da margem mantém total global;
- Dentro da margem mantém total global;
- Margem indisponível mantém total global;
- Insumos ativos mantém total global;
- quantidade de linhas pode ser menor que Produtos ativos.

### Estado vazio

- Empresa sem Produto ativo;
- Empresa com Produtos ativos, nenhum abaixo;
- Empresa com apenas Produtos incompletos;
- filtro inválido.

### Multiempresa

- Empresa A possui Produto abaixo;
- Empresa B possui Produto abaixo;
- Dashboard de A mostra/conta somente A;
- parâmetro `filtro` não altera tenant.

### Estrutural/performance

Revisar explicitamente:

- uma única chamada de `ResumoPrecificacaoProdutosAtual.CalcularAsync`;
- nenhum `PrecificacaoProdutoAtual.CalcularAsync` em loop;
- nenhum segundo processamento para o subconjunto filtrado;
- nenhuma escrita no GET.

## Arquivos esperados

~~~text
src/Precificador.Web/Pages/Dashboard/Index.cshtml.cs
src/Precificador.Web/Pages/Dashboard/Index.cshtml
src/Precificador.Web/Pages/Produtos/Index.cshtml.cs
src/Precificador.Web/Pages/Produtos/Index.cshtml
tests/Precificador.Tests.Integration/Web/DashboardPageTests.cs
tests/Precificador.Tests.Integration/Web/ListarConsultarProdutosPageTests.cs
docs/features/F005-dashboard-margens.md
docs/features/F002-produtos.md
docs/use-cases/UC008-listar-consultar-produtos.md
docs/development/backlog.md
docs/use-cases/catalog.md
~~~

Não há alteração esperada no Core, Infrastructure, schema ou migrations.

## Fora do escopo

- nova página de Dashboard;
- incluir Produtos inativos no recorte abaixo da margem; eles continuam visíveis em `/Produtos` sem esse recorte;
- alterar a semântica da pesquisa textual;
- criar novo filtro por Categoria no Dashboard; `/Produtos` preserva o filtro existente;
- filtro `Dentro da margem`;
- filtro `Margem indisponível`;
- filtro de precificação globalmente incompleta — UC030;
- motivos de incompletude;
- paginação;
- ordenação configurável;
- gráficos;
- tendências;
- exportação;
- alertas;
- persistência da seleção;
- alteração das fórmulas de custo/preço/margem;
- Continuous Deployment.

## Definition of Done

UC029 está concluída quando:

- Dashboard oferece Todos os ativos e Abaixo da margem;
- `/Produtos` também oferece o recorte Abaixo da margem sem perder pesquisa/Categoria;
- query string possui semântica estável e testada nas duas superfícies;
- filtro usa exclusivamente `SituacaoMargem.AbaixoDaMargem`;
- cards permanecem globais;
- lista filtrada preserva ordenação e colunas da UC028;
- estados vazios são distintos;
- multiempresa permanece isolada;
- processamento continua em lote, uma vez por request;
- não existe persistência nem migration;
- UC030 permanece não implementada;
- documentação está alinhada;
- build/testes completos estão verdes;
- backlog marca UC029 como Concluído.

## Branch de implementação sugerida

~~~text
feat/uc029-filtro-abaixo-margem
~~~

## Commit sugerido

~~~text
feat: adiciona filtro abaixo da margem no dashboard
~~~
