# Instrução Codex — UC034 Vincular Produtos a Coleções

Você está implementando a UC034 — Vincular Produtos a Coleções no repositório ajmarzola/Precificador.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/use-cases/UC034-vincular-produtos-colecoes.md;
3. docs/use-cases/UC033-administrar-colecoes.md;
4. docs/use-cases/UC032-administrar-categorias-produto.md;
5. docs/business/business-rules.md, especialmente RN080–RN083;
6. docs/features/F002-produtos.md;
7. docs/development/testing-strategy.md;
8. docs/development/definition-of-done.md;
9. implementação atual de Produto, ColecaoProduto, ColecaoProdutoCategoria, PrecificadorDbContext, /Produtos e /Produtos/Colecoes.

A especificação normativa é:

~~~text
docs/use-cases/UC034-vincular-produtos-colecoes.md
~~~

## Branch

Use:

~~~text
feat/uc034-vincular-produtos-colecoes
~~~

Parta da master após o merge da documentação UC034.

Não implemente na branch documental.

## Objetivo

Entregar relação N:N Produto-Coleção com:

- Destaque por vínculo;
- gerenciamento por Coleção;
- exibição no detalhe do Produto;
- filtro por Coleção em /Produtos.

Não criar vigência própria no vínculo.

## Modelo

Criar:

~~~text
ProdutoColecao : IEntidadeEmpresa
~~~

Campos:

~~~text
EmpresaId
ProdutoId
ColecaoProdutoId
Destaque
~~~

PK:

~~~text
ProdutoId + ColecaoProdutoId
~~~

Não adicionar ColecaoProdutoId em Produto.

Não adicionar datas ao vínculo.

## Período

ProdutoColecao herda integralmente:

~~~text
ColecaoProduto.DataLancamento
ColecaoProduto.DataFinalizacao
~~~

Não criar:

- DataInicioParticipacao;
- DataFimParticipacao;
- Ativo do vínculo.

Se a Coleção mudar de período, todos os vínculos mudam de período efetivo junto.

## Destaque

Bool no vínculo.

Não exclusivo.

Sem limite.

Não participa de precificação.

Método de domínio simples para alternar destaque.

## Categoria

Não validar compatibilidade entre Categoria do Produto e Categorias da Coleção.

Produto sem Categoria pode vincular.

Produto de Categoria fora da Coleção pode vincular.

Remover Categoria da Coleção não remove ProdutoColecao.

## Produto ativo/inativo

Permitir ambos.

O objetivo inclui correção/histórico de Coleções passadas.

Rotular inativo na UI.

Desativar Produto não altera vínculo.

## Persistência

Adicionar:

~~~text
DbSet<ProdutoColecao>
GQF
guard tenant central
~~~

Validar same-tenant para Produto e Coleção usando ownership real/IgnoreQueryFilters.

Tabela:

~~~text
ProdutosColecoes
~~~

PK composta.

FKs Empresa, Produto e Coleção com Restrict.

Índices:

~~~text
EmpresaId + ColecaoProdutoId + Destaque
EmpresaId + ProdutoId
~~~

Sem cascade.

## Migration

Nova migration após UC033.

Sem backfill.

Não alterar schema de Produto/Colecao/Categoria/precificação.

Testar clean DB e upgrade pós-UC033.

## Gerenciamento por Coleção

Criar:

~~~text
/Produtos/Colecoes/Produtos/{id}
~~~

GET + handlers POST:

- Vincular;
- DefinirDestaque;
- Desvincular.

id = ColecaoProduto.Id.

EmpresaAtiva já é a autorização da área /Produtos.

Não usar AdministradorEmpresa.

### GET

Exibir dados da Coleção e tabela de vínculos.

Tabela:

- Produto;
- Categoria;
- Ativo/Inativo;
- Destaque;
- ações.

Ordenar Destaque DESC + Produto.NomeNormalizado ASC.

### Vincular

Input:

~~~text
ProdutoId
Destaque
~~~

ProdutoId precisa pertencer ao tenant.

Select mostra Produtos locais ainda não vinculados, ativos e inativos.

Não mostrar Produto de outro tenant.

Produto já vinculado => mensagem amigável, sem alterar Destaque implicitamente.

### Destaque

Alterar somente ProdutoColecao.Destaque.

Recarregar associação pela Coleção da rota + Produto + tenant.

### Desvincular

Remover somente ProdutoColecao.

Não excluir Produto ou Coleção.

## Concorrência

Dois POSTs simultâneos do mesmo Produto/Coleção:

- PK composta é autoridade final;
- tratar 2601/2627 esperado de forma específica;
- uma linha no máximo;
- perdedora controlada;
- nenhum 500.

Adicionar teste concorrente real com SQL Server, não apenas sequencial.

## Detalhes do Produto

Adicionar seção Coleções.

Exibir por vínculo:

- Coleção;
- período;
- situação temporal usando IDataOperacionalEmpresa.Hoje;
- Destaque.

Ordenar DataLancamento DESC e NomeNormalizado ASC.

Estado vazio explícito.

## /Produtos

Adicionar filtro:

~~~text
colecao
~~~

Opções:

- Todas as coleções;
- Sem coleção;
- cada Coleção do tenant.

Filtrar via ProdutoColecao.

Preservar composição AND com:

- q;
- categoria;
- abaixo-da-margem;
- precificacao-incompleta.

Não alterar cálculo do resumo de precificação.

Coleção inválida/cross-tenant não pode vazar dados.

## Segurança

- EmpresaId nunca vem do request;
- handlers POST antiforgery;
- InputModels restritos;
- GQF normal;
- objeto cross-tenant => 404/validação controlada;
- não usar IgnoreQueryFilters na Web para atravessar tenant.

IgnoreQueryFilters só cabe no guard Infrastructure para validar ownership real.

## Sem impacto em precificação

Não alterar:

- PrecificacaoProdutoAtual;
- ResumoPrecificacaoProdutosAtual;
- calculadoras;
- RegistroPrecoProduto;
- Margem;
- custos;
- filtros existentes, além da composição do novo filtro.

## Testes obrigatórios

Atenda integralmente à matriz UC034.

Pontos críticos:

- N:N;
- same-tenant guard;
- Produto ativo/inativo;
- Categoria não restringe;
- Coleção finalizada editável;
- Destaque múltiplo;
- duplicidade concorrente;
- cross-tenant;
- Details;
- filtro Coleção;
- filtro Sem coleção;
- composição com filtros existentes;
- migration clean/upgrade;
- nenhuma alteração de precificação;
- regressão UC033.

## Documentação ao concluir

- UC034 -> Concluído;
- backlog -> Concluído;
- catálogo/F002 coerentes;
- RN080–RN083 preservadas;
- UC035 continua Planejado;
- não antecipar UC035.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
~~~

Commit sugerido:

~~~text
feat: vincula produtos a colecoes
~~~

Não faça merge em master.
