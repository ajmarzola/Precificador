# MEL019 — Preencher Rendimento da Ficha Técnica com padrão 1

- **Tipo:** UX / default de formulário
- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Prioridade:** baixa
- **Estado:** Concluído
- **Ordem na fila pendente:** 04
- **Dependência técnica:** UC013
- **Gate operacional para ficar Pronto:** MEL018 concluída, conforme fila normativa
- **Alteração de schema:** não
- **Migration:** não
- **Alteração de domínio:** não

## Objetivo

Quando um Produto ainda **não possuir Ficha Técnica**, abrir a página:

~~~text
/Produtos/FichaTecnica/{id}
~~~

com:

~~~text
Rendimento do lote = 1
~~~

como valor inicial do formulário.

O valor é apenas um **default de entrada** para reduzir atrito no primeiro cadastro.

Não representa Ficha existente, não é persistido no GET e não modifica a regra RN009.

## Problema

Hoje, para Produto sem Ficha, o campo:

~~~text
Rendimento do lote (unidades de venda)
~~~

abre vazio.

Como o Rendimento é obrigatório e `1` é uma base neutra e comum para um Produto cuja Ficha representa uma unidade/lote simples, o usuário precisa preencher manualmente um valor que frequentemente será `1`.

MEL019 melhora apenas esse primeiro preenchimento.

## Regra principal

No GET:

~~~text
Produto possui Ficha?
    sim -> carregar Rendimento persistido
    não -> preencher Input.Rendimento = "1"
~~~

A decisão deve ocorrer depois de consultar o estado real da Ficha.

Solução esperada, ou equivalente:

~~~csharp
var ficha = await CarregarEstadoFichaAsync(id);

if (ficha is null)
{
    Input.Rendimento = FichaTecnicaFormulario.FormatarRendimento(1m);
}
else
{
    Input = new FichaTecnicaInputModel
    {
        Rendimento = FichaTecnicaFormulario.FormatarRendimento(ficha.Rendimento),
        TempoAtivoMinutos = ficha.TempoAtivoMinutos
    };
}
~~~

Não é obrigatório substituir todo o `Input`; a implementação pode atribuir somente `Input.Rendimento`.

## Default é somente apresentação inicial

Ao executar GET para Produto sem Ficha:

~~~text
Input.Rendimento = "1"
Input.TempoAtivoMinutos = vazio
PossuiFicha = false
RendimentoAtual = null
~~~

### Importante

Não definir:

~~~text
RendimentoAtual = 1
PossuiFicha = true
TempoAtivoMinutos = 0
~~~

Esses valores possuem semântica diferente.

- `Input.Rendimento = "1"` = sugestão visual;
- `RendimentoAtual` = valor de Ficha persistida;
- `PossuiFicha` = existência real da entidade;
- `TempoAtivoMinutos = 0` é valor funcional explícito, não default implícito.

## Não persistir no GET

O GET deve continuar sendo somente leitura.

Abrir a página não pode:

- criar `FichaTecnica`;
- chamar `SaveChanges`;
- materializar Ficha com Rendimento 1;
- criar Itens;
- criar equipamentos;
- alterar Produto;
- alterar precificação.

Exemplo:

~~~text
antes do GET: 0 Fichas para Produto 123
GET /Produtos/FichaTecnica/123
depois do GET: 0 Fichas para Produto 123
~~~

Repetir o GET também não pode criar registros.

## Produto com Ficha existente

Se já existir:

~~~text
FichaTecnica.Rendimento = 3.5
FichaTecnica.TempoAtivoMinutos = 75
~~~

o formulário deve continuar exibindo:

~~~text
Rendimento = 3,5
Tempo ativo = 75
~~~

Nunca substituir o valor persistido por `1`.

MEL019 atua **somente quando não existe Ficha**.

## POST

MEL019 não altera a validação de POST.

### Usuário aceita o default

Se o GET apresentou:

~~~text
Rendimento = 1
~~~

e o usuário informa:

~~~text
TempoAtivoMinutos = 30
~~~

salvando o formulário sem alterar o Rendimento, o POST normal deve criar:

~~~text
Rendimento = 1m
TempoAtivoMinutos = 30
~~~

Isso acontece pelo fluxo UC013 já existente.

Não adicionar tratamento especial para o número 1.

### Usuário altera o default

Exemplo:

~~~text
Rendimento = 2,5
TempoAtivoMinutos = 45
~~~

deve persistir:

~~~text
Rendimento = 2.5m
TempoAtivoMinutos = 45
~~~

Preservar `DecimalInputParser` e a formatação pt-BR existente.

