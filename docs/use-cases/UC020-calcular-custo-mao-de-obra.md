# UC020 — Calcular custo de mão de obra

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC018, UC026 e UC027
- **Alteração de schema:** não no UC original; MEL022 removeu o modelo hora/tempo
- **Persistência de resultado:** não

## Nota de atualização — MEL022

A implementação vigente de UC020 foi substituída pela MEL022. O modelo antigo:

~~~text
TempoAtivoMinutos × ValorHoraTrabalho
~~~

está superado e não faz parte do modelo ativo.

## Objetivo vigente

Calcular o custo atual de mão de obra do lote usando exclusivamente:

- `CustoBaseItens` produzido pelo UC018;
- `ConfiguracaoPrecificacaoEmpresa.PercentualMaoDeObra` da Empresa Ativa.

UC020 acrescenta um componente ao motor de custo. Não soma componentes nem calcula custo total/unitário do Produto.

## Regra normativa

Aplicar RN013:

~~~text
CustoMaoDeObraLote =
    CustoBaseItens
    × PercentualMaoDeObra
~~~

`PercentualMaoDeObra` é armazenado como fração decimal. Exemplos:

~~~text
10%  = 0,10
100% = 1,00
250% = 2,50
~~~

Não há teto de 100%. Valor negativo é inválido.

## Null x zero

~~~text
CustoBaseItens = null
=> CustoMaoDeObraLote = indisponível
=> Completo = false
~~~

~~~text
CustoBaseItens conhecido
E PercentualMaoDeObra = 0
=> CustoMaoDeObraLote = 0
=> Completo = true
~~~

Ficha sem Itens mantém `CustoBaseItens` indisponível conforme UC018; portanto mão de obra também fica indisponível.

## Precisão

Aplicar RN026. Não arredondar durante a multiplicação. Formatação ocorre somente na apresentação.

## Modelo de cálculo

Componente puro em `Precificador.Core.Precificacao`:

~~~text
CalculadoraCustoMaoDeObra
~~~

Entrada:

~~~text
CustoBaseItens : decimal?
PercentualMaoDeObra : decimal
~~~

Resultado:

~~~text
CustoMaoDeObraLote : decimal?
Completo : bool
~~~

Rejeitar defensivamente:

- `CustoBaseItens < 0`, quando informado;
- `PercentualMaoDeObra < 0`.

## Origem e isolamento dos dados

`CustoBaseItens` vem da orquestração corrente de precificação.

`PercentualMaoDeObra` vem da `ConfiguracaoPrecificacaoEmpresa` da Empresa Ativa.

Preservar FT002/RN039:

- GQF normal;
- sem `IgnoreQueryFilters` em produção;
- sem EmpresaId no request;
- sem percentual de mão de obra no request da Ficha;
- Empresa A nunca usa configuração da Empresa B.

Se a entidade de configuração estiver ausente:

- retornar 404 nas telas que dependem dela;
- não criar configuração silenciosamente;
- não inventar percentual.

## Apresentação

Na Ficha Técnica e no detalhamento da precificação, quando houver Ficha:

~~~text
Custo de mão de obra do lote: <valor ou indisponível>
Mão de obra sobre os insumos: <percentual vigente>
~~~

O detalhamento deve explicar que a base da mão de obra é `CustoBaseItens`, sem incluir perdas, energia ou rendimento na base.

Produto sem Ficha não mostra esse componente e GET não cria Ficha.

Produto inativo continua calculável sem reativação.

## Recalculo atual

O resultado é derivado em tempo de consulta.

Alterações em:

- preço vigente dos Itens;
- composição da Ficha;
- `PercentualMaoDeObra`

devem aparecer no próximo GET.

Não persistir snapshot de mão de obra. O histórico comercial preserva apenas o `CustoReferencia` final vigente no momento do registro.

## Critérios de aceitação vigentes

- **CA01:** custo base conhecido × 10% calcula 10% do custo base.
- **CA02:** percentual zero produz custo zero e componente completo quando o custo base é conhecido.
- **CA03:** percentual de 100% e maior que 100% é válido.
- **CA04:** custo base indisponível produz mão de obra indisponível.
- **CA05:** percentual negativo é rejeitado.
- **CA06:** custo base negativo é rejeitado defensivamente.
- **CA07:** não há arredondamento intermediário.
- **CA08:** alteração de percentual reflete no próximo GET.
- **CA09:** Produto inativo continua calculável.
- **CA10:** isolamento tenant é preservado.
- **CA11:** configuração ausente retorna 404 sem criação.
- **CA12:** Produto sem Ficha não calcula nem cria Ficha.
- **CA13:** custo não é persistido.

## Fora do escopo

- múltiplos tipos de mão de obra;
- funcionários, salários ou encargos;
- custo por atividade;
- percentual diferente por Produto;
- snapshot próprio de percentual de mão de obra;
- perdas — UC019;
- energia/equipamentos — UC021;
- custo total/unitário — UC022;
- preço sugerido — UC023.
