# MEL015 — Padronizar apresentação monetária e entrada decimal pt-BR

- **Tipo:** correção transversal Web / apresentação
- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Prioridade:** alta
- **Estado:** Pronto
- **Ordem na fila pendente:** 01
- **Dependências:** UC005, UC011, UC018–UC025 concluídos
- **Bloqueia:** MEL016, MEL018 e retomada confiável dos testes manuais de custo/precificação
- **Alteração de schema:** não
- **Migration:** não
- **Alteração de fórmulas Core:** não

## Objetivo

Corrigir a interpretação de números decimais informados pelo usuário em fluxos financeiros e padronizar a apresentação de valores monetários em pt-BR, sem alterar a precisão interna, as fórmulas de custo/preço ou os valores persistidos corretamente.

A MEL015 resolve dois problemas diferentes, porém relacionados:

1. **entrada:** valores com vírgula decimal podem ser interpretados incorretamente pelo binding direto para `decimal`;
2. **apresentação:** valores financeiros são exibidos com quantidade de casas decimais e convenções visuais inconsistentes.

A melhoria é bloqueante para os testes manuais de precificação porque um preço digitado incorretamente pode contaminar corretamente toda a cadeia UC018–UC025.

## Defeito reproduzido

Cenário observado:

~~~text
Quantidade da embalagem = 200 g
Preço informado = 20,99
Quantidade usada na Ficha = 50 g
~~~

O POST atual usa binding direto de `decimal` em `PrecoCompra`. No ambiente observado, `20,99` foi interpretado/persistido como:

~~~text
2099
~~~

Consequentemente, UC018 calculou corretamente:

~~~text
2099 / 200 * 50 = 524,75
~~~

Portanto, **não há evidência de erro na fórmula de custo**.

Depois da MEL015, o valor persistido deve ser:

~~~text
20,99
~~~

e o mesmo cenário deve produzir internamente:

~~~text
20,99 / 200 * 50 = 5,2475
~~~

antes de qualquer formatação visual.

## Princípios

1. entrada do usuário não depende da cultura padrão do processo/host;
2. vírgula e ponto podem ser usados como separador decimal nos formatos simples definidos nesta MEL;
3. nenhum separador decimal pode ser reinterpretado silenciosamente como separador de milhar;
4. valores inválidos permanecem no formulário para correção;
5. parsing e apresentação ficam na camada Web;
6. domínio e calculadoras continuam recebendo `decimal`;
7. RN026 permanece válida: **sem arredondamento intermediário**;
8. arredondamento visual nunca altera valor persistido;
9. GET/read-only pode arredondar somente a string exibida;
10. campos editáveis que carregam valor persistido devem preservar sua precisão real;
11. `null` continua diferente de zero;
12. nenhum registro histórico existente é reinterpretado ou corrigido automaticamente.

## Escopo de entrada decimal

### Campos diretamente afetados

Os três campos abaixo usam atualmente binding direto para `decimal` e devem deixar de depender desse binding para interpretar entrada localizada:

~~~text
/Insumos/Precos/Novo/{id}
- Input.QuantidadeCompra
- Input.PrecoCompra

/Produtos/Precos/Novo/{id}
- Input.PrecoPrateleira
~~~

### Direção obrigatória

O modelo de entrada Web desses campos deve preservar o texto postado até que o servidor faça parsing explícito.

Solução preferencial:

~~~text
Input.<campo> : string?
        ↓
parser decimal Web
        ↓
decimal validado
        ↓
domínio/persistência
~~~

É aceitável solução equivalente, desde que:

- `20,99` nunca seja interpretado como `2099`;
- erro de parse não seja confundido com zero;
- o texto inválido continue visível após POST inválido;
- não seja necessário alterar o Core.

Não criar custom model binder global para toda a aplicação apenas para esta MEL, salvo necessidade comprovada. Preferir solução explícita e localizada/reutilizável na camada Web.

