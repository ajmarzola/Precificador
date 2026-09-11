# Instrução Codex — MEL001 Teste explícito da FK Insumo → Empresa

Você está implementando a **MEL001 — Teste explícito da FK Insumo → Empresa** do repositório `ajmarzola/Precificador`.

## Regra de escopo

A única especificação normativa desta entrega é:

```text
docs/development/improvements/MEL001-teste-fk-insumo-empresa.md
```

Documentos de UCs ou melhorias posteriores presentes no repositório **não autorizam sua implementação**.

## Leitura obrigatória

Antes de alterar código, leia integralmente:

1. `AGENTS.md`;
2. `docs/development/improvements/MEL001-teste-fk-insumo-empresa.md`;
3. `docs/development/foundation-multiempresa-auth.md`;
4. `docs/development/testing-strategy.md`;
5. `docs/development/definition-of-done.md`;
6. `docs/development/melhorias.md`;
7. `src/Precificador.Infrastructure/Persistence/Configurations/InsumoConfiguration.cs`;
8. os testes de integração de persistência relacionados a Insumo/Empresa.

## Branch

Use:

```text
test/mel001-fk-insumo-empresa
```

Parta da `master` atualizada após o merge da especificação.

## Objetivo técnico

Adicionar um teste de integração focado que prove que o SQLite rejeita um `Insumo.EmpresaId` referenciando uma Empresa inexistente.

Não altere código de produção para fazer o teste passar.

## Estratégia obrigatória

Use SQLite `:memory:` com conexão mantida aberta e migrations reais aplicadas.

Monte deliberadamente o cenário:

```text
Empresa Ativa do IEmpresaContext = 999
Insumo.EmpresaId = 999
Empresa de Id 999 = inexistente
```

O identificador pode ser outro valor positivo claramente inexistente.

Isso deve permitir que o guard cross-tenant passe e fazer a falha acontecer na FK do SQLite.

## Teste

Prefira uma classe dedicada:

```text
InsumoEmpresaForeignKeyTests
```

e um teste com nome próximo ao critério de aceitação:

```text
CA01_EmpresaId_inexistente_e_rejeitado_pela_fk_do_banco
```

O teste deve:

1. abrir SQLite `:memory:`;
2. aplicar `Database.MigrateAsync()`;
3. criar contexto com Empresa Ativa inexistente;
4. adicionar um Insumo com o mesmo `EmpresaId`;
5. executar `SaveChangesAsync`;
6. esperar `DbUpdateException`;
7. inspecionar a causa para comprovar violação de foreign key.

Se o provider expuser `SqliteException`, prefira validar o código/extended code correspondente a `SQLITE_CONSTRAINT_FOREIGNKEY`. Evite assert frágil baseado somente em texto de mensagem.

## Proibições

Não:

- alterar `Insumo`;
- alterar `Empresa`;
- alterar `PrecificadorDbContext`;
- alterar `InsumoConfiguration`;
- criar/editar migration;
- alterar ModelSnapshot;
- desligar foreign keys;
- desligar ou mudar o guard tenant-aware;
- usar EF InMemory;
- agregar outros cenários da MEL002/MEL003/MEL004;
- refatorar código não relacionado.

Se uma alteração de produção parecer necessária, **pare e reporte** em vez de ampliar a entrega.

## Matriz de testes

Esta melhoria introduz apenas:

- 1 teste de integração focado de FK.

Não criar teste unitário ou Web artificial.

Não remover nem enfraquecer testes existentes.

## Documentação pós-implementação

Ao concluir:

- alterar o status de `docs/development/improvements/MEL001-teste-fk-insumo-empresa.md` para `Concluída`;
- alterar MEL001 em `docs/development/melhorias.md` para `Concluída`;
- não alterar o status de MEL002–MEL004;
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
3. teste novo falharia caso a FK deixasse de existir;
4. nenhuma migration ou ModelSnapshot mudou;
5. nenhum arquivo de produção mudou;
6. MEL001 foi marcada como concluída;
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
test: valida fk de insumo para empresa
```

Se houver qualquer pendência, não escreva "nenhuma".

Não faça merge em `master`.
