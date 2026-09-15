# UC026 — Consultar configurações de precificação da Empresa

- **Funcionalidade:** F006 — Configurações de Precificação
- **Dependências funcionais:** FT002 e MEL009
- **Alteração de schema:** sim
- **Operação do UC:** consulta read-only

## Objetivo

Introduzir o modelo persistido de configurações de precificação por Empresa e permitir consultar os parâmetros vigentes da Empresa Ativa.

A configuração pertence à Empresa, é tenant-owned e será reutilizada pelos UCs de mão de obra, energia, preço sugerido e decisão comercial.

UC026 cria o modelo e a leitura. Alteração de valores pertence ao UC027.

## Decisão de modelagem

Não adicionar os parâmetros diretamente em `Empresa`.

Criar entidade 1:1 separada:

~~~text
ConfiguracaoPrecificacaoEmpresa
- EmpresaId : int
- ValorHoraTrabalho : decimal?
- TarifaEnergiaKwh : decimal?
- MargemPadrao : decimal?
- IncrementoComercial : decimal?
- ReservaComercialDesconto : decimal
~~~

`EmpresaId` é simultaneamente:

- chave primária da configuração;
- chave estrangeira para `Empresas.Id`;
- identificador tenant-aware.

Não criar `Id` artificial adicional.

A entidade implementa `IEntidadeEmpresa`.

## Motivo da entidade separada

`Empresa` representa identidade, situação e contexto operacional da organização.

Os parâmetros acima pertencem ao domínio econômico/precificação e evoluem em conjunto.

A separação:

- evita inflar `Empresa` com regras econômicas;
- concentra validações comerciais;
- permite evolução independente da configuração;
- mantém relacionamento 1:1 explícito;
- preserva o padrão tenant-aware existente.

## Valores inicialmente configurados

Não existem defaults de negócio aprovados para:

- ValorHoraTrabalho;
- TarifaEnergiaKwh;
- MargemPadrao;
- IncrementoComercial.

Portanto, no primeiro schema esses campos são `nullable`.

`null` significa:

~~~text
Não configurado
~~~

Não substituir silenciosamente por zero.

Somente `ReservaComercialDesconto` possui default normativo já aprovado pela MEL009:

~~~text
ReservaComercialDesconto = 0,10
~~~

equivalente a 10 pontos percentuais.

## Semântica dos campos

### ValorHoraTrabalho

Valor em moeda por hora usado futuramente pelo UC020.

Validação quando informado:

~~~text
ValorHoraTrabalho >= 0
~~~

Zero explicitamente configurado é válido. `null` é diferente de zero.

### TarifaEnergiaKwh

Tarifa monetária por kWh usada futuramente pelo UC021.

Validação quando informada:

~~~text
TarifaEnergiaKwh >= 0
~~~

Zero explicitamente configurado é válido. `null` é diferente de zero.

### MargemPadrao

Fração decimal usada futuramente para pré-preencher a Margem-alvo de novos Produtos.

Validação quando informada:

~~~text
0 <= MargemPadrao < 1
~~~

Exemplo:

~~~text
30% = 0,30
~~~

UC026 apenas consulta o valor. Não alterar ainda `/Produtos/Novo`.

### IncrementoComercial

Incremento monetário usado futuramente pela RN021 para arredondar o Preço sugerido para cima.

Validação quando informado:

~~~text
IncrementoComercial > 0
~~~

`null` significa que o arredondamento comercial ainda não foi configurado.

Zero não é incremento válido.

### ReservaComercialDesconto

Aplicar integralmente RN052/MEL009:

~~~text
0 <= ReservaComercialDesconto < 1
default = 0,10
~~~

UC026 deve exibir essa configuração desde o primeiro schema.

## Criação padrão da configuração

Adicionar fábrica de domínio equivalente a:

~~~text
ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaId)
~~~

Resultado:

~~~text
EmpresaId = empresaId
ValorHoraTrabalho = null
TarifaEnergiaKwh = null
MargemPadrao = null
IncrementoComercial = null
ReservaComercialDesconto = 0,10
~~~

