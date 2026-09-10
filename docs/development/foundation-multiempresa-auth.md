# FT002 — Fundação Multiempresa e Autenticação

- **Status:** Pronto para implementação
- **Tipo:** fundação técnica transversal
- **Dependências:** FT001 e UC001 implementados
- **Bloqueia:** UC001B, UC001A e UC002

## Objetivo

Evoluir o Precificador de aplicação local de usuário único para uma aplicação local-first preparada para múltiplas empresas e múltiplos usuários, mantendo SQLite nesta fase.

A FT002 deve introduzir autenticação, vínculo usuário-empresa, seleção de empresa ativa e isolamento obrigatório dos dados pertencentes a uma empresa.

A FT002 não implementa CRUD administrativo completo de empresas ou usuários e não implementa funcionalidades de precificação.

## Decisões centrais

1. O banco permanece SQLite e único nesta fase.
2. Empresas compartilham o mesmo schema e as mesmas tabelas.
3. Entidades de negócio pertencentes a uma empresa possuem `EmpresaId` obrigatório.
4. O acesso é isolado por empresa no nível de persistência e também validado no fluxo web.
5. Autenticação usa ASP.NET Core Identity; não criar mecanismo próprio de senha, hash ou cookie.
6. Um usuário pode pertencer a várias empresas por vínculo N:N.
7. O usuário trabalha sempre no contexto de uma Empresa Ativa.
8. O estado da Empresa Ativa pode ser mantido em sessão nesta fase local-first, mas não é um fato de domínio persistido no usuário.
9. Configurações de precificação futuras pertencem à empresa, não são globais entre empresas.
10. A arquitetura futura deve permitir trocar SQLite por banco servidor sem reescrever as regras do Core.

## Modelo conceitual

```text
Empresa
- Id: int
- Nome: string
- NomeNormalizado: string
- Ativo: bool

UsuarioAplicacao : IdentityUser

UsuarioEmpresa
- UsuarioId: string
- EmpresaId: int
- Ativo: bool

IEntidadeEmpresa
- EmpresaId: int

Insumo : IEntidadeEmpresa
- EmpresaId: int
- ...campos existentes
```

`UsuarioAplicacao` é infraestrutura de autenticação e não deve contaminar as entidades do Core com dependência de ASP.NET Core Identity.

`Empresa` é conceito do domínio e deve permanecer independente da camada Web.

## Empresa

Regras mínimas:

- `Id` inteiro gerado pelo banco;
- Nome obrigatório;
- Nome máximo de 120 caracteres após normalização de espaços;
- NomeNormalizado técnico em maiúsculas invariáveis;
- empresa nasce ativa;
- nome deve ser único no cadastro de empresas independentemente de caixa/espaços;
- não implementar edição/desativação pela UI nesta FT.

## Usuário e vínculo

- autenticação por e-mail e senha;
- e-mail único no Identity;
- um usuário pode possuir zero, um ou vários vínculos;
- somente vínculos ativos com empresas ativas são elegíveis;
- não implementar roles/perfis de autorização nesta FT;
- não implementar auto-registro público;
- não implementar recuperação de senha, confirmação de e-mail ou 2FA nesta fase local-first.

## Primeiro acesso / bootstrap

Como ainda não existe usuário administrativo, a aplicação precisa de um fluxo de bootstrap seguro e único.

Criar `/Setup`, acessível somente quando não existir nenhum usuário no Identity.

Campos mínimos:

- Nome da empresa inicial;
- E-mail do primeiro usuário;
- Senha;
- Confirmação da senha.

Fluxo:

1. banco é migrado;
2. se não houver usuários, `/Setup` fica disponível;
3. o usuário informa empresa e credenciais;
4. o sistema renomeia a empresa técnica criada pela migration para o nome informado;
5. cria o primeiro usuário via `UserManager`;
6. cria o vínculo `UsuarioEmpresa` ativo;
7. conclui em transação coerente;
8. redireciona para Login;
9. após existir qualquer usuário, `/Setup` deixa de permitir novo bootstrap.

Não criar credenciais padrão no código ou em migration.

## Compatibilidade com o banco atual

A migration da FT002 deve preservar o banco já criado pelo UC001.

Estratégia normativa:

1. criar tabela `Empresas`;
2. inserir uma empresa técnica inicial com identificador conhecido e nome neutro, por exemplo `Empresa inicial`;
3. adicionar `EmpresaId` obrigatório aos `Insumos`, associando registros existentes a essa empresa técnica;
4. criar FK de `Insumos.EmpresaId -> Empresas.Id`;
5. remover o índice único atual de `NomeNormalizado`;
6. criar índice único `(EmpresaId, NomeNormalizado)`;
7. criar tabelas do ASP.NET Core Identity;
8. criar `UsuarioEmpresas` com chave composta `UsuarioId + EmpresaId`;
9. no `/Setup`, renomear a empresa técnica para o nome informado pelo primeiro usuário.

