# Instrução Codex — UC031 Administrar usuários e vínculos com Empresas

Você está implementando a UC031 — Administrar usuários e vínculos com Empresas no repositório ajmarzola/Precificador.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/use-cases/UC031-administrar-usuarios-vinculos.md;
3. docs/development/foundation-administration-authorization.md;
4. docs/use-cases/UC039-administrar-empresas-solicitacoes.md;
5. docs/use-cases/UC040-ativar-conta-recuperar-acesso.md;
6. docs/business/business-rules.md, especialmente RN071–RN075;
7. docs/architecture/adr/ADR-011-dois-planos-autorizacao.md;
8. docs/architecture/adr/ADR-012-email-tokens-data-protection.md;
9. docs/architecture/adr/ADR-014-administracao-tenant-vinculos.md;
10. docs/development/testing-strategy.md;
11. docs/development/definition-of-done.md;
12. implementação atual de UsuarioEmpresa, AdministradorEmpresaHandler, EmpresaContext, Login, ServicoConta e ServicoAdministracao.

A especificação normativa é:

~~~text
docs/use-cases/UC031-administrar-usuarios-vinculos.md
~~~

## Branch

Use:

~~~text
feat/uc031-administrar-usuarios-vinculos
~~~

Parta da master depois do merge da documentação UC031.

Não implemente na branch documental.

## Objetivo

Criar a administração tenant de usuários:

- listar vínculos da Empresa Ativa;
- adicionar usuário por e-mail;
- reutilizar identidade global;
- criar identidade nova sem senha;
- atribuir Administrador ou Operacional;
- alterar perfil;
- desvincular logicamente;
- reativar;
- reenviar ativação;
- proteger último Administrador.

Não criar tabela de convite nem alterar credencial global.

## Autorização

Use policy AdministradorEmpresa para toda a área /Usuarios.

EmpresaId vem exclusivamente de EmpresaContext.

Nunca aceite EmpresaId do request.

Todas as consultas/escritas de vínculo devem filtrar pela Empresa Ativa.

Operacional e cross-tenant devem ser negados.

## Navegação

Exibir Usuários somente quando o usuário atual estiver autorizado por AdministradorEmpresa.

Não exibir para Operacional nem SystemAdmin.

A UI não substitui a policy.

## Serviço de domínio/aplicação

Criar serviço coeso, por exemplo:

~~~text
ServicoUsuariosEmpresa
~~~

Responsabilidades:

- resolver/criar identidade;
- criar/reativar vínculo;
- alterar perfil;
- desvincular;
- reativar;
- preservar último Admin;
- executar transações/locks;
- disparar comunicação pós-commit.

Não colocar toda a lógica nos PageModels.

## Identidade

Resolver por e-mail via UserManager.

Nova identidade:

~~~text
UserManager.CreateAsync(usuario)
~~~

sem senha.

Existente:

- reutilizar;
- preservar PasswordHash;
- preservar SecurityStamp;
- preservar EmailConfirmed;
- preservar roles;
- preservar outros vínculos.

SystemAdmin como alvo deve ser rejeitado com mensagem neutra.

## Adicionar

Input:

~~~text
Email
Perfil
~~~

Perfil deve ser enum definido e somente Operacional/Administrador.

Sem vínculo: criar Ativo.

Vínculo inativo: reativar o mesmo registro e aplicar perfil solicitado.

Vínculo ativo: não duplicar nem trocar perfil implicitamente.

## Comunicação

Após commit:

- sem senha -> ServicoConta.EnviarAtivacaoAsync;
- com senha -> ServicoConta.EnviarAcessoLiberadoAsync.

Falha de SMTP não faz rollback.

Não segurar transação/lock durante e-mail.

## Perfil e último Admin

Operacional -> Admin permitido.

Admin -> Operacional só se outro Admin ativo permanecer.

Último Admin nunca pode ser demovido ou desvinculado.

Proteja isso dentro de transação serializada.

## Desvínculo

Não delete UsuarioEmpresa.

Faça:

~~~text
Ativo = false
~~~

Preserve Perfil.

Não altere account Identity nem outros vínculos.

## Reativação

Vínculo inativo:

~~~text
Ativo = true
~~~

Preserve Perfil.

Depois do commit, comunicar conforme HasPassword.

## Auto-demissão e auto-desvínculo

Se ator demover a si mesmo e houver outro Admin:

- commit;
- redirect Dashboard.

Se ator desvincular a si mesmo e houver outro Admin:

- commit;
- EmpresaContext.Limpar();
- redirect /Empresas/Selecionar.

Último Admin bloqueia ambos.

## Concorrência

Use execution strategy + transação SQL + Serializable + sp_getapplock por Empresa.

Resource:

~~~text
Precificador.UC031.Empresa.<EmpresaId>
~~~

LockOwner Transaction.

Teste em SQL Server real:

- A desvincula B enquanto B desvincula A;
- A demove B enquanto B desvincula A.

Resultado: pelo menos um Admin ativo.

## Concorrência Identity

Duas Empresas podem convidar o mesmo e-mail simultaneamente.

A unicidade de NormalizedEmail continua fonte de verdade.

Trate DuplicateEmail/DuplicateUserName e violações esperadas do índice único de forma restrita:

- rollback/limpeza;
- reler usuário;
- repetir;
- não mascarar outros erros.

Resultado:

~~~text
uma UsuarioAplicacao
um vínculo em cada Empresa
~~~

## Rollback

Se identidade nova for criada dentro da tentativa e uma escrita posterior falhar antes do commit, ela não pode sobrar órfã.

Use a mesma transação/DbContext Identity, como UC039.

## Reenviar ativação

Somente vínculo local Ativo cujo usuário não possui senha.

Use UC040 e trate Enviado/NaoNecessario/Indisponivel/Falhou.

Não expor token.

## Páginas

Estrutura sugerida:

~~~text
/Usuarios
/Usuarios/Detalhes
~~~

Lista com filtro Ativos/Inativos/Todos.

Detalhe recebe usuarioId, mas só existe se UsuarioEmpresa pertence à EmpresaContext atual.

Cross-tenant => 404/resultado controlado sem mutação.

## Segurança

POSTs antiforgery.

Use InputModels.

Não bindar entidades EF.

Não permitir alteração de:

- EmpresaId;
- PasswordHash;
- SecurityStamp;
- EmailConfirmed;
- roles globais;
- e-mail da identidade.

Não usar Html.Raw com e-mails/mensagens.

## Schema

Não crie migration por padrão.

O schema atual já suporta o caso.

Se concluir que migration é indispensável, documente claramente na PR por que o contrato existente é insuficiente antes de adicioná-la.

Não criar tabela de convite.

## Testes obrigatórios

Atenda integralmente à matriz UC031.

Pontos críticos:

- autorização e navegação;
- lista sem vazamento cross-tenant;
- novo usuário;
- existente com/sem senha;
- vínculo inativo;
- vínculo já ativo;
- SystemAdmin alvo;
- alteração de perfil;
- último Admin;
- desvínculo lógico;
- reativação;
- reenvio;
- auto-demissão;
- auto-desvínculo e EmpresaContext;
- concorrência do último Admin;
- concorrência Identity cross-tenant;
- rollback de identidade;
- mass assignment;
- regressão UC039/UC040/Login/seleção.

## Documentação ao concluir

- UC031 -> Concluído;
- backlog -> Concluído;
- RN071–RN075 coerentes;
- ADR-014 preservada;
- atualizar catálogo;
- não antecipar UC033.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
~~~

Commit sugerido:

~~~text
feat: adiciona administracao tenant de usuarios
~~~

Não faça merge em master.
