# MEL028 — Otimizar pipeline de CI por tipo de alteração

- **Origem:** observação operacional durante a validação documental da UC028.
- **Classificação:** CI / feedback de PR / eficiência operacional.
- **Prioridade:** alta.
- **Estado:** Especificado.
- **Ordem na fila pendente:** 15.
- **Dependências funcionais:** nenhuma.
- **Gate operacional:** executar após UC028 e antes de UC029.
- **Alteração de domínio/regra de negócio:** não.
- **Alteração de schema:** não.
- **Migration:** não.
- **Relação com CD:** complementar à MEL027; MEL028 otimiza validação de PR, enquanto MEL027 automatizará publicação e manterá validação completa antes do deploy.

## Motivo

A CI atual executa, em toda PR para `master`:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Isso faz com que alterações exclusivamente documentais também:

- restaurem SDK/dependências;
- compilem toda a solution;
- inicializem SQL Server via Testcontainers;
- executem toda a suíte de integração.

Na PR documental da UC028, a execução real do GitHub Actions ficou em aproximadamente dois minutos e concluiu:

~~~text
251 testes unitários
478 testes de integração
729 testes no total
~~~

A captura no aplicativo móvel chegou a exibir mais de 30 minutos em processamento, mas o backend do GitHub já havia finalizado. Portanto, a MEL028 **não parte da premissa de que a suíte completa demora 30–40 minutos**.

O problema real é outro: a suíte completa é executada mesmo quando a mudança não pode afetar runtime, build, persistência ou comportamento da aplicação.

## Objetivo

Reduzir o tempo e o custo de feedback das Pull Requests sem reduzir a cobertura aplicável às alterações de código.

A CI deve distinguir, de forma conservadora:

~~~text
PR somente documental
=> validação rápida

PR com qualquer alteração não documental
=> validação completa
~~~

A MEL028 não remove testes. Ela altera **quando** a suíte completa precisa ser executada.

## Princípio de segurança

A classificação deve ser **fail closed**.

Somente uma alteração comprovadamente documental pode usar o caminho rápido.

Se existir qualquer dúvida, arquivo desconhecido ou alteração fora da whitelist documental:

~~~text
=> executar CI completa
~~~

Não manter blacklist de arquivos "de código". Manter uma whitelist curta de arquivos documentais.

## Whitelist documental

Uma PR é considerada **somente documental** apenas quando todos os arquivos alterados pertencem a:

~~~text
docs/**
*.md
~~~

onde `*.md` representa arquivos Markdown na raiz do repositório.

Exemplos de caminho rápido:

~~~text
docs/use-cases/UCxxx.md
docs/development/backlog.md
docs/features/F005-dashboard-margens.md
README.md
AGENTS.md
~~~

Qualquer outro caminho força CI completa.

Exemplos que **não** são documentais para fins da MEL028:

~~~text
src/**
tests/**
infra/**
.github/**
*.slnx
*.csproj
*.props
*.targets
global.json
.gitignore
scripts/**
~~~

Mesmo que uma alteração em um desses arquivos pareça inofensiva, o pipeline deve escolher o caminho completo.

## Eventos

Preservar:

~~~text
pull_request -> master
push         -> master
~~~

### Pull Request

PR usa classificação por arquivos alterados.

### Push em master

Push em `master` executa sempre a validação completa.

A primeira implementação não otimiza pushes na branch principal. Isso mantém uma validação pós-merge independente do caminho usado pela PR.

MEL027 poderá posteriormente reutilizar ou reorganizar essa validação na esteira de CD, mas não deve ser antecipada aqui.

## Status check obrigatório

O ruleset ativo de `master` exige atualmente o status:

~~~text
build-and-test
~~~

A MEL028 deve preservar esse contexto obrigatório.

É proibido, nesta melhoria:

- remover o job/status `build-and-test`;
- renomeá-lo sem atualizar de forma coordenada o ruleset;
- usar `paths-ignore` no workflow inteiro de forma que o required check deixe de ser publicado em PR documental.

A solução deve fazer o próprio `build-and-test` concluir com sucesso no caminho documental rápido.

Assim, o ruleset continua bloqueando merge quando a CI falhar e não fica pendente esperando um check que nunca iniciou.

## Estrutura do workflow

A implementação pode manter um único job obrigatório, desde que os passos sejam condicionais.

Desenho recomendado:

~~~text
build-and-test
├── checkout
├── classificar alterações
├── git diff --check
│
├── [documentação]
│   └── finalizar com sucesso
│
└── [completo]
    ├── setup .NET
    ├── restore
    ├── build Release
    ├── testes unitários
    └── testes de integração
~~~

O nome/contexto externo continua:

~~~text
build-and-test
~~~

Não é obrigatório criar múltiplos jobs nesta MEL.

## Classificação da PR

Usar os SHAs do evento `pull_request` para comparar base e head.

A implementação deve garantir que ambos os commits estejam disponíveis localmente, por exemplo com checkout suficiente para a comparação.

Conceitualmente:

~~~text
base = github.event.pull_request.base.sha
head = github.event.pull_request.head.sha

arquivos = git diff --name-only base...head

docs_only =
    arquivos não vazio
    AND todos os caminhos pertencem à whitelist documental
~~~

Se a obtenção da lista falhar:

~~~text
docs_only = false
=> CI completa
~~~

Não confiar apenas na mensagem do commit, extensão predominante ou diretório do primeiro arquivo.

## Validação rápida documental

Mesmo no modo documental, executar no mínimo:

~~~text
git diff --check
~~~

A validação deve detectar problemas como whitespace inválido.

Não executar, em PR exclusivamente documental:

- `dotnet tool restore`;
- `dotnet restore`;
- `dotnet build`;
- testes unitários;
- Testcontainers;
- testes de integração.

A MEL028 não introduz linter Markdown obrigatório nem nova dependência externa apenas para justificar o caminho rápido.

## Validação completa

Quando a PR não for exclusivamente documental, preservar:

~~~text
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
~~~

Executar testes em passos explicitamente separados:

~~~text
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj
    --configuration Release
    --no-build

dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj
    --configuration Release
    --no-build
~~~

A separação tem objetivo de:

- mostrar rapidamente se a falha é unitária ou de integração;
- deixar duração e quantidade de cada suíte visíveis;
- evitar que uma falha unitária fique misturada ao startup do SQL Server.

Os testes unitários devem rodar antes dos testes de integração.

## dotnet tool restore

A CI de PR não usa hoje `dotnet ef` depois de `dotnet tool restore`.

Por isso, MEL028 pode remover `dotnet tool restore` do caminho normal da PR, desde que nenhum passo efetivamente dependa dele.

Isso não altera:

- scripts de deploy;
- execução explícita de migrations;
- MEL027;
- comandos locais/documentados que necessitem da ferramenta.

Se durante a implementação for identificada dependência real do tool restore, preservá-lo e registrar a razão.

## SQL Server e Testcontainers

Não alterar a estratégia de integração nesta MEL.

Continuar usando:

~~~text
SQL Server
+
Testcontainers.MsSql
~~~

É proibido otimizar CI substituindo integração por:

- EF InMemory;
- SQLite;
- mocks de persistência;
- banco compartilhado permanente.

A infraestrutura atual inicia um container SQL Server reutilizado pela suíte de integração no processo de testes. MEL028 não muda essa semântica.

## Cobertura

O caminho completo deve continuar executando toda a suíte aplicável.

Baseline observado na especificação:

~~~text
Unitários:   251
Integração:  478
Total:       729
~~~

Esses números são apenas referência temporal e podem crescer.

A implementação não pode:

- apagar testes para reduzir duração;
- adicionar filtros que excluam suítes existentes;
- marcar testes como Skip;
- deixar integração opcional em PR de código.

## Concorrência

Adicionar cancelamento de execuções obsoletas de uma mesma Pull Request.

Objetivo:

~~~text
novo push na mesma PR
=> cancelar CI anterior ainda em andamento
=> manter somente a execução mais recente
~~~

Não é necessário cancelar execuções já iniciadas em `master`.

A configuração deve usar chave de concorrência estável por PR/ref e `cancel-in-progress` somente onde for seguro.

## Timeout

Adicionar timeout explícito ao job completo.

Valor inicial:

~~~text
20 minutos
~~~

A suíte normalmente conclui em poucos minutos. O timeout existe para impedir runner preso indefinidamente por Docker/Testcontainers/rede.

Se o timeout se provar insuficiente em execução legítima, ajustar com evidência; não aumentar preventivamente para valores muito altos.

## Feedback no GitHub

O job deve tornar evidente qual modo foi executado.

Exemplos de saída:

~~~text
CI mode: documentation-only
CI mode: full
~~~

Pode usar log normal ou `GITHUB_STEP_SUMMARY`.

No modo completo, manter passos separados com nomes claros para:

- Restore;
- Build;
- Unit tests;
- Integration tests.

## Relação com MEL027

MEL028 e MEL027 possuem objetivos distintos.

### MEL028 — PR/CI

Responde:

~~~text
esta alteração pode entrar em master?
~~~

Pode usar caminho documental rápido.

### MEL027 — CD

Responderá:

~~~text
o estado integrado de master pode ser publicado?
~~~

Antes de deploy, CD deve executar validação completa, independentemente de a alteração que originou a publicação parecer documental.

MEL028 não cria:

- workflow de deploy;
- login Azure;
- migrations de produção;
- publish;
- smoke remoto;
- rollback.

## Documentação

Atualizar na implementação:

- `.github/workflows/ci.yml`;
- `docs/development/testing-strategy.md`;
- este documento;
- `docs/development/backlog.md`.

Não é necessário alterar documentação funcional de UCs.

## Critérios de aceitação

- **CA01:** workflow continua acionado em PR para `master`.
- **CA02:** workflow continua acionado em push para `master`.
- **CA03:** required status `build-and-test` continua existindo.
- **CA04:** PR somente com `docs/**` usa caminho rápido.
- **CA05:** PR somente com Markdown na raiz usa caminho rápido.
- **CA06:** PR com qualquer arquivo fora da whitelist usa caminho completo.
- **CA07:** falha ao classificar arquivos resulta em caminho completo.
- **CA08:** caminho documental executa `git diff --check`.
- **CA09:** caminho documental não executa restore/build/test .NET.
- **CA10:** caminho documental não inicializa Testcontainers/SQL Server.
- **CA11:** caminho completo executa restore da solution.
- **CA12:** caminho completo executa build Release.
- **CA13:** unitários são executados explicitamente.
- **CA14:** integração SQL Server é executada explicitamente.
- **CA15:** unitários executam antes da integração.
- **CA16:** nenhuma suíte existente é filtrada/removida.
- **CA17:** Testcontainers.MsSql permanece provider de integração.
- **CA18:** push em `master` sempre executa caminho completo.
- **CA19:** alteração em `.github/**` executa caminho completo.
- **CA20:** alteração em `infra/**` executa caminho completo.
- **CA21:** alteração em `src/**` executa caminho completo.
- **CA22:** alteração em `tests/**` executa caminho completo.
- **CA23:** alteração em arquivos de projeto/solution executa caminho completo.
- **CA24:** nova execução da mesma PR cancela execução anterior ainda ativa.
- **CA25:** job possui timeout de 20 minutos.
- **CA26:** log/resumo identifica modo documental ou completo.
- **CA27:** não existe mudança de schema/domain/runtime da aplicação.
- **CA28:** MEL027/CD não é implementada.
- **CA29:** ruleset de `master` continua funcional sem alteração obrigatória do required check.
- **CA30:** documentação de estratégia de testes distingue CI rápida de validação completa.

## Matriz mínima de validação

### Classificação

Validar conceitualmente ou por teste de script:

~~~text
docs/a.md
=> documentation-only

README.md
=> documentation-only

docs/a.md + README.md
=> documentation-only

docs/a.md + src/A.cs
=> full

docs/a.md + .github/workflows/ci.yml
=> full

tests/X.cs
=> full

infra/azure/deploy.ps1
=> full

Precificador.slnx
=> full

arquivo desconhecido
=> full
~~~

Lista vazia/erro de diff:

~~~text
=> full
~~~

### Workflow

Revisar:

- required check publicado no caminho documental;
- required check publicado no caminho completo;
- `git diff --check` falha o job quando necessário;
- passos .NET são pulados somente no modo documental;
- integração continua verde em alteração não documental;
- timeout configurado;
- concurrency configurada.

## Fora do escopo

- reduzir quantidade de testes;
- paralelizar classes de integração;
- shard da suíte;
- cache de imagem Docker;
- substituir Testcontainers;
- usar banco SQL compartilhado;
- coverage report;
- SonarQube;
- lint Markdown;
- análise estática adicional;
- dependabot;
- benchmark;
- Continuous Deployment;
- deploy Azure;
- otimizar testes por domínio/arquivos de código afetados.

Se a suíte completa crescer a ponto de voltar a ser lenta, criar melhoria posterior baseada em medições reais.

## Definition of Done

MEL028 está concluída quando:

- PR documental recebe feedback rápido sem inicializar .NET/SQL Server;
- alteração não documental continua executando build + unitários + integração completos;
- required check `build-and-test` continua protegendo `master`;
- push em `master` continua recebendo validação completa;
- execuções antigas da mesma PR podem ser canceladas;
- timeout evita jobs indefinidamente presos;
- nenhuma cobertura é removida;
- estratégia de testes e backlog estão atualizados;
- CI da própria implementação fica verde;
- MEL027 permanece separada e não implementada.

## Branch de implementação sugerida

~~~text
infra/mel028-ci-por-tipo-alteracao
~~~

## Commit sugerido

~~~text
ci: otimiza validação por tipo de alteração
~~~
