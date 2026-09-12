# UC007 — Cadastrar produto

- **Status:** Implementado
- **Funcionalidade:** F002 — Gestão de Produtos
- **Dependências materiais:** FT002
- **Sequenciamento:** implementar somente após concluir/mergear o UC006, preservando a fila serial
- **Próximo caso relacionado:** UC008 — Listar e consultar produtos

## Objetivo

Permitir cadastrar um **Produto comercializado** pela Empresa Ativa com identidade cadastral mínima e margem-alvo própria, sem antecipar Ficha Técnica, custo calculado ou histórico de preço de venda.

O Produto representa aquilo que a Empresa vende. Como o Precificador atende negócios diferentes, o cadastro não deve assumir panificação, alimentação ou papelaria.

## Modelo de domínio

Criar entidade tenant-owned:

~~~text
Produto : IEntidadeEmpresa
- Id: int
- EmpresaId: int
- Nome: string
- NomeNormalizado: string
- Categoria: string?
- MargemAlvo: decimal
- Ativo: bool
~~~

Não adicionar neste UC:

- preço de venda atual;
- histórico de preço de venda;
- custo atual;
- preço teórico;
- preço sugerido;
- margem atual;
- rendimento;
- ficha técnica;
- perda;
- tempo de produção;
- equipamento;
- imagem;
- SKU/código de barras.

## Nome — RN041

Nome é obrigatório.

Regras:

- remover whitespace externo;
- reduzir sequências internas de whitespace a um único espaço;
- máximo de 120 caracteres após normalização;
- preservar a capitalização informada para exibição;
- manter `NomeNormalizado` em maiúsculas com regra invariável;
- não remover acentos na representação normalizada.

Exemplo:

~~~text
"  Agenda   2027  " -> Nome = "Agenda 2027"
                         NomeNormalizado = "AGENDA 2027"
~~~

## Unicidade — RN042

A identidade cadastral inicial do Produto é:

~~~text
EmpresaId + NomeNormalizado
~~~

Na mesma Empresa, dois Produtos não podem possuir o mesmo Nome normalizado.

Empresas diferentes podem cadastrar Produtos com o mesmo Nome.

A futura situação Ativo/Inativo não participa da identidade; Produto inativo continuará ocupando o Nome dentro da Empresa quando o UC010 existir.

A integridade deve existir em duas camadas:

1. validação Web amigável;
2. índice único no banco.

Mensagem Web exata para duplicidade:

~~~text
Já existe um produto cadastrado com esse nome.
~~~

Não converter qualquer `DbUpdateException` genericamente para duplicidade.

## Categoria opcional — RN043

Categoria serve apenas para organização do catálogo.

Ela é **texto livre**, não enum, porque categorias variam por Empresa e negócio.

Exemplos válidos:

~~~text
Pães
Bolos
Agendas
Planners
Calendários
Adesivos
~~~

Regras:

- opcional;
- máximo de 80 caracteres após normalização;
- remover whitespace externo;
- reduzir sequências internas de whitespace a um único espaço;
- preservar capitalização de exibição;
- whitespace-only => `null`;
- não participa da unicidade;
- não afeta cálculos.

Não criar entidade/tabela `CategoriaProduto` neste UC.

Não criar `CategoriaNormalizada` apenas por antecipação. Se o UC008 exigir pesquisa/filtro específico por categoria, reavaliar com base na necessidade real.

## Situação inicial — RN044

Todo Produto nasce:

~~~text
Ativo = true
~~~

O formulário não permite escolher situação inicial.

Desativação pertence ao UC010.

Não expor setter público de `Ativo`.

## Margem-alvo — RN019 e RN045

Todo Produto deve possuir margem-alvo já no cadastro.

No domínio e persistência, `MargemAlvo` é armazenada como **fração decimal**:

~~~text
30% -> 0,30
25,5% -> 0,255
~~~

Aplicar RN019:

~~~text
0 <= MargemAlvo < 1
~~~

### Interface

O usuário informa percentual:

~~~text
Margem-alvo (%)
~~~

Exemplo:

~~~text
30 -> domínio/persistência 0,30
~~~

O PageModel converte:

~~~text
MargemAlvo = MargemAlvoPercentual / 100
~~~

