# UC033 — Administrar coleções

- **Funcionalidade:** F002 — Gestão de Produtos
- **Dependência funcional:** UC032
- **Estado:** Concluído
- **Alteração de domínio:** sim
- **Alteração de schema:** sim
- **Migration:** sim
- **Autorização:** EmpresaAtiva
- **Relação com Produtos:** fora do escopo; será tratada na UC034
- **Regras principais:** RN076 a RN079

## Objetivo

Permitir que a Empresa Ativa cadastre e mantenha Coleções comerciais para organizar seu catálogo ao longo do tempo.

Uma Coleção representa um período comercial, por exemplo:

~~~text
Coleção 2027
Natal 2027
Dia das Mães 2028
Sakura
~~~

A UC033 deve permitir:

- cadastrar Coleção;
- listar Coleções da Empresa Ativa;
- editar nome e período;
- informar as Categorias de Produto envolvidas;
- preservar isolamento tenant-aware;
- apresentar situação temporal derivada;
- manter histórico sem exclusão física.

A UC033 **não vincula Produtos a Coleções**. Essa responsabilidade pertence integralmente à UC034.

## Decisões de domínio

Criar entidade tenant-owned:

~~~text
ColecaoProduto : IEntidadeEmpresa
- Id : int
- EmpresaId : int
- Nome : string
- NomeNormalizado : string
- DataLancamento : DateOnly
- DataFinalizacao : DateOnly?
~~~

Criar entidade de associação tenant-owned:

~~~text
ColecaoProdutoCategoria : IEntidadeEmpresa
- EmpresaId : int
- ColecaoProdutoId : int
- CategoriaProdutoId : int
~~~

A associação entre Coleção e Categoria é N:N.

Não usar enum ou texto livre para Categorias.

Não compartilhar Coleções entre Empresas.

## Nome

Aplicar a mesma normalização usada em Produto:

~~~text
Nome
-> Trim()
-> sequências de whitespace interno viram um espaço
-> capitalização de exibição preservada

NomeNormalizado
-> Nome.ToUpperInvariant()
~~~

Validações:

~~~text
Nome obrigatório
Nome.Length <= 120
~~~

Mensagens sugeridas:

~~~text
O nome da coleção é obrigatório.
O nome da coleção deve possuir no máximo 120 caracteres.
~~~

## Identidade e unicidade

A identidade funcional é:

~~~text
EmpresaId + NomeNormalizado + DataLancamento
~~~

Criar índice único correspondente.

Motivação: uma Empresa pode repetir um conceito comercial em anos/períodos diferentes.

Exemplo válido:

~~~text
Natal
DataLancamento = 01/11/2027

Natal
DataLancamento = 01/11/2028
~~~

Exemplo inválido no mesmo tenant:

~~~text
Natal
NATAL
mesma DataLancamento
~~~

Consequências:

- Empresas diferentes podem possuir Coleções idênticas;
- o mesmo nome pode reaparecer em outro lançamento;
- DataFinalizacao não participa da identidade;
- editar Nome ou DataLancamento continua sujeito à unicidade;
- a constraint permanece segunda linha de defesa contra concorrência.

Mensagem amigável:

~~~text
Já existe uma coleção com esse nome e data de lançamento.
~~~

## Período comercial

### Data de lançamento

~~~text
DataLancamento
~~~

é obrigatória.

Ela representa o primeiro dia comercial da Coleção.

Pode ser:

- passada;
- hoje;
- futura.

Coleções futuras são válidas e permitem planejamento antecipado.

### Data de finalização

~~~text
DataFinalizacao
~~~

é opcional.

~~~text
null
=> período sem finalização definida
~~~

Quando informada:

~~~text
DataFinalizacao >= DataLancamento
~~~

A igualdade é válida e representa uma Coleção de um único dia.

Datas são inclusivas.

Exemplo:

~~~text
DataLancamento = 01/12/2027
DataFinalizacao = 24/12/2027

01/12 e 24/12 pertencem ao período.
~~~

