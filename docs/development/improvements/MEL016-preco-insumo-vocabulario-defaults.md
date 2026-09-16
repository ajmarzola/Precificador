# MEL016 — Corrigir semântica e defaults do registro de preço do Insumo

- **Tipo:** correção conceitual + UX de formulário
- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Prioridade:** média
- **Estado:** Pronto
- **Ordem na fila pendente:** 02
- **Dependências:** MEL015, UC005, UC006, MEL006
- **Bloqueia:** MEL018
- **Alteração de schema:** não
- **Migration:** não
- **Alteração de fórmula:** não

## Objetivo

Corrigir a linguagem apresentada ao usuário no fluxo de preço do Insumo e reduzir atrito no cadastro diário.

A MEL016 estabelece que o par técnico já existente:

~~~text
QuantidadeCompra
PrecoCompra
~~~

representa, na linguagem de negócio:

~~~text
Quantidade por embalagem
Preço por embalagem
~~~

Também define que a Data de referência do formulário de novo preço deve iniciar preenchida com a data operacional da Empresa Ativa.

Nenhuma dessas mudanças altera:

- o modelo de dados;
- a fórmula de custo unitário;
- a natureza append-only do histórico;
- a aceitação de datas passadas/presentes/futuras;
- a unidade base do Insumo;
- a precisão/parsing implementados pela MEL015.

## Problema conceitual

Os rótulos atuais:

~~~text
Quantidade comprada
Preço total da compra
~~~

sugerem que o sistema registra uma transação de compra inteira.

Esse não é o conceito que queremos apresentar ao usuário.

O dado representa uma **referência comercial unitária de embalagem** usada para obter o custo por unidade base.

Exemplo:

~~~text
Insumo: Farinha
Unidade base: g

Embalagem: 1 kg
Quantidade por embalagem: 1000 g
Preço por embalagem: R$ 5,39

Custo unitário:
5,39 / 1000 = R$ 0,00539 por g
~~~

O usuário não informa:

- quantidade de embalagens compradas;
- valor total de um pedido;
- estoque;
- fornecedor;
- unidade alternativa kg/pacote/caixa.

## Semântica normativa de embalagem

### Quantidade por embalagem

`QuantidadeCompra` continua sendo o nome técnico/persistido.

Na linguagem funcional:

> Quantidade por embalagem é a quantidade de Unidade base contida na embalagem comercial usada como referência do preço.

Exemplos:

~~~text
Unidade base = g
embalagem de 1 kg
QuantidadeCompra = 1000

Unidade base = ml
frasco de 500 ml
QuantidadeCompra = 500

Unidade base = m
rolo de 10 m
QuantidadeCompra = 10

Unidade base = un
pacote com 50 unidades
QuantidadeCompra = 50
~~~

Não criar unidade `kg`, `L`, `pacote`, `caixa` ou conversão automática nesta MEL.

### Preço por embalagem

`PrecoCompra` continua sendo o nome técnico/persistido.

Na linguagem funcional:

> Preço por embalagem é o preço correspondente exatamente à Quantidade por embalagem informada.

Exemplo:

~~~text
Quantidade por embalagem = 1000 g
Preço por embalagem = R$ 5,39
~~~

Não representa o total de várias embalagens compradas.

### Custo unitário

A fórmula permanece:

~~~text
CustoUnitario = PrecoCompra / QuantidadeCompra
~~~

Sem arredondamento intermediário.

MEL015 continua governando parsing e apresentação monetária.

## Nomes técnicos preservados

Não renomear nesta MEL:

- entidade `PrecoInsumo`;
- propriedade `QuantidadeCompra`;
- propriedade `PrecoCompra`;
- colunas SQLite;
- migration histórica;
- índices;
- métodos de cálculo que já usam esses nomes.

A correção é semântica/documental/UI.

Uma eventual renomeação técnica exigiria custo de migration/refatoração sem benefício funcional para este item.

## Formulário de novo preço

Rota:

~~~text
/Insumos/Precos/Novo/{id:int}
~~~

### Rótulos obrigatórios

Alterar:

~~~text
Quantidade comprada (<unidade>)
=> Quantidade por embalagem (<unidade>)

