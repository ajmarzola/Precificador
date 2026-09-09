# FT001 — Fundação Técnica

- **Status:** Implementado
- **Tipo:** Fundação técnica, não é caso de uso funcional
- **Dependências:** Fundação documental aprovada e mergeada
- **Próximo incremento:** UC001 — Cadastrar insumo

## Objetivo

Criar a base executável, testável e integrada do Precificador sem introduzir ainda regras ou entidades de negócio.

Ao final desta entrega, o repositório deve possuir uma solution .NET 10 funcional, aplicação Razor Pages inicial, projetos separados conforme a arquitetura aprovada, Entity Framework Core configurado para SQLite, infraestrutura de testes e CI capaz de executar restore, build e testes em cada pull request.

## Princípio de escopo

Esta entrega prepara o terreno. Ela **não implementa nenhum caso de uso funcional**.

Não criar antecipadamente entidades como `Insumo`, `Produto`, `Receita`, serviços de precificação, repositories ou abstrações de domínio que pertencem aos UCs seguintes.

## Estrutura esperada

```text
Precificador.slnx

global.json
.editorconfig
.gitignore

src/
  Precificador.Core/
    Precificador.Core.csproj

  Precificador.Infrastructure/
    Precificador.Infrastructure.csproj
    Persistence/
      PrecificadorDbContext.cs

  Precificador.Web/
    Precificador.Web.csproj
    Program.cs
    Pages/
    appsettings.json
    appsettings.Development.json

tests/
  Precificador.Tests.Unit/
    Precificador.Tests.Unit.csproj

  Precificador.Tests.Integration/
    Precificador.Tests.Integration.csproj
    Web/
      HomePageTests.cs
    Infrastructure/
      DatabaseConfigurationTests.cs

.github/
  workflows/
    ci.yml

.config/
  dotnet-tools.json
```

A organização interna pode variar minimamente se o template oficial do SDK gerar arquivos adicionais, desde que as responsabilidades e dependências documentadas sejam preservadas.

## Tecnologia

- .NET 10 LTS;
- ASP.NET Core Razor Pages;
- Entity Framework Core 10;
- provider SQLite do EF Core;
- xUnit;
- `Microsoft.AspNetCore.Mvc.Testing` para smoke test web;
- GitHub Actions usando runner padrão Ubuntu.

Pacotes da família EF Core devem usar a mesma versão estável `10.0.x`. A versão patch exata deve ser resolvida no momento da implementação e permanecer explicitamente registrada nos arquivos de projeto/tool manifest; não usar versões preview.

## Solution

Utilizar o formato **SLNX**, que é o padrão do SDK .NET 10.

Nome:

```text
Precificador.slnx
```

Todos os projetos de produção e teste devem fazer parte da solution.

## SDK

Criar `global.json` declarando .NET 10 e permitindo roll-forward apenas dentro de versões compatíveis da linha estável 10.x. Não permitir SDK preview como requisito para o projeto.

O objetivo não é fixar a aplicação a um patch obsoleto, mas garantir que um SDK de outra versão principal não seja selecionado silenciosamente.

## Projetos e dependências

### Precificador.Core

- `classlib`;
- target `net10.0`;
- sem referência aos demais projetos;
- sem pacotes de infraestrutura/web;
- remover o `Class1.cs` gerado pelo template;
- pode permanecer sem classes até o primeiro UC de domínio.

### Precificador.Infrastructure

- `classlib`;
- target `net10.0`;
- referência para `Precificador.Core`;
- referência ao provider `Microsoft.EntityFrameworkCore.Sqlite`;
- suporte às ferramentas de design do EF Core para migrations futuras;
- conter `PrecificadorDbContext` mínimo, sem `DbSet` nesta entrega.

### Precificador.Web

- template Razor Pages;
- target `net10.0`;
- referências para `Precificador.Core` e `Precificador.Infrastructure`;
- registrar `PrecificadorDbContext` no container de DI;
- ler a connection string por configuração;
- manter logging padrão do ASP.NET Core;
- permitir `WebApplicationFactory<Program>` nos testes de integração sem alterar o comportamento da aplicação.

### Precificador.Tests.Unit

- projeto xUnit;
- target `net10.0`;
- referência para `Precificador.Core`;
- remover teste placeholder gerado pelo template;
- nenhum teste unitário artificial é necessário nesta fundação, pois nenhuma regra de negócio será implementada.

### Precificador.Tests.Integration

- projeto xUnit;
- target `net10.0`;
- referências necessárias para testar `Precificador.Web` e `Precificador.Infrastructure`;
- usar `Microsoft.AspNetCore.Mvc.Testing` para smoke test;
- não depender de banco real do usuário.

## EF Core e SQLite

Criar `PrecificadorDbContext` com construtor baseado em `DbContextOptions<PrecificadorDbContext>`.

Registrar o contexto no `Program.cs` da aplicação web com `UseSqlite`.

Connection string inicial:

```text
Data Source=precificador.db
```

O arquivo do banco é dado local do usuário e deve estar ignorado pelo Git, incluindo arquivos auxiliares SQLite (`-shm`, `-wal` quando existirem).

### Migration inicial

**Não criar migration vazia nesta entrega.**

A primeira migration será criada quando o primeiro modelo persistente real existir, iniciando pelo UC001. Evitar artefatos sem valor de domínio.

### Seed

**Não criar seeder vazio ou dados artificiais de domínio nesta entrega.**

O seed de desenvolvimento começará quando houver a primeira entidade persistente que justifique dados de exemplo. A infraestrutura deve permitir sua introdução sem reestruturação, mas não deve antecipá-la com código morto.

## Tool manifest