Não usar DateTime/UTC para essas duas datas. São datas comerciais locais e devem ser persistidas como DateOnly/date SQL.

## Situação temporal

Não persistir coluna de status.

Derivar a situação usando:

~~~text
IDataOperacionalEmpresa.Hoje
~~~

para respeitar o timezone da Empresa.

Estados de apresentação:

~~~text
Planejada
Em andamento
Finalizada
~~~

Regras:

~~~text
Hoje < DataLancamento
=> Planejada

DataLancamento <= Hoje
AND (DataFinalizacao is null OR Hoje <= DataFinalizacao)
=> Em andamento

DataFinalizacao is not null
AND Hoje > DataFinalizacao
=> Finalizada
~~~

É aceitável criar enum derivado:

~~~csharp
SituacaoColecaoProduto
{
    Planejada = 1,
    EmAndamento = 2,
    Finalizada = 3
}
~~~

Esse enum não deve gerar coluna no banco.

## Finalização não é estado terminal persistido

A Coleção não possui:

~~~text
Ativo
Encerrada
Excluida
~~~

nesta UC.

A situação decorre exclusivamente das datas.

Editar uma Coleção pode:

- antecipar/postergar lançamento;
- definir finalização;
- alterar finalização;
- limpar DataFinalizacao.

Portanto uma Coleção exibida como Finalizada pode voltar a Planejada/Em andamento após correção administrativa das datas.

A UC033 não cria histórico dessas alterações.

## Sobreposição

Coleções podem se sobrepor livremente.

Exemplos válidos:

~~~text
Coleção 2027
01/09/2026 -> 31/12/2027

Natal 2026
01/11/2026 -> 26/12/2026
~~~

Mesmo que envolvam a mesma Categoria.

Não bloquear:

- períodos coincidentes;
- períodos parcialmente sobrepostos;
- uma Coleção contida em outra;
- múltiplas Coleções sem finalização definida.

A futura UC034 também não deve inferir exclusividade de Produto apenas a partir desta UC.

## Categorias envolvidas

Uma Coleção pode envolver:

~~~text
zero ou mais Categorias de Produto
~~~

Zero é permitido para possibilitar planejamento da Coleção antes de o mix comercial estar fechado.

A associação é informativa/organizacional nesta UC.

Ela **não significa**:

- que todos os Produtos da Categoria pertencem automaticamente à Coleção;
- que Produto de Categoria não listada será proibido na UC034;
- que Categoria é copiada para Produto;
- que a Coleção altera cálculo ou precificação.

A elegibilidade/vínculo de Produtos será definida exclusivamente na UC034.

## Categorias elegíveis no cadastro

No cadastro de Coleção, oferecer apenas:

~~~text
CategoriaProduto
EmpresaId = Empresa Ativa
Ativo = true
~~~

Ordenadas por NomeNormalizado.

## Categorias na edição

No GET de edição:

- listar todas as Categorias ativas da Empresa Ativa;
- incluir também Categorias atualmente vinculadas mesmo que tenham sido desativadas;
- marcar as vinculadas como selecionadas.

O usuário pode:

- preservar Categoria vinculada inativa;
- remover Categoria vinculada inativa;
- adicionar Categoria ativa;
- remover Categoria ativa.

Não permitir adicionar por request uma **nova** associação com Categoria inativa que não estava vinculada anteriormente.

Não permitir Categoria inexistente ou cross-tenant.

## Efeito da desativação de Categoria

Desativar Categoria pela UC032 não altera Coleções.

Exemplo:

~~~text
Coleção 2027
-> Categoria Agenda

Agenda é desativada depois
=> vínculo da Coleção permanece
~~~

A listagem/edição da Coleção continua mostrando o nome da Categoria vinculada.

Renomear Categoria atualiza a apresentação automaticamente por relacionamento.

## Remoção da Categoria da Coleção

Remover uma Categoria da Coleção:

- remove somente a linha de associação;
- não desativa a Categoria;
- não altera Produtos;
- não altera Fichas;
- não altera precificação.

