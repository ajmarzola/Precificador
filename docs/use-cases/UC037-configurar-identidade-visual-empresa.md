# UC037 — Configurar identidade visual da Empresa

- **Área funcional:** Administração tenant / apresentação
- **Dependências funcionais:** FT002, UC031
- **Estado:** Concluído
- **Alteração de domínio/schema:** sim
- **Migration:** sim
- **Autorização de configuração:** AdministradorEmpresa
- **Consumo da identidade:** qualquer usuário com Empresa Ativa
- **Regras principais:** RN084 a RN087

## Objetivo

Permitir que o Administrador da Empresa configure uma identidade visual mínima e segura para o tenant ativo, composta por:

- cor primária de identidade;
- logo opcional.

A identidade deve aparecer no contexto autenticado da Empresa sem alterar a Home pública, a área global do SystemAdmin, o layout de erro da MEL025 ou regras de negócio/precificação.

## Princípio central

A identidade visual pertence à Empresa, não ao usuário.

~~~text
Empresa A -> identidade A
Empresa B -> identidade B
~~~

O mesmo usuário, ao trocar de Empresa Ativa, deve passar a ver imediatamente a identidade do novo tenant.

Não armazenar identidade visual em UsuarioAplicacao ou UsuarioEmpresa.

## Modelo de dados

Criar entidade tenant-owned 1:1:

~~~text
IdentidadeVisualEmpresa : IEntidadeEmpresa
- EmpresaId : int
- CorPrimaria : string
- LogoConteudo : byte[]?
- LogoContentType : string?
~~~

A PK é EmpresaId.

A FK aponta para Empresas.Id com ON DELETE RESTRICT.

Não adicionar colunas de logo/cor diretamente em Empresa, evitando carregar conteúdo binário em consultas administrativas e de autenticação que usam Empresa.

## Configuração opcional e fallback

A ausência de linha em IdentidadesVisuaisEmpresas representa configuração padrão.

~~~text
CorPrimaria = #0D6EFD
Logo = ausente
~~~

Não criar backfill para Empresas existentes.

Nova Empresa também não precisa receber linha de identidade visual durante UC039.

A primeira alteração feita pelo Administrador cria a linha.

A ação Restaurar padrão remove a linha e volta ao fallback.

## Cor primária

Formato aceito: #RRGGBB.

Validação:

- obrigatória ao salvar configuração;
- exatamente 7 caracteres;
- primeiro caractere #;
- seis dígitos hexadecimais;
- aceitar caixa baixa/alta;
- persistir normalizada em caixa alta.

Exemplos válidos: #0D6EFD, #112233 e #aabbcc, persistido como #AABBCC.

Rejeitar red, #FFF, valor sem #, caracteres não hexadecimais, alpha de oito dígitos ou qualquer conteúdo CSS como url(...), var(...) ou declarações adicionais.

A cor será usada apenas como valor validado de variável CSS controlada pela aplicação. Não aceitar CSS arbitrário.

## Uso da cor

A cor é um acento visual, não um tema completo.

Aplicar, no mínimo:

- destaque/borda do navbar no contexto tenant;
- cabeçalho de identidade no Dashboard;
- preview da página de configuração.

Não sobrescrever globalmente toda a paleta Bootstrap.

Não usar a cor customizada como única fonte de contraste para textos críticos nem depender dela para transmitir situação, erro ou sucesso.

## Logo

Logo é opcional.

Formatos aceitos no MVP: PNG e JPEG.

Não aceitar SVG, GIF, WebP, PDF, ICO, HTML ou outro formato.

Tamanho máximo lógico: 512 KiB = 524288 bytes.

O request multipart deve ter limite de transporte compatível, sugerido em até 1 MiB.

## Validação do arquivo

Não confiar em extensão, FileName ou ContentType enviado pelo navegador.

Validar assinatura binária.

PNG:

~~~text
89 50 4E 47 0D 0A 1A 0A
~~~

JPEG:

~~~text
FF D8 FF
~~~

