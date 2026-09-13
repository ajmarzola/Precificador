# Instrução Codex — UC009 Editar produto

Implemente o **UC009 — Editar produto**.

## Pré-condição de fila

Não iniciar enquanto o UC008 não estiver implementado, revisado e mergeado na master.

Antes de codificar, confirme que a master atual contém:

- UC008 marcado como Implementado;
- Produto atual do UC007;
- /Produtos;
- /Produtos/Detalhes/{id:int};
- ProdutoInputModel;
- Global Query Filter para Produto;
- nenhuma desativação de Produto ainda.

## Leitura obrigatória

Leia integralmente:

1. AGENTS.md;
2. docs/use-cases/UC009-editar-produto.md;
3. docs/use-cases/UC007-cadastrar-produto.md;
4. docs/use-cases/UC008-listar-consultar-produtos.md;
5. docs/features/F002-produtos.md;
6. docs/business/business-rules.md;
7. docs/product/glossary.md;
8. docs/development/foundation-multiempresa-auth.md;
9. docs/development/testing-strategy.md;
10. docs/development/definition-of-done.md;
11. docs/development/melhorias.md;
12. código real atual de Produto, Produtos/Novo, Produtos/Detalhes, DbContext e testes na master.

Use Insumos/Editar apenas como referência de padrão Razor/tenancy/PRG; não copie RN040 ou regras específicas de Insumo.

## Branch

~~~text
feat/uc009-editar-produto
~~~

Parta da master pós-UC008.

### Precondição obrigatória de Git

Antes de alterar qualquer arquivo:

1. confirme que a master é a base esperada;
2. crie/troque para feat/uc009-editar-produto;
3. confirme que a branch atual não é master;
4. somente então edite arquivos.

Se não conseguir trabalhar na branch correta, não altere arquivos.

Nunca faça commit/push direto na master.

Não faça merge da própria implementação.

## Escopo

Entregar:

- comportamento de domínio para atualizar Produto;
- /Produtos/Editar/{id:int};
- edição de Nome;
- edição de Categoria;
- edição de Margem-alvo;
- duplicidade tenant-aware;
- PRG para Detalhes;
- mensagem de sucesso;
- link Editar em Detalhes;
- Cancelar para Detalhes;
- testes;
- docs pós-implementação.

Não implementar UC010+.

## Domínio

Adicionar método claro, por exemplo:

~~~csharp
public void AtualizarDados(string nome, decimal margemAlvo, string? categoria = null)
~~~

### Regra obrigatória de atomicidade

Normalize os candidatos e valide todos antes de atribuir propriedades.

Se qualquer validação falhar, o objeto deve permanecer exatamente no estado anterior.

Atualizar somente Nome, NomeNormalizado, Categoria e MargemAlvo.

Preservar Id, EmpresaId e Ativo.

Não adicione setter público.

## Regras cadastrais

### Nome

Aplicar RN041/RN042:

- obrigatório;
- max 120;
- trim;
- collapse whitespace;
- uppercase invariant no NomeNormalizado;
- acentos preservados.

### Categoria

Aplicar RN043:

- opcional;
- max 80;
- trim/collapse;
- whitespace => null.

### Margem-alvo

Aplicar RN019/RN045.

UI recebe percentual.

Domínio recebe fração.

~~~text
30% -> 0.30
25,5% -> 0.255
~~~

Validar 0 <= MargemAlvo < 1.

## Formulário

Reutilize ProdutoInputModel.

Não crie um segundo InputModel idêntico.

Se Novo e Editar passarem a duplicar validação de MargemAlvoPercentual obrigatória, mensagem de duplicidade ou mapeamento de exceção de domínio para ModelState, extraia somente um helper local específico da área Produto, por exemplo ProdutoFormulario.

Não crie framework genérico, base PageModel ou service CRUD.

A refatoração deve preservar comportamento e testes do UC007.

## GET

Rota:

~~~text
/Produtos/Editar/{id:int}
~~~

Carregar Produto da Empresa Ativa usando Global Query Filter.

Preferir AsNoTracking + projeção para o InputModel.

Popular Nome, Categoria e MargemAlvoPercentual = MargemAlvo * 100. Testes devem validar o valor percentual semanticamente, sem depender de ponto/vírgula textual específico do tag helper.