Criar tool manifest local do .NET e registrar `dotnet-ef` em versão estável 10.0.x compatível com os pacotes EF Core selecionados.

Isso permite restaurar a ferramenta com:

```text
dotnet tool restore
```

## Testes obrigatórios desta fundação

### Integração — aplicação web

Criar smoke test que inicialize a aplicação com `WebApplicationFactory<Program>`, faça `GET /` e confirme resposta HTTP de sucesso.

O teste não deve depender de porta fixa nem de aplicação externa em execução.

### Integração — provider de banco

A partir do container da aplicação de teste, resolver `PrecificadorDbContext` e confirmar que o provider configurado é SQLite.

O teste não deve criar ou modificar o arquivo `precificador.db` real do usuário. A factory de teste pode substituir a connection string/registro de banco por SQLite temporário ou em memória quando necessário.

## Interface inicial

Manter a Razor Pages gerada pelo template em estado mínimo e funcional. É permitido ajustar a Home para identificar a aplicação como **Precificador**, mas não construir menus ou telas de Insumos/Produtos antes dos respectivos UCs.

Não adicionar SPA framework.

## EditorConfig

Criar `.editorconfig` mínimo para garantir pelo menos:

- UTF-8;
- indentação consistente;
- newline final;
- remoção de whitespace final em arquivos de código/configuração quando apropriado.

Não introduzir uma coleção extensa de regras/analyzers nesta entrega.

## Git ignore

Preservar exclusões úteis existentes e garantir exclusão de, no mínimo:

- `.vs/`;
- `bin/`;
- `obj/`;
- `TestResults/`;
- arquivos de cobertura gerados localmente;
- `*.db`;
- `*.db-shm`;
- `*.db-wal`;
- artefatos locais de IDE que não devam ser versionados.

## CI — GitHub Actions

Criar `.github/workflows/ci.yml`.

### Gatilhos

- `pull_request` para `master`;
- `push` em `master`.

### Runner

Usar runner padrão:

```text
ubuntu-latest
```

Não usar runner maior/pago.

### Passos mínimos

1. checkout;
2. setup do .NET 10;
3. `dotnet tool restore`;
4. `dotnet restore Precificador.slnx`;
5. `dotnet build Precificador.slnx --configuration Release --no-restore`;
6. `dotnet test Precificador.slnx --configuration Release --no-build`.

Não publicar artefatos nem coverage nesta primeira versão do workflow.

## Logging

Usar o logging padrão já fornecido pelo ASP.NET Core. Não adicionar Serilog, OpenTelemetry, Application Insights ou infraestrutura externa nesta entrega.

## Critérios de aceitação

### CA01 — Solution reproduzível

**Dado** um ambiente com SDK .NET 10 compatível  
**Quando** `dotnet restore Precificador.slnx` for executado  
**Então** todas as dependências devem ser restauradas sem erro.

### CA02 — Build completo

**Dado** o repositório restaurado  
**Quando** a solution for compilada em `Release`  
**Então** todos os projetos devem compilar sem erro e sem warnings novos relevantes.

### CA03 — Dependências arquiteturais

**Dado** os projetos da solution  
**Então** `Core` não deve depender de Web ou Infrastructure  
**E** `Infrastructure` deve depender apenas de Core entre projetos de produção  
**E** `Web` pode depender de Core e Infrastructure.

### CA04 — Aplicação inicializável

**Quando** `Precificador.Web` for iniciado  
**Então** a aplicação Razor Pages deve subir normalmente  
**E** a página inicial deve responder com sucesso.

### CA05 — SQLite configurado

**Dado** o container de DI da aplicação  
**Quando** `PrecificadorDbContext` for resolvido  
**Então** deve estar configurado para o provider SQLite.

### CA06 — Banco local não versionado

**Quando** um arquivo SQLite local for criado  
**Então** ele e seus arquivos auxiliares não devem aparecer como arquivos versionáveis no Git.

### CA07 — Infraestrutura de testes

**Quando** `dotnet test Precificador.slnx --configuration Release` for executado  
**Então** a suite deve concluir com sucesso  
**E** os smoke tests de integração definidos nesta fundação devem passar.

### CA08 — CI

**Dado** um pull request para `master`  
**Quando** o workflow de CI for executado  
**Então** restore, build e testes devem ser realizados automaticamente.

### CA09 — Sem domínio antecipado

**Ao revisar o diff**  
**Então** não devem existir entidades, CRUDs, regras de precificação, repositories genéricos ou páginas funcionais de Insumos/Produtos.

## Comandos de validação esperados

A implementação deve encerrar com execução bem-sucedida equivalente a:

```text
dotnet --info
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Quando possível, executar também a aplicação localmente para smoke manual, sem transformar essa verificação em dependência do teste automatizado.

## Definition of Done específica

Além de `definition-of-done.md`:

- todos os projetos estão incluídos em `Precificador.slnx`;
- referências entre projetos obedecem a arquitetura;
- EF Core/SQLite está configurado sem modelo artificial;
- `dotnet-ef` pode ser restaurado pelo tool manifest;
- smoke tests passam;
- CI existe e executa os mesmos quality gates principais;
- banco e artefatos locais estão ignorados;
- nenhuma funcionalidade de negócio foi antecipada.

## Fora do escopo

- UC001 ou qualquer CRUD;
- entidades de negócio;
- migration vazia;
- seed vazio;
- autenticação;
- Docker;
- publicação/hospedagem;
- banco servidor;
- API REST;
- Swagger/OpenAPI;
- frontend SPA;
- Selenium/Playwright;
- coverage gate;
- Dependabot;
- analyzers adicionais;
- arquitetura além da aprovada.