## Regra de parsing decimal

Criar ou reutilizar um helper Web centralizado para os campos cobertos por esta MEL.

O parser deve aceitar, no mínimo:

~~~text
10
10,5
10,50
10.5
10.50
0,25
0.25
200,5
200.5
~~~

Semântica:

~~~text
"20,99"  => 20.99m
"20.99"  => 20.99m
"200,5"  => 200.5m
"200.5"  => 200.5m
~~~

### Regra para separadores

Nesta fase, **não suportar separador de milhar em inputs numéricos**.

Assim:

- uma única vírgula => separador decimal;
- um único ponto => separador decimal;
- mais de um separador ou combinação de ponto + vírgula => entrada inválida;
- espaços externos podem ser removidos;
- espaços internos, `R$`, notação científica ou outros símbolos não são obrigatórios nesta MEL.

Exemplos que podem ser rejeitados:

~~~text
1.234,56
1,234.56
1,2,3
R$ 20,99
1e3
~~~

O objetivo é evitar qualquer ambiguidade capaz de transformar `20,99` em `2099`.

### Sinal

O parser pode reconhecer sinal negativo sintaticamente.

As regras de domínio/formulário continuam decidindo validade:

- QuantidadeCompra <= 0 => inválida;
- PrecoCompra <= 0 => inválido;
- PrecoPrateleira <= 0 => inválido.

Não criar nova regra de negócio.

## Mensagens de validação

### Preço do Insumo

Se quantidade não puder ser interpretada:

~~~text
A quantidade deve ser um número válido.
~~~

Se preço não puder ser interpretado:

~~~text
O preço deve ser um número válido.
~~~

Depois do parse, preservar as validações existentes de maior que zero.

É aceitável manter mensagens de domínio existentes para valores <= 0, desde que o campo correto receba o erro.

### Preço de prateleira

Se não puder ser interpretado:

~~~text
O preço de prateleira deve ser um número válido.
~~~

Se <= 0, manter:

~~~text
O preço de prateleira deve ser maior que zero.
~~~

### POST inválido

O valor textual digitado deve permanecer no input.

Exemplo:

~~~text
20,99,1
~~~

retorna página com o mesmo texto e erro, sem persistir registro.

## Formulários já protegidos

Os fluxos abaixo já fazem parsing explícito de strings e devem ser **revalidados**, não reescritos sem necessidade:

- Configurações de precificação;
- Margem-alvo de Produto;
- Rendimento da Ficha;
- Quantidade/Percentual de perda de Item;
- Potência de equipamento.

A MEL015 não deve produzir regressão nesses parsers.

## Precisão persistida

Não alterar configurações EF atuais.

Exemplos existentes:

~~~text
PrecoInsumo.PrecoCompra        => 18,4
PrecoInsumo.QuantidadeCompra   => 18,6
RegistroPrecoProduto           => 18,6
configurações monetárias       => 18,6
~~~

Não criar migration.

Não reduzir escala de coluna.

Não adicionar `Math.Round` antes de persistir.

## Dados existentes incorretos

Não fazer correção automática de registros existentes.

Um valor persistido como:

~~~text
2099
~~~

pode significar legitimamente R$ 2.099,00 ou pode ter vindo do defeito de parsing.

A aplicação não possui informação suficiente para inferir a intenção original.

Portanto:

- MEL015 corrige novos POSTs;
- não criar migration de dados;
- não alterar históricos;
- massa local contaminada deve ser corrigida/recriada explicitamente durante o teste manual.

## Convenção de apresentação

### Cultura

Toda apresentação monetária coberta por esta MEL usa:

~~~text
pt-BR
~~~

### Montantes monetários

Valores que representam montante/preço/custo monetário devem ser exibidos com símbolo e **duas casas decimais**:

~~~text
0       => R$ 0,00
5       => R$ 5,00
5,4     => R$ 5,40
20,99   => R$ 20,99
1234,56 => R$ 1.234,56
12,345  => R$ 12,35
~~~

