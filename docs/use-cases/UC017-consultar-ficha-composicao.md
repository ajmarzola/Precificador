# UC017 — Consultar Ficha Técnica e composição

> **Nota MEL022:** referências a Tempo ativo neste documento são históricas. No modelo ativo, a Ficha Técnica exibe/edita Rendimento na base e não possui `TempoAtivoMinutos`.

- **Funcionalidade:** F003 — Ficha Técnica
- **Dependências materiais:** UC013, UC014, UC015 e UC016 implementados
- **Alteração de schema:** não
- **Revalidação pós-UC016:** concluída contra a implementação real da página de Ficha e da composição atual

## Objetivo

Completar a consulta da Ficha Técnica atual de um Produto, exibindo de forma suficiente para leitura operacional:

- dados do Produto;
- base produtiva da Ficha;
- composição completa dos Itens;
- situação atual dos Insumos;
- Observação contextual de cada Item;
- ações já existentes de manutenção da composição.

A UC017 não cria um segundo conceito de Ficha e não cria uma segunda tela de consulta.

## Decisão de UX e arquitetura

A rota canônica continua sendo:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

A página já concentra:

- Produto;
- Rendimento;
- Tempo ativo;
- criação/edição da base;
- Adicionar Insumo;
- lista dos Itens;
- Editar Item;
- Remover Item.

Portanto, a UC017 deve **evoluir essa mesma página**.

Não criar:

- `/Produtos/FichaTecnica/Detalhes/{id}`;
- nova página somente leitura;
- duplicação de consultas;
- DTO/API paralela apenas para exibição.

## O que significa composição completa

A lista operacional mínima criada no UC015 hoje contém:

- Insumo;
- Quantidade;
- Unidade;
- Situação;
- Ações.

A consulta completa passa a exibir por Item:

1. Nome do Insumo;
2. Marca;
3. Quantidade utilizada;
4. Unidade base;
5. Observação contextual;
6. Situação do Insumo;
7. Ações Editar e Remover.

Marca deve ser apresentada como `—` quando ausente.

Observação contextual deve ser apresentada como `—` quando ausente.

Quando houver quebras de linha na Observação contextual, preservar a leitura visual do conteúdo, preferencialmente com `white-space: pre-wrap` como já usado em outras telas.

## Nome e Marca

Na consulta completa, preferir colunas separadas:

~~~text
Insumo | Marca | Quantidade | Unidade | Observação contextual | Situação | Ações
~~~

Isso substitui o rótulo combinado `Nome — Marca` usado pela lista mínima.

A separação melhora leitura e evita esconder a ausência de Marca dentro de um texto composto.

## Base produtiva

A UC017 não muda o comportamento do UC013.

A mesma página continua mostrando/editando:

- Rendimento do lote;
- Tempo ativo de trabalho.

Os valores persistidos devem continuar carregados no GET.

A UC017 não cria uma segunda apresentação read-only dos mesmos campos.

## Produto

Continuar exibindo:

- Nome;
- Categoria;
- Situação.

Produto ativo ou inativo pode ter sua Ficha consultada.

A consulta não reativa Produto.

## Ficha inexistente

Produto pode existir sem Ficha.

Nesse caso, a rota continua servindo como superfície de criação da base produtiva conforme UC013.

Comportamento:

- Produto é exibido;
- formulário de Rendimento/Tempo ativo permanece disponível;
- `Adicionar insumo` não aparece enquanto não existir Ficha persistida;
- seção de composição não deve fingir que existe uma Ficha;
- não criar Ficha durante GET.

Não retornar 404 apenas porque o Produto ainda não possui Ficha.

## Ficha existente sem Itens

Quando a Ficha existe e não possui Itens:

~~~text
Nenhum insumo adicionado.
~~~

A Ficha continua existente e a ação `Adicionar insumo` permanece disponível.

## Ordenação

Manter ordenação determinística por:

~~~text
Insumo.NomeNormalizado
Insumo.MarcaNormalizada
~~~

Não ordenar por Id, Quantidade ou Situação.

## Insumo inativo

Insumo desativado depois de ser incluído continua aparecendo normalmente na composição.

Exibir:

~~~text
Situação = Inativo
~~~

e manter as ações permitidas pelos UCs anteriores:

- Editar Item;
- Remover Item.

A consulta não reativa o Insumo.

## Observação contextual x Observação global

A coluna da composição exibe exclusivamente:

~~~text
ItemFichaTecnica.Observacao
~~~

