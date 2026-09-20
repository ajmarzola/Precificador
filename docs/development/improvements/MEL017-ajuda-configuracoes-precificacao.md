# MEL017 — Explicar configurações de precificação na interface

> **Nota MEL022:** as referências históricas a Valor da hora/`ValorHoraTrabalho` foram substituídas no modelo ativo por `PercentualMaoDeObra`, exibido como "Mão de obra sobre os insumos (%)" e explicado como percentual aplicado sobre `CustoBaseItens`.

- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Classificação:** UX / explicabilidade
- **Prioridade:** baixa
- **Estado:** Concluído
- **Ordem na fila pendente:** 05
- **Dependências:** UC026, UC027
- **Alteração de schema:** não
- **Migration:** não
- **Alteração de domínio:** não
- **Alteração de cálculos:** não

## Objetivo

Explicar, no próprio ponto de uso, o significado e o efeito das cinco configurações de precificação da Empresa Ativa.

A MEL017 deve permitir que o usuário compreenda:

- o que cada valor representa;
- qual parte da precificação utiliza esse valor;
- o que acontece quando o parâmetro está `Não configurado`, quando aplicável;
- quais parâmetros afetam cálculos correntes;
- quais parâmetros servem apenas como padrão para novos cadastros;
- quais alterações não reescrevem Produtos ou histórico comercial existente.

Esta MEL é exclusivamente de apresentação e explicabilidade.

Não alterar valores persistidos, defaults, fórmulas, validações ou regras de negócio.

## Problema

Os rótulos atuais são tecnicamente corretos, porém exigem conhecimento prévio do modelo de precificação.

Exemplos:

~~~text
Valor da hora de trabalho
Margem padrão para novos produtos
Incremento comercial de arredondamento
Reserva comercial para desconto
~~~

Sem ajuda contextual, o usuário pode não saber:

- se a Margem padrão altera Produtos existentes;
- se o Incremento comercial é um percentual ou valor monetário;
- como o Incremento chega ao Preço sugerido;
- se a Reserva comercial é um desconto automaticamente aplicado;
- quando hora de trabalho ou tarifa de energia tornam um cálculo incompleto.

## Escopo de interface

Aplicar a ajuda contextual às duas superfícies já existentes:

~~~text
GET /Configuracoes/Precificacao
GET /Configuracoes/Precificacao/Editar
~~~

### Consulta

Na página de consulta, cada valor deve possuir sua explicação visível imediatamente abaixo do valor ou no mesmo bloco semântico.

### Edição

Na página de edição, cada input deve possuir sua explicação visível imediatamente abaixo do campo.

A ajuda deve continuar presente quando:

- o campo está vazio;
- há erro de validação;
- o POST inválido retorna a própria página.

## Decisão de UX — ajuda sempre visível

Para o MVP, usar **texto de ajuda sempre visível**, em vez de tooltip como mecanismo principal.

Motivos:

- funciona em desktop e mobile;
- não depende de hover;
- não depende de JavaScript;
- é naturalmente descobrível;
- pode ser associado diretamente ao input para tecnologias assistivas;
- reduz a chance de o usuário configurar um parâmetro econômico sem compreender seu efeito.

É aceitável usar o estilo Bootstrap já existente, por exemplo:

~~~html
<div class="form-text">...</div>
~~~

Não implementar tooltip/popover apenas para atender esta MEL.

Não usar `title` como única forma de explicação.

## Centralização do conteúdo

A mesma explicação deve ser usada na consulta e na edição.

Evitar duplicar textos longos em duas Razor Pages.

Criar um helper de apresentação equivalente a:

~~~text
ConfiguracaoPrecificacaoAjuda
~~~

com uma fonte única para os cinco textos.

Exemplo estrutural aceitável:

~~~csharp
public static class ConfiguracaoPrecificacaoAjuda
{
    public const string ValorHoraTrabalho = "...";
    public const string TarifaEnergiaKwh = "...";
    public const string MargemPadrao = "...";
    public const string IncrementoComercial = "...";
    public const string ReservaComercialDesconto = "...";
}
~~~

Também é aceitável componente/partial compartilhado, desde que:

- exista uma única fonte normativa do texto na Web;
- consulta e edição não possam divergir silenciosamente.

Não colocar essas explicações no Core.

## Conteúdo normativo da ajuda

Os textos abaixo são o conteúdo funcional mínimo. Pequenos ajustes de pontuação podem ser feitos na implementação, mas não alterar o significado.

