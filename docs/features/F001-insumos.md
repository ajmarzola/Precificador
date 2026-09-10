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

- nome do item;
- marca opcional;
- observação técnica opcional;
- categoria: ingrediente, embalagem ou consumível;
- unidade base;
- situação ativo/inativo.

### Identidade do insumo

O Nome representa o item genérico e a Marca identifica a variação comercial quando aplicável.

Exemplo:

```text
Nome: Farinha de Trigo Branca
Marca: Renata Super Premium
```

Marcas diferentes do mesmo item são insumos distintos e, quando o UC005 for implementado, possuirão históricos de preço independentes.

A Marca é opcional para suportar itens sem marca relevante.

A unicidade do cadastro é determinada pela combinação normalizada de Nome + Marca, inclusive para registros inativos.

### Observação técnica

A Observação do Insumo guarda características gerais do item, como força W da farinha, teor de proteína ou característica da embalagem.

Justificativas específicas de uso em uma receita pertencem ao item da ficha técnica e serão tratadas no UC014; não devem ser confundidas com a observação global do insumo.

### Cadastro

O cadastro contém:

- Nome;
- Marca opcional;
- Categoria;
- Unidade base;
- Observação opcional.

Todo novo insumo nasce ativo. Preço não faz parte do cadastro e será tratado separadamente pelo UC005, preservando histórico.

Cada registro de preço deve conter ao menos:

- insumo;
- data de referência;
- quantidade comprada na unidade base;
- preço pago;
- observação opcional do evento de compra/preço.

Fornecedor poderá ser incorporado futuramente sem transformar cadastro de fornecedores em módulo do MVP.

## Regras relacionadas

- RN001 a RN008;
- RN028 a RN034.

## Casos de uso

- [UC001 — Cadastrar insumo](../use-cases/UC001-cadastrar-insumo.md);
- [UC001A — Complementar cadastro com marca e observação](../use-cases/UC001A-complementar-insumo-marca-observacao.md);
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
