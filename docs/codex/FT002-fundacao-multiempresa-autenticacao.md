# Instrução Codex — FT002 Fundação Multiempresa e Autenticação

Você está implementando a **FT002 — Fundação Multiempresa e Autenticação** do repositório `ajmarzola/Precificador`.

## Fonte normativa

Leia integralmente, nesta ordem:

1. `AGENTS.md`;
2. `docs/development/foundation-multiempresa-auth.md`;
3. `docs/architecture/architecture.md`;
4. `docs/architecture/adr/ADR-008-multiempresa-banco-compartilhado.md`;
5. `docs/architecture/adr/ADR-009-aspnet-core-identity.md`;
6. `docs/product/scope.md`;
7. `docs/development/testing-strategy.md`;
8. `docs/development/definition-of-done.md`;
9. `docs/use-cases/UC001-cadastrar-insumo.md`.

A especificação normativa da entrega é `docs/development/foundation-multiempresa-auth.md`.

## Branch

Use:

```text
feat/ft002-multiempresa-autenticacao
```

Parta da `master` atualizada após o merge da documentação FT002.

## Objetivo

Adicionar a infraestrutura mínima para Empresa, ASP.NET Core Identity, vínculo N:N usuário-empresa, bootstrap do primeiro usuário/empresa, login/logout, Empresa Ativa, isolamento tenant-aware, evolução de `Insumo` para `EmpresaId` obrigatório e migration compatível com banco existente.

Não implemente UC001A ou UC002 nesta entrega.

## Modelo esperado

```text
Empresa
- Id
- Nome
- NomeNormalizado
- Ativo

UsuarioAplicacao : IdentityUser

UsuarioEmpresa
- UsuarioId
- EmpresaId
- Ativo

Insumo
- EmpresaId
- demais campos atuais
```

Não coloque `IdentityUser` no Core.

## Identity

Use ASP.NET Core Identity + EF Core SQLite no mesmo banco.

Preferir um único `PrecificadorDbContext` herdando de `IdentityDbContext<UsuarioAplicacao>`.

Configurar e-mail único, login por e-mail, política de senha definida na FT002, cookie HttpOnly e proteção das rotas de negócio.

Não usar JWT, OAuth externo, auto-registro público ou implementação própria de senha.

## Empresa Ativa

Crie contrato request-scoped simples, por exemplo `IEmpresaContext`, sem acoplar o Core a `HttpContext`.

A implementação Web pode usar Session.

Após login: limpar contexto anterior; zero vínculos bloqueia área de negócio; um vínculo seleciona automaticamente; múltiplos vínculos direcionam para `/Empresas/Selecionar`.

Seleção sempre revalida usuário, vínculo ativo e empresa ativa.

## Isolamento EF

`Insumo` passa a ter `EmpresaId` obrigatório.

Configure Global Query Filter para consultas tenant-owned e guard central em `SaveChanges/SaveChangesAsync` para impedir escrita cross-tenant.

Sem Empresa Ativa, consulta tenant-owned não retorna dados.

`EmpresaId` nunca vem do formulário; a criação usa o contexto resolvido pelo servidor.

`IgnoreQueryFilters` somente quando a especificação autoriza e com justificativa evidente.

## Migration

Crie `AddMultiempresaIdentity` ou nome equivalente, sem editar `CreateInsumos`.

A migration deve criar Empresa/Identity/UsuarioEmpresa, inserir empresa técnica neutra, adicionar `EmpresaId` obrigatório a Insumos preservando registros existentes, criar FK e substituir a unicidade de `NomeNormalizado` por `(EmpresaId, NomeNormalizado)`.

Valide banco vazio e upgrade de banco UC001 contendo Insumo.

## Bootstrap

Crie `/Setup`, disponível somente quando não houver usuários.

Campos: NomeEmpresa, Email, Senha e ConfirmacaoSenha.

Use `UserManager`; não manipule hash manualmente.

Renomeie a empresa técnica para o nome informado e crie `UsuarioEmpresa`.

Não criar credenciais default/hardcoded.

## Login/logout/seleção

Crie UI Razor Pages mínima para `/Conta/Login` e `/Empresas/Selecionar`.

Use `SignInManager`.

Logout somente POST + antiforgery e limpa Empresa Ativa.

No layout autenticado, mostrar a Empresa Ativa de forma simples.

## Testes obrigatórios

Preserve/adapte os testes existentes e cubra no mínimo:

- Empresa válida e normalização;
- migration em banco vazio;
- upgrade do UC001;
- bootstrap único;
- login válido/inválido;
- rota de negócio exige autenticação;
- uma empresa auto selecionada;
- múltiplas empresas exigem seleção;
- seleção sem vínculo é negada;
- leitura cross-tenant isolada;
- escrita cross-tenant rejeitada;
- mesmo NomeNormalizado permitido entre empresas e rejeitado dentro da mesma;
- troca de empresa altera visibilidade;
- logout limpa contexto.

Use SQLite temporário/in-memory com conexão mantida; nunca `precificador.db` real.

## Restrições

Não implementar Marca/Observação, UC001A, listagem UC002, CRUD administrativo completo, roles, recuperação de senha, confirmação de e-mail, 2FA, Produto, Ficha Técnica, equipamento, preço, API REST, repository genérico, CQRS/MediatR, auto-migration ou troca de SQLite.

Também não generalize Categoria/Unidade dentro desta FT.

## Documentação ao concluir

Marcar FT002 como Implementado. Não marcar UC001A/UC002 como implementados. Se houver conflito real, registrar desvio em vez de mudar requisito silenciosamente.

## Validação

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Valide também migration em banco vazio e upgrade do UC001.

Commit sugerido: `feat: adiciona fundacao multiempresa e autenticacao`.

Não faça merge em `master`.
