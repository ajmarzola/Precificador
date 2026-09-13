# Instrução Codex — UC015: Alterar item da Ficha Técnica

## Tarefa

Implementar integralmente a UC015 conforme `docs/use-cases/UC015-alterar-item-ficha.md`, partindo da master pós-UC014.

Branch obrigatória:

~~~text
feat/uc015-editar-item-ficha
~~~

Nunca editar, commitar ou fazer push direto em `master`.

Não fazer merge da própria implementação.

## Leitura obrigatória

Antes da primeira alteração, ler:

- `AGENTS.md`;
- `docs/use-cases/UC015-alterar-item-ficha.md`;
- `docs/use-cases/UC014-adicionar-insumo-ficha.md`;
- `docs/features/F003-ficha-tecnica.md`;
- `docs/business/business-rules.md`;
- `docs/development/testing-strategy.md`;
- `docs/development/definition-of-done.md`;
- `docs/development/implementation-order.md`;
- implementação real de:
  - `ItemFichaTecnica`;
  - `ItemFichaTecnicaFormulario`;
  - `/Produtos/FichaTecnica`;
  - `/Produtos/FichaTecnica/{produtoId}/Itens/Novo`;
  - `PrecificadorDbContext`;
  - `/Insumos/Editar`;
- testes reais da UC014 e UC013.

Confirmar antes de editar:

1. branch atual é `feat/uc015-editar-item-ficha`;
2. branch parte da master que contém a PR #56;
3. UC014 está implementada;
4. UC015 está marcada como pronta para implementação;
5. não existe página Editar de Item já implementada;
6. não existe migration pendente necessária à UC015.

Se houver divergência material, interromper e reportar.

## Escopo

Implementar somente:

- comportamento de domínio para alterar Quantidade/Observacao;
- refatoração mínima do helper de Quantidade para reutilização;
- página Editar de Item;
- lista operacional mínima de Itens na página da Ficha;
- correção de estado `PossuiFicha`/lista após POST inválido da base;
- testes U1-U4, P1-P4 e W1-W18;
- documentação pós-implementação.

## Domínio

Adicionar em `ItemFichaTecnica`:

~~~csharp
AtualizarDados(decimal quantidade, string? observacao)
~~~

ou nome equivalente claro.

A atualização:

- valida quantidade >0;
- normaliza Observacao;
- valida max1000;
- só depois atribui;
- preserva Id/EmpresaId/FichaTecnicaId/InsumoId;
- é atômica.

Não adicionar setters públicos.

Não permitir trocar Insumo/Ficha/Empresa.

## Quantidade — helper compartilhado

Hoje `ItemFichaTecnicaFormulario` está acoplado ao `NovoModel.ItemFichaTecnicaInputModel`.

Refatorar de forma mínima para reutilização por Novo e Editar.

Preferência:

~~~csharp
public static bool TentarObterQuantidade(
    ModelStateDictionary modelState,
    string? quantidadeInformada,
    out decimal quantidade)
~~~

Pode manter overload compatível se simplificar a alteração, mas não duplicar parsing em duas páginas.

Adicionar:

~~~csharp
public static string FormatarQuantidade(decimal quantidade)
~~~

seguindo o padrão de `FichaTecnicaFormulario.FormatarRendimento`:

- cultura pt-BR;
- até 6 casas;
- saída determinística.

Regras de parsing permanecem:

- vazio => `A quantidade é obrigatória.`;
- vírgula => pt-BR;
- sem vírgula => invariant;
- inválido => `A quantidade deve ser um número válido.`;
- <=0 => `A quantidade deve ser maior que zero.`.

Garantir regressão verde no fluxo Novo da UC014.

## Web — Editar Item

Criar Razor Page compatível com:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Editar/{itemId:int}
~~~

### GET

Resolver com GQF:

1. Produto;
2. Ficha do Produto;
3. Item da Ficha;
4. Insumo do Item.

Qualquer quebra da cadeia => 404.

Exibir somente leitura:

- Produto;
- Insumo;
- Marca;
- Unidade base;
- Situação do Insumo.

Campos editáveis:

~~~text
Input.Quantidade: string?
Input.Observacao: string?
~~~

Preencher Quantidade com `FormatarQuantidade`.

Não oferecer select/troca de Insumo.

### POST

