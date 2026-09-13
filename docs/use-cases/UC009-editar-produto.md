# UC009 — Editar produto

- **Status:** Implementado
- **Funcionalidade:** F002 — Gestão de Produtos
- **Dependência material:** UC007 implementado
- **Sequenciamento:** implementar após UC008 implementado, revisado e mergeado
- **Próximo caso relacionado:** UC010 — Desativar e reativar produto
- **Sem alteração de schema:** este UC altera comportamento/domínio e UI, não estrutura persistida

## Objetivo

Permitir editar os dados cadastrais e estratégicos atuais de um **Produto da Empresa Ativa** sem alterar sua propriedade, situação ou introduzir qualquer conceito de preço de venda, Ficha Técnica ou custo.

O UC009 edita somente:

1. Nome;
2. Categoria;
3. Margem-alvo.

A edição preserva o mesmo Produto.Id.

## Estado atual revalidado

Na master pós-UC008:

- Produto é tenant-owned via IEntidadeEmpresa;
- possui Id, EmpresaId, Nome, NomeNormalizado, Categoria, MargemAlvo e Ativo;
- Produtos são isolados por Global Query Filter;
- existe índice único (EmpresaId, NomeNormalizado);
- /Produtos e /Produtos/Detalhes/{id:int} já existem;
- /Produtos já está protegido por EmpresaAtiva;
- Produto ainda não possui desativação/reativação;
- não existe preço de venda, histórico de venda, Ficha Técnica ou custo de Produto.

Este UC deve evoluir esse modelo, não substituí-lo.

## Campos editáveis

### Nome

Editar conforme RN041 e RN042:

- obrigatório;
- máximo 120 caracteres após normalização;
- remover whitespace externo;
- reduzir sequências internas de whitespace a um único espaço;
- preservar capitalização de exibição;
- recalcular NomeNormalizado em maiúsculas invariantes;
- preservar acentos;
- manter unicidade por EmpresaId + NomeNormalizado.

Renomear mantém o mesmo Produto.Id.

### Categoria

Editar conforme RN043:

- opcional;
- máximo 80 caracteres após normalização;
- trim;
- colapso de whitespace;
- whitespace-only => null;
- preservar capitalização;
- não participa da unicidade.

### Margem-alvo

Editar conforme RN019/RN045.

Na UI:

~~~text
Margem-alvo (%)
~~~

No domínio/persistência:

~~~text
30    -> 0,30
25,5  -> 0,255
0     -> 0
~~~

Regra:

~~~text
0 <= MargemAlvo < 1
~~~

A edição da margem-alvo atualiza o valor corrente do Produto.

O UC009 não cria histórico de margem-alvo.

## Campos não editáveis

O formulário não deve permitir alterar:

- Id;
- EmpresaId;
- NomeNormalizado diretamente;
- Ativo;
- preço de venda;
- custo;
- Ficha Técnica;
- rendimento;
- qualquer campo de UC010+.

EmpresaId continua vindo exclusivamente do contexto de Empresa e não do request.

Ativo continua fora deste UC. UC010 introduzirá desativação e reativação.

## Domínio

Adicionar comportamento ao agregado existente, sugerido:

~~~csharp
produto.AtualizarDados(nome, margemAlvo, categoria)
~~~

ou assinatura equivalente clara.

A atualização deve:

1. normalizar todos os valores recebidos em variáveis locais;
2. validar todos os valores;
3. somente depois atribuir as propriedades;
4. atualizar Nome, NomeNormalizado, Categoria e MargemAlvo;
5. preservar Id, EmpresaId e Ativo.

### Atomicidade da atualização de domínio

Uma atualização inválida não pode deixar o objeto parcialmente alterado em memória.

Exemplo:

- Nome válido;
- Categoria válida;
- MargemAlvo inválida.

O domínio deve lançar a exceção de validação antes de modificar Nome/Categoria.

Esse contrato deve ser provado em teste unitário.

## Unicidade

A identidade continua:

~~~text
EmpresaId + NomeNormalizado
~~~

### Mesmo registro

Salvar sem alterar o Nome, ou alterando apenas capitalização/whitespace que normalize para o mesmo NomeNormalizado, não é duplicidade do próprio registro.

A consulta funcional de duplicidade deve excluir:

