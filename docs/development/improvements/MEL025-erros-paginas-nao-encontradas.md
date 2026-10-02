# MEL025 — Tratar erros e páginas não encontradas com experiência amigável

- **Origem:** acabamento pós-publicação / experiência de uso.
- **Classificação:** UX / resiliência de apresentação / hardening de produção.
- **Prioridade:** alta na nova fila de acabamento.
- **Estado:** Pronto.
- **Dependências:** MEL021 concluída.
- **Alteração de regra de negócio:** não.
- **Alteração de domínio/schema:** não.
- **Migration:** não.

## Objetivo

Substituir respostas técnicas, vazias ou provenientes do scaffold padrão por uma experiência de erro coerente, segura e em português para navegação Web.

A MEL025 entrega:

- página amigável para 404;
- página amigável para erro inesperado 500 em ambientes não Development;
- tratamento visual de respostas HTML vazias 4xx/5xx relevantes;
- preservação do status HTTP original;
- preservação do diagnóstico em Development;
- ausência de vazamento de detalhes técnicos;
- página de erro resiliente, sem depender de banco, Empresa Ativa ou autorização contextual para renderizar.

## Problema atual

O pipeline possui em ambiente não Development:

~~~text
UseExceptionHandler("/Error")
~~~

mas a página atual ainda é o scaffold padrão em inglês:

~~~text
Error.
An error occurred while processing your request.
Development Mode
~~~

Respostas produzidas por NotFound(), rota inexistente ou recurso cross-tenant preservam corretamente HTTP 404, porém não possuem experiência visual própria.

## 404

Exibir:

~~~text
Página não encontrada
~~~

Mensagem sugerida:

~~~text
A página ou o recurso solicitado não existe ou não está disponível.
~~~

A redação deve permanecer genérica e não distinguir:

- ID inexistente;
- objeto de outro tenant;
- rota inexistente;
- objeto removido;
- recurso não disponível ao usuário.

Isso preserva a regra já existente de não revelar recursos cross-tenant.

## 500

Em ambiente diferente de Development, exceção não tratada deve produzir HTTP 500 com página amigável em português.

Título sugerido:

~~~text
Não foi possível concluir sua solicitação
~~~

Mensagem sugerida:

~~~text
Ocorreu um erro inesperado. Tente novamente. Se o problema persistir, informe a referência exibida nesta página.
~~~

Não mostrar detalhes da exceção.

## Outros status HTML vazios

A infraestrutura pode reutilizar a mesma superfície de erro.

Mapeamento mínimo:

~~~text
400 -> Solicitação inválida
403 -> Acesso não permitido
404 -> Página não encontrada
405 -> Operação não permitida
503 -> Serviço temporariamente indisponível
outro 4xx/5xx -> Não foi possível concluir a solicitação
~~~

Nunca transformar status esperado em HTTP 200.

## Autenticação e autorização permanecem iguais

MEL025 não redefine:

- LoginPath;
- AccessDeniedPath;
- EmpresaAtiva;
- SystemAdmin;
- AdministradorEmpresa;
- policies existentes.

Exemplo:

~~~text
anônimo acessa /Produtos
=> continua seguindo cookie auth / Login
~~~

A MEL025 é fallback de erro, não um novo fluxo de autorização.

## Pipeline

Fora de Development, substituir o destino do exception handler por uma página amigável, por exemplo:

~~~text
UseExceptionHandler("/Erro/500")
~~~

Para respostas de status vazias destinadas a navegador, usar mecanismo equivalente a:

~~~text
UseStatusCodePagesWithReExecute("/Erro/{0}")
~~~

O desenho exato pode variar, desde que:

- preserve o status original;
- não use redirect 302 para representar 404/500;
- trate GET e POST;
- não entre em loop ao reexecutar a rota de erro;
- não altere respostas que já possuem corpo;
- não altere redirects existentes.

## Somente navegação HTML

O reexecute amigável é responsabilidade de UI Web.

Aplicá-lo quando a requisição aceitar HTML, por exemplo:

~~~text
Accept contém text/html
~~~

Não injetar documento HTML de erro em:

- assets CSS/JS/imagens;
- futuros endpoints JSON/API;
- clientes que não aceitam HTML.

