# Regras de Negócio

Este documento contém regras normativas do Precificador. Casos de uso devem referenciar os identificadores abaixo em vez de copiar fórmulas.

## Insumos e preços

### RN001 — Unidade base

Cada insumo deve possuir uma unidade base.

Após o UC001B, as unidades previstas para o escopo atual são:

- `g` — grama;
- `ml` — mililitro;
- `m` — metro;
- `un` — unidade.

Os valores numéricos existentes do enum foram preservados e `Metro = 4` foi acrescentado como valor funcional pelo UC001B.

### RN002 — Quantidade por embalagem válida

A propriedade técnica `QuantidadeCompra` deve ser maior que zero.

Na linguagem funcional, ela representa a **Quantidade por embalagem**: a quantidade de Unidade base contida na embalagem comercial usada como referência do preço.

O UC005/MEL016 não introduzem unidade de compra alternativa nem conversões automáticas. Se o Insumo está em `g` e a embalagem possui 1 kg, registrar `QuantidadeCompra = 1000`.

Não registrar quantidade de embalagens compradas nesse campo.

### RN003 — Preço por embalagem válido

A propriedade técnica `PrecoCompra` deve ser maior que zero.

Na linguagem funcional, ela representa o **Preço por embalagem**: o preço correspondente exatamente à `QuantidadeCompra` informada.

Não representa o valor total de várias embalagens, pedido ou estoque.

### RN004 — Custo unitário do insumo

`CustoUnitario = PrecoCompra / QuantidadeCompra`.

O cálculo deve preservar precisão decimal suficiente para que insumos baratos por unidade base não sejam arredondados prematuramente.

### RN005 — Histórico de preços

Registrar um novo preço não deve sobrescrever o registro anterior. Cada registro pertence a um Insumo específico; marcas diferentes do mesmo item possuem históricos independentes por serem Insumos distintos.

O histórico é append-only no MVP:

- cada registro válido cria uma nova linha;
- registros anteriores não são editados ou excluídos pelo fluxo normal;
- múltiplos preços na mesma DataReferencia são permitidos;
- uma correção é representada por novo registro, preservando o anterior.

### RN006 — Preço atual

O preço atual é o registro de preço vigente mais recente para o insumo. Registros com data futura não são considerados vigentes antes de sua data de referência.

A seleção normativa considera a data operacional da Empresa:

1. filtrar `DataReferencia <= dataOperacionalEmpresa`;
2. ordenar por `DataReferencia DESC`;
3. em empate de DataReferencia, ordenar por `Id DESC`;
4. o primeiro registro é o preço vigente.

O status de apresentação não é persistido. No histórico:

- o registro selecionado é **Vigente**;
- registros com `DataReferencia > dataOperacionalEmpresa` são **Futuros**;
- demais registros são **Anteriores**.

Um registro futuro pode aparecer antes do vigente no histórico completo e ainda assim não ser o preço atual.

### RN007 — Insumo sem preço

Insumo sem preço vigente possui custo desconhecido. O sistema não deve tratá-lo como custo zero.

A regra também se aplica quando existem registros de preço apenas com DataReferencia futura: existe histórico, mas ainda não existe preço vigente.

### RN008 — Desativação e reativação de insumo

Insumos são desativados, não excluídos fisicamente pelo fluxo normal.

Um Insumo desativado:

- permanece consultável e legível;
- continua participando da identidade e unicidade por Empresa + Nome + Marca;
- não pode ser adicionado a novas fichas técnicas quando esse fluxo existir;
- não perde referências, preços ou históricos existentes;
- pode receber novos registros de preço, pois preço é fato histórico/comercial e seu registro não implica reativação.

A desativação é reversível. Ao ser reativado, o Insumo volta ao estado operacional ativo.

### RN028 — Nome do insumo

Todo insumo deve possuir nome válido.

O nome:

- é obrigatório;
- deve possuir no máximo 120 caracteres após normalização de espaços;
- deve ter espaços em branco removidos do início e do fim;
- deve ter sequências internas de whitespace reduzidas a um único espaço;
- preserva para exibição a capitalização informada pelo usuário após a limpeza de espaços.

Para comparação determinística, o sistema mantém uma representação normalizada do nome em maiúsculas com regra invariável. A normalização de comparação não remove acentos.