O ContentType persistido deve ser definido pelo servidor após detecção: image/png ou image/jpeg.

Ignorar FileName para persistência. Não gravar arquivo em disco.

## Persistência do logo

Persistir bytes no Azure SQL/SQL Server:

~~~text
LogoConteudo -> varbinary(max)
LogoContentType -> nvarchar(20)
~~~

Motivos:

- App Service F1 não deve depender de filesystem local persistente;
- o piloto possui baixo volume;
- limite de 512 KiB mantém custo previsível;
- não introduzir Blob Storage apenas para esta entrega.

Ver ADR-015.

## Integridade dos campos do logo

Estados válidos:

~~~text
LogoConteudo = null
LogoContentType = null
~~~

ou ambos preenchidos, com LogoContentType em image/png ou image/jpeg.

Adicionar check constraint equivalente e constraint de tamanho com DATALENGTH(LogoConteudo) <= 524288 quando houver logo.

## Domínio

Operações conceituais:

~~~text
IdentidadeVisualEmpresa.Criar(empresaId, corPrimaria)
AtualizarCor(string corPrimaria)
DefinirLogo(byte[] conteudo, string contentType)
RemoverLogo()
~~~

Validar EmpresaId positivo.

A atualização inválida não deve deixar o objeto parcialmente alterado.

É aceitável manter a detecção de assinatura binária em serviço Web/aplicação, enquanto a entidade reforça tamanho máximo, ContentType permitido e consistência bytes/content type.

## Tenant isolation

IdentidadeVisualEmpresa participa de DbSet, Global Query Filter e guard central via IEntidadeEmpresa.

EmpresaId sempre vem de EmpresaContext.

Nenhum formulário aceita EmpresaId.

SystemAdmin não recebe identidade visual tenant apenas por ser autoridade global.

## Superfície de configuração

~~~text
GET  /Configuracoes/IdentidadeVisual
POST /Configuracoes/IdentidadeVisual
POST /Configuracoes/IdentidadeVisual?handler=RestaurarPadrao
~~~

A página exige AdministradorEmpresa, além da Empresa Ativa implícita.

Operacional não pode configurar. SystemAdmin sem Empresa Ativa não pode configurar.

## Input

~~~text
CorPrimaria
Logo
RemoverLogo
~~~

Logo é IFormFile opcional e RemoverLogo é bool.

Regras:

- nenhum arquivo + RemoverLogo=false -> preservar logo atual;
- arquivo válido + RemoverLogo=false -> substituir logo;
- nenhum arquivo + RemoverLogo=true -> remover logo;
- arquivo informado + RemoverLogo=true -> erro de validação.

Mensagem sugerida: Envie um novo logo ou escolha remover o logo atual, não as duas opções.

## GET de configuração

Exibir:

- nome da Empresa;
- cor atual efetiva;
- preview da cor;
- logo atual, quando existir;
- aviso quando estiver usando padrão;
- formulário de alteração;
- ação Restaurar padrão.

Se não houver linha persistida, exibir #0D6EFD, nenhum logo e status Padrão do Precificador.

GET não cria registro.

## Salvar

POST válido:

1. revalidar AdministradorEmpresa;
2. obter EmpresaId do contexto;
3. validar cor;
4. validar arquivo/remover logo;
5. carregar ou criar IdentidadeVisualEmpresa;
6. aplicar alterações;
7. persistir;
8. PRG para a mesma página;
9. mensagem de sucesso.

Mensagem: Identidade visual atualizada com sucesso.

## Restaurar padrão

POST antiforgery.

Se houver IdentidadeVisualEmpresa, remover a linha. Se não houver, operação idempotente.

Resultado efetivo: #0D6EFD e sem logo.

Mensagem: Identidade visual restaurada para o padrão.

A exclusão física aqui é permitida porque a entidade representa apenas configuração atual, sem requisito de histórico.

## Concorrência na primeira configuração

Duas gravações simultâneas para uma Empresa sem linha não podem produzir HTTP 500 por PK duplicada.