~~~text
Id != produtoEditado.Id
~~~

### Outro Produto da mesma Empresa

Se outro Produto da Empresa Ativa já possuir o mesmo NomeNormalizado, rejeitar com a mensagem exata:

~~~text
Já existe um produto cadastrado com esse nome.
~~~

Nenhum campo deve ser persistido.

### Outra Empresa

Produto de outra Empresa com o mesmo Nome normalizado não bloqueia a edição.

### Integridade do banco

O índice único criado no UC007 permanece como última barreira.

Não converter qualquer DbUpdateException genericamente em duplicidade.

Se ocorrer uma DbUpdateException inesperada após a pré-validação funcional, preservar o comportamento técnico de erro/log vigente em vez de mascarar a causa.

## Multiempresa e segurança

Todas as leituras/escritas comuns usam o Global Query Filter existente.

Não usar IgnoreQueryFilters no fluxo Web de edição.

### GET

Produto inexistente ou pertencente a outro tenant:

~~~text
HTTP 404
~~~

### POST

Produto inexistente ou pertencente a outro tenant:

~~~text
HTTP 404
~~~

O POST cross-tenant:

- não revela se o Produto existe em outra Empresa;
- não altera qualquer dado do outro tenant.

### Manipulação do request

Campos extras como:

~~~text
EmpresaId=2
Input.EmpresaId=2
Ativo=false
Input.Ativo=false
~~~

devem ser ignorados/não bindados.

Após POST válido:

- EmpresaId permanece o original;
- Ativo permanece o original.

## Web

Criar Razor Page:

~~~text
/Produtos/Editar/{id:int}
~~~

A pasta /Produtos já está protegida pela policy EmpresaAtiva; não criar mecanismo paralelo de autorização.

### InputModel

Reutilizar ProdutoInputModel existente, se ele continuar adequado:

- Nome;
- Categoria;
- MargemAlvoPercentual.

Não criar segundo InputModel idêntico.

### GET

Carregar por Global Query Filter e preferir AsNoTracking/projeção para o formulário.

Popular:

~~~text
Nome
Categoria
MargemAlvoPercentual = MargemAlvo * 100
~~~

Não exibir campos técnicos.

### POST

Fluxo esperado:

1. carregar Produto pelo id com Global Query Filter;
2. se ausente, 404;
3. validar campos obrigatórios do formulário;
4. converter percentual para fração;
5. executar atualização de domínio;
6. verificar duplicidade na Empresa Ativa excluindo o próprio Id;
7. persistir;
8. PRG para /Produtos/Detalhes/{id};
9. exibir mensagem de sucesso.

Mensagem exata:

~~~text
Produto atualizado com sucesso.
~~~

## Reuso do formulário Produto

Com UC009, Novo e Editar passam a possuir o mesmo conjunto de campos e validações básicas.

É permitido e esperado evitar duplicação real.

### Deve ser reutilizado

- ProdutoInputModel.

### Pode ser extraído localmente

Se Novo e Editar precisarem repetir:

- validação de Margem-alvo obrigatória;
- mapeamento de ArgumentException.ParamName para ModelState;
- mensagem de duplicidade;

pode ser criado helper específico da área, por exemplo:

~~~text
ProdutoFormulario
~~~

Não criar:

- framework genérico de formulários;
- base PageModel;
- serviço CRUD genérico;
- abstração compartilhada com Insumo apenas por semelhança superficial.

A refatoração não pode alterar o comportamento já entregue pelo UC007.

## Navegação

### Detalhes

Adicionar ação Editar apontando para:

~~~text
/Produtos/Editar/{id}
~~~

### Edição

Adicionar ação Cancelar retornando para:

~~~text
/Produtos/Detalhes/{id}
~~~

### Sucesso

Após edição válida:

~~~text
POST /Produtos/Editar/{id}
    -> redirect
GET /Produtos/Detalhes/{id}
~~~

Detalhes deve exibir:

~~~text
Produto atualizado com sucesso.
~~~

Para isso, adaptar Detalhes para ler TempData sem introduzir mutação adicional.

Não adicionar ação de desativação neste UC.

## Situação Ativo/Inativo

UC009 não implementa nem testa mudança de status.

Na master atual, Produto só pode nascer ativo.

Não usar SQL, reflection ou método artificial para fabricar Produto inativo.

