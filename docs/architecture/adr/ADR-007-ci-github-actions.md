# ADR-007 — CI com GitHub Actions

- **Status:** Aceito
- **Data:** 2026-09-09

## Contexto

A implementação será realizada incrementalmente, inclusive com agentes de código. A Definition of Done exige restore, build e testes verdes. Executar essas verificações apenas localmente não impede que um PR seja aberto com regressões ou ambiente incompleto.

O repositório é público e runners padrão hospedados pelo GitHub podem ser usados sem cobrança de minutos para repositórios públicos conforme a documentação vigente.

Referência oficial: https://docs.github.com/actions/concepts/billing-and-usage

## Decisão

Criar um workflow simples no GitHub Actions executado em:

- pull requests para `master`;
- pushes em `master`.

Usar `ubuntu-latest` e executar restore, build Release e testes da solution.

## Consequências

### Positivas

- cada PR recebe validação independente do ambiente local/Codex;
- regressões de compilação e testes ficam visíveis antes do merge;
- baixo custo de manutenção;
- nenhuma infraestrutura própria é necessária.

### Negativas

- PRs aguardam execução de CI;
- eventual indisponibilidade do GitHub Actions pode atrasar validação.

## Limites

Nesta primeira versão, CI não inclui:

- deploy;
- publicação de artefatos;
- coverage gate;
- análise de segurança adicional;
- runners pagos/maiores.
