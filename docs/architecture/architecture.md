# Arquitetura

## Estilo

O Precificador é um **monólito web** com execução local para desenvolvimento e publicação controlada em Azure para piloto operacional interno, preparado para múltiplas empresas e usuários autenticados.

```text
Navegador
   |
ASP.NET Core / Razor Pages
   |-- ASP.NET Core Identity
   |-- Empresa Ativa
   |
Regras de domínio
   |
Entity Framework Core
   |-- isolamento tenant-aware
   |
SQL Server / Azure SQL
```

## Stack

- .NET 10 LTS
- ASP.NET Core Razor Pages
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server (local/LocalDB em desenvolvimento e testes; Azure SQL na publicação) — ver [MEL020](../development/improvements/MEL020-sqlserver.md) e [MEL021](../development/improvements/MEL021-publicacao-azure.md)
- Bootstrap
- JavaScript mínimo
- xUnit
- GitHub Actions para CI

## Estrutura prevista da solution

```text
Precificador.slnx

src/
  Precificador.Web/
  Precificador.Core/
  Precificador.Infrastructure/

tests/
  Precificador.Tests.Unit/
  Precificador.Tests.Integration/
```

Os três projetos em `src` continuam sendo o limite inicial de projetos de produção. Não criar projeto adicional apenas para autenticação/multi-tenancy sem necessidade real.

## Responsabilidades

### Precificador.Core

- entidades e objetos de domínio;
- regras de negócio;
- `Empresa` e contratos estritamente necessários ao tenant ownership;
- cálculos de custo, margem e preço;
- nenhuma dependência da camada Web ou de ASP.NET Core Identity.

### Precificador.Infrastructure

- EF Core;
- `DbContext`;
- mapeamentos;
- migrations;
- SQL Server/Azure SQL;
- integração Identity/EF;
- garantias centrais de isolamento de persistência;
- seed de desenvolvimento quando existir entidade que o justifique.

### Precificador.Web

- Razor Pages;
- PageModels;
- login/logout/bootstrap;
- seleção/resolução da Empresa Ativa;
- implementação do contexto de empresa sobre HTTP/Session;
- validações de entrada e apresentação.

## Dependências

```text
Web -> Core
Web -> Infrastructure
Infrastructure -> Core
Core -> nenhuma camada da aplicação
```

## Multiempresa

Entidades operacionais pertencentes a uma empresa possuem `EmpresaId` obrigatório. `Insumo` é a primeira delas.

Consultas comuns tenant-owned devem ser isoladas centralmente pelo EF Core, preferencialmente com Global Query Filters. Escritas devem validar a Empresa Ativa e rejeitar operações cross-tenant. Índices de unicidade cujo escopo é a empresa incluem `EmpresaId`.

`Empresa`, `UsuarioEmpresa` e tabelas Identity precisam permanecer consultáveis para autenticação/resolução de contexto e não usam o filtro tenant padrão.

Dados administrativos que existem **antes** da criação de um tenant também são globais. A `SolicitacaoAcessoEmpresa` nasce pré-tenant na UC038, sem `EmpresaId` e sem Global Query Filter. A UC039 pode preencher um `EmpresaId` nullable apenas como referência da decisão Aprovada; a solicitação continua global e nunca passa a usar o filtro tenant.

## Autenticação e autorização

ASP.NET Core Identity autentica o usuário. A autorização possui dois planos explícitos após a FT003.

### Plano global

A role Identity `SystemAdmin` representa autoridade sobre o próprio sistema.

```text
SystemAdmin
+ autenticação válida
= acesso à área /Admin
```

`SystemAdmin` não exige Empresa Ativa e não recebe bypass automático para dados tenant-owned.

### Plano empresarial

`UsuarioEmpresa` determina quais Empresas podem ser ativadas e passa a carregar `Perfil = Operacional | Administrador`.

A regra operacional permanece:

