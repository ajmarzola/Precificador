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
- [`business/insumo-historical-stability.md`](business/insumo-historical-stability.md) — motivação e consequências da RN040 sobre estabilidade cadastral após histórico de preços.

### Arquitetura

- [`architecture/architecture.md`](architecture/architecture.md) — visão técnica da solução.
- [`architecture/adr/`](architecture/adr/) — registros de decisões arquiteturais.

### Funcionalidades

- [`features/`](features/) — documentação do comportamento oferecido por módulo funcional.

### Casos de uso

- [`use-cases/catalog.md`](use-cases/catalog.md) — backlog inicial e dependências.
- [`use-cases/template.md`](use-cases/template.md) — formato obrigatório para novos casos de uso.
- [`use-cases/UC001A-complementar-insumo-marca-observacao.md`](use-cases/UC001A-complementar-insumo-marca-observacao.md) — Marca e Observação do Insumo.
- [`use-cases/UC001B-generalizar-categoria-unidades-insumo.md`](use-cases/UC001B-generalizar-categoria-unidades-insumo.md) — generalização de Matéria-prima e unidades.
- [`use-cases/UC002-listar-consultar-insumos.md`](use-cases/UC002-listar-consultar-insumos.md) — listagem, pesquisa e detalhes tenant-aware.
- [`use-cases/UC038-solicitar-acesso-precificador.md`](use-cases/UC038-solicitar-acesso-precificador.md) — solicitação pública controlada de acesso.
- [`use-cases/UC040-ativar-conta-recuperar-acesso.md`](use-cases/UC040-ativar-conta-recuperar-acesso.md) — ativação da primeira senha e recuperação de acesso por token.
- [`use-cases/UC039-administrar-empresas-solicitacoes.md`](use-cases/UC039-administrar-empresas-solicitacoes.md) — administração global de solicitações e Empresas.
- [`use-cases/UC031-administrar-usuarios-vinculos.md`](use-cases/UC031-administrar-usuarios-vinculos.md) — administração tenant de usuários, perfis e vínculos.
- [`use-cases/UC033-administrar-colecoes.md`](use-cases/UC033-administrar-colecoes.md) — administração tenant-aware de Coleções comerciais.
- [`use-cases/UC034-vincular-produtos-colecoes.md`](use-cases/UC034-vincular-produtos-colecoes.md) — vínculo N:N de Produtos com Coleções.
- [`use-cases/UC037-configurar-identidade-visual-empresa.md`](use-cases/UC037-configurar-identidade-visual-empresa.md) — cor e logo tenant-aware da Empresa.

Cada caso de uso deve possuir um documento individual antes de sua implementação.

### Desenvolvimento

- [`development/execucao-teste-local.md`](development/execucao-teste-local.md) — guia operacional de preparação, Setup, Empresa, migrations, testes e diagnóstico local.
- [`development/foundation-technical.md`](development/foundation-technical.md) — FT001.
- [`development/foundation-multiempresa-auth.md`](development/foundation-multiempresa-auth.md) — FT002.
- [`development/foundation-administration-authorization.md`](development/foundation-administration-authorization.md) — FT003.
- [`development/definition-of-done.md`](development/definition-of-done.md)
- [`development/testing-strategy.md`](development/testing-strategy.md)
- [`development/workflow-codex.md`](development/workflow-codex.md)
- [`development/implementation-order.md`](development/implementation-order.md)
- [`development/melhorias.md`](development/melhorias.md) — backlog de melhorias não bloqueantes.
- [`development/improvements/MEL025-erros-paginas-nao-encontradas.md`](development/improvements/MEL025-erros-paginas-nao-encontradas.md) — fallback Web amigável para erros e páginas não encontradas.
- [`development/improvements/MEL026-home-publica.md`](development/improvements/MEL026-home-publica.md) — apresentação institucional responsiva da Home pública.
- [`development/improvements/MEL013-execucao-teste-local.md`](development/improvements/MEL013-execucao-teste-local.md) — especificação do guia de execução e testes locais.
- [`architecture/adr/ADR-015-logo-empresa-sql.md`](architecture/adr/ADR-015-logo-empresa-sql.md) — persistência do logo tenant no SQL Server/Azure SQL.

### Instruções para Codex

- [`codex/`](codex/) — instruções executáveis versionadas para cada incremento.
- [`codex/FT001-fundacao-tecnica.md`](codex/FT001-fundacao-tecnica.md)
- [`codex/FT002-fundacao-multiempresa-autenticacao.md`](codex/FT002-fundacao-multiempresa-autenticacao.md)
- [`codex/FT003-fundacao-administracao-autorizacao.md`](codex/FT003-fundacao-administracao-autorizacao.md)
- [`codex/UC001A-complementar-insumo-marca-observacao.md`](codex/UC001A-complementar-insumo-marca-observacao.md)
- [`codex/UC001B-generalizar-categoria-unidades-insumo.md`](codex/UC001B-generalizar-categoria-unidades-insumo.md)
- [`codex/UC002-listar-consultar-insumos.md`](codex/UC002-listar-consultar-insumos.md)
- [`codex/UC038-solicitar-acesso-precificador.md`](codex/UC038-solicitar-acesso-precificador.md)
- [`codex/UC040-ativar-conta-recuperar-acesso.md`](codex/UC040-ativar-conta-recuperar-acesso.md)
- [`codex/UC039-administrar-empresas-solicitacoes.md`](codex/UC039-administrar-empresas-solicitacoes.md)
- [`codex/UC031-administrar-usuarios-vinculos.md`](codex/UC031-administrar-usuarios-vinculos.md)
- [`codex/UC033-administrar-colecoes.md`](codex/UC033-administrar-colecoes.md)
- [`codex/UC034-vincular-produtos-colecoes.md`](codex/UC034-vincular-produtos-colecoes.md)
- [`codex/MEL025-erros-paginas-nao-encontradas.md`](codex/MEL025-erros-paginas-nao-encontradas.md)
- [`codex/MEL026-home-publica.md`](codex/MEL026-home-publica.md)
- [`codex/UC037-identidade-visual-empresa.md`](codex/UC037-identidade-visual-empresa.md)
- [`codex/MEL013-execucao-teste-local.md`](codex/MEL013-execucao-teste-local.md)

A especificação normativa e a instrução para o agente são documentos distintos: a especificação define **o que deve ser verdadeiro**; a instrução orienta **como executar a entrega sem extrapolar o escopo**.

## Hierarquia conceitual

- **Funcionalidade**: o que o sistema oferece ao usuário.
- **Caso de uso**: uma interação ou comportamento implementável e verificável.
- **Regra de negócio**: condição que deve permanecer verdadeira independentemente da interface.
- **Fundação técnica**: incremento não funcional necessário para habilitar os casos de uso.
- **ADR**: decisão arquitetural com contexto, consequência e status.

Evite duplicar a mesma regra em vários documentos. Casos de uso devem referenciar as RNs aplicáveis.
