# UC001A — Complementar cadastro de insumo com marca e observação

- **Status:** Implementado
- **Tipo:** evolução funcional do cadastro de Insumo
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001, FT002 e UC001B implementados
- **Próximo caso relacionado:** UC002 — Listar e consultar insumos

## Motivo da alteração

O UC001 representou o insumo apenas por nome, categoria, unidade base e situação. O uso real mostrou que a **marca** precisa fazer parte da identidade econômica do insumo, pois marcas diferentes do mesmo item possuem preços diferentes e, consequentemente, custos diferentes.

Também é necessário registrar uma **observação técnica global do insumo**, por exemplo força W de uma farinha, gramatura/característica de um material ou outra informação útil para decisão de compra e uso.

Desde a especificação original deste UC, o Precificador passou a ser multiempresa pela FT002 e teve categoria/unidades generalizadas pelo UC001B. Esta versão revalidada incorpora essas fundações antes da implementação.

## Objetivo

Evoluir o cadastro de Insumo para suportar:

- marca opcional;
- observação técnica opcional;
- unicidade por Empresa + Nome + Marca;
- preparação do modelo para que cada variação de marca possua histórico de preços independente no UC005;
- preservação integral do isolamento tenant-aware introduzido pela FT002.

## Modelo conceitual após o ajuste

```text
Insumo : IEntidadeEmpresa
- Id: int
- EmpresaId: int
- Nome: string
- NomeNormalizado: string
- Marca: string?
- MarcaNormalizada: string
- Observacao: string?
- Categoria: CategoriaInsumo
- UnidadeBase: UnidadeMedida
- Ativo: bool
```

`EmpresaId` permanece obrigatório e é resolvido pelo servidor a partir da Empresa Ativa. Não deve ser exposto nem aceito como campo editável do formulário.

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
Papel 180 g/m²
Pacote com fechamento zip
```

Regras:

- opcional;
- máximo de 1000 caracteres;
- whitespace externo é removido;
- conteúdo interno e quebras de linha são preservados;
- texto vazio ou composto apenas por whitespace é armazenado como `null`;
- não participa da identidade nem da unicidade do Insumo.

## Observação na ficha técnica

A observação global do Insumo **não substitui** uma justificativa específica da ficha técnica.

Quando o UC014 for detalhado, cada item da ficha técnica poderá possuir uma observação contextual própria, por exemplo:

```text
Escolhi a Renata Super Premium porque a força da farinha é adequada para esta fermentação longa.
```

Essa observação da ficha será independente da observação global do Insumo e permanecerá associada ao item. Alterações futuras na observação global do Insumo não deverão sobrescrever a justificativa registrada na ficha técnica.

Este comportamento é apenas documentado neste ajuste e **não deve ser implementado agora**.

## Unicidade tenant-aware

Após a FT002, a unicidade atual é:

```text
EmpresaId + NomeNormalizado
```

O UC001A substitui essa regra por:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

Exemplos dentro da mesma Empresa:

```text
Farinha de Trigo Branca / Renata
Farinha de Trigo Branca / Caputo
```

são Insumos distintos e permitidos.

Já:

```text
Farinha de Trigo Branca / Renata
  FARINHA   DE TRIGO BRANCA /  RENATA  
