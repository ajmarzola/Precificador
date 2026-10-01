# Review MVP — Primeiro uso, autenticação e acesso

**Data:** 2026-09-16  
**Origem:** primeiro teste manual do MVP após conclusão da UC025  
**Objetivo:** registrar achados para correção/especificação futura sem interromper a fila funcional atual.

## Decisão preservada

O modelo atual permanece:

~~~text
UsuarioAplicacao N:N Empresa
        via UsuarioEmpresa
~~~

Não simplificar o banco para 1:1.

Um usuário pode possuir zero, um ou vários vínculos ativos; uma Empresa pode possuir vários usuários. A Empresa Ativa continua sendo o contexto operacional da sessão, conforme FT002.

## Achados

### RV001 — Banco local não migrado permite chegar a erro técnico no Login

**Classificação:** bug / primeiro uso  
**Rastreamento:** MEL011

Sintoma observado:

~~~text
SQLite Error 1: 'no such table: AspNetUsers'
~~~

O erro ocorre quando a aplicação é executada contra um arquivo SQLite que ainda não recebeu as migrations do projeto e o fluxo de Login consulta o ASP.NET Core Identity.

A MEL011 consolidou uma decisão posterior: **em `Development`, as migrations passam a ser aplicadas automaticamente no startup antes do primeiro request**. Nos demais ambientes, a aplicação continua exigindo migration explícita.

Também deve ser considerado que:

~~~text
Data Source=precificador.db
~~~

é caminho relativo e pode gerar confusão sobre qual arquivo está sendo usado conforme o diretório de execução.

### RV002 — Menus operacionais aparecem para usuário anônimo

**Classificação:** UX  
**Rastreamento:** MEL012

Hoje o layout apresenta links como Insumos, Produtos e Configurações mesmo sem autenticação.

As páginas continuam protegidas; portanto não é falha de autorização. A correção é de navegação/apresentação.

### RV003 — Falta guia de execução e teste local

**Classificação:** Documentação / DX  
**Rastreamento:** MEL013

Criar documentação única para preparar ambiente local, aplicar migrations, executar Setup, criar primeiro usuário, autenticar e reiniciar uma base de desenvolvimento quando necessário.

### RV004 — Administração de usuários e vínculos ainda não existe

**Classificação:** funcionalidade faltante  
**Rastreamento:** UC031 — Administrar usuários e vínculos com Empresas

FT002 implementou a fundação de autenticação/multiempresa, mas deliberadamente deixou CRUD/administração de usuários fora do escopo.

A arquitetura N:N já existe e deve ser preservada.

## Segunda rodada — uso autenticado

### RV005 — Apresentação monetária inconsistente

**Classificação:** UX / consistência de apresentação  
**Rastreamento:** MEL015

Preços, totais e valores monetários devem ter apresentação padronizada em duas casas decimais e cultura pt-BR.

A regra é apenas de exibição. Cálculos continuam com precisão integral.

Custos unitários técnicos por `g/ml/m` são exceção deliberada e devem manter precisão suficiente para não distorcer valores pequenos.

### RV006 — Entrada decimal brasileira falha no Preço do Insumo

**Classificação:** bug  
**Rastreamento:** MEL015

`PrecoCompra` usa binding direto para `decimal` e rejeitou valor com vírgula decimal no teste manual.

Como `PrecoPrateleira` possui desenho semelhante, MEL015 deve revalidar todos os inputs financeiros diretos, não apenas a tela em que o problema foi observado.

### RV007 — Vocabulário do preço do Insumo sugere transação em vez de embalagem

**Classificação:** correção conceitual  
**Rastreamento:** MEL016

Substituir na linguagem do usuário:

~~~text
Quantidade comprada       -> Quantidade por embalagem
Preço total da compra     -> Preço por embalagem
~~~

O cálculo e os dados persistidos permanecem semanticamente os mesmos.

A terminologia deve ser coerente também nas consultas/histórico e documentação.

### RV008 — Data de referência abre vazia no registro de preço

**Classificação:** UX / default funcional  
**Rastreamento:** MEL016

No caso comum, a Data de referência deve iniciar preenchida com a data operacional atual da Empresa:

~~~text
IDataOperacionalEmpresa.Hoje
~~~

O usuário continua podendo alterar a data conforme UC005.

### RV009 — Configurações precisam de ajuda contextual

**Classificação:** UX / explicabilidade  
**Rastreamento:** MEL017

Adicionar ajuda contextual às configurações de precificação, com atenção especial ao `Incremento comercial de arredondamento`.

A mesma estratégia deve abranger Valor/hora, Tarifa de energia, Margem padrão e Reserva comercial.


## Terceira rodada — custos, catálogo e continuidade do fluxo

### RV010 — Preço vigente/custo calculado contaminados por parsing incorreto

**Classificação:** bug bloqueante de teste  
**Rastreamento:** MEL015

Cenário observado:

~~~text
Quantidade por embalagem = 200 g
Preço informado = 20,99
Quantidade usada na Ficha = 50 g
~~~