O domínio continua sendo a validação final.

Mensagem de validação recomendada:

~~~text
A margem-alvo deve ser maior ou igual a 0% e menor que 100%.
~~~

Zero é válido.

100% e valores negativos são inválidos.

Não buscar margem padrão de Configurações neste UC, porque UC026/UC027 ainda não existem.

Quando Configurações forem implementadas, margem padrão poderá apenas **pré-preencher** novos Produtos; não deve alterar silenciosamente Produtos existentes.

## Produto sem preço de venda — RN046

É válido cadastrar Produto sem preço de venda.

O preço praticado possui histórico próprio e será introduzido pelo UC011.

Consequências:

- UC007 não cria coluna `PrecoVenda` em `Produtos`;
- UC007 não cria tabela de histórico de preço de venda;
- ausência de preço de venda não impede o cadastro;
- margem atual ainda não pode ser calculada;
- não usar zero como preço implícito.

## Relação com Ficha Técnica

Produto e Ficha Técnica permanecem conceitos separados.

UC007 cria apenas Produto.

Não criar automaticamente:

- FichaTecnica;
- rendimento;
- itens;
- tempos;
- perdas;
- equipamentos.

UC013+ serão revalidados antes da implementação conforme a generalização multiempresa/produtiva.

## Multiempresa

`Produto` implementa `IEntidadeEmpresa`.

Adicionar:

~~~csharp
DbSet<Produto> Produtos
~~~

Global Query Filter:

~~~text
Produto.EmpresaId == EmpresaAtiva
~~~

O guard central de `SaveChanges/SaveChangesAsync` deve proteger Produto automaticamente via `IEntidadeEmpresa`.

Não receber `EmpresaId` do formulário.

Na criação, `EmpresaId` vem exclusivamente da Empresa Ativa.

Sem Empresa Ativa, a área de Produtos não é acessível.

Não usar `IgnoreQueryFilters` no fluxo normal.

## Persistência

Criar tabela:

~~~text
Produtos
~~~

Campos:

- Id;
- EmpresaId;
- Nome;
- NomeNormalizado;
- Categoria nullable;
- MargemAlvo;
- Ativo.

Configuração sugerida:

~~~text
Nome             max 120
NomeNormalizado  max 120
Categoria        max 80 nullable
MargemAlvo       decimal(9,6)
~~~

Criar FK:

~~~text
Produtos.EmpresaId -> Empresas.Id
~~~

com `DeleteBehavior.Restrict`.

Criar índice único:

~~~text
(EmpresaId, NomeNormalizado)
~~~

## Migration

Criar migration evolutiva:

~~~text
AddProdutos
~~~

ou nome equivalente claro.

Não editar migrations históricas.

Atualizar ModelSnapshot pelo fluxo normal do EF Core.

A migration deve funcionar:

- em banco vazio;
- sobre o schema atual pós-UC005/UC006;
- sem alterar Insumos ou PrecosInsumos existentes.

Não semear Produto automático.

## Web

Criar Razor Page:

~~~text
/Produtos/Novo
~~~

Adicionar proteção da pasta:

~~~text
/Produtos -> policy EmpresaAtiva
~~~

da mesma forma usada para `/Insumos`.

### Campos

Exibir somente:

1. Nome;
2. Categoria opcional;
3. Margem-alvo (%).

Não exibir:

- EmpresaId;
- NomeNormalizado;
- Ativo;
- preço de venda;
- custo;
- ficha técnica.

### POST

Fluxo:

1. validar Empresa Ativa;
2. validar InputModel;
3. normalizar/validar pelo domínio;
4. verificar duplicidade na Empresa Ativa;
5. criar `Produto` com EmpresaId da sessão/contexto;
6. persistir;
7. PRG para `/Produtos/Novo`;
8. exibir mensagem de sucesso.

Mensagem:

~~~text
Produto cadastrado com sucesso.
~~~

Após PRG, formulário deve aparecer limpo para permitir novo cadastro.

## Navegação temporária antes do UC008

Não criar `/Produtos/Index` neste UC.

Adicionar na Home um acesso explícito:

~~~text
Cadastrar produto
~~~

