# Instrução Codex — UC010 Desativar e reativar produto

Implemente o **UC010 — Desativar e reativar produto** do repositório `ajmarzola/Precificador`.

## Pré-condição de fila — concluída documentalmente

A UC009 já foi implementada, revisada e mergeada, e a UC010 foi revalidada contra a master real pós-UC009.

Antes de codificar, confirme que a master atual contém:

- UC009 marcado como Implementado;
- `Produto.AtualizarDados(...)`;
- `/Produtos`;
- `/Produtos/Detalhes/{id:int}`;
- `/Produtos/Editar/{id:int}`;
- link Editar em Detalhes;
- `TempData["MensagemSucesso"]` em Detalhes;
- Global Query Filter para Produto;
- nenhum método `Desativar()`/`Reativar()` em Produto ainda;
- nenhuma migration posterior criada especificamente para situação de Produto.

Se a master divergir materialmente desses pontos, não improvise: reporte a divergência antes de ampliar o escopo.

## Leitura obrigatória

Leia integralmente:

1. `AGENTS.md`;
2. `docs/use-cases/UC010-desativar-reativar-produto.md`;
3. `docs/use-cases/UC009-editar-produto.md`;
4. `docs/use-cases/UC008-listar-consultar-produtos.md`;
5. `docs/use-cases/UC007-cadastrar-produto.md`;
6. `docs/features/F002-produtos.md`;
7. `docs/business/business-rules.md`;
8. `docs/development/foundation-multiempresa-auth.md`;
9. `docs/development/testing-strategy.md`;
10. `docs/development/definition-of-done.md`;
11. `docs/development/melhorias.md`;
12. código real atual de Produto, Produtos/Index, Produtos/Detalhes, Produtos/Editar, DbContext e testes na master.

Use UC004/Insumos apenas como referência de padrão de domínio, Razor Pages, POST handlers, antiforgery, tenancy e PRG. Não copie regras específicas de Insumo.

## Branch

Use:

~~~text
feat/uc010-situacao-produto
~~~

Parta da master que contenha esta revalidação documental.

### Precondição obrigatória de Git

Antes de alterar qualquer arquivo:

1. confirme que a master é a base esperada;
2. crie/troque para `feat/uc010-situacao-produto`;
3. confirme que a branch atual não é `master`;
4. somente então edite arquivos.

Se não conseguir trabalhar na branch correta, não altere arquivos.

Nunca faça commit/push direto na master.

Não faça merge da própria implementação.

## Escopo

Entregar somente:

- `Produto.Desativar()`;
- `Produto.Reativar()`;
- ações Desativar/Reativar em Produtos/Detalhes;
- confirmação simples na desativação;
- POST handlers seguros;
- PRG + mensagens exatas;
- regressões de listagem/detalhes de Produto inativo;
- CA10/W6 de edição de Produto inativo sem reativação implícita;
- testes U1-U3, P1-P2 e W1-W10;
- atualização documental pós-implementação.

Não implementar UC011+.

## Domínio

Adicionar em `Produto`:

~~~csharp
public void Desativar()
public void Reativar()
~~~

Sem setter público de `Ativo`.

Semântica obrigatória:

~~~text
Desativar() -> Ativo = false
Reativar()  -> Ativo = true
~~~

As operações são idempotentes:

- desativar Produto já inativo não lança exceção;
- reativar Produto já ativo não lança exceção;
- chamadas repetidas terminam no estado solicitado.

Não alterar Id, EmpresaId, Nome, NomeNormalizado, Categoria ou MargemAlvo.

## Identidade e unicidade

`Ativo` não participa da identidade.

Preservar:

~~~text
EmpresaId + NomeNormalizado
~~~

Produto inativo continua ocupando o Nome na Empresa.

Não alterar índice único, configuração EF ou schema.

Reativar não exige nova validação de duplicidade.

## Web

Não criar nova Razor Page.

Usar:

~~~text
/Produtos/Detalhes/{id:int}
~~~

Adicionar handlers claros, preferencialmente:

~~~csharp
OnPostDesativarAsync(int id)
OnPostReativarAsync(int id)
~~~

### Produto ativo

Exibir somente a ação:

~~~text
Desativar
~~~

A desativação deve usar confirmação nativa:

~~~text
Deseja desativar este produto?
~~~

Não criar modal customizado nem dependência JavaScript adicional.

### Produto inativo

Exibir somente a ação:

~~~text
Reativar
~~~

Não exigir confirmação para reativar.

A ação **Editar** continua disponível tanto para Produto ativo quanto inativo.

## Mutação segura

Desativar/Reativar:

- somente por POST;
- antiforgery padrão do Razor Pages;
- rebuscar Produto por `id`;
- preservar Global Query Filter;
- não usar `IgnoreQueryFilters`;
- não receber `EmpresaId`;
- não receber booleano `Ativo`;
- inexistente => 404;
- cross-tenant => 404;
- manter guard central de escrita.

GET nunca altera situação.

