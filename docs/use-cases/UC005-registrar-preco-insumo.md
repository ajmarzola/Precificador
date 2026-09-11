# UC005 — Registrar preço de insumo

- **Status:** Implementado
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001A, UC002, UC003, UC004, FT002 e RN040
- **Próximo caso relacionado:** UC006 — Consultar histórico de preços do insumo

## Objetivo

Permitir registrar novos preços conhecidos para um Insumo da **Empresa Ativa**, preservando todos os registros anteriores e estabelecendo a base histórica usada futuramente para custo atual e precificação.

O UC005 também implementa a consequência operacional da RN040: a partir do primeiro registro de preço, Nome, Marca e Unidade base do Insumo tornam-se imutáveis.

## Princípios

1. preço é **histórico append-only**;
2. registrar preço nunca sobrescreve registro anterior;
3. quantidade de compra é informada na **Unidade base do Insumo**;
4. preço de compra representa o valor total pago/cotado para essa quantidade;
5. custo unitário é derivado, não persistido;
6. datas passadas, presentes e futuras são permitidas;
7. qualquer preço registrado, inclusive futuro, ativa imediatamente a RN040;
8. Insumo inativo continua podendo receber registros de preço;
9. preço é tenant-owned e isolado pela Empresa Ativa;
10. não implementar consulta completa de histórico — responsabilidade do UC006.

## Modelo de domínio

Criar entidade:

~~~text
PrecoInsumo : IEntidadeEmpresa
- Id: int
- EmpresaId: int
- InsumoId: int
- QuantidadeCompra: decimal
- PrecoCompra: decimal
- DataReferencia: DateOnly
- CustoUnitario: decimal (calculado, não persistido)
~~~

### EmpresaId

EmpresaId é obrigatório e nunca vem do formulário.

No fluxo Web, deve ser derivado do Insumo previamente carregado com o Global Query Filter ativo.

### InsumoId

InsumoId é obrigatório e referencia um Insumo existente.

A FK deve usar DeleteBehavior.Restrict.

Não permitir exclusão em cascata do histórico se algum código futuro tentar remover fisicamente o Insumo.

### QuantidadeCompra

Aplicar RN002:

~~~text
QuantidadeCompra > 0
~~~

A quantidade é sempre expressa na Unidade base do Insumo.

Exemplos:

~~~text
Insumo em g:
QuantidadeCompra = 1000
PrecoCompra = 5,39
=> compra de 1000 g por R$ 5,39

Insumo em m:
QuantidadeCompra = 3,5
PrecoCompra = 12,00
=> compra de 3,5 m por R$ 12,00
~~~

Não adicionar kg, litro, pacote, caixa ou conversões automáticas neste UC.

### PrecoCompra

Aplicar RN003:

~~~text
PrecoCompra > 0
~~~

Representa o **valor total** correspondente a QuantidadeCompra.

Usar decimal.

A configuração relacional deve preservar escala suficiente; referência recomendada:

~~~text
PrecoCompra      decimal(18,4)
QuantidadeCompra decimal(18,6)
~~~

No SQLite, a precisão declarada não substitui validação de domínio.

### DataReferencia

Usar DateOnly.

Representa a data econômica do preço.

Aceitar data passada, atual ou futura.

Preço futuro é histórico válido, porém só poderá ser considerado vigente a partir de sua DataReferencia quando a seleção de preço atual for usada.

Não adicionar hora/timezone ao registro neste UC.

### CustoUnitario

Aplicar RN004:

~~~text
CustoUnitario = PrecoCompra / QuantidadeCompra
~~~

Não persistir CustoUnitario.

Não arredondar para centavos durante o cálculo.

O domínio deve expor comportamento/propriedade calculada testável.

Exemplo canônico:

~~~text
5,39 / 1000 = 0,00539
~~~

## Histórico append-only

Aplicar RN005.

Cada POST válido cria **novo registro**.

Nunca atualizar, apagar, substituir por data ou fazer upsert de preço anterior.