```text
usuário autenticado
+ vínculo ativo
+ empresa ativa
= acesso aos dados daquela empresa
```

A autorização administrativa da Empresa acrescenta:

```text
+ Perfil == Administrador
```

Administrador de Empresa não é IdentityRole global.

Ver [ADR-011](adr/ADR-011-dois-planos-autorizacao.md) e [FT003](../development/foundation-administration-authorization.md).

### Credenciais, tokens e e-mail

A partir da UC040:

- ativação de conta e recuperação de senha permanecem no ASP.NET Core Identity;
- providers de token de ativação e recuperação possuem propósito/validade independentes;
- tokens não são persistidos;
- o key ring do ASP.NET Core Data Protection é global e persistido no SQL Server/Azure SQL;
- envio de e-mail é abstraído de SMTP e não depende de fornecedor específico;
- links sensíveis usam uma URL pública canônica configurada, não o Host do request em Production.

Ver [ADR-012](adr/ADR-012-email-tokens-data-protection.md) e [UC040](../use-cases/UC040-ativar-conta-recuperar-acesso.md).

### Administração global de Empresas

A UC039 mantém Empresa.Ativo como gate operacional consolidado e adiciona metadados administrativos mínimos:

- EhTecnica distingue o seed técnico de Empresas reais;
- EncerradaEmUtc distingue suspensão reversível de encerramento lógico terminal;
- SystemAdmin decide solicitações e cria o tenant real sem receber Empresa Ativa;
- criação de Empresa inclui ConfiguracaoPrecificacaoEmpresa padrão e primeiro UsuarioEmpresa Administrador na mesma unidade transacional;
- e-mail é side effect pós-commit;
- hard delete de Empresa não faz parte do fluxo normal.

Ver [ADR-013](adr/ADR-013-ciclo-vida-empresa-seed-tecnico.md) e [UC039](../use-cases/UC039-administrar-empresas-solicitacoes.md).

## Diretrizes

- Regras financeiras não devem residir na UI.
- EF Core é a abstração padrão de persistência; não criar repository genérico/Unit of Work customizado sem necessidade comprovada.
- Preferir serviços de domínio simples e funções explícitas a padrões adicionais.
- Não usar CQRS/MediatR como padrão.
- Não criar API separada para a interface Razor Pages no MVP.
- Não usar SPA framework por padrão.
- Alterações de banco usam migrations.
- Em `Development`, aplicar migrations pendentes no startup antes de atender requests, para tornar o primeiro uso local determinístico.
- Fora de `Development`, migrations permanecem explícitas e não são executadas automaticamente no startup.
- Não usar `EnsureCreated` na aplicação.
- Não aceitar `EmpresaId` vindo do formulário como fonte de ownership.
- `IgnoreQueryFilters` exige justificativa explícita.
- testes cross-tenant são obrigatórios para entidades tenant-owned.

## Persistência calculada x armazenada

Devem ser armazenados fatos e decisões do usuário, como históricos, composição e configurações. Custos atuais, margens e preços derivados continuam calculados sob demanda.

Configurações futuras são scoped por Empresa.

## Modelo produtivo

A Ficha Técnica futura deve ser genérica. Forno não é um conceito obrigatório do modelo; será tratado como equipamento/recurso quando esse domínio for detalhado.

## Segurança e operação

A fase atual continua priorizando simplicidade operacional e baixo custo, porém não é mais de usuário único. Autenticação é obrigatória para áreas de negócio após FT002.

A publicação Azure inicial usa App Service F1/Linux e Azure SQL Database Free offer, sem SLA, sem custom domain e sem recursos pagos auxiliares. Em `Development`, migrations podem ser aplicadas automaticamente no startup; fora de `Development`, migrations permanecem uma etapa explícita de deploy e a identidade runtime não recebe permissão DDL.

## Observabilidade

Utilizar logging padrão do ASP.NET Core.

## Integração contínua

Restore, build e testes permanecem gates mínimos do GitHub Actions.
