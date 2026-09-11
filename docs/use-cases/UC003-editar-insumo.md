# UC003 — Editar insumo

- **Status:** Revalidado — pronto para implementação
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001, UC001B, UC001A, UC002 e FT002 implementados
- **Não depende de:** MEL001–MEL004
- **Próximo caso relacionado:** UC004 — Desativar insumo

## Objetivo

Permitir que o usuário autenticado altere os dados cadastrais de um Insumo pertencente à **Empresa Ativa**, preservando a identidade tenant-aware, as regras de normalização e a unicidade `(EmpresaId, NomeNormalizado, MarcaNormalizada)`.

O UC003 não altera situação ativo/inativo, preço, histórico, Empresa proprietária ou schema.

## Ator

Usuário autenticado do Precificador com Empresa Ativa válida.

## Pré-condições

- FT002 implementada;
- UC001, UC001B, UC001A e UC002 implementados;
- usuário autenticado;
- Empresa Ativa válida;
- Insumo pertencente à Empresa Ativa.

## Gatilho

O usuário consulta um Insumo e escolhe **Editar**.

## Rota

Criar Razor Page:

```text
/Insumos/Editar/{id:int}
```

O `id` da rota identifica o registro a editar.

Não expor ou aceitar `EmpresaId` como dado do formulário.

## Campos editáveis

O UC003 permite alterar:

1. Nome;
2. Marca;
3. Categoria;
4. Unidade base;
5. Observação.

As mesmas regras já existentes no cadastro devem ser reaplicadas.

### Não editáveis neste UC

Não permitir alteração de:

- `Id`;
- `EmpresaId`;
- `Ativo`;
- `NomeNormalizado` diretamente;
- `MarcaNormalizada` diretamente.

`NomeNormalizado` e `MarcaNormalizada` devem ser recalculados pelo domínio a partir de Nome e Marca.

A situação do Insumo continua sendo responsabilidade do UC004.

## Regras dos campos

### Nome

Aplicar RN028:

- obrigatório;
- máximo 120 caracteres após normalização;
- trim externo;
- colapso de whitespace interno;
- capitalização de exibição preservada;
- `NomeNormalizado` em maiúsculas invariantes;
- acentos preservados.

### Marca

Aplicar RN032:

- opcional;
- máximo 80 caracteres após normalização;
- trim externo;
- colapso de whitespace interno;
- capitalização de exibição preservada;
- ausência => `Marca = null`;
- ausência => `MarcaNormalizada = ""`;
- acentos preservados.

### Categoria

Aplicar RN030.

Valores válidos:

- Matéria-prima;
- Embalagem;
- Consumível.

Nenhum valor zero/indefinido é válido.

### Unidade base

Aplicar RN001.

Valores válidos:

- g;
- ml;
- m;
- un.

Nenhum valor zero/indefinido é válido.

### Observação

Aplicar RN033:

- opcional;
- máximo 1000 caracteres;
- trim apenas externo;
- conteúdo interno/quebras preservados;
- whitespace-only => `null`.

## Alteração de domínio

A edição deve ocorrer por comportamento explícito do domínio, preferencialmente:

```csharp
insumo.AtualizarDados(nome, categoria, unidadeBase, marca, observacao);
```

ou nome equivalente coerente.

Não liberar setters públicos.

### Atomicidade da atualização

O método deve:

1. normalizar os novos valores em variáveis locais;
2. validar todos os novos valores;
3. somente após todas as validações terem passado, alterar o estado da entidade.

Se qualquer validação falhar, o Insumo deve permanecer integralmente com os dados anteriores.

O método não recebe nem altera `EmpresaId` ou `Ativo`.

## Unicidade durante edição

A RN029 permanece válida:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

Ao validar duplicidade na Web, excluir o próprio registro:

```text
Id != id sendo editado
```

Consequências:

- salvar o registro sem alterar Nome/Marca deve ser permitido;
- mudar outros campos mantendo Nome/Marca deve ser permitido;
- mudar para Nome+Marca já usados por outro Insumo da mesma Empresa deve ser rejeitado;
- a mesma combinação existente em outra Empresa não deve bloquear a edição.

Mensagem funcional de duplicidade:

```text
Já existe um insumo cadastrado com esse nome e marca.
```

A integridade final continua protegida pelo índice único do banco.

Não transformar qualquer `DbUpdateException` genericamente em duplicidade.

## Isolamento por Empresa

Preservar integralmente FT002:

- usar Global Query Filter;
- não usar `IgnoreQueryFilters` no fluxo comum;
- não confiar em `EmpresaId` vindo de request;
- não adicionar `Where(EmpresaId == ...)` como substituto do filtro central;
- o guard tenant-aware continua ativo;
- um Insumo de outra Empresa é indistinguível de um ID inexistente.

### GET

Se o `id` não existir na Empresa Ativa:

```text
HTTP 404
```

Isso inclui registro pertencente a outro tenant.

### POST

Antes de atualizar, buscar novamente o Insumo pelo `id` com o Global Query Filter ativo.

Se não existir na Empresa Ativa:

```text
HTTP 404
```

Não usar dados carregados no GET como autorização para o POST.

