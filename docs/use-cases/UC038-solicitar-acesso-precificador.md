# UC038 — Solicitar acesso ao Precificador

- **Área funcional:** Administração multiempresa e acesso controlado
- **Dependência funcional:** FT003
- **Estado:** Pronto
- **Alteração de schema:** sim
- **Persistência:** sim
- **Superfície principal:** Home pública
- **Autorização:** anônimo
- **Regras principais:** RN059, RN060 e RN061

## Objetivo

Permitir que uma Empresa solicite acesso ao Precificador por meio de um formulário exibido na **Home pública**, sem criar automaticamente Empresa, usuário, vínculo, credencial, convite ou qualquer acesso utilizável.

A submissão cria somente uma **Solicitação de Acesso** em estado `Pendente`, para análise posterior pelo `SystemAdmin` na UC039.

Fluxo conceitual:

~~~text
Visitante
   ↓
Home pública
   ↓
Solicitar acesso
   ↓
Solicitação Pendente
   ↓
[UC039]
SystemAdmin analisa
   ↓
aprova ou recusa
~~~

A UC038 representa apenas a entrada pública do fluxo.

## Relação com FT003, UC039 e UC040

A FT003 já entregou:

- `SystemAdmin` global;
- área `/Admin`;
- separação entre autoridade global e tenant;
- novo `/Setup`;
- perfis `Administrador/Operacional` em `UsuarioEmpresa`.

A UC038 **não usa** essas estruturas para conceder acesso.

A divisão de responsabilidades permanece:

~~~text
UC038
=> recebe e persiste solicitação pública

UC040
=> infraestrutura de ativação/recuperação por token e e-mail

UC039
=> SystemAdmin analisa solicitação
=> aprova/recusa
=> cria Empresa quando aprovado
=> define primeiro Administrador da Empresa

UC031
=> Administrador da Empresa gerencia usuários/vínculos posteriores
~~~

## Princípio central

Enviar o formulário **não equivale a cadastro**.

Depois de uma submissão bem-sucedida:

~~~text
Empresa NÃO criada
UsuarioAplicacao NÃO criado
UsuarioEmpresa NÃO criado
senha NÃO solicitada
token NÃO criado
e-mail NÃO enviado
Empresa Ativa NÃO definida
acesso NÃO concedido
~~~

O único efeito persistido é a Solicitação de Acesso.

## Superfície

A UC038 evolui a Home pública existente:

~~~text
GET /
POST /?handler=SolicitarAcesso
~~~

O nome exato do handler pode variar, desde que:

- o formulário permaneça visualmente na Home pública;
- a ação seja POST;
- antiforgery permaneça obrigatório;
- o fluxo use PRG após sucesso.

Não é necessário criar uma página pública separada de solicitação.

A MEL026 continuará responsável pelo enriquecimento amplo da apresentação institucional da Home. A UC038 deve alterar somente o necessário para inserir o fluxo de solicitação de acesso sem antecipar o redesign completo previsto naquela melhoria.

## Home pública

Para visitante anônimo, preservar no mínimo:

- identificação do Precificador;
- acesso ao Login;
- formulário “Solicitar acesso”.

Texto funcional deve deixar claro que:

- a solicitação será analisada;
- a submissão não concede acesso imediato;
- o contato será feito pelo e-mail informado em etapa posterior do fluxo.

Não prometer prazo de análise.

### Usuário autenticado

A Home preserva a semântica da FT003:

- `SystemAdmin` autenticado -> `/Admin`;
- usuário empresarial com Empresa Ativa -> `/Dashboard`;
- demais comportamentos autenticados existentes permanecem.

O formulário público não deve virar mecanismo de um usuário autenticado criar outra Empresa.

Se um POST do handler de solicitação chegar autenticado, redirecionar para o destino canônico do usuário em vez de persistir nova solicitação.

## Dados solicitados

Campos mínimos:

### Nome da Empresa

~~~text
obrigatório
máximo 120 caracteres após normalização
~~~

A normalização deve ser exatamente compatível com `Empresa.Nome` / `Empresa.NomeNormalizado`:

1. trim externo;
2. colapsar sequências de whitespace para um único espaço;
3. preservar a forma normalizada de exibição em `NomeEmpresa`;
4. gerar `NomeEmpresaNormalizado = NomeEmpresa.ToUpperInvariant()`.

A implementação deve evitar duas regras paralelas de normalização entre `Empresa` e `SolicitacaoAcessoEmpresa`.

