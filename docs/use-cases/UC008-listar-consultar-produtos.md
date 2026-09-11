# UC008 — Listar e consultar produtos

- **Status:** Revalidado — pronto para implementação após UC007
- **Funcionalidade:** F002 — Gestão de Produtos
- **Dependência material:** UC007 implementado
- **Sequenciamento:** implementar somente após UC007 ser implementado, revisado e mergeado
- **Próximo caso relacionado:** UC009 — Editar produto
- **Sem alteração de schema:** este UC é exclusivamente de consulta/apresentação

## Objetivo

Permitir que o usuário autenticado visualize o catálogo de Produtos da **Empresa Ativa**, pesquise por Nome e consulte os dados cadastrais de um Produto sem modificar seu estado.

O UC008 é exclusivamente de leitura e deve preservar o isolamento tenant-aware introduzido pela FT002 e aplicado ao Produto pelo UC007.

## Escopo funcional

O UC possui duas visualizações:

1. listagem/pesquisa de Produtos;
2. consulta detalhada de um Produto.

Não implementar neste UC:

- edição;
- desativação;
- preço de venda;
- histórico de preço de venda;
- Ficha Técnica;
- custo;
- preço sugerido;
- margem atual.

## Isolamento por Empresa

Toda consulta deve operar somente sobre a Empresa Ativa.

Regras:

- usar o Global Query Filter de Produto;
- não repetir `Where(EmpresaId == ...)` como mecanismo principal de segurança;
- não usar `IgnoreQueryFilters` no fluxo comum;
- usar `AsNoTracking` em consultas puramente de leitura;
- Produto de outra Empresa nunca aparece em lista, pesquisa ou detalhes;
- `EmpresaId` não deve ser exibido.

Ao consultar detalhes por `id`, registro de outro tenant deve ser indistinguível de inexistente e retornar **HTTP 404**.

## Listagem

Criar Razor Page:

~~~text
/Produtos
~~~

A página lista todos os Produtos visíveis da Empresa Ativa.

### Colunas mínimas

- Nome;
- Categoria;
- Margem-alvo;
- Situação;
- ação **Consultar**.

Não exibir na listagem:

- EmpresaId;
- NomeNormalizado;
- preço de venda;
- custo;
- preço sugerido;
- margem atual;
- informações de Ficha Técnica.

### Categoria ausente

Quando Categoria for `null`, apresentar:

~~~text
—
~~~

### Margem-alvo

A margem é persistida como fração decimal, mas deve ser exibida como percentual.

Exemplos:

~~~text
0,30  -> 30%
0,255 -> 25,5%
0     -> 0%
~~~

Usar apresentação percentual com até 2 casas decimais, removendo zeros finais desnecessários.

Não alterar o valor persistido.

### Situação

Exibir:

- Ativo;
- Inativo.

Embora Produtos nasçam ativos no UC007 e a desativação só seja implementada no UC010, o UC008 já deve ler e apresentar corretamente o campo `Ativo` existente.

Quando UC010 existir, inativos continuarão visíveis para preservar legibilidade histórica.

Não implementar filtro por situação neste UC.

### Ordenação padrão

Ordenar por:

~~~text
NomeNormalizado ASC
~~~

Como Nome é único por Empresa pela RN042, a ordenação é determinística no tenant.

## Pesquisa

A listagem possui um único campo por query string:

~~~text
q
~~~

A pesquisa procura correspondência parcial **somente em Nome**.

### Justificativa

Categoria é texto livre opcional e, no UC007, não possui `CategoriaNormalizada`.

O UC008 não deve criar schema adicional nem introduzir normalização técnica de Categoria apenas para pesquisa.

Se pesquisa/filtro por Categoria se tornar necessária, deverá ser avaliada em melhoria/UC próprio.

### Normalização da consulta

Antes da pesquisa:

- remover whitespace externo;
- reduzir sequências internas de whitespace a um único espaço;
- converter para maiúsculas com regra invariável;
- não remover acentos.

Comparar com:

~~~text
NomeNormalizado.Contains(termoNormalizado)
~~~

Consequências:

- caixa é ignorada;
- whitespace acidental é normalizado;
- acentos permanecem significativos;
- `q` nulo/vazio/whitespace equivale à listagem completa.

Não implementar parser estruturado.

## Estado vazio

### Nenhum Produto cadastrado

Exibir mensagem clara e ação:

~~~text
Cadastrar produto
~~~

### Pesquisa sem resultado