## GET da edição

No GET:

- usar consulta de leitura sem tracking quando apropriado;
- carregar apenas o Insumo visível à Empresa Ativa;
- preencher o formulário com os valores atuais;
- não mostrar campos técnicos;
- não mostrar campo de situação editável.

## POST da edição

No POST:

1. localizar o Insumo pelo `id` da rota, com Global Query Filter ativo;
2. se não existir, retornar 404;
3. validar dados obrigatórios e enums;
4. chamar o comportamento de domínio de atualização;
5. validar duplicidade na Empresa Ativa excluindo o próprio Id;
6. persistir;
7. usar Post/Redirect/Get.

Após sucesso:

```text
Insumo atualizado com sucesso.
```

Redirecionar preferencialmente para:

```text
/Insumos/Detalhes/{id}
```

A página de detalhes deve exibir a mensagem de sucesso de forma adequada, por exemplo via `TempData`.

## Navegação

Em:

```text
/Insumos/Detalhes/{id}
```

adicionar ação **Editar** para o Insumo atual.

Não é obrigatório adicionar ação Editar diretamente na listagem.

A página de edição deve oferecer:

- Salvar;
- Cancelar/Voltar para os detalhes.

Não adicionar ação de desativar/reativar.

## Apresentação de Categoria e Unidade

A implementação deve reutilizar o mecanismo de apresentação existente na `master` no momento da implementação.

Se a MEL004 já estiver implementada, reutilizar `InsumoRotulos` (ou o nome efetivamente adotado) para Categoria e Unidade.

Não recriar switches/options duplicados.

## Compatibilidade com histórico futuro

No estado atual do sistema ainda não existem Preço de Insumo ou itens de Ficha Técnica. Portanto, Nome, Marca e Unidade base podem ser corrigidos neste UC.

Antes de implementar UC005 e UC014, deve ser reavaliado se alterações desses campos continuam permitidas após existirem registros históricos/dependentes, pois:

- mudar Marca pode reinterpretar a identidade econômica ligada a preços anteriores;
- mudar Unidade base pode reinterpretar quantidades/preços históricos;
- mudar Nome/Marca pode alterar a leitura histórica de referências existentes.

Essa revalidação futura não bloqueia o UC003 atual.

## Regras de negócio aplicáveis

- RN001 — Unidade base;
- RN008 — Desativação de Insumo;
- RN028 — Nome do Insumo;
- RN029 — Unicidade do Insumo por Empresa, Nome e Marca;
- RN030 — Categoria do Insumo;
- RN032 — Marca do Insumo;
- RN033 — Observação do Insumo;
- RN035 — Propriedade por Empresa;
- RN036 — Isolamento de Empresa;
- RN037 — Empresa Ativa.

## Critérios de aceitação

### CA01 — Acesso protegido

Usuário anônimo não pode acessar `/Insumos/Editar/{id}` fora do fluxo de autenticação existente.

### CA02 — GET carrega dados da Empresa Ativa

Dado um Insumo pertencente à Empresa Ativa, o GET retorna sucesso e preenche Nome, Marca, Categoria, Unidade base e Observação.

### CA03 — Campos técnicos não são editáveis

O formulário não deve possuir inputs editáveis para:

- Id;
- EmpresaId;
- Ativo;
- NomeNormalizado;
- MarcaNormalizada.

O Id pode existir apenas na rota.

### CA04 — Edição válida atualiza dados cadastrais

Ao salvar valores válidos, os cinco campos editáveis e suas representações normalizadas devem refletir os novos valores.

`EmpresaId` e `Ativo` devem permanecer inalterados.

### CA05 — Atualização é atômica

Se qualquer novo valor for inválido, a entidade não deve permanecer parcialmente alterada.

### CA06 — Salvar sem mudar identidade é permitido

Editar apenas Categoria, Unidade ou Observação, mantendo Nome/Marca, não deve ser tratado como duplicidade do próprio registro.

### CA07 — Duplicidade na mesma Empresa é rejeitada

Se outro Insumo da mesma Empresa já possuir o Nome+Marca normalizados desejados:

- não persistir a edição;
- exibir `Já existe um insumo cadastrado com esse nome e marca.`.

### CA08 — Mesmo Nome+Marca em outra Empresa não bloqueia

Combinação existente somente em outra Empresa não deve impedir a edição na Empresa Ativa.

### CA09 — Normalizações permanecem válidas

Edição deve reaplicar as regras de Nome, Marca e Observação já usadas no cadastro.

### CA10 — Validações de limites permanecem válidas

Nome > 120, Marca > 80, Observação > 1000 ou enums inválidos devem ser rejeitados e não persistidos.

### CA11 — Id inexistente retorna 404

GET e POST para Id inexistente retornam HTTP 404.

### CA12 — Id de outro tenant retorna 404

GET e POST para Id pertencente a outra Empresa retornam HTTP 404 e não alteram o registro.

### CA13 — PRG e mensagem de sucesso

Edição válida usa Post/Redirect/Get e apresenta `Insumo atualizado com sucesso.`.

### CA14 — Navegação a partir dos detalhes

Detalhes possui ação **Editar** apontando para o registro corrente.

