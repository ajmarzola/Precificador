# ADR-010 — Hospedagem Azure de baixo custo

- **Status:** Aceito
- **Data:** 2026-09-18
- **Relacionado:** [MEL020](../../development/improvements/MEL020-sqlserver.md), [MEL021](../../development/improvements/MEL021-publicacao-azure.md)

## Contexto

A MEL020 substituiu SQLite por SQL Server e preparou a persistência para Azure SQL.

O objetivo seguinte é permitir acesso remoto eventual pelo próprio usuário e por sua esposa, sem intenção atual de escala comercial e com requisito forte de custo operacional próximo de zero.

A carga prevista é baixa e esporádica. SLA, custom domain, autoscale e infraestrutura de rede dedicada não são requisitos atuais.

## Decisão

Publicar o monólito ASP.NET Core em:

```text
Azure App Service Free F1
+
Azure SQL Database Free offer
```

Adotar:

- Linux App Service;
- .NET 10;
- domínio padrão `azurewebsites.net`;
- Azure SQL General Purpose Serverless com Free Limit;
- `AutoPause` ao esgotar a franquia gratuita;
- System Assigned Managed Identity para App Service -> Azure SQL;
- migrations explícitas fora de Development;
- scripts Azure CLI/PowerShell versionados;
- deploy manual reproduzível.

Não criar fallback automático para tiers pagos.

## Consequências positivas

- acesso remoto sem servidor próprio;
- custo alvo zero no cenário atual;
- mesmo provider SQL Server em desenvolvimento/testes/Azure;
- conexão ao banco sem senha persistida;
- arquitetura simples e compatível com o monólito;
- possibilidade futura de escalar sem reescrever o Core.

## Consequências negativas aceitas

- F1 não possui SLA;
- aplicação pode sofrer cold start;
- compute é compartilhado e limitado;
- custom domain não faz parte da solução inicial;
- Azure SQL Free possui franquia mensal;
- ao atingir a franquia, o banco pode permanecer pausado até o próximo mês;
- rede não usa Private Endpoint/VNet nesta fase;
- publicação permanece manual.

## Segurança

Azure SQL usa Microsoft Entra/Managed Identity para o runtime.

A identidade da Web App recebe apenas permissões de leitura/escrita.

Permissões DDL ficam com o operador que aplica migrations.

## Evoluções futuras

Dependem de decisão explícita:

- upgrade do App Service;
- custom domain;
- CI/CD;
- Application Insights;
- VNet/Private Endpoint;
- Key Vault;
- alta disponibilidade;
- tier pago.

Essas evoluções devem responder a uso real e não ser antecipadas no MVP.