Implementação deve serializar a escrita por Empresa ou tratar explicitamente a corrida de criação.

Opção recomendada, coerente com padrões já existentes:

~~~text
execution strategy
+ transação SQL
+ sp_getapplock
resource = Precificador.UC037.Empresa.<EmpresaId>
owner = Transaction
~~~

Não há SMTP nem operação longa dentro do lock.

Sem requisito de histórico/merge de edições simultâneas; última gravação serializada pode prevalecer.

## Navegação

AdministradorEmpresa deve enxergar link Identidade visual.

Operacional não.

O link pode ficar próximo de Configurações e Usuários.

Ocultar link não substitui autorização.

## Aplicação da identidade no layout

A identidade é aplicada a todos os usuários com Empresa Ativa, independentemente de perfil.

Criar serviço de apresentação scoped, por exemplo IdentidadeVisualEmpresaAtual.

Responsabilidades:

- obter configuração da Empresa Ativa;
- aplicar fallback quando não há linha;
- informar CorPrimaria efetiva;
- informar se existe logo.

O serviço não deve alterar sessão.

Não adicionar CorPrimaria/LogoConteudo ao EmpresaContext, evitando cache obsoleto ao trocar ou editar Empresa.

## Consulta no layout

Não consultar identidade visual quando:

- usuário é anônimo;
- usuário é SystemAdmin;
- não existe Empresa Ativa.

No contexto tenant autenticado, uma consulta leve por request é aceitável para o MVP.

Não carregar LogoConteudo na consulta usada apenas para descobrir cor/TemLogo. Projetar somente os campos necessários.

## CSS custom property

É aceitável renderizar variável controlada:

~~~text
--empresa-cor-primaria: #RRGGBB
~~~

somente com valor previamente validado e normalizado.

Não interpolar qualquer outro texto fornecido pelo usuário.

O CSS deve usar a variável apenas em propriedades controladas pela aplicação.

## Marca do produto

Preservar a marca Precificador.

Não substituir o nome do produto pelo nome da Empresa.

No navbar tenant, é permitido exibir Precificador + logo da Empresa + nome da Empresa/Empresa ativa, sem remover a identificação do sistema.

## Dashboard

Adicionar cabeçalho de identidade no Dashboard:

- logo, se existir;
- nome da Empresa;
- acento com CorPrimaria;
- título Dashboard permanece.

Não alterar cards, cálculos ou filtros da UC028–UC030.

## Logo no layout

Exibir logo somente quando houver logo persistido.

Dimensões de apresentação devem ser limitadas por CSS, por exemplo 32px no navbar e 64–80px no Dashboard, usando object-fit: contain.

Não usar dimensões do arquivo para definir layout.

Alt text: Logo da <Nome da Empresa>, com encoding normal do Razor.

## Endpoint do logo

Criar endpoint tenant-aware:

~~~text
GET /Empresa/Logo
~~~

ou rota equivalente.

Requisitos:

- policy EmpresaAtiva;
- sem EmpresaId no request;
- carrega somente logo da Empresa Ativa;
- 404 quando não existe logo;
- retorna ContentType persistido/detectado;
- envia X-Content-Type-Options: nosniff;
- envia Cache-Control: no-store.

Não permitir acesso ao logo de outro tenant por id.

## Cache do logo

Usar Cache-Control: no-store.

Motivo: a URL não contém EmpresaId, o usuário pode alternar entre Empresas e não deve reaproveitar logo da Empresa anterior no browser. O volume do piloto é baixo.

Otimização com ETag/versionamento pode ser futura.

## Home pública

MEL026 permanece neutra.

Não aplicar logo/cor de Empresa na rota / para visitante anônimo.

Não carregar qualquer tenant na Home pública.

## SystemAdmin

A área /Admin mantém identidade global do Precificador.

Não aplicar branding tenant em /Admin, Setup, Login ou páginas de ativação/recuperação quando não há Empresa Ativa.

## MEL025

O layout de erro continua independente.

Não injetar IdentidadeVisualEmpresaAtual em _LayoutErro.

