# UC040 — Ativar conta e recuperar acesso

- **Área funcional:** Administração multiempresa e acesso controlado
- **Dependências funcionais:** FT003
- **Gate operacional:** UC038 concluída
- **Estado:** Pronto
- **Alteração de schema:** sim
- **Persistência de token:** não
- **Persistência adicional:** key ring do ASP.NET Core Data Protection
- **Superfícies principais:** Conta / ativação / recuperação
- **Regras principais:** RN062, RN063 e RN064

## Objetivo

Disponibilizar a infraestrutura e os fluxos necessários para que o próprio usuário:

1. defina sua senha inicial por um link de ativação enviado ao seu e-mail;
2. recupere o acesso quando esquecer a senha.

A UC040 também entrega a infraestrutura de e-mail e de tokens que será reutilizada pela UC039 e pela UC031.

Fluxos conceituais:

~~~text
[UC039 futura]
aprova solicitação
   ↓
cria/reutiliza UsuarioAplicacao
   ↓
se novo usuário sem senha
   ↓
UC040 envia ativação
   ↓
usuário abre link
   ↓
define a própria senha
   ↓
login normal
~~~

~~~text
Login
   ↓
Esqueci minha senha
   ↓
e-mail informado
   ↓
resposta pública neutra
   ↓
link de recuperação
   ↓
usuário define nova senha
   ↓
login normal
~~~

## Princípios

- administradores não conhecem nem escolhem a senha do usuário;
- não existe senha temporária enviada por e-mail;
- ativação e recuperação são concluídas pelo próprio titular do e-mail;
- tokens são temporários, protegidos e não persistidos em texto;
- links continuam válidos através de restart/cold start quando ainda estiverem dentro da validade;
- usuário existente com senha não é reativado nem tem senha alterada para entrar em outra Empresa;
- recuperação de senha não revela publicamente se o e-mail existe.

## Relação com UC039 e UC031

A UC040 não cria Empresa, vínculo ou usuário por iniciativa própria.

Ela fornece o contrato para os fluxos futuros:

### UC039

Ao aprovar uma solicitação:

- se o e-mail ainda não existir no Identity:
  - criar UsuarioAplicacao sem senha;
  - criar o vínculo administrativo correspondente;
  - solicitar à UC040 o envio do link de ativação;
- se o e-mail já existir e possuir senha:
  - reutilizar a identidade;
  - não redefinir senha;
  - a notificação de novo acesso poderá ser tratada pela UC039.

### UC031

Ao convidar usuário adicional:

- identidade nova sem senha poderá usar o mesmo fluxo de ativação;
- identidade já existente com senha mantém sua credencial global;
- nenhum Administrador da Empresa escolhe ou reseta senha.

A UC040 não implementa os fluxos administrativos acima.

## Estado da identidade

Não adicionar senha temporária, flag “PrecisaTrocarSenha” ou campo equivalente.

A identidade nova ainda não ativada é representada por:

~~~text
UsuarioAplicacao existente
+
sem password hash utilizável
~~~

Ela não consegue autenticar pelo Login atual, pois CheckPasswordAsync falha.

A ativação válida adiciona a primeira senha.

Não usar a ausência de UsuarioEmpresa como indicador de ativação.

## E-mail confirmado

Uma ativação bem-sucedida comprova acesso ao endereço para o qual o link foi enviado.

Após definir a senha inicial, marcar:

~~~text
EmailConfirmed = true
~~~

Recuperação de senha não altera o e-mail cadastrado.

Não habilitar nesta UC:

~~~text
SignInOptions.RequireConfirmedEmail = true
~~~

Essa mudança global exigiria migração/estratégia própria para usuários legados e não é necessária para o fluxo atual.

## Token providers

Usar ASP.NET Core Identity + Data Protection.

Não implementar token aleatório próprio.

Configurar dois providers com nomes e opções independentes.

### Ativação

Nome conceitual:

~~~text
ContaAtivacao
~~~

Purpose conceitual:

~~~text
Precificador.AtivarConta
~~~

Validade:

~~~text
48 horas
~~~

Uso:

~~~text
GenerateUserTokenAsync
VerifyUserTokenAsync
~~~

O provider de ativação deve usar opções próprias derivadas de DataProtectionTokenProviderOptions para que sua validade não altere outros tokens Identity.

### Recuperação de senha

Nome conceitual:

~~~text
ContaRecuperacaoSenha
~~~

Validade:

~~~text
1 hora
~~~

Configurar esse provider como:

~~~text
IdentityOptions.Tokens.PasswordResetTokenProvider
~~~

Usar as APIs Identity:

~~~text
GeneratePasswordResetTokenAsync
ResetPasswordAsync
~~~

Não reutilizar o token de ativação como token de recuperação.

## Uso único e invalidação

Tokens não possuem linha própria no banco.

A validade depende do Data Protection payload, purpose, usuário e security stamp.

Após ativação ou redefinição de senha bem-sucedida:

- atualizar/inutilizar o security stamp conforme necessário;
- o mesmo token não pode ser aceito novamente;
- tokens concorrentes/outstanding do mesmo usuário deixam de ser utilizáveis após a mudança de credencial.

A suíte deve provar reuso inválido.

Reemitir um link antes da utilização não precisa invalidar imediatamente todos os links anteriores; o primeiro fluxo concluído com sucesso deve invalidar os demais pela alteração de security stamp.

## Codificação do token na URL

Nunca colocar o token Identity bruto diretamente na query string.

Codificar:

~~~text
UTF-8
-> Base64Url
~~~

Na leitura:

~~~text
Base64Url
-> UTF-8
-> token Identity
~~~

Parâmetros mínimos:

~~~text
userId
code
~~~

Não persistir o code.

Não incluir e-mail na query quando userId for suficiente.

## Data Protection persistente

A aplicação publicada usa App Service Free, sujeito a cold start/restart.

O key ring do Data Protection não pode depender somente de armazenamento efêmero/local da instância.

Persistir as chaves no mesmo Azure SQL/SQL Server do Precificador.

Adicionar pacote compatível com .NET 10:

~~~text
Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
~~~

E evoluir PrecificadorDbContext para implementar:

~~~text
IDataProtectionKeyContext
~~~

com DbSet:

~~~text
DataProtectionKeys
~~~

Configurar:

~~~text
AddDataProtection()
.SetApplicationName("Precificador")
.PersistKeysToDbContext<PrecificadorDbContext>()
~~~

A tabela é global:

- sem EmpresaId;
- sem Global Query Filter;
- não contém tokens de ativação/recuperação;
- contém material do key ring conforme formato do framework.

Criar migration.

Não editar migrations históricas.

## Segurança das chaves

A UC040 garante persistência e estabilidade do key ring, não cria um segundo mecanismo de criptografia de chave.

Não escrever XML/chaves em log.

Não expor DataProtectionKeys na UI.

Não criar CRUD para a tabela.

Uma futura exigência de key escrow/HSM/Key Vault deverá ser MEL/ADR própria.

## Transporte de e-mail

Criar abstração própria e pequena de envio de e-mail para desacoplar regras de conta de SMTP.

Nome indicativo:

~~~text
IEmailSenderAplicacao
~~~

ou equivalente.

Implementação de produção:

~~~text
SMTP via MailKit
~~~

Não usar System.Net.Mail.SmtpClient como nova implementação.

Não acoplar a UC a um fornecedor específico de e-mail.

O servidor SMTP poderá ser escolhido/configurado operacionalmente sem mudar o domínio.

## Configuração SMTP

Configuração conceitual:

~~~text
Email:Smtp:Enabled
Email:Smtp:Host
Email:Smtp:Port
Email:Smtp:Security
Email:Smtp:UserName
Email:Smtp:Password
Email:Smtp:FromAddress
Email:Smtp:FromName
Aplicacao:UrlPublica
~~~

