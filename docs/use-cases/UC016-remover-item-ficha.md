# UC016 — Remover item da Ficha Técnica

- **Funcionalidade:** F003 — Ficha Técnica
- **Dependências materiais:** UC014, UC015 e MEL010 implementados
- **Alteração de schema:** não
- **Revalidação pós-MEL010:** concluída contra a implementação real de `IdentidadeConsolidada`

## Objetivo

Permitir remover um `ItemFichaTecnica` da composição atual de uma Ficha Técnica sem remover o Insumo, sem remover a Ficha e sem reabrir a identidade consolidada do Insumo.

A operação representa alteração da composição atual da Ficha.

## Decisão central

`ItemFichaTecnica` não possui histórico/versionamento no MVP.

Portanto, remover um Item significa excluí-lo fisicamente da composição atual:

~~~text
context.ItensFichaTecnica.Remove(item)
SaveChangesAsync()
~~~

Não criar flag `Ativo` no Item, soft delete, histórico de composição ou evento de remoção neste UC.

## Relação com MEL010 / RN048 / RN051

A remoção nunca altera:

~~~text
Insumo.IdentidadeConsolidada
~~~

Se o Insumo já foi usado em Ficha, sua identidade permanece consolidada mesmo quando:

- o Item removido era a última referência atual em Fichas;
- o Insumo não possui qualquer preço;
- não resta nenhum outro Item apontando para ele.

UC016 não deve:

- contar referências remanescentes;
- consultar `PrecosInsumos` para decidir desbloqueio;
- chamar qualquer lógica equivalente a `DesconsolidarIdentidade`;
- alterar Nome, Marca ou Unidade base do Insumo;
- alterar `IdentidadeConsolidada`.

## Relação com o estado atual da Ficha

Ficha Técnica continua sendo o registro atual do processo produtivo.

Remover o último Item é permitido.

Nesse caso:

- a `FichaTecnica` permanece existente;
- `Rendimento` permanece inalterado;
- `TempoAtivoMinutos` permanece inalterado;
- a composição passa a ter zero Itens;
- Produto permanece associado à mesma Ficha.

Não excluir automaticamente a Ficha vazia.

## Produto e Insumo inativos

Remoção é permitida quando:

- Produto está ativo ou inativo;
- Insumo do Item está ativo ou inativo.

A remoção:

- não reativa Produto;
- não reativa Insumo;
- não altera qualquer outro dado cadastral.

Essa decisão é coerente com UC015: composição existente continua administrável mesmo após inativação.

## Navegação

A página atual:

~~~text
/Produtos/FichaTecnica/{produtoId:int}
~~~

já possui a lista operacional mínima criada no UC015.

Adicionar ação `Remover` para cada Item.

Não transformar a lista em consulta completa do UC017.

## Rota

Criar página:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Remover/{itemId:int}
~~~

Arquivos esperados:

~~~text
Pages/Produtos/FichaTecnica/Itens/Remover.cshtml
Pages/Produtos/FichaTecnica/Itens/Remover.cshtml.cs
~~~

## GET — confirmação

O GET não altera dados.

Deve resolver a cadeia tenant-aware:

~~~text
Produto da Empresa Ativa
    -> FichaTecnica do Produto
        -> ItemFichaTecnica pertencente à Ficha
            -> Insumo referenciado
~~~

Sem `IgnoreQueryFilters` no fluxo Web.

Se qualquer elo da cadeia não existir no tenant ativo, retornar `404`.

A tela de confirmação deve exibir, no mínimo:

- Produto;
- situação do Produto;
- Insumo;
- Marca;
- Unidade base;
- situação do Insumo;
- Quantidade;
- Observação contextual, usando `—` quando ausente.

Texto recomendado:

~~~text
Tem certeza que deseja remover este item da ficha técnica?
~~~

Pode haver texto auxiliar esclarecendo que a ação altera a composição atual.

A tela deve possuir:

- botão `Remover`;
- link `Cancelar` de volta para a Ficha.

