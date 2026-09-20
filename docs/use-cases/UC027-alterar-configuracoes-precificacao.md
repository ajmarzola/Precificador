# UC027 — Alterar configurações de precificação da Empresa

> **Nota MEL022:** referências a `ValorHoraTrabalho` neste documento são históricas. O modelo ativo edita `PercentualMaoDeObra` obrigatório como percentual humano ("Mão de obra sobre os insumos (%)"), aceita zero e valores acima de 100%, e rejeita apenas valores negativos/vazios.

- **Funcionalidade:** F006 — Configurações de Precificação
- **Dependências funcionais:** UC026 e MEL009
- **Alteração de schema:** não prevista
- **Operação do UC:** edição tenant-aware da configuração 1:1 criada no UC026

## Objetivo

Permitir alterar os parâmetros de precificação da Empresa Ativa, preservando:

- isolamento multiempresa;
- diferença semântica entre `null` e zero;
- validações de domínio;
- precisão decimal;
- ausência de efeitos retroativos sobre Produtos e históricos;
- regra da MEL009 para Reserva comercial.

A UC027 também ativa o uso de `MargemPadrao` no cadastro de novos Produtos como pré-preenchimento de interface.

## Pré-condição estrutural

A implementação parte do modelo definido pela UC026:

~~~text
ConfiguracaoPrecificacaoEmpresa
- EmpresaId : int PK/FK
- PercentualMaoDeObra : decimal
- TarifaEnergiaKwh : decimal?
- MargemPadrao : decimal?
- IncrementoComercial : decimal?
- ReservaComercialDesconto : decimal
~~~

UC027 não deve mudar esse schema sem necessidade comprovada após revalidação contra a implementação real da UC026.

## Rota de edição

Manter a consulta read-only:

~~~text
/Configuracoes/Precificacao
~~~

e criar:

~~~text
/Configuracoes/Precificacao/Editar
~~~

Arquivos esperados:

~~~text
Pages/Configuracoes/Precificacao/Editar.cshtml
Pages/Configuracoes/Precificacao/Editar.cshtml.cs
~~~

Após a UC027, a página de consulta pode exibir botão:

~~~text
Alterar configurações
~~~

apontando para a rota de edição.

## Campos editáveis

A tela deve permitir alterar:

1. Mão de obra sobre os insumos (%);
2. Tarifa de energia (R$/kWh);
3. Margem padrão para novos produtos (%);
4. Incremento comercial de arredondamento;
5. Reserva comercial para desconto (%).

Não editar `EmpresaId`.

## Semântica de vazio x zero

Os campos:

- TarifaEnergiaKwh;
- MargemPadrao;
- IncrementoComercial

são opcionais.

`PercentualMaoDeObra` é obrigatório. Input vazio/whitespace para esse campo é inválido.

Input vazio/whitespace significa:

~~~text
null = Não configurado
~~~

Isso permite remover uma configuração anteriormente informada.

Zero é diferente de vazio.

Zero é válido para:

- PercentualMaoDeObra;
- TarifaEnergiaKwh;
- MargemPadrao.

Zero é inválido para:

- IncrementoComercial.

`PercentualMaoDeObra` e `ReservaComercialDesconto` são obrigatórios e não podem ser apagados para `null`.

## Validações

### PercentualMaoDeObra

Valor informado em percentual humano. Exemplos:

~~~text
10  => 0,10
150 => 1,50
250 => 2,50
~~~

Validação:

~~~text
PercentualMaoDeObra >= 0
~~~

Mensagem de domínio recomendada:

~~~text
O percentual de mão de obra não pode ser negativo.
~~~

Input não numérico:

~~~text
O percentual de mão de obra deve ser um número válido.
~~~

### TarifaEnergiaKwh

Quando informada:

~~~text
TarifaEnergiaKwh >= 0
~~~

Mensagem de domínio recomendada:

~~~text
A tarifa de energia não pode ser negativa.
~~~

Input não numérico:

~~~text
A tarifa de energia deve ser um número válido.
~~~