```

representam o mesmo Insumo na mesma Empresa e não podem coexistir.

Dois registros sem marca e com o mesmo nome também são duplicados **quando pertencem à mesma Empresa**.

Empresas diferentes podem possuir a mesma combinação de Nome + Marca.

A unicidade continua incluindo registros ativos e inativos.

A validação funcional da Web deve operar somente no contexto da Empresa Ativa e apresentar mensagem amigável, mas a integridade final deve permanecer garantida pelo índice único no banco.

## Impacto no histórico de preços

Não criar entidade/tabela de preço neste ajuste.

Quando o UC005 for implementado, cada registro de preço pertencerá a um `InsumoId`. Como cada combinação Empresa + Nome + Marca identifica um Insumo específico, históricos de preço e custos permanecerão naturalmente isolados por Empresa.

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

A categoria já utiliza o vocabulário do UC001B:

- Matéria-prima;
- Embalagem;
- Consumível.

As unidades já disponíveis são `g`, `ml`, `m` e `un`.

O fluxo Post/Redirect/Get, antiforgery, autenticação e exigência de Empresa Ativa permanecem inalterados.

O formulário **não** deve conter `EmpresaId`.

### Mensagem de duplicidade

Usar:

```text
Já existe um insumo cadastrado com esse nome e marca.
```

A mensagem deve ser associada preferencialmente ao conjunto Nome/Marca ou apresentada de forma clara no formulário.

Não transformar qualquer `DbUpdateException` genericamente em duplicidade; somente a violação da restrição esperada deve gerar essa mensagem funcional.

## Persistência

Criar uma nova migration evolutiva sem modificar migrations históricas.

Como o UC001B não alterou schema, o estado persistido imediatamente anterior continua sendo o produzido pela FT002 (`AddMultiempresaIdentity`).

A migration do UC001A deve:

1. remover o índice único atual `(EmpresaId, NomeNormalizado)`;
2. adicionar `Marca`, nullable, máximo 80;
3. adicionar `MarcaNormalizada`, obrigatória, máximo 80, com valor padrão `""` para registros existentes;
4. adicionar `Observacao`, nullable, máximo 1000;
5. criar índice único composto em `(EmpresaId, NomeNormalizado, MarcaNormalizada)`.

A FK `Insumo.EmpresaId -> Empresa.Id`, o Global Query Filter e o guard tenant-aware da FT002 devem permanecer válidos.

### Compatibilidade com dados existentes

Registros criados antes desta alteração devem permanecer válidos e sem perda de dados:

```text
EmpresaId = valor já existente
Marca = null
MarcaNormalizada = ""
Observacao = null
```

A migration **não deve tentar inferir marca a partir do Nome existente**.

Um registro como `Farinha Renata Premium` permanece exatamente assim até eventual edição futura pelo UC003 ou correção manual durante desenvolvimento.

## Regras de negócio aplicáveis

- RN001 — Unidade base;
- RN007 — Insumo sem preço;
- RN008 — Desativação de insumo;
- RN028 — Nome do insumo;
- RN029 — Unicidade do insumo por Empresa, Nome e Marca;
- RN030 — Categoria do insumo;
- RN031 — Situação inicial do insumo;
- RN032 — Marca do insumo;
- RN033 — Observação do insumo;
- RN035 — Propriedade por empresa;
- RN036 — Isolamento de empresa;
- RN037 — Empresa ativa.

## Critérios de aceitação

### CA01 — Cadastro com marca

**Quando** o usuário, em uma Empresa Ativa válida, cadastrar `Farinha de Trigo Branca`, marca `Renata Super Premium`, categoria Matéria-prima e unidade `g`  
**Então** o Insumo deve ser persistido com Nome e Marca separados  
**E** deve pertencer à Empresa Ativa  
**E** deve nascer ativo.

### CA02 — Marca opcional

**Quando** o usuário cadastrar um Insumo sem marca  
**Então** o cadastro deve ser válido  
**E** `Marca` deve ser nula  
**E** `MarcaNormalizada` deve ser `""`.

### CA03 — Normalização da marca

**Quando** a marca for informada como `  Renata   Super Premium `  
**Então** deve ser armazenada como `Renata Super Premium`  
**E** sua representação normalizada deve ser `RENATA SUPER PREMIUM`.

### CA04 — Mesmo nome, marcas diferentes na mesma Empresa

**Dado** `Farinha de Trigo Branca / Renata` já cadastrada na Empresa A  
**Quando** for cadastrada `Farinha de Trigo Branca / Caputo` na Empresa A  
**Então** o novo cadastro deve ser permitido.

### CA05 — Mesmo nome e mesma marca na mesma Empresa

**Dado** `Farinha de Trigo Branca / Renata` já cadastrada na Empresa A  
**Quando** o usuário tentar cadastrar a mesma combinação variando apenas caixa e espaços na Empresa A  
**Então** nenhum novo registro deve ser criado  
**E** deve ser exibida a mensagem de duplicidade.

### CA06 — Mesma combinação em Empresas diferentes

**Dado** `Farinha de Trigo Branca / Renata` cadastrada na Empresa A  
**Quando** a mesma combinação for cadastrada na Empresa B  
**Então** o cadastro deve ser permitido  
**E** cada registro deve permanecer visível somente na respectiva Empresa Ativa.

### CA07 — Duplicidade sem marca

**Dado** `Sal Refinado` sem marca já cadastrado na Empresa A  
**Quando** outro `Sal Refinado` sem marca for cadastrado na Empresa A  
**Então** o banco e o fluxo funcional devem rejeitar a duplicidade.

A mesma combinação sem marca continua permitida em Empresa diferente.

### CA08 — Observação

**Quando** uma observação de até 1000 caracteres for informada  
**Então** ela deve ser persistida e recuperada preservando o conteúdo interno e as quebras de linha.

### CA09 — Observação vazia

**Quando** a observação contiver somente whitespace  
**Então** deve ser armazenada como `null`.

### CA10 — Migration evolutiva

**Dado** um banco já no estado persistido da FT002/UC001B contendo Insumos de uma ou mais Empresas  
**Quando** a migration do UC001A for aplicada  
**Então** os registros existentes devem ser preservados  
**E** seus `EmpresaId` devem ser preservados  
**E** os novos campos devem receber os valores compatíveis definidos neste documento  
**E** o índice único deve passar a ser `(EmpresaId, NomeNormalizado, MarcaNormalizada)`.

### CA11 — Isolamento preservado

**Ao cadastrar ou consultar dados durante os testes deste UC**  
**Então** Global Query Filter, guard de escrita e política de Empresa Ativa introduzidos pela FT002 devem continuar protegendo o tenant  
**E** `EmpresaId` não deve ser enviado pelo formulário.

### CA12 — Sem escopo antecipado

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
- observação whitespace-only convertida em `null`;
- observação acima de 1000 caracteres rejeitada;
- `EmpresaId` recebido pelo domínio permanece associado ao Insumo;
- categorias/unidades do UC001B continuam válidas.

### Integração — persistência

Cobrir no mínimo:

- migration aplicada em banco vazio;
- upgrade de banco no estado anterior com registros existentes;
- preservação de `EmpresaId` e demais dados existentes;
- novos registros antigos recebem `Marca = null`, `MarcaNormalizada = ""` e `Observacao = null` quando aplicável;
- índice composto permite mesmo Nome com marcas diferentes na mesma Empresa;
- índice composto rejeita mesmo Nome + mesma Marca na mesma Empresa;
- índice composto permite mesma combinação Nome + Marca em Empresas diferentes;
- índice composto rejeita duplicidade de dois Insumos sem marca na mesma Empresa;
- mesma combinação sem marca é permitida em Empresas diferentes;
- Marca e Observação são persistidas e recuperadas corretamente;
- query filter e guard tenant-aware continuam funcionando após a migration.

### Integração — Web

Cobrir no mínimo:

- formulário apresenta Marca e Observação e não apresenta `EmpresaId`;
- POST válido com marca persiste na Empresa Ativa;
- POST válido sem marca persiste na Empresa Ativa;
- mesma descrição com marcas diferentes é aceita;
- duplicidade por Nome + Marca na mesma Empresa apresenta mensagem funcional;
- mesma combinação em Empresa diferente é permitida e isolada;
- marca/observação inválidas não persistem;
- autenticação + Empresa Ativa continuam obrigatórias.

Os testes devem usar SQLite real/in-memory com conexão mantida e nunca o banco real do usuário.

## Fora do escopo

- listar/consultar Insumos — UC002;
- editar Insumo — UC003;
- desativar/reativar — UC004;
- preço e histórico — UC005/UC006;
- observação por item de ficha técnica — UC014;
- fornecedor;
- estoque;
- inferência automática de marca a partir de nomes existentes;
- alterações de autenticação, seleção de Empresa ou estratégia de tenancy que não sejam necessárias para corrigir regressão causada por este UC.

## Definition of Done específica

Além da DoD global:

- modelo `Insumo` contém `Marca`, `MarcaNormalizada` e `Observacao`;
- `EmpresaId` e `IEntidadeEmpresa` permanecem preservados;
- unicidade passa a ser `(EmpresaId, NomeNormalizado, MarcaNormalizada)`;
- cadastro Web contém os novos campos e não expõe `EmpresaId`;
- migration evolutiva preserva bancos existentes e o isolamento multiempresa;
- testes cobrem marca, observação, upgrade de migration e comportamento cross-tenant da unicidade;
- build Release permanece sem warnings novos relevantes;
- suíte completa permanece verde;
- UC002 ou UCs posteriores não são antecipados.