### RN029 — Unicidade do insumo por empresa, nome e marca

A identidade funcional do Insumo é limitada à Empresa proprietária.

Antes do UC001A, a unicidade temporária era `(EmpresaId, NomeNormalizado)`.

No modelo atual, após o UC001A, não podem existir dois Insumos da mesma Empresa com a mesma combinação de `NomeNormalizado` e `MarcaNormalizada`.

Marcas diferentes do mesmo Nome representam Insumos distintos e podem coexistir na mesma Empresa. Empresas diferentes também podem cadastrar a mesma combinação Nome/Marca.

A situação Ativo/Inativo não participa da identidade. Insumos inativos continuam sujeitos à mesma unicidade.

Quando a Marca não for informada, `MarcaNormalizada` deve assumir string vazia como representação técnica.

A integridade deve ser protegida pelo banco com índice/restrição única adequada à fase do modelo, além da validação funcional usada para apresentar mensagem amigável.

### RN030 — Categoria do insumo

Após o UC001B, todo Insumo deve pertencer exatamente a uma das categorias do escopo atual:

- Matéria-prima;
- Embalagem;
- Consumível.

O código deve representar `MateriaPrima = 1`, preservando o mesmo valor numérico anteriormente usado por `Ingrediente`, para que registros existentes continuem semanticamente válidos sem transformação de dados.

Nenhum valor indefinido/zero é considerado categoria funcional válida.

Matéria-prima representa o material que compõe diretamente o produto ou é consumido como material principal de sua produção, independentemente do segmento da Empresa. Exemplos incluem farinha, açúcar, papel e vinil.

A categoria não determina automaticamente regra de perda; perdas serão modeladas como conceito de material/processo quando aplicável.

### RN031 — Situação inicial do insumo

Todo novo insumo é criado como ativo. A situação inicial não é escolhida pelo usuário no cadastro.

### RN032 — Marca do insumo

A Marca identifica opcionalmente a variação comercial de um insumo.

A Marca:

- é opcional;
- possui no máximo 80 caracteres após normalização;
- remove espaços externos e reduz sequências internas de whitespace a um único espaço;
- preserva a capitalização informada para exibição;
- possui representação `MarcaNormalizada` em maiúsculas com regra invariável;
- não remove acentos durante a normalização de comparação;
- quando ausente, é armazenada como `null`, enquanto `MarcaNormalizada` usa string vazia.

Marcas diferentes do mesmo Nome representam insumos economicamente distintos e podem possuir preços/custos diferentes dentro da mesma Empresa.

### RN033 — Observação do insumo

A Observação do Insumo é uma anotação técnica global e opcional, como força W de uma farinha ou característica relevante de embalagem.

A observação:

- possui no máximo 1000 caracteres;
- remove apenas whitespace externo;
- preserva conteúdo interno e quebras de linha;
- se vazia ou composta apenas por whitespace, é armazenada como `null`;
- não participa da identidade ou unicidade do Insumo.

### RN034 — Observação contextual do item da ficha técnica

Um item de ficha técnica pode possuir observação contextual própria para registrar a justificativa de uso daquele Insumo na ficha, como a razão para escolher uma marca específica.

A observação contextual:

- é opcional;
- possui no máximo 1000 caracteres;
- remove apenas whitespace externo;
- preserva conteúdo interno e quebras de linha;
- se vazia ou composta apenas por whitespace, é armazenada como null.

Essa observação é independente da Observação global do Insumo e deve permanecer associada ao item da ficha. Alterações posteriores na Observação do Insumo não devem sobrescrever a justificativa registrada.

A implementação desta regra pertence aos UCs de Ficha Técnica, não ao UC001A ou UC002.

### RN040 — Primeiro preço consolida permanentemente a identidade do Insumo

Enquanto um Insumo ainda não possuir identidade consolidada, Nome, Marca e Unidade base podem ser alterados conforme as regras cadastrais existentes.

A persistência do primeiro registro de preço — vigente, anterior ou futuro — deve consolidar permanentemente a identidade do Insumo conforme RN051.

A partir dessa consolidação:

- **Nome torna-se imutável**;
- **Marca torna-se imutável**;
- **Unidade base torna-se imutável**;
- Categoria permanece editável;
- Observação permanece editável;
- Situação Ativo/Inativo continua regida pela RN008.