Environment variables equivalentes:

~~~text
Email__Smtp__Enabled
Email__Smtp__Host
Email__Smtp__Port
Email__Smtp__Security
Email__Smtp__UserName
Email__Smtp__Password
Email__Smtp__FromAddress
Email__Smtp__FromName
Aplicacao__UrlPublica
~~~

### Enabled

~~~text
false
~~~

significa que o transporte está deliberadamente desabilitado.

A aplicação continua iniciando e as funções normais do Precificador continuam disponíveis.

O fluxo público de solicitar recuperação deve responder indisponibilidade antes de consultar o e-mail informado.

### Enabled = true

Validar configuração no startup.

Exigir no mínimo:

- Host não vazio;
- Port 1..65535;
- Security com valor suportado;
- FromAddress válido;
- UrlPublica absoluta;
- UrlPublica HTTPS em Production.

Se autenticação SMTP for usada:

~~~text
UserName + Password
~~~

devem ser fornecidos de forma consistente.

Não versionar senha.

No Azure, usar App Service Application Settings.

Em desenvolvimento local, usar user-secrets ou variável de ambiente.

## Modos de segurança SMTP

Suportar configuração explícita, sem fallback silencioso inseguro.

Valores mínimos:

~~~text
StartTls
SslOnConnect
None
~~~

Para Production, `None` não deve ser aceito por padrão.

É aceitável permitir `None` apenas em Development/Test para servidor SMTP local.

Não desabilitar validação de certificado TLS.

## URL pública

Links enviados por e-mail devem partir de:

~~~text
Aplicacao:UrlPublica
~~~

Não construir links de produção diretamente a partir do header Host do request.

Motivo: o link enviado deve possuir origem pública canônica e não depender de host informado pelo cliente.

Normalizar barra final.

Exemplo conceitual:

~~~text
https://app.exemplo/Conta/Ativar?userId=...&code=...
~~~

## Conteúdo dos e-mails

### Ativação

Assunto sugerido:

~~~text
Ative seu acesso ao Precificador
~~~

Corpo mínimo:

- informar que foi criado/liberado acesso ao Precificador;
- link para definir a senha;
- informar que o link expira em 48 horas;
- dizer que, se o destinatário não esperava o e-mail, pode ignorá-lo.

Não enviar senha.

### Recuperação

Assunto sugerido:

~~~text
Redefina sua senha do Precificador
~~~

Corpo mínimo:

- informar que foi solicitada recuperação;
- link para definir nova senha;
- informar que o link expira em 1 hora;
- dizer que, se não reconhece a solicitação, pode ignorá-la.

Não incluir a senha atual.

## Logs

É permitido registrar eventos operacionais como:

~~~text
envio de e-mail falhou
configuração SMTP indisponível
token inválido em tentativa de ativação
~~~

Não registrar:

- token;
- URL completa contendo token;
- senha;
- corpo do e-mail;
- e-mail completo;
- SMTP password.

Preferir identificadores técnicos não sensíveis quando necessário.

## Serviço de ativação

Criar serviço reutilizável para futuros fluxos administrativos.

Contrato conceitual:

~~~text
EnviarAtivacaoAsync(UsuarioAplicacao usuario)
~~~

O resultado deve permitir distinguir para o chamador interno:

~~~text
Enviado
NaoNecessario
Indisponivel/Falhou
~~~

ou semântica equivalente.

### Quando enviar

Enviar somente se:

- usuário possui e-mail;
- usuário ainda não possui senha;
- SMTP está habilitado/configurado.

### Usuário já possui senha

Resultado:

~~~text
NaoNecessario
~~~

Não gerar token.

Não enviar link de redefinição.

Não alterar password hash.

Essa regra é crítica para identidades que futuramente entrarem em uma segunda Empresa.

### Falha de transporte