Preferir extração/reuso de um normalizador compartilhado no Core.

Exemplo:

~~~text
"  Minha   Empresa  "
=> NomeEmpresa = "Minha Empresa"
=> NomeEmpresaNormalizado = "MINHA EMPRESA"
~~~

### Nome do responsável

~~~text
obrigatório
máximo 120 caracteres
~~~

Normalizar:

- trim externo;
- colapsar whitespace horizontal repetido para um único espaço.

Não transformar caixa automaticamente.

### E-mail do responsável

~~~text
obrigatório
formato de e-mail válido
máximo 256 caracteres após trim
~~~

Persistir:

~~~text
EmailResponsavel
EmailResponsavelNormalizado
~~~

Normalização mínima:

~~~text
EmailResponsavel = Trim()
EmailResponsavelNormalizado = EmailResponsavel.ToUpperInvariant()
~~~

A normalização aproxima a semântica usada pelo ASP.NET Core Identity, sem criar `UsuarioAplicacao`.

### Observação

~~~text
opcional
máximo 1000 caracteres
~~~

Regras:

- trim externo;
- vazio/whitespace -> `null`;
- preservar quebras de linha internas;
- texto é somente informativo;
- nunca interpretar HTML enviado pelo usuário.

Exemplos de uso:

- explicar brevemente o negócio;
- contextualizar quem está solicitando;
- registrar alguma observação para o futuro SystemAdmin.

## Modelo persistente

Introduzir entidade global, não tenant-owned:

~~~text
SolicitacaoAcessoEmpresa
- Id : int
- NomeEmpresa : string
- NomeEmpresaNormalizado : string
- NomeResponsavel : string
- EmailResponsavel : string
- EmailResponsavelNormalizado : string
- Observacao : string?
- DataSolicitacaoUtc : DateTimeOffset
- Situacao : SituacaoSolicitacaoAcessoEmpresa
~~~

Enum:

~~~csharp
public enum SituacaoSolicitacaoAcessoEmpresa
{
    Pendente = 1,
    Aprovada = 2,
    Recusada = 3
}
~~~

A UC038 cria exclusivamente `Pendente`.

Os estados Aprovada/Recusada são definidos agora para estabilizar o modelo persistente que a UC039 consumirá; a UC038 não implementa suas transições.

### Natureza global

`SolicitacaoAcessoEmpresa`:

- não implementa `IEntidadeEmpresa`;
- não possui `EmpresaId`;
- não recebe Global Query Filter de tenant;
- não depende de `EmpresaContext`.

Isso é intencional: antes da aprovação não existe tenant.

## Data da solicitação

Usar:

~~~text
TimeProvider.GetUtcNow()
~~~

Persistir como `DateTimeOffset` em UTC.

Não usar:

- `DateTime.Now`;
- `DateTime.UtcNow`;
- `DateTimeOffset.Now`;
- `DateTimeOffset.UtcNow`.

O uso do `TimeProvider` mantém testes determinísticos e segue a estratégia técnica já adotada no projeto.

Não existe “data operacional da Empresa” aqui, pois a Empresa ainda não existe.

## Situação inicial

Toda nova Solicitação nasce:

~~~text
Situacao = Pendente
~~~

A situação não é informada pelo formulário.

O cliente não pode enviar:

- Situacao;
- DataSolicitacaoUtc;
- Id;
- qualquer futuro identificador de Empresa;
- qualquer informação de aprovação.

## Duplicidade e idempotência

A identidade de uma solicitação pendente repetida é:

~~~text
NomeEmpresaNormalizado
+
EmailResponsavelNormalizado
~~~

### Mesmo par Empresa + e-mail

Se já houver solicitação `Pendente` com o mesmo par normalizado:

- não criar segunda linha;
- não alterar a solicitação existente;
- não atualizar data;
- não concatenar/substituir Observação;
- retornar o mesmo resultado público de sucesso.

Isso torna duplo clique, refresh e reenvio acidental idempotentes.

### Solicitação já aprovada

Quando houver registro `Aprovada` para o mesmo par, uma nova submissão pública também não deve criar outro pedido.

Responder com a mesma mensagem genérica de sucesso.

Motivo: uma aprovação anterior já consumiu esse fluxo; eventual problema de acesso será tratado por recuperação/gestão, não por nova solicitação pública idêntica.

### Solicitação recusada

Uma solicitação `Recusada` não bloqueia permanentemente nova tentativa futura.

