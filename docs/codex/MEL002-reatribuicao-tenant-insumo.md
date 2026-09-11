# Instrução Codex — MEL002 Reatribuição de tenant no domínio

Você está implementando a **MEL002 — Teste de tentativa de reatribuição de tenant no domínio** do repositório `ajmarzola/Precificador`.

## Regra de escopo

A única especificação normativa desta entrega é:

```text
docs/development/improvements/MEL002-reatribuicao-tenant-insumo.md
```

Documentos de UCs ou melhorias posteriores presentes no repositório **não autorizam sua implementação**.

## Leitura obrigatória

Antes de alterar código, leia:

1. `AGENTS.md`;
2. `docs/development/improvements/MEL002-reatribuicao-tenant-insumo.md`;
3. `docs/development/foundation-multiempresa-auth.md`;
4. `docs/development/testing-strategy.md`;
5. `docs/development/definition-of-done.md`;
6. `docs/development/melhorias.md`;
7. `src/Precificador.Core/Insumos/Insumo.cs`;
8. `tests/Precificador.Tests.Unit/Insumos/InsumoTests.cs`.

## Branch

Use:

```text
test/mel002-reatribuicao-tenant
```

Parta da `master` atualizada após o merge desta especificação.

## Objetivo técnico

Adicionar um teste unitário focado que prove que um `Insumo` já associado à Empresa 1 não pode ser reatribuído à Empresa 2 e mantém o ownership original após a tentativa.

## Teste obrigatório

Adicionar em:

```text
tests/Precificador.Tests.Unit/Insumos/InsumoTests.cs
```

Nome sugerido:

```text
CA01_Reatribuir_insumo_para_outra_empresa_e_rejeitado_e_preserva_empresa_original
```

Cenário:

```text
var insumo = Insumo.Criar(1, ...);

DefinirEmpresa(2)
=> InvalidOperationException

insumo.EmpresaId
=> 1
```

O teste deve validar:

1. tipo da exceção;
2. preservação do `EmpresaId` original.

Não dependa do texto exato da mensagem da exceção.

## Proibições

Não:

- alterar `Insumo.cs`;
- alterar `IEntidadeEmpresa`;
- alterar `PrecificadorDbContext`;
- alterar guard tenant-aware;
- alterar configuração EF;
- criar migration;
- alterar ModelSnapshot;
- criar teste de integração/Web para essa invariância;
- usar reflexão para alterar `EmpresaId`;
- implementar MEL001/MEL003/MEL004;
- implementar UC003;
- fazer refatorações oportunistas.

Se o teste não passar com o código atual, **pare e reporte** em vez de alterar produção.

## Matriz de testes

Esta melhoria adiciona somente:

- 1 teste unitário focado.

Evite teste agregado. O nome deve refletir cenário e resultado esperado.

## Documentação pós-implementação

Ao concluir:

- alterar o status de `docs/development/improvements/MEL002-reatribuicao-tenant-insumo.md` para `Concluída`;
- alterar MEL002 em `docs/development/melhorias.md` para `Concluída`;
- não alterar status de MEL001, MEL003 ou MEL004;
- não alterar UCs.

## Validação

Execute:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Antes de abrir a PR confirme:

1. build Release sem warnings novos relevantes;
2. suíte completa verde;
3. novo teste prova exceção + estado preservado;
4. nenhum arquivo de produção foi alterado;
5. nenhuma migration/ModelSnapshot mudou;
6. MEL002 foi marcada como concluída;
7. diff restrito à melhoria.

## Retorno obrigatório ao final

Responda com:

```text
Implementação concluída

Resumo:
- ...

Validações:
- ...

Testes:
- Unitários: X/X
- Integração: X/X

Produção/schema:
- nenhuma alteração de produção
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
test: valida ownership imutavel do insumo
```

Se houver qualquer pendência, não escreva "nenhuma".

Não faça merge em `master`.