Não alterar credencial/usuário.

Retornar falha ao chamador interno.

UC039 será responsável por decidir como apresentar/repetir a falha administrativa.

## Página /Conta/Ativar

Rotas conceituais:

~~~text
GET  /Conta/Ativar?userId=...&code=...
POST /Conta/Ativar
~~~

### GET válido

1. decodificar code;
2. localizar usuário;
3. confirmar que ele ainda não possui senha;
4. validar token de ativação;
5. renderizar formulário.

Campos visíveis:

- Nova senha;
- Confirmação.

Não exibir token.

userId/code podem seguir em campos hidden.

### GET inválido

Casos:

- userId ausente;
- code ausente/malformado;
- usuário não existe;
- token inválido;
- token expirado;
- token já usado;
- usuário já possui senha.

Exibir estado genérico:

~~~text
Este link de ativação é inválido ou expirou.
~~~

Não explicar qual condição ocorreu.

### POST válido

1. antiforgery;
2. validar campos;
3. localizar usuário;
4. decodificar e validar token novamente;
5. confirmar que o usuário ainda não possui senha;
6. adicionar senha por UserManager respeitando a política atual;
7. marcar EmailConfirmed = true;
8. garantir mudança de security stamp;
9. persistir de forma transacional quando necessário;
10. redirecionar para Login.

Mensagem sugerida:

~~~text
Conta ativada. Você já pode entrar com sua senha.
~~~

Não autenticar automaticamente.

### Política de senha

Reutilizar integralmente as opções Identity existentes.

Não duplicar regex/política no PageModel.

Erros de password validators devem ser apresentados ao usuário de forma compreensível.

## Reenvio de ativação

A UC040 não cria uma página administrativa de “reenviar convite”.

Porém o fluxo público “Esqueci minha senha” deve ajudar o usuário que possui identidade aprovada mas ainda não ativada:

~~~text
usuário existe
+
não possui senha
=> enviar novo link de ativação
~~~

A resposta pública continua neutra e não informa que a conta aguardava ativação.

Isso fornece recuperação natural para link de ativação expirado.

## Página /Conta/EsqueciSenha

Adicionar link no Login:

~~~text
Esqueci minha senha
~~~

Rotas conceituais:

~~~text
GET  /Conta/EsqueciSenha
POST /Conta/EsqueciSenha
GET  /Conta/EsqueciSenhaConfirmacao
~~~

### GET

Exibir:

- campo E-mail;
- botão para solicitar recuperação;
- link de volta ao Login.

Usuário autenticado deve ser redirecionado para seu destino canônico, sem disparar recuperação.

### SMTP desabilitado

Antes de consultar Identity:

~~~text
Email:Smtp:Enabled == false
=> 503 Service Unavailable
~~~

Mensagem genérica:

~~~text
A recuperação de acesso está temporariamente indisponível.
~~~

Isso evita comportamento diferente conforme existência do usuário.

### POST válido

1. validar antiforgery;
2. validar formato do e-mail;
3. verificar previamente disponibilidade/configuração do transporte;
4. localizar usuário por e-mail;
5. se não existir, não enviar;
6. se existir e possuir senha, gerar token de recuperação e tentar enviar;
7. se existir e não possuir senha, tentar enviar ativação;
8. independentemente da existência, redirecionar para confirmação pública.

Confirmação:

~~~text
Se existir uma conta para esse e-mail, enviaremos as instruções de acesso.
~~~

Não dizer:

- conta não encontrada;
- conta encontrada;
- aguardando ativação;
- e-mail já confirmado;
- usuário pertence a Empresa X.

### Falha transitória de SMTP durante envio

Como a tentativa só ocorre para usuário existente, retornar erro HTTP apenas nesse caso criaria enumeração.

Portanto:

- registrar falha operacional sem PII/token;
- manter a mesma resposta pública neutra de confirmação;
- não revelar a falha ao visitante.

