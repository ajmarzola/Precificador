# MEL022 — Substituir custo de mão de obra por percentual sobre os insumos

- **Origem:** segunda rodada de testes manuais.
- **Classificação:** regra de precificação / configuração por Empresa / simplificação da Ficha Técnica.
- **Prioridade:** alta.
- **Estado:** Concluído.
- **Ordem de implementação:** 08.
- **Dependências:** MEL020 concluída.
- **Gate operacional:** antes de MEL023 e MEL021.
- **Alteração de regra de negócio:** sim.
- **Alteração de domínio:** sim.
- **Alteração de schema:** sim.
- **Migration:** sim.
- **Persistência de resultado calculado:** não.

## Objetivo

Substituir o modelo atual de mão de obra baseado em tempo ativo × valor/hora por um modelo percentual sobre o custo base dos insumos.

Modelo atual:

```text
CustoMaoDeObraLote =
    (TempoAtivoMinutos / 60)
    × ValorHoraTrabalho
```

Modelo após MEL022:

```text
CustoMaoDeObraLote =
    CustoBaseItens
    × PercentualMaoDeObra
```

Exemplo:

```text
CustoBaseItens = R$ 12,00
PercentualMaoDeObra = 10%
CustoMaoDeObraLote = R$ 1,20
```

O objetivo é tornar o modelo mais simples e mais aderente ao cenário de produção artesanal, no qual estimar um valor/hora e controlar tempo ativo por Produto mostrou-se menos natural do que aplicar uma parcela de mão de obra proporcional ao material utilizado.

## Decisões centrais

### D1 — Percentual de mão de obra pertence à Empresa

Substituir em `ConfiguracaoPrecificacaoEmpresa`:

```text
ValorHoraTrabalho : decimal?
```

por:

```text
PercentualMaoDeObra : decimal
```

O valor é armazenado como fração decimal:

```text
10% = 0,10
25% = 0,25
150% = 1,50
```

### D2 — Default obrigatório de 10%

Definir:

```text
PercentualMaoDeObraPadrao = 0,10
```

Toda nova configuração de Empresa nasce com 10%.

A migration deve atribuir 10% às configurações existentes.

Não existe estado `null` para PercentualMaoDeObra após MEL022.

Consequentemente, deixa de existir o impedimento:

```text
Valor da hora de trabalho não configurado.
```

O pedido de link para configurar Valor da hora é absorvido por esta mudança e não deve ser implementado como fluxo transitório.

### D3 — Percentual não possui teto de 100%

Validação normativa:

```text
PercentualMaoDeObra >= 0
```

Não impor:

```text
PercentualMaoDeObra < 1
```

Motivo: em trabalho artesanal, o custo de mão de obra pode legitimamente superar o custo dos materiais.

Exemplos válidos:

```text
0%   = 0,00
10%  = 0,10
100% = 1,00
250% = 2,50
```

Valor negativo é inválido.

Não criar limite superior arbitrário no domínio.

### D4 — Base da mão de obra é exclusivamente CustoBaseItens

Aplicar RN013 revisada:

```text
CustoMaoDeObraLote =
    CustoBaseItens
    × PercentualMaoDeObra
```

Entram na base:

- custos dos Itens calculados por UC018.

Não entram:

- CustoPerdasLote;
- CustoEnergiaLote;
- CustoMaoDeObraLote;
- Rendimento;
- MargemAlvo;
- Preço teórico;
- Preço sugerido;
- Preço de prateleira;
- Reserva comercial.

A mão de obra é calculada antes da composição do UC022.

### D5 — Completude passa a depender dos Itens

Semântica:

```text
CustoBaseItens conhecido
=> CustoMaoDeObraLote conhecido
```

```text
CustoBaseItens = 0
=> CustoMaoDeObraLote = 0
```

```text
PercentualMaoDeObra = 0
+ CustoBaseItens conhecido
=> CustoMaoDeObraLote = 0
```

```text
CustoBaseItens indisponível
=> CustoMaoDeObraLote indisponível
```

Não transformar item sem preço vigente em custo zero.

Ficha sem Itens continua com `CustoBaseItens` indisponível conforme UC018; portanto a mão de obra também fica indisponível.

### D6 — TempoAtivoMinutos deixa de existir no MVP