O arredondamento é somente de apresentação.

Não modificar o `decimal` original.

### Custo unitário técnico do Insumo

O custo por unidade-base do Insumo é exceção porque valores pequenos são economicamente relevantes.

Exemplo:

~~~text
R$ 20,99 / 200 g = R$ 0,10495/g
R$ 5,39 / 1000 g = R$ 0,00539/g
~~~

Esse campo deve preservar até 6 casas úteis na apresentação, com no mínimo duas casas:

~~~text
R$ 5,00
R$ 0,10495
R$ 0,00539
~~~

Não exibir `R$ 0,00` quando o valor técnico conhecido é positivo e menor que R$ 0,005.

É aceitável formato equivalente a:

~~~text
0.00####
~~~

em cultura pt-BR.

### Valores não monetários

Não transformar em moeda:

- Quantidade;
- Rendimento;
- Potência;
- Consumo;
- Tempo;
- Margens;
- Percentuais de perda;
- Reserva comercial percentual.

Preservar formatadores específicos existentes.

## Campos/telas que devem usar montante monetário de duas casas

### Insumo — Detalhes

Em **Preço vigente**:

- Preço total => moeda com 2 casas;
- Custo unitário => custo técnico com até 6 casas;
- Quantidade => formato técnico atual.

Exemplo do defeito:

~~~text
PrecoCompra = 20.99m
=> R$ 20,99
~~~

Nunca:

~~~text
2099
R$ 2.099,00
~~~

quando o registro foi corretamente persistido como `20.99m`.

### Insumo — Histórico de preços

Em resumo e tabela:

- Preço total => moeda com 2 casas;
- Custo unitário => custo técnico;
- Quantidade => formato técnico atual.

MEL016 alterará posteriormente os rótulos “Quantidade”/“Preço total”; não antecipar a troca de vocabulário nesta implementação.

### Ficha Técnica

Tabela de Itens:

- Custo unitário do Insumo => custo técnico;
- Custo do Item => moeda com 2 casas;
- Custo da perda => moeda com 2 casas.

Totais/blocos:

- Custo base dos Itens => moeda com 2 casas;
- Custo de perdas do lote => moeda com 2 casas;
- Custo de mão de obra do lote => moeda com 2 casas;
- Custo de energia por equipamento => moeda com 2 casas;
- Custo de energia do lote => moeda com 2 casas;
- Custo total do lote => moeda com 2 casas;
- Custo unitário do Produto => moeda com 2 casas;
- Preço teórico => moeda com 2 casas;
- Preço sugerido => moeda com 2 casas.

### Produto — Detalhes

- Custo unitário atual => moeda com 2 casas;
- Preço de prateleira atual => moeda com 2 casas.

Margens continuam percentuais.

### Registrar Preço de prateleira

Resumo:

- Custo unitário => moeda com 2 casas;
- Preço teórico => moeda com 2 casas;
- Preço sugerido => moeda com 2 casas.

Input `PrecoPrateleira` continua sendo texto editável e **não** deve ser reformatado com arredondamento antes do POST.

### Histórico de precificação do Produto

Resumo e tabela:

- Custo de referência => moeda com 2 casas;
- Preço sugerido => moeda com 2 casas;
- Preço de prateleira => moeda com 2 casas.

Margem/Reserva/Desconto continuam percentuais.

### Detalhamento da precificação

Usar moeda com duas casas em todos os montantes:

- Preço de prateleira atual;
- Preço sugerido;
- Valor da hora de trabalho;
- Tarifa de energia;
- Incremento comercial;
- Custo base do Item;
- Custo da perda;
- Custo base dos Itens;
- Custo de perdas do lote;
- Custo de mão de obra;
- Custo de energia por equipamento;
- Custo de energia do lote;
- Custo total do lote;
- Custo unitário do Produto;
- Preço teórico;
- Preço sugerido.

