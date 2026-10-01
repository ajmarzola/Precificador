# Instrução Codex — UC039 Administrar Empresas e solicitações

Você está implementando a UC039 — Administrar Empresas e solicitações de acesso no repositório ajmarzola/Precificador.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/use-cases/UC039-administrar-empresas-solicitacoes.md;
3. docs/development/foundation-administration-authorization.md;
4. docs/use-cases/UC038-solicitar-acesso-precificador.md;
5. docs/use-cases/UC040-ativar-conta-recuperar-acesso.md;
6. docs/business/business-rules.md, especialmente RN065–RN070;
7. docs/architecture/adr/ADR-011-dois-planos-autorizacao.md;
8. docs/architecture/adr/ADR-012-email-tokens-data-protection.md;
9. docs/architecture/adr/ADR-013-ciclo-vida-empresa-seed-tecnico.md;
10. docs/development/testing-strategy.md;
11. docs/development/definition-of-done.md;
12. implementação real de Empresa, SolicitacaoAcessoEmpresa, UsuarioEmpresa, Identity, ServicoConta, /Admin e migrations.

A especificação normativa é:

~~~text
docs/use-cases/UC039-administrar-empresas-solicitacoes.md
~~~

## Branch

Use:

~~~text
feat/uc039-administrar-empresas-solicitacoes
~~~

Parta da master depois do merge da documentação da UC039.

Não implemente na branch documental.

## Objetivo

Entregar administração global para SystemAdmin:

- dashboard /Admin;
- solicitações;
- aprovação;
- recusa;
- Empresas reais;
- primeiro Administrador;
- correção de legado sem Admin;
- substituição de Admin;
- reenvio de ativação;
- suspensão;
- reativação;
- encerramento lógico.

Não implementar UC031.

## Empresa técnica

Adicionar EhTecnica em Empresa.

Empresa.Criar => false.

Empresa.CriarTecnica => true.

Migration deve marcar como técnica somente:

~~~text
Id=1 AND NomeNormalizado='EMPRESA INICIAL'
~~~

Não use UpdateData incondicional que transforme Empresa 1 renomeada em técnica.

Se a migration gerada pelo EF produzir update incondicional por causa do HasData, ajuste a nova migration para fazer backfill condicional.

Não edite migration histórica.

Empresa técnica não entra em /Admin/Empresas e não recebe ações UC039.

## Ciclo de vida

Preservar Ativo como gate operacional.

Adicionar EncerradaEmUtc.

Estados derivados:

~~~text
Ativa     -> Ativo && EncerradaEmUtc == null
Suspensa  -> !Ativo && EncerradaEmUtc == null
Encerrada -> !Ativo && EncerradaEmUtc != null
~~~

Adicionar constraint impedindo Ativo + EncerradaEmUtc.

Métodos de domínio devem impedir lifecycle em Empresa técnica.

Encerramento usa TimeProvider.GetUtcNow.

Não adicionar hard delete.

## Solicitação

Evoluir com:

~~~text
EmpresaId?
DataDecisaoUtc?
DecididaPorUsuarioId?
MotivoRecusa?
~~~

Adicionar métodos de domínio Aprovar/Recusar.

Somente Pendente recebe decisão.

Adicionar check constraint para invariantes Pendente/Aprovada/Recusada.

EmpresaId FK Restrict.

DecididaPorUsuarioId deve registrar o usuário autenticado SystemAdmin, nunca vir do request.

## /Admin

Dashboard global:

- Pendentes;
- Empresas Ativas;
- Suspensas;
- Encerradas;
- links Solicitações e Empresas.

Não mostrar dados tenant.

## Solicitações

Criar /Admin/Solicitacoes e detalhe.

Default Pendente.

Filtros conforme especificação.

Detalhe mostra Observação e decisão.

Ações apenas em Pendente.

## Aprovação

InputModel contém somente o necessário, principalmente Nome final.

Não bindar entidade.

Nome final default da solicitação e usa regra Empresa.

Colisão de NomeNormalizado:

- bloquear;
- não criar vínculo em Empresa existente;
- manter solicitação Pendente.

## Identidade na aprovação

Resolver pelo e-mail da solicitação.

### Inexistente

UserManager.CreateAsync sem senha.

### Existente

Reutilizar.

Não alterar password/hash/stamp por novo vínculo.

### SystemAdmin

Rejeitar.

Não criar UsuarioEmpresa para identidade global SystemAdmin.

## Aprovação transacional

Use execution strategy + transaction.

A unidade deve conter:

- UserManager create quando necessário;
- Empresa;
- ConfiguracaoPrecificacaoEmpresa.CriarPadrao;
- UsuarioEmpresa Ativo/Administrador;
- Solicitacao Aprovada.

E-mail somente depois do commit.

Se falhar antes do commit, tudo rollback, inclusive identidade criada nessa tentativa.

