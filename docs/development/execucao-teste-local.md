# Execução e testes locais

Guia para desenvolvedores. Execute os comandos a partir da raiz do repositório. Os comandos `dotnet` abaixo estão em uma linha e funcionam em PowerShell e Bash; os blocos de variáveis identificam o shell. O [workflow do Codex](workflow-codex.md) define o trabalho em branch dedicada e a entrega por PR.

## Quick start

No Windows com SQL Server LocalDB disponível:

```text
dotnet --info
dotnet --version
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
```

Na mesma sessão PowerShell, configure uma chave exclusiva para o bootstrap local. Substitua o placeholder antes de executar; não o reutilize em ambiente real:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Bootstrap__SystemAdminKey = '<CHAVE-EXCLUSIVA-LOCAL>'
dotnet run --project src/Precificador.Web/Precificador.Web.csproj --launch-profile https
```

Abra `https://localhost:7162/Setup`. O startup em Development aplica migrations; Setup cria o primeiro SystemAdmin. Depois, siga as seções de criação de Empresa e SMTP para testar uma identidade nova de Administrador. Sem LocalDB, configure primeiro o banco descrito abaixo. Azure e Docker não são requisitos deste quick start quando já existe SQL Server local acessível.

## Pré-requisitos

- Git e navegador; nenhuma IDE específica é obrigatória.
- .NET SDK 10 compatível com `global.json`: base `10.0.400`, `rollForward=latestMinor`, sem prerelease. Confira a versão selecionada com `dotnet --version` na raiz.
- SQL Server acessível para rodar a aplicação, com permissão para criar/aplicar o schema local.
- Docker Engine ou Docker Desktop com daemon acessível somente para integração e quality gate completo.

`dotnet tool restore` restaura o `dotnet-ef` versionado em `.config/dotnet-tools.json` (atualmente `10.0.12`). Use o manifest, sem instalação global de versão arbitrária. PowerShell também é usado pelos scripts Azure, que pertencem a um fluxo separado.

## Banco local

O provider atual é SQL Server. No Windows, `src/Precificador.Web/appsettings.Development.json` usa LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=Precificador;Trusted_Connection=True;TrustServerCertificate=True;
```

Com essa instância disponível, o quick start não precisa de override. Para diagnóstico específico de LocalDB:

```text
sqllocaldb info MSSQLLocalDB
```

No Linux/macOS ou Windows sem LocalDB, forneça uma instância SQL Server acessível e configure `ConnectionStrings__Precificador`. Substitua os placeholders por dados locais; `TrustServerCertificate=True` neste exemplo é para desenvolvimento local.

PowerShell:

```powershell
$env:ConnectionStrings__Precificador = 'Server=<HOST-LOCAL>,<PORTA>;Database=<BANCO-LOCAL-DESCARTAVEL>;User ID=<USUARIO-LOCAL>;Password=<SEGREDO-LOCAL>;TrustServerCertificate=True;'
```

Bash:

```bash
export ConnectionStrings__Precificador='Server=<HOST-LOCAL>,<PORTA>;Database=<BANCO-LOCAL-DESCARTAVEL>;User ID=<USUARIO-LOCAL>;Password=<SEGREDO-LOCAL>;TrustServerCertificate=True;'
```

A instância da aplicação é independente da infraestrutura de integração: Testcontainers gerencia seu próprio SQL Server, sem exigir que você suba manualmente um banco de testes.

## Configuração segura

Use variáveis apenas na sessão/processo do terminal que inicia a aplicação. O projeto não possui `UserSecretsId` versionado; user-secrets exigiria configuração explícita em trabalho separado e não faz parte deste fluxo.

Em Bash, o equivalente à configuração inicial é:

```bash
export ASPNETCORE_ENVIRONMENT='Development'
export Bootstrap__SystemAdminKey='<CHAVE-EXCLUSIVA-LOCAL>'
```

Nunca versione senhas SQL/SMTP, senha de app Gmail, chave de bootstrap, tokens, connection strings com credenciais ou TenantId/SubscriptionId pessoais. Não imprima connection strings completas nem segredos para diagnosticar configuração.

Em PC compartilhado, evite registrar valores sensíveis no histórico do shell: use entrada interativa protegida quando disponível, sem colar segredos em comandos que serão gravados. Não persista variáveis na máquina, não salve credenciais no repositório e encerre o processo da aplicação ao terminar. Limpe as variáveis da sessão após parar a aplicação:

```powershell
Remove-Item Env:Bootstrap__SystemAdminKey, Env:ConnectionStrings__Precificador, Env:Email__Smtp__Password, Env:Email__Smtp__UserName -ErrorAction SilentlyContinue
```

```bash
unset Bootstrap__SystemAdminKey ConnectionStrings__Precificador Email__Smtp__Password Email__Smtp__UserName
```

Se configurar outros valores sensíveis, limpe-os também. O quick start não exige autenticação Azure nem MEL027/CD.

## Rodar a aplicação

```text
dotnet run --project src/Precificador.Web/Precificador.Web.csproj --launch-profile https
```

O profile `https` define Development e escuta em `https://localhost:7162` e `http://localhost:5096`. O profile `http` usa somente a segunda URL. O build Release do quick start aproxima o quality gate da CI; o comando `run` acima usa a configuração padrão de desenvolvimento.