1. rebuscar Produto;
2. rebuscar Ficha;
3. rebuscar Item da Ficha;
4. cadeia inválida => 404;
5. parsear Quantidade pelo helper compartilhado;
6. chamar `AtualizarDados`;
7. salvar;
8. TempData:
   `Item da ficha técnica atualizado com sucesso.`;
9. PRG para `/Produtos/FichaTecnica/{produtoId}`.

Campos extras manipulados de Empresa/Ficha/Insumo/Item não podem alterar vínculos.

Produto inativo e Insumo inativo permanecem permitidos para edição do Item existente.

## Página da Ficha — lista operacional mínima

A UC014 não lista Itens; sem isso a edição não é navegável.

Na página existente de Ficha, carregar somente os Itens daquela Ficha e exibir:

~~~text
Itens da ficha
~~~

Colunas:

- Insumo;
- Quantidade;
- Unidade;
- Situação;
- Editar.

Não mostrar:

- custo;
- preço;
- Observacao contextual;
- totais;
- filtros/pesquisa.

Mostrar Item de Insumo inativo e sinalizar Situação = Inativo.

Cada Editar aponta para:

~~~text
/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}
~~~

Se vazio, pode exibir:

~~~text
Nenhum insumo adicionado.
~~~

Não transformar esta tarefa na UC017.

## Consistência após POST inválido da base

Hoje `FichaTecnicaModel.OnPostAsync` retorna `Page()` por validação antes de recarregar `PossuiFicha`.

Corrigir de modo simples:

- quando Produto já possui Ficha, manter `PossuiFicha=true`;
- carregar lista mínima de Itens;
- em POST inválido da base, Adicionar insumo e Editar continuam visíveis;
- não alterar valores persistidos.

Evitar duplicar consultas em excesso: um helper privado de carregamento do estado da Ficha/lista é aceitável se permanecer local ao PageModel.

## Persistência

Nenhuma alteração de schema.

Não criar migration.

Não alterar ModelSnapshot.

Não mudar configuração/índices/FKs de ItemFichaTecnica.

Os guards existentes Item->Ficha e Item->Insumo devem continuar verdes.

## RN048

Editar Item mantém a referência.

Logo:

- Nome/Marca/Unidade do Insumo continuam protegidos;
- nenhuma lógica de desbloqueio;
- não alterar `/Insumos/Editar` salvo ajuste estritamente necessário para testes de regressão.

## Não implementar

Não implementar:

- UC016;
- remoção de Item;
- UC017 completa;
- troca de Insumo;
- exclusão de Ficha;
- custo/preço;
- perdas;
- equipamentos;
- filtros/pesquisa;
- versionamento;
- API;
- CQRS/MediatR;
- repository genérico;
- Unit of Work customizado;
- refatorações oportunistas.

## Testes obrigatórios

### Unitários U1-U4

Cobrir:

- atualização válida;
- quantidade inválida;
- Observacao;
- atomicidade.

### Persistência P1-P4

Cobrir:

- round-trip;
- preservação de vínculos;
- RN049 sem regressão;
- ausência de migration/schema.

### Web W1-W18

Implementar integralmente a matriz da UC015, incluindo:

- autenticação/Empresa Ativa;
- GET correto sem Insumo editável;
- POST `1,25`;
- inválidos;
- request manipulado;
- Produto/Insumo inativos;
- cross-tenant;
- item de outra Ficha;
- GET sem mutação;
- antiforgery;
- RN048;
- lista mínima da Ficha;
- Insumo inativo visível;
- POST inválido da base preservando navegação;
- parser sob cultura invariant;
- regressão do fluxo Novo da UC014.

Não remover/enfraquecer testes existentes.

## Validação obrigatória

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Confirmar 0 warnings novos relevantes.

## Documentação pós-implementação

Atualizar:

- UC015 para Implementado;
- F003;
- catálogo;
- ordem de implementação.

UC016 passa a próximo caso para especificação/revalidação.

Não implementar UC016 nesta branch.

## Retorno obrigatório

Informar:

1. branch usada;
2. resumo da implementação;
3. confirmação de que não houve migration;
4. testes adicionados/alterados;
5. resultado build/test;
6. arquivos alterados;
7. divergências;
8. URL/estado da PR.

Commit sugerido:

~~~text
feat: permite editar item da ficha tecnica
~~~

Não fazer merge em master.
