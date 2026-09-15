# Instrução Codex — UC024: Calcular margem atual e situação

## Tarefa

Implementar integralmente:

~~~text
docs/use-cases/UC024-calcular-margem-atual-situacao.md
~~~

Branch sugerida:

~~~text
feat/uc024-margem-atual-situacao
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- UC024;
- UC012;
- UC022;
- UC023;
- RN017, RN019, RN022, RN023, RN026 e RN027;
- `PrecificacaoProdutoAtual`;
- `RegistroPrecoProdutoConsultas`;
- `Produtos/Detalhes`.

## Regra central

Calcular:

~~~text
MargemAtual =
    (PrecoPrateleiraAtual - CustoUnitarioProduto)
    / PrecoPrateleiraAtual
~~~

Entradas atuais:

~~~text
CustoUnitarioProduto => UC022
PrecoPrateleiraAtual => registro atual UC012
MargemAlvo           => Produto atual
~~~

Nunca usar `CustoReferencia` ou `MargemReferencia` históricos para esse cálculo.

## Situação

~~~text
custo null OU preco atual null
=> MargemAtual null
=> Incompleto

MargemAtual < MargemAlvo
=> AbaixoDaMargem

MargemAtual >= MargemAlvo
=> DentroDaMargem
~~~

Sem arredondamento intermediário.

## Calculadora Core

Criar calculadora pura para Margem atual + Situação.

Validar:

~~~text
0 <= MargemAlvo < 1
custo conhecido >= 0
preco conhecido > 0
~~~

Null de custo/preço significa Incompleto, não exceção.

Cobrir U1–U14.

## Independência do UC023

Não usar `PrecoProdutoCompleto` como critério de margem.

Cenário obrigatório:

~~~text
IncrementoComercial = null
CustoUnitarioProduto conhecido
PrecoPrateleiraAtual conhecido

=> MargemAtual calculada
=> Situação completa
~~~

## Evolução de PrecificacaoProdutoAtual

A orquestração deve incorporar:

- registro atual via `SelecionarAtualAsync`;
- PrecoPrateleiraAtual;
- DataReferenciaPrecoAtual;
- MargemAtual;
- SituacaoMargem;
- impedimentos específicos da Margem atual.

Selecionar o preço comercial mesmo quando Ficha/configuração/custo estiverem incompletos.

Os retornos antecipados não podem apagar um preço atual existente.

Não duplicar a ordenação da UC012.

## Detalhes

Evoluir `/Produtos/Detalhes/{id:int}`.

Mostrar resumo:

- Custo unitário atual;
- Preço de prateleira atual;
- Data de referência;
- Margem-alvo;
- Margem atual;
- Situação.

Usar a mesma execução de `PrecificacaoProdutoAtual` para preço e margem.

Remover a consulta independente de preço atual hoje existente em `DetalhesModel`.

Produto ativo/inativo: mesmo cálculo.

## Incompletude

Sem preço:

~~~text
Preço de prateleira atual: não definido.
Margem atual: indisponível
Situação: Incompleto
~~~

Com preço, mas custo incompleto:

- preço/data continuam visíveis;
- margem indisponível;
- situação Incompleto;
- apresentar motivos de custo relevantes.

Não listar `IncrementoComercial não configurado` como motivo de Margem atual incompleta.

## Persistência

Nenhuma migration.

Não alterar ModelSnapshot.

Não persistir:

- MargemAtual;
- SituacaoMargem;
- PrecoPrateleiraAtual;
- CustoUnitarioProduto.

## Multiempresa

- manter GQF;
- sem `IgnoreQueryFilters` no fluxo comum;
- cross-tenant = 404;
- preço atual/custo/configuração sempre da mesma Empresa.

## Testes

Implementar:

- U1–U14;
- P1–P8;
- W1–W22.

Pontos críticos da revisão:

1. custo atual != CustoReferencia histórico;
2. MargemAlvo atual != MargemReferencia histórica;
3. incremento null não impede MargemAtual;
4. preço atual continua visível quando custo está incompleto;
5. Produto sem Ficha + histórico existente;
6. igualdade com meta = DentroDaMargem;
7. margem negativa é válida;
8. fronteira de precisão não pode mudar estado por arredondamento;
9. Ficha Técnica não pode regredir após ampliar `PrecificacaoProdutoAtual`;
10. nenhum detalhamento do UC025 deve ser antecipado.

## Backlog

Na PR de implementação:

~~~text
UC024: Pronto -> Concluído
~~~

Não alterar UC025/UC028 para Pronto ou Concluído.

## Validação obrigatória

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

Informar:

- arquivos alterados;
- desenho da calculadora;
- evolução de `PrecificacaoProdutoAtual`;
- tratamento de completude independente do UC023;
- cobertura U/P/W;
- confirmação de ausência de migration;
- resultado de build/test;
- URL da PR.