Após D4, `FichaTecnica.TempoAtivoMinutos` não possui mais consumidor funcional no MVP.

Remover:

- propriedade do domínio;
- coluna de banco;
- campo da UI;
- parsing/validação;
- projeções;
- DTOs/records;
- testes específicos de tempo ativo;
- referências no detalhamento atual.

A Ficha Técnica passa a possuir como base universal:

```text
Rendimento
```

Não manter campo obrigatório sem efeito.

Se tempo de produção voltar a ser necessário futuramente para agenda, capacidade produtiva, lead time ou produtividade, criar requisito próprio.

### D7 — Rendimento permanece inalterado

`FichaTecnica.Rendimento` continua obrigatório e > 0.

Após MEL022:

```text
FichaTecnica.Criar(
    empresaId,
    produtoId,
    rendimento)
```

e:

```text
AtualizarBase(rendimento)
```

ou API equivalente.

MEL019 continua válida:

- GET de Produto sem Ficha pré-preenche Rendimento com 1;
- não persiste no GET;
- POST inválido não reaplica default.

## Configuração da Empresa

Modelo alvo:

```text
ConfiguracaoPrecificacaoEmpresa
- EmpresaId
- PercentualMaoDeObra : decimal obrigatório
- TarifaEnergiaKwh : decimal?
- MargemPadrao : decimal?
- IncrementoComercial : decimal?
- ReservaComercialDesconto : decimal obrigatório
```

### Precisão

Mapear:

```text
PercentualMaoDeObra
decimal(9,6)
```

Mesmo padrão de precisão de outros percentuais.

Não arredondar durante o cálculo.

### CriarPadrao

`CriarPadrao(empresaId)` deve resultar em:

```text
PercentualMaoDeObra = 0,10
TarifaEnergiaKwh = null
MargemPadrao = null
IncrementoComercial = null
ReservaComercialDesconto = 0,10
```

### Atualizar

A operação de atualização da configuração deve receber PercentualMaoDeObra obrigatório.

Não permitir limpar o campo para `null`.

Entrada vazia na UI deve gerar erro de obrigatório.

## UI de Configurações

Substituir:

```text
Valor da hora de trabalho
```

por:

```text
Mão de obra sobre os insumos (%)
```

ou redação equivalente inequívoca.

Exibir ajuda contextual sempre visível, preservando MEL017.

Texto recomendado:

> Percentual aplicado sobre o custo base dos insumos para compor o custo de mão de obra do lote. Ex.: insumos de R$ 12,00 e percentual de 10% geram R$ 1,20 de mão de obra. O percentual pode ser maior que 100% quando o trabalho artesanal tiver peso maior que os materiais.

Consulta:

```text
Mão de obra sobre os insumos: 10%
```

Edição:

- entrada em percentual humano;
- `10` => 10%;
- `150` => 150%;
- aceitar vírgula e ponto conforme MEL015;
- zero válido;
- negativo inválido;
- vazio inválido.

## Calculadora Core

Manter uma calculadora pura, preferencialmente com o nome existente:

```text
CalculadoraCustoMaoDeObra
```

Nova assinatura conceitual:

```text
Calcular(
    decimal? custoBaseItens,
    decimal percentualMaoDeObra)
```

Resultado:

```text
CustoMaoDeObraLote : decimal?
Completo : bool
```

Regras defensivas:

- percentual negativo => `ArgumentOutOfRangeException`;
- percentual >= 0 => válido;
- custo base negativo => rejeitar defensivamente se informado;
- custo base null => resultado null/incompleto;
- custo base conhecido × percentual 0 => 0/completo.

Não acessar EF/Web.

## Orquestração da precificação atual

Em `PrecificacaoProdutoAtual`:

ordem conceitual:

```text
1. calcular itens
2. obter CustoBaseItens
3. calcular perdas
4. calcular mão de obra sobre CustoBaseItens
5. calcular energia
6. compor CustoLote
7. dividir por Rendimento
8. calcular preços/margem
```

A mão de obra deixa de receber qualquer dado da Ficha além da relação indireta com os Itens.

`ResultadoPrecificacaoProdutoAtual` deve:

remover:

```text
TempoAtivoMinutos
ValorHoraTrabalho
```

