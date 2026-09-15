# UC011 — Registrar preço de prateleira preservando snapshot de precificação

- **Funcionalidades:** F002 — Gestão de Produtos; F004 — Precificação
- **Dependências:** UC023, MEL006, UC026, UC027 e MEL009
- **Schema:** sim
- **Natureza:** histórico comercial append-only

## Objetivo

Permitir registrar o `PrecoPrateleira` de um Produto da Empresa Ativa, congelando no mesmo registro a precificação vigente. O usuário informa somente o preço de prateleira; os demais valores são calculados ou obtidos pelo servidor.

## Snapshot

Criar entidade tenant-owned:

~~~text
RegistroPrecoProduto
- Id : int
- EmpresaId : int
- ProdutoId : int
- DataReferencia : DateOnly
- CustoReferencia : decimal
- MargemReferencia : decimal
- PrecoSugerido : decimal
- PrecoPrateleira : decimal
- ReservaComercialReferencia : decimal
~~~

Origem dos campos:

~~~text
EmpresaId                  = Produto.EmpresaId
ProdutoId                  = Produto.Id
DataReferencia             = IDataOperacionalEmpresa.Hoje
CustoReferencia            = CustoUnitarioProduto atual (UC022)
MargemReferencia           = Produto.MargemAlvo
PrecoSugerido              = resultado atual da UC023
PrecoPrateleira            = valor informado pelo usuário
ReservaComercialReferencia = Configuracao.ReservaComercialDesconto
~~~

Não persistir `PrecoTeorico`, `IncrementoComercialReferencia` ou `DescontoReferencia`.

## Pré-condição

O registro só pode ser criado quando UC023 estiver completo. Custo e Preço sugerido indisponíveis impedem o snapshot.

## Preço de prateleira

Complementar RN024:

~~~text
PrecoPrateleira > 0
~~~

Preço abaixo de `PrecoSugerido` é permitido e não bloqueia a decisão comercial.

## Data de referência

O usuário não informa data.

~~~text
DataReferencia = IDataOperacionalEmpresa.Hoje
~~~

Múltiplos registros na mesma data são permitidos. A ordem histórica futura é `DataReferencia DESC, Id DESC`.

## Append-only

Cada POST válido cria nova linha. Nunca atualizar, substituir, excluir ou fazer upsert de registro anterior. Correções são novos registros.

## MEL009

Aplicar RN054: todo registro congela `ReservaComercialReferencia` com a `ReservaComercialDesconto` vigente. Alteração posterior da configuração não modifica registros antigos. `DescontoReferencia` continua derivado e não é persistido.

## Produto inativo

Produto inativo pode receber novo registro e permanece inativo.

## Reuso da precificação atual

O snapshot deve usar exatamente a mesma semântica UC018–UC023 exibida na Ficha Técnica. Não duplicar fórmulas ou criar uma segunda implementação independente.

Extrair/reutilizar uma orquestração compartilhada da precificação atual do Produto, utilizada por `FichaTecnicaModel` e pela página do UC011. Ela deve continuar usando as calculadoras puras existentes de Itens, Perdas, Mão de obra, Energia, Custo do Produto e Preço do Produto.

A refatoração não pode alterar o comportamento já validado da Ficha Técnica.

## Momento do snapshot

O POST recalcula a precificação no servidor. Não usar valores calculados enviados pelo formulário.

~~~text
POST PrecoPrateleira
=> carregar Produto tenant-aware
=> validar PrecoPrateleira
=> recalcular UC018–UC023
=> exigir UC023 completo
=> ler ReservaComercialDesconto atual
=> criar RegistroPrecoProduto
=> SaveChanges
=> PRG
~~~

Se custo, margem, incremento ou reserva mudarem entre GET e POST, o snapshot usa o estado vigente no POST.

## Domínio

Factory sugerida:

~~~text
RegistroPrecoProduto.Criar(
    empresaId,
    produtoId,
    dataReferencia,
    custoReferencia,
    margemReferencia,
    precoSugerido,
    precoPrateleira,
    reservaComercialReferencia)
~~~

