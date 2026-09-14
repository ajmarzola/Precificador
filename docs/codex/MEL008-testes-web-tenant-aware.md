# Instrução Codex — MEL008: Infraestrutura Web tenant-aware de testes

## Tarefa

Implementar integralmente a MEL008 conforme docs/development/improvements/MEL008-testes-web-tenant-aware.md.

Branch obrigatória:

~~~text
refactor/mel008-testes-web-tenant-aware
~~~

Não editar, commitar ou fazer push direto em master.
Não fazer merge da própria implementação.

## Precondições

Antes de alterar:

1. partir da master atualizada;
2. confirmar branch refactor/mel008-testes-web-tenant-aware;
3. ler AGENTS.md;
4. ler a especificação MEL008;
5. ler docs/development/backlog.md;
6. confirmar MEL008 como Pronto e sem gate pendente no backlog;
7. ler testing-strategy e Definition of Done;
8. inspecionar CustomWebApplicationFactory e as suítes Web atuais.

## Escopo

Alterar somente infraestrutura e testes no projeto Precificador.Tests.Integration, além de documentação de testes.

Não alterar src/.
Não criar migration.

## Criar infraestrutura comum

Adicionar estrutura equivalente a:

~~~text
Web/WebTestContext.cs
Web/WebTestHtml.cs
Web/ContextoEmpresaTeste.cs
~~~

### WebTestContext

Deve encapsular apenas:

- criação de HttpClient com AllowAutoRedirect=false e cookies;
- criação de usuário Identity de teste;
- criação de vínculos UsuarioEmpresa;
- LoginAsync pelo endpoint real /Conta/Login;
- CriarClienteAutenticadoAsync para usuário com vínculo único;
- criação simples de Empresa auxiliar.

Pode usar um record UsuarioTeste com Id, Email e Senha.

Não definir Empresa Ativa diretamente em Session.
Não injetar Claims/cookies manualmente.

### WebTestHtml

Centralizar:

- ExtrairTokenAntiforgery;
- ObterTokenAntiforgeryAsync;
- opcionalmente LerHtmlDecodificadoAsync se a repetição justificar.

Token ausente deve falhar claramente.

### ContextoEmpresaTeste

Extrair a implementação repetida de IEmpresaContext para uma única classe comum.

## Migração mínima das suítes

Migrar as 12 suítes confirmadas na especificação:

- NovoInsumoPageTests;
- EditarInsumoPageTests;
- SituacaoInsumoPageTests;
- ListarConsultarInsumosPageTests;
- HistoricoPrecoInsumoPageTests;
- PrecoInsumoPageTests;
- ProdutoPageTests;
- EditarProdutoPageTests;
- SituacaoProdutoPageTests;
- ListarConsultarProdutosPageTests;
- FichaTecnicaPageTests;
- ItemFichaTecnicaPageTests.

Atualizar token compartilhado também em:

- AutenticacaoPagesTests;
- FluxosMultiempresaTests;
- SetupPageTests.

Se a master tiver novas suítes com a mesma duplicação, migrá-las quando a alteração for mecânica e transversal.

## Regra de fronteira

Manter locais:

- CriarProdutoAsync;
- CriarInsumoAsync;
- CriarFichaAsync;
- CriarItemAsync;
- CriarPrecoAsync;
- payloads Input.*;
- asserts e seletores específicos do UC.

Não criar base class.
Não criar builder genérico.

## Testes de autenticação

AutenticacaoPagesTests, FluxosMultiempresaTests e SetupPageTests não podem usar CriarClienteAutenticadoAsync para verificar login.

Nessas suítes, login/seleção/logout continuam explícitos.

Elas podem reutilizar setup de usuário/empresa e WebTestHtml.

## Segurança dos testes

Não:

- usar TestAuthHandler;
- desabilitar antiforgery;
- desabilitar GQF;
- fabricar cookie de autenticação;
- injetar Empresa Ativa diretamente;
- compartilhar HttpClient autenticado estaticamente.

## Validação estrutural

Executar V1–V6 da especificação.

Verificar especialmente:

1. não restam CriarClienteAutenticadoAsync duplicados nas suítes migradas;
2. regex do token está centralizada;
3. não restam ContextoEmpresaTeste privados duplicados;
4. setup UserManager + UsuarioEmpresa saiu das suítes de negócio;
5. helpers de domínio continuam locais;
6. testes de autenticação continuam explícitos.

## Testes

Não adicionar uma grande suíte para testar os próprios helpers.

Preferir a cobertura indireta pela suíte migrada.
Adicionar smoke focado apenas se necessário.

Não remover nem enfraquecer testes existentes.

## Validação técnica

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Zero warnings novos relevantes.

## Documentação pós-implementação

Atualizar docs/development/testing-strategy.md com a convenção:

- composição de WebTestContext;
- login/cookies/antiforgery reais;
- domínio específico permanece local;
- testes de autenticação não escondem o fluxo sob teste.

Atualizar somente docs/development/backlog.md se a entrega alterar estado, gate ou ordem.

## Fora do escopo

- código de produção;
- Playwright/Selenium;
- GitHub Actions;
- test authentication handler;
- framework interno de testes;
- refatorar helpers não relacionados;
- MEL009.

## Retorno obrigatório

Informar:

1. branch;
2. helpers criados;
3. suítes migradas;
4. duplicações removidas;
5. eventuais exclusões justificadas;
6. V1–V6;
7. build/test;
8. arquivos alterados;
9. URL da PR.

Commit sugerido:

~~~text
test: centraliza infraestrutura web tenant-aware
~~~