A criação deve validar `empresaId > 0`.

UC026 não precisa introduzir método público de atualização completa; isso pertence ao UC027.

É aceitável centralizar as validações em helpers privados já nesta entidade para reutilização futura.

## Persistência

Criar tabela:

~~~text
ConfiguracoesPrecificacaoEmpresas
~~~

Colunas sugeridas:

~~~text
EmpresaId                    INTEGER NOT NULL PK/FK
ValorHoraTrabalho            decimal(18,6) NULL
TarifaEnergiaKwh             decimal(18,6) NULL
MargemPadrao                 decimal(9,6)  NULL
IncrementoComercial          decimal(18,6) NULL
ReservaComercialDesconto     decimal(9,6)  NOT NULL DEFAULT 0.10
~~~

A precisão pode seguir a convenção EF/SQLite equivalente, desde que preserve esses limites funcionais e não use `double`.

FK:

~~~text
ConfiguracoesPrecificacaoEmpresas.EmpresaId -> Empresas.Id
~~~

Usar `DeleteBehavior.Restrict` para manter coerência com o modelo atual sem exclusão física de Empresa.

## Migration

Criar migration evolutiva, sem editar migrations históricas.

Nome sugerido:

~~~text
AddConfiguracoesPrecificacaoEmpresa
~~~

A migration deve:

1. criar a tabela;
2. criar PK/FK 1:1 por `EmpresaId`;
3. backfill para **todas as Empresas existentes**;
4. preencher ReservaComercialDesconto com `0,10`;
5. deixar os quatro parâmetros sem default aprovado como `NULL`.

Backfill conceitual SQLite:

~~~sql
INSERT INTO ConfiguracoesPrecificacaoEmpresas
    (EmpresaId, ReservaComercialDesconto)
SELECT Id, 0.10
FROM Empresas;
~~~

Não criar valores arbitrários para hora, energia, margem ou incremento.

## Empresas futuras

Após UC026, todo fluxo de aplicação que criar uma nova Empresa deverá também inicializar `ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaId)` na mesma unidade de trabalho.

No estado atual, `/Setup` apenas renomeia a Empresa técnica já existente e portanto recebe a configuração pelo backfill da migration.

Helpers de testes que criam Empresas adicionais devem ser atualizados para inicializar a configuração correspondente quando representarem um fluxo válido pós-UC026.

Não implementar CRUD administrativo de Empresa neste UC.

## Invariante 1:1

Deve existir no máximo uma configuração por Empresa, garantido pela PK `EmpresaId`.

A migration garante uma configuração para cada Empresa existente no momento do upgrade.

Ausência inesperada de configuração para uma Empresa existente é violação de integridade da aplicação; a tela não deve criar configuração durante GET para mascarar o problema.

GET de consulta é read-only.

## DbContext e tenant isolation

Adicionar:

~~~text
DbSet<ConfiguracaoPrecificacaoEmpresa> ConfiguracoesPrecificacaoEmpresas
~~~

Como implementa `IEntidadeEmpresa`, a configuração deve automaticamente participar de:

- Global Query Filter por Empresa Ativa;
- guard central de escrita;
- comportamento `EmpresaIdOuSentinela` quando não há contexto.

Não usar `IgnoreQueryFilters` no fluxo Web.

Não receber `EmpresaId` do request.

## Rota

Criar página:

~~~text
/Configuracoes/Precificacao
~~~

Arquivos esperados:

~~~text
Pages/Configuracoes/Precificacao.cshtml
Pages/Configuracoes/Precificacao.cshtml.cs
~~~

Não criar tela separada para a Reserva comercial.

## Navegação

Adicionar item de navegação principal:

~~~text
Configurações
~~~

apontando para `/Configuracoes/Precificacao`.

Não adicionar botão `Alterar` apontando para rota inexistente. UC027 introduzirá a edição.

## Conteúdo da consulta

A tela deve identificar que os valores pertencem à Empresa Ativa e exibir:

1. Valor da hora de trabalho;
2. Tarifa de energia (R$/kWh);
3. Margem padrão para novos produtos (%);
4. Incremento comercial de arredondamento;
5. Reserva comercial para desconto (%).

Para Reserva comercial, incluir ajuda coerente com MEL009:

~~~text
Percentual reservado acima do preço sugerido antes de formar o desconto de referência.
~~~

## Apresentação de valores não configurados

Para os quatro campos nullable:

~~~text
Não configurado
~~~

Não mostrar:

~~~text
0
0,00
0%
~~~

quando o valor real é `null`.

Zero deve ser mostrado como zero somente quando estiver explicitamente persistido futuramente pelo UC027 para campos onde zero é válido.

## Formatação

Usar cultura pt-BR de forma determinística.

Percentuais são persistidos como fração e apresentados como percentual:

~~~text
0,30 -> 30%
0,075 -> 7,5%
0,10 -> 10%
~~~

Valores monetários/tarifários devem preservar precisão útil, sem arredondamento que altere o valor persistido.

Recomenda-se helper de apresentação específico para configurações, evitando lógica de formatação diretamente na Razor.

## Comportamento de Produto existente

UC026 não altera qualquer Produto.

`MargemPadrao`:

- não recalcula MargemAlvo de Produto existente;
- não sobrescreve MargemAlvo;
- não modifica `/Produtos/Novo` neste UC.

A integração de pré-preenchimento será revalidada quando UC027 fechar o fluxo de configuração.

## Comportamento dos cálculos existentes

UC026 não executa nem altera:

- UC018;
- custo de mão de obra;
- custo de energia;
- Preço sugerido;
- desconto de referência;
- margem atual.

Ela apenas disponibiliza o modelo/configuração que UCs futuros consumirão.

Enquanto um parâmetro obrigatório futuro estiver `null`, o cálculo dependente deve ser considerado incompleto conforme RN017 no UC correspondente.

## MEL009 incorporada

A implementação inicial de `ConfiguracaoPrecificacaoEmpresa` já contém:

~~~text
ReservaComercialDesconto
~~~

com default `0,10`.

Não criar migration futura apenas para adicionar esse campo.

UC026 não calcula `DescontoReferencia` e não cria `RegistroPrecoProduto`.

## Segurança

### Anônimo

Não acessa a página operacional; comportamento padrão de autenticação permanece.

### Usuário autenticado sem Empresa Ativa

Não consulta configuração de nenhuma Empresa.

### Empresa Ativa

A página consulta somente a configuração filtrada pelo tenant atual.

Trocar Empresa Ativa deve trocar os valores exibidos.

### Outro tenant

Configuração da Empresa B não pode ser lida quando Empresa A está ativa.

## GET sem mutação

GET `/Configuracoes/Precificacao`:

- não cria configuração;
- não atualiza default;
- não altera Empresa;
- não altera Produto;
- não altera qualquer histórico;
- não chama `SaveChanges`.

## Domínio

Adicionar `ConfiguracaoPrecificacaoEmpresa` ao Core.

Não adicionar dependência do Core para EF/Web.

Não colocar regras de apresentação na entidade.

## Regras relacionadas

- RN017 — dados obrigatórios ausentes tornam cálculo dependente incompleto;
- RN019 — faixa válida de margem;
- RN021 — incremento comercial futuro;
- RN025 — configurações por Empresa;
- RN026 — precisão sem arredondamento intermediário;
- RN039 — isolamento das configurações;
- RN052 — reserva comercial por Empresa.

## Critérios de aceitação

### CA01

Existe entidade 1:1 `ConfiguracaoPrecificacaoEmpresa` tenant-owned por Empresa.

### CA02

Migration cria uma configuração para cada Empresa existente.

### CA03

Configuração padrão possui quatro parâmetros `null` e ReservaComercialDesconto = 0,10.

### CA04

Não são inventados defaults para hora, energia, margem ou incremento.

### CA05