Após a futura UC034, essa remoção também não deverá apagar automaticamente vínculos Produto-Coleção já existentes, salvo decisão explícita daquela UC.

## Sem exclusão física de Coleção

A UC033 não oferece exclusão.

Coleções históricas permanecem consultáveis.

Não criar:

- DELETE Web;
- hard delete;
- soft-delete adicional;
- ação “Excluir”.

Correções são feitas por edição.

## Integridade tenant-aware

Adicionar ColecaoProduto e ColecaoProdutoCategoria ao padrão tenant-aware:

- DbSet;
- Global Query Filter;
- guard central por IEntidadeEmpresa.

Não permitir reatribuição de EmpresaId.

Para cada associação:

~~~text
ColecaoProduto.EmpresaId
=
ColecaoProdutoCategoria.EmpresaId
=
CategoriaProduto.EmpresaId
~~~

A persistência deve validar as duas referências usando ownership real, inclusive contra objetos fabricados fora da Web.

Request nunca escolhe EmpresaId.

## Persistência

Criar tabela:

~~~text
ColecoesProdutos
~~~

Campos:

~~~text
Id int identity PK
EmpresaId int NOT NULL
Nome nvarchar(120) NOT NULL
NomeNormalizado nvarchar(120) NOT NULL
DataLancamento date NOT NULL
DataFinalizacao date NULL
~~~

Índices:

~~~text
UNIQUE (EmpresaId, NomeNormalizado, DataLancamento)
INDEX (EmpresaId, DataLancamento)
~~~

FK:

~~~text
EmpresaId -> Empresas.Id
ON DELETE RESTRICT
~~~

Check constraint:

~~~text
DataFinalizacao IS NULL
OR DataFinalizacao >= DataLancamento
~~~

Nome sugerido:

~~~text
CK_ColecoesProdutos_Periodo
~~~

### Tabela de associação

Criar:

~~~text
ColecoesProdutosCategorias
~~~

Campos:

~~~text
EmpresaId int NOT NULL
ColecaoProdutoId int NOT NULL
CategoriaProdutoId int NOT NULL
~~~

Chave:

~~~text
PRIMARY KEY (ColecaoProdutoId, CategoriaProdutoId)
~~~

Índices:

~~~text
INDEX (EmpresaId, CategoriaProdutoId)
~~~

FKs:

~~~text
EmpresaId -> Empresas.Id RESTRICT
ColecaoProdutoId -> ColecoesProdutos.Id RESTRICT
CategoriaProdutoId -> CategoriasProdutos.Id RESTRICT
~~~

Não usar cascade delete.

## Migration

Criar migration evolutiva após o estado pós-UC031.

Não rebaselinear.

Não editar migrations anteriores.

A migration deve:

1. criar ColecoesProdutos;
2. criar ColecoesProdutosCategorias;
3. criar índices/FKs/check;
4. atualizar ModelSnapshot.

Não há backfill: não existem Coleções legadas persistidas.

Não alterar:

- Produtos;
- CategoriasProdutos;
- precificação;
- Identity;
- vínculos de usuários.

### Testes de migration

Validar:

- clean DB;
- upgrade da master pós-UC031;
- tabelas/colunas corretas;
- tipos SQL date;
- índice único;
- FKs Restrict;
- check do período;
- dados existentes preservados;
- zero migrations pendentes.

## Domínio ColecaoProduto

Métodos conceituais:

~~~csharp
ColecaoProduto.Criar(
    empresaId,
    nome,
    dataLancamento,
    dataFinalizacao)

colecao.AtualizarDados(
    nome,
    dataLancamento,
    dataFinalizacao)
~~~

A atualização deve ser atômica em memória.

Se qualquer campo for inválido, o objeto não fica parcialmente alterado.

Método de situação pode receber data operacional:

~~~csharp
colecao.ObterSituacao(DateOnly hoje)
~~~

ou usar helper puro equivalente.

Não injetar serviço HTTP/timezone no Core.

## Associação de Categorias

