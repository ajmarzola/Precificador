# UC028 — Consultar resumo de margens da Empresa Ativa

- **Funcionalidade:** F005 — Dashboard de Margens
- **Dependências funcionais:** UC024
- **Dependências operacionais concluídas:** MEL015, MEL021 e MEL024
- **Estado:** Pronto
- **Alteração de schema:** não
- **Persistência de resultado:** não
- **Natureza:** consulta somente leitura
- **Regras principais:** RN017, RN022, RN023, RN026, RN027, RN036 e RN037

## Objetivo

Disponibilizar um **Dashboard** tenant-aware para a **Empresa Ativa**, permitindo acompanhar rapidamente a situação atual das margens dos Produtos ativos e identificar quantos deles estão abaixo da Margem-alvo, dentro da Margem-alvo ou sem Margem atual calculável.

A UC028 consolida em uma nova superfície os cálculos correntes já existentes. Ela não cria fórmula financeira nova, não usa snapshots históricos como estado atual e não persiste indicadores derivados.

## Superfície

Criar página:

~~~text
/Dashboard
~~~

Somente GET.

A página deve ser protegida pela policy já existente:

~~~text
EmpresaAtiva
~~~

Adicionar acesso **Dashboard** à navegação autenticada.

## Home pública x Home da Empresa

A UC028 estabelece duas noções distintas de Home:

~~~text
Home pública
= /
= usuário não autenticado

Home da Empresa
= /Dashboard
= usuário autenticado + Empresa Ativa
~~~

O **Dashboard é a Home da Empresa**. Depois que autenticação e Empresa Ativa estiverem resolvidas, qualquer navegação ou ação cujo destino semântico seja "Home", "Início" ou página inicial operacional deve apontar diretamente para `/Dashboard`, e não para `/`.

Isso inclui, no mínimo:

- login concluído sem `ReturnUrl`;
- seleção explícita de Empresa;
- acesso do usuário autenticado à ação/link **Início**;
- clique na marca **Precificador** quando autenticado;
- GET de `/Conta/Login` por usuário já autenticado com Empresa Ativa;
- qualquer redirect existente para `/Index` ou `/` cujo propósito seja voltar à Home operacional após autenticação.

Fluxo padrão:

~~~text
login concluído
+ Empresa Ativa resolvida
=> /Dashboard
~~~

Quando o usuário possui mais de uma Empresa:

~~~text
login
-> /Empresas/Selecionar
-> Empresa definida
-> /Dashboard
~~~

### ReturnUrl

`ReturnUrl` local e válido continua tendo precedência quando o login foi iniciado por tentativa de acesso a uma rota protegida.

~~~text
login com ReturnUrl local válido
=> ReturnUrl

login sem ReturnUrl
=> /Dashboard
~~~

Não substituir um `ReturnUrl` explícito pelo Dashboard.

### Acesso direto a /

A rota `/` continua sendo a Home pública.

Se for acessada por usuário autenticado com Empresa Ativa:

~~~text
GET /
=> redirect /Dashboard
~~~

Isso é fallback de coerência. Os links e redirects autenticados conhecidos devem apontar **diretamente** para `/Dashboard`, evitando navegação intermediária por `/`.

Se o usuário estiver autenticado sem Empresa Ativa, preservar o fluxo de seleção/resolução de Empresa existente; não conceder acesso ao Dashboard sem satisfazer a policy `EmpresaAtiva`.

### Logout

Logout encerra autenticação e limpa a Empresa Ativa. Seu destino deve ser a **Home pública**, não o Login e não o Dashboard:

~~~text
POST /Conta/Logout
-> sign out
-> limpar Empresa Ativa
-> /
~~~

Após o redirect, como o usuário já está deslogado, `/` deve renderizar a Home pública normalmente.

Não duplicar o conteúdo do Dashboard dentro de `/`.

## Universo do Dashboard

A UC028 considera exclusivamente:

~~~text
Produtos da Empresa Ativa
E
Produto.Ativo == true
~~~

Produtos inativos:

- não entram nos indicadores;
- não aparecem na listagem do Dashboard;
- continuam consultáveis/calculáveis nas superfícies de Produto existentes;
- não são alterados por esta UC.

O Dashboard não recebe `EmpresaId` por query string, form ou rota.

## Indicadores-resumo

Exibir, no mínimo, os seguintes indicadores:

~~~text
Produtos ativos
Insumos ativos
Abaixo da margem
Dentro da margem
Margem indisponível
~~~

### Produtos ativos

Quantidade de Produtos ativos da Empresa Ativa.

### Insumos ativos

Quantidade de Insumos ativos da Empresa Ativa.

É indicador operacional de contexto e não participa de nenhum cálculo de margem.

### Abaixo da margem

Quantidade de Produtos ativos cujo resultado atual da UC024 seja:

~~~text
SituacaoMargem = AbaixoDaMargem
~~~

### Dentro da margem

Quantidade de Produtos ativos cujo resultado atual da UC024 seja:

~~~text
SituacaoMargem = DentroDaMargem
~~~

### Margem indisponível

Quantidade de Produtos ativos cujo resultado atual da UC024 seja:

~~~text
SituacaoMargem = Incompleto
~~~

Na interface, preferir rótulo que deixe explícito o significado:

~~~text
Margem indisponível
~~~

ou equivalente.

Não rotular este indicador como **Precificação incompleta**.

Conforme RN017/RN027, `SituacaoMargem.Incompleto` significa especificamente que a Margem atual não pode ser calculada por ausência de Custo unitário atual ou Preço de prateleira atual. A completude global da precificação é conceito distinto e pertence à UC030.

### Invariante do resumo

Para os Produtos ativos da Empresa Ativa:

~~~text
ProdutosAtivos
=
AbaixoDaMargem
+ DentroDaMargem
+ MargemIndisponivel
~~~

Nenhum Produto ativo pode ser contado em mais de uma situação.

## Fonte dos valores atuais

O Dashboard usa exclusivamente estado corrente.

Para cada Produto ativo:

~~~text
CustoUnitarioProduto   => UC022 / estado atual
PrecoSugerido          => UC023 / estado atual
PrecoPrateleiraAtual   => UC012 / registro vigente
MargemAtual            => UC024
MargemAlvo             => Produto.MargemAlvo atual
SituacaoMargem         => UC024
~~~

Não usar como estado atual:

- `RegistroPrecoProduto.CustoReferencia`;
- `RegistroPrecoProduto.MargemReferencia`;
- `RegistroPrecoProduto.PrecoSugerido` histórico;
- outro snapshot comercial anterior.

O histórico continua preservado e consultável pelos UCs próprios, mas não alimenta os indicadores correntes do Dashboard.

## Listagem do Dashboard

Abaixo dos indicadores, listar todos os Produtos ativos da Empresa Ativa.

Colunas mínimas:

~~~text
Produto
Custo unitário atual
Preço de prateleira atual
Margem atual
Margem-alvo
Preço sugerido
Situação da margem
Consultar
~~~

Ordenação inicial:

~~~text
NomeNormalizado ASC
~~~

A UC028 não introduz filtro nem ordenação configurável.

A ação **Consultar** deve navegar para a superfície existente do Produto. Não criar edição inline no Dashboard.

## Situação da margem

Usar diretamente os estados da UC024:

~~~text
Incompleto
AbaixoDaMargem
DentroDaMargem
~~~

Rótulos de apresentação:

~~~text
Incompleto       => Margem indisponível
AbaixoDaMargem   => Abaixo da margem
DentroDaMargem   => Dentro da margem
~~~

Pode haver badge/ênfase visual Bootstrap, mas cor não pode ser a única forma de comunicar o estado.

Não criar novos estados nesta UC.

## Preço sugerido atual

A listagem deve apresentar o **Preço sugerido atual**, derivado pela UC023.

O valor deve usar o mesmo:

- Custo unitário atual;
- Produto.MargemAlvo atual;
- IncrementoComercial atual da Empresa.

Não usar `RegistroPrecoProduto.PrecoSugerido` histórico.

Sem Custo unitário atual:

~~~text
PrecoSugerido = null
~~~

Com custo conhecido e `IncrementoComercial = null`:

~~~text
PrecoTeorico pode ser conhecido
PrecoSugerido = null
~~~

Essa ausência de Preço sugerido **não** transforma automaticamente a `SituacaoMargem` em `Incompleto`.

Cenário obrigatório:

~~~text
CustoUnitarioProduto conhecido
PrecoPrateleiraAtual conhecido
IncrementoComercial null

