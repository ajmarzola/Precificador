# MEL009 — Parametrizar reserva comercial do Desconto de referência

- **Tipo:** melhoria de regra/configuração comercial
- **Origem:** definição do modelo comercial do UC011
- **Prioridade:** baixa
- **Alteração de código de produção imediata:** não
- **Alteração de schema futura:** sim, incorporada desde a implementação inicial de UC026/027 e UC011
- **Implementação autônoma:** não

## Objetivo

Permitir que cada Empresa configure a reserva comercial usada no cálculo do Desconto de referência, substituindo o valor fixo de 10 pontos percentuais sem reinterpretar registros históricos.

A parametrização deve nascer integrada às futuras configurações de precificação e ao snapshot comercial do Produto.

## Decisão de arquitetura funcional

A MEL009 não terá uma implementação isolada posterior.

Ela será absorvida por três entregas futuras:

1. UC026 — consultar configurações de precificação da Empresa;
2. UC027 — alterar configurações de precificação da Empresa;
3. UC011 — registrar Preço de prateleira preservando snapshot comercial.

Motivo: essas entidades ainda não existem. Introduzir a reserva desde o primeiro schema evita migration corretiva e período transitório com regra hardcoded.

UC012 consumirá o snapshot criado pelo UC011.

## Configuração por Empresa

A configuração de precificação da Empresa deve incluir:

~~~text
ReservaComercialDesconto : decimal
~~~

Representação interna em fração decimal:

~~~text
10 p.p. = 0,10
7,5 p.p. = 0,075
0 p.p. = 0
~~~

Valor padrão:

~~~text
0,10
~~~

Esse padrão preserva a regra já definida para o MVP.

## Validação da reserva

Aplicar:

~~~text
0 <= ReservaComercialDesconto < 1
~~~

A interface deve receber percentual e converter para fração antes de atualizar o domínio.

Exemplos válidos:

~~~text
0%
5%
10%
12,5%
99,5%
~~~

100% ou valor negativo são inválidos.

## Tela de configurações

Não criar tela própria para essa melhoria.

A reserva pertence ao mesmo conjunto de configurações de precificação da Empresa que:

- valor/hora de trabalho;
- tarifa de energia;
- margem padrão;
- incremento comercial de arredondamento.

UC026 deve exibir a configuração.

UC027 deve permitir alterá-la.

Rótulo recomendado:

~~~text
Reserva comercial para desconto (%)
~~~

Ajuda recomendada:

~~~text
Percentual reservado acima do preço sugerido antes de formar o desconto de referência.
~~~

## Fórmula do Desconto de referência

Definir:

~~~text
PercentualAcimaSugerido = (PrecoPrateleira / PrecoSugerido) - 1
Reserva = ReservaComercialReferencia
LimiarAplicacao = Reserva + 0,01
~~~

O acréscimo fixo de 0,01 representa 1 ponto percentual e preserva a regra atual de 10 p.p. de reserva com aplicação somente a partir de 11% acima do Preço sugerido.

### Regra

Se:

~~~text
PrecoPrateleira < PrecoSugerido
~~~

então:

~~~text
DescontoReferencia = não aplicável
~~~

Se:

~~~text
PercentualAcimaSugerido < LimiarAplicacao
~~~

então:

~~~text
DescontoReferencia = não aplicável
~~~

Caso contrário:

~~~text
DescontoReferencia = PercentualAcimaSugerido - Reserva
~~~

## Exemplos

### Reserva de 10 p.p.

~~~text
Reserva = 0,10
Limiar = 0,11
Prateleira 10,5% acima -> não aplicável
Prateleira 11% acima -> desconto de referência = 1%
Prateleira 20% acima -> desconto de referência = 10%
~~~

### Reserva de 5 p.p.

~~~text
Reserva = 0,05
Limiar = 0,06
Prateleira 5,9% acima -> não aplicável
Prateleira 6% acima -> desconto de referência = 1%
Prateleira 15% acima -> desconto de referência = 10%
~~~

