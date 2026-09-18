# Codex — MEL020 — SQLite → SQL Server

Implemente exclusivamente a MEL020 conforme:

```text
docs/development/improvements/MEL020-sqlserver.md
```

## Objetivo

Trocar o provider de persistência inteiro para SQL Server, rebaselinear migrations e executar toda a integração em SQL Server real.

Não implementar UC028 nem implantação Azure.

## Ordem recomendada

1. Troque os pacotes EF:
   - remova `Microsoft.EntityFrameworkCore.Sqlite`;
   - adicione `Microsoft.EntityFrameworkCore.SqlServer`.
2. Troque `UseSqlite` por `UseSqlServer` em runtime e design-time.
3. Configure `ConnectionStrings:Precificador` para Development sem segredo.
4. Rebaseline as migrations para SQL Server a partir do modelo atual.
5. Revise o baseline:
   - identity;
   - seed Empresa inicial;
   - FKs;
   - índices;
   - `decimal(p,s)`;
   - `DateOnly -> date`;
   - identity columns.
6. Preserve `Database.MigrateAsync()` somente em Development, conforme MEL011.
7. Migre a infraestrutura de testes para SQL Server real.
8. Adapte/remova testes que dependem exclusivamente da cadeia histórica SQLite, preservando a cobertura funcional correspondente.
9. Garanta que CI execute integração com SQL Server.
10. Atualize documentos arquiteturais que ainda descrevem SQLite como provider atual.
11. Marque MEL020 como `Concluído`; não avance UC028.

## Migrations

Não tente executar migrations SQLite contra SQL Server.

A cadeia atual contém detalhes específicos do SQLite.

A estratégia normativa é:

```text
modelo atual
-> nova migration inicial SQL Server
-> novo ModelSnapshot SQL Server
```

Não use `EnsureCreated`.

Banco SQLite existente não é migrado automaticamente.

## Testes

Não aceite SQLite ou EF InMemory como substituto da integração.

Preferir:

```text
Testcontainers.MsSql
1 container por assembly/sessão
databases isolados por fixture/factory
```

Imagem de referência:

```text
mcr.microsoft.com/mssql/server:2022-latest
```

Não tocar no banco local manual do desenvolvedor.

## Startup

Preservar:

```csharp
if (app.Environment.IsDevelopment())
{
    await context.Database.MigrateAsync();
}
```

Não adicionar `GetPendingMigrationsAsync()` como pré-condição; `MigrateAsync()` já só aplica pendências.

Não habilitar auto-migration em Production/Azure nesta MEL.

## Validação obrigatória

Antes da PR:

1. busca ativa sem `UseSqlite`/`SqliteConnection`;
2. banco SQL Server vazio migra do zero;
3. segunda execução da migration é idempotente;
4. seed técnico correto;
5. `GetPendingMigrationsAsync()` vazio;
6. Setup funciona no primeiro uso;
7. FKs/índices/precisões validados;
8. cross-tenant verde;
9. build Release 0 erros;
10. suíte unitária verde;
11. suíte integração SQL Server verde;
12. UC028 não alterada.

## Observação de escopo

A aplicação deve ficar apta a receber depois uma connection string Azure SQL, mas esta MEL não cria nem publica recursos Azure.
