# UC002 — Listar e consultar insumos

- **Status:** Especificado — aguarda implementação do UC001A
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001 e UC001A implementados
- **Próximo caso relacionado:** UC003 — Editar insumo

## Objetivo

Permitir que o usuário visualize o catálogo de insumos cadastrados, pesquise por nome ou marca e consulte os dados completos de um insumo sem modificar seu cadastro.

## Ator

Usuário local do Precificador.

## Pré-condições

- UC001 implementado;
- UC001A implementado;
- migrations aplicadas ao banco da aplicação.

## Gatilho

O usuário acessa a área **Insumos** pela navegação da aplicação.

## Escopo funcional

O UC possui duas visualizações:

1. listagem/pesquisa de insumos;
2. consulta detalhada de um insumo.

Não há operação de edição, desativação ou preço neste UC.

## Listagem

Criar Razor Page:

```text
/Insumos
```

A página deve listar todos os insumos existentes, ativos e inativos.

### Colunas mínimas

- Nome;
- Marca;
- Categoria;
- Unidade base;
- Situação;
- ação `Consultar`.

A observação não deve ocupar uma coluna da listagem para evitar excesso de informação; ela aparece nos detalhes.

Quando Marca for ausente, apresentar valor visual neutro como `—` ou `Sem marca`, mantendo consistência em toda a aplicação.

### Ordenação padrão

Ordenar por:

1. Nome, usando sua representação normalizada;
2. Marca, usando sua representação normalizada.

A ordenação deve ser determinística.

### Insumos inativos

Insumos inativos permanecem visíveis e devem ter situação claramente indicada. Isso preserva a legibilidade histórica exigida pela RN008.

Não implementar filtro de ativo/inativo neste UC.

## Pesquisa

A listagem deve possuir um único campo de pesquisa livre, enviado por query string.

Parâmetro sugerido:

```text
q
```

A pesquisa deve procurar correspondência parcial em:

- Nome;
- Marca.

Regras:

- ignorar diferenças de maiúsculas/minúsculas;
- normalizar espaços externos e sequências de whitespace da consulta;
- não remover acentos;
- pesquisa por marca deve localizar todos os insumos daquela marca;
- pesquisa por nome deve localizar todas as marcas daquele item.

Exemplos:

```text
q=farinha
```

pode retornar:

```text
Farinha de Trigo Branca / Renata
Farinha de Trigo Branca / Caputo
Farinha de Trigo Integral / Renata
```

```text
q=renata
```

pode retornar todos os insumos cuja Marca seja Renata.

Não é obrigatório interpretar uma frase como `farinha renata` como combinação estruturada de Nome + Marca. O termo deve ser tratado como pesquisa livre simples conforme implementação definida, sem introduzir parser de busca.

## Estado vazio

Se não houver insumos cadastrados, apresentar mensagem clara e um acesso para **Cadastrar insumo**.

Se houver insumos, mas a pesquisa não retornar resultados, apresentar mensagem como:

```text
Nenhum insumo encontrado para a pesquisa informada.
```

Não tratar lista vazia como erro.

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

Não exibir ao usuário campos técnicos como:

- `NomeNormalizado`;
- `MarcaNormalizada`.

Quando Observação não existir, apresentar estado neutro (`—` ou equivalente).

### Insumo inexistente

Se o `id` não existir, retornar HTTP 404.

Não redirecionar silenciosamente para a listagem e não inventar registro vazio.

## Navegação

A navegação principal deve possuir acesso **Insumos** para `/Insumos`.

Na listagem:

- botão/link **Cadastrar insumo** aponta para `/Insumos/Novo`;
- cada linha possui ação **Consultar**.

Na página de detalhes:

- disponibilizar retorno para a listagem;
- não adicionar botão Editar até o UC003.

É permitido substituir o link isolado `Cadastrar insumo` criado no UC001 por uma navegação mais coerente através da área `Insumos`, desde que o cadastro continue facilmente acessível.

## Consultas e arquitetura

- consultas de leitura podem permanecer no PageModel usando EF Core diretamente, conforme arquitetura aprovada;
- usar `AsNoTracking` nas consultas puramente de leitura;
- não criar repository genérico, CQRS, MediatR ou camada adicional apenas para este UC;
- evitar carregar dados que não serão apresentados;
- nenhuma migration é esperada no UC002 se o UC001A tiver sido concluído corretamente.

## Regras de negócio aplicáveis

- RN001 — Unidade base;
- RN008 — Desativação de insumo;
- RN028 — Nome do insumo;
- RN029 — Unicidade do insumo por nome e marca;
- RN030 — Categoria do insumo;
- RN032 — Marca do insumo;
- RN033 — Observação do insumo.

