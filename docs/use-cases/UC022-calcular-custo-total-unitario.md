# UC022 — Calcular custo total e custo unitário

> **Nota MEL022:** referências históricas a `TempoAtivoMinutos` e `ValorHoraTrabalho` foram substituídas por `PercentualMaoDeObra` aplicado sobre `CustoBaseItens` no UC020/RN013 vigente.

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC018, UC019, UC020 e UC021
- **Base de revalidação:** UC018–UC021 concluídas
- **Schema:** não
- **Persistência do resultado:** não

## Objetivo

Compor os componentes de custo já calculados pela Ficha Técnica para determinar:

- o custo total atual do lote;
- o custo unitário atual do Produto.

A UC022 não introduz novas fontes de custo. Ela apenas consolida os resultados dos UCs anteriores, preservando a semântica de completude definida pela RN017.

## Componentes de entrada

O cálculo utiliza exclusivamente:

~~~text
CustoBaseItens
CustoPerdasLote
CustoMaoDeObraLote
CustoEnergiaLote
Rendimento
~~~

Origem:

~~~text
CustoBaseItens          => UC018
CustoPerdasLote         => UC019
CustoMaoDeObraLote      => UC020
CustoEnergiaLote        => UC021
Rendimento              => FichaTecnica / UC013
~~~

UC022 não recalcula nenhum desses componentes.

Em particular:

- não consulta preços de Insumos;
- não recalcula perdas por Item;
- não consulta configurações;
- não recalcula energia por equipamento;
- não recalcula mão de obra.

Os valores produzidos pelos componentes existentes devem ser reutilizados.

## Custo total do lote

Aplicar RN015:

~~~text
CustoLote =
    CustoBaseItens
  + CustoPerdasLote
  + CustoMaoDeObraLote
  + CustoEnergiaLote
~~~

Todos os componentes participam da composição, inclusive quando seu valor conhecido é zero.

## Custo unitário

Aplicar RN016:

~~~text
CustoUnitarioProduto =
    CustoLote / Rendimento
~~~

Rendimento representa unidades vendáveis produzidas pelo lote. Reduções de saída já refletidas no Rendimento afetam naturalmente o custo unitário sem necessidade de outra regra de perda.

## Completude

Aplicar RN017.

O custo total somente existe quando todos os quatro componentes forem determináveis.

~~~text
CustoBaseItens != null
E CustoPerdasLote != null
E CustoMaoDeObraLote != null
E CustoEnergiaLote != null

=> CustoLote conhecido
=> Completo = true
~~~

Se qualquer componente estiver indisponível:

~~~text
CustoLote = null
CustoUnitarioProduto = null
Completo = false
~~~

Não calcular nem apresentar soma parcial como custo total.

Os valores conhecidos continuam visíveis individualmente para explicabilidade.

## Null x zero

Zero conhecido é um valor válido e participa normalmente da soma.

~~~text
CustoBaseItens       = 30
CustoPerdasLote      = 0
CustoMaoDeObraLote   = 0
CustoEnergiaLote     = 0

=> CustoLote = 30
=> Completo = true
~~~

Null significa custo não determinável. UC022 não deve substituir null por zero.

## Ficha sem Itens

UC018 determina que Ficha sem Itens possui CustoBaseItens indisponível.

Mesmo que UC019 retorne CustoPerdasLote = 0 e UC020/UC021 possuam valores conhecidos:

~~~text
CustoLote = null
CustoUnitarioProduto = null
Completo = false
~~~

Uma Ficha vazia nunca representa lote de custo zero.

## Rendimento

A Ficha existente possui Rendimento > 0 conforme RN009.

A calculadora deve validar defensivamente sua entrada e rejeitar:

~~~text
Rendimento <= 0
~~~

Não retornar custo unitário zero, infinito ou indisponível silenciosamente para Rendimento inválido.

## Precisão

Aplicar RN026.

Não arredondar durante:

- soma dos componentes;
- divisão pelo Rendimento;
- armazenamento dos resultados em memória.

Arredondamento/formatação ocorre somente na apresentação ou nas fronteiras comerciais definidas posteriormente.

UC022 não aplica incremento comercial nem arredondamento do preço. Isso pertence ao UC023.

## Calculadora pura

Criar componente em Precificador.Core.Precificacao, preferencialmente:

~~~text
CalculadoraCustoProduto
~~~

Entrada conceitual:

~~~text
CustoBaseItens : decimal?
CustoPerdasLote : decimal?
CustoMaoDeObraLote : decimal?
CustoEnergiaLote : decimal?
Rendimento : decimal
~~~

Saída:

~~~text
CustoLote : decimal?
CustoUnitarioProduto : decimal?
Completo : bool
~~~

Semântica:

~~~text
qualquer componente null
=> CustoLote null
=> CustoUnitarioProduto null
=> Completo false

todos conhecidos
=> somar componentes
=> dividir pelo Rendimento
=> Completo true
~~~

A calculadora:

- não acessa EF;
- não acessa HTTP;
- não acessa tenant;
- não consulta preços;
- não consulta configuração;
- não formata valores;
- não persiste resultados.

Como defesa, rejeitar Rendimento <= 0 e valores conhecidos negativos para componentes de custo.

## Integração Web

Evoluir a tela existente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Não criar nova página para UC022.

Na Ficha, após os componentes de custo, apresentar pelo menos:

~~~text
Custo total do lote: <valor>
Rendimento: <valor>
Custo unitário do produto: <valor>
~~~

Usar a formatação já adotada pelo projeto para custos calculados.

## Precificação incompleta

Quando qualquer componente estiver indisponível:

~~~text
Custo total do lote: indisponível
Custo unitário do produto: indisponível
Precificação incompleta.
~~~

A apresentação deve informar quais componentes impedem a composição, sem esconder os componentes conhecidos.

As mensagens específicas já fornecidas pelos UCs anteriores continuam válidas, como ausência de preço vigente, custo base de mão de obra indisponível ou tarifa.

UC022 apenas consolida a situação.

## Produto sem Ficha

Produto sem Ficha não possui custo total ou unitário calculável.

Preservar o comportamento existente.

O GET:

- não cria Ficha;
- não cria configuração;
- não persiste qualquer resultado;
- não inventa custo zero.

## Produto inativo

Produto inativo continua calculável normalmente.

A situação Ativo/Inativo não altera a matemática da Ficha existente.

## Integração com GET/POST da Ficha

### GET

Após carregar Ficha, Itens/UC018, Perdas/UC019, Mão de obra/UC020 e Energia/UC021, executar a composição da UC022 usando esses resultados já disponíveis em memória.

### POST válido

Preservar o fluxo atual:

~~~text
validar
=> salvar
=> PRG
=> GET
=> recalcular todos os componentes
=> compor UC022
~~~

Não persistir resultados calculados.

### POST inválido

Preservar Input e erros informados pelo usuário, porém todos os custos apresentados devem representar o estado persistido atual.

UC022 deve receber os resultados calculados a partir da Ficha persistida.

Não simular Rendimento, TempoAtivo ou qualquer outro valor ainda não salvo.

## Recalculo atual

CustoLote e CustoUnitarioProduto são valores derivados.

Qualquer alteração válida em suas dependências deve aparecer no próximo GET, incluindo:

- preço vigente de Insumo;
- Quantidade;
- PercentualPerda;
- Rendimento;
- TempoAtivoMinutos;
- ValorHoraTrabalho;
- equipamentos;
- PotenciaKw;
- TempoUsoMinutos;
- TarifaEnergiaKwh;
- data operacional da Empresa quando afetar preço vigente.

Nenhum resultado da UC022 é persistido.

## Multiempresa

UC022 não introduz nova entidade tenant-owned.

O isolamento já aplicado às origens dos componentes deve ser preservado.

A calculadora pura não recebe EmpresaId.

Nenhum componente de outra Empresa pode chegar à composição por bypass de GQF ou consulta adicional.

UC022 não deve introduzir IgnoreQueryFilters.

## Critérios de aceitação

