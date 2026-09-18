# Codex — MEL021 — Publicação Azure

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

## Invariantes

- App Service: **F1/Free**;
- Azure SQL: **Free Limit + AutoPause**;
- nunca criar fallback pago;
- App Service -> Azure SQL por **Managed Identity**, sem senha;
- `ConnectionStrings__Precificador` é a configuração do Azure;
- Production não executa `Database.MigrateAsync()` automaticamente;
- migrations são etapa explícita de deploy;
- domínio inicial é `*.azurewebsites.net`;
- sem custom domain;
- sem App Insights, Key Vault, VNet, Private Endpoint, ACR ou container;
- não migrar dados locais;
- UC028 não entra no diff.

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

1. validar login/subscription;
2. criar/usar Resource Group;
3. validar que o plano escolhido é gratuito;
4. criar App Service Plan F1 Linux;
5. criar Web App .NET 10;
6. habilitar HTTPS Only;
7. habilitar System Assigned Managed Identity;
8. criar Azure SQL logical server;
9. criar banco GeneralPurpose Serverless com `--use-free-limit`;
10. usar `--free-limit-exhaustion-behavior AutoPause`;
11. configurar conexão passwordless;
12. configurar firewall com outbound IPs da Web App;
13. falhar se qualquer etapa exigir tier pago.

Preferir Microsoft Entra-only no SQL.

Service Connector passwordless é aceitável se o resultado final respeitar o contrato da MEL021.

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

- validar login/subscription;
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

1. `dotnet restore Precificador.slnx`;
2. `dotnet build Precificador.slnx --configuration Release --no-restore`;
3. `dotnet test Precificador.slnx --configuration Release --no-build`;
4. App Service F1 confirmado;
5. SQL Free + AutoPause confirmado;
6. Managed Identity confirmada;
7. Production sem auto-migrate;
8. smoke Azure concluído;
9. scripts sem segredo;
10. MEL021 -> Concluído;
11. UC028 permanece Planejado.
