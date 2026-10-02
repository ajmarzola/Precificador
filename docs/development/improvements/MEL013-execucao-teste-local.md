# MEL013 — Documentar execução e teste local

- **Origem:** Review MVP 2026-09-16
- **Classificação:** Documentação / DX
- **Prioridade:** média
- **Estado:** Concluído
- **Dependências materiais:** FT001, FT002, MEL011, MEL020, MEL021, MEL028, FT003, UC038–UC040, UC039, UC031 e UC037 concluídos
- **Dependência de MEL027:** não
- **Dependência de UC035:** não
- **Alteração de código de produção:** não
- **Alteração de schema/migration:** não

## Objetivo

Criar um guia único, reproduzível e seguro para preparar o ambiente local do Precificador, executar a aplicação, inicializar uma instalação vazia, configurar opcionalmente e-mail, executar testes e diagnosticar os problemas mais comuns.

O guia deve permitir que outro desenvolvedor, partindo do repositório, entenda sem consultar conversas externas:

1. quais ferramentas precisa instalar;
2. qual banco é usado localmente;
3. como restaurar/buildar o projeto;
4. como executar a aplicação;
5. como funciona o bootstrap atual;
6. como obter uma Empresa operacional para testes manuais;
7. como rodar testes unitários e de integração;
8. como aplicar/listar migrations;
9. como resetar somente uma base local com segurança;
10. como distinguir problemas de ambiente de falhas da aplicação.

## Atualização do escopo original

A versão antiga desta MEL citava SQLite e criação do primeiro usuário + Empresa no Setup. Esses pontos estão obsoletos.

O guia final deve refletir o estado atual:

~~~text
Persistência local -> SQL Server
Development padrão no Windows -> SQL Server LocalDB
Testes de integração -> SQL Server 2022 via Testcontainers/Docker
Setup -> cria somente o primeiro SystemAdmin
Empresa real -> nasce pela aprovação de solicitação de acesso
Production -> migration explícita, nunca automática no startup
~~~

Não mencionar `precificador.db` como mecanismo vigente.

## Artefato final

Criar o guia:

~~~text
docs/development/execucao-teste-local.md
~~~

O documento MEL013 continua como especificação/histórico do incremento.

Entrega: [guia de execução e testes locais](../execucao-teste-local.md), com referências nos READMEs e validação documental. Os critérios CA01–CA04 registram a preparação/liberação anterior à implementação; o estado final desta entrega é Concluído.

O README raiz e `docs/README.md` devem apontar para o novo guia após a implementação.

## Público do guia

O guia é para desenvolvedores do projeto, não para usuário final.

Não transformar MEL013 em Manual do Usuário; MEL014 continua separado.

## 1. Pré-requisitos

O guia deve listar, no mínimo:

- Git;
- .NET SDK 10;
- SQL Server acessível para execução local;
- Docker Engine/Docker Desktop somente para a suíte de integração;
- navegador;
- opcionalmente PowerShell para scripts Azure, deixando claro que Azure não faz parte do quick start local.

### .NET

O repositório possui:

~~~text
global.json
SDK base 10.0.400
rollForward = latestMinor
~~~

O guia deve orientar a validar:

~~~text
dotnet --info
dotnet --version
~~~

Não exigir uma IDE específica.

### EF Core tool

O repositório versiona `dotnet-ef` em `.config/dotnet-tools.json`.

Sempre orientar:

~~~text
dotnet tool restore
~~~

Não orientar instalação global de versão arbitrária do `dotnet-ef` como caminho principal.

## 2. Banco local

### Windows — padrão do projeto

`appsettings.Development.json` usa:

~~~text
Server=(localdb)\MSSQLLocalDB;
Database=Precificador;
Trusted_Connection=True;
TrustServerCertificate=True;
~~~

Logo, no Windows com LocalDB disponível, não é necessário configurar connection string para o quick start.

O guia pode incluir diagnóstico com:

~~~text
sqllocaldb info MSSQLLocalDB
~~~

sem tornar esse comando requisito para quem usa outra instância SQL Server.

### Linux/macOS/Windows sem LocalDB

Orientar o desenvolvedor a fornecer:

~~~text
ConnectionStrings__Precificador
~~~

apontando para um SQL Server acessível.