### CA15 — Situação fora do escopo

O UC003 não ativa, desativa ou oferece campo para alterar `Ativo`.

### CA16 — Sem migration

Nenhuma migration ou alteração de schema é necessária.

### CA17 — Sem escopo antecipado

Não implementar UC004, UC005, UC006, Produto ou Ficha Técnica.

## Matriz de testes fechada antes da implementação

Os nomes abaixo são orientativos, mas os testes devem permanecer focados e rastreáveis aos critérios.

### Unitários — domínio

Adicionar testes em `InsumoTests` ou arquivo focado equivalente.

#### U1

```text
CA04_Atualizar_dados_validos_altera_campos_editaveis_e_preserva_tenant_e_status
```

Provar:

- Nome/Marca/Categoria/Unidade/Observação atualizados;
- NomeNormalizado/MarcaNormalizada recalculados;
- EmpresaId preservado;
- Ativo preservado.

#### U2

```text
CA09_Atualizar_normaliza_nome_marca_e_observacao
```

Provar as mesmas regras de normalização do cadastro.

#### U3

```text
CA05_Atualizacao_textual_invalida_preserva_estado_original
```

Pode usar Theory para casos equivalentes de Nome/Marca/Observação inválidos, desde que cada falha prove estado integralmente preservado.

#### U4

```text
CA05_Atualizacao_com_categoria_ou_unidade_invalida_preserva_estado_original
```

Pode usar Theory para Categoria/Unidade inválidas.

Evitar um único teste agregado para todos os comportamentos.

### Integração — persistência

#### P1

```text
CA04_Edicao_valida_e_persistida_sem_alterar_empresa
```

Usar SQLite real/in-memory e migrations existentes.

#### P2

```text
CA07_Indice_unico_rejeita_edicao_para_nome_marca_duplicados_na_mesma_empresa
```

Provar que a proteção final do banco continua válida também após alteração de entidade.

Não criar migration.

### Integração — Web

Criar testes focados; não concentrar todos os critérios em um único cenário.

Cobrir no mínimo:

#### W1

```text
CA01_Edicao_exige_autenticacao
```

#### W2

```text
CA02_Get_edicao_carrega_campos_funcionais_sem_campos_tecnicos
```

#### W3

```text
CA04_Post_valido_atualiza_insumo_da_empresa_ativa_e_redireciona
```

Confirmar mensagem de sucesso após redirect.

#### W4

```text
CA06_Post_sem_alterar_nome_marca_nao_detecta_o_proprio_registro_como_duplicado
```

#### W5

```text
CA07_Post_para_nome_marca_de_outro_insumo_da_mesma_empresa_e_rejeitado
```

#### W6

```text
CA08_Post_para_combinacao_existente_apenas_em_outra_empresa_e_permitido
```

Além de permitir, confirmar isolamento entre tenants.

#### W7

```text
CA10_Post_com_dados_invalidos_nao_persiste_alteracoes
```

Pode ser dividido se ficar excessivamente agregado.

#### W8

```text
CA11_Get_e_post_de_id_inexistente_retornam_404
```

Se a clareza melhorar, separar GET e POST.

#### W9

```text
CA12_Get_e_post_de_id_de_outro_tenant_retornam_404_sem_alterar_registro
```

Confirmar que o registro do outro tenant permanece intacto.

#### W10

```text
CA14_Detalhes_exibe_link_editar_do_registro
```

Os testes não usam o banco real do usuário.

## Persistência e migration

Nenhuma alteração de schema é necessária.

Não criar migration.

Não editar migrations históricas.

Não alterar `PrecificadorDbContextModelSnapshot`.

Se surgir necessidade real de schema, interromper e revisar antes de ampliar o escopo.

## Fora do escopo

- desativação/reativação — UC004;
- preço — UC005;
- histórico de preço — UC006;
- auditoria/versionamento de alterações cadastrais;
- edição em massa;
- exclusão física;
- alteração de Empresa proprietária;
- Produto;
- Ficha Técnica;
- API REST;
- optimistic concurrency token;
- mudanças em autenticação/tenancy;
- refatorações oportunistas não necessárias ao UC.

## Definition of Done específica

Além da DoD global:

- `/Insumos/Editar/{id}` funciona apenas para registro visível à Empresa Ativa;
- GET/POST cross-tenant retornam 404;
- somente os cinco campos cadastrais permitidos são editáveis;
- domínio atualiza dados de forma atômica;
- EmpresaId e Ativo permanecem preservados;
- normalização e unicidade continuam válidas;
- próprio registro é excluído da validação de duplicidade;
- detalhes oferece ação Editar;
- PRG + mensagem de sucesso funcionam;
- não há migration/snapshot;
- matriz de testes está coberta com testes focados;
- documentação pós-implementação deixa UC003 como Implementado;
- F001, catálogo e ordem de implementação ficam coerentes;
- UC004 passa a ser o próximo caso a detalhar/revalidar, sem ser implementado nesta PR;
- toda a suíte está verde e build Release sem warnings novos relevantes;
- qualquer ideia útil fora do escopo é registrada em `docs/development/melhorias.md`.
