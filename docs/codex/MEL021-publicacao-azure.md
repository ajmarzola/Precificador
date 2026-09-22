# Codex — MEL021 — Publicação Azure

> **Gate:** BLOQUEADO externamente. MEL020, MEL022, MEL023, UC032 e UC036 estão concluídas; a conta Azure ainda não possui assinatura utilizável. Só executar quando `docs/development/backlog.md` marcar MEL021 como `Pronto`.

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

Não executar provisionamento, scripts contra Azure ou mudanças de runtime enquanto o gate estiver bloqueado.

A ordem obrigatória é:

```text
UC032 -> UC036 -> MEL021
```

Além das dependências de código, é necessário que a conta Azure possua uma assinatura utilizável.

## Invariantes

- App Service: **F1/Free**;
- Azure SQL: **Free Limit + AutoPause**;
- confirmar no provisionamento a franquia gratuita vigente (atualmente 100.000 vCore-seconds/mês, 32 GB dados, 32 GB backup);
- App Service F1 é aceito conscientemente para uso pessoal/familiar, sem SLA e sem suporte Microsoft para workload de produção;
- nunca criar fallback pago;
- App Service -> Azure SQL por **Managed Identity**, sem senha;
- Azure SQL logical server com **Microsoft Entra-only + Entra administrator explícito**;
- se Entra-only não puder ser configurado, parar; não habilitar SQL password como fallback;
- App Service e Azure SQL obrigatoriamente na mesma região;
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

1. validar login/subscription;
2. criar/usar Resource Group;
3. escolher uma única região onde F1, runtime .NET 10 e Azure SQL Free estejam disponíveis;
4. validar runtime Linux .NET 10;
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
16. configurar firewall com outbound IPs da Web App;
17. falhar se qualquer etapa exigir tier pago.

Microsoft Entra-only é o caminho normativo. Não criar senha SQL como fallback automático.

Service Connector passwordless é aceitável se o resultado final respeitar o contrato da MEL021.

## Resiliência

Como a aplicação usa `UseSqlServer`, habilitar `EnableRetryOnFailure()` ou equivalente suportado pelo EF Core 10.

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
14. UC028 permanece Planejado.
