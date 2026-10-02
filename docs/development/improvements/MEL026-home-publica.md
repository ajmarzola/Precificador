# MEL026 — Enriquecer Home pública com apresentação do Precificador

- **Origem:** acabamento visual e apresentação institucional após MEL025.
- **Classificação:** UX / apresentação pública / portfólio.
- **Estado:** Pronto.
- **Dependências:** MEL012 e MEL025 concluídas.
- **Superfície principal:** `GET /`.
- **Alteração de regra de negócio:** não.
- **Alteração de domínio/schema:** não.
- **Migration:** não.
- **Alteração de autorização:** não.

## Objetivo

Transformar a Home pública do Precificador em uma landing page clara, responsiva e coerente com o produto real, sem alterar o fluxo de autenticação nem a solicitação de acesso da UC038.

A Home deve explicar, em poucos segundos:

1. o que é o Precificador;
2. qual problema ele resolve;
3. quais capacidades principais já oferece;
4. como o fluxo de precificação funciona;
5. como entrar ou solicitar acesso.

A MEL026 deve melhorar a apresentação pública sem transformar a Home em documentação técnica, catálogo de funcionalidades ou site comercial complexo.

## Estado atual

A Home pública atualmente contém:

~~~text
Precificador
Aplicação local para formação e acompanhamento de preços.
Entrar

Solicitar acesso
<formulário UC038>
~~~

O fluxo funcional está correto, porém a apresentação:

- ainda parece scaffold/protótipo;
- não comunica a proposta de valor;
- não mostra o que o sistema efetivamente resolve;
- não evidencia o encadeamento Insumos -> Ficha Técnica -> Custos -> Preços -> Margens;
- não possui hierarquia visual suficiente para uma apresentação de portfólio;
- mantém estilo inline para limitar a largura do formulário.

## Princípio

A Home pública deve refletir o produto existente, não prometer capacidades futuras.

Todo texto de apresentação deve ser derivado de funcionalidades já entregues.

Não mencionar como se já existissem:

- referências de mercado da UC035;
- Continuous Deployment da MEL027;
- identidade visual tenant-aware da UC037;
- integrações externas;
- vendas, estoque ou ERP;
- IA;
- automação de decisão de preço.

## Público-alvo apresentado

A comunicação pode usar formulação equivalente a:

~~~text
Negócios artesanais que precisam formar preços com clareza,
acompanhar custos e entender quando a margem deixou de atender à meta.
~~~

Não restringir a Home a panificação.

Não citar uma Empresa específica como cliente oficial.

Não apresentar o sistema como SaaS comercial aberto ao público.

## Estrutura da Home

A Home anônima deve possuir, nesta ordem conceitual:

~~~text
1. Hero
2. Proposta de valor / problema resolvido
3. Capacidades principais
4. Como funciona
5. Acesso controlado / formulário de solicitação
6. Rodapé normal
~~~

A implementação pode combinar visualmente as seções 2 e 3 desde que a mensagem permaneça clara.

## 1. Hero

O primeiro viewport deve identificar imediatamente o produto.

Conteúdo mínimo:

- marca/nome `Precificador`;
- headline orientada ao benefício;
- texto curto explicando o propósito;
- CTA primário `Entrar`;
- CTA secundário `Solicitar acesso`.

Texto sugerido:

~~~text
Precifique com clareza. Acompanhe sua margem.

Centralize insumos, fichas técnicas, custos e históricos para entender
quanto seu produto custa hoje e quando o preço precisa ser revisto.
~~~

Os textos podem receber pequenos ajustes editoriais durante a implementação, mas não devem alterar a promessa funcional.

### CTA Entrar

Destino:

~~~text
/Conta/Login
~~~

Preserva o comportamento existente:

- primeiro uso pode seguir para Setup;
- usuário normal vê Login;
- nenhuma lógica de bootstrap é copiada para a Home.

### CTA Solicitar acesso

Não criar nova página.

Usar âncora para a seção do formulário:

~~~text
#solicitar-acesso
~~~

O CTA deve fazer scroll/navegação local para o formulário da UC038.

## 2. Proposta de valor

Apresentar o problema que o Precificador resolve em linguagem simples.

Mensagem conceitual:

~~~text
Quando o preço de um insumo muda, várias fichas e produtos podem ser afetados.
O Precificador mantém essas relações em uma fonte única e recalcula a visão
atual de custo, preço sugerido e margem sem depender de planilhas espalhadas.
~~~

Não afirmar que o sistema altera preços automaticamente.

Decisão de Preço de Prateleira continua pertencendo ao usuário.

