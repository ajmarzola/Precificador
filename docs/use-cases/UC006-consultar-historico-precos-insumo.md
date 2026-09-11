# UC006 — Consultar histórico de preços do insumo

- **Status:** Revalidado — pronto para implementação
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC005, FT002, RN004, RN005, RN006, RN007 e RN040
- **Próximo caso:** UC007 — Cadastrar produto
- **Sem alteração de schema:** este UC é exclusivamente de consulta/apresentação

## Objetivo

Permitir consultar todo o histórico de preços de um Insumo da **Empresa Ativa**, identificar de forma inequívoca qual registro é o preço vigente atual e distinguir registros anteriores de preços com data futura.

O UC006 também deve apresentar um resumo do preço vigente na página de detalhes do Insumo.

## Princípios

1. histórico é somente leitura;
2. nenhum estado `Vigente`, `Anterior` ou `Futuro` é persistido;
3. preço vigente é derivado pela RN006 no momento da consulta;
4. preço futuro aparece no histórico, mas não é vigente antes de sua DataReferencia;
5. Insumo sem preço vigente possui custo desconhecido, nunca zero;
6. consultas respeitam Empresa Ativa e Global Query Filters;
7. Insumo ativo ou inativo possui histórico consultável;
8. nenhum preço é editado ou excluído neste UC.

## Data atual usada pela consulta

Definir uma única `dataAtual` por request e reutilizá-la para selecionar o preço vigente e classificar as linhas.

No MVP, usar a **data local da aplicação** como referência de calendário.

Não introduzir configuração de timezone por Empresa neste UC.

Essa limitação deve ser registrada como melhoria não bloqueante para eventual hospedagem em timezone diferente do negócio.

Nos testes, usar datas relativas à data corrente da execução quando o cenário depender de passado/presente/futuro, evitando datas fixas que possam envelhecer.

## Seleção do preço vigente — RN006

Para um Insumo:

1. considerar somente registros com `DataReferencia <= dataAtual`;
2. ordenar por `DataReferencia DESC`;
3. em empate, ordenar por `Id DESC`;
4. o primeiro registro é o preço vigente.

Forma conceitual:

~~~text
precoVigente =
    PrecosInsumos
      .Where(p => p.InsumoId == id && p.DataReferencia <= dataAtual)
      .OrderByDescending(p => p.DataReferencia)
      .ThenByDescending(p => p.Id)
      .FirstOrDefault()
~~~

Não persistir:

- `EhVigente`;
- `StatusPreco`;
- `PrecoAtualId`;
- qualquer cache de vigente no Insumo.

## Classificação visual das linhas

Cada registro do histórico recebe status apenas para apresentação:

### Vigente

O registro cujo `Id` é o mesmo do preço vigente selecionado pela RN006.

### Futuro

~~~text
DataReferencia > dataAtual
~~~

### Anterior

Registro que:

- não é futuro; e
- não é o vigente.

Isso inclui um preço mais antigo e também um registro de mesma DataReferencia do vigente com `Id` menor.

Exemplo:

~~~text
Data atual: 11/09/2026

Id 12 | 20/09/2026 | Futuro
Id 11 | 11/09/2026 | Vigente
Id 10 | 11/09/2026 | Anterior
Id  9 | 01/09/2026 | Anterior
~~~

## Ordenação do histórico completo

A tabela completa deve ser ordenada por:

~~~text
DataReferencia DESC
Id DESC
~~~

Consequência intencional:

- preços futuros podem aparecer antes do vigente na tabela;
- o destaque de status impede que o usuário confunda "primeira linha" com "preço atual".

Não reordenar artificialmente para colocar o vigente no topo.

## Insumo sem preço vigente — RN007

Existem dois cenários distintos:

### Nenhum registro de preço

Exibir:

~~~text
Sem preço vigente.
Nenhum preço registrado para este insumo.
~~~

### Existem apenas preços futuros

Exibir no resumo:

~~~text
Sem preço vigente.
~~~

e listar normalmente os registros futuros na tabela com status **Futuro**.

Nunca exibir custo zero.

## Custo unitário

Usar a propriedade calculada existente:

~~~text
PrecoInsumo.CustoUnitario
~~~

Não recalcular por fórmula duplicada na Razor Page.

Não persistir custo.

### Precisão de apresentação

O cálculo de domínio permanece com precisão decimal completa.

Na UI:

- QuantidadeCompra: exibir até 6 casas decimais, removendo zeros finais desnecessários;
- PrecoCompra: exibir até 4 casas decimais;
- CustoUnitario: exibir até 6 casas decimais, removendo zeros finais desnecessários;
- DataReferencia: `dd/MM/yyyy`.

Exemplo obrigatório de precisão:

~~~text
PrecoCompra = 5,39
QuantidadeCompra = 1000
CustoUnitario = 0,00539
~~~

A UI não pode transformar esse custo em `0,01`.

A apresentação atual pode usar a convenção brasileira já adotada pelo produto. Não criar modelo de moeda, multi-currency ou infraestrutura completa de localização neste UC.

