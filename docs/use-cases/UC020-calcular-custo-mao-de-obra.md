# UC020 — Calcular custo de mão de obra

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC013 e UC027
- **Alteração de schema:** não
- **Persistência de resultado:** não

## Objetivo

Calcular o custo atual de mão de obra do lote usando:

- `FichaTecnica.TempoAtivoMinutos`;
- `ConfiguracaoPrecificacaoEmpresa.ValorHoraTrabalho` da Empresa Ativa.

UC020 acrescenta um componente independente ao motor de custo. Não soma componentes nem calcula custo total/unitário do Produto.

## Regra normativa

Aplicar RN013:

~~~text
CustoMaoDeObraLote =
    (TempoAtivoMinutos / 60m)
    × ValorHoraTrabalho
~~~

A divisão deve ser decimal.

Exemplo:

~~~text
30 min / 60m × 40,00 = 20,00
~~~

### Null x zero

Fechar as seguintes semânticas:

~~~text
TempoAtivoMinutos = 0
=> CustoMaoDeObraLote = 0
=> Completo = true
~~~

Isso vale mesmo se `ValorHoraTrabalho = null`, porque não existe tempo de trabalho a valorar.

~~~text
TempoAtivoMinutos > 0
E ValorHoraTrabalho = null
=> CustoMaoDeObraLote = indisponível
=> Completo = false
~~~

Nunca substituir configuração ausente por zero.

~~~text
ValorHoraTrabalho = 0
=> CustoMaoDeObraLote = 0
=> Completo = true
~~~

Zero configurado é diferente de `null`.

## Precisão

Aplicar RN026.

Não arredondar durante:

- divisão por 60;
- multiplicação;
- armazenamento do resultado em memória.

Formatação ocorre somente na apresentação.

Exemplo de proteção:

~~~text
TempoAtivoMinutos = 1
ValorHoraTrabalho = 10
~~~

não pode virar zero por divisão inteira nem ser arredondado antecipadamente para centavos.

## Modelo de cálculo

Criar componente puro em `Precificador.Core.Precificacao`, preferencialmente:

~~~text
CalculadoraCustoMaoDeObra
~~~

Entrada:

~~~text
TempoAtivoMinutos : int
ValorHoraTrabalho : decimal?
~~~

Resultado:

~~~text
CustoMaoDeObraLote : decimal?
Completo : bool
~~~

O componente não acessa EF, HTTP, tenant, configuração ou persistência.

Como defesa de domínio, rejeitar:

- TempoAtivoMinutos < 0;
- ValorHoraTrabalho < 0 quando informado.

## Origem e isolamento dos dados

`TempoAtivoMinutos` vem da Ficha persistida.

`ValorHoraTrabalho` vem da `ConfiguracaoPrecificacaoEmpresa` da Empresa Ativa.

Preservar FT002/RN039:

- GQF normal;
- sem `IgnoreQueryFilters` em produção;
- sem EmpresaId no request;
- sem ValorHoraTrabalho no request;
- Empresa A nunca usa configuração da Empresa B.

`ConfiguracaoPrecificacaoEmpresa` é uma relação 1:1 obrigatória desde UC026.

Se a entidade estiver ausente:

- retornar 404;
- não criar configuração silenciosamente;
- não tratar ausência da entidade como `ValorHoraTrabalho = null`.

## Apresentação

Evoluir a rota existente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Não criar nova tela.

Quando houver Ficha, apresentar:

~~~text
Custo de mão de obra do lote: <valor>
~~~

Quando `TempoAtivoMinutos > 0` e ValorHora estiver null:

~~~text
Custo de mão de obra do lote: indisponível
Valor da hora de trabalho não configurado.
~~~

ou redação equivalente inequívoca.

Usar formatação pt-BR consistente com UC018, preferencialmente até 4 casas decimais, sem alterar o valor interno.

Produto sem Ficha não mostra esse componente e GET não cria Ficha.

Produto inativo continua calculável sem reativação.

## Integração com o formulário da Ficha

UC020 não cria novo formulário. `TempoAtivoMinutos` continua sendo editado pelo UC013.

### GET

Calcular com o TempoAtivoMinutos persistido e a configuração atual da Empresa.

### POST válido

Preservar o fluxo atual:

1. validar;
2. salvar Ficha;
3. PRG;
4. recalcular no GET usando o novo estado persistido.

Não persistir o custo.

### POST inválido

Preservar Input e erros do usuário.

O custo exibido deve continuar representando o **estado persistido atual**, não uma simulação do valor ainda não salvo.

Assim:

- não sobrescrever o Input postado;
- calcular com TempoAtivoMinutos persistido;
- não persistir alteração.

## Independência dos demais componentes

Mão de obra é independente do custo dos Itens.

Se `CustoBaseItens` estiver indisponível, mas a mão de obra for determinável, exibir seu valor normalmente.

UC020 não implementa:

- perdas;
- energia/equipamentos;
- CustoLote;
- custo unitário do Produto.

A composição dos componentes pertence ao UC022.

## Recalculo atual

O resultado é derivado em tempo de consulta.

Alterações em:

- TempoAtivoMinutos;
- ValorHoraTrabalho

devem aparecer no próximo GET.

Não persistir snapshot de mão de obra. Snapshot comercial pertence ao UC011.

## Critérios de aceitação

- **CA01:** tempo > 0 + valor/hora configurado aplica RN013.
- **CA02:** divisão por 60 é decimal.
- **CA03:** tempo = 0 produz custo 0 e componente completo, mesmo com valor/hora null.
- **CA04:** tempo > 0 + valor/hora null produz indisponível, nunca zero.
- **CA05:** valor/hora = 0 é válido e produz custo 0.
- **CA06:** não há arredondamento intermediário.
- **CA07:** mudança de ValorHora reflete no próximo GET.
- **CA08:** mudança salva de TempoAtivo reflete após PRG.
- **CA09:** POST inválido preserva Input, não persiste e usa estado persistido no cálculo.
- **CA10:** Produto inativo continua calculável.
- **CA11:** incompletude de UC018 não esconde mão de obra conhecida.
- **CA12:** isolamento tenant é preservado.
- **CA13:** configuração 1:1 ausente retorna 404 sem criação.
- **CA14:** Produto sem Ficha não calcula nem cria Ficha.
- **CA15:** custo não é persistido.

## Matriz de testes

### Unitários

- U1: 60 min = 1 hora;
- U2: 30 min = meia hora;
- U3: 1 min preserva precisão;
- U4: tempo 0 + valor/hora null => 0/completo;
- U5: tempo > 0 + valor/hora null => null/incompleto;
- U6: valor/hora 0 => 0/completo;
- U7: tempo negativo rejeitado;
- U8: valor/hora negativo rejeitado.

### Web

- W1: cálculo normal;
- W2: fração de hora;
- W3: tempo 0 + valor/hora null;
- W4: tempo > 0 + valor/hora null;
- W5: valor/hora 0;
- W6: alteração de configuração reflete no GET;
- W7: Produto inativo;
- W8: UC018 incompleta não bloqueia mão de obra;
- W9: isolamento entre Empresas;
- W10: configuração ausente => 404 sem criação;
- W11: GET sem mutação/persistência de custo;
- W12: POST inválido preserva Input e usa TempoAtivo persistido;
- W13: POST válido + PRG recalcula;
- W14: Produto sem Ficha não mostra componente;
- W15: formatação pt-BR não altera precisão.

## Alterações esperadas

### Core

- calculadora pura de mão de obra.

### Web

- carregar ValorHora tenant-aware;
- integrar resultado à Ficha;
- preservar UC013–UC018.

### Persistência

- nenhuma migration;
- nenhum novo DbSet;
- nenhum campo de custo.

## Fora do escopo

- múltiplos tipos de mão de obra;
- funcionários, salários ou encargos;
- custo por atividade;
- perdas — UC019;
- energia/equipamentos — UC021;
- custo total/unitário — UC022;
- preço sugerido — UC023;
- snapshots comerciais.

## Gate

UC013 e UC027 estão concluídos. TempoAtivoMinutos e ValorHoraTrabalho já existem, são editáveis e possuem regras fechadas.

Não há gate técnico pendente.

## Branch sugerida

~~~text
feat/uc020-custo-mao-de-obra
~~~

## Commit sugerido

~~~text
feat: calcula custo de mao de obra
~~~
