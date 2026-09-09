# ADR-001 — Monólito web local-first com Razor Pages

- **Status:** Aceito
- **Data:** 2026-09-09

## Contexto

O projeto deve ser simples de implementar e manter em sessões curtas, com custo de hospedagem e banco igual a zero no MVP. A interface precisa ser produtiva para desenvolvimento .NET e não há necessidade atual de múltiplos clientes ou API pública.

## Decisão

Construir o Precificador como monólito ASP.NET Core usando Razor Pages, executado inicialmente de forma local e acessado pelo navegador.

## Consequências

### Positivas

- implantação local simples;
- baixa quantidade de projetos e componentes;
- desenvolvimento rápido de CRUDs e fluxos administrativos;
- possibilidade futura de publicação web sem reescrever regras de negócio.

### Negativas

- acesso inicial restrito à máquina em que a aplicação executa;
- não há separação entre frontend e backend como produtos independentes.

## Não decidido por este ADR

Hospedagem futura, autenticação e acesso multiusuário permanecem fora do MVP.
