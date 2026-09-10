# Documentação do Precificador

Esta pasta é a fonte de verdade funcional e arquitetural do projeto.

## Estrutura

### Produto

- [`product/vision.md`](product/vision.md) — problema, objetivos e princípios do produto.
- [`product/scope.md`](product/scope.md) — escopo do MVP e itens explicitamente excluídos.
- [`product/glossary.md`](product/glossary.md) — vocabulário comum do domínio.
- [`product/multiempresa-generalizacao.md`](product/multiempresa-generalizacao.md) — direção aprovada para multiempresa, ficha técnica genérica e classificação de Insumos.

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
- [`use-cases/UC001B-generalizar-categoria-unidades-insumo.md`](use-cases/UC001B-generalizar-categoria-unidades-insumo.md) — generalização de Matéria-prima e unidades antes do UC001A/UC002.

Cada caso de uso deve possuir um documento individual antes de sua implementação.

### Desenvolvimento

- [`development/foundation-technical.md`](development/foundation-technical.md) — FT001.
- [`development/foundation-multiempresa-auth.md`](development/foundation-multiempresa-auth.md) — FT002.
- [`development/definition-of-done.md`](development/definition-of-done.md)
- [`development/testing-strategy.md`](development/testing-strategy.md)
- [`development/workflow-codex.md`](development/workflow-codex.md)
- [`development/implementation-order.md`](development/implementation-order.md)

### Instruções para Codex

- [`codex/`](codex/) — instruções executáveis versionadas para cada incremento.
- [`codex/FT001-fundacao-tecnica.md`](codex/FT001-fundacao-tecnica.md)
- [`codex/FT002-fundacao-multiempresa-autenticacao.md`](codex/FT002-fundacao-multiempresa-autenticacao.md)

A especificação normativa e a instrução para o agente são documentos distintos: a especificação define **o que deve ser verdadeiro**; a instrução orienta **como executar a entrega sem extrapolar o escopo**.

## Hierarquia conceitual

- **Funcionalidade**: o que o sistema oferece ao usuário.
- **Caso de uso**: uma interação ou comportamento implementável e verificável.
- **Regra de negócio**: condição que deve permanecer verdadeira independentemente da interface.
- **Fundação técnica**: incremento não funcional necessário para habilitar os casos de uso.
- **ADR**: decisão arquitetural com contexto, consequência e status.

Evite duplicar a mesma regra em vários documentos. Casos de uso devem referenciar as RNs aplicáveis.
