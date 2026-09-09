# UC001 — Cadastrar Insumo

- **Status:** Pronto para implementação
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** FT001 — Fundação Técnica implementada
- **Próximo caso relacionado:** UC002 — Listar e consultar insumos

## Objetivo

Permitir que o usuário cadastre um novo insumo que poderá posteriormente receber preços e ser utilizado em fichas técnicas.

Este UC inaugura o primeiro modelo persistente real do Precificador e, por isso, inclui a primeira migration do banco.

## Ator

Usuário local do Precificador.

## Pré-condições

- aplicação executável conforme FT001;
- SQLite e EF Core configurados;
- banco acessível pela connection string da aplicação;
- migrations aplicadas ao banco usado pela aplicação.

## Gatilho

O usuário acessa a página **Cadastrar insumo**.

## Dados informados pelo usuário

### Nome

- obrigatório;
- máximo de 120 caracteres após normalização de espaços;
- espaços no início e no fim são removidos;
- sequências de espaços em branco internos são reduzidas a um único espaço;
- a capitalização digitada pelo usuário é preservada para exibição.

Exemplo:

```text
"  Farinha   Renata Premium  "
```

é armazenado para exibição como:

```text
"Farinha Renata Premium"
```

### Categoria

Obrigatória e limitada a:

- Ingrediente;
- Embalagem;
- Consumível.

### Unidade base

Obrigatória e limitada a:

- `g` — grama;
- `ml` — mililitro;
- `un` — unidade.

### Situação

Não é informada pelo usuário neste UC. Todo novo insumo é criado como **ativo**.

## Fluxo principal

1. O usuário acessa `/Insumos/Novo`.
2. O sistema apresenta os campos Nome, Categoria e Unidade base.
3. O usuário preenche os campos e envia o formulário.
4. O sistema valida os dados de entrada.
5. O sistema normaliza o nome para armazenamento e comparação.
6. O sistema verifica se já existe outro insumo com o mesmo nome normalizado.
7. O sistema cria o insumo como ativo.
8. O sistema persiste o insumo no SQLite.
9. O sistema redireciona para `GET /Insumos/Novo` usando Post/Redirect/Get.
10. O formulário é apresentado limpo com mensagem de sucesso: **"Insumo cadastrado com sucesso."**

## Fluxos alternativos e exceções

### A1 — Campos obrigatórios ausentes

1. Nome, Categoria ou Unidade base não é informado ou é inválido.
2. O sistema não persiste o insumo.
3. A página permanece no formulário.
4. Os valores válidos já digitados são preservados.
5. O sistema apresenta mensagens de validação junto aos campos aplicáveis.

### A2 — Nome maior que o permitido

1. O nome normalizado possui mais de 120 caracteres.
2. O sistema não persiste o insumo.
3. O sistema informa que o nome deve possuir no máximo 120 caracteres.

### A3 — Insumo duplicado

1. Já existe um insumo, ativo ou inativo, com o mesmo nome normalizado.
2. O sistema não cria um novo registro.
3. O formulário permanece preenchido.
4. O sistema apresenta no campo Nome a mensagem: **"Já existe um insumo cadastrado com esse nome."**

A comparação de duplicidade ignora diferenças de maiúsculas/minúsculas e espaços excedentes, mas não remove acentos ou outros caracteres significativos.

Exemplos considerados iguais:

```text
Farinha Renata
farinha renata
  FARINHA   RENATA
```

Exemplos não considerados automaticamente iguais:

```text
Açúcar
Acucar
```

### A4 — Falha de persistência inesperada

1. O banco falha por motivo não representado por uma validação funcional conhecida.
2. O erro deve ser registrado pelo logging padrão.
3. O sistema não deve apresentar stack trace ao usuário fora do ambiente de desenvolvimento.
4. Não criar tratamento genérico que converta qualquer erro de banco em "duplicado".

## Regras de negócio aplicáveis

- RN001 — Unidade base;
- RN007 — Insumo sem preço;
- RN008 — Desativação de insumo;
- RN028 — Nome do insumo;
- RN029 — Unicidade do nome do insumo;
- RN030 — Categoria do insumo;
- RN031 — Situação inicial do insumo.

RN007 é relevante porque o insumo recém-criado ainda não possui preço: isso é estado válido e não implica custo zero.

