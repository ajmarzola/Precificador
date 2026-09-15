# Instrução Codex — UC023: Calcular preço teórico e sugerido

## Tarefa

Implementar integralmente:

~~~text
docs/use-cases/UC023-calcular-preco-teorico-sugerido.md
~~~

Branch:

~~~text
feat/uc023-preco-teorico-sugerido
~~~

Não trabalhar em master e não fazer merge da própria PR.

## Antes de editar

1. atualizar master;
2. criar a branch;
3. confirmar UC023 = Pronto;
4. ler UC023, UC022, UC027, F004, MEL009, RN017, RN019, RN020, RN021, RN025, RN026 e RN045;
5. inspecionar CalculadoraCustoProduto, FichaTecnicaModel, ConfiguracaoPrecificacaoEmpresa e a formatação atual de custos/configurações.

## Calculadora pura

Criar no Core, preferencialmente:

~~~text
CalculadoraPrecoProduto
~~~

Entradas:

~~~text
decimal? custoUnitarioProduto
decimal margemAlvo
decimal? incrementoComercial
~~~

Saída:

~~~text
decimal? PrecoTeorico
decimal? PrecoSugerido
bool Completo
~~~

## Fórmula

~~~text
PrecoTeorico = custoUnitarioProduto / (1 - margemAlvo)

PrecoSugerido =
    decimal.Ceiling(PrecoTeorico / incrementoComercial)
    * incrementoComercial
~~~

Usar decimal. Não converter para double.

Se PrecoTeorico já for múltiplo exato do incremento, PrecoSugerido deve permanecer igual a PrecoTeorico.

## Validação defensiva

Rejeitar:

~~~text
margemAlvo < 0
margemAlvo >= 1
custoUnitarioProduto conhecido < 0
incrementoComercial conhecido <= 0
~~~

null continua permitido para custoUnitarioProduto e incrementoComercial conforme a especificação.

## Incompletude

~~~text
custoUnitarioProduto = null
=> PrecoTeorico null
=> PrecoSugerido null
=> Completo false
~~~

~~~text
custo conhecido + incrementoComercial = null
=> PrecoTeorico calculado
=> PrecoSugerido null
=> Completo false
~~~

~~~text
custo conhecido + incremento conhecido
=> ambos calculados
=> Completo true
~~~

Zero conhecido não vira null.

## Integração com UC022

Reutilizar diretamente CustoUnitarioProduto já calculado.

Não recalcular CustoLote / Rendimento e não repetir UC018–UC021.

## Produto

Ampliar a projeção existente do Produto para incluir MargemAlvo.

Não criar nova query.

A margem usada é Produto.MargemAlvo, nunca Configuracao.MargemPadrao.

## Configuração

Ampliar a projeção existente ConfiguracaoPrecificacaoResumo para incluir IncrementoComercial.

Reutilizar a mesma consulta que já traz ValorHoraTrabalho e TarifaEnergiaKwh.

Não criar segunda query de configuração para UC023.

## MEL009

Não passar ReservaComercialDesconto para a calculadora.

Não implementar ReservaComercialReferencia ou DescontoReferencia.

Alterar a reserva não pode alterar PrecoTeorico/PrecoSugerido.

## Web

Na página existente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

adicionar:

~~~text
Margem-alvo
Preço teórico
Preço sugerido
~~~

Com incremento ausente:

~~~text
Preço teórico: <valor>
Preço sugerido: indisponível
Incremento comercial não configurado.
Precificação incompleta.
~~~

Com custo incompleto:

~~~text
Preço teórico: indisponível
Preço sugerido: indisponível
~~~

Preservar os motivos específicos dos componentes anteriores.

## POST inválido

Preservar o Input inválido, mas CustoUnitarioProduto, PrecoTeorico e PrecoSugerido devem usar somente o estado persistido.

Não simular Rendimento ou TempoAtivo ainda não salvos.

## Produto inativo

Calcular normalmente, sem reativar.

## Produto sem Ficha

Não criar Ficha/configuração, não mostrar preço zero e não persistir cálculo.

## Persistência

Nenhuma migration.

Não adicionar PrecoTeorico/PrecoSugerido persistidos a Produto ou Ficha.

Não criar RegistroPrecoProduto; isso pertence ao UC011.

## Testes

Atender U1–U16 e W1–W22 da UC023.

Riscos prioritários:

1. usar markup em vez de margem sobre preço;
2. arredondar PrecoTeorico antes da RN021;
3. múltiplo exato receber incremento extra;
4. incremento null virar zero;
5. esconder PrecoTeorico quando somente incremento estiver ausente;
6. usar MargemPadrao no lugar de MargemAlvo;
7. usar ReservaComercialDesconto no preço;
8. criar segunda query de configuração;
9. recalcular custo unitário;
10. persistir/snapshotar preço antes do UC011.

## Documentação da implementação

Na PR de implementação, mover somente:

~~~text
UC023: Pronto -> Concluído
~~~

Preservar a ordem normativa do backlog.

## Validação obrigatória

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

- arquivos alterados;
- calculadora criada;
- fórmula implementada;
- tratamento de incremento ausente;
- integração na Ficha;
- cobertura U/W;
- resultado de build/test;
- URL da PR.
