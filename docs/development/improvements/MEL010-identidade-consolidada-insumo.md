# MEL010 — Persistir identidade consolidada do Insumo

- **Tipo:** melhoria técnica / consolidação de identidade
- **Origem:** decisão pré-UC016
- **Dependências:** UC005, UC014 e UC015 implementados
- **Momento obrigatório:** antes da especificação/implementação do UC016
- **Impacta diretamente:** RN040, RN048 e edição de Insumo
- **Alteração de schema:** sim
- **Não implementa:** remoção de Item, UC016, histórico de composição ou exclusão de Insumo

## Objetivo

Persistir no próprio Insumo o fato de que sua identidade cadastral já foi consolidada por uso econômico ou produtivo.

Depois que um Insumo participar pela primeira vez de:

1. um registro de preço; ou
2. uma Ficha Técnica;

os campos Nome, Marca e Unidade base tornam-se permanentemente imutáveis.

Remover referências futuras não reabre esses campos.

A regra é monotônica:

~~~text
IdentidadeConsolidada
false -> true
~~~

Nunca existe transição true -> false.

## Motivação

Até a UC015, a aplicação protege a identidade dinamicamente consultando:

~~~text
possui PrecoInsumo
OU
possui ItemFichaTecnica atual
~~~

Isso funciona enquanto ItemFichaTecnica não pode ser removido.

Com a futura UC016, remover a última referência atual de Ficha faria a consulta deixar de encontrar Item. Se o Insumo também não possuísse preço, Nome/Marca/Unidade voltariam a parecer editáveis.

Essa interpretação foi rejeitada.

A decisão de negócio é:

> O primeiro uso econômico ou produtivo consolida permanentemente a identidade do Insumo.

O fato histórico de ter sido usado não desaparece porque uma composição atual foi alterada.

## Princípio KISS

Não criar:

- histórico separado de consolidação;
- tabela de eventos;
- contador de referências;
- enum de motivos;
- lógica de última Ficha;
- lógica de desbloqueio;
- snapshot cadastral no Item;
- cálculo permanente derivado de consultas históricas.

Persistir somente:

~~~text
Insumo.IdentidadeConsolidada : bool
~~~

## Modelo de domínio

Adicionar ao Insumo:

~~~text
IdentidadeConsolidada: bool
~~~

Novo Insumo nasce com false.

Adicionar comportamento explícito:

~~~csharp
insumo.ConsolidarIdentidade()
~~~

Comportamento:

- define IdentidadeConsolidada = true;
- é idempotente;
- não altera Nome, Marca, Unidade, Categoria, Observacao ou Ativo;
- não possui operação inversa.

## Edição do Insumo

A proteção deve existir no domínio, não apenas na página Web.

Quando IdentidadeConsolidada = false:

- Nome editável;
- Marca editável;
- Unidade base editável;
- Categoria editável;
- Observacao editável.

Quando IdentidadeConsolidada = true:

- Nome imutável;
- Marca imutável;
- Unidade base imutável;
- Categoria editável;
- Observacao editável.

AtualizarDados deve continuar permitindo Categoria/Observacao quando Nome/Marca/Unidade permanecem iguais.

Se tentar alterar efetivamente Nome, Marca ou Unidade, rejeitar.

Mensagem única:

~~~text
Nome, marca e unidade base não podem ser alterados porque a identidade deste insumo já foi consolidada.
~~~

## Eventos que consolidam

### Primeiro registro de preço

Antes de persistir um novo PrecoInsumo válido:

~~~text
insumo.ConsolidarIdentidade()
~~~

Preço e consolidação devem ser persistidos no mesmo SaveChanges/transação.

Se o SaveChanges falhar, nenhum dos dois deve ser persistido.

Preço futuro também consolida.

### Primeiro Item de Ficha Técnica

Antes de persistir um novo ItemFichaTecnica válido:

~~~text
insumo.ConsolidarIdentidade()
~~~

Item e consolidação devem ser persistidos no mesmo SaveChanges/transação.

