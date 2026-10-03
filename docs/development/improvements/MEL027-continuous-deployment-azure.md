# MEL027 — Automatizar Continuous Deployment do Precificador no Azure

- **Origem:** Backlog / evolução da MEL021
- **Classificação:** Infraestrutura / DevOps / Continuous Deployment
- **Prioridade:** alta
- **Estado:** Pronto
- **Dependências materiais:** MEL021, MEL028 e MEL013 concluídas
- **Dependência de UC035:** não
- **Alteração de domínio/regra de negócio:** não
- **Alteração de schema:** não
- **Migration:** não
- **Alteração de infraestrutura Azure:** sim, somente identidade/configuração necessária ao CD
- **Alteração de GitHub Actions:** sim
- **Alteração de código de produção:** não prevista

## Handoff da implementação

O workflow e os scripts implementam o fluxo descrito nesta especificação; [ADR-016](../../architecture/adr/ADR-016-continuous-deployment-oidc-azure.md) registra a decisão e a leitura adicional restrita ao plano e ao database específico necessária ao preflight. O [procedimento Azure](../../../infra/azure/README.md) descreve bootstrap, environment e fallback manual.

Gate obrigatório **antes do merge**: confirmar bootstrap Azure (UAMI/federação/quatro escopos RBAC/usuário SQL) e as oito environment variables. A PR permanece draft até esse gate estar atendido, pois o merge já dispara CD em `master`.

O estado permanece `Pronto` até a CI da PR estar verde, a configuração administrativa estar confirmada e o primeiro run real pós-merge comprovar OIDC, migration, remoção do firewall, deploy e smoke. Não fazer merge pelo agente nem liberar UC035 com base somente no código entregue.

Validação local da implementação em 02/10/2026: tool restore/restore/build Release aprovados, sem warnings; 323 testes unitários e 740 testes de integração aprovados com SQL Server temporário. ZIP produzido com entradas portáveis; migration bundle Linux gerado e executado contra base SQL Server descartável. `actionlint` e validação dos scripts em PowerShell 7 no Windows/Linux aprovados, incluindo preflight e falhas de migration/cleanup. Nenhuma migration de negócio ou configuração paga foi adicionada.

GitHub Environment `production` criado e política lida novamente: somente branch `master`, sem required reviewers por execução. Bootstrap Azure (identidade/federação/RBAC/usuário SQL) e as oito variables do environment ainda precisam ser executados/configurados com o destino confirmado pelo operador. CI da PR e primeiro CD real são evidências separadas da validação local acima.

## Objetivo

Automatizar a publicação do Precificador no ambiente Azure existente após alteração chegar a `master`, preservando os gates de qualidade atuais, a política de custo controlado da MEL021 e o modelo de segurança baseado em Microsoft Entra/Managed Identity.

Fluxo desejado:

~~~text
merge/push em master
-> CI full
-> artefato imutável do mesmo SHA
-> autenticação GitHub -> Azure por OIDC
-> preflight do ambiente
-> migration explícita
-> deploy do artefato validado
-> smoke
-> registro da implantação
~~~

Nenhuma publicação pode ocorrer quando a validação full falhar.

## 1. Build once, deploy same artifact

O código validado pela CI deve originar um artefato imutável associado ao SHA exato de `master`.

Artefato esperado:

~~~text
precificador-deploy-<SHA>
├── app.zip
└── migration-bundle
~~~

`app.zip` contém a publicação Release da aplicação.

`migration-bundle` contém o EF Core Migration Bundle correspondente ao mesmo SHA.

O job de deploy não recompila aplicação nem migrations.

## 2. Preservar MEL028 e o required check

O job atual `build-and-test` continua existindo com esse nome.

Para Pull Request:

~~~text
documentation-only -> comportamento atual MEL028
full -> restore + build + unit + integration
~~~

Para `push` em `master`:

~~~text
sempre full
-> build
-> unit
-> integration
-> geração de artefato de deployment
~~~

Deploy depende do sucesso de `build-and-test`.

PR nunca publica no Azure.

## 3. Estrutura do workflow

Preferir manter o CD no workflow atual, com dois jobs:

~~~text
build-and-test
       |
       v
deploy-production
~~~

`deploy-production` deve possuir `needs: build-and-test` e executar somente em `push` da branch `master`.

Não usar `workflow_run` sem necessidade técnica.

## 4. GitHub Environment

Criar/configurar o environment:

~~~text
production
~~~

O job de deploy deve declarar `environment: production` e associar a URL atual da aplicação.

O environment deve aceitar deployment somente de `master`.

A MEL027 é Continuous Deployment; aprovação humana obrigatória antes de cada publicação não faz parte do fluxo padrão.

## 5. Autenticação GitHub -> Azure

Usar:

~~~text
GitHub Actions OIDC
-> Microsoft Entra
-> identidade dedicada de deployment
~~~

Não usar:

- publish profile;
- `AZURE_CREDENTIALS`;
- client secret;
- certificado privado versionado;
- senha de usuário Azure;
- SQL Login/password.

Permissões GitHub mínimas do job:

~~~text
contents: read
id-token: write
~~~

Não usar `write-all`.

## 6. Identidade dedicada de CD

Criar User Assigned Managed Identity exclusiva do pipeline, por exemplo:

~~~text
mi-precificador-cd
~~~

Separação obrigatória:

~~~text
Web App runtime identity
-> db_datareader
-> db_datawriter

GitHub/CD identity
-> deploy
-> migration
-> firewall temporário
~~~

A System Assigned Managed Identity da Web App continua sendo a identidade runtime e não recebe DDL.

## 7. Federação OIDC

Configurar Federated Identity Credential restrita ao repositório Precificador e ao environment `production`.

Não criar confiança genérica para qualquer repositório, branch ou environment.

## 8. RBAC Azure

Aplicar privilégio mínimo.

### Web App

No escopo da Web App específica:

~~~text
Website Contributor
~~~

### SQL logical server

No escopo do SQL logical server:

~~~text
SQL Security Manager
~~~

Não conceder `Owner`.

Não conceder `Contributor` no Resource Group inteiro apenas para o CD.

## 9. Permissões SQL para migration

Criar usuário Entra no database para a identidade de CD.

Base aceitável:

~~~text
db_ddladmin
db_datareader
db_datawriter
~~~

A identidade runtime não é promovida.

Não usar `db_owner` salvo limitação técnica comprovada e decisão explícita posterior.

## 10. Bootstrap do CD

Criar script idempotente:

~~~text
infra/azure/configure-cd.ps1
~~~

Responsabilidades:

1. validar Tenant/Subscription;
2. criar ou localizar a identidade dedicada;
3. configurar a federação OIDC;
4. aplicar RBAC restrito;
5. preparar o usuário SQL de migration;
6. validar o resultado;
7. informar os identificadores a registrar no GitHub;
8. não imprimir segredo;
9. não criar recurso pago.

O script é administrativo e não roda em todo deployment.

## 11. Configuração GitHub

Environment `production`:

~~~text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
~~~

Variáveis não secretas:

~~~text
AZURE_RESOURCE_GROUP
AZURE_WEBAPP_NAME
AZURE_SQL_SERVER
AZURE_SQL_DATABASE
AZURE_APP_SERVICE_PLAN
~~~

Não armazenar:

~~~text
AZURE_CLIENT_SECRET
SQL_PASSWORD
PUBLISH_PROFILE
AZURE_CREDENTIALS
~~~

## 12. Artefato da aplicação

Após o gate full:

~~~text
dotnet publish src/Precificador.Web/Precificador.Web.csproj --configuration Release --no-build
~~~

Produzir `app.zip`.

O pacote deve corresponder exatamente ao SHA testado.

## 13. Migration bundle

Gerar EF Core Migration Bundle para o mesmo SHA usando:

- projeto: `src/Precificador.Infrastructure/Precificador.Infrastructure.csproj`;
- startup: `src/Precificador.Web/Precificador.Web.csproj`;
- configuração Release;
- runtime compatível com o runner de deployment.

O bundle não contém credenciais.

## 14. Artefato imutável

Nome:

~~~text
precificador-deploy-<github.sha>
~~~

Retenção sugerida: 30 dias.

O deployment baixa o artefato produzido pelo próprio `build-and-test` daquela execução.

Não usar nome mutável como `latest.zip`.

## 15. Preflight Azure

Antes de migration/deploy, validar os guards da MEL021:

- subscription;
- tenant;
- App Service Plan `F1`;
- Azure SQL Free Limit;
- `freeLimitExhaustionBehavior = AutoPause`;
- HTTPS Only;
- runtime connection string usando Managed Identity;
- ausência de `Password`/`User ID` na connection string runtime.

Qualquer divergência interrompe o deploy.

Não existe fallback pago.

## 16. Firewall temporário

O runner deve:

1. detectar seu IP público;
2. criar regra específica da execução no SQL logical server;
3. aplicar migration;
4. remover a regra em `finally`.

Nome sugerido:

~~~text
github-cd-<run-id>-<attempt>
~~~

Não usar nome fixo que gere colisão entre execuções.

## 17. Concurrency de produção

Usar grupo de concorrência:

~~~text
precificador-production
~~~

com:

~~~text
cancel-in-progress: false
~~~

Deploy já iniciado não deve ser cancelado por push posterior.

## 18. Migration passwordless

Connection string:

~~~text
Server=tcp:<server>.database.windows.net,1433;
Database=<database>;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=60;
~~~

A autenticação vem do OIDC/Azure CLI do runner.

## 19. Ordem da publicação

~~~text
preflight
-> firewall temporário
-> migration
-> remover firewall
-> deploy app.zip
-> smoke
~~~

Se migration falhar, não publicar aplicação.

Não executar migration Down automaticamente.

## 20. Compatibilidade sem slot

O ambiente F1 não possui deployment slot.

Migrations devem ser compatíveis com a versão imediatamente anterior durante a janela entre migration e publicação da nova aplicação.

Mudança destrutiva que exija troca atômica deve gerar plano específico posterior.

## 21. Deploy da Web App

Continuar usando mecanismo equivalente ao da MEL021:

~~~text
az webapp deploy
--type zip
--clean true
~~~

Não introduzir container.

## 22. Reutilização de `deploy.ps1`

`infra/azure/deploy.ps1` continua sendo a fonte operacional do deployment Azure.

Refatorar quando necessário para suportar:

### Manual

~~~text
operador
-> valida/build/test
-> migration
-> deploy
-> smoke
~~~

### CI/CD

~~~text
artefato já validado
-> migration bundle
-> deploy package
-> smoke
~~~

Não duplicar em YAML lógica crítica já mantida nos scripts Azure.

O deploy manual permanece como fallback.

## 23. Portabilidade

Scripts executados no GitHub Actions devem funcionar em PowerShell 7 no runner escolhido.

Evitar dependência exclusiva de Windows PowerShell, `tar.exe` ou caminhos somente Windows.

## 24. Smoke automático

Depois do deploy, aguardar resposta com retry limitado para cold start.

Validar pelo menos:

~~~text
GET /
GET /Conta/Login
~~~

Resultado esperado: resposta funcional e sem 5xx.

Não cadastrar dados em produção.

Não armazenar senha de usuário para smoke.

## 25. Setup e configurações funcionais

CD não executa Setup e não altera:

- SystemAdmin;
- `Bootstrap__SystemAdminKey`;
- Empresas;
- SMTP;
- URL pública;
- connection string runtime;
- Application Settings funcionais existentes.

Deploy de código não é reprovisionamento.

## 26. Custo controlado

Continuar exigindo:

~~~text
App Service Plan = F1
Azure SQL = Free Limit
Free exhaustion = AutoPause
~~~

Se não estiverem presentes, o deployment falha.

Não criar B1/S1/tier pago/`BillOverUsage` como fallback.

## 27. Logs e evidência

Não logar tokens, cookies, passwords, SMTP password ou Bootstrap key.

Ao concluir, gerar Step Summary com:

~~~text
Commit
Environment
Web App
Migration: OK
Deploy: OK
Smoke: OK
URL
~~~

## 28. Falhas

