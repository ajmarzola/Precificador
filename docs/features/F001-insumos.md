# F001 — Gestão de Insumos

## Objetivo

Manter o catálogo de itens usados nas fichas técnicas e seu histórico de preços, isolado por Empresa.

## Capacidades

- cadastrar insumo;
- listar e pesquisar insumos;
- editar dados cadastrais permitidos;
- desativar e reativar insumo;
- registrar novo preço sem sobrescrever histórico;
- consultar histórico de preços;
- identificar insumos sem preço vigente;
- calcular custo por unidade base.

## Dados essenciais

- empresa proprietária;
- nome;
- marca opcional;
- observação técnica opcional;
- categoria;
- unidade base;
- situação ativo/inativo.

### Cadastro

O cadastro de Insumo atualmente contém:

- Nome;
- Marca opcional;
- Categoria;
- Unidade base;
- Observação opcional.

Todo novo Insumo nasce ativo. Preço não faz parte do cadastro e será tratado separadamente pelo UC005, preservando histórico.

Com a FT002, todo Insumo possui `EmpresaId` obrigatório, resolvido pelo servidor a partir da Empresa Ativa e não informado pelo usuário.

Após o UC001A, a identidade econômica do Insumo é formada por **Empresa + Nome + Marca**, protegida pelo índice `(EmpresaId, NomeNormalizado, MarcaNormalizada)`.

### Classificação e unidades generalizadas

O UC001B generalizou o vocabulário para atender negócios alimentícios e de papelaria.

Categorias atuais:

- Matéria-prima;
- Embalagem;
- Consumível.

Unidades base:

- `g` — grama;
- `ml` — mililitro;
- `m` — metro;
- `un` — unidade.

`MateriaPrima = 1` preserva o valor anteriormente usado por `Ingrediente`.

### Consulta

O UC002 disponibiliza listagem, pesquisa por Nome/Marca e detalhes, sempre limitados à Empresa Ativa.

Insumos ativos e inativos permanecem consultáveis. Consultas puras usam `AsNoTracking` e preservam o Global Query Filter da FT002.

### Edição

O UC003 permite a alteração de:

- Nome;
- Marca;
- Categoria;
- Unidade base;
- Observação.

A edição preserva `EmpresaId` e `Ativo`, reaplica normalização/validação e mantém a unicidade tenant-aware por Empresa + Nome + Marca.

Antes da implementação de preço/histórico e ficha técnica, a liberdade de alterar identidade comercial ou unidade base deverá ser reavaliada para não reinterpretar dados históricos já existentes.

### Situação

O UC004 está especificado para tornar a situação reversível:

- Ativo -> Desativar -> Inativo;
- Inativo -> Reativar -> Ativo.

Não há exclusão física.

Insumos inativos continuam listados, consultáveis, editáveis e participando da unicidade por Empresa + Nome + Marca.

Quando Ficha Técnica existir, Insumo inativo não poderá ser adicionado a novas fichas, conforme RN008.

Cada registro de preço futuro pertence a um Insumo específico; como o Insumo pertence a uma Empresa, histórico e custo ficam naturalmente isolados por Empresa.

## Regras relacionadas

- RN001 a RN008;
- RN028 a RN039, conforme aplicáveis.

## Casos de uso

- [UC001 — Cadastrar insumo](../use-cases/UC001-cadastrar-insumo.md) — implementado;
- [UC001B — Generalizar categoria e unidades de insumo](../use-cases/UC001B-generalizar-categoria-unidades-insumo.md) — implementado;
- [UC001A — Complementar cadastro com marca e observação](../use-cases/UC001A-complementar-insumo-marca-observacao.md) — implementado;
- [UC002 — Listar e consultar insumos](../use-cases/UC002-listar-consultar-insumos.md) — implementado;
- [UC003 — Editar insumo](../use-cases/UC003-editar-insumo.md) — implementado;
- [UC004 — Desativar e reativar insumo](../use-cases/UC004-desativar-reativar-insumo.md) — revalidado e pronto para implementação;
- UC005 — Registrar preço de insumo;
- UC006 — Consultar histórico de preços do insumo.

## Fora do escopo

- controle de estoque;
- pedido de compra;
- fornecedor como entidade operacional completa;
- atualização automática de preço por integrações externas.
