# Instrução Codex — MEL017: Explicar configurações de precificação na interface

## Tarefa

Implementar integralmente:

~~~text
docs/development/improvements/MEL017-ajuda-configuracoes-precificacao.md
~~~

Branch sugerida:

~~~text
fix/mel017-ajuda-configuracoes-precificacao
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- MEL017;
- F006;
- UC026;
- UC027;
- MEL015;
- MEL009;
- RN013, RN014, RN021, RN052, RN053 e RN054;
- `Pages/Configuracoes/Precificacao.cshtml`;
- `Pages/Configuracoes/Precificacao/Editar.cshtml`;
- `ConfiguracaoPrecificacaoInputModel`;
- `ConfiguracaoPrecificacaoFormulario`;
- `ConfiguracaoPrecificacaoPageTests`.

## Escopo

Adicionar explicação contextual para os cinco parâmetros:

1. Valor da hora de trabalho;
2. Tarifa de energia;
3. Margem padrão para novos produtos;
4. Incremento comercial de arredondamento;
5. Reserva comercial para desconto.

Aplicar em:

~~~text
/Configuracoes/Precificacao
/Configuracoes/Precificacao/Editar
~~~

Não alterar cálculos, domínio, schema, parsing ou validações.

## Forma de apresentação

Usar texto de ajuda **sempre visível** abaixo do valor/input.

Preferência estrutural:

~~~html
<div class="form-text">...</div>
~~~

Não implementar tooltip/popover/JavaScript para esta MEL.

Não depender de `title`.

## Centralização

Consulta e edição devem usar a mesma fonte de conteúdo.

Criar, preferencialmente:

~~~text
src/Precificador.Web/Apresentacao/ConfiguracaoPrecificacaoAjuda.cs
~~~

ou componente equivalente.

Não duplicar as cinco explicações em duas Razor Pages.

Não colocar os textos no Core.

## Conteúdo funcional

### Valor da hora

Comunicar que:

- representa uma hora de trabalho ativo;
- tempo ativo da Ficha é convertido em horas;
- o valor compõe o custo de mão de obra do lote;
- se houver tempo ativo e o valor estiver ausente, o custo fica indisponível.

### Tarifa de energia

Comunicar que:

- representa o valor de 1 kWh;
- potência + tempo geram consumo;
- consumo × tarifa gera custo de energia;
- com uso de equipamento e tarifa ausente, o custo fica indisponível.

### Margem padrão

Comunicar explicitamente:

- serve somente para pré-preencher novos Produtos;
- usuário pode alterar antes de salvar;
- Produtos existentes não são modificados quando a configuração muda.

### Incremento comercial

Comunicar:

- é valor monetário, não percentual;
- define o múltiplo de arredondamento para cima do Preço teórico;
- forma o Preço sugerido;
- múltiplo exato é mantido;
- sem configuração, Preço sugerido fica indisponível.

Exemplo obrigatório:

~~~text
Incremento = R$ 0,50
Preço teórico = R$ 12,13
Preço sugerido = R$ 12,50
~~~

### Reserva comercial

Comunicar:

- não é desconto automático;
- representa a parcela reservada do valor acima do Preço sugerido;
- com reserva 10%, desconto de referência só se aplica a partir de 11% acima do sugerido;
- o que excede os 10% forma o desconto de referência;
- alteração atual só vale para novos registros comerciais;
- histórico existente não é reinterpretado.

## Consulta

Em `Precificacao.cshtml`:

- manter labels;
- manter valores formatados;
- adicionar ajuda sob cada valor;
- ajuda também aparece quando valor = `Não configurado`.

## Edição

Em `Editar.cshtml`:

- manter labels;
- manter inputs;
- manter spans de validação;
- adicionar ajuda abaixo de cada campo;
- associar input e ajuda via `aria-describedby`.

IDs sugeridos:

~~~text
ajuda-valor-hora-trabalho
ajuda-tarifa-energia
ajuda-margem-padrao
ajuda-incremento-comercial
ajuda-reserva-comercial
~~~

Se já existir `aria-describedby`, combinar os IDs.

## POST inválido

A ajuda continua visível.

Preservar:

- input digitado;
- mensagens atuais;
- banco inalterado.

Não mudar PageModel para reintroduzir valores persistidos sobre POST inválido.

## Não alterar

- `ConfiguracaoPrecificacaoEmpresa`;
- DbContext;
- migration/ModelSnapshot;
- `ConfiguracaoPrecificacaoFormulario`;
- `ConfiguracaoPrecificacaoFormatacao`;
- validações;
- parsing;
- MEL015;
- valores `null`/zero;
- regras tenant-aware;
- antiforgery.

Não é esperado alterar nenhum `.cshtml.cs`.

## Testes obrigatórios

Cobrir W1–W10 da MEL017 em `ConfiguracaoPrecificacaoPageTests`.

Prioridades:

1. cinco ajudas na consulta;
2. cinco ajudas na edição;
3. mesma fonte/conteúdo;
4. `aria-describedby` nos cinco inputs;
5. IDs referenciados realmente existem;
6. ajuda aparece com `Não configurado`;
7. POST inválido mantém ajuda;
8. exemplo de Incremento está presente;
9. texto de Margem deixa claro novos Produtos / sem retroatividade;
10. texto de Reserva deixa claro 10% -> 11% e histórico congelado;
11. UC026/UC027 existentes continuam verdes.

Não criar unitário inútil para comparar constante com ela mesma.

## Persistência

Nenhuma migration.

Nenhum arquivo de migrations/ModelSnapshot deve mudar.

## Backlog

Na PR de implementação:

~~~text
MEL017: Pronto -> Concluído
~~~

Não alterar MEL012 ou itens posteriores.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

Informar:

- abordagem usada para centralizar textos;
- como acessibilidade foi ligada aos inputs;
- testes adicionados/alterados;
- confirmação de nenhuma migration;
- resultado build/test;
- URL da PR.
