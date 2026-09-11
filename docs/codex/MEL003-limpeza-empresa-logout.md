# Instrução Codex — MEL003 Limpeza da Empresa Ativa no logout

Você está implementando a **MEL003 — Assert direto de limpeza da Empresa Ativa no logout** do repositório `ajmarzola/Precificador`.

## Regra de escopo

A única especificação normativa desta entrega é:

```text
docs/development/improvements/MEL003-limpeza-empresa-logout.md
```

Documentos de UCs ou melhorias posteriores presentes no repositório **não autorizam sua implementação**.

## Leitura obrigatória

Antes de alterar código, leia:

1. `AGENTS.md`;
2. `docs/development/improvements/MEL003-limpeza-empresa-logout.md`;
3. `docs/development/foundation-multiempresa-auth.md`;
4. `docs/development/testing-strategy.md`;
5. `docs/development/definition-of-done.md`;
6. `docs/development/melhorias.md`;
7. `src/Precificador.Web/Empresas/EmpresaContext.cs`;
8. `src/Precificador.Web/Pages/Conta/Logout.cshtml.cs`;
9. `tests/Precificador.Tests.Integration/Web/FluxosMultiempresaTests.cs`;
10. `tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj`.

## Branch

Use:

```text
test/mel003-limpeza-empresa-logout
```

Parta da `master` atualizada após o merge desta especificação.

## Objetivo técnico

Adicionar cobertura direta que prove que `EmpresaContext.Limpar()` remove completamente a Empresa Ativa da sessão.

Não altere código de produção.

## Teste obrigatório

Crie preferencialmente:

```text
tests/Precificador.Tests.Integration/Web/EmpresaContextTests.cs
```

Nome sugerido:

```text
CA01_Limpar_remove_id_nome_e_chaves_da_empresa_ativa
```

O teste deve:

1. criar `DefaultHttpContext`;
2. fornecer uma implementação mínima de `ISession` em memória no próprio teste;
3. atribuir essa sessão ao contexto;
4. construir `EmpresaContext` usando `HttpContextAccessor`;
5. executar `Definir(1, "Empresa teste")`;
6. afirmar que ID e Nome estão definidos;
7. executar `Limpar()`;
8. afirmar:
   - `EmpresaId == null`;
   - `EmpresaIdOuSentinela == -1`;
   - `Nome == null`;
   - `TryGetValue(EmpresaContext.ChaveSession, ...)` retorna false;
   - `TryGetValue(EmpresaContext.ChaveNomeSession, ...)` retorna false.

O teste não deve subir servidor, banco ou HTTP.

## Cobertura Web existente

Não substitua nem enfraqueça:

```text
Logout_limpa_sessao_e_impede_acesso_operacional
```

Ele deve continuar verde e permanece como prova do fluxo Web de logout.

Não crie outro teste Web agregado se não houver necessidade.

## Proibições

Não:

- alterar `EmpresaContext.cs`;
- alterar `Logout.cshtml.cs`;
- adicionar endpoint de diagnóstico;
- alterar pipeline de middleware;
- alterar configuração de Session/Identity;
- alterar cookies;
- criar migration/alterar ModelSnapshot;
- adicionar dependência de mocking apenas para esta melhoria;
- adicionar referência de `Precificador.Web` ao projeto de testes unitários;
- implementar MEL001/MEL002/MEL004;
- implementar UC003;
- criar helper/abstração compartilhada sem evidência de reutilização.

Se o teste exigir alteração de produção, **pare e reporte**.

## Matriz de testes

Esta melhoria adiciona somente:

- 1 teste focado de `EmpresaContext.Limpar`.

A suíte Web existente deve permanecer verde.

## Documentação pós-implementação

Ao concluir:

- alterar o status de `docs/development/improvements/MEL003-limpeza-empresa-logout.md` para `Concluída`;
- alterar MEL003 em `docs/development/melhorias.md` para `Concluída`;
- não alterar status de outras melhorias;
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
3. novo teste prova ID + Nome + chaves removidos;
4. teste Web de logout existente continua verde;
5. nenhum arquivo de produção foi alterado;
6. nenhuma migration/ModelSnapshot mudou;
7. MEL003 foi marcada como concluída;
8. diff restrito à melhoria.

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
test: valida limpeza da empresa ativa no logout
```

Se houver qualquer pendência, não escreva "nenhuma".

Não faça merge em `master`.