A consolidação não é revertida se o preço deixar de ser vigente ou se novos fatos surgirem posteriormente. O histórico de preço é apenas um dos gatilhos que tornam a identidade definitiva.

Se for necessária uma mudança real de Nome, Marca ou Unidade base depois da consolidação, deve ser criado um **novo Insumo**. O registro anterior pode ser desativado conforme RN008.

A motivação, alternativas avaliadas e consequências desta decisão estão documentadas em [Estabilidade cadastral do Insumo](insumo-historical-stability.md).

### RN048 — Primeiro uso em Ficha consolida permanentemente a identidade do Insumo

A persistência do primeiro ItemFichaTecnica que referencia um Insumo deve consolidar permanentemente sua identidade conforme RN051.

A partir desse primeiro uso:

- Nome é imutável;
- Marca é imutável;
- Unidade base é imutável;
- Categoria permanece editável;
- Observação global permanece editável;
- situação Ativo/Inativo continua regida pela RN008.

A regra protege o significado da composição: Quantidade é expressa na Unidade base e a referência identifica um Insumo econômico específico.

A consolidação é histórica e monotônica. Remover posteriormente um ItemFichaTecnica — inclusive a última referência atual do Insumo — **não reabre Nome, Marca ou Unidade base**.

UC016 não deve contar referências, consultar preços para decidir desbloqueio nem alterar o estado de consolidação.

### RN051 — Identidade consolidada do Insumo

O Insumo possui estado persistido e monotônico de consolidação de identidade:

~~~text
IdentidadeConsolidada : bool
~~~

Novo Insumo nasce com:

~~~text
IdentidadeConsolidada = false
~~~

A primeira ocorrência de qualquer um dos eventos abaixo muda o estado para true:

- registro de PrecoInsumo, conforme RN040;
- inclusão de ItemFichaTecnica, conforme RN048.

A transição permitida é somente:

~~~text
false -> true
~~~

Não existe transição true -> false.

Quando IdentidadeConsolidada = true:

- Nome é permanentemente imutável;
- Marca é permanentemente imutável;
- Unidade base é permanentemente imutável;
- Categoria permanece editável;
- Observação permanece editável;
- Ativo/Inativo permanece regido pela RN008.

A proteção não é derivada da existência atual de preços ou Itens. Remover referências futuras não altera o estado consolidado.

A consolidação deve ser persistida no mesmo SaveChanges/transação que grava o primeiro fato econômico/produtivo correspondente.

Registros existentes devem ser migrados com IdentidadeConsolidada = true quando já houver qualquer PrecoInsumo ou ItemFichaTecnica associado.

### RN049 — Um Insumo por Ficha Técnica

O mesmo Insumo pode aparecer no máximo uma vez na mesma Ficha Técnica no MVP.

A identidade funcional do Item é:

~~~text
EmpresaId + FichaTecnicaId + InsumoId
~~~

Se o Insumo já estiver na Ficha, uma nova inclusão deve ser rejeitada. Alterações de Quantidade ou Observação contextual pertencem ao UC015.

O mesmo Insumo pode participar de Fichas diferentes da mesma Empresa.

### RN050 — Atualização do Item preserva seus vínculos

A edição de um ItemFichaTecnica altera:

- Quantidade;
- Observação contextual;
- PercentualPerda, após UC019.

Permanecem imutáveis:

- EmpresaId;
- FichaTecnicaId;
- InsumoId.

Trocar o Insumo não é considerado edição do Item no MVP. A substituição deve ocorrer por remoção do Item existente e inclusão de outro Insumo pelos fluxos correspondentes.

A atualização deve validar todos os novos valores antes de alterar o estado da entidade, preservando atomicidade.

Item existente continua editável mesmo quando o Insumo referenciado ou o Produto da Ficha estiver inativo. A edição não reativa nenhuma dessas entidades.





## Multiempresa e acesso

### RN035 — Propriedade por empresa

Todo dado operacional tenant-owned pertence exatamente a uma Empresa por `EmpresaId` obrigatório.

### RN036 — Isolamento de empresa

Operações comuns só podem ler ou alterar dados tenant-owned da Empresa Ativa. Ausência de Empresa Ativa não concede acesso implícito a qualquer Empresa.

### RN037 — Empresa ativa

