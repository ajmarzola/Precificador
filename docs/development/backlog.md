# Backlog

Este arquivo é a única fonte normativa para estado, gate/dependência operacional, ordem da fila principal e prioridade das melhorias não bloqueantes.

Estados permitidos:

- Planejado
- Especificado
- Pronto
- Concluído
- Descartado

Não registrar estado `Em andamento`. Branch e Pull Request representam atividade transitória.

## Fila principal

A ordem das linhas define a fila normativa.

| Item | Tipo | Estado | Gate / Dependência | Documento |
|---|---|---|---|---|
| FT001 — Fundação Técnica | FT | Concluído | — | [foundation-technical.md](foundation-technical.md) |
| UC001 — Cadastrar Insumo | UC | Concluído | FT001 | [UC001-cadastrar-insumo.md](../use-cases/UC001-cadastrar-insumo.md) |
| FT002 — Fundação Multiempresa e Autenticação | FT | Concluído | FT001, UC001 | [foundation-multiempresa-auth.md](foundation-multiempresa-auth.md) |
| UC001B — Generalizar categoria e unidades de insumo | UC | Concluído | UC001, FT002 | [UC001B-generalizar-categoria-unidades-insumo.md](../use-cases/UC001B-generalizar-categoria-unidades-insumo.md) |
| UC001A — Complementar cadastro com marca e observação | UC | Concluído | UC001B, FT002 | [UC001A-complementar-insumo-marca-observacao.md](../use-cases/UC001A-complementar-insumo-marca-observacao.md) |
| UC002 — Listar e consultar Insumos | UC | Concluído | UC001A, FT002 | [UC002-listar-consultar-insumos.md](../use-cases/UC002-listar-consultar-insumos.md) |
| UC003 — Editar Insumo | UC | Concluído | UC002, UC001A, FT002 | [UC003-editar-insumo.md](../use-cases/UC003-editar-insumo.md) |
| UC004 — Desativar e reativar Insumo | UC | Concluído | UC003, FT002 | [UC004-desativar-reativar-insumo.md](../use-cases/UC004-desativar-reativar-insumo.md) |
| UC005 — Registrar preço de Insumo | UC | Concluído | UC004, FT002, RN040 | [UC005-registrar-preco-insumo.md](../use-cases/UC005-registrar-preco-insumo.md) |
| MEL006 — Timezone e data operacional da Empresa | MEL | Concluído | FT002; antes do UC006 | [MEL006-timezone-empresa.md](improvements/MEL006-timezone-empresa.md) |
| UC006 — Consultar histórico de preços do insumo | UC | Concluído | UC005, MEL006 | [UC006-consultar-historico-precos-insumo.md](../use-cases/UC006-consultar-historico-precos-insumo.md) |
| UC007 — Cadastrar Produto | UC | Concluído | FT002; UC006 mergeado | [UC007-cadastrar-produto.md](../use-cases/UC007-cadastrar-produto.md) |
| UC008 — Listar e consultar Produtos | UC | Concluído | UC007 | [UC008-listar-consultar-produtos.md](../use-cases/UC008-listar-consultar-produtos.md) |
| UC009 — Editar Produto | UC | Concluído | UC007; após UC008 | [UC009-editar-produto.md](../use-cases/UC009-editar-produto.md) |
| UC010 — Desativar e reativar Produto | UC | Concluído | UC007, UC008, UC009 | [UC010-desativar-reativar-produto.md](../use-cases/UC010-desativar-reativar-produto.md) |
| UC013 — Definir rendimento e tempo ativo da Ficha Técnica | UC | Concluído | UC007 a UC010, FT002 | [UC013-definir-base-ficha-tecnica.md](../use-cases/UC013-definir-base-ficha-tecnica.md) |
| UC014 — Adicionar Insumo à Ficha Técnica | UC | Concluído | UC013, UC001A a UC006, FT002 | [UC014-adicionar-insumo-ficha.md](../use-cases/UC014-adicionar-insumo-ficha.md) |
| UC015 — Alterar item da Ficha Técnica | UC | Concluído | UC014 | [UC015-alterar-item-ficha.md](../use-cases/UC015-alterar-item-ficha.md) |
| MEL010 — Identidade consolidada do Insumo | MEL | Concluído | UC005, UC014, UC015 | [MEL010-identidade-consolidada-insumo.md](improvements/MEL010-identidade-consolidada-insumo.md) |
| UC016 — Remover item da Ficha Técnica | UC | Concluído | — | [UC016-remover-item-ficha.md](../use-cases/UC016-remover-item-ficha.md) |
| UC017 — Consultar Ficha Técnica e composição | UC | Concluído | — | [UC017-consultar-ficha-composicao.md](../use-cases/UC017-consultar-ficha-composicao.md) |
| MEL009 — Reserva comercial do Desconto de referência | MEL | Especificado | Implementação distribuída em UC026, UC027, UC011 e UC012 | [MEL009-reserva-comercial-desconto.md](improvements/MEL009-reserva-comercial-desconto.md) |
| UC026 — Consultar configurações de precificação da Empresa | UC | Concluído | — | [UC026-consultar-configuracoes-precificacao.md](../use-cases/UC026-consultar-configuracoes-precificacao.md) |
| UC027 — Alterar configurações de precificação da Empresa | UC | Concluído | — | [UC027-alterar-configuracoes-precificacao.md](../use-cases/UC027-alterar-configuracoes-precificacao.md) |
| UC018 — Calcular custo atual dos itens do lote | UC | Concluído | — | [UC018-calcular-custo-itens-lote.md](../use-cases/UC018-calcular-custo-itens-lote.md) |
| UC020 — Calcular custo de mão de obra | UC | Pronto | — | [UC020-calcular-custo-mao-de-obra.md](../use-cases/UC020-calcular-custo-mao-de-obra.md) |
| UC021 — Calcular custo de energia/equipamentos | UC | Planejado | UC013, UC027; revalidar antes de implementar | Documento a criar |
| UC019 — Calcular perdas aplicáveis | UC | Planejado | UC018; revalidar antes de implementar | Documento a criar |
| UC022 — Calcular custo total e custo unitário | UC | Planejado | UC018 a UC021 | Documento a criar |
| UC023 — Calcular preço teórico e sugerido | UC | Planejado | UC022, UC027 | Documento a criar |
| UC011 — Registrar preço de prateleira preservando snapshot de precificação | UC | Planejado | UC023; incorporar MEL009 | Documento a criar |
| UC012 — Consultar histórico de precificação do Produto | UC | Planejado | UC011; incorporar MEL009 | Documento a criar |
| UC024 — Calcular margem atual e situação | UC | Planejado | UC011, UC022 | Documento a criar |
| UC025 — Consultar detalhamento da precificação | UC | Planejado | UC023, UC024 | Documento a criar |
| UC028 — Consultar resumo de margens da Empresa Ativa | UC | Planejado | UC024 | Documento a criar |
| UC029 — Filtrar produtos abaixo da margem | UC | Planejado | UC028 | Documento a criar |
| UC030 — Identificar produtos com precificação incompleta | UC | Planejado | UC017, UC028 | Documento a criar |

