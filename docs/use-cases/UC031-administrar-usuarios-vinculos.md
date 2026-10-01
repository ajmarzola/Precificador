# UC031 — Administrar usuários e vínculos com Empresas

- **Área funcional:** Administração tenant / multiempresa
- **Dependências:** FT003, UC039, UC040
- **Estado:** Concluído
- **Alteração de schema:** não prevista
- **Autorização:** AdministradorEmpresa
- **Superfícies:** /Usuarios e /Usuarios/Detalhes
- **Regras principais:** RN071 a RN075

## Objetivo

Implementação: `ServicoUsuariosEmpresa` e Razor Pages `/Usuarios` e `/Usuarios/Detalhes`, protegidas pela policy `AdministradorEmpresa`. O serviço aplica o tenant de `EmpresaContext` em todas as consultas de vínculo e revalida a autoridade do ator dentro da unidade de escrita. A navegação consulta a mesma policy; SystemAdmin permanece na área global.

As escritas usam execution strategy, transação Serializable e `sp_getapplock` com recurso `Precificador.UC031.Empresa.<EmpresaId>` e proprietário Transaction. Conflitos Identity de e-mail/username repetem a resolução em até três tentativas, após rollback e limpeza do tracking; outros erros de persistência não são tratados como corrida Identity. A regra pura de proteção do último Administrador fica em `ProtecaoAdministradorEmpresa`, no Core, e sua leitura é feita dentro da transação.

Não há mudança de schema. Identity e vínculo são persistidos pelo mesmo DbContext/transação. Comunicação usa `ServicoConta` após a liberação da transação/lock. Operação e limites estão descritos em [Administração de usuários da Empresa](../development/administracao-usuarios-empresa.md).

Validação local: tool restore e restore da solution concluídos, build Release sem warnings, 288 testes unitários e 662 testes de integração SQL Server/Web aprovados, sem ignorados. A cobertura UC031 contém 50 cenários Web e quatro casos unitários da invariável; a suíte completa inclui regressões UC039/UC040/Login/seleção. CI do head da PR continua sendo gate obrigatório antes do merge humano.

Permitir que o Administrador da Empresa gerencie usuários vinculados somente à Empresa Ativa, preservando a identidade global do usuário e o isolamento multiempresa.

A UC031 entrega:

- consultar vínculos da Empresa Ativa;
- adicionar/convidar usuário por e-mail;
- reutilizar identidade global existente;
- criar identidade nova sem senha;
- atribuir perfil Operacional ou Administrador;
- alterar perfil de vínculo ativo;
- desvincular logicamente;
- reativar vínculo inativo;
- reenviar ativação;
- impedir que a Empresa fique sem Administrador ativo;
- preservar vínculos e credenciais do usuário em outras Empresas.

## Princípio central

~~~text
UsuarioAplicacao = identidade global
UsuarioEmpresa   = acesso e perfil dentro de uma Empresa
~~~

O Administrador da Empresa administra somente UsuarioEmpresa da Empresa Ativa.

Ele não administra senha, e-mail global, roles Identity, conta global ou vínculos de outras Empresas.

## Relação com UC039 e UC040

UC039 cria o primeiro Administrador e mantém as ações globais/emergenciais. Depois que a Empresa está Ativa, a gestão cotidiana dos vínculos pertence à UC031.

UC031 reutiliza os serviços da UC040:

~~~text
ServicoConta.EnviarAtivacaoAsync
ServicoConta.EnviarAcessoLiberadoAsync
~~~

Usuário sem senha recebe ativação depois do commit. Usuário com senha preserva sua credencial e recebe apenas aviso de acesso liberado.

## Convite sem estado persistente adicional

A UC031 não cria tabela nem status próprio de convite.

Convidar significa:

1. Administrador informa e-mail e perfil;
2. a aplicação localiza ou cria a identidade;
3. cria ou reativa UsuarioEmpresa;
4. o vínculo já fica Ativo;
5. a comunicação ocorre após commit.

Usuário novo ou sem senha só consegue autenticar após concluir a ativação UC040. Usuário já ativado recebe acesso imediatamente.

