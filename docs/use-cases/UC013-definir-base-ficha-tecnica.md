# UC013 — Definir rendimento e tempo ativo da Ficha Técnica

- **Status:** Pronto para implementação
- **Funcionalidade:** F003 — Ficha Técnica
- **Dependências materiais:** UC007 a UC010 implementados; FT002 implementada
- **Próximo caso:** UC014 — Adicionar Insumo à Ficha Técnica
- **Alteração de schema:** sim — introduz FichaTecnica
- **Sequenciamento:** primeiro caso da Etapa 3, após a reordenação aprovada na PR #50

## Objetivo

Permitir definir e manter a **base produtiva atual** de um Produto da Empresa Ativa por meio de uma única Ficha Técnica, registrando:

1. Rendimento do lote em unidades de venda;
2. Tempo ativo de trabalho do lote em minutos.

A UC013 inaugura a Ficha Técnica sem antecipar composição por Insumos, perdas, equipamentos, custos ou cálculos de precificação.

## Decisão de modelagem

A Ficha Técnica representa **uma execução/lote produtivo** de um Produto.

Cada Produto possui no máximo **uma Ficha Técnica atual** no MVP.

A Ficha Técnica é estado atual editável, não histórico:

- criar a Ficha define a base produtiva corrente;
- alterar Rendimento ou TempoAtivoMinutos atualiza a mesma Ficha;
- não criar nova versão;
- não manter histórico próprio de alterações;
- históricos econômicos futuros serão preservados pelos snapshots comerciais da UC011, sem exigir versionamento prematuro da Ficha Técnica.

Produto pode existir sem Ficha Técnica. Enquanto dados necessários de precificação estiverem ausentes, RN017 continua aplicável.

## Revalidação do modelo genérico

A especificação anterior do catálogo citava “rendimento e tempos/recursos do lote”.

Após a generalização multiempresa/produtiva, a UC013 fica restrita aos conceitos universais:

- Rendimento;
- Tempo ativo de trabalho.

Não pertencem à UC013:

- TempoForno;
- PotenciaFornoKw;
- equipamento;
- uso de equipamento;
- energia;
- recurso produtivo genérico.

Equipamentos e seus tempos serão modelados de forma genérica antes/no UC021. Não criar campos temporários específicos de panificação.

## Modelo de domínio

Criar entidade sugerida:

~~~text
FichaTecnica : IEntidadeEmpresa
- Id: int
- EmpresaId: int
- ProdutoId: int
- Rendimento: decimal
- TempoAtivoMinutos: int
~~~

Namespace sugerido:

~~~text
Precificador.Core.FichasTecnicas
~~~

### Identidade

A Ficha Técnica possui Id próprio.

A associação funcional é única por Produto:

~~~text
uma Empresa + um Produto -> no máximo uma FichaTecnica atual
~~~

Persistir índice único:

~~~text
(EmpresaId, ProdutoId)
~~~

Não permitir reatribuir uma Ficha existente para outro Produto ou Empresa.

### EmpresaId

Obrigatório.

Nunca vem do formulário.

No fluxo Web deve ser derivado do Produto carregado com Global Query Filter.

A Ficha Técnica implementa IEntidadeEmpresa e participa do guard central de escrita tenant-aware.

### ProdutoId

Obrigatório.

Referencia Produto existente.

A FK deve usar DeleteBehavior.Restrict.

A persistência deve proteger também a coerência entre EmpresaId da Ficha e EmpresaId do Produto referenciado; não basta confiar apenas na FK simples por ProdutoId.

Não usar IgnoreQueryFilters no fluxo Web comum.

### Rendimento

Aplicar RN009.

Representa a quantidade de **unidades de venda obtidas por lote/execução**.

Usar decimal para não limitar o domínio a rendimentos inteiros.

Regra:

~~~text
Rendimento > 0
~~~

Precisão recomendada:

