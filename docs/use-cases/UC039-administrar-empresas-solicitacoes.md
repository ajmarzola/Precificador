# UC039 — Administrar Empresas e solicitações de acesso

- **Área funcional:** Administração global / multiempresa
- **Dependências:** FT003, UC038, UC040
- **Estado:** Pronto
- **Alteração de schema:** sim
- **Autorização:** SystemAdmin
- **Regras principais:** RN065 a RN070

## Objetivo

Permitir que o SystemAdmin transforme uma solicitação pública da UC038 em uma Empresa real e administrável, sem auto-registro e sem receber acesso implícito aos dados tenant-owned.

A UC039 entrega:

- dashboard global em /Admin;
- listagem e detalhe de solicitações;
- aprovação e recusa;
- criação transacional de Empresa + configuração padrão + primeiro Administrador;
- reutilização segura de identidade já existente;
- reenvio de ativação;
- listagem e detalhe de Empresas reais;
- correção explícita de Empresa legada sem Administrador;
- definição/substituição de Administrador;
- suspensão, reativação e encerramento lógico da Empresa.

A gestão cotidiana dos demais usuários/vínculos continua pertencendo à UC031.

## Fronteira de autoridade

Todas as páginas e ações da UC039 exigem role global SystemAdmin.

Não exigir Empresa Ativa, UsuarioEmpresa ou perfil Administrador de Empresa.

Administrador de Empresa sem SystemAdmin não acessa /Admin.

SystemAdmin não ganha acesso a Insumos, Produtos, Fichas Técnicas, preços, margens ou configurações comerciais tenant-owned.

## /Admin

Evoluir o shell da FT003 para um dashboard global com:

- solicitações Pendentes;
- Empresas reais Ativas;
- Empresas reais Suspensas;
- Empresas reais Encerradas;
- link Solicitações de acesso;
- link Empresas.

Não incluir Empresa técnica nos contadores.

## Empresa técnica legada

### Problema

Existe desde FT002/MEL020 o seed:

~~~text
Id = 1
Nome = Empresa inicial
NomeNormalizado = EMPRESA INICIAL
~~~

FT003 deliberadamente não o removeu.

UC039 deve distingui-lo de Empresas reais antes de expor administração global.

### Campo EhTecnica

Adicionar à Empresa:

~~~text
EhTecnica : bool
~~~

Empresa.Criar(...) produz EhTecnica=false.

Empresa.CriarTecnica(...) produz EhTecnica=true.

### Migration segura

Não marcar Id=1 como técnica de forma cega.

Há instalações antigas em que a Empresa 1 foi renomeada para um cliente real.

Marcar técnica somente quando:

~~~text
Id = 1
AND NomeNormalizado = EMPRESA INICIAL
~~~

Cenários:

~~~text
Id=1 / EMPRESA INICIAL -> EhTecnica=true
Id=1 / CARINHO E AMOR  -> EhTecnica=false
nova Empresa           -> EhTecnica=false
~~~

Não renomear nem excluir o seed.

Empresa técnica:

- não aparece na lista de Empresas reais;
- não entra em contadores;
- não recebe Administrador pela UC039;
- não pode ser suspensa, reativada ou encerrada.

## Ciclo de vida da Empresa

Manter Empresa.Ativo como gate operacional existente.

Adicionar:

~~~text
EncerradaEmUtc : DateTimeOffset?
~~~

Estado administrativo derivado:

~~~text
Ativa:
EhTecnica=false
Ativo=true
EncerradaEmUtc=null

Suspensa:
EhTecnica=false
Ativo=false
EncerradaEmUtc=null

Encerrada:
EhTecnica=false
Ativo=false
EncerradaEmUtc!=null
~~~

Pode existir enum derivado SituacaoAdministrativaEmpresa com valores Ativa=1, Suspensa=2 e Encerrada=3, sem coluna própria.

Adicionar constraint impedindo:

~~~text
Ativo=true AND EncerradaEmUtc IS NOT NULL
~~~

