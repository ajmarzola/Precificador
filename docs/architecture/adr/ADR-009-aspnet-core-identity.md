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

- autenticação externa;
- JWT/API tokens;
- confirmação de e-mail;
- recuperação de senha;
- 2FA;
- roles/permissões granulares.
