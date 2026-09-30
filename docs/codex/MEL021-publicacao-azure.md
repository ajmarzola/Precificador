# Codex — MEL021 — Publicação Azure

> **Gate:** CONCLUÍDO. MEL021 foi provisionada, publicada e validada no Azure em 29/09/2026. `docs/development/backlog.md` marca MEL021 como `Concluído`; não antecipar UC028 antes do merge da PR da MEL021.

Implemente exclusivamente a MEL021 conforme:

```text
docs/development/improvements/MEL021-publicacao-azure.md
```

## Branch obrigatória

```text
infra/mel021-publicacao-azure
```

Nunca editar `master` diretamente.

## Objetivo

Publicar o Precificador em Azure App Service F1 com Azure SQL Free, mantendo custo alvo zero e sem implementar UC028.

O provisionamento está liberado. Antes de qualquer criação de recurso, validar login, assinatura selecionada/ativa, região, F1, runtime .NET 10 e Azure SQL Free; falhar sem criar fallback pago se qualquer pré-condição não for atendida.

A ordem obrigatória é:

```text
UC032 -> UC036 -> MEL021
```

O gate externo de assinatura está atendido desde 26/09/2026. Não hardcodar subscription/tenant pessoal no repositório.

**Correção obrigatória após validação da primeira publicação:** não confiar no contexto implícito da Azure CLI. `TenantId` e `SubscriptionId` devem ser parâmetros obrigatórios de `provision.ps1` e `deploy.ps1`. Os scripts devem apenas validar o contexto ativo; não devem trocar automaticamente tenant/assinatura.

## Invariantes

- App Service: **F1/Free**;
- Azure SQL: **Free Limit + AutoPause**;
- confirmar no provisionamento a franquia gratuita vigente (atualmente 100.000 vCore-seconds/mês, 32 GB dados, 32 GB backup);
- com AutoPause por limite gratuito, confirmar as limitações vigentes (atualmente até 4 vCores, PITR até 7 dias, backup LRS e sem LTR);
- não mudar a oferta para continuidade paga/upgrade como fallback;
- App Service F1 é aceito conscientemente para piloto operacional interno da Carinho e Amor, de baixo uso e sem SLA, apesar de não possuir suporte Microsoft para workload de produção;
- nunca criar fallback pago;
- App Service -> Azure SQL por **Managed Identity**, sem senha;
- Azure SQL logical server com **Microsoft Entra-only + Entra administrator explícito**;
- se Entra-only não puder ser configurado, parar; não habilitar SQL password como fallback;
- App Service e Azure SQL obrigatoriamente na mesma região;
- `TenantId` e `SubscriptionId` obrigatórios nos scripts;
- antes de qualquer operação mutável, `az account show` deve confirmar `state=Enabled`, tenant esperado e subscription esperada;
- divergência de contexto deve abortar a execução antes de criar, alterar, migrar ou publicar recurso;
- **não** executar `az account set` automaticamente dentro dos scripts;
- runtime da Web App somente `db_datareader` + `db_datawriter`;
- `ConnectionStrings__Precificador` é a configuração do Azure;
- Production não executa `Database.MigrateAsync()` automaticamente;
- migrations são etapa explícita de deploy;
- domínio inicial é `*.azurewebsites.net`;
- sem custom domain;
- sem App Insights, Key Vault, VNet, Private Endpoint, ACR ou container;
- não migrar dados locais;
- UC028 não entra no diff;
- `UseSqlServer` deve usar `EnableRetryOnFailure()` ou equivalente para Azure SQL.

## Infraestrutura

Criar:

```text
infra/azure/
  provision.ps1
  deploy.ps1
  README.md
```

Preferir Azure CLI + PowerShell simples.

Scripts parametrizados; proibido versionar senha, token, subscription/tenant pessoal fixo ou segredo.

### provision.ps1

Deve:

1. tornar `TenantId` e `SubscriptionId` parâmetros obrigatórios;
2. executar preflight somente leitura com `az account show`;
3. confirmar `state=Enabled`, `tenantId == TenantId` e `id == SubscriptionId`;
4. abortar em qualquer divergência antes de operação mutável;
5. não executar `az account set` para corrigir contexto;
6. somente depois criar/usar Resource Group;
7. escolher uma única região onde F1, runtime .NET 10 e Azure SQL Free estejam disponíveis;
4. descobrir/validar runtime Linux .NET 10 com `az webapp list-runtimes --os linux`;
5. obter por parâmetro o Entra admin (nome + Object ID/SID);
6. criar App Service Plan F1 Linux;
7. criar Web App .NET 10;
8. habilitar HTTPS Only;
9. habilitar System Assigned Managed Identity;
10. criar Azure SQL logical server com Entra-only e Entra admin explícito;
11. criar banco GeneralPurpose Serverless com `--use-free-limit`;
12. usar `--free-limit-exhaustion-behavior AutoPause`;
13. criar usuário da Managed Identity no banco;
14. conceder somente `db_datareader` + `db_datawriter`;
15. configurar conexão passwordless;
16. configurar firewall considerando `outboundIpAddresses` e `possibleOutboundIpAddresses` da Web App;
17. falhar se qualquer etapa exigir tier pago.