## 3. Capacidades principais

Exibir um conjunto curto de cards/blocos.

Mínimo de cinco e máximo de seis capacidades.

### Capacidade A — Insumos e histórico de preços

Comunicar:

- cadastro de Insumos;
- preço vigente;
- histórico de preços;
- atualização centralizada por Empresa.

### Capacidade B — Ficha Técnica e composição de custos

Comunicar:

- rendimento;
- Insumos;
- perdas;
- mão de obra;
- energia/equipamentos;
- desgaste quando aplicável.

Não detalhar fórmulas.

### Capacidade C — Formação de preço

Comunicar:

- custo atual;
- preço teórico/sugerido;
- margem-alvo;
- preço de prateleira definido pelo usuário.

Não dizer que o sistema “define o preço ideal”.

### Capacidade D — Margem e alertas operacionais

Comunicar:

- comparação entre margem atual e alvo;
- identificação de Produtos abaixo da margem;
- identificação de precificação incompleta.

### Capacidade E — Histórico e explicabilidade

Comunicar:

- histórico de preços;
- snapshots de precificação;
- capacidade de explicar decisões passadas sem reinterpretar registros históricos.

### Capacidade F — Organização do catálogo

Pode comunicar:

- Categorias;
- Coleções;
- Produtos em Coleções;
- múltiplas Empresas/usuários com isolamento de acesso.

Se seis cards deixarem a página excessiva, é permitido combinar Catálogo + Multiempresa no mesmo bloco.

## Linguagem dos cards

Cada card deve usar:

- título curto;
- uma ou duas frases;
- no máximo três linhas conceituais em desktop.

Não transformar card em documentação.

Não expor nomes internos como:

~~~text
UC011
RN024
ProdutoColecao
EmpresaContext
SystemAdmin
~~~

## 4. Como funciona

Exibir fluxo simples em três passos.

### Passo 1 — Centralize os dados

~~~text
Cadastre Insumos, preços e a composição real dos Produtos.
~~~

### Passo 2 — Calcule com os dados vigentes

~~~text
O Precificador consolida custos e aplica as configurações comerciais da Empresa.
~~~

### Passo 3 — Decida com contexto

~~~text
Compare preço, margem e histórico para saber quais Produtos precisam de revisão.
~~~

Não dizer:

~~~text
O sistema escolhe o preço por você.
~~~

A decisão comercial permanece humana.

## 5. Solicitação de acesso

Preservar integralmente o fluxo da UC038.

A seção deve continuar contendo:

~~~text
id = solicitar-acesso
~~~

e o formulário com:

- Nome da Empresa;
- Nome do responsável;
- E-mail;
- Observação;
- antiforgery;
- validações atuais;
- PRG após sucesso;
- mensagem de sucesso neutra.

## Contextualização do formulário

Melhorar somente o texto ao redor do formulário.

Título:

~~~text
Solicitar acesso
~~~

Texto sugerido:

~~~text
O acesso é liberado de forma controlada.
Envie os dados da Empresa e a solicitação será analisada.
O envio não cria uma conta nem concede acesso imediato.
~~~

Não prometer prazo.

Não afirmar que o acesso é pago/gratuito.

Não solicitar novos campos.

## Formulário não muda de contrato

Não alterar:

- Input Model;
- entidade `SolicitacaoAcessoEmpresa`;
- índices;
- regras de idempotência;
- persistência;
- mensagens de validação de domínio;
- handler;
- autorização;
- lógica de SystemAdmin.

A MEL026 pode mover o markup do formulário dentro da nova composição visual, mas não reimplementar seu fluxo.

## Usuário autenticado

Preservar o comportamento atual do `IndexModel.OnGet`.

### SystemAdmin

~~~text
GET /
=> /Admin
~~~

### Usuário com Empresa Ativa

~~~text
GET /
=> /Dashboard
~~~

### Usuário autenticado sem Empresa Ativa

Preservar o comportamento vigente da aplicação; MEL026 não redefine seleção de Empresa.

O conteúdo institucional não deve ser renderizado como nova Home operacional.

## POST autenticado

Preservar UC038:

~~~text
POST /?handler=SolicitarAcesso
com usuário autenticado
=> destino canônico do usuário
=> não cria solicitação
~~~

## Navegação pública

Preservar MEL012:

~~~text
Precificador
Início
Entrar
~~~

Não reintroduzir links operacionais para anônimos.

É permitido adicionar no estado anônimo links de âncora da própria Home, por exemplo:

~~~text
Recursos
Como funciona
~~~

somente se:

- não poluir a navbar;
- funcionar também quando clicado a partir da Home;
- não substituir `Entrar`;
- não criar dependência de JavaScript.

Essa inclusão é opcional, não requisito.

## Layout geral

Usar o `_Layout.cshtml` existente.

Não criar um segundo layout público apenas para a Home.

MEL025 continua usando seu layout específico de erro.

## Responsividade

A Home deve funcionar em:

- desktop;
- tablet;
- celular.

Requisitos mínimos:

- hero empilha CTAs em telas estreitas quando necessário;
- cards não causam overflow horizontal;
- formulário usa largura disponível;
- textos não dependem de largura fixa;
- nenhum conteúdo exige rolagem horizontal;
- heading/CTA permanecem legíveis em 320 px.

Usar grid/utilitários Bootstrap e CSS local.

## CSS

É permitido adicionar classes específicas em:

~~~text
wwwroot/css/site.css
~~~

Preferir nomes com escopo, por exemplo:

~~~text
.home-hero
.home-section
.home-feature-card
.home-step
.home-access
~~~

Não adicionar framework CSS.

Não usar `style="..."` inline para o novo layout.

Remover o atual:

~~~text
style="max-width: 640px"
~~~

do formulário e substituir por classe CSS.

## Identidade visual

A MEL026 usa identidade neutra do Precificador.

Não antecipar a UC037.

Portanto:

- não carregar logo por Empresa;
- não aplicar cor configurável por Empresa;
- não consultar configuração visual tenant;
- não adicionar schema de branding.

É aceitável usar a paleta padrão Bootstrap/Precificador atual.

## Imagens e ícones

Não há necessidade de imagem externa para concluir a MEL026.

Preferência:

- tipografia;
- espaçamento;
- cards;
- formas CSS leves;
- Bootstrap local.

Não adicionar:

- CDN;
- Google Fonts;
- biblioteca de ícones;
- imagem stock;
- chamadas externas.

Se usar elementos decorativos, devem ser puramente CSS/HTML e não essenciais ao entendimento.

## JavaScript

Não adicionar biblioteca JavaScript.

O CTA de solicitação usa âncora nativa.

Bootstrap existente pode continuar carregado pelo layout.

Não criar animações obrigatórias.

## Acessibilidade

A Home deve possuir hierarquia semântica consistente:

~~~text
1 x h1
h2 por seção
h3 para cards/passos quando aplicável
~~~

Requisitos:

- usar `section` com `aria-labelledby` quando útil;
- links e botões com texto explícito;
- contraste compatível com Bootstrap;
- foco visível preservado;
- formulário mantém labels;
- mensagens de validação permanecem acessíveis;
- não depender apenas de cor para transmitir significado.

## Conteúdo e HTML seguro

Todos os textos de apresentação são estáticos.

Conteúdo submetido no formulário continua sendo tratado como texto.

Não usar `Html.Raw` para dados do usuário.

Não inserir HTML de observação/Empresa no conteúdo institucional.

## SEO básico

A Home pública deve possuir:

~~~text
<title> claro
meta description
~~~

Título sugerido:

~~~text
Precificador — Formação e acompanhamento de preços
~~~

Descrição sugerida:

~~~text
Centralize Insumos, fichas técnicas, custos, preços e margens para acompanhar a precificação de produtos artesanais.
~~~

### Implementação da meta description

É aceitável evoluir `_Layout.cshtml` com:

~~~text
ViewData["Description"]
~~~

de forma opcional.

Somente renderizar:

~~~html
<meta name="description" ...>
~~~

quando houver valor.

Não preencher descrição genérica automaticamente em todas as páginas.

## Performance

A MEL026 deve permanecer essencialmente server-rendered.

Não adicionar:

- requests externos;
- scripts de analytics;
- imagens pesadas;
- fontes externas;
- chamadas ao banco apenas para compor conteúdo institucional.

GET anônimo da Home já necessita do PageModel para o fluxo existente, mas a apresentação institucional não deve adicionar consultas.

## Segurança

Não enfraquecer:

- antiforgery;
- autorização;
- isolamento multiempresa;
- fluxo de Login;
- tratamento de erros da MEL025.

Não exibir:

- lista de Empresas cadastradas;
- quantidade de usuários;
- dados operacionais;
- nomes de clientes;
- métricas internas;
- detalhes administrativos.

## Testes de integração — conteúdo público

GET `/` anônimo deve validar no mínimo:

- HTTP 200;
- h1 Precificador;
- headline/proposta de valor;
- link Entrar;
- CTA Solicitar acesso;
- seção de capacidades;
- seção Como funciona;
- formulário UC038;
- campos originais preservados;
- ausência de links operacionais.