### Suspender

- somente Empresa real;
- não Encerrada;
- Ativo=false;
- EncerradaEmUtc permanece null;
- vínculos e dados permanecem;
- não há hard delete.

### Reativar

- somente Empresa real Suspensa;
- não Encerrada;
- exige ao menos um UsuarioEmpresa Ativo com Perfil Administrador;
- Ativo=true.

### Encerrar

- somente Empresa real;
- usa TimeProvider.GetUtcNow();
- Ativo=false;
- EncerradaEmUtc recebe UTC;
- preserva Empresa, usuários, vínculos, configuração e todos os dados tenant-owned.

Encerramento é terminal nesta UC. Não oferecer reabertura.

## Empresa legada real sem Administrador

FT003 converteu vínculos históricos para Perfil Operacional.

Logo uma Empresa real legada pode estar Ativa e não possuir nenhum Administrador.

Não promover ninguém automaticamente.

Exibir indicador:

~~~text
Sem administrador
~~~

SystemAdmin corrige explicitamente por Definir administrador.

Não suspender essa Empresa automaticamente na migration.

## Evolução da Solicitação

Adicionar a SolicitacaoAcessoEmpresa:

~~~text
EmpresaId : int?
DataDecisaoUtc : DateTimeOffset?
DecididaPorUsuarioId : string?
MotivoRecusa : string?
~~~

### Invariantes

Pendente:

~~~text
EmpresaId=null
DataDecisaoUtc=null
DecididaPorUsuarioId=null
MotivoRecusa=null
~~~

Aprovada:

~~~text
EmpresaId!=null
DataDecisaoUtc!=null
DecididaPorUsuarioId!=null
MotivoRecusa=null
~~~

Recusada:

~~~text
EmpresaId=null
DataDecisaoUtc!=null
DecididaPorUsuarioId!=null
MotivoRecusa opcional <= 500
~~~

EmpresaId é FK Restrict para Empresas.

DecididaPorUsuarioId identifica o SystemAdmin que decidiu e deve possuir FK Restrict para Identity configurada na Infrastructure, sem navegação/dependência do Core em ASP.NET Core Identity.

MotivoRecusa:

- opcional;
- trim;
- whitespace vira null;
- máximo 500;
- uso interno;
- não enviado ao solicitante.

Adicionar check constraint para combinações válidas.

### Métodos de domínio

Preferir:

~~~text
Aprovar(empresaId, decididaPorUsuarioId, dataUtc)
Recusar(decididaPorUsuarioId, dataUtc, motivo)
~~~

Somente Pendente pode receber decisão.

Aprovada/Recusada são estados terminais.

## Lista de solicitações

Criar:

~~~text
GET /Admin/Solicitacoes
~~~

Filtro:

~~~text
Pendente
Aprovada
Recusada
Todas
~~~

Default: Pendente.

Ordenação:

- Pendentes: DataSolicitacaoUtc ASC;
- demais: DataSolicitacaoUtc DESC.

Exibir:

- Empresa solicitada;
- responsável;
- e-mail;
- data;
- situação;
- detalhes.

Observação completa apenas no detalhe.

## Detalhe da solicitação

Criar:

~~~text
GET /Admin/Solicitacoes/Detalhes/{id:int}
~~~

Exibir dados originais, Observação, data, situação e dados da decisão.

Aprovada mostra link para Empresa criada.

Somente Pendente mostra Aprovar e Recusar.

## Aprovação — nome final

Aprovação permite corrigir apenas o Nome final da Empresa.

Default = NomeEmpresa da solicitação.

Aplicar exatamente normalização/limite/unicidade de Empresa.

Não permitir editar:

- responsável;
- e-mail;
- situação;
- EmpresaId;
- SystemAdmin da decisão.

Timezone da nova Empresa usa Empresa.Criar(nome), preservando America/Sao_Paulo.

NomeNormalizado já existente bloqueia aprovação.

