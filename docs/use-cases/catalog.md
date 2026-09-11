# Catálogo Inicial de Casos de Uso

Este catálogo define o backlog funcional inicial e a ordem lógica de dependências. O detalhamento completo de cada UC será criado em arquivo próprio imediatamente antes de sua implementação.

Um UC deve caber, idealmente, em uma sessão curta de implementação, testes e revisão. Se crescer demais, deve ser dividido.

## Fundação técnica

- **FT001 — Fundação Técnica:** implementada.
- **FT002 — Fundação Multiempresa e Autenticação:** implementada.

FT002 inclui apenas o bootstrap/login/seleção mínimos necessários ao isolamento. CRUD administrativo completo será detalhado posteriormente.

## Insumos

| UC | Nome | Dependências / Estado |
|---|---|---|
| [UC001](UC001-cadastrar-insumo.md) | Cadastrar insumo | FT001 — implementado |
| [UC001B](UC001B-generalizar-categoria-unidades-insumo.md) | Generalizar categoria e unidades de insumo | UC001, FT002 — implementado |
| [UC001A](UC001A-complementar-insumo-marca-observacao.md) | Complementar cadastro com marca e observação | UC001B, FT002 — implementado |
| [UC002](UC002-listar-consultar-insumos.md) | Listar e consultar insumos | UC001A, FT002 — implementado |
| [UC003](UC003-editar-insumo.md) | Editar insumo | UC002, UC001A, FT002 — **implementado** |
| [UC004](UC004-desativar-reativar-insumo.md) | Desativar e reativar insumo | UC003, FT002 — **implementado** |
| [UC005](UC005-registrar-preco-insumo.md) | Registrar preço de insumo | UC004, FT002, RN040 — **implementado** |
| [UC006](UC006-consultar-historico-precos-insumo.md) | Consultar histórico de preços do insumo | UC005 — **revalidado e pronto para implementação** |

Classificação e unidades implementadas:

- categorias: Matéria-prima, Embalagem e Consumível;
- unidades: `g`, `ml`, `m` e `un`.

A identidade atual de Insumo é tenant-aware por `(EmpresaId, NomeNormalizado, MarcaNormalizada)` e independe de Ativo/Inativo.

UC005 foi implementado com histórico append-only de preços, custo unitário calculado sem persistência e aplicação da RN040 no fluxo de edição. UC006 está especificado para consulta somente leitura do histórico e seleção do preço vigente pela RN006.

**Gate de preço concluído:** a RN040 mantém Nome, Marca e Unidade base imutáveis após o primeiro registro de preço, inclusive futuro.

**Gate de Ficha Técnica permanece:** antes do UC014, revalidar se referências de ficha também devem congelar campos cadastrais mesmo quando o Insumo ainda não possuir preço.

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
| UC014 | Adicionar insumo à ficha técnica com quantidade e observação contextual opcional | UC001A, UC007; revalidar restrições de edição do Insumo antes de implementar |
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