## POST com Rendimento vazio

Este é um cenário obrigatório de regressão.

Se o usuário apagar o valor `1` e enviar:

~~~text
Input.Rendimento = vazio
~~~

o comportamento continua:

~~~text
O rendimento é obrigatório.
~~~

O servidor **não deve reaplicar o default no POST**.

Não fazer:

~~~csharp
if (string.IsNullOrWhiteSpace(Input.Rendimento))
    Input.Rendimento = "1";
~~~

em `OnPostAsync`.

O valor padrão é conveniência do GET, não fallback de domínio.

## POST inválido com outro erro

Se o usuário alterou o Rendimento e o POST falhar por outro motivo, o valor digitado deve permanecer.

Exemplo:

~~~text
GET inicial -> Rendimento = 1

usuário altera:
Rendimento = 2,5
Tempo ativo = -1

POST inválido
~~~

A página deve continuar mostrando:

~~~text
Rendimento = 2,5
~~~

e não voltar para `1`.

O `OnPostAsync` atual já trabalha sobre o `Input` postado. MEL019 não deve reexecutar lógica de default durante POST inválido.

## Tempo ativo

MEL019 não define default para:

~~~text
TempoAtivoMinutos
~~~

Para Produto sem Ficha, o campo continua vazio.

Motivo:

- `0` é um valor funcional válido segundo UC013/RN013;
- vazio significa não informado;
- preencher `0` automaticamente confundiria ausência com decisão explícita.

O usuário continua obrigado a informar o campo antes de salvar.

## Produto ativo ou inativo

O default vale igualmente para:

- Produto ativo;
- Produto inativo.

UC013 já permite criar/alterar Ficha de Produto inativo.

MEL019 não altera essa regra e não reativa Produto.

## Estado de precificação

Produto sem Ficha continua com precificação incompleta.

Exibir `1` no input não deve fazer qualquer calculadora tratar o Produto como se tivesse Ficha.

Portanto, antes do POST válido:

- `PrecificacaoProdutoAtual` continua sem Ficha;
- custo do Produto continua indisponível;
- preço teórico/sugerido continuam conforme contratos atuais de incompletude;
- nenhum valor é propagado para UC018–UC025.

A existência de Ficha continua determinada exclusivamente pela persistência.

## PossuiFicha e ações condicionais

Enquanto não existir Ficha:

~~~text
PossuiFicha = false
~~~

Logo, continuam indisponíveis as ações que dependem da Ficha persistida, como:

- Adicionar insumo;
- Adicionar equipamento.

MEL019 não deve liberar essas ações apenas porque o input mostra Rendimento 1.

Após POST válido e PRG, a Ficha passa a existir e o comportamento atual é mantido.

## RendimentoAtual

`RendimentoAtual` representa o valor persistido da Ficha usado nas seções de custo.

Para Produto sem Ficha:

~~~text
RendimentoAtual = null
~~~

mesmo que:

~~~text
Input.Rendimento = "1"
~~~

Não usar o default visual em cálculos.

## Formatação

Para gerar o valor inicial, reutilizar:

~~~csharp
FichaTecnicaFormulario.FormatarRendimento(1m)
~~~

Resultado esperado em pt-BR:

~~~text
1
~~~

Não hardcodar `"1,0"`, `"1.00"` ou formatação distinta.

Isso mantém consistência com Fichas existentes.

## Parsing

Não alterar:

~~~csharp
FichaTecnicaFormulario.TentarObterRendimento
~~~

Preservar:

- vírgula decimal;
- ponto decimal conforme `DecimalInputParser`;
- obrigatório;
- `Rendimento > 0`;
- texto inválido;
- precisão decimal.

Não introduzir `decimal` binding direto.

## Multiempresa

Nenhuma mudança.

Preservar:

- Produto carregado tenant-aware;
- Ficha carregada pelo contexto da Empresa Ativa;
- cross-tenant 404;
- GQF;
- write guards;
- nenhum `EmpresaId` no formulário;
- nenhum `IgnoreQueryFilters` novo no fluxo Web.

O default `1` independe da Empresa e não deve exigir configuração.

## Persistência

Nenhuma alteração de schema.

Não criar migration.

Não alterar:

- `FichaTecnica`;
- `Produto`;
- `PrecificadorDbContext`;
- configurações EF;
- ModelSnapshot;
- índices/FKs.

## Relação com MEL018

MEL018 antecede MEL019 na fila normativa.

Não há dependência técnica entre o redirect pós-cadastro e o default do Rendimento, mas MEL019 só deve ser promovida para **Pronto** depois de MEL018 estar **Concluída**.

