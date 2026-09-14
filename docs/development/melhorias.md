# Melhorias

Esta lista registra melhorias técnicas ou de produto identificadas durante implementação e revisão. Estado, gate/dependência operacional, prioridade de fila e ordem ficam somente em [`backlog.md`](backlog.md).

Em regra, melhorias não bloqueiam histórias principais, salvo quando uma revalidação de domínio promover explicitamente uma melhoria a pré-requisito técnico de um UC posterior. Quando isso ocorrer, a melhoria deve aparecer na fila principal do backlog.

## Catálogo

### MEL001 — Teste explícito da FK Insumo → Empresa

- **Origem:** revisão da FT002
- **Objetivo:** adicionar teste de integração que valide explicitamente a restrição de chave estrangeira entre `Insumo.EmpresaId` e `Empresas.Id`, além do guard de tenant já existente.
- **Especificação:** [MEL001 — Teste explícito da FK Insumo → Empresa](improvements/MEL001-teste-fk-insumo-empresa.md)
- **Instrução Codex:** [MEL001 — implementação](../codex/MEL001-teste-fk-insumo-empresa.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

### MEL002 — Teste de tentativa de reatribuição de tenant no domínio

- **Origem:** revisão da FT002
- **Objetivo:** cobrir explicitamente que um `Insumo` já associado a uma Empresa não pode ser reatribuído para outra Empresa pelo domínio e preserva o ownership original após a tentativa.
- **Especificação:** [MEL002 — Reatribuição de tenant no domínio](improvements/MEL002-reatribuicao-tenant-insumo.md)
- **Instrução Codex:** [MEL002 — implementação](../codex/MEL002-reatribuicao-tenant-insumo.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

### MEL003 — Assert direto de limpeza da Empresa Ativa no logout

- **Origem:** revisão da FT002
- **Objetivo:** complementar o teste Web de logout com verificação direta de que ID, Nome e chaves da Empresa Ativa são removidos da sessão.
- **Especificação:** [MEL003 — Limpeza da Empresa Ativa no logout](improvements/MEL003-limpeza-empresa-logout.md)
- **Instrução Codex:** [MEL003 — implementação](../codex/MEL003-limpeza-empresa-logout.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

### MEL004 — Centralizar rótulos de CategoriaInsumo e UnidadeMedida na UI

- **Origem:** revisão do UC002
- **Objetivo:** centralizar os rótulos de Categoria e Unidade em um único helper de apresentação do projeto Web, removendo duplicação entre cadastro, listagem e detalhes.
- **Especificação:** [MEL004 — Rótulos de Insumos na UI](improvements/MEL004-rotulos-insumos-ui.md)
- **Instrução Codex:** [MEL004 — implementação](../codex/MEL004-rotulos-insumos-ui.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

### MEL005 — Centralizar entrada e validação de Insumo entre Novo e Editar

- **Origem:** revisão do UC003; reavaliada após UC005
- **Objetivo:** remover a duplicação confirmada entre `NovoModel` e `EditarModel` centralizando o modelo de entrada, validação Web de Categoria/Unidade, mapeamento de erros de domínio e mensagem de duplicidade, sem alterar regras de negócio ou comportamento funcional.
- **Especificação:** [MEL005 — Centralizar entrada e validação de Insumo](improvements/MEL005-centralizar-formulario-insumo.md)
- **Instrução Codex:** [MEL005 — implementação](../codex/MEL005-centralizar-formulario-insumo.md)
- **Decisão:** a duplicação já é suficiente para justificar a refatoração; consultas de duplicidade, RN040 e markup Razor permanecem específicos de cada fluxo para evitar abstração excessiva.
- **Prioridade:** baixa; executar quando houver janela técnica apropriada.

### MEL006 — Tornar a data operacional dependente do timezone da Empresa

- **Origem:** especificação do UC006
- **Objetivo:** eliminar a dependência do timezone do processo/servidor, persistindo `TimeZoneId` por Empresa e fornecendo uma data operacional derivada de `TimeProvider` + timezone da Empresa Ativa.
- **Especificação:** [MEL006 — Timezone e data operacional da Empresa](improvements/MEL006-timezone-empresa.md)
- **Instrução Codex:** [MEL006 — implementação](../codex/MEL006-timezone-empresa.md)
- **Decisão:** usar `America/Sao_Paulo` como padrão de compatibilidade para Empresas existentes e `TimeProvider` para testes determinísticos; não criar CRUD de timezone nesta melhoria.
- **Prioridade:** recomendada antes da implementação do UC006 para evitar introduzir dependência temporária de `DateTime.Now`.

### MEL007 — Centralizar o estado do backlog em uma fonte de verdade clara

- **Origem:** review do projeto após conclusão da UC010
- **Problema:** o estado e a ordem dos itens de trabalho são repetidos em documentos individuais, catálogo, ordem de implementação, features e lista de melhorias.
- **Objetivo:** criar uma única fonte normativa em `docs/development/backlog.md` para estado, gate e ordem operacional.
- **Especificação:** [MEL007 — Centralizar estado e ordem do backlog](improvements/MEL007-centralizar-estado-backlog.md)
- **Instrução Codex:** [MEL007 — implementação](../codex/MEL007-centralizar-estado-backlog.md)
- **Decisão:** usar Markdown simples, cinco estados fechados, gate separado de estado e nenhuma automação/gerador nesta primeira versão.
- **Prioridade:** média-baixa; não bloqueia MEL010/UC016.

### MEL008 — Reduzir duplicação da infraestrutura de testes Web tenant-aware

- **Origem:** review do projeto após UC009/UC010
- **Problema:** 12 suítes Web já repetem criação de cliente autenticado, 15 repetem extração de antiforgery e 11 repetem ContextoEmpresaTeste.
- **Objetivo:** extrair infraestrutura transversal mínima de usuário/vínculo, login real, cookies, antiforgery, Empresa auxiliar e contexto tenant-aware, preservando helpers de domínio específicos em cada suíte.
- **Especificação:** [MEL008 — Infraestrutura Web tenant-aware de testes](improvements/MEL008-testes-web-tenant-aware.md)
- **Instrução Codex:** [MEL008 — implementação](../codex/MEL008-testes-web-tenant-aware.md)
- **Decisão:** composição por helpers pequenos; sem classe base, TestAuthHandler, bypass de antiforgery/GQF ou builders genéricos.
- **Prioridade:** baixa; executar quando houver janela técnica e independente da fila funcional.

### MEL009 — Parametrizar reserva comercial do Desconto de referência

- **Origem:** definição do modelo comercial do UC011
- **Objetivo:** permitir reserva comercial configurável por Empresa sem reinterpretar histórico.
- **Especificação:** [MEL009 — Reserva comercial do Desconto de referência](improvements/MEL009-reserva-comercial-desconto.md)
- **Decisão:** `ReservaComercialDesconto` nasce nas configurações da Empresa com padrão de 10 p.p.; o limiar é derivado como reserva + 1 p.p.; `RegistroPrecoProduto` congela `ReservaComercialReferencia`; `DescontoReferencia` permanece derivado.
- **Implementação:** não haverá PR autônoma da MEL009. UC026/027 implementam a configuração; UC011 implementa o snapshot; UC012 consome o snapshot histórico.
- **Prioridade:** baixa; incorporar obrigatoriamente quando esses UCs forem especificados/implementados.

### MEL010 — Persistir identidade consolidada do Insumo

- **Origem:** decisão pré-UC016
- **Objetivo:** persistir `Insumo.IdentidadeConsolidada` como estado monotônico, tornando Nome, Marca e Unidade base permanentemente imutáveis após o primeiro preço ou primeiro uso em Ficha.
- **Especificação:** [MEL010 — Identidade consolidada do Insumo](improvements/MEL010-identidade-consolidada-insumo.md)
- **Instrução Codex:** [MEL010 — implementação](../codex/MEL010-identidade-consolidada-insumo.md)
- **Decisão:** primeiro uso econômico/produtivo consolida permanentemente a identidade; remover referências futuras não desbloqueia o cadastro.
- **Prioridade:** bloqueante antes da especificação/implementação do UC016.

## Regra de uso

Ao surgir uma ideia útil que não seja blocker da história em revisão, registrar aqui antes de seguir adiante. Não transformar automaticamente uma melhoria em requisito de uma história já aprovada sem reavaliar escopo, dependências e prioridade. Se a melhoria alterar estado, gate ou ordem, atualizar `backlog.md`.
