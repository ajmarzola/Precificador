# Instrução Codex — UC016: Remover item da Ficha Técnica

## Tarefa

Implementar integralmente a UC016 conforme `docs/use-cases/UC016-remover-item-ficha.md`.

Branch obrigatória:

~~~text
feat/uc016-remover-item-ficha
~~~

Não editar, commitar ou fazer push direto em `master`.
Não fazer merge da própria implementação.

## Precondições

Antes de alterar qualquer arquivo:

1. partir da `master` atualizada;
2. criar/trocar para `feat/uc016-remover-item-ficha`;
3. confirmar que a branch atual não é `master`;
4. ler `AGENTS.md`;
5. ler `docs/development/backlog.md` e confirmar UC016 = `Pronto`;
6. ler UC016, UC014, UC015 e MEL010;
7. ler RN047, RN048, RN049, RN050 e RN051;
8. inspecionar a implementação atual de `ItemFichaTecnica`, página da Ficha, edição de Item e `PrecificadorDbContext`.

Se o backlog não indicar UC016 como `Pronto`, não implementar.

## Escopo funcional

Criar remoção física de `ItemFichaTecnica` da composição atual.

Não criar soft delete, histórico de composição ou alteração de `IdentidadeConsolidada`.

## Rota

Criar:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Remover/{itemId:int}
~~~

Arquivos esperados:

~~~text
src/Precificador.Web/Pages/Produtos/FichaTecnica/Itens/Remover.cshtml
src/Precificador.Web/Pages/Produtos/FichaTecnica/Itens/Remover.cshtml.cs
~~~

## GET

Resolver explicitamente a cadeia:

~~~text
Produto
  -> Ficha do Produto
    -> Item pertencente à Ficha
      -> Insumo
~~~

Usar GQF normal. Não usar `IgnoreQueryFilters`.

Se qualquer elo falhar, retornar 404.

O GET não pode mutar estado.

Exibir:

- Produto e situação;
- Insumo, Marca, Unidade e situação;
- Quantidade;
- Observação contextual;
- confirmação de remoção;
- botão Remover;
- link Cancelar para a Ficha.

## POST

Recarregar toda a cadeia usando somente ids da rota.

Carregar o Item rastreado e exigir:

~~~text
item.Id == itemId
item.FichaTecnicaId == ficha.Id
~~~

Depois:

~~~csharp
context.ItensFichaTecnica.Remove(item);
await context.SaveChangesAsync();
~~~

Definir:

~~~text
Item removido da ficha técnica com sucesso.
~~~

e redirecionar para `/Produtos/FichaTecnica/{produtoId}`.

## Regra crítica da MEL010

Não:

- consultar quantidade de referências;
- consultar preços para decidir desbloqueio;
- alterar `Insumo.IdentidadeConsolidada`;
- criar método de desconsolidação;
- alterar Nome/Marca/Unidade do Insumo.

Após remover o último Item de um Insumo sem preço:

~~~text
IdentidadeConsolidada continua true
~~~

e a edição de Nome/Marca/Unidade continua bloqueada.

## Último Item

Remover o último Item é válido.

Não remover a Ficha.

Rendimento e TempoAtivoMinutos permanecem.

A página da Ficha deve passar a exibir composição vazia.

## Inativos

Permitir remoção quando Produto ou Insumo estiver inativo.

Não reativar nenhum deles.

## Navegação na Ficha

Adicionar ação `Remover` à lista operacional atual.

Não ampliar a tabela para consulta completa do UC017.

## Segurança

Preservar:

- autenticação/Empresa Ativa;
- GQF;
- guard central de escrita;
- antiforgery.

Item de outra Ficha, mesmo no mesmo tenant, retorna 404.

Outro tenant retorna 404.

POST sem antiforgery retorna 400 e não remove.

Não criar `InputModel` com ids de ownership.

Campos extras enviados pelo cliente devem ser ignorados.

## Persistência

Não criar migration.

Não alterar FKs/índices.

Não alterar guards do DbContext sem evidência concreta de necessidade.

O guard existente já considera `EntityState.Deleted`.

## Domínio

Não alterar `ItemFichaTecnica`, `FichaTecnica`, `Insumo` ou `Produto` apenas para acomodar a remoção.

Não criar método `Remover()` de domínio que só delegue ao EF.

## Testes obrigatórios

Atender integralmente a matriz do UC016.

### Persistência

- P1 remoção própria persiste;
- P2 Produto/Ficha/Insumo preservados;
- P3 delete técnico cross-tenant bloqueado pelo guard;
- P4 `IdentidadeConsolidada` preservada.

### Web

- W1 autenticação/Empresa Ativa;
- W2 GET exibe e não remove;
- W3 link Remover na Ficha;
- W4 POST remove + PRG + mensagem;
- W5 preserva outros Itens;
- W6 último Item deixa Ficha vazia existente;
- W7 Produto inativo;
- W8 Insumo inativo;
- W9 inexistentes -> 404;
- W10 Item de outra Ficha -> 404;
- W11 outro tenant -> 404 sem mutação;
- W12 ids extras/manipulados ignorados;
- W13 sem antiforgery -> 400;
- W14 identidade continua consolidada;
- W15 última referência sem preço continua bloqueando identidade.

Pode ampliar `ItemFichaTecnicaPageTests` ou criar `RemoverItemFichaTecnicaPageTests`; escolha a opção mais legível.

Não remover/enfraquecer testes existentes.

## Validação técnica

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Esperado: zero warnings novos relevantes e suíte completa verde.

## Documentação pós-implementação

Na PR de implementação:

- manter `docs/use-cases/UC016-remover-item-ficha.md` coerente;
- atualizar F003 somente se necessário;
- alterar UC016 de `Pronto` para `Concluído` apenas em `docs/development/backlog.md`;
- não reintroduzir `Status` em documentos individuais.

## Fora do escopo

- UC017;
- excluir Ficha;
- soft delete;
- histórico/versionamento;
- desbloqueio de Insumo;
- custo;
- estoque;
- modal JavaScript obrigatório;
- refatoração genérica de Ficha/Item/DbContext.

## Retorno obrigatório

Informar:

1. branch;
2. arquivos alterados;
3. rota implementada;
4. comportamento ao remover último Item;
5. confirmação de preservação da identidade consolidada;
6. testes P1-P4 e W1-W15;
7. build/test;
8. URL da PR.

Commit sugerido:

~~~text
feat: remove item da ficha tecnica
~~~
