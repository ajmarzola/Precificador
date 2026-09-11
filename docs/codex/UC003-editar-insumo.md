# Instrução Codex — UC003 Editar insumo

Você está implementando o **UC003 — Editar insumo** do repositório `ajmarzola/Precificador`.

## Regra de escopo

A única especificação normativa desta entrega é:

```text
docs/use-cases/UC003-editar-insumo.md
```

Documentos de UCs posteriores presentes no repositório **não autorizam sua implementação**.

## Leitura obrigatória

Antes de alterar código, leia integralmente:

1. `AGENTS.md`;
2. `docs/use-cases/UC003-editar-insumo.md`;
3. `docs/use-cases/UC002-listar-consultar-insumos.md`;
4. `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md`;
5. `docs/development/foundation-multiempresa-auth.md`;
6. `docs/features/F001-insumos.md`;
7. `docs/business/business-rules.md`;
8. `docs/architecture/architecture.md`;
9. `docs/development/testing-strategy.md`;
10. `docs/development/definition-of-done.md`;
11. `docs/development/melhorias.md`;
12. o estado atual das páginas e testes de Insumos na `master`.

Leia também qualquer MEL001–MEL004 que já esteja **implementada** na `master` e respeite o código resultante. Não reimplemente melhorias já concluídas.

## Branch

Use:

```text
feat/uc003-editar-insumo
```

Parta da `master` atualizada somente quando chegar a vez do UC003 na fila de implementação.

## Página

Implementar:

```text
/Insumos/Editar/{id:int}
```

Campos do formulário:

- Nome;
- Marca;
- Categoria;
- Unidade base;
- Observação.

Não incluir campos editáveis para:

- Id;
- EmpresaId;
- Ativo;
- NomeNormalizado;
- MarcaNormalizada.

O Id vem da rota.

## Domínio

Adicionar comportamento explícito de atualização a `Insumo`, preferencialmente:

```csharp
AtualizarDados(...)
```

ou nome equivalente claro.

Reutilize as mesmas regras de normalização/validação do cadastro.

### Obrigatório: atualização atômica

Normalize e valide todos os valores antes de alterar propriedades.

Se qualquer validação falhar, a entidade deve permanecer com todo o estado anterior.

Não torne setters públicos.

Não permita alterar `EmpresaId` ou `Ativo`.

## GET

- preservar Global Query Filter;
- não usar `IgnoreQueryFilters`;
- id inexistente ou de outro tenant => 404;
- preencher Input com valores atuais;
- leitura pode usar `AsNoTracking`;
- não revelar campos técnicos.

## POST

1. buscar novamente a entidade pelo id com filtro tenant ativo;
2. se não encontrada => 404;
3. validar input/enums;
4. aplicar atualização de domínio;
5. validar duplicidade excluindo o próprio `Id`;
6. persistir;
7. PRG.

Duplicidade funcional:

```text
Já existe um insumo cadastrado com esse nome e marca.
```

Não mapear qualquer `DbUpdateException` genericamente para duplicidade.

Após sucesso:

```text
Insumo atualizado com sucesso.
```

Redirecionar preferencialmente para detalhes.

## Multiempresa

Preserve FT002:

- Global Query Filter ativo;
- guard de escrita ativo;
- nenhum `EmpresaId` de request;
- sem `IgnoreQueryFilters` no fluxo comum;
- cross-tenant GET/POST => 404;
- nenhuma alteração na Empresa proprietária.

## Categoria/Unidade na UI

Use o mecanismo de apresentação existente na `master`.

Se MEL004 estiver implementada, reutilize o helper centralizado de rótulos/opções.

Não recrie mapeamentos duplicados.

## Navegação

Adicionar **Editar** aos detalhes.

Página Editar:

- Salvar;
- Cancelar/Voltar aos detalhes.

Não adicionar Desativar/Reativar.

## Persistência

**Não criar migration.**

Não alterar migrations históricas nem ModelSnapshot.

Se parecer necessário schema novo, pare e reporte.

## Matriz de testes obrigatória

A matriz já está fechada em `UC003-editar-insumo.md`.

### Unitários

Cobrir de forma focada:

- atualização válida + preservação de EmpresaId/Ativo;
- normalização;
- falha textual preserva estado;
- categoria/unidade inválida preserva estado.

### Persistência

Usar SQLite real/in-memory:

- edição válida persiste sem trocar Empresa;
- índice único rejeita edição para identidade duplicada na mesma Empresa.

### Web

Cobrir com testes separados/focados:

- autenticação;
- GET com campos permitidos e sem técnicos;
- POST válido + PRG + sucesso;
- próprio registro não gera falso duplicado;
- duplicidade same-tenant;
- combinação existente apenas em outro tenant é permitida e isolada;
- dados inválidos não persistem;
- GET/POST inexistente => 404;
- GET/POST cross-tenant => 404 e registro intacto;
- detalhes possui Editar.

Não criar um único teste agregando a maior parte dos critérios.

## Cinco regras de qualidade desta rodada

1. a matriz de testes acima é contrato antes da implementação;
2. a DoD inclui atualização documental pós-implementação;
3. nomes dos testes devem se aproximar dos CAs;
4. prefira testes pequenos a cenários agregados;
5. duplicação percebida fora do escopo deve virar MEL, não refatoração oportunista.

## Escopo proibido

Não implementar:

- UC004 desativação/reativação;
- UC005/UC006 preço/histórico;
- auditoria de alterações;
- edição em massa;
- exclusão;
- troca de Empresa;
- optimistic concurrency token;
- Produto;
- Ficha Técnica;
- API;
- mudança de auth/tenancy;
- refatorações não necessárias.

## Melhorias não bloqueantes

Se surgir ideia útil que não seja necessária ao UC003, registrar em:

```text
docs/development/melhorias.md
```

Não ampliar automaticamente a PR.

## Documentação pós-implementação

Ao concluir:

- mudar `docs/use-cases/UC003-editar-insumo.md` para `Implementado`;
- atualizar `docs/features/F001-insumos.md`;
- atualizar `docs/use-cases/catalog.md`;
- atualizar `docs/development/implementation-order.md`;
- indicar UC004 como próximo caso a detalhar/revalidar;
- não marcar UC004+ como implementado;
- preservar o alerta de revalidação de edição antes de UC005/UC014.

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
4. nenhum `IgnoreQueryFilters` no fluxo UC003;
5. GET/POST cross-tenant => 404;
6. EmpresaId e Ativo não podem ser editados;
7. duplicidade exclui o próprio registro;
8. testes estão focados e rastreáveis aos critérios;
9. documentação de estado foi atualizada;
10. ausência de UC004+.

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
- ...
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: edita dados cadastrais do insumo
```

Se houver qualquer pendência, não escreva "nenhuma".

Não faça merge em `master`.
