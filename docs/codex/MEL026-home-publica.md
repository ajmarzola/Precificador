# Instrução Codex — MEL026 Home pública

Você está implementando a MEL026 — Enriquecer Home pública com apresentação do Precificador.

## Fonte normativa

Leia:

1. AGENTS.md;
2. docs/development/improvements/MEL026-home-publica.md;
3. docs/development/improvements/MEL012-navegacao-anonima.md;
4. docs/use-cases/UC038-solicitar-acesso-precificador.md;
5. docs/product/vision.md;
6. docs/product/scope.md;
7. docs/development/backlog.md;
8. docs/development/testing-strategy.md;
9. docs/development/definition-of-done.md;
10. implementação atual de Index.cshtml(.cs), _Layout.cshtml e site.css;
11. testes atuais de Home, autenticação e solicitação de acesso.

A especificação normativa é:

~~~text
docs/development/improvements/MEL026-home-publica.md
~~~

## Branch

Use:

~~~text
feat/mel026-home-publica
~~~

Parta da master após o merge da documentação MEL026.

Não implemente na branch documental.

## Objetivo

Transformar a Home anônima em uma landing page leve e responsiva, sem alterar o fluxo da UC038 nem a navegação/autorização existente.

A página deve comunicar:

- o que é o Precificador;
- problema resolvido;
- capacidades reais;
- fluxo de uso;
- Entrar;
- Solicitar acesso.

## Restrições centrais

Não alterar:

- entidade de solicitação;
- handler/persistência UC038;
- Identity;
- policies;
- Dashboard;
- DbContext;
- migration;
- regras de precificação.

Evite alterar IndexModel. Se precisar mudar código funcional no PageModel, justifique claramente na PR.

## Estrutura mínima

Home anônima:

1. Hero;
2. proposta de valor;
3. 5–6 capacidades;
4. Como funciona em 3 passos;
5. Solicitar acesso;
6. footer atual.

## Hero

Incluir:

- Precificador;
- headline;
- descrição curta;
- CTA Entrar -> /Conta/Login;
- CTA Solicitar acesso -> #solicitar-acesso.

Não usar JavaScript para o CTA de âncora.

## Capacidades

Apresentar somente o que já existe:

- Insumos e histórico;
- Ficha Técnica/custos;
- formação de preço;
- margem/revisão;
- histórico/explicabilidade;
- catálogo/Coleções/multiempresa.

Não mencionar UC035, IA, scraping, CD ou branding configurável.

## Como funciona

Três passos:

~~~text
Centralize os dados
Calcule com dados vigentes
Decida com contexto
~~~

Não dizer que o sistema decide o preço automaticamente.

## Solicitação de acesso

Preserve o formulário atual e todos os campos/validações.

O id da seção deve continuar:

~~~text
solicitar-acesso
~~~

Texto ao redor pode ser melhorado.

Não criar página separada.

## Autenticação

Preserve:

~~~text
SystemAdmin GET / -> /Admin
tenant com Empresa Ativa GET / -> /Dashboard
POST autenticado de solicitação -> destino canônico, sem persistência
~~~

Não renderize landing page para substituir Dashboard.

## CSS

Use Bootstrap existente + site.css.

Crie classes com escopo home, por exemplo:

~~~text
.home-hero
.home-section
.home-feature-card
.home-step
.home-access
~~~

Remova o style inline atual do max-width do formulário.

Não adicionar framework, CDN, Google Fonts ou biblioteca de ícones.

## Responsividade

Testar estrutura para mobile.

Evitar:

- width fixa;
- overflow horizontal;
- CTA comprimido;
- cards com alturas forçadas desnecessárias.

Use grid Bootstrap.

## Acessibilidade

- exatamente um h1;
- h2 por seção;
- h3 quando necessário;
- labels do formulário preservados;
- foco visível;
- não depender apenas de cor;
- CTA com texto descritivo.

## Meta description

É permitido evoluir _Layout.cshtml para renderizar uma meta description opcional por ViewData.

Na Home, definir title/description institucional.

Não duplicar meta description.

## Testes

Atualize HomePageTests para validar conteúdo estrutural.

Preserve SolicitarAcessoPageTests e AutenticacaoPagesTests.

Evite snapshot HTML integral ou assert frágil por números/texto muito genérico.

Cobrir:

- 200 anônimo;
- um h1;
- CTAs;
- capacidades;
- Como funciona;
- formulário;
- ausência de links operacionais;
- meta description;
- redirects autenticados;
- UC038 continua funcionando.

## Sem antecipação

Não implemente:

- UC037;
- MEL027;
- UC035.

Não carregar logo/cor por Empresa.

Não adicionar analytics/tracking.

## Documentação ao concluir

- MEL026 -> Concluído;
- backlog -> Concluído;
- sequência restante:
  - UC037
  - MEL027
  - UC035
  - MEL013
  - MEL014.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
git diff --check
~~~

Não faça merge em master.
