# UC004 — Desativar e reativar insumo

- **Status:** Implementado
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC003 e FT002 implementados
- **Não depende de:** MEL005, UC005 ou funcionalidades posteriores
- **Próximo caso relacionado:** UC005 — Registrar preço de insumo

## Objetivo

Permitir alterar a situação operacional de um Insumo da Empresa Ativa entre **Ativo** e **Inativo**, sem exclusão física e sem permitir alteração arbitrária do campo `Ativo` por binding.

O UC004 implementa o ciclo reversível:

```text
Ativo -> Desativar -> Inativo
Inativo -> Reativar -> Ativo
```

O registro continua existindo, continua consultável e continua participando da unicidade `(EmpresaId, NomeNormalizado, MarcaNormalizada)`.

## Decisão de UX

Não criar nova Razor Page.

Usar a página existente:

```text
/Insumos/Detalhes/{id:int}
```

Nela:

- Insumo ativo exibe ação **Desativar**;
- Insumo inativo exibe ação **Reativar**;
- nunca exibir as duas ações ao mesmo tempo;
- a situação atual continua visível em detalhes;
- listagem continua exibindo ativos e inativos.

As alterações de situação devem ser feitas por **POST handlers** da própria página de detalhes.

Handlers sugeridos:

```csharp
OnPostDesativarAsync(int id)
OnPostReativarAsync(int id)
```

Não criar um formulário com `Ativo=true/false` enviado pelo cliente.

## Confirmação

A desativação deve solicitar confirmação simples antes do POST, por exemplo com `confirm()` nativo do navegador.

Texto sugerido:

```text
Deseja desativar este insumo?
```

Não criar modal customizado ou dependência JavaScript adicional.

A reativação não exige confirmação.

## Domínio

Adicionar comportamentos explícitos em `Insumo`:

```csharp
public void Desativar()
public void Reativar()
```

Não expor setter público de `Ativo`.

### Semântica

`Desativar()`:

```text
Ativo = false
```

`Reativar()`:

```text
Ativo = true
```

As operações devem ser idempotentes:

- desativar um Insumo já inativo não lança exceção;
- reativar um Insumo já ativo não lança exceção.

Isso evita erro desnecessário em reenvio/stale UI e mantém a operação simples.

Nenhum outro dado da entidade deve ser alterado.

## Regra de negócio

Aplicar RN008:

- Insumo é desativado, não excluído;
- referências existentes futuras devem permanecer legíveis;
- Insumo inativo não poderá ser adicionado a novas fichas técnicas quando esse fluxo existir;
- reativar devolve elegibilidade operacional futura;
- a desativação não apaga preços/históricos futuros.

## Unicidade e identidade

A situação **não participa da identidade funcional**.

Logo, um Insumo inativo continua ocupando sua combinação:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

Consequências:

- desativar não permite cadastrar outro Insumo da mesma Empresa com o mesmo Nome+Marca;
- reativar não exige nova validação de duplicidade;
- o índice único existente continua válido sem alteração.

## Edição de inativos

O UC004 não altera o comportamento implementado pelo UC003.

Portanto, um Insumo inativo continua:

- consultável;
- pesquisável/listável;
- editável nos campos cadastrais permitidos pelo UC003.

Isso poderá ser reavaliado no futuro se surgirem regras operacionais adicionais, mas não deve ser restringido neste UC.

## Isolamento por Empresa

Preservar FT002 integralmente.

Nos handlers de POST:

1. buscar novamente o Insumo pelo `id`;
2. manter Global Query Filter ativo;
3. não usar `IgnoreQueryFilters`;
4. não aceitar `EmpresaId` do request;
5. ID inexistente ou pertencente a outro tenant => **HTTP 404**;
6. o guard de escrita tenant-aware continua ativo.

Um Insumo de outra Empresa deve permanecer indistinguível de um ID inexistente.

## POST — Desativar

Fluxo:

1. receber `id` da rota;
2. buscar Insumo visível à Empresa Ativa;
3. se não encontrado, 404;
4. chamar `insumo.Desativar()`;
5. `SaveChangesAsync()`;
6. definir mensagem de sucesso;
7. PRG para detalhes.

Mensagem:

```text
Insumo desativado com sucesso.
```

## POST — Reativar

Fluxo equivalente:

1. buscar por `id` com filtro tenant ativo;
2. 404 se não encontrado;
3. chamar `insumo.Reativar()`;
4. persistir;
5. PRG para detalhes.

Mensagem:

```text
Insumo reativado com sucesso.
```

## Segurança de mutação

As mudanças de situação devem:

- ocorrer somente por POST;
- usar antiforgery padrão do Razor Pages;
- não ser executáveis por GET;
- não aceitar booleano `Ativo` do cliente.

## Critérios de aceitação

### CA01 — Insumo ativo exibe Desativar

Nos detalhes de Insumo ativo:

- Situação = Ativo;
- ação Desativar disponível;
- ação Reativar ausente.

### CA02 — Insumo inativo exibe Reativar

Nos detalhes de Insumo inativo:

- Situação = Inativo;
- ação Reativar disponível;
- ação Desativar ausente.

### CA03 — Desativação altera somente a situação

Ao desativar:

- `Ativo == false`;
- Id, EmpresaId, Nome, Marca, Categoria, Unidade base e Observação permanecem inalterados.

### CA04 — Reativação altera somente a situação

