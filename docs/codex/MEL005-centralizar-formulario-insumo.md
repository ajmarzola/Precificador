# Instrução Codex — MEL005 Centralizar formulário de Insumo

Implemente a **MEL005 — Centralizar entrada e validação de Insumo entre Novo e Editar**.

## Fonte normativa

Leia:

1. `AGENTS.md`;
2. `docs/development/improvements/MEL005-centralizar-formulario-insumo.md`;
3. `docs/development/melhorias.md`;
4. `docs/use-cases/UC003-editar-insumo.md`;
5. `docs/use-cases/UC005-registrar-preco-insumo.md`;
6. `src/Precificador.Web/Pages/Insumos/Novo.cshtml.cs`;
7. `src/Precificador.Web/Pages/Insumos/Editar.cshtml.cs`;
8. testes Web de Novo, Editar e preço/RN040.

## Branch

Use:

~~~text
refactor/mel005-formulario-insumo
~~~

Parta da master atual.

## Objetivo

Remover duplicação real entre Novo e Editar sem alterar comportamento.

Centralizar somente:

- InputModel;
- validação Web de Categoria/Unidade;
- mapeamento de ArgumentException para ModelState;
- mensagem de duplicidade.

## Estrutura esperada

Criar, preferencialmente:

~~~text
src/Precificador.Web/Pages/Insumos/InsumoInputModel.cs
src/Precificador.Web/Pages/Insumos/InsumoFormulario.cs
~~~

### InsumoInputModel

Campos:

~~~text
Nome
Marca
Categoria
UnidadeBase
Observacao
~~~

com os mesmos Display attributes atuais.

Novo e Editar passam a usar esse tipo.

### InsumoFormulario

Helper estático pequeno com:

- MensagemDuplicidade;
- ValidarCamposObrigatorios;
- AdicionarErroDominio ou equivalente.

Preservar textos exatos.

## Não centralizar

Não mover para o helper:

- query de duplicidade;
- SaveChanges;
- redirects;
- TempData;
- logging;
- carregamento de Insumo;
- PossuiHistorico;
- RN040;
- lógica tenant;
- Razor markup.

Não criar base class de PageModel.

Não criar serviço/repository.

## RN040

Editar deve continuar:

- verificando histórico;
- restaurando Nome/Marca/Unidade persistidos no POST;
- impedindo request manipulado;
- apresentando readonly/disabled no GET.

Nenhuma regressão é aceitável.

## Testes

Preservar integralmente:

~~~text
NovoInsumoPageTests
EditarInsumoPageTests
PrecoInsumoPageTests
~~~

Adicionar:

~~~text
MEL005_Novo_com_categoria_invalida_exibe_erro_e_nao_persiste
MEL005_Novo_com_unidade_invalida_exibe_erro_e_nao_persiste
~~~

Não criar testes estruturais de implementação.

## Proibições

Não:

- alterar Core;
- alterar Insumo;
- alterar enums;
- alterar schema;
- criar migration;
- alterar ModelSnapshot;
- criar partial compartilhado;
- alterar textos;
- centralizar query EF;
- implementar outras MELs/UCs.

## Validação

Execute:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Confirme:

1. build sem warnings novos;
2. suíte completa verde;
3. Novo/Editar compartilham input;
4. validação compartilhada;
5. erro de domínio compartilhado;
6. mensagem de duplicidade compartilhada;
7. RN040 intacta;
8. nenhuma migration/schema;
9. nenhum Core alterado.

## Documentação pós-implementação

Atualizar:

- MEL005 em `melhorias.md` -> Concluída;
- documento MEL005 -> Status: Concluída.

Não alterar estados de UCs não relacionados.

## Retorno obrigatório

~~~text
Implementação concluída

Resumo:
- ...

Validações:
- ...

Testes:
- Unitários: X/X
- Integração: X/X

Produção/schema:
- refatoração somente Web
- nenhuma migration/ModelSnapshot

Pendências/observações:
- ...

Mensagem de commit sugerida:
refactor: centraliza formulario de insumo
~~~

Não faça merge em master.
