# UC002 — Listar e consultar insumos

- **Status:** Revalidado — pronto para implementação
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001, FT002, UC001B e UC001A implementados
- **Próximo caso relacionado:** UC003 — Editar insumo

## Objetivo

Permitir que o usuário autenticado visualize o catálogo de Insumos da **Empresa Ativa**, pesquise por Nome ou Marca e consulte os dados completos de um Insumo sem modificar seu cadastro.

O UC002 é exclusivamente de leitura e deve preservar integralmente o isolamento tenant-aware introduzido pela FT002.

## Ator

Usuário autenticado do Precificador com Empresa Ativa válida.

## Pré-condições

- FT002 implementada;
- UC001, UC001B e UC001A implementados;
- usuário autenticado;
- Empresa Ativa válida;
- migrations existentes aplicadas ao banco da aplicação.

## Gatilho

O usuário acessa a área **Insumos** pela navegação da aplicação.

## Escopo funcional

O UC possui duas visualizações:

1. listagem/pesquisa de Insumos;
2. consulta detalhada de um Insumo.

Não há operação de edição, desativação, reativação, preço ou histórico neste UC.

## Isolamento por Empresa

Toda consulta deste UC deve operar somente sobre a Empresa Ativa.

Regras obrigatórias:

- usar o Global Query Filter já existente no `PrecificadorDbContext`;
- não repetir manualmente `Where(EmpresaId == ...)` como mecanismo principal de segurança;
- não usar `IgnoreQueryFilters` no fluxo comum de negócio;
- dados de outra Empresa nunca podem aparecer na listagem, pesquisa ou detalhes;
- ausência de Empresa Ativa não concede acesso a dados tenant-owned;
- `EmpresaId` é informação técnica e não deve ser exibido na interface.

Ao consultar detalhes por `id`, um registro pertencente a outra Empresa deve ser indistinguível de um registro inexistente para o usuário comum e resultar em **HTTP 404**, sem revelar sua existência.

## Listagem

Criar Razor Page:

```text
/Insumos
```

A página deve listar todos os Insumos da Empresa Ativa, ativos e inativos.

### Colunas mínimas

- Nome;
- Marca;
- Categoria;
- Unidade base;
- Situação;
- ação `Consultar`.

A Observação não deve ocupar uma coluna da listagem; ela aparece nos detalhes.

Quando Marca for ausente, apresentar valor visual neutro consistente, preferencialmente `—`.

### Ordenação padrão

Ordenar por:

1. `NomeNormalizado`;
2. `MarcaNormalizada`.

A combinação é única dentro da Empresa após o UC001A, portanto essa ordenação é determinística no escopo do tenant.

### Insumos inativos

Insumos inativos permanecem visíveis e devem ter situação claramente indicada, preservando a legibilidade histórica exigida pela RN008.

Não implementar filtro de ativo/inativo neste UC.

## Pesquisa

A listagem deve possuir um único campo de pesquisa livre enviado por query string:

```text
q
```

A pesquisa procura correspondência parcial em:

- Nome;
- Marca.

### Normalização da consulta

Antes de pesquisar:

- remover whitespace externo;
- reduzir sequências internas de whitespace a um único espaço;
- converter o termo para maiúsculas com regra invariável;
- não remover acentos.

A comparação deve utilizar as representações técnicas já existentes:

```text
NomeNormalizado
MarcaNormalizada
```

Conceitualmente:

```text
NomeNormalizado contém termo normalizado
OU
MarcaNormalizada contém termo normalizado
```

Consequências:

- diferenças de maiúsculas/minúsculas são ignoradas;
- espaços acidentais da consulta são ignorados;
- acentos permanecem significativos;
- pesquisa por Marca localiza os Insumos daquela Marca;
- pesquisa por Nome localiza todas as Marcas correspondentes;
- `q` nulo, vazio ou apenas whitespace equivale à listagem sem filtro.

Não é obrigatório interpretar `farinha renata` como combinação estruturada Nome + Marca. Não implementar parser de pesquisa.

## Estado vazio

Se a Empresa Ativa não possuir Insumos, apresentar mensagem clara e acesso para **Cadastrar insumo**.

Se houver Insumos, mas a pesquisa não retornar resultados, apresentar mensagem clara de nenhum resultado.