Dois ou mais preços com a mesma DataReferencia são permitidos.

Isso permite registrar uma correção como novo fato sem destruir o registro anterior.

### Desempate futuro do preço vigente

Aplicar RN006.

Quando houver múltiplos registros na mesma DataReferencia, o registro com **maior Id** é considerado o registrado por último no MVP.

A ordenação normativa futura será:

~~~text
DataReferencia DESC
Id DESC
~~~

O UC005 deve persistir dados compatíveis com essa regra.

Não é obrigatório implementar a tela/consulta completa de preço vigente neste UC; UC006 detalhará a consulta de histórico.

## Insumo ativo ou inativo

O registro de preço é permitido tanto para Insumo ativo quanto inativo.

Justificativa:

- preço é fato histórico/comercial;
- pode representar backfill, correção ou informação futura;
- registrar preço não torna o Insumo elegível para nova Ficha Técnica;
- registrar preço não reativa o Insumo;
- RN008 continua governando a situação operacional.

A página deve mostrar claramente a Situação atual do Insumo.

## Multiempresa

PrecoInsumo implementa IEntidadeEmpresa.

Adicionar DbSet de PrecosInsumos e Global Query Filter por Empresa Ativa.

O guard central de SaveChanges/SaveChangesAsync já opera sobre IEntidadeEmpresa e deve proteger também PrecoInsumo.

Como consequência, suas mensagens atuais específicas de "insumos" devem ser generalizadas para dados tenant-owned, sem alterar a lógica do guard.

Sugestão:

~~~text
Uma empresa ativa é necessária para alterar dados da empresa.
Não é permitido alterar dados de outra empresa.
~~~

Não confiar em EmpresaId do request.

Não usar IgnoreQueryFilters no fluxo comum.

## Persistência

Criar tabela:

~~~text
PrecosInsumos
~~~

Campos mínimos:

- Id;
- EmpresaId;
- InsumoId;
- QuantidadeCompra;
- PrecoCompra;
- DataReferencia.

FKs:

~~~text
PrecosInsumos.EmpresaId -> Empresas.Id     RESTRICT
PrecosInsumos.InsumoId  -> Insumos.Id      RESTRICT
~~~

Criar índice de apoio à consulta histórica:

~~~text
(EmpresaId, InsumoId, DataReferencia)
~~~

Não criar índice único por data.

### Migration

Criar migration evolutiva.

Nome sugerido:

~~~text
AddPrecosInsumos
~~~

Não editar migrations históricas.

Atualizar PrecificadorDbContextModelSnapshot apenas pelo fluxo normal do EF Core.

A migration deve funcionar em banco vazio e sobre o schema atual contendo Insumos existentes.

Não criar preço retroativo automaticamente para Insumos existentes.

## Web — nova página

Criar Razor Page:

~~~text
/Insumos/Precos/Novo/{id:int}
~~~

O id identifica o Insumo.

A pasta /Insumos já é protegida pela política de Empresa Ativa.

### GET

Carregar apenas o Insumo visível à Empresa Ativa.

Se não existir, retornar HTTP 404, inclusive para Insumo de outro tenant.

Exibir resumo não editável:

- Nome;
- Marca;
- Unidade base;
- Situação.

Campos:

1. Quantidade comprada;
2. Preço total da compra;
3. Data de referência.

O rótulo de Quantidade deve explicitar a unidade, por exemplo:

~~~text
Quantidade comprada (g)
Quantidade comprada (m)
Quantidade comprada (un)
~~~

Usar InsumoRotulos.

### POST

1. rebuscar Insumo pelo id com Global Query Filter ativo;
2. se não existir, 404;
3. validar campos;
4. criar PrecoInsumo usando EmpresaId e Id do Insumo carregado no servidor;
5. adicionar ao DbContext;
6. salvar;
7. PRG para detalhes.

Mensagem:

~~~text
Preço do insumo registrado com sucesso.
~~~

