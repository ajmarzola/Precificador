# MEL012 — Adequar navegação para usuário anônimo

- **Origem:** Review MVP 2026-09-16
- **Classificação:** UX / autenticação
- **Prioridade:** baixa
- **Estado:** Pronto
- **Ordem na fila pendente:** 06
- **Dependência:** FT002
- **Alteração de schema:** não
- **Migration:** não
- **Alteração de domínio:** não
- **Alteração de autorização:** não

## Objetivo

Adequar a navegação pública ao estado real de autenticação, evitando oferecer ações operacionais que o usuário anônimo não pode executar.

A MEL012 deve produzir uma navegação coerente em três situações:

1. aplicação ainda sem usuário — primeiro uso;
2. aplicação já configurada, usuário anônimo;
3. usuário autenticado.

Esta melhoria é de **experiência de navegação**, não de segurança.

As rotas operacionais continuam protegidas por FT002 independentemente de estarem visíveis ou não no menu.

## Problema atual

O layout atual exibe sempre:

~~~text
Início
Insumos
Produtos
Configurações
~~~

mesmo quando:

~~~text
User.Identity.IsAuthenticated != true
~~~

As rotas `/Insumos`, `/Produtos` e `/Configuracoes` estão corretamente protegidas pela política `EmpresaAtiva`, mas o usuário anônimo ainda vê links que apenas o levam ao Login.

A Home também exibe atualmente:

~~~text
Cadastrar produto
~~~

para qualquer visitante, inclusive anônimo.

Portanto, o problema não é somente o navbar: a página inicial também oferece uma ação operacional incompatível com o estado do usuário.

## Princípio

A interface deve oferecer apenas ações coerentes com o estado atual do usuário.

Isso não substitui autorização.

Regra:

~~~text
ocultar link != autorizar rota
~~

Mesmo depois da MEL012:

~~~text
GET /Insumos/Novo como anônimo
=> continua redirecionando para /Conta/Login
~~~

Nenhuma proteção existente deve ser removida ou enfraquecida.

## Estados de navegação

### Estado A — anônimo

Quando:

~~~text
User.Identity?.IsAuthenticated != true
~~~

o navbar deve exibir somente ações públicas compatíveis:

~~~text
Precificador
Início
Entrar
~~~

Não exibir:

~~~text
Insumos
Produtos
Configurações
Empresa ativa
Sair
~~~

### Estado B — autenticado

Quando:

~~~text
User.Identity?.IsAuthenticated == true
~~~

preservar a navegação operacional atual:

~~~text
Precificador
Início
Insumos
Produtos
Configurações
Empresa ativa: <nome>
Sair
~~~

Não exibir o link `Entrar` para usuário autenticado.

MEL012 não altera a forma como a Empresa Ativa é resolvida.

### Estado C — primeiro uso

Quando não existe nenhum usuário Identity, `/Setup` continua sendo o único bootstrap válido conforme FT002.

O usuário não precisa conhecer manualmente a URL `/Setup`.

O ponto público `Entrar` deve conduzir ao Login, e o GET do Login deve detectar primeiro uso e encaminhar para Setup:

~~~text
GET /Conta/Login

se usuário já autenticado:
    -> /Index
senão se não existe nenhum usuário Identity:
    -> /Setup
senão:
    -> exibir Login
~~~

Assim, o mesmo ponto de entrada público funciona antes e depois da configuração inicial.

## Decisão — não consultar estado de bootstrap no Layout

Não consultar `UserManager.Users.AnyAsync()` dentro de `_Layout.cshtml`.

Motivos:

- o layout é renderizado em várias páginas;
- a existência de usuários não precisa ser consultada a cada renderização apenas para decidir o menu;
- o Login já é a superfície correta para decidir entre autenticação normal e bootstrap inicial.

Portanto:

~~~text
navbar anônimo -> Entrar
Entrar -> /Conta/Login
/Conta/Login sem usuários -> /Setup
~~~

Não criar serviço/cache de estado de bootstrap apenas para esta MEL.

## Layout

Arquivo atual:

~~~text
src/Precificador.Web/Pages/Shared/_Layout.cshtml
~~~

### Navegação pública

Manter sempre:

- marca `Precificador` apontando para `/Index`;
- link `Início` apontando para `/Index`.

### Bloco operacional

Mover os links:

- Insumos;
- Produtos;
- Configurações;

para dentro do mesmo estado autenticado que hoje já controla Empresa ativa e Logout.

Estrutura conceitual:

~~~razor
<ul class="navbar-nav flex-grow-1">
    <li>Início</li>

    @if (User.Identity?.IsAuthenticated == true)
    {
        <li>Insumos</li>
        <li>Produtos</li>
        <li>Configurações</li>
    }
</ul>

