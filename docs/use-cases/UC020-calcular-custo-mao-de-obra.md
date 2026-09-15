# UC020 — Calcular custo de mão de obra

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC013 e UC027
- **Alteração de schema:** não
- **Persistência de resultado:** não

## Objetivo

Calcular o custo atual de mão de obra do lote a partir do tempo ativo registrado na Ficha Técnica e do valor da hora de trabalho configurado para a Empresa Ativa.

A UC020 acrescenta um componente independente ao motor de custo. Não soma esse valor ao custo dos Itens, não calcula custo total do lote e não calcula custo unitário do Produto.

## Fórmula normativa

Aplicar RN013:

~~~text
CustoMaoDeObraLote =
    (TempoAtivoMinutos / 60m)
    × ValorHoraTrabalhoDaEmpresa
~~~

Usar divisão decimal. Não usar divisão inteira.

Exemplo:

~~~text
TempoAtivoMinutos = 30
ValorHoraTrabalho = 40,00

CustoMaoDeObraLote = 20,00
~~~

## Origem dos dados

### Tempo ativo

Usar exclusivamente:

~~~text
FichaTecnica.TempoAtivoMinutos
~~~

O campo:

- é obrigatório na Ficha;
- é inteiro;
- deve ser maior ou igual a zero;
- já é validado e persistido pelo UC013.

### Valor da hora

Usar exclusivamente:

~~~text
ConfiguracaoPrecificacaoEmpresa.ValorHoraTrabalho
~~~

da Empresa Ativa.

A configuração:

- é tenant-owned;
- é `decimal?`;
- quando informada, deve ser maior ou igual a zero;
- `null` significa **Não configurado**;
- zero é um valor configurado válido e não equivale a `null`.

Não aceitar EmpresaId ou ValorHoraTrabalho vindos do request.

## Regra para TempoAtivoMinutos = 0

Tempo ativo igual a zero representa explicitamente um processo sem trabalho humano ativo.

Portanto:

~~~text
TempoAtivoMinutos = 0
=> CustoMaoDeObraLote = 0
=> componente de mão de obra completo
~~~

Essa regra vale mesmo quando:

~~~text
ValorHoraTrabalho = null
~~~

Motivo: não existe tempo de trabalho a valorar.

Não exigir uma configuração irrelevante para um componente cuja quantidade consumida é zero.

## Regra para TempoAtivoMinutos > 0 e ValorHoraTrabalho = null

Quando existe trabalho ativo e o valor da hora não foi configurado:

~~~text
TempoAtivoMinutos > 0
E
ValorHoraTrabalho = null
=> CustoMaoDeObraLote = indisponível
=> componente de mão de obra incompleto
~~~

Nunca substituir `null` por zero.

A interface deve indicar explicitamente:

~~~text
Custo de mão de obra do lote: indisponível
Valor da hora de trabalho não configurado.
~~~

ou redação equivalente inequívoca.

## ValorHoraTrabalho = 0

Zero configurado é válido.

Quando:

~~~text
ValorHoraTrabalho = 0
~~~

o custo de mão de obra é zero para qualquer TempoAtivoMinutos válido:

~~~text
CustoMaoDeObraLote = 0
~~~

Isso é diferente de configuração ausente.

A interface não deve exibir mensagem de "não configurado" nesse caso.

## Precisão e arredondamento

Aplicar RN026.

Não arredondar durante o cálculo:

- `TempoAtivoMinutos / 60m`;
- multiplicação pelo ValorHoraTrabalho;
- resultado interno `CustoMaoDeObraLote`.

Formatação é responsabilidade da apresentação.

Exemplo:

~~~text
TempoAtivoMinutos = 1
ValorHoraTrabalho = 10

CustoMaoDeObraLote = 0,166666...
~~~

O decimal deve manter a precisão disponível; não arredondar antecipadamente para centavos.

## Cálculo puro

Criar componente puro e reutilizável em `Precificador.Core.Precificacao`.

Nome sugerido:

~~~text
CalculadoraCustoMaoDeObra
~~~

Entrada conceitual:

~~~text
TempoAtivoMinutos : int
ValorHoraTrabalho : decimal?
~~~

Saída conceitual:

~~~text
CustoMaoDeObraLote : decimal?
Completo : bool
~~~

O componente:

- não acessa EF;
- não acessa HTTP;
- não resolve Empresa Ativa;
- não lê configurações;
- não persiste nada;
- não formata strings.

## Validação defensiva da calculadora

Embora o domínio persistido já proteja os valores, o componente puro não deve calcular custos negativos.