Não receber EmpresaId ou InsumoId como campos confiáveis do formulário.

### Navegação

Adicionar em /Insumos/Detalhes/{id} a ação **Registrar preço** para Insumos ativos e inativos.

Não adicionar histórico completo; UC006.

## Aplicação da RN040 na edição

O UC005 deve adaptar /Insumos/Editar/{id}.

### Sem histórico de preço

Preservar o UC003:

- Nome editável;
- Marca editável;
- Categoria editável;
- Unidade base editável;
- Observação editável.

### Com pelo menos um preço

Renderizar:

- Nome — somente leitura;
- Marca — somente leitura;
- Unidade base — somente leitura;
- Categoria — editável;
- Observação — editável.

Exibir:

~~~text
Nome, marca e unidade base não podem ser alterados porque este insumo já possui histórico de preços.
~~~

A restrição não pode depender apenas do HTML.

No POST:

1. rebuscar Insumo pelo tenant;
2. verificar se existe qualquer PrecoInsumo para o id;
3. se houver histórico, nunca usar valores do request para substituir Nome, Marca ou Unidade base;
4. permitir Categoria e Observação;
5. preservar atomicidade e demais regras do UC003.

Um POST manipulado nunca pode mudar os campos congelados.

Não é obrigatório introduzir nova camada de serviço.

## Critérios de aceitação

### CA01 — Acesso protegido
Usuário anônimo não acessa o fluxo.

### CA02 — GET mostra Insumo correto
GET da Empresa Ativa mostra Nome, Marca, Unidade base e Situação.

### CA03 — Valores válidos criam novo preço
Quantidade > 0, Preço > 0 e DataReferencia válida criam PrecoInsumo.

### CA04 — Custo unitário é exato
CustoUnitario = PrecoCompra / QuantidadeCompra sem persistência/arredondamento prematuro.

### CA05 — Quantidade inválida é rejeitada
Zero ou negativa não cria registro.

### CA06 — Preço inválido é rejeitado
Zero ou negativo não cria registro.

### CA07 — Data obrigatória aceita passado/presente/futuro
Data ausente/inválida é rejeitada; datas válidas, inclusive futuras, persistem.

### CA08 — Histórico append-only
Novo preço não altera/remove anteriores e mesma data aceita múltiplos registros.

### CA09 — Tenant ownership
EmpresaId vem do Insumo/Empresa Ativa, não do request.

### CA10 — Isolamento de preço
Empresa A não lê/altera preços da Empresa B no fluxo comum.

### CA11 — Insumo inexistente/cross-tenant retorna 404
GET e POST não revelam outro tenant.

### CA12 — Inativo aceita preço sem reativar
Preço pode ser registrado e Ativo permanece false.

### CA13 — Detalhes possui Registrar preço
Ação existe para ativo e inativo.

### CA14 — PRG e mensagem
POST válido redireciona a detalhes e exibe a mensagem definida.

### CA15 — Primeiro preço ativa RN040
Depois do primeiro preço, edição bloqueia Nome, Marca e Unidade base.

### CA16 — RN040 protegida no servidor
POST manipulado não altera campos congelados, inclusive quando o único preço é futuro. Categoria/Observação permanecem editáveis.

### CA17 — Sem histórico mantém edição completa
UC003 permanece funcional para Insumo sem preço.

### CA18 — FK preserva histórico
Preço referencia Empresa/Insumo existentes e não é apagado em cascata.

### CA19 — Migration evolutiva
Nova estrutura preserva Insumos existentes e migrations anteriores.

### CA20 — Sem escopo antecipado
Não implementar UC006, gráficos, fornecedor, estoque, importação, edição/exclusão de preço, Produto ou Ficha Técnica.

## Matriz de testes fechada antes da implementação

### Unitários — PrecoInsumo

