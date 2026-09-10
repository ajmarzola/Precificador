# Instrução Codex — UC001A Marca e observação do insumo

Você está implementando o **UC001A — Complementar cadastro de insumo com marca e observação** do repositório `ajmarzola/Precificador`.

## Objetivo

Evoluir o cadastro de Insumo já tenant-aware para suportar Marca e Observação, alterando a unicidade de:

```text
EmpresaId + NomeNormalizado
```

para:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

Preserve integralmente a FT002 e o vocabulário já implementado no UC001B.

Não implemente o UC002 nesta entrega.

## Leitura obrigatória

Leia integralmente antes de alterar código:

1. `AGENTS.md`;
2. `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md`;
3. `docs/use-cases/UC001-cadastrar-insumo.md`;
4. `docs/use-cases/UC001B-generalizar-categoria-unidades-insumo.md`;
5. `docs/development/foundation-multiempresa-auth.md`;
6. `docs/features/F001-insumos.md`;
7. `docs/business/business-rules.md`;
8. `docs/architecture/architecture.md`;
9. `docs/development/testing-strategy.md`;
10. `docs/development/definition-of-done.md`;
11. `docs/development/melhorias.md`.

A especificação normativa desta entrega é `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md`.

## Branch

Use branch dedicada:

```text
feat/uc001a-marca-observacao-insumo
```

Parta da `master` atualizada após o merge da documentação revalidada.

## Implementação obrigatória

Evolua `Insumo` para conter conceitualmente:

```text
Id
EmpresaId
Nome
NomeNormalizado
Marca
MarcaNormalizada
Observacao
Categoria
UnidadeBase
Ativo
```

`Insumo` deve continuar implementando `IEntidadeEmpresa` e seu `EmpresaId` deve continuar vindo do contexto do servidor, nunca do formulário.

### Marca

- opcional;
- máximo 80 caracteres após normalização de espaços;
- trim externo;
- colapsar sequências internas de whitespace para um único espaço;
- preservar capitalização para exibição;
- `MarcaNormalizada` em maiúsculas invariáveis;
- não remover acentos;
- ausência de marca: `Marca = null` e `MarcaNormalizada = ""`.

### Observação

- opcional;
- máximo 1000 caracteres;
- remover apenas whitespace externo;
- preservar conteúdo interno e quebras de linha;
- whitespace-only vira `null`.

### Categoria e unidade

Não reverta o UC001B.

O código deve continuar utilizando:

```text
CategoriaInsumo
- MateriaPrima = 1
- Embalagem = 2
- Consumivel = 3

UnidadeMedida
- Grama = 1
- Mililitro = 2
- Unidade = 3
- Metro = 4
```

Nenhum membro funcional `Ingrediente` deve reaparecer.

## Unicidade tenant-aware

O índice único atual da FT002 é:

```text
EmpresaId + NomeNormalizado
```

Substitua-o por:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

Consequências obrigatórias:

- mesma Empresa + mesmo Nome + mesma Marca: rejeitar;
- mesma Empresa + mesmo Nome + marcas diferentes: permitir;
- mesma Empresa + mesmo Nome sem Marca duas vezes: rejeitar;
- Empresas diferentes + mesma combinação Nome/Marca: permitir;
- registros ativos e inativos continuam participando da unicidade.

A validação Web deve operar somente sobre a Empresa Ativa. Não use `IgnoreQueryFilters` em fluxo comum de negócio.

Mensagem funcional:

```text
Já existe um insumo cadastrado com esse nome e marca.
```

Não transforme qualquer `DbUpdateException` genericamente em duplicidade. Se houver tratamento de exceção de persistência, reconheça somente a violação de unicidade esperada.

## Migration

Crie nova migration evolutiva. Não altere migrations históricas.

Como o UC001B não criou migration, o estado persistido anterior é o da FT002 (`AddMultiempresaIdentity`).

A migration deve:

- remover o índice único atual `(EmpresaId, NomeNormalizado)`;
- adicionar `Marca` nullable, max 80;
- adicionar `MarcaNormalizada` obrigatória, max 80, default `""` para registros existentes;
- adicionar `Observacao` nullable, max 1000;
- criar índice único `(EmpresaId, NomeNormalizado, MarcaNormalizada)`;
- preservar `EmpresaId`, FK de Empresa e todos os registros existentes.

Não tente inferir marcas a partir do Nome de registros existentes.

O ModelSnapshot deve ficar coerente com a migration.

Teste tanto banco vazio quanto upgrade de banco no estado imediatamente anterior contendo dados.

## Multiempresa

Preserve integralmente a FT002:

- `Insumo : IEntidadeEmpresa`;
- `EmpresaId` obrigatório;
- criação recebe `EmpresaId` resolvido no servidor;
- Global Query Filter continua isolando Empresa Ativa;
- guard de `SaveChanges` continua impedindo escrita cross-tenant;
- nenhuma tela expõe ou binda `EmpresaId`;
- páginas de negócio continuam exigindo autenticação + Empresa Ativa;
- `IgnoreQueryFilters` somente em teste/infraestrutura explicitamente justificados.

Não simplifique tenancy para facilitar o UC ou os testes.

## Razor Page `/Insumos/Novo`

Adicionar:

- Marca opcional;
- Observação opcional em `textarea`.

Ordem recomendada:

```text
Nome
Marca
Categoria
Unidade base
Observação
```

Preserve:

- Matéria-prima / Embalagem / Consumível;
- g / ml / m / un;
- PRG;
- antiforgery;
- mensagem de sucesso;
- autenticação e policy de Empresa Ativa.

Não adicione `EmpresaId` ao formulário.

Não implemente listagem nem detalhes.

## Testes obrigatórios

### Unitários

Cobrir:

- marca válida;
- ausência de marca;
- normalização da marca;
- `MarcaNormalizada`;
- limite de 80 caracteres;
- observação válida;
- observação whitespace-only -> null;
- limite de 1000 caracteres;
- `EmpresaId` permanece associado ao Insumo;
- categorias/unidades do UC001B continuam válidas.

Preserve e ajuste os testes existentes sem reduzir cobertura.

### Integração — persistência

Cobrir:

- migration em banco vazio;
- upgrade do estado anterior com registro existente;
- preservação de registros e `EmpresaId`;
- registro antigo recebe `Marca = null`, `MarcaNormalizada = ""` e `Observacao = null`;
- mesma Empresa + mesmo Nome com marcas diferentes é permitido;
- mesma Empresa + mesmo Nome + mesma Marca é rejeitado pelo índice;
- mesma Empresa + mesmo Nome sem marca duplicado é rejeitado;
- Empresas diferentes aceitam a mesma combinação Nome + Marca;
- Empresas diferentes aceitam o mesmo Nome sem Marca;
- persistência de Marca e Observação;
- query filter continua isolando tenants;
- guard continua rejeitando escrita cross-tenant.

Use SQLite real/in-memory com conexão mantida, nunca provider EF InMemory.

### Integração — Web

Cobrir:

- formulário apresenta Marca e Observação;
- formulário não apresenta `EmpresaId`;
- POST válido com marca persiste na Empresa Ativa;
- POST válido sem marca persiste na Empresa Ativa;
- mesmo Nome com marcas diferentes é aceito dentro da mesma Empresa;
- duplicidade por Nome + Marca na mesma Empresa apresenta mensagem funcional;
- mesma combinação em Empresa diferente é permitida e não vaza dados entre tenants;
- validações de tamanho de Marca/Observação não persistem dados inválidos;
- autenticação + Empresa Ativa continuam obrigatórias.

## Observação da ficha técnica

Não implemente observação de item da ficha técnica agora. O requisito permanece associado ao UC014 e é independente da Observação global do Insumo.

## Restrições

Não implementar:

- UC002/listagem/detalhes;
- UC003/edição;
- UC004/desativação;
- UC005/preços;
- UC006/histórico;
- ficha técnica;
- produto;
- parser automático de marca;
- alterações administrativas de Empresa/Usuário;
- auto-migration no startup;
- repository genérico;
- CQRS/MediatR;
- qualquer reversão de `MateriaPrima` para `Ingrediente`.

## Melhorias não bloqueantes

Se surgir uma ideia tecnicamente útil que não seja necessária para satisfazer o UC001A, registre em:

```text
docs/development/melhorias.md
```

Não amplie a PR automaticamente por causa dela.

## Documentação

Ao concluir:

- alterar o status de `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md` para `Implementado`;
- não marcar UC002 como implementado;
- manter RN029 como unicidade por Empresa + Nome + Marca;
- manter F001 e a ordem de implementação coerentes;
- não alterar regra aprovada para acomodar implementação diferente.

## Validação

Execute:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Valide também a migration sobre:

1. banco vazio;
2. banco no estado imediatamente anterior com Insumos existentes e `EmpresaId` já atribuído.

Antes de abrir a PR:

1. confirme build Release sem warnings novos relevantes;
2. confirme suíte completa verde;
3. confirme migration + ModelSnapshot coerentes;
4. confirme índice `(EmpresaId, NomeNormalizado, MarcaNormalizada)`;
5. confirme ausência de `EmpresaId` no formulário;
6. confirme ausência de funcionalidade UC002+;
7. confirme que não houve regressão de FT002 ou UC001B.

Commit sugerido:

```text
feat: adiciona marca e observacao ao insumo
```

Não faça merge em `master`.