Não tente transacionar SMTP.

## Concorrência

Dois approvals da mesma solicitação devem produzir só um tenant.

Implemente proteção de banco real.

Pode usar Serializable/rowversion/locking apropriado.

Teste concorrência com SQL Server real.

Não confiar somente em UI.

## Pós-commit

### Usuário sem senha

ServicoConta.EnviarAtivacaoAsync.

### Usuário com senha

Adicionar operação pequena para enviar aviso de acesso liberado, sem reset/token.

Se e-mail falhar:

- aprovação continua;
- mostrar aviso Admin.

## Recusa

Somente Pendente.

Motivo interno opcional <=500.

Persistir decisão + TimeProvider + SystemAdmin.

Não criar tenant.

Depois do commit, envio simples best-effort sem motivo interno.

Falha não reabre.

## Configuração padrão

Nova Empresa sempre recebe ConfiguracaoPrecificacaoEmpresa.CriarPadrao no mesmo transaction scope.

## Primeiro Admin

UsuarioEmpresa:

~~~text
Ativo=true
Perfil=Administrador
~~~

Não criar Operacional extra.

## /Admin/Empresas

Listar somente EhTecnica=false.

Exibir situação + número de Admins + Sem administrador.

Detalhe somente administrativo.

Não ler Produto/Insumo/Ficha/preços.

## Legado sem Admin

Não auto-promover.

Ação Definir administrador:

- pode promover Operacional;
- reativar vínculo inativo;
- criar vínculo;
- criar identidade sem senha;
- reutilizar identidade.

SystemAdmin como alvo é inválido.

Empresa Encerrada não aceita.

## Substituir Admin

Input: admin atual + novo e-mail.

Transação:

1. validar admin atual;
2. resolver novo usuário;
3. rejeitar SystemAdmin;
4. promover/criar/reativar novo Admin;
5. demover somente admin selecionado para Operacional;
6. commit.

Outros Admins permanecem.

Vínculos em outras Empresas permanecem.

Empresa Suspensa pode trocar Admin.

Empresa Encerrada não.

## Reenviar ativação

Somente vínculo Ativo/Administrador sem senha.

Usar ServicoConta.

Não criar usuário/link novo.

Tratar os quatro resultados existentes.

## Suspender

Real não Encerrada.

Ativo=false.

Não tocar vínculos/dados.

## Reativar

Real Suspensa, não Encerrada.

Exigir Admin ativo.

Ativo=true.

## Encerrar

Confirmação explícita via POST.

Ativo=false.

EncerradaEmUtc=TimeProvider.GetUtcNow.

Sem hard delete.

Sem reativação.

## Segurança

Todas as rotas /Admin/** usam policy SystemAdmin.

Todos POSTs antiforgery.

Não aceitar mass assignment de:

- EmpresaId;
- Situacao;
- SystemAdmin Id;
- Perfil;
- EhTecnica;
- Ativo;
- EncerradaEmUtc.

Não usar Html.Raw com campos de solicitação.

Não logar tokens, senhas, URL com token, e-mail completo ou motivo interno completo.

## Migration

Nova migration inclui Empresa e Solicitação.

Teste:

- banco vazio;
- upgrade pós-UC040;
- seed intacto técnico;
- Id1 renomeada real;
- Pendente antiga preservada;
- DataProtectionKeys preservadas;
- dados tenant preservados.

Não editar migrations antigas.

## Testes obrigatórios

Atenda integralmente à matriz UC039.

Pontos críticos:

- autorização SystemAdmin;
- seed técnico condicional;
- legado Empresa1 renomeada;
- domain lifecycle;
- request terminal;
- approval novo user;
- approval user com password;
- approval user sem password;
- SystemAdmin como responsável;
- collision tenant existente;
- rollback real;
- concorrência real;
- email pós-commit;
- recusa;
- lista Empresas;
- legado sem Admin;
- Definir Admin;
- Substituir Admin;
- Reenviar ativação;
- suspensão invalida acesso tenant;
- reativação exige Admin;
- encerramento preserva dados e não reabre;
- regressão UC038/UC040/Setup/Login.

## Restrições de escopo

Não implementar:

- UC031;
- CRUD cotidiano de usuários;
- hard delete;
- reabertura;
- merge/transferência;
- edição global de e-mail;
- senha administrativa;
- dados comerciais no Admin;
- edição geral de Nome/Timezone pós-criação;
- auditoria geral;
- 2FA/SSO.

## Documentação ao concluir

- UC039 -> Concluído;
- backlog -> Concluído;
- UC031 permanece Planejado;
- RN065–RN070 coerentes;
- ADR-013 preservada;
- catálogo atualizado se necessário.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
~~~

Valide clean DB e upgrade pós-UC040.

Commit sugerido:

~~~text
feat: adiciona administracao global de empresas
~~~

Não faça merge em master.