~~~text
decimal(18,6)
~~~

Exemplos válidos:

~~~text
1
4
12
2,5
~~~

Não arredondar o Rendimento para inteiro.

Na UI:

~~~text
Rendimento do lote (unidades de venda)
~~~

### TempoAtivoMinutos

Aplicar RN013.

Representa o tempo de trabalho humano ativo necessário para executar o lote.

Usar inteiro em minutos.

Regra:

~~~text
TempoAtivoMinutos >= 0
~~~

O campo é obrigatório no formulário.

Valor zero é permitido quando o processo realmente não possuir tempo ativo de trabalho; ausência do campo é inválida e não deve ser convertida silenciosamente em zero.

Na UI:

~~~text
Tempo ativo de trabalho (minutos)
~~~

A UC013 apenas registra esse dado. O custo de mão de obra pertence ao UC020.

## Criação e atualização

A mesma página atende criação e alteração da base.

Comportamentos de domínio sugeridos:

~~~csharp
FichaTecnica.Criar(empresaId, produtoId, rendimento, tempoAtivoMinutos)
ficha.AtualizarBase(rendimento, tempoAtivoMinutos)
~~~

ou nomes equivalentes claros.

### Atualização atômica

AtualizarBase deve:

1. validar todos os valores candidatos;
2. somente depois alterar propriedades;
3. preservar Id, EmpresaId e ProdutoId.

Uma tentativa inválida não pode deixar a entidade parcialmente modificada em memória.

## Produto ativo ou inativo

Produto ativo e Produto inativo podem ter a Ficha Técnica criada ou atualizada.

Justificativa:

- UC009 já permite manutenção cadastral de Produto inativo;
- UC010 define inatividade como situação operacional/comercial, não congelamento cadastral;
- a Ficha pode ser preparada para futura reativação;
- alterar a Ficha não reativa o Produto.

A página deve mostrar a Situação atual do Produto.

## Ficha sem itens

É válido existir Ficha Técnica somente com Rendimento e TempoAtivoMinutos após a UC013.

A ausência de itens:

- não impede salvar a base;
- não equivale a custo zero;
- não torna a precificação completa;
- será tratada pelos UCs seguintes.

UC014 introduzirá itens.

## Multiempresa e segurança

Preservar FT002 integralmente.

Adicionar Global Query Filter para FichaTecnica.

O guard central de SaveChanges/SaveChangesAsync deve protegê-la por implementar IEntidadeEmpresa.

Além disso, a persistência deve rejeitar FichaTecnica cujo ProdutoId pertença a Empresa diferente da própria Ficha.

### GET

Produto inexistente ou de outro tenant:

~~~text
HTTP 404
~~~

### POST

Produto inexistente ou de outro tenant:

~~~text
HTTP 404
~~~

POST cross-tenant não pode revelar existência do Produto e não pode criar/alterar Ficha em outro tenant.

### Manipulação do request

Não bindar/confiar em:

- EmpresaId;
- ProdutoId;
- Id da Ficha.

O id da rota identifica o Produto; ownership e vínculo são resolvidos no servidor.

Campos extras manipulados não podem mudar Empresa/Produto da Ficha.

## Persistência

Criar tabela:

~~~text
FichasTecnicas
~~~

Campos:

- Id;
- EmpresaId;
- ProdutoId;
- Rendimento;
- TempoAtivoMinutos.

FKs:

~~~text
FichasTecnicas.EmpresaId -> Empresas.Id   RESTRICT
FichasTecnicas.ProdutoId -> Produtos.Id   RESTRICT
~~~

Índice único:

~~~text
(EmpresaId, ProdutoId)
~~~

Adicionar:

- DbSet<FichaTecnica>;
- configuration;
- Global Query Filter;
- validação de referência Produto/Empresa no caminho de persistência, seguindo o padrão de proteção tenant-aware já existente.

### Migration

Criar migration evolutiva.

