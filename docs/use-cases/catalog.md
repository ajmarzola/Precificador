# Catálogo Inicial de Casos de Uso

Este catálogo define o backlog funcional inicial e a ordem lógica de dependências. O detalhamento completo de cada UC será criado em arquivo próprio imediatamente antes de sua implementação.

Um UC deve caber, idealmente, em uma sessão curta de implementação, testes e revisão. Se crescer demais, deve ser dividido.

## Fundação técnica

A fundação técnica não representa funcionalidade de usuário, mas precede os UCs:

- criar solution/projetos;
- configurar referências;
- configurar EF Core/SQLite;
- estabelecer infraestrutura de testes;
- configurar logging e seed mínimo de desenvolvimento.

Essa fundação possui especificação própria e foi concluída antes dos casos de uso funcionais.

## Insumos

| UC | Nome | Dependências |
|---|---|---|
| [UC001](UC001-cadastrar-insumo.md) | Cadastrar insumo | Fundação |
| [UC001A](UC001A-complementar-insumo-marca-observacao.md) | Complementar cadastro com marca e observação | UC001 |
| [UC002](UC002-listar-consultar-insumos.md) | Listar e consultar insumos | UC001A |
| UC003 | Editar insumo | UC001A |
| UC004 | Desativar insumo | UC001A |
| UC005 | Registrar preço de insumo | UC001A |
| UC006 | Consultar histórico de preços do insumo | UC005 |

## Produtos

| UC | Nome | Dependências |
|---|---|---|
| UC007 | Cadastrar produto | Fundação |
| UC008 | Listar e consultar produtos | UC007 |
| UC009 | Editar produto | UC007 |
| UC010 | Desativar produto | UC007 |
| UC011 | Alterar preço de venda preservando histórico | UC007 |
| UC012 | Consultar histórico de preço de venda | UC011 |

## Ficha técnica

| UC | Nome | Dependências |
|---|---|---|
| UC013 | Definir rendimento e tempos do lote | UC007 |
| UC014 | Adicionar insumo à ficha técnica com quantidade e observação contextual opcional | UC001A, UC007 |
| UC015 | Alterar quantidade/observação de item da ficha técnica | UC014 |
| UC016 | Remover item da ficha técnica | UC014 |
| UC017 | Consultar ficha técnica e composição | UC013, UC014 |

## Precificação

| UC | Nome | Dependências |
|---|---|---|
| UC018 | Calcular custo atual dos itens do lote | UC005, UC014 |
| UC019 | Calcular perdas de ingredientes | UC018 |
| UC020 | Calcular custo de mão de obra | UC013, Configurações |
| UC021 | Calcular custo de energia | UC013, Configurações |
| UC022 | Calcular custo total e custo unitário | UC018–UC021 |
| UC023 | Calcular preço teórico e sugerido | UC022 |
| UC024 | Calcular margem atual e situação | UC011, UC022 |
| UC025 | Consultar detalhamento da precificação | UC023, UC024 |

## Configurações

| UC | Nome | Dependências |
|---|---|---|
| UC026 | Consultar configurações de precificação | Fundação |
| UC027 | Alterar configurações de precificação | UC026 |

## Dashboard

| UC | Nome | Dependências |
|---|---|---|
| UC028 | Consultar resumo de margens | UC024 |
| UC029 | Filtrar produtos abaixo da margem | UC028 |
| UC030 | Identificar produtos com precificação incompleta | UC017, UC028 |

## Pós-MVP já identificado

Não detalhar nem implementar sem autorização de escopo:

- preparação intermediária reutilizável;
- backup/restauração pela interface;
- gráficos históricos;
- acesso multiusuário;
- hospedagem.

## Documentação individual

Ao preparar um caso de uso, criar:

```text
docs/use-cases/UCxxx-nome-do-caso.md
```

seguindo `template.md`. O arquivo individual deve existir antes de a instrução de implementação ser entregue ao Codex.