## Modelo de domínio esperado

O primeiro modelo persistente deve representar, conceitualmente:

```text
Insumo
- Id: int
- Nome: string
- NomeNormalizado: string
- Categoria: CategoriaInsumo
- UnidadeBase: UnidadeMedida
- Ativo: bool
```

### Identidade

- `Id` inteiro gerado pelo banco;
- não exposto para edição pelo usuário.

### CategoriaInsumo

Valores explícitos, sem usar `0` como valor funcional válido:

```text
Ingrediente = 1
Embalagem = 2
Consumivel = 3
```

### UnidadeMedida

Valores explícitos, sem usar `0` como valor funcional válido:

```text
Grama = 1
Mililitro = 2
Unidade = 3
```

A interface apresenta os rótulos `g`, `ml` e `un`.

### NomeNormalizado

`NomeNormalizado` é um dado técnico persistido para garantir comparação determinística e índice único.

Sua geração deve:

1. usar o nome após normalização de espaços;
2. converter para maiúsculas com regra invariável (`ToUpperInvariant` ou comportamento equivalente);
3. não remover acentos.

O usuário não vê nem informa esse campo diretamente.

## Persistência

### Tabela

Criar tabela `Insumos` contendo, no mínimo:

- `Id` — chave primária, inteiro autoincremental;
- `Nome` — obrigatório, máximo 120;
- `NomeNormalizado` — obrigatório, máximo 120;
- `Categoria` — obrigatório;
- `UnidadeBase` — obrigatório;
- `Ativo` — obrigatório.

### Índice de unicidade

Criar índice único sobre `NomeNormalizado`.

A verificação no fluxo web melhora a mensagem para o usuário; o índice no banco é a garantia final de integridade.

### DbContext

Adicionar `DbSet<Insumo>` ao `PrecificadorDbContext`.

Mapeamento deve permanecer em Infrastructure. É permitido usar `IEntityTypeConfiguration<Insumo>` para manter o `DbContext` pequeno.

### Primeira migration

Criar a primeira migration real do projeto, com nome semelhante a:

```text
CreateInsumos
```

A migration deve ser gerada pelo EF Core e conter apenas o modelo necessário para este UC.

Não criar tabela de preços, produtos, ficha técnica ou configurações antecipadamente.

### Aplicação da migration

Este UC **não autoriza auto-migration no startup**. A aplicação de migrations permanece explícita com `dotnet ef database update` no ambiente local e nos testes que precisarem validar migrations.

## Interface web

Criar Razor Page:

```text
/Insumos/Novo
```

Arquivos esperados:

```text
Pages/Insumos/Novo.cshtml
Pages/Insumos/Novo.cshtml.cs
```

Usar um input model/view model próprio da página; não fazer model binding direto da entidade persistente.

### Campos

- Nome: input texto;
- Categoria: select com opção inicial não selecionada;
- Unidade base: select com opção inicial não selecionada.

### Validação

Usar validação server-side como fonte de verdade. A validação cliente fornecida pelo Razor Pages pode ser aproveitada, mas não substitui os testes e validações do servidor.

### Navegação mínima

Adicionar um acesso simples **Cadastrar insumo** na navegação existente ou na Home para que o UC seja descoberto sem digitar a URL.

Não criar a listagem de insumos antes do UC002.

## Critérios de aceitação

### CA01 — Página acessível

**Dado** a aplicação inicializada  
**Quando** o usuário acessar `/Insumos/Novo`  
**Então** a página deve responder com sucesso  
**E** apresentar Nome, Categoria e Unidade base.

### CA02 — Cadastro válido

**Dado** que não existe insumo de mesmo nome  
**Quando** o usuário cadastrar `Farinha Renata Premium`, categoria Ingrediente e unidade `g`  
**Então** um único insumo deve ser persistido  
**E** deve estar ativo  
**E** a resposta deve usar Post/Redirect/Get  
**E** após o redirect deve aparecer `Insumo cadastrado com sucesso.`

### CA03 — Normalização de espaços

**Quando** o usuário informar `  Farinha   Renata Premium  `  
**Então** o nome persistido deve ser `Farinha Renata Premium`.

### CA04 — Unicidade sem diferenciar caixa/espaços