O **Custo unitário atual do Insumo** na tabela permanece no formato técnico de maior precisão.

### Configurações de precificação — consulta

Na página somente leitura:

- Valor da hora de trabalho => moeda com 2 casas;
- Tarifa de energia => moeda com 2 casas;
- Incremento comercial => moeda com 2 casas.

Percentuais permanecem percentuais.

### Configurações — edição

Não reduzir precisão dos inputs carregados.

Exemplo:

~~~text
IncrementoComercial persistido = 0.500001
input GET = 0,500001
~~~

e não:

~~~text
0,50
~~~

porque salvar o formulário sem intenção de alteração não pode destruir precisão.

Essa regra vale para qualquer campo editável que carregue valor persistido.

## Centralização da apresentação

Evoluir os helpers Web existentes em vez de espalhar `ToString` pelas Razor Pages.

É aceitável:

- ampliar `PrecoInsumoFormatacao`;
- ampliar `ConfiguracaoPrecificacaoFormatacao`;
- criar helper de moeda/decimal comum na camada Web.

A solução deve deixar clara a diferença entre:

~~~text
MontanteMonetario
CustoUnitarioTecnico
Quantidade/NumeroTecnico
Percentual
~~~

Evitar um método genérico como `CustoCalculado` sendo usado indistintamente quando os requisitos de apresentação forem diferentes.

Os nomes concretos podem variar.

## Não alterar cálculos

MEL015 não modifica:

- `PrecoInsumo.CustoUnitario`;
- CalculadoraCustoItens;
- CalculadoraCustoPerdas;
- CalculadoraCustoMaoDeObra;
- CalculadoraCustoEnergia;
- CalculadoraCustoProduto;
- CalculadoraPrecoProduto;
- CalculadoraMargemAtual;
- CalculadoraDescontoReferencia;
- `PrecificacaoProdutoAtual`.

Não adicionar arredondamento às calculadoras.

### Cenário de regressão obrigatório

Persistir via **Web POST**:

~~~text
QuantidadeCompra = "200"
PrecoCompra = "20,99"
~~~

Persistência esperada:

~~~text
QuantidadeCompra = 200m
PrecoCompra = 20.99m
CustoUnitario = 0.10495m
~~~

Com Item de Ficha:

~~~text
Quantidade = 50m
PercentualPerda = 0
~~~

resultado UC018 esperado antes de formatação:

~~~text
CustoItem = 5.2475m
~~~

Na UI, quando esse custo for exibido como montante:

~~~text
R$ 5,25
~~~

A diferença entre `5.2475m` e `R$ 5,25` deve demonstrar que o arredondamento é exclusivamente visual.

## Null e zero

Preservar contratos existentes:

~~~text
null => indisponível / Não configurado / não definido
0    => R$ 0,00 quando o valor monetário é conhecido
~~~

Nunca transformar zero em ausência.

Nunca transformar ausência em `R$ 0,00`.

## Multiempresa

Nenhuma mudança no modelo tenant-aware.

Preservar:

- GQF;
- Empresa Ativa;
- write guards;
- cross-tenant 404;
- nenhum `EmpresaId` vindo do formulário.

O parser não recebe/resolve contexto de Empresa.

## Persistência

Não alterar schema.

Não criar migration.

Não alterar `PrecificadorDbContextModelSnapshot`.

Não alterar precisões EF.

Não atualizar registros existentes apenas para “normalizar” casas decimais.

## Relação com MEL016

MEL015 corrige:

- parsing;
- formatação monetária.

MEL016, executada depois, corrigirá:

- `Quantidade comprada` => `Quantidade por embalagem`;
- `Preço total da compra` => `Preço por embalagem`;
- Data de referência default.

MEL015 **não deve antecipar** esses rótulos/defaults.

## Relação com MEL018

