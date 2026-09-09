# Arquitetura

## Estilo

O Precificador será um **monólito web local-first**, executado inicialmente na máquina do usuário e acessado pelo navegador em endereço local.

```text
Navegador
   |
ASP.NET Core / Razor Pages
   |
Regras de domínio
   |
Entity Framework Core
   |
SQLite
```

## Stack

- .NET 10 LTS
- ASP.NET Core Razor Pages
- Entity Framework Core
- SQLite
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

O formato SLNX é adotado conforme ADR-006. Os três projetos em `src` são o limite inicial de projetos de produção. Projetos de teste são separados para preservar clareza das dependências.

## Responsabilidades

### Precificador.Core

- entidades e objetos de domínio;
- regras de negócio;
- cálculos de custo, margem e preço;
- contratos estritamente necessários para o núcleo;
- nenhuma dependência da camada web.

### Precificador.Infrastructure

- EF Core;
- `DbContext`;
- mapeamentos;
- migrations;
- SQLite;
- seed de desenvolvimento quando existir entidade que o justifique;
- implementações de persistência necessárias.

### Precificador.Web

- Razor Pages;
- PageModels;
- composição da aplicação;
- validações de entrada próprias da interface;
- apresentação de resultados e mensagens.

## Dependências

A direção desejada é:

```text
Web -> Core
Web -> Infrastructure
Infrastructure -> Core
Core -> nenhuma camada da aplicação
```

## Diretrizes

- Regras financeiras não devem residir na UI.
- EF Core é a abstração padrão de persistência; não criar repository genérico/Unit of Work customizado sem necessidade comprovada.
- Preferir serviços de domínio simples e funções explícitas a padrões arquiteturais adicionais.
- Não usar CQRS/MediatR como padrão do projeto.
- Não criar API separada para a própria interface Razor Pages no MVP.
- Não usar SPA framework no MVP.
- Alterações de banco devem usar migrations.
- Não criar migrations vazias antes do primeiro modelo persistente real.
- Banco local deve ser reconstruível a partir das migrations e do seed de desenvolvimento quando este passar a existir.

## Persistência calculada x armazenada

Devem ser armazenados fatos e decisões do usuário, como:

- registros de preço de insumo;
- composição da ficha técnica;
- configurações;
- preço de venda praticado e seu histórico.

Devem ser calculados sob demanda sempre que possível:

- preço atual do insumo;
- custo atual do lote;
- custo unitário atual;
- margem atual;
- preço teórico;
- preço sugerido;
- situação frente à margem-alvo.

## Segurança e operação do MVP

O MVP é local e de usuário único. Autenticação não será adicionada sem mudança de escopo. O arquivo SQLite deve ficar fora de caminhos versionados no Git.

## Observabilidade

Utilizar logging padrão do ASP.NET Core. Logs devem ser úteis para diagnóstico e não devem substituir validações ou tratamento de erro para o usuário.

## Integração contínua

Conforme ADR-007, o repositório deve possuir CI simples no GitHub Actions para validar restore, build e testes dos pull requests antes do merge. CI não é mecanismo de deploy no MVP.
