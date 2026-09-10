# Instrução Codex — UC001B Generalizar categoria e unidades de insumo

Você está implementando o **UC001B — Generalizar categoria e unidades de insumo** do repositório `ajmarzola/Precificador`.

## Objetivo

Generalizar o vocabulário de Insumos já existente para atender diferentes tipos de negócio, sem alterar o schema atual:

- renomear `CategoriaInsumo.Ingrediente = 1` para `CategoriaInsumo.MateriaPrima = 1`;
- não manter `Ingrediente` como alias funcional;
- acrescentar `UnidadeMedida.Metro = 4`;
- preservar os valores numéricos existentes de categoria e unidade;
- atualizar `/Insumos/Novo` e os testes;
- manter integralmente o isolamento multiempresa implementado na FT002.

Não implemente UC001A ou UC002 nesta entrega.

## Leitura obrigatória

Leia integralmente antes de alterar código:

1. `AGENTS.md`;
2. `docs/use-cases/UC001B-generalizar-categoria-unidades-insumo.md`;
3. `docs/use-cases/UC001-cadastrar-insumo.md`;
4. `docs/development/foundation-multiempresa-auth.md`;
5. `docs/features/F001-insumos.md`;
6. `docs/business/business-rules.md`;
7. `docs/architecture/architecture.md`;
8. `docs/development/testing-strategy.md`;
9. `docs/development/definition-of-done.md`.

A especificação normativa desta entrega é `docs/use-cases/UC001B-generalizar-categoria-unidades-insumo.md`.

## Branch

Use branch dedicada:

```text
feat/uc001b-generalizar-categoria-unidades-insumo
```

Parta da `master` atualizada após o merge da documentação desta entrega.

## Alterações de domínio

### CategoriaInsumo

O enum deve ficar conceitualmente assim:

```text
MateriaPrima = 1
Embalagem = 2
Consumivel = 3
```

Requisitos:

- `MateriaPrima` deve possuir exatamente o valor numérico `1`;
- `Embalagem` permanece `2`;
- `Consumivel` permanece `3`;
- remover o nome `Ingrediente` do enum;
- não criar alias `Ingrediente = MateriaPrima`;
- registros existentes com valor inteiro `1` devem continuar sendo materializados normalmente, agora como `MateriaPrima`.

### UnidadeMedida

O enum deve ficar conceitualmente assim:

```text
Grama = 1
Mililitro = 2
Unidade = 3
Metro = 4
```

Requisitos:

- preservar os valores `1`, `2` e `3` já existentes;
- adicionar `Metro = 4`;
- não adicionar `Centimetro` ou outra unidade nesta entrega.

## Persistência

**Não criar migration.**

A alteração é compatível com o schema atual porque os enums são persistidos como inteiros:

- `Categoria = 1` continua `1`;
- `UnidadeBase` continua inteira e passa a aceitar `4`.

Não alterar migrations históricas, ModelSnapshot ou configuração EF apenas para registrar a mudança dos enums.

Se surgir uma necessidade real de mudança de schema, interrompa a expansão do escopo e reporte antes de criar migration.

## Multiempresa

Preserve integralmente a FT002:

- `Insumo` continua implementando `IEntidadeEmpresa`;
- `EmpresaId` continua obrigatório;
- criação recebe `EmpresaId` resolvido no servidor;
- Global Query Filter continua isolando Empresa Ativa;
- guard de `SaveChanges` continua impedindo escrita cross-tenant;
- nenhuma tela deve expor ou bindar `EmpresaId`;
- testes Web devem autenticar usuário e estabelecer Empresa Ativa quando necessário.

Não simplifique ou contorne tenancy para facilitar testes.

## Razor Page `/Insumos/Novo`

Atualize os rótulos/opções funcionais para apresentar:

Categoria:

```text
Matéria-prima
Embalagem
Consumível
```

Unidade base:

```text
g
ml
m
un
```

Requisitos adicionais:

- `Ingrediente` não deve mais aparecer como opção funcional;
- `m` deve mapear para `UnidadeMedida.Metro`;
- preserve PRG, antiforgery, validações e mensagem de sucesso já existentes;
- não adicionar Marca, Observação, listagem ou detalhes.

## Testes obrigatórios

### Unitários

Adicionar/ajustar testes para validar no mínimo:

- `(int)CategoriaInsumo.MateriaPrima == 1`;
- `(int)CategoriaInsumo.Embalagem == 2`;
- `(int)CategoriaInsumo.Consumivel == 3`;
- o enum não possui membro chamado `Ingrediente`;
- `Insumo.Criar` aceita `MateriaPrima`;
- `(int)UnidadeMedida.Grama == 1`;
- `(int)UnidadeMedida.Mililitro == 2`;
- `(int)UnidadeMedida.Unidade == 3`;
- `(int)UnidadeMedida.Metro == 4`;
- `Insumo.Criar` aceita `Metro`;
- valores de enum inválidos continuam rejeitados.

Atualize testes existentes que ainda usam `CategoriaInsumo.Ingrediente`.

### Integração — persistência

Cobrir:

- banco/migration existente materializa um registro com `Categoria = 1` como `CategoriaInsumo.MateriaPrima` sem atualização do dado;
- `UnidadeMedida.Metro` persiste como `4` e é recuperada como `Metro`;
- isolamento tenant-aware continua válido;
- não há migration nova nem alteração indevida de schema.

Use SQLite real/in-memory com conexão mantida. Não use provider EF InMemory.

### Integração — Web

Cobrir em `/Insumos/Novo`:

- formulário apresenta `Matéria-prima`;
- formulário não apresenta `Ingrediente` como opção;
- formulário apresenta unidade `m`;
- POST válido cadastra Matéria-prima;
- POST válido cadastra Insumo com Metro;
- fluxo continua exigindo autenticação + Empresa Ativa conforme FT002.

Preserve os testes já existentes de UC001 e FT002.

## Escopo proibido

Não implementar nesta PR:

- Marca ou Observação do UC001A;
- mudança de unicidade para Nome + Marca;
- UC002/listagem/detalhes;
- edição/desativação;
- preço/histórico;
- Produto;
- Ficha Técnica;
- novas categorias;
- novas unidades além de Metro;
- conversão entre unidades;
- migration vazia;
- auto-migration no startup;
- mudanças em autenticação/multiempresa que não sejam estritamente necessárias para corrigir regressão causada por este UC.

## Documentação

Ao concluir:

- alterar o status de `docs/use-cases/UC001B-generalizar-categoria-unidades-insumo.md` para `Implementado`;
- não marcar UC001A/UC002 como implementados;
- manter RN001/RN030 coerentes com a especificação já aprovada;
- se surgir melhoria útil mas não necessária ao UC001B, registre em `docs/development/melhorias.md` em vez de ampliar esta entrega.

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
3. revise o diff e confirme que não existe migration nova;
4. confirme que `Ingrediente` não permanece como membro funcional do enum;
5. confirme que UC001A/UC002+ não foram antecipados.

Commit sugerido:

```text
feat: generaliza categoria e unidades de insumo
```

Não faça merge em `master`.