Um usuário autenticado trabalha no contexto de uma única Empresa Ativa por vez. Só pode ativar uma Empresa ativa para a qual possua vínculo ativo.

### RN038 — Vínculo usuário-empresa

Um usuário pode possuir vínculos com múltiplas Empresas. Vínculo inativo ou Empresa inativa não concede acesso operacional.

### RN039 — Configurações por empresa

Configurações de precificação pertencem a uma Empresa e suas alterações não afetam cálculos de outra Empresa.

O modelo é tenant-owned e 1:1 por Empresa. A configuração não deve ser inferida de outra Empresa quando ausente nem receber `EmpresaId` controlado pelo request.

## Produtos e ficha técnica

### RN009 — Rendimento

Toda Ficha Técnica deve possuir Rendimento maior que zero, expresso em unidades de venda por lote/execução.

Rendimento é decimal para permitir processos cujo lote produza quantidade fracionária de unidades de venda.

Regra:

`Rendimento > 0`.

### RN010 — Quantidade na ficha

A quantidade de cada item da ficha técnica deve ser maior que zero e estar expressa na unidade base do insumo.

A Quantidade representa a quantidade base necessária para o lote antes de perda adicional esperada. Quando houver perda de material configurada no Item, ela é calculada separadamente conforme RN012; não embutir a mesma perda simultaneamente na Quantidade e no PercentualPerda.

### RN011 — Custo dos itens do lote

Para cada item:

`CustoItem = QuantidadeUtilizada × CustoUnitarioAtualDoInsumo`.

O `CustoUnitarioAtualDoInsumo` vem do preço vigente selecionado conforme RN006 e é calculado pela RN004.

Quando a Ficha possui pelo menos um Item e todos os Itens possuem preço vigente:

`CustoBaseItens = soma dos CustoItem da Ficha`.

Se qualquer Item não possuir preço vigente, os custos conhecidos dos demais Itens podem ser exibidos individualmente, mas `CustoBaseItens` fica indisponível. Não é permitido somar apenas os Itens conhecidos e apresentar o valor como total confiável.

Ficha sem Itens não equivale a custo base zero; o componente permanece indisponível/incompleto conforme RN017.

Aplicar RN026: não realizar arredondamento intermediário do custo unitário, custo do Item ou soma dos Itens.

### RN012 — Perdas aplicáveis

No MVP, perda de material é opcional e pertence ao Item da Ficha por `PercentualPerda`, armazenado como fração decimal:

~~~text
0 <= PercentualPerda < 1
~~~

Zero significa nenhuma perda adicional esperada. Categoria do Insumo não aplica perda automaticamente.

`Quantidade` continua sendo a quantidade base antes da perda. O percentual representa acréscimo esperado sobre essa base:

~~~text
CustoPerdaItem = CustoItemBase × PercentualPerda
CustoPerdasLote = soma dos CustoPerdaItem
~~~

Se PercentualPerda = 0, CustoPerdaItem = 0 mesmo quando o custo base estiver indisponível.

Se PercentualPerda > 0 e o custo base do Item estiver indisponível, o custo da perda daquele Item e o total de perdas ficam indisponíveis. Custos conhecidos dos demais Itens podem ser exibidos, mas não como total parcial confiável.

Ficha sem Itens possui CustoPerdasLote = 0/completo, embora UC018 continue considerando o custo base dos Itens incompleto.

Perda que reduz a quantidade de unidades finais vendáveis do processo não deve ser duplicada em PercentualPerda: deve ser refletida no Rendimento esperado da Ficha. Assim, perda de material aumenta materiais consumidos; perda de saída reduz Rendimento e afeta todos os componentes por unidade no UC022.

Aplicar RN026 sem arredondamento intermediário. Custos de perda são derivados em consulta e não persistidos.

### RN013 — Mão de obra

A partir da MEL022, o custo de mão de obra do lote é proporcional ao custo base dos insumos:

```text
CustoMaoDeObraLote =
    CustoBaseItens
    × PercentualMaoDeObraDaEmpresa
```

`PercentualMaoDeObra` pertence à configuração tenant-aware da Empresa, é obrigatório, armazenado como fração decimal e possui default de `0,10` (10%).

Validação:

```text
PercentualMaoDeObra >= 0
```