### Migration

- job vermelho;
- Web App atual permanece;
- firewall removido;
- novo ZIP não é publicado.

### Deploy

- job vermelho;
- sem rollback destrutivo do database.

### Smoke

5xx depois do deploy mantém o deployment como falha.

## 29. Rollback

Não implementar rollback automático de migration.

Rollback de aplicação pode ocorrer por novo deployment de artefato conhecido/revert commit, desde que compatível com o schema vigente.

Princípio:

~~~text
fail safe
-> preservar dados
-> intervenção consciente
~~~

## 30. Retry

Retry somente para falhas claramente transitórias, como:

- Azure SQL acordando;
- erro 40613;
- propagação curta de firewall;
- cold start HTTP.

Não repetir indefinidamente falhas de autorização, migration inválida ou custo/configuração divergente.

## 31. Fonte da produção

Somente SHA que alcançou `master` pode publicar automaticamente.

Não permitir deploy automático por feature branch, PR head, fork, tag arbitrária ou SHA manual.

`workflow_dispatch` para deploy arbitrário fica fora do escopo.

Rerun da execução já autorizada é permitido.

## 32. ADR

Criar:

~~~text
docs/architecture/adr/ADR-016-continuous-deployment-oidc-azure.md
~~~

Registrar a decisão:

~~~text
GitHub Actions
+ OIDC
+ identidade dedicada de deployment
+ artifact promotion
+ migration explícita
+ App Service ZIP deploy
~~~

Alternativas rejeitadas:

- publish profile;
- client secret;
- senha SQL;
- runtime Managed Identity executando DDL;
- rebuild no job de deploy;
- migration automática no startup.

## 33. Documentação

Atualizar:

- `infra/azure/README.md`: configuração única do CD, deploy automático, fallback manual, smoke e troubleshooting;
- README raiz: `master -> CI full -> CD Azure`;
- `docs/development/testing-strategy.md`: comportamento implementado;
- MEL021: apenas referência à evolução MEL027, preservando seu histórico;
- backlog.

MEL013 continua independente de Azure.

## Fora do escopo

- UC035;
- MEL014;
- staging;
- deployment slots;
- upgrade pago;
- custom domain;
- Application Insights;
- Key Vault;
- VNet/Private Endpoint;
- container/Kubernetes;
- alteração SMTP/Login/Setup;
- nova migration apenas para MEL027;
- migration automática no startup Production;
- rollback automático de banco;
- auto-merge;
- deploy de branch diferente de `master`.

## Critérios de aceitação

