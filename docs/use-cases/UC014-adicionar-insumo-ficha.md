# UC014 — Adicionar Insumo à Ficha Técnica

- **Status:** Pronto para implementação
- **Funcionalidade:** F003 — Ficha Técnica
- **Dependências materiais:** UC013 implementado; UC001A–UC006 implementados; FT002 implementada
- **Próximo caso relacionado:** UC015 — Alterar item da Ficha Técnica
- **Alteração de schema:** sim — introduz ItemFichaTecnica
- **Revalidação pós-UC013:** concluída em 2026-09-13 contra a implementação mergeada pela PR #54

## Objetivo

Permitir adicionar um Insumo ativo da Empresa Ativa à Ficha Técnica atual de um Produto, informando:

1. Quantidade utilizada no lote, expressa na Unidade base do Insumo;
2. Observação contextual opcional sobre o uso daquele Insumo na Ficha.

A UC014 introduz a composição sem calcular custos, sem permitir edição/remoção de itens e sem antecipar perdas ou equipamentos.

## Princípios

1. Item, Ficha e Insumo pertencem à mesma Empresa.
2. Somente Insumo ativo pode ser adicionado.
3. Quantidade é sempre informada na Unidade base atual do Insumo.
4. Quantidade deve ser maior que zero.
5. Observação contextual é opcional e independente da Observação global do Insumo.
6. O mesmo Insumo não pode aparecer duas vezes na mesma Ficha no MVP.
7. Adicionar Item não calcula custo.
8. Adicionar Item não altera Produto, Ficha nem situação do Insumo.
9. A primeira referência produtiva ativa a proteção cadastral da RN048.

## Gate de estabilidade cadastral do Insumo

### Decisão

Enquanto existir pelo menos um ItemFichaTecnica atual referenciando o Insumo:

- Nome é imutável;
- Marca é imutável;
- Unidade base é imutável;
- Categoria continua editável;
- Observação global continua editável;
- Ativo/Inativo continua regido pela RN008.

Isso vale mesmo quando o Insumo nunca teve preço registrado.

### Motivo

O Item registra Quantidade na Unidade base e representa a escolha daquele Insumo específico. Permitir mudar Nome, Marca ou Unidade enquanto ele está referenciado poderia trocar silenciosamente o significado das Fichas.

Exemplos proibidos enquanto houver referência:

~~~text
Farinha / Renata / g
-> Farinha / Caputo / g

Papel Offset / sem marca / g
-> Papel Fotográfico / sem marca / g

Vinil / Marca A / m
-> Vinil / Marca A / un
~~~

### Relação com RN040

A proteção final da identidade cadastral será:

~~~text
proteger Nome/Marca/Unidade quando
    existe qualquer PrecoInsumo
    OU
    existe qualquer ItemFichaTecnica atual
~~~

Histórico de preço bloqueia permanentemente conforme RN040.

Referência de Ficha é estado atual. Portanto, após UC016, se a última referência for removida e não houver histórico de preço, Nome/Marca/Unidade podem voltar a ser editáveis. UC016 deve revalidar esse desbloqueio.

### Edição do Insumo

A UC014 deverá adaptar /Insumos/Editar/{id}.

Se houver histórico de preço, preservar a mensagem atual da RN040.

Se não houver histórico, mas houver referência em Ficha, exibir:

~~~text
Nome, marca e unidade base não podem ser alterados porque este insumo está sendo usado em ficha técnica.
~~~

A proteção deve existir no servidor. POST manipulado não pode alterar os campos protegidos.

## Modelo de domínio

Criar entidade:

~~~text
ItemFichaTecnica : IEntidadeEmpresa
- Id: int
- EmpresaId: int
- FichaTecnicaId: int
- InsumoId: int
- Quantidade: decimal
- Observacao: string?
~~~

Namespace sugerido:

~~~text
Precificador.Core.FichasTecnicas
~~~

## Unicidade do Item

Um Insumo aparece no máximo uma vez na mesma Ficha.

Identidade funcional:

~~~text
EmpresaId + FichaTecnicaId + InsumoId
~~~

Criar índice único correspondente.

Motivos:

- a Ficha não modela etapas de processo;
- múltiplas linhas do mesmo Insumo seriam somadas para custo;
- duplicidade acidental deve ser impedida;
- UC015 permitirá alterar quantidade/observação do item existente.

Mensagem Web exata:

~~~text
Este insumo já foi adicionado à ficha técnica.
~~~

## EmpresaId

Obrigatório e nunca vem do formulário.

É derivado da Ficha carregada no servidor.

## FichaTecnicaId

Obrigatório.

