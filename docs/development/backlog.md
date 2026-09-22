# Backlog

Este arquivo é a única fonte normativa para estado, gate/dependência operacional, ordem da fila principal e prioridade das melhorias não bloqueantes.

Estados permitidos: Planejado, Especificado, Pronto, Concluído, Descartado. Não registrar `Em andamento`; branch e Pull Request representam atividade transitória.

## Histórico concluído

Os itens abaixo permanecem como histórico normativo do que já foi entregue. A ordem futura de execução fica exclusivamente na seção **Fila pendente de execução**.

| Item | Tipo | Estado | Gate / Dependência | Documento |
|---|---|---|---|---|
| FT001 — Fundação Técnica | FT | Concluído | — | [foundation-technical.md](foundation-technical.md) |
| UC001 — Cadastrar Insumo | UC | Concluído | FT001 | [UC001](../use-cases/UC001-cadastrar-insumo.md) |
| FT002 — Fundação Multiempresa e Autenticação | FT | Concluído | FT001, UC001 | [FT002](foundation-multiempresa-auth.md) |
| UC001B — Generalizar categoria e unidades de insumo | UC | Concluído | UC001, FT002 | [UC001B](../use-cases/UC001B-generalizar-categoria-unidades-insumo.md) |
| UC001A — Complementar cadastro com marca e observação | UC | Concluído | UC001B, FT002 | [UC001A](../use-cases/UC001A-complementar-insumo-marca-observacao.md) |
| UC002 — Listar e consultar Insumos | UC | Concluído | UC001A, FT002 | [UC002](../use-cases/UC002-listar-consultar-insumos.md) |
| UC003 — Editar Insumo | UC | Concluído | UC002, UC001A, FT002 | [UC003](../use-cases/UC003-editar-insumo.md) |
| UC004 — Desativar e reativar Insumo | UC | Concluído | UC003, FT002 | [UC004](../use-cases/UC004-desativar-reativar-insumo.md) |
| UC005 — Registrar preço de Insumo | UC | Concluído | UC004, FT002, RN040 | [UC005](../use-cases/UC005-registrar-preco-insumo.md) |
| MEL006 — Timezone e data operacional da Empresa | MEL | Concluído | FT002; antes do UC006 | [MEL006](improvements/MEL006-timezone-empresa.md) |
| UC006 — Consultar histórico de preços do insumo | UC | Concluído | UC005, MEL006 | [UC006](../use-cases/UC006-consultar-historico-precos-insumo.md) |
| UC007 — Cadastrar Produto | UC | Concluído | FT002; UC006 mergeado | [UC007](../use-cases/UC007-cadastrar-produto.md) |
| UC008 — Listar e consultar Produtos | UC | Concluído | UC007 | [UC008](../use-cases/UC008-listar-consultar-produtos.md) |
| UC009 — Editar Produto | UC | Concluído | UC007; após UC008 | [UC009](../use-cases/UC009-editar-produto.md) |
| UC010 — Desativar e reativar Produto | UC | Concluído | UC007, UC008, UC009 | [UC010](../use-cases/UC010-desativar-reativar-produto.md) |
| UC013 — Definir rendimento da Ficha Técnica | UC | Concluído | UC007 a UC010, FT002 | [UC013](../use-cases/UC013-definir-base-ficha-tecnica.md) |
| UC014 — Adicionar Insumo à Ficha Técnica | UC | Concluído | UC013, UC001A a UC006, FT002 | [UC014](../use-cases/UC014-adicionar-insumo-ficha.md) |
| UC015 — Alterar item da Ficha Técnica | UC | Concluído | UC014 | [UC015](../use-cases/UC015-alterar-item-ficha.md) |
| MEL010 — Identidade consolidada do Insumo | MEL | Concluído | UC005, UC014, UC015 | [MEL010](improvements/MEL010-identidade-consolidada-insumo.md) |
| UC016 — Remover item da Ficha Técnica | UC | Concluído | — | [UC016](../use-cases/UC016-remover-item-ficha.md) |
| UC017 — Consultar Ficha Técnica e composição | UC | Concluído | — | [UC017](../use-cases/UC017-consultar-ficha-composicao.md) |
| MEL009 — Reserva comercial do Desconto de referência | MEL | Concluído | UC026, UC027, UC011 e UC012 | [MEL009](improvements/MEL009-reserva-comercial-desconto.md) |
| UC026 — Consultar configurações de precificação da Empresa | UC | Concluído | — | [UC026](../use-cases/UC026-consultar-configuracoes-precificacao.md) |
| UC027 — Alterar configurações de precificação da Empresa | UC | Concluído | — | [UC027](../use-cases/UC027-alterar-configuracoes-precificacao.md) |
| UC018 — Calcular custo atual dos itens do lote | UC | Concluído | — | [UC018](../use-cases/UC018-calcular-custo-itens-lote.md) |
| UC020 — Calcular custo de mão de obra | UC | Concluído | — | [UC020](../use-cases/UC020-calcular-custo-mao-de-obra.md) |
| UC021 — Calcular custo de energia/equipamentos | UC | Concluído | — | [UC021](../use-cases/UC021-calcular-custo-energia-equipamentos.md) |
| UC019 — Calcular perdas aplicáveis | UC | Concluído | — | [UC019](../use-cases/UC019-calcular-perdas-aplicaveis.md) |
| UC022 — Calcular custo total e custo unitário | UC | Concluído | UC018 a UC021 | [UC022](../use-cases/UC022-calcular-custo-total-unitario.md) |
| UC023 — Calcular preço teórico e sugerido | UC | Concluído | UC022, UC027 | [UC023](../use-cases/UC023-calcular-preco-teorico-sugerido.md) |
| UC011 — Registrar preço de prateleira preservando snapshot de precificação | UC | Concluído | UC023; incorporar MEL009 | [UC011](../use-cases/UC011-registrar-preco-prateleira-snapshot.md) |
| UC012 — Consultar histórico de precificação do Produto | UC | Concluído | UC011; incorporar MEL009 | [UC012](../use-cases/UC012-consultar-historico-precificacao-produto.md) |
| UC024 — Calcular margem atual e situação | UC | Concluído | UC012, UC022 | [UC024](../use-cases/UC024-calcular-margem-atual-situacao.md) |
| UC025 — Consultar detalhamento da precificação | UC | Concluído | UC023, UC024 | [UC025](../use-cases/UC025-consultar-detalhamento-precificacao.md) |

