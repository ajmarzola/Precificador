# UC010 — Desativar e reativar produto

- **Status:** Revalidado pós-UC009 — liberado para implementação
- **Funcionalidade:** F002 — Gestão de Produtos
- **Dependências materiais:** UC007, UC008 e UC009 implementados
- **Sequenciamento:** implementar a partir da master pós-UC009, já revalidada
- **Próximo caso relacionado:** UC011 — Alterar preço de venda preservando histórico
- **Sem alteração de schema:** o campo Ativo já existe em Produto

## Objetivo

Permitir alterar a situação operacional de um Produto da Empresa Ativa entre **Ativo** e **Inativo**, sem exclusão física e sem permitir alteração arbitrária do campo Ativo por binding.

O UC010 implementa o ciclo reversível:

~~~text
Ativo -> Desativar -> Inativo
Inativo -> Reativar -> Ativo
~~~

O Produto permanece existente, consultável e participante da unicidade por Empresa + Nome normalizado.

## Decisão funcional

A desativação de Produto é **reversível**.

Embora o catálogo anterior tenha abreviado o UC010 como "Desativar produto", documentos anteriores da área de Produto já reservavam para o UC010 a introdução de **desativação/reativação**.

Este UC formaliza esse comportamento.

## Regra de negócio — RN018

Aplicar e completar RN018:

- Produto é desativado, não excluído fisicamente;
- desativação é reversível;
- Produto inativo continua visível em listagem e detalhes;
- Produto inativo continua participando da identidade/unicidade;
- Produto inativo continua editável nos campos cadastrais permitidos pelo UC009;
- reativar devolve o Produto ao estado operacional ativo;
- desativar/reativar não altera Nome, Categoria, Margem-alvo, Empresa ou Id;
- regras futuras de uso de Produto inativo em preço de venda, Ficha Técnica, cálculo ou outros fluxos devem ser definidas pelos respectivos UCs, não por esta entrega.

## Edição de Produto inativo

O comportamento normativo deste UC é:

~~~text
Produto inativo continua editável pelo UC009.
~~~

A inatividade representa indisponibilidade operacional/comercial, não congelamento cadastral.

Portanto:

- a ação Editar continua disponível em detalhes;
- GET /Produtos/Editar/{id} continua acessível para Produto inativo;
- POST de edição continua permitido conforme as regras do UC009;
- a edição não reativa o Produto implicitamente;
- Ativo permanece false após uma edição válida de Produto inativo.

### Revalidação pós-UC009 — concluída

A UC009 foi implementada, revisada e mergeada antes desta liberação. A revalidação contra a master real confirmou:

- rota real de edição: `/Produtos/Editar/{id:int}`;
- Detalhes mantém o link **Editar** sem condicionar a ação ao valor de `Ativo`;
- GET de edição consulta `context.Produtos` com Global Query Filter e sem filtro por situação;
- POST de edição consulta `context.Produtos` com Global Query Filter e sem filtro por situação;
- `Produto.AtualizarDados(...)` altera somente Nome, NomeNormalizado, Categoria e MargemAlvo, preservando `Ativo`;
- o POST válido da UC009 usa `TempData["MensagemSucesso"]` e PRG para `/Produtos/Detalhes/{id}`;
- os testes Web reais da UC009 já possuem infraestrutura para GET/POST, antiforgery, tenant e validação de persistência;
- após UC010 adicionar `Produto.Desativar()`, W6 pode preparar um Produto inativo pelo próprio domínio e persistência normal, sem SQL direto, reflection, setter artificial ou outro bypass técnico.

Não foi encontrada divergência material que exija alterar o comportamento especificado da UC010.

**CA10/W6 está confirmado e executável. UC010 está liberada para implementação.**

## UX

Não criar nova Razor Page.

Usar a página existente:

~~~text
/Produtos/Detalhes/{id:int}
~~~

### Produto ativo

Exibir:

~~~text
Desativar
~~~

Não exibir Reativar.

### Produto inativo

Exibir:

~~~text
Reativar
~~~

Não exibir Desativar.

Nunca exibir as duas ações ao mesmo tempo.

A situação atual continua visível em Detalhes e na listagem.

## Confirmação

A desativação deve solicitar confirmação simples antes do POST, usando mecanismo nativo já aceito no projeto.

Texto:

~~~text
Deseja desativar este produto?
~~~

Não criar modal customizado nem dependência JavaScript adicional.

A reativação não exige confirmação.

## Domínio

Adicionar comportamentos explícitos em Produto:

~~~csharp
public void Desativar()
public void Reativar()
~~~

Não expor setter público de Ativo.

### Semântica

Desativar:

~~~text
Ativo = false
~~~

Reativar:

~~~text
Ativo = true
~~~

### Idempotência

As operações devem ser idempotentes:

- Desativar Produto já inativo não lança exceção;
- Reativar Produto já ativo não lança exceção;
- chamadas repetidas terminam no estado solicitado.

Nenhum outro campo da entidade deve ser alterado.

## Identidade e unicidade

A situação Ativo/Inativo **não participa da identidade**.

A identidade continua:

~~~text
EmpresaId + NomeNormalizado
~~~

Consequências:

- Produto inativo continua ocupando seu Nome na Empresa;
- desativar não permite cadastrar outro Produto com o mesmo Nome normalizado na mesma Empresa;
- reativar não exige validação nova de duplicidade;
- índice único existente permanece suficiente;
- nenhuma migration é necessária.

## Listagem e detalhes

O UC008 já possui apresentação da situação.

UC010 deve adicionar a regressão que ficou propositalmente pendente:

### Listagem

Produto inativo:

- continua aparecendo;
- exibe Situação = Inativo;
- continua com ação Consultar.

### Detalhes

Produto inativo:

- continua acessível;
- exibe Situação = Inativo;
- exibe Reativar;
- não exibe Desativar;
- mantém Editar após UC009.

Produto ativo:

- exibe Situação = Ativo;
- exibe Desativar;
- não exibe Reativar.

## Web — handlers de mutação

Usar POST handlers na página de Detalhes.

Nomes sugeridos:

~~~csharp
OnPostDesativarAsync(int id)
OnPostReativarAsync(int id)
~~~

ou nomes equivalentes claros.

Não criar formulário que envie:

~~~text
Ativo=true
Ativo=false
~~~

como decisão do cliente.

## POST — Desativar

Fluxo:

1. receber id da rota;
2. buscar novamente o Produto usando Global Query Filter;
3. se não encontrado, 404;
4. chamar Produto.Desativar();
5. SaveChangesAsync();
6. definir mensagem de sucesso;
7. PRG para Detalhes do mesmo Produto.

Mensagem exata:

~~~text
Produto desativado com sucesso.
~~~

## POST — Reativar

Fluxo:

1. receber id da rota;
2. buscar novamente o Produto usando Global Query Filter;
3. se não encontrado, 404;
4. chamar Produto.Reativar();
5. SaveChangesAsync();
6. definir mensagem de sucesso;
7. PRG para Detalhes do mesmo Produto.

Mensagem exata:

~~~text
Produto reativado com sucesso.
~~~

## Multiempresa e segurança

Preservar FT002 integralmente.

Nos handlers:

- Global Query Filter permanece ativo;
- não usar IgnoreQueryFilters;
- não aceitar EmpresaId do request;
- não aceitar booleano Ativo do request;
- inexistente e cross-tenant são indistinguíveis;
- id inexistente => HTTP 404;
- id de Produto de outro tenant => HTTP 404;
- guard central de escrita continua ativo.

## Segurança HTTP

Mudança de situação:

- somente por POST;
- antiforgery padrão do Razor Pages;
- GET nunca altera estado;
- POST sem token válido deve ser rejeitado;
- estado deve permanecer intacto quando antiforgery falhar.

## PRG

Desativar e Reativar usam Post/Redirect/Get.

Após a mutação:

~~~text
POST handler
    -> redirect
GET /Produtos/Detalhes/{id}
~~~

Detalhes exibe a mensagem via TempData.

A implementação deve reutilizar o mecanismo de mensagem existente após UC009, se houver.

## Sem exclusão física

Não implementar:

- DELETE;
- remoção da linha;
- cascade delete;
- botão Excluir;
- hard delete;
- soft-delete com coluna adicional.

O campo Ativo existente é suficiente.

## Sem metadados adicionais

UC010 não introduz:

- DataDesativacao;
- MotivoDesativacao;
- UsuarioDesativacao;
- histórico de status;
- auditoria.

