# UC032 — Administrar categorias de Produto

- **Funcionalidade:** F002 — Gestão de Produtos
- **Origem:** evolução do catálogo / necessidade de regra futura de desgaste de equipamentos por Categoria
- **Estado:** Concluído
- **Prioridade:** antecipada
- **Gate operacional:** executar antes da UC036 e da MEL021
- **Dependências funcionais:** UC007, UC008, UC009 e UC010
- **Alteração de domínio:** sim
- **Alteração de schema:** sim
- **Migration:** sim
- **Alteração de fórmula de precificação:** não
- **Regra de desgaste de equipamentos:** fora do escopo; será tratada pela UC036

## Objetivo

Substituir a Categoria livre atualmente persistida como texto em `Produto` por uma entidade estruturada, tenant-aware e administrável por Empresa.

A UC032 deve permitir:

- cadastrar Categoria de Produto;
- listar Categorias da Empresa Ativa;
- editar o nome;
- desativar;
- reativar;
- vincular opcionalmente um Produto a uma Categoria;
- preservar e migrar as Categorias livres já existentes em Produtos;
- impedir referência cross-tenant;
- manter Produtos existentes vinculados quando uma Categoria for desativada.

A Categoria estruturada será a base da UC036, que adicionará a configuração do custo de desgaste de equipamentos.

A UC032 **não** implementa ainda nenhum campo, cálculo ou regra de desgaste.

## Situação atual

Hoje `Produto` contém:

~~~text
Categoria : string?
~~~

com a regra histórica RN043:

- texto livre opcional;
- até 80 caracteres;
- trim externo;
- redução de whitespace interno;
- capitalização preservada;
- nenhum cadastro estruturado;
- nenhuma participação em cálculo.

A Web permite digitar Categoria diretamente em:

~~~text
/Produtos/Novo
/Produtos/Editar/{id}
~~~

Esse modelo não é suficiente para regras futuras por Categoria, porque diferentes Produtos podem possuir textos equivalentes sem identidade própria e não existe um ponto único de configuração.

## Decisão de domínio

Criar entidade:

~~~text
CategoriaProduto : IEntidadeEmpresa
- Id : int
- EmpresaId : int
- Nome : string
- NomeNormalizado : string
- Ativo : bool
~~~

`CategoriaProduto` é tenant-owned.

Não usar enum.

Não compartilhar Categorias entre Empresas.

Não criar entidade genérica `Categoria`, pois o domínio já possui classificação de Insumo e o nome específico reduz ambiguidade técnica.

## Nome

Aplicar a mesma semântica de normalização já usada pela Categoria livre atual:

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
Nome.Length <= 80
~~~

Mensagem sugerida:

~~~text
O nome da categoria é obrigatório.
O nome da categoria deve possuir no máximo 80 caracteres.
~~~

## Identidade e unicidade

A identidade funcional é:

~~~text
EmpresaId + NomeNormalizado
~~~

Criar índice único correspondente.

Consequências:

- a mesma Empresa não pode possuir `Agenda` e `AGENDA` como Categorias distintas;
- Categoria inativa continua ocupando o nome;
- Empresas diferentes podem possuir Categoria com o mesmo nome;
- reativação não exige nova validação de unicidade;
- Categoria não participa da identidade do Produto.

A Web deve detectar duplicidade antes da constraint quando possível e apresentar mensagem amigável:

~~~text
Já existe uma categoria de produto cadastrada com esse nome.
~~~

A constraint permanece como segunda linha de defesa.

## Situação Ativa/Inativa

Toda nova Categoria nasce:

~~~text
Ativo = true
~~~

A Categoria pode ser desativada e reativada.

Operações devem ser idempotentes:

~~~text
Desativar categoria inativa
=> continua inativa
=> sem exceção

Reativar categoria ativa
=> continua ativa
=> sem exceção
~~~

Não criar exclusão física.

Não criar soft-delete adicional.

## Efeito da desativação

Desativar Categoria **não remove nem altera Produtos já vinculados**.

Exemplo:

~~~text
Categoria Agenda -> inativa
Produto Agenda 2027 -> continua CategoriaProdutoId = Agenda
~~~

Produtos vinculados continuam:

- consultáveis;
- editáveis;
- precificáveis;
- exibindo o nome da Categoria.

