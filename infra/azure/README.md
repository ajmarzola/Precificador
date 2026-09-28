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

```powershell
.\infra\azure\provision.ps1 `
  -ResourceGroupName rg-precificador-piloto `
  -Location brazilsouth `
  -AppName app-precificador-piloto `
  -SqlServerName sql-precificador-piloto `
  -SqlDatabaseName precificador `
  -EntraAdminName usuario@dominio.com `
  -EntraAdminObjectId 00000000-0000-0000-0000-000000000000 `
  -EntraAdminPrincipalType User
```

O script:

- valida login e assinatura ativa;
- descobre o runtime Linux .NET 10 com `az webapp list-runtimes --os linux --runtime dotnet`;
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
  -SqlDatabaseName precificador
```

O deploy:

1. valida login, F1, SQL Free + AutoPause, HTTPS Only e connection string passwordless;
2. executa `dotnet tool restore`;
3. executa restore, build Release e testes Release;
4. abre regra temporaria para o IP do operador;
5. aplica migrations explicitamente com `Authentication=Active Directory Default`;
6. confirma ausencia de migrations pendentes;
7. remove o IP temporario;
8. publica Release e faz deploy zip;
9. executa smoke HTTPS.

## Smoke manual obrigatorio

Apos o deploy, validar no Azure real:

- HTTPS responde em `https://<app>.azurewebsites.net`;
- banco sem usuarios redireciona para Setup;
- Setup cria o primeiro usuario e empresa;
- login funciona;
- Empresa Ativa e resolvida;
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