Exemplo:

~~~text
GET /rota-inexistente
Accept: text/html
=> 404 + página amigável

GET /rota-inexistente
Accept: application/json
=> 404 preservado sem layout HTML
~~~

## Development

Exceções em Development continuam priorizando diagnóstico técnico para o desenvolvedor.

~~~text
Development
-> exceção não tratada permanece diagnosticável

Production/outro ambiente
-> página amigável 500
~~~

O 404 amigável pode permanecer ativo também em Development.

## Superfície /Erro

Criar página:

~~~text
/Erro/{codigo:int}
~~~

Exemplos:

~~~text
/Erro/404
/Erro/500
~~~

A página é somente leitura e não consulta banco.

Não depende de:

- PrecificadorDbContext;
- EmpresaContext;
- IDataOperacionalEmpresa;
- IAuthorizationService;
- serviços de domínio.

## GET e POST

A reexecução pode preservar o método HTTP original.

A página deve renderizar no mínimo:

~~~text
GET
POST
~~~

Compartilhar a mesma inicialização interna.

Não aceitar:

~~~text
POST original falha
-> /Erro/500
-> handler POST inexistente
-> resposta vira 405
~~~

## Antiforgery da página de erro

A página de erro não realiza mutação e deve ignorar antiforgery próprio, equivalente ao scaffold atual com IgnoreAntiforgeryToken.

Motivo:

- um 400 pode ter sido causado por token ausente/inválido;
- a página de erro não pode provocar novo 400 e recursão.

Isso não remove antiforgery dos POSTs normais.

## Status HTTP

Preservar/estabelecer o status correto.

~~~text
rota inexistente -> 404
NotFound() -> 404
exceção em Production -> 500
antiforgery inválido -> 400
~~~

A página amigável não converte o resultado em 200.

Código acessado diretamente fora da faixa 400–599 deve ser normalizado de forma segura, preferencialmente 500, sem lançar nova exceção.

## Layout resiliente

Não usar o layout operacional atual como dependência obrigatória.

O layout normal injeta EmpresaContext e IAuthorizationService e pode falhar novamente quando o problema original envolver banco, tenant ou autorização.

Criar layout mínimo, por exemplo:

~~~text
Pages/Shared/_LayoutErro.cshtml
~~~

Conteúdo mínimo:

- marca Precificador;
- conteúdo da página;
- link Voltar ao início;
- CSS estático seguro;
- footer simples opcional.

Sem consulta ou autorização dinâmica.

## Ação principal

Toda página HTML de erro oferece:

~~~text
Voltar ao início
~~~

destino:

~~~text
/
~~~

Não duplicar na página de erro a lógica de destino pós-login.

## Referência de atendimento

Para erros 5xx, obter:

~~~text
Activity.Current?.Id
ou
HttpContext.TraceIdentifier
~~~

Exibir:

~~~text
Referência: <id>
~~~

Para 404 e demais erros esperados, a referência pode ser omitida.

## Segurança de informação

Jamais renderizar:

- exception.Message;
- stack trace;
- InnerException;
- nomes de tabela;
- SQL;
- connection string;
- caminho físico;
- variáveis de ambiente;
- segredos;
- e-mail/PII;
- EmpresaId;
- nome de recurso cross-tenant.

Remover da experiência publicada os textos do scaffold em inglês e a seção Development Mode.

## Logging

Continuar usando logging padrão do ASP.NET Core.

A exceção inesperada deve permanecer observável pelo middleware/framework.

Não capturar exceção apenas para retornar página sem logging.

Não duplicar a mesma exceção manualmente se o exception handler já a registrar.

404 comum não precisa ser Error no log.

## Cache

A página usa ResponseCache com:

~~~text
Duration = 0
NoStore = true
~~~

ou equivalente.

## SEO

Preservar o status correto.

É aceitável usar:

~~~text
robots = noindex, nofollow
~~~

Não redirecionar 404 para Home.

## 404 de rota, recurso e cross-tenant

A mesma experiência deve cobrir:

~~~text
/rota-inexistente
/Produtos/Detalhes/999999
recurso da Empresa B consultado pela Empresa A
~~~

Todos retornam 404 genérico e o corpo não revela qual cenário ocorreu.

