# UC034 — Vincular Produtos a Coleções

- **Funcionalidade:** F002 — Gestão de Produtos
- **Dependências funcionais:** UC032, UC033
- **Estado:** Pronto
- **Alteração de domínio:** sim
- **Alteração de schema:** sim
- **Migration:** sim
- **Autorização:** EmpresaAtiva
- **Regras principais:** RN080 a RN083

## Objetivo

Permitir que Produtos da Empresa Ativa participem de uma ou mais Coleções comerciais, registrando também se o Produto é destaque naquela Coleção.

A UC034 deve permitir:

- vincular Produto a Coleção;
- desvincular Produto de Coleção;
- marcar/desmarcar Produto como destaque em uma Coleção;
- consultar os Produtos vinculados a uma Coleção;
- consultar, no detalhe do Produto, as Coleções às quais ele pertence;
- filtrar a listagem de Produtos por Coleção;
- preservar isolamento tenant-aware;
- preservar Produtos, Coleções e precificação ao remover apenas o vínculo.

## Decisão de cardinalidade

O relacionamento é N:N:

~~~text
Produto 1..N <-> 1..N Coleção
~~~

Um Produto pode participar de várias Coleções, inclusive com períodos sobrepostos.

Uma Coleção pode possuir vários Produtos.

Não adicionar:

~~~text
ColecaoProdutoId
~~~

em Produto.

Criar entidade associativa própria:

~~~text
ProdutoColecao : IEntidadeEmpresa
- EmpresaId : int
- ProdutoId : int
- ColecaoProdutoId : int
- Destaque : bool
~~~

## Vigência do vínculo

A UC034 **não cria datas próprias no vínculo Produto-Coleção**.

A participação do Produto herda integralmente o período comercial da Coleção:

~~~text
Início efetivo = ColecaoProduto.DataLancamento
Fim efetivo    = ColecaoProduto.DataFinalizacao
~~~

Se a Coleção estiver:

- Planejada -> a participação também é futura;
- Em andamento -> a participação está vigente;
- Finalizada -> a participação é histórica.

Se DataFinalizacao for null, a participação permanece sem fim comercial definido.

Consequência intencional:

- editar as datas da Coleção altera o período efetivo de todos os Produtos vinculados;
- não existem duas fontes de vigência concorrentes;
- não existem DataInicioParticipacao/DataFimParticipacao no vínculo.

Uma necessidade futura de participação parcial dentro da Coleção deve ser especificada separadamente.

## Identidade do vínculo

A identidade funcional é:

~~~text
EmpresaId + ProdutoId + ColecaoProdutoId
~~~

Como ProdutoId e ColecaoProdutoId já são chaves globais, a chave primária pode ser:

~~~text
(ProdutoId, ColecaoProdutoId)
~~~

mantendo EmpresaId explícito para GQF/guard tenant.

O mesmo Produto não pode possuir duas linhas para a mesma Coleção.

## Destaque

~~~text
Destaque : bool
~~~

pertence ao vínculo Produto-Coleção.

Semântica:

~~~text
false -> Produto participa normalmente
true  -> Produto é destaque daquela Coleção
~~~

Destaque:

- não é exclusivo;
- não possui limite máximo por Coleção;
- não possui limite máximo por Produto;
- pode existir em Coleções sobrepostas;
- não altera precificação;
- não altera margem;
- não altera custo;
- não altera Categoria;
- não altera Produto.Ativo.

O estado inicial de um novo vínculo é:

~~~text
Destaque = false
~~~

salvo escolha explícita do usuário ao vincular.

## Produto ativo/inativo

Produtos ativos e inativos podem ser vinculados.

Motivação:

- Coleções podem representar histórico;
- um Produto hoje inativo pode ter participado de Coleção passada;
- a UC034 também deve permitir correções administrativas retroativas.

A interface deve indicar Produto inativo, mas não bloquear o vínculo.

Desativar Produto pela UC010 não remove nem altera seus vínculos com Coleções.

Reativar Produto também não altera vínculos.

## Coleção Planejada/Em andamento/Finalizada

É permitido vincular/desvincular e corrigir destaque em Coleção:

- Planejada;
- Em andamento;
- Finalizada.

Isso segue a decisão da UC033 de permitir correções administrativas de Coleções históricas.

A UC034 não introduz congelamento histórico nem auditoria imutável.

## Relação com Categorias

As Categorias cadastradas na Coleção pela UC033 continuam sendo metadados organizacionais.

Portanto é permitido vincular:

- Produto sem Categoria;
- Produto cuja Categoria não esteja na Coleção;
- Produto cuja Categoria esteja inativa;
- Produto a Coleção sem Categorias.

Não exibir erro nem impedir vínculo por incompatibilidade de Categoria.

Alterar Categoria do Produto não altera ProdutoColecao.

Adicionar/remover Categoria da Coleção não altera ProdutoColecao.

## Desvincular

Desvincular remove somente a associação:

~~~text
ProdutoColecao
~~~

É permitido DELETE físico da **linha associativa**, pois ela não é entidade comercial autônoma e a ação representa correção do pertencimento.

Não remover:

- Produto;
- Coleção;
- Categoria;
- Ficha Técnica;
- preço;
- histórico de precificação.

A UC034 não cria auditoria/histórico das alterações do vínculo.

Caso seja necessário no futuro preservar histórico de entradas/saídas ou múltiplos períodos dentro da mesma Coleção, isso exige evolução separada do modelo.

## Persistência

Criar tabela:

~~~text
ProdutosColecoes
~~~

Campos:

~~~text
EmpresaId int NOT NULL
ProdutoId int NOT NULL
ColecaoProdutoId int NOT NULL
Destaque bit NOT NULL
~~~

Chave:

~~~text
PRIMARY KEY (ProdutoId, ColecaoProdutoId)
~~~

Índices:

~~~text
INDEX (EmpresaId, ColecaoProdutoId, Destaque)
INDEX (EmpresaId, ProdutoId)
~~~

FKs:

~~~text
EmpresaId -> Empresas.Id RESTRICT
ProdutoId -> Produtos.Id RESTRICT
ColecaoProdutoId -> ColecoesProdutos.Id RESTRICT
~~~

Não usar cascade delete.

## Entidade ProdutoColecao

Estrutura conceitual:

~~~csharp
public sealed class ProdutoColecao : IEntidadeEmpresa
{
    public int EmpresaId { get; private set; }
    public int ProdutoId { get; private set; }
    public int ColecaoProdutoId { get; private set; }
    public bool Destaque { get; private set; }
}
~~~

Métodos conceituais:

~~~text
ProdutoColecao.Criar(
    empresaId,
    produtoId,
    colecaoProdutoId,
    destaque)

DefinirDestaque(bool destaque)
DefinirEmpresa(int empresaId)
~~~

Validar ids positivos.

EmpresaId não pode ser reatribuído.

## Integridade tenant-aware

Adicionar ProdutoColecao ao padrão tenant-aware:

- DbSet;
- Global Query Filter;
- guard central via IEntidadeEmpresa.

Para qualquer linha:

~~~text
Produto.EmpresaId
=
ColecaoProduto.EmpresaId
=
ProdutoColecao.EmpresaId
~~~

O guard deve validar ownership real com IgnoreQueryFilters, inclusive para objeto fabricado fora da Web.

Request nunca controla EmpresaId.

## Concorrência

Dois POSTs simultâneos para vincular o mesmo Produto à mesma Coleção não podem gerar:

- HTTP 500;
- duas linhas;
- estado parcial.

A PK composta é a autoridade final.

O fluxo deve fazer pre-check amigável quando conveniente e também tratar violation esperada de PK/índice em corrida.

Resultado esperado:

~~~text
1 vínculo persistido
1 operação bem-sucedida
outra operação controlada como “já vinculado”
~~~

Não mascarar outros DbUpdateException.

## Migration

Criar migration evolutiva após UC033.

Não rebaselinear.

Não editar migrations históricas.

A migration deve:

1. criar ProdutosColecoes;
2. criar PK;
3. criar índices;
4. criar FKs Restrict;
5. atualizar ModelSnapshot.

Não há backfill.

Produtos e Coleções existentes permanecem sem vínculos até ação explícita.

Não alterar schema de:

- Produtos;
- ColecoesProdutos;
- CategoriasProdutos;
- RegistrosPrecosProdutos.

### Testes de migration

Validar:

- clean DB;
- upgrade pós-UC033;
- tabela/PK/índices/FKs;
- Destaque bit NOT NULL;
- ausência de cascade;
- dados existentes preservados;
- nenhum campo novo em Produtos;
- zero migrations pendentes.

## Superfície Web — gerenciamento por Coleção

Adicionar em Coleções a ação:

~~~text
Gerenciar produtos
~~~

Criar página:

~~~text
GET  /Produtos/Colecoes/Produtos/{id:int}
POST /Produtos/Colecoes/Produtos/{id:int}?handler=Vincular
POST /Produtos/Colecoes/Produtos/{id:int}?handler=DefinirDestaque
POST /Produtos/Colecoes/Produtos/{id:int}?handler=Desvincular
~~~

O id representa ColecaoProduto.Id.

A página pertence à área /Produtos e usa EmpresaAtiva.

Não exigir AdministradorEmpresa.

## Cabeçalho da página de gerenciamento

Exibir:

- Nome da Coleção;
- Data de lançamento;
- Data de finalização ou Em aberto;
- Situação temporal;
- Categorias da Coleção apenas como contexto informativo.

Não transformar Categoria em regra de validação.

## Produtos vinculados

Listar vínculos existentes com:

~~~text
Produto
Categoria
Situação do Produto
Destaque
Ações
~~~

Ordenação:

~~~text
Destaque DESC
Produto.NomeNormalizado ASC
~~~

Produto inativo deve aparecer como:

~~~text
Inativo
~~~

Não esconder vínculos históricos.

## Vincular Produto

Formulário mínimo:

~~~text
Produto
Destaque
~~~

O select deve listar Produtos da Empresa Ativa ainda não vinculados à Coleção.

Pode listar ativos e inativos.

Ordenar:

~~~text
Ativos primeiro
NomeNormalizado ASC
~~~

Rótulo sugerido para inativo:

~~~text
Nome do Produto (inativo)
~~~

POST:

1. carregar Coleção tenant-aware;
2. validar Produto tenant-aware;
3. verificar ausência do vínculo;
4. criar ProdutoColecao;
5. SaveChanges;
6. PRG para a mesma página.

Mensagem:

~~~text
Produto vinculado à coleção.
~~~

Produto/Coleção cross-tenant:

~~~text
404 ou validação controlada sem vazamento
~~~

## Produto já vinculado

Tentativa repetida deve resultar em mensagem amigável:

~~~text
Este produto já está vinculado à coleção.
~~~

Não alterar Destaque silenciosamente.

Destaque deve ser alterado pela ação específica.

## Alterar destaque

Ação POST por vínculo existente.

Input:

~~~text
produtoId
destaque
~~~

O vínculo deve ser recarregado por:

~~~text
Empresa Ativa
+ ColecaoProdutoId da rota
+ ProdutoId
~~~

Atualizar somente:

~~~text
Destaque
~~~

A ação pode ser implementada por botão:

~~~text
Marcar como destaque
Remover destaque
~~~

ou checkbox explícito com submit.

Mensagem sugerida:

~~~text
Destaque atualizado.
~~~

## Desvincular Produto

POST antiforgery.

Revalidar vínculo local.

Remover apenas ProdutoColecao.

Mensagem:

~~~text
Produto desvinculado da coleção.
~~~

Vínculo inexistente pode retornar 404 ou resposta idempotente controlada.

Nunca remover Produto.

## Coleção cross-tenant

GET:

~~~text
/Produtos/Colecoes/Produtos/{idDeOutroTenant}
=> 404
~~~

Todos os POSTs devem igualmente não alterar dados cross-tenant.

## Consulta no detalhe do Produto

Evoluir:

~~~text
/Produtos/Detalhes/{id}
~~~

ou a rota vigente equivalente para exibir seção:

~~~text
Coleções
~~~

Para cada vínculo:

- Nome da Coleção;
- período;
- situação temporal;
- Destaque: Sim/Não.

Ordenar:

~~~text
DataLancamento DESC
NomeNormalizado ASC
~~~

Se não houver vínculo:

~~~text
Este produto ainda não participa de nenhuma coleção.
~~~

Não permitir gestão cross-tenant pelo detalhe.

Pode fornecer link para a página de gerenciamento da respectiva Coleção.

## Filtro na listagem de Produtos

Evoluir:

~~~text
GET /Produtos
~~~

com filtro adicional:

~~~text
colecao
~~~

Opções:

~~~text
Todas as coleções
Sem coleção
<coleções da Empresa Ativa>
~~~

Coleções ordenadas:

~~~text
DataLancamento DESC
NomeNormalizado ASC
~~~

Filtro específico:

~~~text
colecao=<id>
=> Produtos vinculados à Coleção
~~~

Filtro:

~~~text
colecao=sem-colecao
=> Produtos sem qualquer ProdutoColecao
~~~

Valor inválido/cross-tenant:

- não vaza existência;
- resulta em coleção vazia ou filtro inválido controlado;
- não retorna dados de outro tenant.