=> MargemAtual calculada
=> SituacaoMargem = AbaixoDaMargem ou DentroDaMargem
=> PrecoSugerido = null
~~~

Isso preserva a independência definida por RN017 e RN027.

## Apresentação de null e zero

Preservar a distinção entre valor ausente e zero conhecido.

### Custo unitário

~~~text
null => indisponível
0    => R$ 0,00
~~~

### Preço de prateleira

~~~text
null => —
valor => moeda pt-BR
~~~

### Preço sugerido

~~~text
null => indisponível
0    => R$ 0,00, se matematicamente válido
~~~

### Margem atual

~~~text
null => indisponível
0    => 0%
negativa => preservar valor negativo
~~~

Margens seguem apresentação percentual pt-BR já usada pelo sistema. Valores monetários seguem MEL015.

Nenhum arredondamento de apresentação pode alterar a classificação da situação.

## Reuso do processamento em lote

A MEL024 criou:

~~~text
ResumoPrecificacaoProdutosAtual
~~~

A UC028 deve reutilizar esse processamento em lote para os Produtos ativos.

É proibido:

~~~text
foreach produto
    await PrecificacaoProdutoAtual.CalcularAsync(produto.Id)
~~~

O Dashboard deve executar no máximo uma chamada do serviço em lote para o conjunto de Produtos exibidos.

## Evolução de ResumoPrecificacaoProdutosAtual

Evoluir o contrato compartilhado para também disponibilizar:

~~~text
PrecoSugerido?
~~~

além dos campos atuais:

~~~text
ProdutoId
CustoUnitarioProduto?
PrecoPrateleiraAtual?
MargemAtual?
SituacaoMargem
~~~

Para calcular `PrecoSugerido`:

- reutilizar `CalculadoraPrecoProduto`;
- carregar `IncrementoComercial` junto à Configuração da Empresa;
- usar o mesmo Custo unitário e Margem-alvo já presentes na fotografia corrente;
- não copiar a fórmula do UC023 para PageModel ou Razor.

A evolução não pode alterar os resultados usados pela listagem `/Produtos`.

## Performance

O número de round-trips não pode crescer proporcionalmente ao número de Produtos.

Preservar a estratégia em lote da MEL024:

- `AsNoTracking` nas leituras;
- GQFs normais;
- carga por conjuntos;
- preços de Insumos vigentes em lote;
- preços atuais dos Produtos em lote;
- nenhuma orquestração individual em loop.

A consulta de quantidade de Insumos ativos pode ser executada separadamente.

Não adicionar paginação nesta UC.

## Multiempresa

Preservar FT002/RN036/RN037.

- Dashboard exige Empresa Ativa;
- Produtos, Insumos, Categorias, Fichas, Itens e preços continuam sob GQF;
- não usar `IgnoreQueryFilters` no runtime do Dashboard;
- não receber `EmpresaId` do request;
- troca de Empresa Ativa deve produzir outro conjunto de indicadores;
- nenhuma contagem ou linha de outra Empresa pode vazar.

## Leitura somente

GET `/Dashboard` não pode:

- criar ou alterar Produto;
- criar Ficha;
- criar configuração;
- registrar preço;
- atualizar Margem;
- persistir Custo/Preço sugerido/Situação;
- corrigir dados incompletos automaticamente.

Nenhuma migration é necessária.

## Estado vazio

Sem Produtos ativos:

- `Produtos ativos = 0`;
- `Abaixo da margem = 0`;
- `Dentro da margem = 0`;
- `Margem indisponível = 0`;
- manter a contagem real de Insumos ativos;
- mostrar mensagem clara de que não existem Produtos ativos para acompanhar;
- não tratar como erro.

Produtos inativos existentes não removem esse estado vazio.

## Relação com UC029

UC029 acrescentará o filtro/recorte operacional de Produtos abaixo da margem.

A UC028:

- mostra o total de Produtos abaixo da margem;
- mostra a situação de cada Produto ativo;
- **não** adiciona filtro `abaixo da margem`;
- **não** altera a URL com parâmetro de situação.

## Relação com UC030

UC030 identificará a **precificação globalmente incompleta**, conforme RN017 e as etapas necessárias do motor.

A UC028:

- usa `SituacaoMargem.Incompleto` apenas para Margem indisponível;
- não cria indicador global `PrecificacaoCompleta`;
- não lista motivos consolidados de incompletude;
- não cria filtro de precificação incompleta.

