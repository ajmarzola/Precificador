# MEL005 — Centralizar entrada e validação de Insumo entre Novo e Editar

- **Status:** Pronto para implementação
- **Tipo:** melhoria técnica / refatoração Web
- **Origem:** revisão do UC003
- **Dependências:** UC003 e UC005 implementados
- **Não altera:** domínio, regras de negócio, schema, migrations ou comportamento funcional

## Objetivo

Eliminar a duplicação hoje existente entre:

- `Pages/Insumos/Novo.cshtml.cs`;
- `Pages/Insumos/Editar.cshtml.cs`;

centralizando apenas os elementos realmente compartilhados do formulário de Insumo:

1. modelo de entrada;
2. validação Web de Categoria/Unidade obrigatórias e válidas;
3. mapeamento de erros de domínio para campos do formulário;
4. mensagem funcional de duplicidade.

A melhoria deve reduzir risco de divergência futura sem transformar o fluxo de Insumos em uma hierarquia ou framework próprio.

## Problema atual confirmado

Na master atual, Novo e Editar possuem cópias independentes de:

### InputModel

Campos idênticos:

~~~text
Nome
Marca
Categoria
UnidadeBase
Observacao
~~~

incluindo os mesmos atributos de apresentação.

### Validação Web

Ambos implementam a mesma regra:

~~~text
Categoria deve existir e ser valor válido do enum
UnidadeBase deve existir e ser valor válido do enum
~~~

com as mesmas mensagens:

~~~text
A categoria é obrigatória.
A unidade base é obrigatória.
~~~

### Mapeamento de exceções de domínio

Ambos possuem a mesma função conceitual:

~~~text
marca      -> Input.Marca
observacao -> Input.Observacao
demais     -> Input.Nome
~~~

### Duplicidade

Ambos usam a mesma mensagem:

~~~text
Já existe um insumo cadastrado com esse nome e marca.
~~~

A consulta de duplicidade, porém, **não é idêntica**:

- Novo verifica qualquer registro com mesma identidade;
- Editar exclui o próprio `Id`.

Essa parte não deve ser centralizada nesta MEL.

## Decisão de arquitetura

A centralização permanece no projeto:

~~~text
Precificador.Web
~~~

Não mover comportamento de formulário para `Precificador.Core`.

O Core continua responsável por:

- normalização de Nome/Marca/Observação;
- validação de tamanho;
- validação final de Categoria/Unidade;
- invariantes de ownership;
- RN040 e demais regras de domínio.

A Web continua responsável por:

- model binding;
- mensagens associadas a campos;
- validação amigável antes de chamar o domínio;
- tradução de exceções de domínio em erros de formulário.

## Estrutura proposta

### 1. InsumoInputModel

Criar arquivo único compartilhado, por exemplo:

~~~text
src/Precificador.Web/Pages/Insumos/InsumoInputModel.cs
~~~

Estrutura esperada:

~~~csharp
public sealed class InsumoInputModel
{
    [Display(Name = "Nome")]
    public string? Nome { get; set; }

    [Display(Name = "Marca")]
    public string? Marca { get; set; }

    public CategoriaInsumo? Categoria { get; set; }

    [Display(Name = "Unidade base")]
    public UnidadeMedida? UnidadeBase { get; set; }

    [Display(Name = "Observação")]
    public string? Observacao { get; set; }
}
~~~

`NovoModel.Input` e `EditarModel.Input` devem usar exatamente esse tipo.

Remover os dois `InputModel` aninhados.

### 2. InsumoFormulario

Criar helper estático pequeno, por exemplo:

~~~text
src/Precificador.Web/Pages/Insumos/InsumoFormulario.cs
~~~

Responsabilidades permitidas:

~~~csharp
internal static class InsumoFormulario
{
    public const string MensagemDuplicidade =
        "Já existe um insumo cadastrado com esse nome e marca.";

