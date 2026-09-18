# MEL022 — Substituir custo de mão de obra por percentual sobre os insumos

- **Origem:** segunda rodada de testes manuais.
- **Classificação:** regra de precificação / configuração por Empresa / simplificação da Ficha Técnica.
- **Estado:** Planejado.
- **Prioridade:** alta.
- **Gate sugerido:** após MEL020 e antes de MEL023/MEL021.
- **Alteração de regra de negócio:** sim.
- **Alteração de schema:** sim.
- **Migration:** sim.

## Problema observado

O modelo atual calcula mão de obra por:

```text
TempoAtivoMinutos × ValorHoraTrabalho
```

Esse modelo exige que o usuário estime um valor/hora e mantenha tempo ativo na Ficha, o que se mostrou pouco natural para parte dos produtos artesanais.

A solicitação validada na revisão é usar um percentual sobre o custo dos insumos.

Exemplo:

```text
Custo base dos insumos = R$ 12,00
Percentual de mão de obra = 10%
Custo de mão de obra = R$ 1,20
```

## Direção aprovada para a especificação

Substituir a regra atual por:

```text
CustoMaoDeObraLote =
    CustoBaseItens
    × PercentualMaoDeObra
```

O percentual é aplicado ao **CustoBaseItens** de UC018.

Não compõem a base:

- perdas;
- energia;
- a própria mão de obra;
- margem;
- preço comercial.

Isso evita recursão e preserva cada componente de custo como parcela independente.

## Configuração da Empresa

Substituir:

```text
ValorHoraTrabalho
```

por:

```text
PercentualMaoDeObra
```

Direção inicial:

- armazenar como fração decimal;
- default de negócio = `0,10` (10%);
- novas Empresas nascem com 10%;
- configurações existentes recebem 10% na migration;
- zero é válido;
- não transformar ausência de custo dos insumos em zero.

Como o percentual passa a ter default normativo, o estado:

```text
Valor da hora de trabalho não configurado.
```

deixa de existir.

Por isso, o item de UX que propunha link a partir dessa mensagem é absorvido pela MEL022 e **não deve ser implementado separadamente**.

## Tempo ativo da Ficha

`TempoAtivoMinutos` atualmente só possui efeito funcional no cálculo de mão de obra.

Depois da mudança para percentual, ele deixa de ter consumidor no MVP.

Direção recomendada:

- remover `TempoAtivoMinutos` da UI da Ficha;
- remover sua obrigatoriedade;
- remover a propriedade do modelo persistido;
- criar migration aditiva que elimina a coluna;
- ajustar criação/edição da Ficha para trabalhar apenas com Rendimento.

Não manter um campo obrigatório sem efeito apenas por compatibilidade histórica.

Se no futuro houver necessidade de tempo de produção para capacidade, agenda ou produtividade, isso deve voltar como requisito explícito de outra funcionalidade.

## Completude

A mão de obra passa a depender do custo dos itens.

Assim:

```text
CustoBaseItens conhecido
+ PercentualMaoDeObra conhecido
=> CustoMaoDeObraLote conhecido
```

```text
CustoBaseItens indisponível
=> CustoMaoDeObraLote indisponível
```

Não substituir item sem preço por custo zero.

## Impactos esperados

Revisar, no mínimo:

- RN013;
- UC013;
- UC017;
- UC018;
- UC020;
- UC022;
- UC025;
- UC026;
- UC027;
- F003;
- F004;
- `ConfiguracaoPrecificacaoEmpresa`;
- `FichaTecnica`;
- `CalculadoraCustoMaoDeObra`;
- `PrecificacaoProdutoAtual`;
- telas de Configurações;
- Ficha Técnica;
- Detalhamento de Precificação;
- migrations;
- testes unitários, persistência e Web.

## Pontos para fechar na especificação

A especificação detalhada deve fechar:

- faixa permitida do percentual;
- labels e ajuda da configuração;
- migração de `ValorHoraTrabalho` / `TempoAtivoMinutos`;
- impactos exatos na projeção de detalhamento;
- regressões de snapshots comerciais já persistidos.

## Fora do escopo

- salário/funcionário;
- encargos;
- custo por atividade;
- percentuais diferentes por Produto;
- múltiplas categorias de mão de obra;
- histórico do percentual.
