# F001 — Gestão de Insumos

## Objetivo

Manter o catálogo de itens usados nas fichas técnicas e seu histórico de preços **por Empresa**.

## Capacidades

- cadastrar insumo;
- listar e pesquisar insumos;
- editar dados cadastrais permitidos;
- desativar insumo;
- registrar novo preço sem sobrescrever histórico;
- consultar histórico de preços;
- identificar insumos sem preço vigente;
- calcular custo por unidade base.

## Escopo multiempresa

Após FT002, todo Insumo possui `EmpresaId` obrigatório e operações comuns enxergam somente dados da Empresa Ativa.

A mesma descrição pode existir em empresas diferentes.

## Dados essenciais

- empresa proprietária;
- nome;
- categoria;
- unidade base;
- situação ativo/inativo.

### Cadastro inicial

UC001 foi implementado antes da fundação multiempresa. FT002 adicionará `EmpresaId` sem expor esse campo no formulário.

UC001A permanece responsável por Marca e Observação e será revalidado após FT002.

A adequação das categorias/unidades para os dois negócios será detalhada separadamente antes do UC002.

## Preços

Cada registro de preço pertence a um Insumo específico; consequentemente, históricos/custos ficam isolados pela Empresa proprietária do Insumo.

## Regras relacionadas

RN001 a RN008 e RN028 a RN039, conforme aplicáveis.

## Fora do escopo

Controle de estoque, pedido de compra, fornecedor operacional completo e atualização automática de preço externa.