## PRG e mensagens

Desativar:

~~~text
Produto desativado com sucesso.
~~~

Reativar:

~~~text
Produto reativado com sucesso.
~~~

Após sucesso:

~~~text
POST handler
    -> redirect
GET /Produtos/Detalhes/{id}
~~~

Reutilize o mecanismo atual de `TempData["MensagemSucesso"]`.

Não crie um segundo mecanismo de feedback.

## Comportamento preservado

Produto inativo continua:

- listado em `/Produtos`;
- consultável em `/Produtos/Detalhes/{id}`;
- exibindo Situação = Inativo;
- editável em `/Produtos/Editar/{id}`;
- participando da unicidade;
- pertencendo à mesma Empresa.

A edição pela UC009 não reativa implicitamente.

`Produto.AtualizarDados(...)` deve continuar preservando `Ativo`.

## CA10/W6 — cenário obrigatório revalidado

O cenário foi validado documentalmente contra a implementação real da UC009.

Implemente W6 sem bypass técnico:

1. criar Produto normalmente;
2. chamar `Produto.Desativar()`;
3. persistir normalmente;
4. GET Detalhes e confirmar link Editar;
5. GET `/Produtos/Editar/{id}` e confirmar acesso;
6. POST válido de edição;
7. confirmar PRG normal da UC009;
8. recarregar do banco;
9. confirmar dados editados;
10. confirmar `Ativo == false`.

É proibido fabricar Produto inativo por:

- SQL direto;
- reflection;
- setter artificial;
- alteração manual de backing field;
- `IgnoreQueryFilters` no fluxo Web;
- helper de teste que burle o domínio quando `Desativar()` já existe.

## Testes obrigatórios

Siga literalmente a matriz fechada em `docs/use-cases/UC010-desativar-reativar-produto.md`.

### Unitários — U1-U3

- U1: Desativar altera somente situação e preserva demais dados;
- U2: Reativar altera somente situação e preserva demais dados;
- U3: Desativar/Reativar são idempotentes.

### Persistência — P1-P2

- P1: round-trip ativo -> inativo -> ativo persiste no SQLite;
- P2: Produto inativo continua bloqueando duplicidade `EmpresaId + NomeNormalizado`.

### Web — W1-W10

- W1: ativo mostra Desativar e não Reativar;
- W2: inativo mostra Reativar e não Desativar;
- W3: POST Desativar persiste + PRG + mensagem;
- W4: POST Reativar persiste + PRG + mensagem;
- W5: inativo continua na listagem e nos detalhes;
- W6: inativo continua editável sem reativação implícita;
- W7: POST de id inexistente => 404 para as duas ações;
- W8: POST cross-tenant => 404 e registro intacto;
- W9: POST sem antiforgery é rejeitado e situação permanece intacta;
- W10: GET não altera situação.

Em W5, prove a linha correta do Produto na listagem; não use assert global frouxo apenas procurando a palavra "Inativo".

## Persistência e schema

Nenhuma migration.

Não alterar:

- migration `AddProdutos`;
- migrations históricas;
- `ProdutoConfiguration`;
- `PrecificadorDbContextModelSnapshot`;
- índice único existente.

Se surgir necessidade real de schema, interrompa e reporte antes de ampliar o escopo.

## Proibições

Não implementar:

- exclusão física;
- DELETE/hard delete;
- nova coluna de soft delete;
- DataDesativacao;
- MotivoDesativacao;
- UsuarioDesativacao;
- histórico de situação;
- auditoria;
- filtro por situação;
- bulk actions;
- preço de venda;
- histórico de preço de venda;
- Ficha Técnica;
- custo;
- margem atual;
- preço sugerido;
- regra de elegibilidade futura de Produto inativo em UC011+;
- API REST;
- generic repository;
- CQRS/MediatR;
- service layer artificial;
- refatoração oportunista sem relação com UC010;
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

1. branch de implementação correta;
2. build Release sem warnings novos relevantes;
3. suíte completa verde;
4. nenhuma migration/ModelSnapshot;
5. nenhum delete físico;
6. nenhum `IgnoreQueryFilters` no fluxo normal;
7. nenhum binding de `Ativo`;
8. antiforgery mantido;
9. cross-tenant/inexistente => 404;
10. inativos seguem listados, consultáveis e editáveis;
11. edição de inativo preserva `Ativo=false`;
12. unicidade continua incluindo inativos;
13. mensagens exatas e PRG;
14. nenhum UC011+.

## Documentação pós-implementação

Ao concluir:

- UC010 => `Implementado`;
- atualizar F002;
- atualizar catálogo;
- atualizar ordem de implementação;
- atualizar RN018 somente se necessário para refletir exatamente o comportamento implementado;
- UC011 passa a próximo caso de Produtos;
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
- ciclo reversível Ativo/Inativo de Produto
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: gerencia situacao do produto
~~~

Se houver pendência, não escreva "nenhuma".

Não faça merge em master.
