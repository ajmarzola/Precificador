# Instrução Codex — UC040 Ativar conta e recuperar acesso

Você está implementando a **UC040 — Ativar conta e recuperar acesso** no repositório `ajmarzola/Precificador`.

## Fonte normativa

Leia integralmente:

1. `AGENTS.md`;
2. `docs/use-cases/UC040-ativar-conta-recuperar-acesso.md`;
3. `docs/development/foundation-administration-authorization.md`;
4. `docs/use-cases/UC038-solicitar-acesso-precificador.md`;
5. `docs/business/business-rules.md`, especialmente RN062–RN064;
6. `docs/architecture/adr/ADR-012-email-tokens-data-protection.md`;
7. `docs/architecture/architecture.md`;
8. `docs/use-cases/catalog.md`;
9. `docs/development/testing-strategy.md`;
10. `docs/development/definition-of-done.md`;
11. implementação real de Identity, Login, Setup, PrecificadorDbContext, Azure/SQL Server e testes Web na master.

A especificação normativa é:

~~~text
docs/use-cases/UC040-ativar-conta-recuperar-acesso.md
~~~

## Branch

Use:

~~~text
feat/uc040-ativar-recuperar-acesso
~~~

Parta da master após o merge da documentação da UC040.

Não implemente na branch documental.

## Objetivo

Entregar:

- infraestrutura SMTP desacoplada;
- links absolutos por URL pública configurada;
- key ring persistente do ASP.NET Core Data Protection no SQL Server;
- token provider de ativação com 48h;
- token provider de recuperação com 1h;
- página de ativação para primeira senha;
- Esqueci minha senha;
- redefinição de senha;
- serviço interno reutilizável para envio de ativação.

Não implementar UC039 nem UC031.

## Dependências NuGet

### Data Protection EF

Adicionar pacote estável compatível com .NET 10:

~~~text
Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
~~~

Alinhar versão com os pacotes ASP.NET Core/EF existentes quando aplicável.

### SMTP

Usar MailKit ou biblioteca moderna equivalente.

Não usar:

~~~text
System.Net.Mail.SmtpClient
~~~

Não acoplar a fornecedor comercial específico.

## Data Protection

Evoluir PrecificadorDbContext para implementar IDataProtectionKeyContext e expor o DbSet requerido pelo provider.

Configurar:

~~~text
AddDataProtection()
.SetApplicationName("Precificador")
.PersistKeysToDbContext<PrecificadorDbContext>()
~~~

Criar migration.

A tabela DataProtectionKeys:

- é global;
- não possui EmpresaId;
- não recebe query filter;
- não é exposta em UI;
- não armazena tokens.

Não editar migrations históricas.

## Token providers

Crie providers separados e constantes centralizadas.

### Ativação

~~~text
provider: ContaAtivacao
purpose: Precificador.AtivarConta
lifespan: 48h
~~~

Use provider/options próprios derivados de DataProtectionTokenProvider para que a validade seja independente.

Gerar/validar via UserManager.GenerateUserTokenAsync / VerifyUserTokenAsync.

### Recuperação

~~~text
provider: ContaRecuperacaoSenha
lifespan: 1h
~~~

Configurar:

~~~text
IdentityOptions.Tokens.PasswordResetTokenProvider
~~~

Usar:

~~~text
GeneratePasswordResetTokenAsync
ResetPasswordAsync
~~~

Não use token customizado persistido.

## URL-safe code

Antes de colocar token em query:

~~~text
Encoding.UTF8.GetBytes(token)
WebEncoders.Base64UrlEncode(...)
~~~

Ao receber:

~~~text
Base64UrlDecode
Encoding.UTF8.GetString(...)
~~~

Malformed code deve virar link inválido/expirado; não exception 500.

Nunca logar code ou URL completa.

## URL pública

Links de e-mail usam:

~~~text
Aplicacao:UrlPublica
~~~

Não usar Host do request como origem de links em Production.

Validation:

- URL absoluta;
- Production => HTTPS;
- normalizar trailing slash.

## SMTP options

Contrato:

~~~text
Email:Smtp:Enabled
Email:Smtp:Host
Email:Smtp:Port
Email:Smtp:Security
Email:Smtp:UserName
Email:Smtp:Password
Email:Smtp:FromAddress
Email:Smtp:FromName
~~~

Security mínima:

~~~text
StartTls
SslOnConnect
None
~~~

Production não aceita None.

Não desligar validação TLS.

### Disabled

A aplicação deve iniciar normalmente.

EsqueciSenha POST deve retornar 503 **antes** de pesquisar usuário.

### Enabled

Validar configuração no startup.

Não versionar password.

Tests devem substituir sender por fake; não enviar e-mail real.

## Serviço de e-mail

Crie abstração pequena, por exemplo:

~~~text
IEmailSenderAplicacao
~~~

Crie serviço de conta separado do transporte para:

- gerar token;
- montar link;
- montar mensagem;
- chamar sender.