Validar:

~~~text
empresaId > 0
produtoId > 0
dataReferencia != default
custoReferencia >= 0
0 <= margemReferencia < 1
precoSugerido >= 0
precoPrateleira > 0
0 <= reservaComercialReferencia < 1
~~~

Não expor edição de snapshots.

## Persistência

Tabela:

~~~text
RegistrosPrecosProdutos
~~~

Precisões recomendadas:

~~~text
CustoReferencia            decimal(18,6)
MargemReferencia           decimal(9,6)
PrecoSugerido              decimal(18,6)
PrecoPrateleira            decimal(18,6)
ReservaComercialReferencia decimal(9,6)
~~~

FKs RESTRICT para `Empresas` e `Produtos`.

Índice não único:

~~~text
(EmpresaId, ProdutoId, DataReferencia)
~~~

Adicionar DbSet, configuração EF, Global Query Filter e guards tenant-aware. Proteger a coerência entre EmpresaId do registro e EmpresaId do Produto.

## Migration

Criar migration evolutiva `AddRegistrosPrecosProdutos` ou equivalente. Não editar migrations históricas e não criar registros comerciais retroativos.

## Web

Criar:

~~~text
/Produtos/Precos/Novo/{id:int}
~~~

### GET

Produto inexistente ou de outro tenant retorna 404.

Exibir Nome, Categoria, Situação, Margem-alvo, Custo unitário, Preço teórico e Preço sugerido atuais. Campo editável único: `Preço de prateleira`.

Não preencher automaticamente o Preço de prateleira com o sugerido.

Se a precificação estiver incompleta, explicar os impedimentos e não permitir snapshot parcial. O servidor também deve bloquear o POST.

### POST

1. carregar Produto tenant-aware;
2. validar `PrecoPrateleira > 0`;
3. recalcular precificação no servidor;
4. rejeitar se UC023 incompleto;
5. usar data operacional atual;
6. congelar ReservaComercialDesconto atual;
7. criar e persistir o registro;
8. PRG para `/Produtos/Detalhes/{id}`;
9. mensagem `Preço de prateleira registrado com sucesso.`

POST inválido preserva o valor informado e não cria registro.

## Navegação

Adicionar em Detalhes a ação `Registrar preço de prateleira`, disponível para Produto ativo e inativo. A página de registro possui Voltar/Cancelar para Detalhes.

## Multiempresa

`RegistroPrecoProduto` implementa `IEntidadeEmpresa`. EmpresaId vem do Produto carregado, nunca do request. ProdutoId e snapshots também são determinados pelo servidor. Não usar `IgnoreQueryFilters` no fluxo comum.

GET/POST cross-tenant retornam 404.

## Precisão

Aplicar RN026. Persistir os valores exatos calculados, sem arredondamento prévio para centavos. Formatação Web não altera o snapshot.

## Critérios de aceitação

- **CA01:** acesso exige autenticação e Empresa Ativa.
- **CA02:** Produto inexistente/cross-tenant retorna 404.
- **CA03:** usuário informa somente PrecoPrateleira.
- **CA04:** PrecoPrateleira > 0.
- **CA05:** DataReferencia vem de `IDataOperacionalEmpresa.Hoje`.
- **CA06:** cada POST válido cria novo registro append-only.
- **CA07:** mesma data admite múltiplos registros.
- **CA08:** CustoReferencia congela UC022 sem arredondamento.
- **CA09:** MargemReferencia congela Produto.MargemAlvo.
- **CA10:** PrecoSugerido congela UC023.
- **CA11:** ReservaComercialReferencia congela RN054/MEL009.
- **CA12:** request não controla ownership, data ou snapshots.
- **CA13:** precificação incompleta impede registro.
- **CA14:** preço abaixo do sugerido é permitido.
- **CA15:** Produto inativo recebe registro sem reativação.
- **CA16:** alterações posteriores não modificam snapshot antigo.
- **CA17:** mudança da reserva gera snapshots distintos antes/depois.
- **CA18:** POST recalcula; valores do GET não são usados como snapshot.
- **CA19:** isolamento multiempresa é preservado.
- **CA20:** FKs usam RESTRICT e histórico não tem unicidade por data.
- **CA21:** DescontoReferencia não é persistido.
- **CA22:** Detalhes oferece Registrar preço para ativo e inativo.
- **CA23:** PRG e mensagem de sucesso após POST válido.
- **CA24:** migration não cria registros retroativos.
- **CA25:** orquestração UC018–UC023 é reutilizada.
- **CA26:** Ficha Técnica mantém comportamento após refatoração.
- **CA27:** GET não persiste registro nem cria configuração.