Não existe teto de 100%: processos artesanais podem possuir mão de obra superior ao custo dos materiais.

A base é exclusivamente `CustoBaseItens`. Não entram perdas, energia, rendimento, margem ou preços comerciais.

Semântica:

- custo base conhecido + percentual zero => custo de mão de obra zero e determinável;
- custo base conhecido + percentual positivo => multiplicação decimal sem arredondamento intermediário;
- custo base indisponível => custo de mão de obra indisponível;
- nunca substituir custo de Item desconhecido por zero.

`TempoAtivoMinutos` e `ValorHoraTrabalho` são removidos do modelo ativo pela MEL022.

O custo de mão de obra é derivado em consulta e não é persistido na Ficha ou no Produto.

### RN014 — Energia de equipamento

Para cada uso de equipamento elétrico na Ficha:

`ConsumoKwh = PotenciaKw × (TempoUsoMinutos / 60m)`.

`CustoEnergiaUso = ConsumoKwh × TarifaEnergiaKwhDaEmpresa`.

O custo de energia do lote soma os usos aplicáveis. Forno é um possível equipamento, não um conceito universal de toda Ficha.

Aplicar RN026 sem arredondamento intermediário.

Se não houver usos, o custo de energia do lote é zero e o componente é determinável mesmo com tarifa não configurada.

Se houver pelo menos um uso e a tarifa estiver `null`, os consumos em kWh permanecem conhecidos, mas os custos por uso e do lote ficam indisponíveis.

Tarifa igual a zero é valor configurado válido e resulta em custo zero.

### RN055 — Uso de equipamento na Ficha

No MVP, equipamento não possui catálogo global. Cada `UsoEquipamentoFicha` representa o uso total atual de um equipamento em uma Ficha.

O uso contém Nome do equipamento, Potência em kW e Tempo de uso em minutos.

Regras:

- Nome obrigatório, normalizado e com máximo de 120 caracteres;
- PotenciaKw > 0;
- TempoUsoMinutos > 0;
- um mesmo NomeEquipamentoNormalizado aparece no máximo uma vez por Ficha;
- EmpresaId e FichaTecnicaId são imutáveis após criação;
- edição altera somente Nome/Potência/Tempo;
- remoção física é permitida porque a Ficha representa estado atual.

### RN056 — Completude do custo de energia

O componente de energia é calculado em tempo de consulta e nunca persistido.

- zero usos => CustoEnergiaLote = 0 e componente completo;
- usos existentes + TarifaEnergiaKwh = null => custo indisponível e componente incompleto;
- TarifaEnergiaKwh = 0 => custo zero e componente completo;
- a incompletude de Itens ou mão de obra não impede exibir energia conhecida;
- a composição final dos componentes pertence ao UC022.

### RN015 — Custo do lote

O custo do lote soma custo dos itens, perdas aplicáveis, mão de obra e recursos/equipamentos aplicáveis ao processo.

### RN016 — Custo unitário do produto

`CustoUnitarioProduto = CustoLote / Rendimento`.

### RN017 — Precificação incompleta

Se qualquer dado obrigatório para um resultado estiver ausente ou inválido — incluindo preço vigente de um insumo para os cálculos de custo dependentes — esse resultado e seus dependentes permanecem indisponíveis. Não deve ser exibido custo total ou margem como se fossem confiáveis quando faltarem entradas necessárias para esses resultados.

A ausência de preço vigente nunca é substituída por custo zero. Se apenas parte da composição possuir custo conhecido, resultados individuais conhecidos podem ser apresentados para explicabilidade, mas totais dependentes do conjunto completo permanecem indisponíveis.

Ficha Técnica existente sem Itens também não equivale a custo zero nem torna os resultados dependentes do custo completos.

A completude é específica por etapa. Um dado ausente que afete somente uma etapa posterior não invalida resultados independentes já determináveis. Em particular, `IncrementoComercial = null` mantém UC023/Preço sugerido incompleto, mas não invalida por si só Custo unitário conhecido nem a Margem atual da UC024 quando existe Preço de prateleira atual.

`SituacaoMargem` da UC024 representa especificamente a situação frente à Margem-alvo e não substitui um indicador global de completude da precificação.

### RN018 — Desativação e reativação de produto

Produtos são desativados, não excluídos fisicamente pelo fluxo normal.