Não existe aceite separado do vínculo nesta UC.

## Rotas

Estrutura recomendada:

~~~text
GET  /Usuarios
POST /Usuarios?handler=Adicionar

GET  /Usuarios/Detalhes?usuarioId=...
POST /Usuarios/Detalhes?handler=AlterarPerfil
POST /Usuarios/Detalhes?handler=Desvincular
POST /Usuarios/Detalhes?handler=Reativar
POST /Usuarios/Detalhes?handler=ReenviarAtivacao
~~~

## Autorização

Toda a área exige AdministradorEmpresa.

Isso pressupõe:

- usuário autenticado;
- Empresa Ativa válida;
- Empresa ainda Ativa;
- vínculo ativo do usuário atual;
- perfil Administrador na Empresa Ativa.

Operacional é negado.

Administrador da Empresa A não administra Empresa B.

SystemAdmin continua usando /Admin e não a área tenant de usuários.

Empresa Suspensa ou Encerrada não usa UC031. Correções nesses estados permanecem com UC039.

## Navegação

- Administrador da Empresa Ativa vê Usuários;
- Operacional não vê;
- SystemAdmin não vê menu tenant.

A rota permanece protegida mesmo que o menu seja manipulado.

## Lista

GET /Usuarios lista vínculos da Empresa Ativa, incluindo ativos e inativos.

Exibir:

- e-mail;
- perfil;
- situação do vínculo;
- situação da credencial:
  - Ativado quando possui senha;
  - Aguardando ativação quando não possui senha;
- link de detalhes.

Não exibir:

- vínculos ou nomes de outras Empresas;
- roles globais;
- PasswordHash;
- SecurityStamp;
- token.

Ordenação:

~~~text
Ativos primeiro
depois e-mail normalizado ASC
~~~

Filtros permitidos:

~~~text
Ativos
Inativos
Todos
~~~

Default: Ativos.

## Detalhe

GET /Usuarios/Detalhes?usuarioId=...

O recurso só existe se houver vínculo com:

~~~text
UsuarioId = usuarioId
EmpresaId = EmpresaContext.EmpresaId
~~~

Se a identidade global existir, mas não estiver vinculada à Empresa Ativa, retornar 404.

Exibir somente dados do vínculo atual: e-mail, perfil, Ativo/Inativo, situação da credencial e ações permitidas.

## Adicionar usuário

Input:

~~~text
Email
Perfil
~~~

Perfis aceitos:

~~~text
Operacional
Administrador
~~~

EmpresaId nunca vem do formulário. É obtido exclusivamente de EmpresaContext.

E-mail:

- obrigatório;
- trim;
- formato válido;
- máximo 256;
- resolução via UserManager.FindByEmailAsync.

Identidade SystemAdmin não pode ser vinculada. Usar mensagem neutra:

~~~text
Este e-mail não pode ser vinculado à Empresa.
~~~

Não revelar autoridade global.

## Resolução da identidade

### Identidade inexistente

Criar via UserManager.CreateAsync sem senha.

Não usar senha temporária.

### Identidade existente

Reutilizar e preservar:

- PasswordHash;
- SecurityStamp;
- EmailConfirmed;
- UserName/Email;
- roles globais;
- vínculos de outras Empresas.

## Estado do vínculo ao adicionar

### Sem vínculo anterior

Criar:

~~~text
EmpresaId = Empresa Ativa
UsuarioId = identidade resolvida
Ativo = true
Perfil = perfil escolhido
~~~

### Vínculo inativo

Reativar o mesmo registro:

~~~text
Ativo = true
Perfil = perfil escolhido
~~~

Não criar segunda linha.

### Vínculo já ativo

Não duplicar nem alterar perfil silenciosamente.

Retornar erro controlado:

~~~text
Este usuário já possui vínculo ativo com a Empresa.
~~~

Mudança de perfil ocorre somente pela ação Alterar perfil.

## Comunicação pós-commit

Depois do commit:

- sem senha -> EnviarAtivacaoAsync;
- com senha -> EnviarAcessoLiberadoAsync.