A indisponibilidade configuracional conhecida antes da busca continua usando 503 comum para todos.

## Página /Conta/RedefinirSenha

Rotas conceituais:

~~~text
GET  /Conta/RedefinirSenha?userId=...&code=...
POST /Conta/RedefinirSenha
~~~

### GET

Validar:

- userId;
- token decodificável;
- usuário;
- token de recuperação;
- usuário possui senha.

Token inválido/expirado/usado:

~~~text
Este link de recuperação é inválido ou expirou.
~~~

### POST válido

1. antiforgery;
2. validar Nova senha + Confirmação;
3. decodificar token;
4. localizar usuário;
5. ResetPasswordAsync;
6. garantir invalidação/reuso impossível;
7. redirecionar para Login.

Mensagem:

~~~text
Senha redefinida. Você já pode entrar.
~~~

Não autenticar automaticamente.

## Usuário sem senha em /RedefinirSenha

Token de recuperação não deve ser emitido para identidade ainda não ativada.

Se uma URL de recuperação for manipulada para esse usuário:

- rejeitar;
- não usar ResetPassword como atalho de ativação.

Ativação usa provider/purpose próprio.

## Login

Adicionar somente o link:

~~~text
Esqueci minha senha
~~~

Não alterar o restante da lógica de login/Empresa Ativa/SystemAdmin.

Não adicionar “primeiro acesso” com senha temporária.

## Canonical redirect de autenticados

As páginas públicas de conta desta UC são destinadas a usuário não autenticado.

Se autenticado:

- SystemAdmin -> /Admin;
- usuário com Empresa Ativa -> /Dashboard;
- usuário empresarial sem Empresa Ativa -> /Empresas/Selecionar.

Preservar a função auxiliar/código comum se for útil, sem criar framework de navegação.

## Não enumerar contas

As superfícies públicas devem evitar diferenças textuais observáveis entre:

- e-mail inexistente;
- usuário ativado;
- usuário ainda sem senha.

Em /EsqueciSenha, a confirmação é a mesma.

GET de Ativar/Redefinir recebe um token secreto enviado por e-mail; nesse contexto é aceitável informar apenas “inválido ou expirado”, sem detalhar usuário/estado.

## Proteção CSRF

POSTs exigem antiforgery:

- Ativar;
- EsqueciSenha;
- RedefinirSenha.

Não desabilitar antiforgery para facilitar teste.

## E-mail/token e HTML

Links em e-mail devem ser HTML-encoded quando usados em corpo HTML.

Se enviar também corpo text/plain, usar URL textual segura.

Não usar entrada do usuário como HTML sem encoding.

## Schema

A alteração de banco esperada é somente a persistência do key ring de Data Protection.

Não criar tabelas de:

- token;
- convite;
- recuperação;
- e-mail enviado;
- senha temporária.

A migration deve criar a tabela exigida pelo provider EF do Data Protection.

Não editar migration da UC038/FT003.

## Compatibilidade com usuários existentes

### SystemAdmin criado pelo Setup

Possui senha.

Pode usar “Esqueci minha senha”.

Não precisa de ativação.

### Usuário empresarial legado

Se possui senha:

- pode recuperar senha;
- não sofre alteração automática;
- não precisa de ativação apenas por existir antes da UC040.

### Usuário futuro criado sem senha

Não consegue logar até ativar.

Pode receber novo link de ativação pelo serviço interno ou pela recuperação pública.

## Referências externas verificadas

A especificação adota mecanismos oficiais do ASP.NET Core:

- Data Protection token providers possuem TokenLifespan configurável;
- Password Reset usa GeneratePasswordResetTokenAsync/ResetPasswordAsync;
- Data Protection possui provider oficial para persistir key ring via EF Core;
- System.Net.Mail.SmtpClient não é recomendado para novo desenvolvimento; usar MailKit ou biblioteca moderna equivalente.

A implementação não deve substituir essas primitivas por criptografia própria.

