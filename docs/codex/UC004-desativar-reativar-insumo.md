# Instrução Codex — UC004 Desativar e reativar insumo

Você está implementando o **UC004 — Desativar e reativar insumo** do repositório `ajmarzola/Precificador`.

## Regra de escopo

A única especificação normativa desta entrega é:

```text
docs/use-cases/UC004-desativar-reativar-insumo.md
```

Não implemente UC005+.

## Leitura obrigatória

Antes de alterar código, leia integralmente:

1. `AGENTS.md`;
2. `docs/use-cases/UC004-desativar-reativar-insumo.md`;
3. `docs/use-cases/UC003-editar-insumo.md`;
4. `docs/use-cases/UC002-listar-consultar-insumos.md`;
5. `docs/business/business-rules.md`;
6. `docs/features/F001-insumos.md`;
7. `docs/development/foundation-multiempresa-auth.md`;
8. `docs/development/testing-strategy.md`;
9. `docs/development/definition-of-done.md`;
10. `docs/development/melhorias.md`;
11. estado atual de `Insumo`, Detalhes, Editar e testes relacionados na `master`.

## Branch

Use:

```text
feat/uc004-situacao-insumo
```

Parta da `master` atualizada após o merge desta especificação.

## Domínio

Adicionar:

```csharp
public void Desativar()
public void Reativar()
```

Sem setter público de `Ativo`.

As operações são idempotentes:

- `Desativar()` sempre termina com `Ativo = false`;
- `Reativar()` sempre termina com `Ativo = true`.

Não alterar outros campos.

## Web

Não criar nova página.

Usar:

```text
/Insumos/Detalhes/{id:int}
```

Adicionar handlers:

```csharp
OnPostDesativarAsync(int id)
OnPostReativarAsync(int id)
```

ou nomes equivalentes claros.

### Estado ativo

Mostrar somente:

```text
Desativar
```

A desativação deve ter confirmação simples nativa:

```text
Deseja desativar este insumo?
```

### Estado inativo

Mostrar somente:

```text
Reativar
```

Não criar modal customizado.

## Mutação segura

As duas ações:

- são POST;
- usam antiforgery padrão;
- rebuscam o Insumo pelo id;
- preservam Global Query Filter;
- não usam `IgnoreQueryFilters`;
- não recebem `EmpresaId`;
- não recebem booleano `Ativo`;
- inexistente/cross-tenant => 404.

## PRG e mensagens

Desativar:

```text
Insumo desativado com sucesso.
```

Reativar:

```text
Insumo reativado com sucesso.
```

Após sucesso, redirecionar de volta para detalhes.

## Comportamento preservado

Não alterar:

- listagem: ativos e inativos continuam visíveis;
- edição: Insumo inativo continua editável;
- unicidade: inativos continuam participando do índice;
- Nome/Marca/Categoria/Unidade/Observação;
- EmpresaId.

## Testes obrigatórios

Siga a matriz fechada da especificação.

### Unitários

Cobrir:

- Desativar altera apenas Ativo;
- Reativar altera apenas Ativo;
- idempotência.

### Persistência

SQLite real/in-memory:

- round-trip ativo -> inativo -> ativo persiste;
- Insumo inativo continua bloqueando duplicidade Nome+Marca no mesmo tenant.

### Web

Cobrir de forma focada:

- ativo mostra Desativar e não Reativar;
- inativo mostra Reativar e não Desativar;
- POST Desativar + PRG + mensagem + persistência;
- POST Reativar + PRG + mensagem + persistência;
- inativo continua listado e editável;
- ID inexistente => 404;
- cross-tenant => 404 e registro intacto;
- POST sem antiforgery => rejeitado e estado intacto.

## Cinco regras de qualidade

1. a matriz de testes é contrato antes da implementação;
2. DoD inclui atualização documental;
3. nomes dos testes devem refletir CAs;
4. prefira testes focados e diagnósticos;
5. melhoria fora do escopo vai para `melhorias.md`, não vira refatoração oportunista.

## Proibições

Não:

- criar migration;
- alterar ModelSnapshot;
- deletar Insumo;
- criar campo DataDesativacao/Motivo;
- criar auditoria;
- implementar filtros de situação;
- bloquear edição de inativos;
- alterar índice único;
- implementar UC005+;
- implementar Ficha Técnica;
- refatorar Novo/Editar pela MEL005;
- adicionar serviço/camada desnecessária.

## Documentação pós-implementação

Ao concluir:

- mudar UC004 para `Implementado`;
- atualizar `docs/business/business-rules.md` se necessário para refletir exatamente o comportamento implementado;
- atualizar `docs/features/F001-insumos.md`;
- atualizar `docs/use-cases/catalog.md`;
- atualizar `docs/development/implementation-order.md`;
- indicar UC005 como próximo caso a detalhar/revalidar;
- preservar o gate de revalidação de edição antes de UC005/UC014.

## Validação

Execute:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Antes da PR confirme:

1. build Release sem warnings novos relevantes;
2. suíte completa verde;
3. nenhuma migration/ModelSnapshot;
4. nenhum delete físico;
5. nenhum `IgnoreQueryFilters`;
6. nenhum binding de `Ativo`;
7. antiforgery mantido;
8. inativos seguem visíveis/editáveis;
9. documentação atualizada;
10. ausência de UC005+.

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
- alteração de domínio e fluxo Web de situação
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: gerencia situacao do insumo
```

Se houver qualquer pendência, não escreva "nenhuma".

Não faça merge em `master`.