Se o SaveChanges falhar, nenhum dos dois deve ser persistido.

## Onde consolidar

Manter solução explícita nos dois fluxos atuais:

- /Insumos/Precos/Novo;
- /Produtos/FichaTecnica/{produtoId}/Itens/Novo.

Não criar infraestrutura genérica de eventos de domínio apenas para esta regra.

### Fluxo de preço

O POST já carrega Insumo rastreado.

Depois das validações e antes do SaveChanges:

1. criar/adicionar PrecoInsumo;
2. chamar ConsolidarIdentidade;
3. SaveChanges.

### Fluxo de Item

O POST atual carrega o Insumo com AsNoTracking.

No POST válido, carregar o Insumo selecionado como entidade rastreada.

Depois das validações de ativo, tenant, duplicidade e quantidade:

1. adicionar ItemFichaTecnica;
2. chamar ConsolidarIdentidade;
3. SaveChanges.

GET/listagem não precisa mudar por causa disso.

## Persistência

Adicionar coluna:

~~~text
Insumos.IdentidadeConsolidada
- bool
- required
~~~

Configuração explícita em InsumoConfiguration.

## Migration e backfill

Criar migration evolutiva:

~~~text
AddInsumoIdentidadeConsolidada
~~~

ou nome equivalente.

Não basta adicionar a coluna com false.

Depois de adicionar a coluna, fazer backfill:

~~~text
IdentidadeConsolidada = true
quando existir:
    qualquer PrecoInsumo do Insumo
    OU
    qualquer ItemFichaTecnica do Insumo
~~~

Insumos sem qualquer desses fatos permanecem false.

SQL de referência:

~~~sql
UPDATE Insumos
SET IdentidadeConsolidada = 1
WHERE EXISTS (
    SELECT 1
    FROM PrecosInsumos p
    WHERE p.InsumoId = Insumos.Id
)
OR EXISTS (
    SELECT 1
    FROM ItensFichaTecnica i
    WHERE i.InsumoId = Insumos.Id
);
~~~

A SQL real deve ser compatível com SQLite.

Não editar migrations históricas.

## Edição Web de Insumo

A página /Insumos/Editar/{id} deixa de derivar a proteção consultando PrecosInsumos e ItensFichaTecnica a cada request.

Substituir:

~~~text
PossuiHistoricoPreco
ReferenciadoEmFicha
IdentidadeProtegida = PossuiHistoricoPreco || ReferenciadoEmFicha
~~~

por:

~~~text
IdentidadeProtegida = Insumo.IdentidadeConsolidada
~~~

GET carrega IdentidadeConsolidada.

Quando true:

- Nome readonly;
- Marca readonly;
- Unidade base disabled/readonly;
- Categoria editável;
- Observacao editável.

POST rebusca Insumo rastreado e o domínio continua sendo a barreira final.

Usar a mensagem única definida acima.

Não consultar preço/Item apenas para descobrir o motivo da consolidação.

## Relação com RN040 e RN048

RN040 e RN048 continuam como gatilhos de negócio:

~~~text
RN040:
primeiro preço -> consolidar identidade permanentemente

RN048:
primeiro uso em Ficha -> consolidar identidade permanentemente
~~~

RN051 centraliza o estado resultante:

~~~text
IdentidadeConsolidada = true
~~~

A proteção deixa de depender da existência atual das referências.

## Relação com UC016

UC016 poderá remover ItemFichaTecnica sem lógica de desbloqueio.

Depois da remoção:

~~~text
Insumo.IdentidadeConsolidada
permanece true
~~~

Mesmo que:

- fosse a última Ficha;
- não exista preço;
- não reste Item algum.

UC016 não deve:

- contar referências;
- consultar preço para decidir desbloqueio;
- alterar IdentidadeConsolidada;
- reabrir Nome/Marca/Unidade.

## Multiempresa

IdentidadeConsolidada pertence ao próprio Insumo tenant-owned.

Nenhum request define esse campo.

A consolidação só ocorre sobre Insumo resolvido no tenant ativo.

