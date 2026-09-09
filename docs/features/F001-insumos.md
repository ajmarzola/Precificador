# F001 — Gestão de Insumos

## Objetivo

Manter o catálogo de itens usados nas fichas técnicas e seu histórico de preços.

## Capacidades

- cadastrar insumo;
- listar e pesquisar insumos;
- editar dados cadastrais permitidos;
- desativar insumo;
- registrar novo preço sem sobrescrever histórico;
- consultar histórico de preços;
- identificar insumos sem preço vigente;
- calcular custo por unidade base.

## Dados essenciais

- nome;
- categoria: ingrediente, embalagem ou consumível;
- unidade base;
- situação ativo/inativo.

Cada registro de preço deve conter ao menos:

- insumo;
- data de referência;
- quantidade comprada na unidade base;
- preço pago;
- observação opcional.

Fornecedor poderá ser incorporado futuramente sem transformar cadastro de fornecedores em módulo do MVP.

## Regras relacionadas

RN001 a RN008.

## Fora do escopo

- controle de estoque;
- pedido de compra;
- fornecedor como entidade operacional completa;
- atualização automática de preço por integrações externas.