Uma Categoria inativa apenas deixa de ser opção para:

- novo Produto;
- nova atribuição em edição de Produto.

### Edição de Produto já vinculado a Categoria inativa

Se o Produto já possui Categoria inativa, o GET de edição deve continuar exibindo essa Categoria como opção selecionada.

O usuário pode:

- manter a Categoria inativa atual;
- remover a Categoria;
- trocar por uma Categoria ativa.

Não obrigar troca apenas porque o usuário editou Nome ou Margem-alvo.

Não permitir selecionar **outra** Categoria inativa por manipulação de request.

## Edição de Categoria inativa

Categoria inativa continua editável.

Renomear Categoria inativa:

- preserva `Ativo = false`;
- atualiza automaticamente o nome exibido nos Produtos vinculados, pois o Produto referencia a entidade;
- continua sujeito à unicidade.

Edição não reativa implicitamente.

## Associação com Produto

Evoluir `Produto` para:

~~~text
CategoriaProdutoId : int?
~~~

A associação continua opcional.

Produto sem Categoria permanece válido:

~~~text
CategoriaProdutoId = null
~~~

Não tornar Categoria obrigatória nesta UC.

A FK deve ser:

~~~text
Produtos.CategoriaProdutoId
-> CategoriasProdutos.Id
ON DELETE RESTRICT
~~~

Não usar cascade delete.

Não armazenar simultaneamente:

~~~text
Categoria string
+
CategoriaProdutoId
~~~

como modelo permanente.

Após migration/backfill bem-sucedidos, a coluna de texto livre deve ser removida.

## Domínio Produto

Substituir o estado:

~~~text
Categoria : string?
~~~

por:

~~~text
CategoriaProdutoId : int?
~~~

A criação/edição do Produto passa a receber a referência opcional.

Direção conceitual:

~~~csharp
Produto.Criar(
    empresaId,
    nome,
    margemAlvo,
    categoriaProdutoId)

produto.AtualizarDados(
    nome,
    margemAlvo,
    categoriaProdutoId)
~~~

É aceitável extrair método explícito de associação se isso deixar o domínio mais claro, desde que a atualização continue atômica.

Validação defensiva no domínio:

~~~text
CategoriaProdutoId null
=> válido

CategoriaProdutoId <= 0
=> inválido
~~~

O domínio `Produto` não deve consultar banco para validar tenant/Ativo.

Ownership e elegibilidade da referência pertencem à camada de persistência/Web.

## Integridade tenant-aware

Adicionar `CategoriaProduto` a:

- `DbSet`;
- Global Query Filter;
- guard central via `IEntidadeEmpresa`.

Adicionar validação sync/async da referência de Categoria dos Produtos alterados.

Regra:

~~~text
Produto.EmpresaId == CategoriaProduto.EmpresaId
~~~

Uma referência cross-tenant deve ser rejeitada mesmo se for fabricada fora da Web.

O guard de referência pode usar `IgnoreQueryFilters` internamente, seguindo o padrão já existente para validar ownership real.

Não permitir reatribuição de `EmpresaId` de Categoria.

## Persistência

Criar tabela:

~~~text
CategoriasProdutos
~~~

Campos:

~~~text
Id int identity PK
EmpresaId int NOT NULL
Nome nvarchar(80) NOT NULL
NomeNormalizado nvarchar(80) NOT NULL
Ativo bit NOT NULL
~~~

Índices:

~~~text
UNIQUE (EmpresaId, NomeNormalizado)
INDEX Produtos(CategoriaProdutoId)
~~~

FKs:

~~~text
CategoriasProdutos.EmpresaId -> Empresas.Id RESTRICT
Produtos.CategoriaProdutoId -> CategoriasProdutos.Id RESTRICT
~~~

## Migration e backfill

A migration da UC032 é evolutiva sobre o baseline SQL Server atual.

Não rebaselinear migrations.

A migration deve preservar os dados atuais de Produto.

### Sequência obrigatória

1. criar `CategoriasProdutos`;
2. adicionar `CategoriaProdutoId nullable` em `Produtos`;
3. ler logicamente os valores atuais não nulos de `Produtos.Categoria`;
4. criar uma Categoria por combinação normalizada distinta de:
   ~~~text
   EmpresaId + Categoria
   ~~~