## Critérios de aceitação

### CA01 — Listagem acessível

**Dado** a aplicação inicializada  
**Quando** o usuário acessar `/Insumos`  
**Então** a página deve responder com sucesso.

### CA02 — Dados listados

**Dado** insumos cadastrados  
**Quando** o usuário abrir a listagem  
**Então** cada registro deve apresentar Nome, Marca, Categoria, Unidade base e Situação.

### CA03 — Marcas distintas permanecem distintas

**Dado** `Farinha de Trigo Branca / Renata` e `Farinha de Trigo Branca / Caputo`  
**Quando** a listagem for exibida  
**Então** ambos devem aparecer como registros separados.

### CA04 — Pesquisa por nome

**Dado** múltiplos insumos  
**Quando** `q=farinha` for informado  
**Então** somente registros cujo Nome contenha o termo normalizado devem ser retornados, além de eventuais correspondências de Marca previstas pela busca livre.

### CA05 — Pesquisa por marca

**Dado** múltiplos insumos de marcas diferentes  
**Quando** `q=renata` for informado  
**Então** insumos cuja Marca contenha `Renata` devem ser retornados independentemente de caixa.

### CA06 — Pesquisa sem resultado

**Quando** nenhuma correspondência existir  
**Então** a página deve responder com sucesso  
**E** apresentar mensagem de nenhum resultado.

### CA07 — Ordenação

**Quando** a listagem contiver vários registros  
**Então** deve ser ordenada por Nome e depois Marca de forma determinística.

### CA08 — Inativos visíveis

**Dado** um insumo inativo existente  
**Quando** a listagem ou detalhes forem consultados  
**Então** o registro deve permanecer visível  
**E** sua situação deve ser identificável.

### CA09 — Detalhes

**Dado** um insumo existente  
**Quando** `/Insumos/Detalhes/{id}` for acessado  
**Então** Nome, Marca, Categoria, Unidade base, Situação e Observação devem ser apresentados.

### CA10 — Detalhes sem campos técnicos

**Quando** os detalhes forem exibidos  
**Então** `NomeNormalizado` e `MarcaNormalizada` não devem aparecer na interface.

### CA11 — Id inexistente

**Quando** for solicitado um `id` inexistente  
**Então** a aplicação deve responder HTTP 404.

### CA12 — Somente leitura

**Ao utilizar listagem, pesquisa ou detalhes**  
**Então** nenhum dado do insumo deve ser modificado.

### CA13 — Sem escopo antecipado

**Ao revisar o diff**  
**Então** não deve existir edição, desativação, reativação, preço, histórico de preço ou ficha técnica.

## Testes esperados

### Unitários

Nenhum teste unitário artificial é obrigatório se o UC002 não introduzir nova regra pura de domínio.

Se houver helper de normalização compartilhado por necessidade real, testar somente seu comportamento público relevante e não detalhes de implementação.

### Integração — persistência/consulta

Cobrir no mínimo:

- listagem recupera ativos e inativos;
- ordenação por Nome e Marca;
- pesquisa por Nome;
- pesquisa por Marca;
- pesquisa ignora caixa e espaços acidentais;
- pesquisa sem resultado retorna coleção vazia sem erro.

Usar SQLite, não provider EF InMemory.

### Integração — web

Cobrir no mínimo:

- `GET /Insumos` retorna sucesso;
- listagem apresenta registros cadastrados;
- mesmo Nome com marcas diferentes aparece em linhas distintas;
- busca por Nome filtra resultados;
- busca por Marca filtra resultados;
- estado vazio é apresentado corretamente;
- detalhes exibem os campos permitidos;
- detalhes de id inexistente retornam 404;
- navegação entre lista, cadastro e detalhes funciona.

## Persistência e migration

Nenhuma alteração de schema é esperada neste UC.

Se durante a implementação surgir necessidade de migration, isso indica possível mistura de escopo com o UC001A e deve ser revisado antes de prosseguir.

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
- ficha técnica.

## Definition of Done específica

Além da DoD global:

- `/Insumos` lista e pesquisa sem modificar dados;
- `/Insumos/Detalhes/{id}` consulta um registro ou retorna 404;
- Marca está visível onde necessário sem ser concatenada tecnicamente ao Nome;
- Observação aparece apenas nos detalhes;
- consultas de leitura usam `AsNoTracking` quando aplicável;
- nenhuma migration é criada;
- testes cobrem listagem, busca, detalhes e estados vazios;
- nenhum comportamento de UC003+ é antecipado.
