# ADR-011 — Dois planos de autorização: sistema e Empresa

- **Status:** Aceito

## Contexto

A FT002 introduziu autenticação com ASP.NET Core Identity, vínculo N:N `UsuarioEmpresa`, seleção de Empresa Ativa e isolamento tenant-aware.

A evolução do Precificador passa a exigir dois tipos de autoridade com escopos diferentes:

1. uma autoridade global para administrar o próprio sistema e o ciclo de vida das Empresas;
2. uma autoridade contextual para administrar usuários/vínculos dentro de uma Empresa específica.

Usar uma única role global para ambos os casos seria incorreto, porque o mesmo usuário pode ser Administrador em uma Empresa e apenas Operacional em outra.

Também foi decidido que o bootstrap da instalação não deve mais criar automaticamente uma Empresa e vincular o primeiro usuário a ela.

## Decisão

### Autoridade global

Representar o Administrador do Sistema pela role ASP.NET Core Identity:

~~~text
SystemAdmin
~~~

Características:

- role global;
- independente de Empresa;
- não exige `EmpresaContext`;
- não concede automaticamente acesso a dados tenant-owned;
- o primeiro membro é criado pelo `/Setup`;
- o `SystemAdmin` inicial não recebe `UsuarioEmpresa`.

### Autoridade empresarial

Representar a autoridade dentro de uma Empresa no próprio vínculo `UsuarioEmpresa`.

O vínculo passa a conter:

~~~text
PerfilUsuarioEmpresa
- Operacional
- Administrador
~~~

A autorização administrativa empresarial depende de:

~~~text
usuário autenticado
+ Empresa Ativa
+ Empresa ativa
+ vínculo ativo
+ Perfil == Administrador
~~~

Não usar Identity Role global para Administrador de Empresa.

### Bootstrap

O `/Setup` passa a criar somente o primeiro `SystemAdmin`.

O bootstrap:

- não cria Empresa;
- não cria `UsuarioEmpresa`;
- não promove automaticamente usuário empresarial existente;
- depende da inexistência de qualquer membro da role `SystemAdmin`;
- exige chave externa configurada fora do banco/repositório.

### Compatibilidade

Vínculos `UsuarioEmpresa` anteriores à FT003 são migrados para `Operacional`, adotando menor privilégio.

Usuários existentes não são promovidos para `SystemAdmin`.

A Empresa técnica histórica da FT002 não é removida automaticamente pela FT003; o tratamento funcional desse legado fica para a UC039.

## Consequências

### Positivas

- autoridade global e autoridade tenant-aware não se confundem;
- um mesmo usuário pode ter perfis diferentes em Empresas distintas;
- `SystemAdmin` não precisa de Empresa artificial para administrar o sistema;
- autorização futura de UC031 pode ser feita por policy contextual;
- reduz risco de acesso cross-tenant por role global excessiva;
- upgrade de bases existentes não concede privilégio global implicitamente.

### Custos

- `UsuarioEmpresa` ganha novo campo persistido;
- uma nova migration é necessária;
- login/layout precisam distinguir SystemAdmin de usuário empresarial;
- o bootstrap exige configuração operacional de uma chave externa;
- instalações FT002 existentes passam por uma etapa explícita de criação do primeiro SystemAdmin após upgrade.

## Alternativas rejeitadas

### Usar IdentityRole "Administrador" para Empresa

Rejeitada porque roles Identity são globais para a identidade e não representam:

~~~text
Usuário X
- Empresa A -> Administrador
- Empresa B -> Operacional
~~~

### Vincular SystemAdmin a uma Empresa especial

Rejeitada porque mistura autoridade global com tenant e poderia incentivar bypass de isolamento.

### Promover automaticamente o primeiro usuário existente

Rejeitada porque a base não contém fato confiável que prove que determinado usuário empresarial deve receber poder global.

### Usar senha temporária administrada pelo sistema

Rejeitada para os fluxos futuros de onboarding. A direção aprovada é ativação/recuperação por token, tratada pela UC040.

## Relação com ADR-009

A ADR-009 continua válida para autenticação via ASP.NET Core Identity.

A observação de que roles granulares não seriam introduzidas sem requisito específico foi satisfeita pela FT003. A role `SystemAdmin` possui escopo global bem definido; a granularidade por Empresa permanece em `UsuarioEmpresa`, não em roles globais.
