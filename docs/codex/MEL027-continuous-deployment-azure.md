# Instrução Codex — MEL027 Continuous Deployment no Azure

Você está implementando a MEL027 no repositório ajmarzola/Precificador.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/development/improvements/MEL027-continuous-deployment-azure.md;
3. docs/development/improvements/MEL021-publicacao-azure.md;
4. docs/development/improvements/MEL028-otimizar-ci-tipo-alteracao.md;
5. docs/development/execucao-teste-local.md;
6. docs/development/testing-strategy.md;
7. docs/development/definition-of-done.md;
8. docs/development/backlog.md;
9. infra/azure/README.md;
10. infra/azure/provision.ps1;
11. infra/azure/deploy.ps1;
12. .github/workflows/ci.yml;
13. global.json;
14. .config/dotnet-tools.json;
15. README.md.

A especificação normativa é:

~~~text
docs/development/improvements/MEL027-continuous-deployment-azure.md
~~~

## Branch

Antes de alterar arquivos:

1. parta da `master` atualizada;
2. crie/troque para `infra/mel027-continuous-deployment`;
3. confirme que a branch atual não é `master`;
4. somente então implemente.

Não faça merge em `master`.

## Objetivo operacional

Transformar o deploy manual reproduzível da MEL021 em Continuous Deployment seguro:

~~~text
push master
-> CI full
-> artefato imutável
-> OIDC Azure
-> preflight
-> migration explícita
-> ZIP deploy
-> smoke
~~~

Não crie um segundo modelo de publicação paralelo à MEL021.

## Princípios obrigatórios

- preservar `build-and-test`;
- PR nunca faz deploy;
- `master` sempre passa por full gate;
- build once / deploy same artifact;
- autenticação Azure por OIDC;
- identidade CD separada da runtime identity;
- migrations explícitas;
- sem segredo Azure de longa duração;
- sem SQL password;
- sem fallback pago;
- deploy manual continua disponível como fallback.

## Workflow

Preferir evoluir `.github/workflows/ci.yml`.

Adicionar job equivalente a:

~~~text
deploy-production
needs: build-and-test
if: push em master
environment: production
~~~

Não remover a classificação MEL028 de PR.

Não usar `workflow_run` sem necessidade.

Deployment deve ter concurrency própria, serializada, com `cancel-in-progress: false`.

## Artefato

No job full de `master`, depois de build/test:

1. `dotnet publish` Release com `--no-build`;
2. gerar `app.zip`;
3. gerar EF Core Migration Bundle do mesmo SHA;
4. upload de um artefato identificado pelo commit SHA.

O deploy deve baixar esse artefato.

Não recompilar no job de deploy.

Não repetir a suíte full no job de deploy.

## OIDC

Configurar o workflow para usar OIDC.

Permissões mínimas:

~~~text
contents: read
id-token: write
~~~

Não usar:

- publish profile;
- AZURE_CREDENTIALS;
- client secret;
- write-all;
- SQL password.

## Bootstrap Azure

Criar:

~~~text
infra/azure/configure-cd.ps1
~~~

O script deve ser idempotente e:

- validar tenant/subscription;
- criar/localizar User Assigned Managed Identity dedicada;
- criar/validar federated identity credential restrita ao repo/environment production;
- aplicar RBAC mínimo à Web App e SQL logical server;
- preparar usuário SQL de migration;
- imprimir apenas identificadores não secretos que precisam ser configurados no GitHub;
- não criar recurso pago.

Não promover a System Assigned Managed Identity da Web App para DDL.

## RBAC

Alvo esperado:

~~~text
Web App específica -> Website Contributor
SQL logical server -> SQL Security Manager
~~~

Evitar Owner e Contributor no Resource Group.

Se os nomes/escopos exatos das roles na subscription divergirem, descubra/valide pela Azure CLI; não troque por permissão ampla como atalho.

## SQL migration identity

Criar usuário Entra da identidade CD no database com permissões mínimas suficientes para EF migrations.

Base esperada:

~~~text
db_ddladmin
db_datareader
db_datawriter
~~~

Não usar `db_owner` por conveniência.

## Preflight