Lista vazia ou pesquisa sem resultado não são erros.

## Consulta detalhada

Criar Razor Page:

```text
/Insumos/Detalhes/{id:int}
```

### Dados exibidos

- Nome;
- Marca;
- Categoria;
- Unidade base;
- Situação;
- Observação.

Não exibir:

- `EmpresaId`;
- `NomeNormalizado`;
- `MarcaNormalizada`.

Quando Marca ou Observação não existir, apresentar estado neutro consistente, preferencialmente `—`.

### Insumo inexistente ou de outra Empresa

Se o `id` não existir **na Empresa Ativa**, retornar HTTP 404.

Isso inclui:

- id realmente inexistente;
- id válido pertencente a outra Empresa.

Não redirecionar silenciosamente para a listagem e não consultar o registro com `IgnoreQueryFilters` para distinguir esses casos.

## Navegação

A navegação principal deve possuir acesso **Insumos** para `/Insumos`.

Na listagem:

- **Cadastrar insumo** aponta para `/Insumos/Novo`;
- cada linha possui ação **Consultar** para o respectivo detalhe.

Nos detalhes:

- disponibilizar retorno para a listagem;
- não adicionar botão Editar até o UC003.

É permitido reorganizar o link isolado de cadastro criado no UC001 para uma navegação coerente através da área Insumos, desde que o cadastro continue facilmente acessível.

## Consultas e arquitetura

- usar EF Core diretamente nos PageModels, conforme arquitetura atual;
- usar `AsNoTracking` em todas as consultas puramente de leitura deste UC;
- manter os Global Query Filters ativos;
- projeção para modelos de exibição é recomendada quando evitar carregar campos não utilizados, sem criar camada artificial;
- não criar repository genérico, CQRS, MediatR ou serviço adicional sem necessidade real;
- nenhuma migration é esperada.

## Regras de negócio aplicáveis

- RN001 — Unidade base;
- RN008 — Desativação de Insumo;
- RN028 — Nome do Insumo;
- RN029 — Unicidade do Insumo por Empresa, Nome e Marca;
- RN030 — Categoria do Insumo;
- RN032 — Marca do Insumo;
- RN033 — Observação do Insumo;
- RN035 — Propriedade por Empresa;
- RN036 — Isolamento de Empresa;
- RN037 — Empresa Ativa.

## Critérios de aceitação

### CA01 — Acesso protegido

**Dado** usuário não autenticado  
**Quando** acessar `/Insumos` ou detalhes  
**Então** deve ser direcionado ao fluxo de autenticação conforme a política atual.

### CA02 — Listagem da Empresa Ativa

**Dado** usuário autenticado com Empresa Ativa  
**Quando** acessar `/Insumos`  
**Então** a página deve responder com sucesso  
**E** exibir somente Insumos pertencentes à Empresa Ativa.

### CA03 — Dados listados

Cada registro listado deve apresentar Nome, Marca, Categoria, Unidade base e Situação.

### CA04 — Marcas distintas permanecem distintas

**Dado** `Farinha de Trigo Branca / Renata` e `Farinha de Trigo Branca / Caputo` na mesma Empresa  
**Quando** a listagem for exibida  
**Então** ambos devem aparecer como registros separados.

### CA05 — Pesquisa por Nome

**Quando** `q=farinha` for informado  
**Então** devem ser retornados os registros da Empresa Ativa cujo Nome normalizado contenha o termo, além de eventuais correspondências de Marca previstas pela busca livre.

### CA06 — Pesquisa por Marca

**Quando** `q=renata` for informado  
**Então** os Insumos da Empresa Ativa cuja Marca contenha `Renata` devem ser retornados independentemente de caixa.

### CA07 — Normalização da pesquisa

Diferenças apenas de caixa e whitespace acidental no termo não devem alterar o conjunto esperado de resultados.

Acentos permanecem significativos.

### CA08 — Pesquisa vazia

`q` nulo, vazio ou apenas whitespace deve retornar a listagem normal da Empresa Ativa.

### CA09 — Pesquisa sem resultado

Quando nenhuma correspondência existir, a página deve responder com sucesso e apresentar estado de nenhum resultado.

### CA10 — Ordenação

A listagem deve ser ordenada por `NomeNormalizado` e depois `MarcaNormalizada`.

