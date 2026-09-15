# UC012 — Consultar histórico de precificação do Produto

- **Funcionalidades:** F002 — Gestão de Produtos; F004 — Precificação
- **Dependências:** UC011 e MEL009
- **Schema:** não
- **Natureza:** consulta histórica somente leitura

## Objetivo

Permitir consultar todo o histórico comercial de um Produto da **Empresa Ativa**, identificar qual `RegistroPrecoProduto` representa o Preço de prateleira atual e reproduzir o **Desconto de referência** de cada decisão usando exclusivamente os snapshots persistidos pelo UC011.

A consulta histórica não recalcula decisões antigas com custo, margem, incremento ou reserva vigentes hoje.

## Princípios

1. histórico é somente leitura;
2. `RegistroPrecoProduto` permanece append-only;
3. o registro atual é derivado por `DataReferencia DESC, Id DESC`;
4. snapshots históricos nunca são reinterpretados com dados atuais;
5. `DescontoReferencia` é derivado, não persistido;
6. o cálculo do desconto usa `ReservaComercialReferencia` do próprio registro;
7. alteração atual de `ReservaComercialDesconto` não muda histórico;
8. Produto ativo ou inativo possui histórico consultável;
9. ausência de registro significa Preço de prateleira **não definido**, nunca zero;
10. este UC não calcula margem atual nem situação frente à margem — responsabilidade do UC024.

## Fonte histórica

Usar exclusivamente os campos persistidos por UC011:

~~~text
RegistroPrecoProduto
- Id
- EmpresaId
- ProdutoId
- DataReferencia
- CustoReferencia
- MargemReferencia
- PrecoSugerido
- PrecoPrateleira
- ReservaComercialReferencia
~~~

Não consultar Ficha Técnica, preços atuais de Insumo ou configuração atual para reconstruir esses valores.

## Registro atual

RN024 já determina que datas futuras não são admitidas no fluxo comercial.

Para um Produto, o registro comercial atual é:

~~~text
RegistrosPrecosProdutos
  .Where(r => r.ProdutoId == produtoId)
  .OrderByDescending(r => r.DataReferencia)
  .ThenByDescending(r => r.Id)
  .FirstOrDefault()
~~~

Múltiplos registros na mesma `DataReferencia` são permitidos; maior `Id` vence o empate.

Não persistir:

- `RegistroAtualId`;
- `EhAtual`;
- `PrecoPrateleiraAtual` no Produto;
- qualquer cache do registro atual.

A mesma regra deve ser reutilizada pelo Histórico, por Detalhes e futuramente pelo UC024.

## Desconto de referência

Aplicar RN024, RN053, RN054 e MEL009 usando **somente o snapshot da linha histórica**.

Para `PrecoSugerido > 0`:

~~~text
PercentualAcimaSugerido =
    (PrecoPrateleira / PrecoSugerido) - 1

LimiarAplicacao =
    ReservaComercialReferencia + 0,01
~~~

### Não aplicável

`DescontoReferencia = null` quando qualquer condição abaixo ocorrer:

~~~text
PrecoSugerido == 0

ou

PrecoPrateleira < PrecoSugerido

ou

PercentualAcimaSugerido < LimiarAplicacao
~~~

### Aplicável

Caso contrário:

~~~text
DescontoReferencia =
    PercentualAcimaSugerido - ReservaComercialReferencia
~~~

### Preço sugerido igual a zero

UC023 e UC011 permitem `PrecoSugerido = 0` quando o custo calculado é zero.

Nesse cenário, a razão percentual em relação ao preço sugerido é matematicamente indefinida. Portanto:

~~~text
PrecoSugerido = 0
=> DescontoReferencia = não aplicável
~~~

Nunca dividir por zero e nunca inventar percentual de desconto.

## Precisão

Aplicar RN026.

Não arredondar antes de:

- calcular `PercentualAcimaSugerido`;
- comparar com `LimiarAplicacao`;
- calcular `DescontoReferencia`.

Exemplo de fronteira com reserva de 10 p.p.:

~~~text
10,9999% acima -> não aplicável
11,0000% acima -> 1% de DescontoReferencia
~~~

Arredondamento/formatação é apenas de apresentação.

## Componente puro de domínio

Criar calculadora pura, por exemplo:

~~~text
CalculadoraDescontoReferencia.Calcular(
    decimal precoSugerido,
    decimal precoPrateleira,
    decimal reservaComercialReferencia)
    -> decimal?
~~~

Sem EF, HTTP, tenant, formatação ou acesso a configuração.

Validar:

~~~text
precoSugerido >= 0
precoPrateleira > 0
0 <= reservaComercialReferencia < 1
~~~

`null` representa **não aplicável**, não zero.

Não adicionar `DescontoReferencia` à entidade persistida.

## Reutilização da seleção do registro atual

Evitar duplicar a ordenação normativa entre:

- página de Histórico;
- página de Detalhes;
- futuro UC024.