Exemplos devem usar placeholders e nunca credenciais reais versionadas.

Não adicionar Docker Compose nem alterar infraestrutura nesta MEL.

É permitido mostrar um exemplo opcional de SQL Server local/container, mas o guia deve deixar claro que ele é apenas uma alternativa para a aplicação; os testes de integração já gerenciam seu próprio container.

## 3. Segredos e configuração local

Nunca versionar:

- senha SQL;
- senha SMTP;
- senha de app Gmail;
- chave de bootstrap;
- TenantId/SubscriptionId pessoais;
- tokens;
- connection strings com credenciais.

Como o projeto não possui `UserSecretsId` versionado hoje, o guia principal deve usar variáveis de ambiente para segredos locais.

Não orientar `dotnet user-secrets init` como passo obrigatório, pois isso modificaria o projeto.

Se mencionar user-secrets, marcar como alternativa somente após configuração explícita do projeto e fora do fluxo mínimo.

## 4. Bootstrap local

`/Setup` exige a configuração:

~~~text
Bootstrap:SystemAdminKey
~~~

Via variável de ambiente:

~~~text
Bootstrap__SystemAdminKey
~~~

O guia deve mostrar exemplo com valor fictício/local e instruir a não reutilizá-lo em ambiente real.

Sem a chave:

~~~text
GET /Setup -> 503
A configuração inicial está indisponível.
~~~

Depois de existir o primeiro SystemAdmin:

~~~text
/Setup -> 404
~~~

Isso é comportamento esperado.

## 5. Restore e build

Quick start a partir da raiz do repositório:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
~~~

Explicar que build Release aproxima o quality gate da CI.

## 6. Execução da aplicação

Com `ASPNETCORE_ENVIRONMENT=Development`, usar caminho equivalente a:

~~~text
dotnet run --project src/Precificador.Web/Precificador.Web.csproj --launch-profile https
~~~

Profiles atuais:

~~~text
https://localhost:7162
http://localhost:5096
~~~

Em máquina que ainda não confia no certificado de desenvolvimento HTTPS, mencionar:

~~~text
dotnet dev-certs https --trust
~~~

como passo opcional/diagnóstico.

## 7. Migrations por ambiente

Documentar explicitamente:

~~~text
Development
-> Database.MigrateAsync() no startup

Production/outros
-> NÃO aplica migration no startup
-> migration é etapa explícita de deploy
~~~

Logo, no quick start Development, iniciar a aplicação em uma base vazia deve criar/aplicar o schema automaticamente.

## 8. EF Core manual

Para listar migrations:

~~~text
dotnet ef migrations list \
  --project src/Precificador.Infrastructure/Precificador.Infrastructure.csproj \
  --startup-project src/Precificador.Web/Precificador.Web.csproj
~~~

Para aplicar explicitamente quando necessário:

~~~text
dotnet ef database update \
  --project src/Precificador.Infrastructure/Precificador.Infrastructure.csproj \
  --startup-project src/Precificador.Web/Precificador.Web.csproj
~~~

Em documentação Windows/PowerShell, usar continuação sintaticamente apropriada ou apresentar em uma linha para evitar comando inválido.

Explicar que a design-time factory usa `ConnectionStrings__Precificador` quando definida; caso contrário, usa LocalDB padrão.

## 9. Primeiro uso atual

Em banco novo:

1. iniciar a aplicação em Development;
2. acessar Login ou `/Setup`;
3. informar `Bootstrap__SystemAdminKey`, e-mail e senha;
4. Setup cria somente o primeiro SystemAdmin;
5. autenticar;
6. SystemAdmin entra em `/Admin`.

Não afirmar que Setup cria Empresa ou Empresa Ativa.

Não documentar credencial padrão porque ela não existe.

## 10. Criar uma Empresa operacional para teste manual

O guia deve documentar o fluxo real, sem atalhos de banco:

1. usuário anônimo envia Solicitação de acesso na Home;
2. SystemAdmin entra em Administração/Solicitações;
3. aprova a solicitação;
4. aprovação cria Empresa, configuração padrão e primeiro vínculo Administrador;
5. novo Administrador define a própria senha pelo link de ativação;
6. Administrador faz Login;
7. com uma única Empresa elegível, Login resolve Empresa Ativa e segue ao Dashboard;
8. com múltiplas Empresas, o usuário passa por `/Empresas/Selecionar`.