Exibir mensagem de nenhum resultado, preservando o campo de pesquisa e oferecendo retorno à listagem completa.

Nenhum desses estados é erro.

## Detalhes

Criar Razor Page:

~~~text
/Produtos/Detalhes/{id:int}
~~~

### Dados exibidos

- Nome;
- Categoria;
- Margem-alvo;
- Situação.

Quando Categoria não existir, mostrar `—`.

### Não exibir

- EmpresaId;
- NomeNormalizado;
- preço de venda;
- histórico de venda;
- custo;
- preço teórico/sugerido;
- margem atual;
- dados de Ficha Técnica.

### Inexistente/cross-tenant

Se o `id` não existir na Empresa Ativa:

~~~text
HTTP 404
~~~

Isso inclui Produto pertencente a outro tenant.

Não usar `IgnoreQueryFilters` para diferenciar os casos.

## Navegação

A navegação principal deve possuir:

~~~text
Produtos
~~~

apontando para:

~~~text
/Produtos
~~~

Na listagem:

- **Cadastrar produto** -> `/Produtos/Novo`;
- **Consultar** -> `/Produtos/Detalhes/{id}`.

Nos detalhes:

- **Voltar para produtos** -> `/Produtos`.

O acesso temporário **Cadastrar produto** criado na Home pelo UC007 pode ser removido ou mantido, desde que a navegação principal passe a ser a entrada oficial da área Produtos.

Não adicionar **Editar** até UC009.

## Arquitetura

Usar EF Core diretamente nos PageModels, coerente com a arquitetura atual.

Recomendações:

- `AsNoTracking`;
- projeções simples para modelos de exibição quando úteis;
- Global Query Filters ativos;
- sem repository genérico;
- sem CQRS/MediatR;
- sem camada de serviço criada apenas para consultas simples.

Nenhuma migration é esperada.

## Regras aplicáveis

- RN018 — Desativação de Produto;
- RN019 — Margem-alvo válida;
- RN035 — Propriedade por Empresa;
- RN036 — Isolamento de Empresa;
- RN037 — Empresa Ativa;
- RN041 — Nome do Produto;
- RN042 — Unicidade do Produto;
- RN043 — Categoria opcional;
- RN044 — Situação inicial;
- RN045 — Margem-alvo do Produto;
- RN046 — Produto pode existir sem preço de venda.

## Critérios de aceitação

### CA01 — Acesso protegido

Usuário anônimo não acessa `/Produtos` nem detalhes.

### CA02 — Listagem do tenant

`/Produtos` exibe somente Produtos da Empresa Ativa.

### CA03 — Dados listados

Cada linha mostra Nome, Categoria, Margem-alvo e Situação.

### CA04 — Categoria ausente usa estado neutro

Produto sem Categoria exibe `—`.

### CA05 — Margem é apresentada como percentual

Valores persistidos em fração são exibidos corretamente como percentual, sem alterar persistência.

### CA06 — Pesquisa por Nome

`q` filtra por correspondência parcial em `NomeNormalizado`.

### CA07 — Pesquisa normaliza caixa e whitespace

Diferenças apenas de caixa/whitespace da consulta não alteram o conjunto esperado.

Acentos permanecem significativos.

### CA08 — Pesquisa vazia

`q` nulo/vazio/whitespace equivale à listagem completa.

### CA09 — Pesquisa não usa Categoria

Termo presente apenas em Categoria não deve ser considerado correspondência por este UC.

### CA10 — Ordenação

Listagem ordena por `NomeNormalizado ASC`.

### CA11 — Situação é exibida

Campo `Ativo` é apresentado como Ativo/Inativo.

### CA12 — Estado vazio

Empresa sem Produtos exibe mensagem e acesso a **Cadastrar produto**.

### CA13 — Pesquisa sem resultado

Página responde com sucesso e mostra estado apropriado.

### CA14 — Detalhes válidos

Produto da Empresa Ativa exibe Nome, Categoria, Margem-alvo e Situação.

### CA15 — Campos técnicos/futuros ocultos

Listagem/detalhes não exibem EmpresaId, NomeNormalizado, preço, custo ou Ficha Técnica.

### CA16 — Id inexistente

Retorna 404.

### CA17 — Id de outro tenant

Retorna 404 sem revelar dados.

### CA18 — Somente leitura

Listagem, pesquisa e detalhes não alteram qualquer dado.

### CA19 — Sem migration

