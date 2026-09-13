# Instrução Codex — MEL010: Identidade consolidada do Insumo

## Tarefa

Implementar integralmente MEL010 conforme docs/development/improvements/MEL010-identidade-consolidada-insumo.md.

Branch obrigatória:

~~~text
feat/mel010-identidade-consolidada-insumo
~~~

Não editar ou fazer push direto em master.
Não implementar UC016.
Não fazer merge da própria implementação.

## Leitura obrigatória

Ler antes de alterar:

- AGENTS.md;
- docs/development/improvements/MEL010-identidade-consolidada-insumo.md;
- docs/business/business-rules.md;
- docs/business/insumo-historical-stability.md;
- docs/features/F001-insumos.md;
- docs/features/F003-ficha-tecnica.md;
- docs/use-cases/UC005-registrar-preco-insumo.md;
- docs/use-cases/UC014-adicionar-insumo-ficha.md;
- docs/use-cases/UC015-alterar-item-ficha.md;
- Insumo.cs e sua configuração;
- PrecificadorDbContext;
- /Insumos/Editar;
- /Insumos/Precos/Novo;
- /Produtos/FichaTecnica/{produtoId}/Itens/Novo;
- testes de Insumo, preço e ItemFichaTecnica.

Confirmar que a master contém as PRs #56 e #58.

## Domínio

Adicionar ao Insumo:

~~~text
IdentidadeConsolidada: bool
~~~

Novo Insumo nasce false.

Adicionar método:

~~~csharp
ConsolidarIdentidade()
~~~

ou equivalente claro.

A operação:

- false -> true;
- true -> true;
- nunca retorna para false;
- não altera outros campos.

AtualizarDados deve proteger a identidade no domínio.

Se IdentidadeConsolidada = true:

- tentativa real de mudar Nome deve falhar;
- tentativa real de mudar Marca deve falhar;
- tentativa de mudar UnidadeBase deve falhar;
- Categoria e Observacao continuam editáveis;
- informar a mesma identidade atual deve permitir atualizar Categoria/Observacao.

Não criar setter público para IdentidadeConsolidada.

## Persistência

Configurar IdentidadeConsolidada como required em Insumos.

Criar migration evolutiva:

~~~text
AddInsumoIdentidadeConsolidada
~~~

ou nome equivalente.

Não editar migrations históricas.

### Backfill obrigatório

Após adicionar a coluna, marcar true para qualquer Insumo que já possua:

- PrecoInsumo; ou
- ItemFichaTecnica.

Insumo sem ambos permanece false.

Usar SQL compatível com SQLite.

Validar upgrade de banco existente, não apenas banco novo.

## Fluxo de preço

Em /Insumos/Precos/Novo:

- manter validações atuais;
- preço válido e consolidação devem usar o mesmo SaveChanges;
- chamar ConsolidarIdentidade antes do SaveChanges que grava o novo PrecoInsumo;
- preço futuro também consolida;
- não criar segundo SaveChanges.

## Fluxo de novo Item

Em /Produtos/FichaTecnica/{produtoId}/Itens/Novo:

- manter GET/listagem como está;
- no POST válido, carregar o Insumo selecionado rastreado em vez de AsNoTracking;
- preservar GQF, Ativo=true e proteção cross-tenant;
- após validar duplicidade/quantidade e antes do SaveChanges:
  - adicionar ItemFichaTecnica;
  - ConsolidarIdentidade no Insumo;
- um único SaveChanges.

Não alterar a regra que impede adicionar Insumo inativo.

## Edição de Insumo

Simplificar /Insumos/Editar/{id}.

Remover dependência funcional de:

~~~text
PossuiHistoricoPreco
ReferenciadoEmFicha
~~~

A proteção passa a vir de:

~~~text
Insumo.IdentidadeConsolidada
~~~

GET deve conhecer esse estado.

POST deve confiar no domínio como barreira final.

A UI pode manter readonly/disabled para UX.

Mensagem única:

~~~text
Nome, marca e unidade base não podem ser alterados porque a identidade deste insumo já foi consolidada.
~~~

Categoria e Observacao continuam editáveis.

Não consultar PrecosInsumos/ItensFichaTecnica somente para decidir proteção ou mensagem.

## Transação

Preço/Item e consolidação precisam ser parte do mesmo SaveChanges.

Não persistir a flag antecipadamente em SaveChanges separado.

Se a criação do fato falhar, não pode ficar IdentidadeConsolidada=true isoladamente.

## Testes obrigatórios

Implementar a matriz normativa:

### U1-U6

- novo Insumo false;
- consolidação monotônica/idempotente;
- bloqueio de Nome;
- bloqueio de Marca;
- bloqueio de Unidade;
- Categoria/Observacao editáveis.

### P1-P6

- migration;
- backfill preço;
- backfill Item;
- nunca usado permanece false;
- round-trip;
- remoção técnica de último Item não volta flag para false.

### W1-W9

- primeiro preço consolida;
- primeiro Item consolida;
- edição usa flag/mensagem única;
- request manipulado não altera identidade;
- Categoria/Observacao editáveis;
- não consolidado continua editável;
- preço futuro consolida;
- Item em Insumo já consolidado continua funcional;
- RN040/RN048 sem regressão.

Atualizar testes antigos que verificam mensagens específicas por histórico/Ficha para a nova mensagem única.

Não enfraquecer tenant, antiforgery ou guards existentes.

## Não implementar

Não implementar:

- UC016;
- botão Remover;
- remoção Web de Item;
- motivo/data/usuário da consolidação;
- enum de estado;
- tabela histórica;
- event sourcing;
- contador de referências;
- desbloqueio;
- repository genérico;
- CQRS/MediatR;
- refatorações oportunistas.

## Validação obrigatória

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Build sem warnings novos relevantes e suíte completa verde.

## Documentação pós-implementação

Atualizar:

- MEL010 para Concluída;
- docs/development/melhorias.md;
- docs/development/implementation-order.md;
- docs/use-cases/catalog.md se necessário;
- F001/F003 se houver divergência de implementação.

Depois da MEL010 validada/mergeada, UC016 fica liberada para especificação.

## Retorno obrigatório

Informar:

1. branch;
2. migration criada;
3. regra de backfill;
4. alterações de domínio;
5. alterações nos dois fluxos de consolidação;
6. simplificação de /Insumos/Editar;
7. testes;
8. build/test;
9. URL da PR.

Commit sugerido:

~~~text
feat: consolida identidade do insumo
~~~
