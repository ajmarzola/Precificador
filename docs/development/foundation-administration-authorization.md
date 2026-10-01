# FT003 — Fundação de administração e autorização

- **Estado:** Pronto
- **Tipo:** Fundação técnica transversal
- **Dependências:** FT002
- **Bloqueia:** UC038, UC040, UC039 e UC031

## Objetivo

Evoluir a fundação de autenticação/multiempresa da FT002 para suportar dois planos distintos de autorização:

1. **administração global do Precificador**, exercida por um `SystemAdmin` que não pertence a nenhuma Empresa;
2. **administração contextual de uma Empresa**, expressa no vínculo `UsuarioEmpresa`.

A FT003 também redefine o `/Setup` para criar exclusivamente o primeiro Administrador do Sistema e introduz uma área administrativa global mínima protegida, sem antecipar os fluxos funcionais de solicitação de acesso, ativação por token, administração de Empresas ou gestão de usuários.

## Contexto atual

A FT002 já entrega:

- ASP.NET Core Identity;
- `UsuarioAplicacao`;
- vínculo N:N `UsuarioEmpresa`;
- Empresa Ativa em sessão;
- policy `EmpresaAtiva`;
- login/logout;
- seleção de Empresa;
- isolamento tenant-aware;
- `/Setup` que atualmente cria o primeiro usuário, renomeia a Empresa técnica e cria vínculo com ela.

A FT003 deve preservar o que permanece válido e substituir apenas a semântica de autorização/bootstrap que foi deliberadamente adiada pela FT002.

## Decisões centrais

1. `SystemAdmin` é uma autoridade **global do sistema**.
2. `SystemAdmin` usa **ASP.NET Core Identity Role**, pois sua autoridade não depende de Empresa.
3. Administrador/Operacional de Empresa são perfis do vínculo `UsuarioEmpresa`, não roles globais.
4. O mesmo `UsuarioAplicacao` pode ser Administrador em uma Empresa e Operacional em outra.
5. O `SystemAdmin` criado pelo Setup não recebe `UsuarioEmpresa` nem Empresa Ativa.
6. O `/Setup` cria somente o primeiro `SystemAdmin`.
7. O `/Setup` é protegido por uma chave de bootstrap externa ao banco e ao repositório.
8. Usuários empresariais existentes não são promovidos automaticamente a `SystemAdmin`.
9. Vínculos empresariais existentes são migrados de forma conservadora para `Operacional`.
10. A FT003 não envia e-mail, não gera convite e não implementa recuperação de senha.
11. A FT003 não cria, aprova, edita, suspende ou encerra Empresas.
12. A FT003 não implementa ainda a UI de administração dos vínculos; apenas estabelece a infraestrutura de autorização que a UC031 usará.

## Modelo conceitual

~~~text
UsuarioAplicacao : IdentityUser
    |
    +-- IdentityRole "SystemAdmin"
    |      autoridade global
    |      sem Empresa Ativa
    |      sem UsuarioEmpresa por desenho
    |
    +-- UsuarioEmpresa N:N Empresa
           - UsuarioId
           - EmpresaId
           - Ativo
           - Perfil
                 Operacional
                 Administrador
~~~

### Perfil do vínculo

Introduzir enum explícito:

~~~csharp
public enum PerfilUsuarioEmpresa
{
    Operacional = 1,
    Administrador = 2
}
~~~

`UsuarioEmpresa.Perfil` é obrigatório.

Não usar string livre para perfil.

Não usar `IdentityRole` para representar Administrador de Empresa.

## Constantes de autorização

Centralizar os nomes para evitar strings duplicadas.

Conceitualmente:

~~~text
Role global:
SystemAdmin

Policies:
EmpresaAtiva
SystemAdmin
AdministradorEmpresa
~~~

Os nomes concretos podem ficar em uma classe simples de constantes na camada Web/Autorização ou infraestrutura de autenticação, sem criar framework próprio de permissões.

## Plano global — SystemAdmin

### Semântica

Um `SystemAdmin`:

- autentica normalmente pelo ASP.NET Core Identity;
- possui a role global `SystemAdmin`;
- não depende de Empresa Ativa;
- não depende de `UsuarioEmpresa`;
- acessa a área `/Admin`;
- não recebe implicitamente acesso aos dados tenant-owned de nenhuma Empresa;
- não entra no fluxo de seleção de Empresa após login.

A role global não deve ser interpretada como “superusuário de dados comerciais”.

