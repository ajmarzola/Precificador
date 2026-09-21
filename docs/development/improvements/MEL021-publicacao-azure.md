# MEL021 — Publicar Precificador no Azure com custo controlado

- **Origem:** disponibilização remota do Precificador para uso pessoal/familiar.
- **Classificação:** infraestrutura / hospedagem / operação.
- **Prioridade:** alta.
- **Estado:** Especificado — bloqueado por UC032/UC036 e pela indisponibilidade externa da conta Azure.
- **Ordem na fila pendente:** 10.
- **Dependências:** MEL020, MEL022 e MEL023 concluídas; UC032 e UC036 pendentes.
- **Gate operacional:** executar somente após UC032 e UC036; bloqueio externo adicional até existir assinatura Azure utilizável; ainda antes de UC028.
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

## Gate adicional — UC032 / UC036

Após a especificação original da publicação, foi decidido antecipar:

```text
UC032 — Categorias estruturadas de Produto
UC036 — Custo de desgaste de equipamentos por Categoria
```

A ordem normativa passou a ser:

```text
UC032
-> UC036
-> MEL021
```

Motivo: o desgaste altera o custo atual do Produto e deve estar consolidado antes da primeira publicação hospedada.

Além disso, a conta Azure encontra-se atualmente bloqueada para criação/uso de assinatura, com solicitação de suporte aberta. Esse bloqueio é externo ao código.

Portanto, não iniciar o provisionamento enquanto qualquer um destes gates estiver pendente:

- UC032 não concluída;
- UC036 não concluída;
- conta Azure sem assinatura utilizável.

A especificação técnica desta MEL permanece válida e não deve ser descartada.

## Referências de plataforma

Situação revalidada em 20/09/2026 contra documentação oficial da Microsoft:

- **Azure App Service F1 / Linux** permanece gratuito, com compute compartilhado, **60 minutos de CPU por dia**, **1 GB de RAM** e **1 GB de armazenamento** por aplicativo;
- F1 não possui SLA e a Microsoft declara que a camada Free é destinada a avaliação/experimentação/aprendizado e **não é suportada para workloads de produção**;
- F1 permite somente o subdomínio padrão `*.azurewebsites.net`; domínio personalizado fica fora desta MEL;
- **Azure SQL Database Free offer** permanece sem prazo de expiração, com franquia mensal por banco de **100.000 vCore-seconds**, **32 GB de dados** e **32 GB de backup**; a oferta gratuita também não possui SLA e é recomendada principalmente para desenvolvimento/PoC;
- a oferta gratuita do Azure SQL permite até 10 bancos por assinatura, mas o Precificador usará apenas 1;
- o comportamento **AutoPause** ao atingir o limite gratuito interrompe o banco até o início do próximo mês e evita cobrança de excedente;
- App Service suporta System Assigned Managed Identity para conexão passwordless com Azure SQL;
- o projeto já está em .NET 10 / SQL Server e Production não executa migrations automaticamente.

Referências oficiais:

- https://azure.microsoft.com/pricing/details/app-service/linux/
- https://learn.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits
- https://learn.microsoft.com/azure/azure-sql/database/free-offer
- https://learn.microsoft.com/azure/azure-sql/database/free-offer-faq
- https://learn.microsoft.com/cli/azure/sql/db
- https://learn.microsoft.com/azure/app-service/tutorial-connect-msi-sql-database
- https://learn.microsoft.com/ef/core/providers/sql-server

A implementação deve revalidar disponibilidade regional e os parâmetros da oferta gratuita imediatamente antes do provisionamento.

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

### Limitações e risco aceitos

O Precificador será um **ambiente hospedado pessoal/familiar de baixo uso**, não uma oferta comercial com SLA.

É decisão consciente usar F1 mesmo com a advertência da Microsoft de que a camada Free não é suportada para workloads de produção.

Aceitamos:

- ausência de SLA;
- compute compartilhado;
- 60 minutos de CPU/dia por aplicativo;
- 1 GB de RAM;
- 1 GB de armazenamento;
- cold start;
- ausência de Always On;
- ausência de deployment slots;
- somente `*.azurewebsites.net`;
- eventual indisponibilidade ao atingir cotas do F1.

`ASPNETCORE_ENVIRONMENT=Production` continua obrigatório no Azure por segurança/comportamento da aplicação. Isso **não significa** que o tier F1 tenha suporte/SLA de produção.

Não implementar ping artificial para manter a aplicação acordada.

## Azure SQL Database

Criar exatamente dentro da oferta gratuita:

```text
Edition: GeneralPurpose
Compute model: Serverless
Use free limit: true
Free limit exhaustion behavior: AutoPause
```

Limites atuais aceitos:

```text
100.000 vCore-seconds / mês
32 GB dados
32 GB backup
1 banco para o Precificador
```

`BillOverUsage` é proibido.

Se o limite gratuito de compute ou armazenamento for atingido com AutoPause, o banco pode ficar indisponível até o início do próximo mês. Esse comportamento é preferível a qualquer cobrança automática.

Esse `AutoPause` é o **comportamento ao esgotar a franquia gratuita**. Ele não deve ser confundido com a pausa automática por inatividade do compute serverless. A configuração serverless deve continuar permitindo pausa por inatividade quando suportado pela oferta; não desabilitá-la apenas para reduzir cold start.