## Critérios de aceitação

- **CA01:** existe GET `/Dashboard`.
- **CA02:** Dashboard exige autenticação e Empresa Ativa.
- **CA03:** existe acesso Dashboard na navegação autenticada.
- **CA04:** `/Dashboard` é a Home da Empresa para usuário autenticado com Empresa Ativa.
- **CA04A:** Home pública `/` continua disponível para anônimos.
- **CA04B:** acessar `/` autenticado com Empresa Ativa redireciona para `/Dashboard`.
- **CA04C:** login sem ReturnUrl e com uma única Empresa termina em `/Dashboard`.
- **CA04D:** ReturnUrl local válido continua tendo precedência sobre o destino padrão.
- **CA04E:** seleção explícita de Empresa termina em `/Dashboard`.
- **CA04F:** links/ações autenticados de Início/Home, incluindo marca Precificador, apontam diretamente para `/Dashboard`.
- **CA04G:** GET de Login por usuário já autenticado com Empresa Ativa redireciona para `/Dashboard`.
- **CA04H:** Logout limpa autenticação/Empresa Ativa e retorna para a Home pública `/`.
- **CA05:** somente Produtos ativos entram no Dashboard.
- **CA06:** Produtos inativos não entram nos indicadores nem na lista.
- **CA07:** Produtos ativos conta apenas o tenant atual.
- **CA08:** Insumos ativos conta apenas o tenant atual.
- **CA09:** Abaixo da margem usa `SituacaoMargem.AbaixoDaMargem`.
- **CA10:** Dentro da margem usa `SituacaoMargem.DentroDaMargem`.
- **CA11:** Margem indisponível usa `SituacaoMargem.Incompleto`.
- **CA12:** a soma das três situações equivale a Produtos ativos.
- **CA13:** Custo unitário usa estado corrente.
- **CA14:** Preço de prateleira usa o registro atual da UC012.
- **CA15:** Margem atual reproduz UC024.
- **CA16:** Margem-alvo usa o Produto atual.
- **CA17:** Preço sugerido reproduz UC023 com configuração atual.
- **CA18:** Preço sugerido histórico não é usado como atual.
- **CA19:** snapshots históricos de custo/margem não alimentam o Dashboard.
- **CA20:** null e zero permanecem distintos.
- **CA21:** margem negativa é exibida e continua Abaixo da margem.
- **CA22:** igualdade com a meta continua Dentro da margem.
- **CA23:** preço ausente mantém Margem indisponível, sem inventar zero.
- **CA24:** custo ausente mantém Margem indisponível, mesmo com preço conhecido.
- **CA25:** `IncrementoComercial = null` pode deixar Preço sugerido indisponível sem invalidar Margem atual conhecida.
- **CA26:** listagem é ordenada por Nome normalizado.
- **CA27:** Consultar reutiliza superfície existente do Produto.
- **CA28:** não existe filtro por situação nesta UC.
- **CA29:** não existe indicador global de precificação incompleta nesta UC.
- **CA30:** `ResumoPrecificacaoProdutosAtual` é reutilizado em lote.
- **CA31:** não existe chamada de `PrecificacaoProdutoAtual` por Produto.
- **CA32:** preço sugerido é calculado por calculadora existente, sem fórmula duplicada.
- **CA33:** GQFs permanecem ativos.
- **CA34:** nenhum dado cross-tenant aparece ou é contado.
- **CA35:** GET não persiste alteração.
- **CA36:** nenhuma migration/ModelSnapshot é alterado.
- **CA37:** `/Produtos` preserva os resultados da MEL024.
- **CA38:** build Release fica verde.
- **CA39:** suíte completa fica verde.
- **CA40:** UC029 e UC030 não são antecipadas.

## Matriz mínima de testes

### Resumo em lote

- Produto completo mantém paridade de Custo, Preço atual, Margem e Situação com a orquestração individual;
- novo `PrecoSugerido` mantém paridade com UC023;
- Produto sem preço atual mantém Preço sugerido conhecido quando o custo/configuração permitem;
- `IncrementoComercial = null` retorna Preço sugerido null sem alterar Situação de margem calculável;
- custo incompleto retorna Preço sugerido null;
- margem negativa preservada;
- tenant externo não aparece;
- Produto inativo solicitado fora do Dashboard continua calculável pelo serviço sem mudar sua regra geral.