- **CA01:** todos os componentes conhecidos são somados conforme RN015.
- **CA02:** custo unitário é CustoLote / Rendimento conforme RN016.
- **CA03:** qualquer componente indisponível torna CustoLote e CustoUnitarioProduto indisponíveis.
- **CA04:** soma parcial nunca é apresentada como custo total.
- **CA05:** componente conhecido igual a zero é válido e participa da composição.
- **CA06:** null nunca é convertido em zero.
- **CA07:** Ficha sem Itens permanece com custo total/unitário indisponível.
- **CA08:** Rendimento fracionário é suportado.
- **CA09:** Rendimento inválido é rejeitado defensivamente pela calculadora.
- **CA10:** não há arredondamento intermediário.
- **CA11:** valores conhecidos continuam visíveis quando a composição está incompleta.
- **CA12:** a UI identifica claramente a precificação incompleta.
- **CA13:** Produto inativo continua calculável.
- **CA14:** Produto sem Ficha não cria estado nem apresenta custo zero.
- **CA15:** POST inválido calcula usando estado persistido.
- **CA16:** alterações nas dependências refletem no próximo GET.
- **CA17:** UC022 não executa nova consulta de preços/configuração para recompor dados já conhecidos.
- **CA18:** nenhum custo total ou unitário é persistido.
- **CA19:** isolamento multiempresa permanece preservado.

## Matriz de testes

### Unitários

- U1: soma normal dos quatro componentes;
- U2: todos os componentes zero;
- U3: perda zero com demais componentes conhecidos;
- U4: CustoBaseItens null;
- U5: CustoPerdasLote null;
- U6: CustoMaoDeObraLote null;
- U7: CustoEnergiaLote null;
- U8: múltiplos componentes null;
- U9: divisão por Rendimento inteiro;
- U10: Rendimento fracionário;
- U11: precisão sem arredondamento intermediário;
- U12: Rendimento zero rejeitado;
- U13: Rendimento negativo rejeitado;
- U14: custo conhecido negativo rejeitado.

### Web

- W1: Ficha completa mostra CustoLote correto;
- W2: Ficha completa mostra CustoUnitarioProduto correto;
- W3: componente zero mantém composição completa;
- W4: Item sem preço torna total/unitário indisponíveis;
- W5: perda positiva indeterminável torna total/unitário indisponíveis;
- W6: mão de obra indeterminável torna total/unitário indisponíveis;
- W7: energia indeterminável torna total/unitário indisponíveis;
- W8: múltiplas pendências são apresentadas sem soma parcial;
- W9: Ficha sem Itens permanece incompleta;
- W10: componentes conhecidos continuam visíveis quando total incompleto;
- W11: alteração de preço vigente recalcula total/unitário;
- W12: alteração de perda recalcula total/unitário;
- W13: alteração de Rendimento recalcula custo unitário;
- W14: alteração de ValorHora recalcula total/unitário;
- W15: alteração de energia recalcula total/unitário;
- W16: Produto inativo continua calculável;
- W17: Produto sem Ficha não mostra total/unitário;
- W18: POST inválido usa Rendimento/estado persistido;
- W19: GET não persiste custos;
- W20: isolamento entre Empresas permanece intacto.

## Persistência

Nenhuma alteração de schema.

Não adicionar:

- campo CustoLote à Ficha;
- campo CustoUnitario ao Produto;
- tabela de cálculo;
- snapshot;
- migration.

Os valores são calculados em tempo de consulta.

Snapshots comerciais pertencem ao UC011.

## Alterações esperadas

### Core

Adicionar calculadora pura para composição do custo do Produto.

### Web

Integrar o resultado à FichaTecnicaModel e à página existente da Ficha.

Reutilizar os quatro resultados já calculados pelos UCs anteriores.

### Persistência

Nenhuma alteração.

## Fora do escopo

- novo componente de custo;
- custos fixos administrativos;
- impostos;
- comissão;
- frete;
- categoria automática de custos;
- preço teórico;
- preço sugerido — UC023;
- margem atual — UC024;
- preço de prateleira e snapshot comercial — UC011;
- histórico de precificação — UC012;
- persistência de CustoLote ou CustoUnitarioProduto.

## Gate

UC018, UC019, UC020 e UC021 estão concluídas.

O estado atual já fornece separadamente CustoBaseItens, CustoPerdasLote, CustoMaoDeObraLote, CustoEnergiaLote e Rendimento.

RN015, RN016 e RN017 já definem a composição e a semântica de incompletude.

Não existe alteração estrutural necessária antes da implementação.

UC022 está liberada para implementação.

## Branch sugerida

~~~text
feat/uc022-custo-total-unitario
~~~

## Commit sugerido

~~~text
feat: calcula custo total e unitario do produto
~~~