### 1. Valor da hora de trabalho

Texto esperado:

> Valor atribuído a uma hora de trabalho ativo. O sistema usa o tempo ativo da Ficha Técnica, convertido em horas, para calcular o custo de mão de obra do lote. Se houver tempo ativo e este valor não estiver configurado, o custo de mão de obra fica indisponível.

Semântica que deve permanecer correta:

~~~text
CustoMaoDeObraLote =
    (TempoAtivoMinutos / 60) × ValorHoraTrabalho
~~~

Observações:

- `null` = Não configurado;
- zero explicitamente configurado é válido;
- se TempoAtivoMinutos = 0, o componente pode ser determinável como zero mesmo com ValorHoraTrabalho ausente;
- MEL017 não precisa expor essa exceção de Tempo zero no texto principal, mas não pode escrever algo que a contradiga.

### 2. Tarifa de energia

Texto esperado:

> Valor pago por 1 kWh de energia. O sistema aplica essa tarifa ao consumo calculado a partir da potência e do tempo de uso dos equipamentos da Ficha Técnica. Se houver uso de equipamento e a tarifa não estiver configurada, o custo de energia fica indisponível.

Semântica:

~~~text
ConsumoKwh = PotenciaKw × (TempoUsoMinutos / 60)
CustoEnergiaUso = ConsumoKwh × TarifaEnergiaKwh
~~~

Observações:

- `null` = Não configurado;
- zero explicitamente configurado é válido;
- Ficha sem usos de equipamento possui componente de energia determinável como zero;
- não alterar cadastro/uso de equipamentos.

### 3. Margem padrão para novos produtos

Texto esperado:

> Valor usado somente para pré-preencher a Margem-alvo ao cadastrar um novo Produto. O usuário pode alterar a margem antes de salvar, e Produtos já cadastrados não são modificados quando esta configuração muda.

Semântica:

~~~text
MargemPadrao configurada
-> GET /Produtos/Novo pré-preenche MargemAlvoPercentual
-> usuário pode substituir
-> Produto salva sua própria MargemAlvo
~~~

Deixar explícito por meio do texto que:

- não é uma margem global aplicada continuamente a todos os Produtos;
- não altera Produto existente;
- não recalcula MargemAlvo existente;
- `null` deixa o campo de novo Produto sem pré-preenchimento;
- zero configurado pré-preenche 0%.

### 4. Incremento comercial de arredondamento

Texto esperado:

> Define o múltiplo monetário usado para arredondar o Preço teórico para cima e formar o Preço sugerido. Ex.: com incremento de R$ 0,50, um Preço teórico de R$ 12,13 gera Preço sugerido de R$ 12,50. Se o Preço teórico já for um múltiplo exato, ele é mantido. Sem esta configuração, o Preço sugerido fica indisponível.

Semântica:

~~~text
PrecoSugerido =
    Ceiling(PrecoTeorico / IncrementoComercial)
    × IncrementoComercial
~~~

Exemplos normativos:

~~~text
Incremento = R$ 0,50
Preço teórico = R$ 12,13
Preço sugerido = R$ 12,50

Incremento = R$ 0,50
Preço teórico = R$ 12,50
Preço sugerido = R$ 12,50
~~~

Não descrever o Incremento como:

- percentual;
- margem;
- taxa adicionada ao custo;
- desconto.

É um **múltiplo monetário de arredondamento para cima**.

### 5. Reserva comercial para desconto

Texto esperado:

> Percentual do valor acima do Preço sugerido que fica reservado antes de calcular o Desconto de referência. Ex.: com reserva de 10%, o desconto só passa a ser aplicável quando o Preço de prateleira estiver pelo menos 11% acima do sugerido; a parcela que excede os 10% forma o Desconto de referência. Alterações valem para novos registros comerciais e não reinterpretam o histórico existente.

A ajuda deve evitar a interpretação de que Reserva comercial é um desconto aplicado automaticamente.

Semântica atual:

~~~text
PercentualAcimaSugerido =
    (PrecoPrateleira / PrecoSugerido) - 1

LimiarAplicacao =
    ReservaComercialReferencia + 0,01

se PercentualAcimaSugerido >= LimiarAplicacao:
    DescontoReferencia =
        PercentualAcimaSugerido - ReservaComercialReferencia
~~~

Exemplo com reserva 10%:

~~~text
Preço de prateleira 10% acima do sugerido
=> desconto de referência não aplicável