É aceitável que a Web/serviço de aplicação sincronize a coleção de associações em uma única unidade de trabalho:

~~~text
ids atuais
vs
ids selecionados
=> adicionar/remover diferenças
~~~

Não usar uma chamada SaveChanges por Categoria.

Cadastro e edição devem persistir:

~~~text
ColecaoProduto
+
associações de Categoria
~~~

atomicamente.

## Web

Criar área:

~~~text
/Produtos/Colecoes
~~~

Rotas esperadas:

~~~text
/Produtos/Colecoes
/Produtos/Colecoes/Novo
/Produtos/Colecoes/Editar/{id:int}
~~~

Não é necessária página Detalhes separada nesta UC.

Como está sob /Produtos, continua protegida pela policy EmpresaAtiva já existente.

Não exigir AdministradorEmpresa: administração do catálogo segue a mesma autorização atual de Produtos/Categorias.

## Listagem

Exibir:

~~~text
Nome
Lançamento
Finalização
Situação
Categorias
Ações
~~~

Finalização nula pode ser exibida como:

~~~text
Em aberto
~~~

Categorias:

- exibir nomes em ordem NomeNormalizado;
- nenhuma Categoria => “—”.

Ordenação principal:

~~~text
DataLancamento DESC
NomeNormalizado ASC
~~~

Assim Coleções mais recentes/futuras ficam em destaque.

Ações:

- Nova coleção;
- Editar.

Não há Desativar/Reativar/Excluir.

## Cadastro

Campos:

~~~text
Nome
Data de lançamento
Data de finalização (opcional)
Categorias envolvidas (zero ou mais)
~~~

Categorias podem ser apresentadas por checkboxes ou multiselect.

POST válido:

~~~text
criar ColecaoProduto
sincronizar associações
SaveChanges
PRG /Produtos/Colecoes
mensagem de sucesso
~~~

Mensagem:

~~~text
Coleção cadastrada com sucesso.
~~~

POST inválido:

- retorna 200;
- preserva Input;
- recarrega Categorias;
- não persiste estado parcial.

## Edição

Campos:

~~~text
Nome
Data de lançamento
Data de finalização
Categorias envolvidas
~~~

POST válido:

- atualiza apenas dados da Coleção e associações;
- preserva Id;
- preserva EmpresaId;
- usa uma unidade de trabalho;
- usa PRG.

Mensagem:

~~~text
Coleção atualizada com sucesso.
~~~

GET/POST cross-tenant:

~~~text
404
~~~

## Duplicidade amigável

Antes de persistir, quando viável:

~~~text
EmpresaId
+ NomeNormalizado
+ DataLancamento
~~~

deve ser consultado excluindo o próprio Id na edição.

Em corrida, tratar violation do índice único e apresentar a mesma mensagem amigável.

Não retornar 500 por duplicidade esperada.

## Validação de Categoria no POST

O request informa apenas ids de Categoria.

Para cada id selecionado:

- deve existir;
- deve pertencer à Empresa Ativa;
- deve estar ativa, salvo se já estava vinculada à Coleção em edição.

Ids duplicados no request devem ser colapsados ou rejeitados de forma controlada, nunca gerar PK violation 500.

Id <= 0, inexistente ou cross-tenant => erro de validação.

Mensagem sugerida:

~~~text
Selecione categorias de produto válidas.
~~~

## Acesso a partir de Produtos

Adicionar em /Produtos acesso claro para:

~~~text
Categorias de produto
Coleções
Cadastrar produto
~~~

Não adicionar Coleção como campo de Produto nesta UC.

## Segurança HTTP

Todas as mutações:

- usam POST;
- exigem antiforgery;
- não aceitam EmpresaId;
- preservam PRG.

GET não altera dados.

InputModels não devem bindar entidade EF diretamente.

Cross-tenant deve resultar em 404/validação sem vazamento.

## Relação com UC034

A UC034 começará somente depois da UC033 concluída.

UC033 **não pode** antecipar:

- ColecaoProdutoId em Produto;
- lista de Coleções em Produto/Novo ou Editar;
- tabela ProdutoColecao;
- período de participação do Produto;
- destaque de Produto;
- regras de cardinalidade Produto-Coleção;
- filtro de Produtos por Coleção;
- validação da Categoria do Produto contra Categorias da Coleção.

As Categorias da UC033 são metadados da Coleção e não constituem regra de elegibilidade de Produto.

## Relação com precificação

Coleção não participa de:

- custo;
- desgaste;
- margem;
- preço sugerido;
- preço de prateleira;
- snapshot comercial;
- completude de precificação.

Nenhuma calculadora ou serviço de precificação deve ser alterado.

## Regras de negócio

### RN076 — Identidade da Coleção é tenant-aware e temporal

Coleção pertence a uma única Empresa.

Sua identidade funcional é:

~~~text
EmpresaId + NomeNormalizado + DataLancamento
~~~

O mesmo conceito pode reaparecer em outro lançamento, mas não pode existir duplicado na mesma Empresa/data.

### RN077 — Período da Coleção usa datas comerciais inclusivas

DataLancamento é obrigatória.

DataFinalizacao é opcional e, quando informada, deve ser maior ou igual à DataLancamento.

Datas futuras são válidas.

A situação Planejada/Em andamento/Finalizada é derivada por IDataOperacionalEmpresa.Hoje e não persistida.

### RN078 — Coleções podem sobrepor períodos e Categorias

Não existe exclusividade temporal entre Coleções.

Uma Categoria pode participar de várias Coleções simultaneamente.

Sobreposição não bloqueia cadastro nem edição.

### RN079 — Categorias da Coleção são metadados, não vínculo de Produto

Uma Coleção pode possuir zero ou mais Categorias tenant-aware.

A associação não inclui Produtos automaticamente e não restringe por si só a futura vinculação da UC034.

Desativar/renomear Categoria não remove a associação existente.

## Critérios de aceitação

