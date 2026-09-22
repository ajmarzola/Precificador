
# MEL024 — Enriquecer listagem de Produtos com filtro por Categoria e indicadores atuais

- **Origem:** testes manuais / adaptação do usuário que hoje trabalha com planilha.
- **Classificação:** UX / consulta operacional / desempenho de leitura.
- **Prioridade:** alta.
- **Estado:** Pronto.
- **Ordem na fila pendente:** 13.
- **Dependências:** UC008, UC012, UC022, UC024, UC032 e UC036 concluídas.
- **Gate operacional:** liberado por decisão explícita para implementação imediata, mesmo com MEL021 bloqueada externamente; demais novas UCs permanecem pausadas.
- **Alteração de regra de negócio:** não.
- **Alteração de domínio/schema:** não.
- **Migration:** não.
- **Persistência de valores calculados:** não.

## Objetivo

Evoluir /Produtos para aproximar a experiência da planilha usada atualmente no dia a dia.

A listagem deve manter pesquisa por Nome e passar a:

1. filtrar por Categoria;
2. exibir Custo unitário atual;
3. exibir Preço de prateleira vigente;
4. exibir Margem atual.

Os três indicadores devem usar exclusivamente as regras existentes de UC012, UC022, UC024 e UC036.

## Resultado esperado

Grid:

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

Exemplo:

~~~text
Agenda 2027 | Agenda        | R$ 18,35     | R$ 35,00 | 47,57%       | 40% | Ativo   | Consultar
Pão rústico | Pães Rústicos | R$ 9,80      | R$ 18,00 | 45,56%       | 50% | Ativo   | Consultar
Produto X   | —             | indisponível | —        | indisponível | 35% | Inativo | Consultar
~~~

Não adicionar Preço teórico, Preço sugerido, data do preço, situação da margem, paginação ou ordenação configurável.

## Evolução do UC008

O UC008 originalmente proíbe filtro por Categoria, custo, preço e margem atual na listagem.

MEL024 é a evolução formal dessas restrições.

Na implementação, UC008 deve registrar que:

- pesquisa textual continua somente por Nome;
- Categoria passa a possuir filtro estruturado próprio;
- listagem passa a expor indicadores atuais;
- detalhes do Produto não precisam mudar;
- valores derivados continuam sem persistência.

## Filtro por Categoria

Adicionar seletor GET:

~~~text
Categoria
Todas as categorias
Sem categoria
<Categorias da Empresa Ativa>
~~~

Ordenar Categorias por NomeNormalizado.

Incluir Categorias ativas e inativas. Categoria inativa deve aparecer como:

~~~text
Nome da categoria (inativa)
~~~

Categoria inativa continua filtrável porque Produtos existentes podem permanecer vinculados a ela.

### Query string

Usar parâmetro:

~~~text
categoria
~~~

Semântica:

~~~text
ausente/vazio
=> todas as categorias

sem-categoria
=> CategoriaProdutoId == null

inteiro positivo
=> CategoriaProdutoId == valor
~~~

Exemplos:

~~~text
/Produtos?q=agenda
/Produtos?categoria=3
/Produtos?q=agenda&categoria=3
/Produtos?categoria=sem-categoria
~~~

Não usar ID negativo como sentinela.

Valor inválido, zero ou negativo:

- HTTP 200;
- não ampliar silenciosamente para Todas;
- retornar zero Produtos;
- permitir Limpar filtros.

ID de Categoria inexistente ou cross-tenant também retorna zero Produtos sem revelar a Categoria externa.

## Combinação com pesquisa

Pesquisa q continua:

~~~text
NomeNormalizado.Contains(consultaNormalizada)
~~~

Categoria não entra na busca textual.

Quando q e categoria existirem:

~~~text
q AND categoria
~~~

## Estado dos filtros

Preservar no GET:

- texto pesquisado;
- Categoria selecionada.

Quando houver qualquer filtro, mostrar:

~~~text
Limpar filtros
~~~

destino /Produtos.

Sem Produtos e sem filtros:

~~~text
Não há produtos cadastrados para a empresa ativa.
~~~

Com filtros e zero resultados:

~~~text
Nenhum produto encontrado para os filtros informados.
~~~

## Custo unitário atual

Usar CustoUnitarioProduto atual de UC022.

Após UC036:

~~~text
CustoLote =
    CustoBaseItens
  + CustoPerdasLote
  + CustoMaoDeObraLote
  + CustoEnergiaLote
  + CustoDesgasteEquipamentosLote
~~~

Não usar RegistroPrecoProduto.CustoReferencia.

Não copiar fórmula para o PageModel.

Apresentação:

~~~text
null => indisponível
0    => R$ 0,00
~~~

Valores monetários seguem MEL015.

## Preço de prateleira vigente

Usar o RegistroPrecoProduto atual conforme UC012:

~~~text
DataReferencia DESC
Id DESC
Primeiro registro
~~~

Exibir somente PrecoPrateleira.

Não usar PrecoSugerido, CustoReferencia ou snapshot diferente do registro vigente.

Apresentação:

~~~text
null => —
~~~

Criar seleção em lote equivalente à regra de SelecionarAtualAsync. Não chamar SelecionarAtualAsync Produto por Produto.

## Margem atual

Usar CalculadoraMargemAtual e UC024:

~~~text
MargemAtual =
    (PrecoPrateleiraAtual - CustoUnitarioProduto)
    / PrecoPrateleiraAtual
~~~

Não duplicar fórmula.

Apresentação:

~~~text
null => indisponível
~~~

Preservar margem zero, 100% e negativa.

Usar percentual pt-BR com até duas casas decimais, sem arredondar antes do cálculo.

Não usar MargemReferencia histórica.

## Margem-alvo e Situação

Preservar as colunas atuais.

Situação continua significando:

~~~text
Ativo
Inativo
~~~

Não substituir por SituacaoMargem.

Produtos inativos continuam listados, filtráveis e calculáveis.

## Arquitetura — evitar N+1

PrecificacaoProdutoAtual é adequada para um Produto, mas não deve ser chamada em loop.

É proibido:

~~~text
foreach produto
    await PrecificacaoProdutoAtual.CalcularAsync(produto.Id)
~~~

Criar serviço em lote, nome sugerido:

~~~text
ResumoPrecificacaoProdutosAtual
~~~

Contrato conceitual:

~~~text
CalcularAsync(produtoIds)
=> mapa ProdutoId -> ResumoPrecificacaoProdutoAtual
~~~

Resultado mínimo:

~~~text
ProdutoId
CustoUnitarioProduto?
PrecoPrateleiraAtual?
MargemAtual?
SituacaoMargem
~~~

SituacaoMargem pode ser retornada para reuso futuro, mas não precisa ser coluna.

## Estratégia de carga em lote

A quantidade de consultas deve depender dos conjuntos de dados, não da quantidade de Produtos.

Carregar em conjuntos:

1. Produtos e configuração de desgaste da Categoria;
2. ConfiguracaoPrecificacaoEmpresa uma vez;
3. Fichas dos ProdutoIds;
4. Itens das Fichas;
5. preços vigentes dos Insumos usando SelecionarVigentesAsync uma única vez;
6. UsosEquipamentosFicha;
7. registros atuais de preço dos Produtos em lote.

Agrupar em memória e usar as calculadoras do Core.

Não exigir número exato de comandos SQL, mas o número de round-trips não pode crescer proporcionalmente a N Produtos.

Usar AsNoTracking e GQFs normais.

Evitar Include cartesiano.

Não calcular PrecoTeorico/PrecoSugerido apenas para alimentar o grid.

## Reuso das calculadoras

Usar:

- CalculadoraCustoItens;
- CalculadoraCustoPerdas;
- CalculadoraCustoMaoDeObra;
- CalculadoraCustoEnergia;
- CalculadoraCustoDesgasteEquipamentos;
- CalculadoraCustoProduto;
- CalculadoraMargemAtual.

As fórmulas permanecem exclusivamente no Core.

## Paridade

Para o mesmo Produto e mesma data operacional, o serviço em lote deve devolver os mesmos:

~~~text
CustoUnitarioProduto
PrecoPrateleiraAtual
MargemAtual
SituacaoMargem
~~~

que PrecificacaoProdutoAtual.CalcularAsync(produtoId).

Adicionar testes de paridade.

Não é obrigatório refatorar a orquestração individual para delegar ao lote se isso piorar a legibilidade.

## Preço atual de Produto em lote

Evoluir RegistroPrecoProdutoConsultas com método equivalente a:

~~~text
SelecionarAtuaisAsync(produtoIds)
~~~