MEL018 depende desta melhoria porque os redirects pós-cadastro levarão o usuário diretamente para telas que exibem preço/custo.

MEL015 não altera redirects de cadastro.

## Critérios de aceitação

- **CA01:** `PrecoCompra = "20,99"` é persistido como `20.99m`.
- **CA02:** `PrecoCompra = "20.99"` é persistido como `20.99m`.
- **CA03:** `QuantidadeCompra = "200,5"` é persistida como `200.5m`.
- **CA04:** `QuantidadeCompra = "200.5"` é persistida como `200.5m`.
- **CA05:** `PrecoPrateleira = "20,99"` é persistido como `20.99m`.
- **CA06:** `PrecoPrateleira = "20.99"` é persistido como `20.99m`.
- **CA07:** entradas ambíguas/inválidas não são reinterpretadas e não persistem.
- **CA08:** POST inválido preserva o texto informado.
- **CA09:** zero/negativo continuam rejeitados conforme regras existentes.
- **CA10:** parsers já existentes de Configuração/Ficha/Item/Equipamento permanecem funcionais.
- **CA11:** Preço vigente `20.99m` aparece como `R$ 20,99`.
- **CA12:** custo unitário técnico `0.10495m` preserva precisão útil na apresentação.
- **CA13:** custo técnico positivo pequeno não é apresentado falsamente como zero.
- **CA14:** Custo do Item/perda/energia/mão de obra/totais usam moeda com 2 casas.
- **CA15:** Custo total do lote usa moeda com 2 casas.
- **CA16:** Custo unitário do Produto usa moeda com 2 casas.
- **CA17:** Preço teórico usa moeda com 2 casas.
- **CA18:** Preço sugerido usa moeda com 2 casas.
- **CA19:** Preço de prateleira atual/histórico usa moeda com 2 casas.
- **CA20:** snapshots monetários do histórico usam moeda com 2 casas sem alterar valores persistidos.
- **CA21:** consulta de Configuração usa apresentação monetária de 2 casas.
- **CA22:** edição de Configuração preserva precisão persistida nos inputs.
- **CA23:** cenário `200 g / R$ 20,99 / 50 g` produz `CustoItem = 5.2475m` antes da apresentação.
- **CA24:** o mesmo custo aparece como `R$ 5,25` quando exibido como montante.
- **CA25:** RN026 permanece intacta; nenhum cálculo usa valor visual arredondado.
- **CA26:** `null` não vira zero e zero conhecido vira `R$ 0,00`.
- **CA27:** nenhuma fórmula Core é alterada.
- **CA28:** nenhuma migration/model snapshot é alterada.
- **CA29:** nenhum registro existente é corrigido automaticamente.
- **CA30:** isolamento multiempresa permanece sem regressão.
- **CA31:** MEL016/MEL018 não são antecipadas.

## Matriz de testes

Não é necessário adicionar referência do projeto Web aos testes unitários apenas para testar helpers de apresentação.

Priorizar integração Web e, quando útil, teste direto no projeto de integração.

### Entrada / persistência

- **I1:** preço do Insumo com vírgula `20,99` persiste `20.99m`;
- **I2:** preço do Insumo com ponto `20.99` persiste `20.99m`;
- **I3:** quantidade com vírgula `200,5` persiste `200.5m`;
- **I4:** quantidade com ponto `200.5` persiste `200.5m`;
- **I5:** preço de prateleira com vírgula persiste corretamente;
- **I6:** preço de prateleira com ponto persiste corretamente;
- **I7:** input com ponto+vírgula/repetição/símbolo retorna erro e não persiste;
- **I8:** POST inválido preserva texto original;
- **I9:** zero e negativo continuam rejeitados;
- **I10:** configuração continua aceitando vírgula decimal.

### Regressão econômica