### Reserva de 0 p.p.

~~~text
Reserva = 0
Limiar = 0,01
Prateleira 0,5% acima -> não aplicável
Prateleira 1% acima -> desconto de referência = 1%
~~~

## Precisão

Aplicar RN026.

Não arredondar PercentualAcimaSugerido, Reserva, Limiar ou DescontoReferencia durante o cálculo.

Arredondamento é apenas de apresentação.

## Snapshot histórico

A configuração vigente da Empresa não pode reinterpretar registros comerciais antigos.

Portanto, RegistroPrecoProduto deve persistir:

~~~text
ReservaComercialReferencia : decimal
~~~

junto com:

- DataReferencia;
- CustoReferencia;
- MargemReferencia;
- PrecoSugerido;
- PrecoPrateleira.

Esse campo é preenchido automaticamente pelo sistema com a reserva vigente da Empresa no momento do registro.

O usuário não informa ReservaComercialReferencia no UC011.

## DescontoReferencia continua derivado

Não persistir DescontoReferencia.

Para um registro histórico:

~~~text
DescontoReferencia = f(
    PrecoSugerido,
    PrecoPrateleira,
    ReservaComercialReferencia
)
~~~

Assim o histórico continua reproduzível sem armazenar dado redundante.

## Alteração futura da configuração

Quando a Empresa altera ReservaComercialDesconto:

- novos registros comerciais usam o novo valor;
- registros anteriores mantêm ReservaComercialReferencia original;
- DescontoReferencia histórico não muda;
- PrecoSugerido histórico não muda;
- CustoReferencia e MargemReferencia históricos não mudam.

## Relação com UC026

Quando UC026 for especificado/implementado, a consulta de configurações deve incluir ReservaComercialDesconto.

Se as configurações ainda não existirem para uma Empresa, o bootstrap/default deve resultar em 10 p.p.

## Relação com UC027

UC027 deve:

- aceitar Reserva comercial em percentual;
- converter para fração;
- validar 0 <= reserva < 1;
- persistir por Empresa;
- não alterar Produtos existentes;
- não alterar registros comerciais existentes.

## Relação com UC011

A especificação definitiva do UC011 deve incluir ReservaComercialReferencia no modelo inicial de RegistroPrecoProduto.

O snapshot mínimo passa a ser:

~~~text
RegistroPrecoProduto
- Id
- EmpresaId
- ProdutoId
- DataReferencia
- CustoReferencia
- MargemReferencia
- PrecoSugerido
- PrecoPrateleira
- ReservaComercialReferencia
~~~

Não criar primeiro uma tabela sem esse campo para adicioná-lo depois.

## Relação com UC012

UC012 deve calcular qualquer Desconto de referência histórico usando ReservaComercialReferencia do próprio registro.

Nunca usar a configuração atual da Empresa para reinterpretar registros anteriores.

A exibição explícita da reserva usada pode ser incluída em detalhe histórico, mas não é obrigatória como coluna principal da listagem.

## Relação com UC023

ReservaComercialDesconto não participa do cálculo de PrecoTeorico ou PrecoSugerido.

Ela só participa da interpretação comercial entre PrecoSugerido e PrecoPrateleira.

Portanto, alterar a reserva não recalcula PrecoSugerido.

## Multiempresa

A configuração é tenant-owned.

Empresa A não pode ler ou alterar a reserva da Empresa B.

RegistroPrecoProduto deve carregar ReservaComercialReferencia da mesma Empresa do Produto e do registro.

Request nunca informa EmpresaId da configuração nem do snapshot.

## Regras normativas propostas

### RN052 — Reserva comercial de desconto por Empresa

Cada Empresa possui ReservaComercialDesconto em fração decimal, com 0 <= valor < 1 e padrão de 0,10.

### RN053 — Limiar do Desconto de referência

O Desconto de referência só se aplica quando PercentualAcimaSugerido >= ReservaComercialReferencia + 0,01.

### RN054 — Snapshot da reserva comercial