Referencia FichaTecnica da mesma Empresa.

FK com DeleteBehavior.Restrict.

Não permitir reatribuir Item existente para outra Ficha.

## InsumoId

Obrigatório.

Referencia Insumo da mesma Empresa.

No momento da inclusão, o Insumo deve estar ativo.

FK com DeleteBehavior.Restrict.

Não permitir reatribuir Item existente para outro Insumo.

## Quantidade

Aplicar RN010.

~~~text
Quantidade > 0
~~~

No domínio/persistência, usar decimal com referência de precisão:

~~~text
decimal(18,6)
~~~

Na fronteira Web, **não bindar diretamente para decimal/decimal?**. A UC013 demonstrou que o model binding pode interpretar vírgula como separador de milhar em ambiente com cultura diferente.

Usar entrada textual e parsing explícito, seguindo o padrão já implementado em `FichaTecnicaFormulario`:

~~~text
Input.Quantidade: string?
~~~

Criar helper equivalente, por exemplo:

~~~text
ItemFichaTecnicaFormulario.TentarObterQuantidade(...)
~~~

Regras do parser:

- trim da entrada;
- ausente/vazia => `A quantidade é obrigatória.`;
- se contém vírgula, parsear com `pt-BR`;
- caso contrário, parsear com cultura invariant;
- valor não numérico => `A quantidade deve ser um número válido.`;
- valor <= 0 => `A quantidade deve ser maior que zero.`;
- o resultado decimal validado é o único valor enviado ao domínio.

A quantidade é interpretada na Unidade base do Insumo.

Exemplos:

~~~text
Farinha / g      -> 400 = 400 g por lote
Vinil / m        -> 1,25 = 1,25 m por lote
Embalagem / un   -> 2 = 2 unidades por lote
~~~

Não criar unidade própria no Item e não fazer conversão automática.

## Observação contextual

Aplicar RN034.

Regras:

- opcional;
- máximo 1000 caracteres;
- trim externo;
- preservar conteúdo interno e quebras de linha;
- whitespace-only => null;
- não copiar automaticamente Observação global do Insumo;
- alterar Observação global depois não modifica Observação do Item.

## Pré-condição: Ficha existente

A UC014 depende de FichaTecnica já criada pela UC013.

Não criar Ficha implicitamente ao adicionar Item.

Rota confirmada para o novo fluxo:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Novo
~~~

A página existente da UC013 permanece em `/Produtos/FichaTecnica/{id:int}` e usa `id` como ProdutoId. A UC014 deve preservar essa convenção na navegação e resolver a Ficha persistida pelo Produto no servidor.

A ação Adicionar insumo só aparece quando existir Ficha persistida.

Se o Produto existir na Empresa Ativa mas ainda não possuir Ficha, GET/POST manual deve redirecionar para:

~~~text
/Produtos/FichaTecnica/{produtoId}
~~~

com:

~~~text
Defina a base da ficha técnica antes de adicionar insumos.
~~~

Não retornar 404 nesse cenário, pois o Produto existe e apenas falta uma pré-condição funcional.

## Produto ativo ou inativo

Produto ativo e inativo podem receber novos Itens, desde que a Ficha exista.

Adicionar Item não reativa Produto e não altera Rendimento, TempoAtivoMinutos ou MargemAlvo.

## Insumo ativo ou inativo

### Insumo ativo

Pode ser selecionado e adicionado.

### Insumo inativo

Não pode ser adicionado a nova composição.

GET não oferece Insumo inativo.

POST manipulado com Insumo inexistente, cross-tenant ou inativo deve usar a mesma mensagem:

~~~text
O insumo selecionado não está disponível para inclusão na ficha técnica.
~~~

Isso evita revelar dados de outro tenant.

Se um Insumo for desativado depois de já estar referenciado, o Item existente permanece; UC017 cuidará da leitura da composição existente.

## Insumo sem preço

Insumo ativo sem preço vigente pode ser adicionado.

A composição e o custo são responsabilidades distintas.

Ausência de preço tornará a precificação futura incompleta conforme RN007/RN017, mas não impede montar a Ficha.

## Multiempresa e segurança

ItemFichaTecnica é tenant-owned.

Adicionar:

- DbSet<ItemFichaTecnica>;
- Global Query Filter;
- guard central via IEntidadeEmpresa;
- validação de coerência das referências.

Para persistir:

~~~text
Item.EmpresaId
= FichaTecnica.EmpresaId
= Insumo.EmpresaId
~~~

Não confiar apenas em FKs simples.

O fluxo Web comum não usa IgnoreQueryFilters.

## Request

O formulário recebe somente:

~~~text
InsumoId: int?
Quantidade: string?
Observacao: string?
~~~

`Quantidade` é textual na fronteira e convertida explicitamente para decimal antes de criar o Item.

Não confiar/bindar:

- EmpresaId;
- FichaTecnicaId;
- ProdutoId como fonte de verdade fora da rota;
- ItemId.

ProdutoId vem da rota.

FichaTecnicaId e EmpresaId são resolvidos pelo servidor.

InsumoId deve ser rebuscado no tenant ativo e com Ativo=true.

## Persistência

Criar tabela:

~~~text
ItensFichaTecnica
~~~

Campos:

- Id;
- EmpresaId;
- FichaTecnicaId;
- InsumoId;
- Quantidade;
- Observacao.

FKs:

~~~text
ItensFichaTecnica.EmpresaId      -> Empresas.Id       RESTRICT
ItensFichaTecnica.FichaTecnicaId -> FichasTecnicas.Id RESTRICT
ItensFichaTecnica.InsumoId       -> Insumos.Id        RESTRICT
~~~

Índice único:

~~~text
(EmpresaId, FichaTecnicaId, InsumoId)
~~~

Configuração:

- Quantidade decimal(18,6);
- Observacao max1000 nullable.

### Migration

Somente após o gate pós-UC013.

Nome sugerido:

~~~text
AddItensFichaTecnica
~~~

Não editar migration da UC013 ou migrations históricas.

Não criar Itens retroativos.

## Web

### Página

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Itens/Novo
~~~

Exibir resumo somente leitura:

- Produto;
- Situação do Produto;
- Rendimento;
- Tempo ativo.

Campos:

1. Insumo;
2. Quantidade;
3. Observação contextual.

### Seleção de Insumo

Listar somente Insumos ativos da Empresa Ativa.

Ordenar por:

~~~text
NomeNormalizado
MarcaNormalizada
~~~

Rótulo:

~~~text
Nome — Marca (unidade)
~~~

Sem Marca:

~~~text
Nome (unidade)
~~~

Não depender da UI para impedir duplicidade ou Insumo inativo.

### Quantidade

Input textual obrigatório com `inputmode="decimal"`, evitando dependência do model binding decimal.

Ajuda:

~~~text
Informe a quantidade utilizada no lote na unidade base do insumo.
~~~

Mensagens:

~~~text
A quantidade é obrigatória.
A quantidade deve ser um número válido.
A quantidade deve ser maior que zero.
~~~

### Observação

Textarea opcional, rótulo Observação contextual.

### GET

1. carregar Produto pelo GQF;
2. se ausente/cross-tenant => 404;
3. carregar Ficha;
4. se ausente => redirect para Ficha + mensagem de pré-condição;
5. carregar Insumos ativos;
6. renderizar;
7. nunca criar/mutar Item.

### POST

1. rebuscar Produto pelo GQF;
2. se ausente => 404;
3. rebuscar Ficha;
4. se ausente => redirect para Ficha + mensagem;
5. validar InputModel;
6. rebuscar Insumo por InsumoId com GQF e Ativo=true;
7. se indisponível => erro genérico de indisponibilidade;
8. verificar duplicidade Ficha+Insumo;
9. se duplicado => mensagem exata de duplicidade;
10. criar Item usando EmpresaId/FichaId do servidor;
11. salvar;
12. PRG para /Produtos/FichaTecnica/{produtoId};
13. mensagem:

~~~text
Insumo adicionado à ficha técnica com sucesso.
~~~

Ao retornar Page por erro, repopular o select.

## Navegação

A implementação real da UC013 confirmou que `FichaTecnicaModel.OnGetAsync` já consulta a Ficha por `ProdutoId` para preencher Rendimento/Tempo ativo.

A UC014 deve reutilizar essa consulta para expor um estado somente leitura, por exemplo:

~~~text
PossuiFicha
~~~

ou identificador equivalente não bindável.

Na página da Ficha:

- manter edição de Rendimento/Tempo ativo;
- mostrar **Adicionar insumo** somente quando já existir Ficha persistida;
- o link aponta para `/Produtos/FichaTecnica/{produtoId}/Itens/Novo`.

GET da Ficha sem registro continua sem criar Ficha e não mostra a ação.

UC014 não implementa a consulta completa da composição; isso fica para UC017.

## Antiforgery

POST usa antiforgery padrão.

POST sem token válido não cria Item.

## Regras de negócio