### Dashboard — indicadores

- nenhum Produto ativo;
- um Produto em cada situação;
- vários Produtos na mesma situação;
- inativo não contado;
- igualdade exata com Margem-alvo conta Dentro da margem;
- custo zero conhecido;
- preço ausente;
- custo ausente;
- Insumo ativo/inativo;
- Empresa B não altera contagens da Empresa A;
- invariante das situações.

### Dashboard — listagem

- somente Produtos ativos;
- ordenação por Nome;
- moeda pt-BR;
- margem percentual;
- null de custo;
- null de preço atual;
- null de Preço sugerido;
- margem negativa;
- rótulos das três situações;
- link Consultar;
- sem filtros/query string de situação;
- GET não persiste.

### Autorização, Home e navegação

- anônimo em `/` continua vendo a Home pública;
- login sem ReturnUrl + uma única Empresa => `/Dashboard`;
- login com ReturnUrl local válido => ReturnUrl preservado;
- seleção de Empresa => `/Dashboard`;
- acesso a `/` autenticado com Empresa Ativa => redirect `/Dashboard`;
- links autenticados de Início/Home e a marca Precificador => `/Dashboard`;
- GET de Login já autenticado com Empresa Ativa => `/Dashboard`;
- Logout => limpa sessão/Empresa Ativa e retorna para `/` deslogado;
- autenticado sem Empresa Ativa não recebe acesso operacional;
- Empresa Ativa A não lê dados de B.

## Arquivos esperados

~~~text
src/Precificador.Web/Precificacao/ResumoPrecificacaoProdutosAtual.cs
src/Precificador.Web/Pages/Dashboard/Index.cshtml.cs
src/Precificador.Web/Pages/Dashboard/Index.cshtml
src/Precificador.Web/Pages/Index.cshtml.cs
src/Precificador.Web/Pages/Conta/Login.cshtml.cs
src/Precificador.Web/Pages/Conta/Logout.cshtml.cs
src/Precificador.Web/Pages/Empresas/Selecionar.cshtml.cs
src/Precificador.Web/Pages/Shared/_Layout.cshtml
src/Precificador.Web/Program.cs
tests/Precificador.Tests.Integration/Infrastructure/*
tests/Precificador.Tests.Integration/Web/*
docs/features/F005-dashboard-margens.md
docs/development/backlog.md
docs/use-cases/catalog.md
~~~

A implementação pode ajustar nomes/organização interna se preservar o contrato funcional.

## Fora do escopo

- migration ou alteração de schema;
- persistir indicadores calculados;
- incluir Produtos inativos no Dashboard;
- filtro abaixo da margem — UC029;
- identificação/filtro de precificação globalmente incompleta — UC030;
- filtro por Categoria;
- pesquisa textual;
- ordenação configurável;
- paginação;
- gráficos;
- tendências históricas;
- comparação entre Empresas;
- exportação;
- alertas/notificações;
- edição inline;
- substituir a Home pública anônima por Dashboard;
- Continuous Deployment — MEL027;
- qualquer alteração Azure necessária apenas para publicar esta UC.

## Definition of Done

UC028 está concluída quando:

- Dashboard tenant-aware existe e exige Empresa Ativa;
- Dashboard é a Home da Empresa e o destino de toda navegação autenticada cujo propósito seja Início/Home;
- login/seleção de Empresa e GET de Login já autenticado convergem para o Dashboard, respeitando ReturnUrl local explícito;
- Logout retorna à Home pública deslogada;
- indicadores de Produtos/Insumos ativos e situações de margem estão corretos;
- a listagem mostra os valores correntes definidos;
- Preço sugerido atual é derivado sem usar snapshot histórico;
- `SituacaoMargem.Incompleto` não é confundido com completude global;
- apenas Produtos ativos participam do Dashboard;
- processamento multi-Produto permanece em lote, sem N+1;
- não há persistência nem migration;
- documentação está alinhada;
- build/testes completos estão verdes;
- backlog marca UC028 como Concluído;
- UC029 e UC030 continuam não implementadas.

## Branch de implementação sugerida

~~~text
feat/uc028-dashboard-margens
~~~

## Commit sugerido

~~~text
feat: adiciona dashboard de margens da empresa ativa
~~~
