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

A decisão vigente continua sendo **não aplicar migrations automaticamente no startup**.

O problema a resolver é a experiência/diagnóstico do primeiro uso, não necessariamente introduzir auto-migration.

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

## Regra atual de primeiro acesso

O primeiro acesso da **instalação** já possui um bootstrap definido pela FT002:

1. banco deve estar com migrations aplicadas;
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
