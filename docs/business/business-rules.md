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

### RN002 — Quantidade de compra válida

A quantidade informada em um registro de preço deve ser maior que zero e é expressa na Unidade base do Insumo.

O UC005 não introduz unidade de compra alternativa nem conversões automáticas. Se o Insumo está em `g`, por exemplo, uma compra de 1 kg é registrada como quantidade `1000`.

### RN003 — Preço de compra válido

O preço pago deve ser maior que zero e representa o valor total correspondente à QuantidadeCompra informada.

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

A edição de um ItemFichaTecnica altera somente:

- Quantidade;
- Observação contextual.

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

### RN011 — Custo dos itens do lote

Para cada item:

`CustoItem = QuantidadeUtilizada × CustoUnitarioAtualDoInsumo`.

O `CustoUnitarioAtualDoInsumo` vem do preço vigente selecionado conforme RN006 e é calculado pela RN004.

Quando a Ficha possui pelo menos um Item e todos os Itens possuem preço vigente:

`CustoBaseItens = soma dos CustoItem da Ficha`.

Se qualquer Item não possuir preço vigente, os custos conhecidos dos demais Itens podem ser exibidos individualmente, mas `CustoBaseItens` fica indisponível. Não é permitido somar apenas os Itens conhecidos e apresentar o valor como total confiável.

Ficha sem Itens não equivale a custo base zero; o componente permanece indisponível/incompleto conforme RN017.

Aplicar RN026: não realizar arredondamento intermediário do custo unitário, custo do Item ou soma dos Itens.

### RN012 — Perdas de material/processo

Perda deixa de ser assumida como atributo obrigatório específico de panificação. Quando aplicável, representa perda de material/processo.

O modelo exato e a forma de incidência serão fechados antes do UC019, considerando os diferentes negócios. Até lá, nenhuma implementação nova deve cristalizar uma fórmula específica de panificação.

### RN013 — Mão de obra

O tempo ativo é informado para o lote/execução em minutos inteiros.

`TempoAtivoMinutos` é obrigatório na Ficha Técnica e deve ser maior ou igual a zero. Zero é válido quando explicitamente informado para um processo sem trabalho humano ativo; ausência não deve ser convertida silenciosamente em zero.

O cálculo futuro é:

`CustoMaoDeObraLote = (TempoAtivoMinutos / 60) × ValorHoraTrabalhoDaEmpresa`.

### RN014 — Energia de equipamento

Quando houver equipamento com consumo mensurável:

`CustoEnergiaUso = PotenciaEquipamentoKw × (TempoUsoMinutos / 60) × TarifaKwhDaEmpresa`.

O custo de energia do lote soma os usos aplicáveis. Forno é um possível equipamento, não um conceito universal de toda ficha.

O modelo de Equipamento/Uso será detalhado antes do UC021.

### RN015 — Custo do lote

O custo do lote soma custo dos itens, perdas aplicáveis, mão de obra e recursos/equipamentos aplicáveis ao processo.

### RN016 — Custo unitário do produto

`CustoUnitarioProduto = CustoLote / Rendimento`.

### RN017 — Precificação incompleta

Se qualquer dado obrigatório para o cálculo estiver ausente ou inválido — incluindo preço vigente de um insumo — o produto deve ser marcado como precificação incompleta. Não deve ser exibido custo total ou margem como se fossem confiáveis.

A ausência de preço vigente nunca é substituída por custo zero. Se apenas parte da composição possuir custo conhecido, resultados individuais conhecidos podem ser apresentados para explicabilidade, mas totais dependentes do conjunto completo permanecem indisponíveis.

Ficha Técnica existente sem Itens também não equivale a custo zero nem torna a precificação completa.

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

Uma futura margem padrão da Empresa pode pré-preencher novos Produtos, mas não altera silenciosamente Produtos já existentes.

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

O preço sugerido deve ser arredondado **para cima** para o próximo múltiplo do incremento comercial configurado da Empresa, garantindo que o arredondamento não reduza a margem abaixo da margem-alvo.

Exemplo com incremento de R$ 0,50: R$ 25,08 resulta em R$ 25,50.

### RN022 — Margem atual

Para Preço de prateleira vigente maior que zero:

`MargemAtual = (PrecoPrateleiraAtual - CustoUnitarioProduto) / PrecoPrateleiraAtual`.

Preço sugerido é referência econômica; margem atual usa o preço comercial efetivamente definido para prateleira.

### RN023 — Produto abaixo da margem

Um produto com precificação completa está abaixo da margem quando `MargemAtual < MargemAlvo`.

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

Definir:

```text
PercentualAcimaDoSugerido = (PrecoPrateleira / PrecoSugerido) - 1
LimiarAplicacao = ReservaComercialReferencia + 0,01
```

- se o Preço de prateleira for inferior ao Preço sugerido, DescontoReferencia não é aplicável;
- se `PercentualAcimaDoSugerido < LimiarAplicacao`, DescontoReferencia não é aplicável;
- caso contrário, `DescontoReferencia = PercentualAcimaDoSugerido - ReservaComercialReferencia`;
- não realizar arredondamento intermediário conforme RN026.

## Configurações

### RN025 — Configurações de precificação por empresa

Cada Empresa possui uma configuração de precificação 1:1 contendo, no mínimo:

- `ValorHoraTrabalho`;
- `TarifaEnergiaKwh`;
- `MargemPadrao`;
- `IncrementoComercial`;
- `ReservaComercialDesconto`, conforme RN052.

Não existem defaults de negócio aprovados para `ValorHoraTrabalho`, `TarifaEnergiaKwh`, `MargemPadrao` e `IncrementoComercial`. Enquanto não configurados, esses campos permanecem `null` e devem ser apresentados como **Não configurado**, nunca convertidos silenciosamente em zero.

Quando informados:

- `ValorHoraTrabalho >= 0`;
- `TarifaEnergiaKwh >= 0`;
- `0 <= MargemPadrao < 1`;
- `IncrementoComercial > 0`.

A reserva comercial possui default e validação próprios na RN052.

A potência deixa de ser tratada como configuração global de um forno e pertencerá ao Equipamento quando esse domínio for implementado.

Alterações de configuração afetam imediatamente apenas cálculos atuais dependentes da mesma Empresa. Parâmetro obrigatório ausente torna o cálculo dependente incompleto conforme RN017.

A Margem padrão pode futuramente pré-preencher novos Produtos, mas nunca altera silenciosamente a `MargemAlvo` de Produtos já existentes.

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

### RN027 — Estados mínimos

Para fins de acompanhamento, um produto ativo pode estar pelo menos em um dos estados:

- `Incompleto`: não pode ser precificado com segurança;
- `AbaixoDaMargem`: cálculo válido e margem atual inferior à meta;
- `DentroDaMargem`: cálculo válido e margem atual igual ou superior à meta.
