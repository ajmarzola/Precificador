# F002 — Gestão de Produtos

## Objetivo

Manter os itens comercializados e seus parâmetros cadastrais/estratégicos por Empresa, preservando separação entre Produto, Ficha Técnica, cálculo de precificação e histórico da decisão comercial de preço.

## Capacidades

- cadastrar produto;
- listar e pesquisar produtos;
- editar dados cadastrais;
- desativar e reativar produto;
- definir margem-alvo;
- calcular Preço sugerido a partir do custo e da margem de referência;
- registrar Preço de prateleira preservando snapshot da precificação;
- consultar histórico de precificação do Produto.

## Cadastro inicial — UC007

Implementado com a página `/Produtos/Novo`, entidade tenant-owned `Produto`, migration evolutiva e margem-alvo armazenada como fração decimal.

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

Produto nasce ativo conforme RN044. Desativação e reativação são gerenciadas pelo UC010.

## Preço sugerido, Preço de prateleira e histórico

Preço comercial **não pertence ao UC007**.

O Produto pode existir sem Preço de prateleira, conforme RN046.

O Preço sugerido é calculado pelo sistema a partir do custo unitário, Margem de referência e regra de arredondamento comercial. Ele representa a referência economicamente saudável para a decisão comercial.

O Preço de prateleira é a decisão comercial informada pelo usuário e pode ser igual, maior ou menor que o Preço sugerido. Quando ficar abaixo do sugerido, a apresentação deve evidenciar essa condição sem bloquear a decisão.

A UC011 foi deslocada para depois da UC023. Ao registrar um novo Preço de prateleira, o histórico deve congelar no mesmo registro:
- Data de referência determinada pelo sistema;
- Custo de referência;
- Margem de referência;
- Preço sugerido;
- Preço de prateleira informado pelo usuário.

O usuário informa somente o Preço de prateleira. O histórico é append-only, admite múltiplos registros na mesma data para correções, não aceita data futura e pode receber registros para Produto inativo sem reativá-lo.

UC012 consultará esse histórico.

Não persistir um simples `PrecoVendaAtual` ou `PrecoPrateleiraAtual` diretamente em Produto; o valor atual será derivado do histórico.

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

## Consulta — UC008

Implementado com as páginas `/Produtos` e `/Produtos/Detalhes/{id}`, mantendo consultas tenant-aware somente leitura.

- listar Produtos da Empresa Ativa;
- pesquisar por Nome;
- ordenar por Nome normalizado;
- exibir Categoria, Margem-alvo e Situação;
- consultar detalhes cadastrais;
- preservar isolamento tenant-aware.

A pesquisa não usa Categoria neste incremento, evitando criar normalização/schema apenas para filtro.

Preço sugerido, Preço de prateleira, custo, margem atual e Ficha Técnica permanecem ausentes da consulta até seus respectivos UCs.

## Edição cadastral — UC009

Implementado com a página `/Produtos/Editar/{id}`, preservando o mesmo Id, a Empresa proprietária e a Situação do Produto.

Campos editáveis:

- Nome;
- Categoria;
- Margem-alvo.

A edição reutiliza RN041–RN045, mantém a unicidade por Empresa + Nome normalizado e não altera Situação.

A atualização de domínio é atômica: se qualquer campo for inválido, o Produto não fica parcialmente alterado em memória.

UC009 não cria histórico de Nome/Categoria/Margem, não altera schema e não introduz desativação, precificação comercial, Ficha Técnica ou custo.

## Situação — UC010

Implementado com ações em `/Produtos/Detalhes/{id}` para o ciclo reversível de situação:

- Ativo -> Inativo por Desativar;
- Inativo -> Ativo por Reativar;
- sem exclusão física;
- sem liberar o Nome ocupado na Empresa;
- mantendo inativos visíveis e consultáveis;
- mantendo, após a UC009, edição cadastral de Produto inativo sem reativação implícita.

O fluxo de edição preserva `Ativo`, mantém Editar disponível para Produto inativo e não reativa implicitamente após edição cadastral.

## Casos de uso

- [UC007 — Cadastrar produto](../use-cases/UC007-cadastrar-produto.md) — implementado;
- [UC008 — Listar e consultar produtos](../use-cases/UC008-listar-consultar-produtos.md) — implementado;
- [UC009 — Editar produto](../use-cases/UC009-editar-produto.md) — implementado;
- [UC010 — Desativar e reativar produto](../use-cases/UC010-desativar-reativar-produto.md) — implementado;
- UC011 — Registrar preço de prateleira preservando snapshot de precificação — após UC023;
- UC012 — Consultar histórico de precificação do Produto — após UC011.

O próximo caso na fila global passa a ser UC013.

## Fora do escopo

- estoque de produto acabado;
- vendas;
- catálogo público/e-commerce;
- promoções e cupons;
- SKU/código de barras no MVP atual;
- imagem de Produto no cadastro inicial.
