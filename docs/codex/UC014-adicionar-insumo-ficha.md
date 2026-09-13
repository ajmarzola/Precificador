# Instrução Codex — UC014: Adicionar Insumo à Ficha Técnica

## Tarefa

Implementar integralmente a UC014 conforme `docs/use-cases/UC014-adicionar-insumo-ficha.md`, partindo da master pós-UC013.

Branch obrigatória:

~~~text
feat/uc014-adicionar-insumo-ficha
~~~

Nunca editar, commitar ou fazer push direto em `master`.

Não fazer merge da própria implementação.

## Leitura obrigatória

Antes da primeira alteração, ler:

- `AGENTS.md`;
- `docs/use-cases/UC014-adicionar-insumo-ficha.md`;
- `docs/use-cases/UC013-definir-base-ficha-tecnica.md`;
- `docs/features/F003-ficha-tecnica.md`;
- `docs/features/F001-insumos.md`;
- `docs/business/business-rules.md`;
- `docs/business/insumo-historical-stability.md`;
- `docs/product/multiempresa-generalizacao.md`;
- `docs/development/testing-strategy.md`;
- `docs/development/definition-of-done.md`;
- `docs/development/implementation-order.md`;
- implementação real de `FichaTecnica`, `Insumo`, `PrecificadorDbContext`, `/Produtos/FichaTecnica` e `/Insumos/Editar`;
- testes reais de Ficha Técnica e edição de Insumo.

Confirmar antes de editar:

1. branch atual é `feat/uc014-adicionar-insumo-ficha`;
2. branch parte da master que contém a PR #54;
3. UC013 está implementada;
4. UC014 está marcada como pronta para implementação;
5. não existe `ItemFichaTecnica` já implementado.

Se houver divergência material, interromper e reportar.

## Escopo

Implementar somente:

- entidade `ItemFichaTecnica : IEntidadeEmpresa`;
- `Id`, `EmpresaId`, `FichaTecnicaId`, `InsumoId`, `Quantidade`, `Observacao`;
- inclusão de Insumo ativo em Ficha existente;
- unicidade de um Insumo por Ficha;
- migration evolutiva;
- DbSet/configuração/GQF;
- guards tenant-aware Item->Ficha e Item->Insumo;
- página `/Produtos/FichaTecnica/{produtoId:int}/Itens/Novo`;
- ação **Adicionar insumo** na Ficha somente quando ela existir;
- aplicação da RN048 em `/Insumos/Editar/{id}`;
- testes definidos na UC014;
- documentação pós-implementação.

## Modelo de domínio

Referência:

~~~text
ItemFichaTecnica
- Id: int
- EmpresaId: int
- FichaTecnicaId: int
- InsumoId: int
- Quantidade: decimal
- Observacao: string?
~~~

Regras:

~~~text
EmpresaId > 0
FichaTecnicaId > 0
InsumoId > 0
Quantidade > 0
Observacao <= 1000
~~~

Observacao:

- trim externo;
- whitespace-only => null;
- preservar conteúdo interno/quebras.

Criar por factory, por exemplo:

~~~csharp
ItemFichaTecnica.Criar(...)
~~~

Não criar Unidade no Item.

Não calcular custo.

## Persistência

Criar `ItemFichaTecnicaConfiguration`.

Tabela:

~~~text
ItensFichaTecnica
~~~

Configuração:

~~~text
Quantidade decimal(18,6)
Observacao max 1000 nullable
~~~

FKs `Restrict`:

- Empresa;
- FichaTecnica;
- Insumo.

Índice único:

~~~text
(EmpresaId, FichaTecnicaId, InsumoId)
~~~

Adicionar DbSet e Global Query Filter.

### Guards

Seguir o padrão explícito já existente no `PrecificadorDbContext`.

Para Item adicionado/modificado, validar:

~~~text
Item.EmpresaId == FichaTecnica.EmpresaId
Item.EmpresaId == Insumo.EmpresaId
~~~

A verificação de referência pode usar `IgnoreQueryFilters` apenas dentro do guard de persistência, como já ocorre nos guards de referência existentes.

Não usar `IgnoreQueryFilters` no fluxo Web comum.

### Migration

Criar migration evolutiva:

~~~text
AddItensFichaTecnica
~~~

ou equivalente claro.

Não editar `AddFichasTecnicas` nem migrations históricas.

Não criar Itens retroativos.

## Quantidade na Web — obrigatório

Não usar binding direto de `decimal?` para Quantidade.

A UC013 mostrou que vírgula pode ser interpretada incorretamente em ambiente Linux/invariant.

Input:

~~~text
string? Quantidade
~~~

Criar helper equivalente a `FichaTecnicaFormulario`, por exemplo:

~~~text
ItemFichaTecnicaFormulario.TentarObterQuantidade(...)
~~~

Parsing:

- trim;
- vazio => `A quantidade é obrigatória.`;
- contém vírgula => pt-BR;
- sem vírgula => invariant;
- inválido => `A quantidade deve ser um número válido.`;
- <=0 => `A quantidade deve ser maior que zero.`.

Teste explicitamente `1,25 -> 1.25m` com cultura corrente invariant.

## Web — inclusão

Rota funcional:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Novo
~~~

Pode ser implementada com Razor Page em estrutura física compatível, usando rota explícita se necessário.

### InputModel

~~~text
int? InsumoId
string? Quantidade
string? Observacao
~~~