A especificação pode existir antecipadamente; a implementação não deve furar a fila.

## Relação com UC013

MEL019 evolui apenas o comportamento inicial de apresentação de UC013.

Permanecem válidos:

~~~text
Rendimento > 0
TempoAtivoMinutos >= 0
Tempo ativo obrigatório
Produto pode existir sem Ficha
uma Ficha por Produto
Ficha atual editável
~~~

MEL019 não altera RN009/RN013.

## Relação com UC017+

Os fluxos posteriores continuam dependendo de **Ficha persistida**, nunca do valor visual do formulário.

Não alterar:

- inclusão/edição/remoção de Item;
- perdas;
- equipamentos;
- custos;
- precificação;
- histórico comercial.

## Documentação normativa

Alinhar:

- MEL019;
- UC013;
- F003;
- UC017, apenas se necessário para explicitar que Produto sem Ficha continua sem persistência;
- backlog.

Não é necessário alterar regras de negócio porque RN009 continua idêntica.

## Critérios de aceitação

- **CA01:** Produto sem Ficha abre `/Produtos/FichaTecnica/{id}` com HTTP 200.
- **CA02:** Input de Rendimento inicia com valor `1`.
- **CA03:** Tempo ativo continua vazio.
- **CA04:** GET não cria `FichaTecnica`.
- **CA05:** GET repetido não cria `FichaTecnica`.
- **CA06:** `PossuiFicha` continua falso para Produto sem Ficha.
- **CA07:** ações dependentes de Ficha persistida continuam ocultas antes do primeiro save.
- **CA08:** `RendimentoAtual` continua null para Produto sem Ficha.
- **CA09:** precificação continua incompleta antes do POST.
- **CA10:** Produto com Ficha existente exibe seu Rendimento persistido, não `1`.
- **CA11:** Produto com Ficha existente preserva TempoAtivoMinutos.
- **CA12:** salvar sem alterar o default persiste `Rendimento = 1m`.
- **CA13:** usuário pode substituir `1` por outro decimal válido.
- **CA14:** `2,5` continua persistindo `2.5m`.
- **CA15:** Rendimento vazio no POST continua inválido.
- **CA16:** zero continua inválido.
- **CA17:** negativo continua inválido.
- **CA18:** POST inválido não reaplica `1` sobre valor digitado pelo usuário.
- **CA19:** Tempo ativo vazio continua inválido.
- **CA20:** Tempo ativo zero explicitamente informado continua válido.
- **CA21:** Produto inativo sem Ficha também recebe default visual `1`.
- **CA22:** criar Ficha para Produto inativo não reativa Produto.
- **CA23:** cross-tenant continua 404.
- **CA24:** nenhuma migration/schema/model snapshot é alterado.
- **CA25:** nenhuma fórmula/cálculo é alterado.
- **CA26:** nenhuma Ficha/Item/equipamento é criado automaticamente.
- **CA27:** `FichaTecnicaFormulario.FormatarRendimento` é reutilizado ou comportamento equivalente é mantido.
- **CA28:** Itens posteriores não são antecipados.

## Matriz de testes

Priorizar `FichaTecnicaPageTests`.

### GET sem Ficha

#### W1 — Default 1

Criar Produto sem Ficha.

GET:

~~~text
/Produtos/FichaTecnica/{id}
~~~

Validar:

~~~text
ValorDoInput(html, "Input.Rendimento") == "1"
~~~

Não basta testar `Assert.Contains("1")` no HTML inteiro.

#### W2 — Tempo ativo permanece vazio

Validar:

~~~text
ValorDoInput(html, "Input.TempoAtivoMinutos") == ""
~~~

#### W3 — GET não persiste

Antes e depois do GET:

~~~text
ListarFichasAsync(produtoId: id)
~~~

continua vazio.

#### W4 — GET repetido não persiste

Executar dois GETs e confirmar zero Fichas.

#### W5 — Estado não simula Ficha existente

Na página de Produto sem Ficha:

- não exibe `Adicionar insumo`;
- não exibe `Adicionar equipamento`;
- não exibe blocos condicionados por `PossuiFicha` como se houvesse Ficha.

Pode reaproveitar asserts já existentes.

### Ficha existente

#### W6 — Rendimento persistido prevalece

Criar:

~~~text
Rendimento = 3.5m
TempoAtivoMinutos = 75
~~~

GET deve exibir:

~~~text
Rendimento = 3,5
TempoAtivo = 75
~~~

Não aceitar `1`.

### Primeiro save

#### W7 — Aceitar default

Fluxo real:

1. GET sem Ficha;
2. extrair token;
3. obter o valor de `Input.Rendimento` do HTML;
4. POST esse mesmo valor + TempoAtivo `30`.

Persistência:

~~~text
Rendimento = 1m
TempoAtivoMinutos = 30
~~~

Isso prova que o default renderizado é compatível com o parser.

#### W8 — Sobrescrever default

GET sem Ficha e POST:

~~~text
Rendimento = "2,5"
TempoAtivoMinutos = "45"
~~~

Persistir:

~~~text
2.5m / 45
~~~

### Validação

#### W9 — Usuário apaga Rendimento

Após obter token por GET, POST sem `Input.Rendimento`.

Validar:

- HTTP 200;
- mensagem `O rendimento é obrigatório.`;
- nenhuma Ficha criada.

#### W10 — Zero/negativo

Preservar cobertura existente.

#### W11 — POST inválido preserva edição do usuário

Exemplo:

~~~text
Input.Rendimento = "2,5"
Input.TempoAtivoMinutos = "-1"
~~~

Validar:

~~~text
ValorDoInput(html, "Input.Rendimento") == "2,5"
~~~

e erro de tempo ativo.

Não permitir que o valor volte a `1`.

#### W12 — Tempo ativo vazio

Preservar obrigatório e não persistir.

#### W13 — Tempo ativo zero

POST `Rendimento = 1`, `Tempo = 0` deve continuar válido.

### Produto inativo

#### W14

Produto inativo sem Ficha:

- GET 200;
- Rendimento `1`;
- Produto continua inativo;
- GET não cria Ficha.

### Tenant

#### W15

Produto de outro tenant:

~~~text
GET /Produtos/FichaTecnica/{idOutroTenant}
=> 404
~~~

Nenhuma Ficha criada.

### Regressão de precificação

#### W16

Produto sem Ficha após GET continua sem Ficha persistida e sem estado calculável artificial.

É suficiente confirmar persistência vazia e manter testes de `FichaTecnicaCustoPageTests` verdes; não duplicar toda a suíte de precificação.

## Implementação esperada

Mudança mínima em:

~~~text
src/Precificador.Web/Pages/Produtos/FichaTecnica.cshtml.cs
tests/Precificador.Tests.Integration/Web/FichaTecnicaPageTests.cs
~~~

Possível alteração:

~~~csharp
var ficha = await CarregarEstadoFichaAsync(id);

if (ficha is null)
{
    Input.Rendimento = FichaTecnicaFormulario.FormatarRendimento(1m);
}
else
{
    ...
}
~~~

Não criar novo serviço/helper para um único default constante.

Não alterar Razor se o `asp-for` já renderizar corretamente o valor de `Input.Rendimento`.

## Testes unitários

Nenhum novo teste unitário é obrigatório se nenhuma regra/domínio/helper for alterado.

`FichaTecnicaFormulario` já possui comportamento de parsing e formatação consolidado.

Adicionar unitário somente se a implementação introduzir lógica reutilizável real — o que não é esperado.

## Fora do escopo

- default de Tempo ativo;
- criar Ficha automaticamente;
- salvar automaticamente;
- wizard;
- cadastro de Item junto com a Ficha;
- cadastro de equipamento junto com a Ficha;
- alterar Rendimento padrão de Ficha existente;
- alterar RN009/RN013;
- alterar custo/precificação;
- migration;
- configuração por Empresa do default;
- preferência por Produto;
- MEL017;
- MEL012;
- UC028+.

## Definition of Done específica

MEL019 está concluída quando:

- Produto sem Ficha abre com Rendimento `1`;
- Tempo ativo continua vazio;
- GET não persiste nada;
- estado da UI continua distinguindo default de Ficha existente;
- Ficha existente preserva Rendimento real;
- aceitar o default salva `1m`;
- sobrescrever o default salva o valor informado;
- POST vazio/zero/negativo continua inválido;
- POST inválido não reaplica o default;
- Produto inativo recebe o mesmo default sem reativação;
- cross-tenant permanece 404;
- nenhuma migration é criada;
- nenhuma fórmula/domínio é alterado;
- matriz W1–W16 está coberta ou comprovada por regressão existente;
- build Release tem 0 erros e sem warnings novos relevantes;
- suíte completa está verde;
- backlog altera MEL019 de `Especificado` para `Pronto` somente após MEL018 estar Concluída; na PR de implementação, `Pronto -> Concluído`.

## Branch sugerida para implementação

~~~text
fix/mel019-rendimento-padrao-ficha
~~~

## Commit sugerido

~~~text
fix: preenche rendimento inicial da ficha com um
~~~