Nenhuma migration/ModelSnapshot.

### CA20 — Sem escopo antecipado

Não implementar UC009+, preço de venda, Ficha Técnica, custo, dashboard ou filtros avançados.

## Matriz de testes fechada antes da implementação

### Unitários

Nenhum teste unitário novo é obrigatório se não surgir lógica pura compartilhada.

Não criar helper artificial apenas para justificar teste unitário.

### Integração — persistência/consulta

#### P1

~~~text
CA02_CA10_Query_filter_isola_produtos_e_ordena_por_nome_normalizado
~~~

Duas Empresas, múltiplos Produtos.

#### P2

~~~text
CA06_CA07_Pesquisa_parcial_por_nome_normaliza_caixa_e_whitespace
~~~

#### P3

~~~text
CA08_Pesquisa_vazia_equivale_a_listagem_completa
~~~

#### P4

~~~text
CA09_Pesquisa_nao_considera_categoria
~~~

#### P5

~~~text
CA13_Pesquisa_sem_resultado_retorna_colecao_vazia_sem_erro
~~~

Usar SQLite real/in-memory quando houver acesso à persistência.

### Integração — Web

#### W1

~~~text
CA01_Area_de_produtos_exige_autenticacao
~~~

Cobrir listagem e detalhes.

#### W2

~~~text
CA02_CA03_Listagem_exibe_apenas_produtos_do_tenant_com_campos_funcionais
~~~

#### W3

~~~text
CA04_Produto_sem_categoria_exibe_traco
~~~

#### W4

~~~text
CA05_Margem_alvo_e_exibida_como_percentual
~~~

Usar pelo menos:

- 0;
- 0,255;
- 0,30.

#### W5

~~~text
CA06_CA07_Pesquisa_por_nome_filtra_e_normaliza_consulta
~~~

#### W6

~~~text
CA08_Pesquisa_vazia_mantem_listagem
~~~

#### W7

~~~text
CA09_Termo_presente_apenas_na_categoria_nao_filtra_produto
~~~

#### W8

~~~text
CA12_Empresa_sem_produtos_exibe_estado_vazio_e_link_de_cadastro
~~~

#### W9

~~~text
CA13_Pesquisa_sem_resultado_exibe_estado_apropriado
~~~

#### W10

~~~text
CA14_CA15_Detalhes_exibem_somente_campos_permitidos
~~~

#### W11

~~~text
CA16_Detalhes_de_id_inexistente_retorna_404
~~~

#### W12

~~~text
CA17_Detalhes_cross_tenant_retorna_404
~~~

#### W13

~~~text
CA11_Listagem_e_detalhes_apresentam_situacao
~~~

Preparar Produto inativo diretamente pelo domínio/persistência somente se o método de domínio já existir no momento da implementação; se UC010 ainda não existir e não houver forma legítima de criar inativo, cobrir a apresentação de Ativo e preservar o requisito para regressão no UC010, sem adicionar método de desativação antecipadamente.

#### W14

~~~text
Navegacao_principal_aponta_para_produtos_e_lista_navega_para_cadastro_e_detalhes
~~~

## Migration

Nenhuma migration.

Não alterar migrations históricas.

Não alterar ModelSnapshot.

Se surgir necessidade de schema, interromper e reavaliar o escopo.

## Fora do escopo

- UC009 edição;
- UC010 desativação;
- UC011 preço de venda;
- UC012 histórico de venda;
- Ficha Técnica;
- custo;
- margem atual;
- preço teórico/sugerido;
- filtro por Categoria;
- filtro por Situação;
- paginação;
- ordenação configurável;
- exportação;
- gráficos;
- API REST.

## Definition of Done específica

Além da DoD global:

- `/Produtos` lista somente a Empresa Ativa;
- pesquisa por Nome funciona com normalização;
- Categoria é exibida, mas não pesquisada;
- margem aparece como percentual;
- `/Produtos/Detalhes/{id}` respeita tenancy/404;
- consultas puras usam `AsNoTracking`;
- Global Query Filter permanece ativo;
- navegação principal possui Produtos;
- cadastro continua acessível;
- nenhuma migration;
- nenhuma mutação;
- nenhum UC009+;
- UC008 passa para Implementado;
- F002/catálogo/ordem ficam coerentes;
- UC009 passa a próximo caso de Produtos;
- implementação só inicia após UC007 implementado/revisado/mergeado;
- build Release sem warnings novos relevantes;
- suíte completa verde.