Não vincular automaticamente o solicitante a uma Empresa existente com mesmo nome.

A solicitação permanece Pendente para investigação/recusa manual.

## Aprovação — identidade do responsável

O e-mail da solicitação define o primeiro Administrador.

### Usuário inexistente

Criar UsuarioAplicacao via UserManager.CreateAsync sem senha.

Depois do commit, enviar ativação pela UC040.

### Usuário existente sem senha

Reutilizar identidade.

Não duplicar usuário.

Criar vínculo Administrador.

Depois do commit, enviar/reemitir ativação.

### Usuário existente com senha

Reutilizar identidade.

Não alterar PasswordHash, senha, EmailConfirmed ou security stamp por causa do novo vínculo.

Criar vínculo Administrador.

Depois do commit, enviar aviso simples de acesso liberado, sem token de reset.

### E-mail de SystemAdmin

Bloquear aprovação.

Não misturar incidentalmente autoridade global e tenant na mesma identidade porque o Login prioriza SystemAdmin.

Mensagem administrativa clara:

~~~text
O responsável informado é um Administrador do Sistema. Use outro e-mail para o Administrador da Empresa.
~~~

Não remover role nem criar vínculo.

## Comunicação para identidade já ativada

Adicionar operação pequena ao serviço de conta/e-mail para enviar:

~~~text
Assunto: Acesso liberado ao Precificador

Seu acesso à Empresa <nome> foi liberado.
Entre no Precificador usando sua credencial existente.
~~~

Usar Aplicacao:UrlPublica + /Conta/Login.

Não enviar senha.

Falha de envio não desfaz aprovação.

## Aprovação transacional

A aprovação é uma única unidade de banco.

Usar execution strategy + transação SQL.

Dentro da transação:

1. recarregar solicitação;
2. confirmar Pendente;
3. validar Nome final;
4. confirmar ausência de Empresa com mesmo NomeNormalizado;
5. resolver/criar UsuarioAplicacao;
6. rejeitar identidade SystemAdmin;
7. criar Empresa real;
8. SaveChanges para obter Empresa.Id;
9. criar ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresa.Id);
10. criar UsuarioEmpresa Ativo/Administrador;
11. marcar solicitação Aprovada com EmpresaId, data e SystemAdmin;
12. persistir;
13. commit.

Somente após commit tentar enviar e-mail.

A transação deve incluir as escritas Identity do UserManager no mesmo escopo/DbContext.

Falha antes do commit não pode deixar usuário novo, Empresa, configuração, vínculo ou decisão parcial.

Falha do e-mail depois do commit não executa rollback.

UI deve informar:

~~~text
A Empresa foi criada, mas a comunicação ao Administrador não pôde ser enviada.
~~~

## Concorrência

Dois POSTs concorrentes para a mesma Solicitação não podem criar dois tenants.

Revalidar Pendente dentro da transação.

É aceitável usar Serializable, rowversion ou locking SQL apropriado.

Obrigatório teste com SQL Server real provando:

- uma Empresa;
- uma decisão;
- um vínculo;
- no máximo um novo usuário;
- segunda tentativa controlada;
- sem estado parcial.

Não depender apenas de botão desabilitado.

## Concorrência entre solicitações diferentes

A UC038 permite o mesmo e-mail solicitar Empresas diferentes.

Se duas solicitações diferentes do mesmo e-mail forem aprovadas concorrentemente:

- deve existir uma única UsuarioAplicacao para o e-mail;
- cada Empresa aprovada recebe seu próprio vínculo Administrador;
- nenhuma aprovação válida pode terminar em erro 500 apenas por corrida de criação da identidade.

A implementação deve tratar a unicidade de e-mail do Identity como fonte de verdade. Se uma tentativa perder a corrida de criação da conta, deve resolver novamente a identidade já persistida e continuar de forma controlada quando a transação permitir/repetir a operação com segurança.

Também é possível haver duas solicitações para o mesmo NomeEmpresaNormalizado com e-mails diferentes.

