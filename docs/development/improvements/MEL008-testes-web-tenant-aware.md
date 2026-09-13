# MEL008 — Reduzir duplicação da infraestrutura de testes Web tenant-aware

- **Tipo:** melhoria técnica de testes
- **Origem:** review do projeto após UC009/UC010
- **Prioridade:** baixa
- **Alteração de código de produção:** não
- **Alteração de schema:** não
- **Impacta:** somente Precificador.Tests.Integration

## Objetivo

Reduzir o boilerplate repetido dos testes Web de integração sem esconder o comportamento que cada cenário pretende validar.

A extração cobre somente infraestrutura transversal comprovadamente repetida:

1. criação de usuário de teste;
2. vínculo UsuarioEmpresa;
3. HttpClient com redirects/cookies controlados;
4. login real por /Conta/Login;
5. token antiforgery real;
6. contexto tenant-aware mínimo;
7. Empresa auxiliar genérica.

Produto, Insumo, Ficha, Item, Preço, payloads e asserts específicos permanecem locais.

## Evidência

Na master usada para esta especificação:

- 12 classes Web possuem CriarClienteAutenticadoAsync local;
- 15 classes possuem extração própria de token antiforgery;
- 11 classes repetem ContextoEmpresaTeste;
- várias repetem UserManager + UsuarioEmpresa + SaveChanges apenas para setup.

A duplicação já é suficiente para implementar a melhoria; não é necessário esperar UC011/UC012.

## Princípio

Usar composição, não herança.

Não criar:

- classe base de PageTests;
- framework interno;
- builders genéricos;
- DSL de formulário;
- autenticação fake;
- cookie manual;
- TestAuthHandler;
- bypass de Session;
- bypass de antiforgery;
- mocks para substituir o pipeline Web.

O helper deve continuar exercitando Identity, cookies, Session, Empresa Ativa, antiforgery e GQF reais.

## Estrutura proposta

Adicionar ao projeto de integração:

~~~text
tests/Precificador.Tests.Integration/Web/WebTestContext.cs
tests/Precificador.Tests.Integration/Web/WebTestHtml.cs
tests/Precificador.Tests.Integration/Web/ContextoEmpresaTeste.cs
~~~

Nomes equivalentes claros são aceitáveis.

## WebTestContext

Recebe CustomWebApplicationFactory e centraliza somente setup transversal.

### Criar cliente

Expor método equivalente a:

~~~csharp
HttpClient CriarCliente(bool manterCookies = true)
~~~

Padrão dos testes de negócio:

~~~text
AllowAutoRedirect = false
HandleCookies = true
~~~

Redirects continuam visíveis aos testes.

### Criar usuário

Expor método equivalente a:

~~~csharp
Task<UsuarioTeste> CriarUsuarioAsync(params int[] empresaIds)
~~~

UsuarioTeste pode ser um record simples com Id, Email e Senha.

Regras:

- email único;
- senha fixa de teste já usada no projeto;
- usuário criado via UserManager real;
- vínculos UsuarioEmpresa ativos para os ids informados;
- um SaveChanges para vínculos;
- não definir Empresa Ativa diretamente na Session.

Esse método prepara dados e não autentica.

### Login real

Expor método equivalente a:

~~~csharp
Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string senha)
~~~

Fluxo obrigatório:

1. GET /Conta/Login;
2. obter token antiforgery real;
3. POST /Conta/Login;
4. retornar a resposta sem seguir redirect.

O helper não converte qualquer redirect em sucesso silencioso.

### Cliente autenticado com Empresa Ativa

Expor conveniência:

~~~csharp
Task<HttpClient> CriarClienteAutenticadoAsync(int empresaId = 1)
~~~

Fluxo:

1. criar usuário com exatamente um vínculo;
2. criar HttpClient;
3. executar LoginAsync;
4. validar setup esperado;
5. devolver o mesmo cliente com cookies preservados.

A Empresa Ativa deve surgir pelo fluxo real da aplicação.

### Autenticado sem Empresa Ativa

Não criar helper opaco que esconda a seleção de Empresa.

Quando o cenário exige usuário com múltiplos vínculos:

~~~text
usuario = CriarUsuarioAsync(empresaA, empresaB)
client = CriarCliente()
response = LoginAsync(client, usuario.Email, usuario.Senha)
~~~

O teste decide se deve validar redirect para /Empresas/Selecionar.

## Empresa auxiliar

WebTestContext pode expor:

~~~csharp
Task<int> CriarEmpresaAsync(string? nome = null, string? timeZoneId = null)
~~~

Regras:

- nome único quando omitido;
- timezone padrão quando omitido;
- DbContext real;
- retorna Id.

Não criar helpers genéricos de Produto/Insumo/Ficha/Item/Preço.

## WebTestHtml

Centralizar somente operações HTML transversais.

### Antiforgery

Expor:

~~~csharp
string ExtrairTokenAntiforgery(string html)
~~~

Regras:

- usar a marcação real;
- HtmlDecode;
- falhar claramente se não houver token;
- nunca retornar string vazia silenciosamente.

Pode também expor:

~~~csharp
Task<string> ObterTokenAntiforgeryAsync(HttpClient client, string caminho)
~~~

Fluxo: GET, exigir sucesso, ler HTML e extrair token.

### HTML genérico

É permitido centralizar LerHtmlDecodificadoAsync se houver redução real.

Não centralizar seletores específicos como ValorDoInput, ObterTag ou parsing de campos do domínio.

## ContextoEmpresaTeste

Extrair a implementação repetida de IEmpresaContext para uma classe única:

~~~csharp
internal sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
~~~

Deve expor apenas EmpresaId, EmpresaIdOuSentinela e TimeZoneId padrão.

## Fronteira de responsabilidade

### Compartilhar