Rejeitar:

~~~text
TempoAtivoMinutos < 0
ValorHoraTrabalho < 0
~~~

com exceção apropriada.

Não converter valor inválido em zero.

## Escopo de apresentação

Evoluir a rota canônica:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Não criar uma nova tela de precificação.

Quando existir Ficha, acrescentar um resumo de mão de obra próximo aos componentes de custo já apresentados.

Rótulo recomendado:

~~~text
Custo de mão de obra do lote
~~~

### Custo determinável

Exibir o valor calculado.

### Custo indisponível

Exibir:

~~~text
Custo de mão de obra do lote: indisponível
Valor da hora de trabalho não configurado.
~~~

### Produto sem Ficha

Não existe cálculo de mão de obra.

Não exibir custo de mão de obra e não criar Ficha durante GET.

## Relação com o formulário da Ficha

O `TempoAtivoMinutos` continua sendo editado pelo fluxo já existente do UC013.

UC020 não cria novo formulário e não muda a responsabilidade do POST da Ficha.

### GET

Calcular usando o TempoAtivoMinutos persistido na Ficha.

### POST válido

O fluxo continua:

1. validar;
2. persistir a atualização da Ficha;
3. PRG;
4. no GET após redirect, recalcular usando o novo TempoAtivoMinutos persistido.

Não calcular e persistir snapshot durante o POST.

### POST inválido

Preservar os valores digitados e os erros de validação do formulário.

O custo de mão de obra exibido no retorno do POST inválido deve continuar representando o **estado persistido atual da Ficha**, não uma simulação baseada em valor ainda não salvo.

Portanto:

- carregar o TempoAtivoMinutos persistido para o cálculo;
- não sobrescrever `Input.TempoAtivoMinutos` postado;
- não persistir alteração;
- não recalcular o custo como se o valor inválido/não salvo já estivesse em vigor.

Isso mantém a semântica de "custo atual".

## Configuração ausente como violação de integridade

`ConfiguracaoPrecificacaoEmpresa` é 1:1 obrigatória para Empresas válidas desde UC026.

Se a configuração da Empresa Ativa estiver inesperadamente ausente:

- não criar configuração durante GET/POST;
- não tratar a ausência da entidade como `ValorHoraTrabalho = null`;
- retornar `404` no fluxo da Ficha quando o cálculo de UC020 for carregado.

A ausência da entidade é diferente de uma configuração existente cujo `ValorHoraTrabalho` seja `null`.

## Multiempresa e segurança

Preservar FT002 e RN039.

- carregar configuração somente pelo GQF normal;
- não usar `IgnoreQueryFilters` no código de produção;
- não receber EmpresaId no request;
- configuração de outra Empresa não pode participar do cálculo;
- troca de Empresa Ativa altera naturalmente o ValorHoraTrabalho aplicável;
- o cálculo não realiza escrita.

## Produto inativo

Produto inativo continua calculável.

UC020 não reativa Produto.

## Independência em relação ao custo dos Itens

O componente de mão de obra é independente de UC018.

Se:

~~~text
CustoBaseItens = indisponível
~~~

mas os dados de mão de obra forem suficientes, o sistema pode e deve exibir:

~~~text
CustoMaoDeObraLote = valor conhecido
~~~

Não esconder componentes conhecidos porque outro componente do custo está incompleto.

A composição final e a decisão de completude do custo total pertencem ao UC022.

## Recalculo atual

O custo é derivado em tempo de consulta.

Alterações em:

- `FichaTecnica.TempoAtivoMinutos`;
- `ConfiguracaoPrecificacaoEmpresa.ValorHoraTrabalho`

devem refletir no próximo cálculo.

Não persistir snapshot de mão de obra na Ficha, Produto ou configuração.

Snapshots comerciais pertencem ao UC011.

## Relação com UC019

UC020 não depende de perdas.

Pode ser implementado e validado antes de UC019.

## Relação com UC021

UC020 e UC021 são componentes independentes.

UC020 não deve criar modelo de Equipamento, Potência ou Tempo de uso.

## Relação com UC022

UC022 futuramente usará `CustoMaoDeObraLote` como um dos componentes de `CustoLote`.

UC020 não deve chamar seu resultado de:

- Custo do lote;
- Custo total;
- Custo unitário do Produto.

## Critérios de aceitação

### CA01

Com TempoAtivoMinutos > 0 e ValorHoraTrabalho configurado, calcula pela RN013.

### CA02

A divisão por 60 usa decimal e não divisão inteira.

