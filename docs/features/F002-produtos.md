# F002 — Gestão de Produtos

## Objetivo

Manter os itens comercializados e seus parâmetros cadastrais/estratégicos por Empresa, preservando separação entre Produto, Ficha Técnica e histórico de preço de venda.

## Capacidades

- cadastrar produto;
- listar e pesquisar produtos;
- editar dados cadastrais;
- desativar produto;
- definir margem-alvo;
- definir preço de venda preservando histórico;
- consultar histórico de preço de venda.

## Cadastro inicial — UC007

O Produto inicial contém:

- Empresa proprietária;
- Nome;
- Categoria opcional de organização;
- Margem-alvo;
- Situação Ativo/Inativo.

### Nome

Nome é obrigatório, normalizado para comparação e único por Empresa conforme RN041/RN042.

### Categoria

Categoria é texto livre opcional conforme RN043.

Não criar enum compartilhado entre negócios: categorias como `Pães`, `Agendas`, `Planners` ou `Calendários` pertencem à organização de cada Empresa.

### Margem-alvo

Margem-alvo é obrigatória no Produto desde o cadastro.

No domínio é armazenada como fração decimal e validada por RN019/RN045.

### Situação

Produto nasce ativo conforme RN044. Desativação pertence ao UC010.

## Preço de venda

Preço de venda **não pertence ao UC007**.

O Produto pode existir sem preço praticado, conforme RN046.

UC011 introduzirá alteração de preço de venda com preservação de histórico; UC012 consultará esse histórico.

Não persistir um simples `PrecoVendaAtual` no cadastro inicial para depois migrá-lo para histórico.

## Ficha Técnica e produção

Dados de produção/composição não pertencem ao Produto cadastral.

Ficam fora do UC007:

- rendimento;
- insumos e quantidades;
- tempo ativo;
- perdas;
- equipamentos/recursos;
- custo do lote;
- custo unitário.

Esses conceitos pertencem à Ficha Técnica e aos UCs de precificação.

O percentual de perda deixa de ser dado cadastral obrigatório do Produto. Perdas serão revalidadas como conceito de material/processo antes dos UCs correspondentes.

## Multiempresa

Todo Produto é tenant-owned:

- possui `EmpresaId` obrigatório;
- usa Global Query Filter pela Empresa Ativa;
- escrita comum é protegida pelo guard central;
- `EmpresaId` nunca é escolhido pelo formulário;
- mesmo Nome pode existir em Empresas diferentes.

## Regras relacionadas

- RN018 a RN024, conforme aplicáveis;
- RN035 a RN039;
- RN041 a RN046.

## Casos de uso

- [UC007 — Cadastrar produto](../use-cases/UC007-cadastrar-produto.md) — revalidado e pronto para implementação após conclusão do UC006;
- UC008 — Listar e consultar produtos;
- UC009 — Editar produto;
- UC010 — Desativar produto;
- UC011 — Alterar preço de venda preservando histórico;
- UC012 — Consultar histórico de preço de venda.

## Fora do escopo

- estoque de produto acabado;
- vendas;
- catálogo público/e-commerce;
- promoções e cupons;
- SKU/código de barras no MVP atual;
- imagem de Produto no cadastro inicial.