A interação entre edição e Produto inativo deverá ser fechada no UC010 junto com a implementação legítima da desativação.

## Persistência

Nenhuma alteração de schema.

Não alterar:

- ProdutoConfiguration;
- migration AddProdutos;
- migrations históricas;
- PrecificadorDbContextModelSnapshot.

Se a implementação exigir migration, interromper e reavaliar o escopo.

## Critérios de aceitação

### CA01 — Acesso protegido

Usuário anônimo não acessa /Produtos/Editar/{id}.

Usuário autenticado sem Empresa Ativa não obtém acesso operacional.

### CA02 — GET carrega somente campos editáveis

GET válido exibe Nome, Categoria e Margem-alvo (%).

Não expõe EmpresaId, NomeNormalizado, Ativo, preço, custo ou Ficha Técnica.

### CA03 — Conversão de margem no GET

Produto com MargemAlvo = 0,255 popula MargemAlvoPercentual com o valor decimal equivalente a 25,5%, sem alterar a persistência. O teste não deve depender da pontuação decimal textual exata gerada pelo HTML/tag helper.

### CA04 — Edição válida

POST válido atualiza Nome/NomeNormalizado, Categoria e MargemAlvo e preserva Id, EmpresaId e Ativo.

### CA05 — Nome segue RN041

Nome vazio/whitespace ou >120 é rejeitado.

Nome válido é normalizado conforme RN041.

### CA06 — Categoria segue RN043

Categoria whitespace vira null.

Categoria válida é normalizada.

Categoria >80 é rejeitada.

### CA07 — Margem segue RN019/RN045

Aceitar 0% e valor válido menor que 100%.

Rejeitar campo ausente, valor negativo, 100% e valor >100%.

### CA08 — Atualização de domínio é atômica

Erro em qualquer campo não deixa os demais campos parcialmente alterados no objeto.

### CA09 — Próprio Nome não gera duplicidade

Salvar mantendo o mesmo Nome normalizado é permitido.

### CA10 — Duplicidade na mesma Empresa

Renomear para o Nome normalizado de outro Produto da mesma Empresa é rejeitado com:

~~~text
Já existe um produto cadastrado com esse nome.
~~~

Nenhuma alteração é persistida.

### CA11 — Mesmo Nome em outra Empresa

Mesmo Nome normalizado existente apenas em outro tenant não bloqueia edição.

### CA12 — Request não controla ownership/status

Campos manipulados de EmpresaId/Ativo não alteram ownership ou situação.

### CA13 — Id inexistente

GET e POST retornam 404.

### CA14 — Cross-tenant

GET e POST para Produto de outra Empresa retornam 404 e não alteram o registro.

### CA15 — PRG e mensagem

POST válido redireciona para Detalhes do mesmo Produto e exibe:

~~~text
Produto atualizado com sucesso.
~~~

### CA16 — Navegação

Detalhes possui ação Editar.

Edição possui Cancelar para Detalhes.

### CA17 — Sem histórico implícito

Editar margem, Nome ou Categoria não cria entidade/tabela de histórico.

### CA18 — Sem schema

Nenhuma migration ou ModelSnapshot é alterado.

### CA19 — Sem escopo antecipado

Não implementar UC010 desativação, preço/histórico de venda, Ficha Técnica, custo, margem atual, preço sugerido ou dashboard.

## Matriz de testes fechada antes da implementação

### Unitários — Produto

#### U1

~~~text
CA04_Atualizar_produto_valido_normaliza_campos_e_preserva_ownership_e_status
~~~

#### U2

~~~text
CA05_Atualizar_nome_valida_limite_e_normaliza
~~~

Cobrir vazio/whitespace, >120, whitespace interno e capitalização/acentos.

#### U3

~~~text
CA06_Atualizar_categoria_opcional_normaliza_e_valida_limite
~~~

Cobrir null/whitespace, válida e >80.

#### U4

~~~text
CA07_Atualizar_margem_aceita_intervalo_valido_e_rejeita_fora
~~~

Cobrir 0, valor válido próximo de 1, negativo e 1.

#### U5

~~~text
CA08_Atualizacao_invalida_nao_altera_estado_anterior
~~~

Provocar erro em campo validado após outros valores candidatos e confirmar que todas as propriedades permanecem com o estado anterior.

