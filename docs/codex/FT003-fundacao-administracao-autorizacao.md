# Instrução Codex — FT003 Fundação de administração e autorização

Você está implementando a **FT003 — Fundação de administração e autorização** do repositório `ajmarzola/Precificador`.

## Fonte normativa

Leia integralmente, nesta ordem:

1. `AGENTS.md`;
2. `docs/development/foundation-administration-authorization.md`;
3. `docs/development/foundation-multiempresa-auth.md`;
4. `docs/architecture/architecture.md`;
5. `docs/architecture/adr/ADR-009-aspnet-core-identity.md`;
6. `docs/architecture/adr/ADR-011-dois-planos-autorizacao.md`;
7. `docs/development/reviews/2026-09-16-review-mvp-primeiro-uso.md`;
8. `docs/product/scope.md`;
9. `docs/development/testing-strategy.md`;
10. `docs/development/definition-of-done.md`;
11. implementação real atual de Setup, Login, seleção de Empresa, layout, EmpresaAtivaRequirement, `UsuarioEmpresa`, DbContext/migrations e testes Web na `master`.

A especificação normativa da entrega é:

~~~text
docs/development/foundation-administration-authorization.md
~~~

Se houver conflito entre documentação histórica da FT002 e a FT003, a FT003 prevalece apenas nos pontos explicitamente evoluídos por ela.

## Branch

Use:

~~~text
feat/ft003-administracao-autorizacao
~~~

Parta da `master` atualizada após o merge da documentação FT003.

Não implemente na branch documental.

## Objetivo

Introduzir a infraestrutura de autorização para:

- `SystemAdmin` global via ASP.NET Core Identity Role;
- perfil `Operacional/Administrador` no vínculo `UsuarioEmpresa`;
- policies `SystemAdmin` e `AdministradorEmpresa`;
- novo `/Setup` que cria somente o primeiro Administrador do Sistema;
- bootstrap protegido por chave externa;
- `/Admin` mínimo protegido;
- login/navegação distintos entre SystemAdmin e usuário empresarial.

Não implemente as UCs 038, 040, 039 ou 031 nesta entrega.

## Modelo esperado

~~~text
UsuarioAplicacao : IdentityUser
    |
    +-- role global SystemAdmin
    |
    +-- UsuarioEmpresa
           UsuarioId
           EmpresaId
           Ativo
           Perfil = Operacional | Administrador
~~~

Use valores estáveis:

~~~text
Operacional = 1
Administrador = 2
~~~

Não use IdentityRole para Administrador de Empresa.

## Migration

Adicionar `Perfil` em `UsuariosEmpresas`.

A migration deve:

1. adicionar coluna temporariamente nullable ou usar estratégia equivalente segura;
2. preencher TODOS os vínculos existentes com `Operacional`;
3. tornar a coluna obrigatória;
4. terminar sem default de banco que conceda perfil Administrador;
5. preservar todos os usuários, vínculos e dados empresariais;
6. não promover usuário existente para `SystemAdmin`;
7. não editar migrations históricas.

Não remover nem reaproveitar automaticamente a Empresa técnica `Empresa inicial` nesta FT.

O novo Setup não pode renomeá-la nem vinculá-la ao SystemAdmin.

## Autorização

Centralize nomes de role/policies para evitar strings espalhadas.

### SystemAdmin

Policy global baseada em role.

Deve funcionar sem Empresa Ativa.

Proteja `/Admin` por essa policy.

Não conceda bypass de Global Query Filters nem acesso automático às áreas tenant-owned.

### AdministradorEmpresa

Crie requirement/handler ou composição equivalente que exija:

- usuário autenticado;
- Empresa Ativa válida;
- Empresa ativa;
- `UsuarioEmpresa` ativo;
- `Perfil == Administrador`.

A policy precisa respeitar o tenant atual.

Não altere áreas operacionais existentes para exigir Administrador; elas continuam em `EmpresaAtiva`.

## Novo Setup

O critério de bootstrap é “não existe usuário na role SystemAdmin”, e não “não existe usuário”.

Campos:

- Chave de configuração;
- E-mail;
- Senha;
- Confirmação.

Remover Nome da Empresa.

Configuração esperada:

~~~text
Bootstrap:SystemAdminKey
~~~

Environment variable equivalente:

~~~text
Bootstrap__SystemAdminKey
~~~

Não versionar valor real.

Não persistir ou logar a chave.

Preferir hash + comparação em tempo constante ou mecanismo equivalente.

### GET

- SystemAdmin já existe -> 404;
- nenhum SystemAdmin + chave ausente -> 503;
- nenhum SystemAdmin + chave configurada -> 200.

### POST

