# Codex — UC030 Identificar Produtos com precificação incompleta

## Pré-condições

1. atualizar `master`;
2. criar/trocar para `feat/uc030-precificacao-incompleta`;
3. confirmar UC030 = `Pronto` no backlog;
4. ler integralmente:
   - `docs/use-cases/UC030-identificar-produtos-precificacao-incompleta.md`;
   - RN017/RN022/RN023/RN026/RN027/RN036/RN037;
   - UC023, UC024, UC025, UC028 e UC029;
   - F002, F004 e F005;
5. inspecionar:
   - `PrecificacaoProdutoAtual`;
   - `ResumoPrecificacaoProdutosAtual`;
   - calculadoras de custo/preço/margem;
   - Dashboard;
   - Produtos/Index;
   - testes atuais dessas superfícies.

Se UC030 não estiver `Pronto`, não implementar.

## Intenção

Introduzir um conceito global e estruturado de **completude da precificação atual**, distinto de `SituacaoMargem`, com motivos acionáveis e processamento em lote.

Não usar:

~~~text
SituacaoMargem == Incompleto
~~~

como definição de precificação incompleta.

Caso crítico que precisa funcionar:

~~~text
custo conhecido
preço de prateleira conhecido
margem calculável
incremento comercial ausente
=> margem pode estar Dentro/Abaixo
=> precificação global é Incompleta
~~~

## Classificação

A fotografia é completa somente quando:

~~~text
CustoProdutoCompleto
AND PrecoProdutoCompleto
AND PrecoPrateleiraAtual != null
AND MargemAtual != null
~~~

Materializar estado + motivos estruturados.

Motivos mínimos:

~~~text
FichaTecnicaAusente
FichaTecnicaSemItens
InsumoSemPrecoVigente
ConfiguracaoPrecificacaoAusente
TarifaEnergiaNaoConfigurada
IncrementoComercialNaoConfigurado
PrecoPrateleiraNaoDefinido
~~~

Não decidir por strings de UI.

## Compartilhamento

Evitar duas implementações divergentes da regra entre:

~~~text
PrecificacaoProdutoAtual
ResumoPrecificacaoProdutosAtual
~~~

Preferir classificador/helper puro compartilhado quando isso reduzir duplicação sem acoplar persistência ao Core.

O serviço em lote deve continuar realmente em lote.

É proibido:

~~~text
foreach produto
    await PrecificacaoProdutoAtual.CalcularAsync(produto.Id)
~~~

## Resumo em lote

Evoluir `ResumoPrecificacaoProdutoAtual` com estado de completude e motivos.

O mesmo carregamento em lote já usado para custo/preço/margem deve fornecer os fatos necessários para os motivos.

Não adicionar query por Produto.

Atenção:

- tarifa nula só é motivo se houver uso elétrico;
- configuração ausente gera motivo próprio, sem inventar submotivos internos;
- preço de prateleira ausente é motivo independente;
- incremento nulo pode coexistir com Margem disponível.

## Dashboard

Adicionar terceiro recorte:

~~~text
Todos os ativos
Abaixo da margem
Precificação incompleta
~~~

Query:

~~~text
/Dashboard?filtro=precificacao-incompleta
~~~

Preservar `aria-current="page"`/semântica equivalente para o modo ativo.

Cards continuam globais e não mudam de significado.

Adicionar coluna:

~~~text
Precificação
~~~

Mostrar `Completa` ou `Incompleta` + motivos.

## Produtos

Adicionar opção ao filtro existente:

~~~text
precificacao-incompleta
~~~

Preservar:

~~~text
q AND categoria AND filtro
~~~

Sem filtro, ativos e inativos continuam listados.

Com qualquer recorte operacional (`abaixo-da-margem` ou `precificacao-incompleta`), considerar somente Produtos ativos.

Adicionar coluna `Precificação`.

Não alterar a coluna `Situação`: ela continua Ativo/Inativo.

## Testes obrigatórios

### Unitários

Cobrir classificador de completude/motivos, especialmente:

- completo;
- ficha ausente;
- ficha sem itens;
- preço de Insumo ausente;
- configuração ausente;
- tarifa ausente com e sem uso;
- incremento ausente;
- preço de prateleira ausente;
- múltiplos motivos;
- ordem/deduplicação.

### Integração — precificação

Garantir equivalência entre caminho individual e lote.

Caso crítico:

~~~text
incremento nulo + custo/margem disponíveis
=> PrecificacaoCompleta false
=> SituacaoMargem não precisa ser Incompleto
~~~

### Web — Dashboard

- novo recorte;
- estado selecionado acessível;
- motivos exibidos;
- cards preservados;
- produto completo fora;
- inativo fora;
- vazio específico;
- filtro inválido preservado.

### Web — Produtos

- novo recorte;
- combinação q/categoria;
- inativo fora do recorte;
- visão normal preserva inativos;
- coluna Precificação;
- Situação continua Ativo/Inativo;
- limpar filtros;
- filtro inválido.

## Não fazer

- migration;
- persistir completude/motivos;
- novo card obrigatório;
- múltiplos filtros simultâneos;
- filtro por motivo;
- N+1;
- fórmula financeira duplicada;
- mudança de semântica de Margem indisponível;
- renomear `SituacaoMargem.Incompleto`;
- alterar UC030 para corrigir dados automaticamente.

## Documentação ao concluir

Atualizar o estado para `Concluído` em:

- backlog;
- UC030;
- catálogo/F005/F002/UC008 conforme necessário.

Não antecipar UC031.