apontando para `/Produtos/Novo`.

Quando UC008 implementar listagem, a navegação principal poderá evoluir para **Produtos** -> listagem.

Não criar listagem vazia apenas para navegação.

## Critérios de aceitação

### CA01 — Acesso protegido

Usuário anônimo não acessa `/Produtos/Novo`.

Usuário autenticado sem Empresa Ativa não obtém acesso operacional ao cadastro.

### CA02 — Formulário possui somente campos funcionais

GET exibe Nome, Categoria e Margem-alvo (%).

Não expõe EmpresaId, NomeNormalizado, Ativo, preço, custo ou dados de Ficha Técnica.

### CA03 — Cadastro válido

POST válido cria Produto:

- na Empresa Ativa;
- com Nome normalizado;
- Categoria normalizada ou null;
- MargemAlvo convertida de percentual para fração;
- Ativo = true.

### CA04 — Nome obrigatório/limite

Nome vazio/whitespace ou >120 após normalização é rejeitado.

### CA05 — Nome é normalizado

Whitespace externo/interno é normalizado e capitalização de exibição preservada.

### CA06 — Categoria opcional/limite

Categoria vazia/whitespace vira null.

Categoria válida é normalizada.

Categoria >80 é rejeitada.

### CA07 — Margem-alvo válida

Percentual 0 é aceito.

Percentual >=100 ou negativo é rejeitado.

### CA08 — Unicidade na mesma Empresa

Mesmo Nome normalizado na mesma Empresa é rejeitado com:

~~~text
Já existe um produto cadastrado com esse nome.
~~~

### CA09 — Mesmo Nome em Empresas diferentes

Empresas diferentes podem possuir Produto com o mesmo Nome normalizado.

### CA10 — Índice único protege persistência

Escrita direta duplicada na mesma Empresa é rejeitada pelo banco.

### CA11 — Tenant ownership vem do servidor

`EmpresaId` persistido é o da Empresa Ativa e não pode ser escolhido/manipulado pelo formulário.

### CA12 — Query filter isola Produtos

Consulta comum na Empresa A não retorna Produto da Empresa B.

### CA13 — Guard rejeita escrita cross-tenant

Produto de Empresa diferente da Empresa Ativa não pode ser persistido/alterado pelo fluxo comum.

### CA14 — Produto nasce ativo

Novo Produto sempre possui `Ativo = true`.

### CA15 — Produto pode nascer sem preço/ficha

Cadastro não exige nem cria preço de venda, Ficha Técnica ou custo.

### CA16 — PRG

Cadastro válido redireciona para GET de `/Produtos/Novo` e mostra:

~~~text
Produto cadastrado com sucesso.
~~~

### CA17 — Navegação

Home possui link **Cadastrar produto**.

### CA18 — Migration evolutiva

Nova migration cria Produtos sem afetar dados existentes.

### CA19 — Sem escopo antecipado

Não implementar UC008+, preço de venda, Ficha Técnica, custo, margem atual, preço sugerido, dashboard ou configurações.

## Matriz de testes fechada antes da implementação

### Unitários — Produto

#### U1

~~~text
CA03_Criar_produto_valido_normaliza_campos_define_margem_e_nasce_ativo
~~~

Validar EmpresaId, Nome, NomeNormalizado, Categoria, MargemAlvo e Ativo.

#### U2

~~~text
CA04_CA05_Nome_invalido_e_rejeitado_e_nome_valido_e_normalizado
~~~

Preferir Theory focada para:

- null/empty/whitespace;
- >120;
- normalização de whitespace.

Se ficar agregado demais, separar invalidez de normalização.

#### U3

~~~text
CA06_Categoria_opcional_normaliza_whitespace_e_respeita_limite
~~~

Cobrir null/whitespace, válida e >80 de forma diagnóstica.

#### U4

~~~text
CA07_Margem_alvo_aceita_limites_validos_e_rejeita_fora_do_intervalo
~~~

Cobrir no domínio:

- 0;
- valor válido próximo de 1;
- negativo;
- 1.

#### U5

~~~text
CA13_Reatribuir_produto_para_outra_empresa_e_rejeitado_e_preserva_empresa_original
~~~