## POST — remoção

O POST deve resolver novamente toda a cadeia a partir dos ids da rota.

Não confiar em ids enviados por formulário.

Fluxo:

1. obter `produtoId` e `itemId` da rota;
2. carregar Produto pelo Global Query Filter;
3. carregar Ficha do Produto;
4. carregar o Item rastreado, exigindo `Item.FichaTecnicaId == Ficha.Id`;
5. opcionalmente carregar Insumo apenas para reconstrução da página/segurança de cadeia;
6. remover somente o Item;
7. `SaveChangesAsync()`;
8. definir mensagem de sucesso;
9. redirecionar para `/Produtos/FichaTecnica/{produtoId}`.

Mensagem de sucesso:

~~~text
Item removido da ficha técnica com sucesso.
~~~

## Segurança de ownership

A segurança deve reutilizar os mecanismos existentes:

- autenticação/Empresa Ativa;
- Global Query Filters;
- encadeamento Produto -> Ficha -> Item;
- guard central de escrita do `PrecificadorDbContext` para estado `Deleted`.

Não utilizar `IgnoreQueryFilters` na página.

### Item de outra Ficha da mesma Empresa

Mesmo que o `itemId` exista no tenant ativo, se ele não pertencer à Ficha do `produtoId` da rota, retornar `404`.

### Outro tenant

Produto, Ficha ou Item de outra Empresa devem resultar em `404` pelo fluxo Web.

O request nunca define `EmpresaId`.

## Request manipulado

A página não precisa de `InputModel` com ids de domínio.

Mesmo que o cliente envie campos extras como:

~~~text
EmpresaId
FichaTecnicaId
InsumoId
ItemId
Input.EmpresaId
Input.FichaTecnicaId
Input.InsumoId
Input.ItemId
~~~

eles devem ser ignorados.

Somente os ids da rota e a cadeia carregada do banco determinam qual Item pode ser removido.

## Antiforgery

POST válido usa antiforgery normal de Razor Pages.

POST sem token deve resultar em `400 Bad Request` e não remover Item.

Não desabilitar antiforgery.

## Concorrência simples

Se o Item não existir no momento do POST, retornar `404`.

Não transformar remoção repetida em sucesso idempotente neste UC.

Não adicionar controle de concorrência/version token.

## Domínio

Não adicionar método `Remover` em `ItemFichaTecnica` apenas para chamar `DbSet.Remove`.

A remoção não representa transição interna da entidade; a entidade deixa de existir na composição atual.

Não alterar:

- `ItemFichaTecnica.AtualizarDados`;
- `FichaTecnica`;
- `Insumo`;
- `Produto`.

## Persistência

Não há migration.

A FK e os índices atuais permanecem.

O guard central `AplicarIsolamentoEmpresa` já considera `EntityState.Deleted` e deve continuar protegendo remoções cross-tenant.

`ValidarReferenciasDosItensFichaTecnica` continua restrito a Added/Modified; não há necessidade de validar referências ao deletar um Item válido.

## Impacto em cálculo futuro

UC016 não calcula custos.

Após a remoção, UCs futuros que consultarem a composição atual naturalmente deixarão de considerar o Item removido.

Não persistir recálculo, total ou custo neste UC.

## Critérios de aceitação

### CA01

A lista operacional da Ficha oferece ação Remover para cada Item.

### CA02

GET da remoção exige autenticação/Empresa Ativa e não altera dados.

### CA03

GET válido apresenta dados suficientes para identificar Produto, Item e Insumo.

### CA04

POST válido remove somente o Item selecionado e redireciona para a Ficha.

### CA05

Após remoção, a Ficha exibe a mensagem `Item removido da ficha técnica com sucesso.`

### CA06

Remover um entre vários Itens preserva os demais.

### CA07

Remover o último Item é permitido e mantém a Ficha existente com zero Itens.

### CA08

Produto inativo permite remoção e permanece inativo.

### CA09

Insumo inativo permite remoção e permanece inativo.

### CA10