## Fila pendente de execução

Esta é a **única ordem normativa para itens ainda não concluídos**, independentemente de serem UC ou MEL.

Regra operacional:

1. tratar **um item por vez**;
2. só especificar/liberar o próximo item depois de o anterior estar concluído/mergeado, salvo decisão explícita registrada no backlog;
3. a coluna **Gate / Dependência** registra dependências reais ou gates operacionais que precisam estar concluídos antes do item;
4. a posição na fila resolve a prioridade entre itens que não possuem dependência técnica direta.

| Ordem | Item | Tipo | Estado | Gate / Dependência | Documento |
|---:|---|---|---|---|---|
| 01 | MEL015 — Padronizar apresentação monetária e entrada decimal pt-BR | MEL | Concluído | UC005, UC011, UC018–UC025 | [MEL015](improvements/MEL015-valores-financeiros-ptbr.md) |
| 02 | MEL016 — Corrigir semântica e defaults do registro de preço do Insumo | MEL | Concluído | MEL015; UC005, UC006, MEL006 | [MEL016](improvements/MEL016-preco-insumo-vocabulario-defaults.md) |
| 03 | MEL018 — Redirecionar cadastros para os detalhes da entidade | MEL | Concluído | MEL015, MEL016; UC001, UC007 | [MEL018](improvements/MEL018-redirecionar-cadastros-detalhes.md) |
| 04 | MEL019 — Preencher Rendimento da Ficha Técnica com padrão 1 | MEL | Concluído | UC013; gate operacional atendido: MEL018 Concluído | [MEL019](improvements/MEL019-rendimento-padrao-ficha.md) |
| 05 | MEL017 — Explicar configurações de precificação na interface | MEL | Concluído | UC026, UC027 | [MEL017](improvements/MEL017-ajuda-configuracoes-precificacao.md) |
| 06 | MEL012 — Adequar navegação para usuário anônimo | MEL | Concluído | FT002 | [MEL012](improvements/MEL012-navegacao-anonima.md) |
| 07 | MEL020 — Migrar persistência de SQLite para SQL Server | MEL | Concluído | FT002, MEL011; antes da nova estabilização | [MEL020](improvements/MEL020-sqlserver.md) |
| 08 | MEL022 — Substituir custo de mão de obra por percentual sobre os insumos | MEL | Concluído | MEL020; antes de MEL023 e MEL021 | [MEL022](improvements/MEL022-mao-de-obra-percentual.md) |
| 09 | MEL023 — Refinar equipamentos elétricos e navegação de tarifa na Ficha Técnica | MEL | Concluído | MEL022 concluída; antes de MEL021 | [MEL023](improvements/MEL023-equipamentos-eletricos-navegacao-tarifa.md) |
| 10 | UC032 — Administrar categorias de Produto | UC | Concluído | UC007–UC010 | [UC032](../use-cases/UC032-administrar-categorias-produto.md) |
| 11 | UC036 — Configurar e calcular custo de desgaste de equipamentos por Categoria | UC | Concluído | UC032, UC018, UC022 | [UC036](../use-cases/UC036-configurar-calcular-desgaste-equipamentos-categoria.md) |
| 12 | MEL021 — Publicar Precificador no Azure com custo controlado | MEL | Especificado | dependências funcionais concluídas; bloqueio externo exclusivo: conta Azure sem assinatura utilizável | [MEL021](improvements/MEL021-publicacao-azure.md) |
| 13 | MEL024 — Enriquecer listagem de Produtos com filtro por Categoria e indicadores atuais | MEL | Especificado | após MEL021, salvo decisão explícita; implementação pausada durante testes | [MEL024](improvements/MEL024-listagem-produtos-filtro-indicadores.md) |
| 14 | UC028 — Consultar resumo de margens da Empresa Ativa | UC | Planejado | MEL021, MEL015, UC024; após MEL024 na fila | Documento a criar |
| 15 | UC029 — Filtrar produtos abaixo da margem | UC | Planejado | UC028 | Documento a criar |
| 16 | UC030 — Identificar produtos com precificação incompleta | UC | Planejado | UC017, UC028 | Documento a criar |
| 17 | UC031 — Administrar usuários e vínculos com Empresas | UC | Planejado | FT002; regras de primeiro acesso/autorização a definir | Documento a criar |
| 18 | UC033 — Administrar coleções | UC | Planejado | UC032 | Documento a criar |
| 19 | UC034 — Vincular Produtos a Coleções | UC | Planejado | UC032, UC033 | Documento a criar |
| 20 | UC035 — Consultar referências de mercado para apoio ao preço de prateleira | UC | Planejado | UC011, UC025; gate operacional após UC034 | [UC035](../use-cases/UC035-consultar-referencias-mercado.md) |
| 21 | MEL013 — Documentar execução e teste local | MEL | Planejado | MEL011, MEL020, MEL021, UC031, UC035 | [MEL013](improvements/MEL013-execucao-teste-local.md) |
| 22 | MEL014 — Criar Manual do Usuário | MEL | Planejado | todos os itens anteriores da fila; MVP funcional consolidado | [MEL014](improvements/MEL014-manual-usuario.md) |

