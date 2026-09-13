# UC015 — Alterar item da Ficha Técnica

- **Status:** Implementado
- **Funcionalidade:** F003 — Ficha Técnica
- **Dependências materiais:** UC014 implementado
- **Próximo caso relacionado:** UC016 — Remover item da Ficha Técnica
- **Alteração de schema:** não
- **Revalidação pós-UC014:** concluída em 2026-09-13 contra a implementação mergeada pela PR #56
- **Implementação:** concluída em 2026-09-13 na branch `feat/uc015-editar-item-ficha`

## Objetivo

Permitir alterar os dados mutáveis de um Item já existente na Ficha Técnica:

1. Quantidade utilizada no lote;
2. Observação contextual.

A UC015 preserva integralmente a identidade e os vínculos do Item.

## Modelo real confirmado

A implementação da UC014 confirmou:

~~~text
ItemFichaTecnica
- Id
- EmpresaId
- FichaTecnicaId
- InsumoId
- Quantidade
- Observacao
~~~

Também confirmou:

- ItemFichaTecnica implementa IEntidadeEmpresa;
- existe GQF por Empresa;
- existe índice único `(EmpresaId, FichaTecnicaId, InsumoId)`;
- FKs Empresa/Ficha/Insumo usam `RESTRICT`;
- o DbContext valida Item -> Ficha e Item -> Insumo na mesma Empresa;
- a inclusão usa `/Produtos/FichaTecnica/{produtoId}/Itens/Novo`;
- Quantidade já usa parsing explícito pt-BR/invariant;
- RN048 está aplicada em `/Insumos/Editar`.

Nenhuma mudança estrutural é necessária para editar Item.

## Escopo fechado

Campos editáveis:

- Quantidade;
- Observacao.

Campos imutáveis:

- Id;
- EmpresaId;
- FichaTecnicaId;
- InsumoId.

Trocar o Insumo não é edição do Item no MVP.

Quando necessário, o fluxo correto será:

~~~text
UC016 remove o Item atual
+
UC014 adiciona o novo Insumo
~~~

## Atualização de domínio

Adicionar comportamento explícito:

~~~csharp
item.AtualizarDados(quantidade, observacao)
~~~

ou nome equivalente claro.

A atualização deve ser atômica:

1. normalizar Observacao em variável local;
2. validar Quantidade;
3. validar Observacao normalizada;
4. somente então atribuir Quantidade e Observacao;
5. preservar Id, EmpresaId, FichaTecnicaId e InsumoId.

Uma tentativa inválida não pode deixar a entidade parcialmente alterada em memória.

## Quantidade

Aplicar RN010.

~~~text
Quantidade > 0
~~~

Quantidade continua decimal no domínio/persistência e permanece expressa na Unidade base do Insumo.

A UC015 não permite selecionar/trocar Unidade e não faz conversão.

### Fronteira Web

Não usar binding direto de `decimal?`.

Reutilizar o padrão real da UC014:

~~~text
Input.Quantidade: string?
~~~

Refatorar `ItemFichaTecnicaFormulario` para que o parser seja reutilizável por Novo e Editar, evitando duplicação.

Forma sugerida:

~~~csharp
TentarObterQuantidade(
    ModelStateDictionary modelState,
    string? quantidadeInformada,
    out decimal quantidade)
~~~

ou overload equivalente.

O fluxo `Novo` existente deve continuar usando o mesmo helper e permanecer verde.

Parsing:

- trim;
- ausente/vazia => `A quantidade é obrigatória.`;
- contendo vírgula => pt-BR;
- sem vírgula => invariant;
- inválida => `A quantidade deve ser um número válido.`;
- <= 0 => `A quantidade deve ser maior que zero.`.

Adicionar também formatação reutilizável, por exemplo:

~~~csharp
FormatarQuantidade(decimal quantidade)
~~~

usando pt-BR e até 6 casas decimais significativas, para preencher o formulário de edição sem depender da cultura do servidor.

Exemplo obrigatório:

~~~text
1,25 -> 1.25m no domínio
~~~

e nunca `125m`.

## Observação contextual

Aplicar RN034.

A Observacao:

- é opcional;
- máximo 1000 caracteres;
- remove whitespace externo;
- preserva conteúdo interno e quebras de linha;
- whitespace-only => null.

Editar Observacao do Item não altera Observacao global do Insumo.

Editar Observacao global do Insumo não altera Observacao contextual do Item.

## Insumo ativo ou inativo