## 11. SMTP no ambiente local

SMTP é opcional para o startup.

Com:

~~~text
Email__Smtp__Enabled=false
~~~

ou configuração ausente, a aplicação pode iniciar normalmente.

Porém, sem SMTP real, o fluxo manual completo de ativação/recuperação não entrega o link ao usuário.

Isso deve ser documentado como comportamento esperado.

Para testar manualmente ativação real, apontar para:

~~~text
docs/development/configuracao-email-conta.md
~~~

e listar que `Aplicacao__UrlPublica` precisa ser URL local canônica quando SMTP estiver habilitado, por exemplo o endereço HTTPS do launch profile.

Não duplicar no MEL013 todas as regras de segurança do SMTP.

## 12. Testes unitários

Comando canônico:

~~~text
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
~~~

Explicar que unitários não precisam de Docker/SQL Server externo.

## 13. Testes de integração

Comando canônico:

~~~text
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
~~~

Pré-requisito adicional:

~~~text
Docker daemon acessível
~~~

A infraestrutura atual:

~~~text
Testcontainers.MsSql
imagem mcr.microsoft.com/mssql/server:2022-latest
um container SQL Server compartilhado por processo de teste
um database isolado por cenário/helper
cleanup pelo Ryuk
~~~

Deixar claro que o desenvolvedor não precisa subir manualmente o SQL Server de testes.

## 14. Diagnóstico do Docker/Testcontainers

Quando integração falhar antes de executar os testes, orientar verificar:

~~~text
docker version
docker info
~~~

e confirmar que o daemon está em execução.

No Windows, citar Docker Desktop/WSL2 apenas como possibilidade de ambiente, não como requisito arquitetural.

Não mandar aumentar recursos de Docker de forma arbitrária.

## 15. Quality gate local completo

Documentar a sequência equivalente à CI full:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
git diff --check
~~~

Explicar MEL028:

- PR somente documental continua executando o required check, mas pula setup .NET/build/testes;
- PR com código executa a suíte full;
- executar localmente a sequência completa continua recomendado antes de PR com código.

## 16. Teste focado durante desenvolvimento

O guia pode mostrar `dotnet test --filter` como ferramenta de produtividade.

Exemplo deve ser genérico, sem tornar teste focado substituto da suíte completa antes de entrega.

## 17. Reset seguro da base local

Reset deve ser apresentado como operação destrutiva exclusiva de ambiente de desenvolvimento.

Antes de qualquer comando, exigir confirmação de:

~~~text
ASPNETCORE_ENVIRONMENT = Development
connection string aponta para base local descartável
não é Azure / homologação / produção
~~~

Caminho recomendado para LocalDB de desenvolvimento:

~~~text
dotnet ef database drop --force
~~~

com os mesmos `--project` e `--startup-project` corretos.

Depois, iniciar a aplicação em Development reaplica migrations automaticamente.

Também é válido usar `dotnet ef database update` explicitamente.

Não incluir scripts que apaguem servidor, Resource Group ou banco Azure.

## 18. Como saber que está usando o banco errado

Listar sintomas:

- dados antigos inesperados;
- `/Setup` retorna 404 quando se esperava instalação vazia;
- Login encontra usuário que deveria não existir;
- migration esperada não aparece;
- connection error para host diferente do esperado.

Orientar verificar a configuração efetiva sem imprimir senha.

Para Development Windows, relembrar que a ausência de override usa LocalDB/Database=Precificador.

Não instruir a logar connection string completa contendo segredo.

## 19. Diagnóstico de schema/migrations

Cenários:

### Login/Setup com tabela ausente

Verificar:

- ambiente realmente Development;
- connection string correta;
- startup chegou a executar migrations;
- usuário SQL possui DDL quando execução local exige criar schema.

### Migration pendente

Usar `dotnet ef migrations list` e, quando apropriado, `database update`.

### Production/Azure

Não sugerir ligar migration automática.

Apontar para `infra/azure/README.md`.

## 20. Diagnóstico do Setup

### `/Setup` retorna 503

Provável causa local: `Bootstrap__SystemAdminKey` ausente.

### `/Setup` retorna 404

Provável causa: já existe SystemAdmin.