### MargemPadrao

A interface recebe percentual.

Quando informado:

~~~text
0 <= percentual < 100
~~~

Converter antes de atualizar domínio:

~~~text
MargemPadrao = percentual / 100
~~~

Mensagem:

~~~text
A margem padrão deve ser maior ou igual a 0% e menor que 100%.
~~~

### IncrementoComercial

Quando informado:

~~~text
IncrementoComercial > 0
~~~

Mensagem:

~~~text
O incremento comercial deve ser maior que zero.
~~~

Input não numérico:

~~~text
O incremento comercial deve ser um número válido.
~~~

### ReservaComercialDesconto

A interface recebe percentual e o campo é obrigatório.

Validar:

~~~text
0 <= percentual < 100
~~~

Converter:

~~~text
ReservaComercialDesconto = percentual / 100
~~~

Mensagens:

~~~text
A reserva comercial para desconto é obrigatória.
A reserva comercial para desconto deve ser maior ou igual a 0% e menor que 100%.
~~~

Zero é válido.

## Parsing e cultura

Usar parsing decimal explícito e determinístico, seguindo o padrão já implementado em `ProdutoFormulario`:

- se o texto contiver vírgula, interpretar com cultura `pt-BR`;
- caso contrário, interpretar com cultura invariável;
- usar `decimal.TryParse` com `NumberStyles.Number`.

A interface deve aceitar, no mínimo:

~~~text
0
10
10,5
123,45
~~~

Não depender implicitamente de `CurrentCulture` do processo.

Recomenda-se criar um helper compartilhado `ConfiguracaoPrecificacaoFormulario` para parsing, conversão percentual/fração e mapeamento de erros da edição.

Evitar `[DataType]`/model binding decimal direto como única validação para campos em que vírgula pt-BR é requisito funcional.

## Atualização de domínio

Adicionar método atômico equivalente a:

~~~text
Atualizar(
    decimal? valorHoraTrabalho,
    decimal? tarifaEnergiaKwh,
    decimal? margemPadrao,
    decimal? incrementoComercial,
    decimal reservaComercialDesconto)
~~~

O método deve:

1. validar todos os valores propostos;
2. somente depois das validações, mutar o estado;
3. preservar EmpresaId;
4. não permitir atualização parcial se qualquer campo for inválido.

Em falha, todos os valores anteriores devem permanecer intactos.

## GET de edição

GET `/Configuracoes/Precificacao/Editar`:

- carrega somente a configuração da Empresa Ativa pelo GQF;
- não recebe EmpresaId;
- não usa `IgnoreQueryFilters`;
- popula os inputs com valores atuais;
- converte frações percentuais para percentual de interface;
- deixa em branco os quatro campos atualmente `null`;
- exibe ReservaComercialDesconto atual;
- não muta estado.

Se a configuração esperada da Empresa Ativa não existir, retornar erro explícito coerente com violação de integridade; não criar silenciosamente configuração no GET.

Preferência: `404` ou falha controlada já usada no projeto, desde que não mascare o problema com criação automática.

## POST de edição

O POST deve:

1. obter a configuração pelo tenant ativo, nunca pelo request;
2. parsear os cinco campos;
3. converter os dois percentuais para fração;
4. validar input;
5. chamar atualização atômica de domínio;
6. `SaveChangesAsync()` uma única vez;
7. PRG para `/Configuracoes/Precificacao`, salvo quando houver `returnUrl` local válido conforme MEL023;
8. exibir mensagem de sucesso.

Mensagem:

~~~text
Configurações de precificação atualizadas com sucesso.
~~~

### Retorno local opcional — MEL023

A edição pode receber `returnUrl` opcional para voltar à origem após salvar, por exemplo quando a Ficha Técnica direciona o usuário para configurar `TarifaEnergiaKwh`.

O retorno só pode ser preservado e seguido quando for URL local validada por `Url.IsLocalUrl` ou mecanismo equivalente. URLs absolutas externas, protocol-relative externas ou qualquer host recebido do cliente devem ser ignorados e cair no destino padrão `/Configuracoes/Precificacao`.

