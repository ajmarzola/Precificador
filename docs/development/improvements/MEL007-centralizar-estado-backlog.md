# MEL007 — Centralizar estado e ordem do backlog

- **Tipo:** melhoria de governança documental
- **Origem:** review do projeto após UC010
- **Prioridade:** média-baixa
- **Alteração de código de produção:** não
- **Alteração de schema:** não
- **Impacta:** documentação, fluxo de trabalho e Definition of Done

## Objetivo

Criar uma única fonte normativa para o estado atual dos itens de trabalho, a ordem da fila principal e os gates que impedem uma implementação.

A fonte será:

~~~text
docs/development/backlog.md
~~~

Os demais documentos continuam existindo, mas deixam de repetir estado corrente, próximo passo e ordem operacional.

## Problema atual

Hoje a mesma informação aparece em vários lugares:

- documento individual de UC;
- documento individual de MEL;
- docs/use-cases/catalog.md;
- docs/development/implementation-order.md;
- documentos de feature F00x;
- docs/development/melhorias.md;
- instruções Codex ainda abertas.

Isso aumenta o custo de manutenção e permite divergências entre Implementado, Pronto, Bloqueado e Próximo caso.

## Decisão

docs/development/backlog.md passa a ser a única fonte normativa para:

1. estado;
2. gate/dependência operacional;
3. ordem da fila principal;
4. prioridade das melhorias não bloqueantes.

## Vocabulário de estado

Usar somente:

~~~text
Planejado
Especificado
Pronto
Concluído
Descartado
~~~

### Planejado

O item existe, mas a especificação executável ainda não está fechada.

### Especificado

A especificação está fechada, porém ainda existe gate pendente ou falta instrução executável.

### Pronto

Especificação, critérios, matriz aplicável, instrução Codex, branch e gates estão fechados. Pronto significa implementável, não necessariamente próximo da fila.

### Concluído

A entrega foi revisada, validada, mergeada na master e possui checks aplicáveis verdes.

### Descartado

O item foi explicitamente abandonado por decisão registrada.

## Estados que não serão persistidos

Não criar estado Em andamento. Branch e Pull Request já representam atividade transitória.

Não criar estados compostos como Especificado — bloqueado ou Pronto — próximo caso.

Bloqueio deve ser representado separadamente.

## Gate / Dependência

Cada item pode possuir um campo Gate / Dependência.

Exemplos:

~~~text
MEL010
UC023
Revalidar após UC014
—
~~~

Um item só chega a Pronto quando todos os gates obrigatórios estiverem satisfeitos.

## Estrutura de backlog.md

### Fila principal

A ordem das linhas é normativa.

Colunas mínimas:

~~~text
Item | Tipo | Estado | Gate / Dependência | Documento
~~~

Itens concluídos permanecem na tabela para preservar o histórico da sequência.

Melhoria promovida a pré-requisito obrigatório entra na fila principal na posição correspondente.

### Melhorias não bloqueantes

Colunas mínimas:

~~~text
Item | Estado | Prioridade | Gate / Dependência | Documento
~~~

Uma melhoria não bloqueante não compete com a fila principal. Se virar gate obrigatório, deve ser movida para a fila principal na mesma PR que registra a decisão.

## Regras de governança

### Estado aparece em um único lugar

Somente backlog.md define formalmente se um item está Planejado, Especificado, Pronto, Concluído ou Descartado.

### Próximo caso não é metadata distribuída

UC, feature, catálogo e MEL não devem usar Próximo caso, Próxima ação ou equivalentes como indicador operacional.

A fila deve ser consultada no backlog.

### Documentos individuais descrevem conteúdo

UCs e MELs continuam contendo objetivo, regras, decisões, dependências funcionais, critérios, testes, impacto, fora de escopo e histórico útil.

Eles deixam de possuir campo Status.

### Features descrevem comportamento

F001, F002, F003 e futuras features podem listar UCs para navegação, mas sem sufixos implementado, pronto, próximo ou bloqueado.

### catalog.md vira índice funcional

O catálogo continua organizando UCs por domínio, nome, link e dependências funcionais estáveis.

Remover estado corrente, próximo caso, ordem operacional e gates transitórios.

Adicionar referência explícita para backlog.md.

### implementation-order.md vira estratégia

Manter o arquivo para preservar links existentes.

Seu papel passa a ser explicar fases, decisões duradouras de sequenciamento e racional de dependências.

Remover listas de estado corrente e seção Próximo passo.

Adicionar link para backlog.md como fonte da fila atual.

### melhorias.md vira catálogo de melhorias

Manter origem, problema, objetivo, decisão, prioridade e links.

Remover os campos Status e apontar para backlog.md.

### Templates

Remover o campo Status de docs/use-cases/template.md e de eventual template MEL.

Adicionar nota de que estado e ordem ficam em docs/development/backlog.md.

## AGENTS.md

Atualizar Fonte de verdade para declarar backlog.md como fonte normativa de estado, gate e ordem.

Antes de implementar, o agente deve consultar o backlog e confirmar Estado = Pronto e gate liberado, salvo instrução versionada explicitamente diferente.

## workflow-codex.md

Adicionar os gates:

1. confirmar item no backlog;
2. confirmar Estado = Pronto;
3. confirmar gate satisfeito;
4. implementar em branch dedicada;
5. após revisão e merge, atualizar o item para Concluído em PR apropriada.

Não exigir atualização intermediária para Em andamento.

## Definition of Done

Substituir a exigência de status coerente no documento individual por:

~~~text
backlog atualizado quando a entrega altera estado, gate ou ordem
~~~

