# UC015 — Alterar item da Ficha Técnica

- **Status:** Especificado — bloqueado até UC014 implementado e revalidado
- **Funcionalidade:** F003 — Ficha Técnica
- **Dependências materiais:** UC014 implementado
- **Próximo caso relacionado:** UC016 — Remover item da Ficha Técnica
- **Alteração de schema:** não
- **Gate obrigatório:** após implementação/revisão/merge da UC014, revalidar esta especificação contra o modelo e as páginas reais de ItemFichaTecnica antes de criar a instrução Codex

## Objetivo

Permitir alterar os dados mutáveis de um Item já existente na Ficha Técnica:

1. Quantidade utilizada no lote;
2. Observação contextual.

A UC015 preserva integralmente a identidade e os vínculos do Item.

## Escopo fechado

Campos editáveis:

- Quantidade;
- Observacao.

Campos imutáveis:

- Id;
- EmpresaId;
- FichaTecnicaId;
- InsumoId.

Trocar o Insumo não é edição do Item no MVP. Quando necessário, o fluxo correto será remover o Item no UC016 e adicionar o novo Insumo pela UC014.

## Motivo da imutabilidade do InsumoId

ItemFichaTecnica representa a referência explícita de um Insumo na composição.

Permitir alterar InsumoId em uma edição:

- mudaria a identidade funcional do Item;
- poderia contornar RN049;
- tornaria a proteção RN048 mais difícil de interpretar;
- misturaria alteração de quantidade com substituição de material;
- exigiria tratar duplicidade com outro Item já existente.

Portanto, a atualização do Item mantém o mesmo Insumo.

## Regras de Quantidade

Aplicar RN010.

~~~text
Quantidade > 0
~~~

Quantidade continua decimal e expressa na Unidade base do Insumo referenciado.

A UC015 não permite selecionar unidade e não converte unidades.

Validações Web:

- ausente => A quantidade é obrigatória.
- zero/negativa => A quantidade deve ser maior que zero.

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

## Atualização de domínio

Adicionar comportamento explícito ao Item, por exemplo:

~~~csharp
item.AtualizarDados(quantidade, observacao)
~~~

ou nome equivalente.

A atualização deve ser atômica:

1. normalizar Observacao em variável local;
2. validar Quantidade e Observacao;
3. somente depois atribuir;
4. preservar Id, EmpresaId, FichaTecnicaId e InsumoId.

Erro em qualquer campo não pode deixar estado parcial em memória.

## Insumo ativo ou inativo

Um Item existente continua editável mesmo que o Insumo referenciado tenha sido desativado depois de sua inclusão.

Motivo:

- RN008 preserva referências existentes;
- UC014 proíbe apenas adicionar novos Insumos inativos;
- corrigir Quantidade/Observacao da composição existente não torna o Insumo novamente elegível para novas Fichas.

A edição não reativa o Insumo.

A página deve exibir a situação atual do Insumo.

## Produto ativo ou inativo

Itens da Ficha de Produto inativo continuam editáveis, seguindo a política de manutenção da Ficha definida em UC013/UC014.

Editar Item não reativa Produto.

## RN048 durante edição

Editar Quantidade ou Observacao mantém a referência ao mesmo Insumo.

Portanto:

- RN048 continua ativa antes e depois da edição;
- Nome/Marca/Unidade base continuam protegidos;
- nenhuma lógica de desbloqueio é executada na UC015.

Desbloqueio só pode ocorrer após remoção da última referência, assunto da UC016.

## Multiempresa e segurança

Todas as leituras/escritas comuns usam Global Query Filters.

A rota contém ProdutoId e ItemId, mas ambos precisam ser coerentes com:

~~~text
Produto
  -> FichaTecnica
      -> ItemFichaTecnica
~~~

A edição só é válida quando o Item pertence à Ficha do Produto informado na rota.

Casos que retornam HTTP 404:

- Produto inexistente;
- Produto cross-tenant;
- Ficha inexistente;
- Item inexistente;
- Item de outro tenant;
- Item pertencente a outra Ficha/Produto.

Não revelar que um Item existe em outra Empresa ou em outra Ficha.

Não usar IgnoreQueryFilters no fluxo Web.

## Request e binding

O formulário recebe somente:

~~~text
Quantidade
Observacao
~~~

Não bindar/confiar em:

- EmpresaId;
- ProdutoId;
- FichaTecnicaId;
- InsumoId;
- ItemId como campo editável.

ProdutoId e ItemId vêm da rota.

Mesmo que campos extras manipulados sejam enviados, referências persistidas permanecem inalteradas.

## Persistência

Nenhuma alteração de schema.

Não criar migration.

Não alterar:

- configuração de chaves/FKs;
- índice único da RN049;
- ModelSnapshot;
- migrations históricas.

Se a implementação exigir migration, interromper e reavaliar o escopo.

## Web

Criar página:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Editar/{itemId:int}
~~~

### GET

Carregar pelo tenant:

1. Produto;
2. Ficha do Produto;
3. Item pertencente àquela Ficha;
4. Insumo referenciado.

Exibir resumo somente leitura:

- Produto;
- Insumo;
- Marca;
- Unidade base;
- situação do Insumo.

Campos editáveis:

- Quantidade;
- Observação contextual.

Não permitir alterar Insumo.

### POST

Fluxo:

1. rebuscar Produto pelo GQF;
2. rebuscar Ficha do Produto;
3. rebuscar Item da Ficha;
4. se qualquer vínculo for inválido => 404;
5. validar InputModel;
6. chamar comportamento de domínio de atualização;
7. salvar;
8. PRG para a página da Ficha do Produto;
9. exibir:

~~~text
Item da ficha técnica atualizado com sucesso.
~~~

### InputModel

Usar:

~~~text
decimal? Quantidade
string? Observacao
~~~

Quantidade nullable na fronteira para diferenciar ausência de zero.

## Navegação

A UC015 não deve antecipar a consulta completa da UC017.

Pode adicionar ação Editar somente onde a UC014 já tornar o Item acessível na interface real após revalidação.

A especificação final pós-UC014 deve confirmar o ponto de navegação correto.

Cancelar retorna para:

~~~text
/Produtos/FichaTecnica/{produtoId}
~~~

## Antiforgery

POST usa antiforgery padrão.

POST sem token válido não altera Item.

GET é somente leitura.

## Regras de negócio aplicáveis

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
Exibe Produto/Insumo/Unidade/Situação e os valores atuais de Quantidade/Observacao.

### CA03 — Atualização válida
Quantidade e Observacao válidas são atualizadas no mesmo Item.

### CA04 — Id e vínculos são preservados
Id, EmpresaId, FichaTecnicaId e InsumoId permanecem inalterados.

### CA05 — Quantidade válida
Decimal >0 é aceita; ausente, zero ou negativa é rejeitada.

### CA06 — Observação válida
Null/whitespace vira null; trim externo; conteúdo interno preservado; >1000 rejeitado.

### CA07 — Atualização é atômica
Erro em Quantidade ou Observacao não deixa alteração parcial.

### CA08 — Insumo não é editável
UI não oferece troca e POST manipulado com InsumoId não altera referência.

### CA09 — Item de Insumo inativo é editável
Pode alterar Quantidade/Observacao sem reativar Insumo.

### CA10 — Produto inativo é editável
Item pode ser alterado sem reativar Produto.

### CA11 — Produto inexistente/cross-tenant
GET/POST retornam 404.

### CA12 — Item inexistente/cross-tenant
GET/POST retornam 404 sem vazamento.

### CA13 — Item de outra Ficha
Produto válido com itemId pertencente a outro Produto/Ficha retorna 404.

