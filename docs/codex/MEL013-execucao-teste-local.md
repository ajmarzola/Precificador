# Instrução Codex — MEL013 Guia de execução e testes locais

Você está implementando a MEL013 no repositório ajmarzola/Precificador.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/development/improvements/MEL013-execucao-teste-local.md;
3. docs/development/backlog.md;
4. README.md;
5. docs/README.md;
6. docs/development/testing-strategy.md;
7. docs/development/definition-of-done.md;
8. docs/development/configuracao-email-conta.md;
9. docs/development/improvements/MEL020-sqlserver.md;
10. docs/development/improvements/MEL021-publicacao-azure.md;
11. docs/development/improvements/MEL028-otimizar-ci-tipo-alteracao.md;
12. infra/azure/README.md;
13. global.json;
14. .config/dotnet-tools.json;
15. src/Precificador.Web/appsettings.Development.json;
16. src/Precificador.Web/Properties/launchSettings.json;
17. src/Precificador.Infrastructure/Persistence/DesignTimePrecificadorDbContextFactory.cs;
18. tests/Precificador.Tests.Integration/Infrastructure/SqlServerTestDatabase.cs;
19. .github/workflows/ci.yml.

A especificação normativa é:

~~~text
docs/development/improvements/MEL013-execucao-teste-local.md
~~~

## Branch

Antes de alterar qualquer arquivo:

1. parta da master atualizada;
2. crie/troque para `docs/mel013-guia-execucao-local`;
3. confirme que a branch atual não é master;
4. somente então edite documentação.

Se não for possível trabalhar nessa branch, não altere arquivos.

## Natureza da entrega

MEL013 é **documentação-only**.

Não altere:

- código C#;
- csproj;
- migrations/snapshot;
- appsettings;
- launchSettings;
- workflow CI;
- scripts Azure;
- comportamento da aplicação.

Se encontrar divergência real entre código e especificação, não adapte código nesta PR; registre o problema no handoff.

## Artefato principal

Criar:

~~~text
docs/development/execucao-teste-local.md
~~~

O guia deve ser operacional e copiável, com comandos corretos a partir da raiz do repositório.

## Conteúdo obrigatório

Organize de forma próxima a:

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

## Estado atual que deve ser respeitado

- .NET 10 / global.json 10.0.400 com latestMinor;
- dotnet-ef vem do tool manifest;
- aplicação usa SQL Server;
- Development Windows usa LocalDB por padrão;
- override usa `ConnectionStrings__Precificador`;
- Development aplica migrations no startup;
- Production não aplica migrations automaticamente;
- Setup cria apenas SystemAdmin;
- Setup depende de `Bootstrap__SystemAdminKey`;
- Empresa real nasce por solicitação pública aprovada;
- SMTP é opcional para startup, mas necessário para completar manualmente ativação de identidade nova;
- testes de integração usam Testcontainers.MsSql + SQL Server 2022;
- CI docs-only da MEL028 não sobe .NET/Testcontainers.

## Comandos

Garanta que o guia contenha comandos canônicos de:

~~~text
dotnet --info
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet run --project src/Precificador.Web/Precificador.Web.csproj --launch-profile https
dotnet ef migrations list ...
dotnet ef database update ...
dotnet test tests/Precificador.Tests.Unit/... --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/... --configuration Release --no-build
git diff --check
~~~

Use paths reais do repositório.

Para comandos multilinha, diferencie sintaxe PowerShell/Bash ou prefira versão em uma linha quando isso reduzir ambiguidade.

## Segurança

Não coloque valores reais de segredo.

Exemplos de variáveis devem usar placeholders.

Não instrua a persistir segredos no repositório.

Como o projeto não possui UserSecretsId versionado, variáveis de ambiente são o caminho principal no guia.

Inclua cuidado específico para PC compartilhado conforme a especificação.

## Reset

Qualquer comando destrutivo deve ficar cercado de aviso explícito:

- somente Development;
- somente banco local descartável;
- nunca Azure/homologação/produção.

Não inclua comandos de exclusão de recursos Azure.

## Testcontainers

Explique que Docker é necessário somente para integração/full gate, não para unitários nem necessariamente para rodar a aplicação quando SQL Server local já existe.

Não adicione docker-compose.

## Primeiro tenant

Não invente seed, senha padrão ou bypass.

Documente o fluxo real:

~~~text
Home anônima -> solicitação
SystemAdmin -> aprovação
Administrador -> link de ativação
Login -> Empresa Ativa/Dashboard
~~~

Sem SMTP, explique a limitação do teste manual, sem propor manipulação de senha no banco.

## Referências

Linke, sem duplicar desnecessariamente:

- configuração SMTP;
- estratégia de testes;
- Azure MEL021;
- workflow do Codex;
- docs de arquitetura quando útil.

## Atualizações ao concluir

- MEL013 -> Concluído;
- backlog -> Concluído;
- README raiz -> link para guia local;
- docs/README -> link para guia local;
- manter MEL027 e UC035 como Planejado;
- manter MEL014 como fechamento posterior.

Após MEL013, a fila pendente deve permanecer:

~~~text
MEL027
UC035
MEL014
~~~

salvo nova decisão humana posterior.

## Validação

Como a entrega é documental:

- confira manualmente paths, nomes de configurações e comandos contra o repositório atual;
- execute `git diff --check`;
- não introduza links quebrados conhecidos;
- confirme que o diff contém somente Markdown;
- confirme que a CI classifica a PR como documentation-only.

Não faça merge em master.