Não enviar reset de senha.

Falha ou indisponibilidade de e-mail não desfaz o vínculo.

Mensagem de sucesso não precisa informar se a identidade global já existia.

## Alterar perfil

Somente vínculo ativo da Empresa Ativa.

### Operacional para Administrador

Permitido.

### Administrador para Operacional

Permitido somente se permanecer outro Administrador ativo.

Se for o último:

~~~text
Não é possível remover o último Administrador ativo da Empresa.
~~~

Nenhuma mutação é persistida.

## Desvincular

Desvincular é lógico:

~~~text
UsuarioEmpresa.Ativo = false
~~~

Não executar DELETE.

Preservar:

- UsuarioAplicacao;
- Perfil no vínculo;
- vínculos em outras Empresas;
- credencial;
- histórico.

Se o alvo for Administrador, deve permanecer outro Administrador ativo.

Vínculo já inativo pode ser tratado idempotentemente ou com resposta controlada.

## Reativar

Somente vínculo inativo da Empresa Ativa.

Reativar:

~~~text
Ativo = true
~~~

Preservar perfil existente.

Depois do commit:

- sem senha -> ativação;
- com senha -> aviso de acesso.

Não recriar identidade.

## Reenviar ativação

Somente quando:

~~~text
vínculo pertence à Empresa Ativa
Ativo = true
usuário não possui senha
~~~

Chamar EnviarAtivacaoAsync e tratar Enviado, NaoNecessario, Indisponivel e Falhou.

Não expor token nem alterar vínculo.

## Autoadministração

O Administrador pode atuar sobre o próprio vínculo desde que preserve a invariável do último Administrador.

### Auto-demissão

Se houver outro Administrador ativo:

- perfil passa para Operacional;
- EmpresaContext permanece;
- redirecionar para /Dashboard;
- próxima chamada a /Usuarios é negada.

Se for o último Administrador, bloquear.

### Auto-desvínculo

Se houver outro Administrador ativo:

- Ativo=false;
- limpar EmpresaContext imediatamente;
- redirecionar para /Empresas/Selecionar.

Se for o último Administrador, bloquear.

## Invariável do último Administrador

Enquanto uma Empresa Ativa é administrada pela UC031, deve existir:

~~~text
pelo menos 1 UsuarioEmpresa
Ativo = true
Perfil = Administrador
~~~

As operações que podem reduzir a quantidade são:

- Administrador para Operacional;
- Desvincular Administrador.

A verificação deve ocorrer dentro da transação.

## Concorrência

Cenário crítico:

~~~text
Admin A desvincula Admin B
Admin B desvincula Admin A
ao mesmo tempo
~~~

Resultado obrigatório:

~~~text
pelo menos um Administrador permanece ativo
~~~

Usar:

~~~text
execution strategy
transação SQL
IsolationLevel.Serializable
sp_getapplock por Empresa
~~~

Recurso conceitual:

~~~text
Precificador.UC031.Empresa.<EmpresaId>
~~~

LockOwner = Transaction.

O lock cobre somente a unidade curta de banco. SMTP ocorre depois.

## Concorrência de identidade global

Duas Empresas podem convidar o mesmo e-mail simultaneamente.

Resultado:

~~~text
1 UsuarioAplicacao
1 vínculo na Empresa A
1 vínculo na Empresa B
~~~

A unicidade de AspNetUsers.NormalizedEmail continua sendo a autoridade.

Se houver corrida de criação:

- detectar conflito esperado de e-mail/username;
- limpar estado da tentativa;
- reler a identidade;
- repetir de forma controlada;
- não mascarar outros erros.

## Atomicidade

Adicionar/reativar usuário persiste na mesma unidade:

- identidade nova, quando necessária;
- vínculo;
- perfil.

Falha antes do commit não deixa identidade nova órfã criada exclusivamente pela tentativa.

Alterar perfil e desvincular também são transacionais.

E-mail é pós-commit.

## Ownership

Nenhuma ação recebe EmpresaId editável.

Toda consulta ou escrita de UsuarioEmpresa deve aplicar:

~~~text
EmpresaId = EmpresaContext.EmpresaId
~~~

Administrador da Empresa A não pode, mesmo conhecendo UsuarioId ou EmpresaId:

- consultar vínculo B;
- alterar perfil B;
- desvincular B;
- reativar B;
- reenviar ativação com base no vínculo B.

Convidar explicitamente o mesmo e-mail para A pode reutilizar a identidade global, sem revelar qualquer dado de B.

## SystemAdmin como alvo

A UC031 não pode criar, reativar ou promover vínculo para identidade SystemAdmin.

Se existir dado legado/corrompido, UC031 não deve ampliá-lo. Saneamento global fica fora do escopo.

## Sem administração de credencial

AdministradorEmpresa não pode:

- definir ou resetar senha;
- trocar e-mail;
- marcar EmailConfirmed;
- visualizar tokens;
- visualizar PasswordHash;
- alterar roles Identity.

Recuperação de senha continua sendo fluxo do próprio usuário pela UC040.

## Segurança

Todos os POSTs exigem antiforgery.

Usar InputModels próprios.

Não bindar UsuarioEmpresa ou UsuarioAplicacao diretamente.

Ações de vínculo existente recebem usuarioId, mas sempre revalidam o vínculo na Empresa Ativa.

## Logs

Pode registrar eventos técnicos como vínculo criado, reativado, perfil alterado, desvínculo e falha de envio.

Não registrar senha, token, URL com token, corpo de e-mail, e-mail completo sem necessidade ou dados de outras Empresas.

## Schema

A UC031 usa o schema existente:

~~~text
UsuarioAplicacao
UsuarioEmpresa
PerfilUsuarioEmpresa
~~~

Nenhuma migration é prevista.

Se a implementação concluir que precisa alterar schema, justificar explicitamente na PR antes da mudança.

Não adicionar tabela de convite, senha temporária, EmpresaId em UsuarioAplicacao ou role global de Administrador de Empresa.

## Regras de negócio

### RN071 — Autoridade de usuário pertence ao vínculo da Empresa Ativa

AdministradorEmpresa administra apenas UsuarioEmpresa da Empresa Ativa.

Perfil e situação pertencem ao vínculo, não à identidade global.

Nenhuma operação local altera vínculos de outras Empresas.

### RN072 — Identidade global é reutilizada sem alterar credencial

Convidar e-mail já existente reutiliza UsuarioAplicacao.

Não criar conta duplicada nem alterar senha, e-mail, security stamp ou roles globais.

Identidade nova nasce sem senha e usa UC040.

### RN073 — Empresa não pode ficar sem Administrador ativo

Demover ou desvincular Administrador só é permitido quando outro Administrador ativo permanece.

A invariável deve ser transacional e resistente a concorrência.

### RN074 — Desvínculo é lógico e local ao tenant

Desvincular define UsuarioEmpresa.Ativo=false.

Não remove a conta global e não afeta outros vínculos.

Reativação reutiliza o mesmo registro.

### RN075 — Comunicação ocorre após persistência

Ativação e aviso de acesso são efeitos pós-commit.

Falha de e-mail não desfaz vínculo, perfil ou reativação.

SMTP não participa de transação nem lock.

## Critérios de aceitação