Não bindar:

- EmpresaId;
- FichaTecnicaId;
- ItemId.

ProdutoId vem da rota.

### GET

1. carregar Produto pelo GQF;
2. inexistente/cross-tenant => 404;
3. carregar Ficha pelo ProdutoId;
4. sem Ficha => redirect para `/Produtos/FichaTecnica/{produtoId}` com:
   `Defina a base da ficha técnica antes de adicionar insumos.`;
5. carregar apenas Insumos ativos do tenant;
6. ordenar por NomeNormalizado/MarcaNormalizada;
7. renderizar Produto, situação, Rendimento e Tempo ativo;
8. GET não muta.

Rótulo do select:

~~~text
Nome — Marca (unidade)
Nome (unidade) // sem marca
~~~

Usar `InsumoRotulos.Unidade`.

### POST

1. rebuscar Produto;
2. rebuscar Ficha;
3. validar InputModel/parsing;
4. rebuscar Insumo pelo GQF e `Ativo == true`;
5. inexistente/cross-tenant/inativo => erro:
   `O insumo selecionado não está disponível para inclusão na ficha técnica.`;
6. verificar duplicidade Ficha+Insumo;
7. duplicado =>:
   `Este insumo já foi adicionado à ficha técnica.`;
8. criar Item com EmpresaId/FichaId resolvidos pelo servidor;
9. salvar;
10. PRG para `/Produtos/FichaTecnica/{produtoId}`;
11. TempData:
   `Insumo adicionado à ficha técnica com sucesso.`.

Repopular select ao retornar Page por erro.

Produto inativo continua permitido.

Insumo sem preço continua permitido.

## Página existente da Ficha Técnica

A UC013 já consulta a Ficha no GET.

Adaptar o PageModel para expor estado somente leitura, por exemplo:

~~~text
PossuiFicha
~~~

Mostrar **Adicionar insumo** somente quando a Ficha estiver persistida.

Não criar Ficha no GET.

Não listar composição completa nesta tarefa.

## RN048 — edição de Insumo

Hoje `/Insumos/Editar/{id}` possui `PossuiHistorico`.

Evoluir conceitualmente para:

~~~text
PossuiHistoricoPreco
ReferenciadoEmFicha
IdentidadeProtegida =
    PossuiHistoricoPreco || ReferenciadoEmFicha
~~~

### GET

Se IdentidadeProtegida:

- Nome readonly;
- Marca readonly;
- Unidade base readonly/disabled;
- Categoria editável;
- Observacao global editável.

Mensagem:

- histórico de preço presente: preservar mensagem atual;
- somente referência em Ficha:
  `Nome, marca e unidade base não podem ser alterados porque este insumo está sendo usado em ficha técnica.`.

Se ambas existirem, pode priorizar a mensagem de histórico.

### POST

Rebuscar dependências no servidor.

Se IdentidadeProtegida:

- sobrescrever Input.Nome/Marca/UnidadeBase com valores persistidos antes da validação/domínio;
- ignorar tentativa manipulada;
- Categoria/Observacao continuam editáveis.

Não alterar RN040.

## Não implementar

Não implementar:

- UC015;
- edição de Item;
- UC016;
- remoção de Item;
- UC017;
- listagem detalhada da composição;
- troca de Insumo;
- custo do Item;
- preço/custo persistido no Item;
- perdas;
- equipamentos;
- conversão de unidades;
- histórico/versionamento de Ficha/Item;
- API;
- CQRS/MediatR;
- repository genérico;
- Unit of Work customizado;
- refatorações oportunistas.

## Testes obrigatórios

Revalidar e implementar a matriz da UC014:

### Unitários U1–U5

Cobrir criação, quantidade, observação, ids e ausência de Unidade própria.

### Persistência P1–P7

Cobrir:

- migration evolutiva;
- unicidade;
- mesmo Insumo em Fichas diferentes;
- GQF;
- guard Item->Ficha;
- guard Item->Insumo;
- FKs Restrict.

### Web W1–W19

Cobrir integralmente a matriz normativa, incluindo:

- autenticação/Empresa Ativa;
- Ficha ausente;
- somente Insumos ativos do tenant;
- Quantidade decimal com vírgula;
- duplicidade;
- Insumo inativo;
- Produto inativo;
- cross-tenant;
- request manipulado;
- RN048 GET/POST;
- RN040 sem regressão;
- Categoria/Observacao editáveis;
- independência da Observacao contextual;
- GET sem mutação;
- antiforgery;
- parsing `1,25 -> 1.25m` independente da cultura corrente.

Não enfraquecer testes existentes da UC013/UC005.

## Validação obrigatória

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Confirmar 0 warnings novos relevantes.

## Documentação pós-implementação

Atualizar:

- UC014 para Implementado;
- F003;
- F001 se necessário;
- catálogo;
- ordem de implementação.

UC015 passa a próximo caso **somente para revalidação pós-UC014**, conforme especificação já existente.

Não implementar UC015 nesta branch.

## Retorno obrigatório

Informar:

1. branch usada;
2. resumo da implementação;
3. migration criada;
4. testes adicionados/alterados;
5. resultado de build/test;
6. arquivos alterados;
7. divergências;
8. URL/estado da PR.

Commit sugerido:

~~~text
feat: adiciona insumo a ficha tecnica
~~~

Não fazer merge em master.
