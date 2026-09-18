# MEL020 — Migrar persistência de SQLite para SQL Server

- **Origem:** evolução de infraestrutura / preparação para publicação em Azure
- **Classificação:** fundação técnica / persistência
- **Prioridade:** alta
- **Estado:** Concluído
- **Ordem na fila pendente:** 07
- **Dependências:** FT002, MEL011 e schema funcional atual consolidado
- **Gate operacional:** executar antes de UC028 e das próximas evoluções funcionais
- **Alteração de schema lógico:** não intencional
- **Alteração de provider físico:** sim — SQLite → SQL Server
- **Migration:** sim — rebaseline para SQL Server
- **Alteração de domínio:** não
- **Alteração de regras de negócio:** não

## Objetivo

Substituir integralmente o SQLite pelo Microsoft SQL Server como provider de persistência do Precificador, mantendo o comportamento funcional já implementado e preparando a mesma base técnica para uso posterior com Azure SQL.

Após a MEL020:

```text
aplicação local
-> SQL Server

testes de integração
-> SQL Server real

futuro Azure SQL
-> mesmo provider EF Core SQL Server
```

A mudança é de infraestrutura e persistência. Não deve introduzir funcionalidade de negócio nova.

## Motivação

O projeto nasceu com SQLite por simplicidade local-first.

O novo objetivo operacional é:

- desenvolvimento com SQL Server local;
- publicação futura em Azure App Service;
- banco futuro em Azure SQL;
- redução das diferenças entre o banco usado em desenvolvimento/testes e o banco de publicação;
- validação das regras de persistência no mesmo mecanismo relacional que será usado em nuvem.

O provider oficial `Microsoft.EntityFrameworkCore.SqlServer` suporta SQL Server e Azure SQL, portanto não deve existir provider específico para Azure nesta aplicação.

## Decisões centrais

### D1 — SQL Server passa a ser o único provider suportado

Remover a dependência de:

```text
Microsoft.EntityFrameworkCore.Sqlite
Microsoft.Data.Sqlite
UseSqlite(...)
```

e substituir por:

```text
Microsoft.EntityFrameworkCore.SqlServer
UseSqlServer(...)
```

Após a MEL020, SQLite não é provider alternativo suportado.

Não manter dois providers apenas por compatibilidade histórica.

### D2 — desenvolvimento usa SQL Server local

O ambiente `Development` deve apontar para uma instância SQL Server local.

Referência recomendada para Windows/Visual Studio:

```text
Server=(localdb)\MSSQLLocalDB;
Database=Precificador;
Trusted_Connection=True;
TrustServerCertificate=True;
```

A instância pode ser sobrescrita por configuração.

Não codificar nome de máquina, usuário, senha ou segredo no código-fonte.

### D3 — configuração por ambiente

A aplicação continua consumindo:

```text
ConnectionStrings:Precificador
```

A origem concreta deve obedecer ao pipeline normal de configuração ASP.NET Core.

#### Development

Pode existir uma connection string local não sensível em:

```text
appsettings.Development.json
```

com LocalDB/SQL Server local.

Também deve ser possível sobrescrever por:

```text
ConnectionStrings__Precificador
```

ou User Secrets.

#### Production / Azure

Não versionar credenciais.

A connection string deve ser fornecida pelo ambiente/plataforma, mantendo a mesma chave:

```text
ConnectionStrings__Precificador
```

MEL020 apenas prepara a aplicação para esse contrato.

Provisionar Azure SQL, criar App Service, configurar secrets no Azure e publicar a aplicação pertencem ao item posterior de implantação Azure.

### D4 — não tentar executar migrations SQLite em SQL Server

A cadeia atual de migrations contém artefatos específicos do SQLite, incluindo:

```text
Sqlite:Autoincrement
type: "INTEGER"
type: "TEXT"
PRAGMA em testes
```

Portanto, não tratar as migrations atuais como portáveis entre providers.

Como SQLite deixará de ser suportado, MEL020 deve realizar um **rebaseline de migrations para SQL Server**.

