# Catálogo Funcional de Casos de Uso

Este catálogo organiza os casos de uso por domínio funcional e registra dependências funcionais estáveis.

Estado, gate/dependência operacional e ordem da fila principal ficam somente em [`../development/backlog.md`](../development/backlog.md).

Um UC deve caber, idealmente, em uma sessão curta de implementação, testes e revisão. Se crescer demais, deve ser dividido.

## Fundação técnica

- **FT001 — Fundação Técnica:** solution, projetos, EF Core/SQLite, testes, logging e CI.
- **FT002 — Fundação Multiempresa e Autenticação:** Empresa, ASP.NET Core Identity, UsuarioEmpresa, bootstrap inicial, login/logout, Empresa Ativa, isolamento tenant-aware e `EmpresaId` em Insumo.

FT002 inclui apenas o bootstrap/login/seleção mínimos necessários ao isolamento. CRUD administrativo completo será detalhado posteriormente.

## Insumos

| UC | Nome | Dependências funcionais |
|---|---|---|
| [UC001](UC001-cadastrar-insumo.md) | Cadastrar insumo | FT001 |
| [UC001B](UC001B-generalizar-categoria-unidades-insumo.md) | Generalizar categoria e unidades de insumo | UC001, FT002 |
| [UC001A](UC001A-complementar-insumo-marca-observacao.md) | Complementar cadastro com marca e observação | UC001B, FT002 |
| [UC002](UC002-listar-consultar-insumos.md) | Listar e consultar insumos | UC001A, FT002 |
| [UC003](UC003-editar-insumo.md) | Editar insumo | UC002, UC001A, FT002 |
| [UC004](UC004-desativar-reativar-insumo.md) | Desativar e reativar insumo | UC003, FT002 |
| [UC005](UC005-registrar-preco-insumo.md) | Registrar preço de insumo | UC004, FT002, RN040 |
| [UC006](UC006-consultar-historico-precos-insumo.md) | Consultar histórico de preços do insumo | UC005, MEL006 |

Classificação e unidades:

- categorias: Matéria-prima, Embalagem e Consumível;
- unidades: `g`, `ml`, `m` e `un`.

A identidade atual de Insumo é tenant-aware por `(EmpresaId, NomeNormalizado, MarcaNormalizada)` e independe de Ativo/Inativo.

UC005 introduziu histórico append-only de preços, custo unitário calculado sem persistência e aplicação da RN040 no fluxo de edição. MEL006 adicionou a data operacional da Empresa via `IDataOperacionalEmpresa`. UC006 consulta o histórico, seleciona o preço vigente pela RN006 usando essa data operacional e exibe o resumo de preço vigente em Detalhes.

RN040 mantém Nome, Marca e Unidade base imutáveis após o primeiro registro de preço, inclusive futuro.

RN048 + RN051 tornam Nome, Marca e Unidade base permanentemente imutáveis após o primeiro uso em Ficha. A MEL010 persiste esse estado antes do UC016.

## Produtos

O UC007 inaugurou o domínio Produto sem antecipar Ficha Técnica ou precificação comercial. O cadastro inicial contém Nome, Categoria opcional, Margem-alvo e situação ativa. Produto pode existir sem preço de prateleira; UC011/UC012 dependem do cálculo completo até o preço sugerido.

| UC | Nome | Dependências funcionais |
|---|---|---|
| [UC007](UC007-cadastrar-produto.md) | Cadastrar produto | FT002 |
| [UC008](UC008-listar-consultar-produtos.md) | Listar e consultar produtos | UC007 |
| [UC009](UC009-editar-produto.md) | Editar produto | UC007 |
| [UC010](UC010-desativar-reativar-produto.md) | Desativar e reativar produto | UC007, UC009 |
| UC011 | Registrar preço de prateleira preservando snapshot de precificação | UC023; incorporar snapshot `ReservaComercialReferencia` conforme MEL009 |
| UC012 | Consultar histórico de precificação do produto | UC011; derivar desconto histórico pela reserva congelada conforme MEL009 |

UC011/UC012 pertencem ao domínio Produtos, mas o registro comercial deve congelar Custo de referência, Margem de referência e Preço sugerido calculados pelo sistema.

## Ficha técnica

| UC | Nome | Dependências funcionais |
|---|---|---|
| [UC013](UC013-definir-base-ficha-tecnica.md) | Definir rendimento e tempo ativo da Ficha Técnica | UC007, FT002 |
| [UC014](UC014-adicionar-insumo-ficha.md) | Adicionar Insumo à Ficha Técnica | UC013, UC001A–UC006 |
| [UC015](UC015-alterar-item-ficha.md) | Alterar item da Ficha Técnica | UC014 |
| [UC016](UC016-remover-item-ficha.md) | Remover item da ficha técnica | UC014, UC015, MEL010 |
| [UC017](UC017-consultar-ficha-composicao.md) | Consultar ficha técnica e composição | UC013, UC014, UC015, UC016 |

## Configurações

UC026/UC027 existem porque UC020, UC021 e UC023 dependem de configurações da Empresa. Conforme MEL009, o mesmo conjunto de configurações deve incluir desde o início a Reserva comercial para desconto, com padrão de 10 p.p.

| UC | Nome | Dependências funcionais |
|---|---|---|
| [UC026](UC026-consultar-configuracoes-precificacao.md) | Consultar configurações de precificação da Empresa | FT002, MEL009 |
| [UC027](UC027-alterar-configuracoes-precificacao.md) | Alterar configurações de precificação da Empresa | UC026, MEL009 |

## Precificação

| UC | Nome | Dependências funcionais |
|---|---|---|
| [UC018](UC018-calcular-custo-itens-lote.md) | Calcular custo atual dos itens do lote | UC005, UC006, UC014, UC017 |
| UC019 | Calcular perdas aplicáveis | UC018 |
| UC020 | Calcular custo de mão de obra | UC013, UC027 |
| UC021 | Calcular custo de energia/equipamentos | UC013, UC027 |
| UC022 | Calcular custo total e custo unitário | UC018–UC021 |
| UC023 | Calcular preço teórico e sugerido | UC022, UC027 |
| UC024 | Calcular margem atual e situação | UC011, UC022 |
| UC025 | Consultar detalhamento da precificação | UC023, UC024 |

## Dashboard

| UC | Nome | Dependências funcionais |
|---|---|---|
| UC028 | Consultar resumo de margens da Empresa Ativa | UC024 |
| UC029 | Filtrar produtos abaixo da margem | UC028 |
| UC030 | Identificar produtos com precificação incompleta | UC017, UC028 |

## Administração multiempresa

Backlog a detalhar:

- cadastro/consulta de empresas;
- cadastro de usuários e gestão de vínculos usuário-empresa.

## Pós-MVP identificado

- preparação intermediária reutilizável;
- backup/restauração pela interface;
- gráficos históricos;
- hospedagem a definir.

## Documentação individual

Ao preparar um caso de uso, criar `docs/use-cases/UCxxx-nome-do-caso.md` seguindo o template. O arquivo individual deve existir antes da instrução de implementação entregue ao Codex.
