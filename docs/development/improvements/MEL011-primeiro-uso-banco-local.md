# MEL011 — Tratar primeiro uso com banco local não migrado

- **Origem:** Review MVP 2026-09-16
- **Classificação:** correção de primeiro uso
- **Prioridade:** alta
- **Dependência:** FT002
- **Estado:** Concluído

## Problema

Ao executar a aplicação contra um banco ainda não migrado, o Login falhava com exceção técnica (histórico original em SQLite: `SQLite Error 1: 'no such table: AspNetUsers'`; o mesmo risco se aplica ao SQL Server, provider atual desde a MEL020).

O mesmo risco existia para o `/Setup`, porque ambos dependem das tabelas do ASP.NET Core Identity.

## Decisão

A política de migrations passa a depender do ambiente.

### Development

Antes de atender qualquer request:

~~~text
PrecificadorDbContext.Database.MigrateAsync()
~~~

Consequências:

- banco inexistente é criado pelas migrations;
- banco local desatualizado recebe migrations pendentes;
- `/Setup` pode ser usado no primeiro acesso sem comando manual prévio;
- Login não chega a consultar `AspNetUsers` antes de o schema existir.

### Outros ambientes

Não executar migrations automaticamente no startup.

Publicação/produção deve aplicar migrations explicitamente como etapa operacional/deploy.

## Restrições preservadas

- não usar `EnsureCreated`;
- não criar ou alterar migration histórica;
- não criar credenciais padrão;
- não mascarar erro de migration em ambientes não Development;
- o mecanismo usa somente migrations EF existentes.

## Implementação

No startup Web, após `builder.Build()` e antes do pipeline HTTP:

1. verificar `app.Environment.IsDevelopment()`;
2. criar scope;
3. resolver `PrecificadorDbContext`;
4. executar `Database.MigrateAsync()`.

## Teste de regressão

Um teste de integração deve iniciar a aplicação em `Development` apontando para um arquivo SQLite novo e, sem chamar migration manualmente:

- acessar `/Setup` com sucesso;
- comprovar que não existem migrations pendentes;
- comprovar que tabelas Identity são consultáveis;
- comprovar que a Empresa técnica inicial existe.

## Observação sobre o arquivo SQLite

A connection string atual continua:

~~~text
Data Source=precificador.db
~~~

O caminho relativo ainda deve ser explicado pelo MEL013, inclusive para evitar confusão sobre qual arquivo está sendo utilizado no ambiente local.
