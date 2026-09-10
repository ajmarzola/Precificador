# F002 — Gestão de Produtos

## Objetivo

Manter os itens comercializados e seus parâmetros de precificação por Empresa.

## Capacidades

- cadastrar produto;
- listar e pesquisar produtos;
- editar dados cadastrais;
- desativar produto;
- definir preço de venda atual;
- definir margem-alvo;
- preservar histórico de preço de venda.

## Dados essenciais

- empresa proprietária;
- nome;
- categoria opcional de organização;
- situação ativo/inativo;
- preço de venda atual;
- margem-alvo.

Dados de produção e composição pertencem à ficha técnica.

O percentual de perda deixa de ser considerado dado cadastral obrigatório do Produto. Perdas serão revalidadas como conceito de material/processo antes dos UCs correspondentes, permitindo produtos aos quais essa regra não se aplica.

Todo Produto é tenant-owned e só pode ser consultado/alterado no contexto da Empresa Ativa.

## Regras relacionadas

RN018 a RN024 e RN035 a RN039, conforme aplicáveis.

## Fora do escopo

- estoque de produto acabado;
- vendas;
- catálogo público/e-commerce;
- promoções e cupons.