- **CA01:** existe entidade tenant-owned ColecaoProduto.
- **CA02:** existe associação tenant-owned N:N ColecaoProdutoCategoria.
- **CA03:** Nome é obrigatório, normalizado e limitado a 120 caracteres.
- **CA04:** identidade única é EmpresaId + NomeNormalizado + DataLancamento.
- **CA05:** mesmo nome pode existir em datas de lançamento diferentes.
- **CA06:** mesma identidade em Empresas diferentes é válida.
- **CA07:** DataLancamento é obrigatória.
- **CA08:** DataLancamento futura é válida.
- **CA09:** DataFinalizacao é opcional.
- **CA10:** DataFinalizacao anterior ao lançamento é rejeitada.
- **CA11:** DataFinalizacao igual ao lançamento é válida.
- **CA12:** datas são persistidas como date/DateOnly.
- **CA13:** status temporal não é persistido.
- **CA14:** status usa IDataOperacionalEmpresa.Hoje.
- **CA15:** Hoje antes do lançamento => Planejada.
- **CA16:** Hoje dentro do período inclusivo => Em andamento.
- **CA17:** Hoje depois da finalização => Finalizada.
- **CA18:** finalização null mantém Em andamento após o lançamento.
- **CA19:** editar datas pode alterar o status derivado.
- **CA20:** períodos sobrepostos são permitidos.
- **CA21:** mesma Categoria pode participar de Coleções sobrepostas.
- **CA22:** Coleção pode ser criada sem Categorias.
- **CA23:** cadastro oferece somente Categorias ativas do tenant.
- **CA24:** edição preserva Categoria atual inativa.
- **CA25:** edição pode remover Categoria inativa atual.
- **CA26:** edição não aceita nova Categoria inativa manipulada.
- **CA27:** Categoria inexistente/cross-tenant é rejeitada.
- **CA28:** ids duplicados de Categoria não geram erro 500.
- **CA29:** desativar Categoria não remove vínculo da Coleção.
- **CA30:** renomear Categoria atualiza apresentação por relacionamento.
- **CA31:** remover associação não altera Categoria nem Produto.
- **CA32:** Coleção não possui exclusão física na UC033.
- **CA33:** Coleção não possui Ativo/soft-delete adicional.
- **CA34:** edição de Coleção Finalizada continua permitida.
- **CA35:** limpar DataFinalizacao é permitido.
- **CA36:** listagem mostra apenas Coleções da Empresa Ativa.
- **CA37:** listagem mostra nome, datas, situação e Categorias.
- **CA38:** listagem ordena DataLancamento DESC e NomeNormalizado ASC.
- **CA39:** cadastro válido usa PRG.
- **CA40:** cadastro inválido não persiste estado parcial.
- **CA41:** edição válida preserva Id/EmpresaId.
- **CA42:** edição sincroniza associações atomicamente.
- **CA43:** duplicidade retorna mensagem amigável.
- **CA44:** corrida de duplicidade não retorna 500.
- **CA45:** GET de edição cross-tenant retorna 404.
- **CA46:** POST cross-tenant não altera dados.
- **CA47:** EmpresaId não é aceito como ownership no request.
- **CA48:** POSTs exigem antiforgery.
- **CA49:** ColecaoProduto participa do GQF/guard tenant.
- **CA50:** ColecaoProdutoCategoria participa do GQF/guard tenant.
- **CA51:** guard rejeita Coleção/Categoria de Empresas diferentes.
- **CA52:** FKs usam Restrict; não há cascade delete.
- **CA53:** migration clean DB funciona.
- **CA54:** upgrade pós-UC031 preserva todos os dados.
- **CA55:** não há backfill de Coleções.
- **CA56:** Produtos não recebem campo/relação de Coleção.
- **CA57:** nenhuma tabela ProdutoColecao é criada.
- **CA58:** UC034 não é antecipada.
- **CA59:** nenhuma fórmula de precificação muda.
- **CA60:** acesso Coleções aparece na área de Produtos.
- **CA61:** Build Release fica verde.
- **CA62:** unitários aplicáveis ficam verdes.
- **CA63:** integração SQL Server/Web fica verde.

## Matriz mínima de testes

### Unitários — ColecaoProduto

- criação válida normaliza Nome;
- nome vazio/acima de 120 rejeita;
- data futura aceita;
- finalização null aceita;
- finalização < lançamento rejeita;
- finalização = lançamento aceita;
- atualização válida altera todos os dados;
- atualização inválida é atômica;
- EmpresaId não pode ser reatribuído;
- situação Planejada;
- situação Em andamento no primeiro dia;
- situação Em andamento no último dia;
- situação Finalizada depois do último dia;
- sem finalização permanece Em andamento.

### Persistência

- tabela ColecoesProdutos;
- tabela ColecoesProdutosCategorias;
- tipos date;
- índice único;
- check de período;
- FKs Restrict;
- GQF das duas entidades;
- guard cross-tenant;
- duplicidade same tenant/data rejeitada;
- mesmo nome outra data aceita;
- mesmo nome/data em outro tenant aceita;
- vínculo de Categoria cross-tenant rejeitado;
- associação duplicada não persiste em duplicidade.

### Migration

- clean DB;
- upgrade pós-UC031;
- dados de Empresas/Identity/Produtos/Categorias/precificação preservados;
- sem alteração de schema em Produtos/Categorias;
- zero migrations pendentes.

### Web — lista

Preparar:

- passada/finalizada;
- atual sem fim;
- futura;
- outra Empresa.

Validar:

- isolamento;
- status via data operacional;
- ordenação;
- datas formatadas;
- categorias em ordem;
- nenhuma categoria => travessão.

### Web — cadastro

- válido sem Categorias;
- válido com uma Categoria;
- válido com várias Categorias;
- futuro;
- finalização null;
- período de um dia;
- período inválido;
- nome inválido;
- duplicidade amigável;
- Categoria inativa/cross-tenant/manipulada;
- ids duplicados;
- antiforgery;
- mass assignment EmpresaId;
- PRG.