Preço de prateleira 11% acima do sugerido
=> desconto de referência = 1%

Preço de prateleira 20% acima do sugerido
=> desconto de referência = 10%
~~~

Importante:

- a configuração atual é congelada como `ReservaComercialReferencia` quando um novo preço de prateleira/snapshot é registrado;
- alterar a Reserva depois não reinterpreta registros históricos;
- Reserva não participa do cálculo do Preço sugerido.

## Ajuda na tela de consulta

Arquivo atual:

~~~text
src/Precificador.Web/Pages/Configuracoes/Precificacao.cshtml
~~~

Para cada par `<dt>/<dd>`:

1. manter o rótulo existente;
2. manter o valor formatado existente;
3. adicionar a ajuda logo abaixo do valor.

Exemplo estrutural:

~~~html
<dt>Incremento comercial de arredondamento</dt>
<dd>
    R$ 0,50
    <div class="form-text">...</div>
</dd>
~~~

A ajuda aparece mesmo quando o valor é:

~~~text
Não configurado
~~~

Não condicionar a explicação à existência de valor.

## Ajuda na tela de edição

Arquivo atual:

~~~text
src/Precificador.Web/Pages/Configuracoes/Precificacao/Editar.cshtml
~~~

Para cada campo:

1. manter `<label asp-for>`;
2. manter o `<input asp-for>`;
3. manter `<span asp-validation-for>`;
4. adicionar texto de ajuda visível;
5. associar semanticamente o input ao texto de ajuda.

### IDs sugeridos

~~~text
ajuda-valor-hora-trabalho
ajuda-tarifa-energia
ajuda-margem-padrao
ajuda-incremento-comercial
ajuda-reserva-comercial
~~~

### aria-describedby

Cada input deve apontar para seu texto:

~~~html
<input ... aria-describedby="ajuda-incremento-comercial" />
<div id="ajuda-incremento-comercial" class="form-text">...</div>
~~~

Se Razor já gerar outro `aria-describedby`, combinar IDs sem remover acessibilidade existente.

O objetivo é que tecnologias assistivas relacionem campo e explicação diretamente.

## Validação e ajuda

Erros de validação continuam sendo erros e a ajuda continua sendo ajuda.

Não substituir:

~~~html
<span asp-validation-for="..."></span>
~~~

pela explicação.

Em POST inválido:

- valores digitados continuam preservados;
- mensagens de validação continuam visíveis;
- ajuda contextual continua visível;
- nenhuma configuração é persistida parcialmente.

MEL017 não altera qualquer mensagem de validação existente.

## Estado Não configurado

A MEL017 não muda a semântica de `null`.

Continuar exibindo:

~~~text
Não configurado
~~~

para:

- ValorHoraTrabalho;
- TarifaEnergiaKwh;
- MargemPadrao;
- IncrementoComercial;

quando o valor persistido for `null`.

A ajuda aparece ao lado desse estado para explicar a consequência.

Não converter `null` em zero.

## Zero configurado

Preservar integralmente UC027:

Zero é válido para:

- ValorHoraTrabalho;
- TarifaEnergiaKwh;
- MargemPadrao;
- ReservaComercialDesconto.

Zero é inválido para IncrementoComercial quando informado.

A inclusão dos textos de ajuda não altera parsing/validação.

## Formatação

Preservar MEL015.

Não alterar:

- formatação monetária;
- precisão dos inputs de edição;
- formato pt-BR;
- conversão percentual/fração;
- `ConfiguracaoPrecificacaoFormatacao`;
- `ConfiguracaoPrecificacaoFormulario`.

O exemplo `R$ 0,50 / R$ 12,13 / R$ 12,50` é conteúdo explicativo, não nova regra de formatação de inputs.

## Efeito temporal das configurações

A ajuda deve refletir o comportamento real, sem sugerir uma única política temporal para todos os campos.

### ValorHoraTrabalho

Afeta os próximos cálculos atuais de mão de obra da Empresa, pois o custo é derivado em consulta.

### TarifaEnergiaKwh

Afeta os próximos cálculos atuais de energia da Empresa, pois o custo é derivado em consulta.

### MargemPadrao

Afeta somente o pré-preenchimento de novos Produtos.

Não altera Produto existente.

### IncrementoComercial

Afeta o próximo cálculo atual de Preço sugerido.

Registros comerciais históricos preservam seus snapshots.

### ReservaComercialDesconto

É usada como referência para novos registros comerciais e congelada no snapshot correspondente.