### CA14 — Request não controla ownership
Campos extras de Empresa/Ficha/Insumo/Item não alteram os vínculos persistidos.

### CA15 — RN048 permanece ativa
Editar Item não desbloqueia Nome/Marca/Unidade do Insumo.

### CA16 — Observação contextual continua independente
Alteração do Item não modifica Observacao global do Insumo.

### CA17 — GET não muta
GET não altera Item, Ficha, Produto ou Insumo.

### CA18 — Antiforgery
POST sem token não altera Item.

### CA19 — PRG e mensagem
POST válido redireciona à Ficha e mostra: Item da ficha técnica atualizado com sucesso.

### CA20 — Sem schema
Nenhuma migration ou ModelSnapshot é alterado.

### CA21 — Sem escopo antecipado
Não implementar remoção, troca de Insumo, consulta completa, custo, perdas, equipamentos ou versionamento.

## Matriz de testes fechada

### Unitários — ItemFichaTecnica

- U1: atualização válida altera quantidade/observação e preserva ids;
- U2: quantidade zero/negativa é rejeitada;
- U3: observação normaliza e valida limite;
- U4: atualização inválida é atômica.

### Persistência

- P1: round-trip atualiza Quantidade/Observacao no mesmo Item;
- P2: atualização preserva EmpresaId/FichaTecnicaId/InsumoId;
- P3: índice único/RN049 permanece válido sem alteração;
- P4: nenhuma migration/schema nova.

### Web

- W1: exige autenticação e Empresa Ativa;
- W2: GET carrega dados e não oferece Insumo editável;
- W3: POST válido atualiza e faz PRG;
- W4: inválidos não persistem;
- W5: POST manipulando InsumoId/EmpresaId/FichaId não altera vínculos;
- W6: Insumo inativo continua editável sem reativação;
- W7: Produto inativo continua editável sem reativação;
- W8: Produto inexistente/cross-tenant => 404;
- W9: Item inexistente/cross-tenant => 404;
- W10: Item de outra Ficha/Produto => 404;
- W11: GET não muta;
- W12: antiforgery ausente não altera Item;
- W13: edição mantém RN048 ativa na edição do Insumo.

## Gate obrigatório pós-UC014

Esta UC está especificada, mas **não liberada para implementação**.

Após implementação/revisão/merge da UC014:

1. revalidar ItemFichaTecnica real;
2. confirmar nomes/propriedades e método de criação;
3. confirmar rota e navegação reais da UC014;
4. confirmar como a Ficha apresenta/acessa Itens;
5. confirmar guards cross-tenant Item/Ficha/Insumo;
6. confirmar adaptação real de /Insumos/Editar pela RN048;
7. confirmar que Quantidade/Observacao continuam sendo os únicos campos mutáveis;
8. revalidar matriz U1-U4, P1-P4 e W1-W13;
9. ajustar a especificação se necessário;
10. somente então criar a instrução Codex;
11. liberar branch sugerida:

~~~text
feat/uc015-editar-item-ficha
~~~

## Impacto no UC016

UC016 removerá Item.

A remoção pode mudar a RN048 quando o Item removido for a última referência atual do Insumo e não existir histórico de preço.

UC015 não executa essa lógica porque a referência permanece.

## Fora do escopo

- implementação antes do gate pós-UC014;
- troca de Insumo;
- remoção de Item;
- UC016/UC017;
- histórico/versionamento da composição;
- custo do Item;
- preço vigente;
- perdas;
- equipamentos;
- conversões;
- API REST;
- refatorações oportunistas.

## Definition of Done da especificação

Para liberar implementação posteriormente:

- UC014 implementada/revisada/mergeada;
- gate pós-UC014 concluído;
- RN050 confirmada contra modelo real;
- rota/navegação real confirmadas;
- matriz de testes revalidada;
- instrução Codex criada somente após gate;
- F003/catálogo/ordem/regras coerentes.