Não exibir nem copiar automaticamente:

~~~text
Insumo.Observacao
~~~

na tabela da composição.

A Observação global pertence ao cadastro do Insumo; a Observação contextual descreve o uso daquele Insumo naquela Ficha.

Essa separação preserva RN034.

## Navegação opcional para Insumo

É aceitável tornar o Nome do Insumo um link para:

~~~text
/Insumos/Detalhes/{insumoId}
~~~

desde que:

- o link use o `InsumoId` já resolvido pelo tenant ativo;
- não seja necessário para atender o UC;
- não introduza nova dependência funcional.

Não é critério obrigatório.

## Multiempresa e segurança

A consulta usa apenas Global Query Filters normais.

Não utilizar `IgnoreQueryFilters` na página.

Produto de outro tenant ou inexistente retorna `404`.

A consulta de Itens deve continuar limitada à Ficha carregada do Produto atual.

Um Item de outra Ficha ou outro tenant nunca pode aparecer.

Request não informa `EmpresaId`.

## GET não muta

O GET de `/Produtos/FichaTecnica/{id}`:

- não cria Ficha;
- não cria Item;
- não altera Produto;
- não altera Insumo;
- não altera `IdentidadeConsolidada`;
- não recalcula/persiste qualquer custo.

## POST inválido da base

O comportamento implementado no UC015 continua obrigatório.

Se o POST de Rendimento/Tempo ativo for inválido:

- retornar a página com os erros;
- preservar os valores informados pelo usuário;
- continuar carregando a composição completa;
- manter links Editar/Remover;
- não perder Observações contextuais da consulta;
- não mutar a Ficha persistida.

## Sem custo/preço nesta UC

A UC017 não consulta nem apresenta:

- preço vigente do Insumo;
- histórico de preços;
- custo unitário do Insumo;
- custo do Item;
- custo do lote;
- custo unitário do Produto;
- margem;
- Preço sugerido;
- totais monetários.

Esses dados começam no UC018 e etapas seguintes.

Mesmo que já exista preço vigente para um Insumo, a UC017 não o mostra na composição.

## Sem estado de precificação

A UC017 não decide se a precificação está completa ou incompleta.

Insumo sem preço continua aparecendo normalmente na composição.

RN007/RN017 serão consumidas pelo motor de custo.

## Filtros e pesquisa

Não adicionar filtros, pesquisa, paginação ou ordenação interativa no MVP desta UC.

A Ficha representa uma composição única de Produto e a consulta completa é uma lista determinística.

Se volume real justificar ferramentas de navegação, isso vira melhoria posterior.

## Domínio e persistência

Não alterar:

- `FichaTecnica`;
- `ItemFichaTecnica`;
- `Insumo`;
- `Produto`;
- configurações EF;
- migrations.

A UC017 é uma evolução da projeção/leitura Web.

## Critérios de aceitação

### CA01

Usuário autenticado com Empresa Ativa consegue consultar a Ficha de Produto do tenant.

### CA02

Produto inexistente ou de outro tenant retorna `404`.

### CA03

Produto sem Ficha continua acessível para criação da base e GET não cria Ficha.

### CA04

Ficha existente exibe Rendimento e Tempo ativo persistidos.

### CA05

Composição exibe Nome, Marca, Quantidade, Unidade, Observação contextual, Situação e Ações.

### CA06

Marca ausente é exibida como `—`.

### CA07

Observação contextual ausente é exibida como `—`.

### CA08

Observação contextual existente é exibida sem substituição pela Observação global do Insumo.

### CA09

Insumo inativo permanece visível com situação `Inativo`.

### CA10

Produto inativo permanece consultável e não é reativado.

### CA11

Itens são ordenados por NomeNormalizado e MarcaNormalizada.

### CA12

Ficha vazia exibe `Nenhum insumo adicionado.` e preserva `Adicionar insumo`.

### CA13

Ações Editar e Remover continuam disponíveis para cada Item.

### CA14

A composição não mostra Item de outra Ficha ou tenant.

### CA15

GET não muta Produto/Ficha/Item/Insumo.

### CA16

POST inválido da base mantém a composição completa renderizada sem alterar a Ficha persistida.

### CA17

A página não exibe custo, preço, total ou margem.

### CA18

Insumo sem preço pode ser exibido normalmente e não gera custo zero.

## Matriz de testes

Não há novos testes unitários ou de persistência obrigatórios se não houver alteração de domínio/schema.