Mesmo contrato de ownership usado por Insumo.

### Integração — persistência

#### P1

~~~text
CA18_Migration_cria_produtos_e_preserva_dados_existentes
~~~

Migrar banco do estado anterior e aplicar `AddProdutos`.

#### P2

~~~text
CA03_Produto_valido_persiste_e_e_recuperado
~~~

#### P3

~~~text
CA10_Indice_unico_rejeita_nome_normalizado_duplicado_na_mesma_empresa
~~~

#### P4

~~~text
CA09_Mesmo_nome_normalizado_e_permitido_em_empresas_diferentes
~~~

#### P5

~~~text
CA12_Query_filter_isola_produtos_por_empresa
~~~

#### P6

~~~text
CA13_Guard_rejeita_escrita_de_produto_para_outra_empresa
~~~

#### P7

~~~text
CA18_Fk_rejeita_empresa_inexistente
~~~

Usar SQLite real/in-memory com migrations.

### Integração — Web

#### W1

~~~text
CA01_Cadastro_de_produto_exige_autenticacao
~~~

#### W2

~~~text
CA02_Get_exibe_apenas_campos_funcionais_do_cadastro
~~~

Confirmar Nome, Categoria, Margem-alvo e ausência dos campos técnicos/escopo futuro.

#### W3

~~~text
CA03_CA16_Post_valido_persiste_produto_da_empresa_ativa_e_faz_PRG
~~~

Confirmar:

- conversão 30 -> 0,30;
- Ativo true;
- EmpresaId correto;
- mensagem de sucesso.

#### W4

~~~text
CA04_CA06_CA07_Post_invalido_nao_persiste_produto
~~~

Usar Theory com **um erro por cenário**:

- Nome vazio;
- Nome >120;
- Categoria >80;
- margem negativa;
- margem 100;
- margem >100.

Não enviar múltiplos erros no mesmo caso.

#### W5

~~~text
CA08_Duplicidade_na_mesma_empresa_exibe_mensagem_e_nao_cria_segundo_produto
~~~

Usar variante de Nome que normalize para o mesmo valor.

#### W6

~~~text
CA09_Mesmo_nome_em_outra_empresa_nao_bloqueia_cadastro
~~~

#### W7

~~~text
CA11_Request_nao_controla_empresa_ou_status_inicial
~~~

Enviar campos adicionais manipulados `EmpresaId` e `Ativo=false` e confirmar que são ignorados/não bindados: EmpresaId vem do contexto e Produto nasce ativo.

#### W8

~~~text
CA17_Home_exibe_link_cadastrar_produto
~~~

## Fora do escopo

- UC008 listagem/consulta;
- UC009 edição;
- UC010 desativação;
- UC011 preço de venda/histórico;
- UC012 histórico de preço de venda;
- Ficha Técnica;
- rendimento;
- composição;
- perdas;
- mão de obra;
- equipamentos;
- custo;
- preço teórico/sugerido;
- margem atual;
- dashboard;
- margem padrão por Configurações;
- entidade CategoriaProduto;
- SKU/código de barras;
- imagem;
- estoque/vendas.

## Definition of Done específica

Além da DoD global:

- Produto tenant-owned no Core;
- Nome e Categoria normalizados;
- Nome único por Empresa;
- MargemAlvo persistida como fração decimal e validada pela RN019;
- Produto nasce ativo;
- Produto pode existir sem preço de venda e sem Ficha Técnica;
- DbSet/configuração/query filter;
- guard tenant-aware cobre Produto;
- FK Restrict para Empresa;
- migration AddProdutos ou equivalente;
- migrations históricas intocadas;
- /Produtos protegido por EmpresaAtiva;
- /Produtos/Novo com apenas campos autorizados;
- PRG + mensagem;
- Home permite navegar ao cadastro;
- nenhuma listagem do UC008;
- nenhum preço/ficha/custo antecipado;
- documentação pós-implementação marca UC007 como Implementado;
- F002/catálogo/ordem/regras/glossário ficam coerentes;
- UC008 passa a ser próximo caso de Produtos;
- implementação do UC007 só inicia depois do UC006 conforme a fila serial;
- build Release sem warnings novos relevantes;
- suíte completa verde.