Remoção não altera `Insumo.IdentidadeConsolidada`.

### CA11

Mesmo após remover a última referência de Ficha de um Insumo sem preço, Nome/Marca/Unidade continuam protegidos pela RN051.

### CA12

Produto inexistente, Ficha inexistente, Item inexistente ou Item de outra Ficha retornam `404`.

### CA13

Dados de outro tenant não podem ser lidos nem removidos.

### CA14

Campos extras/manipulados no POST não alteram qual Item será removido.

### CA15

POST sem antiforgery retorna `400` e preserva o Item.

### CA16

A operação não exclui Produto, Ficha ou Insumo.

## Matriz de testes

### Persistência / tenant guard

- P1: remover Item próprio persiste exclusão;
- P2: exclusão preserva Produto, Ficha e Insumo;
- P3: tentativa técnica de deletar `ItemFichaTecnica` de outra Empresa é rejeitada pelo guard central;
- P4: exclusão não altera `IdentidadeConsolidada` do Insumo.

### Web

- W1: rota de remoção exige autenticação e Empresa Ativa;
- W2: GET válido exibe dados do Produto/Item/Insumo e não remove;
- W3: lista da Ficha contém link Remover correto;
- W4: POST válido remove Item, faz PRG e exibe mensagem de sucesso;
- W5: remover um Item preserva os demais;
- W6: remover último Item mantém Ficha existente e lista vazia;
- W7: Produto inativo permite remoção sem reativação;
- W8: Insumo inativo permite remoção sem reativação;
- W9: Produto/Ficha/Item inexistente retorna 404;
- W10: Item de outra Ficha da mesma Empresa retorna 404;
- W11: Produto/Item de outro tenant retorna 404 e registro permanece;
- W12: request com ids extras/manipulados não remove outro Item;
- W13: POST sem antiforgery retorna 400 e preserva Item;
- W14: remoção mantém `IdentidadeConsolidada = true`; 
- W15: após remover última referência de Ficha de Insumo sem preço, edição continua bloqueando Nome/Marca/Unidade.

Não são necessários testes unitários novos se nenhuma regra de domínio for adicionada.

## Alterações esperadas

### Web

- nova página `Itens/Remover`;
- ação Remover na lista operacional de `FichaTecnica.cshtml`;
- ajuste mínimo do PageModel da Ficha apenas se necessário para navegação/apresentação.

### Testes

- ampliar `ItemFichaTecnicaPageTests` ou criar arquivo específico `RemoverItemFichaTecnicaPageTests`, preferindo legibilidade;
- adicionar teste de persistência/tenant guard apenas se ainda não houver cobertura equivalente para `EntityState.Deleted`.

### Documentação

- manter este UC coerente com a implementação;
- atualizar F003 apenas se o comportamento final divergir da especificação;
- atualizar `backlog.md` para `Concluído` na PR de implementação.

## Fora do escopo

- excluir Ficha Técnica;
- remover Produto;
- remover/desativar Insumo;
- soft delete de Item;
- histórico/versionamento da composição;
- restauração de Item removido;
- substituir Insumo em um único comando;
- desbloquear identidade do Insumo;
- contar referências;
- consulta completa da composição do UC017;
- cálculo/recalculo de custo;
- estoque;
- auditoria de quem removeu;
- modal JavaScript obrigatório.

## Revalidação pós-MEL010

A MEL010 eliminou o único blocker conceitual da UC016.

A implementação real confirma que:

- `IdentidadeConsolidada` é persistida no Insumo;
- não existe transição true -> false;
- `DbContext` protege operações `Deleted` por Empresa;
- Item/Ficha/Produto já possuem GQF tenant-aware;
- a lista operacional do UC015 já oferece o ponto de navegação necessário.

Portanto, UC016 pode ser implementada sem nova alteração de domínio ou schema.

## Branch sugerida

~~~text
feat/uc016-remover-item-ficha
~~~

## Commit sugerido

~~~text
feat: remove item da ficha tecnica
~~~