Se não houver Pendente/Aprovada para o par, é permitido criar uma nova Pendente.

A UC038 não define intervalo mínimo entre uma recusa e nova tentativa.

### Mesmo e-mail, outra Empresa

Permitido.

Exemplo:

~~~text
joao@email.com + Empresa A
joao@email.com + Empresa B
~~~

podem gerar duas solicitações pendentes distintas.

Isso é coerente com o modelo em que um usuário pode futuramente pertencer a várias Empresas.

### Mesma Empresa, outro e-mail

Permitido.

Não usar somente `NomeEmpresaNormalizado` como chave de unicidade, pois isso permitiria que uma submissão indevida “reservasse” o nome da Empresa e impedisse o responsável legítimo de solicitar acesso.

A UC039 será responsável por avaliar pedidos concorrentes/relacionados da mesma Empresa.

## Proteção de concorrência no banco

Criar índice único filtrado para a pendência:

~~~text
(NomeEmpresaNormalizado, EmailResponsavelNormalizado)
WHERE Situacao = 1
~~~

Nome sugerido:

~~~text
UX_SolicitacoesAcessoEmpresas_Pendente_Nome_Email
~~~

Também é recomendável índice para futura fila administrativa:

~~~text
(Situacao, DataSolicitacaoUtc)
~~~

O POST deve:

1. consultar Pendente/Aprovada existente;
2. se existir, seguir sucesso idempotente;
3. tentar inserir nova Pendente;
4. em corrida concorrente, tratar especificamente violação do índice único de pendência como sucesso idempotente;
5. não engolir outras `DbUpdateException`.

Não implementar lock distribuído.

## Fluxo principal

### GET /

Para visitante anônimo:

1. renderizar Home;
2. renderizar Login atual;
3. renderizar formulário de solicitação;
4. formulário vazio;
5. nenhum acesso é criado.

GET não persiste.

### POST válido

1. confirmar que o request é anônimo;
2. validar antiforgery;
3. validar ModelState;
4. normalizar dados;
5. validar invariantes;
6. procurar solicitação Pendente/Aprovada equivalente;
7. se não existir, criar `SolicitacaoAcessoEmpresa` Pendente;
8. persistir usando `TimeProvider.GetUtcNow()`;
9. redirecionar para GET da Home;
10. exibir mensagem genérica de recebimento.

Usar PRG.

Mensagem sugerida:

~~~text
Recebemos sua solicitação de acesso. Ela será analisada e, se necessário, entraremos em contato pelo e-mail informado.
~~~

O texto pode variar sem alterar a semântica.

### POST duplicado

Mesmo comportamento externo do POST válido:

~~~text
redirect /
+ mesma mensagem pública
~~~

Não revelar:

- que a Empresa já solicitou;
- que o e-mail já existe;
- se a solicitação está Pendente/Aprovada;
- se existe `UsuarioAplicacao` correspondente.

## Validação inválida

Com dados inválidos:

- HTTP 200;
- reexibir a Home/formulário;
- manter valores válidos já digitados;
- exibir mensagens próximas aos campos;
- não criar solicitação;
- não executar PRG de sucesso.

Exemplos:

- Nome da Empresa vazio;
- Responsável vazio;
- e-mail inválido;
- comprimento excedido;
- Observação > 1000.

Não depender somente de validação client-side.

## Mensagem de privacidade

Próximo ao formulário, incluir texto informativo curto indicando que Nome e E-mail serão usados para analisar e responder à solicitação de acesso.

Exemplo sem caráter jurídico prescritivo:

~~~text
Usaremos os dados informados para analisar e responder à sua solicitação de acesso.
~~~

Não exigir checkbox de “aceite” nesta UC.

A UC038 não define política jurídica/LGPD completa nem página de privacidade.

## Segurança

### Antiforgery

POST exige antiforgery.

POST sem token válido deve ser rejeitado.

### Enumeração

A resposta pública para:

- nova solicitação;
- solicitação Pendente repetida;
- solicitação já Aprovada;

deve ser semanticamente a mesma.

Não expor existência de:

- Empresa;
- usuário;
- vínculo;
- pedido anterior;
- decisão administrativa.

### Credenciais

O formulário não recebe:

- senha;
- confirmação de senha;
- token;
- chave de Setup;
- código de convite.

### Dados pessoais

Não registrar explicitamente em logs:

- corpo completo da Observação;
- e-mail completo;
- Nome do responsável;

