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

Adicionar a infraestrutura mínima para:

- Empresa;
- ASP.NET Core Identity;
- vínculo N:N usuário-empresa;
- bootstrap do primeiro usuário/empresa;
- login/logout;
- Empresa Ativa;
- isolamento tenant-aware;
- evolução de `Insumo` para `EmpresaId` obrigatório;
- migration compatível com banco existente.

Não implemente UC001A, UC001B ou UC002.

## Modelo esperado

Conceitualmente:

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

Use ASP.NET Core Identity + EF Core SQLite.

Preferir um único `PrecificadorDbContext` herdando de `IdentityDbContext<UsuarioAplicacao>` para manter domínio e Identity na mesma base/migration history.

Configurar:

- e-mail único;
- login por e-mail;
- senha mínima 8;
- maiúscula, minúscula e dígito obrigatórios;
- não exigir caractere não alfanumérico;
- cookie HttpOnly;
- rotas de negócio autenticadas por padrão.

Não usar JWT, OAuth externo, API endpoints de Identity ou auto-registro público.

## Empresa Ativa

Crie contrato request-scoped simples, por exemplo `IEmpresaContext`, sem acoplar o Core a `HttpContext`.

A implementação Web pode ler/escrever o ID em Session.

Fluxo após Login:

- limpar empresa ativa anterior;
- zero vínculos elegíveis: não liberar área de negócio;
- um vínculo: ativar automaticamente e ir para Home;
- mais de um: redirecionar para `/Empresas/Selecionar`.

Seleção deve revalidar `UsuarioId + EmpresaId`, vínculo ativo e empresa ativa antes de armazenar a sessão.

## Isolamento EF

`Insumo` deve implementar um contrato tenant-owned e possuir `EmpresaId` obrigatório.

Configure Global Query Filter no EF para que consultas normais retornem somente a Empresa Ativa.

Sem Empresa Ativa, não retornar entidade tenant-owned.

Adicione um guard central em SaveChanges/SaveChangesAsync que rejeite Added/Modified/Deleted tenant-owned com empresa divergente da Empresa Ativa.

Não confie em hidden field ou `EmpresaId` vindo da request.

Atualize a criação de Insumo para receber o EmpresaId resolvido pelo servidor.

Use `IgnoreQueryFilters` somente onde a especificação autoriza explicitamente e com justificativa evidente.

## Migration

Crie nova migration, sem editar `CreateInsumos`.

Nome sugerido:

```text
AddMultiempresaIdentity
```

Ela deve:

1. criar Empresas;
2. inserir empresa técnica inicial neutra;
3. adicionar `EmpresaId` obrigatório aos Insumos apontando registros existentes para a empresa técnica;
4. criar FK;
5. substituir índice `NomeNormalizado` por `(EmpresaId, NomeNormalizado)`;
6. criar tabelas Identity;
7. criar UsuarioEmpresas com chave composta e FKs.

Valide dois caminhos:

- banco SQLite vazio;
- banco migrado até UC001 contendo pelo menos um Insumo.

No upgrade, o Insumo deve sobreviver e ficar associado à empresa técnica.

## Bootstrap

Crie `/Setup`.

Somente disponível enquanto não existir nenhum usuário.

Input model próprio:

- NomeEmpresa;
- Email;
- Senha;
- ConfirmacaoSenha.

Use UserManager para criação do usuário. Não manipule hash de senha manualmente.

O setup deve renomear a empresa técnica para o nome informado e criar UsuarioEmpresa.

Não criar senha default, usuário hardcoded ou segredo em appsettings.

Depois do primeiro usuário, o fluxo deve ficar indisponível.

## Login/logout

Crie interface Razor Pages mínima, preferencialmente:

```text
/Conta/Login
/Empresas/Selecionar
```

Use SignInManager.

Logout somente POST + antiforgery; limpar session/Empresa Ativa.

Home e páginas de negócio devem exigir autenticação.

## UI

No layout autenticado, mostrar a Empresa Ativa de forma simples.

Não criar painel administrativo de usuários/empresas.

## Testes obrigatórios

Preserve os testes existentes e adapte-os ao novo tenant context.

Cobrir no mínimo:

- Empresa válida/normalização;
- migration vazia;
- migration de upgrade UC001;
- Identity tables;
- bootstrap único;
- login válido/inválido;
- rota de negócio anônima redireciona para login;
- uma empresa auto selecionada;
- duas empresas exigem seleção;
- seleção não autorizada bloqueada;
- leitura cross-tenant isolada;
- escrita cross-tenant rejeitada;
- mesmo NomeNormalizado permitido em empresas diferentes;
- duplicidade na mesma empresa rejeitada;
- troca de empresa muda visibilidade;
- logout limpa contexto.

Testes devem usar SQLite temporário/in-memory com conexão mantida. Nunca tocar `precificador.db` real.

## Restrições

Não implementar:

- Marca/Observação de Insumo;
- UC001A;
- generalização Categoria/Unidade do UC001B;
- listagem/detalhes UC002;
- CRUD administrativo completo de Empresa/Usuário;
- roles;
- recuperação de senha;
- confirmação de e-mail;
- 2FA;
- Produto;
- Ficha Técnica;
- equipamento;
- preço;
- API REST;
- repository genérico;
- CQRS/MediatR;
- auto-migration no startup;
- troca de SQLite.

## Documentação ao concluir

- alterar FT002 para `Status: Implementado`;
- não marcar UC001A/UC001B/UC002 como implementados;
- registrar desvios reais em vez de ajustar requisitos silenciosamente.

## Validação

Execute:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Valide também migration em banco vazio e upgrade do UC001.

Revise o diff antes de finalizar.

Commit sugerido:

```text
feat: adiciona fundacao multiempresa e autenticacao
```

Não faça merge em `master`.