Não usar asserts de HTML excessivamente frágeis por texto genérico curto.

## Testes — fluxo UC038

Preservar e/ou ampliar cobertura existente:

- form válido;
- form inválido;
- PRG;
- sucesso;
- duplicidade idempotente;
- antiforgery;
- XSS/encoding;
- usuário autenticado não cria solicitação.

A MEL026 não deve remover cobertura da UC038 para simplificar testes visuais.

## Testes — usuário autenticado

Validar regressão:

- SystemAdmin em `/` -> `/Admin`;
- usuário tenant com Empresa Ativa -> `/Dashboard`;
- conteúdo público não substitui Dashboard;
- navbar autenticada permanece operacional.

## Testes — responsividade estrutural

Não é necessário teste visual pixel-perfect.

Testes podem comprovar estruturalmente:

- classes Bootstrap responsivas;
- ausência do inline `max-width` anterior;
- seção de cards usa grid responsivo;
- CTAs possuem markup semântico.

Não adicionar snapshot de HTML completo.

## Testes — meta description

GET anônimo deve conter uma única meta description correspondente à Home.

Páginas sem `ViewData["Description"]` não devem receber description duplicada/vazia, caso o layout seja evoluído.

## Sem testes pixel-perfect

Não testar:

- cor exata;
- quantidade de pixels;
- posição absoluta;
- quebra de linha específica;
- ordem de classes CSS.

Esses detalhes podem evoluir sem quebrar o contrato funcional.

## Critérios de aceitação

- **CA01:** MEL025 está concluída antes da MEL026.
- **CA02:** GET / anônimo continua HTTP 200.
- **CA03:** Home possui um único h1.
- **CA04:** h1 identifica o Precificador.
- **CA05:** hero comunica formação/acompanhamento de preço e margem.
- **CA06:** hero possui CTA Entrar.
- **CA07:** CTA Entrar aponta para /Conta/Login.
- **CA08:** hero possui CTA Solicitar acesso.
- **CA09:** CTA Solicitar acesso aponta para #solicitar-acesso.
- **CA10:** Home explica o problema de atualização centralizada de custos.
- **CA11:** Home não promete alteração automática do Preço de Prateleira.
- **CA12:** Home apresenta Insumos/histórico.
- **CA13:** Home apresenta Ficha Técnica/custos.
- **CA14:** Home apresenta formação de preço.
- **CA15:** Home apresenta margem/necessidade de revisão.
- **CA16:** Home apresenta histórico/explicabilidade.
- **CA17:** Home apresenta organização de catálogo/multiempresa sem expor detalhes internos.
- **CA18:** Home possui seção Como funciona.
- **CA19:** Como funciona possui fluxo em três passos.
- **CA20:** o fluxo termina em apoio à decisão, não decisão automática.
- **CA21:** formulário Solicitar acesso permanece na própria Home.
- **CA22:** formulário mantém Nome da Empresa.
- **CA23:** formulário mantém Nome do responsável.
- **CA24:** formulário mantém E-mail.
- **CA25:** formulário mantém Observação opcional.
- **CA26:** nenhum campo novo é exigido.
- **CA27:** texto deixa claro que solicitação não concede acesso imediato.
- **CA28:** texto não promete prazo de análise.
- **CA29:** POST do formulário continua antiforgery.
- **CA30:** PRG da UC038 é preservado.
- **CA31:** idempotência da UC038 é preservada.
- **CA32:** usuário autenticado não cria solicitação por POST.
- **CA33:** SystemAdmin acessando / continua redirecionado a /Admin.
- **CA34:** tenant com Empresa Ativa acessando / continua redirecionado a /Dashboard.
- **CA35:** Home pública não vira Dashboard duplicado.
- **CA36:** navbar anônima continua sem Insumos/Produtos/Configurações.
- **CA37:** navbar anônima continua oferecendo Entrar.
- **CA38:** layout operacional autenticado não é redesenhado pela MEL026.
- **CA39:** Home usa Bootstrap/CSS local.
- **CA40:** nenhuma biblioteca CSS nova é adicionada.
- **CA41:** nenhuma fonte/asset externo é obrigatório.
- **CA42:** nenhum JavaScript novo é necessário para CTA principal.
- **CA43:** conteúdo funciona sem JavaScript adicional.
- **CA44:** novo layout não usa estilos inline para composição principal.
- **CA45:** cards usam estrutura responsiva.
- **CA46:** formulário é responsivo sem largura inline fixa.
- **CA47:** página não produz overflow horizontal em viewport estreito.
- **CA48:** hierarquia de headings é semântica.
- **CA49:** labels/validação do formulário permanecem acessíveis.
- **CA50:** foco visível existente não é removido.
- **CA51:** dados submetidos continuam escapados/encoded.
- **CA52:** nenhuma informação de Empresas existentes é exposta.
- **CA53:** MEL026 não introduz consulta de banco para conteúdo institucional.
- **CA54:** UC037 não é antecipada.
- **CA55:** UC035 não é antecipada.
- **CA56:** MEL027 não é antecipada.
- **CA57:** nenhuma regra de precificação é alterada.
- **CA58:** nenhuma entidade/schema/migration é alterado.
- **CA59:** Home possui title institucional coerente.
- **CA60:** Home possui meta description.
- **CA61:** meta description não é duplicada.
- **CA62:** Login/Setup continuam funcionando pela mesma cadeia existente.
- **CA63:** MEL025 permanece funcional.
- **CA64:** testes UC038 existentes permanecem verdes.
- **CA65:** Build Release fica verde.
- **CA66:** testes unitários aplicáveis ficam verdes.
- **CA67:** integração Web fica verde.