## Melhorias não bloqueantes

| Item | Estado | Prioridade | Gate / Dependência | Documento |
|---|---|---|---|---|
| MEL001 — Teste explícito da FK Insumo → Empresa | Concluído | baixa | FT002 | [MEL001-teste-fk-insumo-empresa.md](improvements/MEL001-teste-fk-insumo-empresa.md) |
| MEL002 — Teste de tentativa de reatribuição de tenant no domínio | Concluído | baixa | FT002 | [MEL002-reatribuicao-tenant-insumo.md](improvements/MEL002-reatribuicao-tenant-insumo.md) |
| MEL003 — Assert direto de limpeza da Empresa Ativa no logout | Concluído | baixa | FT002 | [MEL003-limpeza-empresa-logout.md](improvements/MEL003-limpeza-empresa-logout.md) |
| MEL004 — Centralizar rótulos de CategoriaInsumo e UnidadeMedida na UI | Concluído | baixa | UC001B, UC002 | [MEL004-rotulos-insumos-ui.md](improvements/MEL004-rotulos-insumos-ui.md) |
| MEL005 — Centralizar entrada e validação de Insumo entre Novo e Editar | Concluído | baixa | UC003, UC005 | [MEL005-centralizar-formulario-insumo.md](improvements/MEL005-centralizar-formulario-insumo.md) |
| MEL007 — Centralizar estado e ordem do backlog | Concluído | média-baixa | — | [MEL007-centralizar-estado-backlog.md](improvements/MEL007-centralizar-estado-backlog.md) |
| MEL008 — Infraestrutura Web tenant-aware de testes | Concluído | baixa | — | [MEL008-testes-web-tenant-aware.md](improvements/MEL008-testes-web-tenant-aware.md) |
