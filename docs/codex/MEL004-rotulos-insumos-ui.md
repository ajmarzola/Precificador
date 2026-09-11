# Instrução Codex — MEL004 Centralizar rótulos de Insumos na UI

Você está implementando a **MEL004 — Centralizar rótulos de CategoriaInsumo e UnidadeMedida na UI** do repositório `ajmarzola/Precificador`.

## Regra de escopo

A única especificação normativa desta entrega é:

```text
docs/development/improvements/MEL004-rotulos-insumos-ui.md
```

Documentos de UCs ou melhorias posteriores presentes no repositório **não autorizam sua implementação**.

## Leitura obrigatória

Antes de alterar código, leia:

1. `AGENTS.md`;
2. `docs/development/improvements/MEL004-rotulos-insumos-ui.md`;
3. `docs/development/testing-strategy.md`;
4. `docs/development/definition-of-done.md`;
5. `docs/development/melhorias.md`;
6. `src/Precificador.Core/Insumos/CategoriaInsumo.cs`;
7. `src/Precificador.Core/Insumos/UnidadeMedida.cs`;
8. `src/Precificador.Web/Pages/Insumos/Novo.cshtml`;
9. `src/Precificador.Web/Pages/Insumos/Index.cshtml`;
10. `src/Precificador.Web/Pages/Insumos/Detalhes.cshtml`;
11. testes Web de cadastro/listagem/detalhes de Insumos.

## Branch

Use:

```text
refactor/mel004-rotulos-insumos
```

Parta da `master` atualizada após o merge desta especificação.

## Objetivo técnico

Criar um único helper Web de apresentação para rótulos e opções de:

- `CategoriaInsumo`;
- `UnidadeMedida`.

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

Não crie serviço injetável.

## Rótulos obrigatórios

```text
MateriaPrima => Matéria-prima
Embalagem    => Embalagem
Consumivel   => Consumível

Grama       => g
Mililitro   => ml
Metro       => m
Unidade     => un
```

## Páginas a adaptar

### Novo.cshtml

Remover options duplicados e iterar as coleções do helper.

Preservar:

- placeholders;
- valores submetidos pelo model binding;
- ordem atual:
  - categorias: MateriaPrima, Embalagem, Consumivel;
  - unidades: Grama, Mililitro, Metro, Unidade.

### Index.cshtml

Remover funções Razor locais de categoria/unidade e usar o helper.

### Detalhes.cshtml

Remover funções Razor locais de categoria/unidade e usar o helper.

## Testes obrigatórios

Criar:

```text
tests/Precificador.Tests.Integration/Web/InsumoRotulosTests.cs
```

Sem servidor/banco/HTTP.

Adicionar dois testes focados:

```text
CA01_Categorias_possuem_rotulos_esperados_e_cobrem_todo_enum
CA02_Unidades_possuem_rotulos_esperados_e_cobrem_todo_enum
```

Além dos rótulos exatos, compare as coleções do helper com:

```csharp
Enum.GetValues<CategoriaInsumo>()
Enum.GetValues<UnidadeMedida>()
```

para garantir cobertura completa dos valores atuais.

Os testes Web existentes devem continuar verdes.

## Proibições

Não:

- alterar enums do Core;
- adicionar `DisplayAttribute` ao Core;
- criar resources/i18n;
- criar TagHelper;
- criar serviço DI;
- criar extensão genérica para todos os enums;
- centralizar Ativo/Inativo;
- alterar validação/model binding;
- criar migration/alterar ModelSnapshot;
- implementar UC003;
- implementar outras MELs;
- fazer refatorações oportunistas.

## Documentação pós-implementação

Ao concluir:

- mudar `docs/development/improvements/MEL004-rotulos-insumos-ui.md` para `Concluída`;
- marcar MEL004 como `Concluída` em `docs/development/melhorias.md`;
- não alterar status de MEL001–MEL003;
- não alterar UCs.

## Validação

Execute:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Antes da PR confirme:

1. build Release sem warnings novos relevantes;
2. suíte completa verde;
3. rótulos centralizados em um único ponto;
4. todos os valores atuais dos enums cobertos;
5. páginas preservam saída visual;
6. nenhum arquivo de Core foi alterado;
7. nenhuma migration/ModelSnapshot mudou;
8. MEL004 marcada como concluída;
9. diff restrito à melhoria.

## Retorno obrigatório ao final

Responda com:

```text
Implementação concluída

Resumo:
- ...

Validações:
- ...

Testes:
- Unitários: X/X
- Integração: X/X

Produção/schema:
- alteração apenas de apresentação Web
- nenhuma alteração de Core
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
refactor: centraliza rotulos de insumos na ui
```

Se houver qualquer pendência, não escreva "nenhuma".

Não faça merge em `master`.