### CA11 — Inativos visíveis

Insumos inativos da Empresa Ativa permanecem visíveis na listagem e detalhes, com situação identificável.

### CA12 — Detalhes

Um Insumo existente da Empresa Ativa deve exibir Nome, Marca, Categoria, Unidade base, Situação e Observação.

### CA13 — Campos técnicos ocultos

Detalhes e listagem não devem exibir `EmpresaId`, `NomeNormalizado` ou `MarcaNormalizada`.

### CA14 — Id inexistente

Id inexistente deve retornar HTTP 404.

### CA15 — Id de outro tenant

Id pertencente a outra Empresa deve retornar HTTP 404 e não revelar dados do registro.

### CA16 — Somente leitura

Listagem, pesquisa e detalhes não devem modificar qualquer dado persistido.

### CA17 — Sem migration

O diff não deve conter migration ou alteração de schema.

### CA18 — Sem escopo antecipado

O diff não deve implementar edição, desativação/reativação, preço, histórico de preço, Produto ou Ficha Técnica.

## Testes esperados

### Unitários

Nenhum teste unitário artificial é obrigatório se o UC002 não introduzir regra pura de domínio.

Se surgir helper puro compartilhado para normalização da pesquisa, testar seu comportamento público relevante sem testar detalhes internos.

### Integração — persistência/consulta

Cobrir no mínimo:

- Empresa A recupera seus Insumos ativos e inativos;
- Insumos da Empresa B não aparecem para Empresa A;
- ausência de Empresa Ativa não retorna Insumos;
- ordenação por `NomeNormalizado` e `MarcaNormalizada`;
- pesquisa parcial por Nome;
- pesquisa parcial por Marca;
- pesquisa ignora caixa e normaliza whitespace;
- acentos permanecem significativos;
- pesquisa vazia equivale à listagem sem filtro;
- pesquisa sem resultado retorna coleção vazia sem erro;
- consultas de leitura não rastreiam entidades quando usado `AsNoTracking`.

Usar SQLite real/in-memory com conexão mantida, nunca provider EF InMemory.

### Integração — Web

Cobrir no mínimo:

- acesso anônimo à área de Insumos exige autenticação;
- `GET /Insumos` autenticado com Empresa Ativa retorna sucesso;
- listagem apresenta somente dados da Empresa Ativa;
- ativos e inativos aparecem;
- mesmo Nome com Marcas diferentes aparece em linhas distintas;
- busca por Nome filtra resultados;
- busca por Marca filtra resultados;
- busca com caixa/whitespace variável mantém resultado esperado;
- estado vazio é apresentado corretamente;
- pesquisa sem resultado possui estado apropriado;
- detalhes exibem somente os campos permitidos;
- detalhes de id inexistente retornam 404;
- detalhes de id de outra Empresa retornam 404;
- navegação entre lista, cadastro e detalhes funciona.

Os testes não podem usar o banco real do usuário.

## Persistência e migration

Nenhuma alteração de schema é necessária ou esperada neste UC.

Se surgir necessidade de migration, interromper a expansão do escopo e revisar a decisão antes de prosseguir.

Não alterar migrations históricas nem ModelSnapshot.

## Fora do escopo

- edição — UC003;
- desativação/reativação — UC004;
- registro de preço — UC005;
- histórico de preço — UC006;
- paginação;
- filtros avançados por categoria, unidade ou situação;
- ordenação escolhida pelo usuário;
- exportação;
- API REST;
- preço/custo na listagem;
- ficha técnica;
- parser estruturado de pesquisa;
- alterações na estratégia de autenticação ou tenancy.

## Definition of Done específica

Além da DoD global:

- `/Insumos` lista e pesquisa somente dados da Empresa Ativa;
- `/Insumos/Detalhes/{id}` consulta um registro do tenant ou retorna 404;
- id de outro tenant retorna 404;
- Marca aparece separadamente do Nome;
- Observação aparece apenas nos detalhes;
- consultas puras usam `AsNoTracking`;
- Global Query Filters permanecem ativos;
- nenhuma migration é criada;
- testes cobrem listagem, busca, detalhes, estados vazios e isolamento cross-tenant;
- nenhum comportamento de UC003+ é antecipado.
