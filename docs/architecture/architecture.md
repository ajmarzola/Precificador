# Arquitetura

## Estilo

O Precificador permanece um **monólito web local-first**, agora preparado para múltiplas empresas e usuários autenticados.

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
   |-- Global Query Filters tenant-aware
   |-- guard de escrita tenant-aware
   |
SQLite
```

## Stack

- .NET 10 LTS;
- ASP.NET Core Razor Pages;
- ASP.NET Core Identity;
- EF Core;
- SQLite nesta fase;
- Bootstrap/JavaScript mínimo;
- xUnit;
- GitHub Actions.

## Projetos

Mantém-se:

```text
Precificador.Web
Precificador.Core
Precificador.Infrastructure
Precificador.Tests.Unit
Precificador.Tests.Integration
```

Não criar novo projeto apenas para autenticação ou multi-tenancy nesta fase.

## Responsabilidades

### Core

- entidades e regras de domínio;
- `Empresa`;
- contratos simples necessários ao tenant ownership, sem dependência de HttpContext/Identity;
- cálculos financeiros.

### Infrastructure

- EF Core/SQLite;
- DbContext/migrations;
- ASP.NET Core Identity EF integration;
- `UsuarioAplicacao`;
- mapeamentos e garantias de persistência;
- query filters/guard de escrita quando tecnicamente apropriados.

### Web

- Razor Pages;
- login/logout/bootstrap;
- resolução/seleção de Empresa Ativa;
- implementação de `IEmpresaContext` sobre contexto HTTP/Session;
- validações e apresentação.

## Dependências

```text
Web -> Core
Web -> Infrastructure
Infrastructure -> Core
Core -> nenhuma camada da aplicação
```

## Multi-tenancy

Adotar banco compartilhado com `EmpresaId` obrigatório nas entidades tenant-owned.

Atualmente `Insumo` é tenant-owned. Toda nova entidade operacional deverá declarar explicitamente se pertence a uma empresa.

Consultas comuns tenant-owned devem ser isoladas centralmente. Escritas devem validar Empresa Ativa. Índices de unicidade tenant-scoped incluem `EmpresaId`.

`Empresa`, `UsuarioEmpresa` e tabelas Identity não usam o filtro tenant padrão porque são necessárias para autenticação e resolução do contexto.

## Autenticação e autorização

Identity autentica o usuário. O vínculo `UsuarioEmpresa` autoriza quais empresas podem ser ativadas.

A primeira versão não usa roles. A regra mínima é:

```text
usuário autenticado
+ vínculo ativo
+ empresa ativa
= acesso aos dados daquela empresa
```

## Persistência calculada x armazenada

Continuam armazenados fatos e decisões; custos derivados continuam calculados sob demanda.

Configurações e históricos futuros são scoped por Empresa.

## Modelo produtivo

A Ficha Técnica futura é genérica. Forno não é conceito arquitetural obrigatório; será um Equipamento quando esse domínio for implementado.

## SQLite e publicação

SQLite permanece a decisão atual. A estratégia de publicação não está definida e será reavaliada separadamente. Se houver acesso concorrente remoto relevante, o banco servidor poderá substituir SQLite sem alterar o Core.

## Diretrizes

- não criar repository genérico/UoW customizado sem necessidade;
- não usar CQRS/MediatR como padrão;
- não criar API separada para Razor Pages;
- não usar SPA framework por padrão;
- schema muda somente por migrations;
- não executar auto-migration no startup;
- não armazenar EmpresaId vindo diretamente de formulário do usuário;
- `IgnoreQueryFilters` exige justificativa explícita;
- testes cross-tenant são obrigatórios para entidades tenant-owned.

## Observabilidade

Logging padrão ASP.NET Core.

## CI

Restore, build e testes permanecem gates mínimos do GitHub Actions.
