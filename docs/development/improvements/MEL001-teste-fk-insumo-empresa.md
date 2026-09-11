# MEL001 — Teste explícito da FK Insumo → Empresa

- **Status:** Concluída
- **Tipo:** melhoria técnica / hardening de persistência
- **Origem:** revisão da FT002
- **Dependências:** FT002 implementada
- **Não depende de:** UC002, UC003 ou funcionalidades posteriores

## Objetivo

Adicionar um teste de integração focado que prove explicitamente que a chave estrangeira de `Insumo.EmpresaId` para `Empresas.Id` é realmente aplicada pelo SQLite.

A melhoria não altera comportamento funcional, schema, domínio ou interface. Seu objetivo é aumentar a confiança na integridade referencial já definida em `InsumoConfiguration`.

## Contexto

A configuração atual declara:

```csharp
builder.HasOne<Empresa>()
    .WithMany()
    .HasForeignKey(insumo => insumo.EmpresaId)
    .OnDelete(DeleteBehavior.Restrict);
```

A suíte já cobre:

- guard de escrita cross-tenant;
- query filter por Empresa;
- persistência válida de Insumo;
- migrations e índice único.

Falta um teste que prove diretamente que o banco rejeita um `Insumo` apontando para uma Empresa inexistente.

## Risco coberto

Sem esse teste, uma regressão na configuração/migration da FK poderia passar despercebida mesmo com o guard de tenant funcionando corretamente.

O teste deve separar claramente as duas responsabilidades:

- **guard da aplicação:** impede escrita em tenant diferente da Empresa Ativa;
- **FK do banco:** impede referência a uma Empresa que não existe.

Para provar a FK, o teste deve evitar ser bloqueado antes pelo guard.

## Estratégia normativa do teste

Usar SQLite real `:memory:` com conexão mantida aberta e migrations aplicadas.

Criar o `PrecificadorDbContext` com um `IEmpresaContext` cujo `EmpresaId` seja um identificador positivo inexistente no banco, por exemplo `999`.

Criar um `Insumo` com esse mesmo `EmpresaId`:

```text
Empresa Ativa do contexto = 999
Insumo.EmpresaId = 999
Empresas.Id = 999 não existe
```

Assim:

1. o guard de tenant aceita a operação porque o `EmpresaId` do Insumo coincide com a Empresa Ativa;
2. o SQLite recebe o INSERT;
3. a FK `Insumos.EmpresaId -> Empresas.Id` deve rejeitar a persistência.

## Critérios de aceitação

### CA01 — FK rejeita Empresa inexistente

**Dado** banco SQLite migrado  
**E** contexto de Empresa Ativa com `EmpresaId` positivo que não existe em `Empresas`  
**E** um novo Insumo com o mesmo `EmpresaId`  
**Quando** `SaveChangesAsync` for executado  
**Então** a persistência deve falhar com `DbUpdateException` causada por violação de FK do SQLite.

O teste deve comprovar que a falha é de foreign key, não apenas qualquer erro de persistência.

### CA02 — Guard não é o motivo da falha

O cenário deve usar o mesmo `EmpresaId` no contexto e no Insumo para que a rejeição não seja causada pelo guard cross-tenant.

Não desabilitar ou alterar o guard de produção para executar o teste.

### CA03 — Sem alteração de produção

O diff de implementação não deve alterar:

- `Insumo`;
- `Empresa`;
- `PrecificadorDbContext`;
- `InsumoConfiguration`;
- migrations;
- ModelSnapshot;
- páginas Web.

Se o teste só passar mediante alteração de produção, interromper e reportar antes de ampliar o escopo.

## Matriz de testes fechada antes da implementação

### Integração — persistência

Implementar **um teste focado**, preferencialmente em uma classe dedicada como:

```text
InsumoEmpresaForeignKeyTests
```

Nome sugerido, alinhado ao critério de aceitação:

```text
CA01_EmpresaId_inexistente_e_rejeitado_pela_fk_do_banco
```

O teste deve:

1. abrir conexão SQLite `:memory:`;
2. aplicar migrations;
3. usar `IEmpresaContext` com Empresa inexistente, mas positiva;
4. adicionar Insumo com o mesmo `EmpresaId`;
5. executar `SaveChangesAsync`;
6. capturar `DbUpdateException`;
7. validar que a causa interna é `SqliteException` de constraint/foreign key.

Quando viável com a versão atual do provider, preferir assert do código estendido SQLite de FK (`SQLITE_CONSTRAINT_FOREIGNKEY`) em vez de depender de texto de mensagem.

### Unitários

Nenhum teste unitário novo é necessário.

### Web

Nenhum teste Web novo é necessário.

## Granularidade

Não adicionar esse cenário a um teste agregado com múltiplas responsabilidades.

A melhoria existe justamente para provar uma garantia de persistência específica; o teste deve falhar com causa clara quando essa garantia quebrar.

## Persistência

- não criar migration;
- não editar migrations históricas;
- não alterar ModelSnapshot;
- não executar DDL manual para criar a FK no teste;
- usar as migrations reais da aplicação.

## Fora do escopo

- testar reatribuição de tenant — MEL002;
- testar logout/limpeza de Empresa Ativa — MEL003;
- refatorar labels de UI — MEL004;
- alterar `DeleteBehavior`;
- testar exclusão de Empresa com Insumos;
- criar CRUD de Empresa;
- alterar guard de `SaveChanges`;
- adicionar abstrações de persistência.

## Definition of Done específica

A MEL001 está concluída quando:

- CA01–CA03 estão atendidos;
- existe teste de integração focado da FK;
- o teste usa SQLite real/in-memory, não EF InMemory;
- a falha é comprovadamente de foreign key;
- não houve mudança de código de produção/schema;
- toda a suíte permanece verde;
- build Release não introduz warnings relevantes;
- `docs/development/melhorias.md` marca MEL001 como `Concluída`;
- este documento passa para **Status: Concluída**;
- o diff final permanece restrito à MEL001.

## Resultado esperado do Codex

Ao concluir, o agente deve retornar:

- resumo objetivo do que foi alterado;
- teste criado e garantia que ele prova;
- comandos de validação executados;
- totais de testes aprovados;
- confirmação de ausência de alteração de produção/migration;
- pendências, se houver;
- **mensagem de commit sugerida**.

Mensagem de commit sugerida para esta melhoria:

```text
test: valida fk de insumo para empresa
```