O acesso cross-tenant para suporte/impersonation não faz parte da FT003.

### Invariante de separação

O primeiro `SystemAdmin` criado pelo Setup **não pode receber vínculo empresarial**.

Fluxos futuros também devem preservar a regra de que autoridade global não cria automaticamente vínculo com Empresa.

A FT003 não precisa introduzir constraint SQL cruzando `AspNetUserRoles` e `UsuariosEmpresas`; a garantia é feita pelos fluxos autorizados e por testes.

## Plano empresarial — AdministradorEmpresa

### Policy

Introduzir policy `AdministradorEmpresa`.

Para satisfazê-la, o request deve possuir simultaneamente:

~~~text
usuário autenticado
AND Empresa Ativa válida
AND Empresa ativa
AND UsuarioEmpresa ativo
AND UsuarioEmpresa.Perfil == Administrador
~~~

A policy deve reutilizar a semântica da `EmpresaAtiva` já existente e acrescentar a exigência de perfil administrativo.

Um vínculo `Operacional` continua podendo acessar todas as áreas operacionais atualmente protegidas apenas por `EmpresaAtiva`.

FT003 **não altera** Produtos, Insumos, Configurações, Dashboard ou demais páginas operacionais para exigir Administrador.

### Isolamento

Administrador da Empresa A não satisfaz `AdministradorEmpresa` quando a Empresa Ativa é B, salvo se possuir vínculo ativo com perfil Administrador também em B.

Vínculo inativo nunca concede autorização.

Empresa inativa nunca concede autorização.

## Novo /Setup

## Finalidade

O Setup passa a ser exclusivamente o bootstrap do primeiro Administrador do Sistema.

Ele não cria Empresa e não cria `UsuarioEmpresa`.

### Condição de disponibilidade

O critério deixa de ser:

~~~text
não existe nenhum usuário Identity
~~~

e passa a ser:

~~~text
não existe nenhum usuário na role SystemAdmin
~~~

Isso é obrigatório para permitir upgrade de instalações FT002 já utilizadas, que podem possuir usuários empresariais mas ainda não possuem administrador global.

### Chave externa de bootstrap

O bootstrap deve exigir uma chave configurada fora da base.

Usar configuração equivalente a:

~~~text
Bootstrap:SystemAdminKey
~~~

Em variável de ambiente:

~~~text
Bootstrap__SystemAdminKey
~~~

Regras:

- não possuir valor default;
- não persistir a chave no banco;
- não adicioná-la a `appsettings*.json` versionado;
- não escrevê-la em logs;
- em desenvolvimento, usar variável de ambiente/user-secrets;
- no Azure, usar App Service Application Setting ou mecanismo de secret equivalente;
- valor ausente ou whitespace torna o Setup indisponível para conclusão;
- a comparação deve evitar comparação ingênua de string sensível; preferir hash + comparação em tempo constante ou mecanismo equivalente.

A especificação não fixa um valor real de chave.

### Campos

O novo formulário possui:

- Chave de configuração;
- E-mail do Administrador do Sistema;
- Senha;
- Confirmação da senha.

Remover `Nome da empresa`.

A senha segue a política Identity já configurada.

### GET /Setup

Quando já existe ao menos um `SystemAdmin`:

~~~text
404 Not Found
~~~

Quando não existe `SystemAdmin`, mas a chave de bootstrap não está configurada:

~~~text
503 Service Unavailable
~~~

com mensagem funcional genérica de que a configuração inicial está indisponível.

Não expor o valor esperado, tamanho ou qualquer fragmento da chave.

Quando não existe `SystemAdmin` e a chave está configurada, renderizar o formulário.

### POST /Setup

Fluxo normativo:

1. validar antiforgery;
2. revalidar que ainda não existe `SystemAdmin`;
3. validar ModelState;
4. validar a chave de bootstrap;
5. abrir transação compatível com a estratégia de execução SQL Server;
6. revalidar dentro da unidade crítica que ainda não existe `SystemAdmin`;
7. garantir a existência da role `SystemAdmin` via `RoleManager`;
8. criar um novo `UsuarioAplicacao` via `UserManager`;
9. adicionar o usuário à role `SystemAdmin`;
10. não criar/alterar Empresa;
11. não criar `UsuarioEmpresa`;
12. concluir a transação;
13. redirecionar para `/Conta/Login`.

A implementação deve reduzir a janela para dois bootstraps concorrentes; a checagem crítica deve ocorrer dentro de transação com isolamento adequado ou solução equivalente.