Nome sugerido:

~~~text
AddFichasTecnicas
~~~

Não editar migrations históricas.

Atualizar ModelSnapshot apenas pelo fluxo normal do EF Core.

A migration deve:

- funcionar em banco vazio;
- funcionar sobre banco atual com Produtos existentes;
- não criar Fichas retroativas automaticamente;
- preservar todos os dados existentes.

## Web

Criar Razor Page:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

O id identifica o Produto.

A pasta /Produtos já é protegida pela policy EmpresaAtiva.

### GET — Produto sem Ficha

Carregar Produto com Global Query Filter.

Exibir resumo somente leitura:

- Nome;
- Categoria;
- Situação.

Exibir formulário vazio para:

- Rendimento do lote;
- Tempo ativo de trabalho.

Não criar automaticamente Ficha no GET.

### GET — Produto com Ficha

Exibir o mesmo formulário preenchido com os valores atuais.

Não carregar/mostrar itens, perdas, custos ou equipamentos.

### POST

Fluxo:

1. rebuscar Produto pelo id com Global Query Filter;
2. se ausente, 404;
3. validar InputModel;
4. consultar FichaTecnica existente do mesmo Produto;
5. se não existir, criar usando EmpresaId/ProdutoId resolvidos pelo servidor;
6. se existir, chamar AtualizarBase;
7. salvar;
8. PRG para a própria página da Ficha;
9. exibir mensagem exata:

~~~text
Ficha técnica salva com sucesso.
~~~

Não implementar endpoint separado “Criar” versus “Editar”; a tela representa a definição/manutenção da base atual.

### Navegação

Em:

~~~text
/Produtos/Detalhes/{id}
~~~

adicionar ação:

~~~text
Ficha técnica
~~~

para Produtos ativos e inativos.

Na página da Ficha adicionar:

~~~text
Voltar para o produto
~~~

## Antiforgery

POST usa antiforgery padrão do Razor Pages.

Não desabilitar antiforgery.

POST sem token válido deve ser rejeitado e não alterar dados.

GET nunca cria nem modifica Ficha.

## Regras de negócio aplicáveis

- RN009 — Rendimento;
- RN013 — Mão de obra / TempoAtivoMinutos;
- RN017 — Precificação incompleta;
- RN018 — Situação do Produto;
- RN035 — Propriedade por Empresa;
- RN036 — Isolamento de Empresa;
- RN047 — Ficha Técnica única por Produto e Empresa.

## Critérios de aceitação

### CA01 — Acesso protegido

Usuário anônimo não acessa o fluxo.

Usuário autenticado sem Empresa Ativa não obtém acesso operacional.

### CA02 — Produto sem Ficha pode definir base

GET de Produto válido sem Ficha exibe resumo do Produto e formulário de Rendimento/Tempo ativo, sem criar registro.

### CA03 — Criação válida

POST com Rendimento > 0 e TempoAtivoMinutos >= 0 cria uma única Ficha vinculada ao Produto e Empresa corretos.

### CA04 — Rendimento decimal

Rendimento aceita valor decimal positivo e o persiste sem conversão para inteiro.

### CA05 — Rendimento inválido

Campo ausente, zero ou negativo é rejeitado sem persistência.

### CA06 — Tempo ativo obrigatório

Campo ausente ou negativo é rejeitado.

Zero é aceito quando explicitamente informado.

### CA07 — Consulta da base existente

GET após criação apresenta Rendimento e TempoAtivoMinutos atuais.

### CA08 — Atualização mantém a mesma Ficha

POST para Produto que já possui Ficha atualiza Rendimento/TempoAtivoMinutos no mesmo registro, preservando Id, EmpresaId e ProdutoId.

### CA09 — Atualização de domínio atômica

Valor inválido não deixa alteração parcial em memória nem persiste mudança parcial.

### CA10 — Uma Ficha por Produto

