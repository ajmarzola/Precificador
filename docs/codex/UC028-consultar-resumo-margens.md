# Instrução Codex — UC028: Consultar resumo de margens da Empresa Ativa

## Tarefa

Implementar integralmente:

~~~text
docs/use-cases/UC028-consultar-resumo-margens.md
~~~

Branch sugerida:

~~~text
feat/uc028-dashboard-margens
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- UC028;
- F005;
- UC022, UC023 e UC024;
- MEL024;
- RN017, RN022, RN023, RN026, RN027, RN036 e RN037;
- `ResumoPrecificacaoProdutosAtual`;
- `PrecificacaoProdutoAtual`;
- `Produtos/Index`;
- `_Layout`;
- formatadores monetários/percentuais existentes.

## Escopo central

Criar Dashboard somente leitura:

~~~text
/Dashboard
~~~

com:

- Produtos ativos;
- Insumos ativos;
- Abaixo da margem;
- Dentro da margem;
- Margem indisponível;
- lista de todos os Produtos ativos com valores atuais.

Não implementar UC029/UC030.

## Autorização

Proteger `/Dashboard` com a policy `EmpresaAtiva`, usando a mesma convenção das demais áreas operacionais.

Não criar autorização paralela.

Não receber EmpresaId no request.

## Universo

Consultar somente:

~~~text
Produto.Ativo == true
~~~

do tenant atual.

Produtos inativos ficam fora dos cards e da tabela.

## Cards

Calcular contagens usando `SituacaoMargem` da UC024:

~~~text
AbaixoDaMargem
DentroDaMargem
Incompleto
~~~

Na UI, `Incompleto` deve significar **Margem indisponível**.

Não chamar isso de Precificação incompleta; a completude global pertence à UC030.

Validar:

~~~text
ProdutosAtivos
=
Abaixo
+ Dentro
+ MargemIndisponivel
~~~

## Lista

Colunas:

~~~text
Produto
Custo unitário atual
Preço de prateleira atual
Margem atual
Margem-alvo
Preço sugerido
Situação da margem
Consultar
~~~

Ordenar por `NomeNormalizado ASC`.

Sem filtros nesta UC.

## Reuso em lote

Usar uma única execução de:

~~~text
ResumoPrecificacaoProdutosAtual.CalcularAsync(produtoIds)
~~~

para o conjunto exibido.

Proibido chamar:

~~~text
PrecificacaoProdutoAtual.CalcularAsync
~~~

em loop.

Preservar a estratégia em lote da MEL024.

## Preço sugerido

Evoluir `ResumoPrecificacaoProdutoAtual` para expor:

~~~text
PrecoSugerido?
~~~

Carregar `IncrementoComercial` na configuração já lida pelo serviço e reutilizar `CalculadoraPrecoProduto`.

Não duplicar fórmula.

Não usar `RegistroPrecoProduto.PrecoSugerido` histórico.

Cenário obrigatório:

~~~text
custo conhecido
preço de prateleira conhecido
IncrementoComercial null

=> PrecoSugerido null
=> MargemAtual continua calculada
=> SituacaoMargem continua completa
~~~

## Contagem de Insumos

Pode executar `CountAsync` separado para Insumos ativos, sempre sob GQF.

Não carregar lista completa de Insumos apenas para contar.

## Apresentação

Reutilizar formatadores existentes.

- custo null => indisponível;
- preço atual null => —;
- preço sugerido null => indisponível;
- margem null => indisponível;
- zero conhecido continua zero;
- margem negativa não é truncada.

Rótulos:

~~~text
AbaixoDaMargem => Abaixo da margem
DentroDaMargem => Dentro da margem
Incompleto     => Margem indisponível
~~~

Cor pode complementar, nunca substituir texto.

## Navegação — Dashboard é a Home da Empresa

Adicionar `Dashboard` ao menu autenticado.