## Matriz mínima de testes

### Home anônima

- 200;
- hero;
- headline;
- Entrar;
- Solicitar acesso;
- capacidades;
- Como funciona;
- formulário;
- quatro campos;
- sem links operacionais;
- meta description.

### Solicitação de acesso

- válida;
- inválida;
- duplicada;
- antiforgery;
- PRG;
- encoding;
- sucesso.

### Autenticação

- SystemAdmin -> Admin;
- tenant com Empresa Ativa -> Dashboard;
- POST autenticado não persiste solicitação;
- usuário anônimo -> Home pública.

### Navegação

- navbar anônima coerente;
- Entrar preservado;
- sem reintrodução de Insumos/Produtos/Configurações.

### Estrutura

- um h1;
- h2 nas seções;
- grid responsivo;
- sem inline max-width do formulário;
- sem dependência externa.

### Regressão

- MEL025 404/500;
- Login;
- Setup;
- UC038;
- Dashboard;
- nenhuma mudança em DbContext/migration.

## Arquivos esperados

Lista indicativa:

~~~text
src/Precificador.Web/Pages/Index.cshtml
src/Precificador.Web/Pages/Index.cshtml.cs
  # idealmente sem mudança funcional

src/Precificador.Web/Pages/Shared/_Layout.cshtml
  # somente se necessário para meta description / navegação pública mínima

src/Precificador.Web/wwwroot/css/site.css

tests/Precificador.Tests.Integration/Web/HomePageTests.cs
tests/Precificador.Tests.Integration/Web/SolicitarAcessoPageTests.cs
tests/Precificador.Tests.Integration/Web/AutenticacaoPagesTests.cs

docs/development/improvements/MEL026-home-publica.md
docs/codex/MEL026-home-publica.md
docs/development/backlog.md
docs/architecture/architecture.md
~~~

## Fora do escopo

- UC037 — identidade visual configurável por Empresa;
- logo/upload de imagem;
- cor configurável;
- branding tenant-aware;
- UC035 — referências de mercado;
- IA;
- web scraping;
- MEL027 — CD;
- analytics;
- Google Analytics;
- cookies de tracking;
- chat;
- FAQ dinâmico;
- blog;
- preços/planos comerciais;
- depoimentos;
- logos de clientes;
- cadastro automático;
- CAPTCHA;
- alteração do fluxo UC038;
- redesign das páginas autenticadas;
- novo design system;
- dark mode;
- animações complexas;
- novas bibliotecas front-end;
- CMS;
- internacionalização.

## Definition of Done

MEL026 está concluída quando:

- Home pública comunica claramente o Precificador;
- hero e CTAs estão funcionais;
- capacidades reais são apresentadas sem promessas futuras;
- seção Como funciona descreve o fluxo em três passos;
- formulário UC038 permanece funcional e integrado ao novo layout;
- Home é responsiva e acessível;
- conteúdo usa apenas recursos locais existentes;
- usuário autenticado continua redirecionado ao destino canônico;
- navegação pública da MEL012 permanece;
- MEL025 permanece funcional;
- nenhuma regra, entidade ou migration muda;
- UC037/MEL027/UC035 não são antecipadas;
- documentação/backlog estão alinhados;
- suíte completa está verde.

## Branch de implementação

~~~text
feat/mel026-home-publica
~~~

## Commit sugerido

~~~text
feat: enriquece home publica
~~~