    public static void ValidarCamposObrigatorios(
        ModelStateDictionary modelState,
        InsumoInputModel input);

    public static void AdicionarErroDominio(
        ModelStateDictionary modelState,
        ArgumentException exception);
}
~~~

A API exata pode variar desde que mantenha o mesmo recorte.

### ValidarCamposObrigatorios

Deve preservar exatamente o comportamento existente:

~~~text
Categoria nula ou não definida no enum
-> erro em Input.Categoria
-> "A categoria é obrigatória."

UnidadeBase nula ou não definida no enum
-> erro em Input.UnidadeBase
-> "A unidade base é obrigatória."
~~~

Não mover essa validação para JavaScript, Razor ou banco.

### AdicionarErroDominio

Deve preservar o mapeamento atual:

~~~text
ParamName == "marca"      -> Input.Marca
ParamName == "observacao" -> Input.Observacao
demais                    -> Input.Nome
~~~

A mensagem original da exceção deve continuar sendo usada.

Não engolir exceções não previstas fora do fluxo atual.

## O que não deve ser centralizado

### Consulta de duplicidade

Manter separada.

Novo:

~~~text
mesmo NomeNormalizado + MarcaNormalizada
~~~

Editar:

~~~text
mesmo NomeNormalizado + MarcaNormalizada
E Id != registro atual
~~~

Criar um método genérico com parâmetro opcional de Id nesta MEL adicionaria abstração sem ganho relevante.

### Lógica da RN040

Permanece exclusiva do Editar:

- `PossuiHistorico`;
- proteção de Nome/Marca/Unidade;
- restauração dos valores persistidos no POST;
- mensagem informativa no formulário.

Não mover RN040 para o helper compartilhado.

### Fluxo de persistência

Continuam próprios de cada PageModel:

- criação no Novo;
- atualização no Editar;
- redirects;
- TempData;
- logging;
- carregamento de entidade;
- checks cross-tenant.

### Razor markup

Não criar partial compartilhado para os campos nesta MEL.

Motivo: Editar possui comportamento condicional da RN040 para:

- readonly de Nome;
- readonly de Marca;
- disabled + hidden de UnidadeBase.

Forçar um partial agora aumentaria flags/condicionais e reduziria clareza.

## Comportamento funcional obrigatório

A MEL005 é uma refatoração.

Depois da implementação, devem permanecer exatamente iguais:

### Novo

- campos exibidos;
- labels;
- opções de Categoria/Unidade;
- mensagens de validação;
- mensagem de duplicidade;
- normalização pelo domínio;
- PRG;
- mensagem de sucesso;
- tenancy.

### Editar

- carregamento dos dados;
- RN040;
- validação;
- duplicidade excluindo o próprio Id;
- atualização;
- cross-tenant 404;
- PRG;
- mensagem de sucesso.

## Critérios de aceitação

### CA01 — InputModel único

Existe apenas uma definição funcional de modelo de entrada de Insumo usada por Novo e Editar.

Não devem permanecer classes aninhadas duplicadas com os mesmos cinco campos.

### CA02 — Validação Web centralizada

A validação de Categoria e Unidade base usada por Novo e Editar vem do mesmo ponto.

Mensagens permanecem:

~~~text
A categoria é obrigatória.
A unidade base é obrigatória.
~~~

### CA03 — Mapeamento de erro de domínio centralizado

Novo e Editar usam o mesmo mecanismo para mapear:

- marca;
- observação;
- nome/default.

### CA04 — Mensagem de duplicidade centralizada

A string:

~~~text
Já existe um insumo cadastrado com esse nome e marca.
~~~

possui um único ponto de definição na camada Web usada pelos dois fluxos.

### CA05 — RN040 preservada

Editar continua bloqueando Nome, Marca e UnidadeBase quando há histórico.

POST manipulado continua sem conseguir alterar esses campos.

### CA06 — Sem alteração de consulta de duplicidade

Novo continua verificando qualquer identidade igual.