como parte de logging funcional da UC.

Não persistir por padrão:

- IP;
- User-Agent;
- fingerprint;
- geolocalização.

### HTML

Todo conteúdo fornecido pelo usuário deve ser renderizado posteriormente com encoding padrão do Razor.

Não usar `Html.Raw` para Observação ou nomes.

## Controle de abuso

A UC038 exige como proteção básica:

- antiforgery;
- validação server-side;
- limites de comprimento;
- idempotência de solicitação pendente;
- constraint de banco para corrida de duplicidade.

Ficam fora da UC038:

- CAPTCHA;
- prova de trabalho;
- bloqueio por reputação;
- throttling persistente;
- blacklist de domínio/e-mail;
- confirmação de e-mail.

Rate limiting poderá ser introduzido posteriormente se houver evidência operacional de abuso, sem alterar a regra de negócio da solicitação.

## E-mail

A UC038 **não envia e-mail**.

Nenhuma abstração SMTP/provider é necessária aqui.

E-mails de ativação/recuperação pertencem à UC040.

A eventual notificação de recebimento da solicitação pode ser avaliada depois, mas não bloqueia o MVP desta UC.

## Administração

A UC038 não cria interface para SystemAdmin consultar solicitações.

A tabela poderá possuir registros pendentes ainda invisíveis pela aplicação administrativa até a UC039.

Isso é esperado pela ordem do backlog.

Não adicionar listagem/contador em `/Admin` nesta UC.

## Migração

Criar nova migration SQL Server para:

- tabela `SolicitacoesAcessoEmpresas`;
- constraints de tamanho/nullability;
- check constraint de Situacao:
  ~~~text
  Situacao IN (1, 2, 3)
  ~~~
- índice único filtrado de Pendente por Empresa normalizada + e-mail normalizado;
- índice por Situação + DataSolicitacaoUtc, se adotado conforme especificação.

Não editar migrations históricas.

Atualizar `PrecificadorDbContextModelSnapshot`.

## Relação com Empresa existente

UC038 não consulta `Empresas` para decidir se uma solicitação pode ser criada.

Motivos:

- pode haver Empresa técnica legada;
- pode haver nomes iguais em cenários que exigem análise administrativa;
- a aprovação e a resolução de conflito pertencem à UC039;
- resposta pública não deve funcionar como consulta de existência de tenant.

A UC039 fará a validação administrativa necessária antes de criar uma Empresa real.

## Relação com UsuarioAplicacao existente

UC038 também não cria nem altera `UsuarioAplicacao`.

Se o e-mail informado já estiver no Identity:

- não alterar senha;
- não alterar role;
- não criar vínculo;
- não informar isso ao visitante.

A UC039/UC040 decidirão como reutilizar a identidade existente quando uma solicitação for aprovada.

## Regras de negócio

### RN059 — Solicitação pública não concede acesso

Solicitar acesso cria somente `SolicitacaoAcessoEmpresa`.

É proibido à UC038 criar ou modificar:

- Empresa;
- UsuarioAplicacao;
- UsuarioEmpresa;
- role;
- senha;
- convite/token;
- Empresa Ativa.

### RN060 — Solicitação nasce Pendente

Toda solicitação nova nasce `Pendente`.

Aprovar ou recusar é decisão exclusiva da administração global e pertence à UC039.

### RN061 — Reenvio idempotente da solicitação

Para o mesmo:

~~~text
NomeEmpresaNormalizado + EmailResponsavelNormalizado
~~~

uma solicitação Pendente existente impede nova linha Pendente.

Solicitação Aprovada também impede novo pedido idêntico.

Solicitação Recusada permite nova tentativa futura.

A resposta pública é neutra e não revela o estado anterior.

## Critérios de aceitação

