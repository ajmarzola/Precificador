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

### Cadastro inicial

No UC001, o cadastro contém somente:

- Nome;
- Categoria;
- Unidade base.

Todo novo insumo nasce ativo. Preço não faz parte do cadastro inicial e será tratado separadamente pelo UC005, preservando histórico.

O nome deve ser normalizado para eliminar diferenças acidentais de espaços e possuir uma representação técnica normalizada para impedir duplicidades por caixa/espaçamento. A capitalização usada para exibição é preservada após a limpeza de espaços.

Não podem existir dois insumos com o mesmo nome normalizado, inclusive quando um deles estiver inativo.

Cada registro de preço deve conter ao menos:

- insumo;
- data de referência;
- quantidade comprada na unidade base;
- preço pago;
- observação opcional.

Fornecedor poderá ser incorporado futuramente sem transformar cadastro de fornecedores em módulo do MVP.

## Regras relacionadas

- RN001 a RN008;
- RN028 a RN031.

## Casos de uso

- [UC001 — Cadastrar insumo](../use-cases/UC001-cadastrar-insumo.md);
- UC002 — Listar e consultar insumos;
- UC003 — Editar insumo;
- UC004 — Desativar insumo;
- UC005 — Registrar preço de insumo;
- UC006 — Consultar histórico de preços do insumo.

## Fora do escopo

- controle de estoque;
- pedido de compra;
- fornecedor como entidade operacional completa;
- atualização automática de preço por integrações externas.
