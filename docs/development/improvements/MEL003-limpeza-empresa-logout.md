# MEL003 — Assert direto de limpeza da Empresa Ativa no logout

- **Status:** Pronto para implementação
- **Tipo:** melhoria técnica / hardening de autenticação e sessão
- **Origem:** revisão da FT002
- **Dependências:** FT002 implementada
- **Não depende de:** MEL001, MEL002, UC002, UC003 ou funcionalidades posteriores

## Objetivo

Complementar a cobertura existente do logout com uma verificação direta de que o contexto/sessão da Empresa Ativa é efetivamente limpo.

Hoje o fluxo Web já comprova que, após o logout, o usuário perde acesso operacional. A MEL003 deve provar também que as chaves técnicas da Empresa Ativa deixam de existir na sessão.

## Contexto atual

`LogoutModel.OnPostAsync` executa:

```csharp
empresaContext.Limpar();
await signInManager.SignOutAsync();
return RedirectToPage("/Conta/Login");
```

`EmpresaContext.Limpar()` remove:

```text
EmpresaAtivaId
EmpresaAtivaNome
```

A suíte atual possui um teste Web de logout que valida redirecionamento e perda de acesso protegido, mas não verifica diretamente o conteúdo da sessão.

## Regra protegida

Depois que `EmpresaContext.Limpar()` é executado:

```text
EmpresaId == null
Nome == null
EmpresaIdOuSentinela == -1
sessão não contém EmpresaAtivaId
sessão não contém EmpresaAtivaNome
```

O teste Web existente de logout continua responsável por provar o comportamento operacional do endpoint.

## Estratégia normativa

Não adicionar endpoint de diagnóstico à aplicação.

Não alterar código de produção.

Adicionar um teste focado sobre `EmpresaContext`, usando um `HttpContext` de teste com uma implementação simples de `ISession` em memória.

O teste deve:

1. criar sessão vazia em memória;
2. associá-la a um `DefaultHttpContext`;
3. construir `EmpresaContext` com `IHttpContextAccessor`;
4. chamar `Definir(1, "Empresa teste")`;
5. comprovar que ID e nome estão definidos;
6. chamar `Limpar()`;
7. comprovar diretamente que ID e nome foram removidos da sessão/contexto.

Esse teste complementa, sem substituir, `Logout_limpa_sessao_e_impede_acesso_operacional`.

## Critérios de aceitação

### CA01 — Limpar remove EmpresaId

**Dado** um `EmpresaContext` com Empresa Ativa definida  
**Quando** `Limpar()` for chamado  
**Então** `EmpresaId` deve ser `null`  
**E** `EmpresaIdOuSentinela` deve ser `-1`.

### CA02 — Limpar remove Nome

Após `Limpar()`:

```text
Nome == null
```

### CA03 — Chaves de sessão deixam de existir

Após `Limpar()`, a sessão não deve conter:

```text
EmpresaContext.ChaveSession
EmpresaContext.ChaveNomeSession
```

### CA04 — Fluxo Web existente continua válido

O teste existente:

```text
Logout_limpa_sessao_e_impede_acesso_operacional
```

deve continuar verde, comprovando que o endpoint de logout mantém o comportamento operacional esperado.

### CA05 — Sem alteração de produção

O diff de implementação não deve alterar:

- `LogoutModel`;
- `EmpresaContext`;
- configuração de Session;
- configuração de Identity;
- middleware/pipeline;
- páginas Web;
- migrations ou ModelSnapshot.

Se a cobertura direta não puder ser implementada sem alteração de produção, interromper e reportar antes de ampliar o escopo.

## Matriz de testes fechada antes da implementação

### Teste focado de contexto/sessão

Criar preferencialmente:

```text
tests/Precificador.Tests.Integration/Web/EmpresaContextTests.cs
```

O projeto de integração já referencia `Precificador.Web`, portanto não é necessário adicionar referência Web ao projeto de testes unitários.

Nome sugerido:

```text
CA01_Limpar_remove_id_nome_e_chaves_da_empresa_ativa
```

Apesar de estar no projeto de integração por dependência ao projeto Web, o teste deve ser pequeno e isolado, sem subir servidor, banco ou HTTP.

Implementar no próprio arquivo de teste uma `ISession` mínima em memória, caso não exista helper reutilizável adequado.

Não criar abstração compartilhada apenas para este cenário.

### Integração Web

Não criar um segundo fluxo agregado de logout.

Manter o teste existente de `FluxosMultiempresaTests` como confirmação do comportamento operacional.

### Unitários de Core

Nenhum teste novo é necessário.

## Granularidade

O novo teste deve ter uma responsabilidade clara: provar a limpeza direta das chaves da Empresa Ativa.

Não misturar login, criação de usuário, seleção de empresa, banco ou autorização nesse teste.

## Fora do escopo

- alterar comportamento de login/logout;
- limpar toda a Session com `Session.Clear()`;
- alterar nomes das chaves de sessão;
- introduzir endpoint de inspeção/teste;
- alterar cookies;
- alterar `SignInManager`;
- MEL001, MEL002 ou MEL004;
- UC003;
- refatorar `EmpresaContext`.

## Definition of Done específica

A MEL003 está concluída quando:

- CA01–CA05 estão atendidos;
- existe teste direto e focado da limpeza de Empresa Ativa;
- o teste prova remoção de ID, Nome e ambas as chaves da sessão;
- o teste Web existente de logout permanece verde;
- nenhum endpoint/test hook foi introduzido em produção;
- nenhum código de produção/schema foi alterado;
- toda a suíte permanece verde;
- build Release não introduz warnings relevantes;
- `docs/development/melhorias.md` marca MEL003 como `Concluída`;
- este documento passa para **Status: Concluída**;
- o diff final permanece restrito à MEL003.

## Resultado esperado do Codex

Ao concluir, o agente deve retornar:

- resumo objetivo;
- teste criado e garantia comprovada;
- comandos de validação executados;
- totais de testes aprovados;
- confirmação de ausência de alteração de produção/schema;
- pendências, se houver;
- **mensagem de commit sugerida**.

Mensagem de commit sugerida:

```text
test: valida limpeza da empresa ativa no logout
```