GET preserva apenas `returnUrl` local e não persiste nada. POST válido com `returnUrl` local salva a configuração e retorna à origem; POST inválido preserva inputs e a origem local sem alterar banco. O comando Cancelar pode voltar à origem local quando ela existir.

## Request manipulado

Não criar `EmpresaId` editável no InputModel.

Campos extras como:

~~~text
EmpresaId
Input.EmpresaId
Id
Input.Id
~~~

devem ser ignorados.

O tenant alvo é exclusivamente a Empresa Ativa.

## Segurança multiempresa

Preservar FT002/RN039.

Com Empresa A ativa:

- GET mostra somente configuração A;
- POST altera somente configuração A;
- configuração B permanece inalterada;
- request não pode selecionar B.

Não usar `IgnoreQueryFilters` no fluxo Web.

O guard central deve permanecer como segunda linha de defesa.

## Antiforgery

POST usa antiforgery padrão de Razor Pages.

POST sem token:

~~~text
400 Bad Request
~~~

e não altera configuração.

## PRG e erros

### POST válido

~~~text
POST
 -> SaveChanges
 -> Redirect /Configuracoes/Precificacao
 -> mensagem de sucesso
~~~

### POST inválido

- retorna `200` na própria página de edição;
- exibe erros;
- preserva exatamente os valores digitados pelo usuário;
- não substitui inputs inválidos pelos valores persistidos;
- não persiste alteração parcial.

## Efeitos sobre cálculos atuais

Configuração não possui snapshot próprio para cálculos correntes.

Alterações válidas afetam o próximo cálculo atual da mesma Empresa nos UCs dependentes:

- ValorHoraTrabalho -> UC020;
- TarifaEnergiaKwh -> UC021;
- IncrementoComercial -> UC023;
- ReservaComercialDesconto -> novas decisões comerciais futuras do UC011;
- MargemPadrao -> pré-preenchimento de novos Produtos.

UC027 não executa esses cálculos.

## Efeito sobre Produtos existentes

Alterar `MargemPadrao` nunca altera:

- Produto.MargemAlvo existente;
- Produtos ativos ou inativos já cadastrados;
- históricos comerciais.

Não executar UPDATE em massa.

Não sincronizar Produtos existentes com a nova margem.

## Pré-preenchimento de Produto Novo

Após UC027, `MargemPadrao` passa a ser consumida pelo GET:

~~~text
/Produtos/Novo
~~~

### Se MargemPadrao estiver configurada

Pré-preencher:

~~~text
Input.MargemAlvoPercentual = MargemPadrao × 100
~~~

O usuário continua livre para alterar o valor antes do POST.

O Produto persiste a margem efetivamente enviada/validada, não uma referência à configuração.

### Se MargemPadrao for null

Manter o comportamento atual:

- campo de Margem-alvo sem valor padrão;
- usuário deve informar uma margem válida para cadastrar.

### Se MargemPadrao = 0

Pré-preencher explicitamente:

~~~text
0
~~~

Não confundir zero configurado com `null`.

### Se a configuração 1:1 estiver ausente

A ausência de `ConfiguracaoPrecificacaoEmpresa` para uma Empresa válida é violação de integridade conforme UC026.

No GET de `/Produtos/Novo`:

- não tratar ausência da configuração como se `MargemPadrao = null`;
- não criar configuração silenciosamente;
- retornar `404`, mantendo o mesmo princípio já implementado em `/Configuracoes/Precificacao`.

### POST inválido de Produto Novo

Não reaplicar MargemPadrao por cima do valor digitado.

Se o POST de Produto for inválido, preservar o valor informado pelo usuário.

## Reserva comercial — MEL009

UC027 implementa o bloco de alteração previsto pela MEL009.

Regras:

- input percentual;
- persistência em fração;
- 0 <= valor < 1;
- zero válido;
- alteração somente da Empresa Ativa;
- alteração não modifica Produto existente;
- alteração não modifica histórico comercial existente;
- alteração não recalcula Preço sugerido;
- alteração será usada apenas por novos snapshots de UC011 quando esse UC existir.