## Composição com filtros existentes

O filtro de Coleção compõe com:

- pesquisa por nome;
- Categoria;
- Abaixo da margem;
- Precificação incompleta.

Semântica:

~~~text
AND
~~~

Exemplo:

~~~text
Categoria = Agenda
Coleção = 2027
Filtro = abaixo-da-margem

=> Produtos que atendem aos três critérios
~~~

Cards do Dashboard não são alterados.

## Apresentação na listagem

Não é obrigatório adicionar coluna Coleções à tabela de Produtos, pois a tabela já possui muitos indicadores.

O requisito da UC034 na listagem é:

- filtro funcional;
- preservação dos filtros existentes.

As Coleções do Produto ficam visíveis em Detalhes.

## Relação com Produto Ativo/Inativo

Sem filtro de margem, a listagem continua exibindo ativos e inativos conforme comportamento atual.

Filtro por Coleção não força Produto.Ativo.

Logo:

~~~text
Coleção X
=> pode retornar Produto ativo e inativo
~~~

Se combinado com filtros que já exigem ativo, preservar a regra existente desses filtros.

## Relação com Coleção Finalizada

Filtro e detalhe continuam mostrando Coleções Finalizadas.

Não esconder histórico por situação temporal.

## Relação com UC033

UC034 não altera:

- Nome da Coleção;
- período da Coleção;
- Categorias da Coleção;
- situação temporal.

A página de gerenciamento de Produtos apenas consome esses dados.

Remover Categoria da Coleção não remove ProdutoColecao.

Desativar Categoria também não remove ProdutoColecao.

## Relação com precificação

ProdutoColecao e Destaque não participam de:

- custo de itens;
- mão de obra;
- energia;
- desgaste;
- perdas;
- custo total/unitário;
- preço teórico;
- preço sugerido;
- preço de prateleira;
- margem;
- reserva comercial;
- completude de precificação.

Não alterar serviços/calculadoras de precificação.

## Relação com UC035

A UC035 poderá usar Coleções do Produto como contexto para pesquisar referências externas comparáveis.

UC034 não implementa integração externa.

Não persistir dados de mercado.

## Segurança HTTP

Todos os POSTs:

- antiforgery;
- EmpresaId somente do contexto;
- InputModels restritos;
- sem bind direto de entidades EF.

GET não altera estado.

produtoId e colecaoId informados pelo cliente são sempre revalidados contra GQF/Empresa Ativa.

## Regras de negócio

### RN080 — Produto e Coleção possuem relação N:N tenant-aware

Um Produto pode participar de várias Coleções e uma Coleção pode conter vários Produtos.

O vínculo ProdutoColecao pertence à mesma Empresa dos dois lados e é único por Produto/Coleção.

### RN081 — Participação do Produto herda o período da Coleção

A UC034 não mantém vigência própria no vínculo.

O período efetivo de participação é exatamente o período comercial da Coleção.

Alterar as datas da Coleção altera o período efetivo de todos os Produtos vinculados.

### RN082 — Destaque é atributo não exclusivo do vínculo

Destaque pertence a ProdutoColecao.

Vários Produtos podem ser destaque na mesma Coleção e o mesmo Produto pode ser destaque em várias Coleções.

Destaque não altera nenhuma regra de precificação.

### RN083 — Categoria e situação do Produto não restringem o vínculo

Categorias da Coleção são metadados e não regras de elegibilidade.

Produtos ativos ou inativos, com ou sem Categoria, podem ser vinculados.

Mudanças em Categoria ou Ativo não removem nem alteram ProdutoColecao.

## Critérios de aceitação