- RN001 — Unidade base;
- RN008 — Insumo inativo;
- RN010 — Quantidade na Ficha;
- RN034 — Observação contextual;
- RN035/RN036 — tenant;
- RN040 — estabilidade por histórico de preço;
- RN047 — Ficha única por Produto;
- RN048 — estabilidade cadastral por referência em Ficha;
- RN049 — unicidade de Insumo por Ficha.

## Critérios de aceitação

### CA01 — Acesso protegido
Anônimo não acessa. Usuário sem Empresa Ativa não possui acesso operacional.

### CA02 — Ficha é pré-condição
Produto sem Ficha redireciona para definição da base, não cria Item e mostra a mensagem definida.

### CA03 — GET lista somente Insumos ativos do tenant
Não apresenta inativos nem Insumos de outras Empresas.

### CA04 — Inclusão válida
POST válido persiste Empresa/Ficha/Insumo corretos, Quantidade e Observacao normalizadas.

### CA05 — Quantidade válida
Decimal >0 é aceita. Ausente, zero ou negativa é rejeitada.

### CA06 — Unidade vem do Insumo
Item não persiste unidade própria.

### CA07 — Observação contextual
Null/whitespace => null; trim externo; conteúdo interno/quebras preservados; >1000 rejeitado.

### CA08 — Mesmo Insumo não duplica
Segunda inclusão na mesma Ficha é rejeitada com a mensagem definida.

### CA09 — Mesmo Insumo em Fichas diferentes
Permitido.

### CA10 — Insumo inativo não entra
Não aparece no GET; POST manipulado é rejeitado.

### CA11 — Produto inativo aceita Item
Pode adicionar Insumo ativo sem reativar Produto.

### CA12 — Cross-tenant de Produto/Ficha
GET/POST retornam 404 e não criam Item.

### CA13 — Cross-tenant de Insumo
POST não revela existência e usa erro genérico de indisponibilidade.

### CA14 — Request não controla ownership
EmpresaId/FichaTecnicaId/ProdutoId/ItemId manipulados não alteram referências resolvidas no servidor.

### CA15 — Persistência rejeita referências cross-tenant
Item de Empresa A não referencia Ficha ou Insumo da Empresa B.

### CA16 — Query Filter isola Itens
Empresa A não consulta Itens da Empresa B no fluxo comum.

### CA17 — FKs Restrict
Empresa/Ficha/Insumo são obrigatórios e sem cascade delete.

### CA18 — Primeiro Item protege identidade
Insumo sem preço passa a proteger Nome/Marca/Unidade ao ser adicionado.

### CA19 — RN040 permanece
Insumo com histórico de preço continua protegido com ou sem Item.

### CA20 — Categoria e Observação global permanecem editáveis
Referência em Ficha não congela esses campos.

### CA21 — Observação contextual é independente
Editar Observação global não modifica Observação do Item.

### CA22 — Insumo sem preço pode ser adicionado
Não exigir preço vigente.

### CA23 — GET não muta
Não cria Item nem altera Ficha/Insumo/Produto.

### CA24 — Antiforgery
POST sem token não cria Item.

### CA25 — PRG e mensagem
POST válido redireciona à Ficha e mostra: Insumo adicionado à ficha técnica com sucesso.

### CA26 — Migration evolutiva
Preserva Fichas/Produtos/Insumos existentes e não gera Itens automáticos.

### CA27 — Sem escopo antecipado
Não implementar UC015/016/017, custos, perdas, equipamentos, conversões ou versionamento.

## Matriz de testes fechada

### Unitários — ItemFichaTecnica

- U1: criação válida preserva empresa/ficha/insumo/quantidade/observação;
- U2: quantidade zero/negativa é rejeitada;
- U3: observação opcional normaliza e valida limite;
- U4: ids de Empresa/Ficha/Insumo devem ser positivos;
- U5: Item não possui Unidade própria.

### Persistência

- P1: migration cria Itens e preserva dados existentes;
- P2: índice único rejeita mesmo Insumo duas vezes na mesma Ficha;
- P3: mesmo Insumo é permitido em Fichas diferentes;
- P4: query filter isola Itens;
- P5: guard rejeita Item com Ficha de outra Empresa;
- P6: guard rejeita Item com Insumo de outra Empresa;
- P7: FKs Restrict preservam referências.

### Web