### Chave inválida

- não criar usuário;
- não criar role/membership parcial;
- retornar a própria página;
- apresentar erro funcional genérico;
- não revelar se o e-mail informado já existe antes da validação da chave.

### E-mail já existente

Após chave válida, a criação segue as validações do Identity.

A FT003 não deve “converter” silenciosamente um usuário empresarial existente em `SystemAdmin`.

O Administrador do Sistema inicial deve ser uma conta global distinta dos usuários já vinculados a Empresas.

### Setup único

Após existir o primeiro membership `SystemAdmin`, GET e POST de `/Setup` deixam de permitir novo bootstrap.

A role poderá suportar mais de um membro no futuro, mas a FT003 não cria UI para adicionar/remover outros `SystemAdmin`.

## Área /Admin

Criar uma superfície mínima:

~~~text
/Admin
~~~

ou Razor Page equivalente `/Admin/Index`.

Regras:

- exige policy `SystemAdmin`;
- não exige Empresa Ativa;
- não lista dados de Empresas nesta FT;
- não oferece CRUD;
- serve como Home administrativa global e ponto de extensão para UC039.

Conteúdo mínimo:

- título “Administração do sistema”;
- identificação textual de que a área é administrativa;
- logout.

Não exibir “Empresa ativa” para `SystemAdmin`.

## Login

### GET

Se o usuário já estiver autenticado:

- `SystemAdmin` -> `/Admin`;
- usuário empresarial com Empresa Ativa -> `/Dashboard`;
- usuário empresarial sem Empresa Ativa -> `/Empresas/Selecionar`.

Se estiver anônimo e **não existir SystemAdmin**:

- redirecionar para `/Setup`.

A verificação deixa de usar “existem usuários?” como critério de bootstrap.

### POST

Após validar credenciais:

1. limpar qualquer `EmpresaContext` anterior;
2. se o usuário pertence à role `SystemAdmin`, redirecionar para `/Admin`;
3. não carregar vínculos nem definir Empresa Ativa para `SystemAdmin`;
4. para usuário não-SystemAdmin, preservar o fluxo FT002:
   - zero vínculos elegíveis -> seleção/estado sem Empresa;
   - um vínculo elegível -> selecionar automaticamente;
   - vários vínculos elegíveis -> `/Empresas/Selecionar`.

Para `SystemAdmin`, ignorar `ReturnUrl` que aponte para área tenant-owned e usar `/Admin` como destino canônico.

Para usuários empresariais, manter a regra atual de `ReturnUrl` local.

## Home e navegação

### Anônimo

Preservar a navegação pública atual.

FT003 não implementa ainda o formulário da UC038.

### SystemAdmin autenticado

A navegação deve:

- apontar “Precificador/Início” para `/Admin`;
- exibir entrada “Administração”;
- exibir logout;
- não exibir Dashboard, Insumos, Produtos e Configurações;
- não exibir texto “Empresa ativa”.

### Usuário empresarial autenticado

Preservar a navegação operacional existente.

## /Empresas/Selecionar

`SystemAdmin` não participa de seleção de Empresa.

Se navegar diretamente para `/Empresas/Selecionar`, deve ser redirecionado para `/Admin` ou ter acesso negado de forma consistente; não renderizar lista vazia como se fosse usuário empresarial comum.

Usuários empresariais preservam o comportamento FT002.

## Persistência e migration

### UsuarioEmpresa.Perfil

Criar migration para adicionar `Perfil`.

A migration deve evitar conceder privilégio administrativo implicitamente.

Estratégia normativa:

1. adicionar coluna temporariamente nullable;
2. preencher vínculos existentes com `Operacional`;
3. tornar a coluna NOT NULL;
4. não deixar default de banco que transforme futuros vínculos sem perfil explícito em Administrador.

Se a ferramenta gerar default temporário para migração, removê-lo da definição final.

### Compatibilidade com dados FT002

Nenhum usuário existente é promovido automaticamente para `SystemAdmin`.

Isso vale mesmo quando a instalação possui apenas um usuário.

Motivo: o modelo novo separa identidade global de identidade empresarial e não há dado confiável que autorize inferir que um usuário vinculado deva receber poder global.

### Empresa técnica existente

A FT003 **não remove nem reaproveita automaticamente** o seed técnico `Empresa inicial`.

Razões:

- o seed é legado estrutural da FT002/MEL020;
- uma instalação existente pode ter renomeado o registro e possuir dados reais;
- remoção automática nesta fundação teria risco destrutivo e impacto amplo na suíte.

O novo Setup não renomeia, ativa, seleciona nem vincula o `SystemAdmin` a esse registro.

A UC039, ao implementar administração real de Empresas, deverá tratar explicitamente a existência do seed técnico remanescente em instalações novas/legadas antes de expor a listagem administrativa como cadastro real.

Não editar migrations históricas.

## Upgrade de uma instalação existente

Cenário esperado:

~~~text
antes da FT003
UsuarioAplicacao empresa-admin@x
UsuarioEmpresa -> Empresa X
nenhum SystemAdmin

depois da migration FT003
mesmo UsuarioAplicacao preservado
mesmo UsuarioEmpresa preservado, Perfil = Operacional
nenhum SystemAdmin ainda

operador configura Bootstrap:SystemAdminKey
acessa /Setup
cria system-admin@x (ou outro e-mail)
system-admin@x recebe role SystemAdmin
system-admin@x NÃO recebe UsuarioEmpresa
~~~

A aplicação não deve perder os dados empresariais existentes.

Depois do bootstrap global, o usuário empresarial antigo continua fazendo login e operando normalmente com sua Empresa Ativa.

A futura UC039 será responsável por designar/trocar Administrador de Empresa.

## Autorização — comportamento esperado

| Identidade | Empresa Ativa | Perfil | /Admin | área EmpresaAtiva | futura policy AdministradorEmpresa |
|---|---|---|---|---|---|
| Anônimo | — | — | negado | negado | negado |
| SystemAdmin | nenhuma | — | permitido | negado | negado |
| Usuário empresarial | válida | Operacional | negado | permitido | negado |
| Usuário empresarial | válida | Administrador | negado | permitido | permitido |
| Usuário empresarial | ausente | qualquer | negado | negado | negado |
| vínculo inativo | qualquer | qualquer | negado | negado | negado |

## Segurança

- continuar usando ASP.NET Core Identity para senha/hash/cookie;
- não criar autenticação paralela;
- antiforgery permanece obrigatório nos POSTs;
- cookie permanece HttpOnly;
- chave de bootstrap nunca é credencial de login;
- chave de bootstrap não deve virar claim, cookie ou campo de banco;
- não confiar em parâmetro de request para escolher Empresa em policy;
- `SystemAdmin` não ganha bypass dos Global Query Filters;
- não usar `IgnoreQueryFilters` para implementar autoridade global;
- autorização de AdministradorEmpresa sempre usa usuário autenticado + Empresa Ativa resolvida no servidor;
- não registrar senha/chave em logs ou mensagens de erro.

## Gestão futura de SystemAdmins

A infraestrutura deve aceitar que a role `SystemAdmin` possua mais de um membro no futuro.

Entretanto, FT003 implementa somente o primeiro bootstrap.

Ficam fora desta entrega:

- convidar outro SystemAdmin;
- remover/demover SystemAdmin;
- transferir autoridade global;
- tela de gestão de SystemAdmins.

Qualquer futura remoção deverá impedir que o sistema fique sem ao menos um `SystemAdmin`.

Recuperação de senha do SystemAdmin será coberta pela UC040, usando token e senha escolhida pelo próprio usuário.

## Critérios de aceitação

