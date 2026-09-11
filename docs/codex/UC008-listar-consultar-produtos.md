# Instrução Codex — UC008 Listar e consultar produtos

Implemente o **UC008 — Listar e consultar produtos**.

## Pré-condição de fila

Não iniciar enquanto o UC007 não estiver implementado, revisado e mergeado na master.

Antes de codificar, confirme que:

- Produto existe no Core;
- migration de Produtos está na master;
- UC007 está marcado como Implementado.

## Leitura obrigatória

1. `AGENTS.md`;
2. `docs/use-cases/UC008-listar-consultar-produtos.md`;
3. `docs/use-cases/UC007-cadastrar-produto.md`;
4. `docs/features/F002-produtos.md`;
5. `docs/business/business-rules.md`;
6. `docs/development/foundation-multiempresa-auth.md`;
7. `docs/development/testing-strategy.md`;
8. `docs/development/definition-of-done.md`;
9. código real de Produto/DbContext/Produtos Novo na master.

## Branch

~~~text
feat/uc008-listar-consultar-produtos
~~~

## Escopo

Entregar:

- `/Produtos`;
- pesquisa por Nome via `q`;
- ordenação por NomeNormalizado;
- estados vazios;
- `/Produtos/Detalhes/{id:int}`;
- navegação principal Produtos;
- testes;
- docs pós-implementação.

Não implementar UC009+.

## Listagem

Colunas:

- Nome;
- Categoria;
- Margem-alvo;
- Situação;
- Consultar.

Categoria null => `—`.

Margem:

- persistida como fração;
- exibida como percentual com até 2 casas.

Ordenar:

~~~text
NomeNormalizado ASC
~~~

## Pesquisa

Campo:

~~~text
q
~~~

Pesquisar **somente Nome**.

Normalizar consulta:

- trim;
- colapsar whitespace;
- uppercase invariant;
- acentos preservados.

Comparar com `NomeNormalizado.Contains(...)`.

Não criar `CategoriaNormalizada`.

Não pesquisar Categoria.

## Detalhes

Rota:

~~~text
/Produtos/Detalhes/{id:int}
~~~

Exibir:

- Nome;
- Categoria;
- Margem-alvo;
- Situação.

Não exibir campos técnicos, preço, custo ou Ficha Técnica.

Inexistente/cross-tenant => 404.

## Tenancy e leitura

- Global Query Filter;
- AsNoTracking;
- sem IgnoreQueryFilters;
- sem Where de Empresa como substituto do mecanismo central.

## Navegação

Layout principal deve ter **Produtos** -> `/Produtos`.

Listagem:

- Cadastrar produto;
- Consultar.

Detalhes:

- Voltar para produtos.

Não adicionar Editar.

## Situação

Não implemente Desativar/Reativar.

Se UC010 ainda não existe, não crie método de domínio para fabricar cenário inativo só para teste.

Apresente corretamente o campo Ativo atual e deixe cobertura de inativos para regressão do UC010 se não houver forma legítima de preparar o estado.

## Testes

Siga a matriz fechada do UC008.

Especial atenção:

- pesquisa não usa Categoria;
- margem 0,255 => 25,5%;
- cross-tenant detalhes => 404;
- campos técnicos/futuros ausentes;
- nenhum teste frouxo que apenas procure textos globais quando precisar associar linha/Produto.

## Arquitetura

EF direto em PageModels.

Não criar:

- repository;
- service genérico;
- CQRS/MediatR;
- migration;
- DTO layer artificial.

## Proibições

Não implementar:

- edição;
- desativação;
- preço;
- histórico de venda;
- ficha;
- custo;
- filtros avançados;
- paginação;
- CategoriaNormalizada;
- API.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Confirmar:

1. nenhuma migration/snapshot;
2. build sem warnings novos;
3. suíte verde;
4. Global Query Filter ativo;
5. AsNoTracking em leitura;
6. pesquisa somente Nome;
7. detalhes cross-tenant 404;
8. UC009+ ausente.

## Documentação pós-implementação

- UC008 => Implementado;
- atualizar F002;
- catálogo;
- ordem;
- UC009 passa a próximo caso.

## Retorno obrigatório

~~~text
Implementação concluída

Resumo:
- ...

Validações:
- ...

Testes:
- Unitários: X/X
- Integração: X/X

Produção/schema:
- listagem, pesquisa e detalhes de Produto
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: lista e consulta produtos
~~~

Não faça merge em master.
