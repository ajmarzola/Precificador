# Instrução Codex — UC026: Consultar configurações de precificação da Empresa

## Tarefa

Implementar integralmente a UC026 conforme `docs/use-cases/UC026-consultar-configuracoes-precificacao.md`.

Branch obrigatória:

~~~text
feat/uc026-consultar-configuracoes-precificacao
~~~

Não editar/commitar/push direto em `master`. Não fazer merge da própria implementação.

## Precondições

Antes de alterar arquivos:

1. atualizar `master`;
2. criar/trocar para a branch obrigatória;
3. confirmar branch != master;
4. ler `AGENTS.md`;
5. ler `docs/development/backlog.md` e confirmar UC026 = `Pronto`;
6. ler UC026, F006, RN017/RN019/RN021/RN025/RN039/RN052 e MEL009;
7. inspecionar `Empresa`, `EmpresaConfiguration`, `PrecificadorDbContext`, `IEmpresaContext`, `IEntidadeEmpresa`, `_Layout.cshtml` e migrations atuais.

Se UC026 não estiver `Pronto`, não implementar.

## Modelo obrigatório

Criar entidade 1:1:

~~~text
ConfiguracaoPrecificacaoEmpresa : IEntidadeEmpresa
- EmpresaId : int (PK/FK)
- ValorHoraTrabalho : decimal?
- TarifaEnergiaKwh : decimal?
- MargemPadrao : decimal?
- IncrementoComercial : decimal?
- ReservaComercialDesconto : decimal
~~~

Não criar Id adicional.

## Defaults

`CriarPadrao(empresaId)` deve resultar em:

~~~text
ValorHoraTrabalho = null
TarifaEnergiaKwh = null
MargemPadrao = null
IncrementoComercial = null
ReservaComercialDesconto = 0.10m
~~~

Não inventar outros defaults.

## Invariantes

Preparar validações coerentes com a especificação:

- EmpresaId > 0;
- ValorHoraTrabalho, se informado, >= 0;
- TarifaEnergiaKwh, se informada, >= 0;
- MargemPadrao, se informada, >= 0 e < 1;
- IncrementoComercial, se informado, > 0;
- ReservaComercialDesconto >= 0 e < 1.

UC026 não precisa disponibilizar UI/método público de edição completa; UC027 fará isso.

## EF / Persistência

Adicionar:

~~~text
DbSet<ConfiguracaoPrecificacaoEmpresa> ConfiguracoesPrecificacaoEmpresas
~~~

Configurar:

- tabela `ConfiguracoesPrecificacaoEmpresas`;
- `EmpresaId` como PK;
- FK para `Empresas.Id` com Restrict;
- nullable nos quatro parâmetros sem default;
- Reserva obrigatória, default 0.10;
- precisões decimais conforme UC;
- GQF/guard por implementar `IEntidadeEmpresa`.

Não criar índice único redundante em EmpresaId além da PK.

## Migration

Criar migration evolutiva `AddConfiguracoesPrecificacaoEmpresa` ou equivalente.

Ela deve:

1. criar tabela;
2. backfill de todas as Empresas existentes;
3. deixar quatro parâmetros NULL;
4. usar Reserva = 0.10;
5. não editar migrations históricas.

Testar upgrade real do schema anterior.

## Empresa futura / testes

Fluxos pós-UC026 que criam Empresa devem criar configuração padrão na mesma unidade de trabalho.

Como `/Setup` atual apenas renomeia a Empresa técnica, não alterar o Setup para criar duplicata.

Atualizar helper de testes que cria Empresas adicionais para inicializar configuração padrão quando representar uma Empresa válida pós-UC026.

## Página

Criar:

~~~text
/Configuracoes/Precificacao
~~~

Read-only.

Exibir:

- Valor da hora de trabalho;
- Tarifa de energia (R$/kWh);
- Margem padrão para novos produtos (%);
- Incremento comercial de arredondamento;
- Reserva comercial para desconto (%).

Campos null => `Não configurado`.

Reserva default => 10%.

Ajuda obrigatória:

~~~text
Percentual reservado acima do preço sugerido antes de formar o desconto de referência.
~~~

## Navegação

Adicionar `Configurações` no menu principal apontando para `/Configuracoes/Precificacao`.

Não criar link/botão de edição para rota inexistente.

## Formatação

Usar pt-BR determinístico.

Percentuais persistidos como fração e exibidos em percentual.

Evitar lógica de formatação espalhada na Razor; criar helper se necessário.

`null` nunca aparece como 0/0%/R$ 0.

## Segurança

- autenticação e Empresa Ativa obrigatórias;
- sem `IgnoreQueryFilters`;
- request não controla EmpresaId;
- Empresa A não lê configuração da B;
- GET não chama SaveChanges e não cria configuração.

## Não antecipar UC027

Não implementar:

- POST de alteração;
- InputModel de edição;
- botão Alterar funcional;
- PRG de atualização;
- mensagens de sucesso de edição.

## Não alterar Produto

UC026 não deve:

- alterar `/Produtos/Novo`;
- pré-preencher MargemAlvo;
- recalcular Produtos;
- alterar MargemAlvo existente.

## MEL009

`ReservaComercialDesconto` deve existir desde esta migration inicial.

Não implementar:

- ReservaComercialReferencia;
- RegistroPrecoProduto;
- DescontoReferencia.

## Testes obrigatórios

Atender U1-U4, P1-P8 e W1-W10 da especificação.

Ênfases:

- migration/backfill real;
- quatro nulls + Reserva 0.10;
- PK 1:1;
- FK;
- GQF;
- guard cross-tenant;
- read-only Web;
- troca de Empresa sem vazamento;
- `Não configurado` distinto de zero;
- Reserva 10%;
- ausência de UI de edição.

Não enfraquecer testes existentes.

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

- manter UC026/F006 coerentes;
- alterar UC026 de `Pronto` para `Concluído` somente no backlog;
- não marcar MEL009 como Concluído: ela ainda depende de UC027/UC011/UC012;
- não reintroduzir Status em documentos individuais.

## Retorno obrigatório

Informar:

1. branch;
2. arquivos alterados;
3. modelo final;
4. migration/backfill;
5. comportamento dos nulls/default;
6. isolamento tenant;
7. testes U/P/W;
8. build/test;
9. URL da PR.

Commit sugerido:

~~~text
feat: adiciona configuracoes de precificacao da empresa
~~~