Se esses requisitos surgirem, pertencem a melhoria/UC específico.

## Relação com UC011+

UC010 não define ainda se Produto inativo pode:

- receber novo preço de venda;
- ter preço futuro registrado;
- ser adicionado a Ficha Técnica;
- participar de novas composições;
- aparecer em seletores operacionais futuros.

Essas regras devem ser fechadas nos UCs correspondentes quando os fluxos existirem.

Não antecipar bloqueios ou permissões inexistentes.

## Critérios de aceitação

### CA01 — Produto ativo exibe Desativar

Nos detalhes de Produto ativo:

- Situação = Ativo;
- Desativar disponível;
- Reativar ausente.

### CA02 — Produto inativo exibe Reativar

Nos detalhes de Produto inativo:

- Situação = Inativo;
- Reativar disponível;
- Desativar ausente.

### CA03 — Desativação altera somente situação

Ao desativar:

- Ativo == false;
- Id preservado;
- EmpresaId preservado;
- Nome preservado;
- NomeNormalizado preservado;
- Categoria preservada;
- MargemAlvo preservada.

### CA04 — Reativação altera somente situação

Ao reativar:

- Ativo == true;
- demais dados preservados.

### CA05 — Operações idempotentes

Chamadas repetidas de Desativar/Reativar não lançam exceção e mantêm o estado solicitado.

### CA06 — Desativação usa PRG

POST válido:

- persiste Ativo=false;
- redireciona para Detalhes;
- exibe "Produto desativado com sucesso.".

### CA07 — Reativação usa PRG

POST válido:

- persiste Ativo=true;
- redireciona para Detalhes;
- exibe "Produto reativado com sucesso.".

### CA08 — Inativo continua na listagem

Produto inativo continua aparecendo em /Produtos com Situação = Inativo.

### CA09 — Inativo continua consultável

/Produtos/Detalhes/{id} continua acessível e apresenta Situação = Inativo.

### CA10 — Inativo continua editável

Após UC009 implementado:

- Detalhes de Produto inativo mantém ação Editar;
- GET de edição continua acessível;
- edição válida não reativa o Produto.

Este critério exige revalidação contra a implementação real do UC009 antes de liberar UC010 para código.

### CA11 — Inativo continua ocupando Nome

Produto inativo continua participando da unicidade EmpresaId + NomeNormalizado.

Cadastrar outro Produto com o mesmo Nome normalizado na mesma Empresa continua sendo rejeitado.

### CA12 — Id inexistente

POST Desativar e POST Reativar para id inexistente retornam 404.

### CA13 — Cross-tenant

POST Desativar/Reativar para Produto de outra Empresa:

- retorna 404;
- não altera o registro.

### CA14 — Antiforgery

POST sem token antiforgery válido é rejeitado e não altera Ativo.

### CA15 — GET não muta

Nenhum GET deve desativar ou reativar Produto.

### CA16 — Sem exclusão física

Nenhuma ação remove Produto fisicamente.

### CA17 — Sem schema

Nenhuma migration ou ModelSnapshot é alterado.

### CA18 — Sem escopo antecipado

Não implementar:

- preço/histórico de venda;
- Ficha Técnica;
- custo;
- regras de elegibilidade futura de Produto inativo;
- filtros por situação;
- exclusão;
- auditoria.

## Matriz de testes fechada

### Unitários — Produto

#### U1

~~~text
CA03_Desativar_altera_apenas_status_e_preserva_demais_dados
~~~

#### U2

~~~text
CA04_Reativar_altera_apenas_status_e_preserva_demais_dados
~~~

#### U3

~~~text
CA05_Desativar_e_reativar_sao_idempotentes
~~~

### Integração — persistência

#### P1

~~~text
CA03_CA04_Alteracoes_de_status_persistem_no_sqlite
~~~

Validar round-trip:

~~~text
ativo -> inativo -> ativo
~~~

#### P2

~~~text
CA11_Produto_inativo_continua_participando_do_indice_unico
~~~

Cenário:

1. criar Produto;
2. desativar;
3. persistir;
4. tentar inserir outro Produto com mesmo EmpresaId + NomeNormalizado;
5. banco rejeita pela constraint existente.

### Integração — Web

#### W1

~~~text
CA01_Detalhes_de_produto_ativo_exibe_desativar_e_nao_reativar
~~~