- **E1:** criar preço via Web com `200` e `20,99`;
- **E2:** confirmar no banco `PrecoCompra = 20.99m` e `CustoUnitario = 0.10495m`;
- **E3:** usar 50 g em Ficha e confirmar resultado numérico UC018 `5.2475m`;
- **E4:** confirmar que nenhuma calculadora passou a arredondar esse valor.

### Apresentação — Insumo

- **A1:** Detalhes mostra `R$ 20,99`;
- **A2:** Detalhes mostra custo unitário técnico com precisão útil;
- **A3:** Histórico usa mesma convenção;
- **A4:** quantidade continua não monetária.

### Apresentação — Produto/Ficha/Precificação

- **A5:** Custo do Item/perda aparecem com 2 casas;
- **A6:** mão de obra/energia aparecem com 2 casas;
- **A7:** Custo total do lote aparece com 2 casas;
- **A8:** Custo unitário do Produto aparece com 2 casas;
- **A9:** Preço teórico aparece com 2 casas;
- **A10:** Preço sugerido aparece com 2 casas;
- **A11:** Detalhes do Produto usa moeda para custo/preço atual;
- **A12:** registro de preço de prateleira usa moeda no resumo;
- **A13:** histórico comercial usa moeda nos snapshots monetários;
- **A14:** detalhamento UC025 usa moeda nos montantes e mantém custo unitário do Insumo técnico;
- **A15:** zero monetário conhecido aparece como `R$ 0,00`;
- **A16:** valores ausentes mantêm os textos atuais de indisponibilidade.

### Configuração

- **C1:** consulta mostra montantes com 2 casas;
- **C2:** GET de edição continua mostrando precisão integral persistida;
- **C3:** salvar sem alterar valor de alta precisão não o reduz para duas casas;
- **C4:** percentuais permanecem sem mudança semântica.

### Regressão geral

Atualizar testes existentes que hoje esperam valores como:

~~~text
18,6
11,625
12
~~~

para a nova apresentação:

~~~text
R$ 18,60
R$ 11,63
R$ 12,00
~~~

quando forem montantes monetários.

Não enfraquecer asserts apenas para “aceitar qualquer número”.

Evitar `Assert.DoesNotContain("99", html)` ou equivalentes sobre HTML inteiro; usar valores/trechos semanticamente delimitados para não reintroduzir testes flakey.

## Fora do escopo

- alterar fórmulas;
- alterar MargemAlvo ou percentuais;
- alterar IncrementoComercial como regra de negócio;
- alterar precisões de banco;
- corrigir automaticamente dados históricos;
- suportar moeda com símbolo no input;
- suportar separadores de milhar no input;
- internacionalização além de pt-BR;
- MEL016;
- MEL017;
- MEL018;
- MEL019;
- UC028+;
- refatoração global de todos os parsers numéricos sem necessidade.

## Definition of Done específica

MEL015 está concluída quando:

- os três campos diretos (`QuantidadeCompra`, `PrecoCompra`, `PrecoPrateleira`) têm parsing explícito e seguro;
- vírgula e ponto decimal simples funcionam;
- valores ambíguos não são reinterpretados;
- POST inválido preserva texto;
- preços/totais/custos monetários usam pt-BR com 2 casas;
- custo unitário técnico do Insumo preserva precisão útil;
- inputs de edição não perdem precisão persistida;
- cenário real `200 / 20,99 / 50 => 5.2475` passa;
- nenhuma calculadora Core é alterada;
- nenhuma migration é criada;
- nenhuma correção automática de histórico é feita;
- matriz I1–I10, E1–E4, A1–A16 e C1–C4 está coberta;
- build Release tem 0 erros e sem warnings novos relevantes;
- suíte completa está verde;
- backlog altera somente MEL015 de `Pronto` para `Concluído` na PR de implementação.

## Branch sugerida

~~~text
fix/mel015-valores-financeiros-ptbr
~~~

## Commit sugerido

~~~text
fix: padroniza entrada e exibicao monetaria ptbr
~~~
