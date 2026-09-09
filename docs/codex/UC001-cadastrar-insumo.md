# Instrução Codex — UC001 Cadastrar Insumo

Use esta instrução para implementar o primeiro caso de uso funcional do Precificador.

---

Você está implementando o **UC001 — Cadastrar Insumo** do repositório `ajmarzola/Precificador`.

## Objetivo

Entregar uma fatia vertical completa para cadastrar um novo insumo: domínio, persistência, primeira migration, Razor Page, validações e testes.

Não implemente funcionalidades dos UCs seguintes.

## Leitura obrigatória antes de alterar arquivos

Leia integralmente, nesta ordem:

1. `AGENTS.md`;
2. `docs/use-cases/UC001-cadastrar-insumo.md`;
3. `docs/features/F001-insumos.md`;
4. `docs/business/business-rules.md`;
5. `docs/architecture/architecture.md`;
6. `docs/architecture/adr/ADR-002-sqlite-ef-core.md`;
7. `docs/development/testing-strategy.md`;
8. `docs/development/definition-of-done.md`;
9. `docs/development/workflow-codex.md`.

A especificação normativa desta entrega é `docs/use-cases/UC001-cadastrar-insumo.md`.

## Branch

Não trabalhe diretamente em `master`.

Crie/use:

```text
feat/uc001-cadastrar-insumo
```

Parta da `master` atualizada após o merge da documentação deste UC.

## Implementação obrigatória

Implemente somente o UC001, incluindo:

- entidade `Insumo` em `Precificador.Core`;
- enum `CategoriaInsumo` com valores explícitos `Ingrediente = 1`, `Embalagem = 2`, `Consumivel = 3`;
- enum `UnidadeMedida` com valores explícitos `Grama = 1`, `Mililitro = 2`, `Unidade = 3`;
- normalização do nome no domínio;
- `NomeNormalizado` persistido para comparação/índice;
- `Ativo = true` na criação;
- `DbSet<Insumo>`;
- mapeamento EF Core no Infrastructure;
- índice único para `NomeNormalizado`;
- primeira migration real, preferencialmente `CreateInsumos`;
- Razor Page `/Insumos/Novo`;
- input model próprio da página;
- validações server-side;
- verificação funcional de duplicidade;
- fluxo Post/Redirect/Get após sucesso;
- mensagem `Insumo cadastrado com sucesso.`;
- acesso simples à página pela Home ou navegação existente;
- testes unitários e de integração definidos no UC;
- atualização do status do documento do UC para `Implementado` ao final.

## Regras centrais

### Nome

- obrigatório;
- máximo 120 caracteres após normalização;
- remover espaços externos;
- reduzir sequências de whitespace internas a um único espaço;
- preservar a capitalização digitada no `Nome` exibido;
- gerar `NomeNormalizado` a partir do nome limpo usando caixa invariável em maiúsculas;
- não remover acentos.

Exemplo:

```text
"  Farinha   Renata  "
```

resulta em:

```text
Nome = "Farinha Renata"
NomeNormalizado = "FARINHA RENATA"
```

### Unicidade

O nome normalizado é único entre **todos** os insumos, inclusive inativos.

A aplicação deve fazer verificação antes de salvar para oferecer mensagem funcional, e o banco deve possuir índice único como garantia final de integridade.

Mensagem de duplicidade:

```text
Já existe um insumo cadastrado com esse nome.
```

Não converta qualquer `DbUpdateException` genericamente em duplicidade.

### Categoria e unidade

Nenhum valor `0` é funcionalmente válido.

A UI deve exibir:

Categoria:
- Ingrediente
- Embalagem
- Consumível

Unidade base:
- g
- ml
- un

### Situação

O formulário não possui campo Ativo. Novo insumo nasce ativo.

## Modelo esperado

Conceitualmente:

```text
Insumo
- Id: int
- Nome: string
- NomeNormalizado: string
- Categoria: CategoriaInsumo
- UnidadeBase: UnidadeMedida
- Ativo: bool
```

Não adicione campos de preço, fornecedor, estoque ou auditoria que não estejam especificados.

## Domínio

A entidade deve proteger invariantes que independem da UI, incluindo nome válido, categoria válida e unidade válida.

Não dependa de DataAnnotations de Razor Pages como única proteção do domínio.

Evite um `public set` irrestrito que permita construir um `Insumo` inválido fora da UI.

Não introduza base entity, generic result, domain events, mediator ou abstrações genéricas apenas para este caso de uso.

## Persistência

Use EF Core/SQLite já configurados.

Tabela:

```text
Insumos
```

Mapear:

- `Id` como PK inteiro gerado;
- `Nome` obrigatório, máximo 120;
- `NomeNormalizado` obrigatório, máximo 120;
- `Categoria` obrigatório;
- `UnidadeBase` obrigatório;
- `Ativo` obrigatório;
- índice único em `NomeNormalizado`.

É recomendado usar `IEntityTypeConfiguration<Insumo>` no Infrastructure, sem obrigatoriedade se uma solução igualmente simples e coerente já estiver presente.