Erro 500 deve continuar renderizando mesmo com banco indisponível.

## Segurança do upload

POST exige antiforgery.

Configurar limite de request/multipart compatível com o máximo definido.

Rejeitar arquivo vazio, >512 KiB ou assinatura desconhecida.

Não gravar temporariamente em wwwroot, não construir path com FileName e não disponibilizar URL pública permanente de upload.

## Mensagens sugeridas

~~~text
Selecione uma cor válida no formato #RRGGBB.
O logo deve possuir no máximo 512 KiB.
O logo deve ser um arquivo PNG ou JPEG válido.
Envie um novo logo ou escolha remover o logo atual, não as duas opções.
~~~

## Persistência / migration

Criar tabela IdentidadesVisuaisEmpresas com:

~~~text
EmpresaId int NOT NULL PK
CorPrimaria nvarchar(7) NOT NULL
LogoConteudo varbinary(max) NULL
LogoContentType nvarchar(20) NULL
~~~

FK EmpresaId -> Empresas.Id com ON DELETE RESTRICT.

Checks equivalentes:

- CorPrimaria no formato #RRGGBB;
- LogoConteudo/LogoContentType ambos null ou ambos preenchidos;
- LogoContentType permitido quando logo existe;
- DATALENGTH(LogoConteudo) <= 524288.

Sem seed/backfill.

## Migration upgrade

Validar upgrade pós-MEL026:

- Empresas existentes permanecem intactas;
- nenhuma linha de identidade é criada automaticamente;
- ausência de linha usa fallback em runtime;
- Identity, produtos, precificação e demais dados preservados.

Não editar migrations históricas.

## Sem alteração de EmpresaContext

EmpresaContext continua armazenando somente EmpresaId, Nome e TimeZoneId.

Não adicionar logo ou cor à sessão.

Isso garante que salvar identidade visual seja refletido no próximo request sem precisar reescrever sessão.

## Regras de negócio

### RN084 — Identidade visual pertence à Empresa Ativa

A identidade visual é tenant-owned e nunca pertence ao usuário.

Somente dados da Empresa Ativa podem ser lidos/aplicados na experiência tenant.

Ausência de configuração usa o padrão do Precificador.

### RN085 — Somente AdministradorEmpresa configura identidade

Todos os usuários do tenant consomem a identidade visual, mas somente AdministradorEmpresa pode criar, alterar, remover logo ou restaurar o padrão.

SystemAdmin não ganha autoridade tenant implícita.

### RN086 — Logo é conteúdo binário controlado e persistido no banco

Logo aceita somente PNG/JPEG válido, com até 512 KiB, detectado por assinatura binária e persistido no SQL Server/Azure SQL.

Não usar filesystem local nem confiar em extensão/ContentType do cliente.

### RN087 — Identidade visual é apresentação e não altera domínio comercial

Cor e logo não alteram precificação, Produtos, Insumos, Fichas, Coleções, histórico, autorização ou cálculo.

A cor é acento visual, não CSS arbitrário nem regra de negócio.

## Critérios de aceitação

