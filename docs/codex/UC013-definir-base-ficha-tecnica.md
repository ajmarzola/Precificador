# Instrução Codex — UC013: Definir rendimento e tempo ativo da Ficha Técnica

## Tarefa

Implementar integralmente a UC013 conforme `docs/use-cases/UC013-definir-base-ficha-tecnica.md`, partindo da master que contém a PR documental desta UC.

Branch obrigatória:

~~~text
feat/uc013-base-ficha-tecnica
~~~

Nunca editar, commitar ou fazer push direto em `master`.

Não fazer merge da própria implementação.

## Leitura obrigatória antes de alterar código

Ler:

- `AGENTS.md`;
- `docs/use-cases/UC013-definir-base-ficha-tecnica.md`;
- `docs/features/F003-ficha-tecnica.md`;
- `docs/features/F002-produtos.md`;
- `docs/product/multiempresa-generalizacao.md`;
- `docs/business/business-rules.md`;
- `docs/business/pricing-model.md`;
- `docs/development/testing-strategy.md`;
- `docs/development/definition-of-done.md`;
- `docs/development/implementation-order.md`;
- implementação real de Produto, PrecificadorDbContext, configurações EF e testes Web atuais.

Confirmar antes da primeira edição:

1. branch atual é `feat/uc013-base-ficha-tecnica`;
2. branch parte da master esperada;
3. UC010 está implementada;
4. não existe FichaTecnica já implementada;
5. não existem campos temporários de TempoForno/PotenciaForno no Produto.

Se houver divergência material, reportar antes de ampliar escopo.

## Escopo

Implementar somente:

- entidade `FichaTecnica : IEntidadeEmpresa`;
- `Id`, `EmpresaId`, `ProdutoId`, `Rendimento`, `TempoAtivoMinutos`;
- uma Ficha atual por Produto;
- criação e atualização da base;
- migration evolutiva;
- DbSet/configuração/GQF/guard de referência Produto-tenant;
- página `/Produtos/FichaTecnica/{id:int}`;
- navegação a partir de Detalhes;
- testes U1-U6, P1-P6 e W1-W12;
- atualização documental pós-implementação.

## Modelo

Referência:

~~~text
FichaTecnica
- Id: int
- EmpresaId: int
- ProdutoId: int
- Rendimento: decimal
- TempoAtivoMinutos: int
~~~

Regras:

~~~text
Rendimento > 0
TempoAtivoMinutos >= 0
~~~

Tempo ativo é obrigatório no formulário; zero explícito é válido.

Rendimento deve permanecer decimal.

Uma Ficha por `(EmpresaId, ProdutoId)`.

EmpresaId e ProdutoId não podem ser reatribuídos após criação.

## Domínio

Implementar factory e atualização explícita, por exemplo:

~~~csharp
FichaTecnica.Criar(...)
ficha.AtualizarBase(...)
~~~

A atualização deve validar todos os candidatos antes de atribuir qualquer propriedade.

Não colocar regra de UI ou EF no Core.

## Persistência

Criar `FichaTecnicaConfiguration`.

Tabela:

~~~text
FichasTecnicas
~~~

Precisão de Rendimento:

~~~text
decimal(18,6)
~~~

FK Empresa e Produto com `DeleteBehavior.Restrict`.

Índice único:

~~~text
(EmpresaId, ProdutoId)
~~~

Adicionar DbSet e Global Query Filter.

O guard central por IEntidadeEmpresa cobre ownership, mas deve existir proteção adicional para impedir Ficha de uma Empresa referenciar Produto de outra Empresa. Seguir o padrão já usado para referências tenant-aware sem criar repository genérico ou camada artificial.

Migration sugerida:

~~~text
AddFichasTecnicas
~~~

Não editar migrations históricas.

## Web

Página única:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

O id é ProdutoId.

GET:

- carrega Produto pelo GQF;
- 404 para inexistente/cross-tenant;
- mostra Nome/Categoria/Situação;
- se Ficha existe, popula Rendimento e TempoAtivoMinutos;
- se não existe, mostra formulário vazio;
- nunca cria Ficha.

POST:

- rebusca Produto pelo GQF;
- 404 para inexistente/cross-tenant;
- valida input;
- busca Ficha existente;
- cria ou atualiza;
- request não controla EmpresaId/ProdutoId/Id;
- salva;
- TempData:
  `Ficha técnica salva com sucesso.`;
- PRG para a mesma página.

Adicionar em Detalhes:

~~~text
Ficha técnica
~~~

para Produto ativo e inativo.

Produto inativo pode criar/atualizar Ficha e permanece inativo.

## Não implementar

Não implementar nesta tarefa:

- item de Ficha;
- InsumoId na Ficha;
- quantidade de Insumo;
- observação contextual;
- perda;
- TempoForno;
- PotenciaFornoKw;
- Equipamento;
- uso de Equipamento;
- energia;
- cálculo de mão de obra;
- custo do lote;
- custo unitário;
- Preço sugerido;
- Preço de prateleira;
- versão/histórico de Ficha;
- exclusão de Ficha;
- UC014+;
- API;
- CQRS/MediatR;
- repository genérico;
- Unit of Work customizado;
- refatoração oportunista.

## Testes obrigatórios

Implementar exatamente a matriz fechada na UC013:

### Unitários

- U1 criação válida;
- U2 rendimento inválido;
- U3 tempo zero/negativo;
- U4 atualização preserva vínculo;
- U5 atomicidade;
- U6 ids positivos.

### Persistência

- P1 migration evolutiva;
- P2 unicidade uma Ficha/Produto;
- P3 query filter;
- P4 guard cross-tenant Produto/Ficha;
- P5 FKs Restrict;
- P6 round-trip de atualização.

### Web

- W1 autenticação/Empresa Ativa;
- W2 GET sem Ficha não cria;
- W3 POST cria + PRG;
- W4 GET existente;
- W5 POST atualiza mesmo Id;
- W6 inválidos;
- W7 inexistente;
- W8 cross-tenant;
- W9 request não controla ids/ownership;
- W10 Produto inativo;
- W11 navegação;
- W12 antiforgery.

Não enfraquecer GQF, antiforgery ou guard de tenant para facilitar testes.

## Validação obrigatória

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Confirmar ausência de warnings novos relevantes.

## Documentação pós-implementação

Atualizar:

- UC013 para Implementado;
- F003;
- catálogo;
- ordem de implementação;
- regras somente se a implementação real exigir ajuste fiel.

UC014 deve passar a próximo caso.

Não alterar ordem posterior sem nova decisão.

## Retorno obrigatório

Informar:

1. branch usada;
2. resumo da implementação;
3. migrations criadas;
4. testes adicionados;
5. resultado de build/test;
6. arquivos alterados;
7. divergências, se houver;
8. URL/estado da PR criada.

Commit sugerido:

~~~text
feat: define base da ficha tecnica
~~~

Não fazer merge em master.
