# Instrução Codex — UC002 Listar e consultar insumos

Você está implementando o **UC002 — Listar e consultar insumos** do repositório `ajmarzola/Precificador`.

## Objetivo

Implementar listagem, pesquisa e consulta detalhada dos Insumos da **Empresa Ativa**, em modo exclusivamente de leitura, preservando FT002, UC001B e UC001A.

Não implemente UC003 ou funcionalidades posteriores nesta entrega.

## Leitura obrigatória

Leia integralmente antes de alterar código:

1. `AGENTS.md`;
2. `docs/use-cases/UC002-listar-consultar-insumos.md`;
3. `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md`;
4. `docs/development/foundation-multiempresa-auth.md`;
5. `docs/features/F001-insumos.md`;
6. `docs/business/business-rules.md`;
7. `docs/architecture/architecture.md`;
8. `docs/development/testing-strategy.md`;
9. `docs/development/definition-of-done.md`;
10. `docs/development/melhorias.md`.

A especificação normativa é `docs/use-cases/UC002-listar-consultar-insumos.md`.

## Branch

Use branch dedicada:

```text
feat/uc002-listar-consultar-insumos
```

Parta da `master` atualizada após o merge desta documentação.

## Páginas

Implementar:

```text
/Insumos
/Insumos/Detalhes/{id:int}
```

### /Insumos

Listar somente Insumos da Empresa Ativa, incluindo ativos e inativos.

Colunas:

- Nome;
- Marca;
- Categoria;
- Unidade base;
- Situação;
- ação Consultar.

Marca ausente deve usar representação visual neutra consistente, preferencialmente `—`.

Ordenar por:

```text
NomeNormalizado
MarcaNormalizada
```

Adicionar acesso **Cadastrar insumo** para `/Insumos/Novo`.

### Pesquisa

Usar query string:

```text
q
```

Normalizar o termo:

- trim externo;
- colapsar sequências internas de whitespace;
- `ToUpperInvariant()`;
- preservar acentos.

Filtrar conceitualmente por:

```text
NomeNormalizado.Contains(termo)
|| MarcaNormalizada.Contains(termo)
```

`q` nulo/vazio/whitespace não aplica filtro.

Não criar parser para `farinha renata`.

### /Insumos/Detalhes/{id:int}

Exibir:

- Nome;
- Marca;
- Categoria;
- Unidade base;
- Situação;
- Observação.

Não exibir:

- EmpresaId;
- NomeNormalizado;
- MarcaNormalizada.

Marca/Observação ausentes usam estado neutro consistente.

Id inexistente **ou pertencente a outra Empresa** deve retornar HTTP 404.

## Multiempresa e segurança

Preserve integralmente a FT002:

- consultas devem utilizar o Global Query Filter;
- não usar `IgnoreQueryFilters` em fluxo comum;
- não usar `EmpresaId` vindo de request/form/query;
- não revelar existência de Insumo de outro tenant;
- páginas continuam exigindo autenticação + Empresa Ativa;
- ausência de Empresa Ativa não concede acesso a dados.

Não adicione `Where(EmpresaId == ...)` como substituto do mecanismo central de isolamento. Filtros explícitos adicionais só são aceitáveis se tiverem finalidade funcional distinta.

## Leitura e EF Core

Use `AsNoTracking` nas consultas puras de listagem, pesquisa e detalhes.

EF Core pode permanecer diretamente nos PageModels.

Não criar:

- repository genérico;
- Unit of Work adicional;
- CQRS/MediatR;
- API;
- camada de serviço artificial.

Projeções para view models são permitidas e recomendadas quando reduzirem carregamento desnecessário.

## Navegação

- navegação principal: **Insumos** -> `/Insumos`;
- listagem: **Cadastrar insumo** -> `/Insumos/Novo`;
- listagem: **Consultar** -> detalhes;
- detalhes: retorno para listagem;
- não adicionar Editar.

## Persistência

**Não criar migration.**

Não alterar migrations históricas nem `PrecificadorDbContextModelSnapshot`.

Se surgir necessidade real de schema, pare e reporte em vez de ampliar o escopo.

## Testes obrigatórios

### Persistência/consulta

Cobrir:

- ativos e inativos da Empresa Ativa;
- Empresa B invisível para Empresa A;
- ausência de Empresa Ativa não retorna Insumos;
- ordenação NomeNormalizado + MarcaNormalizada;
- busca parcial por Nome;
- busca parcial por Marca;
- caixa e whitespace da consulta;
- acentos significativos;
- consulta vazia sem filtro;
- pesquisa sem resultado;
- leitura sem tracking quando aplicável.

Use SQLite real/in-memory com conexão mantida. Nunca EF InMemory.

### Web

Cobrir:

- acesso anônimo exige autenticação;
- `GET /Insumos` autenticado + Empresa Ativa retorna sucesso;
- listagem não vaza outro tenant;
- ativos e inativos visíveis;
- mesmo Nome com marcas distintas aparece separado;
- busca por Nome;
- busca por Marca;
- normalização de caixa/whitespace;
- estado sem Insumos;
- estado de busca sem resultado;
- detalhes apresentam campos funcionais;
- detalhes não apresentam EmpresaId/NomeNormalizado/MarcaNormalizada;
- id inexistente -> 404;
- id de outra Empresa -> 404;
- links de navegação esperados.

Não reduza cobertura existente de FT002, UC001B ou UC001A.

## Escopo proibido

Não implementar:

- UC003 edição;
- UC004 desativação/reativação;
- UC005/UC006 preço e histórico;
- paginação;
- filtros avançados;
- ordenação escolhida pelo usuário;
- exportação;
- API REST;
- preço/custo na listagem;
- Produto;
- Ficha Técnica;
- parser estruturado de pesquisa;
- alterações administrativas de Empresa/Usuário;
- mudanças na estratégia de autenticação/tenancy.

## Melhorias não bloqueantes

Ideia útil que não seja necessária ao UC002 deve ser registrada em:

```text
docs/development/melhorias.md
```

Não amplie automaticamente a PR.

## Documentação

Ao concluir:

- alterar o status de `docs/use-cases/UC002-listar-consultar-insumos.md` para `Implementado`;
- não marcar UC003+ como implementado;
- manter F001/catálogo/ordem coerentes;
- não mudar regra aprovada para acomodar implementação divergente.

## Validação

Execute:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Antes de abrir a PR:

1. confirme build Release sem warnings novos relevantes;
2. confirme suíte completa verde;
3. confirme ausência de migration/alteração de snapshot;
4. confirme ausência de `IgnoreQueryFilters` nas páginas do UC002;
5. confirme 404 para id de outro tenant;
6. confirme `AsNoTracking` nas consultas puras;
7. confirme ausência de UC003+.

Commit sugerido:

```text
feat: lista e consulta insumos
```

Não faça merge em `master`.