Um Item existente continua editável mesmo se o Insumo referenciado tiver sido desativado depois da inclusão.

A edição:

- não reativa o Insumo;
- não torna o Insumo elegível para novas Fichas;
- mantém a identidade consolidada; após a MEL010, a proteção é permanente e não depende da referência continuar existindo.

A página de edição deve mostrar a situação atual do Insumo.

## Produto ativo ou inativo

Item de Ficha pertencente a Produto inativo continua editável.

A edição não reativa o Produto.

## Multiempresa e coerência de rota

Rota:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Editar/{itemId:int}
~~~

O acesso só é válido quando existe a cadeia tenant-aware:

~~~text
Produto da Empresa Ativa
  -> FichaTecnica desse Produto
      -> ItemFichaTecnica dessa Ficha
          -> Insumo referenciado
~~~

Retornar HTTP 404 para:

- Produto inexistente;
- Produto de outro tenant;
- Ficha inexistente;
- Item inexistente;
- Item de outro tenant;
- Item pertencente a outra Ficha/Produto.

Não revelar existência de Item de outro tenant ou de outra Ficha.

Não usar `IgnoreQueryFilters` no fluxo Web comum.

## Request e binding

O formulário recebe somente:

~~~text
Quantidade: string?
Observacao: string?
~~~

Não bindar/confiar em:

- EmpresaId;
- ProdutoId;
- FichaTecnicaId;
- InsumoId;
- ItemId como campo editável.

ProdutoId e ItemId vêm da rota.

Mesmo que campos extras sejam enviados, Id e vínculos persistidos permanecem inalterados.

## Persistência

Nenhuma alteração de schema.

Não criar migration.

Não alterar:

- configuração de ItemFichaTecnica;
- chaves/FKs;
- índice RN049;
- ModelSnapshot;
- migrations históricas.

Se a implementação exigir migration, interromper e reavaliar.

## Web — página Editar

Criar:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Editar/{itemId:int}
~~~

### GET

Carregar Produto, Ficha, Item e Insumo pela cadeia tenant-aware.

Exibir resumo somente leitura:

- Produto;
- Insumo;
- Marca;
- Unidade base;
- Situação do Insumo.

Campos editáveis:

- Quantidade;
- Observação contextual.

Não oferecer select/troca de Insumo.

Quantidade deve ser preenchida pelo helper de formatação pt-BR.

### POST

Fluxo:

1. rebuscar Produto;
2. rebuscar Ficha pelo Produto;
3. rebuscar Item pertencente àquela Ficha;
4. se cadeia inválida => 404;
5. parsear/validar Quantidade pelo helper compartilhado;
6. chamar `AtualizarDados`;
7. salvar;
8. PRG para `/Produtos/FichaTecnica/{produtoId}`;
9. exibir:

~~~text
Item da ficha técnica atualizado com sucesso.
~~~

## Navegação mínima necessária

A UC014 não lista Itens na página da Ficha. Sem uma superfície de navegação, a UC015 teria uma rota tecnicamente existente, porém inacessível ao usuário.

Por isso, a UC015 adiciona à página da Ficha uma **lista operacional mínima**, sem antecipar a consulta completa da UC017.

Quando existir Ficha, exibir seção:

~~~text
Itens da ficha
~~~

Colunas mínimas:

- Insumo;
- Quantidade;
- Unidade;
- Situação do Insumo;
- Ação Editar.

O rótulo do Insumo pode reutilizar a convenção:

~~~text
Nome — Marca
Nome              // sem marca
~~~

A lista:

- mostra apenas Itens da Ficha atual;
- mantém visível Item cujo Insumo foi desativado depois da inclusão;
- identifica Insumo inativo na coluna Situação;
- não mostra custo;
- não mostra preço;
- não mostra observação contextual;
- não calcula totais;
- não implementa filtros/pesquisa;
- não substitui UC017.

Cada linha aponta para:

~~~text
/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}
~~~

Se não houver Itens, é permitido mostrar:

~~~text
Nenhum insumo adicionado.
~~~

O botão **Adicionar insumo** continua existindo quando a Ficha está persistida.

## Consistência de estado da página da Ficha

A revisão da PR #56 identificou uma inconsistência não bloqueante: em POST inválido de Rendimento/Tempo ativo para Ficha já persistida, `PossuiFicha` não é recarregado antes de `return Page()`, fazendo o botão Adicionar insumo desaparecer apenas naquela renderização.