## Regras de negócio

### RN062 — Ativação sem senha temporária

Uma identidade nova que precise de credencial recebe um link de ativação.

O próprio usuário define a primeira senha.

Administrador/SystemAdmin não escolhe, conhece nem recebe a senha.

Usuário que já possui senha não entra no fluxo de ativação e sua senha nunca é redefinida para novo vínculo empresarial.

### RN063 — Recuperação de acesso não enumera contas

O fluxo público de “Esqueci minha senha” responde de forma neutra, sem revelar se o e-mail existe nem o estado de ativação.

Usuário ativado recebe link de recuperação.

Identidade existente sem senha recebe novo link de ativação.

Senha nova é sempre definida pelo próprio usuário.

### RN064 — Tokens de conta são temporários e não persistidos

Ativação e recuperação usam providers Identity/Data Protection separados.

Validades:

~~~text
Ativação = 48 horas
Recuperação = 1 hora
~~~

Tokens:

- não são persistidos no banco;
- são enviados apenas dentro de link;
- tornam-se inválidos após uso bem-sucedido/security stamp;
- dependem de key ring persistente para sobreviver a restart dentro da validade.

## Critérios de aceitação

- **CA01:** Login exibe link “Esqueci minha senha”.
- **CA02:** /Conta/EsqueciSenha é acessível anonimamente.
- **CA03:** recuperação exige e-mail em formato válido.
- **CA04:** confirmação pública não revela se o usuário existe.
- **CA05:** e-mail inexistente recebe a mesma confirmação pública.
- **CA06:** usuário com senha recebe link de recuperação quando SMTP está disponível.
- **CA07:** usuário sem senha recebe link de ativação ao solicitar recuperação.
- **CA08:** recuperação pública não altera usuário antes do link ser usado.
- **CA09:** SMTP desabilitado gera 503 antes da busca de usuário.
- **CA10:** falha transitória de envio não revela existência do usuário.
- **CA11:** token de ativação usa provider/purpose próprio.
- **CA12:** token de recuperação usa provider próprio configurado como PasswordResetTokenProvider.
- **CA13:** ativação possui validade de 48 horas.
- **CA14:** recuperação possui validade de 1 hora.
- **CA15:** tokens não são persistidos.
- **CA16:** tokens são codificados em Base64Url nos links.
- **CA17:** link de produção usa Aplicacao:UrlPublica, não Host do request.
- **CA18:** UrlPublica é HTTPS obrigatória em Production.
- **CA19:** SMTP é desacoplado por abstração.
- **CA20:** implementação SMTP usa MailKit ou biblioteca moderna equivalente, não System.Net.Mail.SmtpClient.
- **CA21:** senha SMTP não é versionada.
- **CA22:** Production não permite SMTP Security=None.
- **CA23:** /Conta/Ativar válido exibe somente Nova senha/Confirmação.
- **CA24:** ativação inválida/expirada/usada mostra mensagem genérica.
- **CA25:** ativação válida respeita política Identity atual.
- **CA26:** ativação adiciona a primeira senha.
- **CA27:** ativação marca EmailConfirmed.
- **CA28:** ativação não cria Empresa nem UsuarioEmpresa.
- **CA29:** ativação não autentica automaticamente.
- **CA30:** token de ativação não pode ser reutilizado após sucesso.
- **CA31:** usuário já com senha não pode usar ativação para trocar senha.
- **CA32:** serviço de ativação retorna NaoNecessario para usuário com senha.
- **CA33:** serviço de ativação não altera usuário se envio falhar.
- **CA34:** /Conta/RedefinirSenha valida token antes de permitir alteração.
- **CA35:** redefinição válida usa ResetPasswordAsync.
- **CA36:** redefinição respeita política Identity atual.
- **CA37:** token de recuperação não pode ser reutilizado após sucesso.
- **CA38:** recuperação não autentica automaticamente.
- **CA39:** usuário sem senha não usa recuperação como atalho de ativação.
- **CA40:** POSTs da UC040 exigem antiforgery.
- **CA41:** usuários autenticados são desviados para destino canônico.
- **CA42:** Data Protection possui ApplicationName estável.
- **CA43:** key ring é persistido no SQL Server/Azure SQL.
- **CA44:** DataProtectionKeys não possui EmpresaId nem Global Query Filter.
- **CA45:** token gerado antes de reinicialização continua validável em nova instância com o mesmo banco/key ring.
- **CA46:** key ring não é exposto em UI/log.
- **CA47:** migration funciona em banco vazio.
- **CA48:** migration funciona sobre banco pós-UC038 sem perda.
- **CA49:** migrations históricas não são editadas.
- **CA50:** UC040 não cria fluxo administrativo de aprovação/invite.
- **CA51:** UC040 não cria Empresa.
- **CA52:** UC040 não cria UsuarioEmpresa.
- **CA53:** UC040 não introduz senha temporária.
- **CA54:** build Release fica verde.
- **CA55:** unitários aplicáveis ficam verdes.
- **CA56:** integração SQL Server/Web fica verde.