### D5 — rebaseline SQL Server

A implementação deve:

1. considerar o modelo EF atual como fonte do schema lógico;
2. remover da cadeia ativa as migrations SQLite existentes;
3. gerar uma migration inicial nova para SQL Server, por exemplo:

```text
InitialSqlServer
```

4. gerar novo `PrecificadorDbContextModelSnapshot` usando SQL Server;
5. revisar manualmente a migration gerada antes do merge;
6. validar que um banco SQL Server vazio chega ao schema atual somente com a nova cadeia.

Esta MEL é uma exceção explícita às regras antigas de “não editar/remover migrations históricas”, porque ocorre troca deliberada de provider **antes da publicação produtiva**.

Não renomear migrations SQLite e fingir que são SQL Server.

Não executar `EnsureCreated`.

### D6 — banco SQLite existente não é atualizado in-place

EF Core migrations não transportam dados entre motores de banco diferentes.

Portanto:

```text
precificador.db
!=
banco SQL Server atualizado pela migration
```

A MEL020 não implementa ETL automático do arquivo SQLite para SQL Server.

Para o estado atual do projeto, o banco local SQLite deve ser considerado base de desenvolvimento/teste descartável e o SQL Server nasce pelo novo baseline.

Se surgir necessidade de preservar dados reais existentes no SQLite, criar item separado de migração de dados/exportação-importação antes de apagar a origem.

Não adicionar código temporário de cópia de dados ao startup.

## Provider EF Core

### Pacotes

`Precificador.Infrastructure` deve:

- remover `Microsoft.EntityFrameworkCore.Sqlite`;
- adicionar `Microsoft.EntityFrameworkCore.SqlServer` na mesma linha de versão EF Core do projeto;
- manter `Microsoft.EntityFrameworkCore.Design`;
- manter `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.

Testes não devem depender de `Microsoft.Data.Sqlite` após a migração.

### Runtime

Em `Program.cs`:

```csharp
builder.Services.AddDbContext<PrecificadorDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Precificador")));
```

Solução equivalente é aceitável.

Não criar seleção dinâmica entre SQLite e SQL Server.

### Design time

`DesignTimePrecificadorDbContextFactory` deve passar a criar o contexto com SQL Server.

Não manter:

```csharp
UseSqlite("Data Source=precificador.db")
```

O design-time deve usar uma connection string SQL Server local segura ou configuração equivalente.

É aceitável:

- ler variável de ambiente;
- usar fallback LocalDB não sensível;
- ou remover a duplicação por outro mecanismo simples que continue permitindo `dotnet ef migrations ...`.

Não incluir senha fixa.

## Revisão do modelo físico

A troca de provider exige revisar explicitamente o schema gerado.

### Inteiros e identity

Chaves inteiras geradas devem usar identity do SQL Server.

Não preservar `Sqlite:Autoincrement`.

### Booleanos

Propriedades `bool` devem mapear corretamente para `bit`.

Não alterar semântica de `Ativo`, flags ou estados.

### Strings

Revisar:

- `nvarchar` / `varchar` conforme geração padrão;
- `HasMaxLength` já definidos;
- índices únicos;
- collation.

Não remover normalização explícita de nomes só porque SQL Server normalmente usa collation case-insensitive.

As regras de `NomeNormalizado` e demais normalizações continuam pertencendo à aplicação.

### Decimais

Preservar todas as precisões declaradas pelo modelo.

Exemplos existentes incluem:

```text
decimal(18,6)
decimal(18,4)
decimal(9,6)
```

A migration SQL Server deve produzir tipos `decimal(p,s)` correspondentes.

Não reduzir escala.

Não arredondar valores na persistência além do que o modelo já determina.

Preservar MEL015 e RN026.

### Datas

`DateOnly` deve resultar em tipo SQL Server compatível, preferencialmente `date` pelo provider.

Preservar toda semântica de Data de referência e Data operacional.

Não introduzir `datetime` apenas para acomodar provider.

### Chaves estrangeiras e delete behavior

Revalidar todas as FKs e respectivos `DeleteBehavior`:

- Empresa;
- Insumo;
- Produto;
- Ficha Técnica;
- Item da Ficha;
- Uso de Equipamento;
- Preços;
- Registros de preço;
- Identity;
- UsuarioEmpresa;
- Configuração de precificação.

Nenhuma relação deve mudar de cascade/restrict por efeito acidental da troca de provider.

### Índices e unicidade

Preservar todos os índices funcionais, em especial:

- unicidade de Empresa;
- identidade consolidada de Insumo;
- unicidade de Produto por Empresa;
- Ficha Técnica única por Produto;
- configuração 1:1 por Empresa;
- índices de Identity;
- demais índices da cadeia atual.

O teste deve provar a semântica, não apenas comparar nomes de índice.

## Seed e primeiro uso

O rebaseline SQL Server deve preservar a Empresa técnica:

```text
Id = 1
Nome = Empresa inicial
NomeNormalizado = EMPRESA INICIAL
TimeZoneId = America/Sao_Paulo
Ativo = true
```

Isso é requisito para FT002 e para `/Setup`.

O primeiro uso continua:

```text
banco vazio
-> migration SQL Server
-> Empresa técnica existe
-> /Setup disponível
-> usuário cria empresa/credencial
```

Não criar usuário padrão.

## Migrations no startup

### Development — manter MEL011

O comportamento existente continua correto:

```csharp
if (app.Environment.IsDevelopment())
{
    ...
    await context.Database.MigrateAsync();
}
```

`MigrateAsync()` já verifica a tabela de histórico e aplica somente migrations pendentes.

Não é necessário executar antes:

```csharp
GetPendingMigrationsAsync()
```

apenas para decidir se `MigrateAsync()` será chamado.

Quando não há migration pendente, `MigrateAsync()` não altera o schema.

Portanto, MEL020 deve **preservar** o startup migration automático em Development e adaptar seu teste de regressão para SQL Server.

### Production / Azure — não habilitar nesta MEL

Não ampliar o bloco para Production.

Motivos:

- a identidade da aplicação precisaria permissão DDL;
- migration passaria a ocorrer junto com o startup da aplicação;
- rollback e revisão operacional ficam mais difíceis;
- futuramente pode haver mais de uma instância da aplicação;
- implantação Azure merece um passo explícito de schema.

Para Azure/Production, a direção é aplicar migrations por etapa de deploy, preferencialmente:

- migration bundle;
- script idempotente;
- ou processo operacional equivalente.

Isso pertence ao item de implantação Azure.

## Connection resiliency

A MEL020 não precisa introduzir lógica de retry específica de Azure para ser considerada concluída.

Não hardcodar LocalDB em código de runtime.

A configuração deve permitir que futuramente a mesma chamada `UseSqlServer` receba uma connection string Azure SQL sem mudança de domínio ou DbContext.

`EnableRetryOnFailure` pode ser avaliado no item de implantação Azure, quando o comportamento operacional real estiver definido.

## Testes de integração

### Princípio

Após MEL020, teste de integração de persistência não pode continuar validando SQLite.

A suíte deve usar **SQL Server real**.

Não usar:

- provider InMemory do EF;
- SQLite in-memory como substituto;
- mocks do DbContext para validar constraints.

### Estratégia recomendada — SQL Server em container

Usar `Testcontainers.MsSql` ou infraestrutura equivalente.

Referência de imagem:

```text
mcr.microsoft.com/mssql/server:2022-latest
```

Objetivo:

- mesmo mecanismo relacional em desenvolvimento de testes e CI;
- execução Linux no GitHub Actions;
- isolamento de bancos;
- sem depender da instância pessoal do desenvolvedor.

### Escopo do container

Evitar um container novo por teste individual.

Preferir:

```text
1 container SQL Server por assembly/sessão de testes
+
1 database isolado por factory/fixture/cenário que precise isolamento
```

ou solução equivalente com custo de inicialização controlado.

Cada banco de teste deve possuir nome exclusivo.

Testes nunca podem apontar para o banco local `Precificador` usado manualmente.

### Limpeza

Ao encerrar a fixture/factory:

- fechar pools/conexões;
- remover database temporário quando aplicável;
- liberar container ao final da suíte.

Evitar estado residual entre execuções.

### Paralelismo

Revisar o paralelismo da suíte.

Se fixtures compartilharem a mesma database, serializar explicitamente os testes afetados.

Preferir isolamento por database a desabilitar paralelismo global sem necessidade.

## Adaptação dos testes existentes

A MEL020 deve revisar toda referência a SQLite.

Exemplos conhecidos:

```text
SqliteConnection
UseSqlite
Data Source=:memory:
PRAGMA
SQLite Error
migrations históricas SQLite por nome
SQL literal dependente de afinidade TEXT/INTEGER
```

### Testes Web

`CustomWebApplicationFactory` deve substituir a conexão SQLite in-memory por database SQL Server isolado.

O comportamento Web deve permanecer idêntico.

### Testes de persistência

Helpers de contexto devem usar `UseSqlServer`.

Constraints devem ser validadas pelo SQL Server real.

### Testes de migration histórica

Há testes atuais que sobem para migrations intermediárias SQLite, por exemplo:

```text
CreateInsumos
AddMultiempresaIdentity
AddProdutos
AddFichasTecnicas
...
```

Como MEL020 rebaselineia a cadeia, esses IDs deixam de representar uma rota suportada de upgrade.

Esses testes não devem ser mantidos artificialmente apenas para preservar migrations SQLite.

Regra:

- preservar a **cobertura funcional** relevante;
- substituir testes de upgrade histórico por testes do baseline SQL Server quando a regra continua necessária;
- remover somente testes cuja única razão de existir era validar uma transição interna da antiga cadeia SQLite;
- toda remoção deve ser justificável no diff/PR e não pode eliminar cobertura de regra de negócio, FK, índice, seed ou isolamento.

### SQL bruto de teste

SQL manual usado para preparar cenários deve ser revisado para sintaxe/tipos SQL Server.

Preferir API EF para arrange quando SQL bruto não for requisito do teste.

Quando SQL bruto for necessário, parametrizar valores.

## Teste de migration SQL Server

Criar cobertura explícita para banco vazio:

1. criar database SQL Server temporário vazio;
2. confirmar ausência de tabelas da aplicação;
3. executar `Database.MigrateAsync()`;
4. confirmar `GetPendingMigrationsAsync()` vazio;
5. confirmar tabelas principais;
6. confirmar tabelas Identity;
7. confirmar Empresa técnica;
8. confirmar principais FKs/índices;
9. executar `MigrateAsync()` novamente;
10. confirmar idempotência.

## Teste MEL011 em SQL Server

Adaptar a regressão de primeiro uso:

1. iniciar aplicação em `Development`;
2. apontar para database SQL Server novo;
3. não aplicar migration manualmente;
4. startup executa `MigrateAsync()`;
5. GET `/Conta/Login` encaminha para `/Setup` quando não há usuário;
6. GET `/Setup` funciona;
7. nenhuma migration permanece pendente.

Isso prova que a troca de provider não reintroduziu o erro de primeiro uso.

## CI

O workflow deve executar a suíte completa com SQL Server disponível.

Com Testcontainers, o runner GitHub-hosted deve usar Docker.

Se for adotado service container em vez de Testcontainers, ele deve ser configurado no workflow de forma reproduzível.

A CI não pode:

- depender de SQL Server instalado manualmente no runner;
- pular testes de integração quando SQL Server não estiver disponível;
- continuar usando SQLite silenciosamente.

## Desenvolvimento local

Após MEL020, o fluxo esperado é:

```text
1. SQL Server/LocalDB disponível
2. ConnectionStrings:Precificador configurada
3. dotnet run
4. Development executa MigrateAsync()
5. aplicação pronta
```

Também deve continuar possível usar:

```text
dotnet ef database update
```

para intervenção explícita.

## Azure SQL — preparação, não implantação

MEL020 deve deixar a aplicação tecnicamente compatível com uma futura connection string Azure SQL.

Isso significa:

- provider SQL Server;
- migrations SQL Server;
- tipos suportados pelo Azure SQL;
- ausência de dependência de arquivo local;
- ausência de caminho relativo de banco;
- configuração externa da connection string;
- testes no mesmo provider.

Não inclui:

- criar servidor Azure SQL;
- criar database no Azure;
- firewall/networking;
- Managed Identity;
- App Service;
- Key Vault;
- pipeline de deployment;
- migration bundle no deploy;
- smoke test em Azure.

Esses pontos devem ser tratados na história de implantação Azure.

## Compatibilidade funcional obrigatória

A troca de provider não pode alterar:

- autenticação;
- bootstrap Setup;
- multiempresa;
- GQF;
- write guards;
- isolamento cross-tenant;
- regras de Insumo;
- histórico de preços;
- Produtos;
- Ficha Técnica;
- custos;
- precificação;
- snapshots;
- margem;
- formatação pt-BR;
- datas operacionais;
- navegação.

Toda a suíte que cobre esses contratos deve permanecer verde após adaptação para SQL Server.

## Critérios de aceitação

- **CA01:** `Microsoft.EntityFrameworkCore.Sqlite` removido da infraestrutura.
- **CA02:** `Microsoft.EntityFrameworkCore.SqlServer` é o provider runtime.
- **CA03:** não existe `UseSqlite` no código ativo.
- **CA04:** Development utiliza connection string SQL Server.
- **CA05:** runtime lê `ConnectionStrings:Precificador`.
- **CA06:** Production pode sobrescrever connection string por configuração externa sem mudança de código.
- **CA07:** `DesignTimePrecificadorDbContextFactory` funciona com SQL Server.
- **CA08:** migrations SQLite não permanecem como cadeia ativa do contexto.
- **CA09:** existe baseline SQL Server novo e coerente com o modelo atual.
- **CA10:** ModelSnapshot é gerado pelo provider SQL Server.
- **CA11:** banco SQL Server vazio migra até o estado atual.
- **CA12:** não há migrations pendentes após `MigrateAsync()`.
- **CA13:** segunda execução de `MigrateAsync()` é segura/idempotente.
- **CA14:** Empresa técnica inicial é criada corretamente.
- **CA15:** Setup de primeiro uso continua funcional.
- **CA16:** chaves `int` geradas usam identity SQL Server.
- **CA17:** bools preservam semântica.
- **CA18:** `DateOnly` preserva semântica de data sem hora.
- **CA19:** precisões decimais do modelo são preservadas.
- **CA20:** FKs e DeleteBehavior permanecem funcionalmente equivalentes.
- **CA21:** índices e unicidades permanecem funcionalmente equivalentes.
- **CA22:** normalização de nomes continua independente de collation.
- **CA23:** GQF permanece funcional em SQL Server.
- **CA24:** write guards permanecem funcionais em SQL Server.
- **CA25:** testes de integração não usam SQLite/InMemory.
- **CA26:** `CustomWebApplicationFactory` usa database SQL Server isolado.
- **CA27:** testes de persistência usam SQL Server real.
- **CA28:** testes históricos SQLite são substituídos sem perda de cobertura funcional relevante.
- **CA29:** nenhum teste toca o banco local manual do desenvolvedor.
- **CA30:** CI executa a suíte de integração em SQL Server.
- **CA31:** suíte unitária completa permanece verde.
- **CA32:** suíte de integração completa permanece verde.
- **CA33:** build Release possui 0 erros e nenhum warning novo relevante.
- **CA34:** startup em Development continua aplicando migrations pendentes automaticamente.
- **CA35:** startup fora de Development não passa a executar migrations automaticamente.
- **CA36:** não existe `EnsureCreated` no fluxo.
- **CA37:** nenhuma regra de negócio é alterada pela troca de provider.
- **CA38:** nenhum código de Azure específico é necessário para executar localmente.
- **CA39:** aplicação aceita futuramente connection string Azure SQL sem troca de provider.
- **CA40:** não é implementado ETL automático do SQLite.
- **CA41:** UC028 e itens funcionais posteriores não são antecipados.

## Matriz mínima de testes

### W1 — provider runtime

Inspecionar/validar DI:

```text
PrecificadorDbContext
-> SQL Server
```

### W2 — baseline em banco vazio

Database SQL Server novo:

```text
MigrateAsync()
=> sucesso
GetPendingMigrationsAsync()
=> vazio
```

### W3 — idempotência de migration

```text
MigrateAsync()
MigrateAsync()
=> segunda chamada não altera schema nem falha
```

### W4 — seed técnico

Após migration:

```text
Empresa Id 1
NomeNormalizado = EMPRESA INICIAL
Ativa
Timezone padrão
```

### W5 — Identity

Confirmar tabelas Identity consultáveis e primeiro Setup funcional.

### W6 — Development startup

Aplicação Development contra database novo, sem migration manual:

```text
startup
-> migration
-> /Setup disponível
```

### W7 — constraints SQL Server

Cobrir ao menos:

- FK inválida;
- unicidade relevante;
- identity;
- Restrict onde esperado.

### W8 — precisão decimal

Persistir e reler valores com escala relevante, incluindo casos MEL015/RN026.

### W9 — data

Persistir/reler `DateOnly` sem deslocamento/time component.

### W10 — tenant isolation

Executar regressão cross-tenant completa em SQL Server.

### W11 — Web factory

Dois factories/bancos independentes não vazam dados entre si.

### W12 — migrations pendentes

Depois de criar banco do zero:

```text
GetPendingMigrationsAsync()
=> []
```

### W13 — nenhuma dependência SQLite ativa

Busca no código ativo:

```text
UseSqlite
SqliteConnection
Microsoft.Data.Sqlite
Microsoft.EntityFrameworkCore.Sqlite
```

deve retornar apenas documentação histórica, se mantida, e nunca runtime/testes ativos.

### W14 — suíte de regressão

Executar:

```text
dotnet build Precificador.slnx --configuration Release
dotnet test Precificador.slnx --configuration Release --no-build
```

Tudo verde.

## Arquivos esperados

Mudanças esperadas, não exaustivas:

```text
src/Precificador.Infrastructure/Precificador.Infrastructure.csproj
src/Precificador.Infrastructure/Persistence/DesignTimePrecificadorDbContextFactory.cs
src/Precificador.Infrastructure/Migrations/*
src/Precificador.Web/Program.cs
src/Precificador.Web/appsettings*.json

tests/Precificador.Tests.Integration/*
.github/workflows/*              (se necessário para SQL Server)

docs/development/improvements/MEL011-primeiro-uso-banco-local.md
docs/development/foundation-technical.md
docs/architecture/architecture.md
docs/development/backlog.md
```

Documentos que afirmam SQLite como arquitetura atual devem ser atualizados na PR de implementação.

## Fora do escopo

- Azure App Service;
- provisionamento de Azure SQL;
- criação de recursos Azure;
- Key Vault;
- Managed Identity;
- pipeline completo de deploy Azure;
- migração automática dos dados do arquivo `precificador.db`;
- suporte simultâneo SQLite + SQL Server;
- nova funcionalidade de negócio;
- UC028+.

## Definition of Done específica

MEL020 está concluída quando:

- SQL Server é o único provider ativo;
- development funciona com SQL Server local;
- migrations SQL Server foram rebaselineadas e revisadas;
- banco SQL Server vazio sobe integralmente por migrations;
- MEL011 funciona em SQL Server;
- integração não usa SQLite/InMemory;
- CI executa a suíte contra SQL Server;
- cobertura funcional relevante das migrations antigas foi preservada/substituída;
- suíte completa está verde;
- build Release sem warnings novos relevantes;
- documentação arquitetural não afirma mais que SQLite é o provider atual;
- backlog altera somente MEL020 de `Pronto` para `Concluído` entre os itens pendentes;
- UC028 continua sem implementação.

## Branch sugerida

```text
infra/mel020-sqlserver
```
