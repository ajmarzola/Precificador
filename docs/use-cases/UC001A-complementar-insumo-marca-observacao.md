# UC001A — Complementar cadastro de insumo com marca e observação

- **Status:** Pronto para implementação
- **Tipo:** ajuste de requisito do UC001 já implementado
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001 implementado
- **Próximo caso relacionado:** UC002 — Listar e consultar insumos

## Motivo da alteração

O UC001 representou o insumo apenas por nome, categoria, unidade base e situação. O uso real mostrou que a **marca** precisa fazer parte da identidade econômica do insumo, pois marcas diferentes do mesmo item possuem preços diferentes e, consequentemente, custos diferentes.

Também é necessário registrar uma **observação técnica global do insumo**, por exemplo força W de uma farinha, características de embalagem ou outra informação útil para decisão de compra e uso.

Este ajuste preserva o UC001 como registro histórico do primeiro incremento e formaliza a evolução do modelo antes do UC002.

## Objetivo

Evoluir o cadastro de insumo para suportar:

- marca opcional;
- observação técnica opcional;
- unicidade por combinação de nome + marca;
- preparação do modelo para que cada variação de marca possua histórico de preços independente no UC005.

## Modelo conceitual após o ajuste

```text
Insumo
- Id: int
- Nome: string
- NomeNormalizado: string
- Marca: string?
- MarcaNormalizada: string
- Observacao: string?
- Categoria: CategoriaInsumo
- UnidadeBase: UnidadeMedida
- Ativo: bool
```

## Nome

Permanece sendo o nome genérico do item, sem incorporar a marca.

Exemplo recomendado:

```text
Nome = "Farinha de Trigo Branca"
Marca = "Renata Super Premium"
```

Evitar cadastrar:

```text
Nome = "Farinha de Trigo Branca Renata Super Premium"
Marca = null
```

quando a informação de marca for conhecida e relevante.

As regras existentes de normalização do Nome continuam válidas.

## Marca

- opcional;
- máximo de 80 caracteres após normalização de espaços;
- espaços externos são removidos;
- sequências internas de whitespace são reduzidas a um único espaço;
- capitalização informada pelo usuário é preservada para exibição;
- para comparação, manter `MarcaNormalizada` em maiúsculas com regra invariável;
- a normalização não remove acentos;
- marca vazia ou composta apenas por espaços é tratada como ausência de marca.

Para permitir unicidade determinística no SQLite, `MarcaNormalizada` deve ser persistida como string não nula. Quando não houver marca, usar string vazia como representação técnica normalizada.

## Observação do insumo

A observação é uma anotação técnica global do cadastro do insumo.

Exemplos:

```text
W 300
Proteína 13,5%
Pacote com fechamento zip
```

Regras:

- opcional;
- máximo de 1000 caracteres;
- espaços externos são removidos;
- conteúdo interno e quebras de linha são preservados;
- texto vazio ou composto apenas por espaços é armazenado como `null`;
- não participa da identidade nem da unicidade do insumo.

## Observação na ficha técnica

A observação global do insumo **não substitui** uma justificativa específica da receita.

Quando o UC014 for detalhado, cada item da ficha técnica poderá possuir uma observação contextual própria, por exemplo:

```text
Escolhi a Renata Super Premium porque a força da farinha é adequada para esta fermentação longa.
```

Essa observação da ficha será independente da observação global do insumo e permanecerá associada ao item da receita. Alterações futuras na observação global do insumo não deverão sobrescrever a justificativa registrada na ficha técnica.

Este comportamento é apenas documentado neste ajuste e **não deve ser implementado agora**.

## Unicidade

A regra anterior de unicidade somente por `NomeNormalizado` é substituída por unicidade da combinação:

```text
NomeNormalizado + MarcaNormalizada
```

Exemplos:

```text
Farinha de Trigo Branca / Renata
Farinha de Trigo Branca / Caputo
```

são insumos distintos e permitidos.

Já:

```text
Farinha de Trigo Branca / Renata
  FARINHA   DE TRIGO BRANCA /  RENATA  
```

representam o mesmo insumo e não podem coexistir.

Dois registros sem marca e com o mesmo nome também são duplicados.

A unicidade continua incluindo registros inativos.

## Impacto no histórico de preços

Não criar entidade/tabela de preço neste ajuste.

Quando o UC005 for implementado, cada registro de preço pertencerá a um `InsumoId`. Como cada combinação Nome + Marca é um insumo distinto, seus históricos de preço e custos permanecerão naturalmente separados.

Exemplo:

```text
Farinha de Trigo Branca / Renata -> histórico próprio
Farinha de Trigo Branca / Caputo -> histórico próprio
```

## Interface

Atualizar `/Insumos/Novo` adicionando:

- Marca — input texto opcional;
- Observação — textarea opcional.

Ordem sugerida:

1. Nome;
2. Marca;
3. Categoria;
4. Unidade base;
5. Observação.

O fluxo Post/Redirect/Get e a mensagem de sucesso do UC001 permanecem inalterados.

### Mensagem de duplicidade

Usar:

```text
Já existe um insumo cadastrado com esse nome e marca.
```

A mensagem deve ser associada preferencialmente ao conjunto Nome/Marca ou apresentada de forma clara no formulário.

## Persistência

Criar uma nova migration, sem modificar a migration `CreateInsumos` já aplicada.

A migration deve:

1. remover o índice único antigo de `NomeNormalizado`;
2. adicionar `Marca`, nullable, máximo 80;
3. adicionar `MarcaNormalizada`, obrigatório, máximo 80, com valor padrão `""` para registros existentes;
4. adicionar `Observacao`, nullable, máximo 1000;
5. criar índice único composto em `NomeNormalizado` + `MarcaNormalizada`.

### Compatibilidade com dados existentes

Registros criados antes desta alteração devem permanecer válidos e sem perda de dados:

```text
Marca = null
MarcaNormalizada = ""
Observacao = null
```

A migration **não deve tentar inferir marca a partir do Nome existente**. Um registro como `Farinha Renata Premium` permanece exatamente assim até eventual edição futura pelo UC003 ou correção manual durante desenvolvimento.

## Regras de negócio aplicáveis

- RN001 — Unidade base;
- RN007 — Insumo sem preço;
- RN008 — Desativação de insumo;
- RN028 — Nome do insumo;
- RN029 — Unicidade do insumo por nome e marca;
- RN030 — Categoria do insumo;
- RN031 — Situação inicial do insumo;
- RN032 — Marca do insumo;
- RN033 — Observação do insumo.

## Critérios de aceitação

### CA01 — Cadastro com marca

**Quando** o usuário cadastrar `Farinha de Trigo Branca`, marca `Renata Super Premium`, categoria Ingrediente e unidade `g`  
**Então** o insumo deve ser persistido com Nome e Marca separados  
**E** deve nascer ativo.

### CA02 — Marca opcional

**Quando** o usuário cadastrar um insumo sem marca  
**Então** o cadastro deve ser válido  
**E** `Marca` deve ser nula  
**E** `MarcaNormalizada` deve ser `""`.

### CA03 — Normalização da marca

**Quando** a marca for informada como `  Renata   Super Premium `  
**Então** deve ser armazenada como `Renata Super Premium`  
**E** sua representação normalizada deve ser `RENATA SUPER PREMIUM`.

### CA04 — Mesma descrição, marcas diferentes

**Dado** `Farinha de Trigo Branca / Renata` já cadastrada  
**Quando** for cadastrada `Farinha de Trigo Branca / Caputo`  
**Então** o novo cadastro deve ser permitido.

### CA05 — Mesma descrição e mesma marca

**Dado** `Farinha de Trigo Branca / Renata` já cadastrada  
**Quando** o usuário tentar cadastrar a mesma combinação variando apenas caixa e espaços  
**Então** nenhum novo registro deve ser criado  
**E** deve ser exibida a mensagem de duplicidade.

### CA06 — Duplicidade sem marca

**Dado** `Sal Refinado` sem marca já cadastrado  
**Quando** outro `Sal Refinado` sem marca for cadastrado  
**Então** o banco e o fluxo funcional devem rejeitar a duplicidade.

### CA07 — Observação

**Quando** uma observação de até 1000 caracteres for informada  
**Então** ela deve ser persistida e recuperada preservando o conteúdo interno.

### CA08 — Observação vazia

**Quando** a observação contiver somente espaços  
**Então** deve ser armazenada como `null`.

### CA09 — Migration evolutiva

**Dado** um banco já migrado até `CreateInsumos` contendo registros  
**Quando** a nova migration for aplicada  
**Então** os registros existentes devem ser preservados  
**E** os novos campos devem receber os valores compatíveis definidos neste documento  
**E** o novo índice composto deve existir.

### CA10 — Sem escopo antecipado

**Ao revisar o diff**  
**Então** não deve existir implementação de listagem/consulta do UC002, edição do UC003, preço do UC005 ou ficha técnica do UC014.

## Testes esperados

### Unitários

Cobrir no mínimo:

- criação com marca válida;
- criação sem marca;
- normalização de espaços da marca;
- geração de `MarcaNormalizada`;
- marca acima de 80 caracteres rejeitada;
- observação opcional;
- observação vazia convertida em `null`;
- observação acima de 1000 caracteres rejeitada.

### Integração — persistência

Cobrir no mínimo:

- migration aplicada em banco vazio;
- upgrade de banco existente com dados da migration anterior;
- preservação dos registros existentes;
- índice composto permite mesmo Nome com marcas diferentes;
- índice composto rejeita mesmo Nome + mesma Marca;
- índice composto rejeita duplicidade de dois insumos sem marca;
- Marca e Observação são persistidas e recuperadas corretamente.

### Integração — web

Cobrir no mínimo:

- formulário apresenta Marca e Observação;
- POST válido com marca persiste;
- POST válido sem marca persiste;
- mesma descrição com marcas diferentes é aceita;
- duplicidade por Nome + Marca apresenta mensagem funcional;
- marca/observação inválidas não persistem.

## Fora do escopo

- listar/consultar insumos — UC002;
- editar insumo — UC003;
- desativar/reativar — UC004;
- preço e histórico — UC005/UC006;
- observação por item de ficha técnica — UC014;
- fornecedor;
- estoque;
- inferência automática de marca a partir de nomes existentes.

## Definition of Done específica

Além da DoD global:

- modelo `Insumo` contém Marca, MarcaNormalizada e Observacao;
- unicidade passa a ser composta por NomeNormalizado + MarcaNormalizada;
- cadastro web contém os novos campos;
- migration evolutiva preserva bancos existentes;
- testes cobrem marca, observação e upgrade de migration;
- UC002 ou UCs posteriores não são antecipados.
