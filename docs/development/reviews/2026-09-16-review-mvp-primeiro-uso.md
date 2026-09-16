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


## Regra atual de primeiro acesso

O primeiro acesso da **instalação** já possui um bootstrap definido pela FT002:

1. em `Development`, o startup aplica migrations pendentes automaticamente; nos demais ambientes, o banco deve chegar migrado por processo explícito;
2. enquanto não houver usuários Identity, `/Setup` fica disponível;
3. o Setup recebe Nome da Empresa, E-mail, Senha e Confirmação;
4. cria o primeiro `UsuarioAplicacao`;
5. cria um vínculo ativo `UsuarioEmpresa` com a Empresa inicial;
6. após existir usuário, o Setup deixa de ser reutilizável;
7. login com um único vínculo elegível seleciona automaticamente a Empresa;
8. login com múltiplos vínculos exige seleção da Empresa Ativa.

Não criar usuário/senha padrão no código.

## Regra ainda pendente — primeiro acesso de usuários adicionais

A lacuna real é definir como um **segundo ou posterior usuário** passa a ter acesso.

Direção preservada:

- não implementar auto-registro público;
- manter `UsuarioEmpresa` N:N;
- não permitir que a tela de Login crie conta implicitamente;
- cadastro/vínculo deve ocorrer em fluxo administrativo explícito;
- usuário sem vínculo elegível não deve acessar dados tenant-owned;
- usuário com um vínculo elegível recebe a Empresa Ativa automaticamente;
- usuário com vários vínculos elegíveis usa a seleção já existente.

## Questões que UC031 deverá decidir antes da implementação

1. **Quem pode cadastrar outro usuário?**
   - ainda não há role/perfil administrativo formal.

2. **Como o primeiro usuário ganha autoridade administrativa?**
   - role Identity;
   - permissão própria;
   - outra regra explícita.

3. **Como nasce a credencial do novo usuário?**
   - administrador define senha inicial;
   - convite/token para o próprio usuário definir senha;
   - outro mecanismo local-first.

4. **Senha temporária exige troca no primeiro login?**

5. **No cadastro, o novo usuário pode receber um ou vários vínculos?**

6. **Quem pode adicionar/remover vínculos `UsuarioEmpresa` depois?**

7. **É permitido desativar um usuário ou apenas vínculos individuais?**

8. **Como evitar que uma Empresa fique sem nenhum usuário com capacidade administrativa?**

9. **Precisamos distinguir administrador de usuário operacional já no MVP?**

10. **Recuperação de senha continua fora do MVP ou passa a ser necessária para tornar o acesso utilizável?**

Essas decisões devem ser fechadas antes de especificar UC031.

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