5. marcar todas as Categorias migradas como ativas;
6. vincular cada Produto à Categoria correspondente;
7. criar FK/índices definitivos;
8. remover a coluna antiga `Produtos.Categoria`;
9. atualizar ModelSnapshot.

### Duplicidades legadas

Os Produtos atuais já normalizam whitespace, mas a migration deve considerar que textos com diferenças apenas de capitalização representam a mesma Categoria.

Exemplo:

~~~text
Produto A -> Agenda
Produto B -> AGENDA
Produto C -> agenda
~~~

Resultado:

~~~text
1 CategoriaProduto
NomeNormalizado = AGENDA
3 Produtos vinculados à mesma Categoria
~~~

Para o `Nome` de exibição em caso de variantes legadas, usar regra determinística:

~~~text
preservar o texto do Produto de menor Id dentro do grupo
~~~

Não criar múltiplas Categorias apenas para preservar capitalizações distintas.

### Null legado

~~~text
Produtos.Categoria = null
=> CategoriaProdutoId = null
~~~

Não criar Categoria artificial como:

~~~text
Sem categoria
Outros
Não informado
~~~

### Teste evolutivo obrigatório

Criar teste de migration que parta do estado imediatamente anterior à UC032, insira Produtos com:

- Categoria null;
- Categoria única;
- mesma Categoria em vários Produtos;
- variantes de capitalização na mesma Empresa;
- mesmo nome de Categoria em Empresas diferentes;

aplique a migration e confirme:

- Categorias corretas;
- vínculos corretos;
- ausência da coluna antiga;
- FK/índice únicos;
- zero migrations pendentes.

## Web — administração de Categorias

Criar área:

~~~text
/Produtos/Categorias
~~~

Rotas esperadas:

~~~text
/Produtos/Categorias
/Produtos/Categorias/Novo
/Produtos/Categorias/Editar/{id:int}
~~~

Não é necessária página Detalhes separada nesta UC.

### Listagem

Exibir:

~~~text
Nome
Situação
Ações
~~~

Listar Categorias ativas e inativas da Empresa Ativa.

Ordenar por:

~~~text
NomeNormalizado
~~~

Ações:

- Nova categoria;
- Editar;
- Desativar, quando ativa;
- Reativar, quando inativa.

Adicionar acesso claro a partir da área de Produtos.

### Cadastro

Campos:

~~~text
Nome
~~~

POST válido:

~~~text
criar CategoriaProduto ativa
-> SaveChangesAsync()
-> PRG /Produtos/Categorias
-> mensagem
~~~

Mensagem:

~~~text
Categoria de produto cadastrada com sucesso.
~~~

POST inválido:

- retorna 200;
- preserva Input;
- não persiste.

### Edição

Campo:

~~~text
Nome
~~~

POST válido:

- altera apenas Nome/NomeNormalizado;
- preserva Id;
- preserva EmpresaId;
- preserva Ativo;
- usa PRG;
- mensagem:

~~~text
Categoria de produto atualizada com sucesso.
~~~

### Desativar

Somente POST.

Mensagem:

~~~text
Categoria de produto desativada com sucesso.
~~~

Pode usar confirmação simples.

Não desvincular Produtos.

### Reativar

Somente POST.

Mensagem:

~~~text
Categoria de produto reativada com sucesso.
~~~

Não alterar Produtos.

## Produto Novo

Substituir input textual de Categoria por seletor.

Label:

~~~text
Categoria
~~~

Opções:

~~~text
Sem categoria
<categorias ativas da Empresa Ativa>
~~~

Ordenar opções por NomeNormalizado.

Não listar Categorias inativas.

POST:

- `CategoriaProdutoId` é opcional;
- id ausente => Produto sem Categoria;
- id informado deve existir no tenant ativo e estar ativo;
- request cross-tenant/inativo/manipulado => erro de validação;
- não aceitar Nome de Categoria pelo request.

Mensagem sugerida:

~~~text
Selecione uma categoria de produto válida.
~~~

POST inválido deve recarregar as opções sem perder os demais Inputs.

## Produto Editar

Substituir input textual pelo mesmo seletor.

No GET:

- listar todas as Categorias ativas;
- se a Categoria atual estiver inativa, incluí-la também como opção selecionada;
- Produto sem Categoria seleciona `Sem categoria`.