## Migration

Criar a primeira migration via ferramenta EF Core existente no manifest.

Comandos equivalentes:

```text
dotnet tool restore
dotnet ef migrations add CreateInsumos --project src/Precificador.Infrastructure --startup-project src/Precificador.Web
dotnet ef database update --project src/Precificador.Infrastructure --startup-project src/Precificador.Web
```

Não criar tabelas de outros UCs.

Não adicionar auto-migration ao `Program.cs`.

## Razor Page

Criar:

```text
src/Precificador.Web/Pages/Insumos/Novo.cshtml
src/Precificador.Web/Pages/Insumos/Novo.cshtml.cs
```

Não faça binding direto de `Insumo`.

Use um input model contendo somente os campos permitidos:

- Nome;
- Categoria;
- UnidadeBase.

A página deve:

- responder em `GET /Insumos/Novo`;
- exibir validações por campo e resumo quando apropriado;
- manter dados digitados em POST inválido;
- fazer PRG no POST válido;
- usar `TempData` ou mecanismo Razor Pages equivalente para a mensagem de sucesso;
- retornar ao formulário vazio após o redirect.

Adicionar apenas um acesso simples `Cadastrar insumo` pela Home ou layout. Não criar listagem vazia fingindo o UC002.

## Testes unitários obrigatórios

No projeto existente `Precificador.Tests.Unit`, cobrir no mínimo:

1. criação válida define ativo;
2. nome é limpo de espaços externos e whitespace duplicado;
3. nome normalizado usa caixa invariável;
4. nome vazio/espaços é rejeitado;
5. nome acima de 120 após normalização é rejeitado;
6. categoria inválida é rejeitada;
7. unidade inválida é rejeitada.

Testar comportamento público do domínio, não detalhes privados de implementação.

## Testes de integração obrigatórios

### Persistência

Cobrir no mínimo:

1. migrations aplicam em SQLite vazio;
2. insumo válido persiste e é recuperado com os valores corretos;
3. índice único rejeita `NomeNormalizado` duplicado.

### Web

Cobrir no mínimo:

1. `GET /Insumos/Novo` retorna sucesso;
2. POST válido persiste um registro e redireciona;
3. após redirect a mensagem de sucesso aparece;
4. POST inválido não persiste;
5. nome duplicado não persiste e apresenta `Já existe um insumo cadastrado com esse nome.`.

A infraestrutura de teste deve continuar usando SQLite e jamais tocar no `precificador.db` real.

Se a factory da FT001 precisar evoluir para manter uma conexão SQLite `:memory:` aberta durante migrations/requests, faça apenas a alteração mínima necessária e cubra seu comportamento pelos testes.

## Restrições obrigatórias

Não implemente:

- UC002/listagem ou busca;
- UC003/edição;
- UC004/desativação pela interface;
- UC005/preço do insumo;
- UC006/histórico de preços;
- entidade/tabela de preço;
- produto;
- ficha técnica;
- fornecedor;
- estoque;
- importação da planilha;
- seed de dados reais;
- reativação;
- exclusão física;
- API REST/Swagger;
- repository genérico;
- Unit of Work customizado;
- MediatR/CQRS;
- auto-migration no startup.

Não refatore arquivos fora do escopo salvo necessidade técnica diretamente causada pelo UC001.

## Documentação na mesma entrega

Ao concluir:

- alterar `docs/use-cases/UC001-cadastrar-insumo.md` para `Status: Implementado`;
- não alterar as regras aprovadas apenas para adequá-las a uma implementação diferente;
- se descobrir conflito real de requisito, pare de expandir o escopo e registre o desvio na resposta/PR em vez de inventar nova regra.

## Validação obrigatória

Antes de concluir, execute:

```text
dotnet --info
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Valide também a migration em um banco limpo/temporário. Não dependa apenas do banco local já existente.

O build deve terminar sem warnings novos relevantes e todos os testes devem passar.

## Revisão do diff

Antes de finalizar, confirme:

- somente o UC001 foi implementado;
- nenhum `.db`, `bin`, `obj` ou `TestResults` entrou no Git;
- migration contém somente `Insumos` e seu índice;
- não existe funcionalidade de preço/listagem antecipada;
- regras permanecem no domínio ou no nível apropriado, não duplicadas desnecessariamente no Razor;
- testes realmente validam persistência e fluxo web, sem mocks que escondam SQLite.

## Resultado esperado da resposta do Codex

Informe objetivamente:

- principais arquivos criados/alterados;
- estrutura final da entidade e mapping;
- nome da migration;
- quantidade de testes unitários e de integração executados;
- resultado de restore/build/test;
- resultado da aplicação da migration em banco limpo;
- qualquer desvio da especificação e justificativa;
- branch/commit criado, se o ambiente permitir.

Faça commit com mensagem semelhante a:

```text
feat: implementa cadastro de insumo
```

Não faça merge em `master`.

---

Esta instrução autoriza somente o UC001.
