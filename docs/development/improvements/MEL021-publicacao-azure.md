# MEL021 — Publicar Precificador no Azure com custo controlado

- **Origem:** disponibilização remota do Precificador para uso pessoal/familiar.
- **Classificação:** infraestrutura / hospedagem / operação.
- **Prioridade:** alta.
- **Estado:** Especificado — bloqueado até conclusão de MEL022 e MEL023.
- **Ordem na fila pendente:** 10.
- **Dependências:** MEL020 concluída; MEL022 e MEL023 pendentes.
- **Gate operacional:** só liberar após MEL022 e MEL023; executar antes de UC028.
- **Alteração de domínio/regra de negócio:** não.
- **Alteração de schema lógico:** não intencional.
- **Provisionamento Azure:** sim.
- **Publicação:** sim.
- **Migração de dados locais:** não.

## Objetivo

Publicar o Precificador em Azure, acessível pela internet, preservando o objetivo de **custo mensal igual a zero** no cenário atual de poucos usuários conhecidos e uso esporádico.

Arquitetura alvo:

```text
Navegador
  -> HTTPS
Azure App Service Free F1 / Linux / .NET 10
  -> Managed Identity
Azure SQL Database Free offer / General Purpose Serverless
  -> EF Core migrations
```

A MEL021 materializa a publicação preparada pela MEL020 e não cria funcionalidade de negócio.

## Referências de plataforma

Situação verificada em 18/09/2026:

- App Service F1 é camada gratuita, compartilhada, sem SLA e adequada a experimentação/uso leve;
- Azure SQL Free oferece franquia mensal gratuita e permite pausar ao atingir a franquia;
- App Service suporta Managed Identity para acesso passwordless ao Azure SQL;
- o domínio padrão `*.azurewebsites.net` possui HTTPS.

A implementação deve revalidar a disponibilidade das ofertas gratuitas antes de provisionar.

## Regra de custo

**É proibido criar recurso pago como fallback automático.**

Se App Service F1 ou Azure SQL Free não estiver disponível na assinatura/região:

1. interromper o provisionamento;
2. informar o recurso indisponível;
3. não promover automaticamente para B1, S1, General Purpose pago ou outra SKU faturável.

Qualquer mudança para tier pago exige decisão explícita posterior.

## App Service

Usar:

```text
Azure App Service Plan
SKU: F1 / Free
OS: Linux
Runtime: .NET 10
```

Publicação framework-dependent por padrão.

Não criar Docker, custom container ou Azure Container Registry.

URL inicial:

```text
https://<nome-app>.azurewebsites.net
```

Não configurar domínio próprio nesta MEL.

Habilitar HTTPS Only.

### Limitações aceitas

São aceitas:

- ausência de SLA;
- compute compartilhado;
- cold start;
- ausência de Always On;
- ausência de deployment slots;
- ausência de custom domain;
- limites de CPU/memória/armazenamento da camada Free.

Não implementar ping artificial para manter a aplicação acordada.

## Azure SQL Database

Criar:

```text
Edition: GeneralPurpose
Compute model: Serverless
Use free limit: true
Free limit exhaustion behavior: AutoPause
```

`BillOverUsage` é proibido.

Criar logical server dedicado ao Precificador.

Preferir autenticação **Microsoft Entra-only**.

Não versionar login/senha de SQL Server.

### Região

Região é parâmetro.

Preferir Brazil South somente se App Service F1 e Azure SQL Free estiverem disponíveis. Caso contrário, selecionar manualmente outra região compatível.

Nunca trocar automaticamente para SKU paga.

## App Service -> Azure SQL

Habilitar **System Assigned Managed Identity** na Web App.

A aplicação deve conectar sem senha.

Referência:

```text
Server=tcp:<server>.database.windows.net,1433;
Database=<database>;
Authentication=Active Directory Managed Identity;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

`Active Directory Default` é aceitável se continuar passwordless e determinístico no App Service.

No Azure, fornecer a conexão por:

```text
ConnectionStrings__Precificador
```

Não mudar o contrato `ConnectionStrings:Precificador`.

A identidade runtime recebe somente leitura/escrita necessárias. Referência:

```sql
CREATE USER [<webapp>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<webapp>];
ALTER ROLE db_datawriter ADD MEMBER [<webapp>];
```

Não conceder `db_owner` ou `db_ddladmin` à Web App.

Service Connector passwordless é aceitável se resultar no mesmo contrato e não persistir segredo.

## Rede

Azure SQL pode usar endpoint público nesta arquitetura de custo zero.

Não criar VNet, NAT Gateway, Private Endpoint ou Azure Firewall.

Preferir allowlist explícita dos outbound/possible outbound IPs da Web App.

Evitar manter permanentemente `0.0.0.0` / “Allow Azure services and resources to access this server” quando a allowlist da Web App for viável.

Provisionamento deve:

1. obter outbound IPs da Web App;
2. criar regras do SQL para esses IPs;
3. adicionar IP do operador temporariamente apenas para migration/bootstrap;
4. remover o IP temporário ao final.

## Migrations em Azure

Preservar MEL020:

```text
Development -> Database.MigrateAsync() no startup
Production/Azure -> NÃO aplicar migration automaticamente no startup
```

Não alterar `Program.cs` para auto-migrate em Production.

Antes do primeiro acesso:

1. Azure SQL existe;
2. operador consegue conectar temporariamente;
3. migrations SQL Server são aplicadas explicitamente;
4. nenhuma migration fica pendente;
5. regra temporária do operador é removida;
6. aplicação é publicada/validada.

O procedimento pode usar `dotnet ef database update`, migration bundle ou equivalente.

Preferir autenticação Entra do operador, sem senha SQL armazenada.

## Provisionamento reproduzível

Criar:

```text
infra/azure/
  provision.ps1
  deploy.ps1
  README.md
