# MEL002 — Teste de tentativa de reatribuição de tenant no domínio

- **Status:** Concluída
- **Tipo:** melhoria técnica / hardening de domínio
- **Origem:** revisão da FT002
- **Dependências:** FT002 implementada
- **Não depende de:** MEL001, UC002, UC003 ou funcionalidades posteriores

## Objetivo

Adicionar um teste unitário focado que prove explicitamente que um `Insumo` já associado a uma Empresa não pode ser reatribuído para outra Empresa pelo domínio.

A melhoria não altera comportamento funcional, persistência, schema ou interface. O comportamento já existe em `Insumo.DefinirEmpresa`; falta apenas cobertura direta e legível dessa invariância.

## Contexto

O domínio atual possui:

```csharp
public void DefinirEmpresa(int empresaId)
{
    if (empresaId <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(empresaId));
    }

    if (EmpresaId != 0 && EmpresaId != empresaId)
    {
        throw new InvalidOperationException("O insumo já pertence a outra empresa.");
    }

    EmpresaId = empresaId;
}
```

A criação de `Insumo` chama `DefinirEmpresa`, portanto a entidade nasce com ownership explícito.

O risco não coberto diretamente é uma regressão futura que permita trocar a propriedade da entidade depois de criada.

## Regra protegida

Uma entidade tenant-owned já pertencente a uma Empresa não pode ser reatribuída a outra Empresa pelo domínio.

Para `Insumo`:

```text
EmpresaId atual = 1
tentativa de DefinirEmpresa(2)
=> rejeitar
=> EmpresaId continua 1
```

## Critérios de aceitação

### CA01 — Reatribuição para outra Empresa é rejeitada

**Dado** um Insumo criado para a Empresa 1  
**Quando** `DefinirEmpresa(2)` for chamado  
**Então** deve ser lançada `InvalidOperationException`.

### CA02 — Estado original é preservado

Após a tentativa rejeitada:

```text
Insumo.EmpresaId == 1
```

O teste deve provar que não houve mutação parcial antes da exceção.

### CA03 — Sem alteração de produção

O diff de implementação não deve alterar:

- `Insumo`;
- `IEntidadeEmpresa`;
- `PrecificadorDbContext`;
- guard de SaveChanges;
- configurações EF;
- migrations;
- páginas Web.

Se o teste falhar com o comportamento atual, interromper e reportar antes de alterar produção.

## Matriz de testes fechada antes da implementação

### Unitário — domínio

Implementar **um teste focado** no arquivo existente:

```text
tests/Precificador.Tests.Unit/Insumos/InsumoTests.cs
```

Nome sugerido, alinhado aos critérios de aceitação:

```text
CA01_Reatribuir_insumo_para_outra_empresa_e_rejeitado_e_preserva_empresa_original
```

O teste deve:

1. criar `Insumo` com `EmpresaId = 1`;
2. executar `DefinirEmpresa(2)`;
3. capturar `InvalidOperationException`;
4. afirmar que `EmpresaId` continua igual a `1`.

Não é obrigatório validar o texto exato da exceção; o contrato importante é tipo + invariância de estado.

### Integração

Nenhum teste de integração novo é necessário.

### Web

Nenhum teste Web novo é necessário.

## Granularidade

Não agregar esse cenário a testes de persistência ou autenticação.

O comportamento é puro de domínio e deve permanecer testável sem EF Core, banco ou HTTP.

## Persistência

Nenhuma alteração de persistência é esperada:

- sem migration;
- sem ModelSnapshot;
- sem alteração de configuration;
- sem banco temporário.

## Fora do escopo

- FK `Insumo -> Empresa` — MEL001;
- guard cross-tenant do DbContext;
- mudança de Empresa via reflexão ou manipulação interna de EF;
- logout/limpeza de Empresa Ativa — MEL003;
- centralização de labels — MEL004;
- edição de Insumo — UC003;
- generalizar ownership para novas entidades ainda inexistentes;
- refatorar `DefinirEmpresa`.

## Definition of Done específica

A MEL002 está concluída quando:

- CA01–CA03 estão atendidos;
- existe teste unitário focado da invariância de ownership;
- o teste prova exceção e preservação do `EmpresaId` original;
- nenhum teste de integração/Web artificial foi criado;
- nenhum código de produção foi alterado;
- toda a suíte permanece verde;
- build Release não introduz warnings relevantes;
- `docs/development/melhorias.md` marca MEL002 como `Concluída`;
- este documento passa para **Status: Concluída**;
- o diff final permanece restrito à MEL002.

## Resultado esperado do Codex

Ao concluir, o agente deve retornar:

- resumo objetivo do teste adicionado;
- invariância protegida;
- comandos de validação executados;
- totais de testes aprovados;
- confirmação de ausência de alteração de produção/schema;
- pendências, se houver;
- **mensagem de commit sugerida**.

Mensagem de commit sugerida:

```text
test: valida ownership imutavel do insumo
```
