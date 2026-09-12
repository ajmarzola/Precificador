# Instrução Codex — UC006 Consultar histórico de preços do insumo

Implemente o **UC006 — Consultar histórico de preços do insumo**.

## Fonte normativa

Leia integralmente antes de alterar código:

1. `AGENTS.md`;
2. `docs/use-cases/UC006-consultar-historico-precos-insumo.md`;
3. `docs/use-cases/UC005-registrar-preco-insumo.md`;
4. `docs/business/business-rules.md`;
5. `docs/business/pricing-model.md`;
6. `docs/features/F001-insumos.md`;
7. `docs/development/foundation-multiempresa-auth.md`;
8. `docs/development/testing-strategy.md`;
9. `docs/development/definition-of-done.md`;
10. `docs/development/melhorias.md`;
11. código atual de PrecoInsumo, Detalhes, Novo Preço, DbContext e testes pós-PR #30.

## Branch

Use:

~~~text
feat/uc006-historico-precos-insumo
~~~

Parta da master atualizada após o merge desta especificação.


### Precondição obrigatória de Git

Antes de alterar qualquer arquivo:

1. confirme que a `master` é o ponto de partida esperado para esta tarefa;
2. crie/troque para `feat/uc006-historico-precos-insumo`;
3. confirme que a branch atual é `feat/uc006-historico-precos-insumo` e **não é `master`**;
4. somente então inicie qualquer edição.

Se não for possível trabalhar nessa branch, **não altere arquivos** e reporte o impedimento.

É proibido editar, commitar ou fazer push direto em `master`. A entrega termina em branch/PR; não faça merge da própria implementação.

## Escopo

Entregar:

- página de histórico somente leitura;
- seleção e destaque do preço vigente pela RN006;
- classificação Vigente/Anterior/Futuro;
- estado vazio;
- histórico de ativo/inativo;
- resumo do preço vigente em Detalhes;
- link Histórico de preços;
- testes fechados na especificação;
- docs pós-implementação.

Não implementar UC007+ ou UC018.

## Data atual

Calcule uma única `dataAtual` por request usando a data local da aplicação.

Reutilize o mesmo valor para:

- seleção do vigente;
- classificação Futuro/Anterior.

Não criar configuração de timezone neste UC.

Testes dependentes de calendário devem usar datas relativas ao dia da execução.

## RN006

Preço vigente:

~~~text
DataReferencia <= dataAtual
OrderByDescending(DataReferencia)
ThenByDescending(Id)
FirstOrDefault
~~~

Não persistir status/preço atual.

Histórico completo:

~~~text
OrderByDescending(DataReferencia)
ThenByDescending(Id)
~~~

Status:

- Id == vigente.Id => Vigente;
- DataReferencia > dataAtual => Futuro;
- demais => Anterior.

Um preço futuro pode aparecer antes do vigente na tabela. Não promova o futuro.

## Nova página

Criar:

~~~text
/Insumos/Precos/Historico/{id:int}
~~~

GET apenas.

Buscar Insumo com Global Query Filter e AsNoTracking.

Cross-tenant/inexistente => 404.

Exibir resumo:

- Nome;
- Marca;
- Unidade;
- Situação.

Exibir resumo do vigente ou:

~~~text
Sem preço vigente.
~~~

Tabela:

- Data;
- Status;
- Quantidade;
- Preço total;
- Custo unitário.

Sem registros:

~~~text
Nenhum preço registrado para este insumo.
~~~

Botões/links:

- Registrar novo preço;
- Voltar ao insumo.

Não adicionar Editar/Excluir.

## Detalhes do Insumo

Adicionar:

- link Histórico de preços;
- resumo do preço vigente.

Não listar histórico completo.

Se não houver vigente, mostrar:

~~~text
Sem preço vigente.
~~~

Preço futuro não é atual.

## Precisão

Use `PrecoInsumo.CustoUnitario`.

Não duplique a fórmula.

Apresentação:

- Quantidade: até 6 casas;
- Preço total: até 4 casas;
- Custo unitário: até 6 casas;
- Data: dd/MM/yyyy;
- remover zeros finais desnecessários quando adequado.

O caso 5.39 / 1000 deve aparecer como 0.00539 (com separador cultural da apresentação), nunca 0.01.

Não criar infraestrutura de currency/i18n.

## Reutilização da query

Evite duas implementações semanticamente diferentes da RN006 entre Histórico e Detalhes.

Pode criar helper/query focado para PrecoInsumo se útil.

Não criar:

- Repository genérico;
- Unit of Work;
- CQRS/MediatR;
- serviço genérico;
- abstração antecipada de UC018.

## Multiempresa

- Global Query Filter obrigatório;
- sem IgnoreQueryFilters no fluxo;
- AsNoTracking em consultas;
- cross-tenant => 404;
- nenhuma linha de outro tenant.

## Testes

Siga literalmente a matriz do UC006.

### Persistência/consulta

- ordenação Data DESC, Id DESC;
- vigente exclui futuro;
- maior Id desempata mesma data;
- somente futuros => sem vigente;
- query filter entre Empresas.

### Web

Cobrir W1–W12 da especificação.

Especialmente:

- não fazer asserts globais frouxos para status;
- provar qual linha/data recebeu Vigente/Futuro/Anterior;
- provar preço futuro não promovido;
- provar precisão 5.39/1000;
- provar Detalhes e Histórico usam a mesma RN006.

## Proibições

Não:

- criar migration;
- alterar snapshot;
- editar/excluir preço;
- criar POST no histórico;
- criar paginação;
- criar filtros;
- criar gráfico/exportação;
- criar timezone por Empresa;
- implementar Produto/Ficha Técnica;
- implementar UC018;
- reimplementar MEL005.

## Documentação pós-implementação

- UC006 => Implementado;
- atualizar F001;
- atualizar catálogo;
- atualizar ordem;
- atualizar pricing-model se necessário para refletir código real;
- UC007 passa a próximo caso;
- gate UC014 permanece;
- manter MEL de timezone como pendente.

## Validação

Execute:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Antes da PR confira:

1. nenhuma migration/snapshot;
2. build sem warnings novos;
3. suíte completa verde;
4. RN006 literal;
5. futuro não vigente;
6. tie por Id;
7. cross-tenant 404;
8. sem custo zero;
9. somente leitura;
10. UC007+/UC018 ausentes.

## Retorno obrigatório

~~~text
Implementação concluída

Resumo:
- ...

Validações:
- ...

Testes:
- Unitários: X/X
- Integração: X/X

Produção/schema:
- consulta de histórico e preço vigente
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: consulta historico de precos do insumo
~~~

Se houver pendência, não escreva "nenhuma".

Não faça merge em master.