A persistência rejeita segunda Ficha para o mesmo EmpresaId + ProdutoId.

### CA11 — Mesmo Produto não pode ser ligado a Ficha de outro tenant

Referência Produto/Empresa inconsistente é rejeitada pela camada de persistência.

### CA12 — Query Filter isola Fichas

Empresa A não consulta Ficha da Empresa B no fluxo comum.

### CA13 — Produto inexistente

GET e POST retornam 404.

### CA14 — Cross-tenant

GET e POST para Produto de outra Empresa retornam 404 e não criam/alteram Ficha.

### CA15 — Request não controla ownership/vínculo

EmpresaId, ProdutoId ou Id de Ficha manipulados no request não alteram vínculo ou ownership.

### CA16 — Produto inativo aceita Ficha

Produto inativo pode criar/atualizar Ficha e permanece inativo.

### CA17 — Navegação

Detalhes de Produto ativo ou inativo possui ação Ficha técnica; a página da Ficha retorna ao mesmo Produto.

### CA18 — PRG e mensagem

POST válido redireciona para /Produtos/FichaTecnica/{id} e exibe:

~~~text
Ficha técnica salva com sucesso.
~~~

### CA19 — Antiforgery

POST sem token válido é rejeitado e não cria/altera Ficha.

### CA20 — GET não muta

GET de Produto sem Ficha não cria registro; GET de Produto com Ficha não altera seus valores.

### CA21 — Migration evolutiva

Migration cria a nova estrutura e preserva Produtos existentes sem gerar Fichas automáticas.

### CA22 — Sem escopo antecipado

Não implementar:

- itens/quantidades de Insumos;
- observação contextual de item;
- perdas;
- equipamento;
- TempoForno;
- PotenciaFornoKw;
- energia;
- cálculo de mão de obra;
- custo de itens/lote/unidade;
- Preço sugerido;
- Preço de prateleira;
- histórico/versionamento de Ficha;
- exclusão de Ficha.

## Matriz de testes fechada antes da implementação

### Unitários — FichaTecnica

#### U1

~~~text
CA03_Criar_ficha_valida_preserva_empresa_produto_rendimento_e_tempo
~~~

#### U2

~~~text
CA05_Rendimento_deve_ser_maior_que_zero
~~~

Theory com zero e negativo.

#### U3

~~~text
CA06_Tempo_ativo_aceita_zero_e_rejeita_negativo
~~~

#### U4

~~~text
CA08_Atualizar_base_preserva_id_empresa_e_produto
~~~

#### U5

~~~text
CA09_Atualizacao_invalida_nao_altera_estado_anterior
~~~

#### U6

~~~text
CA03_Ids_de_empresa_e_produto_devem_ser_positivos
~~~

### Integração — persistência

#### P1

~~~text
CA21_Migration_cria_fichas_e_preserva_produtos_existentes
~~~

Migrar SQLite até o estado anterior, inserir Produto e aplicar AddFichasTecnicas.

#### P2

~~~text
CA10_Indice_unico_rejeita_segunda_ficha_do_mesmo_produto
~~~

#### P3

~~~text
CA12_Query_filter_isola_fichas_por_empresa
~~~

#### P4

~~~text
CA11_Guard_rejeita_ficha_referenciando_produto_de_outra_empresa
~~~

#### P5

~~~text
CA21_Fks_restrict_preservam_coerencia_de_empresa_e_produto
~~~

#### P6

~~~text
CA08_Atualizacao_round_trip_preserva_a_mesma_ficha
~~~

### Integração — Web

#### W1

~~~text
CA01_Ficha_tecnica_exige_autenticacao_e_empresa_ativa
~~~

#### W2

~~~text
CA02_CA20_Get_sem_ficha_exibe_formulario_sem_criar_registro
~~~

#### W3

~~~text
CA03_CA04_CA18_Post_valido_cria_ficha_e_faz_PRG_com_sucesso
~~~

