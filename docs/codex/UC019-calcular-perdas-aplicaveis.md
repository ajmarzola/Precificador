# Instrução Codex — UC019: Calcular perdas aplicáveis

## Tarefa

Implementar `docs/use-cases/UC019-calcular-perdas-aplicaveis.md`.

Branch:

~~~text
feat/uc019-perdas-itens
~~~

Não trabalhar em `master` nem fazer merge da própria PR.

## Antes de editar

1. atualizar master e criar a branch;
2. confirmar UC019 = `Pronto`;
3. ler UC019, F003/F004 e RN010/RN012/RN017/RN026/RN050;
4. inspecionar ItemFichaTecnica, formulário Novo/Editar Item, UC018 e FichaTecnicaModel.

## Domínio

Adicionar a ItemFichaTecnica:

~~~text
PercentualPerda : decimal
~~~

Regra:

~~~text
0 <= PercentualPerda < 1
~~~

Default zero.

Edição passa a alterar Quantidade + Observacao + PercentualPerda, validando tudo antes de mutar.

Não mudar Id/Empresa/Ficha/Insumo.

## Migration

Adicionar:

~~~text
PercentualPerda decimal(9,6) NOT NULL DEFAULT 0
~~~

em ItensFichaTecnica.

Migration evolutiva; provar preservação de Itens existentes com zero.

## Cálculo

Criar calculadora pura:

~~~text
CustoPerdaItem = CustoItemBase × PercentualPerda
CustoPerdasLote = soma
~~~

Semântica:

~~~text
perda = 0 + CustoItemBase null
=> custo perda = 0

perda > 0 + CustoItemBase null
=> custo perda = null
=> total = null/incompleto
~~~

Coleção vazia => total 0/completo.

Não arredondar e não recalcular preço/custo unitário: reutilizar CustoItem do UC018.

## Quantidade e rendimento

Quantidade é a base antes da perda adicional.

Defeito que reduz unidades vendáveis pertence ao Rendimento, não ao PercentualPerda.

Não criar percentual global no Produto nem regra automática por Categoria.

## Web — Novo/Editar Item

Adicionar:

~~~text
Perda esperada (%)
~~~

UI percentual; domínio fração.

- vazio => 0;
- vírgula => pt-BR;
- senão => invariant;
- 0 <= percentual < 100;
- dividir por 100m.

GET Editar deve preservar:

~~~text
0,123456 <=> 12,3456
~~~

Centralizar parsing/formatação no helper existente ou equivalente.

POST inválido preserva raw input e não muta Item.

## Web — Ficha

Adicionar:

- Perda esperada (%);
- Custo da perda;
- resumo Custo de perdas do lote.

Regras:

- perda zero => custo 0;
- perda positiva + custo conhecido => valor;
- perda positiva + custo base indisponível => —;
- qualquer perda positiva sem custo => total indisponível;
- Ficha sem Itens => perdas 0/completo.

Preservar UC018, UC020 e UC021.

POST inválido da base usa estado persistido.

## Não implementar

- perda automática por Categoria;
- percentual global no Produto;
- estoque/perda real;
- custo total/unitário;
- preço sugerido;
- snapshot comercial.

## Testes

Atender D1–D5, U1–U8, P1–P4 e W1–W16.

Riscos prioritários:

1. perda aplicada duas vezes;
2. Quantidade interpretada como já contendo perda;
3. perda zero dependendo de preço;
4. total parcial mascarado;
5. round-trip percentual perdendo precisão;
6. migration alterando Itens existentes;
7. nova consulta/N+1 de preços;
8. POST inválido usando estado não persistido.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

Na PR, mover somente UC019 de Pronto para Concluído.

Retornar arquivos/migration, evolução de Item, calculadora, parsing percentual, integração Web, testes, build/test e URL da PR.
