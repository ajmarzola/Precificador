# ADR-003 — .NET 10 LTS

- **Status:** Aceito
- **Data:** 2026-09-09

## Contexto

A solution será criada do zero em setembro de 2026. O projeto prioriza estabilidade, suporte prolongado e manutenção simples.

## Decisão

Adotar .NET 10 LTS para os projetos de produção e teste.

## Fundamentação

Na data da decisão, .NET 10 é a versão LTS ativa. A política oficial da Microsoft informa lançamento em novembro de 2025 e suporte até novembro de 2028.

Referência: https://dotnet.microsoft.com/platform/support/policy

## Consequências

- usar versões de ASP.NET Core e EF Core compatíveis com .NET 10;
- manter o runtime/SDK atualizado dentro da linha 10.x suportada;
- não migrar para versões preview ou STS apenas por novidade.
