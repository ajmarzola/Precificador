# MEL018 — Redirecionar cadastros para os detalhes da entidade

- **Tipo:** UX / fluxo de trabalho
- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Prioridade:** média
- **Estado:** Concluído
- **Ordem na fila pendente:** 03
- **Dependências:** MEL015, MEL016, UC001, UC007
- **Bloqueia:** nenhum item por dependência técnica direta; antecede MEL019 pela fila normativa
- **Alteração de schema:** não
- **Migration:** não
- **Alteração de domínio:** não

## Objetivo

Após cadastrar com sucesso um Insumo ou Produto, conduzir o usuário diretamente para os Detalhes da entidade recém-criada.

Fluxos finais:

~~~text
POST /Insumos/Novo
    -> persistir Insumo
    -> GET /Insumos/Detalhes/{id}

/Produtos/Novo
    -> persistir Produto
    -> GET /Produtos/Detalhes/{id}
~~~

A mensagem de sucesso deve sobreviver ao Post/Redirect/Get por `TempData` e ser exibida no destino.

## Problema

Hoje, depois de um cadastro bem-sucedido, os dois fluxos retornam para a própria página de cadastro:

~~~text
/Insumos/Novo
/Produtos/Novo
~~~

Esse comportamento favorecia cadastros sequenciais, mas interrompe o fluxo predominante observado no teste manual: **cadastrar → complementar/consultar a entidade recém-criada**.

No estado atual do produto, os Detalhes já concentram as ações seguintes.

### Após cadastrar Insumo

Em Detalhes o usuário pode:

- conferir Nome/Marca/Categoria/Unidade/Observação;
- registrar preço;
- consultar preço vigente;
- acessar histórico de preços;
- editar;
- desativar.

### Após cadastrar Produto

Em Detalhes o usuário pode:

- conferir Nome/Categoria/Margem-alvo;
- acessar Ficha Técnica;
- detalhar precificação;
- registrar Preço de prateleira;
- consultar histórico de precificação;
- editar;
- desativar.

MEL018 apenas melhora a continuidade desse fluxo.

## Escopo

Alterar somente o comportamento pós-sucesso de:

~~~text
/Insumos/Novo
/Produtos/Novo
~~~

e adaptar testes/documentação diretamente afetados.

Não alterar:

- formulários;
- validações;
- regras de domínio;
- persistência;
- cálculos;
- Detalhes além do necessário para consumir mensagem de sucesso já existente;
- Ficha Técnica;
- preço;
- navegação global;
- listagens.

## Estado atual dos destinos

Os dois destinos já possuem suporte para:

~~~csharp
public string? MensagemSucesso => TempData["MensagemSucesso"] as string;
~~~

e já renderizam alerta de sucesso.

Portanto, MEL018 **não deve criar novo mecanismo de mensagens**.

Reutilizar o `TempData` já existente.

## Regra — Insumo

No POST válido de:

~~~text
/Insumos/Novo
~~~

preservar todas as validações e regras atuais.

Depois de:

~~~csharp
context.Insumos.Add(insumo);
await context.SaveChangesAsync();
~~~

o EF Core já terá preenchido:

~~~csharp
insumo.Id
~~~

Usar **esse Id da entidade persistida** no redirect:

~~~csharp
TempData["MensagemSucesso"] = "Insumo cadastrado com sucesso.";
return RedirectToPage("/Insumos/Detalhes", new { id = insumo.Id });
~~~

Resultado esperado:

~~~text
HTTP 302
Location: /Insumos/Detalhes/{idCriado}
~~~

### Proibições

Não obter o Id por:

- `Max(Id)`;
- nova query “último registro”;
- busca por Nome;
- busca por NomeNormalizado;
- valor vindo do request.

O Id correto é o valor gerado e materializado na própria entidade após `SaveChangesAsync`.

## Regra — Produto

No POST válido de:

~~~text
/Produtos/Novo
~~~

depois de:

~~~csharp
context.Produtos.Add(produto);
await context.SaveChangesAsync();
~~~

usar:

~~~csharp
produto.Id
~~~

e redirecionar:

~~~csharp
TempData["MensagemSucesso"] = "Produto cadastrado com sucesso.";
return RedirectToPage("/Produtos/Detalhes", new { id = produto.Id });
~~~

Resultado esperado:

~~~text
HTTP 302
Location: /Produtos/Detalhes/{idCriado}
~~~

Aplicam-se as mesmas proibições de busca indireta do Id.

## Post/Redirect/Get

MEL018 preserva PRG.

Fluxo:

~~~text
POST válido
    -> gravação concluída
    -> 302 para Detalhes
    -> GET Detalhes
~~~

Não retornar `Page()` após sucesso.

Não renderizar Detalhes diretamente dentro do POST.

Não redirecionar antes de `SaveChangesAsync`.

Isso garante:

- refresh no destino não repete o POST;
- URL final identifica a entidade criada;
- a página de Detalhes executa seu fluxo normal tenant-aware;
- mensagem via TempData aparece no primeiro GET.

## Mensagem de sucesso

Preservar exatamente:

~~~text
Insumo cadastrado com sucesso.
Produto cadastrado com sucesso.
~~~

As mensagens continuam sendo colocadas em `TempData["MensagemSucesso"]`.

Os Detalhes já consomem essa chave.

### Página Novo

Como o fluxo bem-sucedido deixa de retornar para a página Novo, os seguintes elementos passam a ser obsoletos nos dois cadastros:

~~~csharp
public string? MensagemSucesso => TempData["MensagemSucesso"] as string;
~~~

e o bloco Razor:

~~~text
@if (Model.MensagemSucesso is not null)
{
    ...
}
~~~

Remover esses elementos se não houver outro fluxo real que utilize essa mensagem na página Novo.

Não manter código morto apenas por compatibilidade com o comportamento anterior.

## Validação e erros

### POST inválido

Preservar o comportamento atual:

~~~text
POST inválido
-> HTTP 200
-> permanece em /Insumos/Novo ou /Produtos/Novo
-> exibe erros
-> não persiste
-> não define redirect
~~~

Não enviar o usuário aos Detalhes quando:

- InputModel inválido;
- domínio rejeita dados;
- duplicidade funcional detectada.

### Erro inesperado de persistência

Preservar tratamento/logging atual.

Não converter falha de banco em redirect.

Não definir mensagem de sucesso antes de saber que a persistência concluiu.

## Tenant / multiempresa

Preservar integralmente FT002.

### Cadastro

- `EmpresaId` continua vindo da Empresa Ativa;
- request não escolhe tenant;
- write guard continua ativo.

### Destino

O GET de Detalhes resolve a entidade pelo Global Query Filter.

Assim, o redirect correto deve levar ao Id da entidade recém-criada na Empresa Ativa.

Não usar `IgnoreQueryFilters`.

Não adicionar `EmpresaId` à URL.

## Insumo — continuidade funcional

O Insumo recém-criado é válido mesmo sem preço.

Depois do redirect, Detalhes pode exibir:

~~~text
Sem preço vigente.
~~~

Isso é comportamento correto conforme RN007/UC006.

MEL018 não deve:

- criar preço automático;
- abrir automaticamente o formulário de preço;
- preencher preço;
- alterar identidade consolidada;
- criar histórico.

A ação **Registrar preço** já disponível em Detalhes é suficiente.

## Produto — continuidade funcional

Produto recém-criado é válido sem:

- Ficha Técnica;
- custo calculável;
- Preço de prateleira.

O destino de Detalhes deve continuar suportando esse estado incompleto.

MEL018 não deve:

- criar Ficha Técnica automaticamente;
- definir Rendimento;
- abrir automaticamente Ficha Técnica;
- registrar preço;
- alterar Margem-alvo;
- recalcular/persistir precificação.

As ações já existentes em Detalhes são suficientes.

## Relação com MEL015/MEL016

MEL018 depende de MEL015/MEL016 apenas porque o fluxo manual seguinte utiliza as telas estabilizadas de preço do Insumo.

Não alterar:

- parsing decimal;
- apresentação monetária;
- terminologia “Quantidade por embalagem”;
- Data de referência default.

## Relação com MEL019