A situação é reversível:

- `Desativar` torna o Produto inativo;
- `Reativar` devolve o Produto ao estado ativo;
- as operações de situação não alteram Id, Empresa, Nome, Categoria ou Margem-alvo;
- Produto inativo permanece consultável e visível em listagens;
- Produto inativo continua participando da unicidade `EmpresaId + NomeNormalizado`;
- após o UC009, Produto inativo permanece editável nos campos cadastrais permitidos e uma edição não o reativa implicitamente.

Regras futuras sobre uso de Produto inativo em precificação comercial, Ficha Técnica, cálculos ou seletores operacionais devem ser definidas pelos respectivos UCs quando esses fluxos existirem. O UC011 define que Produto inativo pode receber novo registro de Preço de prateleira sem ser reativado.

### RN041 — Nome do produto

Todo Produto deve possuir Nome válido.

O Nome:

- é obrigatório;
- possui no máximo 120 caracteres após normalização de espaços;
- remove whitespace externo;
- reduz sequências internas de whitespace a um único espaço;
- preserva a capitalização informada para exibição.

Para comparação determinística, o sistema mantém `NomeNormalizado` em maiúsculas com regra invariável, sem remover acentos.

### RN042 — Unicidade do produto por Empresa e Nome

Na mesma Empresa não podem existir dois Produtos com o mesmo `NomeNormalizado`.

A identidade cadastral inicial é:

`EmpresaId + NomeNormalizado`.

Empresas diferentes podem cadastrar Produtos com o mesmo Nome.

Categoria não participa da identidade.

A situação Ativo/Inativo também não participa da identidade; Produto inativo continua ocupando o Nome dentro da Empresa.

A integridade deve ser protegida por validação funcional e índice único no banco.

### RN043 — Categoria opcional do produto

Categoria é um texto livre opcional usado apenas para organização do catálogo.

A Categoria:

- possui no máximo 80 caracteres após normalização;
- remove whitespace externo;
- reduz sequências internas de whitespace a um único espaço;
- preserva capitalização de exibição;
- se vazia ou composta apenas por whitespace, é armazenada como `null`;
- não participa da unicidade;
- não altera regras de cálculo.

Não existe enum ou cadastro global de Categoria de Produto no UC007.

### RN044 — Situação inicial do produto

Todo novo Produto nasce ativo.

A situação inicial não é escolhida pelo usuário no cadastro.

### RN045 — Margem-alvo do produto

Todo Produto possui MargemAlvo própria desde o cadastro.

No domínio/persistência ela é armazenada como fração decimal:

- 30% = `0,30`;
- 25,5% = `0,255`.

Aplicar RN019: `0 <= MargemAlvo < 1`.

A interface pode receber percentual e convertê-lo para fração antes de criar/atualizar o domínio.

Quando `MargemPadrao` estiver configurada, o GET de cadastro de novo Produto pode pré-preencher a Margem-alvo com esse valor. O usuário continua livre para alterá-lo antes de salvar, e o Produto persiste sua própria `MargemAlvo`.

Se `MargemPadrao` estiver `null`, o cadastro não inventa valor padrão. Alterações posteriores da configuração nunca alteram silenciosamente Produtos já existentes.

### RN046 — Produto pode existir sem preço de prateleira

É válido cadastrar Produto sem Preço de prateleira.

A decisão comercial possui histórico próprio e será registrada pelo UC011 somente depois que o sistema puder calcular Custo de referência, Margem de referência e Preço sugerido.

Ausência de Preço de prateleira:

- não impede o cadastro do Produto;
- não equivale a preço zero;
- impede o cálculo de margem atual até que os demais dados necessários existam.

O cadastro inicial não persiste `PrecoVendaAtual` nem `PrecoPrateleiraAtual` diretamente em Produto.


### RN047 — Ficha Técnica única por Produto e Empresa

Cada Produto pode possuir no máximo uma Ficha Técnica atual no MVP.

A identidade funcional da associação é:

`EmpresaId + ProdutoId`.

A Ficha Técnica:

- pertence à mesma Empresa do Produto;
- não pode ser reatribuída para outro Produto ou outra Empresa;
- pode ser criada ou alterada para Produto ativo ou inativo;
- não altera a situação do Produto;
- representa estado produtivo atual editável, sem versionamento próprio no MVP.