e adicionar:

```text
PercentualMaoDeObra
```

O percentual atual deve ficar disponível para explicabilidade.

## Ficha Técnica

Na rota:

```text
/Produtos/FichaTecnica/{id}
```

remover o input:

```text
Tempo ativo de trabalho (minutos)
```

A base passa a editar somente Rendimento.

Se existir Ficha:

```text
Custo de mão de obra do lote:
<valor calculado>
Mão de obra: <percentual vigente>
```

A redação exata pode ser ajustada mantendo clareza.

Não mostrar mais:

```text
Valor da hora de trabalho não configurado.
```

### POST

POST da Ficha passa a validar apenas Rendimento.

POST válido:

- cria/atualiza Ficha;
- PRG;
- recalcula custo no GET.

POST inválido:

- preserva Rendimento informado;
- não altera Ficha;
- custos exibidos continuam representando estado persistido, conforme padrão atual.

## Detalhamento de Precificação

Na rota de UC025:

remover:

```text
Tempo ativo
Valor da hora
```

adicionar:

```text
Percentual de mão de obra
Base da mão de obra = Custo base dos itens
Custo de mão de obra do lote
```

A tela deve tornar explicável:

```text
R$ 12,00 × 10% = R$ 1,20
```

sem introduzir arredondamento intermediário.

## Custo total

UC022 continua:

```text
CustoLote =
    CustoBaseItens
    + CustoPerdasLote
    + CustoMaoDeObraLote
    + CustoEnergiaLote
```

Apenas muda a origem de `CustoMaoDeObraLote`.

Não aplicar percentual novamente sobre perdas/energia.

## Registro de Preço e histórico

### Registros existentes

`RegistroPrecoProduto.CustoReferencia` já congela o custo unitário usado no momento da decisão comercial.

Portanto:

- não alterar registros históricos;
- não recalcular `CustoReferencia`;
- não adicionar `PercentualMaoDeObraReferencia` nesta MEL;
- não fazer backfill de históricos;
- UC012 continua exibindo snapshots antigos como foram registrados.

Alterar o modelo atual pode mudar o custo atual de um Produto, mas isso não reinterpreta decisões passadas.

### Novos registros

Após MEL022, novos registros de preço congelam o `CustoReferencia` produzido pelo novo modelo.

Não há necessidade de expandir o snapshot comercial apenas para guardar o percentual, pois UC011 atualmente congela o resultado final de custo e não snapshots de cada componente.

## Migration SQL Server

Criar migration evolutiva, por exemplo:

```text
ReplaceHourlyLaborWithPercentage
```

Não rebaselinear migrations da MEL020.

### Up

A migration deve, conceitualmente:

1. adicionar `PercentualMaoDeObra decimal(9,6) NOT NULL DEFAULT 0.10` em `ConfiguracoesPrecificacaoEmpresas`;
2. garantir 0,10 para todas as linhas existentes;
3. remover `ValorHoraTrabalho`;
4. remover `TempoAtivoMinutos` de `FichasTecnicas`;
5. atualizar ModelSnapshot.

A ordem concreta gerada pode variar desde que o resultado final seja equivalente.

### Dados existentes

Não tentar converter ValorHora + TempoAtivo em percentual.

Não existe transformação semanticamente confiável porque a fórmula antiga depende de cada Ficha e do custo dos insumos.

Decisão explícita:

```text
todas as Empresas existentes
-> PercentualMaoDeObra = 10%
```

Os antigos:

```text
ValorHoraTrabalho
TempoAtivoMinutos
```

são descartados.

Esta perda é intencional e aprovada antes da publicação produtiva Azure.

### Down

É aceitável que `Down` restaure apenas a estrutura anterior:

- `ValorHoraTrabalho` nullable;
- `TempoAtivoMinutos` obrigatório com default técnico 0;
- remover `PercentualMaoDeObra`.

Não é possível recuperar os valores antigos descartados.

Documentar essa limitação na migration/PR.

## Seed

O seed da Empresa técnica Id=1 deve passar a incluir:

```text
PercentualMaoDeObra = 0,10
ReservaComercialDesconto = 0,10
```

A migration precisa ser compatível com o `HasData` atualizado sem gerar mutação inesperada do seed.