- **CA01:** visitante anônimo visualiza formulário de Solicitar acesso na Home pública.
- **CA02:** UC038 não cria página pública separada obrigatória.
- **CA03:** Home autenticada preserva os redirecionamentos definidos pela FT003.
- **CA04:** POST autenticado não persiste solicitação e redireciona para destino canônico.
- **CA05:** formulário exige Nome da Empresa.
- **CA06:** Nome da Empresa suporta no máximo 120 caracteres após normalização.
- **CA07:** Nome da Empresa usa a mesma semântica de normalização de `Empresa`.
- **CA08:** formulário exige Nome do responsável.
- **CA09:** Nome do responsável suporta no máximo 120 caracteres.
- **CA10:** formulário exige e-mail válido.
- **CA11:** e-mail suporta no máximo 256 caracteres após trim.
- **CA12:** formulário oferece Observação opcional de até 1000 caracteres.
- **CA13:** Observação vazia é persistida como null.
- **CA14:** submissão válida cria exatamente uma `SolicitacaoAcessoEmpresa`.
- **CA15:** nova solicitação nasce Pendente.
- **CA16:** DataSolicitacaoUtc usa `TimeProvider.GetUtcNow()`.
- **CA17:** entidade não possui EmpresaId.
- **CA18:** entidade não é tenant-owned nem recebe Global Query Filter.
- **CA19:** UC038 não cria Empresa.
- **CA20:** UC038 não cria UsuarioAplicacao.
- **CA21:** UC038 não cria UsuarioEmpresa.
- **CA22:** UC038 não cria role, senha, token ou convite.
- **CA23:** UC038 não envia e-mail.
- **CA24:** POST válido usa PRG para a Home.
- **CA25:** sucesso apresenta mensagem informando recebimento/análise sem prometer prazo.
- **CA26:** mesma Empresa normalizada + mesmo e-mail normalizado + Pendente não cria duplicata.
- **CA27:** duplicata Pendente recebe a mesma resposta pública de sucesso.
- **CA28:** solicitação Aprovada equivalente não cria nova linha.
- **CA29:** solicitação Recusada equivalente permite nova Pendente.
- **CA30:** mesmo e-mail pode solicitar Empresas diferentes.
- **CA31:** mesma Empresa pode receber solicitações de e-mails diferentes.
- **CA32:** banco possui proteção concorrente contra duas Pendentes do mesmo par normalizado.
- **CA33:** corrida no índice de duplicidade é tratada como idempotência, sem engolir outros erros de persistência.
- **CA34:** dados inválidos não persistem.
- **CA35:** POST sem antiforgery é rejeitado.
- **CA36:** resposta pública não revela existência de Empresa, usuário, vínculo ou pedido anterior.
- **CA37:** formulário não possui campo de senha.
- **CA38:** formulário não possui CNPJ obrigatório, telefone obrigatório ou outros dados não aprovados.
- **CA39:** mensagem informativa de uso dos dados é exibida próxima ao formulário.
- **CA40:** UC038 não persiste IP/User-Agent/fingerprint.
- **CA41:** GET da Home não persiste.
- **CA42:** UC038 não adiciona consulta/listagem de solicitações em `/Admin`.
- **CA43:** migration funciona em banco SQL Server vazio.
- **CA44:** migration funciona sobre banco atual com FT003 concluída sem perda de dados.
- **CA45:** migrations históricas não são editadas.
- **CA46:** build Release fica verde.
- **CA47:** unitários aplicáveis ficam verdes.
- **CA48:** integração SQL Server/Web fica verde.

## Matriz mínima de testes

### Domínio/normalização

- Nome da Empresa remove espaços externos;
- Nome da Empresa colapsa whitespace;
- normalização da Solicitação equivale à normalização de `Empresa`;
- Nome da Empresa vazio -> inválido;
- Nome da Empresa > 120 -> inválido;
- Responsável vazio -> inválido;
- Responsável normaliza whitespace;
- Responsável > 120 -> inválido;
- e-mail recebe trim;
- e-mail normalizado é determinístico/case-insensitive;
- Observação whitespace -> null;
- Observação interna com quebras de linha é preservada;
- nova entidade nasce Pendente.

### Persistência/migration

- banco vazio aplica migration;
- upgrade desde FT003 aplica migration sem perda;
- `Situacao` é obrigatória;
- check constraint rejeita valor fora de 1,2,3;
- índice filtrado impede duas Pendentes do mesmo par;
- mesmo e-mail + Empresa diferente é permitido;
- mesma Empresa + e-mail diferente é permitido;
- registro Recusado + nova Pendente do mesmo par é permitido;
- nenhuma Empresa/Identity/vínculo é criado pela migration.

### Home pública

- GET anônimo -> 200;
- exibe Login;
- exibe formulário Solicitar acesso;
- exibe campos aprovados;
- não exibe senha;
- exibe texto informativo sobre uso dos dados;
- GET não cria solicitação.

### POST válido

