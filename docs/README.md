# Documentação do Precificador

Esta pasta é a fonte de verdade funcional e arquitetural do projeto.

## Estrutura

### Produto

- [`product/vision.md`](product/vision.md) — problema, objetivos e princípios do produto.
- [`product/scope.md`](product/scope.md) — escopo do MVP e itens explicitamente excluídos.
- [`product/glossary.md`](product/glossary.md) — vocabulário comum do domínio.

### Negócio

- [`business/business-rules.md`](business/business-rules.md) — regras normativas identificadas por RN.
- [`business/pricing-model.md`](business/pricing-model.md) — composição de custos, margem e preço sugerido.

### Arquitetura

- [`architecture/architecture.md`](architecture/architecture.md) — visão técnica da solução.
- [`architecture/adr/`](architecture/adr/) — registros de decisões arquiteturais.

### Funcionalidades

- [`features/`](features/) — documentação do comportamento oferecido por módulo funcional.

### Casos de uso

- [`use-cases/catalog.md`](use-cases/catalog.md) — backlog inicial e dependências.
- [`use-cases/template.md`](use-cases/template.md) — formato obrigatório para novos casos de uso.

Cada caso de uso deve possuir um documento individual antes de sua implementação.

### Desenvolvimento

- [`development/definition-of-done.md`](development/definition-of-done.md)
- [`development/testing-strategy.md`](development/testing-strategy.md)
- [`development/workflow-codex.md`](development/workflow-codex.md)
- [`development/implementation-order.md`](development/implementation-order.md)

## Hierarquia conceitual

- **Funcionalidade**: o que o sistema oferece ao usuário.
- **Caso de uso**: uma interação ou comportamento implementável e verificável.
- **Regra de negócio**: condição que deve permanecer verdadeira independentemente da interface.
- **ADR**: decisão arquitetural com contexto, consequência e status.

Evite duplicar a mesma regra em vários documentos. Casos de uso devem referenciar as RNs aplicáveis.
