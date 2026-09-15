# Instrução Codex — UC020: Calcular custo de mão de obra

## Tarefa

Implementar `docs/use-cases/UC020-calcular-custo-mao-de-obra.md`.

Branch obrigatória:

~~~text
feat/uc020-custo-mao-de-obra
~~~

Não trabalhar diretamente em `master` e não fazer merge da própria PR.

## Antes de editar

1. atualizar `master`;
2. criar/trocar para a branch acima;
3. confirmar UC020 = `Pronto` no backlog;
4. ler UC020, RN013/RN017/RN026/RN039, UC013 e UC027;
5. inspecionar Ficha atual, `ConfiguracaoPrecificacaoEmpresa`, calculadora UC018 e testes relacionados.

## Implementação

### Core

Criar calculadora pura em `Precificador.Core.Precificacao`.

Regra:

~~~text
(TempoAtivoMinutos / 60m) × ValorHoraTrabalho
~~~

Semântica obrigatória:

~~~text
tempo = 0 + valor/hora null => custo 0, completo
tempo > 0 + valor/hora null => custo null, incompleto
valor/hora = 0 => custo 0, completo
~~~

Rejeitar tempo ou valor/hora negativos.

Não arredondar.

### Web

Integrar à rota existente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Usar:

- TempoAtivoMinutos persistido;
- ValorHoraTrabalho da configuração da Empresa Ativa via GQF.

Não receber EmpresaId ou ValorHora pelo request.

Configuração 1:1 ausente => 404, sem lazy-create.

Mostrar:

~~~text
Custo de mão de obra do lote: <valor>
~~~

ou:

~~~text
Custo de mão de obra do lote: indisponível
Valor da hora de trabalho não configurado.
~~~

Produto sem Ficha não mostra componente.

Produto inativo continua calculável.

### Preservar UC018

Não alterar semântica de:

- custos dos Itens;
- CustoBaseItens;
- seleção de preço vigente;
- mensagens de incompletude.

Mão de obra é independente: se UC018 estiver incompleta, ainda mostrar mão de obra quando conhecida.

### POST da Ficha

POST válido:

~~~text
salvar -> PRG -> GET recalcula
~~~

POST inválido:

- preservar Input/erros;
- calcular mão de obra com o TempoAtivoMinutos persistido;
- não usar valor postado não salvo para representar "custo atual";
- não persistir custo.

## Persistência

Não criar migration, DbSet, coluna ou snapshot.

## Não antecipar

Não implementar UC019, UC021, UC022 ou UC023.

Não modelar funcionários, salários, encargos ou categorias de mão de obra.

## Testes

Implementar U1–U8 e W1–W15 da UC020.

Riscos que devem estar explicitamente cobertos:

1. divisão inteira;
2. `null` confundido com zero;
3. tempo zero exigindo configuração desnecessariamente;
4. vazamento cross-tenant;
5. POST inválido usando valor não persistido;
6. dependência indevida de UC018 completa.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

Na PR, mover apenas UC020 de `Pronto` para `Concluído`.

## Retorno

Informar branch, arquivos alterados, calculadora criada, tratamento null/zero, integração Web, testes, resultado do build/test e URL da PR.
