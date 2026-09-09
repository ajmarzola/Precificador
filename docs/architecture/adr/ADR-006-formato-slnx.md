# ADR-006 — Formato SLNX para a solution

- **Status:** Aceito
- **Data:** 2026-09-09

## Contexto

O Precificador será iniciado em .NET 10. A partir do .NET 10, o comando `dotnet new sln` passou a criar solution no formato SLNX por padrão. O formato é suportado pelo SDK atual e é mais simples de ler e manter em controle de versão.

Referência oficial: https://learn.microsoft.com/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default

## Decisão

Utilizar:

```text
Precificador.slnx
```

como arquivo principal da solution.

## Consequências

### Positivas

- acompanha o padrão nativo do SDK .NET 10;
- formato textual mais enxuto;
- diffs de adição/remoção de projetos mais simples;
- evita forçar formato legado sem necessidade.

### Negativas

- ferramentas antigas que não suportem SLNX não serão adequadas ao projeto.

## Alternativa rejeitada

Forçar `Precificador.sln` com `dotnet new sln --format sln`. Não há necessidade conhecida de compatibilidade com tooling antigo que justifique essa opção.
