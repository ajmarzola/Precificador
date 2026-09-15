# Catálogo Funcional de Casos de Uso

Este catálogo organiza os casos de uso por domínio funcional e registra dependências funcionais estáveis. Estado, gate e ordem da fila principal ficam em [`../development/backlog.md`](../development/backlog.md).

## Fundação técnica

- **FT001 — Fundação Técnica:** solution, projetos, EF Core/SQLite, testes, logging e CI.
- **FT002 — Fundação Multiempresa e Autenticação:** Empresa, Identity, vínculos, Empresa Ativa e isolamento tenant-aware.

## Insumos

| UC | Nome | Dependências |
|---|---|---|
| [UC001](UC001-cadastrar-insumo.md) | Cadastrar insumo | FT001 |
| [UC001B](UC001B-generalizar-categoria-unidades-insumo.md) | Generalizar categoria e unidades | UC001, FT002 |
| [UC001A](UC001A-complementar-insumo-marca-observacao.md) | Complementar marca e observação | UC001B, FT002 |
| [UC002](UC002-listar-consultar-insumos.md) | Listar e consultar | UC001A, FT002 |
| [UC003](UC003-editar-insumo.md) | Editar | UC002, UC001A, FT002 |
| [UC004](UC004-desativar-reativar-insumo.md) | Desativar e reativar | UC003, FT002 |
| [UC005](UC005-registrar-preco-insumo.md) | Registrar preço | UC004, FT002, RN040 |
| [UC006](UC006-consultar-historico-precos-insumo.md) | Consultar histórico de preços | UC005, MEL006 |

A identidade de Insumo é tenant-aware por `(EmpresaId, NomeNormalizado, MarcaNormalizada)`. RN040/RN048/RN051 preservam identidade histórica após primeiro preço ou primeiro uso em Ficha.

## Produtos

Produto possui Nome, Categoria opcional, Margem-alvo e Situação. Pode existir sem preço de prateleira; UC011/UC012 dependem do cálculo até o preço sugerido.

| UC | Nome | Dependências |
|---|---|---|
| [UC007](UC007-cadastrar-produto.md) | Cadastrar produto | FT002 |
| [UC008](UC008-listar-consultar-produtos.md) | Listar e consultar produtos | UC007 |
| [UC009](UC009-editar-produto.md) | Editar produto | UC007 |
| [UC010](UC010-desativar-reativar-produto.md) | Desativar e reativar produto | UC007, UC009 |
| [UC011](UC011-registrar-preco-prateleira-snapshot.md) | Registrar preço de prateleira preservando snapshot de precificação | UC023; MEL006; incorporar MEL009 |
| UC012 | Consultar histórico de precificação do produto | UC011; consumir reserva congelada conforme MEL009 |

UC011 cria histórico comercial append-only e congela Custo de referência, Margem de referência, Preço sugerido, Preço de prateleira e Reserva comercial de referência.

## Ficha técnica

| UC | Nome | Dependências |
|---|---|---|
| [UC013](UC013-definir-base-ficha-tecnica.md) | Definir rendimento e tempo ativo | UC007, FT002 |
| [UC014](UC014-adicionar-insumo-ficha.md) | Adicionar Insumo | UC013, UC001A–UC006 |
| [UC015](UC015-alterar-item-ficha.md) | Alterar item | UC014 |
| [UC016](UC016-remover-item-ficha.md) | Remover item | UC014, UC015, MEL010 |
| [UC017](UC017-consultar-ficha-composicao.md) | Consultar ficha e composição | UC013–UC016 |

## Configurações

| UC | Nome | Dependências |
|---|---|---|
| [UC026](UC026-consultar-configuracoes-precificacao.md) | Consultar configurações de precificação | FT002, MEL009 |
| [UC027](UC027-alterar-configuracoes-precificacao.md) | Alterar configurações de precificação | UC026, MEL009 |

## Precificação

| UC | Nome | Dependências |
|---|---|---|
| [UC018](UC018-calcular-custo-itens-lote.md) | Calcular custo atual dos itens | UC005, UC006, UC014, UC017 |
| [UC020](UC020-calcular-custo-mao-de-obra.md) | Calcular mão de obra | UC013, UC027 |
| [UC021](UC021-calcular-custo-energia-equipamentos.md) | Calcular energia/equipamentos | UC013, UC027 |
| [UC019](UC019-calcular-perdas-aplicaveis.md) | Calcular perdas | UC018 |
| [UC022](UC022-calcular-custo-total-unitario.md) | Calcular custo total e unitário | UC018–UC021 |
| [UC023](UC023-calcular-preco-teorico-sugerido.md) | Calcular preço teórico e sugerido | UC022, UC027 |
| UC024 | Calcular margem atual e situação | UC011, UC022 |
| UC025 | Consultar detalhamento da precificação | UC023, UC024 |

## Dashboard

| UC | Nome | Dependências |
|---|---|---|
| UC028 | Consultar resumo de margens da Empresa Ativa | UC024 |
| UC029 | Filtrar produtos abaixo da margem | UC028 |
| UC030 | Identificar produtos com precificação incompleta | UC017, UC028 |

## Administração multiempresa

Backlog a detalhar: cadastro/consulta de empresas, cadastro de usuários e gestão de vínculos.

## Pós-MVP identificado

Preparação intermediária reutilizável, backup/restauração pela interface, gráficos históricos e hospedagem a definir.

## Documentação individual

Ao preparar um caso de uso, criar `docs/use-cases/UCxxx-nome-do-caso.md` antes da instrução Codex correspondente.
