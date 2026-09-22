
# Codex — MEL024 — Listagem de Produtos com filtro e indicadores atuais

Implemente exclusivamente a MEL024 conforme:

~~~text
docs/development/improvements/MEL024-listagem-produtos-filtro-indicadores.md
~~~

## Gate

A MEL024 está Especificada, mas sua implementação permanece pausada enquanto a fila estiver bloqueada na MEL021, salvo alteração explícita no backlog.

Só iniciar quando backlog marcar MEL024 como Pronto.

Branch:

~~~text
feat/mel024-listagem-produtos-indicadores
~~~

## Objetivo

Evoluir /Produtos para:

- filtrar por Categoria;
- exibir Custo unitário atual;
- exibir Preço de prateleira vigente;
- exibir Margem atual.

Preservar Nome, Categoria, Margem-alvo, Situação e Consultar.

Não persistir indicadores.

## Filtro

Manter q somente para Nome.

Adicionar categoria:

~~~text
vazio => todas
sem-categoria => CategoriaProdutoId null
inteiro positivo => CategoriaProdutoId
outro => inválido / zero resultados
~~~

Combinar q AND categoria.

Dropdown contém Todas, Sem categoria, Categorias ativas e inativas. Marcar inativas com (inativa).

Cross-tenant não retorna nem revela Categoria.

## Grid

Ordem:

~~~text
Nome
Categoria
Custo unitário atual
Preço de prateleira vigente
Margem atual
Margem-alvo
Situação
Consultar
~~~

Usar table-responsive.

Custo:

~~~text
UC022 + UC036
null => indisponível
zero => R$ 0,00
~~~

Preço:

~~~text
Registro atual por DataReferencia DESC, Id DESC
null => —
~~~

Margem:

~~~text
CalculadoraMargemAtual
null => indisponível
~~~

Não usar snapshots históricos para custo/margem atual.

## Proibição de N+1

Não chamar PrecificacaoProdutoAtual.CalcularAsync dentro de loop.

Criar serviço em lote, nome sugerido:

~~~text
ResumoPrecificacaoProdutosAtual
~~~

Retorno mínimo:

~~~text
ProdutoId
CustoUnitarioProduto?
PrecoPrateleiraAtual?
MargemAtual?
SituacaoMargem
~~~

Carregar em lote:

- Produtos/Categoria;
- configuração da Empresa uma vez;
- Fichas;
- Itens;
- preços vigentes de Insumo via SelecionarVigentesAsync;
- UsosEquipamentosFicha;
- registros atuais de Produto.

Agrupar em memória.

## Calculadoras

Reutilizar obrigatoriamente:

- CalculadoraCustoItens;
- CalculadoraCustoPerdas;
- CalculadoraCustoMaoDeObra;
- CalculadoraCustoEnergia;
- CalculadoraCustoDesgasteEquipamentos;
- CalculadoraCustoProduto;
- CalculadoraMargemAtual.

Não duplicar fórmulas em PageModel ou serviço.

## Preço atual em lote

Adicionar método equivalente a SelecionarAtuaisAsync em RegistroPrecoProdutoConsultas.

Por Produto:

~~~text
DataReferencia DESC
Id DESC
First
~~~

Sem loop chamando SelecionarAtualAsync.

## Paridade

Testar o lote contra PrecificacaoProdutoAtual para:

~~~text
CustoUnitarioProduto
PrecoPrateleiraAtual
MargemAtual
SituacaoMargem
~~~

## Incompletude

Sem Ficha, item sem preço, Ficha vazia ou energia incompleta:

~~~text
custo = null
margem = null
~~~

Preço vigente pode continuar sendo exibido.

Sem configuração da Empresa:

~~~text
custo/margem indisponíveis
preço vigente preservado
HTTP 200
~~~

Produto sem Categoria => desgaste zero.

Categoria inativa vinculada continua calculando.

## Multiempresa

GQF sempre ativo.

Não usar IgnoreQueryFilters no runtime.

Nenhum EmpresaId vem do request.

## Leitura somente

GET não cria nem atualiza nada.

Nenhuma migration.

## Documentação na implementação

Atualizar:

- MEL024 -> Concluído;
- backlog;
- melhorias;
- UC008;
- F002;
- F004 quando aplicável.

UC028 continua Planejado.

Não implementar MEL021/Azure.

## Testes

Cobrir a matriz da especificação, principalmente:

- filtro ativo/inativo/sem categoria;
- q + Categoria;
- inválido/cross-tenant;
- custo completo/incompleto/zero;
- preço vigente/ausente/desempate;
- margem normal/negativa/incompleta;
- Produto inativo;
- paridade batch vs individual;
- preço de Produto em lote;
- GQF;
- GET sem persistência;
- ausência de N+1.

## Validação final

~~~text
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Confirmar:

- 0 erros;
- nenhum warning novo relevante;
- unitários verdes;
- integração SQL Server/Web verde;
- nenhuma migration;
- nenhum indicador persistido;
- nenhuma precificação individual em loop;
- UC028 continua Planejado;
- MEL021 não implementada.

Ao finalizar, informar o desenho da carga em lote, arquivos alterados, contagem de testes e confirmação de ausência de migration/N+1.
