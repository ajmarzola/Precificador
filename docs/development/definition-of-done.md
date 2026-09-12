# Definition of Done

Um caso de uso, melhoria ou fundação só está concluído quando todos os itens aplicáveis abaixo foram atendidos.

## Fluxo Git

- a implementação foi feita em branch dedicada à tarefa;
- nenhuma edição/commit/push direto foi feito em `master`;
- a branch segue a instrução versionada ou convenção aprovada;
- o diff foi comparado com `master`;
- a entrega está em Pull Request;
- base da PR é a branch esperada, normalmente `master`;
- merge é realizado somente após revisão/CI e não pelo agente implementador.

## Comportamento

- o comportamento solicitado foi implementado;
- os critérios de aceitação do UC/MEL foram atendidos;
- as regras de negócio referenciadas foram respeitadas;
- fluxos inválidos previstos foram tratados;
- nenhuma funcionalidade fora do escopo foi adicionada.

## Código

- o diff é pequeno e relacionado à tarefa;
- não há refatoração oportunista sem necessidade;
- regras de negócio não foram duplicadas na UI;
- não foram criadas abstrações ou camadas sem justificativa;
- novos warnings relevantes não foram introduzidos.

## Persistência

Quando aplicável:

- alteração de esquema possui migration;
- migration é reproduzível a partir de banco limpo;
- migrations históricas não foram reescritas para acomodar evolução nova;
- não houve alteração manual de banco como substituto de migration;
- dados reais/sensíveis não foram adicionados ao repositório.

## Testes

- regras novas/alteradas possuem testes unitários quando aplicável;
- persistência relevante possui testes de integração com SQLite;
- matriz de testes definida antes da implementação foi atendida;
- golden cases são mantidos quando o motor de precificação for afetado;
- todos os testes existentes passam;
- testes não foram removidos/enfraquecidos para acomodar regressão.

## Validação técnica

- restore concluído;
- solution compila;
- suíte completa de testes passa;
- aplicação inicia quando a tarefa envolve fluxo web;
- o diff final contra `master` foi revisado;
- head atual da PR possui CI verde antes do merge.

## Documentação

- documento individual do UC/MEL está atualizado e com status coerente;
- regras de negócio afetadas foram atualizadas;
- funcionalidade afetada foi atualizada quando necessário;
- ADR foi criado/alterado se houve decisão arquitetural;
- documentação não descreve comportamento inexistente como implementado;
- instrução Codex permanece coerente com o resultado efetivamente entregue quando aplicável.

## Revisão

- PR descreve a tarefa implementada;
- base/head da PR são os esperados;
- mudanças não relacionadas foram removidas ou justificadas;
- blockers foram resolvidos antes do merge;
- melhorias não bloqueantes relevantes foram registradas no backlog;
- se o head mudou após a revisão, o novo head foi revisado;
- pendências conhecidas ficam registradas, não escondidas no código.