## Multiempresa

Preservar FT002/RN039:

- PercentualMaoDeObra pertence à Empresa;
- GQF normal;
- request não informa EmpresaId;
- Empresa A nunca usa percentual da Empresa B;
- alteração afeta apenas Empresa Ativa.

## Documentação que deve ser alinhada na implementação

No mínimo:

- RN013;
- RN025;
- UC013;
- UC017;
- UC020;
- UC022;
- UC025;
- UC026;
- UC027;
- F003;
- F004;
- F006;
- MEL017;
- MEL019, com nota de que TempoAtivo deixou de existir;
- documentação Codex histórica apenas quando necessário para evitar contradição como instrução vigente.

Documentos históricos podem manter o comportamento original se explicitamente marcados como superados pela MEL022.

## Critérios de aceitação

- **CA01:** `ValorHoraTrabalho` deixa de existir no modelo ativo.
- **CA02:** `TempoAtivoMinutos` deixa de existir no modelo ativo.
- **CA03:** existe `PercentualMaoDeObra` obrigatório por Empresa.
- **CA04:** default é 0,10.
- **CA05:** configurações existentes recebem 0,10 na migration.
- **CA06:** novas Empresas recebem 0,10 via `CriarPadrao`.
- **CA07:** percentual zero é válido.
- **CA08:** percentual 100% é válido.
- **CA09:** percentual >100% é válido.
- **CA10:** percentual negativo é rejeitado.
- **CA11:** vazio na UI é rejeitado como obrigatório.
- **CA12:** entrada `10` representa 10%, não 1000%.
- **CA13:** entrada pt-BR/invariant respeita MEL015.
- **CA14:** mão de obra usa exclusivamente `CustoBaseItens × PercentualMaoDeObra`.
- **CA15:** perdas não entram na base.
- **CA16:** energia não entra na base.
- **CA17:** rendimento não entra na base.
- **CA18:** custo base conhecido + percentual 0 => custo mão de obra 0/completo.
- **CA19:** custo base indisponível => mão de obra indisponível.
- **CA20:** ficha vazia não ganha mão de obra artificial.
- **CA21:** não há arredondamento intermediário.
- **CA22:** Ficha edita apenas Rendimento na base.
- **CA23:** MEL019 continua pré-preenchendo Rendimento=1 somente no GET sem Ficha.
- **CA24:** POST inválido da Ficha preserva Input e não persiste.
- **CA25:** Configurações exibem PercentualMaoDeObra.
- **CA26:** ajuda contextual explica base e permite >100%.
- **CA27:** Ficha não exibe mensagem de ValorHora ausente.
- **CA28:** detalhamento atual exibe percentual/base/custo de mão de obra.
- **CA29:** UC022 soma o novo componente uma única vez.
- **CA30:** registros históricos de preço permanecem byte/semanticamente inalterados.
- **CA31:** novos Registros usam o custo atual calculado pela nova fórmula.
- **CA32:** migration é evolutiva sobre o baseline SQL Server da MEL020.
- **CA33:** seed técnico continua válido.
- **CA34:** GQF/guard tenant permanecem válidos.
- **CA35:** suíte unitária completa passa.
- **CA36:** suíte integração SQL Server completa passa.
- **CA37:** build Release sem warnings novos relevantes.
- **CA38:** MEL023 e MEL021 não são antecipadas.

## Matriz mínima de testes

### Domínio — Configuração

- D1: CriarPadrao => 10%;
- D2: Atualizar 0%;
- D3: Atualizar 10%;
- D4: Atualizar 100%;
- D5: Atualizar 250%;
- D6: negativo rejeitado;
- D7: tenant imutável.

### Domínio — Ficha

- D8: criar apenas com Rendimento;
- D9: Rendimento <=0 rejeitado;
- D10: atualizar Rendimento;
- D11: ownership preservado.

### Unitários — mão de obra

- U1: 12 × 10% = 1,2;
- U2: 12 × 0% = 0/completo;
- U3: 12 × 100% = 12;
- U4: 12 × 250% = 30;
- U5: custo null + 10% => null/incompleto;
- U6: custo 0 + 10% => 0/completo;
- U7: alta precisão sem arredondamento;
- U8: percentual negativo rejeitado;
- U9: custo negativo rejeitado defensivamente.