Página `/Configuracoes/Precificacao` exige autenticação e Empresa Ativa.

### CA06

A página exibe somente configuração da Empresa Ativa.

### CA07

Campos `null` aparecem como `Não configurado`.

### CA08

Reserva padrão aparece como 10%.

### CA09

Percentuais são exibidos em percentual e persistidos como fração.

### CA10

A página inclui o texto de ajuda da reserva comercial.

### CA11

GET não cria nem altera configuração.

### CA12

UC026 não altera Produto/MargemAlvo nem histórico comercial.

### CA13

Configuração participa de GQF e guard tenant-aware.

### CA14

Navegação principal oferece acesso a Configurações.

### CA15

Não existe UI de edição neste UC.

## Matriz de testes

### Unitários

- U1: `CriarPadrao` exige EmpresaId válido;
- U2: `CriarPadrao` deixa ValorHoraTrabalho/TarifaEnergiaKwh/MargemPadrao/IncrementoComercial nulos;
- U3: `CriarPadrao` define ReservaComercialDesconto = 0,10;
- U4: `DefinirEmpresa` não permite reatribuição de tenant após definida.

### Persistência / migration

- P1: migration em banco vazio cria tabela/configuração da Empresa técnica;
- P2: upgrade com Empresas existentes faz backfill de todas;
- P3: backfill mantém quatro parâmetros nulos e reserva 0,10;
- P4: PK EmpresaId impede segunda configuração para a mesma Empresa;
- P5: FK impede configuração para Empresa inexistente;
- P6: GQF com Empresa A não retorna configuração da Empresa B;
- P7: guard central rejeita escrita cross-tenant;
- P8: roundtrip preserva precisão decimal e nulls.

### Web

- W1: página exige autenticação e Empresa Ativa;
- W2: Empresa padrão exibe quatro `Não configurado` e Reserva 10%;
- W3: labels dos cinco parâmetros são exibidos;
- W4: ajuda da Reserva comercial é exibida;
- W5: troca de Empresa Ativa muda a configuração visível sem vazamento;
- W6: Empresa A não exibe valores preparados para Empresa B;
- W7: GET não persiste qualquer alteração;
- W8: navegação principal contém Configurações;
- W9: não existe formulário/botão funcional de alteração neste UC;
- W10: percentuais e números seguem formatação pt-BR determinística.

## Alterações esperadas

### Core

- `ConfiguracaoPrecificacaoEmpresa`.

### Infrastructure

- `DbSet`;
- configuração EF;
- migration `AddConfiguracoesPrecificacaoEmpresa` ou equivalente;
- snapshot atualizado.

### Web

- `/Configuracoes/Precificacao` read-only;
- helper de apresentação se necessário;
- link de navegação.

### Testes

- unidade da entidade;
- migration/persistência/tenant;
- página Web.

## Fora do escopo

- editar configurações (UC027);
- pré-preencher MargemAlvo no cadastro de Produto;
- recalcular Produtos existentes;
- custo de mão de obra;
- energia/equipamentos;
- cálculo de Preço sugerido;
- RegistroPrecoProduto;
- snapshot ReservaComercialReferencia;
- DescontoReferencia;
- histórico/versionamento próprio das configurações;
- CRUD administrativo de Empresa;
- permissões por papel;
- API REST.

## Revalidação da MEL009

A MEL009 foi incorporada nesta especificação desde o primeiro schema:

- campo `ReservaComercialDesconto` existe na entidade inicial;
- é decimal em fração;
- default 0,10;
- faixa normativa 0 <= valor < 1;
- aparece na mesma tela dos demais parâmetros;
- não existe tela própria;
- não participa do Preço sugerido;
- snapshot comercial continua reservado ao UC011.

Portanto, o gate `incorporar MEL009` está satisfeito para UC026.

## Branch sugerida

~~~text
feat/uc026-consultar-configuracoes-precificacao
~~~

## Commit sugerido

~~~text
feat: adiciona configuracoes de precificacao da empresa
~~~
