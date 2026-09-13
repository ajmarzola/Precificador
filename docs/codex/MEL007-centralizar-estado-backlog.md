# Instrução Codex — MEL007: Centralizar estado e ordem do backlog

## Tarefa

Implementar integralmente a MEL007 conforme docs/development/improvements/MEL007-centralizar-estado-backlog.md.

Branch obrigatória:

~~~text
docs/mel007-centralizar-backlog
~~~

Não editar, commitar ou fazer push direto em master.
Não fazer merge da própria implementação.

## Leitura obrigatória

- AGENTS.md;
- docs/development/improvements/MEL007-centralizar-estado-backlog.md;
- docs/development/melhorias.md;
- docs/development/implementation-order.md;
- docs/use-cases/catalog.md;
- docs/development/workflow-codex.md;
- docs/development/definition-of-done.md;
- docs/use-cases/template.md;
- documentos F00x;
- documentos UC e MEL atuais;
- instruções Codex de tarefas ainda não concluídas.

Antes de editar, confirmar que a branch parte da master atual e que nenhuma implementação funcional foi incorporada ao mesmo diff.

## Resultado obrigatório

Criar docs/development/backlog.md como única fonte normativa para estado, gate e ordem operacional.

Estados permitidos:

~~~text
Planejado
Especificado
Pronto
Concluído
Descartado
~~~

Não criar estado Em andamento.

## Estrutura do backlog

### Fila principal

Usar tabela com:

~~~text
Item | Tipo | Estado | Gate / Dependência | Documento
~~~

A ordem das linhas define a fila normativa.

### Melhorias não bloqueantes

Usar tabela com:

~~~text
Item | Estado | Prioridade | Gate / Dependência | Documento
~~~

Uma melhoria que virou pré-requisito obrigatório deve aparecer na fila principal.

## Migração documental

Reconciliar a master real antes de preencher backlog.md.

Obrigatório:

1. incluir FT, UC e MEL conhecidos;
2. remover Status de UC*.md;
3. remover Status de MEL*.md;
4. remover Próximo caso quando for fila operacional;
5. remover sufixos de status em features;
6. transformar catalog.md em índice funcional;
7. transformar implementation-order.md em estratégia sem fila duplicada;
8. remover Status de melhorias.md;
9. remover Status do template de UC;
10. atualizar AGENTS.md, workflow-codex.md e Definition of Done;
11. atualizar instruções Codex ainda executáveis para consultar backlog.md;
12. preservar instruções concluídas como artefatos históricos.

## Restrições

Não:

- alterar código de produção;
- alterar schema;
- criar migration;
- implementar UC/MEL funcional;
- criar gerador YAML/JSON;
- criar bot;
- criar novo pipeline ou step de CI;
- integrar GitHub Projects;
- reorganizar toda a árvore docs.

## Validação documental

Executar V1–V10 da especificação.

Em especial:

- todos os itens conhecidos aparecem no backlog;
- não restam campos Status em UC*.md/MEL*.md;
- features não carregam estado operacional;
- catalog e implementation-order não duplicam fila;
- melhorias.md delega estado ao backlog;
- AGENTS/workflow/DoD apontam para backlog;
- instruções ainda abertas usam backlog.

## Validação técnica

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Não alterar testes para acomodar esta MEL.

## Documentação pós-implementação

Após a migração documental:

- MEL007 deve aparecer como Concluído somente em backlog.md;
- o arquivo individual MEL007 não deve possuir campo Status;
- melhorias.md deve continuar descrevendo a melhoria sem duplicar seu estado.

## Retorno obrigatório

Informar:

1. branch;
2. arquivos migrados;
3. estrutura final de backlog.md;
4. quantidade de campos Status removidos;
5. instruções abertas ajustadas;
6. resultado V1–V10;
7. build/test;
8. URL da PR.

Commit sugerido:

~~~text
docs: centraliza estado do backlog
~~~