MEL019 vem depois na fila e definirá:

~~~text
Rendimento inicial da Ficha Técnica = 1
~~~

MEL018 não deve antecipar essa mudança.

## Documentação normativa

Atualizar para refletir o novo PRG:

- UC001 — Cadastrar Insumo;
- UC007 — Cadastrar Produto;
- F001 — Gestão de Insumos;
- F002 — Gestão de Produtos;
- MEL018;
- backlog.

Os documentos históricos Codex de UC001/UC007 podem preservar o comando original usado na época da implementação. A fonte para a alteração atual é MEL018 + seu guia Codex.

## Critérios de aceitação

- **CA01:** POST válido de Insumo persiste antes do redirect.
- **CA02:** Id gerado de Insumo é usado no destino.
- **CA03:** redirect de Insumo aponta exatamente para `/Insumos/Detalhes/{idCriado}`.
- **CA04:** GET do destino retorna 200 e exibe o Insumo criado.
- **CA05:** destino exibe `Insumo cadastrado com sucesso.`.
- **CA06:** Detalhes do Insumo mantém ações existentes, incluindo Registrar preço.
- **CA07:** página Novo de Insumo não mantém bloco/propriedade de sucesso obsoletos.
- **CA08:** POST inválido de Insumo permanece no formulário sem redirect.
- **CA09:** duplicidade de Insumo permanece no formulário sem redirect.
- **CA10:** POST válido de Produto persiste antes do redirect.
- **CA11:** Id gerado de Produto é usado no destino.
- **CA12:** redirect de Produto aponta exatamente para `/Produtos/Detalhes/{idCriado}`.
- **CA13:** GET do destino retorna 200 e exibe o Produto criado.
- **CA14:** destino exibe `Produto cadastrado com sucesso.`.
- **CA15:** Detalhes do Produto mantém as ações existentes de continuidade.
- **CA16:** página Novo de Produto não mantém bloco/propriedade de sucesso obsoletos.
- **CA17:** POST inválido de Produto permanece no formulário sem redirect.
- **CA18:** duplicidade de Produto permanece no formulário sem redirect.
- **CA19:** Produto recém-criado continua podendo estar com precificação incompleta sem erro.
- **CA20:** nenhuma Ficha/preço/histórico é criado automaticamente.
- **CA21:** EmpresaId continua vindo exclusivamente do contexto ativo.
- **CA22:** nenhum `IgnoreQueryFilters` é introduzido no fluxo.
- **CA23:** mesma Empresa/dados persistidos do comportamento anterior são preservados.
- **CA24:** nenhuma migration/schema/model snapshot é alterado.
- **CA25:** MEL019 não é antecipada.

## Matriz de testes

### Insumo

Atualizar `NovoInsumoPageTests`.

#### I1 — redirect usa o Id persistido

Cadastrar Insumo válido e buscar o registro persistido.

Validar:

~~~text
response.StatusCode == Redirect
response.Headers.Location == /Insumos/Detalhes/{insumo.Id}
~~~

Não aceitar somente:

~~~text
StartsWith("/Insumos/Detalhes/")
~~~

O teste deve provar o Id exato.

#### I2 — destino exibe entidade e sucesso

Seguir o Location e validar:

- HTTP 200;
- Nome do Insumo;
- mensagem `Insumo cadastrado com sucesso.`;
- ação `Registrar preço`.

#### I3 — dados continuam corretos

Preservar asserts existentes:

- EmpresaId;
- Nome/Marca;
- normalização;
- Categoria;
- Unidade base;
- Observação;
- Ativo.

#### I4 — inválido não redireciona

Pelo menos um cenário inválido deve comprovar:

~~~text
StatusCode == OK
Location == null
contagem de Insumos não aumenta
~~~

#### I5 — duplicado não redireciona

Confirmar:

- HTTP 200;
- Location null;
- mensagem de duplicidade;
- nenhum segundo registro.

### Produto

Atualizar `ProdutoPageTests`.

#### P1 — redirect usa o Id persistido

No cadastro válido:

~~~text
response.StatusCode == Redirect
response.Headers.Location == /Produtos/Detalhes/{produto.Id}
~~~