Microsoft Entra-only é o caminho normativo. Não criar senha SQL como fallback automático.

Service Connector passwordless é aceitável se o resultado final respeitar o contrato da MEL021.

## Resiliência

Como a aplicação usa `UseSqlServer`, habilitar `EnableRetryOnFailure()` ou equivalente suportado pelo EF Core 10.

O Azure SQL Serverless pode ficar `Paused` após inatividade; isso é comportamento esperado. A primeira conexão pode precisar aguardar o auto-resume. Corrigir as connection strings Azure usadas pela aplicação e pelas migrations para `Connection Timeout=60` ou superior, preservando `EnableRetryOnFailure()`. O bootstrap de `provision.ps1` deve repetir de forma finita a abertura da conexão para o erro transitório `40613`, com backoff e descarte da conexão a cada tentativa.

Não trocar provider entre desenvolvimento e Azure.

## Migrations

Preservar MEL020:

```text
Development -> auto-migrate
Azure/Production -> migration explícita
```

Não habilitar auto-migration em Production.

Para aplicar migration:
- permitir temporariamente o IP do operador se necessário;
- usar autenticação Entra do operador;
- aplicar migrations;
- confirmar zero pendências;
- remover IP temporário.

A Managed Identity runtime não recebe DDL.

## Deploy

`deploy.ps1` deve:

- tornar `TenantId` e `SubscriptionId` parâmetros obrigatórios;
- validar `az account show` em modo fail closed antes de migration, deploy ou qualquer alteração;
- abortar se tenant/subscription atuais não forem exatamente os esperados;
- não trocar automaticamente o contexto com `az account set`;
- usar `Connection Timeout=60` ou superior na connection string Entra usada pelas migrations;
- compilar/publicar Release;
- aplicar migrations explicitamente;
- fazer deploy para App Service;
- executar smoke HTTPS;
- não criar recursos pagos.

Não criar CD automático.

## Smoke obrigatório

No Azure real:

- HTTPS responde;
- zero usuário -> Setup;
- criar primeiro usuário;
- login;
- Empresa Ativa;
- cadastrar/consultar Insumo;
- cadastrar/consultar Produto;
- remover IP temporário do operador;
- confirmar que Web App continua acessando Azure SQL.

## Documentação

Ajustar documentação atual que ainda trate SQLite/local-only como arquitetura vigente, especialmente:

- README;
- AGENTS;
- architecture;
- testing strategy;
- DoD, quando aplicável.

Não alterar documentos históricos além do necessário para marcar contexto histórico.

## Validação antes da PR

Antes de provision/deploy, validar localmente o contexto (sem publicar IDs pessoais na PR):

```text
az account show
state == Enabled
tenant atual == TenantId esperado
subscription atual == SubscriptionId esperada
```

Em seguida:

1. `dotnet restore Precificador.slnx`;
2. `dotnet build Precificador.slnx --configuration Release --no-restore`;
3. `dotnet test Precificador.slnx --configuration Release --no-build`;
4. App Service F1 e limites atuais confirmados;
5. Web App e Azure SQL confirmados na mesma região;
6. SQL Free + AutoPause e franquia atual confirmados;
7. Entra-only + Entra admin confirmados;
8. Managed Identity confirmada com apenas reader/writer;
9. `EnableRetryOnFailure` confirmado;
10. Production sem auto-migrate;
11. smoke Azure concluído;
12. scripts sem segredo;
13. MEL021 -> Concluído;
14. UC028 permanece Planejado;
15. execução com `TenantId` incorreto falha antes de qualquer mutação;
16. execução com `SubscriptionId` incorreto falha antes de qualquer mutação;
17. scripts não contêm troca automática de tenant/subscription;
18. connection strings Azure da aplicação e das migrations usam `Connection Timeout=60` ou superior;
19. documentação distingue `Paused` por inatividade do esgotamento da franquia Free.