Inexistente/cross-tenant => 404.

Não usar IgnoreQueryFilters.

## POST

Fluxo:

1. carregar Produto pelo id usando GQF;
2. se ausente, 404;
3. validar campo percentual obrigatório;
4. validar ModelState;
5. converter percentual para fração;
6. chamar atualização de domínio;
7. mapear erros de domínio para campos corretos;
8. verificar duplicidade excluindo o próprio id;
9. salvar;
10. definir TempData com a mensagem exata Produto atualizado com sucesso.;
11. redirect para /Produtos/Detalhes/{id}.

Duplicidade:

~~~text
item.Id != id
AND item.NomeNormalizado == produto.NomeNormalizado
~~~

Mensagem exata:

~~~text
Já existe um produto cadastrado com esse nome.
~~~

Não mascarar qualquer DbUpdateException como duplicidade.

## Segurança / tenant

Não bindar EmpresaId, Ativo, NomeNormalizado ou Id como propriedade editável.

Campos extras manipulados no request não podem alterar EmpresaId/Ativo.

GET e POST cross-tenant => 404.

Não usar IgnoreQueryFilters no fluxo normal.

## Detalhes

Adicionar link/botão Editar para o Produto atual.

Adicionar leitura de TempData e exibir:

~~~text
Produto atualizado com sucesso.
~~~

após PRG.

Não adicionar Desativar.

## Edição

Exibir somente:

- Nome;
- Categoria;
- Margem-alvo (%);
- Salvar;
- Cancelar.

Cancelar -> Detalhes do mesmo Produto.

## Situação

Não implemente desativação.

Não crie método Desativar antecipadamente.

Não fabrique Produto inativo via SQL/reflection para testes.

UC010 tratará situação e interação futura com edição.

## Persistência

Nenhuma migration.

Não alterar migrations, PrecificadorDbContextModelSnapshot ou schema.

Se surgir necessidade real de schema, pare e reporte antes de ampliar o escopo.

## Testes

Siga literalmente a matriz fechada do UC009.

### Unitários

U1-U5:

- atualização válida;
- Nome;
- Categoria;
- Margem;
- atomicidade em falha.

### Persistência

P1-P3:

- round-trip da atualização;
- unique same tenant em rename;
- mesmo nome entre tenants permitido.

### Web

W1-W11:

- auth + Empresa Ativa;
- GET campos/margem;
- POST válido + PRG;
- inválidos um por cenário;
- próprio nome não duplica;
- duplicidade same tenant;
- nome em outro tenant permitido;
- request não controla EmpresaId/Ativo;
- inexistente GET/POST 404;
- cross-tenant GET/POST 404;
- Editar/Cancelar.

Não use asserts globais frouxos quando o teste precisa provar valores do Produto editado.

## Proibições

Não implementar:

- UC010 desativação/reativação;
- preço de venda;
- histórico de preço;
- histórico de margem;
- Ficha Técnica;
- custo;
- margem atual;
- preço sugerido;
- auditoria;
- API;
- generic repository;
- CQRS/MediatR;
- service layer artificial;
- migration.

## Validação

Execute:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Antes da PR confirme:

1. nenhuma migration/ModelSnapshot;
2. migrations históricas intocadas;
3. Produto.AtualizarDados é atômico;
4. EmpresaId/Ativo preservados;
5. duplicidade exclui próprio Id;
6. cross-tenant GET/POST 404;
7. request manipulado não controla tenant/status;
8. PRG + mensagem;
9. Detalhes possui Editar;
10. nenhum UC010+;
11. build Release sem warnings;
12. suíte completa verde.

## Documentação pós-implementação

- UC009 => Implementado;
- atualizar F002;
- catálogo;
- ordem;
- UC010 passa a próximo caso de Produtos;
- não alterar dependências materiais dos UCs futuros sem justificativa.

## Retorno obrigatório

~~~text
Implementação concluída

Resumo:
- ...

Validações:
- ...

Testes:
- Unitários: X/X
- Integração: X/X

Produção/schema:
- edição cadastral de Produto
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: edita produto
~~~

Se houver pendência, não escreva "nenhuma".

Não faça merge em master.