### chave rejeitada

Revalidar a variável no processo que executa a aplicação.

## 21. Diagnóstico de ativação/e-mail

Se aprovação cria Empresa mas informa que comunicação não foi enviada:

- isso não desfaz a transação;
- conferir se SMTP está habilitado/configurado;
- sem SMTP, é esperado que o novo usuário sem senha não consiga completar ativação manual pela UI.

Apontar para documentação UC040.

## 22. Segurança em PC compartilhado

Como o projeto pode ser executado em máquinas compartilhadas, incluir seção curta:

- preferir variáveis somente no processo/sessão de terminal;
- não persistir senha/chave em arquivo versionado;
- limpar variáveis sensíveis ao terminar;
- não salvar credenciais no histórico do shell quando possível;
- não autenticar Azure para o quick start local;
- MEL027/CD não é requisito para MEL013.

Não transformar o guia em política corporativa de endpoint.

## 23. Azure é fluxo separado

O guia local deve apontar, e não duplicar:

~~~text
infra/azure/README.md
~~~

Deixar explícito:

- execução local não requer Azure CLI;
- testes não usam Azure SQL;
- não copiar connection string local para Production;
- Production usa Managed Identity e migration explícita segundo MEL021;
- MEL027 permanece pendente e não deve ser antecipada pela MEL013.

## 24. Estrutura recomendada do guia final

~~~text
# Execução e testes locais
## Quick start
## Pré-requisitos
## Banco local
## Configuração segura
## Rodar a aplicação
## Primeiro Setup
## Criar Empresa para teste manual
## SMTP opcional
## Migrations
## Testes unitários
## Testes de integração
## Quality gate completo
## Reset da base de Development
## Troubleshooting
## Diferenças para Azure/Production
~~~

## Alterações permitidas na implementação

A MEL013 é documental.

Permitido:

- criar `docs/development/execucao-teste-local.md`;
- atualizar MEL013 para Concluído;
- atualizar backlog;
- atualizar README raiz e docs/README;
- alinhar referências documentais evidentemente obsoletas que contradigam diretamente o guia atual.

Não alterar código, csproj, migrations, workflow CI, scripts Azure ou configurações da aplicação somente para fazer a documentação 'bater'.

Se durante a implementação o guia revelar defeito real no código, registrar item separado.

## Fora do escopo

- implementar MEL027;
- alterar GitHub Actions;
- configurar secrets do GitHub;
- criar pipeline CD;
- implementar UC035;
- criar Docker Compose;
- trocar LocalDB por outro default;
- instalar SQL Server automaticamente;
- configurar Docker automaticamente;
- criar seed de usuário/Empresa;
- criar senha padrão;
- bypassar ativação de conta;
- adicionar endpoint de desenvolvimento;
- alterar Setup;
- alterar fluxo de aprovação;
- alterar SMTP;
- documentar uso funcional completo do Precificador (MEL014);
- alterar scripts Azure MEL021.

## Critérios de aceitação

