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

A revalidação para histórico de preços foi concluída pela RN040:

- antes do primeiro preço, Nome, Marca e Unidade base permanecem editáveis;
- após qualquer preço registrado, Nome, Marca e Unidade base tornam-se imutáveis;
- Categoria e Observação continuam editáveis;
- mudança de identidade ou unidade após o início do histórico exige novo Insumo, preservando o antigo.

A restrição foi aplicada ao fluxo de edição no UC005: qualquer histórico, inclusive apenas preço futuro, congela Nome, Marca e Unidade base no servidor e na apresentação.

A decisão foi simplificada antes do UC016: o primeiro preço (RN040) ou o primeiro uso em Ficha (RN048) consolida permanentemente Nome, Marca e Unidade base conforme RN051. Remover referências futuras não desbloqueia a identidade. A MEL010 persiste esse estado em `Insumo.IdentidadeConsolidada` antes do UC016.

### Situação

O UC004 torna a situação reversível:

- Ativo -> Desativar -> Inativo;
- Inativo -> Reativar -> Ativo.

Não há exclusão física.

Insumos inativos continuam listados, consultáveis, editáveis e participando da unicidade por Empresa + Nome + Marca.

Quando Ficha Técnica existir, Insumo inativo não poderá ser adicionado a novas fichas, conforme RN008.

Cada registro de preço futuro pertence a um Insumo específico; como o preço também é tenant-owned, histórico e custo permanecem isolados pela Empresa Ativa.

### Preços

O UC005 foi revalidado para introduzir `PrecoInsumo` como histórico append-only.

Cada preço registra:

- Quantidade por embalagem, expressa na Unidade base;
- Preço por embalagem;
- Data de referência.

Os nomes técnicos persistidos permanecem `QuantidadeCompra` e `PrecoCompra`.

“Embalagem” é apenas a referência comercial usada para formar o custo; não existe nesta etapa cadastro de embalagem, quantidade de embalagens compradas ou conversão automática de unidade.

O custo unitário é calculado por RN004 e não é persistido.

Datas futuras são permitidas e qualquer primeiro preço, inclusive futuro, ativa imediatamente a RN040.

Insumos inativos também podem receber registros de preço sem serem reativados.

O UC006 implementou a consulta do histórico completo, identificando o preço vigente pela RN006 e distinguindo registros anteriores de preços futuros.

Além da página de histórico, Detalhes do Insumo passa a exibir um resumo do preço vigente ou **Sem preço vigente**, sem tratar ausência como custo zero.

## Regras relacionadas

- RN001 a RN008;
- RN028 a RN040, conforme aplicáveis;
- RN048 e RN051.

## Casos de uso

- [UC001 — Cadastrar insumo](../use-cases/UC001-cadastrar-insumo.md);
- [UC001B — Generalizar categoria e unidades de insumo](../use-cases/UC001B-generalizar-categoria-unidades-insumo.md);
- [UC001A — Complementar cadastro com marca e observação](../use-cases/UC001A-complementar-insumo-marca-observacao.md);
- [UC002 — Listar e consultar insumos](../use-cases/UC002-listar-consultar-insumos.md);
- [UC003 — Editar insumo](../use-cases/UC003-editar-insumo.md);
- [UC004 — Desativar e reativar insumo](../use-cases/UC004-desativar-reativar-insumo.md);
- [UC005 — Registrar preço de insumo](../use-cases/UC005-registrar-preco-insumo.md);
- [UC006 — Consultar histórico de preços do insumo](../use-cases/UC006-consultar-historico-precos-insumo.md).

## Fora do escopo

- controle de estoque;
- pedido de compra;
- fornecedor como entidade operacional completa;
- atualização automática de preço por integrações externas.
