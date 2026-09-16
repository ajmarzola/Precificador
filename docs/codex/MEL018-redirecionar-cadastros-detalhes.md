# Instrução Codex — MEL018: Redirecionar cadastros para os detalhes da entidade

## Tarefa

Implementar integralmente:

~~~text
docs/development/improvements/MEL018-redirecionar-cadastros-detalhes.md
~~~

Branch sugerida:

~~~text
fix/mel018-redirecionar-cadastros-detalhes
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- MEL018;
- UC001;
- UC007;
- UC002/UC008 para os destinos Detalhes;
- MEL015/MEL016 apenas como regressão;
- Novo e Detalhes de Insumo;
- Novo e Detalhes de Produto;
- NovoInsumoPageTests;
- ProdutoPageTests;
- FT002.

## Escopo

Alterar somente o pós-sucesso de:

~~~text
POST /Insumos/Novo
POST /Produtos/Novo
~~~

Novos destinos:

~~~text
/Insumos/Detalhes/{idCriado}
/Produtos/Detalhes/{idCriado}
~~~

## Id do redirect

Usar o Id da própria entidade depois de `SaveChangesAsync`:

~~~csharp
insumo.Id
produto.Id
~~~

Proibido usar:

- Max(Id);
- query por último registro;
- query por Nome/NomeNormalizado;
- Id vindo do request.

## Insumo

Após persistência bem-sucedida:

~~~csharp
TempData["MensagemSucesso"] = "Insumo cadastrado com sucesso.";
return RedirectToPage("/Insumos/Detalhes", new { id = insumo.Id });
~~~

O GET destino deve:

- retornar 200;
- mostrar o Insumo criado;
- mostrar a mensagem de sucesso;
- manter Registrar preço.

Não criar preço automático.

## Produto

Após persistência bem-sucedida:

~~~csharp
TempData["MensagemSucesso"] = "Produto cadastrado com sucesso.";
return RedirectToPage("/Produtos/Detalhes", new { id = produto.Id });
~~~

O GET destino deve:

- retornar 200;
- mostrar o Produto criado;
- mostrar a mensagem de sucesso;
- manter Ficha técnica;
- manter Registrar preço de prateleira;
- suportar Produto ainda sem Ficha/preço.

Não criar Ficha ou preço automático.

## Código morto

Como Novo deixa de receber o PRG de sucesso, remover nos dois fluxos, se não houver outro consumidor real:

~~~csharp
public string? MensagemSucesso => TempData["MensagemSucesso"] as string;
~~~

e os blocos Razor correspondentes.

Não remover o suporte a MensagemSucesso de Detalhes.

## Erros

POST inválido/duplicado:

~~~text
HTTP 200
permanece no formulário
Location == null
não persiste novo registro
~~~

Falha inesperada de persistência mantém logging/throw atuais.

Não definir sucesso antes do SaveChanges completar.

## Multiempresa

Preservar:

- EmpresaId do contexto ativo;
- GQF;
- write guards;
- nenhuma EmpresaId em URL/form;
- nenhuma IgnoreQueryFilters no código de produção.

## Persistência

Nenhuma migration.

Não alterar:

- Core;
- DbContext;
- configurações EF;
- ModelSnapshot.

## Testes obrigatórios

### Insumo

Cobrir I1–I5 da MEL018:

- redirect exato com Id real;
- destino 200;
- entidade/mensagem/Registrar preço;
- dados persistidos inalterados;
- inválido sem redirect;
- duplicado sem redirect.

### Produto

Cobrir P1–P6:

- redirect exato com Id real;
- destino 200;
- entidade/mensagem/Ficha técnica/Registrar preço de prateleira;
- recém-criado sem Ficha/preço continua válido;
- dados persistidos inalterados;
- inválido sem redirect;
- duplicado sem redirect.

### Tenant

Cobrir T1 ou provar por teste existente + novo fluxo que o redirect aponta para a entidade do tenant autenticado.

## Atenção aos testes existentes

Hoje há asserts esperando:

~~~text
/Produtos/Novo
~~~

e o teste de Novo Insumo segue o Location sem conferir que ele é Detalhes.

Atualizar os asserts para o novo contrato sem remover validações de persistência.

Não substituir assert exato por `Contains`/StartsWith frouxo quando o Id real é conhecido.

## Documentação pós-implementação

Na PR de implementação:

- MEL018: Pronto -> Concluído;
- manter UC001/UC007/F001/F002 coerentes;
- não alterar MEL019 ou demais estados.

## Fora do escopo

Não implementar:

- MEL019;
- wizard;
- “Cadastrar outro”;
- redirect configurável;
- autoabrir Ficha;
- autoabrir preço;
- mudanças de layout;
- navegação global;
- regras novas de domínio.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

Informar:

- redirects implementados;
- remoção de código morto em Novo;
- testes alterados/adicionados;
- confirmação de nenhuma migration;
- resultado build/test;
- URL da PR.