## Erros em POST

Cobrir:

~~~text
POST para recurso inexistente -> 404 amigável
POST sem antiforgery -> 400 amigável
POST com exceção inesperada em Production -> 500 amigável
~~~

Reexecução não pode converter o erro em 405.

## Respostas já tratadas

Não substituir:

- ModelState inválido com página e mensagens;
- duplicidade tratada no formulário;
- TempData;
- respostas com corpo próprio;
- redirects;
- Login;
- seleção de Empresa.

MEL025 é fallback de infraestrutura.

## Sem domínio ou persistência

Não criar:

- entidade;
- enum de domínio;
- migration;
- configuração persistida;
- tabela de erros;
- auditoria própria.

Não alterar precificação.

## Testes — 404

### Rota inexistente HTML

Validar:

- status 404;
- “Página não encontrada”;
- “Voltar ao início”;
- sem scaffold em inglês;
- sem stack trace.

### Recurso inexistente

Usar página real, por exemplo:

~~~text
/Produtos/Detalhes/999999
~~~

com usuário/Empresa válidos.

Validar o mesmo contrato 404.

### Cross-tenant

Criar recurso na Empresa B e consultar pela Empresa A.

Validar:

- 404;
- mesma mensagem genérica;
- nome/dado externo ausente.

## Testes — POST e antiforgery

Usar handler real protegido.

Enviar POST HTML sem token.

Validar:

~~~text
400
Solicitação inválida
~~~

Também testar POST real que produza NotFound, comprovando suporte a reexecução POST.

## Teste — exceção 500

Executar aplicação de teste em ambiente não Development.

Provocar exceção somente na infraestrutura de teste.

Não adicionar endpoint de produção como:

~~~text
/Throw
/TesteErro
~~~

Aceitável usar middleware/fake/interceptor apenas no WebApplicationFactory.

Validar:

- HTTP 500;
- página amigável;
- Referência presente;
- exception.Message ausente;
- stack trace ausente;
- Development Mode ausente;
- connection string/SQL ausentes.

## Teste — resiliência sem banco

A página /Erro/500 deve renderizar mesmo com banco indisponível.

O objetivo é provar:

~~~text
renderização do erro
!= depende do PrecificadorDbContext
~~~

## Teste — Development

Provocar exceção em host Development.

Validar que ela não é silenciosamente convertida no fallback 500 de produção.

Não é necessário comparar o HTML exato de diagnóstico.

## Teste — conteúdo não HTML

Enviar:

~~~text
Accept: application/json
~~~

para rota inexistente.

Validar:

- 404;
- ausência do layout HTML amigável.

## Critérios de aceitação

- **CA01:** 404 HTML possui experiência amigável em português.
- **CA02:** rota inexistente retorna 404, não 200/302.
- **CA03:** NotFound funcional usa a mesma experiência.
- **CA04:** cross-tenant permanece indistinguível de inexistente.
- **CA05:** 404 não expõe dados do recurso.
- **CA06:** ambiente não Development possui fallback amigável 500.
- **CA07:** erro 500 preserva HTTP 500.
- **CA08:** 500 não exibe exception.Message.
- **CA09:** 500 não exibe stack trace.
- **CA10:** 500 não exibe SQL/connection string.
- **CA11:** scaffold em inglês é removido da experiência publicada.
- **CA12:** 5xx exibe referência do request.
- **CA13:** cache da página de erro é desabilitado.
- **CA14:** página não consulta PrecificadorDbContext.
- **CA15:** página não depende de EmpresaContext.
- **CA16:** página não depende de IAuthorizationService.
- **CA17:** layout de erro é mínimo e resiliente.
- **CA18:** existe ação Voltar ao início.
- **CA19:** GET renderiza /Erro.
- **CA20:** POST renderiza /Erro.
- **CA21:** reexecução POST não vira 405.
- **CA22:** página de erro ignora antiforgery próprio.
- **CA23:** POSTs normais continuam protegidos por antiforgery.
- **CA24:** antiforgery inválido pode resultar em 400 amigável HTML.
- **CA25:** 400 preserva HTTP 400.
- **CA26:** 405 vazio pode receber mensagem amigável sem virar 200.
- **CA27:** 503 vazio pode receber mensagem amigável sem virar 200.
- **CA28:** redirects existentes não são reescritos.
- **CA29:** LoginPath/AccessDeniedPath não mudam.
- **CA30:** respostas com corpo próprio não são substituídas.
- **CA31:** status code pages atuam apenas sobre respostas apropriadas.
- **CA32:** request HTML recebe página amigável.
- **CA33:** request não HTML não recebe documento HTML da UI.
- **CA34:** asset inexistente não precisa receber layout HTML.
- **CA35:** Development não esconde exceção técnica atrás da página de produção.
- **CA36:** 404 amigável pode funcionar em Development.
- **CA37:** HSTS/HTTPS existentes permanecem.
- **CA38:** logging padrão de exceção permanece.
- **CA39:** 404 comum não exige logging Error customizado.
- **CA40:** nenhuma entidade é criada.
- **CA41:** nenhuma migration/ModelSnapshot é alterado.
- **CA42:** nenhuma regra de negócio/precificação muda.
- **CA43:** nenhuma rota pública de teste de exceção é criada.
- **CA44:** 500 renderiza mesmo com banco indisponível.
- **CA45:** autorização existente permanece.
- **CA46:** 404 cross-tenant continua sem revelar existência externa.
- **CA47:** CI segue a estratégia MEL028.
- **CA48:** Build Release fica verde.
- **CA49:** unitários aplicáveis ficam verdes.
- **CA50:** integração Web fica verde.