Regras:

- somente IDs solicitados;
- agrupar por ProdutoId;
- DataReferencia DESC;
- Id DESC;
- primeiro por Produto;
- zero ou um registro por Produto;
- GQF normal;
- lista vazia retorna vazio sem consulta desnecessária.

## Incompletude

### Sem Ficha

~~~text
CustoUnitarioProduto = null
MargemAtual = null
~~~

Preço de prateleira pode continuar visível.

### Ficha sem Itens ou item sem preço

~~~text
CustoUnitarioProduto = null
MargemAtual = null
~~~

### Energia incompleta

Preservar UC021/MEL023.

### Desgaste

Preservar UC036:

~~~text
sem Categoria => desgaste 0
Categoria inativa vinculada => regra continua aplicada
~~~

### Configuração da Empresa ausente

A página continua HTTP 200:

~~~text
custo = indisponível
margem = indisponível
preço vigente = exibido se existir
~~~

Não criar configuração silenciosamente.

## Multiempresa

Preservar FT002.

- Produtos, Categorias, preços, Fichas, Itens e equipamentos usam GQF;
- não usar IgnoreQueryFilters no runtime;
- não receber EmpresaId do request;
- Categoria cross-tenant não retorna dados;
- indicador de outra Empresa nunca entra no resumo.

## Leitura somente

GET /Produtos não pode criar ou alterar:

- Produto;
- Ficha;
- Configuração;
- RegistroPrecoProduto;
- custo;
- margem.

Nenhum resultado derivado é persistido.

## Performance

Não adicionar paginação nesta MEL.

Ainda assim:

- sem N+1;
- projeções enxutas;
- deduplicar InsumoIds;
- consultas em lote;
- sem cálculos desnecessários.

Se o catálogo exigir paginação futuramente, criar item próprio.

## Apresentação

O grid fica mais largo. Usar contêiner responsivo Bootstrap:

~~~text
table-responsive
~~~

Não ocultar os três indicadores em desktop.

## Documentação obrigatória na implementação

Atualizar:

- MEL024;
- backlog;
- catálogo de melhorias;
- UC008;
- F002;
- F004, se necessário para registrar a nova superfície de indicadores.

Após implementação:

~~~text
MEL024 = Concluído
UC028 = Planejado
~~~

MEL021 continua bloqueada externamente e não é implementada junto com MEL024.

## Critérios de aceitação

- **CA01:** /Produtos continua tenant-aware.
- **CA02:** pesquisa por Nome continua conforme UC008.
- **CA03:** existe filtro por Categoria.
- **CA04:** q + Categoria usa AND.
- **CA05:** existem Todas as categorias e Sem categoria.
- **CA06:** Categorias ativas e inativas são filtráveis.
- **CA07:** Categoria inativa é identificada no seletor.
- **CA08:** filtro inválido/cross-tenant não amplia nem vaza dados.
- **CA09:** filtros permanecem preenchidos.
- **CA10:** Limpar filtros retorna à listagem completa.
- **CA11:** estado vazio filtrado difere do catálogo vazio.
- **CA12:** grid exibe Custo unitário atual.
- **CA13:** custo inclui UC036.
- **CA14:** custo incompleto exibe indisponível.
- **CA15:** custo zero exibe R$ 0,00.
- **CA16:** grid exibe Preço de prateleira vigente.
- **CA17:** preço usa DataReferencia DESC + Id DESC.
- **CA18:** preço ausente exibe travessão.
- **CA19:** grid exibe Margem atual.
- **CA20:** margem usa CalculadoraMargemAtual.
- **CA21:** margem incompleta exibe indisponível.
- **CA22:** margem negativa não é truncada.
- **CA23:** Margem-alvo permanece.
- **CA24:** Situação Ativo/Inativo permanece.
- **CA25:** Produto inativo continua calculável.
- **CA26:** preço pode aparecer mesmo com custo incompleto.
- **CA27:** nenhum indicador é persistido.
- **CA28:** snapshot histórico não é usado como custo atual.
- **CA29:** serviço em lote não executa orquestração individual por Produto.
- **CA30:** preço atual de Produtos é selecionado em lote.
- **CA31:** preços de Insumos reutilizam SelecionarVigentesAsync.
- **CA32:** lote possui paridade com PrecificacaoProdutoAtual.
- **CA33:** GQFs permanecem ativos.
- **CA34:** GET não persiste nada.
- **CA35:** nenhuma migration.
- **CA36:** grid é responsivo.
- **CA37:** build Release sem warnings novos relevantes.
- **CA38:** unitários verdes.
- **CA39:** integração SQL Server/Web verde.
- **CA40:** UC028+ não são antecipados.
- **CA41:** MEL021 não é implementada.