- **CA01:** existe entidade tenant-owned IdentidadeVisualEmpresa.
- **CA02:** relação com Empresa é 1:1 por EmpresaId.
- **CA03:** identidade não é armazenada em UsuarioAplicacao/UsuarioEmpresa.
- **CA04:** identidade não adiciona bytes/logo diretamente em Empresa.
- **CA05:** ausência de linha usa #0D6EFD.
- **CA06:** ausência de linha significa sem logo.
- **CA07:** migration não faz backfill.
- **CA08:** nova Empresa pode existir sem linha de identidade.
- **CA09:** cor aceita somente #RRGGBB.
- **CA10:** cor é normalizada para uppercase.
- **CA11:** CSS arbitrário é rejeitado.
- **CA12:** PNG válido é aceito.
- **CA13:** JPEG válido é aceito.
- **CA14:** SVG é rejeitado.
- **CA15:** extensão não determina formato.
- **CA16:** ContentType do cliente não determina formato.
- **CA17:** arquivo >512 KiB é rejeitado.
- **CA18:** arquivo vazio é rejeitado.
- **CA19:** FileName não é persistido nem usado como path.
- **CA20:** logo é persistido no banco.
- **CA21:** LogoConteudo/LogoContentType permanecem consistentes.
- **CA22:** FKs usam Restrict.
- **CA23:** GQF protege IdentidadeVisualEmpresa.
- **CA24:** guard tenant protege escrita.
- **CA25:** EmpresaId não é bindado do formulário.
- **CA26:** /Configuracoes/IdentidadeVisual exige AdministradorEmpresa.
- **CA27:** Operacional não configura.
- **CA28:** SystemAdmin não configura sem vínculo tenant.
- **CA29:** GET sem configuração não cria linha.
- **CA30:** GET mostra fallback.
- **CA31:** GET mostra preview do logo atual.
- **CA32:** salvar somente cor preserva logo existente.
- **CA33:** enviar novo logo substitui logo.
- **CA34:** RemoverLogo remove somente o logo.
- **CA35:** upload + RemoverLogo simultâneos são rejeitados.
- **CA36:** Restaurar padrão remove configuração persistida.
- **CA37:** Restaurar padrão é idempotente.
- **CA38:** POSTs usam antiforgery.
- **CA39:** request multipart possui limite adequado.
- **CA40:** corrida da primeira configuração não retorna 500.
- **CA41:** corrida deixa no máximo uma linha 1:1.
- **CA42:** Admin vê link Identidade visual.
- **CA43:** Operacional não vê link.
- **CA44:** identidade é aplicada a Admin e Operacional do tenant.
- **CA45:** usuário multiempresa vê identidade da Empresa Ativa.
- **CA46:** trocar Empresa muda identidade no request seguinte.
- **CA47:** EmpresaContext não recebe campos de branding.
- **CA48:** layout não consulta branding para anônimo.
- **CA49:** layout não consulta branding para SystemAdmin.
- **CA50:** logo aparece no contexto tenant quando configurado.
- **CA51:** ausência de logo não gera imagem quebrada.
- **CA52:** navbar mantém marca Precificador.
- **CA53:** cor aparece como acento do tenant.
- **CA54:** Dashboard mostra identidade sem alterar cards.
- **CA55:** logo usa dimensões CSS controladas.
- **CA56:** endpoint /Empresa/Logo não recebe EmpresaId.
- **CA57:** endpoint exige EmpresaAtiva.
- **CA58:** endpoint retorna apenas logo do tenant atual.
- **CA59:** endpoint sem logo retorna 404.
- **CA60:** endpoint define ContentType correto.
- **CA61:** endpoint envia nosniff.
- **CA62:** endpoint envia no-store.
- **CA63:** troca de Empresa não reutiliza logo cacheado.
- **CA64:** Home pública não recebe branding tenant.
- **CA65:** /Admin não recebe branding tenant.
- **CA66:** _LayoutErro não depende do branding.
- **CA67:** MEL025 continua renderizando sem banco.
- **CA68:** nenhuma regra de precificação muda.
- **CA69:** nenhuma autorização existente é enfraquecida.
- **CA70:** migration clean DB funciona.
- **CA71:** upgrade pós-MEL026 preserva dados.
- **CA72:** zero migrations pendentes após upgrade.
- **CA73:** Build Release fica verde.
- **CA74:** unitários aplicáveis ficam verdes.
- **CA75:** integração SQL Server/Web fica verde.

## Matriz mínima de testes

### Unitários

- criação/cor válida e inválida;
- normalização uppercase;
- PNG/JPEG;
- tamanho e MIME inválidos;
- remover logo;
- atualização inválida atômica;
- EmpresaId não reatribuível;
- detecção por assinatura, incluindo ContentType mentiroso e SVG.

### Persistência/migration

- tabela 1:1;
- PK/FK/checks;
- GQF/guard;
- cross-tenant;
- clean DB;
- upgrade pós-MEL026;
- sem backfill;
- dados preservados;
- zero migration pendente.