Cada RegistroPrecoProduto congela ReservaComercialReferencia vigente no momento da decisão comercial.

Alterações posteriores da configuração não reinterpretam registros antigos.

## Critérios de aceitação da parametrização futura

### CA01

Empresa possui reserva configurável com padrão de 10 p.p.

### CA02

Empresas diferentes podem possuir reservas diferentes.

### CA03

Reserva negativa ou >=100% é rejeitada.

### CA04

UC026 exibe a reserva vigente.

### CA05

UC027 altera apenas a Empresa Ativa.

### CA06

Alterar reserva não muda PrecoSugerido.

### CA07

Novo RegistroPrecoProduto congela ReservaComercialReferencia.

### CA08

Usuário não controla o valor snapshot no request do UC011.

### CA09

DescontoReferencia não é persistido.

### CA10

Com reserva 10 p.p., comportamento permanece: abaixo de 11% acima do sugerido = não aplicável; em 11% = 1%.

### CA11

Com reserva 5 p.p., limiar passa a 6%.

### CA12

Alterar configuração depois de registros existentes não altera DescontoReferencia histórico.

### CA13

UC012 usa ReservaComercialReferencia do registro, nunca a configuração atual.

### CA14

Preço de prateleira abaixo do sugerido continua sem Desconto de referência.

## Matriz de testes a incorporar nos UCs

### Configuração — UC026/027

- C1: default 0,10;
- C2: leitura tenant-aware;
- C3: alteração válida;
- C4: 0 é válido;
- C5: valor negativo inválido;
- C6: 1 ou maior inválido;
- C7: Empresa A não altera Empresa B;
- C8: alteração não modifica Produto ou histórico comercial.

### Snapshot — UC011

- S1: novo registro copia reserva vigente;
- S2: request manipulado não controla snapshot;
- S3: Produto inativo também congela reserva sem reativação;
- S4: registros antes/depois de mudança de configuração guardam valores diferentes;
- S5: mesma DataReferencia continua admitindo múltiplos registros com snapshots próprios.

### Derivação — UC011/UC012

- D1: reserva 10%, +10,99% => não aplicável;
- D2: reserva 10%, +11% => 1%;
- D3: reserva 10%, +20% => 10%;
- D4: reserva 5%, +5,99% => não aplicável;
- D5: reserva 5%, +6% => 1%;
- D6: prateleira abaixo do sugerido => não aplicável;
- D7: histórico usa snapshot antigo após mudança de configuração;
- D8: nenhum arredondamento intermediário altera o limiar.

## Estratégia de implementação

Não criar instrução Codex autônoma para MEL009.

A implementação será distribuída nos UCs que criam os modelos:

~~~text
UC026/UC027 -> ConfiguracaoPrecificacaoEmpresa.ReservaComercialDesconto
UC011       -> RegistroPrecoProduto.ReservaComercialReferencia
UC012       -> derivação histórica pelo snapshot
~~~

Antes de cada um desses UCs ser liberado, sua especificação deve ser revalidada contra esta MEL.

A MEL009 só será considerada integralmente concluída quando os três blocos estiverem implementados e validados.

## Fora do escopo

- cupom;
- promoção;
- preço promocional temporário;
- desconto máximo obrigatório;
- aprovação gerencial;
- regra por Produto;
- regra por Categoria;
- múltiplas reservas simultâneas;
- versionamento separado de política comercial;
- persistir DescontoReferencia;
- parametrizar o 1 p.p. mínimo nesta melhoria;
- alterar fórmula de PrecoSugerido.

## Definition of Done da especificação

A MEL009 está suficientemente especificada quando:

- campo de configuração está definido;
- default e validação estão definidos;
- fórmula parametrizada está fechada;
- limiar derivado reserva + 1 p.p. está fechado;
- snapshot ReservaComercialReferencia está definido;
- responsabilidade de UC026/027/011/012 está separada;
- ausência de implementação autônoma está explícita;
- matriz de testes futura está definida;
- pricing-model e regras de negócio estão coerentes.