É aceitável criar uma extensão/query focada em `RegistroPrecoProduto` para selecionar o atual.

Não criar Repository genérico, Unit of Work, CQRS ou MediatR apenas para este UC.

## Web — histórico

Criar Razor Page:

~~~text
/Produtos/Precos/Historico/{id:int}
~~~

O `id` identifica o Produto.

### GET

Carregar Produto com Global Query Filter ativo e `AsNoTracking`.

Produto inexistente ou pertencente a outra Empresa:

~~~text
HTTP 404
~~~

A consulta deve funcionar para Produto ativo e inativo.

### Resumo do Produto

Exibir:

- Nome;
- Categoria;
- Situação.

Não é necessário exibir custo atual, Preço sugerido atual ou margem atual nesta tela.

### Resumo comercial atual

Se houver registro:

- Preço de prateleira atual;
- Data de referência do registro atual.

Se não houver:

~~~text
Preço de prateleira atual: não definido.
~~~

Nunca apresentar zero como substituto de ausência.

### Tabela histórica

Ordenar:

~~~text
DataReferencia DESC, Id DESC
~~~

Colunas mínimas:

1. Data de referência;
2. Status;
3. Custo de referência;
4. Margem de referência;
5. Preço sugerido;
6. Preço de prateleira;
7. Reserva comercial de referência;
8. Desconto de referência.

Status:

- primeiro registro pela ordenação normativa: **Atual**;
- demais registros: **Anterior**.

Não persistir status.

Para `DescontoReferencia = null`, exibir:

~~~text
Não aplicável
~~~

### Navegação

A página deve conter:

- **Registrar novo preço** -> `/Produtos/Precos/Novo/{id}`;
- **Voltar ao produto** -> `/Produtos/Detalhes/{id}`.

Registrar novo preço permanece disponível para Produto ativo e inativo conforme UC011.

### Estado vazio

Sem registros:

~~~text
Preço de prateleira atual: não definido.
Nenhum preço de prateleira registrado para este produto.
~~~

Não renderizar linha fictícia e não exibir zero.

## Alteração em Detalhes do Produto

Em:

~~~text
/Produtos/Detalhes/{id:int}
~~~

adicionar:

### Link

~~~text
Histórico de precificação
~~~

para Produto ativo e inativo.

### Resumo do Preço de prateleira atual

Se houver registro atual, exibir:

- Preço de prateleira;
- Data de referência.

Se não houver:

~~~text
Preço de prateleira atual: não definido.
~~~

Não exibir margem atual ou situação de margem; isso pertence ao UC024.

Não listar o histórico inteiro em Detalhes.

## Independência do estado atual

Depois de um registro ter sido criado, mudanças em:

- `Produto.MargemAlvo`;
- preços atuais dos Insumos;
- Ficha Técnica;
- `IncrementoComercial`;
- `ReservaComercialDesconto`;

não alteram sua apresentação histórica.

UC012 deve usar:

~~~text
CustoReferencia
MargemReferencia
PrecoSugerido
PrecoPrateleira
ReservaComercialReferencia
~~~

da própria linha.

Em especial, não consultar `ConfiguracaoPrecificacaoEmpresa.ReservaComercialDesconto` para calcular desconto histórico.

O histórico deve continuar consultável mesmo se a precificação atual estiver incompleta.

## Multiempresa

Preservar FT002:

- Produto carregado por GQF;
- `RegistrosPrecosProdutos` carregados por GQF;
- não usar `IgnoreQueryFilters` no fluxo comum;
- GET cross-tenant retorna 404;
- nenhuma linha de outra Empresa aparece;
- nenhum `EmpresaId` vem do request.

UC012 é somente leitura e não altera guards de escrita.

## Performance e paginação

Reutilizar o índice criado no UC011:

~~~text
(EmpresaId, ProdutoId, DataReferencia)
~~~

O MVP exibe o histórico completo do Produto.

Não implementar paginação agora.

Se volume real justificar, registrar melhoria futura.

## Sem alteração de schema

Não criar migration.

Não alterar:

- migrations históricas;
- `PrecificadorDbContextModelSnapshot`;
- entidade `RegistroPrecoProduto` para persistir dado derivado.

## Critérios de aceitação