- **CA01:** existe entidade tenant-owned ProdutoColecao.
- **CA02:** relacionamento Produto-Coleção é N:N.
- **CA03:** Produto não recebe ColecaoProdutoId.
- **CA04:** chave impede duplicidade Produto/Coleção.
- **CA05:** EmpresaId do vínculo é explícito e tenant-aware.
- **CA06:** Produto pode participar de várias Coleções.
- **CA07:** Coleção pode possuir vários Produtos.
- **CA08:** Coleções sobrepostas podem compartilhar Produto.
- **CA09:** vínculo não possui datas próprias.
- **CA10:** período efetivo vem da Coleção.
- **CA11:** editar período da Coleção altera período efetivo dos vínculos.
- **CA12:** Destaque é bool por vínculo.
- **CA13:** Destaque default é false quando não escolhido.
- **CA14:** vários Produtos podem ser destaque na mesma Coleção.
- **CA15:** mesmo Produto pode ser destaque em várias Coleções.
- **CA16:** Destaque não altera precificação.
- **CA17:** Produto ativo pode ser vinculado.
- **CA18:** Produto inativo pode ser vinculado.
- **CA19:** desativar Produto preserva vínculos.
- **CA20:** reativar Produto preserva vínculos.
- **CA21:** Coleção Planejada aceita vínculo.
- **CA22:** Coleção Em andamento aceita vínculo.
- **CA23:** Coleção Finalizada aceita correção de vínculo.
- **CA24:** Produto sem Categoria pode ser vinculado.
- **CA25:** Categoria fora da lista da Coleção não bloqueia vínculo.
- **CA26:** Coleção sem Categorias aceita Produtos.
- **CA27:** mudar Categoria do Produto preserva vínculos.
- **CA28:** mudar/remover Categoria da Coleção preserva vínculos.
- **CA29:** desvincular remove somente ProdutoColecao.
- **CA30:** desvincular não remove Produto/Coleção/precificação.
- **CA31:** tabela ProdutosColecoes possui Destaque bit NOT NULL.
- **CA32:** PK é ProdutoId + ColecaoProdutoId.
- **CA33:** FKs usam Restrict.
- **CA34:** ProdutoColecao participa do GQF/guard tenant.
- **CA35:** guard rejeita Produto cross-tenant.
- **CA36:** guard rejeita Coleção cross-tenant.
- **CA37:** migration clean DB funciona.
- **CA38:** upgrade pós-UC033 preserva dados.
- **CA39:** migration não altera schema de Produto.
- **CA40:** não há backfill automático.
- **CA41:** página Gerenciar produtos exige EmpresaAtiva.
- **CA42:** coleção de outro tenant retorna 404.
- **CA43:** lista de vinculados mostra Produto, Categoria, Ativo e Destaque.
- **CA44:** produto inativo vinculado permanece visível.
- **CA45:** select de vínculo não exibe Produto de outro tenant.
- **CA46:** select não oferece Produto já vinculado.
- **CA47:** vínculo válido usa PRG.
- **CA48:** tentativa repetida retorna mensagem amigável.
- **CA49:** corrida de dois vínculos idênticos não retorna 500.
- **CA50:** corrida persiste no máximo uma linha.
- **CA51:** alterar Destaque só altera o vínculo local.
- **CA52:** desvincular cross-tenant não altera dados.
- **CA53:** POSTs exigem antiforgery.
- **CA54:** EmpresaId não é controlado pelo request.
- **CA55:** Detalhes do Produto lista suas Coleções.
- **CA56:** Detalhes mostra período, situação e Destaque.
- **CA57:** Produto sem Coleção exibe estado vazio.
- **CA58:** /Produtos possui filtro por Coleção.
- **CA59:** filtro por Coleção específica retorna apenas vinculados.
- **CA60:** filtro Sem coleção retorna apenas Produtos sem vínculo.
- **CA61:** filtro de Coleção compõe com pesquisa por nome.
- **CA62:** filtro de Coleção compõe com Categoria.
- **CA63:** filtro de Coleção compõe com filtros de margem/completude.
- **CA64:** filtro por Coleção não força Ativo por si só.
- **CA65:** Coleção finalizada permanece disponível em consulta/filtro.
- **CA66:** valor de coleção cross-tenant não vaza dados.
- **CA67:** Categoria da Coleção não é tratada como elegibilidade.
- **CA68:** nenhuma fórmula de precificação é alterada.
- **CA69:** UC035 não é antecipada.
- **CA70:** Build Release fica verde.
- **CA71:** unitários aplicáveis ficam verdes.
- **CA72:** integração SQL Server/Web fica verde.

## Matriz mínima de testes

### Unitários — ProdutoColecao

- criar ids válidos;
- ids <=0 rejeitam;
- Destaque false;
- Destaque true;
- alternar Destaque;
- EmpresaId não pode ser reatribuído.

### Persistência

- tabela ProdutosColecoes;
- PK composta;
- Destaque bit;
- FKs Restrict;
- índices;
- GQF;
- guard same-tenant;
- Produto cross-tenant rejeitado;
- Coleção cross-tenant rejeitada;
- mesmo par duplicado rejeitado;
- mesmo Produto em outra Coleção permitido;
- outro Produto na mesma Coleção permitido.

### Migration

- clean DB;
- upgrade pós-UC033;
- Produtos/Coleções/Categorias/precificação preservados;
- nenhum campo novo em Produtos;
- zero migrations pendentes.