- **CA01:** /Usuarios exige AdministradorEmpresa.
- **CA02:** Operacional é negado.
- **CA03:** Administrador A não administra Empresa B.
- **CA04:** SystemAdmin não usa a navegação tenant de usuários.
- **CA05:** Empresa Suspensa/Encerrada não permite UC031.
- **CA06:** Admin vê menu Usuários; Operacional não.
- **CA07:** lista contém somente vínculos da Empresa Ativa.
- **CA08:** lista suporta ativos e inativos.
- **CA09:** lista não expõe outras Empresas.
- **CA10:** detalhe sem vínculo local retorna 404.
- **CA11:** lista mostra e-mail, perfil, vínculo e ativação.
- **CA12:** Adicionar exige e-mail válido.
- **CA13:** Adicionar exige perfil válido.
- **CA14:** EmpresaId não é controlado pelo request.
- **CA15:** identidade nova é criada sem senha via UserManager.
- **CA16:** identidade existente é reutilizada.
- **CA17:** credencial existente é preservada.
- **CA18:** outros vínculos são preservados.
- **CA19:** SystemAdmin não pode ser vinculado.
- **CA20:** novo vínculo nasce Ativo com perfil escolhido.
- **CA21:** vínculo inativo é reativado, não duplicado.
- **CA22:** re-convite de vínculo inativo aplica perfil escolhido.
- **CA23:** vínculo ativo não é duplicado nem muda perfil silenciosamente.
- **CA24:** usuário sem senha recebe ativação pós-commit.
- **CA25:** usuário com senha recebe aviso, sem reset.
- **CA26:** falha de e-mail não desfaz vínculo.
- **CA27:** Alterar perfil só atua em vínculo ativo local.
- **CA28:** Operacional pode ser promovido.
- **CA29:** Admin pode ser demovido se outro Admin permanecer.
- **CA30:** último Admin não pode ser demovido.
- **CA31:** Desvincular define Ativo=false sem DELETE.
- **CA32:** desvínculo preserva perfil.
- **CA33:** desvínculo preserva UsuarioAplicacao.
- **CA34:** desvínculo preserva outros vínculos.
- **CA35:** último Admin não pode ser desvinculado.
- **CA36:** ação sobre vínculo já inativo é controlada.
- **CA37:** Reativar reutiliza o mesmo UsuarioEmpresa.
- **CA38:** Reativar preserva perfil.
- **CA39:** reativação sem senha envia ativação pós-commit.
- **CA40:** reativação com senha envia aviso.
- **CA41:** Reenviar ativação exige vínculo ativo local.
- **CA42:** reenvio não cria usuário/vínculo.
- **CA43:** reenvio não expõe token.
- **CA44:** auto-demissão exige outro Admin.
- **CA45:** auto-demissão redireciona Dashboard.
- **CA46:** auto-desvínculo exige outro Admin.
- **CA47:** auto-desvínculo limpa EmpresaContext.
- **CA48:** auto-desvínculo redireciona seleção de Empresa.
- **CA49:** concorrência nunca deixa zero Admin ativo.
- **CA50:** cenários mistos de demissão/desvínculo preservam Admin.
- **CA51:** duas Empresas convidando mesmo e-mail criam uma identidade.
- **CA52:** concorrência cria um vínculo correto em cada Empresa.
- **CA53:** falha pré-commit não deixa identidade nova órfã.
- **CA54:** SMTP não roda dentro de transação/lock.
- **CA55:** usuarioId cross-tenant não altera vínculo.
- **CA56:** EmpresaId manipulado não altera ownership.
- **CA57:** perfil inválido não persiste.
- **CA58:** vínculo inativo não concede acesso.
- **CA59:** Admin demovido perde UC031 na próxima requisição.
- **CA60:** usuário desvinculado perde acesso ao tenant.
- **CA61:** UC031 não altera senha.
- **CA62:** UC031 não altera e-mail global.
- **CA63:** UC031 não altera roles Identity.
- **CA64:** UC031 não expõe PasswordHash/SecurityStamp.
- **CA65:** POSTs exigem antiforgery.
- **CA66:** InputModels evitam mass assignment.
- **CA67:** nenhuma tabela de convite é criada.
- **CA68:** nenhuma migration é necessária salvo justificativa.
- **CA69:** UC039 continua responsável por Empresa suspensa.
- **CA70:** UC040 ativação/recuperação continua funcionando.
- **CA71:** Login/seleção continuam usando apenas vínculos ativos.
- **CA72:** Build Release verde.
- **CA73:** unitários aplicáveis verdes.
- **CA74:** integração SQL Server/Web verde.

## Matriz mínima de testes

### Autorização e navegação

- anônimo -> Login;
- Operacional -> negado;
- AdministradorEmpresa -> permitido;
- Administrador A com B ativa -> negado;
- vínculo Admin inativo -> negado;
- Empresa inativa -> negado;
- Admin vê Usuários;
- Operacional/SystemAdmin não veem menu tenant.