- **CA01:** existem dois planos explícitos de autorização: global e empresarial.
- **CA02:** `SystemAdmin` é representado por Identity Role global.
- **CA03:** Administrador de Empresa não é representado por Identity Role global.
- **CA04:** `UsuarioEmpresa` possui `Perfil` obrigatório.
- **CA05:** perfis mínimos são Operacional e Administrador.
- **CA06:** vínculos pré-FT003 são migrados para Operacional.
- **CA07:** migration final não possui default que conceda Administrador implicitamente.
- **CA08:** policy `SystemAdmin` exige role SystemAdmin.
- **CA09:** policy `AdministradorEmpresa` exige Empresa Ativa válida, vínculo ativo e perfil Administrador.
- **CA10:** Administrador da Empresa A não obtém autorização administrativa na Empresa B sem vínculo administrativo próprio.
- **CA11:** Operacional continua autorizado às páginas protegidas apenas por EmpresaAtiva.
- **CA12:** Operacional não satisfaz AdministradorEmpresa.
- **CA13:** vínculo inativo não satisfaz AdministradorEmpresa.
- **CA14:** Empresa inativa não satisfaz AdministradorEmpresa.
- **CA15:** `/Setup` deixa de receber Nome da Empresa.
- **CA16:** `/Setup` recebe chave de bootstrap, e-mail, senha e confirmação.
- **CA17:** disponibilidade do Setup depende da inexistência de SystemAdmin, e não da inexistência de usuários.
- **CA18:** instalação com usuário empresarial existente e nenhum SystemAdmin ainda permite bootstrap global.
- **CA19:** Setup exige chave externa configurada.
- **CA20:** chave ausente impede conclusão do Setup.
- **CA21:** chave inválida não cria usuário, role parcial nem vínculo.
- **CA22:** chave não é persistida nem logada.
- **CA23:** Setup cria novo UsuarioAplicacao via UserManager.
- **CA24:** Setup garante/usa role SystemAdmin via RoleManager.
- **CA25:** usuário criado pelo Setup recebe SystemAdmin.
- **CA26:** usuário criado pelo Setup não recebe UsuarioEmpresa.
- **CA27:** Setup não renomeia nem cria Empresa.
- **CA28:** Setup não define Empresa Ativa.
- **CA29:** após o primeiro SystemAdmin, Setup fica indisponível para novo bootstrap.
- **CA30:** usuário empresarial existente não é promovido automaticamente a SystemAdmin.
- **CA31:** `/Admin` exige SystemAdmin.
- **CA32:** `/Admin` não exige Empresa Ativa.
- **CA33:** SystemAdmin autenticado é direcionado para `/Admin`.
- **CA34:** SystemAdmin não entra em seleção de Empresa.
- **CA35:** SystemAdmin não recebe acesso automático às páginas tenant-owned.
- **CA36:** login de usuário empresarial preserva seleção automática com um vínculo elegível.
- **CA37:** login de usuário empresarial preserva seleção explícita com múltiplos vínculos.
- **CA38:** logout continua limpando autenticação e EmpresaContext.
- **CA39:** navegação do SystemAdmin não exibe menus operacionais nem Empresa Ativa.
- **CA40:** navegação de usuário empresarial preserva menus operacionais.
- **CA41:** seed técnico de Empresa não é renomeado nem vinculado pelo novo Setup.
- **CA42:** migrations históricas não são editadas.
- **CA43:** migration FT003 funciona em banco limpo.
- **CA44:** migration FT003 funciona sobre banco existente FT002/MEL020 com usuário/vínculo/dados.
- **CA45:** build Release fica verde.
- **CA46:** testes unitários aplicáveis ficam verdes.
- **CA47:** integração SQL Server/Web fica verde.

## Matriz mínima de testes

### Perfil UsuarioEmpresa

- enum possui valores estáveis Operacional = 1 e Administrador = 2;
- persistência exige Perfil;
- upgrade preenche vínculo existente como Operacional;
- novo vínculo de teste precisa informar perfil explicitamente;
- não há default administrativo implícito.

### Policy SystemAdmin

- anônimo -> negado;
- usuário empresarial -> negado;
- SystemAdmin -> permitido;
- SystemAdmin sem Empresa Ativa -> permitido.

### Policy AdministradorEmpresa

- Administrador + vínculo ativo + Empresa Ativa correspondente -> permitido;
- Operacional -> negado;
- Administrador da Empresa A com Empresa B ativa -> negado;
- vínculo inativo -> negado;
- Empresa inativa -> negado;
- sem Empresa Ativa -> negado;
- SystemAdmin sem vínculo -> negado.

### Setup

- sem SystemAdmin + chave configurada -> GET 200;
- SystemAdmin existente -> GET 404;
- chave ausente -> GET/POST indisponível conforme regra;
- chave inválida -> nenhum efeito persistido;
- usuário empresarial já existente não bloqueia Setup;
- e-mail empresarial já existente não é elevado implicitamente;
- chave válida + e-mail novo -> cria um usuário global;
- membership SystemAdmin criado;
- zero UsuarioEmpresa para o novo usuário;
- Empresa técnica não renomeada;
- repetição após sucesso -> 404;
- POST sem antiforgery -> rejeitado;
- tentativa concorrente não resulta em dois bootstraps iniciais.

### Login

- SystemAdmin válido -> /Admin;
- SystemAdmin não recebe EmpresaContext;
- ReturnUrl tenant-owned não desvia SystemAdmin de /Admin;
- usuário empresarial com uma Empresa -> Dashboard;
- usuário empresarial com múltiplas -> Selecionar;
- login inválido preserva mensagem funcional;
- logout de ambos os tipos limpa sessão/autenticação.

