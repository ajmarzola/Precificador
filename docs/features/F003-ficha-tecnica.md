# F003 — Ficha Técnica

## Objetivo

Representar a produção de um lote/execução de Produto de forma genérica para diferentes Empresas, sem acoplar o domínio a panificação.

A Ficha Técnica concentra composição e parâmetros produtivos atuais usados posteriormente pelo motor de custo.

## Princípios

- Ficha Técnica pertence a uma Empresa;
- cada Produto possui no máximo uma Ficha Técnica atual no MVP;
- Ficha Técnica representa um lote/execução;
- o estado da Ficha é atual e editável, sem versionamento próprio no MVP;
- Produto pode existir sem Ficha;
- ausência de dados necessários não significa custo zero;
- equipamentos são genéricos e não devem ser substituídos por campos específicos de forno;
- mudanças correntes de Ficha não reescrevem snapshots históricos futuros da decisão comercial.

## Base produtiva — UC013

A UC013 introduz somente os conceitos universais:

- Rendimento do lote em unidades de venda;
- Tempo ativo de trabalho do lote em minutos.

Modelo inicial:

~~~text
FichaTecnica
- Id
- EmpresaId
- ProdutoId
- Rendimento
- TempoAtivoMinutos
~~~

Rendimento é decimal e deve ser maior que zero.

Tempo ativo é inteiro obrigatório e não negativo; zero explicitamente informado é válido.

Uma única Ficha é permitida por Empresa + Produto.

Produto ativo ou inativo pode ter a base criada ou alterada; isso não modifica sua situação.

A mesma Ficha é atualizada quando Rendimento/Tempo ativo mudam. UC013 não cria versionamento.

## Composição — UC014 a UC017

### UC014 — Adicionar Insumo

Implementada após revalidação contra a implementação real da UC013.

Modelo atual:

~~~text
ItemFichaTecnica
- Id
- EmpresaId
- FichaTecnicaId
- InsumoId
- Quantidade
- Observacao
~~~

Regras fechadas:

- somente Insumo ativo pode ser adicionado;
- Insumo sem preço vigente pode ser adicionado;
- Quantidade é decimal >0 na Unidade base do Insumo;
- Observação contextual é opcional;
- um mesmo Insumo aparece no máximo uma vez por Ficha;
- Produto inativo pode manter e receber composição;
- adicionar Item não calcula custo;
- a primeira referência em Ficha protege Nome, Marca e Unidade base do Insumo conforme RN048.

Insumos desativados após já terem sido adicionados permanecem referenciados e legíveis; RN008 impede apenas novas inclusões.

### UC015 — Alterar Item

Implementada após revalidação contra a implementação real da UC014.

A edição altera somente:

- Quantidade;
- Observação contextual.

EmpresaId, FichaTecnicaId e InsumoId permanecem imutáveis. Item de Insumo inativo e Item de Produto inativo continuam editáveis sem reativação.

A UC015 também adiciona uma lista operacional mínima de Itens na página da Ficha (Insumo, Quantidade, Unidade, Situação e Editar), apenas para tornar edição/removal futuros navegáveis. A consulta completa continua reservada ao UC017.

### Incrementos relacionados

- UC016 — remover item; remoção não desbloqueia identidade do Insumo;
- UC017 — consultar Ficha e composição completa.

## Equipamentos e recursos

UC013 **não** cria:

- TempoForno;
- PotenciaFornoKw;
- Equipamento;
- UsoEquipamento.

Forno, impressora, laminadora e outros recursos serão modelados genericamente no UC021 quando necessário ao custo.

## Cálculos posteriores

UC013 não calcula custo.

Seus dados serão consumidos futuramente:

~~~text
TempoAtivoMinutos
    -> UC020 / custo de mão de obra

Rendimento
    -> UC022 / custo unitário
~~~

Itens da Ficha alimentarão UC018.

Perdas serão revalidadas no UC019.

Equipamentos/energia serão tratados no UC021.

## Histórico

Ficha Técnica não possui histórico/versionamento no MVP.

O histórico comercial introduzido mais tarde pelo UC011 congelará Custo de referência, Margem de referência e Preço sugerido no momento da decisão de Preço de prateleira.

Assim, mudanças posteriores da Ficha alteram cálculos correntes, mas não reinterpretam snapshots comerciais já registrados.

## Multiempresa

FichaTecnica é tenant-owned:

- EmpresaId obrigatório;
- Global Query Filter;
- guard central de escrita;
- Produto referenciado deve pertencer à mesma Empresa;
- ownership não vem do request.

## Regras relacionadas

- RN009;
- RN010 a RN017 conforme os UCs evoluírem;
- RN018;
- RN034;
- RN035–RN039;
- RN047;
- RN048;
- RN049;
- RN050;
- RN051.

## Casos de uso

- [UC013 — Definir rendimento e tempo ativo da Ficha Técnica](../use-cases/UC013-definir-base-ficha-tecnica.md);
- [UC014 — Adicionar Insumo à Ficha Técnica](../use-cases/UC014-adicionar-insumo-ficha.md);
- [UC015 — Alterar item da Ficha Técnica](../use-cases/UC015-alterar-item-ficha.md);
- UC016 — Remover item da Ficha Técnica;
- UC017 — Consultar Ficha Técnica e composição.

## Fora do escopo do estágio atual

- preparação intermediária reutilizável;
- versionamento de Ficha;
- estoque;
- ordens de produção;
- apontamento real de produção;
- equipamentos específicos de panificação;
- custo persistido no Produto.