No POST:

- null continua válido;
- manter a mesma Categoria inativa atual é válido;
- trocar para Categoria ativa do tenant é válido;
- trocar para outra Categoria inativa é inválido;
- id cross-tenant/inexistente é inválido.

POST inválido deve preservar seleção e demais Inputs.

## Apresentação de Produto

Toda apresentação que hoje usa:

~~~text
produto.Categoria
~~~

deve passar a obter o nome por relacionamento/projeção da Categoria estruturada.

No mínimo revisar:

- listagem de Produtos;
- detalhes;
- edição;
- Ficha Técnica;
- detalhamento de precificação;
- registro de preço;
- histórico de precificação;
- demais projeções Web que exibam Categoria.

Produto sem Categoria continua exibindo:

~~~text
—
~~~

Não duplicar o Nome da Categoria novamente em Produto.

## Pesquisa e filtros

A pesquisa atual de Produtos por Nome permanece inalterada.

UC032 não adiciona:

- filtro por Categoria;
- busca por Categoria;
- agrupamento por Categoria;
- dashboard por Categoria.

Esses comportamentos podem ser adicionados por item posterior.

## Segurança HTTP

Todas as mutações de Categoria:

- usam POST;
- exigem antiforgery;
- não aceitam EmpresaId do request;
- não usam GET para mutar;
- preservam PRG.

GET cross-tenant de edição retorna 404.

POST cross-tenant retorna 404 e não altera dados.

## Relação com UC036 — desgaste de equipamentos

A UC036 será implementada imediatamente depois da UC032.

Ela adicionará à Categoria a configuração necessária para o componente:

~~~text
CustoDesgasteEquipamentosLote
~~~

Formas já decididas conceitualmente:

~~~text
Valor fixo por lote
Percentual sobre CustoBaseItens
~~~

Exemplos:

~~~text
Agenda
Forma = Valor fixo por lote
Valor = R$ 1,50
=> CustoDesgasteEquipamentosLote = R$ 1,50

Pães Rústicos
Forma = Percentual sobre insumos
Valor = 5%
CustoBaseItens = R$ 10,00
=> CustoDesgasteEquipamentosLote = R$ 0,50
~~~

**Nada disso deve ser implementado na UC032.**

Em especial, não criar antecipadamente:

- enum de forma de desgaste;
- valor/percentual de desgaste;
- calculadora de desgaste;
- alteração de `CalculadoraCustoProduto`;
- alteração de `PrecificacaoProdutoAtual`;
- alteração de snapshots comerciais.

A UC032 deve apenas entregar uma Categoria estruturada que possa ser evoluída pela UC036.

## Relação com MEL021

A ordem passa a ser:

~~~text
UC032
-> UC036
-> MEL021
~~~

MEL021 continua tecnicamente especificada, porém volta a ficar bloqueada até a conclusão da UC032/UC036 e pela indisponibilidade externa atual da conta Azure.

UC032 não implementa qualquer código Azure.

## Regras documentais a evoluir na implementação

No mínimo:

- F002 — Gestão de Produtos;
- RN043 em `business-rules.md`;
- UC007;
- UC008;
- UC009;
- UC010;
- catálogo de UCs;
- backlog.

RN043 deve deixar de afirmar que Categoria é texto livre e passar a registrar a Categoria estruturada opcional.

Documentos históricos podem manter o estado antigo quando estiver claramente identificado como histórico.

## Critérios de aceitação