- W1: acesso exige autenticação e Empresa Ativa;
- W2: Produto sem Ficha redireciona sem criar Item;
- W3: GET lista apenas Insumos ativos do tenant;
- W4: POST válido com Quantidade decimal cria Item e faz PRG;
- W5: POST inválido não cria Item;
- W6: duplicidade exibe mensagem e não cria segundo Item;
- W7: Insumo inativo via POST manipulado é rejeitado;
- W8: Produto inativo aceita Item sem reativação;
- W9: Produto/Ficha cross-tenant => 404;
- W10: Insumo cross-tenant => indisponível sem vazamento;
- W11: request não controla ids/ownership;
- W12: GET edição de Insumo referenciado sem preço bloqueia Nome/Marca/Unidade;
- W13: POST manipulado não altera identidade do Insumo referenciado;
- W14: comportamento de RN040 continua intacto;
- W15: Categoria/Observação global continuam editáveis;
- W16: alterar Observação global não altera Observação contextual;
- W17: GET inclusão não cria Item;
- W18: POST sem antiforgery não cria Item;
- W19: parsing de Quantidade com vírgula é independente da cultura corrente e persiste, por exemplo, `1,25` como `1.25m`, nunca `125m`.

## Revalidação pós-UC013 — concluída

A revalidação obrigatória foi executada contra a implementação real mergeada pela PR #54.

Confirmações:

1. `FichaTecnica` real possui `Id`, `EmpresaId`, `ProdutoId`, `Rendimento` e `TempoAtivoMinutos`;
2. tabela real é `FichasTecnicas`;
3. existe índice único `(EmpresaId, ProdutoId)`;
4. FKs para Empresa e Produto usam `DeleteBehavior.Restrict`;
5. existe Global Query Filter por Empresa;
6. `PrecificadorDbContext` possui guard adicional que valida Ficha -> Produto com a mesma Empresa;
7. rota real da Ficha é `/Produtos/FichaTecnica/{id:int}`, com `id = ProdutoId`;
8. GET da Ficha consulta o registro existente sem criá-lo;
9. Produto ativo e inativo usam a mesma página;
10. Detalhes já navega para Ficha de Produto ativo/inativo;
11. testes da UC013 fornecem infraestrutura Web reutilizável para autenticação, tenant, antiforgery e criação de Produto/Ficha;
12. `ItemFichaTecnica` continua sendo o menor incremento coerente para a composição;
13. não houve necessidade de mudar a entidade FichaTecnica para suportar a UC014;
14. a correção de cultura da UC013 exige que Quantidade use parsing explícito, evitando model binding decimal dependente de ambiente;
15. RN048/RN049 permanecem válidas sem alteração conceitual.

A UC014 está liberada para implementação na branch:

~~~text
feat/uc014-adicionar-insumo-ficha
~~~

Instrução Codex normativa:

~~~text
docs/codex/UC014-adicionar-insumo-ficha.md
~~~

## Impacto nos UCs seguintes

### UC015
Editar somente Quantidade e Observação. Insumo/Ficha/Empresa permanecem imutáveis.

### UC016
Remover Item. Deve revalidar o desbloqueio da RN048 quando desaparecer a última referência e não houver histórico de preço.

### UC017
Consultar composição completa, inclusive Insumos desativados depois da inclusão.

### UC018
Usar Quantidade × CustoUnitarioAtualDoInsumo. UC014 não calcula custo.

## Fora do escopo

- UC015–UC018;
- troca de Insumo do Item;
- remoção;
- histórico/versionamento da composição;
- snapshots de Nome/Marca/Unidade;
- preço/custo persistido no Item;
- exigência de preço vigente;
- perdas;
- equipamentos;
- estoque;
- etapas de processo;
- conversão de unidades;
- API REST;
- refatorações oportunistas.

## Definition of Done específica

Além da DoD global:

- UC013 real permanece sem regressão;
- ItemFichaTecnica tenant-owned implementado;
- uma linha por Insumo/Ficha protegida no banco;
- Quantidade decimal > 0 no domínio e parsing Web explícito pt-BR/invariant;
- Observacao contextual opcional/max1000 normalizada no domínio;
- Item não persiste Unidade própria;
- FKs Empresa/Ficha/Insumo em Restrict;
- GQF para Item;
- guards Item->Ficha e Item->Insumo tenant-aware;
- migration evolutiva AddItensFichaTecnica ou equivalente;
- /Produtos/FichaTecnica/{produtoId}/Itens/Novo funcional;
- página da Ficha mostra Adicionar insumo somente quando PossuiFicha;
- apenas Insumos ativos do tenant são oferecidos/incluídos;
- Insumo sem preço pode ser incluído;
- Produto inativo pode receber Item sem reativação;
- RN048 aplicada no GET e POST de /Insumos/Editar;
- RN040 preservada;
- nenhum cálculo de custo antecipado;
- nenhuma edição/remoção/consulta completa da composição antecipada;
- matriz U1-U5, P1-P7 e W1-W19 atendida;
- build Release sem warnings novos relevantes;
- suíte completa verde.