- **CA01:** acesso exige autenticação e Empresa Ativa.
- **CA02:** Produto inexistente/cross-tenant retorna 404.
- **CA03:** histórico funciona para Produto ativo e inativo.
- **CA04:** linhas ordenam por `DataReferencia DESC, Id DESC`.
- **CA05:** primeiro registro da ordenação recebe status Atual.
- **CA06:** empate de data é resolvido pelo maior Id.
- **CA07:** Detalhes e Histórico usam a mesma seleção do registro atual.
- **CA08:** estado vazio diferencia ausência de preço de zero.
- **CA09:** tabela exibe snapshots persistidos, não valores atuais.
- **CA10:** DescontoReferencia não é persistido.
- **CA11:** reserva histórica vem de `ReservaComercialReferencia`.
- **CA12:** reserva 10%, +10,99% => não aplicável.
- **CA13:** reserva 10%, +11% => 1%.
- **CA14:** reserva 10%, +20% => 10%.
- **CA15:** reserva 5%, +5,99% => não aplicável.
- **CA16:** reserva 5%, +6% => 1%.
- **CA17:** prateleira abaixo do sugerido => não aplicável.
- **CA18:** `PrecoSugerido = 0` => não aplicável, sem divisão por zero.
- **CA19:** nenhum arredondamento intermediário altera o limiar.
- **CA20:** alterar configuração atual não reinterpreta histórico.
- **CA21:** histórico continua consultável com precificação atual incompleta.
- **CA22:** Detalhes possui link para histórico em ativo e inativo.
- **CA23:** Detalhes mostra Preço de prateleira atual e data corretos.
- **CA24:** UC012 não cria, edita ou exclui registro.
- **CA25:** nenhuma migration/schema é alterado.
- **CA26:** não calcular margem atual/situação do UC024.
- **CA27:** MEL009 fica integralmente consumida após implementação/validação deste UC.

## Matriz de testes

### Unitários — CalculadoraDescontoReferencia

- U1: reserva 10%, +10,99% => null.
- U2: reserva 10%, +11% => 0,01.
- U3: reserva 10%, +20% => 0,10.
- U4: reserva 5%, +5,99% => null.
- U5: reserva 5%, +6% => 0,01.
- U6: preço de prateleira abaixo do sugerido => null.
- U7: reserva zero, +1% => 0,01.
- U8: `PrecoSugerido = 0` => null, sem exceção.
- U9: fronteira de alta precisão não sofre arredondamento intermediário.
- U10: PrecoSugerido negativo é rejeitado.
- U11: PrecoPrateleira <= 0 é rejeitado.
- U12: reserva negativa ou >= 1 é rejeitada.

### Integração — persistência/consulta

- P1: ordenação `DataReferencia DESC, Id DESC` seleciona registro atual.
- P2: dois registros na mesma data selecionam maior Id.
- P3: GQF isola histórico entre Empresas.
- P4: consulta histórica não altera registros.
- P5: seleção do atual independe de configuração vigente.

### Integração — Web

- W1: histórico exige autenticação e Empresa Ativa.
- W2: GET exibe resumo do Produto e snapshots históricos.
- W3: linhas possuem ordem e status Atual/Anterior corretos.
- W4: sem registros exibe estado vazio sem zero.
- W5: Produto inativo possui histórico consultável.
- W6: cross-tenant retorna 404.
- W7: Detalhes possui link de histórico para ativo e inativo.
- W8: Detalhes seleciona Preço atual por data/Id e exibe DataReferencia.
- W9: Detalhes sem registro exibe preço não definido.
- W10: histórico deriva corretamente limiar com reserva 10%.
- W11: histórico deriva corretamente limiar com reserva 5%.
- W12: preço abaixo do sugerido exibe Não aplicável.
- W13: alteração da reserva atual não altera DescontoReferencia histórico.
- W14: `PrecoSugerido = 0` exibe Não aplicável.
- W15: fronteira de precisão não é alterada por arredondamento intermediário.
- W16: histórico é somente leitura e GET não persiste nada.
- W17: histórico não depende da configuração atual para reconstruir snapshots.
- W18: precificação atual incompleta não impede consulta histórica.
- W19: valores históricos continuam sendo Custo/Margem/Preço sugerido snapshots após mudanças atuais.
- W20: nenhuma tela introduz margem atual/situação antes do UC024.

## Documentação relacionada

Atualizar:

- catálogo de UCs;
- F002 — Gestão de Produtos;
- F004 — Precificação;
- modelo de precificação;
- RN024/RN053 conforme a guarda de `PrecoSugerido = 0`.

MEL009 permanece `Especificado` nesta PR documental.

Na PR de implementação, após UC012 implementado e validado:

~~~text
UC012: Pronto -> Concluído
MEL009: Especificado -> Concluído
~~~

UC024 permanece `Planejado`.

## Fora do escopo

- UC024 margem atual/situação;
- UC025 detalhamento da precificação atual;
- filtros por período;
- paginação;
- gráficos;
- exportação;
- editar/excluir histórico;
- corrigir snapshot;
- recalcular histórico com configuração atual;
- persistir DescontoReferencia;
- promoções/cupons;
- auditoria de usuário/timestamp;
- API REST.

## Gate

UC011 está concluído e fornece o snapshot completo.

MEL009 já está implementada em UC026/027/011; UC012 é o último consumo necessário para fechar sua derivação histórica.

**UC012 está liberada para implementação após o merge desta documentação.**

## Branch sugerida

~~~text
feat/uc012-historico-precificacao-produto
~~~

## Commit sugerido

~~~text
feat: consulta historico de precificacao do produto
~~~
