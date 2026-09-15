# Instrução Codex — UC022: Calcular custo total e custo unitário

## Tarefa

Implementar:

~~~text
docs/use-cases/UC022-calcular-custo-total-unitario.md
~~~

Branch:

~~~text
feat/uc022-custo-total-unitario
~~~

Não trabalhar em master nem fazer merge da própria PR.

## Antes de editar

1. atualizar master e criar a branch;
2. confirmar UC022 = Pronto;
3. ler UC022, F004 e RN009/RN015/RN016/RN017/RN026;
4. inspecionar as calculadoras de UC018–UC021 e FichaTecnicaModel;
5. preservar integralmente os cálculos individuais existentes.

## Implementar

Criar calculadora pura no Core, preferencialmente:

~~~text
CalculadoraCustoProduto
~~~

Entradas:

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

Regra:

~~~text
se qualquer componente = null
    CustoLote = null
    CustoUnitarioProduto = null
    Completo = false
senão
    CustoLote =
        CustoBaseItens
      + CustoPerdasLote
      + CustoMaoDeObraLote
      + CustoEnergiaLote

    CustoUnitarioProduto =
        CustoLote / Rendimento

    Completo = true
~~~

Zero é valor conhecido e válido.

Nunca converter null em zero nem apresentar soma parcial como total.

Validar defensivamente:

~~~text
Rendimento > 0
custos conhecidos >= 0
~~~

Não arredondar resultados intermediários.

## Integração

Usar os resultados que FichaTecnicaModel já possui para UC018–UC021.

Não:

- consultar preços novamente;
- consultar configuração novamente;
- recalcular os componentes dentro da nova calculadora;
- criar query adicional apenas para UC022.

Executar a composição somente depois de todos os componentes existentes terem sido carregados.

## Web

Na Ficha Técnica existente, acrescentar:

~~~text
Custo total do lote
Rendimento
Custo unitário do produto
~~~

Completo:

~~~text
Custo total do lote: <valor>
Custo unitário do produto: <valor>
~~~

Incompleto:

~~~text
Custo total do lote: indisponível
Custo unitário do produto: indisponível
Precificação incompleta.
~~~

Identificar os componentes indisponíveis sem esconder valores individuais conhecidos.

Ficha sem Itens continua incompleta.

Produto inativo continua calculável.

Produto sem Ficha não cria estado nem apresenta custo zero.

POST inválido da base deve compor UC022 usando exclusivamente o estado persistido, preservando o Input inválido na tela.

## Persistência

Nenhuma migration.

Não adicionar campos de custo à Ficha ou Produto.

Não persistir:

~~~text
CustoLote
CustoUnitarioProduto
Completo
~~~

Snapshot pertence ao UC011.

## Não implementar

- preço teórico;
- preço sugerido;
- preço de prateleira;
- margem;
- histórico;
- impostos/comissões/frete;
- novos componentes de custo;
- snapshots comerciais.

## Testes

Atender U1–U14 e W1–W20 definidos na UC022.

Riscos prioritários de revisão:

1. null convertido em zero;
2. soma parcial exibida como total;
3. perda adicionada duas vezes;
4. nova consulta de preços/configuração;
5. divisão ou arredondamento prematuro;
6. Rendimento ainda não salvo usado em POST inválido;
7. persistência acidental de valores derivados;
8. regressão na explicabilidade de UC018–UC021.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

Na PR de implementação, mover somente:

~~~text
UC022: Pronto -> Concluído
~~~

Preservar a ordem normativa do backlog.

Retornar:

- arquivos alterados;
- calculadora criada;
- integração Web;
- cobertura dos cenários incompletos;
- resultado de build/test;
- URL da PR.
