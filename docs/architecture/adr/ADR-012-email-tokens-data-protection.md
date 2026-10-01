# ADR-012 — E-mail, tokens de conta e persistência do Data Protection

- **Status:** Aceito

## Contexto

A evolução do acesso controlado exige:

- ativar novas identidades sem senha temporária;
- recuperar senha por e-mail;
- reutilizar os mesmos mecanismos nas UCs 039 e 031;
- preservar links válidos apesar de cold start/restart do App Service;
- não acoplar o sistema a um fornecedor comercial específico de e-mail.

O Precificador já usa ASP.NET Core Identity e está publicado em App Service Linux com Azure SQL.

## Decisão

### Identity e Data Protection

Continuar usando ASP.NET Core Identity para geração e validação de tokens.

Não criar token aleatório próprio nem tabela de token.

Criar providers separados para:

~~~text
ContaAtivacao
ContaRecuperacaoSenha
~~~

com validades distintas:

~~~text
Ativação = 48 horas
Recuperação = 1 hora
~~~

### Key ring persistente

Persistir o key ring do ASP.NET Core Data Protection no mesmo SQL Server/Azure SQL da aplicação por:

~~~text
Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
PersistKeysToDbContext<PrecificadorDbContext>()
~~~

Usar ApplicationName estável:

~~~text
Precificador
~~~

A tabela de keys é global e não tenant-owned.

### Transporte de e-mail

Criar abstração de transporte SMTP.

A implementação de produção deve usar MailKit ou biblioteca moderna equivalente.

Não usar System.Net.Mail.SmtpClient para nova implementação.

O transporte não deve depender de um fornecedor específico. Host, porta, TLS e credenciais são configuração operacional.

### Origem dos links

Usar URL pública canônica configurada:

~~~text
Aplicacao:UrlPublica
~~~

Links de Production não são construídos a partir do Host header recebido.

### Segredos

Credenciais SMTP não entram no repositório.

Usar:

- user-secrets/variáveis de ambiente em desenvolvimento;
- App Service Application Settings no Azure.

### SMTP opcional

A aplicação pode iniciar com SMTP desabilitado.

Isso permite manter o Precificador operacional mesmo antes de o transporte ser configurado.

Quando desabilitado:

- envio interno informa indisponibilidade;
- recuperação pública retorna 503 antes de pesquisar e-mail.

Configuração parcialmente habilitada/inválida deve falhar na validação de opções.

### Implantação inicial suportada — Gmail SMTP

Para o MVP/piloto, uma conta Gmail dedicada é uma implantação suportada do transporte genérico SMTP.

Configuração esperada:

~~~text
Host = smtp.gmail.com
Port = 587
Security = StartTls
UserName = conta dedicada @gmail.com
Password = senha de app
FromAddress = mesma conta
FromName = Precificador
~~~

Essa opção:

- não exige domínio próprio;
- não exige recurso Azure adicional;
- não altera a arquitetura provider-agnostic;
- não autoriza código específico do Gmail.

A credencial deve ser uma senha de app separada da senha normal da conta e permanecer fora do repositório.

Se Gmail deixar de atender ao cenário operacional, trocar de SMTP não deve exigir alteração nos fluxos de ativação/recuperação.

## Consequências

### Positivas

- links não são perdidos por restart dentro da validade;
- não há senha temporária;
- token não precisa ser persistido;
- ativação e recuperação possuem validade independente;
- e-mail pode mudar de fornecedor sem alterar domínio;
- credenciais ficam fora do código;
- futuras UCs usam um serviço comum de ativação.

### Custos

- nova migration para DataProtectionKeys;
- novo pacote de Data Protection EF;
- dependência SMTP moderna;
- App Service precisa receber novas App Settings;
- suporte local de e-mail exige configuração própria;
- a implantação inicial pode usar Gmail SMTP sem domínio próprio, mantendo a possibilidade de troca posterior.

## Alternativas rejeitadas

### Token próprio persistido em tabela

Rejeitado porque duplicaria primitivas de segurança já fornecidas pelo ASP.NET Core Identity/Data Protection e aumentaria superfície de risco.

### Key ring somente no filesystem da instância

Rejeitado porque o sistema publicado está sujeito a restart/cold start e links emitidos precisam permanecer válidos dentro do prazo.

### System.Net.Mail.SmtpClient

Rejeitado para novo desenvolvimento. A documentação atual do .NET recomenda MailKit ou outra biblioteca moderna.

### Provider comercial hardcoded

Rejeitado para evitar lock-in e dependência desnecessária de preço/conta externa no domínio.

### Link baseado no Host do request

Rejeitado em Production para evitar que origem pública de links sensíveis seja influenciada pelo cliente/proxy de forma indevida.

## Relação com ADR-009 e ADR-011

A ADR-009 continua definindo ASP.NET Core Identity como mecanismo de autenticação.

A ADR-011 continua separando SystemAdmin global de perfis por Empresa.

A ADR-012 trata apenas de credenciais, tokens, e-mail e proteção criptográfica usada nesses fluxos.
