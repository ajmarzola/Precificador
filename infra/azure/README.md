# Publicacao Azure - MEL021

Scripts reproduziveis para provisionar e publicar o Precificador em Azure App Service F1 e Azure SQL Database Free offer, sem fallback pago.

## Contrato

- App Service Plan Linux `F1`/Free.
- Web App `.NET 10`, sem container, sem custom domain e com HTTPS Only.
- Azure SQL Database `GeneralPurpose`/`Serverless` com `--use-free-limit` e `--free-limit-exhaustion-behavior AutoPause`.
- App Service e Azure SQL na mesma regiao.
- Conexao por System Assigned Managed Identity em `ConnectionStrings__Precificador`.
- SQL logical server com Microsoft Entra-only e Entra administrator explicito.
- Identidade runtime da Web App somente em `db_datareader` e `db_datawriter`.
- Production nao executa migration no startup; migrations sao etapa explicita de deploy.
- Sem App Insights, Key Vault, VNet, Private Endpoint, ACR, container ou recurso pago auxiliar.

## Pre-flight externo

Revalide no momento do provisionamento:

- App Service Free F1: $0, compute compartilhado, 60 minutos de CPU por dia, 1 GB de RAM e 1 GB de storage por app; sem SLA e nao suportado para workloads de producao.
- Azure SQL Free offer: 100.000 vCore-seconds por mes, 32 GB de dados e 32 GB de backup por banco; ate 10 bancos por assinatura.
- `AutoPause` ao esgotar o limite gratuito para evitar cobranca automatica; nao usar `BillOverUsage`.
- Com o limite gratuito, PITR ate 7 dias, backup LRS/local e sem LTR.

Fontes oficiais usadas pela MEL021:

- https://azure.microsoft.com/pricing/details/app-service/linux/
- https://learn.microsoft.com/azure/azure-sql/database/free-offer
- https://learn.microsoft.com/azure/azure-sql/database/free-offer-faq
- https://learn.microsoft.com/cli/azure/sql/db
- https://learn.microsoft.com/cli/azure/webapp
- https://learn.microsoft.com/azure/azure-sql/database/authentication-azure-ad-user-assigned-managed-identity
- https://learn.microsoft.com/sql/t-sql/statements/create-user-transact-sql

## Provisionamento

Escolha nomes unicos e uma regiao onde F1 Linux, runtime .NET 10 e Azure SQL Free estejam disponiveis.

Antes de executar qualquer script, o operador deve autenticar-se e selecionar manualmente a assinatura correta. Os scripts nao executam `az account set` e apenas aceitam o contexto ja selecionado:

```powershell
az login --tenant <TenantId>
az account set --subscription <SubscriptionId>
az account show
```

`TenantId` e `SubscriptionId` sao obrigatorios nos dois scripts. Antes de qualquer operacao mutavel, cada script executa `az account show` e interrompe a execucao se a assinatura nao estiver `Enabled`, se `tenantId` for diferente do `TenantId` informado ou se `id` for diferente do `SubscriptionId` informado. Use os identificadores reais somente como parametros locais; nao os registre no repositorio ou na PR.

```powershell
.\infra\azure\provision.ps1 `
  -ResourceGroupName rg-precificador-piloto `
  -Location brazilsouth `
  -AppName app-precificador-piloto `
  -SqlServerName sql-precificador-piloto `
  -SqlDatabaseName precificador `
  -TenantId <TenantId> `
  -SubscriptionId <SubscriptionId> `
  -EntraAdminName usuario@dominio.com `
  -EntraAdminObjectId 00000000-0000-0000-0000-000000000000 `
  -EntraAdminPrincipalType User
```

O script:

- valida, em modo fail-closed, o login, a assinatura `Enabled`, o tenant e a subscription ja selecionados pelo operador;
- descobre o runtime Linux .NET 10 com `az webapp list-runtimes --os linux`;
- valida F1, Azure SQL Free e `AutoPause` antes de criar recursos;
- cria/usa Resource Group, App Service Plan F1, Web App, SQL logical server Entra-only e database Free;
- configura `ConnectionStrings__Precificador` sem senha;
- cria o usuario da Managed Identity no banco usando SID/Object ID e concede somente `db_datareader` + `db_datawriter`;
- cria regras de firewall para `outboundIpAddresses` e `possibleOutboundIpAddresses`;
- abre e remove uma regra temporaria para o IP do operador durante o bootstrap.

Se qualquer pre-condicao gratuita ou passwordless falhar, o script interrompe. Ele nao troca para B1/S1, SQL pago, senha SQL ou `BillOverUsage`.

## Deploy

```powershell
.\infra\azure\deploy.ps1 `
  -ResourceGroupName rg-precificador-piloto `
  -AppName app-precificador-piloto `
  -SqlServerName sql-precificador-piloto `
  -SqlDatabaseName precificador `
  -TenantId <TenantId> `
  -SubscriptionId <SubscriptionId>
```

O deploy:

1. valida em modo fail-closed login, assinatura `Enabled`, tenant e subscription ja selecionados, alem de F1, SQL Free + AutoPause, HTTPS Only e connection string passwordless com `Connection Timeout=60` ou superior;
2. executa `dotnet tool restore`;
3. executa restore, build Release e testes Release;
4. abre regra temporaria para o IP do operador;
5. aplica migrations explicitamente com `Authentication=Active Directory Default`;
6. confirma ausencia de migrations pendentes;
7. remove o IP temporario;
8. publica Release e faz deploy zip;
9. executa smoke HTTPS.

## Azure SQL Serverless pausado

O Azure SQL Serverless pode aparecer como `Paused` apos inatividade. Esse e o comportamento esperado de pausa do compute serverless: a primeira conexao faz o auto-resume e pode demorar mais. Por isso, a connection string da Web App por Managed Identity, a conexao de bootstrap e a conexao Entra usada pelas migrations usam `Connection Timeout=60`; o EF Core tambem preserva `EnableRetryOnFailure()` para falhas transitorias. Durante o bootstrap, `provision.ps1` tambem tenta novamente a abertura da conexao ate cinco vezes quando o Azure SQL retorna o erro transitorio `40613`, com backoff e descarte da conexao de cada tentativa.

Isso e diferente de `AutoPause` por esgotamento da franquia Azure SQL Free. A pausa por inatividade e reversivel quando chega uma nova conexao. Ja o `AutoPause` da franquia gratuita impede o uso ate o inicio do proximo mes, para evitar cobranca por excedente. Nao alterar esse comportamento para continuidade paga.

## Smoke manual obrigatorio

Apos o deploy, validar no Azure real:

- HTTPS responde em `https://<app>.azurewebsites.net`;
- Login sem SystemAdmin redireciona para Setup, com `Bootstrap__SystemAdminKey` configurada;
- Setup cria somente o primeiro SystemAdmin; seu login encaminha para `/Admin`;
- solicitacao publica aprovada cria Empresa, configuracao padrao e vinculo Administrador;
- Administrador com identidade nova define senha pelo link de ativacao enviado com SMTP configurado;
- login do Administrador resolve Empresa Ativa ou permite selecao quando ha multiplas Empresas elegiveis;
- cadastro e consulta de Insumo persistem;
- cadastro e consulta de Produto persistem;
- apos remover o IP temporario do operador, a Web App continua acessando o Azure SQL pela Managed Identity.

## Evidencias para PR

Anexe ao handoff:

- assinatura ativa e regiao usada;
- runtime .NET 10 descoberto pela CLI;
- App Service Plan `F1`;
- Web App e Azure SQL na mesma regiao;
- Azure SQL com `useFreeLimit=true` e `freeLimitExhaustionBehavior=AutoPause`;
- Entra-only e Entra admin configurados;
- Managed Identity com somente `db_datareader` + `db_datawriter`;
- `ConnectionStrings__Precificador` sem senha;
- migrations aplicadas e zero pendencias;
- smoke manual completo;
- confirmacao de que nenhum recurso pago auxiliar foi criado.