Não reinterpreta histórico já persistido.

## Multiempresa

Nenhuma mudança de comportamento.

As explicações são estáticas e independentes do tenant; os valores continuam tenant-aware.

Preservar:

- GQF;
- Empresa Ativa;
- ausência de `EmpresaId` no formulário;
- write guards;
- sem `IgnoreQueryFilters` no fluxo Web.

Não incluir nome/valor de outra Empresa na ajuda.

## Segurança

Não alterar autenticação, autorização ou antiforgery.

As rotas continuam protegidas conforme UC026/UC027.

## Persistência

Nenhuma alteração.

Não criar migration.

Não alterar:

- `ConfiguracaoPrecificacaoEmpresa`;
- DbContext;
- ModelSnapshot;
- valores/defaults existentes;
- histórico;
- Produtos.

## Arquivos esperados

Mudança mínima esperada:

~~~text
src/Precificador.Web/Pages/Configuracoes/Precificacao.cshtml
src/Precificador.Web/Pages/Configuracoes/Precificacao/Editar.cshtml
src/Precificador.Web/Apresentacao/ConfiguracaoPrecificacaoAjuda.cs
    ou componente compartilhado equivalente

tests/Precificador.Tests.Integration/Web/ConfiguracaoPrecificacaoPageTests.cs

docs/development/improvements/MEL017-ajuda-configuracoes-precificacao.md
docs/development/backlog.md
~~~

Não é esperado alterar PageModels.

Se a implementação exigir alteração de PageModel, justificar na PR; a ajuda não depende de nova consulta ou dado dinâmico.

## Critérios de aceitação

- **CA01:** consulta exibe ajuda para Valor da hora de trabalho.
- **CA02:** consulta exibe ajuda para Tarifa de energia.
- **CA03:** consulta exibe ajuda para Margem padrão.
- **CA04:** consulta exibe ajuda para Incremento comercial.
- **CA05:** consulta exibe ajuda para Reserva comercial.
- **CA06:** edição exibe as mesmas cinco explicações.
- **CA07:** consulta e edição usam uma fonte compartilhada de conteúdo ou equivalente que evite divergência.
- **CA08:** ajuda permanece visível para parâmetros `Não configurado`.
- **CA09:** ajuda permanece visível em POST inválido.
- **CA10:** cada input da edição é associado à sua ajuda por `aria-describedby` ou semântica acessível equivalente.
- **CA11:** nenhuma ajuda depende exclusivamente de `title`, hover ou JavaScript.
- **CA12:** Incremento é descrito como múltiplo monetário de arredondamento para cima.
- **CA13:** exemplo R$ 0,50 / R$ 12,13 / R$ 12,50 aparece na ajuda do Incremento.
- **CA14:** texto deixa claro que múltiplo exato é mantido.
- **CA15:** texto deixa claro que Incremento ausente deixa Preço sugerido indisponível.
- **CA16:** Margem padrão é descrita como pré-preenchimento de novos Produtos.
- **CA17:** ajuda deixa claro que mudar Margem padrão não altera Produtos existentes.
- **CA18:** ValorHoraTrabalho é relacionado ao tempo ativo/custo de mão de obra.
- **CA19:** TarifaEnergiaKwh é relacionada ao consumo/custo dos equipamentos.
- **CA20:** Reserva é explicitamente diferenciada de desconto automático.
- **CA21:** exemplo de Reserva 10% informa corretamente o limiar de 11%.
- **CA22:** ajuda de Reserva deixa claro que histórico existente não é reinterpretado.
- **CA23:** valores, parsing, validações e fórmulas permanecem inalterados.
- **CA24:** `null` continua diferente de zero.
- **CA25:** nenhuma migration/schema/model snapshot é alterado.
- **CA26:** autenticação, tenant isolation e antiforgery permanecem inalterados.
- **CA27:** MEL015 continua sem regressão.
- **CA28:** itens posteriores da fila não são antecipados.

## Matriz de testes

Priorizar:

~~~text
tests/Precificador.Tests.Integration/Web/ConfiguracaoPrecificacaoPageTests.cs
~~~

### Consulta

#### W1 — cinco ajudas visíveis

GET:

~~~text
/Configuracoes/Precificacao
~~~

Validar presença dos cinco conteúdos.

Não testar apenas palavras genéricas como `custo` ou `preço`; usar trechos distintivos de cada ajuda.

#### W2 — ajuda com valores não configurados