Usar Rendimento decimal.

#### W4

~~~text
CA07_Get_com_ficha_carrega_valores_atuais
~~~

#### W5

~~~text
CA08_Post_em_ficha_existente_atualiza_mesmo_registro
~~~

Confirmar Id preservado.

#### W6

~~~text
CA05_CA06_Post_invalido_nao_cria_nem_altera_ficha
~~~

Theory:

- Rendimento ausente;
- Rendimento zero;
- Rendimento negativo;
- Tempo ativo ausente;
- Tempo ativo negativo.

#### W7

~~~text
CA13_Get_e_post_de_produto_inexistente_retornam_404
~~~

#### W8

~~~text
CA14_Get_e_post_cross_tenant_retornam_404_sem_alteracao
~~~

#### W9

~~~text
CA15_Request_nao_controla_empresa_produto_ou_id_da_ficha
~~~

#### W10

~~~text
CA16_Produto_inativo_pode_salvar_ficha_sem_reativacao
~~~

#### W11

~~~text
CA17_Detalhes_exibe_ficha_tecnica_para_ativo_e_inativo
~~~

#### W12

~~~text
CA19_Post_sem_antiforgery_e_rejeitado_sem_alterar_ficha
~~~

## Impacto nos UCs seguintes

### UC014

Recebe uma FichaTecnica existente ou pode exigir que o usuário defina a base antes de adicionar itens.

O UC014 deve manter o gate já previsto para estabilidade cadastral do Insumo quando ele passa a ser referenciado por Ficha Técnica.

### UC020

Usará TempoAtivoMinutos para:

~~~text
CustoMaoDeObraLote =
(TempoAtivoMinutos / 60) × ValorHoraTrabalhoDaEmpresa
~~~

UC013 não executa esse cálculo.

### UC022

Usará Rendimento para:

~~~text
CustoUnitarioProduto = CustoLote / Rendimento
~~~

UC013 não executa esse cálculo.

### Histórico comercial

Mudanças posteriores na Ficha afetam apenas cálculos correntes.

Quando UC011 existir, registros comerciais já gravados manterão seus snapshots e não serão recalculados retroativamente por mudanças na Ficha.

## Fora do escopo

- UC014–UC017;
- Insumos da Ficha;
- quantidade/observação de item;
- perdas;
- equipamentos e usos;
- energia;
- configurações da Empresa;
- cálculos de custo;
- cálculo de mão de obra;
- preço teórico/sugerido;
- preço de prateleira;
- margem atual;
- histórico/versionamento da Ficha;
- exclusão da Ficha;
- preparações intermediárias reutilizáveis;
- API REST;
- refatorações oportunistas.

## Definition of Done específica

Além da DoD global:

- FichaTecnica tenant-owned criada;
- uma Ficha por Produto protegida no banco;
- Rendimento decimal > 0;
- TempoAtivoMinutos obrigatório e >= 0;
- criação/atualização usam validação de domínio;
- atualização é atômica;
- EmpresaId/ProdutoId imutáveis após criação;
- DbSet/configuração/query filter implementados;
- referência Ficha/Produto cross-tenant protegida na persistência;
- FKs Restrict;
- migration AddFichasTecnicas ou equivalente criada normalmente;
- nenhuma migration histórica alterada;
- /Produtos/FichaTecnica/{id:int} funcional;
- GET não muta;
- POST usa antiforgery;
- criação e edição usam a mesma Ficha atual;
- Produto inativo pode manter Ficha sem reativação;
- Detalhes possui ação Ficha técnica;
- PRG + mensagem exata;
- nenhum item/perda/equipamento/cálculo antecipado;
- F003, RN009/RN013/RN047, catálogo e ordem coerentes;
- UC014 passa a próximo caso;
- matriz U1-U6, P1-P6 e W1-W12 atendida;
- build Release sem warnings novos relevantes;
- suíte completa verde.