- **CA01:** documento normativo da MEL027 existe e estado é `Pronto`.
- **CA02:** MEL021, MEL028 e MEL013 são bases concluídas.
- **CA03:** UC035 permanece fora do escopo.
- **CA04:** `build-and-test` mantém nome e função de required check.
- **CA05:** PR docs-only continua usando otimização MEL028.
- **CA06:** PR full continua build/unit/integration.
- **CA07:** push em `master` executa gate full.
- **CA08:** deploy depende do sucesso de `build-and-test`.
- **CA09:** PR nunca publica no Azure.
- **CA10:** apenas `master` publica automaticamente.
- **CA11:** job usa environment `production`.
- **CA12:** autenticação Azure usa OIDC.
- **CA13:** job usa `id-token: write` e privilégio mínimo.
- **CA14:** nenhum client secret/publish profile/SQL password é criado para o pipeline.
- **CA15:** identidade CD é separada da runtime identity.
- **CA16:** runtime identity continua sem DDL.
- **CA17:** federação OIDC é restrita ao repo/environment.
- **CA18:** identidade CD não recebe Owner nem Contributor amplo sem necessidade.
- **CA19:** RBAC de Web App e SQL fica no menor escopo aplicável.
- **CA20:** identidade CD possui permissões SQL suficientes para migration sem `db_owner` por padrão.
- **CA21:** `configure-cd.ps1` é idempotente e não cria recurso pago.
- **CA22:** aplicação gera `app.zip` somente após gate full.
- **CA23:** migration bundle corresponde ao mesmo SHA.
- **CA24:** artefato é identificado pelo commit SHA.
- **CA25:** deploy consome o artefato da mesma execução.
- **CA26:** job de deploy não recompila nem repete a suíte full.
- **CA27:** preflight valida tenant/subscription/F1/Free Limit/AutoPause/HTTPS/Managed Identity.
- **CA28:** divergência de custo/configuração interrompe o deploy.
- **CA29:** firewall temporário é específico da execução e removido em `finally`.
- **CA30:** migration usa Microsoft Entra/passwordless.
- **CA31:** migration ocorre antes do ZIP deploy.
- **CA32:** falha de migration impede publicação.
- **CA33:** Production continua sem migration automática no startup.
- **CA34:** não existe migration Down automática.
- **CA35:** deploy usa o `app.zip` validado.
- **CA36:** Application Settings existentes são preservados.
- **CA37:** smoke roda após deploy com retry limitado.
- **CA38:** smoke não cadastra dados nem exige senha de usuário.
- **CA39:** resposta 5xx faz o deployment falhar.
- **CA40:** deployments são serializados e não cancelados durante execução.
- **CA41:** logs não expõem segredo.
- **CA42:** sucesso gera summary com SHA/ambiente/URL/status.
- **CA43:** scripts do runner são compatíveis com PowerShell 7.
- **CA44:** deploy manual permanece disponível como fallback.
- **CA45:** lógica crítica não é duplicada desnecessariamente no YAML.
- **CA46:** ADR-016 registra OIDC/artifact promotion.
- **CA47:** documentação Azure/README/testing strategy é atualizada.
- **CA48:** MEL021 permanece histórico da primeira publicação manual.
- **CA49:** MEL013 permanece independente de Azure.
- **CA50:** nenhuma regra de negócio ou schema é alterado.
- **CA51:** nenhum recurso pago é introduzido.
- **CA52:** App Service continua F1.
- **CA53:** Azure SQL continua Free/AutoPause.
- **CA54:** CI completa da PR de implementação fica verde.
- **CA55:** primeiro CD real pós-merge conclui migration/deploy/smoke.
- **CA56:** produção permanece acessível após o primeiro CD.
- **CA57:** nenhuma firewall rule temporária permanece.
- **CA58:** backlog marca MEL027 `Concluído` somente após o primeiro deployment automático real validado.
- **CA59:** após conclusão, UC035 é o próximo item da fila.

## Matriz mínima de validação

| Cenário | Resultado esperado |
|---|---|
| PR docs-only | MEL028; sem Azure |
| PR com código | full CI; sem deploy |
| build/unit/integration falha | sem deploy |
| push master saudável | artefato + migration + deploy + smoke |
| OIDC inválido | falha antes da migration |
| tenant/subscription incorreto | fail closed |
| F1/Free/AutoPause divergente | fail closed |
| migration falha | aplicação atual preservada |
| deploy falha | job vermelho; sem DB rollback automático |
| smoke 5xx | job vermelho |
| firewall criada | removida mesmo em exceção |
| dois pushes próximos | deploy serializado |

## Validação obrigatória da implementação

Antes do merge:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
git diff --check
~~~

Também validar:

- sintaxe do workflow;
- geração de `app.zip`;
- geração de migration bundle;
- ausência de segredos commitados;
- permissões do workflow;
- scripts Azure;
- ausência de recursos pagos.

Após merge, acompanhar o primeiro run real em `master` e confirmar OIDC, migration, remoção do firewall, ZIP deploy e smoke. Somente depois marcar MEL027 `Concluído`.

## Definition of Done

MEL027 está concluída quando um commit em `master` passa pela CI full, produz artefato imutável, autentica no Azure sem segredo de longa duração, aplica migrations explicitamente com identidade separada da aplicação, publica exatamente o artefato validado no App Service F1, executa smoke com sucesso e deixa o ambiente sem regra temporária de firewall.

O deploy manual da MEL021 permanece como fallback, mas deixa de ser o caminho normal.

## Branch de implementação

~~~text
infra/mel027-continuous-deployment
~~~

## Commit sugerido

~~~text
ci: automatiza continuous deployment no Azure
~~~
