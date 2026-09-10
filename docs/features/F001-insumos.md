# F001 — Gestão de Insumos

## Objetivo

Manter o catálogo de itens usados nas fichas técnicas e seu histórico de preços, isolado por Empresa.

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

- empresa proprietária;
- nome;
- categoria;
- unidade base;
- situação ativo/inativo.

### Cadastro inicial

No UC001, o cadastro contém somente:

- Nome;
- Categoria;
- Unidade base.

Todo novo insumo nasce ativo. Preço não faz parte do cadastro inicial e será tratado separadamente pelo UC005, preservando histórico.

O nome deve ser normalizado para eliminar diferenças acidentais de espaços e possuir uma representação técnica normalizada para impedir duplicidades por caixa/espaçamento. A capitalização usada para exibição é preservada após a limpeza de espaços.

Com a FT002, todo Insumo possui `EmpresaId` obrigatório, resolvido pelo servidor a partir da Empresa Ativa e não informado pelo usuário. A unicidade do cadastro é limitada à Empresa proprietária.

O UC001A complementará a identidade econômica do Insumo com Nome + Marca dentro da Empresa e adicionará Observação técnica opcional ao cadastro.

### Classificação e unidades generalizadas

O UC001B generalizou o vocabulário para atender negócios alimentícios e de papelaria sem distorção semântica.

Categorias atuais do domínio:

- Matéria-prima;
- Embalagem;
- Consumível.

Unidades base do escopo atual:

- `g` — grama;
- `ml` — mililitro;
- `m` — metro;
- `un` — unidade.

`MateriaPrima = 1` preserva o valor anteriormente usado por `Ingrediente`, garantindo compatibilidade com registros existentes.

Cada registro de preço deve conter ao menos:

- insumo;
- data de referência;
- quantidade comprada na unidade base;
- preço pago;
- observação opcional.

Fornecedor poderá ser incorporado futuramente sem transformar cadastro de fornecedores em módulo do MVP.

Cada registro de preço pertence a um Insumo específico. Como o Insumo pertence a uma Empresa, histórico e custo ficam naturalmente isolados por Empresa.

## Regras relacionadas

- RN001 a RN008;
- RN028 a RN039, conforme aplicáveis.

## Casos de uso

- [UC001 — Cadastrar insumo](../use-cases/UC001-cadastrar-insumo.md);
- [UC001B — Generalizar categoria e unidades de insumo](../use-cases/UC001B-generalizar-categoria-unidades-insumo.md) — implementado;
- [UC001A — Complementar cadastro com marca e observação](../use-cases/UC001A-complementar-insumo-marca-observacao.md) — revalidado e pronto para implementação;
- [UC002 — Listar e consultar insumos](../use-cases/UC002-listar-consultar-insumos.md);
- UC003 — Editar insumo;
- UC004 — Desativar insumo;
- UC005 — Registrar preço de insumo;
- UC006 — Consultar histórico de preços do insumo.

## Fora do escopo

- controle de estoque;
- pedido de compra;
- fornecedor como entidade operacional completa;
- atualização automática de preço por integrações externas.