@if (User.Identity?.IsAuthenticated == true)
{
    Empresa ativa
    Logout
}
else
{
    Entrar
}
~~~

É aceitável organizar os blocos de outra forma, desde que o HTML final obedeça ao contrato.

### Link Entrar

Para usuário anônimo, exibir link visível:

~~~text
Entrar
~~~

apontando para:

~~~text
/Conta/Login
~~~

Não implementar formulário de Login dentro do layout.

Não adicionar auto-registro público.

## Home

Arquivo atual:

~~~text
src/Precificador.Web/Pages/Index.cshtml
~~~

Hoje a Home contém:

~~~text
Cadastrar produto
~~~

independentemente da autenticação.

Alterar para:

### Anônimo

Exibir a apresentação da aplicação e uma ação pública:

~~~text
Entrar
~~~

Não exibir:

~~~text
Cadastrar produto
~~~

nem qualquer outro CTA tenant-owned.

### Autenticado

Preservar o CTA operacional existente:

~~~text
Cadastrar produto
~~~

MEL012 não redesenha a Home e não introduz dashboard.

## Login — primeiro uso

Arquivo atual:

~~~text
src/Precificador.Web/Pages/Conta/Login.cshtml.cs
~~~

O GET atual diferencia apenas:

- autenticado;
- anônimo.

Após MEL012, implementar a terceira condição de bootstrap.

Comportamento esperado:

~~~csharp
public async Task<IActionResult> OnGetAsync()
{
    if (User.Identity?.IsAuthenticated == true)
    {
        return RedirectToPage("/Index");
    }

    if (!await userManager.Users.AnyAsync())
    {
        return RedirectToPage("/Setup");
    }

    return Page();
}
~~~

Solução equivalente é aceitável.

### Não alterar POST de autenticação sem necessidade

O fluxo normal de POST continua pertencendo à FT002.

Preservar:

- e-mail/senha;
- mensagem de credenciais inválidas;
- limpeza de Empresa ativa antes da autenticação;
- seleção automática para um vínculo;
- seleção manual para múltiplos vínculos;
- `ReturnUrl` para login normal;
- cookie Identity.

Não implementar recuperação de senha, cadastro ou confirmação de e-mail.

## Setup

Preservar integralmente o contrato atual de FT002:

~~~text
sem usuários -> /Setup disponível
com qualquer usuário -> /Setup retorna 404
~~~

MEL012 não altera campos, regras de senha, transação ou criação do primeiro vínculo.

Após Setup válido, o redirect atual para:

~~~text
/Conta/Login
~~~

continua correto.

Como o primeiro usuário já existe nesse momento, o GET do Login deve exibir o formulário normalmente.

## ReturnUrl

Para aplicação já configurada, preservar o comportamento normal do middleware Identity:

~~~text
anônimo tenta /Produtos/Novo
-> /Conta/Login?ReturnUrl=...
-> login válido
-> retorna ao destino permitido quando aplicável
~~~

No primeiro uso, se o GET de Login encaminhar para Setup, não é requisito da MEL012 preservar um `ReturnUrl` anterior através do bootstrap inicial.

Não criar complexidade adicional para esse caso excepcional.

## Usuário autenticado sem Empresa Ativa

MEL012 não redefine esse fluxo.

FT002 continua responsável por:

- selecionar automaticamente a única Empresa elegível;
- redirecionar para `/Empresas/Selecionar` quando houver múltiplas;
- negar área de negócio quando não houver Empresa elegível.

Não usar MEL012 para mudar regras de seleção de Empresa.

## Autorização

Não alterar:

~~~csharp
AuthorizeFolder("/Insumos", "EmpresaAtiva")
AuthorizeFolder("/Produtos", "EmpresaAtiva")
AuthorizeFolder("/Configuracoes", "EmpresaAtiva")
~~~

Não alterar:

- `EmpresaAtivaRequirement`;
- `EmpresaAtivaHandler`;
- configuração do cookie;
- `LoginPath`;
- `AccessDeniedPath`;
- antiforgery;
- sessão de Empresa ativa.

MEL012 não substitui proteção server-side por condicionais Razor.

## Logout

Preservar o Logout somente para usuário autenticado.

Continuar usando:

~~~text
POST /Conta/Logout
~~~

com antiforgery padrão.

Não trocar Logout por GET.

Usuário anônimo não deve ver `Sair`.

## Empresa ativa

Usuário anônimo não deve ver:

~~~text
Empresa ativa: não selecionada
~~~

nem qualquer informação de tenant.

Usuário autenticado continua vendo o estado atual da Empresa no layout.

Não alterar `EmpresaContext`.

## Segurança

A MEL012 reduz ruído visual, mas **não é controle de segurança**.

Testes devem continuar provando que acesso direto anônimo a rotas operacionais é bloqueado.

Não considerar como teste suficiente apenas:

~~~text
link não aparece no HTML
~~~

É obrigatório preservar pelo menos uma prova direta da proteção da rota.

## Multiempresa

Nenhuma alteração.

Não mudar:

- GQF;
- write guards;
- vínculo UsuarioEmpresa;
- Empresa Ativa;
- isolamento tenant-aware.

A navegação autenticada continua refletindo a Empresa já resolvida pela FT002.

## Persistência

Nenhuma alteração.

Não criar migration.

Não alterar:

- entidades Core;
- Identity schema;
- DbContext;
- ModelSnapshot;
- Empresas;
- UsuarioEmpresas.

## Arquivos esperados

Mudança mínima esperada em:

~~~text
src/Precificador.Web/Pages/Shared/_Layout.cshtml
src/Precificador.Web/Pages/Index.cshtml
src/Precificador.Web/Pages/Conta/Login.cshtml.cs
tests/Precificador.Tests.Integration/Web/AutenticacaoPagesTests.cs
~~`

Pode ser necessário adaptar testes auxiliares que hoje assumem que GET `/Conta/Login` funciona em banco sem usuários.

Não é esperado alterar:

- `Program.cs`;
- PageModels de Insumo/Produto/Configuração;
- Core;
- Infrastructure.

## Critérios de aceitação

- **CA01:** usuário anônimo vê `Início`.
- **CA02:** usuário anônimo vê `Entrar` apontando para `/Conta/Login`.
- **CA03:** usuário anônimo não vê link de Insumos.
- **CA04:** usuário anônimo não vê link de Produtos.
- **CA05:** usuário anônimo não vê link de Configurações.
- **CA06:** usuário anônimo não vê Empresa ativa.
- **CA07:** usuário anônimo não vê Logout/Sair.
- **CA08:** Home anônima não exibe `Cadastrar produto`.
- **CA09:** Home anônima exibe ação `Entrar`.
- **CA10:** usuário autenticado continua vendo Insumos, Produtos e Configurações.
- **CA11:** usuário autenticado continua vendo Empresa ativa e Logout.
- **CA12:** usuário autenticado não vê link `Entrar` no bloco de autenticação.
- **CA13:** Home autenticada continua exibindo `Cadastrar produto`.
- **CA14:** GET de Login sem nenhum usuário redireciona para `/Setup`.
- **CA15:** GET de Login com usuário existente retorna 200 e exibe formulário.
- **CA16:** GET de Login para usuário já autenticado continua redirecionando para `/Index`.
- **CA17:** `/Setup` sem usuários continua acessível.
- **CA18:** `/Setup` com usuário existente continua retornando 404.
- **CA19:** Setup válido continua redirecionando para `/Conta/Login` e, depois da criação, o Login fica acessível.
- **CA20:** acesso direto anônimo a rota operacional continua redirecionando para Login.
- **CA21:** Login inválido continua exibindo `E-mail ou senha inválidos.`.
- **CA22:** Logout continua sendo POST e limpa autenticação/Empresa ativa conforme FT002.
- **CA23:** nenhum mecanismo de autorização é removido ou relaxado.
- **CA24:** nenhuma migration/schema/model snapshot é alterado.
- **CA25:** nenhum registro público/recuperação de senha/2FA é introduzido.
- **CA26:** `_Layout` não consulta o banco para descobrir se Setup está disponível.
- **CA27:** itens posteriores da fila não são antecipados.

## Matriz de testes

Priorizar:

~~~text
tests/Precificador.Tests.Integration/Web/AutenticacaoPagesTests.cs
~~~

É aceitável criar `NavegacaoPageTests.cs` se isso deixar os cenários mais coesos.

### W1 — Home anônima

Com aplicação já configurada e cliente anônimo:

~~~text
GET /
=> 200
~~~

Validar no HTML:

- `Entrar` existe;
- link para `/Conta/Login` existe;
- não existe link para `/Insumos/Index`;
- não existe link para `/Produtos/Index`;
- não existe link para `/Configuracoes/Precificacao`;
- não existe link para `/Produtos/Novo`;
- não existe `Empresa ativa:`;
- não existe botão/form `Sair`.

Preferir asserts sobre `href`/form reais, não apenas ausência das palavras `Produtos` ou `Configurações`, pois esses termos podem aparecer futuramente em texto institucional.

### W2 — Login anônimo com aplicação configurada

Com pelo menos um usuário existente:

~~~text
GET /Conta/Login
=> 200
~~~

Validar formulário de autenticação e navegação anônima sem links operacionais.

### W3 — primeiro uso encaminha para Setup

Usar factory isolada sem usuários.

~~~text
GET /Conta/Login
AllowAutoRedirect = false
=> 302
Location = /Setup
~~~

Não executar esse cenário em fixture compartilhada cujo banco possa ter usuário criado por outro teste.

### W4 — Setup disponível somente no primeiro uso

#### sem usuários

~~~text
GET /Setup
=> 200
~~~

#### com usuário existente

~~~text
GET /Setup
=> 404
~~~

Preservar cobertura FT002 existente quando houver.

### W5 — Setup conclui para Login funcional

Em ambiente isolado:

1. GET `/Setup`;
2. POST válido;
3. confirmar redirect para `/Conta/Login`;
4. GET `/Conta/Login`;
5. confirmar HTTP 200 e formulário.

O objetivo é provar que o novo redirect do Login para Setup não cria loop depois do bootstrap.

### W6 — rota operacional continua protegida

Anônimo:

~~~text
GET /Insumos/Novo
AllowAutoRedirect = false
=> Redirect
Location contém /Conta/Login
~~~

Preservar teste existente.

### W7 — navegação autenticada preservada

Usar cliente autenticado com Empresa ativa.

GET `/` ou outra página com layout.

Validar:

- link Insumos;
- link Produtos;
- link Configurações;
- `Empresa ativa:`;
- form POST de Logout/Sair;
- ausência do link público `Entrar`.

### W8 — Home autenticada

Validar:

~~~text
/Cadastrar produto -> /Produtos/Novo
~~~

ou equivalente por inspeção do link.

Não remover o CTA operacional para usuários autenticados.

### W9 — Login inválido permanece funcional

Criar/garantir usuário existente antes de abrir Login.

POST com credencial inválida:

~~~text
=> HTTP 200
=> E-mail ou senha inválidos.
~~~

Adaptar o teste atual, pois um banco totalmente vazio agora será redirecionado para Setup antes de fornecer o formulário de Login.

### W10 — usuário já autenticado não abre Login

Cliente autenticado:

~~~text
GET /Conta/Login
AllowAutoRedirect = false
=> Redirect /Index
~~~

Preservar comportamento atual.

## Estratégia para testes com usuários

Como `CustomWebApplicationFactory` usa SQLite em memória por instância e algumas fixtures são compartilhadas, cenários dependentes de:

~~~text
zero usuários
versus
pelo menos um usuário
~~~

devem controlar explicitamente o estado.

Para cenários de primeiro uso, preferir:

~~~text
using var factory = new CustomWebApplicationFactory();
~~~

criada dentro do próprio teste.

Para aplicação configurada, reutilizar helper de autenticação existente ou criar o usuário necessário explicitamente.

Não depender de ordem de execução dos testes.

## Implementação esperada

### `_Layout.cshtml`

Condicionar links tenant-owned ao estado autenticado e adicionar `Entrar` no ramo anônimo.

### `Index.cshtml`

Estrutura conceitual:

~~~razor
@if (User.Identity?.IsAuthenticated == true)
{
    <p><a asp-page="/Produtos/Novo">Cadastrar produto</a></p>
}
else
{
    <p><a asp-page="/Conta/Login">Entrar</a></p>
}
~~~

### `Login.cshtml.cs`

Converter o GET para assíncrono para consultar apenas nesta superfície se existe usuário Identity.

Não mover essa consulta para o layout.

## Fora do escopo

- redesign visual do navbar;
- dashboard;
- menu responsivo novo;
- roles/permissões;
- autorização granular;
- administração de usuários;
- auto-registro;
- convite;
- recuperação de senha;
- confirmação de e-mail;
- 2FA;
- autenticação externa;
- alteração de Empresa ativa;
- alteração de seleção de Empresa;
- redirecionamento pós-login além do comportamento já existente;
- preservar ReturnUrl através do primeiro bootstrap;
- MEL013/MEL014;
- UC028+.

## Definition of Done específica

MEL012 está concluída quando:

- navbar anônimo não oferece ações tenant-owned;
- navbar anônimo oferece `Entrar`;
- Home anônima não oferece `Cadastrar produto`;
- Home autenticada preserva o CTA atual;
- primeiro acesso por `Entrar` chega ao Setup sem o usuário conhecer a URL;
- Login normal permanece funcional depois do bootstrap;
- navegação autenticada atual é preservada;
- proteção direta das rotas operacionais continua comprovada;
- Login inválido continua funcional;
- Setup continua único;
- Logout continua POST;
- `_Layout` não consulta banco/Identity para estado de bootstrap;
- nenhuma migration/schema/domínio/autorização é alterado;
- matriz W1–W10 está coberta ou comprovada por regressão existente;
- build Release tem 0 erros e sem warnings novos relevantes;
- suíte completa está verde;
- backlog altera somente MEL012 de `Pronto` para `Concluído` na PR de implementação.

## Branch sugerida

~~~text
fix/mel012-navegacao-anonima
~~~

## Commit sugerido

~~~text
fix: adequa navegacao para usuario anonimo
~~~
