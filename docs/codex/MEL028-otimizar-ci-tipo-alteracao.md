# Instrução Codex — MEL028: Otimizar pipeline de CI por tipo de alteração

## Tarefa

Implementar integralmente:

~~~text
docs/development/improvements/MEL028-otimizar-ci-tipo-alteracao.md
~~~

Branch sugerida:

~~~text
infra/mel028-ci-por-tipo-alteracao
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- MEL028;
- `.github/workflows/ci.yml`;
- `docs/development/testing-strategy.md`;
- `tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj`;
- `tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj`;
- infraestrutura Testcontainers atual.

## Restrição principal

Não reduzir cobertura.

A melhoria decide entre:

~~~text
PR documental
=> caminho rápido

qualquer outra alteração
=> CI completa
~~~

A classificação deve falhar para o lado seguro: dúvida => full.

## Required check

O ruleset atual de `master` exige:

~~~text
build-and-test
~~~

Preservar exatamente esse contexto/job obrigatório.

Não usar `paths-ignore` no workflow inteiro se isso impedir a emissão do required check.

## Whitelist documental

Somente:

~~~text
docs/**
*.md
~~~

na raiz.

Qualquer outro arquivo => full.

## PR

Usar SHAs base/head do evento e garantir histórico suficiente no checkout.

Se o diff falhar, executar full.

Sempre executar:

~~~text
git diff --check
~~~

No modo documental, encerrar depois dessa validação.

## Push master

Sempre full.

Não classificar push de master como docs-only nesta primeira versão.

## Full CI

Executar:

~~~text
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore

dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj \
  --configuration Release \
  --no-build

dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj \
  --configuration Release \
  --no-build
~~~

Unitários antes da integração.

Não aplicar filtro de testes.

Não trocar SQL Server/Testcontainers.

## dotnet tool restore

Remover do PR CI se nenhum passo realmente consumir `dotnet-ef`.

Não alterar scripts Azure/deploy nem comandos que precisem da ferramenta.

## Concurrency

Cancelar run anterior ainda ativo quando houver novo push na mesma PR.

Não exigir cancelamento de runs de master.

## Timeout

~~~text
timeout-minutes: 20
~~~

## Observabilidade do job

Registrar claramente:

~~~text
CI mode: documentation-only
~~~

ou:

~~~text
CI mode: full
~~~

Nomear passos de teste separadamente.

## Não fazer

- não implementar MEL027;
- não criar workflow de deploy;
- não remover/skipar testes;
- não alterar código de produção;
- não alterar migrations;
- não adicionar SQLite/EF InMemory;
- não introduzir action externa para filtragem se uma solução simples com git/bash atender;
- não renomear `build-and-test`.

## Documentação

Atualizar:

- MEL028 para `Concluído`;
- backlog MEL028 para `Concluído`;
- testing-strategy com as duas modalidades da CI.

Não alterar o estado de UC029 ou MEL027.

## Validação

A PR da própria MEL028 altera `.github/workflows/ci.yml`, portanto deve seguir o caminho **full**.

Confirmar na execução:

- Restore OK;
- Build Release OK;
- unitários completos OK;
- integração completa OK;
- contexto publicado como `build-and-test`.

Revisar manualmente a lógica do modo documental contra a matriz da especificação.

## Retorno esperado

Informar:

- arquivos alterados;
- regra de classificação;
- confirmação de fail-closed;
- confirmação do required check preservado;
- resultado da CI full;
- contagem unitária/integração;
- timeout/concurrency;
- confirmação de nenhuma alteração de runtime/schema;
- URL da PR.
