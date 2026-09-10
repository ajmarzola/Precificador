# Arquitetura

## Estilo

O Precificador será um **monólito web local-first**, agora preparado para múltiplas empresas e usuários autenticados.

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
SQLite
```

## Stack

- .NET 10 LTS
- ASP.NET Core Razor Pages
- ASP.NET Core Identity
- Entity Framework Core
- SQLite nesta fase
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
- SQLite;
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

## Autenticação e autorização

ASP.NET Core Identity autentica o usuário. `UsuarioEmpresa` determina quais empresas podem ser ativadas.

A regra inicial é:

```text
usuário autenticado
+ vínculo ativo
+ empresa ativa
= acesso aos dados daquela empresa
```

Roles/permissões granulares não são parte da FT002.

## Diretrizes

- Regras financeiras não devem residir na UI.
- EF Core é a abstração padrão de persistência; não criar repository genérico/Unit of Work customizado sem necessidade comprovada.
- Preferir serviços de domínio simples e funções explícitas a padrões adicionais.
- Não usar CQRS/MediatR como padrão.
- Não criar API separada para a interface Razor Pages no MVP.
- Não usar SPA framework por padrão.
- Alterações de banco usam migrations.
- Não executar auto-migration no startup.
- Não aceitar `EmpresaId` vindo do formulário como fonte de ownership.
- `IgnoreQueryFilters` exige justificativa explícita.
- testes cross-tenant são obrigatórios para entidades tenant-owned.

## Persistência calculada x armazenada

Devem ser armazenados fatos e decisões do usuário, como históricos, composição e configurações. Custos atuais, margens e preços derivados continuam calculados sob demanda.

Configurações futuras são scoped por Empresa.

## Modelo produtivo

A Ficha Técnica futura deve ser genérica. Forno não é um conceito obrigatório do modelo; será tratado como equipamento/recurso quando esse domínio for detalhado.

## Segurança e operação

A fase atual continua local-first, porém não é mais de usuário único. Autenticação é obrigatória para áreas de negócio após FT002.

SQLite permanece até definição de publicação. Se houver acesso remoto concorrente relevante, a persistência será reavaliada sem alterar as regras do Core.

## Observabilidade

Utilizar logging padrão do ASP.NET Core.

## Integração contínua

Restore, build e testes permanecem gates mínimos do GitHub Actions.