- usuário Identity;
- UsuarioEmpresa;
- HttpClient/cookies;
- login real;
- antiforgery;
- Empresa auxiliar;
- ContextoEmpresaTeste;
- HTML genérico, se adotado.

### Manter local

- CriarProdutoAsync;
- CriarInsumoAsync;
- CriarFichaAsync;
- CriarItemAsync;
- CriarPrecoAsync;
- contagens/asserts de entidades;
- payload de formulários;
- campos Input.*;
- navegação específica do UC.

## Testes de autenticação — exceção

AutenticacaoPagesTests, FluxosMultiempresaTests e SetupPageTests não podem usar uma abstração de alto nível que esconda o comportamento sob teste.

Eles podem reutilizar WebTestHtml, CriarUsuarioAsync, CriarEmpresaAsync e CriarCliente.

Quando login, seleção, logout ou mudança de Session forem o objeto do teste, esses atos permanecem explícitos.

Não usar CriarClienteAutenticadoAsync para provar que o login funciona.

## Antiforgery

Fluxos válidos podem usar o helper de token.

Testes de ausência de token continuam fazendo POST sem __RequestVerificationToken.

Não desabilitar antiforgery em configuração de testes.

## Isolamento

- cada CriarClienteAutenticadoAsync cria usuário único;
- não compartilhar cliente autenticado entre classes;
- não usar estado estático para usuário/Empresa Ativa;
- CustomWebApplicationFactory continua controlando SQLite in-memory.

## Migração das suítes

Migrar no mínimo as suítes com duplicação confirmada:

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

Atualizar também o token helper em AutenticacaoPagesTests, FluxosMultiempresaTests e SetupPageTests, sem esconder o fluxo que essas suítes validam.

Se novas classes equivalentes existirem na master da implementação, incluí-las quando a duplicação for a mesma.

## Remoções esperadas

Das classes migradas, remover versões locais equivalentes de:

~~~text
CriarClienteAutenticadoAsync
Token / ExtrairToken
ContextoEmpresaTeste
~~~

Remover também blocos UserManager + UsuarioEmpresa usados apenas para setup transversal.

CriarEmpresaAsync local pode permanecer apenas quando tiver semântica específica do cenário.

## Não otimizar por linhas

O objetivo é legibilidade e manutenção, não redução máxima de LOC.

É melhor manter um helper local pequeno e expressivo do que criar API genérica demais.

## Critérios de aceitação

- CA01: testes de negócio usam helper comum para usuário, vínculo, cliente e login;
- CA02: login continua passando por /Conta/Login;
- CA03: Empresa Ativa continua sendo definida pelo fluxo real;
- CA04: antiforgery continua vindo do HTML real;
- CA05: testes sem token continuam rejeitados;
- CA06: existe uma única implementação de ContextoEmpresaTeste;
- CA07: helpers de domínio permanecem locais;
- CA08: testes de autenticação mantêm login/seleção/logout explícitos;
- CA09: não existe classe base Web;
- CA10: nenhum arquivo src/ é alterado;
- CA11: Identity, Session, GQF e antiforgery não são burlados;
- CA12: todos os testes existentes permanecem semanticamente equivalentes e verdes.

## Validação estrutural

### V1

Busca por private async Task<HttpClient> CriarClienteAutenticadoAsync não deve encontrar duplicações nas suítes migradas.

### V2

A regex de __RequestVerificationToken fica centralizada em WebTestHtml, salvo exceção tecnicamente diferente e justificada.

### V3

Busca por private sealed class ContextoEmpresaTeste não encontra cópias nas suítes migradas.

### V4

Blocos UserManager + UsuarioEmpresa usados apenas para setup comum saem das suítes de negócio.

### V5

CriarProdutoAsync, CriarInsumoAsync, CriarFichaAsync e CriarItemAsync continuam locais.

### V6

AutenticacaoPagesTests, FluxosMultiempresaTests e SetupPageTests ainda mostram explicitamente os atos que testam.

## Testes novos

Não criar uma grande suíte para testar helpers de teste.

A suíte migrada deve provar o helper por uso real.

Se necessário, adicionar no máximo smoke tests focados de infraestrutura.

## Build e testes

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Não remover ou enfraquecer testes para facilitar a refatoração.

## Documentação

Atualizar docs/development/testing-strategy.md com a convenção de infraestrutura Web compartilhada.

Atualizar o estado conforme a governança vigente: melhorias.md ou, se MEL007 já estiver implementada, somente backlog.md.

## Relação com MEL007

MEL008 é independente funcionalmente da MEL007.

Se MEL007 estiver implementada quando MEL008 for executada, usar backlog.md e não reintroduzir Status em documentos individuais.

## Fora do escopo

- Playwright;
- Selenium;
- browser real;
- fixture global com usuário autenticado;
- TestAuthHandler;
- desabilitar antiforgery;
- desabilitar GQF;
- builders genéricos;
- classe base de PageTests;
- extrair todos os helpers locais;
- alterar testes unitários;
- alterar código de produção;
- MEL007;
- MEL009.

## Definition of Done específica

MEL008 está concluída quando:

- WebTestContext cobre o setup transversal definido;
- WebTestHtml centraliza antiforgery;
- ContextoEmpresaTeste possui implementação única;
- as 12 suítes identificadas foram migradas ou justificadamente excluídas;
- testes de autenticação não escondem comportamento sob teste;
- helpers de domínio permanecem locais;
- V1 a V6 atendidos;
- nenhum arquivo src/ foi alterado;
- suíte completa verde;
- testing-strategy documenta a convenção.

## Branch sugerida

~~~text
refactor/mel008-testes-web-tenant-aware
~~~

## Commit sugerido

~~~text
test: centraliza infraestrutura web tenant-aware
~~~
