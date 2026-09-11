# MEL004 — Centralizar rótulos de CategoriaInsumo e UnidadeMedida na UI

- **Status:** Pronto para implementação
- **Tipo:** melhoria técnica / apresentação
- **Origem:** revisão do UC002
- **Dependências:** UC001B e UC002 implementados
- **Não depende de:** MEL001, MEL002, MEL003 ou UC003

## Objetivo

Eliminar a duplicação de rótulos de `CategoriaInsumo` e `UnidadeMedida` nas Razor Pages de Insumos, mantendo a responsabilidade exclusivamente no projeto `Precificador.Web`.

A melhoria não altera regra de negócio, enum, persistência, schema ou fluxo funcional.

## Problema atual

Hoje os mesmos rótulos aparecem repetidos em três pontos:

- `Pages/Insumos/Novo.cshtml` — options manuais;
- `Pages/Insumos/Index.cshtml` — switches locais;
- `Pages/Insumos/Detalhes.cshtml` — switches locais.

Rótulos atuais:

```text
CategoriaInsumo.MateriaPrima => Matéria-prima
CategoriaInsumo.Embalagem    => Embalagem
CategoriaInsumo.Consumivel   => Consumível

UnidadeMedida.Grama       => g
UnidadeMedida.Mililitro   => ml
UnidadeMedida.Metro       => m
UnidadeMedida.Unidade     => un
```

## Decisão de arquitetura

Os rótulos pertencem à camada de apresentação.

Portanto:

- manter os enums no `Precificador.Core` sem atributos de UI;
- criar um helper estático pequeno no `Precificador.Web`;
- não criar serviço injetável;
- não adicionar camada nova;
- não introduzir mecanismo de localização/i18n nesta melhoria.

Nome sugerido:

```text
src/Precificador.Web/Apresentacao/InsumoRotulos.cs
```

API sugerida:

```csharp
public static class InsumoRotulos
{
    public static IReadOnlyList<CategoriaInsumo> Categorias { get; }
    public static IReadOnlyList<UnidadeMedida> Unidades { get; }

    public static string Categoria(CategoriaInsumo categoria);
    public static string Unidade(UnidadeMedida unidade);
}
```

A implementação interna pode usar `switch` explícito.

As listas devem controlar a ordem de apresentação da UI e contemplar todos os valores válidos atuais.

## Uso esperado

### Cadastro

`Novo.cshtml` deve deixar de duplicar os `<option>` e passar a iterar:

```text
InsumoRotulos.Categorias
InsumoRotulos.Unidades
```

Cada option usa o enum como valor e o helper para o texto.

Os placeholders continuam iguais:

```text
Selecione uma categoria
Selecione uma unidade
```

### Listagem

`Index.cshtml` deve remover os métodos locais `Categoria(...)` e `Unidade(...)` e usar `InsumoRotulos`.

### Detalhes

`Detalhes.cshtml` deve remover os métodos locais `Categoria(...)` e `Unidade(...)` e usar `InsumoRotulos`.

## Critérios de aceitação

### CA01 — Categoria centralizada

Os três rótulos de categoria devem vir do mesmo helper:

- Matéria-prima;
- Embalagem;
- Consumível.

### CA02 — Unidade centralizada

Os quatro rótulos de unidade devem vir do mesmo helper:

- g;
- ml;
- m;
- un.

### CA03 — Todos os valores atuais estão contemplados

As coleções de opções do helper devem conter exatamente todos os valores definidos atualmente em:

- `CategoriaInsumo`;
- `UnidadeMedida`.

Se um novo valor de enum for adicionado no futuro sem atualização do helper, a suíte deve sinalizar a inconsistência.

### CA04 — Comportamento visual preservado

Cadastro, listagem e detalhes devem continuar exibindo exatamente os mesmos textos existentes antes da refatoração.

### CA05 — Sem alteração de domínio

Não adicionar:

- `DisplayAttribute`;
- descrição;
- texto de UI;
- dependência Web

aos enums ou ao projeto `Precificador.Core`.

### CA06 — Sem alteração funcional/schema

Não alterar:

- validação;
- model binding;
- ordem funcional das opções;
- dados persistidos;
- migrations;
- ModelSnapshot.

## Matriz de testes fechada antes da implementação

### Testes focados de apresentação

Criar preferencialmente:

```text
tests/Precificador.Tests.Integration/Web/InsumoRotulosTests.cs
```

O projeto de integração já referencia `Precificador.Web`; não adicionar referência Web ao projeto de testes unitários.

Os testes não devem subir servidor, banco ou HTTP.

#### Teste 1

Nome sugerido:

```text
CA01_Categorias_possuem_rotulos_esperados_e_cobrem_todo_enum
```

Validar:

- MateriaPrima => Matéria-prima;
- Embalagem => Embalagem;
- Consumivel => Consumível;
- `InsumoRotulos.Categorias` contém exatamente `Enum.GetValues<CategoriaInsumo>()`.

#### Teste 2

Nome sugerido:

```text
CA02_Unidades_possuem_rotulos_esperados_e_cobrem_todo_enum
```

Validar:

- Grama => g;
- Mililitro => ml;
- Metro => m;
- Unidade => un;
- `InsumoRotulos.Unidades` contém exatamente `Enum.GetValues<UnidadeMedida>()`.

### Testes Web existentes

Devem continuar verdes, especialmente os que já validam:

- `Matéria-prima`;
- `m`;
- listagem e detalhes de UC002.

Não duplicar testes Web apenas para provar a mesma saída visual.

## Granularidade

A melhoria pode alterar apenas:

- helper novo de apresentação;
- as três Razor Pages que hoje duplicam os rótulos;
- testes focados;
- documentação da MEL004.

Não aproveitar a entrega para centralizar:

- Situação Ativo/Inativo;
- placeholders;
- mensagens;
- navegação;
- outros enums.

## Fora do escopo

- localização/i18n;
- resources;
- TagHelper customizado;
- extension methods globais para todos os enums;
- serviço injetável de apresentação;
- mudança nos enums;
- UC003;
- refatoração geral das Razor Pages;
- MEL001–MEL003.

## Definition of Done específica

A MEL004 está concluída quando:

- CA01–CA06 estão atendidos;
- existe um único ponto de definição dos rótulos de Categoria/Unidade;
- `Novo.cshtml`, `Index.cshtml` e `Detalhes.cshtml` usam esse ponto comum;
- switches/opções duplicados são removidos dessas páginas;
- testes focados provam rótulos e cobertura completa dos enums;
- testes Web existentes permanecem verdes;
- nenhum código de domínio/schema é alterado;
- toda a suíte permanece verde;
- build Release não introduz warnings relevantes;
- `docs/development/melhorias.md` marca MEL004 como `Concluída`;
- este documento passa para **Status: Concluída**;
- o diff permanece restrito à MEL004.

## Resultado esperado do Codex

Ao concluir, retornar:

- resumo objetivo;
- ponto central criado;
- páginas adaptadas;
- testes adicionados;
- comandos de validação executados;
- totais de testes aprovados;
- confirmação de ausência de mudança de domínio/schema;
- pendências, se houver;
- **mensagem de commit sugerida**.

Mensagem de commit sugerida:

```text
refactor: centraliza rotulos de insumos na ui
```