Preço total da compra
=> Preço por embalagem
~~~

Manter:

~~~text
Data de referência
~~~

Exemplos:

~~~text
Quantidade por embalagem (g)
Quantidade por embalagem (ml)
Quantidade por embalagem (m)
Quantidade por embalagem (un)
~~~

### Display metadata

Atualizar também os atributos `Display` do `InputModel`:

~~~text
Input.QuantidadeCompra => "Quantidade por embalagem"
Input.PrecoCompra      => "Preço por embalagem"
~~~

Não depender apenas de texto hardcoded na Razor Page.

## Mensagens de validação

A linguagem de validação deve acompanhar a correção conceitual.

### Parse inválido

Alterar para:

~~~text
A quantidade por embalagem deve ser um número válido.
O preço por embalagem deve ser um número válido.
~~~

### Valor <= 0

No domínio, atualizar somente as mensagens:

~~~text
A quantidade por embalagem deve ser maior que zero.
O preço por embalagem deve ser maior que zero.
~~~

As invariáveis continuam exatamente as mesmas:

~~~text
QuantidadeCompra > 0
PrecoCompra > 0
~~~

Não alterar tipos de exceção nem regras de domínio.

## Data de referência — default

### Regra

No GET inicial da página:

~~~text
Input.DataReferencia = IDataOperacionalEmpresa.Hoje
~~~

A data deve vir da abstração introduzida pela MEL006.

Não usar:

- `DateTime.Today`;
- `DateTime.Now`;
- `DateTimeOffset.Now`;
- `DateOnly.FromDateTime(...)` com relógio estático do processo.

### Fluxo esperado

~~~text
GET /Insumos/Precos/Novo/{id}
1. carregar Insumo tenant-aware;
2. se não existir/cross-tenant => 404;
3. definir Input.DataReferencia = dataOperacionalEmpresa.Hoje;
4. renderizar Page.
~~~

É aceitável capturar a data em variável local antes da atribuição.

### Default somente no GET

A regra de default **não deve ser reaplicada no POST**.

Se o usuário apagar a data e enviar:

~~~text
Input.DataReferencia = null
~~~

o comportamento continua:

~~~text
A data de referência é obrigatória.
~~~

Não preencher hoje silenciosamente no POST.

Isso diferencia:

- conveniência de formulário;
- validação obrigatória.

### Datas diferentes de hoje

UC005 continua aceitando:

- data passada;
- data atual;
- data futura.

O default não restringe o usuário à data atual.

## Uso de IDataOperacionalEmpresa

Evoluir a PageModel para receber:

~~~csharp
IDataOperacionalEmpresa
~~~

junto ao DbContext.

Exemplo estrutural:

~~~csharp
public sealed class NovoModel(
    PrecificadorDbContext context,
    IDataOperacionalEmpresa dataOperacional) : PageModel
~~~

Não duplicar cálculo de timezone.

MEL006 já é a fonte da semântica temporal.

## Detalhes do Insumo

Rota:

~~~text
/Insumos/Detalhes/{id:int}
~~~

No bloco **Preço vigente**, alterar os rótulos:

~~~text
Quantidade
=> Quantidade por embalagem

Preço total
=> Preço por embalagem
~~~

Manter:

- Referência;
- Custo unitário.

Exemplo esperado:

~~~text
Preço vigente

Referência: 16/09/2026
Quantidade por embalagem: 200 g
Preço por embalagem: R$ 20,99
Custo unitário: R$ 0,10495 por g
~~~

Não alterar a query de preço vigente.

## Histórico de preços

Rota:

~~~text
/Insumos/Precos/Historico/{id:int}
~~~

### Resumo do vigente

Alterar:

~~~text
Quantidade
=> Quantidade por embalagem

Preço total
=> Preço por embalagem
~~~

### Tabela

Alterar os cabeçalhos:

~~~text
Quantidade
=> Quantidade por embalagem

Preço total
=> Preço por embalagem
~~~

Manter:

- Data;
- Status;
- Custo unitário.

Não alterar:

- ordenação;
- classificação Vigente/Anterior/Futuro;
- regra RN006;
- consulta tenant-aware.