Tratar `/Dashboard` como a **Home da Empresa**. Depois que há usuário autenticado + Empresa Ativa, todo destino semântico de Home/Início deve ir diretamente ao Dashboard.

Revisar todos os pontos atuais que apontam para `/Index` ou `/` com esse propósito. No mínimo:

- `Conta/Login.cshtml.cs`;
- `Conta/Logout.cshtml.cs`;
- `Empresas/Selecionar.cshtml.cs`;
- `Pages/Index.cshtml.cs`;
- `Shared/_Layout.cshtml`.

Comportamento obrigatório:

~~~text
login sem ReturnUrl + uma Empresa
=> /Dashboard

login com ReturnUrl local válido
=> ReturnUrl

seleção explícita de Empresa
=> /Dashboard

GET /Conta/Login já autenticado + Empresa Ativa
=> /Dashboard

link Início autenticado
=> /Dashboard

marca Precificador autenticada
=> /Dashboard

GET / autenticado + Empresa Ativa
=> redirect /Dashboard
~~~

Não usar `/` como salto intermediário quando o destino autenticado já é conhecido.

### Logout

Logout é a transição inversa:

~~~text
POST /Conta/Logout
=> limpar Empresa Ativa
=> SignOut
=> /
~~~

O destino é a Home pública. Não redirecionar o logout para `/Conta/Login` nem para `/Dashboard`.

A Home `/` continua pública para anônimos. Não copiar o Dashboard para `Index.cshtml`; no estado autenticado com Empresa Ativa, redirecionar.

Usuário autenticado sem Empresa Ativa continua no fluxo de seleção/resolução existente e não acessa o Dashboard por bypass da policy.

Consultar deve levar à superfície existente do Produto; não criar edição inline.

## Persistência

Nenhuma migration.

Não alterar ModelSnapshot.

GET não grava nada.

## Multiempresa

- GQF ativo;
- sem `IgnoreQueryFilters`;
- sem EmpresaId do request;
- nenhuma contagem cross-tenant;
- troca de Empresa Ativa troca integralmente os dados exibidos.

## Testes

Cobrir a matriz da especificação, com prioridade para:

1. 0 Produtos ativos;
2. uma linha em cada situação;
3. inativos excluídos;
4. invariante dos cards;
5. insumos ativos tenant-aware;
6. preço atual e custo atuais;
7. Preço sugerido atual por UC023;
8. IncrementoComercial null sem contaminar Situação de margem;
9. margem negativa;
10. igualdade com meta;
11. preço/custo ausentes;
12. formatação null x zero;
13. isolamento entre Empresas;
14. sem escrita no GET;
15. sem N+1 por Produto;
16. regressão de `/Produtos`;
17. login padrão termina em `/Dashboard`;
18. ReturnUrl local continua preservado;
19. seleção de Empresa termina em `/Dashboard`;
20. GET de Login já autenticado termina em `/Dashboard`;
21. links autenticados Início/marca Precificador apontam direto ao Dashboard;
22. `/` anônimo continua público e `/` autenticado com Empresa Ativa redireciona ao Dashboard;
23. Logout limpa contexto e retorna para `/` deslogado.

Não é necessária nova fórmula Core.

## Documentação na implementação

Atualizar:

- UC028;
- backlog;
- F005, se necessário apenas para refletir implementação real;
- catálogo, se necessário.

Na PR de implementação:

~~~text
UC028: Pronto -> Concluído
~~~

Não alterar UC029/UC030 para Pronto ou Concluído.

## CD / Azure

UC028 não inclui publicação Azure nem Continuous Deployment.

A próxima publicação em produção deve seguir a decisão de tratar MEL027 antes de novo deploy manual; não introduzir workflow de CD escondido nesta UC.

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
- desenho dos cards e tabela;
- evolução de `ResumoPrecificacaoProdutoAtual`;
- confirmação de reuso das calculadoras;
- confirmação de ausência de N+1;
- cobertura da matriz;
- confirmação de ausência de migration;
- resultado build/test;
- URL da PR.
