# Catálogo Inicial de Casos de Uso

Este catálogo define o backlog funcional inicial e a ordem lógica de dependências. O detalhamento completo de cada UC será criado em arquivo próprio imediatamente antes de sua implementação.

Um UC deve caber, idealmente, em uma sessão curta de implementação, testes e revisão. Se crescer demais, deve ser dividido.

## Fundação técnica

- **FT001 — Fundação Técnica:** implementada.
- **FT002 — Fundação Multiempresa e Autenticação:** próxima fundação; deve ser implementada antes de UC001A/UC002.

FT002 inclui apenas o bootstrap/login/seleção mínimos necessários ao isolamento. CRUD administrativo completo será detalhado posteriormente.

## Insumos

| UC | Nome | Dependências |
|---|---|---|
| [UC001](UC001-cadastrar-insumo.md) | Cadastrar insumo | FT001 — implementado |
| [UC001A](UC001A-complementar-insumo-marca-observacao.md) | Complementar cadastro com marca e observação | UC001, FT002; **revalidar especificação após FT002** |
| [UC002](UC002-listar-consultar-insumos.md) | Listar e consultar insumos | UC001A e ajuste de classificação/unidades a detalhar |
| UC003 | Editar insumo | UC001A |
| UC004 | Desativar insumo | UC001A |
| UC005 | Registrar preço de insumo | UC001A |
| UC006 | Consultar histórico de preços do insumo | UC005 |

Antes do UC002, categorias/unidades de Insumo serão reavaliadas para os dois negócios com base em dados reais.

## Produtos

| UC | Nome | Dependências |
|---|---|---|
| UC007 | Cadastrar produto | FT002 |
| UC008 | Listar e consultar produtos | UC007 |
| UC009 | Editar produto | UC007 |
| UC010 | Desativar produto | UC007 |
| UC011 | Alterar preço de venda preservando histórico | UC007 |
| UC012 | Consultar histórico de preço de venda | UC011 |

## Ficha técnica

| UC | Nome | Dependências |
|---|---|---|
| UC013 | Definir rendimento e tempos/recursos do lote | UC007; **revalidar modelo genérico antes de implementar** |
| UC014 | Adicionar insumo à ficha técnica com quantidade e observação contextual opcional | UC001A, UC007 |
| UC015 | Alterar quantidade/observação de item da ficha técnica | UC014 |
| UC016 | Remover item da ficha técnica | UC014 |
| UC017 | Consultar ficha técnica e composição | UC013, UC014 |

## Precificação

| UC | Nome | Dependências |
|---|---|---|
| UC018 | Calcular custo atual dos itens do lote | UC005, UC014 |
| UC019 | Calcular perdas aplicáveis | UC018; revalidar antes de implementar |
| UC020 | Calcular custo de mão de obra | UC013, Configurações |
| UC021 | Calcular custo de energia/equipamentos | UC013, Configurações; revalidar antes de implementar |
| UC022 | Calcular custo total e custo unitário | UC018–UC021 |
| UC023 | Calcular preço teórico e sugerido | UC022 |
| UC024 | Calcular margem atual e situação | UC011, UC022 |
| UC025 | Consultar detalhamento da precificação | UC023, UC024 |

## Configurações

| UC | Nome | Dependências |
|---|---|---|
| UC026 | Consultar configurações de precificação da Empresa | FT002 |
| UC027 | Alterar configurações de precificação da Empresa | UC026 |

## Dashboard

| UC | Nome | Dependências |
|---|---|---|
| UC028 | Consultar resumo de margens da Empresa Ativa | UC024 |
| UC029 | Filtrar produtos abaixo da margem | UC028 |
| UC030 | Identificar produtos com precificação incompleta | UC017, UC028 |

## Administração multiempresa

Backlog a detalhar:

- cadastro/consulta de empresas;
- cadastro de usuários e gestão de vínculos usuário-empresa.

## Pós-MVP já identificado

- preparação intermediária reutilizável;
- backup/restauração pela interface;
- gráficos históricos;
- hospedagem a definir.

## Documentação individual

Ao preparar um caso de uso, criar `docs/use-cases/UCxxx-nome-do-caso.md` seguindo o template. O arquivo individual deve existir antes da instrução de implementação entregue ao Codex.
