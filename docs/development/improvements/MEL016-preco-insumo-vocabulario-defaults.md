# MEL016 — Corrigir semântica e defaults do registro de preço do Insumo

- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Classificação:** correção conceitual + UX de formulário
- **Prioridade:** média
- **Dependência:** UC005, UC006, MEL006
- **Estado:** Planejado

## Problemas observados

Na tela de registro de preço do Insumo:

- `Quantidade comprada` sugere evento de compra, mas o dado representa a quantidade contida na embalagem/unidade comercial usada para formar o custo;
- `Preço total da compra` também sugere valor total de uma transação, quando representa o preço daquela embalagem;
- `Data de referência` é obrigatória, mas abre vazia mesmo no caso comum de registro do preço de hoje.

## Correção conceitual

Alterar os rótulos visíveis para:

~~~text
Quantidade por embalagem
Preço por embalagem
Data de referência
~~~

O funcionamento e o modelo persistido permanecem os mesmos nesta melhoria.

Não renomear colunas/propriedades de domínio apenas por causa do rótulo sem necessidade técnica; a prioridade é corrigir o vocabulário do usuário.

## Consistência de consulta

A mesma terminologia deve ser revalidada em:

- Detalhes do Insumo;
- Histórico de preços;
- mensagens;
- documentação UC005/UC006;
- futuro Manual do Usuário.

Evitar manter `Preço total`/`Quantidade comprada` em telas de consulta se o formulário passou a usar `embalagem`.

## Default da Data de referência

No GET de novo preço:

~~~text
Input.DataReferencia = IDataOperacionalEmpresa.Hoje
~~~

Não usar diretamente `DateTime.Today`/`DateOnly.FromDateTime(DateTime.Now)`.

A data deve respeitar a data operacional/timezone da Empresa definida pela MEL006.

O usuário continua podendo alterar a data conforme as regras de UC005.

## Persistência

Nenhuma migration esperada.

A semântica existente de:

~~~text
CustoUnitario = PrecoCompra / QuantidadeCompra
~~~

permanece inalterada.

## Testes esperados

- GET novo preço preenche Data de referência com `IDataOperacionalEmpresa.Hoje`;
- timezone/data operacional fixa é respeitada;
- POST continua aceitando data informada;
- rótulos novos aparecem no formulário;
- Detalhes/Histórico usam terminologia coerente;
- cálculo e registros existentes não são reinterpretados.
