# Fluxo de Trabalho com Codex

## Princípio

Cada entrega deve ser pequena, documentada, testável e revisável. O Codex executa uma especificação já decidida; não é responsável por expandir o escopo do produto.

A `master` recebe alterações **somente por Pull Request**. Agentes não trabalham diretamente nela.

## Fluxo

```text
necessidade
  -> funcionalidade/regra
  -> caso de uso/melhoria documentado
  -> critérios de aceitação + matriz de testes
  -> instrução de implementação versionada
  -> master atualizada
  -> branch dedicada da tarefa
  -> Codex
  -> build/testes
  -> revisão do diff
  -> Pull Request + CI
  -> revisão técnica/humana
  -> merge humano
```

Para fundações não funcionais, como a FT001, a especificação técnica substitui o documento de caso de uso, mantendo os mesmos princípios de escopo e revisão.

## Gate 0 — branch antes de qualquer edição

Este gate é obrigatório e ocorre **antes de alterar qualquer arquivo**.

O agente deve:

1. identificar a branch atual;
2. confirmar o ponto de partida esperado;
3. criar/trocar para a branch definida na instrução versionada;
4. confirmar que a branch atual não é `master`;
5. somente então iniciar edições.

Forma conceitual:

```text
master atualizada
      |
      +--> feat/ucxxx-...
      +--> fix/...
      +--> refactor/melxxx-...
      +--> docs/...
```

Se o agente estiver em `master`, nenhuma edição é permitida antes da troca de branch.

Se não conseguir criar/trocar para a branch correta, deve interromper a implementação e reportar o impedimento.

### Proibições

O agente não pode:

- editar em `master`;
- commitar em `master`;
- fazer push direto para `master`;
- fazer merge da própria PR;
- continuar uma implementação em branch pertencente a outra tarefa;
- contornar proteção/ruleset da `master`.

## Antes de enviar uma implementação ao Codex

1. confirmar que o UC, MEL ou fundação existe em arquivo individual;
2. confirmar regras de negócio aplicáveis;
3. fechar critérios de aceitação;
4. fechar a matriz de testes;
5. listar explicitamente o que está fora do escopo;
6. identificar se haverá migration;
7. garantir que o incremento seja pequeno o suficiente para revisão;
8. criar a instrução executável correspondente em `docs/codex/`;
9. definir explicitamente o nome da branch de implementação;
10. confirmar que dependências anteriores já foram implementadas/revisadas/mergeadas quando o sequenciamento exigir.

## Instruções versionadas

As instruções entregues ao Codex fazem parte do repositório e devem ser armazenadas em:

```text
docs/codex/
```

Convenções:

```text
FTxxx-nome.md
UCxxx-nome.md
MELxxx-nome.md
```

Toda instrução de implementação deve conter uma seção de branch/precondição equivalente a:

```text
Antes de alterar qualquer arquivo:
1. parta da master atualizada;
2. crie/troque para <branch-da-tarefa>;
3. confirme que a branch atual não é master;
4. somente então implemente.

Se não for possível trabalhar nessa branch, não altere arquivos e reporte o impedimento.
```

A instrução deve apontar, sem copiar desnecessariamente, para:

- UC/especificação a implementar;
- documentos normativos;
- branch esperada;
- arquivos/áreas permitidos quando isso for útil;
- critérios de aceitação;
- matriz/testes obrigatórios;
- Definition of Done;
- proibição de alterações fora do escopo;
- proibição de merge pelo agente.

A especificação é a fonte normativa; a instrução do Codex é o roteiro de execução e não deve contradizê-la.

## Durante a execução

O agente deve:

- permanecer na branch dedicada;
- inspecionar o código existente antes de editar;
- reutilizar padrões já presentes;
- evitar reestruturações não solicitadas;
- atualizar testes e documentação na mesma entrega;
- executar os quality gates definidos na DoD;
- não iniciar outra história na mesma branch.

## Conclusão da execução

Antes de declarar a implementação concluída, o agente deve confirmar:

1. branch atual = branch da tarefa;
2. branch atual != `master`;
3. diff contra `master` contém apenas o escopo esperado;
4. build e testes estão verdes;
5. migrations estão corretas quando aplicável;
6. documentação pós-implementação foi atualizada;
7. nenhuma alteração foi feita diretamente em `master`.

A entrega termina em branch/PR. **Merge é uma ação humana posterior à revisão.**

## Revisão da Pull Request

A revisão deve verificar principalmente:

- head/base corretos;
- aderência ao UC/MEL/especificação;
- regras financeiras quando aplicáveis;
- migrations;
- testes realmente significativos;
- ausência de escopo incidental;
- legibilidade/manutenção;
- documentação coerente com o comportamento entregue;
- CI verde no head atual antes do merge.

Se o head mudar depois da revisão, revisar novamente o novo diff/checks antes do merge.

## Sequenciamento

Implementações são seriais quando a fila do projeto assim determinar.

Uma tarefa posterior pode ser documentada antecipadamente, mas sua implementação só começa depois que a dependência/etapa anterior estiver:

```text
implementada
-> revisada
-> corrigida, se necessário
-> mergeada em master
```

## Proteção técnica da master

Além das regras para agentes, o repositório deve possuir proteção técnica da `master` no GitHub.

Configuração recomendada está em:

`docs/development/git-branch-protection.md`.

Essa proteção é complementar: documentação reduz erro do agente; ruleset impede que um erro desse tipo vire alteração direta na branch principal.

## Política de mudança de requisito

Se durante um UC surgir uma nova necessidade funcional, ela deve ser registrada para avaliação posterior. Não deve ser incorporada automaticamente ao mesmo diff, salvo quando for indispensável para satisfazer um critério já aprovado.