A migration não deve inferir empresa pelo conteúdo dos Insumos.

## Isolamento por empresa

### Leitura

Toda entidade tenant-owned deve ser filtrada automaticamente pela Empresa Ativa no `PrecificadorDbContext`.

Usar Global Query Filter ou mecanismo EF Core equivalente centralizado. Não depender de cada PageModel lembrar de aplicar `Where(EmpresaId == ...)`.

Atualmente `Insumo` é a única entidade tenant-owned existente. Produtos, preços, fichas e configurações deverão implementar o mesmo contrato quando surgirem.

Sem Empresa Ativa, consultas tenant-owned devem retornar nenhum dado; nunca assumir uma empresa por padrão depois do bootstrap.

### Escrita

O `DbContext` deve possuir um guard central para impedir que uma operação comum adicione, altere ou exclua entidade tenant-owned com `EmpresaId` diferente da Empresa Ativa.

A criação de `Insumo` deve receber explicitamente o `EmpresaId` atual no domínio. Não permitir que o usuário envie `EmpresaId` pelo formulário.

`IgnoreQueryFilters` não deve aparecer em fluxos comuns de negócio. Seu uso é permitido somente em infraestrutura claramente justificada, bootstrap/migration ou testes de isolamento.

## Empresa Ativa

Criar abstração request-scoped, por exemplo `IEmpresaContext`, que exponha `int? EmpresaId`.

A implementação Web pode usar Session para armazenar o identificador selecionado.

Regras:

- ao fazer login, limpar qualquer empresa ativa anterior;
- se houver exatamente uma empresa elegível, selecioná-la automaticamente;
- se houver mais de uma, redirecionar para `/Empresas/Selecionar`;
- se não houver empresa elegível, negar acesso à área de negócio e permitir logout;
- ao selecionar empresa, validar sempre que o usuário autenticado possui vínculo ativo e que a empresa está ativa;
- tentativa de selecionar empresa sem vínculo deve resultar em acesso negado e não alterar a empresa ativa;
- logout limpa a sessão de empresa ativa.

## Autenticação

Usar ASP.NET Core Identity com EF Core no mesmo SQLite.

Recomendação estrutural:

- `PrecificadorDbContext` evolui para herdar de `IdentityDbContext<UsuarioAplicacao>`;
- Identity e entidades do Precificador compartilham o mesmo banco e migration history;
- `UsuarioAplicacao` pode permanecer sem propriedades adicionais nesta FT.

Configurar explicitamente:

- e-mail único;
- login por e-mail;
- cookie HttpOnly;
- páginas de negócio exigem autenticação por política fallback ou convenção equivalente;
- `/Setup`, `/Conta/Login` e páginas de erro necessárias são anônimas;
- logout somente por POST com antiforgery.

Política de senha inicial:

- mínimo 8 caracteres;
- exigir letra maiúscula;
- exigir letra minúscula;
- exigir dígito;
- caractere não alfanumérico não obrigatório.

Não implementar autenticação externa, JWT, API tokens ou OAuth nesta FT.

## Páginas mínimas

```text
/Setup
/Conta/Login
/Empresas/Selecionar
```

Logout deve ser ação POST, podendo ser handler no layout ou página dedicada.

Após autenticação e resolução de Empresa Ativa, o usuário é redirecionado para a Home.

A interface deve mostrar de forma simples qual empresa está ativa quando houver contexto válido.

## Impacto em UC001

`Insumo` passa a possuir `EmpresaId` obrigatório.

A unicidade temporária após a FT002 é:

```text
EmpresaId + NomeNormalizado
```

O formulário `/Insumos/Novo` não exibe EmpresaId. Ele utiliza a Empresa Ativa.

Cadastro de Insumo sem usuário autenticado/Empresa Ativa não é permitido.

## Relação com UC001B e UC001A

A FT002 não altera categoria/unidades de Insumo, Marca ou Observação.

Após a FT002, o **UC001B** generaliza o vocabulário de Insumos para:

```text
CategoriaInsumo
- MateriaPrima = 1
- Embalagem = 2
- Consumivel = 3

UnidadeMedida
- Grama = 1
- Mililitro = 2
- Unidade = 3
- Metro = 4
```

A troca `Ingrediente -> MateriaPrima` preserva o valor numérico `1`; `Metro = 4` é apenas novo valor funcional. O UC001B não deve gerar migration vazia.

Depois do UC001B, o UC001A permanece responsável por Marca e Observação. Seu índice de unicidade deverá incluir a empresa:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

A especificação/instrução do UC001A será revisada após a implementação da FT002/UC001B e antes de sua execução.

## Generalização para diferentes negócios

Foi aprovada a direção conceitual de tornar a Ficha Técnica genérica: rendimento e tempo ativo permanecem gerais; perdas deixam de ser conceito obrigatório de panificação; forno será futuramente tratado como equipamento/recurso quando esse domínio for detalhado.