- **CA01:** existe entidade tenant-owned `CategoriaProduto`.
- **CA02:** Nome é obrigatório e possui máximo 80 caracteres.
- **CA03:** Nome é normalizado com trim/whitespace e NomeNormalizado invariável.
- **CA04:** unicidade é EmpresaId + NomeNormalizado.
- **CA05:** nova Categoria nasce ativa.
- **CA06:** Categoria pode ser desativada e reativada de forma idempotente.
- **CA07:** Categoria inativa permanece existente e editável.
- **CA08:** não existe exclusão física.
- **CA09:** Produto passa a possuir `CategoriaProdutoId nullable`.
- **CA10:** Produto sem Categoria continua válido.
- **CA11:** Produto não mantém string de Categoria duplicada após migration.
- **CA12:** FK Produto -> Categoria usa Restrict.
- **CA13:** Categoria pertence à mesma Empresa do Produto.
- **CA14:** referência cross-tenant é rejeitada também pela persistência.
- **CA15:** migration preserva Categorias livres legadas.
- **CA16:** variantes de capitalização legadas colapsam deterministicamente.
- **CA17:** Categorias migradas nascem ativas.
- **CA18:** Produtos legados null permanecem sem Categoria.
- **CA19:** listagem de Categorias mostra ativas/inativas do tenant.
- **CA20:** cadastro válido usa PRG e mensagem de sucesso.
- **CA21:** duplicidade gera erro amigável.
- **CA22:** edição altera nome sem mudar Ativo.
- **CA23:** desativar Categoria não altera Produtos vinculados.
- **CA24:** reativar Categoria não altera Produtos vinculados.
- **CA25:** novo Produto lista somente Categorias ativas.
- **CA26:** novo Produto permite `Sem categoria`.
- **CA27:** edição permite preservar Categoria atual inativa.
- **CA28:** edição não permite trocar para outra Categoria inativa.
- **CA29:** Categoria manipulada/inexistente/cross-tenant não é aceita no Produto.
- **CA30:** listagem/detalhes/Ficha/precificação exibem o nome estruturado.
- **CA31:** Produto sem Categoria continua exibindo travessão/ausência coerente.
- **CA32:** Produto inativo continua podendo ser editado sem reativação.
- **CA33:** Categoria inativa não impede precificação de Produto já vinculado.
- **CA34:** antiforgery permanece obrigatório nas mutações.
- **CA35:** GET não muta dados.
- **CA36:** GQF/guard cobrem CategoriaProduto.
- **CA37:** nenhuma fórmula de custo é alterada.
- **CA38:** nenhum campo de desgaste é criado.
- **CA39:** nenhuma implementação Azure é antecipada.
- **CA40:** suíte unitária completa permanece verde.
- **CA41:** suíte de integração SQL Server/Web permanece verde.
- **CA42:** build Release permanece sem warnings novos relevantes.

## Matriz mínima de testes

### Unitários — CategoriaProduto

- **U1:** criação válida normaliza Nome e nasce ativa.
- **U2:** Nome vazio/whitespace é rejeitado.
- **U3:** Nome acima de 80 caracteres é rejeitado.
- **U4:** desativar/reativar são idempotentes.
- **U5:** renomear preserva Id, EmpresaId e Ativo.
- **U6:** renomeação inválida é atômica.
- **U7:** reatribuição de Empresa é rejeitada.

### Persistência / migration

- **P1:** migration cria tabela/FK/índices esperados.
- **P2:** backfill cria Categoria para texto legado.
- **P3:** vários Produtos com mesma Categoria compartilham uma entidade.
- **P4:** variantes de capitalização colapsam e usam nome do menor Produto.Id.
- **P5:** mesmo nome em Empresas diferentes cria Categorias distintas.
- **P6:** Categoria null permanece null.
- **P7:** coluna `Produtos.Categoria` deixa de existir.
- **P8:** índice único rejeita duplicidade no mesmo tenant.
- **P9:** GQF isola Categorias.
- **P10:** guard rejeita alteração cross-tenant.
- **P11:** Produto -> Categoria de outra Empresa é rejeitado.
- **P12:** FK Restrict impede remoção física acidental de Categoria referenciada.
- **P13:** migration deixa zero pendências.

### Web — Categorias

- **W1:** index lista apenas Categorias da Empresa Ativa, incluindo inativas.
- **W2:** cadastro válido persiste e usa PRG.
- **W3:** cadastro duplicado apresenta erro amigável.
- **W4:** cadastro inválido preserva Input.
- **W5:** edição válida renomeia e preserva situação.
- **W6:** edição cross-tenant retorna 404.
- **W7:** desativar persiste inatividade sem mexer em Produtos.
- **W8:** reativar persiste atividade.
- **W9:** POST sem antiforgery não altera estado.

### Web — Produto