Não coloque SMTP/PageModel/UserManager tudo na mesma classe.

Evite arquitetura excessiva; duas responsabilidades separadas são suficientes.

## Serviço de ativação

Contrato interno deve permitir futuro uso por UC039/UC031.

Para usuario:

### Sem password

- gerar token ativação;
- construir link;
- enviar e-mail;
- retornar Enviado.

### Com password

- retornar NaoNecessario;
- zero token;
- zero e-mail;
- zero alteração.

### Transporte indisponível/falha

- não alterar usuário;
- informar falha ao chamador interno.

Não crie usuário/vínculo aqui.

## /Conta/Ativar

### GET

Parâmetros userId/code.

Validar:

- decode;
- usuário existe;
- não possui senha;
- token válido.

Qualquer falha => mesma mensagem:

~~~text
Este link de ativação é inválido ou expirou.
~~~

Se válido, mostrar Nova senha + Confirmação.

### POST

- antiforgery;
- validar novamente token;
- garantir que ainda não há senha;
- usar UserManager para adicionar primeira password;
- marcar EmailConfirmed = true;
- garantir security stamp alterado;
- preferir transação quando forem necessárias múltiplas escritas;
- redirect Login;
- não auto-login.

Teste reuso.

Não implemente password regex manual.

## /Conta/EsqueciSenha

Adicionar link no Login.

GET: e-mail.

POST:

1. antiforgery;
2. model validation;
3. se SMTP disabled => 503 antes de buscar usuário;
4. FindByEmail;
5. inexistente => não enviar;
6. existe + HasPassword => recuperação;
7. existe + sem password => reenvio de ativação;
8. sempre redirect para confirmação neutra quando configuração está disponível.

Mensagem:

~~~text
Se existir uma conta para esse e-mail, enviaremos as instruções de acesso.
~~~

Se sender falhar apenas para usuário existente:

- logar falha sem PII/token;
- ainda usar confirmação neutra;
- não retornar status diferente e criar enumeração.

## /Conta/RedefinirSenha

GET userId/code:

- decode;
- usuário existe;
- possui password;
- token reset válido.

Falha => mensagem genérica inválido/expirado.

POST:

- antiforgery;
- validar token novamente;
- ResetPasswordAsync;
- senha + confirmação;
- redirect Login;
- não auto-login;
- token usado novamente deve falhar.

Usuário sem password não usa reset como ativação.

## Usuários autenticados

Em páginas públicas desta UC:

- SystemAdmin -> /Admin;
- usuário com Empresa Ativa -> /Dashboard;
- usuário empresarial sem Empresa -> /Empresas/Selecionar.

Não disparar e-mail/token para autenticado.

## E-mail content

Ativação:

- assunto claro;
- link;
- validade 48h;
- nenhum password.

Recuperação:

- assunto claro;
- link;
- validade 1h;
- nenhum password atual.

HTML encode.

Pode enviar HTML + text/plain; não requer template engine.

## Segurança

Não logar:

- password;
- SMTP password;
- token;
- URL completa;
- corpo;
- e-mail completo.

Não persistir:

- token;
- senha temporária;
- convite;
- mensagem enviada.

Não criar custom crypto.

Não alterar SignInOptions.RequireConfirmedEmail.

## Testes obrigatórios

Atenda integralmente à matriz da UC040.

Pontos críticos:

- options de 48h/1h;
- providers não intercambiáveis;
- DataProtectionKeys migration;
- token validado por nova instância usando mesmo banco;
- SMTP disabled -> 503 antes da busca;
- fake sender;
- ativação sem senha;
- usuário com senha => NaoNecessario;
- activation code inválido/malformed;
- activation reuso;
- EmailConfirmed;
- recovery anti-enumeration;
- usuário sem senha recebe ativação em Forgot Password;
- sender failure não enumera;
- reset muda password e invalida token;
- activation token não funciona no reset e vice-versa;
- antiforgery;
- canonical redirect autenticado;
- regressão Setup/Login/UC038.

Não faça sleeps longos para testar expiração; valide lifespan configurado e o provider.

## Restrições de escopo

Não implementar:

- aprovação/recusa;
- Empresa/vínculo;
- convite administrativo;
- notificação de novo vínculo para usuário existente;
- troca de e-mail;
- change password autenticado;
- 2FA/SSO;
- outbox/fila;
- CAPTCHA;
- rate limit distribuído;
- Key Vault/HSM;
- provider comercial específico.

## Documentação ao concluir

- UC040 -> Concluído;
- backlog -> Concluído;
- UC039/UC031 permanecem Planejado;
- RN062–RN064 coerentes;
- ADR-012 preservada;
- documentar nomes das App Settings necessárias sem valores secretos.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
~~~

Validar migration em banco vazio e upgrade pós-UC038.

Commit sugerido:

~~~text
feat: adiciona ativacao e recuperacao de acesso
~~~

Não faça merge em master.
