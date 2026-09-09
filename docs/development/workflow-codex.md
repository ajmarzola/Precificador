# Fluxo de Trabalho com Codex

## Princípio

Cada entrega deve ser pequena, documentada, testável e revisável. O Codex executa uma especificação já decidida; não é responsável por expandir o escopo do produto.

## Fluxo

```text
necessidade
  -> funcionalidade/regra
  -> caso de uso documentado
  -> critérios de aceitação
  -> instrução de implementação versionada
  -> Codex
  -> build/testes
  -> revisão do diff
  -> PR + CI
  -> merge
```

Para fundações não funcionais, como a FT001, a especificação técnica substitui o documento de caso de uso, mantendo os mesmos princípios de escopo e revisão.

## Antes de enviar uma implementação ao Codex

1. confirmar que o UC (ou fundação técnica) existe em arquivo individual;
2. confirmar regras de negócio aplicáveis;
3. fechar critérios de aceitação;
4. listar explicitamente o que está fora do escopo;
5. identificar testes esperados;
6. identificar se haverá migration;
7. garantir que o incremento seja pequeno o suficiente para revisão;
8. criar a instrução executável correspondente em `docs/codex/`.

## Instruções versionadas

As instruções entregues ao Codex fazem parte do repositório e devem ser armazenadas em:

```text
docs/codex/
```

Convenção inicial:

```text
FTxxx-nome.md
UCxxx-nome.md
```

A instrução deve apontar, sem copiar desnecessariamente, para:

- UC/especificação a implementar;
- documentos normativos;
- arquivos/áreas permitidos quando isso for útil;
- critérios de aceitação;
- testes obrigatórios;
- Definition of Done;
- proibição de alterações fora do escopo.

A especificação é a fonte normativa; a instrução do Codex é o roteiro de execução e não deve contradizê-la.

## Durante a execução

O agente deve:

- inspecionar o código existente antes de editar;
- reutilizar padrões já presentes;
- evitar reestruturações não solicitadas;
- atualizar testes e documentação na mesma entrega;
- executar os quality gates definidos na DoD.

## Após a execução

A revisão humana deve verificar principalmente:

- aderência ao UC/especificação;
- regras financeiras quando aplicáveis;
- migrations;
- testes realmente significativos;
- ausência de escopo incidental;
- legibilidade/manutenção;
- documentação coerente com o comportamento entregue;
- CI verde antes do merge.

## Política de mudança de requisito

Se durante um UC surgir uma nova necessidade funcional, ela deve ser registrada para avaliação posterior. Não deve ser incorporada automaticamente ao mesmo diff, salvo quando for indispensável para satisfazer um critério já aprovado.
