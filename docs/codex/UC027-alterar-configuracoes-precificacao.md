# Instrução Codex — UC027: Alterar configurações de precificação da Empresa

## Tarefa

Implementar integralmente a UC027 conforme `docs/use-cases/UC027-alterar-configuracoes-precificacao.md`.

Branch obrigatória:

~~~text
feat/uc027-alterar-configuracoes-precificacao
~~~

Não editar/commitar/push direto em `master`. Não fazer merge da própria implementação.

## Precondições

Antes de alterar arquivos:

1. atualizar `master`;
2. criar/trocar para a branch obrigatória;
3. confirmar branch != master;
4. ler `AGENTS.md`;
5. ler `docs/development/backlog.md` e confirmar UC027 = `Pronto`;
6. ler UC027, UC026, F006, MEL009 e RN025/RN039/RN045/RN052;
7. inspecionar a implementação real de UC026 e os testes existentes.

Se UC027 não estiver `Pronto`, não implementar.

## Base real confirmada

Partir do modelo existente:

~~~text
ConfiguracaoPrecificacaoEmpresa
- EmpresaId : int PK/FK
- ValorHoraTrabalho : decimal?
- TarifaEnergiaKwh : decimal?
- MargemPadrao : decimal?
- IncrementoComercial : decimal?
- ReservaComercialDesconto : decimal
~~~

Não criar migration e não alterar schema, salvo blocker material não previsto — nesse caso, interromper e reportar.

## Domínio

Adicionar método atômico em `ConfiguracaoPrecificacaoEmpresa` equivalente a:

~~~text
Atualizar(
    decimal? valorHoraTrabalho,
    decimal? tarifaEnergiaKwh,
    decimal? margemPadrao,
    decimal? incrementoComercial,
    decimal reservaComercialDesconto)
~~~

Validar todos os valores antes de qualquer mutação.

Regras:

- hora null ou >= 0;
- tarifa null ou >= 0;
- margem null ou 0 <= valor < 1;
- incremento null ou > 0;
- reserva obrigatória com 0 <= valor < 1;
- EmpresaId imutável;
- falha preserva estado anterior integralmente.

## Formulário de edição

Criar:

~~~text
/Configuracoes/Precificacao/Editar
~~~

Usar InputModel textual para os cinco campos.

Campos opcionais vazios -> null.

Reserva vazia -> erro obrigatório.

Percentuais de MargemPadrao e Reserva entram em percentual e são convertidos para fração.

## Parsing

Seguir o padrão real de `ProdutoFormulario`:

- texto com vírgula -> `pt-BR`;
- caso contrário -> `InvariantCulture`;
- `decimal.TryParse` + `NumberStyles.Number`.

Recomenda-se `ConfiguracaoPrecificacaoFormulario` para centralizar parsing, conversão e erros.

Não depender apenas do model binder decimal.

## GET

GET da edição:

- usa GQF normal;
- não recebe EmpresaId;
- não usa IgnoreQueryFilters;
- carrega somente Empresa Ativa;
- nulls -> inputs vazios;
- margem/reserva -> percentual textual;
- configuração ausente -> 404;
- não cria configuração;
- não chama SaveChanges.

## POST

POST:

1. resolve configuração pela Empresa Ativa;
2. ignora EmpresaId/Id manipulados no request;
3. parseia os cinco campos;
4. converte percentuais para fração;
5. chama `Atualizar`;
6. SaveChangesAsync uma única vez;
7. PRG para `/Configuracoes/Precificacao`;
8. TempData: `Configurações de precificação atualizadas com sucesso.`

POST inválido:

- retorna 200;
- preserva exatamente os valores digitados;
- não recarrega por cima com valores persistidos;
- não persiste alteração parcial.

POST sem antiforgery -> 400 sem mutação.

## Consulta UC026

Adicionar botão/link:

~~~text
Alterar configurações
~~~

em `/Configuracoes/Precificacao` apontando para a edição.

Reutilizar `ConfiguracaoPrecificacaoFormatacao` para a consulta pós-save.

## Produto Novo — MargemPadrao

Alterar somente o GET de `/Produtos/Novo` para consumir a configuração atual.

Regras:

- MargemPadrao != null -> pré-preencher `Input.MargemAlvoPercentual` com valor * 100;
- MargemPadrao == 0 -> preencher `0`, não vazio;
- MargemPadrao == null -> deixar campo sem valor padrão;
- configuração 1:1 ausente -> 404, não tratar como null e não criar silenciosamente;
- usuário pode sobrescrever o valor;
- POST persiste a margem efetivamente informada;
- POST inválido preserva o valor digitado e não reaplica default.

Não alterar Produtos existentes.

## MEL009

Implementar apenas o bloco de alteração de `ReservaComercialDesconto`:

- UI percentual;
- persistência em fração;
- 0 é válido;
- negativo ou >= 100% inválido;
- alteração afeta somente Empresa Ativa;
- não recalcula Preço sugerido;
- não cria RegistroPrecoProduto;
- não cria ReservaComercialReferencia;
- não cria DescontoReferencia;
- não altera histórico comercial.

Não marcar MEL009 como Concluído após UC027.

## Segurança

- `/Configuracoes` já está protegido pela policy `EmpresaAtiva`; preservar;
- sem IgnoreQueryFilters no fluxo Web;
- request não controla tenant;
- guard central continua segunda linha de defesa;
- troca de Empresa deve alterar o alvo de edição.

## Testes obrigatórios

Atender integralmente U1-U10, P1-P5 e W1-W22 da UC027.

Ênfases:

- atomicidade de domínio;
- null vs zero;
- vírgula pt-BR;
- antiforgery;
- request manipulado;
- cross-tenant;
- configuração ausente = 404;
- Produto Novo com margem null/zero/configurada;
- POST inválido de Produto preserva input;
- Produtos existentes não mudam.

Reutilizar e ampliar:

- `ConfiguracaoPrecificacaoEmpresaTests`;
- `ConfiguracaoPrecificacaoPageTests`;
- testes de Produto Novo existentes;
- `WebTestContext`.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

## Documentação pós-implementação

Na PR de implementação:

- alterar UC027 de `Pronto` para `Concluído` somente no backlog;
- não marcar MEL009 como Concluído;
- não criar migration;
- não reintroduzir Status em documentos individuais.

## Retorno obrigatório

Informar:

1. branch;
2. arquivos alterados;
3. método de domínio final;
4. parsing/formulário;
5. comportamento null/zero;
6. integração MargemPadrao em Produto Novo;
7. tenant/antiforgery;
8. testes U/P/W;
9. build/test;
10. URL da PR.

Commit sugerido:

~~~text
feat: altera configuracoes de precificacao
~~~