Nesse caso:

- apenas uma pode criar a Empresa daquele nome;
- a outra continua Pendente;
- a segunda não recebe vínculo automático no tenant criado pela primeira.

Esses dois cenários devem possuir testes de integração com SQL Server real.

## Configuração padrão

Toda Empresa criada pela aprovação recebe, na mesma transação:

~~~text
ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaId)
~~~

Aprovação sem configuração é inválida.

## Primeiro vínculo

Criar:

~~~text
UsuarioEmpresa
UsuarioId = responsável
EmpresaId = nova Empresa
Ativo = true
Perfil = Administrador
~~~

Não criar vínculo Operacional adicional.

Não vincular SystemAdmin.

## Recusa

POST autenticado/antiforgery.

Somente Pendente.

Campo opcional Motivo interno <= 500.

Fluxo:

1. carregar/revalidar Pendente;
2. marcar Recusada;
3. DataDecisaoUtc = TimeProvider.GetUtcNow();
4. DecididaPorUsuarioId = SystemAdmin atual;
5. persistir;
6. não criar Empresa;
7. não criar usuário;
8. não criar vínculo.

Depois do commit, se SMTP disponível, enviar comunicação simples:

~~~text
Sua solicitação de acesso ao Precificador foi analisada e não foi aprovada neste momento.
~~~

Não incluir MotivoRecusa.

Falha de e-mail não desfaz recusa.

Reenvio de recusa não é obrigatório nesta UC.

## Lista de Empresas

Criar:

~~~text
GET /Admin/Empresas
~~~

Somente EhTecnica=false.

Exibir:

- Nome;
- situação;
- quantidade de Administradores ativos;
- Sem administrador quando zero;
- detalhes.

Ordenação por NomeNormalizado.

Filtro opcional: Ativa, Suspensa, Encerrada, Todas.

Default: Todas.

Não exibir dados comerciais tenant-owned.

## Detalhe da Empresa

Criar:

~~~text
GET /Admin/Empresas/Detalhes/{id:int}
~~~

Exibir:

- Nome;
- situação;
- TimeZoneId;
- Administradores ativos;
- aviso Sem administrador;
- solicitação de origem quando houver;
- EncerradaEmUtc quando houver.

Não mostrar Insumos, Produtos, Fichas, preços, margens ou configurações financeiras.

## Definir Administrador

Disponível para Empresa real não Encerrada sem Administradores ativos.

Input:

~~~text
E-mail
~~~

Resolver identidade:

- inexistente -> criar sem senha;
- existente sem senha -> reutilizar;
- existente com senha -> reutilizar;
- SystemAdmin -> rejeitar.

Se vínculo já existir:

- Ativo/Operacional -> promover;
- inativo -> reativar + Administrador.

Se não existir, criar Ativo/Administrador.

Usuário sem senha recebe ativação após commit.

Usuário com senha recebe aviso de acesso.

Essa operação corrige explicitamente o legado FT003 sem auto-promoção.

## Substituir Administrador

Para Empresa real não Encerrada.

Inputs:

~~~text
Administrador atual
Novo e-mail
~~~

Regras:

1. administrador atual deve ser Ativo/Administrador nessa Empresa;
2. novo usuário deve ser diferente;
3. novo usuário não pode ser SystemAdmin;
4. resolver/criar identidade;
5. criar/reativar/promover novo vínculo para Administrador;
6. somente depois demover o administrador selecionado para Operacional;
7. vínculo antigo permanece Ativo;
8. outros Administradores não mudam;
9. vínculos em outras Empresas não mudam.

Tudo em uma transação.

Empresa Suspensa pode corrigir Administrador antes de reativar.

Empresa Encerrada não permite definir/substituir.

## Reenviar ativação

No detalhe da Empresa, para vínculo Ativo/Administrador cujo usuário ainda não possui senha:

~~~text
Reenviar ativação
~~~

POST antiforgery.

Revalidar Empresa e vínculo.