Antes da migration, preservar/centralizar os guards atuais:

- tenant correto;
- subscription correta;
- App Service Plan F1;
- Azure SQL Free Limit;
- AutoPause;
- HTTPS Only;
- runtime connection string com Managed Identity;
- ausência de User ID/Password runtime.

Qualquer divergência deve falhar fechado.

## Firewall temporário

O runner precisa de acesso ao Azure SQL para migration.

Use regra específica por run:

~~~text
github-cd-<run-id>-<attempt>
~~~

Crie antes da migration e remova em `finally`.

Nunca deixe regra temporária após sucesso ou falha.

## Migration

Use Migration Bundle passwordless com `Authentication=Active Directory Default`.

Ordem:

~~~text
preflight
-> firewall
-> migration
-> firewall cleanup
-> deploy
-> smoke
~~~

Falha de migration impede deploy.

Não executar Down automaticamente.

## Deploy

Reutilize/refatore `infra/azure/deploy.ps1` em vez de copiar toda a lógica crítica para YAML.

O caminho CI deve consumir artefato já construído/testado.

O caminho manual deve continuar funcional.

Garanta compatibilidade PowerShell 7 no runner.

Evite `tar.exe`/paths Windows-only se o runner escolhido não for Windows.

## Smoke

Após deploy, validar com retry limitado para cold start:

~~~text
GET /
GET /Conta/Login
~~~

Falhar se houver 5xx persistente.

Não criar dados, não autenticar usuário de produção e não executar Setup.

## GitHub Environment/configuração

Usar environment:

~~~text
production
~~~

O workflow deve consumir identificadores como:

~~~text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
AZURE_RESOURCE_GROUP
AZURE_WEBAPP_NAME
AZURE_SQL_SERVER
AZURE_SQL_DATABASE
AZURE_APP_SERVICE_PLAN
~~~

Não versionar valores pessoais ou segredo.

Se a ferramenta disponível não permitir configurar Environment/variables/federation diretamente, implemente o código/scripts e documente claramente no handoff quais passos administrativos precisam ser executados antes do merge/deploy real. Não invente configuração concluída.

## ADR e documentação

Criar:

~~~text
docs/architecture/adr/ADR-016-continuous-deployment-oidc-azure.md
~~~

Atualizar:

- infra/azure/README.md;
- README.md;
- docs/development/testing-strategy.md;
- MEL021 somente com referência à evolução, sem reescrever histórico;
- MEL027 -> Concluído apenas conforme regra abaixo;
- backlog.

## Estado da MEL027

Na implementação, não marque `Concluído` prematuramente se o primeiro deployment automático real ainda não aconteceu.

O estado final exige evidência pós-merge:

~~~text
CI full master = verde
OIDC = OK
migration = OK
firewall cleanup = OK
ZIP deploy = OK
smoke = OK
~~~

Se a PR precisa ser mergeada para permitir esse primeiro run, deixe o handoff explícito e faça a alteração normativa para `Concluído` somente quando houver evidência real, conforme a especificação.

## Fora do escopo

Não:

- implementar UC035;
- criar staging/slots;
- subir tier;
- adicionar Application Insights/Key Vault/VNet;
- containerizar;
- usar Kubernetes;
- alterar login/Setup/SMTP;
- criar migration de negócio;
- habilitar migration automática em Production;
- implementar rollback automático de database;
- permitir deploy de feature branch/PR;
- criar workflow_dispatch de SHA arbitrário.

## Validação local obrigatória

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
git diff --check
~~~

Além disso:

- validar sintaxe do workflow;
- produzir `app.zip`;
- produzir migration bundle;
- revisar permissões do workflow;
- procurar segredos acidentalmente versionados;
- conferir que nenhuma configuração paga foi introduzida;
- conferir que PR não dispara deploy.

## Handoff

Relate:

- arquivos alterados;
- desenho CI/CD final;
- OIDC/RBAC efetivamente configurado ou passos administrativos pendentes;
- resultado do full gate;
- resultado da geração de artefatos;
- CI da PR;
- o que ainda depende do primeiro run pós-merge;
- nenhuma credencial ou segredo em texto.

Não faça merge em master.