### Web — gerenciamento da Coleção

Preparar:

- Produto ativo da Categoria da Coleção;
- Produto ativo de outra Categoria;
- Produto sem Categoria;
- Produto inativo;
- Produto de outro tenant;
- Produto já vinculado.

Validar:

- todos os Produtos locais elegíveis aparecem;
- Produto alheio não aparece;
- já vinculado não aparece no select;
- Categoria não bloqueia;
- inativo pode vincular;
- vínculo normal;
- vínculo como Destaque;
- PRG;
- estado da tabela;
- toggle de Destaque;
- desvínculo;
- antiforgery;
- cross-tenant;
- mass assignment EmpresaId.

### Concorrência

Duas requisições simultâneas vinculando o mesmo Produto à mesma Coleção.

Resultado:

- uma linha;
- uma resposta vencedora;
- perdedora controlada;
- nenhum 500.

### Detalhes do Produto

- zero Coleções;
- uma Coleção;
- várias Coleções;
- sobreposição;
- Finalizada;
- destaque Sim/Não;
- ordenação por lançamento;
- Produto de outro tenant 404 normal já existente.

### Filtro /Produtos

- todas;
- Coleção específica;
- Sem coleção;
- coleção inválida;
- coleção de outro tenant;
- combinado com q;
- combinado com Categoria;
- combinado com abaixo-da-margem;
- combinado com precificação-incompleta;
- Produto inativo permanece quando o filtro atual permitir.

### Regressão

- UC033 cadastro/edição de Coleções continua funcional;
- remover Categoria da Coleção preserva ProdutoColecao;
- UC032 Categoria continua funcional;
- UC010 Ativo/Inativo continua funcional;
- UC036 desgaste não muda;
- UC011–UC030 precificação e filtros existentes permanecem funcionais;
- nenhuma integração externa é criada.

## Arquivos esperados

Lista indicativa:

~~~text
src/Precificador.Core/Produtos/ProdutoColecao.cs

src/Precificador.Infrastructure/Persistence/Configurations/ProdutoColecaoConfiguration.cs
src/Precificador.Infrastructure/Persistence/PrecificadorDbContext.cs
src/Precificador.Infrastructure/Migrations/<migration UC034>.cs

src/Precificador.Web/Pages/Produtos/Colecoes/Produtos.cshtml(.cs)
src/Precificador.Web/Pages/Produtos/Colecoes/Index.cshtml
src/Precificador.Web/Pages/Produtos/Detalhes.cshtml(.cs)
src/Precificador.Web/Pages/Produtos/Index.cshtml(.cs)

tests/Precificador.Tests.Unit/Produtos/ProdutoColecaoTests.cs
tests/Precificador.Tests.Integration/Infrastructure/ProdutoColecaoPersistenceTests.cs
tests/Precificador.Tests.Integration/Web/ProdutoColecaoPageTests.cs
~~~

## Fora do escopo

- vigência própria dentro da Coleção;
- múltiplos períodos do mesmo Produto na mesma Coleção;
- histórico/auditoria de vínculo;
- histórico de mudanças de Destaque;
- destaque exclusivo;
- ranking/ordem dos destaques;
- imagem/banner do Produto na Coleção;
- preço específico por Coleção;
- desconto específico por Coleção;
- margem específica por Coleção;
- estoque;
- vendas/metas por Coleção;
- inclusão automática por Categoria;
- bloqueio por Categoria;
- integração externa/referências de mercado — UC035;
- recomendação automática de Coleção;
- importação/exportação;
- API REST;
- novas permissões.

## Definition of Done

UC034 está concluída quando:

- ProdutoColecao N:N tenant-aware existe;
- participação herda o período da Coleção;
- Destaque funciona por vínculo;
- Categoria não restringe;
- Produtos ativos/inativos podem participar;
- gerenciamento por Coleção funciona;
- Detalhes do Produto mostra Coleções;
- /Produtos filtra por Coleção;
- desvínculo afeta somente a associação;
- corrida de vínculo duplicado é controlada;
- migration clean/upgrade passa;
- precificação não muda;
- UC035 permanece fora do escopo;
- RN080–RN083 estão alinhadas;
- CI completa fica verde;
- backlog marca UC034 como Concluído após implementação.

## Branch sugerida

~~~text
feat/uc034-vincular-produtos-colecoes
~~~

## Commit sugerido

~~~text
feat: vincula produtos a colecoes
~~~
