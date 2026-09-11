# Instrução Codex — UC005 Registrar preço de insumo

Implemente o **UC005 — Registrar preço de insumo**.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/use-cases/UC005-registrar-preco-insumo.md;
3. docs/business/business-rules.md;
4. docs/business/pricing-model.md;
5. docs/business/insumo-historical-stability.md;
6. docs/use-cases/UC003-editar-insumo.md;
7. docs/use-cases/UC004-desativar-reativar-insumo.md;
8. docs/features/F001-insumos.md;
9. docs/development/foundation-multiempresa-auth.md;
10. docs/development/testing-strategy.md;
11. docs/development/definition-of-done.md;
12. docs/development/melhorias.md;
13. código atual de Insumo, Editar, Detalhes, PrecificadorDbContext, configurations e migrations.

## Branch

~~~text
feat/uc005-registrar-preco-insumo
~~~

Parta da master atualizada após o merge desta especificação.

## Escopo obrigatório

- PrecoInsumo;
- persistência + migration;
- isolamento tenant-aware;
- página de registro;
- navegação em Detalhes;
- aplicação server-side da RN040 no Editar;
- testes da matriz;
- docs pós-implementação.

Não implementar UC006+.

## Domínio

Criar PrecoInsumo com:

~~~text
Id
EmpresaId
InsumoId
QuantidadeCompra
PrecoCompra
DataReferencia
~~~

Implementar IEntidadeEmpresa.

CustoUnitario é calculado como PrecoCompra / QuantidadeCompra e não é persistido.

Validar ids positivos quando aplicável, QuantidadeCompra > 0 e PrecoCompra > 0.

Usar DateOnly para DataReferencia.

Não arredondar CustoUnitario.

## Persistência

Adicionar DbSet de PrecosInsumos e PrecoInsumoConfiguration.

Mapear:

- tabela PrecosInsumos;
- precisão decimal suficiente;
- FK Empresa Restrict;
- FK Insumo Restrict;
- índice (EmpresaId, InsumoId, DataReferencia);
- sem unique por data.

Adicionar Global Query Filter por Empresa Ativa.

O guard de IEntidadeEmpresa deve cobrir preço. Generalizar somente suas mensagens de "insumos" para "dados da empresa".

## Migration

Gerar via EF Core:

~~~text
AddPrecosInsumos
~~~

ou nome equivalente.

Não editar migrations históricas.

Não usar EnsureCreated ou auto-migration.

Validar upgrade do schema anterior com Insumos existentes.

## Histórico

Cada registro é append-only.

Não criar Update/Delete/upsert/substituição.

Mesma DataReferencia aceita múltiplos registros.

Maior Id desempata mesma data para futura seleção vigente.

Não implementar UC006.

## Web

Criar:

~~~text
/Insumos/Precos/Novo/{id:int}
~~~

GET busca Insumo pelo tenant, 404 cross-tenant, mostra Nome/Marca/Unidade/Situação e Quantidade com unidade via InsumoRotulos.

POST rebusca o Insumo, valida, deriva EmpresaId/InsumoId do registro carregado, cria novo preço, salva e faz PRG para Detalhes.

Mensagem:

~~~text
Preço do insumo registrado com sucesso.
~~~

Permitir preço para ativo e inativo. Nunca reativar.

Adicionar Registrar preço em Detalhes nos dois estados.

## RN040 no Editar

Com qualquer preço, inclusive futuro:

- Nome somente leitura;
- Marca somente leitura;
- Unidade base somente leitura;
- Categoria editável;
- Observação editável.

POST consulta histórico novamente.

Request manipulado não pode alterar os três campos congelados.

Não persistir PossuiHistorico e não criar snapshots.

Sem preço, preservar UC003 integralmente.

## Testes obrigatórios

Siga literalmente a matriz do UC005.

Unitários:
- criação;
- custo exato;
- quantidade inválida;
- preço inválido;
- ids inválidos se aplicável.

Persistência SQLite:
- migration upgrade;
- FKs Restrict;
- múltiplos preços mesma data;
- query filter;
- guard cross-tenant;
- delete físico de Insumo com preço rejeitado.

Web:
- autenticação;
- resumo/unidade;
- POST válido + PRG;
- inválidos um por cenário;
- cross-tenant;
- inativo;
- link Registrar preço;
- RN040 GET;
- RN040 POST manipulado;
- preço futuro;
- regressão sem histórico.

## Regras de qualidade

1. matriz de testes é contrato;
2. DoD inclui docs;
3. nomes refletem CAs;
4. testes focados;
5. melhoria fora do escopo vai para melhorias.md.

## Proibições

Não implementar:

- UC006;
- tela completa de histórico;
- edição/exclusão de preço;
- fornecedor/estoque;
- conversão de unidade;
- Produto/Ficha Técnica;
- snapshots;
- API;
- MEL005.

## Validação

Execute:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Antes da PR confirme:

1. migration nova + snapshot coerente;
2. migrations antigas intocadas;
3. build sem warnings novos;
4. suíte verde;
5. query filter de preço;
6. guard cross-tenant;
7. FKs Restrict;
8. custo não persistido;
9. append-only;
10. RN040 server-side;
11. inativo não reativado;
12. UC006+ ausente.

## Documentação pós-implementação

- UC005 => Implementado;
- atualizar F001;
- catálogo;
- ordem;
- business rules/pricing model se necessário para refletir código real;
- UC006 vira próximo;
- gate UC014 permanece.

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
- PrecoInsumo + migration AddPrecosInsumos (ou nome efetivo)
- RN040 aplicada à edição

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: registra preco de insumo
~~~

Se houver pendência, não escreva "nenhuma".

Não faça merge em master.
