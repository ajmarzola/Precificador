# Instrução Codex — MEL025 Erros e páginas não encontradas amigáveis

Você está implementando a MEL025 no repositório ajmarzola/Precificador.

Entrega implementada em `feat/mel025-erros-amigaveis`: a superfície `/Erro/{codigo:int}` substitui o scaffold `Error`, com layout independente, negociação HTML e testes Web em Production/Development. Esta instrução preserva o roteiro normativo da implementação; a especificação MEL025 registra o resultado.

## Fonte normativa

Leia:

1. AGENTS.md;
2. docs/development/improvements/MEL025-erros-paginas-nao-encontradas.md;
3. docs/development/backlog.md;
4. docs/development/testing-strategy.md;
5. docs/development/definition-of-done.md;
6. docs/architecture/architecture.md;
7. Program.cs;
8. Pages/Error.cshtml(.cs);
9. _Layout.cshtml;
10. testes Web atuais de 404, antiforgery, autenticação e tenant.

A especificação normativa é:

~~~text
docs/development/improvements/MEL025-erros-paginas-nao-encontradas.md
~~~

## Branch

Use:

~~~text
feat/mel025-erros-amigaveis
~~~

Parta da master após o merge da documentação MEL025.

Não implemente na branch documental.

## Objetivo

Entregar fallback Web amigável e seguro para:

- 404;
- 500 em ambiente não Development;
- status HTML vazios relevantes, como 400/405/503.

Preservar os códigos HTTP.

Não mudar autorização, domínio, banco ou precificação.

## Página de erro

Criar:

~~~text
/Erro/{codigo:int}
~~~

A página deve:

- funcionar em GET e POST;
- ser somente leitura;
- ignorar antiforgery próprio;
- usar ResponseCache NoStore;
- exibir título/mensagem por status;
- exibir Referência em 5xx;
- nunca exibir detalhes da exceção.

Não injetar DbContext, EmpresaContext, IAuthorizationService ou serviço de domínio.

## Layout

Criar layout mínimo específico para erro.

Não reutilizar como dependência obrigatória o _Layout operacional, porque ele injeta contexto/autorização e pode falhar durante tratamento de erro.

O layout mínimo pode usar Bootstrap/site.css estáticos.

Incluir:

~~~text
Precificador
Voltar ao início
~~~

Sem menu dinâmico.

## Pipeline

Fora de Development:

~~~text
UseExceptionHandler("/Erro/500")
~~~

ou equivalente.

Para status pages HTML vazias:

~~~text
UseStatusCodePagesWithReExecute("/Erro/{0}")
~~~

ou equivalente.

Aplicar o status page apenas a requests que aceitem text/html.

Não injetar HTML amigável em application/json, CSS, JS ou assets.

Preservar HSTS/HTTPS existentes.

## Development

Não esconder exceções de desenvolvimento atrás do fallback 500 de produção.

O 404 amigável pode permanecer.

## Status

Preservar:

~~~text
400 -> 400
403 -> 403
404 -> 404
405 -> 405
500 -> 500
503 -> 503
~~~

Nunca transformar em 200 apenas porque a página renderizou.

Código de /Erro fora de 400–599 deve cair de forma segura em 500.

## Mensagens

Mínimo:

~~~text
400 Solicitação inválida
403 Acesso não permitido
404 Página não encontrada
405 Operação não permitida
500 Não foi possível concluir sua solicitação
503 Serviço temporariamente indisponível
~~~

404:

~~~text
A página ou o recurso solicitado não existe ou não está disponível.
~~~

Não diferenciar cross-tenant de inexistente.

## Segurança

Nunca renderizar:

- exception.Message;
- stack;
- SQL;
- connection string;
- paths físicos;
- environment variables;
- EmpresaId;
- PII;
- dados de outro tenant.

Remover o scaffold em inglês da experiência publicada.

## Referência

Para 5xx:

~~~text
Activity.Current?.Id ?? HttpContext.TraceIdentifier
~~~

Exibir como Referência.

Não precisa exibir referência em 404.

## Antiforgery

A página /Erro ignora antiforgery porque não escreve.

Os demais POSTs permanecem protegidos.

Teste POST sem token deve conseguir terminar em 400 amigável, sem recursão.

## Content negotiation

Adicionar helper simples, se necessário, para decidir se request aceita HTML.

Evite framework/abstração desnecessária.

Regras:

~~~text
Accept text/html -> status page HTML
Accept application/json -> não injetar layout HTML
asset não HTML -> não injetar layout
~~~

## Teste 500

Não crie endpoint público /Throw ou similar.

Use apenas infraestrutura de teste, por exemplo:

- middleware no WebApplicationFactory;
- fake/interceptor;
- host de teste configurado para lançar.

Executar em ambiente Production ou outro não Development.

## Teste sem banco

Provar que /Erro/500 renderiza com conexão de banco indisponível.

Não consultar banco para decidir CTA ou mensagem.

## Testes obrigatórios

Cobrir a matriz MEL025:

- rota inexistente 404;
- NotFound de recurso real;
- cross-tenant 404 genérico;
- POST NotFound;
- POST sem antiforgery -> 400 amigável;
- exceção Production -> 500 amigável;
- referência 5xx;
- sem stack/SQL/message;
- Development não usa fallback de produção;
- banco indisponível não quebra /Erro;
- Accept application/json não recebe layout HTML;
- regressão de auth/policies.

## Sem mudanças permitidas

Não alterar:

- entidades;
- DbContext/model/migration;
- Identity;
- policies;
- LoginPath/AccessDeniedPath;
- regras de precificação;
- MEL026;
- UC037;
- MEL027.

## Documentação ao concluir

- MEL025 -> Concluído;
- backlog -> Concluído;
- arquitetura registra fallback Web de erro;
- ordem restante permanece:
  - MEL026
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