Na Empresa padrão:

- quatro campos continuam exibindo `Não configurado`;
- as cinco ajudas continuam presentes.

#### W3 — valores configurados permanecem formatados

Preparar valores conhecidos e validar que os textos não alteram:

~~~text
R$ 12,34
R$ 0,98
30%
R$ 0,50
15%
~~~

Pode reaproveitar cobertura existente.

### Edição

#### W4 — cinco ajudas visíveis

GET:

~~~text
/Configuracoes/Precificacao/Editar
~~~

Validar as mesmas explicações da consulta.

#### W5 — associação acessível

Para cada input, verificar `aria-describedby` contendo o ID da respectiva ajuda.

Campos:

~~~text
Input.ValorHoraTrabalho
Input.TarifaEnergiaKwh
Input.MargemPadraoPercentual
Input.IncrementoComercial
Input.ReservaComercialDescontoPercentual
~~~

Também confirmar que cada ID referenciado existe no HTML.

#### W6 — POST inválido preserva ajuda

Enviar, por exemplo:

~~~text
Input.TarifaEnergiaKwh = abc
~~~

Confirmar:

- HTTP 200;
- erro de validação existente;
- valor `abc` preservado;
- cinco ajudas continuam presentes;
- banco não alterado.

### Conteúdo do Incremento

#### W7

Validar presença de trechos que comprovem:

~~~text
R$ 0,50
R$ 12,13
R$ 12,50
Preço sugerido
múltiplo
~~~

Não exige reproduzir fórmula matemática na UI.

### Conteúdo da Margem padrão

#### W8

Validar que a ajuda comunica:

- novos Produtos;
- pré-preenchimento;
- Produtos existentes não são alterados.

### Conteúdo da Reserva

#### W9

Validar que a ajuda comunica:

- reserva de 10%;
- limiar de 11%;
- Desconto de referência;
- histórico não reinterpretado.

### Regressão

#### W10

Suíte existente de UC026/UC027 permanece verde, especialmente:

- null vs zero;
- parsing com vírgula;
- precisão MEL015;
- PRG;
- cross-tenant;
- antiforgery;
- Margem padrão em Produto Novo.

## Testes unitários

Nenhum novo teste unitário é obrigatório se a implementação introduzir apenas constantes/textos de apresentação.

Não criar teste unitário para comparar string constante com ela mesma.

A cobertura útil é Web, provando que o conteúdo aparece e está associado corretamente aos inputs.

## Documentação normativa

Alinhar:

- MEL017;
- F006;
- UC026 — consulta passa a exibir ajuda contextual dos cinco parâmetros;
- UC027 — edição passa a exibir ajuda contextual dos cinco parâmetros;
- backlog.

MEL014 permanece responsável pela documentação abrangente no Manual do Usuário.

MEL017 resolve a dúvida no ponto de uso.

## Fora do escopo

- alterar regras de precificação;
- alterar fórmulas;
- alterar defaults;
- alterar validações;
- alterar nomes persistidos;
- novo campo de configuração;
- histórico de configuração;
- auditoria;
- modal de ajuda;
- tour guiado;
- tooltip/popover complexo;
- JavaScript novo para explicações;
- alteração de Produto existente;
- alteração de histórico comercial;
- MEL012;
- UC028+;
- Manual do Usuário MEL014.

## Definition of Done específica

MEL017 está concluída quando:

- as cinco configurações possuem explicação contextual na consulta;
- as cinco possuem a mesma explicação na edição;
- textos estão centralizados ou compartilham uma única fonte;
- inputs possuem associação acessível com a ajuda;
- conteúdo descreve corretamente os efeitos econômicos reais;
- Incremento contém o exemplo normativo R$ 0,50 / R$ 12,13 / R$ 12,50;
- Reserva explica corretamente 10% -> limiar 11%;
- Margem deixa claro que não altera Produtos existentes;
- POST inválido continua exibindo ajuda e preservando inputs;
- nenhuma migration/schema/domínio/cálculo é alterado;
- testes W1–W10 estão cobertos ou comprovados por regressão existente;
- build Release tem 0 erros e sem warnings novos relevantes;
- suíte completa está verde;
- backlog altera somente MEL017 de `Pronto` para `Concluído` na PR de implementação.

## Branch sugerida

~~~text
fix/mel017-ajuda-configuracoes-precificacao
~~~

## Commit sugerido

~~~text
fix: explica configuracoes de precificacao na interface
~~~
