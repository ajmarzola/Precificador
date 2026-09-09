# Instrução Codex — FT001 Fundação Técnica

Use esta instrução para executar a primeira implementação do Precificador.

---

Você está implementando a **FT001 — Fundação Técnica** do repositório `ajmarzola/Precificador`.

## Objetivo

Criar a base executável, testável e integrada do Precificador em .NET 10, **sem implementar ainda nenhum caso de uso funcional**.

## Leitura obrigatória antes de alterar arquivos

Leia integralmente, nesta ordem:

1. `AGENTS.md`;
2. `docs/development/foundation-technical.md`;
3. `docs/architecture/architecture.md`;
4. `docs/architecture/adr/ADR-001-monolito-web-local-first.md`;
5. `docs/architecture/adr/ADR-002-sqlite-ef-core.md`;
6. `docs/architecture/adr/ADR-003-dotnet-10-lts.md`;
7. `docs/architecture/adr/ADR-006-formato-slnx.md`;
8. `docs/architecture/adr/ADR-007-ci-github-actions.md`;
9. `docs/development/testing-strategy.md`;
10. `docs/development/definition-of-done.md`.

A especificação normativa desta entrega é `docs/development/foundation-technical.md`.

## Branch

Não trabalhe diretamente em `master`.

Se o ambiente ainda não estiver em uma branch dedicada, crie/use:

```text
feat/ft001-fundacao-tecnica
```

Parta da `master` atualizada.

## Implementação

Implemente **somente** o que está definido na FT001.

A entrega deve incluir:

- `Precificador.slnx`;
- `global.json` para .NET 10 estável;
- projetos `Precificador.Core`, `Precificador.Infrastructure` e `Precificador.Web` em `src/`;
- projetos `Precificador.Tests.Unit` e `Precificador.Tests.Integration` em `tests/`;
- referências entre projetos exatamente conforme a arquitetura;
- Razor Pages funcional;
- `PrecificadorDbContext` mínimo, sem entidades/`DbSet`;
- EF Core 10 + SQLite configurados;
- tool manifest local com `dotnet-ef` 10.0.x;
- connection string local para SQLite;
- smoke tests de integração definidos na FT001;
- `.editorconfig` mínimo;
- `.gitignore` adequado para .NET e SQLite;
- workflow `.github/workflows/ci.yml`;
- logging padrão do ASP.NET Core.

Use versões **estáveis** `10.0.x` e mantenha a mesma versão patch para pacotes/ferramentas EF Core. Não use previews.

## Restrições obrigatórias

Não implemente:

- `Insumo`;
- `Produto`;
- `Receita`/ficha técnica;
- regras de custo, margem ou preço;
- UC001;
- repository genérico;
- Unit of Work customizado;
- CQRS/MediatR;
- API REST/Swagger;
- autenticação;
- Docker;
- migrations vazias;
- seeders vazios;
- páginas de negócio;
- refatorações da documentação sem necessidade da FT001.

Não crie código placeholder apenas para preencher projetos. Remova `Class1.cs`, `UnitTest1.cs` e equivalentes gerados por template quando não tiverem função real.

## Banco nos testes

Os testes não podem tocar no arquivo local `precificador.db`.

Ao testar a configuração da aplicação, substitua o banco no ambiente de teste por SQLite temporário ou em memória, preservando o provider SQLite.

## Testes mínimos obrigatórios

1. smoke test com `WebApplicationFactory<Program>` confirmando `GET /` com sucesso;
2. teste de integração confirmando que `PrecificadorDbContext` usa provider SQLite no ambiente de teste.

Não crie teste unitário artificial para uma regra que ainda não existe. O projeto `Precificador.Tests.Unit` pode permanecer sem testes nesta entrega.

## CI

O workflow deve executar em `pull_request` para `master` e `push` em `master`, usando runner padrão `ubuntu-latest`.

Execute pelo menos:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

## Validação local obrigatória

Antes de concluir, execute:

```text
dotnet --info
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Se algum comando falhar, corrija a implementação antes de considerar a entrega pronta. Não remova/ignore testes para obter sucesso artificial.

## Revisão do diff

Antes de finalizar:

1. revise `git diff`/mudanças da branch;
2. remova arquivos de template sem uso;
3. confirme que nenhum banco `.db`, `bin`, `obj`, `.vs` ou `TestResults` foi incluído;
4. confirme que não há funcionalidade de domínio antecipada;
5. confirme que a documentação existente continua coerente.

## Resultado esperado da sua resposta

Ao terminar, informe de forma objetiva:

- arquivos/estruturas principais criados;
- versões estáveis relevantes resolvidas;
- resultado de restore/build/test;
- resultado dos testes de integração;
- qualquer desvio da especificação e a justificativa;
- branch/commit criado, se o ambiente permitir commit.

Se houver credenciais e integração Git disponíveis, faça commit da entrega com mensagem semelhante a:

```text
chore: cria fundação técnica do Precificador
```

Não faça merge em `master`.

---

Esta instrução não autoriza a implementação do UC001. O próximo incremento será preparado somente após a revisão da FT001.