## Consistência documental

A implementação deve manter coerentes:

- MEL016;
- UC005;
- UC006;
- F001;
- RN002;
- RN003.

### RN002

A linguagem funcional passa a ser:

~~~text
Quantidade por embalagem válida
~~~

A propriedade técnica permanece `QuantidadeCompra`.

### RN003

A linguagem funcional passa a ser:

~~~text
Preço por embalagem válido
~~~

A propriedade técnica permanece `PrecoCompra`.

### ADR histórica

Não é necessário reescrever ADR-004 retroativamente.

ADR registra a decisão histórica de modelagem e pode conservar terminologia original.

## Compatibilidade com dados existentes

Nenhum registro é reinterpretado numericamente.

Exemplo existente:

~~~text
QuantidadeCompra = 200
PrecoCompra = 20.99
~~~

continua exatamente:

~~~text
Quantidade por embalagem = 200
Preço por embalagem = R$ 20,99
~~~

O significado pretendido apenas passa a ser apresentado corretamente.

Não executar UPDATE de dados.

## Relação com MEL015

MEL015 está concluída e deve permanecer intacta.

MEL016 deve reutilizar:

- `DecimalInputParser`;
- formato monetário pt-BR;
- custo unitário técnico;
- preservação de precisão.

Não reintroduzir binding direto de `decimal`.

Não alterar regras de separadores decimais.

## Relação com MEL018

MEL018 será executada depois e tratará redirects pós-cadastro de Insumo/Produto.

MEL016 não altera redirect do registro de preço.

O POST válido de preço continua retornando para:

~~~text
/Insumos/Detalhes/{id}
~~~

conforme UC005.

## Multiempresa

Preservar integralmente FT002:

- Insumo resolvido pelo Global Query Filter;
- cross-tenant => 404;
- nenhum `EmpresaId` do request;
- data operacional pertence à Empresa Ativa;
- nenhum acesso a dados de outra Empresa.

O default de data não depende de dados do Insumo além do contexto ativo.

## Persistência

Nenhuma alteração de schema.

Não criar migration.

Não alterar `PrecificadorDbContextModelSnapshot`.

Não renomear propriedades EF.

Não atualizar registros existentes.

## Critérios de aceitação

- **CA01:** formulário usa `Quantidade por embalagem (<unidade>)`.
- **CA02:** formulário usa `Preço por embalagem`.
- **CA03:** atributos `Display` usam a nova terminologia.
- **CA04:** rótulos antigos `Quantidade comprada` e `Preço total da compra` não aparecem no formulário.
- **CA05:** GET novo preço inicia `DataReferencia` com `IDataOperacionalEmpresa.Hoje`.
- **CA06:** data default independe da data local do runner/servidor.
- **CA07:** usuário pode substituir o default por data passada.
- **CA08:** usuário pode substituir o default por data futura.
- **CA09:** POST com data vazia continua inválido e não recebe default silencioso.
- **CA10:** parse inválido usa `quantidade por embalagem`/`preço por embalagem` nas mensagens.
- **CA11:** valores <= 0 preservam as invariáveis e usam a nova linguagem nas mensagens do domínio.
- **CA12:** Detalhes usa `Quantidade por embalagem`.
- **CA13:** Detalhes usa `Preço por embalagem`.
- **CA14:** Histórico — resumo do vigente usa os novos rótulos.
- **CA15:** Histórico — tabela usa os novos cabeçalhos.
- **CA16:** Histórico não altera RN006/status/ordenação.
- **CA17:** custo unitário continua `PrecoCompra / QuantidadeCompra`.
- **CA18:** MEL015 permanece sem regressão de parsing/formatação.
- **CA19:** dados existentes não são alterados.
- **CA20:** nomes técnicos `QuantidadeCompra`/`PrecoCompra` permanecem.
- **CA21:** nenhuma migration/schema/model snapshot é alterado.
- **CA22:** cross-tenant continua 404.
- **CA23:** Insumo ativo/inativo continua aceitando registro de preço.
- **CA24:** nenhum comportamento de estoque, quantidade de embalagens ou conversão de unidade é introduzido.
- **CA25:** MEL018 não é antecipada.