Como a UC015 passa a depender da navegação de Itens na mesma página, corrigir esse estado dentro desta tarefa.

Antes de retornar `Page()` em POST inválido da base da Ficha:

- detectar/recarregar a Ficha persistida;
- manter `PossuiFicha = true` quando aplicável;
- carregar a lista mínima de Itens;
- manter Adicionar insumo e Editar visíveis.

Isso não altera regra de domínio nem schema.

## Antiforgery

POST usa antiforgery padrão.

POST sem token válido não altera Item.

GET não altera Item/Ficha/Produto/Insumo.

## Regras aplicáveis

- RN010 — Quantidade na Ficha;
- RN034 — Observação contextual;
- RN035/RN036 — tenant;
- RN047 — Ficha única por Produto;
- RN048 — estabilidade cadastral do Insumo referenciado;
- RN049 — um Insumo por Ficha;
- RN050 — atualização de Item preserva vínculos.

## Critérios de aceitação

### CA01 — Acesso protegido
Usuário anônimo não acessa. Usuário sem Empresa Ativa não possui acesso operacional.

### CA02 — GET carrega Item correto
Exibe Produto, Insumo, Marca, Unidade, Situação, Quantidade e Observacao atuais.

### CA03 — Atualização válida
Quantidade/Observacao válidas atualizam o mesmo Item.

### CA04 — Vínculos preservados
Id, EmpresaId, FichaTecnicaId e InsumoId permanecem inalterados.

### CA05 — Quantidade válida
Entrada textual decimal >0 é aceita por parsing explícito; ausente, não numérica, zero ou negativa é rejeitada.

### CA06 — Parsing independente de cultura
`1,25` persiste como `1.25m` mesmo com cultura corrente invariant.

### CA07 — Observação válida
Null/whitespace => null; trim externo; conteúdo interno preservado; >1000 rejeitado.

### CA08 — Atualização atômica
Erro em Quantidade ou Observacao não deixa alteração parcial.

### CA09 — Insumo não é editável
UI não oferece troca; POST manipulado com InsumoId não altera referência.

### CA10 — Insumo inativo continua editável
Quantidade/Observacao podem mudar sem reativar Insumo.

### CA11 — Produto inativo continua editável
Item pode mudar sem reativar Produto.

### CA12 — Produto inexistente/cross-tenant
GET/POST retornam 404.

### CA13 — Item inexistente/cross-tenant
GET/POST retornam 404 sem vazamento.

### CA14 — Item de outra Ficha
Produto válido com itemId pertencente a outra Ficha/Produto retorna 404.

### CA15 — Request não controla ownership/vínculos
Campos extras manipulados não mudam Empresa/Ficha/Insumo/Item.

### CA16 — Identidade consolidada permanece protegida
Editar Item não altera o estado consolidado nem desbloqueia Nome/Marca/Unidade do Insumo.

### CA17 — Observação contextual continua independente
Alterar Item não modifica Observacao global do Insumo.

### CA18 — Navegação mínima
Ficha persistida exibe seus Itens com ação Editar; não exibe Itens de outra Ficha/tenant.

### CA19 — Insumo inativo permanece visível
Item existente com Insumo inativo aparece na lista e pode ser aberto para edição.

### CA20 — POST inválido da base preserva navegação
Em Ficha já existente, erro de Rendimento/Tempo não esconde Adicionar insumo nem a lista/links de edição.

### CA21 — GET não muta
GET da edição e GET da Ficha não alteram dados.

### CA22 — Antiforgery
POST sem token não altera Item.

### CA23 — PRG e mensagem
POST válido redireciona à Ficha e mostra `Item da ficha técnica atualizado com sucesso.`.

### CA24 — Sem schema
Nenhuma migration ou ModelSnapshot é alterado.

### CA25 — Sem escopo antecipado
Não implementar remoção, troca de Insumo, consulta completa, custos, perdas, equipamentos ou versionamento.

## Matriz de testes revalidada

### Unitários — ItemFichaTecnica

- U1: AtualizarDados válido altera Quantidade/Observacao e preserva Id/Empresa/Ficha/Insumo;
- U2: quantidade zero/negativa é rejeitada;
- U3: Observacao normaliza e valida limite;
- U4: atualização inválida é atômica.

### Persistência

- P1: round-trip atualiza Quantidade/Observacao no mesmo Item;
- P2: atualização preserva EmpresaId/FichaTecnicaId/InsumoId;
- P3: índice único/RN049 permanece intacto;
- P4: nenhuma migration/schema nova.

