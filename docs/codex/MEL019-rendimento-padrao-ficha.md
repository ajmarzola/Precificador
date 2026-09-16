# Instrução Codex — MEL019: Preencher Rendimento da Ficha Técnica com padrão 1

## Tarefa

Implementar integralmente:

~~~text
docs/development/improvements/MEL019-rendimento-padrao-ficha.md
~~~

Branch sugerida:

~~~text
fix/mel019-rendimento-padrao-ficha
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Gate de fila

A MEL019 está especificada, mas só pode ser implementada depois que MEL018 estiver `Concluído`.

Se o backlog ainda mostrar MEL018 diferente de `Concluído`, não iniciar implementação.

## Antes de editar

Ler:

- MEL019;
- UC013;
- UC017;
- F003;
- `FichaTecnica.cshtml.cs`;
- `FichaTecnica.cshtml`;
- `FichaTecnicaFormulario.cs`;
- `FichaTecnicaPageTests.cs`;
- `FichaTecnicaCustoPageTests.cs`.

## Regra principal

Para Produto sem Ficha:

~~~text
GET /Produtos/FichaTecnica/{id}
Input.Rendimento = "1"
Input.TempoAtivoMinutos = vazio
PossuiFicha = false
RendimentoAtual = null
~~~

Não criar Ficha.

Para Produto com Ficha:

~~~text
Rendimento = valor persistido
TempoAtivoMinutos = valor persistido
~~~

Nunca substituir por 1.

## Implementação esperada

Após carregar o estado da Ficha no GET:

~~~csharp
var ficha = await CarregarEstadoFichaAsync(id);

if (ficha is null)
{
    Input.Rendimento = FichaTecnicaFormulario.FormatarRendimento(1m);
}
else
{
    Input = new FichaTecnicaInputModel
    {
        Rendimento = FichaTecnicaFormulario.FormatarRendimento(ficha.Rendimento),
        TempoAtivoMinutos = ficha.TempoAtivoMinutos
    };
}
~~~

Solução equivalente é aceitável.

## Proibições

Não fazer no GET:

- SaveChanges;
- criar `FichaTecnica`;
- criar Item;
- criar equipamento;
- preencher Tempo ativo com 0;
- definir `PossuiFicha = true`;
- definir `RendimentoAtual = 1`.

Não fazer no POST:

- reaplicar `1` se o usuário apagar o campo;
- alterar parser;
- transformar vazio em 1;
- alterar regras de validação.

## POST

Se o usuário aceitar o valor renderizado 1 e preencher Tempo ativo:

~~~text
Rendimento = 1
Tempo = 30
~~~

persistir normalmente `1m / 30`.

Se alterar para `2,5`, persistir `2.5m`.

Se apagar Rendimento, continuar exibindo:

~~~text
O rendimento é obrigatório.
~~~

## POST inválido

Se o usuário trocar o Rendimento para `2,5` e Tempo ativo for inválido:

~~~text
Rendimento deve continuar 2,5
~~~

Não voltar para 1.

## Produto inativo

Mesmo comportamento:

- GET mostra Rendimento 1 quando sem Ficha;
- não cria Ficha;
- não reativa Produto.

## Multiempresa

Preservar:

- GQF;
- cross-tenant 404;
- write guards;
- nenhum IgnoreQueryFilters no código de produção;
- nenhum EmpresaId vindo do formulário.

## Persistência

Nenhuma migration.

Não alterar Core, DbContext, ModelSnapshot ou entidade FichaTecnica.

## Testes

Cobrir W1–W16 da MEL019, priorizando `FichaTecnicaPageTests`.

Pontos obrigatórios:

1. sem Ficha -> input Rendimento exatamente `1`;
2. Tempo ativo vazio;
3. GET não cria Ficha;
4. GET repetido não cria Ficha;
5. ações condicionadas a Ficha continuam ocultas;
6. Ficha existente com 3,5 permanece 3,5;
7. aceitar o default salva 1;
8. substituir por 2,5 salva 2.5;
9. Rendimento apagado continua obrigatório;
10. POST inválido preserva valor digitado;
11. Tempo zero continua válido;
12. Produto inativo recebe default sem reativação;
13. cross-tenant 404;
14. suíte de precificação permanece verde.

## Backlog

Quando MEL018 já estiver Concluída e a implementação da MEL019 for aberta:

~~~text
MEL019: Especificado -> Pronto
~~~

na preparação da implementação, e na PR concluída:

~~~text
MEL019: Pronto -> Concluído
~~~

Não alterar MEL017 ou itens posteriores.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

Informar:

- ponto do GET alterado;
- confirmação de que GET não persiste;
- testes adicionados/atualizados;
- confirmação de ausência de migration;
- resultado build/test;
- URL da PR.
