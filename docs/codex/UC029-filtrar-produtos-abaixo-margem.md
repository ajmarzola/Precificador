# Instrução Codex — UC029: Filtrar Produtos abaixo da margem

## Tarefa

Implementar integralmente:

~~~text
docs/use-cases/UC029-filtrar-produtos-abaixo-margem.md
~~~

Branch sugerida:

~~~text
feat/uc029-filtro-abaixo-margem
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- UC029;
- UC028;
- F005;
- UC024;
- RN022, RN023, RN027, RN036 e RN037;
- `Dashboard/Index.cshtml.cs`;
- `Dashboard/Index.cshtml`;
- `Produtos/Index.cshtml.cs`;
- `Produtos/Index.cshtml`;
- MEL024;
- `ResumoPrecificacaoProdutosAtual`;
- testes atuais de Dashboard e Produtos.

Confirmar no backlog:

~~~text
UC029 = Pronto
~~~

## Escopo

Evoluir as páginas existentes:

~~~text
/Dashboard
/Produtos
~~~

Não criar nova página.

No Dashboard, adicionar:

~~~text
Todos os ativos
Abaixo da margem
~~~

Na lista de Produtos, adicionar o mesmo recorte **Abaixo da margem**, preservando pesquisa por Nome, filtro por Categoria, colunas e Produtos inativos na visão sem recorte.

Query canônica nas duas superfícies:

~~~text
filtro=abaixo-da-margem
~~~

## Parsing do filtro

Semântica:

~~~text
null/vazio
=> todos

abaixo-da-margem
=> abaixo

qualquer outro valor
=> inválido
~~~

Trim no valor recebido e comparação case-insensitive.

Links gerados sempre usam:

~~~text
abaixo-da-margem
~~~

No Dashboard, valor inválido não pode ampliar para todos: HTTP 200, lista vazia, mensagem explícita e link Mostrar todos.

Em `/Produtos`, valor inválido também retorna HTTP 200/lista vazia, com mensagem explícita e ação Limpar filtros.

Em `/Produtos`, `q`, `categoria` e `filtro` combinam por AND; alterar somente o recorte de margem deve preservar `q` e `categoria`.

Não criar o filtro da UC030.

## Ordem de processamento

### Dashboard

Preservar uma única fotografia corrente:

~~~text
1. consultar todos os Produtos ativos
2. ResumoPrecificacaoProdutosAtual.CalcularAsync(ids) uma vez
3. materializar todos os ProdutoDashboard
4. calcular cards globais sobre todos
5. aplicar filtro à lista em memória
~~~

Não filtrar os IDs por margem antes do resumo, pois os cards são globais.

### Produtos

Preservar `q` e `categoria` como filtros cadastrais. Para `filtro=abaixo-da-margem`:

~~~text
1. carregar candidatos conforme q/categoria
2. restringir a ativos
3. ResumoPrecificacaoProdutosAtual.CalcularAsync(ids) uma vez
4. aplicar SituacaoMargem.AbaixoDaMargem em memória
5. renderizar a lista
~~~

Sem `filtro`, preservar o comportamento da MEL024, inclusive Produtos inativos.

Não executar segundo `CalcularAsync`.

Não chamar `PrecificacaoProdutoAtual` em loop.

## Cards

Cards são sempre globais:

~~~text
Produtos ativos
Insumos ativos
Abaixo da margem
Dentro da margem
Margem indisponível
~~~

O filtro altera somente a tabela.

Cuide para não derivar `ProdutosAtivos` de `Produtos.Count` depois que `Produtos` já tiver sido filtrado.

## Regra do recorte

A lista abaixo da margem deve usar exatamente:

~~~csharp
produto.SituacaoMargem == SituacaoMargemProduto.AbaixoDaMargem
~~~

Não recalcular margem no PageModel/Razor.

Não usar comparação direta de valores para substituir `SituacaoMargem`.

Assim:

- DentroDaMargem => fora;
- Incompleto => fora;
- igualdade com meta => fora;
- negativa abaixo da meta => dentro.

## UI

### Dashboard

Adicionar controles explícitos acima da tabela:

~~~text
Todos os ativos
Abaixo da margem
~~~

Rotas:

~~~text
Todos => /Dashboard
Abaixo => /Dashboard?filtro=abaixo-da-margem
~~~

Card Abaixo da margem pode ser clicável como conveniência, mas não é requisito.

Preservar tabela/colunas da UC028.

### Produtos

Integrar o recorte **Abaixo da margem** aos controles existentes de `q` e `categoria`.

- preservar `q/categoria` ao alternar somente o recorte;
- incluir `filtro` no estado de filtros;
- Limpar filtros => `/Produtos`;
- preservar todas as colunas da MEL024;
- não adicionar coluna SituacaoMargem;
- Situação continua Ativo/Inativo;
- sem recorte, inativos continuam visíveis.

## Estados vazios

### zero Produtos ativos

Preservar mensagem atual.

### filtro abaixo sem resultados

Usar mensagem específica:

~~~text
Nenhum Produto ativo está abaixo da margem.
~~~

ou equivalente sem ambiguidade.

### filtro inválido

Mensagem de filtro inválido + ação Mostrar todos.

## Multiempresa

- GQFs normais;
- sem `IgnoreQueryFilters`;
- sem EmpresaId do request;
- filtro em memória sobre coleção já tenant-aware;
- nenhum dado externo entra em cards ou tabela.

## Persistência

GET somente leitura.

Nenhuma migration.

Nenhum ModelSnapshot.

## Testes

Cobrir no mínimo no Dashboard:

1. sem filtro => todos ativos;
2. abaixo-da-margem => somente AbaixoDaMargem;
3. dentro fora;
4. igualdade fora;
5. Incompleto fora;
6. margem negativa abaixo entra;
7. inativo fora;
8. filtro vazio => todos;
9. caixa/whitespace => reconhecido;
10. inválido => lista vazia/mensagem/Mostrar todos;
11. cards globais permanecem iguais sob filtro;
12. estado vazio sem Produtos;
13. estado vazio sem abaixo;
14. isolamento entre Empresas;
15. uma fotografia em lote.

Cobrir no mínimo em `/Produtos`:

16. sem filtro preserva ativos/inativos e comportamento MEL024;
17. abaixo-da-margem => somente ativos AbaixoDaMargem;
18. inativo abaixo => fora;
19. q + categoria + filtro => AND;
20. alternar recorte preserva q/categoria;
21. filtro vazio => visão atual;
22. inválido => lista vazia/mensagem/Limpar filtros;
23. estado vazio filtrado distinto do catálogo vazio;
24. isolamento entre Empresas;
25. resumo continua em lote, sem N+1;
26. ausência de escrita/migration.

Preservar todos os testes da UC028 e da MEL024.

## Documentação

Na implementação atualizar:

- UC029: Pronto -> Concluído;
- backlog: UC029 -> Concluído;
- F005;
- F002;
- UC008;
- catálogo funcional.

Não alterar UC030 para Pronto/Concluído.

## CI

A PR de implementação altera código, portanto deve executar:

~~~text
CI mode: full
~~~

Validar:

- Restore;
- Build Release;
- unitários;
- integração.

Não reduzir cobertura para fazer a CI passar.

## Retorno esperado

Informar:

- arquivos alterados;
- parsing/semântica do filtro nas duas superfícies;
- como q/categoria/filtro são combinados em /Produtos;
- como os cards permaneceram globais;
- confirmação das chamadas em lote sem N+1;
- cenários de testes adicionados;
- confirmação de nenhuma migration;
- resultado da CI;
- URL da PR.
