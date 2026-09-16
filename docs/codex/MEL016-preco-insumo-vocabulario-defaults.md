# Instrução Codex — MEL016: Corrigir semântica e defaults do registro de preço do Insumo

## Tarefa

Implementar integralmente:

~~~text
docs/development/improvements/MEL016-preco-insumo-vocabulario-defaults.md
~~~

Branch sugerida:

~~~text
fix/mel016-semantica-preco-insumo
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- MEL016;
- MEL015;
- UC005;
- UC006;
- MEL006;
- F001;
- RN002, RN003, RN004, RN006;
- `PrecoInsumo`;
- `/Insumos/Precos/Novo`;
- `/Insumos/Detalhes`;
- `/Insumos/Precos/Historico`;
- testes atuais de preço/histórico;
- `CustomWebApplicationFactory`.

## Regra conceitual principal

Os nomes técnicos permanecem:

~~~text
QuantidadeCompra
PrecoCompra
~~~

Na UI/documentação funcional, a semântica passa a ser:

~~~text
Quantidade por embalagem
Preço por embalagem
~~~

Não renomear propriedades, colunas ou migrations.

Não criar unidade de compra alternativa.

### Exemplo

~~~text
Unidade base = g
embalagem de 1 kg

Quantidade por embalagem = 1000 g
Preço por embalagem = R$ 5,39

CustoUnitario = 5,39 / 1000
~~~

O sistema não registra quantidade de embalagens nem total de pedido.

## Formulário

Em:

~~~text
/Insumos/Precos/Novo/{id}
~~~

usar:

~~~text
Quantidade por embalagem (<unidade>)
Preço por embalagem
Data de referência
~~~

Atualizar também `DisplayAttribute`.

### Validação

Mensagens de parse:

~~~text
A quantidade por embalagem deve ser um número válido.
O preço por embalagem deve ser um número válido.
~~~

Mensagens de domínio para <= 0:

~~~text
A quantidade por embalagem deve ser maior que zero.
O preço por embalagem deve ser maior que zero.
~~~

Não alterar as invariáveis.

## Data de referência

Injetar `IDataOperacionalEmpresa` na PageModel de novo preço.

No GET válido:

~~~text
Input.DataReferencia = dataOperacionalEmpresa.Hoje
~~~

Não usar relógio estático.

Não aplicar default no POST.

Se o usuário limpar a data:

~~~text
Data de referência é obrigatória
~~~

e nenhum preço é persistido.

Datas passadas e futuras continuam permitidas.

## Detalhes

No bloco Preço vigente:

~~~text
Quantidade por embalagem
Preço por embalagem
~~~

Manter Referência e Custo unitário.

Não alterar a query vigente.

## Histórico

No resumo e na tabela:

~~~text
Quantidade por embalagem
Preço por embalagem
~~~

Não alterar:

- Data;
- Status;
- Custo unitário;
- RN006;
- ordenação;
- classificação de Futuro/Anterior/Vigente.

## MEL015

Preservar integralmente:

- `DecimalInputParser`;
- vírgula/ponto decimal;
- formatação monetária;
- custo unitário técnico;
- precisão interna.

O cenário:

~~~text
200 / 20,99
~~~

deve continuar persistindo:

~~~text
200m / 20.99m
~~~

## Domínio

É permitido alterar apenas as mensagens de validação de `PrecoInsumo` para a nova terminologia.

Não alterar:

- fórmula;
- propriedades;
- construtor/contrato funcional além das mensagens;
- tenant ownership.

## Documentação

Manter coerentes:

- MEL016;
- UC005;
- UC006;
- F001;
- business-rules RN002/RN003.

Não é necessário reescrever ADR-004 retroativamente.

## Multiempresa

Preservar:

- GQF;
- cross-tenant 404;
- nenhum EmpresaId do request;
- data operacional da Empresa Ativa;
- write guards.

## Persistência

Nenhuma migration.

Não alterar ModelSnapshot.

Não renomear colunas.

Não atualizar dados existentes.

## Testes obrigatórios

Cobrir U1–U5 e W1–W23 da especificação.

Pontos prioritários:

1. rótulos novos no formulário;
2. rótulos antigos ausentes;
3. data default vem de `IDataOperacionalEmpresa.Hoje`;
4. factory com `2030-01-02` produz input `2030-01-02`;
5. data passada/futura enviada pelo usuário é preservada;
6. POST sem data permanece inválido;
7. mensagens usam `embalagem`;
8. Detalhes usa novos rótulos;
9. Histórico usa novos rótulos;
10. parsing MEL015 não regride;
11. custo unitário não muda;
12. cross-tenant permanece 404;
13. nenhuma migration.

## Fora do escopo

Não implementar:

- MEL018;
- redirect de cadastro de Insumo/Produto;
- unidade de compra;
- conversões;
- estoque;
- fornecedor;
- pedido de compra;
- edição/exclusão de preço;
- UC028+.

## Backlog

Na PR de implementação:

~~~text
MEL016: Pronto -> Concluído
~~~

Não alterar estado da MEL018 ou demais itens.

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
- rótulos/mensagens corrigidos;
- como `IDataOperacionalEmpresa` foi usado;
- testes adicionados/atualizados;
- confirmação de ausência de migration;
- confirmação de preservação da MEL015;
- resultado build/test;
- URL da PR.