- antiforgery obrigatório;
- validar chave antes de revelar erro de e-mail existente;
- revalidar ausência de SystemAdmin dentro da unidade crítica;
- usar `RoleManager` para garantir role;
- usar `UserManager` para criar usuário;
- adicionar role `SystemAdmin`;
- não criar `UsuarioEmpresa`;
- não criar/renomear Empresa;
- não definir Empresa Ativa;
- redirecionar para Login.

Reduza a janela de concorrência para evitar dois bootstraps iniciais. Use transação/isolamento apropriado ou solução equivalente suportada pelo SQL Server/Identity.

Não invente lock distribuído ou infraestrutura externa.

## Compatibilidade com instalação existente

Cenário suportado:

- existem usuários empresariais;
- existem vínculos/Empresas/dados;
- ainda não existe SystemAdmin.

Nesse cenário:

- `/Setup` continua disponível;
- usuário empresarial não é promovido;
- novo SystemAdmin deve usar nova conta/e-mail;
- após bootstrap, usuários empresariais antigos continuam fazendo login;
- seus vínculos foram migrados para `Operacional`;
- a futura UC039 decidirá/designará Administrador de Empresa.

## Login

No GET autenticado:

- SystemAdmin -> `/Admin`;
- empresarial + Empresa Ativa -> `/Dashboard`;
- empresarial sem Empresa Ativa -> `/Empresas/Selecionar`.

No GET anônimo, se não existe SystemAdmin, redirecionar para `/Setup`.

No POST:

1. validar credenciais;
2. limpar `EmpresaContext`;
3. se SystemAdmin -> `/Admin`;
4. não consultar/selecionar Empresa para SystemAdmin;
5. para demais usuários preservar exatamente a lógica FT002 de zero/um/vários vínculos.

Para SystemAdmin, ignore ReturnUrl tenant-owned e use `/Admin`.

## /Empresas/Selecionar

SystemAdmin não deve receber a experiência de seleção de Empresa.

Se acessar diretamente, redirecione para `/Admin` ou negue de forma consistente.

Não invente vínculo só para satisfazer essa página.

## Navegação

### SystemAdmin

Mostrar:

- marca/Início apontando para `/Admin`;
- Administração;
- Sair.

Não mostrar:

- Dashboard;
- Insumos;
- Produtos;
- Configurações;
- “Empresa ativa”.

### Usuário empresarial

Preservar navegação atual.

### Anônimo

Preservar navegação atual.

Não implementar formulário público da UC038.

## Área /Admin

Criar somente shell mínimo, sem CRUD.

Conteúdo suficiente para provar:

- policy global;
- navegação;
- destino pós-login.

Não listar Empresas, solicitações ou usuários.

## Testes obrigatórios

Implemente a matriz da especificação e preserve toda a suíte existente.

Cobrir especialmente:

### Migration
- banco vazio;
- upgrade com usuário/vínculo/dados;
- perfil legado -> Operacional;
- nenhum auto-SystemAdmin.

### Setup
- chave configurada;
- chave ausente;
- chave inválida;
- usuário empresarial existente não bloqueia bootstrap;
- e-mail já existente não é elevado;
- cria role + novo usuário SystemAdmin;
- não cria vínculo;
- não renomeia Empresa;
- repetição bloqueada;
- antiforgery;
- concorrência de bootstrap.

### Policies
- SystemAdmin global;
- AdministradorEmpresa no tenant correto;
- Operacional negado;
- vínculo inativo;
- Empresa inativa;
- cross-tenant.

### Login/Navegação
- SystemAdmin -> /Admin;
- SystemAdmin sem EmpresaContext;
- SystemAdmin não entra em seleção;
- empresarial preserva FT002;
- layout correto para cada tipo;
- logout preservado.

Ajuste os helpers de teste para informar `PerfilUsuarioEmpresa` explicitamente. Se houver default de helper, use `Operacional` apenas como convenção de teste, nunca como default de banco.

## Restrições

Não implementar:

- UC038;
- e-mail;
- token de convite;
- recuperação de senha;
- UC040;
- aprovação de Empresa;
- UC039;
- convite/desvínculo de usuários;
- UC031;
- gestão de múltiplos SystemAdmins;
- impersonation;
- 2FA;
- SSO;
- hard delete;
- refatoração ampla não necessária.

Não criar framework genérico de ACL/permissões.

Duas policies e o perfil do vínculo são suficientes para esta FT.

## Documentação ao concluir

Ao finalizar:

- marcar FT003 como Concluído no backlog;
- atualizar o documento FT003 para Estado Concluído;
- manter UC038, UC040, UC039 e UC031 como Planejado;
- atualizar documentação afetada somente se o comportamento implementado divergir legitimamente da especificação;
- não descrever e-mail/token/administração de Empresas como já implementados.

## Validação

Executar no mínimo:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
~~~

Validar também migration em banco vazio e upgrade representativo de instalação FT002/MEL020.

Commit sugerido:

~~~text
feat: adiciona fundacao de administracao e autorizacao
~~~

Não faça merge em `master`.
