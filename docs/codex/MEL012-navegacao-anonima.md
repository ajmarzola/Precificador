# Instrução Codex — MEL012: Adequar navegação para usuário anônimo

## Tarefa

Implementar integralmente:

~~~text
docs/development/improvements/MEL012-navegacao-anonima.md
~~~

Branch sugerida:

~~~text
fix/mel012-navegacao-anonima
~~~

Não trabalhar em `master` e não fazer merge da própria PR.

## Antes de editar

Ler:

- MEL012;
- FT002;
- `_Layout.cshtml`;
- `Index.cshtml`;
- `Conta/Login.cshtml.cs`;
- `Setup.cshtml.cs`;
- `AutenticacaoPagesTests.cs`;
- helpers Web de autenticação existentes.

## Escopo funcional

### Usuário anônimo

Navbar deve mostrar:

~~~text
Precificador
Início
Entrar
~~~

Não mostrar:

~~~text
Insumos
Produtos
Configurações
Empresa ativa
Sair
~~~

Home anônima não mostra `Cadastrar produto`; mostra `Entrar`.

### Usuário autenticado

Preservar:

~~~text
Início
Insumos
Produtos
Configurações
Empresa ativa
Sair
~~~

Home continua mostrando `Cadastrar produto`.

Não mostrar o link público `Entrar` no bloco anônimo quando autenticado.

## Primeiro uso

Não consultar existência de usuários no `_Layout`.

Alterar somente o GET de Login para decidir bootstrap:

~~~csharp
if (User.Identity?.IsAuthenticated == true)
    return RedirectToPage("/Index");

if (!await userManager.Users.AnyAsync())
    return RedirectToPage("/Setup");

return Page();
~~~

Solução equivalente é aceitável.

Assim:

~~~text
Entrar
-> /Conta/Login
-> se zero usuários: /Setup
-> se já configurado: Login
~~~

## Setup

Não alterar contrato:

- zero usuários -> acessível;
- existe usuário -> 404;
- POST válido cria primeiro usuário/vínculo e redireciona para Login.

Depois do bootstrap, GET `/Conta/Login` precisa retornar 200 e não redirecionar novamente para Setup.

## Segurança

Não remover ou alterar:

~~~csharp
AuthorizeFolder("/Insumos", "EmpresaAtiva")
AuthorizeFolder("/Produtos", "EmpresaAtiva")
AuthorizeFolder("/Configuracoes", "EmpresaAtiva")
~~~

Não alterar:

- `EmpresaAtivaRequirement`;
- `EmpresaAtivaHandler`;
- cookie Identity;
- antiforgery;
- sessão de Empresa ativa;
- GQF/write guards.

Ocultar link é UX, não autorização.

## Login

Preservar POST existente:

- credenciais inválidas;
- `ReturnUrl` no login normal;
- resolução de Empresa ativa;
- seleção de Empresa;
- cookie.

Não implementar cadastro, recuperação de senha, confirmação, 2FA ou autenticação externa.

## Layout

Arquivo:

~~~text
src/Precificador.Web/Pages/Shared/_Layout.cshtml
~~~

Condicionar somente links operacionais ao estado autenticado.

Adicionar `Entrar` no ramo anônimo.

Não injetar `UserManager`, DbContext ou serviço de bootstrap no layout.

Logout continua sendo POST.

## Home

Arquivo:

~~~text
src/Precificador.Web/Pages/Index.cshtml
~~~

Usar somente o estado de autenticação disponível no Razor:

~~~razor
@if (User.Identity?.IsAuthenticated == true)
{
    // Cadastrar produto
}
else
{
    // Entrar
}
~~~

Não adicionar query ou PageModel apenas para decidir esse CTA.

## Testes obrigatórios

Cobrir W1–W10 da MEL012.

Pontos críticos:

1. Home anônima: links tenant-owned ausentes por `href`, não apenas por texto;
2. `Entrar` aponta para Login;
3. Home anônima não aponta para `/Produtos/Novo`;
4. Login sem usuários redireciona exatamente para `/Setup`;
5. usar factory isolada para cenário zero usuários;
6. Setup deixa Login acessível depois da criação do primeiro usuário;
7. acesso direto anônimo a `/Insumos/Novo` continua protegido;
8. navegação autenticada continua completa;
9. Login inválido continua funcional, criando usuário antes do GET;
10. autenticado acessando Login continua redirecionado a `/Index`.

Não depender da ordem de testes ou de estado deixado por fixture compartilhada.

## Persistência

Nenhuma migration.

Não alterar:

- Core;
- Infrastructure;
- DbContext;
- ModelSnapshot;
- entidades Identity/Empresa.

## Backlog

Na PR de implementação:

~~~text
MEL012: Pronto -> Concluído
~~~

Não alterar UC028 ou itens posteriores.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

## Retorno esperado

Informar:

- comportamento anônimo do layout;
- comportamento autenticado preservado;
- fluxo Login -> Setup no primeiro uso;
- testes adicionados/ajustados;
- confirmação de ausência de migration;
- resultado build/test;
- URL da PR.