Empresa não pode estar Encerrada.

Chamar ServicoConta.EnviarAtivacaoAsync.

Resultados tratados:

~~~text
Enviado
NaoNecessario
Indisponivel
Falhou
~~~

Não criar usuário/vínculo e não expor token.

## Suspender

POST antiforgery.

Somente Empresa real não Encerrada.

Resultado:

~~~text
Ativo=false
EncerradaEmUtc=null
~~~

Preservar vínculos e dados.

Repetição pode ser idempotente.

## Reativar

Somente Empresa real Suspensa e não Encerrada.

Exigir ao menos um vínculo Ativo/Administrador.

Resultado:

~~~text
Ativo=true
EncerradaEmUtc=null
~~~

Não reativar vínculos inativos automaticamente.

## Encerrar

POST antiforgery com confirmação explícita.

Somente Empresa real não Encerrada.

Resultado:

~~~text
Ativo=false
EncerradaEmUtc=TimeProvider.GetUtcNow()
~~~

Preservar todos os dados e vínculos.

Não hard-delete.

Não oferecer Reativar após encerramento.

## Efeito nos fluxos tenant

Como Login/seleção/policies já verificam Empresa.Ativo:

- Suspensa deixa de ser elegível;
- Encerrada deixa de ser elegível;
- contexto antigo deve ser invalidado pela policy ao detectar Empresa inativa;
- reativação volta a permitir uso quando há vínculo ativo.

Não alterar o princípio de Empresa Ativa.

## Navegação SystemAdmin

Menu global:

~~~text
Administração
- Solicitações
- Empresas
~~~

Brand/Home do SystemAdmin continua /Admin.

Não mostrar menus tenant.

## Segurança