```

Azure CLI + PowerShell é a abordagem preferida.

Scripts devem ser parametrizados e não conter senha, token, subscription/tenant pessoal fixo ou segredo.

### provision.ps1

Responsabilidades:

- validar login/subscription;
- criar/obter Resource Group;
- criar App Service Plan F1;
- criar Web App .NET 10;
- habilitar HTTPS Only e Managed Identity;
- criar Azure SQL Server;
- criar Azure SQL Database com Free Limit + AutoPause;
- configurar conexão passwordless;
- configurar firewall;
- imprimir apenas outputs não sensíveis;
- falhar se a configuração gratuita não puder ser criada.

### deploy.ps1

Responsabilidades:

1. validar login/subscription;
2. executar ou exigir restore/build/test/CI verde;
3. aplicar migrations explicitamente;
4. publicar `Precificador.Web` em Release;
5. fazer deploy no App Service;
6. executar smoke HTTPS;
7. informar URL final.

Não criar recurso pago durante deploy.

## Publicação

CI/CD automático está fora de escopo.

Deploy manual reproduzível é suficiente.

Não criar deploy automático a cada push em `master`.

O CI existente continua gate obrigatório.

## Ambiente ASP.NET Core

Azure deve operar como `Production` (default do App Service ou configuração explícita).

Não configurar `Development` para obter auto-migration.

Não expor Detailed Errors em Production.

## Primeiro uso

Com banco migrado e sem usuários:

```text
Entrar
-> /Conta/Login
-> zero usuários
-> /Setup
-> cria primeiro usuário/empresa
-> login normal
```

FT002, MEL011 e MEL012 permanecem válidos.

Não criar credencial padrão.

## Sessão e cold start

Não introduzir Redis ou sessão distribuída.

Aceitar que reciclagem/cold start possa exigir nova seleção da Empresa Ativa.

Ausência de Empresa Ativa continua bloqueando as áreas de negócio.

## Logs

Usar logging padrão ASP.NET Core/App Service.

Não habilitar Application Insights ou Log Analytics por padrão.

## Dados

Azure SQL nasce pelas migrations.

Não transportar automaticamente dados do SQL Server local.

Se for necessária migração de dados reais, criar melhoria separada.

## Smoke real obrigatório

Após deploy:

1. HTTPS responde;
2. Home carrega;
3. zero usuários leva a Setup;
4. Setup cria usuário/empresa;
5. login funciona;
6. Empresa Ativa é resolvida;
7. cadastrar/consultar Insumo;
8. cadastrar/consultar Produto;
9. aplicação acessa SQL via Managed Identity;
10. após remover IP temporário do operador, Web App continua acessando o banco.

## Validação de custo

Antes de concluir:

- App Service Plan = F1;
- Azure SQL = Free Limit;
- `freeLimitExhaustionBehavior = AutoPause`;
- nenhum App Service pago;
- nenhum recurso faturável adicional criado pela MEL021;
- nenhum fallback pago habilitado.

## Documentação arquitetural

A implementação deve corrigir documentos atuais que ainda descrevam SQLite/local-only como estado vigente quando encontrados, especialmente:

- `README.md`;
- `AGENTS.md`;
- `docs/architecture/architecture.md`;
- `docs/development/testing-strategy.md`;
- `docs/development/definition-of-done.md`, quando aplicável.

Referências históricas podem permanecer se identificadas como históricas.

## Critérios de aceitação

- **CA01:** MEL020 concluída.
- **CA02:** App Service Plan F1/Free.
- **CA03:** Web App Linux/.NET 10 sem container customizado.
- **CA04:** Web App acessível em `azurewebsites.net`.
- **CA05:** HTTPS Only habilitado.
- **CA06:** Azure SQL usa Free Limit.
- **CA07:** comportamento de esgotamento é `AutoPause`.
- **CA08:** Azure SQL usa General Purpose/Serverless compatível com a oferta.
- **CA09:** Web App possui System Assigned Managed Identity.
- **CA10:** conexão App->SQL não contém senha.
- **CA11:** conexão chega via `ConnectionStrings__Precificador`.
- **CA12:** identidade runtime não possui DDL amplo.
- **CA13:** Production não executa `Database.MigrateAsync()` no startup.
- **CA14:** migrations são aplicadas explicitamente.
- **CA15:** nenhuma migration fica pendente após deploy.
- **CA16:** firewall permite Web App sem manter IP temporário do operador.
- **CA17:** provisionamento é reproduzível por scripts.
- **CA18:** scripts não contêm segredos.
- **CA19:** deploy Release é reproduzível.
- **CA20:** smoke real Azure passa.
- **CA21:** fluxo Setup/login/Empresa Ativa permanece funcional.
- **CA22:** Insumo e Produto persistem no Azure SQL.
- **CA23:** CI/suíte completa continuam verdes.
- **CA24:** nenhum recurso pago é criado como fallback.
- **CA25:** Application Insights/Key Vault/VNet/Private Endpoint/ACR não são introduzidos.
- **CA26:** nenhuma regra de negócio muda.
- **CA27:** nenhum dado local é migrado automaticamente.
- **CA28:** documentação atual reflete SQL Server/Azure SQL e modo hospedado.
- **CA29:** MEL021 passa a `Concluído`.
- **CA30:** UC028 permanece sem implementação.

## Matriz mínima

### W1 — custo

Confirmar via Azure CLI:

```text
App Service -> F1
Azure SQL -> useFreeLimit = true
Azure SQL -> freeLimitExhaustionBehavior = AutoPause
```

### W2 — autenticação

Confirmar Managed Identity + connection string sem senha.

### W3 — migration

Banco novo -> aplicar migration -> nenhuma pendência.

### W4 — Production

Provar que Production não auto-migra.

### W5 — rede

Remover IP temporário e confirmar acesso da Web App ao SQL.

### W6 — bootstrap

Login -> Setup -> primeiro usuário -> login.

### W7 — persistência Azure

Criar/consultar Insumo e Produto.

### W8 — deploy repetível

Reexecutar deploy sem recriar recurso pago nem corromper schema.

### W9 — regressão

```text
dotnet build Precificador.slnx --configuration Release
dotnet test Precificador.slnx --configuration Release --no-build
```

Tudo verde.

## Fora do escopo

- custom domain;
- App Service B1+;
- SLA/autoscale/slots;
- Application Insights/Log Analytics;
- Key Vault;
- VNet/Private Endpoint/Azure Firewall/NAT;
- Docker/containers/ACR;
- Front Door/CDN;
- migração de dados locais;
- CD automático;
- UC028+.

## Definition of Done

MEL021 concluída quando:

- App Service F1 e Azure SQL Free estão criados;
- SQL Free está em AutoPause;
- conexão é passwordless por Managed Identity;
- migrations foram aplicadas explicitamente;
- app roda em Production;
- smoke real passou;
- nenhum recurso pago foi introduzido;
- scripts estão versionados;
- documentação está coerente;
- suíte completa está verde;
- backlog marca MEL021 concluída;
- UC028 continua aguardando.

## Branch sugerida

```text
infra/mel021-publicacao-azure
```