### Configuração Web

- Admin GET default;
- salvar cor;
- PNG/JPEG;
- substituir/preservar/remover logo;
- restaurar padrão;
- inputs inválidos;
- arquivo grande/falso;
- antiforgery;
- Operacional/SystemAdmin negados;
- link apenas para Admin.

### Concorrência

Dois Administradores salvam primeira configuração simultaneamente: no máximo uma linha, sem HTTP 500 e configuração final válida.

### Aplicação

- Admin e Operacional veem mesma identidade;
- Empresas A/B distintas;
- troca A -> B muda branding;
- navbar preserva Precificador;
- Dashboard preserva cards.

### Logo endpoint

- tenant com logo -> 200 e bytes/MIME corretos;
- nosniff/no-store;
- sem logo -> 404;
- anônimo -> Login;
- tenant B não obtém logo A.

### Regressão

- Home MEL026;
- Erro MEL025;
- /Admin;
- Login/seleção/logout;
- UC031 autorização;
- Dashboard/precificação;
- cálculos inalterados.

## Fora do escopo

- alterar Nome da Empresa;
- tema completo por Empresa;
- cor secundária;
- tipografia/CSS configurável;
- SVG/favicon/banner;
- imagem de Produto;
- branding da Home pública, e-mail, SystemAdmin ou layout de erro;
- Blob Storage/CDN;
- crop/redimensionamento server-side;
- histórico/auditoria de branding;
- cache/ETag avançado;
- UC035;
- MEL027.

## Definition of Done

UC037 está concluída quando:

- identidade 1:1 tenant-aware existe;
- cor e logo são configuráveis somente por AdministradorEmpresa;
- PNG/JPEG até 512 KiB são validados pelo conteúdo;
- logo fica no SQL Server/Azure SQL;
- fallback funciona sem backfill;
- todos os usuários do tenant consomem identidade da Empresa Ativa;
- troca de Empresa não conserva branding anterior;
- Home/SystemAdmin/Erro permanecem neutros;
- Dashboard/layout exibem branding sem alterar regras comerciais;
- migration clean/upgrade passa;
- RN084–RN087 e ADR-015 estão alinhadas;
- CI completa fica verde;
- backlog marca UC037 como Concluído após implementação.

## Implementação e validação

A configuração é persistida em `IdentidadesVisuaisEmpresas`, pela migration `20261002144803_UC037_IdentidadeVisualEmpresa`, sem seed/backfill e sem alterar migrations históricas. PK/FK por EmpresaId, Restrict e checks reforçam cor uppercase, consistência do logo e limite de 512 KiB.

`ServicoIdentidadeVisualEmpresa` serializa salvar/restaurar com execution strategy, transação Serializable e `sp_getapplock` por Empresa; revalida Empresa e vínculo Administrador dentro da transação. `IdentidadeVisualEmpresaAtual` é scoped e compartilha uma projeção leve por request entre layout, Dashboard e configuração, sem carregar bytes do logo ou alterar sessão.

O multipart fica limitado a 1 MiB, com buffering em memória e leitura lógica limitada a 512 KiB. Assinatura PNG/JPEG determina o MIME, independentemente dos metadados do cliente. `/Empresa/Logo` usa EmpresaAtiva, GQF e headers `nosniff`/`no-store`.

As suítes `IdentidadeVisualEmpresaTests`, `IdentidadeVisualEmpresaPersistenceTests` e `IdentidadeVisualEmpresaPageTests` cobrem domínio, checks, clean/upgrade, isolamento, autorização, ciclo de edição, concorrência, aplicação por perfil, troca de Empresa, logo e neutralidade das superfícies públicas/globais/erro. A suíte completa inclui regressões existentes de Dashboard, precificação e renderização de erros com banco indisponível.

## Branch de implementação

~~~text
feat/uc037-identidade-visual-empresa
~~~

## Commit sugerido

~~~text
feat: adiciona identidade visual da empresa
~~~
