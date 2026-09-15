# Instrução Codex — MEL009: Reserva comercial do Desconto de referência

## Natureza desta instrução

Esta instrução é **transversal** e **não representa uma implementação autônoma da MEL009**.

Não criar branch `feat/mel009-*` para implementar a MEL009 isoladamente.

A MEL009 é incorporada pelos UCs responsáveis pelos modelos reais:

~~~text
UC026/UC027 -> ConfiguracaoPrecificacaoEmpresa.ReservaComercialDesconto
UC011       -> RegistroPrecoProduto.ReservaComercialReferencia
UC012       -> derivação histórica do DescontoReferencia pelo snapshot
~~~

Esta instrução deve ser lida junto com a instrução Codex do UC em execução e funciona como checklist obrigatório para impedir drift entre configuração, snapshot e cálculo histórico.

## Leitura obrigatória

Antes de implementar qualquer UC dependente da MEL009, ler:

- `AGENTS.md`;
- `docs/development/backlog.md`;
- `docs/development/improvements/MEL009-reserva-comercial-desconto.md`;
- `docs/business/business-rules.md`, especialmente RN024, RN025, RN026, RN052, RN053 e RN054;
- `docs/business/pricing-model.md`;
- a especificação e a instrução Codex do UC em execução;
- implementação real já mergeada dos UCs predecessores.

## Regra de execução

Esta instrução **nunca substitui** a instrução específica do UC.

Executar somente dentro da branch do UC responsável.

Exemplos:

~~~text
feat/uc026-consultar-configuracoes-precificacao
feat/uc027-alterar-configuracoes-precificacao
feat/uc011-registrar-preco-prateleira
feat/uc012-historico-precificacao-produto
~~~

Não criar commits, migrations ou código cuja única justificativa seja 'implementar MEL009' fora de um desses UCs.

## Conceito normativo

A Empresa possui:

~~~text
ReservaComercialDesconto : decimal
~~~

Representação interna em fração decimal:

~~~text
10%   -> 0.10
7,5%  -> 0.075
0%    -> 0
~~~

Default:

~~~text
0.10
~~~

Validação:

~~~text
0 <= ReservaComercialDesconto < 1
~~~

Na interface, usar percentual. No domínio/persistência, usar fração.

## Regra de desconto de referência

Para um registro comercial:

~~~text
PercentualAcimaSugerido = (PrecoPrateleira / PrecoSugerido) - 1
Reserva = ReservaComercialReferencia
LimiarAplicacao = Reserva + 0.01
~~~

O `0.01` representa 1 ponto percentual e **não é configurável** nesta MEL.

Regras:

~~~text
se PrecoPrateleira < PrecoSugerido:
    DescontoReferencia = N/A

senão se PercentualAcimaSugerido < ReservaComercialReferencia + 0.01:
    DescontoReferencia = N/A

senão:
    DescontoReferencia = PercentualAcimaSugerido - ReservaComercialReferencia
~~~

Não realizar arredondamento intermediário, conforme RN026.

## UC026 — Configuração inicial

Quando o UC026 for implementado, garantir:

- `ConfiguracaoPrecificacaoEmpresa` contém `ReservaComercialDesconto` desde a primeira migration;
- valor padrão `0.10`;
- campo obrigatório e decimal;
- configuração tenant-owned;
- backfill das Empresas existentes com `0.10`;
- consulta exibe `10%` para o default;
- ajuda funcional:

~~~text
Percentual reservado acima do preço sugerido antes de formar o desconto de referência.
~~~

Não criar:

- `ReservaComercialReferencia`;
- `RegistroPrecoProduto`;
- `DescontoReferencia`;
- tela própria exclusiva da reserva.

Testes MEL009 a cobrir dentro de UC026:

- C1: default 0,10;
- C2: leitura tenant-aware.

## UC027 — Alteração da configuração

Quando o UC027 for implementado, garantir:

- input percentual;
- conversão percentual -> fração;
- `0` é válido;
- negativo é inválido;
- `100%` ou maior é inválido;
- campo obrigatório;
- alteração somente da Empresa Ativa;
- request não controla `EmpresaId`;
- alteração não modifica Produtos existentes;
- alteração não modifica registros comerciais existentes;
- alteração não recalcula Preço sugerido;
- ausência de arredondamento intermediário.

Testes MEL009 a cobrir dentro de UC027:

- C3: alteração válida;
- C4: 0 é válido;
- C5: negativo inválido;
- C6: 1 ou maior inválido;
- C7: Empresa A não altera Empresa B;
- C8: alteração não modifica Produto ou histórico comercial.

## UC011 — Snapshot comercial

Quando o UC011 for especificado/implementado, o modelo inicial deve conter:

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

`ReservaComercialReferencia`:

- é obrigatória;
- é preenchida automaticamente pelo sistema;
- copia `ReservaComercialDesconto` vigente da mesma Empresa no momento do registro;
- não é informada nem controlada pelo usuário;
- não deve ser obtida de outra Empresa;
- não muda depois que o registro é criado.

Não criar primeiro `RegistroPrecoProduto` sem o campo para adicioná-lo depois.

`DescontoReferencia` continua derivado e não deve ser persistido.

Testes MEL009 a cobrir dentro de UC011:

- S1: novo registro copia reserva vigente;
- S2: request manipulado não controla snapshot;
- S3: Produto inativo também congela reserva sem reativação;
- S4: registros antes/depois de alteração guardam reservas diferentes;
- S5: múltiplos registros na mesma DataReferencia mantêm snapshots próprios.

## UC012 — Histórico

Quando o UC012 for implementado:

- nunca usar `ConfiguracaoPrecificacaoEmpresa.ReservaComercialDesconto` atual para reinterpretar registro histórico;
- derivar `DescontoReferencia` usando `ReservaComercialReferencia` do próprio registro;
- mudança posterior de configuração não altera resultado histórico;
- `DescontoReferencia` permanece não persistido;
- não realizar arredondamento intermediário.

Testes MEL009 a cobrir dentro de UC012/derivação:

- D1: reserva 10%, +10,99% => N/A;
- D2: reserva 10%, +11% => 1%;
- D3: reserva 10%, +20% => 10%;
- D4: reserva 5%, +5,99% => N/A;
- D5: reserva 5%, +6% => 1%;
- D6: prateleira abaixo do sugerido => N/A;
- D7: histórico usa snapshot antigo após mudança;
- D8: sem arredondamento intermediário no limiar.

## Relação com UC023

`ReservaComercialDesconto` **não participa** de:

- `PrecoTeorico`;
- `PrecoSugerido`;
- arredondamento do Preço sugerido.

Alterar a reserva não deve provocar recálculo de Preço sugerido.

Se implementação de UC023 consultar a reserva, isso é sinal de acoplamento incorreto e deve ser corrigido.

## Multiempresa

Aplicar em todos os UCs:

- GQF normal;
- sem `IgnoreQueryFilters` em fluxo comum;
- request nunca define EmpresaId da configuração/snapshot;
- Empresa A não lê nem altera reserva da B;
- snapshot comercial deve vir da mesma Empresa do Produto/registro.

## Precisão

Usar `decimal`, nunca `double`.

Não arredondar:

- Reserva;
- PercentualAcimaSugerido;
- LimiarAplicacao;
- DescontoReferencia

durante o cálculo.

Formatação percentual é somente apresentação.

## Proibições

Não implementar pela MEL009 isoladamente:

- nova entidade fora de UC026/027;
- migration exclusiva posterior da reserva se UC026 ainda puder incorporá-la no schema inicial;
- histórico de configuração;
- versionamento de política comercial;
- cupom;
- promoção;
- preço promocional;
- desconto máximo obrigatório;
- aprovação gerencial;
- reserva por Produto/Categoria;
- múltiplas reservas simultâneas;
- persistência de `DescontoReferencia`;
- parametrização do 1 p.p. mínimo;
- alteração da fórmula de Preço sugerido.

## Checkpoint obrigatório por UC

Antes de concluir cada UC dependente, responder no PR:

### UC026

- Reserva está no schema inicial?
- default é 0,10?
- leitura é tenant-aware?

### UC027

- edição aceita 0 e rejeita <0 ou >=1?
- alteração é somente da Empresa Ativa?
- nenhum Produto/histórico foi alterado?

### UC011

- snapshot é automático e não controlado pelo request?
- `ReservaComercialReferencia` está no modelo inicial?
- `DescontoReferencia` não foi persistido?

### UC012

- histórico usa snapshot próprio?
- mudança da configuração atual não muda o passado?
- fórmula e limiar respeitam RN053 sem arredondamento intermediário?

## Estado da MEL009

Não marcar MEL009 como `Concluído` após apenas UC026 ou UC027.

A MEL009 só pode ser considerada integralmente concluída quando os blocos distribuídos estiverem implementados e validados:

~~~text
UC026/UC027 -> configuração
UC011       -> snapshot
UC012       -> derivação histórica
~~~

A atualização de estado deve seguir `docs/development/backlog.md` e ocorrer apenas quando a cobertura distribuída estiver completa.

## Validação padrão

Em cada UC executado, além dos testes específicos:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

## Retorno obrigatório

No retorno de qualquer UC que incorpore parte da MEL009, informar explicitamente:

1. qual bloco da MEL009 foi incorporado;
2. arquivos/modelos afetados;
3. testes C/S/D correspondentes cobertos;
4. confirmação de que não houve implementação fora do UC responsável;
5. confirmação de que MEL009 permanece não concluída enquanto houver blocos futuros pendentes.
