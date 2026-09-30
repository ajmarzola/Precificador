# UC029 — Filtrar Produtos abaixo da margem

- **Funcionalidade:** F005 — Dashboard de Margens
- **Dependências funcionais:** UC028
- **Dependências operacionais concluídas:** MEL028
- **Estado:** Pronto
- **Alteração de schema:** não
- **Persistência de resultado:** não
- **Natureza:** consulta somente leitura
- **Regras principais:** RN022, RN023, RN027, RN036 e RN037

## Objetivo

Evoluir o Dashboard criado pela UC028 para permitir ao usuário concentrar a lista nos **Produtos ativos cuja Margem atual está abaixo da Margem-alvo**, preservando os indicadores globais da Empresa Ativa.

A UC029 não cria novo cálculo financeiro. O filtro usa exclusivamente a `SituacaoMargem` já derivada pela UC024/UC028:

~~~text
SituacaoMargem = AbaixoDaMargem
~~~

## Superfície

A UC029 evolui a página existente:

~~~text
GET /Dashboard
~~~

Não criar nova página.

O Dashboard continua protegido pela policy:

~~~text
EmpresaAtiva
~~~

e continua sendo a Home da Empresa.

## Modos da listagem

A partir da UC029 o Dashboard possui dois modos de listagem:

~~~text
Todos os ativos
Abaixo da margem
~~~

### Todos os ativos

É o comportamento padrão da UC028.

Exibe todos os Produtos ativos da Empresa Ativa, independentemente da situação de margem.

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

Usar parâmetro GET:

~~~text
filtro
~~~

Valor canônico da UC029:

~~~text
abaixo-da-margem
~~~

Semântica:

~~~text
/Dashboard
/Dashboard?filtro=
=> Todos os ativos

/Dashboard?filtro=abaixo-da-margem
=> somente SituacaoMargem.AbaixoDaMargem
~~~

Normalizar whitespace externo e comparação textual de forma não sensível a maiúsculas/minúsculas, mas sempre gerar links com o valor canônico em minúsculas.

### Valor inválido

Qualquer valor diferente dos suportados:

~~~text
/Dashboard?filtro=qualquer-outro
~~~

deve:

- responder HTTP 200;
- não ampliar silenciosamente para Todos os ativos;
- exibir zero linhas;
- informar que o filtro é inválido ou não suportado;
- oferecer ação para **Mostrar todos**.

Esse comportamento deixa a evolução futura explícita e evita que erros de URL sejam confundidos com a visão completa.

A UC030 poderá acrescentar um novo valor ao mesmo parâmetro; não antecipá-lo aqui.

## Controle visual do filtro

Acima da listagem, disponibilizar controles explícitos:

~~~text
Todos os ativos
Abaixo da margem
~~~

Recomendação de apresentação:

- links ou botões Bootstrap;
- estado selecionado visualmente identificável;
- seleção não pode depender somente de cor;
- opcionalmente exibir a contagem já disponível nos cards.

Destino:

~~~text
Todos os ativos
=> /Dashboard

Abaixo da margem
=> /Dashboard?filtro=abaixo-da-margem
~~~

O card **Abaixo da margem** pode também funcionar como atalho para o filtro, mas isso é opcional. O controle explícito da listagem é obrigatório.

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

Fluxo conceitual:

~~~text
1. carregar Produtos ativos da Empresa Ativa
2. calcular ResumoPrecificacaoProdutosAtual uma única vez para todos
3. montar os indicadores globais
4. aplicar filtro em memória sobre SituacaoMargem
5. renderizar a lista filtrada
~~~

É proibido executar um segundo cálculo em lote apenas para o subconjunto filtrado.

Também permanece proibido:

~~~text
foreach produto
    await PrecificacaoProdutoAtual.CalcularAsync(produto.Id)
~~~

A UC029 não necessita de nova consulta SQL proporcional ao número de Produtos.

## Ordenação

Preservar a ordenação definida na UC028:

~~~text
NomeNormalizado ASC
~~~

O filtro não altera a ordenação.

## Colunas

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

Exibir mensagem distinta, por exemplo:

~~~text
Filtro do Dashboard inválido.
~~~

e ação:

~~~text
Mostrar todos
~~~

## Multiempresa

Preservar FT002/RN036/RN037.

- filtro opera somente sobre Produtos já carregados sob GQF da Empresa Ativa;
- não receber `EmpresaId`;
- não usar `IgnoreQueryFilters`;
- contagens globais continuam tenant-aware;
- lista filtrada nunca inclui Produto de outra Empresa;
- query string não pode selecionar tenant.

## Leitura somente

GET `/Dashboard` continua sem persistência.

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

Após UC029, os filtros disponíveis são somente:

~~~text
Todos os ativos
Abaixo da margem
~~~

## Critérios de aceitação

- **CA01:** `/Dashboard` continua sendo a única superfície do F005.
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

## Matriz mínima de testes

### Filtro

- sem parâmetro => todos os ativos;
- `filtro=` => todos os ativos;
- `filtro=abaixo-da-margem` => somente abaixo;
- caixa diferente + whitespace externo => abaixo;
- valor inválido => zero linhas + mensagem + Mostrar todos.

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
tests/Precificador.Tests.Integration/Web/DashboardPageTests.cs
docs/features/F005-dashboard-margens.md
docs/development/backlog.md
docs/use-cases/catalog.md
~~~

Não há alteração esperada no Core, Infrastructure, schema ou migrations.

## Fora do escopo

- nova página de Dashboard;
- Produtos inativos;
- pesquisa textual;
- filtro por Categoria;
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
- query string possui semântica estável e testada;
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
