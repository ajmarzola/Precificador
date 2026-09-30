# F005 — Dashboard de Margens

## Objetivo

Dar visão rápida dos produtos que precisam de atenção após mudanças em preços de insumos, ficha técnica ou configurações, sempre no contexto da **Empresa Ativa**.

## Entrega por fatias

### UC028 — resumo inicial

UC028 cria `/Dashboard` e considera somente Produtos ativos da Empresa Ativa.

O Dashboard passa a ser a **Home da Empresa** após autenticação e resolução da Empresa Ativa. Toda ação autenticada de Início/Home deve apontar diretamente para `/Dashboard`, incluindo login sem `ReturnUrl`, seleção de Empresa, link Início e marca Precificador. A rota `/` permanece como **Home pública** para anônimos e funciona apenas como fallback de redirect para `/Dashboard` quando acessada já autenticado. Logout encerra o contexto da Empresa e retorna para `/` deslogado.

Indicadores:

- quantidade de Produtos ativos;
- quantidade de Insumos ativos;
- Produtos abaixo da Margem-alvo;
- Produtos dentro da Margem-alvo;
- Produtos com **Margem indisponível** (`SituacaoMargem.Incompleto`).

A lista inicial mostra todos os Produtos ativos e, quando disponíveis:

- Produto;
- Preço de prateleira atual;
- Custo unitário atual;
- Margem atual;
- Margem-alvo;
- Preço sugerido atual;
- Situação da margem.

A UC028 não adiciona filtros.

`SituacaoMargem.Incompleto` não deve ser chamada de **precificação incompleta**: conforme RN017/RN027, ela representa somente indisponibilidade da Margem atual por falta de custo atual ou preço de prateleira atual.

### UC029 — abaixo da margem

UC029 adiciona o recorte operacional de Produtos abaixo da Margem-alvo sobre o Dashboard criado pela UC028 **e também sobre a listagem `/Produtos`**.

Filtros disponíveis após esta UC:

~~~text
Todos os ativos
Abaixo da margem
~~~

A query canônica usa o mesmo parâmetro nas duas superfícies:

~~~text
/Dashboard?filtro=abaixo-da-margem
/Produtos?filtro=abaixo-da-margem
~~~

O recorte usa exclusivamente `SituacaoMargem.AbaixoDaMargem`. Produtos `DentroDaMargem`, `Incompleto` e inativos não entram.

No Dashboard, os cards permanecem globais para todos os Produtos ativos da Empresa Ativa; o filtro altera somente a tabela. O resumo em lote é calculado uma única vez para o universo completo e o recorte é aplicado depois, em memória.

Em `/Produtos`, o mesmo recorte se combina por AND com `q` e `categoria`. A visão sem recorte continua exibindo ativos e inativos; o recorte abaixo da margem mostra somente Produtos ativos classificados como `AbaixoDaMargem`.

### UC030 — precificação incompleta

UC030 identifica a **completude global da precificação**, conceito distinto da situação de margem, e adiciona o recorte correspondente ao Dashboard **e à listagem `/Produtos`**.

A precificação global é completa somente quando a fotografia atual consegue determinar:

- Custo unitário do Produto;
- Preço sugerido;
- Preço de prateleira atual;
- Margem atual.

A classificação não pode ser reduzida a `SituacaoMargem.Incompleto`. Um Produto pode possuir Margem atual calculável e ainda estar globalmente incompleto — por exemplo, quando `IncrementoComercial = null` impede apenas o Preço sugerido.

A UC030 também passa a expor motivos estruturados de incompletude, incluindo:

- Ficha técnica ausente;
- Ficha sem Itens;
- Item(ns) sem preço vigente;
- configuração de precificação ausente;
- tarifa de energia ausente quando existe uso elétrico;
- Incremento comercial ausente;
- Preço de prateleira não definido.

Novo valor canônico:

~~~text
filtro=precificacao-incompleta
~~~

Superfícies:

~~~text
/Dashboard?filtro=precificacao-incompleta
/Produtos?filtro=precificacao-incompleta
~~~

Os recortes `abaixo-da-margem` e `precificacao-incompleta` não são categorias mutuamente exclusivas. Um Produto pode pertencer aos dois conjuntos; o parâmetro `filtro` continua single-value e apresenta um recorte por vez.

As duas tabelas passam a tornar explícito o estado de **Precificação** e, quando incompleta, seus motivos. Em `/Produtos`, a coluna **Situação** continua significando exclusivamente Ativo/Inativo.

Os cards existentes do Dashboard mantêm a semântica da UC028/UC029. **Margem indisponível** não é renomeada para **Precificação incompleta**, e nenhum sexto card é obrigatório nesta UC.

O cálculo continua derivado e em lote por `ResumoPrecificacaoProdutosAtual`, sem N+1 e sem persistência do estado/motivos.

## Filtros do F005 ao final das três UCs

- todos os ativos;
- abaixo da margem;
- precificação incompleta.

## Regras relacionadas

RN017, RN022, RN023 e RN027.

## Princípios

- valores correntes são derivados em consulta;
- snapshots históricos não substituem estado atual;
- nenhum indicador do Dashboard é persistido;
- consultas permanecem tenant-aware;
- processamento multi-Produto deve ser em lote, sem N+1.

## Fora do escopo do MVP

- gráficos analíticos avançados;
- BI externo;
- alertas por e-mail/push;
- monitoramento em background.