### Web

- W1: edição exige autenticação e Empresa Ativa;
- W2: GET carrega Item e não oferece Insumo editável;
- W3: POST válido com `1,25` atualiza e faz PRG;
- W4: quantidade ausente/texto/zero/negativa e Observacao >1000 não persistem;
- W5: POST manipulando InsumoId/EmpresaId/FichaId/ItemId não altera vínculos;
- W6: Insumo inativo continua editável sem reativação;
- W7: Produto inativo continua editável sem reativação;
- W8: Produto inexistente/cross-tenant => 404;
- W9: Item inexistente/cross-tenant => 404;
- W10: Item de outra Ficha/Produto => 404;
- W11: GET não muta;
- W12: antiforgery ausente não altera Item;
- W13: edição mantém a identidade consolidada protegida na edição do Insumo;
- W14: página da Ficha lista somente Itens da própria Ficha com links Editar;
- W15: Item com Insumo inativo permanece visível/editável;
- W16: POST inválido da base da Ficha preserva PossuiFicha, Adicionar insumo e lista/links;
- W17: parser compartilhado interpreta `1,25` como `1.25m` sob cultura invariant;
- W18: fluxo Novo da UC014 continua aceitando Quantidade após refatoração do helper.

## Revalidação pós-UC014 — concluída

A revalidação foi executada contra a implementação real mergeada pela PR #56.

Confirmações:

1. `ItemFichaTecnica` real possui exatamente os campos previstos;
2. Quantidade e Observacao continuam sendo os únicos dados que devem ser mutáveis;
3. Id/EmpresaId/FichaTecnicaId/InsumoId permanecem identidade/vínculos;
4. não há método de atualização ainda, portanto `AtualizarDados` é o menor comportamento de domínio necessário;
5. não há necessidade de alteração de schema;
6. GQF e guards de Item já existem e não precisam ser redesenhados;
7. Insumo inativo permanece consultável, permitindo edição de Item existente;
8. Produto inativo já é suportado no fluxo da Ficha;
9. Quantidade deve reutilizar o parsing explícito implementado pela UC014;
10. o helper atual está acoplado ao `NovoModel` e deve ser tornado reutilizável, sem duplicar regra;
11. a página da Ficha ainda não lista Itens, exigindo uma lista operacional mínima para tornar a edição navegável;
12. a consulta completa continua reservada ao UC017;
13. RN048 está realmente aplicada em GET/POST de Insumo e permanece ativa durante edição de Item;
14. a observação visual de `PossuiFicha` da review da PR #56 pode ser corrigida dentro desta tarefa por ser diretamente relacionada à navegação introduzida pela UC015.

A UC015 foi implementada na branch:

~~~text
feat/uc015-editar-item-ficha
~~~

Instrução Codex normativa:

~~~text
docs/codex/UC015-alterar-item-ficha.md
~~~

## Impacto no UC016

UC016 removerá Item.

A decisão pré-UC016 eliminou qualquer desbloqueio cadastral: depois da MEL010, o Insumo mantém IdentidadeConsolidada = true mesmo quando o Item removido era sua última referência atual e não existe histórico de preço.

UC016 não deve contar referências, consultar preços para decidir desbloqueio ou alterar IdentidadeConsolidada.

## Fora do escopo

- UC016/UC017 completos;
- remoção de Item;
- troca de Insumo;
- exclusão física de Ficha;
- histórico/versionamento da composição;
- custos/preços;
- perdas;
- equipamentos;
- conversão de unidades;
- filtros/pesquisa da composição;
- API REST;
- refatorações oportunistas.

## Definition of Done específica

Além da DoD global:

- `ItemFichaTecnica.AtualizarDados` ou equivalente implementado atomicamente;
- somente Quantidade/Observacao são mutáveis;
- helper de Quantidade compartilhado entre Novo/Editar;
- parsing/formatação pt-BR determinísticos;
- nenhuma migration;
- rota Editar funcional;
- cadeia Produto->Ficha->Item protegida por tenant;
- Insumo inativo e Produto inativo suportados sem reativação;
- lista operacional mínima na Ficha implementada;
- RN048 preservada;
- estado de `PossuiFicha`/Itens preservado após POST inválido da base;
- matriz U1-U4, P1-P4 e W1-W18 atendida;
- UC014 sem regressão;
- build Release sem warnings novos relevantes;
- suíte completa verde.