### Lista/detalhe

Preparar Admin ativo, Operacional ativo, vínculo inativo e usuário apenas em outra Empresa.

Comprovar filtro/ordenação, estado de credencial e ausência de qualquer dado cross-tenant.

### Adicionar

Cobrir:

- identidade nova;
- existente com senha;
- existente sem senha;
- vínculo inativo;
- vínculo já ativo;
- SystemAdmin proibido;
- perfis Operacional e Administrador.

Validar comunicação pós-commit e preservação de hash/stamp/outros vínculos.

### Perfil

- Operacional para Admin;
- Admin para Operacional com outro Admin;
- último Admin bloqueado;
- perfil inválido;
- alvo cross-tenant bloqueado.

### Desvínculo e reativação

- Operacional desvinculado;
- Admin com outro Admin desvinculado;
- último Admin bloqueado;
- sem DELETE;
- outros vínculos preservados;
- reativação preserva perfil;
- comunicação conforme senha.

### Reenvio

Cobrir Enviado, NaoNecessario, Indisponivel e Falhou sem duplicação.

### Autoadministração

Auto-demissão com dois Admins: vira Operacional, mantém EmpresaContext, vai ao Dashboard e perde /Usuarios.

Auto-desvínculo com dois Admins: Ativo=false, limpa EmpresaContext e vai para /Empresas/Selecionar.

Ambos bloqueados quando ator é o último Admin.

### Concorrência último Admin

Com SQL Server real:

~~~text
A desvincula B
B desvincula A
~~~

e:

~~~text
A demove B
B desvincula A
~~~

Resultado: pelo menos um Admin ativo, sem deadlock/500 esperado.

### Concorrência Identity cross-tenant

Empresas A e B convidam simultaneamente o mesmo e-mail.

Resultado: uma UsuarioAplicacao e dois UsuarioEmpresa, cada um no tenant correto.

### Rollback

Forçar falha pré-commit depois da criação de identidade nova.

Comprovar zero identidade órfã, zero vínculo e zero e-mail.

### Cross-tenant / mass assignment

Testar detalhe, alteração, desvínculo, reativação e reenvio com usuarioId externo.

EmpresaId manipulado não move vínculo.

### Regressão

- /Admin UC039 continua funcional;
- UC039 definir/substituir Admin continua verde;
- UC040 ativação/reset continua verde;
- login com um vínculo -> Dashboard;
- login com múltiplos -> seleção;
- vínculo inativo não é elegível;
- Operacional continua acessando páginas operacionais tenant.

## Fora do escopo

- criar/alterar Empresa;
- lifecycle da Empresa;
- remover conta global;
- hard delete de UsuarioEmpresa;
- trocar e-mail;
- definir/resetar senha pelo Admin;
- forçar recuperação de senha;
- alterar SystemAdmin;
- roles globais;
- 2FA/SSO;
- permissões além de Administrador/Operacional;
- aceite separado/expiração própria de convite;
- tabela de convite;
- auditoria completa;
- display name/telefone;
- grupos/equipes;
- transferência de ownership;
- busca global;
- paginação avançada.

## Definition of Done

UC031 concluída quando:

- AdministradorEmpresa gerencia somente a Empresa Ativa;
- lista/detalhe não vazam cross-tenant;
- identidade pode ser criada/reutilizada por e-mail;
- credencial global é preservada;
- usuário novo usa UC040;
- vínculo pode ser criado, reativado, ter perfil alterado e ser desvinculado logicamente;
- SystemAdmin não pode ser alvo;
- último Admin é protegido sob concorrência;
- auto-demissão/desvínculo tratam EmpresaContext;
- e-mail é pós-commit;
- não existe tabela/estado extra de convite;
- não há migration salvo justificativa;
- UC039/UC040 permanecem verdes;
- RN071–RN075 alinhadas;
- CI completa verde;
- backlog marca UC031 Concluído após implementação.

## Branch sugerida

~~~text
feat/uc031-administrar-usuarios-vinculos
~~~

## Commit sugerido

~~~text
feat: adiciona administracao tenant de usuarios
~~~