A classificação de Insumos também foi generalizada: `Matéria-prima` substitui `Ingrediente`, e o conjunto de unidades passa a contemplar `g`, `ml`, `m` e `un`. Essa alteração pertence ao UC001B e **não deve ser implementada na FT002**.

## Critérios de aceitação

### CA01 — Upgrade preserva Insumos

Dado banco no estado atual do UC001 com Insumos, quando a migration FT002 for aplicada, então os registros permanecem existentes e associados à empresa técnica inicial.

### CA02 — Primeiro setup

Dado banco sem usuários, quando `/Setup` criar empresa inicial e usuário válido, então o usuário Identity e vínculo ativo são persistidos e a empresa técnica é renomeada.

### CA03 — Setup único

Dado que já existe usuário, `/Setup` não permite criar outro bootstrap.

### CA04 — Login válido

Usuário válido consegue autenticar com e-mail/senha e recebe cookie Identity.

### CA05 — Login inválido

Credenciais inválidas não autenticam nem revelam informação sensível.

### CA06 — Página protegida

Usuário anônimo tentando acessar `/Insumos/Novo` é direcionado ao Login.

### CA07 — Empresa única

Usuário com um vínculo elegível recebe essa empresa como ativa automaticamente após login.

### CA08 — Múltiplas empresas

Usuário com dois vínculos elegíveis deve selecionar uma empresa antes de acessar dados tenant-owned.

### CA09 — Seleção não autorizada

Usuário não pode ativar empresa com a qual não possui vínculo ativo.

### CA10 — Isolamento de leitura

Com Empresa A ativa, consultas normais de `Insumos` não retornam Insumos da Empresa B.

### CA11 — Isolamento de escrita

Uma escrita comum não consegue persistir/alterar entidade tenant-owned de empresa diferente da Empresa Ativa.

### CA12 — Unicidade por empresa

Duas empresas podem possuir Insumos com o mesmo NomeNormalizado; a mesma empresa não pode duplicá-lo nesta etapa.

### CA13 — Troca de empresa

Após selecionar outra empresa válida, novas consultas retornam somente dados da nova Empresa Ativa.

### CA14 — Logout

Logout invalida autenticação e limpa a Empresa Ativa.

### CA15 — Sem escopo antecipado

O diff não contém CRUD administrativo completo de empresa/usuário, generalização de categoria/unidades do UC001B, Marca/Observação do UC001A, listagem do UC002, Produto, Ficha Técnica ou precificação.

## Testes obrigatórios

### Unitários

- criação válida de Empresa;
- normalização/validação do nome da Empresa;
- regras puras de tenant ownership introduzidas no Core.

Não testar internals do Identity como unidade.

### Integração — persistência

- migration em banco vazio;
- upgrade do banco UC001 contendo Insumo;
- tabelas Identity, Empresas e UsuarioEmpresas existem;
- Insumo antigo é associado à empresa técnica;
- FK Empresa/Insumo;
- índice `(EmpresaId, NomeNormalizado)` permite nomes iguais em empresas diferentes e rejeita duplicidade na mesma;
- query filter isola duas empresas;
- guard de SaveChanges rejeita escrita cross-tenant.

### Integração — web/autenticação

- `/Setup` funciona somente sem usuários;
- primeiro usuário/empresa/vínculo são criados;
- login válido e inválido;
- rota protegida exige login;
- um vínculo seleciona empresa automaticamente;
- múltiplos vínculos exigem seleção;
- seleção sem vínculo é negada;
- alternância de empresa muda visibilidade de Insumos;
- logout limpa contexto;
- testes não usam banco real do usuário.

## Migration

Criar migration evolutiva, sem alterar migrations históricas.

Nome sugerido:

```text
AddMultiempresaIdentity
```

Não usar `EnsureCreated` na aplicação.
Não adicionar auto-migration ao startup.

## Fora do escopo

- publicação/hospedagem;
- troca de SQLite;
- cadastro administrativo completo de empresas;
- convite/cadastro administrativo de usuários;
- roles/permissões granulares;
- recuperação de senha;
- confirmação de e-mail;
- 2FA;
- autenticação externa;
- auditoria completa;
- UC001B — generalização de categoria/unidades;
- Marca/Observação do UC001A;
- Produto/Ficha Técnica;
- UC002+;
- API REST.

## Definition of Done específica

- ASP.NET Core Identity integrado ao SQLite existente;
- Empresa e UsuarioEmpresa persistidos;
- bootstrap inicial funcional e não reutilizável;
- Insumo possui EmpresaId obrigatório;
- isolamento centralizado de leitura e escrita validado;
- login/logout e seleção de empresa funcionam;
- migration preserva banco anterior;
- testes cross-tenant cobrem risco de vazamento;
- docs de arquitetura/escopo permanecem coerentes;
- build Release sem warnings novos relevantes;
- suíte completa verde;
- nenhuma funcionalidade posterior antecipada.
