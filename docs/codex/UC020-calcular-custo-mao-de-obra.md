# Instrução Codex — UC020: Calcular custo de mão de obra

## Tarefa

Implementar integralmente a UC020 conforme `docs/use-cases/UC020-calcular-custo-mao-de-obra.md`.

Branch obrigatória:

~~~text
feat/uc020-custo-mao-de-obra
~~~

Não editar/commitar/push direto em `master`. Não fazer merge da própria implementação.

## Precondições

Antes de alterar arquivos:

1. atualizar `master`;
2. criar/trocar para a branch obrigatória;
3. confirmar branch != master;
4. ler `AGENTS.md`;
5. ler `docs/development/backlog.md` e confirmar UC020 = `Pronto`;
6. ler UC020, F003, F004, UC013, UC027 e RN013/RN017/RN026/RN039;
7. inspecionar `FichaTecnicaModel`, `FichaTecnica.cshtml`, `ConfiguracaoPrecificacaoEmpresa`, `CalculadoraCustoItens` e testes atuais da Ficha/configurações.

Se UC020 não estiver `Pronto`, não implementar.

## Regra principal

Calcular:

~~~text
CustoMaoDeObraLote =
    (TempoAtivoMinutos / 60m)
    × ValorHoraTrabalho
~~~

Usar `decimal`. Não usar divisão inteira.

## Semântica null x zero

Implementar exatamente:

~~~text
TempoAtivoMinutos = 0
=> custo = 0
=> completo = true
mesmo se ValorHoraTrabalho = null
~~~

~~~text
TempoAtivoMinutos > 0
E ValorHoraTrabalho = null
=> custo = null
=> completo = false
~~~

~~~text
ValorHoraTrabalho = 0
=> custo = 0
=> completo = true
~~~

Não converter configuração ausente em zero.

## Core

Criar componente puro em `Precificador.Core.Precificacao`, preferencialmente:

~~~text
CalculadoraCustoMaoDeObra
~~~

Entrada:

~~~text
int TempoAtivoMinutos
decimal? ValorHoraTrabalho
~~~

Saída:

~~~text
decimal? CustoMaoDeObraLote
bool Completo
~~~

Requisitos:

- sem EF;
- sem Web;
- sem contexto de Empresa;
- sem formatação;
- sem persistência;
- rejeitar tempo negativo;
- rejeitar valor/hora negativo quando informado;
- sem arredondamento intermediário.

## Configuração

Usar `ConfiguracaoPrecificacaoEmpresa.ValorHoraTrabalho` da Empresa Ativa.

No código de produção:

- GQF normal;
- sem `IgnoreQueryFilters`;
- sem EmpresaId no request;
- sem parâmetro de ValorHora vindo do request;
- não criar configuração silenciosamente.

Se a entidade 1:1 estiver ausente, retornar 404 no fluxo da Ficha.

Não confundir:

~~~text
configuração ausente
~~~

com:

~~~text
configuração existente + ValorHoraTrabalho = null
~~~

## Integração na Ficha

Evoluir:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Não criar página nova.

Quando houver Ficha, acrescentar:

~~~text
Custo de mão de obra do lote: <valor>
~~~

ou, quando indisponível:

~~~text
Custo de mão de obra do lote: indisponível
Valor da hora de trabalho não configurado.
~~~

Produto sem Ficha não mostra esse componente.

## Relação com UC018

Preservar tudo que UC018 já entrega:

- custos dos Itens;
- total base dos Itens;
- mensagens de incompletude;
- data operacional;
- seleção de preço vigente;
- composição e ações da Ficha.

Mão de obra é independente.

Mesmo com `CustoBaseItens = null`, mostrar CustoMaoDeObraLote quando determinável.

Não somar mão de obra ao custo base dos Itens.

## GET

Calcular usando:

- TempoAtivoMinutos persistido da Ficha;
- ValorHoraTrabalho atual da configuração.

GET não chama SaveChanges.

## POST válido

Preservar o fluxo atual:

1. validar base da Ficha;
2. salvar TempoAtivoMinutos;
3. PRG;
4. GET recalcula com o novo valor persistido.

Não persistir o custo.

## POST inválido

Este ponto é obrigatório.

O Input postado deve continuar visível e com seus erros.

O custo exibido deve representar o estado persistido atual da Ficha, não o valor postado ainda não salvo.

Portanto:

- não sobrescrever `Input.TempoAtivoMinutos`;
- carregar separadamente TempoAtivoMinutos persistido para o cálculo;
- não simular novo custo com valor não persistido;
- não salvar nada.

## Configuração alterada

Alterar ValorHoraTrabalho na UC027 deve refletir no próximo GET da Ficha.

Não atualizar Produto, Ficha ou Item.

## Produto inativo

Calcular normalmente sem reativar.

## Precisão/apresentação

Aplicar RN026.

Não arredondar cálculo.

Na UI usar formatação pt-BR consistente com os componentes de custo existentes, preferencialmente até 4 casas decimais.

Pode reutilizar helper existente ou criar helper genérico de precificação pequeno.

Não fazer refatoração ampla de apresentação nesta UC.

## Não antecipar

Não implementar:

- perdas;
- equipamentos/energia;
- custo total do lote;
- custo unitário do Produto;
- preço sugerido;
- margem;
- categorias de mão de obra;
- funcionários/salários/encargos;
- snapshots comerciais.

## Testes obrigatórios

### Unitários U1–U8

Cobrir toda a matriz definida na UC020.

### Web W1–W15

Cobrir toda a matriz definida na UC020, com ênfase em:

- null x zero;
- tempo zero;
- divisão decimal;
- alteração de configuração refletida dinamicamente;
- Produto inativo;
- independência de UC018;
- isolamento tenant;
- entidade de configuração ausente;
- GET sem mutação;
- POST inválido usando estado persistido;
- PRG após POST válido.

Não criar migration.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Documentação pós-implementação

Na PR de implementação:

- alterar UC020 de `Pronto` para `Concluído` somente no backlog;
- não alterar MEL009;
- não marcar UC019/UC021 como concluídos;
- não criar migration.

## Retorno obrigatório

Informar:

1. branch;
2. arquivos alterados;
3. componente de cálculo criado;
4. tratamento de tempo zero/null/zero configurado;
5. integração na Ficha;
6. comportamento em POST inválido;
7. tenant/configuração ausente;
8. testes U/W;
9. build/test;
10. URL da PR.

Commit sugerido:

~~~text
feat: calcula custo de mao de obra
~~~
