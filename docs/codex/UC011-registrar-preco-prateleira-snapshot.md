# Instrução Codex — UC011: Registrar preço de prateleira preservando snapshot

## Tarefa

Implementar integralmente:

~~~text
docs/use-cases/UC011-registrar-preco-prateleira-snapshot.md
~~~

Branch sugerida:

~~~text
feat/uc011-registro-preco-prateleira
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler UC011, UC023, MEL009, MEL006, RN024, RN026 e RN052–RN054. Inspecionar a implementação atual de `FichaTecnicaModel`, as calculadoras UC018–UC023, `ConfiguracaoPrecificacaoEmpresa`, guards tenant-aware e o padrão de `PrecoInsumo` append-only.

## Modelo

Criar `RegistroPrecoProduto : IEntidadeEmpresa` com:

~~~text
Id
EmpresaId
ProdutoId
DataReferencia
CustoReferencia
MargemReferencia
PrecoSugerido
PrecoPrateleira
ReservaComercialReferencia
~~~

Validar conforme a UC. Não expor edição do registro.

## Snapshot

O usuário informa somente `PrecoPrateleira`.

Valores persistidos pelo servidor:

~~~text
DataReferencia             = IDataOperacionalEmpresa.Hoje
CustoReferencia            = CustoUnitarioProduto atual
MargemReferencia           = Produto.MargemAlvo
PrecoSugerido              = UC023 atual
ReservaComercialReferencia = ReservaComercialDesconto atual
~~~

Não aceitar esses valores do request.

## Regra comercial

~~~text
PrecoPrateleira > 0
~~~

Preço abaixo do sugerido é permitido.

Precificação incompleta impede registro.

## Reuso UC018–UC023

Não copie a lógica de cálculo da Ficha Técnica para a nova página.

Extraia/reutilize uma orquestração compartilhada de precificação atual do Produto, usada pela Ficha e pelo UC011. Continue usando as calculadoras puras existentes.

A suíte da Ficha Técnica deve permanecer verde e provar ausência de regressão.

## POST

Recalcular no servidor no momento do POST. Não usar snapshots em hidden fields.

~~~text
carregar Produto tenant-aware
=> validar PrecoPrateleira
=> recalcular precificação atual
=> exigir UC023 completo
=> obter reserva atual
=> criar RegistroPrecoProduto
=> salvar
=> PRG para Detalhes
~~~

## Persistência

Criar tabela `RegistrosPrecosProdutos` com precisões definidas na UC.

- FKs RESTRICT para Empresa e Produto;
- índice não único `(EmpresaId, ProdutoId, DataReferencia)`;
- DbSet/GQF/guard tenant-aware;
- proteger coerência Empresa do registro x Produto;
- migration evolutiva sem backfill comercial.

Não persistir `PrecoTeorico`, `IncrementoComercialReferencia` ou `DescontoReferencia`.

## Web

Criar:

~~~text
/Produtos/Precos/Novo/{id:int}
~~~

GET mostra Produto + precificação atual e somente `PrecoPrateleira` editável.

Adicionar em Detalhes `Registrar preço de prateleira` para Produto ativo e inativo.

POST válido: mensagem `Preço de prateleira registrado com sucesso.` e PRG para Detalhes.

## MEL009

Todo registro congela `ReservaComercialReferencia`. Mudança posterior de reserva não modifica registros antigos. Novo registro usa a nova reserva.

MEL009 não deve ser marcado como concluído nesta implementação; UC012 ainda precisa consumir o snapshot histórico.

## Testes

Atender U1–U8, P1–P8 e W1–W24 da especificação.

Pontos críticos de revisão:

1. append-only real, sem upsert por data;
2. DataReferencia vem da data operacional;
3. snapshot é recalculado no POST;
4. campos manipulados do request não controlam snapshot;
5. Produto inativo é permitido sem reativação;
6. preço abaixo do sugerido é permitido;
7. reserva histórica é congelada;
8. GQF/guard/FKs impedem mistura entre Empresas;
9. nenhuma fórmula UC018–UC023 é duplicada;
10. Ficha Técnica não sofre regressão.

## Backlog

Na PR de implementação mover somente:

~~~text
UC011: Pronto -> Concluído
~~~

Não alterar UC012/UC024/UC025 para Pronto ou Concluído.

## Validação obrigatória

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

Informar arquivos alterados, migration criada, desenho do snapshot, componente compartilhado de precificação, cobertura U/P/W, resultado de build/test e URL da PR.