Não criar:

- ReservaComercialReferencia;
- RegistroPrecoProduto;
- DescontoReferencia.

## Ausência de histórico de configuração

No MVP, alterar configuração atual sobrescreve a configuração vigente da Empresa.

Não criar:

- histórico append-only de configuração;
- data de vigência;
- auditoria própria;
- versionamento;
- snapshots da configuração fora dos registros comerciais futuros definidos pelo UC011.

## Persistência

Não criar migration se a UC026 real tiver implementado exatamente o schema especificado.

UC027 deve revalidar o modelo real antes da implementação.

Se alguma diferença material no schema da UC026 exigir migration, interromper e revalidar a especificação; não criar migration corretiva incidental sem decisão registrada.

## Precisão

Aplicar RN026.

Não arredondar valores persistidos para apresentação.

Percentuais são convertidos entre fração e percentual sem arredondamento intermediário adicional.

Usar `decimal`, nunca `double`.

## Formatação da consulta pós-save

Após atualização, `/Configuracoes/Precificacao` deve refletir os valores persistidos usando o helper de apresentação introduzido pela UC026.

`null` volta a aparecer como:

~~~text
Não configurado
~~~

Zero configurado deve aparecer como zero.

## Critérios de aceitação

### CA01

Usuário da Empresa Ativa consegue abrir a edição dos cinco parâmetros.

### CA02

Inputs refletem corretamente valores atuais, incluindo nulls em branco e percentuais convertidos.

### CA03

POST válido atualiza todos os campos atomicamente e faz PRG.

### CA04

Campos opcionais vazios são persistidos como null.

### CA05

Zero é aceito em ValorHoraTrabalho, TarifaEnergiaKwh, MargemPadrao e ReservaComercialDesconto.

### CA06

IncrementoComercial vazio vira null; zero ou negativo é rejeitado.

### CA07

MargemPadrao e Reserva aceitam somente [0%,100%).

### CA08

Reserva vazia é rejeitada.

### CA09

Input inválido não persiste alteração parcial.

### CA10

Request manipulado não altera outra Empresa.

### CA11

POST sem antiforgery não altera dados.

### CA12

Alterar MargemPadrao não modifica Produtos existentes.

### CA13

`/Produtos/Novo` pré-preenche MargemAlvo com MargemPadrao configurada.

### CA14

MargemPadrao null deixa Produto Novo sem pré-preenchimento.

### CA15

MargemPadrao = 0 pré-preenche 0, sem ser confundida com null.

### CA16

POST inválido de Produto Novo preserva a margem digitada, sem reaplicar default.

### CA17

Alterar Reserva comercial não altera Preço sugerido nem histórico existente.

### CA18

Consulta pós-save apresenta novos valores e `Não configurado` para campos limpos.

## Matriz de testes

### Unitários — domínio

- U1: Atualizar aceita combinação válida completa;
- U2: aceita null nos quatro campos opcionais;
- U3: aceita zero em hora, tarifa, margem e reserva;
- U4: rejeita hora negativa;
- U5: rejeita tarifa negativa;
- U6: rejeita margem <0 ou >=1;
- U7: rejeita incremento <=0 quando informado;
- U8: rejeita reserva <0 ou >=1;
- U9: falha em qualquer campo preserva integralmente estado anterior;
- U10: atualização não altera EmpresaId.

### Persistência

- P1: roundtrip persiste valores e nulls com precisão;
- P2: alteração da configuração A não altera B;
- P3: guard central rejeita alteração técnica cross-tenant;
- P4: nenhuma segunda configuração é criada durante atualização;
- P5: Produtos existentes mantêm MargemAlvo após mudança de MargemPadrao.

### Web — Configurações

