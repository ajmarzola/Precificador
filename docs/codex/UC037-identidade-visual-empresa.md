# Instrução Codex — UC037 Identidade visual da Empresa

Você está implementando a UC037 no repositório ajmarzola/Precificador.

## Fonte normativa

Leia integralmente:

1. AGENTS.md;
2. docs/use-cases/UC037-configurar-identidade-visual-empresa.md;
3. docs/development/foundation-multiempresa-auth.md;
4. docs/use-cases/UC031-administrar-usuarios-vinculos.md;
5. docs/business/business-rules.md, RN084–RN087;
6. docs/architecture/adr/ADR-015-logo-empresa-sql.md;
7. docs/development/testing-strategy.md;
8. docs/development/definition-of-done.md;
9. implementação atual de Empresa, EmpresaContext, PrecificadorDbContext, _Layout, Dashboard e autorização AdministradorEmpresa.

## Branch

Use:

~~~text
feat/uc037-identidade-visual-empresa
~~~

Parta da master após o merge da documentação UC037.

## Modelo

Criar IdentidadeVisualEmpresa : IEntidadeEmpresa, 1:1 por EmpresaId, separada de Empresa.

Campos:

~~~text
EmpresaId
CorPrimaria
LogoConteudo
LogoContentType
~~~

Ausência de linha = #0D6EFD + sem logo.

Não faça backfill.

## Cor

Aceitar apenas #RRGGBB e persistir uppercase.

Não aceite CSS livre.

Use a cor apenas como acento visual controlado pela aplicação.

## Logo

Somente PNG/JPEG.

Máximo 524288 bytes.

Detecte pelo magic number; não confie em extensão, FileName ou ContentType do cliente.

Persistir ContentType calculado pelo servidor.

Não grave em disco/wwwroot.

## Banco

Criar IdentidadesVisuaisEmpresas com PK/FK EmpresaId, Restrict, checks de cor/logo/tamanho.

Adicionar DbSet, GQF e isolamento IEntidadeEmpresa.

Sem alterações nas migrations históricas.

## Autorização

/Configuracoes/IdentidadeVisual exige AdministradorEmpresa.

Operacional apenas consome a identidade aplicada ao layout.

SystemAdmin não recebe branding tenant.

## Escrita

GET não cria linha.

POST salvar:
- valida cor/upload;
- preserva logo se nenhum upload/remover;
- substitui com novo arquivo;
- remove quando RemoverLogo;
- PRG.

Restaurar padrão remove a linha.

Proteja a corrida da primeira criação; prefira transação curta + sp_getapplock por Empresa, seguindo padrões existentes.

## Serviço de apresentação

Crie serviço scoped para obter a identidade efetiva do tenant.

Não altere EmpresaContext/session.

No layout, não consulte branding para anônimo/SystemAdmin/sem Empresa Ativa.

Não carregue LogoConteudo só para decidir cor/TemLogo.

## Logo endpoint

Criar GET /Empresa/Logo, protegido por EmpresaAtiva.

Não receba EmpresaId.

Retorne apenas logo do tenant atual.

Sem logo => 404.

Headers:

~~~text
X-Content-Type-Options: nosniff
Cache-Control: no-store
~~~

## UI

Admin vê link Identidade visual.

Aplicar identidade a todos os usuários tenant:
- acento no navbar;
- logo/nome no contexto tenant;
- cabeçalho do Dashboard.

Preserve texto Precificador.

Não altere cards/cálculos/filtros do Dashboard.

Home pública, /Admin e _LayoutErro permanecem neutros.

## Upload

Use multipart/request limit compatível com 512 KiB.

Input: CorPrimaria, Logo, RemoverLogo.

Upload + RemoverLogo simultâneos => erro.

## Testes obrigatórios

Cubra:
- domínio/cor;
- magic number PNG/JPEG;
- falso ContentType/extensão;
- limite de tamanho;
- migration clean/upgrade sem backfill;
- GQF/guard cross-tenant;
- autorização Admin/Operacional/SystemAdmin;
- salvar/substituir/preservar/remover/restaurar;
- primeira criação concorrente;
- aplicação Admin/Operacional;
- troca multiempresa;
- endpoint logo/headers;
- Home/Admin/Error neutros;
- regressão Dashboard/precificação.

## Sem antecipação

Não implementar Blob Storage, CDN, tema completo, SVG, branding de e-mail, UC035 ou MEL027.

## Documentação ao concluir

- UC037 -> Concluído;
- backlog -> Concluído;
- RN084–RN087 e ADR-015 preservados;
- próxima fila: MEL027 -> UC035 -> MEL013 -> MEL014.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test tests/Precificador.Tests.Unit/Precificador.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/Precificador.Tests.Integration/Precificador.Tests.Integration.csproj --configuration Release --no-build
git diff --check
~~~

Não faça merge em master.