Editar continua excluindo o próprio Id.

### CA07 — Sem alteração de domínio

Não alterar:

- `Insumo.Criar`;
- `Insumo.AtualizarDados`;
- normalizações;
- limites;
- enums;
- RN040;
- ownership.

### CA08 — Sem schema

Nenhuma migration ou ModelSnapshot é alterado.

### CA09 — Sem abstração excessiva

Não criar:

- base class para PageModels;
- repository;
- service genérico de Insumo;
- mediator;
- pipeline;
- framework de formulário;
- partial genérico configurado por flags;
- reflection para mapear propriedades.

### CA10 — Regressão completa preservada

Todos os testes existentes de Novo, Editar e RN040 permanecem verdes.

## Matriz de testes fechada antes da implementação

A melhoria não introduz regra de negócio nova, portanto não exige nova suíte unitária no Core.

O foco é regressão Web.

### Testes existentes obrigatórios

Devem continuar verdes integralmente:

~~~text
NovoInsumoPageTests
EditarInsumoPageTests
PrecoInsumoPageTests
~~~

Especialmente:

- cadastro válido;
- marca/observação inválidas;
- duplicidade;
- edição válida;
- edição com campo inválido;
- cross-tenant;
- RN040 no GET;
- RN040 no POST manipulado;
- preço futuro congelando campos.

### Novos testes mínimos

Adicionar apenas cobertura hoje ausente para provar que o helper compartilhado não perde a validação amigável no Novo.

#### W1

~~~text
MEL005_Novo_com_categoria_invalida_exibe_erro_e_nao_persiste
~~~

Enviar valor de enum inválido, por exemplo `0`.

Confirmar:

- HTTP 200;
- mensagem `A categoria é obrigatória.`;
- nenhum novo Insumo.

#### W2

~~~text
MEL005_Novo_com_unidade_invalida_exibe_erro_e_nao_persiste
~~~

Enviar valor de enum inválido, por exemplo `0`.

Confirmar:

- HTTP 200;
- mensagem `A unidade base é obrigatória.`;
- nenhum novo Insumo.

Não adicionar testes estruturais frágeis que verifiquem quantidade de classes, nomes privados ou implementação interna.

## Granularidade do diff

Arquivos esperados:

- novo `InsumoInputModel.cs`;
- novo `InsumoFormulario.cs`;
- `Novo.cshtml.cs`;
- `Editar.cshtml.cs`;
- ajuste pequeno em `NovoInsumoPageTests.cs`;
- documentação MEL005.

As Razor Views não precisam mudar se o nome da propriedade pública `Input` permanecer igual.

Não alterar schema.

## Fora do escopo

- partial compartilhado Novo/Editar;
- centralização de consultas EF;
- centralização da verificação de duplicidade;
- refatoração de `Insumo` no Core;
- DataAnnotations adicionais para substituir regras atuais;
- mudança de textos;
- mudança de validação;
- mudança na RN040;
- mudança em InsumoRotulos;
- Produto;
- UC006/UC007/UC008;
- MEL006.

## Definition of Done específica

MEL005 está concluída quando:

- CA01–CA10 atendidos;
- Novo e Editar usam o mesmo `InsumoInputModel`;
- validação de Categoria/Unidade existe em um único ponto;
- mapeamento de erros do domínio existe em um único ponto;
- mensagem de duplicidade existe em um único ponto;
- consultas de duplicidade continuam específicas por fluxo;
- RN040 permanece intacta;
- novos testes W1/W2 passam;
- todos os testes existentes permanecem verdes;
- nenhuma migration/ModelSnapshot;
- nenhuma mudança no Core;
- build Release sem warnings novos relevantes;
- `docs/development/melhorias.md` passa MEL005 para Concluída após implementação;
- este documento passa para Status: Concluída após implementação;
- diff permanece restrito à MEL005.

## Mensagem de commit sugerida

~~~text
refactor: centraliza formulario de insumo
~~~