Se o navegador não confiar no certificado local, use como diagnóstico/configuração opcional:

```text
dotnet dev-certs https --trust
```

Confirme a confiança no certificado conforme o suporte do sistema operacional. Pare a aplicação com `Ctrl+C`.

## Primeiro Setup

Em banco novo, acesse `/Conta/Login` (que encaminha para Setup sem SystemAdmin) ou `/Setup`. Informe no formulário a chave definida em `Bootstrap__SystemAdminKey`, seu e-mail, senha e confirmação. Não existe credencial padrão.

Setup cria somente o primeiro **SystemAdmin global**, sem Empresa ou Empresa Ativa. Após a criação, faça Login; o SystemAdmin entra em `/Admin`.

- Sem `Bootstrap:SystemAdminKey` configurada, `/Setup` retorna **503**: “A configuração inicial está indisponível.”
- Após existir SystemAdmin, `/Setup` retorna **404**, comportamento esperado do bootstrap encerrado.

## Criar Empresa para teste manual

1. Em sessão anônima, abra a Home e envie uma Solicitação de acesso.
2. Com o SystemAdmin, entre em Administração/Solicitações (`/Admin/Solicitacoes`) e aprove a solicitação.
3. A aprovação cria a Empresa, configuração padrão e primeiro vínculo Administrador.
4. Para uma identidade nova, o Administrador recebe o link de ativação por e-mail e define a própria senha. A ativação não autentica automaticamente.
5. Faça Login como Administrador. Com uma única Empresa elegível, o fluxo resolve Empresa Ativa e segue ao Dashboard; com múltiplas Empresas, passa por `/Empresas/Selecionar`.

Use SMTP configurado antes da aprovação para completar manualmente esse fluxo. Se a identidade já tem senha, ela recebe aviso de acesso liberado e usa sua senha existente. Veja [administração global](administracao-global.md) e [UC040](../use-cases/UC040-ativar-conta-recuperar-acesso.md).

## SMTP opcional

SMTP não é necessário para startup. Configuração ausente ou `Email__Smtp__Enabled=false` permite iniciar normalmente. Sem transporte real, o link não chega ao novo Administrador, e o fluxo manual de ativação não pode ser completado pela UI. O POST de recuperação fica indisponível (503) com SMTP desabilitado.

Para testar e-mail real, siga [configuração de e-mail e acesso](configuracao-email-conta.md), que descreve `Email__Smtp__Enabled`, `Host`, `Port`, `Security`, `UserName`, `Password`, `FromAddress` e `FromName`. Configure também a URL canônica local para os links:

```powershell
$env:Aplicacao__UrlPublica = 'https://localhost:7162'
```

```bash
export Aplicacao__UrlPublica='https://localhost:7162'
```

SMTP habilitado com configuração inválida impede startup. Falha de comunicação após aprovação não desfaz a transação: Empresa e vínculo permanecem criados. Depois de corrigir SMTP, use o reenvio de ativação no detalhe administrativo da Empresa para Administrador ativo sem senha. Não manipule senha no banco nem crie bypass de ativação.

## Migrations

Em **Development**, `Program.cs` executa `Database.MigrateAsync()` no startup. Iniciar em base vazia cria/aplica o schema. Em **Production e outros ambientes**, migrations não são aplicadas no startup; são etapa explícita de implantação.

Para inspeção e aplicação manual, a partir da raiz:

```text
dotnet tool restore
dotnet ef migrations list --project src/Precificador.Infrastructure/Precificador.Infrastructure.csproj --startup-project src/Precificador.Web/Precificador.Web.csproj
dotnet ef database update --project src/Precificador.Infrastructure/Precificador.Infrastructure.csproj --startup-project src/Precificador.Web/Precificador.Web.csproj
```

Confirme o banco de destino antes de `database update`. A `DesignTimePrecificadorDbContextFactory` lê **a variável de ambiente** `ConnectionStrings__Precificador`; se ausente, usa LocalDB/Database=Precificador. Configure-a na mesma sessão dos comandos EF quando usar outra instância. O ambiente, por si só, não seleciona outro destino nessa factory.

## Testes unitários

Após restore e build Release:

```text
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
```

Unitários não precisam de Docker nem SQL Server externo. `--no-build` exige um build Release atualizado; recompile após alterar código ou testes.

## Testes de integração

Com Docker daemon acessível e build Release atualizado:

```text
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
```

`SqlServerTestDatabase` usa `Testcontainers.MsSql` com imagem `mcr.microsoft.com/mssql/server:2022-latest`: um container SQL Server compartilhado por processo de teste, database isolado por cenário/helper e cleanup pelo Ryuk ao final do processo. Não usa Azure SQL nem o banco local da aplicação. O primeiro uso pode baixar a imagem e requer acesso ao registry.

Durante desenvolvimento, um filtro pode acelerar investigação:

```text
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build --filter "FullyQualifiedName~<TRECHO-DO-NOME-DO-TESTE>"
```

Substitua o placeholder por um nome existente e confira que testes foram encontrados. Teste focado não substitui a suíte completa na entrega de código. Consulte a [estratégia de testes](testing-strategy.md).

## Quality gate completo

Para PR com código, execute a sequência de validação completa, com o restore das ferramentas locais:

```text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
git diff --check
```

Pare e corrija qualquer etapa que falhar antes de considerar o gate aprovado. Docker é obrigatório para a etapa de integração. Falha de inicialização de Docker/Testcontainers é impedimento de ambiente e significa integração **não validada**.

A [MEL028](improvements/MEL028-otimizar-ci-tipo-alteracao.md) mantém o required check `build-and-test`. PR com diff não vazio restrito a `docs/**` e Markdown na raiz usa `documentation-only`: verifica whitespace e pula setup .NET, restore, build e testes/Testcontainers. Arquivo fora dessa whitelist ou falha de classificação exige `full`; push em `master` sempre usa `full`. A sequência completa continua recomendada antes de PR com código. Veja a [Definition of Done](definition-of-done.md).

## Reset da base de Development

**Operação destrutiva: apaga todos os dados do banco de destino. Somente Development e banco local descartável; nunca Azure, homologação ou produção.** Pare a aplicação antes de prosseguir.

Confirme explicitamente, antes de qualquer comando destrutivo:

1. `ASPNETCORE_ENVIRONMENT` é `Development` na sessão.
2. O destino efetivo da factory é um banco local descartável (servidor e nome conferidos sem exibir senha).
3. Nenhum override aponta para Azure, homologação, produção ou dados que precisam ser preservados.

Para o LocalDB padrão, confirme que `ConnectionStrings__Precificador` está ausente e que `Precificador` é realmente descartável. Para outro destino local, confira o override. **Definir Development não protege contra um override remoto.**

Somente após essas confirmações:

```text
dotnet ef database drop --force --project src/Precificador.Infrastructure/Precificador.Infrastructure.csproj --startup-project src/Precificador.Web/Precificador.Web.csproj
```

Depois, inicie novamente a aplicação em Development para recriar/aplicar migrations, ou execute o `database update` da seção Migrations. Refaça Setup e solicitação/aprovação; dados e identidades anteriores foram apagados.

## Troubleshooting

| Sintoma | Verificação / ação |
|---|---|
| SDK não encontrado ou incompatível | Execute `dotnet --info` e `dotnet --version` na raiz e confira `global.json`. |
| `dotnet ef` indisponível | Execute `dotnet tool restore` na raiz; confira o manifest e acesso ao NuGet. |
| Conexão SQL falha | Confira instância, disponibilidade, autenticação e permissões. Em LocalDB, use `sqllocaldb info MSSQLLocalDB`. Verifique host/database do override sem imprimir credenciais. |
| Dados antigos, usuário inesperado, Setup 404 em instalação supostamente vazia | Confira o banco efetivo. Sem override, Development Windows e factory EF usam LocalDB/Precificador. Não resete até confirmar o destino. |
| Login/Setup com tabela ausente | Confira Development, banco correto, logs de startup/migrations e permissão DDL do usuário SQL local. |
| Migration esperada não aparece ou está pendente | Confira branch/build e destino da factory; execute `migrations list` e, no banco correto, `database update`. |
| Setup 503 | Configure `Bootstrap__SystemAdminKey` no processo que inicia a aplicação e reinicie-o. |
| Setup 404 | Já existe SystemAdmin; use Login. Se esperava base vazia, confira o destino antes de pensar em reset. |
| Chave do Setup rejeitada | Confira o valor informado e a variável na sessão que iniciou o processo; reinicie após alterar variáveis. |
| HTTPS local não confiável | Confira o certificado de desenvolvimento e use `dotnet dev-certs https --trust` conforme o sistema operacional. |
| Integração falha ao iniciar container | Execute os diagnósticos Docker abaixo; confirme daemon ativo e acesso à imagem. A suíte não está validada enquanto o ambiente falhar. |
| Aprovação salva, mas e-mail não enviado | A decisão já foi commitada. Confira SMTP e URL pública; corrija configuração e reenvie ativação pela administração. Sem SMTP, a limitação é esperada. |
| Startup falha após habilitar SMTP | Confira campos e valores na [configuração de e-mail](configuracao-email-conta.md), sem registrar segredos. |

Diagnóstico de Docker/Testcontainers:

```text
docker version
docker info
```

Confirme que o daemon responde. No Windows, Docker Desktop/WSL2 pode ser a infraestrutura utilizada, sem ser requisito arquitetural da aplicação. Não aumente recursos arbitrariamente nem substitua integração por mocks para declarar o gate verde.

## Diferenças para Azure/Production

Execução local não exige Azure CLI ou login Azure. Testes usam SQL Server temporário, sem Azure SQL. Não copie a connection string local para Production nem habilite migrations automáticas nesse ambiente.

Production usa Managed Identity e migrations explícitas conforme a [MEL021](improvements/MEL021-publicacao-azure.md) e o [procedimento Azure](../../infra/azure/README.md). Consulte também a [arquitetura](../architecture/architecture.md). MEL027/CD e UC035 permanecem Planejado; MEL014 será o fechamento posterior com o Manual do Usuário. Este guia cobre a preparação técnica local.
