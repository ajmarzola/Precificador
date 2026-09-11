# Instrução Codex — UC007 Cadastrar produto

Implemente o **UC007 — Cadastrar produto**.

## Pré-condição de fila

Não iniciar enquanto o UC006 não estiver implementado, revisado e mergeado na master.

Antes de codificar, confirme que a master contém o UC006 como Implementado.

## Fonte normativa

Leia integralmente:

1. `AGENTS.md`;
2. `docs/use-cases/UC007-cadastrar-produto.md`;
3. `docs/features/F002-produtos.md`;
4. `docs/business/business-rules.md`;
5. `docs/product/glossary.md`;
6. `docs/product/scope.md`;
7. `docs/product/multiempresa-generalizacao.md`;
8. `docs/development/foundation-multiempresa-auth.md`;
9. `docs/development/testing-strategy.md`;
10. `docs/development/definition-of-done.md`;
11. `docs/development/melhorias.md`;
12. código real atual de Empresa, Insumo e PrecoInsumo apenas como referência dos padrões tenant-aware, sem copiar abstrações desnecessárias.

## Branch

~~~text
feat/uc007-cadastrar-produto
~~~

Parta da master atualizada após o UC006.

## Modelo

Criar:

~~~text
Produto : IEntidadeEmpresa
- Id
- EmpresaId
- Nome
- NomeNormalizado
- Categoria?
- MargemAlvo
- Ativo
~~~

Factory sugerida:

~~~csharp
Produto.Criar(int empresaId, string nome, decimal margemAlvo, string? categoria = null)
~~~

Sem setters públicos para ownership/status.

## Regras

### Nome

- obrigatório;
- max 120;
- trim externo;
- colapsar whitespace interno;
- NomeNormalizado uppercase invariant;
- acentos preservados.

### Categoria

- string opcional;
- max 80;
- trim + collapse whitespace;
- whitespace => null;
- não é enum;
- não participa da identidade.

### MargemAlvo

Domínio armazena fração:

~~~text
30% = 0.30
~~~

Validar:

~~~text
0 <= MargemAlvo < 1
~~~

### Ativo

Novo Produto sempre ativo.

### Unicidade

~~~text
EmpresaId + NomeNormalizado
~~~

Mensagem Web:

~~~text
Já existe um produto cadastrado com esse nome.
~~~

## Persistência

Adicionar:

~~~csharp
DbSet<Produto> Produtos
~~~

Configuration dedicada.

Mapear:

- Produtos;
- Nome max120;
- NomeNormalizado max120;
- Categoria max80 nullable;
- MargemAlvo decimal(9,6);
- FK Empresa Restrict;
- unique (EmpresaId, NomeNormalizado).

Adicionar Global Query Filter de Empresa Ativa.

O guard central deve cobrir Produto automaticamente via IEntidadeEmpresa.

## Migration

Gerar:

~~~text
AddProdutos
~~~

ou equivalente claro.

Não editar migrations antigas.

Não usar EnsureCreated/auto migration.

Validar upgrade do schema existente.

## Web

Proteger pasta /Produtos pela policy EmpresaAtiva.

Criar:

~~~text
/Produtos/Novo
~~~

InputModel somente:

- Nome;
- Categoria;
- MargemAlvoPercentual.

Converter percentual para fração antes de chamar o domínio:

~~~text
MargemAlvoPercentual / 100m
~~~

Não bindar EmpresaId, NomeNormalizado ou Ativo.

Fluxo:

1. obter Empresa Ativa;
2. validar input;
3. criar domínio;
4. verificar duplicidade tenant-aware;
5. persistir;
6. PRG para /Produtos/Novo;
7. TempData: Produto cadastrado com sucesso.

Adicionar na Home link **Cadastrar produto**.

Não criar Produtos/Index neste UC.

## Escopo proibido

Não criar:

- preço de venda;
- histórico de venda;
- Ficha Técnica;
- rendimento;
- custo;
- cálculo;
- lista/detalhes;
- edição/desativação;
- CategoriaProduto;
- SKU;
- imagem;
- margem padrão de Configurações.

## Testes

Siga literalmente a matriz do UC007.

### Unitários

- criação válida;
- nome/normalização;
- categoria opcional/limite;
- margem [0,1);
- ownership imutável.

### Persistência

- migration upgrade;
- round-trip;
- unique same tenant;
- same name other tenant;
- query filter;
- guard cross-tenant;
- FK Empresa.

### Web

- autenticação;
- campos autorizados;
- POST válido + 30% -> 0.30 + PRG;
- inválidos um por cenário;
- duplicidade;
- outro tenant;
- request manipulando EmpresaId/Ativo não controla servidor;
- link Home.

## Cinco regras permanentes

1. matriz de testes é contrato;
2. DoD inclui docs;
3. nomes refletem CAs;
4. prefira testes focados;
5. melhoria fora do escopo vai para melhorias.md.

## Validação

Execute:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Antes da PR confirme:

1. migration nova + snapshot coerente;
2. migrations antigas intocadas;
3. build sem warnings novos;
4. suíte completa verde;
5. Produto tenant-owned;
6. query filter/guard;
7. unique tenant-aware;
8. margem como fração;
9. nenhum preço/Ficha;
10. nenhum UC008+.

## Documentação pós-implementação

- UC007 => Implementado;
- atualizar F002;
- catálogo;
- ordem;
- business rules/glossário se necessário para refletir o código real;
- UC008 passa a próximo caso;
- não alterar gates futuros sem necessidade.

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
- Produto + migration AddProdutos (ou nome efetivo)
- nenhuma entidade de preço de venda/ficha técnica

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: cadastra produto
~~~

Se houver pendência, não escreva "nenhuma".

Não faça merge em master.