O valor foi persistido como `2099`, e por isso:

~~~text
2099 / 200 * 50 = 524,75
~~~

O cálculo UC018 está matematicamente coerente com o dado persistido. O defeito raiz é a interpretação/persistência do valor monetário pt-BR.

Depois da correção, o mesmo cenário deve usar `20,99` e produzir:

~~~text
20,99 / 200 * 50 = 5,2475
~~~

antes da formatação visual.

A tela de Detalhes do Insumo também deve apresentar o preço vigente em formato monetário coerente.

### RV011 — Categorias de Produto precisam ser padronizadas

**Classificação:** funcionalidade faltante  
**Rastreamento:** UC032 — Administrar categorias de Produto

Hoje `Produto.Categoria` é texto livre opcional.

A evolução desejada é ter cadastro tenant-aware de Categorias e seleção no Produto, evitando variações de grafia e permitindo uso estruturado em filtros/coleções.

A migração do texto livre atual para relacionamento deverá ser especificada antes da implementação.

### RV012 — Coleções precisam representar períodos comerciais

**Classificação:** funcionalidade faltante  
**Rastreamento:** UC033 — Administrar coleções

Coleção deve permitir ao menos:

- identificação/nome;
- data de lançamento/início;
- data de finalização;
- Categorias de Produto envolvidas;
- isolamento por Empresa.

Regras de sobreposição, encerramento e obrigatoriedade das datas ficam para especificação.

### RV013 — Produtos devem poder participar de Coleções

**Classificação:** funcionalidade faltante  
**Rastreamento:** UC034 — Vincular Produtos a Coleções

O relacionamento deve permitir acompanhar os períodos em que um Produto pertence/é destaque em uma Coleção.

A cardinalidade e a eventual vigência específica do vínculo deverão ser especificadas; não assumir 1:1.

### RV014 — Cadastro deve continuar no detalhe da entidade

**Classificação:** UX / fluxo  
**Rastreamento:** MEL018

Após criar Insumo ou Produto com sucesso, redirecionar para os respectivos Detalhes, permitindo continuidade imediata do processo.

### RV015 — Rendimento inicial da Ficha Técnica

**Classificação:** UX / default  
**Rastreamento:** MEL019

Produto sem Ficha deve abrir o formulário com `Rendimento = 1`, sem persistir nada no GET.

### RV016 — Valores consolidados devem parecer moeda

**Classificação:** UX / apresentação  
**Rastreamento:** MEL015

`Custo total do lote`, `Preço teórico` e `Preço sugerido` devem ser exibidos como valores monetários com duas casas decimais.

Isso não altera a precisão interna nem a regra de arredondamento comercial do Preço sugerido.


## Decisões fechadas — bootstrap, administração e primeiro acesso

A direção original da FT002 é evoluída da seguinte forma.

### Bootstrap da instalação

`/Setup` permanece um fluxo excepcional e de uso único, mas deixa de criar Empresa e usuário vinculado.

O novo objetivo do Setup será:

1. ficar disponível apenas quando ainda não existir Administrador do Sistema;
2. criar o primeiro `UsuarioAplicacao` com autoridade global `SystemAdmin`;
3. não criar `UsuarioEmpresa`;
4. não selecionar Empresa Ativa;
5. bloquear novo bootstrap após existir o primeiro `SystemAdmin`;
6. não criar credenciais padrão em código/migration.

Após o bootstrap, a administração cotidiana ocorre em uma área separada, por exemplo `/Admin`; o Setup não se transforma em painel administrativo permanente.

### Administrador do Sistema

O `SystemAdmin` é uma autoridade global do Precificador e não pertence a uma Empresa.

Direção aprovada:

- usar autorização global compatível com ASP.NET Core Identity para `SystemAdmin`;
- `SystemAdmin` não depende de `UsuarioEmpresa`;
- `SystemAdmin` não recebe Empresa Ativa por ser administrador do sistema;
- a área administrativa global é separada das áreas tenant-owned;
- acesso do `SystemAdmin` a dados comerciais de Empresas não é concedido implicitamente.

### Solicitação pública de acesso

A Home pública poderá oferecer formulário para uma Empresa solicitar acesso ao Precificador.

A solicitação:

- não cria conta utilizável;
- não cria Empresa antes de aprovação;
- não equivale a auto-registro;
- permanece pendente até decisão do `SystemAdmin`.

O `SystemAdmin` poderá aprovar ou recusar a solicitação.

Ao aprovar, o fluxo administrativo deverá criar a Empresa e definir seu primeiro Administrador da Empresa.

### Perfis no vínculo UsuarioEmpresa

A arquitetura N:N é preservada:

~~~text
UsuarioAplicacao N:N Empresa
        via UsuarioEmpresa
~~~

A autoridade dentro de uma Empresa pertence ao vínculo, permitindo que o mesmo usuário tenha perfis diferentes em Empresas distintas.

Conjunto mínimo previsto:

~~~text
Administrador
Operacional
~~~