- W1: edição exige autenticação e Empresa Ativa;
- W2: GET carrega valores atuais e nulls como inputs vazios;
- W3: GET converte MargemPadrao/Reserva para percentual;
- W4: POST válido atualiza e redireciona com mensagem;
- W5: campos opcionais vazios limpam para null;
- W6: zero válido é preservado e exibido como zero;
- W7: incremento zero/negativo é rejeitado;
- W8: margem inválida é rejeitada;
- W9: reserva vazia/negativa/100%+ é rejeitada;
- W10: vírgula decimal pt-BR é aceita;
- W11: POST inválido preserva inputs e banco inalterado;
- W12: request com EmpresaId/Id manipulados não muda alvo;
- W13: troca de Empresa altera configuração editada;
- W14: outro tenant não é alterado;
- W15: sem antiforgery retorna 400 sem mutação;
- W16: página de consulta oferece `Alterar configurações` após UC027.

### Web — Produto Novo

- W17: MargemPadrao configurada pré-preenche MargemAlvoPercentual;
- W18: MargemPadrao null não pré-preenche;
- W19: MargemPadrao zero pré-preenche 0;
- W20: usuário pode sobrescrever o valor pré-preenchido e Produto persiste o valor informado;
- W21: POST inválido preserva margem digitada em vez de reaplicar MargemPadrao;
- W22: Produto existente não é alterado ao mudar MargemPadrao.

## Alterações esperadas

### Core

- método atômico de atualização em `ConfiguracaoPrecificacaoEmpresa`.

### Web

- nova página `/Configuracoes/Precificacao/Editar`;
- botão `Alterar configurações` na consulta;
- parser/helper de formulário para os cinco campos;
- integração mínima de MargemPadrao com GET `/Produtos/Novo`.

### Infrastructure

- nenhuma mudança esperada de schema;
- reutilizar DbSet/configuração/GQF/guard da UC026.

### Testes

- ampliar testes de configuração da UC026;
- ampliar testes de Produto Novo para MargemPadrao.

## Fora do escopo

- histórico/versionamento de configuração;
- data de vigência;
- aprovação de alteração;
- permissões administrativas granulares;
- recalcular Produto existente;
- alterar MargemAlvo de Produto existente;
- cálculo de mão de obra;
- cálculo de energia;
- cálculo de Preço sugerido;
- RegistroPrecoProduto;
- ReservaComercialReferencia;
- DescontoReferencia;
- snapshot de configuração;
- API REST.

## Revalidação pós-UC026

Revalidação concluída contra a implementação real mergeada da UC026.

Foi confirmado:

1. `ConfiguracaoPrecificacaoEmpresa` existe como entidade 1:1 tenant-owned;
2. `EmpresaId` é PK/FK e não existe Id artificial;
3. os nomes reais são `ValorHoraTrabalho`, `TarifaEnergiaKwh`, `MargemPadrao`, `IncrementoComercial` e `ReservaComercialDesconto`;
4. os quatro primeiros campos são `decimal?`;
5. `ReservaComercialDesconto` é `decimal` obrigatório com padrão `0,10`;
6. precisões EF são 18,6 para hora/tarifa/incremento e 9,6 para margem/reserva;
7. `CriarPadrao(empresaId)` existe e preserva os quatro opcionais em `null`;
8. `DefinirEmpresa` impede reatribuição de tenant;
9. GQF e guard central cobrem a configuração;
10. a consulta real é `/Configuracoes/Precificacao`;
11. configuração ausente nessa consulta retorna `404` e não é criada no GET;
12. `ConfiguracaoPrecificacaoFormatacao` já centraliza formatação pt-BR e distinção `null`/zero;
13. `WebTestContext.CriarEmpresaAsync` já cria configuração padrão para novas Empresas de teste;
14. `/Produtos/Novo` ainda não consome `MargemPadrao`, exatamente como esperado antes da UC027;
15. nenhuma migration adicional é necessária para UC027.

Não houve divergência material de schema ou arquitetura.

A UC027 está liberada para implementação.

## Branch sugerida

~~~text
feat/uc027-alterar-configuracoes-precificacao
~~~

## Commit sugerido futuro

~~~text
feat: altera configuracoes de precificacao
~~~