**Dado** um insumo `Farinha Renata` já cadastrado  
**Quando** o usuário tentar cadastrar `  FARINHA   RENATA `  
**Então** nenhum novo registro deve ser criado  
**E** deve ser exibida a mensagem de duplicidade no campo Nome.

### CA05 — Duplicidade inclui inativos

**Dado** um insumo inativo com nome normalizado `AÇÚCAR`  
**Quando** o usuário tentar cadastrar `açúcar`  
**Então** nenhum novo registro deve ser criado.

### CA06 — Nome obrigatório

**Quando** o usuário enviar Nome vazio ou composto somente por espaços  
**Então** o sistema não deve persistir o insumo  
**E** deve apresentar erro de validação.

### CA07 — Limite do nome

**Quando** o nome normalizado exceder 120 caracteres  
**Então** o sistema não deve persistir o insumo  
**E** deve apresentar erro de validação.

### CA08 — Categoria obrigatória

**Quando** nenhuma categoria válida for selecionada  
**Então** o sistema não deve persistir o insumo.

### CA09 — Unidade obrigatória

**Quando** nenhuma unidade válida for selecionada  
**Então** o sistema não deve persistir o insumo.

### CA10 — Banco protege duplicidade

**Dado** dois registros com o mesmo `NomeNormalizado`  
**Quando** a persistência for tentada diretamente  
**Então** o banco deve rejeitar a violação do índice único.

### CA11 — Migration reproduzível

**Dado** um banco SQLite vazio  
**Quando** as migrations forem aplicadas  
**Então** a tabela `Insumos` e seu índice único devem ser criados com sucesso.

### CA12 — Sem escopo antecipado

**Ao revisar o diff**  
**Então** não devem existir tabela/entidade de preço de insumo, listagem, edição, desativação pela UI, produto, ficha técnica ou motor de precificação.

## Cenários de teste esperados

### Unitários

Cobrir o comportamento de domínio, no mínimo:

- criação válida define `Ativo = true`;
- normalização remove espaços externos e reduz espaços internos;
- normalização de comparação usa caixa invariável;
- nome vazio/espaços é rejeitado;
- nome acima de 120 caracteres é rejeitado;
- categoria fora dos valores definidos é rejeitada;
- unidade fora dos valores definidos é rejeitada.

Não testar EF Core em testes unitários.

### Integração — persistência

Cobrir, no mínimo:

- migration aplicada em SQLite vazio;
- persistência de um insumo válido;
- índice único impede `NomeNormalizado` duplicado;
- valores de Categoria, UnidadeBase e Ativo são recuperados corretamente.

Usar SQLite real temporário ou em memória com conexão adequadamente mantida. Não usar provider EF InMemory como substituto de SQLite.

### Integração — web

Cobrir, no mínimo:

- `GET /Insumos/Novo` retorna sucesso;
- POST válido persiste e redireciona;
- mensagem de sucesso aparece após redirect;
- POST inválido não persiste;
- tentativa de nome duplicado não persiste e apresenta mensagem funcional.

Os testes não podem tocar no `precificador.db` real do usuário.

## Impacto de documentação

Ao implementar:

- alterar este documento para `Status: Implementado`;
- manter F001 e as RNs coerentes caso alguma mudança aprovada seja necessária;
- não criar ADR se a implementação apenas seguir a arquitetura já aprovada.

## Fora do escopo deste UC

- listar/pesquisar insumos — UC002;
- editar insumo — UC003;
- desativar insumo pela interface — UC004;
- registrar preço — UC005;
- histórico de preços — UC006;
- cadastrar fornecedor;
- estoque;
- importação da planilha;
- preço inicial no mesmo formulário;
- exclusão física;
- reativação;
- auto-migration no startup;
- seed de dados reais;
- paginação;
- API REST.

## Definition of Done específica

Além da DoD global:

- `Insumo` e enums pertencem ao Core;
- mapeamento EF pertence ao Infrastructure;
- primeira migration real existe e é reproduzível;
- nome normalizado possui índice único no banco;
- formulário usa input model próprio;
- fluxo válido usa Post/Redirect/Get;
- testes unitários e de integração cobrem os critérios definidos;
- suite completa permanece verde;
- build Release termina sem warnings novos relevantes;
- nenhuma funcionalidade de UC002+ foi antecipada.
