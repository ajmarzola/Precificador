# Instrução Codex — UC033 Administrar coleções

Você está implementando a UC033 — Administrar coleções no repositório ajmarzola/Precificador.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/use-cases/UC033-administrar-colecoes.md;
3. docs/use-cases/UC032-administrar-categorias-produto.md;
4. docs/business/business-rules.md, especialmente RN076–RN079;
5. docs/features/F002-produtos.md;
6. docs/development/testing-strategy.md;
7. docs/development/definition-of-done.md;
8. implementação atual de CategoriaProduto, Produto, PrecificadorDbContext e /Produtos/Categorias.

A especificação normativa é:

~~~text
docs/use-cases/UC033-administrar-colecoes.md
~~~

## Branch

Use:

~~~text
feat/uc033-administrar-colecoes
~~~

Parta da master depois do merge da documentação UC033.

Não implemente na branch documental.

## Objetivo

Criar Coleções comerciais tenant-aware com:

- Nome;
- DataLancamento obrigatória;
- DataFinalizacao opcional;
- situação temporal derivada;
- associação N:N com Categorias de Produto;
- listagem;
- cadastro;
- edição.

Não implementar vínculo Produto-Coleção. Isso é UC034.

## Entidades

Criar:

~~~text
ColecaoProduto : IEntidadeEmpresa
ColecaoProdutoCategoria : IEntidadeEmpresa
~~~

ColecaoProduto:

~~~text
Id
EmpresaId
Nome
NomeNormalizado
DataLancamento : DateOnly
DataFinalizacao : DateOnly?
~~~

ColecaoProdutoCategoria:

~~~text
EmpresaId
ColecaoProdutoId
CategoriaProdutoId
~~~

Não adicionar Ativo à Coleção.

Não adicionar ColecaoProdutoId a Produto.

## Nome e identidade

Nome:

- trim;
- colapsar whitespace;
- preservar capitalização;
- máximo 120.

NomeNormalizado = ToUpperInvariant.

Unicidade:

~~~text
EmpresaId + NomeNormalizado + DataLancamento
~~~

Mesmo Nome em outro lançamento é permitido.

Tratar duplicidade esperada de forma amigável, inclusive corrida do índice único.

## Período

DataLancamento obrigatória.

DataFinalizacao nullable.

Validar:

~~~text
DataFinalizacao == null
OR DataFinalizacao >= DataLancamento
~~~

Aceitar datas futuras.

Usar DateOnly / SQL date.

Não usar DateTimeOffset.

## Situação

Derivar com IDataOperacionalEmpresa.Hoje:

~~~text
Hoje < lançamento -> Planejada

lançamento <= Hoje
e (fim null ou Hoje <= fim)
-> Em andamento

fim != null
e Hoje > fim
-> Finalizada
~~~

Não persistir status.

Criar helper ou enum derivado puro no Core se útil.

## Sobreposição

Não validar exclusividade.

Coleções podem se sobrepor, inclusive nas mesmas Categorias.

Não criar query de conflito temporal.

## Categorias

Cadastro:

- oferecer apenas Categorias ativas do tenant.

Edição:

- Categorias ativas do tenant;
- mais Categorias inativas atualmente vinculadas.

Pode existir Coleção com zero Categorias.

Persistir N:N em ColecoesProdutosCategorias.

Sincronizar associações em uma unidade de trabalho, sem SaveChanges por item.

Não permitir:

- Categoria inexistente;
- cross-tenant;
- nova Categoria inativa não vinculada anteriormente.

Ids repetidos do request não podem causar PK violation 500.

## Sem regra de Produto

Não implementar:

- ProdutoColecao;
- ColecaoProdutoId em Produto;
- seletor de Coleção em Produto;
- filtro de Produto por Coleção;
- regra “Categoria do Produto deve estar na Coleção”;
- destaque/vigência de Produto.

A associação Categoria-Coleção é metadado nesta UC.

## Persistência

Adicionar DbSet + GQF + guard central para as duas entidades.

Tabelas:

~~~text
ColecoesProdutos
ColecoesProdutosCategorias
~~~

ColecoesProdutos:

- PK Id;
- FK Empresa RESTRICT;
- unique EmpresaId/NomeNormalizado/DataLancamento;
- index EmpresaId/DataLancamento;
- check de período.

ColecoesProdutosCategorias:

- PK ColecaoProdutoId/CategoriaProdutoId;
- FK Empresa RESTRICT;
- FK Coleção RESTRICT;
- FK Categoria RESTRICT;
- index EmpresaId/CategoriaProdutoId.

Não usar cascade.

Validar no SaveChanges que:

~~~text
Coleção.EmpresaId == associação.EmpresaId
Categoria.EmpresaId == associação.EmpresaId
~~~

inclusive para objetos fabricados fora da Web.

## Migration

Criar migration nova.

Não editar migrations históricas.

Sem backfill.

Testar:

- clean DB;
- upgrade pós-UC031;
- tipos date;
- constraints;
- FKs;
- índices;
- preservação de Produtos/Categorias/Identity;
- zero migrations pendentes.

## Web

Criar:

~~~text
/Produtos/Colecoes
/Produtos/Colecoes/Novo
/Produtos/Colecoes/Editar/{id}
~~~

A pasta já herda EmpresaAtiva de /Produtos.

Não mudar para AdministradorEmpresa.

Seguir UX/padrões de /Produtos/Categorias.

### Index

Colunas:

- Nome;
- Lançamento;
- Finalização;
- Situação;
- Categorias;
- Editar.

Ordenar:

~~~text
DataLancamento DESC
NomeNormalizado ASC
~~~

Finalização null = Em aberto.

Sem Categoria = —.

Adicionar botão Coleções em /Produtos junto a Categorias.

### Form

Campos:

- Nome;
- DataLancamento;
- DataFinalizacao;
- CategoriaProdutoIds.

Pode usar checkboxes ou multi-select.

POST inválido recarrega opções e preserva seleção.

PRG em sucesso.

## Edição

Permitir editar Coleção passada/finalizada.

Permitir limpar DataFinalizacao.

Não criar comando separado Finalizar/Reabrir.

Editar Categoria inativa atual:

- pode manter;
- pode remover.

Não pode adicionar outra inativa por request manipulado.

## Duplicidade

Fazer pre-check amigável.

Também capturar violação do índice único esperada, sem mascarar outros DbUpdateException.

Mensagem:

~~~text
Já existe uma coleção com esse nome e data de lançamento.
~~~

## Segurança

- EmpresaId somente do contexto;
- antiforgery em POSTs;
- InputModels próprios;
- cross-tenant GET/POST não vaza dados;
- sem GET mutável;
- nenhuma entidade EF bindada diretamente.

## Testes obrigatórios

Atenda integralmente à matriz UC033.

Pontos críticos:

- normalização/unicidade;
- DateOnly e bordas inclusivas;
- situação com IDataOperacionalEmpresa.Hoje;
- finalização null;
- sobreposição permitida;
- zero Categorias;
- Categorias múltiplas;
- inativa preservada na edição;
- cross-tenant;
- GQF/guard das duas entidades;
- migration clean/upgrade;
- corrida de duplicidade;
- antiforgery;
- ausência de qualquer mudança em Produto/precificação;
- regressão UC032/UC036.

## Documentação ao concluir

- UC033 -> Concluído;
- backlog -> Concluído;
- catálogo/F002 coerentes;
- RN076–RN079 preservadas;
- UC034 permanece Planejado;
- não antecipar UC034.

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
feat: adiciona administracao de colecoes
~~~

Não faça merge em master.