Produto pode existir sem Ficha Técnica. A ausência de Ficha ou de outros dados necessários mantém a precificação incompleta conforme RN017.


## Margem e preço

### RN019 — Margem-alvo válida

A margem-alvo deve ser maior ou igual a zero e menor que 100%.

### RN020 — Preço teórico

`PrecoTeorico = CustoUnitarioProduto / (1 - MargemAlvo)`.

### RN021 — Arredondamento do preço sugerido

O preço sugerido é o menor múltiplo do incremento comercial configurado da Empresa que seja maior ou igual ao preço teórico, garantindo que o arredondamento não reduza a margem abaixo da margem-alvo.

Fórmula:

~~~text
PrecoSugerido = Ceiling(PrecoTeorico / IncrementoComercial) × IncrementoComercial
~~~

`IncrementoComercial` deve ser maior que zero quando configurado. Se estiver `null`, o Preço teórico pode continuar determinável, mas o Preço sugerido fica indisponível.

Se o Preço teórico já for múltiplo exato do incremento, o Preço sugerido é igual ao Preço teórico; não adicionar um incremento artificial.

Exemplo com incremento de R$ 0,50: R$ 25,08 resulta em R$ 25,50, enquanto R$ 25,00 permanece R$ 25,00.

Aplicar RN026: não arredondar o Preço teórico antes de determinar o múltiplo comercial.

### RN022 — Margem atual

A Margem atual usa exclusivamente estado corrente:

- `CustoUnitarioProduto` atual calculado pela UC022;
- `PrecoPrateleiraAtual` selecionado pela UC012 por `DataReferencia DESC, Id DESC`;
- `Produto.MargemAlvo` atual.

Quando custo e Preço de prateleira estiverem disponíveis:

`MargemAtual = (PrecoPrateleiraAtual - CustoUnitarioProduto) / PrecoPrateleiraAtual`.

Não usar `CustoReferencia` ou `MargemReferencia` históricos no cálculo atual.

Se custo atual ou Preço de prateleira atual estiverem indisponíveis, `MargemAtual` permanece indisponível. Ausência de preço nunca é convertida em zero.

Preço sugerido, Incremento comercial, Reserva comercial e Desconto de referência não participam da Margem atual.

Aplicar RN026 sem arredondamento intermediário.

### RN023 — Produto abaixo da margem

Com Margem atual calculável:

- se `MargemAtual < Produto.MargemAlvo`, a situação é `AbaixoDaMargem`;
- se `MargemAtual >= Produto.MargemAlvo`, a situação é `DentroDaMargem`.

A igualdade exata pertence a `DentroDaMargem`.

Quando Margem atual não for calculável por ausência de custo atual ou Preço de prateleira atual, a situação é `Incompleto`.

### RN024 — Preço de prateleira e histórico de precificação

Registrar uma nova decisão comercial cria um novo registro append-only e não altera nem exclui registros anteriores.

O usuário informa somente `PrecoPrateleira`. O sistema determina e congela no registro:

- DataReferencia pela data operacional da Empresa;
- CustoReferencia = CustoUnitarioProduto vigente calculado pelo sistema;
- MargemReferencia = MargemAlvo do Produto usada naquele cálculo;
- PrecoSugerido = preço calculado pelo UC023 a partir dessas referências e do arredondamento comercial;
- PrecoPrateleira informado.

Não são permitidas datas futuras. Múltiplos registros na mesma DataReferencia são permitidos para correção; o vigente é o de maior DataReferencia e, em empate, maior Id.

Produto inativo pode receber novo registro e permanece inativo.

O Preço de prateleira pode ser inferior ao Preço sugerido. Essa decisão não é bloqueada, mas deve ser evidenciada na apresentação.

O DescontoReferencia é derivado, não persistido. Cada registro comercial congela `ReservaComercialReferencia` conforme RN054.

Definir, quando `PrecoSugerido > 0`:

```text
PercentualAcimaDoSugerido = (PrecoPrateleira / PrecoSugerido) - 1
LimiarAplicacao = ReservaComercialReferencia + 0,01
```