### Web

- W1: acesso exige autenticação e Empresa Ativa;
- W2: Produto inexistente/cross-tenant retorna 404;
- W3: Produto sem Ficha renderiza superfície de criação, sem `Adicionar insumo`, e GET não cria Ficha; após MEL019, o campo de entrada de Rendimento pode vir preenchido com `1` sem alterar esse estado;
- W4: Ficha existente carrega Rendimento e Tempo ativo persistidos;
- W5: Item exibe Nome e Marca em campos/colunas distintos;
- W6: Item exibe Quantidade formatada e Unidade base;
- W7: Observação contextual existente é exibida e ausência vira `—`;
- W8: Observação global do Insumo não é usada como Observação contextual;
- W9: Insumo inativo aparece como `Inativo` com Editar/Remover disponíveis;
- W10: Produto inativo permite consulta sem reativação;
- W11: múltiplos Itens aparecem na ordem NomeNormalizado/MarcaNormalizada;
- W12: Ficha sem Itens exibe mensagem vazia e `Adicionar insumo`; 
- W13: composição não mistura Itens de outra Ficha nem outro tenant;
- W14: GET não muta nenhuma entidade;
- W15: POST inválido da base preserva composição completa e ações;
- W16: não há Custo/Preço/Total/Margem na composição.

Reutilizar testes atuais de UC015/UC016 quando já provarem parte da regra; atualizar asserts que hoje exigem ausência de Observação contextual porque essa ausência deixa de ser correta.

## Alterações esperadas

### `FichaTecnica.cshtml.cs`

Expandir `ItemFichaResumo` para carregar no mínimo:

~~~text
Id
InsumoId
Nome
Marca
Quantidade formatada
Unidade
Observacao contextual
InsumoAtivo
~~~

É aceitável manter `Quantidade` já formatada na projeção Web conforme padrão atual.

### `FichaTecnica.cshtml`

Evoluir a tabela para:

~~~text
Insumo
Marca
Quantidade
Unidade
Observação contextual
Situação
Ações
~~~

Manter:

- Adicionar insumo;
- Editar;
- Remover;
- mensagem de Ficha vazia;
- formulário da base.

### Testes

Preferir ampliar `ItemFichaTecnicaPageTests` porque a consulta da Ficha já é exercitada ali.

Evitar criar nova suíte só para duplicar setup de Produto/Ficha/Item.

## Relação com UC018

A UC017 encerra a visualização estrutural da Ficha.

O próximo UC de custo poderá reutilizar a mesma composição, mas deve adicionar seus próprios conceitos de preço vigente e custo sem retroativamente transformar a UC017 em uma tela financeira.

## Relação com UC030

UC030 depende de UC017 porque precisa distinguir Produto com e sem Ficha/composição disponível.

A UC017 não implementa o estado `Incompleto`; apenas garante que a estrutura atual da Ficha seja consultável.

## Fora do escopo

- custo do Item;
- preço vigente;
- totais;
- margem;
- status de precificação;
- filtros;
- pesquisa;
- paginação;
- exportação;
- impressão;
- histórico/versionamento de Ficha;
- soft delete;
- estoque;
- equipamento;
- perdas;
- alterar modelo de Item;
- alterar modelo de Ficha;
- nova rota de detalhes da Ficha;
- API REST.

## Revalidação pós-UC016

A implementação real confirma que:

1. `/Produtos/FichaTecnica/{id}` já é a rota canônica da Ficha;
2. a página já carrega Produto, base e Itens pelo tenant ativo;
3. UC015 criou a lista mínima com Nome/Marca combinados, Quantidade, Unidade, Situação e Editar;
4. UC016 adicionou Remover mantendo a mesma lista;
5. a única informação funcional do Item omitida da consulta atual é a Observação contextual;
6. Marca já existe na projeção de leitura indiretamente, mas deve ser apresentada separadamente na consulta completa;
7. Insumo inativo já é carregado e permanece administrável;
8. Produto inativo já é suportado;
9. o POST inválido da base já recarrega os Itens;
10. não existe necessidade de migration ou regra de domínio nova;
11. custos/preços permanecem corretamente reservados ao UC018+.

Portanto, UC017 está liberada como incremento predominantemente de apresentação/projeção Web.

## Branch sugerida

~~~text
feat/uc017-consultar-ficha-composicao
~~~

## Commit sugerido

~~~text
feat: completa consulta da ficha tecnica
~~~