## Matriz de testes

### Unitários

- U1: criação válida preserva snapshots.
- U2: ids inválidos são rejeitados.
- U3: data default é rejeitada.
- U4: custo negativo rejeitado; zero válido.
- U5: margem fora de [0,1) rejeitada.
- U6: preço sugerido negativo rejeitado; zero válido.
- U7: preço de prateleira <=0 rejeitado.
- U8: reserva fora de [0,1) rejeitada.

### Persistência

- P1: migration preserva dados anteriores.
- P2: round-trip preserva precisão.
- P3: FKs Empresa/Produto são RESTRICT.
- P4: mesma data admite múltiplos registros.
- P5: GQF isola Empresas.
- P6: guard rejeita escrita cross-tenant.
- P7: Produto de outra Empresa não pode ser referenciado.
- P8: migration não faz backfill comercial.

### Web

- W1: autenticação/Empresa Ativa obrigatórias.
- W2: GET mostra Produto e precificação atual.
- W3: somente PrecoPrateleira é editável.
- W4: POST válido grava snapshot exato + PRG.
- W5: preço <=0 não persiste.
- W6: campos manipulados de snapshot/tenant são ignorados.
- W7: GET/POST cross-tenant -> 404.
- W8: precificação incompleta impede registro.
- W9: Item sem preço impede registro.
- W10: incremento ausente impede snapshot por falta de PrecoSugerido.
- W11: Produto inativo recebe registro e permanece inativo.
- W12: preço abaixo do sugerido é aceito.
- W13: dois registros no mesmo dia são preservados.
- W14: DataReferencia usa data operacional.
- W15: mudança de custo entre GET/POST entra no snapshot do POST.
- W16: mudança de MargemAlvo entre GET/POST entra no snapshot.
- W17: mudança de Incremento entre GET/POST atualiza PrecoSugerido snapshot.
- W18: mudança de Reserva entre GET/POST atualiza Reserva snapshot.
- W19: mudança posterior de reserva não altera registro anterior.
- W20: Empresa A não usa Produto/configuração da Empresa B.
- W21: Detalhes mostra ação para ativo e inativo.
- W22: GET não persiste nada.
- W23: POST inválido preserva PrecoPrateleira.
- W24: regressão UC018–UC023 permanece verde após compartilhamento da orquestração.

## Alterações esperadas

### Core
- `RegistroPrecoProduto`.

### Infrastructure
- configuração EF, DbSet, GQF/guards e migration.

### Web
- orquestração compartilhada da precificação atual;
- `/Produtos/Precos/Novo/{id:int}`;
- ação em Detalhes;
- PRG/mensagem.

## Estado documental

Na implementação mover somente `UC011: Pronto -> Concluído`.

MEL009 permanece `Especificado` até UC012 consumir o snapshot histórico.

## Fora do escopo

UC012 histórico completo; UC024 margem atual; UC025 detalhamento; editar/excluir registro; data manual/futura; promoções/cupons; persistir DescontoReferencia ou PrecoTeorico; composição detalhada do snapshot; usuário responsável/timestamp; API REST.

## Gate

UC023 está concluída. UC026/027 fornecem ReservaComercialDesconto. MEL006 fornece data operacional. RN024/RN054/MEL009 definem o snapshot.

**UC011 está liberada para implementação após o merge desta documentação.**

## Branch sugerida

~~~text
feat/uc011-registro-preco-prateleira
~~~

## Commit sugerido

~~~text
feat: registra preco de prateleira com snapshot
~~~