#### W2

~~~text
CA02_Detalhes_de_produto_inativo_exibe_reativar_e_nao_desativar
~~~

#### W3

~~~text
CA06_Post_desativar_persiste_status_faz_PRG_e_exibe_sucesso
~~~

#### W4

~~~text
CA07_Post_reativar_persiste_status_faz_PRG_e_exibe_sucesso
~~~

#### W5

~~~text
CA08_CA09_Produto_inativo_permanece_na_listagem_e_detalhes
~~~

O teste deve provar a linha correta na listagem, não apenas procurar "Inativo" globalmente.

#### W6

~~~text
CA10_Produto_inativo_permanece_editavel_sem_reativacao_implicita
~~~

Executar usando o fluxo real da UC009 já revalidado.

Preparação normativa do cenário:

1. criar Produto normalmente;
2. desativá-lo por `Produto.Desativar()`, introduzido pela própria UC010;
3. persistir pelo DbContext;
4. confirmar link Editar em Detalhes;
5. confirmar GET Editar permitido;
6. executar POST válido de edição;
7. confirmar que `Ativo` permanece `false`.

Não usar SQL direto, reflection, setter artificial ou alteração manual de tracking para fabricar o estado inativo.

#### W7

~~~text
CA12_Post_de_id_inexistente_retorna_404
~~~

Cobrir Desativar e Reativar.

#### W8

~~~text
CA13_Post_cross_tenant_retorna_404_e_preserva_status
~~~

Preparar estados apropriados e confirmar registro intacto.

#### W9

~~~text
CA14_Post_sem_antiforgery_e_rejeitado_sem_alterar_status
~~~

Não enfraquecer antiforgery para facilitar o teste.

#### W10

~~~text
CA15_Get_nao_altera_situacao
~~~

Confirmar que navegação/consulta normal não altera Ativo.

## Migration

Nenhuma migration.

Não alterar:

- migration AddProdutos;
- migrations históricas;
- ProdutoConfiguration;
- PrecificadorDbContextModelSnapshot.

Se surgir necessidade de schema, interromper e reavaliar.

## Fora do escopo

- exclusão física;
- motivo/data de desativação;
- auditoria;
- histórico de situação;
- filtro por Situação;
- bulk actions;
- preço de venda;
- histórico de preço;
- Ficha Técnica;
- custo;
- margem atual;
- preço sugerido;
- regras futuras de uso de Produto inativo;
- API REST;
- refatorações oportunistas sem relação com o UC.

## Gate obrigatório antes da implementação — concluído

O gate foi concluído contra a master real pós-UC009.

Confirmado:

1. UC009 implementada, revisada e mergeada;
2. rota real de edição compatível com a especificação;
3. navegação real de Detalhes mantém Editar;
4. GET/POST de edição não bloqueiam Produto inativo;
5. edição preserva `Ativo`;
6. TempData/PRG existente pode ser reutilizado;
7. CA10/W6 pode ser testado sem bypass técnico;
8. nenhuma divergência material exige mudança funcional ou de schema;
9. a instrução executável do Codex foi criada em `docs/codex/UC010-desativar-reativar-produto.md`.

**Liberação:** criar a implementação em `feat/uc010-situacao-produto`, partindo da master que contenha esta revalidação.

## Definition of Done específica

Além da DoD global:

- domínio possui Desativar() e Reativar();
- nenhum setter público de Ativo;
- operações são idempotentes;
- detalhes exibe somente ação coerente com o estado;
- desativação pede confirmação simples;
- mutações usam POST + antiforgery;
- GET não altera estado;
- handlers usam Global Query Filter;
- cross-tenant/inexistente => 404;
- PRG + mensagens exatas;
- inativos permanecem listados e consultáveis;
- inativos permanecem editáveis sem reativação implícita, conforme CA10/W6 revalidado;
- unicidade continua incluindo inativos;
- nenhuma migration/snapshot;
- nenhuma exclusão física;
- nenhuma regra antecipada de UC011+;
- matriz U1-U3, P1-P2 e W1-W10 atendida;
- documentação pós-implementação marca UC010 como Implementado;
- F002, RN018, catálogo e ordem ficam coerentes;
- UC011 passa a próximo caso de Produtos;
- build Release sem warnings novos relevantes;
- suíte completa verde.