Exemplo válido:

~~~text
Usuario X
- Empresa A => Administrador
- Empresa B => Operacional
~~~

O `SystemAdmin` é distinto desses perfis e não depende de vínculo empresarial.

### Administrador da Empresa

O Administrador da Empresa pode administrar somente vínculos da própria Empresa.

Ele poderá, conforme UC031:

- convidar usuário para a Empresa;
- vincular usuário já existente;
- desvincular usuário da Empresa;
- consultar usuários vinculados à Empresa;
- atribuir perfil permitido dentro da Empresa.

Ele não poderá:

- administrar outra Empresa;
- remover a conta global do usuário;
- conhecer ou definir senha do usuário;
- redefinir diretamente a credencial global;
- enxergar vínculos de outras Empresas apenas por ser administrador local.

Desvincular um usuário da Empresa A não afeta vínculos ativos que ele possua com outras Empresas.

### Convite e ativação de conta

Não usar senha temporária enviada por e-mail.

O fluxo aprovado usa token de uso único:

1. uma ação administrativa cria/localiza o `UsuarioAplicacao`;
2. cria o vínculo necessário quando aplicável;
3. emite token de ativação/convite com validade limitada;
4. envia link ao e-mail do usuário;
5. o usuário define a própria senha;
6. não existe troca obrigatória de senha no primeiro login, pois a senha inicial já foi escolhida pelo próprio usuário.

Se o e-mail já pertencer a um `UsuarioAplicacao` existente:

- não criar conta duplicada;
- não alterar senha existente;
- criar apenas o novo vínculo permitido;
- enviar convite/aviso compatível com aceitação do vínculo, conforme especificação da UC correspondente.

### Recuperação de senha

Recuperação de senha passa a fazer parte da linha de trabalho de acesso.

Direção aprovada:

- recuperação usa token enviado ao e-mail do próprio usuário;
- o próprio usuário escolhe a nova senha;
- `SystemAdmin` pode disparar/reemitir o fluxo de recuperação, mas não escolhe nem conhece a senha;
- Administrador da Empresa não recebe poder direto sobre a credencial global de um usuário.

### Ciclo de vida da Empresa

A administração global deverá distinguir administração de Empresa de exclusão física de dados.

Direção inicial:

- `SystemAdmin` cria Empresa apenas após aprovação;
- pode trocar o Administrador da Empresa;
- deve existir proteção contra Empresa ficar sem Administrador válido;
- suspensão/reativação/encerramento devem ser preferidos a hard delete;
- exclusão física definitiva não deve ser tratada como operação comum sem especificação própria.

## Linha de trabalho criada

As decisões acima são divididas para evitar concentrar todo o fluxo na UC031:

1. **FT003 — Fundação de administração e autorização**
   - novo bootstrap do `SystemAdmin`;
   - autoridade global do sistema;
   - perfil no `UsuarioEmpresa`;
   - policies/base de autorização administrativa.

2. **UC038 — Solicitar acesso ao Precificador**
   - formulário público;
   - solicitação pendente;
   - sem auto-registro e sem criação antecipada de Empresa.

3. **UC040 — Ativar conta e recuperar acesso**
   - envio de e-mail;
   - tokens de ativação/convite;
   - definição inicial da senha pelo usuário;
   - recuperação de senha por token.

4. **UC039 — Administrar Empresas e solicitações de acesso**
   - área do `SystemAdmin`;
   - aprovar/recusar solicitações;
   - criar Empresa após aprovação;
   - definir/trocar Administrador da Empresa;
   - suspender/reativar/encerrar Empresa conforme regras a detalhar.

5. **UC031 — Administrar usuários e vínculos com Empresas**
   - Administrador da Empresa administra somente o próprio tenant;
   - convite/vínculo/desvínculo;
   - usuário pode pertencer a várias Empresas;
   - perfil pertence ao vínculo `UsuarioEmpresa`.

A fila normativa e os gates ficam no backlog.

## Ordem recomendada para tratar os achados

Os itens de primeiro uso não precisam interromper imediatamente UC028–UC030, mas devem ser resolvidos antes de considerar o MVP pronto para entrega a terceiros.

Prioridade sugerida:

1. MEL011 — diagnóstico/primeiro uso com banco local;
2. MEL013 — guia de execução local;
3. MEL012 — navegação anônima;
4. especificar UC031 após decidir autorização e fluxo de criação de credenciais.

## Critério para a review

Novos achados dos testes manuais devem ser classificados como:

- **Bug** — comportamento objetivamente incorreto;
- **UX** — comportamento funcional, porém confuso ou inadequado para uso;
- **Documentação / DX** — dificuldade de executar, configurar, testar ou compreender o ambiente;
- **Regra faltante** — situação de negócio sem comportamento definido;
- **Funcionalidade faltante** — capacidade necessária ainda não coberta;
- **Melhoria** — evolução desejável, mas não necessária para correção/MVP.

Este documento é registro de review. Estado, prioridade e ordem normativa permanecem no backlog.