### CA03

TempoAtivoMinutos = 0 resulta em custo 0 e componente completo, inclusive se ValorHoraTrabalho estiver null.

### CA04

TempoAtivoMinutos > 0 com ValorHoraTrabalho null resulta em custo indisponível, nunca zero.

### CA05

ValorHoraTrabalho = 0 é configuração válida e resulta em custo 0, sem mensagem de configuração ausente.

### CA06

Não há arredondamento intermediário.

### CA07

Alterar ValorHoraTrabalho reflete no próximo GET sem alterar Ficha ou Produto.

### CA08

Alterar TempoAtivoMinutos e salvar reflete no GET após PRG.

### CA09

POST inválido preserva input postado, não persiste alteração e exibe custo baseado no estado persistido.

### CA10

Produto inativo continua calculável sem reativação.

### CA11

Custo de mão de obra pode ser exibido mesmo se CustoBaseItens estiver indisponível.

### CA12

Empresa A não usa ValorHoraTrabalho da Empresa B.

### CA13

Configuração 1:1 ausente retorna 404 e não é criada silenciosamente.

### CA14

Produto sem Ficha não calcula mão de obra nem cria Ficha.

### CA15

Nenhum custo de mão de obra é persistido.

## Matriz de testes

### Unitários — cálculo puro

- U1: 60 minutos × valor/hora calcula exatamente uma hora;
- U2: 30 minutos calcula meia hora;
- U3: 1 minuto preserva precisão sem arredondamento intermediário;
- U4: TempoAtivoMinutos = 0 + ValorHora null => custo 0 e completo;
- U5: TempoAtivoMinutos > 0 + ValorHora null => custo null e incompleto;
- U6: ValorHora = 0 => custo 0 e completo;
- U7: tempo negativo é rejeitado;
- U8: valor/hora negativo é rejeitado.

### Web

- W1: Ficha com tempo > 0 e valor/hora configurado mostra custo correto;
- W2: cálculo fracionário de hora usa decimal;
- W3: tempo zero + valor/hora null mostra custo zero, não indisponível;
- W4: tempo > 0 + valor/hora null mostra indisponível e mensagem explícita;
- W5: valor/hora zero mostra custo zero e não mostra "não configurado";
- W6: mudança de ValorHoraTrabalho reflete no próximo GET;
- W7: Produto inativo é calculado sem reativação;
- W8: custo dos Itens incompleto não impede exibição da mão de obra conhecida;
- W9: Empresa Ativa usa somente sua configuração;
- W10: configuração 1:1 ausente retorna 404 sem criação;
- W11: GET não persiste custo nem altera Ficha/Produto/configuração;
- W12: POST inválido preserva Input e calcula com TempoAtivo persistido;
- W13: POST válido altera TempoAtivo, faz PRG e o GET recalcula;
- W14: Produto sem Ficha não mostra cálculo de mão de obra e GET não cria Ficha;
- W15: apresentação pt-BR não altera a precisão interna.

## Alterações esperadas

### Core

- adicionar calculadora pura de custo de mão de obra;
- sem dependência de EF/Web.

### Infrastructure

- nenhuma consulta especializada nova obrigatória;
- usar GQF normal de `ConfiguracaoPrecificacaoEmpresa`.

### Web

- carregar a configuração tenant-aware junto do estado necessário da Ficha;
- integrar cálculo ao carregamento atual da rota;
- preservar UC013–UC018;
- apresentar custo/indisponibilidade sem criar nova tela.

### Persistência

- nenhuma migration;
- nenhum novo DbSet;
- nenhum campo de custo persistido.

## Fora do escopo

- editar configuração de ValorHoraTrabalho — UC027 já cobre;
- múltiplas categorias de mão de obra;
- funcionários;
- salários;
- encargos;
- custos fixos;
- custo por atividade;
- tempo por funcionário;
- perdas (UC019);
- energia/equipamentos (UC021);
- custo total/unitário (UC022);
- preço sugerido (UC023);
- margem;
- snapshots comerciais;
- persistência de custo.

## Gate

As dependências materiais estão concluídas:

- UC013 — concluído;
- UC027 — concluído;
- configuração `ValorHoraTrabalho` existe e é editável;
- `TempoAtivoMinutos` existe e é editável;
- a rota canônica da Ficha está consolidada.

Não há gate técnico pendente para implementação.

## Branch sugerida

~~~text
feat/uc020-custo-mao-de-obra
~~~

## Commit sugerido

~~~text
feat: calcula custo de mao de obra
~~~
