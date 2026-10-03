# Publica??o Azure ? MEL021 / MEL027

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

`TenantId` e `SubscriptionId` sao obrigatorios em provision.ps1, deploy.ps1 e configure-cd.ps1. Antes de qualquer operacao mutavel, cada script executa `az account show` e interrompe a execucao se a assinatura nao estiver `Enabled`, se `tenantId` for diferente do `TenantId` informado ou se `id` for diferente do `SubscriptionId` informado. Use os identificadores reais somente como parametros locais; nao os registre no repositorio ou na PR.

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

O fallback manual requer PowerShell 7 e Azure CLI. Executa o gate completo, gera ZIP port?vel e migration bundle do build Release, aplica o bundle explicitamente, confirma remo??o da regra tempor?ria e publica. O smoke valida `/` e `/Conta/Login` sem autentica??o, com retry limitado.

## Configura??o ?nica do Continuous Deployment ? MEL027

Pr?-requisitos administrativos: contexto Azure explicitamente selecionado, permiss?o de criar UAMI/federa??o e atribuir RBAC nos tr?s recursos espec?ficos; acesso SQL como Entra administrator. O script n?o troca tenant/subscription, n?o cria plano/banco e n?o configura o GitHub.

Em PowerShell 7, disponibilize o m?dulo administrativo `SqlServer` (por exemplo, `Install-Module SqlServer -Scope CurrentUser`) e execute com os nomes reais do piloto:

```powershell
./infra/azure/configure-cd.ps1 `
  -ResourceGroupName <ResourceGroup> `
  -AppName <WebApp> `
  -AppServicePlanName <Plano> `
  -SqlServerName <SqlServer> `
  -SqlDatabaseName <Database> `
  -TenantId <TenantId> `
  -SubscriptionId <SubscriptionId>
```

O bootstrap ? idempotente: cria/localiza `mi-precificador-cd`, valida confian?a GitHub restrita ao repo/environment production, descobre as roles built-in e confirma as atribui??es. Concede Website Contributor na Web App, SQL Security Manager no SQL logical server e Reader apenas no plano (leitura F1). Recusa atribui??es/federa??es divergentes; n?o usa Owner/Contributor amplo. Cria usu?rio SQL pelo SID do **client ID** com `db_ddladmin`, `db_datareader` e `db_datawriter`, sem promover runtime. Um SID divergente exige revis?o administrativa, sem apagar usu?rios automaticamente.

No GitHub, em Settings ? Environments:

1. Criar `production` sem required reviewers/espera obrigat?ria por execu??o.
2. Em Deployment branches and tags, selecionar branches espec?ficas e permitir somente a branch `master`, sem regra para tags.
3. Registrar como **environment variables** os oito identificadores impressos pelo bootstrap: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP`, `AZURE_WEBAPP_NAME`, `AZURE_SQL_SERVER`, `AZURE_SQL_DATABASE` e `AZURE_APP_SERVICE_PLAN`.
4. Confirmar subject `repo:ajmarzola/Precificador:environment:production`, issuer `https://token.actions.githubusercontent.com` e audience `api://AzureADTokenExchange` na federa??o.

Nenhum segredo GitHub ? necess?rio. N?o criar client secret, publish profile ou SQL password. Valores reais ficam no ambiente administrativo/GitHub, nunca no reposit?rio ou na PR.

## Deploy autom?tico

```text
push master ? build-and-test full ? ZIP + bundle ? upload por SHA
? deploy-production/production ? download da mesma execu??o ? OIDC
? preflight ? firewall por run/attempt ? bundle ? cleanup confirmado
? az webapp deploy ZIP ? GET / e /Conta/Login ? Step Summary
```

PRs preservam MEL028 e nunca fazem deploy. `deploy-production` n?o executa build/test/publish; consome o artefato `precificador-deploy-<SHA>` retido por 30 dias. `manifest.json` registra SHA e SHA256 do ZIP/bundle. O bundle self-contained Linux n?o precisa de SDK no job de deploy; `appsettings.json` vazio acompanha a factory que recebe a conex?o pelo ambiente. O ZIP usa `/` nas entradas, inclusive quando gerado no Windows.

`common.ps1` centraliza o preflight dos fluxos manual/CD/bootstrap: contexto Enabled e tenant/subscription corretos, plano efetivo da Web App F1, SQL Free/AutoPause, HTTPS Only, runtime Managed Identity sem User ID/Password/UID/PWD, timeout m?nimo e mesmo servidor/database da migration. Qualquer diverg?ncia falha antes de migration. Application Settings, SMTP, URL p?blica e Setup s?o preservados.

Firewall usa `github-cd-<run-id>-<attempt>` e apenas o IPv4 detectado. `finally` remove e consulta novamente as regras; erro no cleanup bloqueia deploy. O workflow tamb?m executa cleanup com `always()`. N?o assumir que finally pode rodar ap?s perda total de runner: nessa ocorr?ncia, remover administrativamente a regra exata e confirmar aus?ncia antes de concluir o incidente.

Concurrency `precificador-production` impede deployments simult?neos e n?o cancela o job em execu??o. O GitHub pode substituir um job ainda pendente por push posterior; n?o h? garantia FIFO de cada commit. Rerun de execu??o master autorizada ? permitido; n?o h? workflow_dispatch de SHA arbitr?rio.

## Valida??o e primeiro run p?s-merge

Antes do merge, validar restore/build/unit/integration, sintaxe YAML/actions e PowerShell 7, gera??o real do ZIP e bundle e guards/cleanup. `build-artifact.ps1` pressup?e gate full j? aprovado e diret?rio de sa?da novo:

```powershell
./infra/azure/build-artifact.ps1 -OutputDirectory <DiretorioNovo> -Runtime linux-x64 -CommitSha <SHA>
./infra/azure/validate-cd.ps1
```

O estado MEL027 permanece `Pronto`. A evid?ncia final exige CI full master verde, login OIDC, migration, firewall cleanup, ZIP deploy e smoke reais, com produ??o acess?vel e nenhuma regra tempor?ria restante. Somente ap?s esse run atualizar MEL027/backlog para `Conclu?do` em outra PR. O agente n?o faz merge.

## Troubleshooting do CD

- OIDC: revisar environment, branch permitida, subject/audience e identifiers; n?o substituir por segredo.
- RBAC/preflight: confirmar roles e escopos com Azure CLI; Reader no plano ? necess?rio ao guard F1. Falha de leitura interrompe; n?o ampliar ao Resource Group.
- Migration: conferir usu?rio/SID client ID, roles SQL, firewall e disponibilidade Free/AutoPause. O EF preserva retry transit?rio; n?o repetir falha de autoriza??o ou migration inv?lida indefinidamente.
- Cleanup: se falhar, job fica vermelho e ZIP n?o ? publicado; remover a regra espec?fica e confirmar. N?o criar allowlist gen?rica 0.0.0.0.
- Deploy/smoke: conferir logs operacionais sem tokens/settings sens?veis; seis tentativas por endpoint cobrem cold start. 5xx persistente ? falha, sem Setup/POST/dados de produ??o.
- F1 sem slot: migrations futuras precisam ser compat?veis com a aplica??o anterior durante a janela migration/deploy. Nunca executar Down automaticamente; revert de aplica??o exige compatibilidade com schema vigente.

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
