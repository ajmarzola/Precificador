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

## Base produtiva — UC013 / MEL022

UC013 introduziu Rendimento e Tempo ativo. A MEL022 simplifica a base produtiva e remove `TempoAtivoMinutos`, pois o custo de mão de obra deixa de depender de tempo.

Modelo vigente após MEL022:

~~~text
FichaTecnica
- Id
- EmpresaId
- ProdutoId
- Rendimento
~~~

Rendimento é decimal e deve ser maior que zero.

Com a MEL019, Produto sem Ficha abre o formulário com Rendimento `1` como sugestão inicial. O GET continua somente leitura e não cria Ficha.

Uma única Ficha é permitida por Empresa + Produto.

Produto ativo ou inativo pode ter a base criada ou alterada; isso não modifica sua situação.

A mesma Ficha é atualizada quando o Rendimento muda. A Ficha não possui versionamento próprio no MVP.

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
- PercentualPerda
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

- [UC016 — Remover item da Ficha Técnica](../use-cases/UC016-remover-item-ficha.md) — remoção física do Item atual não desbloqueia a identidade do Insumo, não remove a Ficha e permite composição vazia;
- [UC017 — Consultar Ficha Técnica e composição](../use-cases/UC017-consultar-ficha-composicao.md) — completa a leitura da rota existente com Nome, Marca, Quantidade, Unidade, Observação contextual, Situação e ações, sem antecipar custos/preços.

## Perdas de material — UC019

Perda deixa de ser atributo global do Produto e passa a ser específica de cada Item da Ficha:

~~~text
ItemFichaTecnica.PercentualPerda
~~~

O percentual é opcional, default zero e representa material adicional esperado sobre a Quantidade base.

Não existe regra automática por Categoria. Matéria-prima, embalagem ou consumível podem possuir perda quando o processo real justificar.

Perdas que reduzam unidades finais vendáveis devem ser refletidas no Rendimento da Ficha, e não duplicadas como perda de material.

O custo da perda é derivado em consulta pelo UC019 e não é persistido.

## Equipamentos e recursos — UC021

UC021 mantém o domínio genérico sem criar campos específicos de forno e sem introduzir um cadastro patrimonial global.

Modelo:

~~~text
UsoEquipamentoFicha
- Id
- EmpresaId
- FichaTecnicaId
- NomeEquipamento
- NomeEquipamentoNormalizado
- PotenciaKw
- TempoUsoMinutos
~~~

O registro representa o uso total do equipamento naquela execução/lote. Forno, impressora, plotter, laminadora e equipamentos equivalentes usam o mesmo modelo.

Um mesmo nome normalizado aparece no máximo uma vez por Ficha. Se houver dois equipamentos físicos distintos, usar nomes distintos.

A Ficha continua sendo estado atual editável: uso pode ser criado, alterado ou removido. O histórico comercial futuro preservará o custo de referência sem exigir versionamento da Ficha.

## Cálculos posteriores

UC013 não calcula custo.

Seus dados serão consumidos futuramente:

~~~text
Rendimento
    -> UC022 / custo unitário

CustoBaseItens
+ PercentualMaoDeObra da Empresa
    -> UC020 / custo de mão de obra
~~~

Itens da Ficha alimentarão UC018.

Perdas de material são tratadas pelo UC019 por Item da Ficha.

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
- RN051;
- RN055;
- RN056.

## Casos de uso

- [UC013 — Definir rendimento da Ficha Técnica](../use-cases/UC013-definir-base-ficha-tecnica.md);
- [UC014 — Adicionar Insumo à Ficha Técnica](../use-cases/UC014-adicionar-insumo-ficha.md);
- [UC015 — Alterar item da Ficha Técnica](../use-cases/UC015-alterar-item-ficha.md);
- [UC016 — Remover item da Ficha Técnica](../use-cases/UC016-remover-item-ficha.md);
- [UC017 — Consultar Ficha Técnica e composição](../use-cases/UC017-consultar-ficha-composicao.md);
- [UC019 — Calcular perdas aplicáveis](../use-cases/UC019-calcular-perdas-aplicaveis.md);
- [UC021 — Calcular custo de energia/equipamentos](../use-cases/UC021-calcular-custo-energia-equipamentos.md).

## Fora do escopo do estágio atual

- preparação intermediária reutilizável;
- versionamento de Ficha;
- estoque;
- ordens de produção;
- apontamento real de produção;
- equipamentos específicos de panificação;
- custo persistido no Produto.
