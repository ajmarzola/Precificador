# ADR-009 — ASP.NET Core Identity para autenticação

- **Status:** Aceito

## Contexto

Com múltiplas empresas e usuários, o Precificador precisa autenticar pessoas e proteger o acesso às empresas autorizadas.

## Decisão

Usar **ASP.NET Core Identity** com EF Core e o mesmo banco SQLite da aplicação.

`UsuarioAplicacao` deriva de `IdentityUser` e permanece na camada de infraestrutura/autenticação. As entidades de domínio não dependem de Identity.

Um vínculo próprio `UsuarioEmpresa` representa a relação N:N entre usuário e empresa.

## Consequências

- senha, hash, cookie e lockout usam componentes mantidos pelo ASP.NET Core;
- evita solução de segurança proprietária;
- Identity adiciona suas tabelas ao mesmo DbContext/migration history;
- seleção de Empresa Ativa continua sendo responsabilidade da aplicação;
- autorização granular por roles não é introduzida sem requisito específico.

## Fora da decisão inicial

Autenticação externa, JWT/API tokens, confirmação de e-mail, recuperação de senha, 2FA e roles granulares.

## Evolução pela FT003

A [ADR-011](ADR-011-dois-planos-autorizacao.md) introduz o requisito específico que não existia na decisão inicial:

- role global `SystemAdmin` para administração do sistema;
- perfil contextual `Operacional/Administrador` no vínculo `UsuarioEmpresa`.

A autenticação continua integralmente no ASP.NET Core Identity. A granularidade por Empresa não é modelada como IdentityRole global.