### Critério da ordem

- **01–06 — estabilização do MVP atual:** corrigir entrada monetária e fluxos/UX já encontrados no teste manual antes de expandir funcionalidade;
- **07 — fundação de persistência:** SQL Server concluído e usado como base da nova rodada;
- **08–09 — segunda estabilização manual:** corrigir o modelo de mão de obra e tornar explícita a opcionalidade dos equipamentos elétricos antes de publicar;
- **10–11 — Categoria estruturada e desgaste:** antecipar Categorias de Produto e, em seguida, o custo de desgaste de equipamentos por Categoria antes da primeira publicação;
- **12 — publicação:** disponibilizar o sistema no Azure somente após UC032/UC036 e quando o bloqueio externo da conta Azure estiver resolvido;
- **13 — adaptação operacional pós-planilha:** enriquecer a listagem de Produtos com filtro por Categoria e indicadores atuais, sem antecipar Dashboard;
- **14–16 — fechamento do escopo original:** concluir Dashboard e identificação de problemas de margem/precificação;
- **17 — acesso multiusuário:** fechar o primeiro acesso e a administração de usuários/vínculos sobre a fundação FT002 já existente;
- **18–19 — expansão de catálogo:** introduzir Coleções sobre a Categoria estruturada já entregue;
- **20 — inteligência de mercado:** consultar referências externas comparáveis como apoio à decisão de Preço de Prateleira, sem automatizar a decisão comercial;
- **21–22 — documentação:** escrever o guia técnico e o Manual do Usuário depois de os fluxos funcionais estarem estabilizados.

## Melhorias concluídas

| Item | Estado | Prioridade | Gate / Dependência | Documento |
|---|---|---|---|---|
| MEL001 — Teste explícito da FK Insumo → Empresa | Concluído | baixa | FT002 | [MEL001](improvements/MEL001-teste-fk-insumo-empresa.md) |
| MEL002 — Teste de tentativa de reatribuição de tenant no domínio | Concluído | baixa | FT002 | [MEL002](improvements/MEL002-reatribuicao-tenant-insumo.md) |
| MEL003 — Assert direto de limpeza da Empresa Ativa no logout | Concluído | baixa | FT002 | [MEL003](improvements/MEL003-limpeza-empresa-logout.md) |
| MEL004 — Centralizar rótulos de CategoriaInsumo e UnidadeMedida na UI | Concluído | baixa | UC001B, UC002 | [MEL004](improvements/MEL004-rotulos-insumos-ui.md) |
| MEL005 — Centralizar entrada e validação de Insumo entre Novo e Editar | Concluído | baixa | UC003, UC005 | [MEL005](improvements/MEL005-centralizar-formulario-insumo.md) |
| MEL007 — Centralizar estado e ordem do backlog | Concluído | média-baixa | — | [MEL007](improvements/MEL007-centralizar-estado-backlog.md) |
| MEL008 — Infraestrutura Web tenant-aware de testes | Concluído | baixa | — | [MEL008](improvements/MEL008-testes-web-tenant-aware.md) |
| MEL011 — Tratar primeiro uso com banco local não migrado | Concluído | alta | FT002 | [MEL011](improvements/MEL011-primeiro-uso-banco-local.md) |