Os documentos individuais continuam obrigatórios para comportamento e especificação, mas não carregam estado operacional.

## Migração documental

A implementação deve reconciliar a master real no momento da execução.

### 1. Criar backlog.md

Popular todos os itens conhecidos, incluindo fundações FT, UCs catalogados e MELs existentes.

Não usar uma lista congelada nesta especificação como fonte: reconciliar catalog.md, implementation-order.md, melhorias.md e documentos individuais.

### 2. UCs

Em docs/use-cases/UC*.md:

- remover linha Status;
- remover metadata Próximo caso quando representar fila;
- preservar dependências materiais e histórico útil;
- não reescrever conteúdo funcional.

### 3. MELs

Em docs/development/improvements/MEL*.md:

- remover linha Status;
- preservar prioridade, origem, dependências e momento recomendado ou obrigatório.

### 4. Features

Remover sufixos operacionais dos links/listas de UCs.

### 5. catalog.md

Transformar em índice funcional sem estados.

### 6. implementation-order.md

Transformar em documento de estratégia, sem duplicar a fila.

### 7. melhorias.md

Remover Status de cada melhoria e apontar para backlog.md.

### 8. Instruções ainda executáveis

Instruções Codex de tarefas ainda não concluídas devem consultar backlog.md em vez de validar status em documento individual.

Não reescrever em massa instruções de tarefas já concluídas; elas são artefatos históricos.

## Histórico versus estado corrente

Frases históricas como UC015 foi implementada na PR #58 ou revalidação concluída em determinada data podem permanecer.

Essas frases não são fonte operacional para decidir prontidão ou ordem.

## Rotina futura

### Novo item

Na mesma PR, adicionar ao backlog como Planejado ou Especificado e criar o documento individual quando aplicável.

### Especificação fechada

Alterar Planejado para Especificado. Se todos os gates e a instrução Codex já estiverem prontos, pode ir diretamente para Pronto.

### Gate concluído

Alterar Especificado para Pronto e limpar ou atualizar Gate.

Não editar feature ou catálogo apenas por essa transição.

### Implementação mergeada e validada

Alterar Pronto para Concluído.

O documento individual só recebe informação histórica de implementação se isso agregar valor.

## Decisão sobre automação

Não adicionar gerador de documentação, YAML, JSON, banco de backlog, bot ou novo step de CI nesta MEL.

A causa atual é duplicação. Remover as cópias resolve o problema com menor complexidade.

Se drift voltar a ocorrer mesmo com fonte única, registrar melhoria específica para validação automatizada.

## Validação

Como a MEL é documental, a validação é por inspeção determinística.

### V1 — backlog completo

Todos os itens presentes no catálogo e em melhorias aparecem no backlog.

### V2 — UCs/MELs sem Status

Não restam campos Status em docs/use-cases/UC*.md nem docs/development/improvements/MEL*.md.

### V3 — Features sem estado operacional

Features não usam implementado, pronto, próximo ou bloqueado para acompanhar UCs.

### V4 — catalog.md sem estado

O catálogo contém índice e dependências estáveis, não estado corrente.

### V5 — implementation-order.md sem fila duplicada

O arquivo explica estratégia e referencia backlog.md.

### V6 — melhorias.md sem Status

O catálogo de melhorias delega estado ao backlog.

### V7 — templates atualizados

Novos documentos não nascem com campo Status.

### V8 — AGENTS, workflow e DoD coerentes

Os três apontam backlog.md como fonte operacional.

### V9 — instruções abertas coerentes

Tarefas ainda não concluídas consultam backlog.md.

### V10 — links válidos

Links novos apontam para arquivos existentes.

## Build e testes

A MEL007 não altera código de produção ou testes.

Ainda assim executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

## Critérios de aceitação

- CA01: existe uma única fonte normativa de estado e ordem: backlog.md;
- CA02: estados permitidos são Planejado, Especificado, Pronto, Concluído e Descartado;
- CA03: bloqueio/gate é separado de Estado;
- CA04: a fila principal é ordenada apenas no backlog;
- CA05: UCs e MELs individuais não possuem Status operacional;
- CA06: features não funcionam como quadro de acompanhamento;
- CA07: catalog.md não é fonte de estado;
- CA08: implementation-order.md não duplica fila corrente;
- CA09: melhorias.md referencia backlog para status;
- CA10: AGENTS e workflow exigem consulta ao backlog;
- CA11: DoD atualiza backlog quando estado, gate ou ordem mudam;
- CA12: nenhuma automação adicional é criada;
- CA13: histórico útil permanece preservado;
- CA14: nenhum código funcional, migration ou comportamento da aplicação é alterado.

## Fora do escopo

- GitHub Projects;
- sincronização automática com Issues ou PRs;
- bot de status;
- geração de Markdown a partir de YAML/JSON;
- reorganização geral da árvore docs;
- renumeração de UCs/MELs;
- reescrita funcional das especificações;
- implementação de qualquer UC/MEL funcional;
- MEL008 e MEL009.

## Definition of Done específica

MEL007 está concluída quando:

- backlog.md contém estado e ordem reconciliados com a master;
- catálogo, implementation-order, features, UCs, MELs e melhorias deixam de duplicar estado corrente;
- templates seguem a nova convenção;
- AGENTS, workflow e DoD apontam para backlog;
- instruções ainda abertas foram ajustadas;
- V1 a V10 foram executadas;
- build e suíte completa permanecem verdes;
- nenhum código funcional foi alterado.

## Branch sugerida

~~~text
docs/mel007-centralizar-backlog
~~~

## Commit sugerido

~~~text
docs: centraliza estado do backlog
~~~