- cria uma Pendente;
- persiste normalizações;
- persiste DataSolicitacaoUtc determinística via TimeProvider de teste;
- Observação opcional funciona;
- não altera Empresas;
- não altera AspNetUsers;
- não altera AspNetUserRoles;
- não altera UsuariosEmpresas;
- redirect PRG para Home;
- GET seguinte exibe mensagem de sucesso.

### Validações

- Empresa vazia;
- Responsável vazio;
- e-mail vazio;
- e-mail inválido;
- limites máximos;
- Observação > 1000;
- sem antiforgery;
- todos sem persistência.

### Idempotência

- duas submissões sequenciais equivalentes -> uma linha;
- diferenças apenas de caixa/espaço em Empresa -> uma linha;
- diferenças apenas de caixa no e-mail -> uma linha;
- mensagem externa equivalente para nova e duplicada;
- concorrência de duas submissões equivalentes -> uma Pendente;
- erro de banco não relacionado à unicidade não deve ser convertido silenciosamente em sucesso.

### Estados futuros simulados

É aceitável preparar dados diretamente no teste para representar estados que a UC039 ainda não possui UI:

- Pendente equivalente -> não duplica;
- Aprovada equivalente -> não duplica;
- Recusada equivalente -> nova Pendente permitida.

### Multiempresa / identidade

- mesmo e-mail solicita A e B -> duas Pendentes;
- Empresa A com e-mail 1 e e-mail 2 -> duas Pendentes;
- e-mail já existente em `AspNetUsers` não é alterado nem exposto;
- usuário autenticado não usa este POST para criar pedido de Empresa.

## Arquivos esperados

Lista indicativa, não prescritiva:

~~~text
src/Precificador.Core/Acessos/SolicitacaoAcessoEmpresa.cs
src/Precificador.Core/Acessos/SituacaoSolicitacaoAcessoEmpresa.cs
src/Precificador.Core/Empresas/... normalizador compartilhado, se necessário

src/Precificador.Infrastructure/Persistence/Configurations/SolicitacaoAcessoEmpresaConfiguration.cs
src/Precificador.Infrastructure/Persistence/PrecificadorDbContext.cs
src/Precificador.Infrastructure/Migrations/<nova migration UC038>.cs
src/Precificador.Infrastructure/Migrations/PrecificadorDbContextModelSnapshot.cs

src/Precificador.Web/Pages/Index.cshtml
src/Precificador.Web/Pages/Index.cshtml.cs

tests/Precificador.Tests.Unit/Acessos/...
tests/Precificador.Tests.Integration/Infrastructure/SolicitacaoAcessoEmpresaPersistenceTests.cs
tests/Precificador.Tests.Integration/Web/SolicitarAcessoPageTests.cs

docs/use-cases/UC038-solicitar-acesso-precificador.md
docs/business/business-rules.md
docs/use-cases/catalog.md
docs/development/backlog.md
~~~

## Fora do escopo

- aprovação/recusa — UC039;
- listagem administrativa de solicitações — UC039;
- criação de Empresa — UC039;
- definição/troca do Administrador da Empresa — UC039;
- envio de e-mail — UC040;
- token de ativação — UC040;
- definição inicial de senha — UC040;
- recuperação de senha — UC040;
- convite de usuários adicionais — UC031;
- auto-registro;
- criação de senha no formulário público;
- CNPJ obrigatório;
- telefone obrigatório;
- validação documental/jurídica da Empresa;
- verificação automática de domínio de e-mail;
- CAPTCHA;
- rate limiting persistente;
- tela pública de acompanhamento da solicitação;
- cancelamento público da solicitação;
- edição de solicitação já enviada;
- purge/retenção automática;
- redesign completo da Home — MEL026.

## Definition of Done

UC038 está concluída quando:

- Home pública recebe o formulário;
- submissão válida persiste somente uma Solicitação Pendente;
- nenhum tenant/usuário/vínculo/credencial é criado;
- normalização da Empresa é compatível com o domínio real;
- duplicidade pendente é idempotente inclusive sob concorrência;
- resposta pública não permite enumeração;
- dados mínimos e limites estão validados no servidor;
- antiforgery está preservado;
- schema/migration SQL Server estão testados;
- testes de banco vazio e upgrade FT003 passam;
- documentação e RN059–RN061 estão alinhadas;
- CI completa está verde;
- backlog marca UC038 como Concluído após implementação.

## Branch de implementação sugerida

~~~text
feat/uc038-solicitar-acesso
~~~

## Commit sugerido

~~~text
feat: adiciona solicitacao publica de acesso
~~~
