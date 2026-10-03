# ADR-016 — Continuous Deployment por OIDC no Azure

- **Status:** Aceito

## Contexto

A MEL021 publica o piloto em App Service Linux F1 e Azure SQL Free/AutoPause. A MEL028 mantém `build-and-test` como required check e distingue PR documental de PR full. A MEL027 automatiza a publicação após push em `master`, sem alterar domínio, schema ou custo do piloto.

## Decisão

Evoluir `ci.yml`: `master` sempre executa restore/build/unit/integration, publica Release com `--no-build` e gera migration bundle self-contained `linux-x64`. Um artefato identificado pelo SHA contém ZIP, bundle, configuração vazia de suporte e manifesto de hashes. O job `deploy-production` baixa o artefato da mesma execução e não recompila.

O environment `production` restringe deployments a `master`, sem aprovação obrigatória por execução. GitHub OIDC usa uma User Assigned Managed Identity dedicada, com subject `repo:ajmarzola/Precificador:environment:production`, issuer GitHub e audience `api://AzureADTokenExchange`.

RBAC: Website Contributor somente na Web App; SQL Security Manager somente no SQL logical server; Reader somente no App Service Plan para o preflight F1. A permissão de leitura no plano é necessária porque a atribuição na Web App não cobre o recurso irmão do plano. O bootstrap descobre e valida as roles built-in; nunca amplia para Owner/Contributor no Resource Group.

O usuário SQL CD usa SID derivado do **client ID**, com `db_ddladmin`, `db_datareader` e `db_datawriter`. A identidade runtime permanece separada, sem DDL. O bootstrap administrativo é idempotente e falha diante de confiança, SID ou permissões divergentes.

`deploy.ps1` concentra os dois caminhos: manual com gate local e geração de pacote; CD com pacote já testado. O preflight valida contexto, plano efetivo F1, Free/AutoPause, HTTPS e conexão runtime Managed Identity no mesmo destino SQL, sem credenciais. Em seguida abre firewall específico da execução, executa bundle passwordless e remove/confirma remoção em `finally`, antes do ZIP deploy. O workflow tem cleanup adicional com `always()`.

Deployments usam concurrency `precificador-production` e `cancel-in-progress: false`. O smoke faz somente GET anônimo de `/` e `/Conta/Login`, com seis tentativas limitadas por rota. Sucesso registra SHA, ambiente, Web App, migration, cleanup, deploy, smoke e URL no Step Summary.

## Alternativas rejeitadas

- Publish profile, client secret e senha SQL: segredos de longa duração desnecessários.
- Runtime Managed Identity com DDL e migration no startup Production: mistura de privilégios e publicação implícita.
- Rebuild no deploy: perde a promoção do artefato testado.
- `workflow_run`, deploy de PR/feature branch ou SHA arbitrário: adicionam caminhos de autorização fora do contrato.
- Slots, tiers pagos e rollback automático de database: fora do piloto e da MEL027.

## Consequências

O bootstrap Azure e a configuração do environment são administrativos e antecedem o primeiro run pós-merge. F1 não tem slot; migrations futuras devem ser compatíveis com a aplicação imediatamente anterior. Falha de migration ou cleanup impede ZIP deploy; falha de deploy/smoke mantém job vermelho, sem executar Down. O fallback manual continua disponível em PowerShell 7.

`finally` e `always()` cobrem falhas normais e interrupções em que o runner ainda executa cleanup. Perda total do runner/processo pode exigir remoção administrativa da regra específica; essa ocorrência bloqueia a conclusão operacional até haver evidência de cleanup. Não existe garantia de cleanup executável em uma máquina destruída.

GitHub concurrency serializa jobs em execução, mas pode substituir um job ainda pendente por outro push; não é uma fila FIFO de todos os commits. Cada deployment que inicia promove o artefato da própria execução autorizada.

## Referências

- [Azure Login OIDC](https://github.com/Azure/login#login-with-openid-connect-oidc-recommended)
- [Federação de User Assigned Managed Identity](https://learn.microsoft.com/entra/workload-id/workload-identity-federation-create-trust-user-assigned-managed-identity)
- [Website Contributor](https://learn.microsoft.com/azure/role-based-access-control/built-in-roles/web-and-mobile#website-contributor)
- [SQL Security Manager](https://learn.microsoft.com/azure/role-based-access-control/built-in-roles/databases#sql-security-manager)
- [EF migration bundles](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#bundles)