## Matriz de testes

### Unitários — domínio

Atualizar `PrecoInsumoTests` somente para validar as mensagens quando útil.

- **U1:** quantidade zero/negativa continua rejeitada;
- **U2:** preço zero/negativo continua rejeitado;
- **U3:** mensagem de quantidade usa `quantidade por embalagem`;
- **U4:** mensagem de preço usa `preço por embalagem`;
- **U5:** custo unitário exato permanece sem alteração.

Não criar nova fórmula.

### Integração Web — Novo preço

- **W1:** GET mostra `Quantidade por embalagem (m)` para Insumo em metro;
- **W2:** GET mostra `Preço por embalagem`;
- **W3:** GET não mostra `Quantidade comprada` nem `Preço total da compra`;
- **W4:** factory com data operacional fixa `2030-01-02` renderiza input de data com `2030-01-02`;
- **W5:** POST alterando para data passada persiste a data informada;
- **W6:** POST alterando para data futura persiste a data informada;
- **W7:** POST com data vazia retorna página com erro obrigatório e não persiste;
- **W8:** input decimal inválido preserva texto e usa nova mensagem conceitual;
- **W9:** quantidade/preço <= 0 continuam rejeitados;
- **W10:** cenário MEL015 `200 / 20,99` continua persistindo `200m / 20.99m`;
- **W11:** ativo e inativo continuam registrando preço;
- **W12:** cross-tenant permanece 404.

### Integração Web — Detalhes

- **W13:** preço vigente exibe `Quantidade por embalagem`;
- **W14:** preço vigente exibe `Preço por embalagem`;
- **W15:** valores continuam formatados segundo MEL015;
- **W16:** sem preço vigente mantém estado vazio existente.

### Integração Web — Histórico

- **W17:** resumo do vigente usa `Quantidade por embalagem`;
- **W18:** resumo do vigente usa `Preço por embalagem`;
- **W19:** cabeçalhos da tabela usam os novos termos;
- **W20:** status Vigente/Anterior/Futuro permanecem corretos;
- **W21:** custo unitário técnico mantém precisão MEL015;
- **W22:** cross-tenant continua 404;
- **W23:** página continua somente leitura.

### Regressão documental

Na implementação, confirmar que:

- UC005 usa a nova terminologia funcional;
- UC006 usa a nova terminologia funcional;
- F001 usa a nova terminologia funcional;
- RN002/RN003 usam a nova terminologia funcional;
- nomes de propriedades persistidas continuam documentados quando tecnicamente necessário.

## Fora do escopo

- renomear `QuantidadeCompra`/`PrecoCompra` no código ou banco;
- migration;
- unidade de compra alternativa;
- conversão kg ↔ g ou L ↔ ml;
- cadastro de embalagem;
- estoque;
- fornecedor;
- quantidade de embalagens compradas;
- pedido/nota de compra;
- preço médio;
- edição/exclusão de preço;
- alterar RN006;
- alterar parsing da MEL015;
- alterar formatação monetária da MEL015;
- MEL018;
- MEL019;
- UC028+.

## Definition of Done específica

MEL016 está concluída quando:

- formulário usa integralmente a terminologia `embalagem`;
- Detalhes e Histórico usam a mesma terminologia;
- mensagens de validação visíveis ao usuário são coerentes;
- GET preenche Data de referência com `IDataOperacionalEmpresa.Hoje`;
- POST não reaplica default;
- datas passada/futura continuam válidas;
- MEL015 permanece verde;
- custo unitário não muda;
- propriedades/schema permanecem `QuantidadeCompra`/`PrecoCompra`;
- nenhuma migration é criada;
- matriz U1–U5 e W1–W23 está coberta;
- documentos normativos estão coerentes;
- build Release tem 0 erros e sem warnings novos relevantes;
- suíte completa está verde;
- backlog altera somente MEL016 de `Pronto` para `Concluído` na PR de implementação.

## Branch sugerida

~~~text
fix/mel016-semantica-preco-insumo
~~~

## Commit sugerido

~~~text
fix: corrige semantica do preco do insumo
~~~