### Navegação e rotas

- /Admin anônimo -> Login;
- /Admin usuário empresarial -> negado;
- /Admin SystemAdmin -> 200;
- SystemAdmin não vê Dashboard/Insumos/Produtos/Configurações na navegação;
- SystemAdmin não vê “Empresa ativa”;
- usuário empresarial mantém navegação operacional;
- SystemAdmin não usa /Empresas/Selecionar.

### Migration/upgrade

Validar explicitamente:

1. banco vazio -> migrations completas;
2. banco FT002/MEL020 com Empresa/usuário/vínculo/dados -> upgrade sem perda;
3. perfil existente vira Operacional;
4. nenhum usuário existente vira SystemAdmin;
5. migration histórica permanece intacta.

## Testes existentes a adaptar

Revisar no mínimo:

~~~text
tests/Precificador.Tests.Integration/Web/SetupPageTests.cs
tests/Precificador.Tests.Integration/Web/AutenticacaoPagesTests.cs
tests/Precificador.Tests.Integration/Web/FluxosMultiempresaTests.cs
tests/Precificador.Tests.Integration/Web/WebTestContext.cs
~~~

Helpers que criam `UsuarioEmpresa` devem receber perfil explícito ou usar `Operacional` apenas como convenção de teste claramente declarada.

Não enfraquecer testes tenant-aware existentes.

## Arquivos esperados

Lista indicativa, não prescritiva:

~~~text
src/Precificador.Infrastructure/Autenticacao/PerfilUsuarioEmpresa.cs
src/Precificador.Infrastructure/Autenticacao/UsuarioEmpresa.cs
src/Precificador.Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs
src/Precificador.Infrastructure/Migrations/<nova migration FT003>.cs

src/Precificador.Web/Autorizacao/...
src/Precificador.Web/Pages/Setup.cshtml
src/Precificador.Web/Pages/Setup.cshtml.cs
src/Precificador.Web/Pages/Admin/Index.cshtml
src/Precificador.Web/Pages/Admin/Index.cshtml.cs
src/Precificador.Web/Pages/Conta/Login.cshtml.cs
src/Precificador.Web/Pages/Empresas/Selecionar.cshtml.cs
src/Precificador.Web/Pages/Index.cshtml.cs
src/Precificador.Web/Pages/Shared/_Layout.cshtml
src/Precificador.Web/Program.cs

tests/Precificador.Tests.Unit/...
tests/Precificador.Tests.Integration/...
~~~

## Fora do escopo

- formulário público para solicitar acesso — UC038;
- envio de e-mail;
- convite/ativação por token — UC040;
- recuperação de senha — UC040;
- aprovação/recusa de solicitações — UC039;
- CRUD/listagem administrativa de Empresas — UC039;
- definir/trocar Administrador da Empresa — UC039;
- suspensão/reativação/encerramento de Empresa — UC039;
- convite de usuários para uma Empresa — UC031;
- desvincular usuários — UC031;
- UI para alterar perfil de UsuarioEmpresa — UC031;
- gestão de múltiplos SystemAdmins;
- impersonation;
- bypass cross-tenant;
- hard delete de Empresa;
- 2FA;
- SSO/autenticação externa;
- alteração das regras financeiras.

## Definition of Done específica

FT003 está concluída quando:

- os dois planos de autorização estão modelados e testados;
- `UsuarioEmpresa.Perfil` está persistido com migration segura;
- a policy `SystemAdmin` funciona sem Empresa Ativa;
- a policy `AdministradorEmpresa` funciona no tenant correto;
- o novo Setup cria somente o primeiro SystemAdmin;
- bootstrap exige chave externa e não promove usuário existente;
- SystemAdmin não recebe vínculo empresarial;
- login/navegação distinguem SystemAdmin de usuário empresarial;
- /Admin está protegido e funcional como shell;
- fluxo empresarial FT002 permanece funcional;
- banco limpo e upgrade legado passam;
- documentação/ADR estão alinhados;
- CI completa está verde;
- backlog marca FT003 como Concluído após implementação.

## Branch de implementação sugerida

~~~text
feat/ft003-administracao-autorizacao
~~~

## Commit sugerido

~~~text
feat: adiciona fundacao de administracao e autorizacao
~~~