- se `PrecoSugerido = 0`, DescontoReferencia não é aplicável; a razão percentual é indefinida e não deve haver divisão por zero;
- se o Preço de prateleira for inferior ao Preço sugerido, DescontoReferencia não é aplicável;
- se `PercentualAcimaDoSugerido < LimiarAplicacao`, DescontoReferencia não é aplicável;
- caso contrário, `DescontoReferencia = PercentualAcimaDoSugerido - ReservaComercialReferencia`;
- não realizar arredondamento intermediário conforme RN026.

## Configurações

### RN025 — Configurações de precificação por empresa

Cada Empresa possui uma configuração de precificação 1:1 contendo, no mínimo:

- `PercentualMaoDeObra`;
- `TarifaEnergiaKwh`;
- `MargemPadrao`;
- `IncrementoComercial`;
- `ReservaComercialDesconto`, conforme RN052.

A partir da MEL022, `PercentualMaoDeObra` é obrigatório, armazenado como fração decimal e nasce em `0,10` (10%). Zero é válido e valores maiores que 100% também são válidos; somente valor negativo é rejeitado.

Não existem defaults de negócio aprovados para `TarifaEnergiaKwh`, `MargemPadrao` e `IncrementoComercial`. Enquanto não configurados, permanecem `null` e devem ser apresentados como **Não configurado**, nunca convertidos silenciosamente em zero.

UC027 permite limpar esses três parâmetros opcionais de volta para `null`. `PercentualMaoDeObra` e `ReservaComercialDesconto` são obrigatórios e não podem ser limpos.

Quando informados:

- `PercentualMaoDeObra >= 0`, sem teto de 100%;
- `TarifaEnergiaKwh >= 0`;
- `0 <= MargemPadrao < 1`;
- `IncrementoComercial > 0`.

A reserva comercial possui default e validação próprios na RN052.

Alterações de configuração afetam imediatamente apenas cálculos atuais dependentes da mesma Empresa. Parâmetro opcional necessário e ausente torna o cálculo dependente incompleto conforme RN017.

A Margem padrão pode pré-preencher novos Produtos, mas nunca altera silenciosamente a `MargemAlvo` de Produtos já existentes.

### RN052 — Reserva comercial de desconto por Empresa

Cada Empresa possui `ReservaComercialDesconto`, armazenada como fração decimal.

Validação:

```text
0 <= ReservaComercialDesconto < 1
```

O valor padrão é `0,10`, equivalente a 10 pontos percentuais.

Alterar a reserva afeta apenas novas decisões comerciais. Não altera Preço sugerido nem reinterpreta registros históricos.

### RN053 — Limiar do Desconto de referência

O limiar de aplicação do Desconto de referência é derivado da reserva congelada no registro:

```text
LimiarAplicacao = ReservaComercialReferencia + 0,01
```

O `0,01` representa 1 ponto percentual mínimo acima da reserva.

O limiar não é uma configuração separada.

### RN054 — Snapshot da reserva comercial

Cada RegistroPrecoProduto deve persistir `ReservaComercialReferencia` com o valor vigente de `ReservaComercialDesconto` da Empresa no momento do registro.

O usuário não informa esse campo.

Alterações posteriores da configuração da Empresa não alteram o snapshot nem o Desconto de referência derivado de registros antigos.

## Precisão

### RN026 — Arredondamento intermediário

Cálculos intermediários não devem ser arredondados para centavos. O arredondamento monetário de apresentação ocorre apenas nas fronteiras definidas pelo modelo de precificação.

## Status de cálculo

### RN027 — Estados mínimos da situação de margem

Para fins de acompanhamento da Margem atual, Produto ativo ou inativo pode estar em um dos estados:

- `Incompleto`: Margem atual não pode ser determinada porque o Custo unitário atual ou o Preço de prateleira atual está indisponível;
- `AbaixoDaMargem`: Margem atual calculável e inferior à `MargemAlvo` atual;
- `DentroDaMargem`: Margem atual calculável e igual ou superior à `MargemAlvo` atual.

A `SituacaoMargem` é derivada em consulta e não é persistida.

Ela não representa, isoladamente, a completude global de todas as etapas de precificação. A completude de `PrecoSugerido`/UC023 é independente desta classificação. Por exemplo, `IncrementoComercial = null` pode deixar UC023 incompleta e ainda assim permitir Margem atual e Situação de margem quando custo e Preço de prateleira estiverem conhecidos.
