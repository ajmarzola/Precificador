# Instrução Codex — MEL006 Timezone e data operacional da Empresa

Implemente a **MEL006 — Tornar a data operacional dependente do timezone da Empresa**.

## Fonte normativa

Leia:

1. `AGENTS.md`;
2. `docs/development/improvements/MEL006-timezone-empresa.md`;
3. `docs/development/melhorias.md`;
4. `docs/use-cases/UC006-consultar-historico-precos-insumo.md`;
5. `docs/business/business-rules.md`;
6. `docs/development/foundation-multiempresa-auth.md`;
7. código atual de Empresa, EmpresaConfiguration, EmpresaContext, Login, Selecionar e Setup;
8. testes de EmpresaContext, autenticação, logout e migrations.

## Branch

Use:

~~~text
feat/mel006-timezone-empresa
~~~

Parta da master atual.


### Precondição obrigatória de Git

Antes de alterar qualquer arquivo:

1. confirme que a `master` é o ponto de partida esperado para esta tarefa;
2. crie/troque para `feat/mel006-timezone-empresa`;
3. confirme que a branch atual é `feat/mel006-timezone-empresa` e **não é `master`**;
4. somente então inicie qualquer edição.

Se não for possível trabalhar nessa branch, **não altere arquivos** e reporte o impedimento.

É proibido editar, commitar ou fazer push direto em `master`. A entrega termina em branch/PR; não faça merge da própria implementação.

## Escopo obrigatório

Entregar:

- Empresa.TimeZoneId;
- migration evolutiva;
- timezone padrão America/Sao_Paulo para dados existentes;
- TimeZoneId no contexto da Empresa Ativa;
- propagação em Login/Selecionar;
- limpeza no logout/contexto;
- abstração IDataOperacionalEmpresa;
- implementação baseada em TimeProvider;
- testes fechados;
- docs pós-implementação.

Não implementar UC006.

## Empresa

Adicionar TimeZoneId obrigatório, max 100 e validado.

Centralizar o valor padrão:

~~~text
America/Sao_Paulo
~~~

Não espalhar literal.

Atualizar criação/seed técnico de forma coerente.

Não criar UI administrativa.

## Persistência

Migration:

~~~text
AddEmpresaTimeZone
~~~

ou nome equivalente.

Empresas existentes recebem America/Sao_Paulo.

Não editar migrations antigas.

Atualizar snapshot normalmente.

## EmpresaContext

Adicionar TimeZoneId ao contrato IEmpresaContext.

EmpresaContext deve guardar session:

- id;
- nome;
- timezone.

Definir recebe os três.

Limpar remove os três.

Atualizar Login e Selecionar para carregar timezone da Empresa persistida.

## Data operacional

Criar interface:

~~~csharp
public interface IDataOperacionalEmpresa
{
    DateOnly Hoje { get; }
}
~~~

ou nome equivalente.

Implementação:

1. obtém TimeProvider.GetUtcNow();
2. resolve TimeZoneInfo pelo TimeZoneId da Empresa Ativa;
3. converte o instante;
4. retorna DateOnly local da Empresa.

Registrar TimeProvider.System no DI.

Não usar DateTime.Now/Today/DateTimeOffset.Now/UtcNow diretamente.

Sem Empresa ativa/timezone => InvalidOperationException. Não fazer fallback.

## Testabilidade

Não adicionar pacote externo apenas para fake clock.

Pode criar TimeProvider de teste pequeno.

Teste obrigatório do instante:

~~~text
2026-09-12T02:30:00Z
America/Sao_Paulo => 11/09/2026
UTC               => 12/09/2026
~~~

## Compatibilidade

Atualize todos os helpers/testes que criam Empresa para fornecer timezone válido.

Não enfraqueça isolamento multiempresa.

Não mude o significado de Empresa Ativa além de acrescentar timezone.

## UC006

Não crie página de histórico.

Após implementar a infraestrutura, atualize a documentação do UC006/RN006 para trocar "data local da aplicação" por "data operacional da Empresa".

A implementação futura do UC006 deverá injetar IDataOperacionalEmpresa.

## Testes

Siga a matriz MEL006:

- domínio Empresa;
- migration upgrade;
- banco novo/seed;
- round-trip timezone;
- DataOperacional São Paulo x UTC;
- ausência de contexto;
- EmpresaContext define/limpa;
- login empresa única;
- seleção de empresa com timezones diferentes.

A MEL003 existente deve ser ampliada para a nova chave de sessão, não duplicada sem necessidade.

## Proibições

Não:

- implementar UC006;
- criar CRUD/configuração de Empresa;
- detectar timezone no browser;
- adicionar NodaTime;
- criar timezone por usuário;
- alterar cultura/moeda;
- refatorar autenticação além do necessário;
- implementar MEL005.

## Validação

Execute:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

Confirme:

1. migration nova e snapshot coerente;
2. migrations antigas intactas;
3. banco existente preservado;
4. TimeZoneId não nulo;
5. session limpa timezone no logout;
6. TimeProvider usado;
7. sem DateTime.Now/Today na nova lógica;
8. fronteira São Paulo/UTC verde;
9. UC006 não implementado;
10. suíte completa verde.

## Documentação pós-implementação

- MEL006 -> Concluída;
- documento MEL006 -> Concluída;
- RN006 passa a referenciar data operacional da Empresa;
- UC006 troca data local da aplicação por IDataOperacionalEmpresa;
- remover do UC006 a limitação que originou MEL006.

## Retorno obrigatório

~~~text
Implementação concluída

Resumo:
- ...

Validações:
- ...

Testes:
- Unitários: X/X
- Integração: X/X

Produção/schema:
- Empresas.TimeZoneId + migration AddEmpresaTimeZone (ou nome efetivo)
- data operacional baseada em TimeProvider
- UC006 não implementado

Pendências/observações:
- ...

Mensagem de commit sugerida:
feat: adiciona data operacional por timezone da empresa
~~~

Não faça merge em master.