## Nova página Web

Criar Razor Page:

~~~text
/Insumos/Precos/Historico/{id:int}
~~~

O `id` identifica o Insumo.

A pasta `/Insumos` permanece protegida pela política de Empresa Ativa.

## GET do histórico

### Carregamento do Insumo

Buscar o Insumo por `id` com Global Query Filter ativo e `AsNoTracking`.

Se não existir ou pertencer a outro tenant:

~~~text
HTTP 404
~~~

Não usar `IgnoreQueryFilters`.

### Resumo do Insumo

Exibir:

- Nome;
- Marca;
- Unidade base;
- Situação Ativo/Inativo.

Usar `InsumoRotulos` para Unidade base.

### Resumo do preço vigente

Se houver preço vigente, exibir pelo menos:

- Data de referência;
- Quantidade comprada com unidade;
- Preço total;
- Custo unitário por unidade base.

Se não houver:

~~~text
Sem preço vigente.
~~~

### Tabela do histórico

Colunas mínimas:

1. Data;
2. Status;
3. Quantidade;
4. Preço total;
5. Custo unitário.

Status possíveis:

- Vigente;
- Anterior;
- Futuro.

Não exibir controles Editar/Excluir.

### Navegação

A página deve conter:

- **Registrar novo preço** -> `/Insumos/Precos/Novo/{id}`;
- **Voltar ao insumo** -> `/Insumos/Detalhes/{id}`.

Registrar novo preço continua disponível para Insumo ativo ou inativo, conforme UC005/RN008.

## Alteração em Detalhes do Insumo

A página:

~~~text
/Insumos/Detalhes/{id:int}
~~~

deve ganhar:

### Link

~~~text
Histórico de preços
~~~

para ativo e inativo.

### Resumo do preço vigente

Se houver preço vigente:

~~~text
Preço vigente
- referência
- quantidade + unidade
- preço total
- custo unitário
~~~

Se não houver:

~~~text
Preço vigente: Sem preço vigente.
~~~

Não listar o histórico inteiro em Detalhes.

Não mostrar preço futuro como atual.

## Consulta e reutilização da regra

A lógica `DataReferencia <= dataAtual + OrderByDescending(DataReferencia) + ThenByDescending(Id)` é regra normativa e será reutilizada futuramente pelo motor de custo.

Evitar duplicar variantes divergentes entre Histórico e Detalhes.

É aceitável criar um helper/query focado para `PrecoInsumo` se isso evitar duplicação, mas:

- não criar Repository genérico;
- não criar Unit of Work;
- não criar CQRS/MediatR;
- não criar serviço de domínio sem necessidade;
- não antecipar UC018.

## Performance e paginação

O MVP exibe o histórico completo do Insumo.

Não implementar paginação neste UC.

O índice já existente:

~~~text
(EmpresaId, InsumoId, DataReferencia)
~~~

deve ser reutilizado.

Se volume real justificar paginação, registrar melhoria futura em vez de ampliar este UC.

## Multiempresa

Preservar FT002 e as correções pós-UC005.

- Insumo é buscado pelo Global Query Filter;
- PrecosInsumos também usam Global Query Filter;
- não usar `IgnoreQueryFilters` em Histórico ou Detalhes;
- cross-tenant => 404;
- nenhuma linha de preço de outra Empresa pode aparecer.

A integridade de escrita não muda neste UC.

## Critérios de aceitação

### CA01 — Acesso protegido

Usuário anônimo não acessa o histórico.

### CA02 — Histórico exibe o Insumo correto

GET válido mostra Nome, Marca, Unidade base e Situação do Insumo da Empresa Ativa.

### CA03 — Histórico é ordenado corretamente

Linhas seguem:

~~~text
DataReferencia DESC, Id DESC
~~~

inclusive quando existem múltiplos registros na mesma data.

### CA04 — Preço vigente segue RN006

Entre registros não futuros, o de maior DataReferencia e, em empate, maior Id recebe status **Vigente**.

### CA05 — Preço futuro não é vigente

Registro com DataReferencia futura recebe status **Futuro** e nunca substitui o vigente atual antes da data.

### CA06 — Registros não vigentes do passado são Anteriores

Todo registro não futuro que não seja o vigente recebe status **Anterior**.

### CA07 — Mesma data desempata por Id

Com dois preços na mesma DataReferencia vigente:

- maior Id = Vigente;
- menor Id = Anterior.

### CA08 — Sem registros possui estado vazio

Exibe:

~~~text
Sem preço vigente.
Nenhum preço registrado para este insumo.
~~~

e não exibe custo zero.

### CA09 — Apenas preços futuros não criam vigente

Histórico lista os futuros, mas resumo informa **Sem preço vigente**.

### CA10 — Custo unitário usa precisão adequada

O exemplo `5,39 / 1000` é apresentado como `0,00539` por unidade, sem arredondamento para centavos.

### CA11 — Histórico de Insumo inativo é consultável

