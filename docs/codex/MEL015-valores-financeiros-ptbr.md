# Instrução Codex — MEL015: Padronizar apresentação monetária e entrada decimal pt-BR

## Tarefa

Implementar integralmente:

~~~text
docs/development/improvements/MEL015-valores-financeiros-ptbr.md
~~~

Branch sugerida:

~~~text
fix/mel015-valores-financeiros-ptbr
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- MEL015;
- UC005 e UC006;
- UC011 e UC012;
- UC018–UC025;
- RN026 / pricing-model;
- `PrecoInsumoFormatacao`;
- `ConfiguracaoPrecificacaoFormatacao`;
- `ConfiguracaoPrecificacaoFormulario`;
- `Insumos/Precos/Novo`;
- `Produtos/Precos/Novo`;
- `Produtos/FichaTecnica`;
- `Produtos/Detalhes`;
- `Produtos/Precificacao`;
- históricos de Insumo e Produto;
- testes Web correspondentes.

## Diagnóstico que não pode ser perdido

O bug reproduzido foi:

~~~text
Input.PrecoCompra = "20,99"
=> persistido incorretamente como 2099
~~~

Com:

~~~text
QuantidadeCompra = 200
Quantidade Item = 50
~~~

UC018 calculou corretamente:

~~~text
2099 / 200 * 50 = 524,75
~~~

A fórmula **não deve ser alterada**.

Após a correção:

~~~text
PrecoCompra = 20.99m
CustoUnitario = 0.10495m
CustoItem = 5.2475m
~~~

A apresentação do CustoItem pode ser:

~~~text
R$ 5,25
~~~

sem alterar o `decimal` calculado.

## Entrada decimal

Corrigir explicitamente:

~~~text
/Insumos/Precos/Novo
- Input.QuantidadeCompra
- Input.PrecoCompra

/Produtos/Precos/Novo
- Input.PrecoPrateleira
~~~

Não depender do binding direto de `decimal`.

Preferir inputs textuais + parser Web centralizado.

Aceitar:

~~~text
10
10,5
10,50
10.5
10.50
200,5
200.5
~~~

Regras:

- uma vírgula => decimal;
- um ponto => decimal;
- ponto + vírgula => inválido;
- múltiplos separadores => inválido;
- não suportar separador de milhar nesta MEL;
- não aceitar silenciosamente `20,99` como `2099`;
- preservar texto postado em erro.

Não criar binder global sem necessidade.

## Validação

Mensagens mínimas:

~~~text
A quantidade deve ser um número válido.
O preço deve ser um número válido.
O preço de prateleira deve ser um número válido.
~~~

Depois do parse, manter regras atuais de > 0.

## Apresentação

Centralizar formatação Web.

### Montante monetário

Usar pt-BR, símbolo e duas casas:

~~~text
R$ 0,00
R$ 5,40
R$ 20,99
R$ 1.234,56
~~~

Aplicar aos montantes listados na MEL015.

### Custo unitário técnico do Insumo

Preservar precisão útil de até 6 casas, mínimo 2:

~~~text
R$ 0,10495
R$ 0,00539
R$ 5,00
~~~

Não arredondar valor técnico positivo pequeno para zero visual.

### Inputs de edição

Não formatar inputs persistidos para 2 casas se isso reduzir precisão.

Exemplo obrigatório:

~~~text
0.500001m
=> GET edição: 0,500001
~~~

## Pontos obrigatórios de UI

Revalidar/ajustar:

- Insumo Detalhes;
- Histórico do Insumo;
- Ficha Técnica;
- Produto Detalhes;
- Registrar Preço de prateleira;
- Histórico de precificação;
- Detalhamento da precificação;
- Consulta de Configurações.

MEL016 alterará os rótulos do preço do Insumo depois. **Não antecipar MEL016**.

## Não alterar

Não modificar:

- fórmulas Core;
- `PrecificacaoProdutoAtual`;
- RN026;
- precisões EF;
- migrations/model snapshot;
- registros existentes;
- regras de margem;
- regras de arredondamento comercial.

Não adicionar `Math.Round` em cálculo/persistência.

## Dados já corrompidos

Não tentar converter `2099` para `20.99`.

Não há informação suficiente para inferir intenção.

Sem data fix/data migration.

## Testes obrigatórios

Cobrir integralmente a matriz:

~~~text
I1–I10
E1–E4
A1–A16
C1–C4
~~~

da MEL015.

Prioridades de revisão:

1. `20,99` persiste `20.99m`;
2. `20.99` continua válido;
3. `200,5` persiste `200.5m`;
4. preço de prateleira aceita vírgula;
5. input ambíguo é rejeitado, não reinterpretado;
6. POST inválido preserva texto;
7. cenário `200 / 20,99 / 50 => 5.2475m`;
8. montantes aparecem com `R$ xx,xx`;
9. custo unitário de Insumo mantém precisão;
10. zero/null continuam distintos;
11. inputs de configuração não perdem precisão;
12. nenhum Core/schema foi alterado.

Atualizar asserts antigos de apresentação com expectativas específicas. Não simplesmente remover cobertura.

Evitar asserts globais frágeis como:

~~~text
Assert.DoesNotContain("99", html)
~~~

## Persistência

Nenhuma migration.

Não alterar:

~~~text
PrecificadorDbContextModelSnapshot
~~~

Não mudar `HasPrecision`.

## Backlog

Na PR de implementação:

~~~text
MEL015: Pronto -> Concluído
~~~

Não alterar estado de MEL016 ou qualquer outro item.

## Validação obrigatória

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

Esperado:

- build Release com 0 erros;
- sem warnings novos relevantes;
- suíte completa verde.

## Retorno esperado

Informar:

- arquivos alterados;
- estratégia de parsing adotada;
- helpers de apresentação criados/evoluídos;
- telas ajustadas;
- cobertura da matriz;
- confirmação do cenário 20,99;
- confirmação de ausência de migration/Core change;
- resultado build/test;
- URL da PR.