Todas as rotas /Admin/** exigem SystemAdmin.

Todos os POSTs exigem antiforgery.

InputModels próprios; não bindar entidades EF diretamente.

Request não controla:

- SystemAdmin da decisão;
- EmpresaId da aprovação;
- Situacao;
- Perfil;
- EhTecnica;
- Ativo;
- EncerradaEmUtc.

Não registrar token, senha, URL com token, corpo de e-mail ou motivo completo de recusa em logs.

## Migration

Adicionar em Empresas:

~~~text
EhTecnica bit NOT NULL
EncerradaEmUtc datetimeoffset NULL
~~~

Backfill EhTecnica por condição Id=1 + EMPRESA INICIAL.

Demais Empresas ficam false.

Adicionar constraint de encerramento.

Adicionar em SolicitacoesAcessoEmpresas:

~~~text
EmpresaId int NULL
DataDecisaoUtc datetimeoffset NULL
DecididaPorUsuarioId nvarchar(450) NULL
MotivoRecusa nvarchar(500) NULL
~~~

Adicionar FKs, índices e check de invariantes.

Solicitações existentes permanecem Pendente com campos novos null.

Não editar migrations históricas.

## Regras de negócio

### RN065 — Aprovação cria tenant completo e um Administrador

Aprovar uma Solicitação Pendente cria na mesma unidade transacional:

- Empresa real;
- configuração padrão;
- identidade nova ou reutilizada;
- vínculo Ativo/Administrador;
- decisão Aprovada ligada à Empresa.

Falha antes do commit não deixa estado parcial.

### RN066 — Identidade global é reutilizada e credencial preservada

E-mail existente identifica a mesma conta global.

Novo vínculo não cria conta duplicada e não altera senha existente ou vínculos de outras Empresas.

Usuário sem senha recebe ativação.

Usuário com senha mantém credencial.

SystemAdmin não pode ser usado como Administrador de Empresa pela UC039.

### RN067 — Solicitação decidida é terminal

Somente Pendente pode ser Aprovada ou Recusada.

Decisão não volta a Pendente nem troca de estado.

Recusa não cria tenant.

### RN068 — Empresa real deve possuir caminho administrativo válido

Nova Empresa nasce com Administrador ativo.

Reativar exige Administrador ativo.

Substituição promove o novo antes de demover o anterior na mesma transação.

Empresa legada sem Admin não recebe auto-promoção.

### RN069 — Suspensão e encerramento preservam dados

Suspensão e encerramento são lógicos.

Não removem Empresa, vínculos, usuários ou dados tenant-owned.

Suspensão é reversível.

Encerramento é terminal nesta UC.

### RN070 — Empresa técnica não é tenant administrável

O seed Empresa inicial é infraestrutura quando ainda preserva sua identidade técnica original.

Não aparece como Empresa real e não recebe ações administrativas da UC039.

Empresa 1 historicamente renomeada deve permanecer real.

## Critérios de aceitação

- **CA01:** /Admin continua exclusivo de SystemAdmin.
- **CA02:** dashboard Admin exibe contadores globais.
- **CA03:** Empresa técnica não entra em lista/contadores.
- **CA04:** /Admin/Solicitacoes exige SystemAdmin.
- **CA05:** filtro default de solicitações é Pendente.
- **CA06:** filtros de situação funcionam.
- **CA07:** detalhe mostra Observação e dados da decisão.
- **CA08:** somente Pendente pode Aprovar/Recusar.
- **CA09:** aprovação edita somente Nome final.
- **CA10:** Nome final segue normalização/unicidade de Empresa.
- **CA11:** nome já existente bloqueia aprovação.
- **CA12:** colisão de nome não vincula automaticamente tenant existente.
- **CA13:** aprovação cria Empresa EhTecnica=false.
- **CA14:** nova Empresa nasce Ativa e não Encerrada.
- **CA15:** aprovação cria Configuração padrão.
- **CA16:** responsável inexistente gera usuário sem senha.
- **CA17:** responsável existente reutiliza usuário.
- **CA18:** usuário com senha mantém credencial.
- **CA19:** usuário sem senha recebe ativação pós-commit.
- **CA20:** usuário novo recebe ativação pós-commit.
- **CA21:** usuário com senha recebe aviso sem reset.
- **CA22:** e-mail de SystemAdmin bloqueia aprovação.
- **CA23:** primeiro vínculo é Ativo/Administrador.
- **CA24:** SystemAdmin aprovador não recebe vínculo.
- **CA25:** aprovação registra EmpresaId.
- **CA26:** decisão usa TimeProvider.GetUtcNow().
- **CA27:** decisão registra SystemAdmin autenticado.
- **CA28:** mass assignment administrativo é bloqueado.
- **CA29:** falha pré-commit não deixa estado parcial.
- **CA30:** falha de e-mail pós-commit não desfaz aprovação.
- **CA31:** Admin sem senha permite Reenviar ativação.
- **CA32:** reenvio não duplica usuário/vínculo.
- **CA33:** aprovação concorrente da mesma solicitação cria no máximo uma Empresa.
- **CA34:** recusa só aceita Pendente.
- **CA35:** motivo de recusa opcional <=500.
- **CA36:** recusa registra data/admin.
- **CA37:** recusa não cria Empresa/user/vínculo.
- **CA38:** motivo interno não é enviado.
- **CA39:** falha de e-mail não desfaz recusa.
- **CA40:** estados decididos não podem ser alterados.
- **CA41:** /Admin/Empresas lista somente EhTecnica=false.
- **CA42:** status Ativa/Suspensa/Encerrada é derivado corretamente.
- **CA43:** legado real sem Admin mostra Sem administrador.
- **CA44:** migration não auto-promove legado.
- **CA45:** Definir administrador corrige legado explicitamente.
- **CA46:** Definir administrador reutiliza/promove/reativa vínculo.
- **CA47:** Definir administrador pode criar identidade sem senha.
- **CA48:** Substituir promove novo antes de demover atual.
- **CA49:** outros Administradores permanecem.
- **CA50:** antigo Admin permanece Ativo/Operacional.
- **CA51:** vínculos de outras Empresas permanecem.
- **CA52:** SystemAdmin não pode ser alvo de Definir/Substituir.
- **CA53:** Suspender define Ativo=false e EncerradaEmUtc=null.
- **CA54:** suspensão preserva vínculos/dados.
- **CA55:** Empresa suspensa deixa de ser elegível em Login/seleção.
- **CA56:** Reativar exige Admin ativo.
- **CA57:** Reativar não funciona para Encerrada.
- **CA58:** Encerrar define Ativo=false.
- **CA59:** Encerrar grava EncerradaEmUtc via TimeProvider.
- **CA60:** Encerrada não oferece reativação.
- **CA61:** encerramento não hard-deleta dados.
- **CA62:** Empresa técnica não recebe ações UC039.
- **CA63:** migration marca técnica apenas Id=1 ainda EMPRESA INICIAL.
- **CA64:** Id=1 renomeada permanece Empresa real.
- **CA65:** solicitações antigas permanecem Pendentes.
- **CA66:** check de solicitação rejeita combinações inválidas.
- **CA67:** check de Empresa rejeita Ativo=true + encerramento.
- **CA68:** migrations históricas não são editadas.
- **CA69:** banco vazio aplica todas as migrations.
- **CA70:** upgrade pós-UC040 preserva Identity, Empresas, solicitações, key ring e dados.
- **CA71:** páginas Admin não exigem Empresa Ativa.
- **CA72:** AdministradorEmpresa sem SystemAdmin é negado.
- **CA73:** POSTs exigem antiforgery.
- **CA74:** SystemAdmin não vê dados comerciais tenant-owned.
- **CA75:** UC031 permanece fora do escopo.
- **CA76:** Build Release verde.
- **CA77:** unitários aplicáveis verdes.
- **CA78:** integração SQL Server/Web verde.
- **CA79:** duas solicitações concorrentes de Empresas diferentes para o mesmo e-mail resultam em uma identidade global e dois vínculos Administrador, sem erro 500.
- **CA80:** duas solicitações concorrentes para o mesmo nome de Empresa criam no máximo um tenant; a perdedora continua Pendente e não é vinculada automaticamente.

## Matriz mínima de testes

### Domínio Empresa

- Criar -> real/Ativa;
- CriarTecnica -> técnica;
- Suspender;
- Reativar;
- Encerrar;
- técnica rejeita lifecycle;
- Encerrada rejeita reativação;
- status derivado.

### Domínio Solicitação

- Pendente nasce sem decisão;
- Aprovar preenche empresa/data/admin;
- Recusar preenche data/admin/motivo;
- motivo whitespace -> null;
- motivo >500 rejeita;
- segunda decisão rejeita.

### Migration

Testar:

- banco vazio;
- upgrade pós-UC040;
- seed intacto -> técnica;
- Id1 renomeada -> real;
- Pendente antiga mantém nulls;
- Identity, UsuarioEmpresa, Solicitação, DataProtectionKeys e dados tenant preservados;
- constraints.

### Autorização

Para Solicitações e Empresas:

- anônimo -> Login;
- tenant user -> negado;
- AdministradorEmpresa -> negado;
- SystemAdmin sem Empresa Ativa -> permitido.

### Aprovação novo usuário

Com sender fake:

- Empresa;
- configuração;
- usuário sem senha;
- vínculo Admin;
- solicitação Aprovada;
- ativação;
- zero vínculo do SystemAdmin.

### Aprovação usuário existente com senha

- user count não aumenta;
- hash/stamp preservados;
- vínculo Admin;
- aviso simples;
- nenhuma ativação/reset;
- vínculos prévios preservados.

### Aprovação usuário existente sem senha

- user count não aumenta;
- vínculo criado;
- ativação enviada.

### SystemAdmin responsável

- aprovação rejeitada;
- zero efeitos;
- solicitação permanece Pendente.

### Colisão de Empresa

- Empresa já existente;
- aprovação bloqueada;
- sem vínculo;
- Pendente permanece.

### Rollback

Forçar falha de persistência antes do commit e provar ausência de Empresa/user/config/vínculo/decisão parcial.

### Concorrência

Dois POSTs simultâneos na mesma Pendente:

- uma Empresa;
- uma decisão;
- um vínculo;
- no máximo um usuário novo;
- resposta controlada para perdedor.

### Concorrência cross-request

Testar com SQL Server real:

1. duas solicitações diferentes, mesmo e-mail, nomes de Empresa diferentes:
   - duas aprovações válidas;
   - duas Empresas;
   - uma UsuarioAplicacao;
   - dois vínculos Administrador;

2. duas solicitações, mesmo NomeEmpresaNormalizado, e-mails diferentes:
   - uma Empresa;
   - uma Aprovada;
   - outra permanece Pendente;
   - nenhum vínculo automático do segundo solicitante.

### Falha de e-mail

Após commit:

- aprovação permanece;
- Admin sem senha permanece sem senha;
- UI alerta;
- Reenviar ativação posterior funciona.

### Recusa

- Pendente -> Recusada;
- zero tenant;
- motivo interno;
- mensagem não contém motivo;
- sender falhando não reabre.

### Empresas

- técnica omitida;
- real exibida;
- status;
- Sem administrador.

### Legado sem Admin

Empresa real + vínculo Operacional:

- não auto-promove;
- Definir administrador promove explicitamente.

### Definir Administrador

Cobrir usuário novo, existente com senha, existente sem senha, vínculo Operacional, vínculo inativo e SystemAdmin proibido.

### Substituir Administrador

- novo promovido antes da demissão lógica;
- anterior vira Operacional;
- outros Admins preservados;
- outras Empresas preservadas;
- sem janela persistida de zero Admin.

### Suspensão/Reativação/Encerramento

- acesso tenant bloqueado quando inativa;
- sessão antiga perde acesso;
- reativação com Admin funciona;
- reativação sem Admin falha;
- encerramento preserva dados;
- encerrada não reativa.

### Regressão

- Setup/SystemAdmin;
- UC038 continua criando Pendente;
- UC040 ativação/reset;
- Empresa técnica continua disponível como infraestrutura;
- SystemAdmin continua sem acesso tenant automático.

## Fora do escopo

- CRUD cotidiano de usuários/vínculos — UC031;
- hard delete de Empresa;
- reabrir Empresa Encerrada;
- merge/transferência entre Empresas;
- vincular automaticamente pedido a tenant existente;
- alterar e-mail do solicitante/usuário;
- definir senha por admin;
- múltiplos SystemAdmins;
- 2FA/SSO;
- auditoria global completa;
- paginação avançada;
- dados comerciais para SystemAdmin;
- editar configurações financeiras;
- edição geral de Nome/Timezone após criação;
- identidade visual — UC037.

## Definition of Done

UC039 concluída quando:

- /Admin possui dashboard global útil;
- solicitações são listadas/detalhadas/decididas;
- aprovação cria tenant completo e primeiro Admin transacionalmente;
- identidade existente é reutilizada sem tocar senha;
- identidade nova/sem senha usa UC040;
- falha de e-mail não corrompe banco;
- reenvio de ativação existe;
- seed técnico é distinguido sem destruir legado renomeado;
- Empresas reais são administráveis;
- legado sem Admin é corrigível explicitamente;
- substituição de Admin preserva ao menos um caminho administrativo;
- suspensão/reativação/encerramento lógico funcionam;
- nenhum hard delete é introduzido;
- SystemAdmin permanece fora dos dados tenant;
- migrations clean/upgrade passam;
- RN065–RN070 alinhadas;
- UC031 não é antecipada;
- CI completa verde;
- backlog marca UC039 Concluído após implementação.

## Branch sugerida

~~~text
feat/uc039-administrar-empresas-solicitacoes
~~~

## Commit sugerido

~~~text
feat: adiciona administracao global de empresas
~~~
