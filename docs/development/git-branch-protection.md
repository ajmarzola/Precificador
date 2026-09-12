# Proteção da branch master

## Objetivo

Impedir tecnicamente que uma implementação contorne o fluxo de Pull Request e altere diretamente a `master`.

As regras de `AGENTS.md` e `workflow-codex.md` são o primeiro guardrail. A proteção do GitHub é o segundo: mesmo que um agente erre, a plataforma deve recusar o caminho incorreto.

## Regra normativa

A `master` não recebe push direto de agentes.

Fluxo esperado:

```text
master
  -> branch dedicada
  -> implementação
  -> testes
  -> PR
  -> CI
  -> revisão
  -> merge humano
```

## Configuração recomendada no GitHub

Criar um **Ruleset de branch** ou proteção equivalente para a branch:

```text
master
```

### Regras obrigatórias

Ativar:

- exigir Pull Request antes de merge;
- exigir status checks antes de merge;
- usar o job de CI `build-and-test` como check obrigatório;
- bloquear force push;
- bloquear exclusão da branch;
- exigir resolução das conversas de revisão, se disponível.

### Aprovação

Este repositório atualmente é operado principalmente pela mesma conta que cria as branches/PRs.

Por isso, **não tornar uma aprovação formal de outra conta requisito técnico enquanto não houver um segundo reviewer elegível**. A revisão técnica pode continuar registrada por comentário e o merge permanece uma decisão humana.

Quando houver outro colaborador/reviewer real, reavaliar:

```text
required approvals = 1
```

### Atualização da branch antes do merge

É recomendável exigir que a PR esteja baseada em estado compatível com a `master` atual quando houver concorrência de mudanças.

Como o projeto trabalha com fila serial, não é necessário introduzir políticas de merge mais complexas enquanto não houver desenvolvimento paralelo.

### Bypass

Se o plano/configuração do GitHub oferecer lista de bypass:

- não conceder bypass a agentes/bots de implementação;
- manter exceções administrativas apenas quando realmente necessárias;
- uma emergência humana não deve virar o fluxo normal.

## O que não recomendamos neste momento

Não ativar apenas por formalidade:

- linear history obrigatória, pois o projeto já utiliza merge commits;
- merge queue, enquanto o desenvolvimento for serial;
- assinatura obrigatória de commits, salvo decisão posterior;
- aprovação obrigatória de Code Owners sem equipe/reviewer separado.

Essas opções podem ser úteis no futuro, mas não resolvem o risco principal observado.

## Verificação após configurar

Testar manualmente com uma branch descartável:

1. confirmar que PR para `master` funciona;
2. confirmar que CI aparece como requisito;
3. confirmar que uma tentativa de push direto para `master` é rejeitada;
4. confirmar que force push é rejeitado;
5. confirmar que a branch não pode ser excluída pelo fluxo comum.

## Limitação da integração atual

A integração GitHub usada pelo assistente não possui permissão administrativa para configurar ou consultar integralmente branch protection/rulesets deste repositório.

Por isso, a proteção de plataforma deve ser habilitada pelo proprietário do repositório na interface/configuração do GitHub.

Essa limitação não reduz a regra para agentes: mesmo antes do ruleset ser configurado, `AGENTS.md` proíbe trabalho direto na `master`.
