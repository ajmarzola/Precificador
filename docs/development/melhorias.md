# Melhorias

Esta lista registra melhorias técnicas ou de produto identificadas durante implementação e revisão que **não bloqueiam** a entrega das histórias principais.

As melhorias podem ser executadas depois do fluxo principal ou antecipadas quando houver conveniência técnica clara, desde que não ampliem indevidamente o escopo da história em andamento.

## Convenções

Status possíveis:

- `Pendente` — registrada e ainda não especificada para execução;
- `Pronto para implementação` — especificação, matriz de testes e instrução do agente estão fechadas;
- `Em andamento` — incorporada a uma entrega em execução;
- `Concluída` — implementada e validada;
- `Descartada` — reavaliada e considerada desnecessária.

## Pendentes

### MEL001 — Teste explícito da FK Insumo → Empresa

- **Status:** Concluída
- **Origem:** revisão da FT002
- **Objetivo:** adicionar teste de integração que valide explicitamente a restrição de chave estrangeira entre `Insumo.EmpresaId` e `Empresas.Id`, além do guard de tenant já existente.
- **Especificação:** [MEL001 — Teste explícito da FK Insumo → Empresa](improvements/MEL001-teste-fk-insumo-empresa.md)
- **Instrução Codex:** [MEL001 — implementação](../codex/MEL001-teste-fk-insumo-empresa.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

### MEL002 — Teste de tentativa de reatribuição de tenant no domínio

- **Status:** Concluída
- **Origem:** revisão da FT002
- **Objetivo:** cobrir explicitamente que um `Insumo` já associado a uma Empresa não pode ser reatribuído para outra Empresa pelo domínio e preserva o ownership original após a tentativa.
- **Especificação:** [MEL002 — Reatribuição de tenant no domínio](improvements/MEL002-reatribuicao-tenant-insumo.md)
- **Instrução Codex:** [MEL002 — implementação](../codex/MEL002-reatribuicao-tenant-insumo.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

### MEL003 — Assert direto de limpeza da Empresa Ativa no logout

- **Status:** Concluída
- **Origem:** revisão da FT002
- **Objetivo:** complementar o teste Web de logout com verificação direta de que ID, Nome e chaves da Empresa Ativa são removidos da sessão.
- **Especificação:** [MEL003 — Limpeza da Empresa Ativa no logout](improvements/MEL003-limpeza-empresa-logout.md)
- **Instrução Codex:** [MEL003 — implementação](../codex/MEL003-limpeza-empresa-logout.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

### MEL004 — Centralizar rótulos de CategoriaInsumo e UnidadeMedida na UI

- **Status:** Pronto para implementação
- **Origem:** revisão do UC002
- **Objetivo:** centralizar os rótulos de Categoria e Unidade em um único helper de apresentação do projeto Web, removendo duplicação entre cadastro, listagem e detalhes.
- **Especificação:** [MEL004 — Rótulos de Insumos na UI](improvements/MEL004-rotulos-insumos-ui.md)
- **Instrução Codex:** [MEL004 — implementação](../codex/MEL004-rotulos-insumos-ui.md)
- **Prioridade:** baixa; selecionada para execução antes do UC003.

## Regra de uso

Ao surgir uma ideia útil que não seja blocker da história em revisão, registrar aqui antes de seguir adiante. Não transformar automaticamente uma melhoria em requisito de uma história já aprovada sem reavaliar escopo, dependências e prioridade.