## Matriz mínima de testes

### Configuração/token providers

- provider Ativacao registrado;
- lifespan Ativacao = 48h;
- provider Recuperacao registrado;
- PasswordResetTokenProvider aponta para Recuperacao;
- lifespan Recuperacao = 1h;
- ApplicationName do Data Protection é estável;
- tokens de providers diferentes não são intercambiáveis;
- purpose incorreto invalida token.

Não criar teste com sleep longo para provar expiração. Validar as opções configuradas e comportamento do framework com token inválido/reuso.

### Data Protection / migration

- banco vazio aplica migration;
- upgrade pós-UC038 preserva Empresas, Identity e solicitações;
- DataProtectionKeys existe;
- não possui EmpresaId/GQF;
- uma instância gera token;
- uma segunda instância/processo de teste usando o mesmo banco/key ring valida o token;
- banco diferente/key ring diferente não valida o mesmo token, quando viável sem teste frágil.

### SMTP/configuração

- Enabled=false => transporte indisponível;
- Forgot Password retorna 503 antes de consultar conta;
- Enabled=true com configuração incompleta => validação de startup falha;
- Production + Security=None => configuração inválida;
- Development/Test pode permitir None;
- senha/segredos não aparecem em config versionada/log;
- fake sender de teste captura destinatário/assunto/link sem rede real.

### Serviço de ativação

- usuário novo sem senha => token + e-mail de ativação;
- usuário com senha => NaoNecessario, zero e-mail;
- usuário sem e-mail => não envia/resultado inválido;
- SMTP indisponível => resultado de falha, usuário inalterado;
- link usa UrlPublica configurada;
- link usa Base64Url;
- assunto/corpo não contêm senha.

### Ativação Web

- GET válido => formulário;
- GET userId ausente => inválido/expirado;
- GET code ausente => inválido/expirado;
- token malformado => inválido/expirado;
- usuário inexistente => inválido/expirado;
- token de recuperação usado como ativação => rejeitado;
- usuário já com senha => rejeitado;
- POST sem antiforgery => rejeitado;
- senha inválida => exibe erros e não ativa;
- confirmação divergente => não ativa;
- POST válido => HasPassword true;
- EmailConfirmed true;
- nenhum vínculo/Empresa criado;
- redirect Login;
- token usado novamente => rejeitado.

### Esqueci minha senha

- GET anônimo => formulário;
- Login possui link;
- e-mail inválido => erro sem consulta/envio;
- e-mail inexistente => confirmação genérica;
- e-mail existente com senha => recovery e-mail;
- e-mail existente sem senha => activation e-mail;
- respostas públicas iguais nos três estados quando transporte disponível;
- falha do fake sender para usuário existente => mesma confirmação pública + log operacional;
- nenhum PasswordHash muda no POST inicial;
- usuário autenticado não dispara envio.