Ferramentas como SSMS/Visual Studio/SQL tooling devem ser desconectadas quando não estiverem em uso, pois conexões abertas podem impedir auto-pause e consumir a franquia de vCore.

Criar logical server dedicado ao Precificador.

Usar autenticação **Microsoft Entra-only** como caminho normativo. Se a assinatura/tenant não permitir configurar o Entra admin necessário, interromper o provisionamento e informar o bloqueio; não habilitar senha SQL automaticamente.

O logical server deve possuir um **Microsoft Entra administrator** explícito para bootstrap operacional. Esse administrador deve ser parametrizado pelo script (nome + Object ID/SID) e nunca hardcoded no repositório.

É aceitável criar o logical server diretamente com:

```text
--enable-ad-only-auth
--external-admin-principal-type User|Group
--external-admin-name <parametro>
--external-admin-sid <parametro>
```

Não criar ou versionar login/senha SQL como caminho normal.

### Região

Região é parâmetro.

App Service e Azure SQL devem ficar **na mesma região**.

Preferir Brazil South somente se App Service F1, runtime .NET 10 e Azure SQL Free estiverem disponíveis. Caso contrário, selecionar manualmente outra região compatível para ambos os recursos.

Nunca separar Web App e banco entre regiões apenas para contornar disponibilidade e nunca trocar automaticamente para SKU paga.

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

Depois de habilitar a System Assigned Managed Identity da Web App, o operador conectado como Microsoft Entra admin deve criar o principal da aplicação no banco.

A identidade runtime recebe somente leitura/escrita necessárias. Referência:

```sql
CREATE USER [<webapp>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<webapp>];
ALTER ROLE db_datawriter ADD MEMBER [<webapp>];
```

Não conceder `db_owner` ou `db_ddladmin` à Web App.

Se `CREATE USER ... FROM EXTERNAL PROVIDER` não conseguir resolver o principal pelo Microsoft Entra/Graph, usar a alternativa oficial baseada em SID/Object ID; **não** contornar o problema habilitando senha SQL ou dando permissão ampla.

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

## Resiliência de conexão

Como o runtime atual usa `UseSqlServer`, a MEL021 deve habilitar resiliência para falhas transitórias do Azure SQL:

```csharp
options.UseSqlServer(
    connectionString,
    sql => sql.EnableRetryOnFailure());
```

ou configuração equivalente suportada pelo EF Core 10.

A mesma configuração pode permanecer ativa no SQL Server local; não criar seleção de provider por ambiente.

Não alterar regras de transação/negócio apenas por causa do retry.

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
- validar disponibilidade regional do F1 e Azure SQL Free;
- validar que o runtime Linux .NET 10 está disponível;
- resolver/receber os parâmetros do Microsoft Entra administrator;
- criar App Service Plan F1;
- criar Web App .NET 10;
- habilitar HTTPS Only e Managed Identity;
- criar Azure SQL logical server com Entra-only e admin explícito;
- criar Azure SQL Database com Free Limit + AutoPause;
- criar/conceder principal da Managed Identity no banco com `db_datareader` + `db_datawriter`;
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

- App Service Plan = F1 / custo $0;
- confirmar limites F1 vigentes no momento do provisionamento;
- Azure SQL = Free Limit;
- `freeLimitExhaustionBehavior = AutoPause`;
- confirmar franquia gratuita vigente do Azure SQL;
- confirmar que nenhum recurso auxiliar pago foi criado;
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
- **CA31:** limites gratuitos de App Service e Azure SQL são revalidados no momento do provisionamento.
- **CA32:** risco/limitação do F1 para uso pessoal sem SLA está documentado.
- **CA33:** logical server possui Microsoft Entra admin explícito e não depende de senha SQL.
- **CA34:** Web App usa apenas `db_datareader` + `db_datawriter` em runtime.
- **CA35:** `EnableRetryOnFailure` ou equivalente está habilitado para o provider SQL Server.
- **CA36:** atingir o limite gratuito do Azure SQL não pode produzir cobrança automática.
- **CA37:** scripts falham em vez de trocar automaticamente para tier pago ou runtime incompatível.
- **CA38:** App Service e Azure SQL são provisionados na mesma região.
- **CA39:** pausa serverless por inatividade não é deliberadamente desabilitada para eliminar cold start.

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

### W9 — resiliência

Confirmar que `UseSqlServer` está configurado com retry para falhas transitórias do Azure SQL.

### W10 — Entra bootstrap

Confirmar:

```text
logical server -> Entra-only
Entra admin -> configurado
Web App MI -> usuário no banco
roles runtime -> db_datareader + db_datawriter
sem db_owner/db_ddladmin
```

### W11 — quotas gratuitas

Registrar os valores detectados/confirmados no momento do deploy e comprovar:

```text
App Service = F1
SQL = Free Limit
exhaustion = AutoPause
```

### W12 — regressão

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
- limites gratuitos atuais foram revalidados e registrados;
- conexão é passwordless por Managed Identity;
- logical server usa Entra admin explícito sem credencial SQL versionada;
- runtime da Web App possui somente leitura/escrita no banco;
- retry de conexão Azure SQL está habilitado;
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
