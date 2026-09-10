# F001 — Gestão de Insumos

## Objetivo

Manter o catálogo de insumos de cada Empresa e seu histórico de preços.

## Capacidades

- cadastrar, listar, pesquisar, editar e desativar Insumos da Empresa Ativa;
- registrar preços sem sobrescrever histórico;
- consultar histórico;
- identificar ausência de preço vigente;
- calcular custo por unidade base.

## Escopo multiempresa

Todo Insumo possui `EmpresaId` e somente é visível/alterável no contexto da Empresa Ativa.

A mesma descrição pode existir em empresas diferentes.

Após UC001A, Marca faz parte da identidade econômica dentro da Empresa e Observação técnica passa a compor o cadastro.

UC001B generalizará categoria/unidades para atender os diferentes negócios antes do UC002.

## Preços

Cada registro de preço pertence a um `InsumoId`; como o Insumo pertence a uma Empresa, seu histórico e custo ficam naturalmente isolados.

## Casos de uso

- UC001 — Cadastrar insumo — implementado;
- UC001A — Marca e observação — aguarda FT002;
- UC001B — Generalizar classificação/unidades — a detalhar;
- UC002 — Listar e consultar — aguarda FT002/UC001A/UC001B;
- UC003 — Editar;
- UC004 — Desativar;
- UC005 — Registrar preço;
- UC006 — Consultar histórico.

## Fora do escopo

Estoque, compras, fornecedor operacional completo e atualização automática externa.