### Integração — persistência

#### P1

~~~text
CA04_Atualizacao_valida_persiste_campos_e_preserva_id_empresa_status
~~~

Round-trip com SQLite migrado.

#### P2

~~~text
CA10_Indice_unico_rejeita_renomeacao_para_nome_de_outro_produto_da_mesma_empresa
~~~

#### P3

~~~text
CA11_Mesmo_nome_normalizado_permanece_permitido_em_empresas_diferentes
~~~

### Integração — Web

#### W1

~~~text
CA01_Edicao_de_produto_exige_autenticacao_e_empresa_ativa
~~~

Cobrir anônimo e usuário autenticado sem Empresa Ativa.

#### W2

~~~text
CA02_CA03_Get_edicao_carrega_campos_permitidos_e_margem_percentual
~~~

Usar Produto com margem 0,255, provar semanticamente MargemAlvoPercentual = 25,5% e confirmar ausência dos campos proibidos, sem amarrar o teste à pontuação decimal textual do HTML.

#### W3

~~~text
CA04_CA15_Post_valido_atualiza_produto_e_faz_PRG_para_detalhes
~~~

Confirmar normalização, margem percentual -> fração, Categoria, EmpresaId, Ativo e mensagem de sucesso.

#### W4

~~~text
CA05_CA06_CA07_Post_invalido_nao_persiste_alteracoes
~~~

Theory com um erro por cenário:

- Nome vazio;
- Nome >120;
- Categoria >80;
- Margem ausente;
- Margem negativa;
- Margem 100;
- Margem >100.

#### W5

~~~text
CA09_Salvar_sem_mudar_nome_normalizado_nao_detecta_o_proprio_produto_como_duplicado
~~~

Pode alterar Categoria/Margem no mesmo cenário.

#### W6

~~~text
CA10_Renomear_para_outro_produto_da_mesma_empresa_exibe_duplicidade_e_nao_persiste
~~~

Usar variação de caixa/whitespace que normalize para o Nome já existente.

#### W7

~~~text
CA11_Nome_existente_apenas_em_outra_empresa_nao_bloqueia_edicao
~~~

#### W8

~~~text
CA12_Request_nao_controla_empresa_ou_status
~~~

Enviar campos extras manipulados de EmpresaId/Ativo e confirmar preservação.

#### W9

~~~text
CA13_Get_e_post_de_id_inexistente_retornam_404
~~~

#### W10

~~~text
CA14_Get_e_post_cross_tenant_retornam_404_sem_alterar_registro
~~~

#### W11

~~~text
CA16_Detalhes_exibe_editar_e_edicao_exibe_cancelar_para_o_mesmo_produto
~~~

## Migration

Nenhuma migration.

Não alterar ModelSnapshot.

## Fora do escopo

- UC010 — Desativar Produto;
- UC011 — Alterar preço de venda;
- UC012 — Histórico de preço de venda;
- Ficha Técnica;
- rendimento;
- itens;
- perdas;
- mão de obra;
- equipamentos;
- custo;
- margem atual;
- preço teórico/sugerido;
- histórico de alterações cadastrais;
- auditoria/versionamento do Produto;
- filtros/listagem adicionais;
- API.

## Definition of Done específica

Além da DoD global:

- Produto.AtualizarDados ou equivalente implementado atomicamente;
- Nome/Categoria/Margem seguem RN041/RN043/RN019/RN045;
- EmpresaId/Ativo preservados;
- reutilização de ProdutoInputModel;
- duplicidade exclui o próprio Id;
- duplicidade cross-tenant não bloqueia;
- GET/POST cross-tenant => 404;
- request não controla EmpresaId/Ativo;
- /Produtos/Editar/{id:int} implementado;
- Detalhes possui Editar;
- Cancelar retorna a Detalhes;
- PRG para Detalhes + mensagem exata;
- nenhuma migration/snapshot;
- nenhuma entidade de histórico;
- nenhum UC010+;
- documentação pós-implementação marca UC009 como Implementado;
- F002/catálogo/ordem ficam coerentes;
- UC010 passa a próximo caso de Produtos;
- matriz U1-U5, P1-P3 e W1-W11 atendida;
- build Release sem warnings novos relevantes;
- suíte completa verde.