Inativo possui a mesma consulta histórica e pode navegar para Registrar novo preço.

### CA12 — Cross-tenant retorna 404

ID de Insumo de outra Empresa é indistinguível de inexistente.

### CA13 — Detalhes possui link para histórico

Ativo e inativo exibem **Histórico de preços**.

### CA14 — Detalhes mostra preço vigente correto

Resumo em Detalhes usa a mesma RN006 e nunca apresenta preço futuro como vigente.

### CA15 — Detalhes sem vigente não usa zero

Sem registro vigente exibe **Sem preço vigente**.

### CA16 — Somente leitura

UC006 não cria, altera ou exclui registros de preço.

### CA17 — Sem migration

Nenhuma migration, schema ou ModelSnapshot é alterado.

### CA18 — Sem escopo antecipado

Não implementar Produto, Ficha Técnica, cálculo de custo de Produto, gráficos, edição/exclusão de preço ou filtros avançados.

## Matriz de testes fechada antes da implementação

### Unitários

Nenhum novo teste unitário é obrigatório se não houver nova lógica pura de domínio.

Se for criado helper puro para classificar/selecionar preço, adicionar testes unitários focados para RN006; não criar helper apenas para justificar testes unitários.

### Integração — persistência/consulta

#### P1

~~~text
CA03_CA04_Consulta_ordena_por_data_e_id_e_seleciona_vigente
~~~

Preparar:

- passado;
- dois registros na data atual;
- futuro.

Confirmar ordenação completa e vigente pelo maior Id da maior data não futura.

#### P2

~~~text
CA09_Apenas_precos_futuros_nao_produzem_preco_vigente
~~~

#### P3

~~~text
CA12_Query_filter_impede_historico_de_outro_tenant
~~~

Usar duas Empresas.

Não usar `IgnoreQueryFilters` para provar o fluxo comum.

### Integração — Web

#### W1

~~~text
CA01_Historico_exige_autenticacao
~~~

#### W2

~~~text
CA02_Historico_exibe_resumo_do_insumo
~~~

Usar unidade não trivial, por exemplo Metro.

#### W3

~~~text
CA03_CA04_CA05_CA06_Historico_exibe_ordem_e_status_corretos
~~~

Cenário com futuro + vigente + anterior.

Não afirmar apenas presença global; verificar a relação entre cada registro/data e seu status.

#### W4

~~~text
CA07_Mesma_data_marca_maior_id_como_vigente
~~~

#### W5

~~~text
CA08_Sem_precos_exibe_estado_vazio_sem_custo_zero
~~~

#### W6

~~~text
CA09_Apenas_precos_futuros_exibe_sem_vigente_e_lista_futuros
~~~

#### W7

~~~text
CA10_Custo_unitario_preserva_precisao_na_apresentacao
~~~

Usar `5.39 / 1000`.

#### W8

~~~text
CA11_Historico_de_inativo_e_consultavel_e_permite_registrar_novo_preco
~~~

#### W9

~~~text
CA12_Historico_cross_tenant_retorna_404
~~~

#### W10

~~~text
CA13_Detalhes_exibe_link_historico_para_ativo_e_inativo
~~~

#### W11

~~~text
CA14_Detalhes_exibe_preco_vigente_sem_promover_preco_futuro
~~~

#### W12

~~~text
CA15_Detalhes_sem_preco_vigente_exibe_estado_desconhecido_sem_zero
~~~

Incluir cenário com apenas preço futuro, pois existe histórico mas não vigente.

## Migration

Não criar migration.

Não alterar migrations históricas.

Não alterar `PrecificadorDbContextModelSnapshot`.

Qualquer necessidade percebida de schema novo deve interromper a implementação e ser reavaliada.

## Fora do escopo

- editar preço;
- excluir preço;
- corrigir preço por update;
- paginação;
- filtros por período/status;
- exportação;
- gráficos;
- fornecedor;
- estoque;
- unidade de compra/conversões;
- currency/multi-currency;
- timezone configurável por Empresa;
- Produto;
- Ficha Técnica;
- motor de custo UC018;
- snapshots históricos.

## Definition of Done específica

Além da DoD global:

- nova página de histórico funcional;
- consultas `AsNoTracking`;
- Global Query Filters preservados;
- seleção do vigente implementa literalmente RN006;
- mesma regra usada em Histórico e Detalhes;
- futuro nunca vira vigente antecipadamente;
- estados Vigente/Anterior/Futuro corretos;
- estado vazio diferencia ausência de vigente de custo zero;
- custo unitário exibido sem arredondamento prematuro;
- histórico disponível para ativo/inativo;
- links Histórico/Registrar novo preço funcionam;
- nenhuma mutação de preço;
- nenhuma migration/snapshot;
- UC006 passa para Implementado;
- F001/catálogo/ordem/modelo de preço ficam coerentes;
- UC007 passa a ser o próximo caso;
- gate do UC014 permanece;
- melhoria de timezone fica registrada, sem implementação;
- build Release sem warnings novos relevantes;
- suíte completa verde.
