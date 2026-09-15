# Instrução Codex — UC012: Consultar histórico de precificação do Produto

## Tarefa

Implementar integralmente:

~~~text
docs/use-cases/UC012-consultar-historico-precificacao-produto.md
~~~

Branch sugerida:

~~~text
feat/uc012-historico-precificacao-produto
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- UC012;
- UC011;
- MEL009;
- RN024, RN026, RN053 e RN054;
- F002 e F004;
- implementação de `RegistroPrecoProduto`;
- padrão do UC006 para histórico somente leitura.

## Histórico

Criar:

~~~text
/Produtos/Precos/Historico/{id:int}
~~~

Consulta tenant-aware, `AsNoTracking`, ativa/inativa e somente leitura.

Ordenação obrigatória:

~~~text
DataReferencia DESC
Id DESC
~~~

Primeiro registro = **Atual**; demais = **Anterior**.

Reutilizar a mesma seleção em Detalhes e deixar o desenho reutilizável pelo futuro UC024.

## Desconto de referência

Criar calculadora pura no Core.

Entrada:

~~~text
PrecoSugerido
PrecoPrateleira
ReservaComercialReferencia
~~~

Saída:

~~~text
decimal? DescontoReferencia
~~~

Usar apenas snapshots da linha histórica.

Não consultar `ReservaComercialDesconto` atual.

Regras:

~~~text
PrecoSugerido == 0
=> null

PrecoPrateleira < PrecoSugerido
=> null

PercentualAcimaSugerido =
    (PrecoPrateleira / PrecoSugerido) - 1

Limiar =
    ReservaComercialReferencia + 0,01

PercentualAcimaSugerido < Limiar
=> null

caso contrário
=> PercentualAcimaSugerido - ReservaComercialReferencia
~~~

Sem arredondamento intermediário.

Não persistir resultado.

## Página de Histórico

Exibir:

- resumo do Produto;
- Preço de prateleira atual + DataReferencia;
- histórico completo;
- Data;
- status Atual/Anterior;
- CustoReferencia;
- MargemReferencia;
- PrecoSugerido;
- PrecoPrateleira;
- ReservaComercialReferencia;
- DescontoReferencia.

`null` de desconto => **Não aplicável**.

Sem registros => **Preço de prateleira atual: não definido** e estado vazio.

Adicionar Registrar novo preço e Voltar ao produto.

## Detalhes

Adicionar para ativo e inativo:

- link **Histórico de precificação**;
- resumo do Preço de prateleira atual + DataReferencia;
- estado **não definido** quando sem histórico.

Não calcular margem atual ou situação.

## Independência histórica

UC012 não deve depender de:

- Ficha Técnica atual;
- preço atual de Insumo;
- MargemAlvo atual;
- IncrementoComercial atual;
- ReservaComercialDesconto atual.

A linha histórica é autossuficiente para a apresentação prevista neste UC.

## Multiempresa

- GQF para Produto e RegistroPrecoProduto;
- sem `IgnoreQueryFilters` no fluxo comum;
- cross-tenant = 404;
- nenhuma linha de outra Empresa;
- nenhuma escrita.

## Persistência

Nenhuma migration.

Não alterar ModelSnapshot.

Não adicionar `DescontoReferencia`, `EhAtual` ou `PrecoPrateleiraAtual` persistidos.

## Testes

Implementar U1–U12, P1–P5 e W1–W20 da especificação.

Pontos críticos:

1. +10,99% e +11% não podem colapsar por arredondamento;
2. `PrecoSugerido = 0` não divide por zero;
3. histórico usa reserva snapshot;
4. mudança da configuração atual não altera histórico;
5. empate de DataReferencia usa maior Id;
6. Produto inativo é consultável;
7. estado vazio não usa zero;
8. histórico continua funcionando se a precificação atual estiver incompleta;
9. nenhum cálculo do UC024 é antecipado.

## Backlog

Na PR de implementação, após testes verdes:

~~~text
UC012: Pronto -> Concluído
MEL009: Especificado -> Concluído
~~~

Não alterar UC024 para Pronto/Concluído.

## Validação obrigatória

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

Informar:

- arquivos alterados;
- desenho da calculadora de DescontoReferencia;
- estratégia de seleção do registro atual;
- cobertura U/P/W;
- confirmação de ausência de migration;
- resultado de build/test;
- URL da PR.
