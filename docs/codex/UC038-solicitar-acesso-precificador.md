# Instrução Codex — UC038 Solicitar acesso ao Precificador

Você está implementando a **UC038 — Solicitar acesso ao Precificador** no repositório `ajmarzola/Precificador`.

## Fonte normativa

Leia integralmente:

1. `AGENTS.md`;
2. `docs/use-cases/UC038-solicitar-acesso-precificador.md`;
3. `docs/development/foundation-administration-authorization.md`;
4. `docs/business/business-rules.md`, especialmente RN059–RN061;
5. `docs/use-cases/catalog.md`;
6. `docs/architecture/architecture.md`;
7. `docs/product/scope.md`;
8. `docs/development/testing-strategy.md`;
9. `docs/development/definition-of-done.md`;
10. implementação real atual de `Empresa`, `PrecificadorDbContext`, Home pública, Identity, FT003 e migrations/testes SQL Server.

A especificação normativa da entrega é:

~~~text
docs/use-cases/UC038-solicitar-acesso-precificador.md
~~~

## Branch

Use:

~~~text
feat/uc038-solicitar-acesso
~~~

Parta da `master` atualizada após o merge da documentação da UC038.

Não implemente na branch documental.

## Objetivo

Adicionar na **Home pública** um formulário para solicitar acesso ao Precificador.

Uma submissão válida deve criar somente:

~~~text
SolicitacaoAcessoEmpresa
Situacao = Pendente
~~~

É proibido nesta UC criar:

- Empresa;
- UsuarioAplicacao;
- UsuarioEmpresa;
- role;
- senha;
- token;
- convite;
- Empresa Ativa;
- envio de e-mail.

## Modelo

Criar entidade global, fora de tenant:

~~~text
SolicitacaoAcessoEmpresa
- Id
- NomeEmpresa
- NomeEmpresaNormalizado
- NomeResponsavel
- EmailResponsavel
- EmailResponsavelNormalizado
- Observacao?
- DataSolicitacaoUtc
- Situacao
~~~

Enum estável:

~~~text
Pendente = 1
Aprovada = 2
Recusada = 3
~~~

UC038 só cria `Pendente`.

Não implementar `IEntidadeEmpresa`.

Não adicionar `EmpresaId`.

Não aplicar Global Query Filter de Empresa.

## Campos Web

Formulário anônimo na Home:

- Nome da Empresa — obrigatório, máx. 120;
- Nome do responsável — obrigatório, máx. 120;
- E-mail — obrigatório, formato válido, máx. 256;
- Observação — opcional, máx. 1000.

Não adicionar:

- senha;
- CNPJ obrigatório;
- telefone obrigatório;
- token;
- código de convite.

## Normalização

### Empresa

A Solicitação deve usar exatamente a mesma semântica de nome de `Empresa`:

~~~text
trim
+ colapsar whitespace
+ NomeNormalizado = upper invariant
~~~

Evite duplicar algoritmo.

Preferir extrair um normalizador compartilhado no Core e fazer `Empresa` + `SolicitacaoAcessoEmpresa` reutilizarem a mesma implementação.

Não alterar a semântica atual de `Empresa`.

### Responsável

Trim + colapso de whitespace.

### E-mail

~~~text
EmailResponsavel = Trim()
EmailResponsavelNormalizado = ToUpperInvariant()
~~~

### Observação

Trim externo; whitespace puro vira null; preserve quebras internas.

## Data

Use:

~~~text
TimeProvider.GetUtcNow()
~~~

Persistir `DateTimeOffset` UTC.

Não usar APIs estáticas de relógio.

## Estados e idempotência

Identidade de reenvio:

~~~text
NomeEmpresaNormalizado + EmailResponsavelNormalizado
~~~

### Pendente existente

Não criar duplicata.

Não alterar:

- data;
- observação;
- campos já persistidos.

Retornar o mesmo sucesso público.

### Aprovada existente

Não criar nova solicitação idêntica.

Retornar o mesmo sucesso público.

### Recusada existente

Permitir nova Pendente.

### Combinações permitidas

Permitir:

~~~text
mesmo e-mail + Empresas diferentes
mesma Empresa + e-mails diferentes
~~~

## Índices

Criar índice único filtrado:

~~~text
(NomeEmpresaNormalizado, EmailResponsavelNormalizado)
WHERE Situacao = 1
~~~

Preferir nome explícito:

~~~text
UX_SolicitacoesAcessoEmpresas_Pendente_Nome_Email
~~~

Adicionar índice de consulta futura:

~~~text
Situacao + DataSolicitacaoUtc
~~~

Adicionar check constraint:

~~~text
Situacao IN (1, 2, 3)
~~~

Não editar migrations históricas.

## Concorrência

O POST deve consultar duplicidade antes de inserir, mas confiar também no índice único filtrado.

Se ocorrer corrida:

- tratar **somente** violação específica de unicidade da pendência como sucesso idempotente;
- não converter toda `DbUpdateException` em sucesso.

Para SQL Server, trate os códigos de unique violation compatíveis (2601/2627) de maneira restrita e testável.

Não usar lock distribuído.

## Home pública

A UC038 evolui a Home atual.

Para anônimo, renderizar:

- apresentação atual;
- Login;
- formulário “Solicitar acesso”;
- texto curto de uso dos dados.

Não executar o redesign completo reservado à MEL026.

### Sucesso

Usar PRG.

Mensagem semanticamente equivalente:

~~~text
Recebemos sua solicitação de acesso. Ela será analisada e, se necessário, entraremos em contato pelo e-mail informado.
~~~

Mesma mensagem para:

- nova solicitação;
- Pendente repetida;
- Aprovada repetida.

Não revelar existência de registros.

### Inválido

- HTTP 200;
- formulário com erros;
- valores preservados;
- nenhuma persistência.

## Request autenticado

O handler público não pode ser usado por usuário autenticado para criar solicitação.

Se autenticado:

- SystemAdmin -> destino canônico `/Admin`;
- usuário empresarial -> preservar destino canônico já usado pela Home/FT003;
- não persistir solicitação.

Não inventar auto-criação de Empresa.

## Segurança

- antiforgery obrigatório;
- server-side validation obrigatório;
- não usar `Html.Raw` com entrada;
- não persistir IP/User-Agent/fingerprint;
- não logar explicitamente PII/Observação;
- não criar endpoint público de consulta/status;
- não expor se e-mail já existe no Identity.

Não adicionar CAPTCHA/rate limiting nesta UC.

## E-mail

Não criar:

- SMTP;
- SendGrid;
- abstração de mail;
- template de e-mail.

UC040 cuidará de token/e-mail.

## /Admin

Não listar solicitações ainda.

UC039 será responsável por fila de análise, aprovação e recusa.

Não adicionar contador, CRUD ou detalhes administrativos nesta UC.

## Testes obrigatórios

### Core

Cobrir normalizações, limites e estado Pendente.

Especialmente provar que normalização de Nome da Solicitação e `Empresa` é equivalente.

### Persistence

Cobrir:

- migration em banco vazio;
- upgrade da master/FT003;
- check constraint;
- índice único filtrado;
- mesmo e-mail + Empresa diferente;
- mesma Empresa + e-mail diferente;
- Recusada + nova Pendente;
- ausência de efeitos em tabelas de Empresa/Identity.

### Web

Cobrir:

- GET anônimo mostra formulário;
- não mostra senha;
- GET não persiste;
- POST válido cria Pendente;
- PRG;
- mensagem de sucesso;
- normalização;
- TimeProvider determinístico;
- invalid ModelState não persiste;
- antiforgery;
- Pendente repetida idempotente;
- Aprovada repetida idempotente;
- Recusada permite reenvio;
- corrida concorrente -> uma Pendente;
- erro DbUpdate não relacionado a unicidade não é engolido;
- mesmo e-mail em duas Empresas;
- mesma Empresa com dois e-mails;
- e-mail existente no Identity não é modificado;
- POST autenticado não persiste;
- contagem de Empresas/Users/Roles/UsuarioEmpresa permanece igual após solicitação.

## Restrições

Não implementar:

- UC039;
- UC040;
- UC031;
- aprovação;
- recusa via UI;
- criação de Empresa;
- convite;
- ativação;
- recuperação de senha;
- envio de e-mail;
- status público;
- cancelamento/edição de pedido;
- CNPJ/telefone obrigatórios;
- CAPTCHA;
- redesign MEL026.

## Documentação ao concluir

Ao finalizar:

- marcar UC038 como `Concluído` no backlog;
- alterar `Estado: Concluído` no documento UC038;
- preservar UC040/UC039/UC031 como Planejado;
- manter RN059–RN061 coerentes;
- atualizar catálogo se necessário;
- não afirmar que solicitação já pode ser aprovada pela UI.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
~~~

Validar migration em banco vazio e upgrade representativo da master com FT003.

Commit sugerido:

~~~text
feat: adiciona solicitacao publica de acesso
~~~

Não faça merge em `master`.