## Matriz mínima de testes

### Filtro

- vazio => todas;
- sem-categoria => somente CategoriaProdutoId null;
- ID positivo => Categoria correspondente;
- inválido/zero/negativo => filtro inválido.

### Persistência — preço atual em lote

- vários Produtos retornam no máximo um registro cada;
- DataReferencia mais recente vence;
- empate de data usa maior Id;
- Produto sem preço não retorna registro;
- GQF isola tenants;
- IDs vazios retornam vazio.

### Resumo em lote

- Produto completo tem paridade com orquestração individual;
- sem Categoria aplica desgaste zero;
- desgaste fixo e percentual afetam custo corretamente;
- Categoria inativa continua calculando;
- sem Ficha => custo/margem null e preço preservado;
- item sem preço => custo/margem null;
- energia incompleta => custo/margem null;
- preço ausente => margem null;
- margem negativa preservada;
- Produto inativo calculado;
- tenant externo não vaza.

### Web — filtro

- sem filtros;
- Categoria ativa;
- Categoria inativa;
- Sem categoria;
- q + Categoria;
- q continua somente Nome;
- categoria cross-tenant;
- categoria inválida;
- seleção preservada;
- indicação de inativa;
- Limpar filtros;
- estado vazio filtrado;
- catálogo vazio.

### Web — indicadores

- custo atual formatado;
- custo incompleto;
- custo zero;
- preço vigente;
- preço ausente;
- desempate de preço por Id;
- margem correta;
- margem negativa;
- margem incompleta;
- Margem-alvo preservada;
- Ativo/Inativo preservado;
- Produto inativo com indicadores;
- preço conhecido com custo incompleto;
- mesma linha não mistura dados de Produtos;
- GET não persiste.

### Performance estrutural

Revisar explicitamente que não há await de PrecificacaoProdutoAtual em loop.

Se a infraestrutura permitir DbCommandInterceptor de forma simples, validar que a quantidade de comandos não cresce linearmente com N Produtos. Não adicionar framework pesado apenas para isso.

## Arquivos esperados

~~~text
src/Precificador.Infrastructure/Persistence/RegistroPrecoProdutoConsultas.cs
src/Precificador.Web/Precificacao/ResumoPrecificacaoProdutosAtual.cs
src/Precificador.Web/Pages/Produtos/Index.cshtml.cs
src/Precificador.Web/Pages/Produtos/Index.cshtml
tests/Precificador.Tests.Integration/Infrastructure/*
tests/Precificador.Tests.Integration/Web/ListarConsultarProdutosPageTests.cs
~~~

## Fora do escopo

- migration;
- persistir custo/preço/margem;
- novo snapshot;
- alterar fórmulas;
- Preço teórico/sugerido no grid;
- data do preço no grid;
- filtro por Situação;
- filtro por margem;
- filtro por completude;
- ordenação clicável;
- paginação;
- exportação;
- gráficos;
- destaque visual por faixa;
- UC028/UC029/UC030;
- Azure/MEL021.

## Definition of Done

MEL024 está concluída quando:

- filtro por Categoria e pesquisa por Nome funcionam em conjunto;
- Todas/Sem categoria/Categorias ativas e inativas estão cobertas;
- grid mostra custo unitário, preço vigente e margem atual;
- semântica de null/zero está correta;
- valores seguem UC012/UC022/UC024/UC036;
- nenhum indicador é persistido;
- não existe N+1 por Produto;
- seleção de preço de Produto é em lote;
- preços de Insumo reutilizam consulta em lote;
- paridade com PrecificacaoProdutoAtual está testada;
- multiempresa permanece isolada;
- documentação está alinhada;
- não existe migration;
- build/testes completos verdes;
- UC028 continua Planejado;
- MEL021 não é implementada nesta PR.

## Branch de implementação

~~~text
feat/mel024-listagem-produtos-indicadores
~~~

## Commit sugerido

~~~text
feat: enriquece listagem de produtos com indicadores atuais
~~~