- **CA01:** MEL013 fica `Pronto` com dependências materiais atuais documentadas.
- **CA02:** UC035 deixa de constar como dependência técnica da MEL013.
- **CA03:** MEL027 deixa de constar como dependência técnica da MEL013.
- **CA04:** backlog registra exceção explícita para executar MEL013 antes de MEL027/UC035.
- **CA05:** guia final existe em `docs/development/execucao-teste-local.md`.
- **CA06:** guia não cita SQLite/`precificador.db` como persistência atual.
- **CA07:** guia identifica SQL Server como persistência atual.
- **CA08:** guia identifica LocalDB como default Development Windows.
- **CA09:** guia documenta override `ConnectionStrings__Precificador`.
- **CA10:** guia explica que não se versionam credenciais.
- **CA11:** guia documenta SDK do `global.json`.
- **CA12:** guia documenta `dotnet tool restore`.
- **CA13:** guia documenta restore da solution.
- **CA14:** guia documenta build Release.
- **CA15:** guia documenta launch profile HTTPS.
- **CA16:** guia documenta URLs locais atuais.
- **CA17:** guia documenta `Bootstrap__SystemAdminKey`.
- **CA18:** guia explica 503 do Setup sem chave.
- **CA19:** guia explica 404 do Setup após bootstrap.
- **CA20:** guia afirma que Setup cria somente SystemAdmin.
- **CA21:** guia não promete criação de Empresa no Setup.
- **CA22:** guia afirma que não há credencial padrão.
- **CA23:** guia documenta fluxo real de solicitação/aprovação para criar Empresa.
- **CA24:** guia explica ativação do primeiro Administrador.
- **CA25:** guia explica Empresa Ativa e seleção multiempresa.
- **CA26:** SMTP é documentado como opcional para startup.
- **CA27:** guia explica limitação do fluxo manual sem SMTP.
- **CA28:** guia aponta para `configuracao-email-conta.md`.
- **CA29:** guia diferencia migrations Development e Production.
- **CA30:** guia documenta `dotnet ef migrations list` com projects corretos.
- **CA31:** guia documenta `dotnet ef database update` com projects corretos.
- **CA32:** guia explica design-time factory/connection override.
- **CA33:** guia documenta testes unitários.
- **CA34:** guia afirma que unitários não requerem Docker.
- **CA35:** guia documenta testes de integração.
- **CA36:** guia identifica Docker como pré-requisito dos testes de integração.
- **CA37:** guia identifica Testcontainers.MsSql.
- **CA38:** guia identifica SQL Server 2022 containerizado nos testes.
- **CA39:** guia explica que o container é gerenciado automaticamente.
- **CA40:** guia contém diagnóstico básico `docker version/info`.
- **CA41:** guia documenta quality gate local completo.
- **CA42:** quality gate corresponde à CI full vigente.
- **CA43:** guia explica otimização docs-only da MEL028.
- **CA44:** teste focado não substitui suíte completa no handoff.
- **CA45:** reset exige confirmação explícita de ambiente local descartável.
- **CA46:** reset não contém ação sobre Azure.
- **CA47:** guia documenta `database drop` somente para Development.
- **CA48:** guia explica que startup Development recria/aplica migrations.
- **CA49:** troubleshooting cobre banco errado.
- **CA50:** troubleshooting cobre schema/migration.
- **CA51:** troubleshooting cobre Setup.
- **CA52:** troubleshooting cobre Docker/Testcontainers.
- **CA53:** troubleshooting cobre SMTP/ativação.
- **CA54:** guia não imprime/loga segredos como prática recomendada.
- **CA55:** guia inclui cuidados para PC compartilhado.
- **CA56:** quick start local não exige login Azure.
- **CA57:** Azure é referenciado por `infra/azure/README.md`.
- **CA58:** Production continua sem migration automática.
- **CA59:** MEL027 não é implementada/antecipada.
- **CA60:** UC035 não é implementada/antecipada.
- **CA61:** nenhum código de produção é alterado pela MEL013.
- **CA62:** nenhum csproj é alterado para habilitar user-secrets incidentalmente.
- **CA63:** nenhuma migration é criada.
- **CA64:** workflow CI não é alterado.
- **CA65:** README raiz aponta para o guia final após implementação.
- **CA66:** docs/README indexa o guia final.
- **CA67:** comandos documentados usam caminhos existentes no repositório.
- **CA68:** links internos do guia apontam para arquivos existentes.
- **CA69:** `git diff --check` fica limpo.
- **CA70:** PR da implementação permanece exclusivamente documental.

## Validação documental

Na implementação, validar pelo menos:

- caminhos citados existem;
- comandos correspondem ao `global.json`, tool manifest e csproj atuais;
- launch profiles/URLs correspondem ao repositório;
- nomes das configurações correspondem ao código;
- nenhuma referência ao SQLite permanece no guia novo como estado vigente;
- links relativos funcionam;
- `git diff --check` passa.

Como a MEL013 é documentação-only, a CI MEL028 deve classificar a PR como `documentation-only` e manter o required check verde sem inicializar .NET/Testcontainers.

## Definition of Done

MEL013 está concluída quando o guia final permite preparar, executar, inicializar, testar, resetar e diagnosticar o ambiente local atual sem depender de conhecimento externo; README/backlog estão alinhados; MEL027 e UC035 permanecem pendentes; e a PR contém somente documentação.

## Branch de implementação

~~~text
docs/mel013-guia-execucao-local
~~~

## Commit sugerido

~~~text
docs: adiciona guia de execucao e testes locais
~~~