### Web — edição

- altera Nome;
- altera lançamento;
- define/limpa finalização;
- adiciona/remove Categorias;
- mantém Categoria inativa já vinculada;
- remove Categoria inativa;
- rejeita nova Categoria inativa;
- preserva Id/EmpresaId;
- cross-tenant GET/POST;
- duplicidade;
- input inválido preserva seleção;
- nenhuma mutação parcial.

### Concorrência

Dois cadastros simultâneos da mesma identidade funcional:

~~~text
Empresa
NomeNormalizado
DataLancamento
~~~

Resultado:

- uma Coleção no máximo;
- uma resposta pode ter sucesso;
- a outra recebe erro controlado;
- nenhum 500;
- associações não ficam órfãs.

### Regressão

- UC032 Categorias continua funcional;
- Categoria inativa vinculada a Produto continua válida;
- UC036 desgaste permanece idêntico;
- Produtos/Novo e Editar não ganham campo Coleção;
- listagem/dashboard/precificação não mudam;
- Operacional com Empresa Ativa mantém comportamento normal de Produtos.

## Arquivos esperados

Lista indicativa:

~~~text
src/Precificador.Core/Produtos/ColecaoProduto.cs
src/Precificador.Core/Produtos/ColecaoProdutoCategoria.cs
src/Precificador.Core/Produtos/SituacaoColecaoProduto.cs

src/Precificador.Infrastructure/Persistence/Configurations/ColecaoProdutoConfiguration.cs
src/Precificador.Infrastructure/Persistence/Configurations/ColecaoProdutoCategoriaConfiguration.cs
src/Precificador.Infrastructure/Persistence/PrecificadorDbContext.cs
src/Precificador.Infrastructure/Migrations/<migration UC033>.cs

src/Precificador.Web/Pages/Produtos/Colecoes/Index.cshtml(.cs)
src/Precificador.Web/Pages/Produtos/Colecoes/Novo.cshtml(.cs)
src/Precificador.Web/Pages/Produtos/Colecoes/Editar.cshtml(.cs)
src/Precificador.Web/Pages/Produtos/Colecoes/ColecaoProdutoInputModel.cs
src/Precificador.Web/Pages/Produtos/Index.cshtml

tests/Precificador.Tests.Unit/Produtos/ColecaoProdutoTests.cs
tests/Precificador.Tests.Integration/Infrastructure/ColecaoProdutoPersistenceTests.cs
tests/Precificador.Tests.Integration/Web/ColecaoProdutoPageTests.cs
~~~

## Fora do escopo

- vincular Produtos a Coleções — UC034;
- cardinalidade Produto-Coleção;
- vigência individual do Produto dentro da Coleção;
- destaque/prioridade do Produto;
- regra de Categoria como elegibilidade de Produto;
- filtro de Produtos por Coleção;
- dashboard por Coleção;
- vendas/faturamento da Coleção;
- metas de venda;
- preço específico por Coleção;
- desconto específico por Coleção;
- imagem/banner da Coleção;
- cor/identidade visual da Coleção;
- exclusão física;
- auditoria de alterações;
- importação/exportação;
- API REST;
- permissões novas;
- alterações de precificação.

## Definition of Done

UC033 está concluída quando:

- ColecaoProduto tenant-aware existe;
- período comercial e situação derivada funcionam;
- sobreposição é permitida;
- Categorias são N:N e cross-tenant seguro;
- cadastro/listagem/edição funcionam em /Produtos/Colecoes;
- Categoria inativa vinculada pode ser preservada;
- não existe exclusão;
- migration clean/upgrade passa;
- Produtos e precificação não foram alterados;
- UC034 permanece fora do escopo;
- RN076–RN079 estão alinhadas;
- CI completa fica verde;
- backlog marca UC033 como Concluído após implementação.

## Branch sugerida

~~~text
feat/uc033-administrar-colecoes
~~~

## Commit sugerido

~~~text
feat: adiciona administracao de colecoes
~~~
