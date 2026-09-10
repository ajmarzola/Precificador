# UC001B — Generalizar categoria e unidades de insumo

- **Status:** Especificado — pronto para implementação
- **Tipo:** ajuste de domínio do cadastro de Insumo
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001 implementado e FT002 implementada
- **Próximo caso relacionado:** UC001A — Complementar cadastro com marca e observação

## Motivo da alteração

O UC001 foi criado inicialmente para o cenário de panificação e adotou a categoria `Ingrediente` e as unidades `g`, `ml` e `un`.

Com a evolução do Precificador para atender também negócios de papelaria artesanal, o termo `Ingrediente` tornou-se específico demais. Materiais como papel devem participar da mesma lógica de custo sem serem classificados artificialmente como ingrediente.

Também foi identificado que materiais lineares, como fitas, cordões, vinis e outros itens de papelaria, precisam ser medidos em metros.

## Objetivo

Generalizar o vocabulário de Insumos sem alterar a estrutura de persistência:

- substituir `Ingrediente` por `MateriaPrima`;
- preservar o valor numérico `1` já persistido;
- manter `Embalagem = 2` e `Consumivel = 3`;
- acrescentar `Metro = 4` às unidades de medida;
- atualizar a interface e os testes do cadastro de Insumo.

## Categoria de Insumo

O domínio passa a adotar:

```text
MateriaPrima = 1
Embalagem = 2
Consumivel = 3
```

### Matéria-prima

Material que compõe diretamente o produto ou é consumido como material principal de sua produção.

Exemplos:

- farinha;
- açúcar;
- chocolate;
- papel;
- papelão;
- vinil adesivo;
- tecido quando fizer parte do produto.

O termo não determina regra automática de perda. Perdas serão modeladas separadamente como conceito de material/processo quando aplicável.

### Compatibilidade

`Ingrediente = 1` será renomeado no código para `MateriaPrima = 1`.

O valor numérico **não deve mudar**, pois registros existentes persistem a categoria como inteiro. Um Insumo armazenado anteriormente com valor `1` deve ser materializado após a alteração como `MateriaPrima` sem necessidade de transformação dos dados.

Não manter os dois nomes (`Ingrediente` e `MateriaPrima`) como aliases funcionais no enum. O vocabulário novo deve ser único para evitar ambiguidade no domínio.

## Unidade base

O conjunto aprovado passa a ser:

```text
Grama = 1        -> g
Mililitro = 2    -> ml
Unidade = 3      -> un
Metro = 4        -> m
```

A numeração dos valores existentes deve ser preservada.

### Metro

`Metro` representa quantidade linear em metros.

Exemplos:

- fita;
- cordão;
- tecido;
- vinil em rolo;
- materiais comprados/utilizados por comprimento.

Não introduzir centímetros como unidade base nesta etapa. Quantidades menores que um metro devem ser representadas decimalmente, por exemplo `0,25 m`.

## Interface

Atualizar `/Insumos/Novo`.

Categoria deve exibir:

- Matéria-prima;
- Embalagem;
- Consumível.

Unidade base deve exibir:

- g;
- ml;
- m;
- un.

Não alterar os demais campos ou fluxos do UC001/FT002.

## Persistência e migration

Nenhuma alteração de schema é necessária.

A coluna `Categoria` continua inteira e o valor `1` mantém seu significado funcional, agora nomeado `MateriaPrima` no código.

A coluna `UnidadeBase` continua inteira e passa a aceitar também o novo valor `4` (`Metro`).

**Não criar migration vazia** somente para registrar a mudança de nomes/valores do enum.

Se durante a implementação surgir necessidade real de mudança de schema, interromper a expansão do escopo e revisar a decisão antes de gerar migration.

## Regras de negócio relacionadas

Este UC atualiza a interpretação de:

- RN001 — Unidade base;
- RN030 — Categoria do Insumo.

Após a implementação, RN001 deve reconhecer `g`, `ml`, `m` e `un`, e RN030 deve utilizar `Matéria-prima`, `Embalagem` e `Consumível`.

## Critérios de aceitação

### CA01 — Categoria Matéria-prima

**Quando** um novo Insumo for cadastrado como Matéria-prima  
**Então** deve ser persistido com `Categoria = 1`  
**E** recuperado pelo domínio como `CategoriaInsumo.MateriaPrima`.

### CA02 — Compatibilidade de registros existentes

**Dado** um Insumo criado antes deste UC com `Categoria = 1`  
**Quando** for carregado pelo código novo  
**Então** deve ser interpretado como Matéria-prima sem alteração do dado persistido.

### CA03 — Metro

**Quando** um novo Insumo for cadastrado com unidade `m`  
**Então** deve ser persistido com `UnidadeBase = 4`  
**E** recuperado como `UnidadeMedida.Metro`.

### CA04 — Valores existentes preservados

`Grama = 1`, `Mililitro = 2` e `Unidade = 3` permanecem com os mesmos valores numéricos.

### CA05 — Interface atualizada

A página `/Insumos/Novo` não deve mais apresentar `Ingrediente` e deve apresentar `Matéria-prima`; a lista de unidades deve incluir `m`.

### CA06 — Cadastro existente continua funcional

Os fluxos válidos e inválidos do UC001 permanecem funcionando após a alteração.

### CA07 — Sem migration desnecessária

O diff não contém migration vazia ou alteração de schema causada apenas pela mudança dos enums.

### CA08 — Sem escopo antecipado

O diff não implementa Marca/Observação do UC001A, listagem do UC002, preço, Produto ou Ficha Técnica.

## Testes esperados

### Unitários

Cobrir no mínimo:

- `CategoriaInsumo.MateriaPrima` possui valor `1`;
- não existe valor funcional `Ingrediente` no domínio novo;
- criação válida aceita Matéria-prima;
- `UnidadeMedida.Metro` possui valor `4`;
- criação válida aceita Metro;
- valores numéricos existentes de categoria/unidade permanecem inalterados;
- valores inválidos continuam rejeitados.

### Integração

Cobrir no mínimo:

- Insumo já persistido com Categoria `1` é recuperado como Matéria-prima;
- Insumo com Metro persiste e é recuperado corretamente;
- POST `/Insumos/Novo` aceita Matéria-prima;
- POST `/Insumos/Novo` aceita unidade Metro;
- formulário apresenta os novos rótulos e não apresenta `Ingrediente`.

Os testes permanecem tenant-aware após FT002 e não podem acessar o banco real do usuário.

## Fora do escopo

- Marca e Observação — UC001A;
- novas categorias além de Matéria-prima, Embalagem e Consumível;
- novas unidades além de g, ml, m e un;
- conversão automática entre unidades;
- unidade de compra diferente da unidade base — será tratada pelo modelo de preço quando necessário;
- regras de perda específicas;
- listagem/consulta — UC002;
- preço/histórico — UC005/UC006.

## Definition of Done específica

- `CategoriaInsumo` usa `MateriaPrima = 1`;
- `UnidadeMedida` inclui `Metro = 4`;
- valores numéricos anteriores são preservados;
- formulário do cadastro usa os novos rótulos;
- nenhuma migration desnecessária é criada;
- testes cobrem compatibilidade e novos valores;
- suíte completa permanece verde;
- UC001A/UC002+ não são antecipados.