## Matriz mínima de testes

### 404

- rota inexistente;
- Produto inexistente;
- Produto cross-tenant;
- GET;
- POST NotFound;
- status;
- mensagem genérica;
- ausência de dado externo.

### 400

- POST sem antiforgery;
- status 400;
- página amigável;
- sem loop.

### 500 Production

- exceção induzida apenas no host de teste;
- status 500;
- referência;
- sem detalhe técnico.

### Development

- exceção induzida;
- fallback Production não utilizado.

### Resiliência

- /Erro/500 com banco indisponível;
- sem Empresa Ativa;
- anônimo;
- página ainda renderiza.

### Content negotiation

- Accept text/html -> página amigável;
- Accept application/json -> status sem layout HTML.

### Regressão

- Login anônimo continua redirect;
- /Produtos anônimo continua protegido;
- SystemAdmin continua em /Admin;
- áreas tenant continuam usando EmpresaAtiva;
- ModelState/PRG/TempData permanecem.

## Arquivos esperados

~~~text
src/Precificador.Web/Program.cs
src/Precificador.Web/Pages/Erro.cshtml
src/Precificador.Web/Pages/Erro.cshtml.cs
src/Precificador.Web/Pages/Shared/_LayoutErro.cshtml
src/Precificador.Web/Pages/Error.cshtml(.cs)  # remover/substituir scaffold

tests/Precificador.Tests.Integration/Web/ErroPageTests.cs

docs/development/improvements/MEL025-erros-paginas-nao-encontradas.md
docs/codex/MEL025-erros-paginas-nao-encontradas.md
docs/development/backlog.md
docs/architecture/architecture.md
~~~

## Fora do escopo

- APM/Application Insights/Sentry;
- persistência de logs;
- consulta de logs;
- alterar mensagens de cada formulário;
- retry/circuit breaker;
- maintenance mode;
- redesign geral;
- MEL026;
- UC037;
- MEL027;
- API ProblemDetails;
- tradução multilíngue.

## Definition of Done

MEL025 está concluída quando:

- 404 HTML é amigável e genérico;
- 500 não Development é amigável e seguro;
- códigos HTTP são preservados;
- GET e POST são cobertos;
- antiforgery não entra em loop;
- Development preserva diagnóstico;
- página de erro independe de banco/tenant/autorização contextual;
- detalhes sensíveis não são expostos;
- request não HTML não recebe layout HTML;
- autenticação/autorização permanecem;
- não há migration;
- documentação/backlog estão alinhados;
- suíte completa está verde.

## Branch de implementação

~~~text
feat/mel025-erros-amigaveis
~~~

## Commit sugerido

~~~text
feat: adiciona paginas de erro amigaveis
~~~