### Persistência/migration

- P1: banco no baseline MEL020 aplica migration MEL022;
- P2: configuração existente recebe 0,10;
- P3: coluna ValorHora removida;
- P4: coluna TempoAtivo removida;
- P5: PercentualMaoDeObra decimal(9,6), NOT NULL;
- P6: seed Empresa 1 = 0,10;
- P7: nova Empresa + CriarPadrao = 0,10;
- P8: GQF;
- P9: guard cross-tenant;
- P10: nenhuma migration pendente após update.

### Web — Configurações

- W1: consulta mostra percentual;
- W2: edição inicia com 10%;
- W3: salva 15%;
- W4: aceita 150%;
- W5: aceita zero;
- W6: rejeita negativo;
- W7: rejeita vazio;
- W8: aceita vírgula/ponto;
- W9: isolamento tenant;
- W10: ajuda MEL017 atualizada.

### Web — Ficha

- W11: não existe input TempoAtivo;
- W12: ficha nova usa somente Rendimento;
- W13: Rendimento default 1 permanece;
- W14: POST válido cria/atualiza;
- W15: POST inválido não altera persistido;
- W16: custo mão de obra 10%;
- W17: item sem preço => mão de obra indisponível;
- W18: ficha vazia => mão de obra indisponível;
- W19: alteração de percentual reflete no próximo GET;
- W20: Produto inativo continua calculável.

### Web — Detalhamento

- W21: não mostra Tempo ativo;
- W22: não mostra Valor da hora;
- W23: mostra percentual;
- W24: mostra base e custo de mão de obra;
- W25: explicabilidade numérica coerente.

### Histórico

- W26: registro comercial antigo preserva CustoReferencia;
- W27: mudança do percentual não reinterpreta histórico;
- W28: novo registro congela novo custo atual.

## Arquivos esperados

Não exaustivo:

```text
src/Precificador.Core/Empresas/ConfiguracaoPrecificacaoEmpresa.cs
src/Precificador.Core/FichasTecnicas/FichaTecnica.cs
src/Precificador.Core/Precificacao/CalculadoraCustoMaoDeObra.cs

src/Precificador.Infrastructure/Persistence/Configurations/ConfiguracaoPrecificacaoEmpresaConfiguration.cs
src/Precificador.Infrastructure/Persistence/Configurations/FichaTecnicaConfiguration.cs
src/Precificador.Infrastructure/Migrations/*

src/Precificador.Web/Precificacao/PrecificacaoProdutoAtual.cs
src/Precificador.Web/Pages/Configuracoes/Precificacao*
src/Precificador.Web/Pages/Produtos/FichaTecnica*
src/Precificador.Web/Pages/Produtos/Precificacao*

tests/Precificador.Tests.Unit/*
tests/Precificador.Tests.Integration/*

docs/business/business-rules.md
docs/features/F003-ficha-tecnica.md
docs/features/F004-precificacao.md
docs/features/F006-configuracoes.md
docs/use-cases/*
docs/development/backlog.md
```

## Fora do escopo

- MEL023;
- link para configurar tarifa;
- salvar Ficha antes de navegar para Configurações;
- salário/funcionário;
- encargos;
- custo por atividade;
- percentual diferente por Produto;
- histórico de percentual;
- snapshot de percentual;
- tempo de produção/capacidade/lead time;
- publicação Azure;
- UC028+.

## Definition of Done específica

MEL022 está concluída quando:

- modelo percentual substitui integralmente o modelo hora/tempo;
- schema SQL Server foi migrado evolutivamente;
- dados antigos de hora/tempo foram descartados conforme decisão;
- Empresas existentes e novas possuem 10%;
- cálculo atual usa CustoBaseItens × Percentual;
- Ficha não possui TempoAtivo;
- Configurações não possuem ValorHora;
- UI e detalhamento estão coerentes;
- histórico comercial permanece preservado;
- documentação normativa foi alinhada;
- toda suíte passa em SQL Server;
- backlog marca MEL022 como Concluído;
- MEL023 permanece Planejado até merge da MEL022;
- MEL021 permanece bloqueada.

## Branch obrigatória para implementação

```text
feat/mel022-mao-de-obra-percentual
```
