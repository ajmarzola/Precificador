# Codex — UC032 — Administrar categorias de Produto

Implemente exclusivamente a UC032 conforme:

```text
docs/use-cases/UC032-administrar-categorias-produto.md
```

## Branch obrigatória

```text
feat/uc032-categorias-produto
```

Nunca editar `master` diretamente.

## Objetivo

Substituir a Categoria livre textual de `Produto` por uma entidade tenant-aware `CategoriaProduto`, administrável pela Empresa Ativa, preservando os dados existentes.

A UC032 prepara a UC036, mas **não implementa desgaste de equipamentos nem altera qualquer fórmula de precificação**.

## Modelo obrigatório

Criar:

```text
CategoriaProduto : IEntidadeEmpresa
- Id
- EmpresaId
- Nome
- NomeNormalizado
- Ativo
```

E evoluir Produto de:

```text
Categoria : string?
```

para:

```text
CategoriaProdutoId : int?
```

A Categoria continua opcional.

Não manter a string antiga como estado persistido definitivo.

## CategoriaProduto

Nome:

- obrigatório;
- máximo 80;
- trim externo;
- whitespace interno colapsado;
- capitalização preservada;
- `NomeNormalizado = Nome.ToUpperInvariant()`.

Unicidade:

```text
EmpresaId + NomeNormalizado
```

Situação:

- nova Categoria nasce ativa;
- desativar/reativar reversível e idempotente;
- inativa continua editável;
- sem hard delete.

Não permitir reatribuição de Empresa.

## Migration obrigatória

Criar migration SQL Server evolutiva.

Fluxo:

1. criar `CategoriasProdutos`;
2. adicionar `Produtos.CategoriaProdutoId nullable`;
3. converter os textos legados não nulos em Categorias por Empresa;
4. vincular Produtos;
5. criar FK/índices;
6. remover `Produtos.Categoria`;
7. atualizar snapshot.

Regras de backfill:

- Categoria null continua null;
- Categorias migradas nascem ativas;
- variantes `Agenda`, `AGENDA`, `agenda` da mesma Empresa viram uma única Categoria;
- em variantes, usar como Nome de exibição o texto do Produto de menor Id;
- o mesmo nome em Empresas diferentes gera Categorias diferentes.

Criar teste evolutivo real da migration partindo do estado imediatamente anterior à UC032.

Não rebaselinear migrations.

## Persistência tenant-aware

Adicionar:

- DbSet;
- configuration;
- GQF;
- guard central via `IEntidadeEmpresa`;
- validação sync/async Produto -> Categoria.

A persistência deve rejeitar Produto ligado a Categoria de outra Empresa, mesmo se fabricado fora da Web.

FK Produto -> Categoria:

```text
nullable
ON DELETE RESTRICT
```

## Web — administração

Criar:

```text
/Produtos/Categorias
/Produtos/Categorias/Novo
/Produtos/Categorias/Editar/{id:int}
```

Index:

- Nome;
- Situação;
- Editar;
- Desativar/Reativar;
- Nova categoria.

Listar ativas e inativas da Empresa Ativa.

Cadastro/edição devem tratar duplicidade amigavelmente.

Mensagens:

```text
Categoria de produto cadastrada com sucesso.
Categoria de produto atualizada com sucesso.
Categoria de produto desativada com sucesso.
Categoria de produto reativada com sucesso.
```

Mutações usam POST + antiforgery + PRG.

GET nunca altera estado.

## Produto Novo

Remover textbox livre de Categoria.

Usar seletor:

```text
Sem categoria
+ Categorias ativas da Empresa Ativa
```

Ordenar por NomeNormalizado.

POST com id inexistente, cross-tenant ou inativo é inválido.

Não aceitar nome livre de Categoria.

POST inválido deve recarregar as opções e preservar inputs.

## Produto Editar

GET:

- categorias ativas;
- se Categoria atual estiver inativa, incluí-la selecionada;
- Sem categoria continua disponível.

POST:

- manter mesma Categoria inativa atual é permitido;
- remover é permitido;
- trocar para Categoria ativa é permitido;
- trocar para outra Categoria inativa é proibido;
- inexistente/cross-tenant é inválido.

Produto inativo continua editável conforme UC010 e não é reativado implicitamente.

## Desativação da Categoria

Desativar não pode:

- desvincular Produtos;
- colocar CategoriaProdutoId em null;
- bloquear cálculo;
- alterar Produto.

Produtos existentes continuam exibindo a Categoria inativa.

A Categoria apenas deixa de ser elegível para novas associações.

## Apresentação

Localize todas as referências ativas a:

```text
Produto.Categoria
p.Categoria
Input.Categoria
```

e adapte para a Categoria estruturada.

Revisar no mínimo:

- Produtos/Index;
- Produtos/Detalhes;
- Produtos/Novo;
- Produtos/Editar;
- Ficha Técnica;
- Precificação;
- Registro de preço;
- Histórico de precificação;
- projeções/resumos relacionados.

Produto sem Categoria deve continuar exibindo ausência coerente (`—` onde já usado).

Não duplicar Nome da Categoria em Produto.

## Não implementar UC036

É proibido nesta PR criar:

- Tipo/Forma de cálculo de desgaste;
- Valor de desgaste;
- Percentual de desgaste;
- `CustoDesgasteEquipamentosLote`;
- calculadora de desgaste;
- alteração de `CalculadoraCustoProduto`;
- alteração de `PrecificacaoProdutoAtual` por causa de desgaste;
- novo snapshot relacionado a desgaste.

Apenas deixe a Categoria estruturada pronta para extensão posterior.

## Documentação obrigatória na implementação

Alinhar:

- UC032 -> Concluído;
- backlog;
- F002;
- RN043;
- UC007;
- UC008;
- UC009;
- UC010;
- catálogo funcional.

UC036 permanece Planejado e é o próximo item.

MEL021 permanece sem implementação e bloqueada até UC036 e resolução do bloqueio externo da conta Azure.

## Testes obrigatórios

Cubra a matriz U1-U7, P1-P13 e W1-W25 da especificação.

Pontos especialmente críticos:

- migration/backfill;
- duplicidade por capitalização;
- mesmo nome em tenants diferentes;
- GQF;
- guard cross-tenant;
- FK Produto -> Categoria;
- desativação sem desvincular Produto;
- seletor apenas com ativas;
- preservação da Categoria inativa atual no Editar;
- request manipulado para outra Categoria inativa/cross-tenant;
- Produto sem Categoria;
- regressões UC007–UC010.

Não enfraquecer testes de integração usando provider InMemory/SQLite. A suíte continua em SQL Server real conforme MEL020.

## Validação final

Executar:

```text
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Confirmar:

- 0 erros;
- nenhum warning novo relevante;
- unitários verdes;
- integração SQL Server/Web verde;
- migration testada;
- nenhuma migration pendente;
- nenhuma fórmula de precificação alterada;
- nenhum campo de desgaste criado;
- UC036 não implementada;
- MEL021 não implementada.

Ao finalizar, informar:

- arquivos alterados;
- migration criada;
- estratégia de backfill;
- contagens de testes;
- confirmação explícita de que desgaste/precificação não foram implementados.
