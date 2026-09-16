# MEL015 — Padronizar apresentação monetária e entrada decimal pt-BR

- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Classificação:** bug de entrada + consistência de apresentação
- **Prioridade:** alta
- **Dependência:** UC005, UC011, UC018–UC025
- **Estado:** Planejado

## Problemas observados

1. Campos de valores monetários não apresentam padrão visual consistente de duas casas decimais.
2. O formulário de preço do Insumo não interpreta corretamente valor brasileiro com vírgula decimal.
3. Caso observado no teste manual: `20,99` foi persistido como `2099`, e a tela de Detalhes exibiu o valor incorreto sem separador decimal.
4. O valor incorreto persistido contaminou corretamente os cálculos dependentes: embalagem de 200 g registrada como `2099` e uso de 50 g resultou em `524,75` (`2099 / 200 * 50`). Portanto, o defeito está na entrada/persistência do preço, não na fórmula de custo UC018.
5. Há outros formulários com binding direto para `decimal`, como Preço de prateleira, que devem ser revalidados para o mesmo risco.

## Objetivo

Padronizar a experiência brasileira de entrada e apresentação de valores financeiros sem reduzir a precisão dos cálculos internos.

Este item é **bloqueante para testes manuais de custo/precificação** enquanto a entrada pt-BR puder persistir valores monetários incorretos.

## Regra de apresentação

Valores monetários que representam preço/total/valor configurado devem ser exibidos com **duas casas decimais** e cultura `pt-BR`.

Exemplos:

~~~text
5      -> 5,00
5,4    -> 5,40
12,345 -> 12,35 (somente na apresentação)
~~~

A regra é exclusivamente visual.

Não arredondar nem persistir novamente o valor apenas para atender a exibição.

## Exceção necessária — custo unitário técnico

Custos por unidade-base podem precisar de mais precisão, especialmente para unidades pequenas como `g`, `ml` ou `m`.

Exemplo realista:

~~~text
R$ 5,39 / 1000 g = R$ 0,00539 por g
~~~

Exibir esse custo como `R$ 0,01` ou `R$ 0,00` seria materialmente enganoso.

Portanto:

- preços/totais/valores monetários comerciais: duas casas;
- o resumo de preço vigente do Insumo deve exibir, por exemplo, `20,99` como `20,99` (preferencialmente com convenção monetária consistente), nunca `2099`;
- `Custo total do lote`, `Preço teórico` e `Preço sugerido` devem ser apresentados em formato monetário com duas casas;
- custo unitário técnico por unidade-base: manter precisão suficiente já suportada pelo domínio;
- cálculo interno: precisão integral conforme RN026.

A implementação deve distinguir essas duas categorias de apresentação.

## Entrada decimal brasileira

Campos monetários devem aceitar pelo menos:

~~~text
10
10,5
10,50
10.50
~~~

quando semanticamente válidos.

A entrada com vírgula deve funcionar no servidor independentemente da cultura padrão do processo.

## Escopo mínimo de revalidação

Revalidar ao menos:

- Registrar preço do Insumo — `PrecoCompra`;
- Registrar preço de prateleira — `PrecoPrateleira`;
- configurações monetárias;
- demais inputs financeiros ligados diretamente a `decimal`.

Formulários que já fazem parsing explícito pt-BR podem ser mantidos se consistentes.

## Direção técnica

Evitar depender do binding direto de `decimal` quando isso tornar a aceitação de `pt-BR` dependente da cultura do host.

É aceitável centralizar parser/formatação de valor monetário na camada Web para evitar implementações divergentes.

## Testes esperados

- cenário de regressão explícito: embalagem de 200 g a `20,99` e item de 50 g deve produzir custo-base de `5,2475` antes de formatação de apresentação;
- vírgula decimal aceita;
- ponto decimal continua aceito;
- valor inválido apresenta erro de validação amigável;
- duas casas na apresentação de preços/totais;
- custo unitário técnico não perde precisão informativa;
- nenhum arredondamento intermediário altera cálculo ou persistência.