Usar o Id real recuperado/persistido.

#### P2 — destino exibe entidade e sucesso

Seguir Location:

- HTTP 200;
- Nome do Produto;
- mensagem `Produto cadastrado com sucesso.`;
- ação `Ficha técnica`;
- ação `Registrar preço de prateleira`.

#### P3 — estado incompleto é válido

Para Produto recém-criado sem Ficha/preço:

- GET Detalhes continua 200;
- não ocorre exceção;
- apresentação de custo/preço ausente permanece funcional.

Não criar dados apenas para fazer Detalhes funcionar.

#### P4 — dados continuam corretos

Preservar asserts existentes:

- EmpresaId;
- Nome;
- Categoria;
- MargemAlvo;
- Ativo.

#### P5 — inválido não redireciona

Confirmar:

~~~text
StatusCode == OK
Location == null
não persiste
~~~

#### P6 — duplicado não redireciona

Confirmar mensagem/contagem e ausência de Location.

### Multiempresa

#### T1

Cadastro em Empresa 2 deve redirecionar para o detalhe do Id criado na Empresa 2 e o GET autenticado nessa mesma Empresa deve retornar 200.

Não é necessário criar nova suíte se teste existente já provar ownership e o novo teste usar cliente tenant-aware corretamente.

### Regressão

Executar toda a suíte porque a mudança afeta fluxos usados por:

- testes de UC001/UC007;
- testes que criam dados pela UI;
- UC027 — MargemPadrao no cadastro de Produto.

Não enfraquecer testes existentes retirando asserts de persistência para apenas acomodar o novo Location.

## Implementação esperada

Mudança mínima.

### Insumo

De:

~~~csharp
TempData["MensagemSucesso"] = "Insumo cadastrado com sucesso.";
return RedirectToPage();
~~~

Para equivalente a:

~~~csharp
TempData["MensagemSucesso"] = "Insumo cadastrado com sucesso.";
return RedirectToPage("/Insumos/Detalhes", new { id = insumo.Id });
~~~

### Produto

De:

~~~csharp
TempData["MensagemSucesso"] = "Produto cadastrado com sucesso.";
return RedirectToPage();
~~~

Para equivalente a:

~~~csharp
TempData["MensagemSucesso"] = "Produto cadastrado com sucesso.";
return RedirectToPage("/Produtos/Detalhes", new { id = produto.Id });
~~~

Remover o consumo obsoleto de mensagem nas páginas Novo.

Não criar serviço/abstração para dois redirects simples.

## Persistência

Nenhuma alteração.

Não criar migration.

Não alterar:

- DbContext;
- configurations EF;
- ModelSnapshot;
- entidades Core.

## Fora do escopo

- botão “Cadastrar outro”;
- redirect configurável;
- preferência de usuário;
- wizard;
- autoabrir modal;
- autoabrir Ficha Técnica;
- autoabrir registro de preço;
- criação automática de Ficha/preço;
- MEL019;
- MEL017;
- MEL012;
- UC028+;
- alteração de layout de Detalhes;
- alteração de navegação global;
- mudança de regras de validação.

## Definition of Done específica

MEL018 está concluída quando:

- Insumo válido redireciona para seu próprio Detalhes;
- Produto válido redireciona para seu próprio Detalhes;
- os dois redirects usam o Id da entidade salva;
- mensagens de sucesso aparecem no destino;
- Detalhes retornam 200 no estado recém-criado;
- inválidos/duplicados permanecem no formulário;
- código morto de sucesso em Novo foi removido;
- nenhuma regra de persistência/domínio mudou;
- nenhuma migration foi criada;
- multiempresa permanece íntegra;
- matriz I1–I5, P1–P6 e T1 está coberta;
- documentação normativa está coerente;
- build Release tem 0 erros e sem warnings novos relevantes;
- suíte completa está verde;
- backlog altera somente MEL018 de `Pronto` para `Concluído` na PR de implementação.

## Branch sugerida

~~~text
fix/mel018-redirecionar-cadastros-detalhes
~~~

## Commit sugerido

~~~text
fix: redireciona cadastros para detalhes
~~~
