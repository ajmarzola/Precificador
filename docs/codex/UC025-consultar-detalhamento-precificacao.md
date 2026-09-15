# Instrução Codex — UC025: Consultar detalhamento da precificação atual

## Tarefa

Implementar integralmente:

~~~text
docs/use-cases/UC025-consultar-detalhamento-precificacao.md
~~~

Branch sugerida:

~~~text
feat/uc025-detalhamento-precificacao
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- UC025;
- UC017–UC024;
- F002 e F004;
- pricing-model;
- `PrecificacaoProdutoAtual`;
- `FichaTecnicaModel`;
- `Produtos/Detalhes`;
- formatadores existentes.

## Regra arquitetural principal

UC025 é **consulta**, não novo motor de cálculo.

Executar uma única vez:

~~~text
PrecificacaoProdutoAtual.CalcularAsync(produtoId)
~~~

Não chamar diretamente calculadoras Core na PageModel.

Não refazer preços vigentes, perdas, mão de obra, energia, total, preço ou margem.

## Nova página

Criar:

~~~text
/Produtos/Precificacao/{id:int}
~~~

Somente GET.

Produto cross-tenant/inexistente => 404.

Produto ativo/inativo => consultável.

## Ampliar resultado compartilhado

Expor na mesma fotografia da orquestração:

~~~text
DataOperacional
Rendimento
TempoAtivoMinutos
ValorHoraTrabalho
TarifaEnergiaKwh
IncrementoComercial
~~~

Preservar todos os resultados UC018–UC024 existentes.

Nos retornos incompletos, preencher apenas o que realmente estiver conhecido; não inventar zero.

## Metadados

Pode consultar metadados de Ficha/Itens/Equipamentos para rótulos.

Associar:

~~~text
ItemId -> Resultado.Itens
UsoId  -> Resultado.Usos
~~~

Nunca recalcular valores.

## Blocos obrigatórios

1. Produto;
2. Estado da precificação;
3. Parâmetros usados;
4. Itens/perdas;
5. Mão de obra;
6. Equipamentos/energia;
7. Consolidação do custo;
8. Formação do preço;
9. Situação comercial;
10. Pendências.

## Incompletude

Preservar null x zero.

- item sem preço não vira zero;
- componente conhecido continua visível;
- total/unitário não aceitam soma parcial;
- incremento null mantém Preço teórico conhecido;
- ausência de Preço de prateleira não esconde custo/Preço teórico/sugerido;
- Produto sem Ficha continua acessível;
- configuração ausente continua acessível e não deve ser criada.

## Navegação

Adicionar em Detalhes:

~~~text
Detalhar precificação
~~~

Na nova página incluir:

- Voltar ao produto;
- Editar Ficha técnica;
- Registrar preço de prateleira;
- Histórico de precificação.

Não transformar a página em superfície de edição.

## Histórico

Não listar histórico e não calcular DescontoReferencia.

Snapshots históricos não substituem valores atuais.

## Multiempresa

- GQF em todas as consultas comuns;
- sem IgnoreQueryFilters;
- nenhum EmpresaId do request;
- cross-tenant 404;
- sem vazamento entre Empresas.

## Persistência

Nenhuma migration.

Não alterar ModelSnapshot.

GET não cria Ficha/configuração nem persiste cálculo.

## Testes

Implementar P1–P8 e W1–W35 da especificação.

Não é necessária nova suíte unitária se nenhuma fórmula Core nova for criada.

Pontos de revisão prioritários:

1. uma única execução de PrecificacaoProdutoAtual;
2. nenhum cálculo duplicado na PageModel;
3. inputs explicativos correspondem à mesma fotografia;
4. null e zero distintos;
5. parcial conhecido visível sem total parcial;
6. incremento null preserva Preço teórico;
7. preço de prateleira ausente não invalida UC023;
8. Produto sem Ficha/configuração continua consultável;
9. Itens/Usos associados por Id;
10. Ficha continua editável sem regressão;
11. Detalhes permanece resumo, não vira UC025;
12. nenhuma antecipação de UC028–UC030.

## Backlog

Na PR de implementação:

~~~text
UC025: Pronto -> Concluído
~~~

Não alterar UC028/UC029/UC030.

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
- desenho da nova página;
- campos adicionados ao resultado compartilhado;
- confirmação de ausência de cálculo duplicado;
- cobertura P/W;
- confirmação de ausência de migration;
- resultado build/test;
- URL da PR.