### Redefinir senha

- token válido => formulário;
- token inválido/malformado => inválido/expirado;
- token de ativação no reset => rejeitado;
- usuário sem password => rejeitado;
- POST sem antiforgery => rejeitado;
- senha fora da policy => não altera;
- confirmação divergente => não altera;
- POST válido => senha anterior deixa de funcionar;
- senha nova funciona;
- token reutilizado => rejeitado;
- redirect Login;
- não auto-login.

### Regressão

- Setup/SystemAdmin continua funcionando;
- Login empresarial continua resolvendo Empresa Ativa;
- UC038 continua sem enviar e-mail;
- nenhuma tabela de token/invite é criada;
- páginas tenant-owned continuam isoladas.

## Arquivos esperados

Lista indicativa:

~~~text
src/Precificador.Infrastructure/Persistence/PrecificadorDbContext.cs
src/Precificador.Infrastructure/Migrations/<migration data protection>.cs
src/Precificador.Infrastructure/...

src/Precificador.Web/Autenticacao/...
src/Precificador.Web/Email/...
src/Precificador.Web/Pages/Conta/Ativar.cshtml(.cs)
src/Precificador.Web/Pages/Conta/EsqueciSenha.cshtml(.cs)
src/Precificador.Web/Pages/Conta/EsqueciSenhaConfirmacao.cshtml(.cs)
src/Precificador.Web/Pages/Conta/RedefinirSenha.cshtml(.cs)
src/Precificador.Web/Pages/Conta/Login.cshtml
src/Precificador.Web/Program.cs
src/Precificador.Web/Precificador.Web.csproj

tests/Precificador.Tests.Integration/...
tests/Precificador.Tests.Unit/...

docs/use-cases/UC040-ativar-conta-recuperar-acesso.md
docs/business/business-rules.md
docs/architecture/...
docs/development/backlog.md
docs/use-cases/catalog.md
~~~

## Fora do escopo

- aprovar/recusar solicitação — UC039;
- criar Empresa — UC039;
- criar primeiro vínculo administrativo — UC039;
- botão administrativo de reenviar ativação — UC039, se necessário;
- convidar usuários adicionais — UC031;
- notificar usuário existente de novo vínculo — UC031/UC039;
- múltiplos SystemAdmins;
- troca de e-mail;
- alteração de senha de usuário autenticado;
- 2FA;
- autenticação externa/SSO;
- CAPTCHA;
- rate limiting distribuído;
- fila/outbox de e-mails;
- tracking de abertura;
- templates avançados;
- provider comercial específico;
- Key Vault/HSM;
- confirmação obrigatória global de e-mail;
- exclusão de conta;
- auditoria completa de segurança.

## Definition of Done

UC040 está concluída quando:

- infraestrutura SMTP configurável está implementada e testável sem rede real;
- links absolutos usam origem pública configurada;
- Data Protection key ring persiste no SQL Server/Azure SQL;
- providers de ativação e recuperação possuem validades separadas;
- ativação permite ao próprio usuário definir primeira senha;
- recuperação pública é anti-enumeração;
- identidade sem senha recebe ativação no fluxo de recuperação;
- identidade com senha recebe reset;
- senha existente nunca é substituída por fluxo de ativação;
- tokens são URL-safe, temporários, não persistidos e não reutilizáveis após sucesso;
- Login aponta para recuperação;
- nenhum fluxo administrativo da UC039/UC031 é antecipado;
- migration limpa e upgrade pós-UC038 passam;
- documentação/RN062–RN064 estão alinhadas;
- CI completa está verde;
- backlog marca UC040 como Concluído após implementação.

## Branch sugerida

~~~text
feat/uc040-ativar-recuperar-acesso
~~~

## Commit sugerido

~~~text
feat: adiciona ativacao e recuperacao de acesso
~~~