Ao reativar:

- `Ativo == true`;
- demais dados permanecem inalterados.

### CA05 — Operações são idempotentes

Chamadas repetidas de `Desativar()` ou `Reativar()` não lançam exceção e mantêm o estado esperado.

### CA06 — Desativação usa PRG

POST válido de desativação redireciona para detalhes e exibe:

```text
Insumo desativado com sucesso.
```

### CA07 — Reativação usa PRG

POST válido de reativação redireciona para detalhes e exibe:

```text
Insumo reativado com sucesso.
```

### CA08 — Inativos continuam visíveis

A listagem do UC002 continua exibindo registros ativos e inativos com a situação correta.

### CA09 — Inativo continua editável

A edição do UC003 continua acessível para Insumo inativo.

### CA10 — Desativar não libera identidade

Mesmo inativo, o Insumo continua participando da unicidade Nome+Marca da mesma Empresa.

### CA11 — ID inexistente retorna 404

POST de Desativar/Reativar para ID inexistente retorna HTTP 404.

### CA12 — Cross-tenant retorna 404

POST de Desativar/Reativar para ID de outro tenant retorna HTTP 404 e não altera o registro.

### CA13 — Antiforgery obrigatório

POST sem token antiforgery válido é rejeitado e não altera o estado.

### CA14 — Sem exclusão física

Nenhuma ação de delete físico é implementada.

### CA15 — Sem migration

Nenhuma migration ou alteração de schema é necessária.

### CA16 — Sem escopo antecipado

Não implementar preço, histórico de preço, Ficha Técnica, Produto ou regras de uso de Insumo inativo em entidades ainda inexistentes.

## Matriz de testes fechada antes da implementação

### Unitários — domínio

#### U1

```text
CA03_Desativar_altera_apenas_status_e_preserva_demais_dados
```

#### U2

```text
CA04_Reativar_altera_apenas_status_e_preserva_demais_dados
```

#### U3

```text
CA05_Desativar_e_reativar_sao_idempotentes
```

Pode ser Theory ou teste focado equivalente, desde que fique diagnóstico.

### Integração — persistência

#### P1

```text
CA03_CA04_Alteracoes_de_status_persistem_no_sqlite
```

Usar SQLite real/in-memory e migrations existentes.

Validar round-trip:

```text
ativo -> inativo -> ativo
```

#### P2

```text
CA10_Insumo_inativo_continua_participando_do_indice_unico
```

Cenário:

1. criar Insumo;
2. desativar;
3. persistir;
4. tentar inserir outro com mesmo Empresa+Nome+Marca;
5. banco deve rejeitar pela unicidade existente.

Não criar migration.

### Integração — Web

Criar testes focados.

#### W1

```text
CA01_Detalhes_de_ativo_exibe_desativar_e_nao_reativar
```

#### W2

```text
CA02_Detalhes_de_inativo_exibe_reativar_e_nao_desativar
```

#### W3

```text
CA06_Post_desativar_persiste_status_redireciona_e_exibe_sucesso
```

#### W4

```text
CA07_Post_reativar_persiste_status_redireciona_e_exibe_sucesso
```

#### W5

```text
CA08_CA09_Inativo_permanece_na_listagem_e_pode_ser_editado
```

Se ficar agregado demais, separar listagem e edição.

#### W6

```text
CA11_Post_de_id_inexistente_retorna_404
```

Cobrir os dois handlers de forma clara.

#### W7

```text
CA12_Post_cross_tenant_retorna_404_e_preserva_registro
```

Cobrir Desativar/Reativar conforme o estado preparado e confirmar registro intacto.

#### W8

```text
CA13_Post_sem_antiforgery_e_rejeitado_sem_alterar_status
```

Não enfraquecer antiforgery para facilitar teste.

## Persistência

Não criar migration.

Não editar migrations históricas.

Não alterar `PrecificadorDbContextModelSnapshot`.

Não criar coluna nova.

O campo `Ativo` já existe e é suficiente.

## Fora do escopo

- exclusão física;
- motivo/data de desativação;
- auditoria;
- usuário que desativou;
- bloqueio de edição de inativos;
- filtros de listagem por situação;
- preço/histórico;
- ficha técnica;
- produto;
- bulk actions;
- API REST;
- refatorar Novo/Editar pela MEL005;
- alterar identidade/unicidade.

## Definition of Done específica

Além da DoD global:

- domínio possui `Desativar()` e `Reativar()`;
- nenhum setter público de `Ativo`;
- detalhes exibe somente a ação coerente com o estado atual;
- desativação pede confirmação simples;
- mutações usam POST + antiforgery;
- GET não altera estado;
- handlers rebuscam a entidade com Global Query Filter;
- cross-tenant/inexistente => 404;
- PRG + mensagens funcionam;
- inativos permanecem listados, consultáveis e editáveis;
- unicidade permanece incluindo inativos;
- nenhuma migration/snapshot;
- matriz de testes está coberta com testes focados;
- UC004 passa para **Implementado**;
- F001, catálogo, regras e ordem de implementação ficam coerentes;
- UC005 passa a ser o próximo caso a detalhar/revalidar;
- antes de UC005, preservar o gate já documentado sobre edição de Nome/Marca/Unidade após histórico;
- qualquer nova ideia útil fora do escopo vai para `docs/development/melhorias.md`;
- build Release sem warnings novos relevantes e suíte completa verde.
