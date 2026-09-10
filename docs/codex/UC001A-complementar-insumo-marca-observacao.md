# Instrução Codex — UC001A Marca e observação do insumo

Você está implementando o **UC001A — Complementar cadastro de insumo com marca e observação** do repositório `ajmarzola/Precificador`.

## Objetivo

Evoluir o UC001 já implementado para suportar Marca e Observação no Insumo, alterando a unicidade para Nome + Marca e preservando bancos existentes.

Não implemente o UC002 nesta entrega.

## Leitura obrigatória

Leia integralmente:

1. `AGENTS.md`;
2. `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md`;
3. `docs/use-cases/UC001-cadastrar-insumo.md`;
4. `docs/features/F001-insumos.md`;
5. `docs/business/business-rules.md`;
6. `docs/architecture/architecture.md`;
7. `docs/development/testing-strategy.md`;
8. `docs/development/definition-of-done.md`.

A especificação normativa desta entrega é o documento do UC001A.

## Branch

Use branch dedicada:

```text
feat/uc001a-marca-observacao-insumo
```

Parta da `master` atualizada após o merge da documentação.

## Implementação obrigatória

Evolua `Insumo` para conter conceitualmente:

```text
Id
Nome
NomeNormalizado
Marca
MarcaNormalizada
Observacao
Categoria
UnidadeBase
Ativo
```

### Marca

- opcional;
- máximo 80 caracteres após normalização de espaços;
- preservar capitalização para exibição;
- `MarcaNormalizada` em maiúsculas invariáveis;
- ausência de marca: `Marca = null` e `MarcaNormalizada = ""`;
- não remover acentos.

### Observação

- opcional;
- máximo 1000 caracteres;
- trim externo;
- preservar conteúdo interno e quebras de linha;
- whitespace-only vira `null`.

### Unicidade

Substitua a unicidade de `NomeNormalizado` por índice único composto:

```text
NomeNormalizado + MarcaNormalizada
```

A validação web também deve usar a combinação Nome + Marca.

Mensagem funcional:

```text
Já existe um insumo cadastrado com esse nome e marca.
```

Não transforme qualquer `DbUpdateException` genericamente em duplicidade.

## Migration

Crie nova migration evolutiva. Não altere `CreateInsumos`.

A nova migration deve:

- remover `IX_Insumos_NomeNormalizado`;
- adicionar `Marca` nullable, max 80;
- adicionar `MarcaNormalizada` obrigatória, max 80, default `""` para registros existentes;
- adicionar `Observacao` nullable, max 1000;
- criar índice único composto de NomeNormalizado + MarcaNormalizada.

Não tente inferir marcas a partir do Nome de registros já existentes.

Teste tanto banco vazio quanto upgrade de banco já migrado até a migration anterior contendo dados.

## Razor Page

Atualize `/Insumos/Novo` adicionando:

- Marca opcional;
- Observação opcional em textarea.

Não implemente listagem nem detalhes.

Mantenha PRG e a mensagem de sucesso já existentes.

## Testes obrigatórios

### Unitários

Cobrir:

- marca válida;
- ausência de marca;
- normalização da marca;
- MarcaNormalizada;
- limite de 80 caracteres;
- observação válida;
- observação whitespace-only -> null;
- limite de 1000 caracteres.

Preserve os testes existentes do UC001.

### Integração

Cobrir:

- migration em banco vazio;
- upgrade da migration anterior com registro existente;
- preservação do registro existente;
- mesmo Nome com marcas diferentes permitido;
- mesmo Nome + mesma Marca rejeitado pelo índice;
- mesmo Nome sem marca duplicado rejeitado;
- persistência de Marca e Observação;
- formulário web com os novos campos;
- cadastro com e sem marca;
- duplicidade funcional por Nome + Marca;
- validações de tamanho.

Use SQLite real/in-memory com conexão mantida, nunca provider EF InMemory.

## Observação da ficha técnica

Não implemente observação de item da receita agora. O requisito foi registrado para o UC014 e é independente da Observação do Insumo.

## Restrições

Não implementar:

- UC002/listagem/detalhes;
- UC003/edição;
- UC004/desativação;
- UC005/preços;
- UC006/histórico;
- ficha técnica;
- produto;
- parser automático de marca;
- auto-migration no startup;
- repository genérico;
- CQRS/MediatR.

## Documentação

Ao concluir:

- alterar o status de `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md` para `Implementado`;
- não marcar UC002 como implementado;
- não alterar regras aprovadas para acomodar uma implementação diferente.

## Validação

Execute:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Valide também a migration sobre banco vazio e sobre um banco no estado imediatamente anterior.

Revise o diff e confirme ausência de funcionalidade do UC002+.

Commit sugerido:

```text
feat: adiciona marca e observacao ao insumo
```

Não faça merge em `master`.