Backfill opera por InsumoId e não muda ownership.

## Critérios de aceitação

### CA01
Novo Insumo sem preço/Ficha nasce com IdentidadeConsolidada = false.

### CA02
ConsolidarIdentidade muda false para true, é idempotente e não existe API inversa.

### CA03
Insumo consolidado rejeita alteração real de Nome, Marca ou Unidade no domínio.

### CA04
Categoria e Observacao continuam editáveis com identidade inalterada.

### CA05
Registrar primeiro preço persiste também IdentidadeConsolidada = true.

### CA06
Adicionar primeiro Item persiste também IdentidadeConsolidada = true.

### CA07
Falha antes do commit não deixa consolidação persistida sem o fato correspondente.

### CA08
Migration marca true para Insumo existente com qualquer preço.

### CA09
Migration marca true para Insumo existente com qualquer Item.

### CA10
Migration mantém false para Insumo nunca usado.

### CA11
Edição Web usa o estado persistido, sem depender de consultas a preço/Item.

### CA12
UI usa a mensagem única de identidade consolidada.

### CA13
Após consolidar por Item e remover tecnicamente esse Item em teste de persistência, a flag continua true.

Esse teste antecipa somente a invariável, não implementa UC016.

### CA14
RN040 permanece protegida pela consolidação persistida.

### CA15
RN048 permanece protegida e passa a ser permanente.

## Matriz de testes

### Unitários — Insumo

- U1: novo Insumo nasce não consolidado;
- U2: ConsolidarIdentidade é monotônico/idempotente;
- U3: consolidado rejeita alteração real de Nome;
- U4: consolidado rejeita alteração real de Marca;
- U5: consolidado rejeita alteração de Unidade;
- U6: consolidado permite Categoria/Observacao com identidade inalterada.

### Persistência / migration

- P1: migration adiciona coluna e preserva Insumos;
- P2: backfill true para Insumo com preço;
- P3: backfill true para Insumo com Item;
- P4: backfill false para Insumo nunca usado;
- P5: round-trip preserva IdentidadeConsolidada;
- P6: remover diretamente último Item não muda flag.

### Web

- W1: primeiro preço consolida no mesmo SaveChanges;
- W2: primeiro Item consolida no mesmo SaveChanges;
- W3: edição de consolidado bloqueia identidade com mensagem única;
- W4: POST manipulado não muda Nome/Marca/Unidade;
- W5: Categoria/Observacao continuam editáveis;
- W6: Insumo não consolidado continua com identidade editável;
- W7: preço futuro também consolida;
- W8: adicionar Item de Insumo já consolidado continua funcionando;
- W9: testes existentes de RN040/RN048 são atualizados sem enfraquecer proteção.

## Fora do escopo

- UC016;
- remoção Web de Item;
- restauração/desbloqueio de identidade;
- motivo/data/usuário da consolidação;
- tabela de eventos;
- snapshots cadastrais;
- exclusão física de Insumo;
- custo/preço de Produto;
- refatorações genéricas de DbContext.

## Definition of Done específica

MEL010 está concluída quando:

- IdentidadeConsolidada existe no Insumo e nasce false;
- domínio possui transição monotônica para true;
- domínio protege Nome/Marca/Unidade;
- Categoria/Observacao continuam editáveis;
- primeiro preço consolida;
- primeiro Item consolida;
- fato + consolidação usam um único SaveChanges;
- migration faz backfill correto;
- edição Web usa a flag persistida;
- consultas dinâmicas de proteção deixam de ser necessárias;
- mensagem única está aplicada;
- RN040/RN048/RN051 estão coerentes;
- nenhuma lógica de UC016 foi implementada;
- matriz U1-U6, P1-P6 e W1-W9 atendida;
- build Release sem warnings novos relevantes;
- suíte completa verde.

## Branch sugerida

~~~text
feat/mel010-identidade-consolidada-insumo
~~~

## Commit sugerido

~~~text
feat: consolida identidade do insumo
~~~