- U1: CA03_Criar_preco_valido_preserva_dados_e_tenant
- U2: CA04_Custo_unitario_divide_preco_por_quantidade_sem_arredondar
- U3: CA05_Quantidade_zero_ou_negativa_e_rejeitada — Theory
- U4: CA06_Preco_zero_ou_negativo_e_rejeitado — Theory
- U5: CA03_Ids_de_empresa_e_insumo_devem_ser_positivos, se a factory receber ids

Não testar EF em unidade.

### Integração — persistência

- P1: CA19_Migration_cria_precos_e_preserva_insumos_existentes
- P2: CA18_Preco_persiste_com_fks_restrict_para_empresa_e_insumo
- P3: CA08_Dois_precos_na_mesma_data_sao_preservados
- P4: CA10_Query_filter_isola_precos_por_empresa
- P5: CA10_Guard_rejeita_escrita_de_preco_para_outra_empresa
- P6: CA18_Exclusao_fisica_de_insumo_com_preco_e_rejeitada_pela_fk

P1 deve preferencialmente migrar SQLite até o estado anterior ao UC005, inserir Insumo e então aplicar a nova migration.

### Integração — Web

- W1: CA01_Registrar_preco_exige_autenticacao
- W2: CA02_Get_exibe_resumo_do_insumo_e_unidade_na_quantidade — usar Metro
- W3: CA03_CA14_Post_valido_registra_preco_e_redireciona_com_sucesso
- W4: CA05_CA06_CA07_Post_invalido_nao_cria_preco — Theory com um erro por caso
- W5: CA11_Get_e_post_cross_tenant_retornam_404
- W6: CA12_Insumo_inativo_recebe_preco_e_permanece_inativo
- W7: CA13_Detalhes_exibe_registrar_preco_para_ativo_e_inativo
- W8: CA15_Get_edicao_com_historico_bloqueia_nome_marca_e_unidade
- W9: CA16_Post_manipulado_com_historico_nao_altera_campos_congelados
- W10: CA15_Preco_futuro_tambem_bloqueia_campos_da_RN040
- W11: CA17_Sem_historico_mantem_nome_marca_e_unidade_editaveis

Não enfraquecer testes do UC003.

## Observações da RN040

Não criar snapshots de Nome/Marca/Unidade no preço.

Não adicionar booleano PossuiHistorico persistido no Insumo; a existência é derivada por consulta.

Não alterar Insumo.Ativo ao registrar preço.

Não reimplementar MEL005.

## Fora do escopo

- UC006;
- histórico completo;
- editar/excluir preço;
- fornecedor;
- nota fiscal;
- estoque;
- unidade de compra diferente da Unidade base;
- conversão kg/g ou l/ml;
- importação em massa;
- anexos;
- gráficos;
- custo de Produto;
- Ficha Técnica;
- snapshots cadastrais;
- auditoria completa;
- soft-delete de preço;
- API REST.

## Definition of Done específica

Além da DoD global:

- PrecoInsumo tenant-owned criado;
- custo unitário calculado e não persistido;
- histórico append-only;
- ativo/inativo aceita preço sem mudança de situação;
- DbSet/configuração/query filter adicionados;
- guard tenant-aware cobre preços e mensagens ficam genéricas;
- FKs Restrict para Empresa e Insumo;
- índice histórico criado;
- migration AddPrecosInsumos ou equivalente criada normalmente;
- migrations históricas intocadas;
- página /Insumos/Precos/Novo/{id} funcional;
- detalhes possui Registrar preço;
- cross-tenant/inexistente => 404;
- PRG + mensagem funcionam;
- RN040 aplicada no GET e POST da edição;
- preço futuro também congela campos;
- Insumo sem histórico mantém edição completa;
- nenhuma consulta completa do UC006;
- documentação pós-implementação marca UC005 como Implementado;
- F001/catálogo/ordem/modelo de preço coerentes;
- UC006 passa a próximo caso;
- gate do UC014 permanece aberto;
- ideias fora do escopo vão para docs/development/melhorias.md;
- build Release sem warnings novos;
- suíte completa verde.