- **W10:** Novo exibe Sem categoria + apenas Categorias ativas.
- **W11:** Novo com Categoria ativa persiste FK.
- **W12:** Novo sem Categoria persiste null.
- **W13:** Novo com id cross-tenant/inativo/inexistente é rejeitado.
- **W14:** Editar Produto com Categoria ativa mostra seleção correta.
- **W15:** Editar Produto com Categoria inativa atual preserva essa opção.
- **W16:** Editar pode manter Categoria inativa atual.
- **W17:** Editar pode remover Categoria.
- **W18:** Editar pode trocar para Categoria ativa.
- **W19:** Editar não pode trocar para outra Categoria inativa.
- **W20:** POST inválido recarrega opções e preserva Inputs.
- **W21:** Produto inativo mantém fluxo de edição da UC010.
- **W22:** listagem/detalhes/Ficha exibem Nome da Categoria.
- **W23:** Produto sem Categoria exibe ausência coerente.
- **W24:** troca de Empresa não expõe Categoria de outro tenant.
- **W25:** regressões de UC007–UC010 permanecem verdes.

## Arquivos esperados

Não exaustivo:

~~~text
src/Precificador.Core/Produtos/CategoriaProduto.cs
src/Precificador.Core/Produtos/Produto.cs

src/Precificador.Infrastructure/Persistence/Configurations/CategoriaProdutoConfiguration.cs
src/Precificador.Infrastructure/Persistence/Configurations/ProdutoConfiguration.cs
src/Precificador.Infrastructure/Persistence/PrecificadorDbContext.cs
src/Precificador.Infrastructure/Migrations/*

src/Precificador.Web/Pages/Produtos/Categorias/Index.cshtml
src/Precificador.Web/Pages/Produtos/Categorias/Index.cshtml.cs
src/Precificador.Web/Pages/Produtos/Categorias/Novo.cshtml
src/Precificador.Web/Pages/Produtos/Categorias/Novo.cshtml.cs
src/Precificador.Web/Pages/Produtos/Categorias/Editar.cshtml
src/Precificador.Web/Pages/Produtos/Categorias/Editar.cshtml.cs

src/Precificador.Web/Pages/Produtos/Novo.*
src/Precificador.Web/Pages/Produtos/Editar.*
src/Precificador.Web/Pages/Produtos/Index.*
src/Precificador.Web/Pages/Produtos/Detalhes.*
src/Precificador.Web/Pages/Produtos/ProdutoInputModel.cs
src/Precificador.Web/Pages/Produtos/ProdutoFormulario.cs

tests/Precificador.Tests.Unit/*
tests/Precificador.Tests.Integration/*
~~~

Revisar também todas as projeções que ainda referenciem `Produto.Categoria`.

## Fora do escopo

- desgaste de equipamentos — UC036;
- valor fixo/percentual de desgaste;
- qualquer alteração de cálculo;
- Categoria obrigatória;
- hierarquia Categoria/Subcategoria;
- Categoria compartilhada entre Empresas;
- descrição, cor, ícone ou imagem de Categoria;
- ordenação manual;
- exclusão física;
- filtro de Produtos por Categoria;
- dashboard por Categoria;
- Coleções — UC033/UC034;
- Azure — MEL021;
- API REST;
- importação/exportação de Categorias;
- auditoria/histórico de nome/status.

## Definition of Done específica

UC032 está concluída quando:

- Categoria livre foi substituída por `CategoriaProduto`;
- dados legados foram migrados sem perda de vínculo;
- Categoria possui CRUD administrativo sem hard delete;
- desativação/reativação é reversível;
- Produtos novos/editados usam seletor tenant-aware;
- Produto sem Categoria continua válido;
- Categoria inativa é preservada para Produtos já vinculados;
- cross-tenant é protegido na Web e persistência;
- todas as apresentações usam a Categoria estruturada;
- RN043/F002/UC007–UC010 estão coerentes;
- migration evolutiva foi testada;
- nenhuma regra de desgaste foi antecipada;
- nenhuma fórmula de precificação foi alterada;
- UC036 permanece próximo item;
- MEL021 permanece bloqueada até UC036 e resolução da conta Azure;
- build Release está verde;
- suíte unitária está verde;
- suíte integração SQL Server/Web está verde;
- backlog marca apenas UC032 como Concluído entre os itens desta nova sequência.

## Branch obrigatória para implementação

~~~text
feat/uc032-categorias-produto
~~~

## Commit sugerido

~~~text
feat: administra categorias de produto
~~~
