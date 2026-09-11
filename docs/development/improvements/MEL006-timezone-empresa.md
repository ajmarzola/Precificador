# MEL006 — Tornar a data operacional dependente do timezone da Empresa

- **Status:** Pronto para implementação
- **Tipo:** melhoria técnica / fundação temporal multiempresa
- **Origem:** especificação do UC006
- **Dependências:** FT002 implementada
- **Momento recomendado:** antes da implementação do UC006
- **Impacta diretamente:** RN006 e futuras regras dependentes de data efetiva
- **Não implementa:** histórico de preços, UC006, CRUD de Empresa ou configurações gerais

## Objetivo

Eliminar a dependência do timezone do processo/servidor para decisões de negócio baseadas em calendário.

A aplicação deve possuir uma **data operacional da Empresa Ativa**, calculada a partir de:

1. instante UTC fornecido por `TimeProvider`;
2. `TimeZoneId` persistido na Empresa;
3. contexto da Empresa Ativa.

O primeiro consumidor previsto é o UC006/RN006, que precisa distinguir corretamente preço vigente de preço futuro.

## Problema

RN006 depende da comparação:

~~~text
DataReferencia <= dataAtual
~~~

Se `dataAtual` vier de `DateTime.Now`, `DateTime.Today` ou equivalente, o resultado depende do timezone da máquina que hospeda a aplicação.

Exemplo:

~~~text
Instante UTC: 12/09/2026 02:30 UTC

Empresa em America/Sao_Paulo:
11/09/2026 23:30
Data operacional = 11/09/2026

Servidor em UTC:
12/09/2026 02:30
Data local do processo = 12/09/2026
~~~

Um preço com DataReferencia = 12/09/2026 seria promovido cedo demais se a regra usasse a data do servidor.

## Decisão

### 1. Timezone pertence à Empresa

Adicionar à entidade `Empresa`:

~~~text
TimeZoneId: string
~~~

O valor representa um identificador de timezone compatível com `TimeZoneInfo`.

Padrão normativo do MVP:

~~~text
America/Sao_Paulo
~~~

Motivo:

- preserva a expectativa operacional atual;
- permite migration segura para Empresas existentes;
- remove dependência do timezone do host;
- mantém caminho aberto para Empresas em outros fusos.

### 2. Data operacional não usa relógio estático diretamente

Introduzir abstração, por exemplo:

~~~csharp
public interface IDataOperacionalEmpresa
{
    DateOnly Hoje { get; }
}
~~~

Implementação esperada:

~~~text
instanteUtc = TimeProvider.GetUtcNow()
timezone = TimeZoneInfo.FindSystemTimeZoneById(EmpresaAtiva.TimeZoneId)
instanteEmpresa = converter instanteUtc para timezone
Hoje = data de instanteEmpresa
~~~

Nenhum consumidor de regra temporal deve precisar conhecer o timezone diretamente.

### 3. Usar TimeProvider

Registrar `TimeProvider.System` no DI.

A implementação de data operacional deve depender de `TimeProvider`, nunca chamar diretamente:

- `DateTime.Now`;
- `DateTime.Today`;
- `DateTimeOffset.Now`;
- `DateTimeOffset.UtcNow`.

Isso permite testes determinísticos na fronteira da meia-noite.

## Modelo Empresa

### TimeZoneId

Regras:

- obrigatório;
- trim externo;
- máximo de 100 caracteres;
- deve representar timezone reconhecido pela plataforma através de `TimeZoneInfo`;
- não pode ser alterado implicitamente por login/seleção de Empresa.

Mensagem de domínio sugerida para vazio:

~~~text
O fuso horário da empresa é obrigatório.
~~~

Mensagem de domínio sugerida para identificador não reconhecido:

~~~text
O fuso horário da empresa é inválido.
~~~

### Criação

`Empresa.Criar` e `Empresa.CriarTecnica` devem produzir Empresa com timezone válido.

A API exata pode exigir `timeZoneId` explicitamente ou fornecer um overload claramente documentado com o padrão do MVP.

Evitar espalhar a string `America/Sao_Paulo` em vários arquivos; manter um único ponto de definição.

### Alteração futura

Não criar tela para editar timezone nesta MEL.

Quando houver CRUD/configuração administrativa de Empresa, esse fluxo deverá reutilizar a validação de domínio.

## Empresa Ativa

O contexto ativo deve carregar:

~~~text
EmpresaId
Nome
TimeZoneId
~~~

Evoluir `IEmpresaContext` para expor:

~~~csharp
string? TimeZoneId { get; }
~~~

Evoluir `EmpresaContext` para persistir o timezone na Session.

Chave sugerida:

~~~text
EmpresaAtivaTimeZoneId
~~~

### Definir

`EmpresaContext.Definir(...)` deve receber e armazenar `TimeZoneId` junto com Id/Nome.

### Limpar

`EmpresaContext.Limpar()` deve remover também a chave de timezone.

Não manter timezone ativo depois de logout.

## Login e seleção de Empresa

### Login com uma Empresa elegível

A consulta que resolve a Empresa única deve carregar também `TimeZoneId`.

A chamada de `EmpresaContext.Definir` deve armazenar Id, Nome e TimeZoneId.

### Múltiplas Empresas

Ao selecionar Empresa em `/Empresas/Selecionar`, carregar o TimeZoneId da Empresa escolhida e armazená-lo no contexto ativo.

Trocar Empresa deve trocar também a data operacional derivada.

## Persistência e migration

Criar migration evolutiva:

~~~text
AddEmpresaTimeZone
~~~

ou nome equivalente.

Adicionar coluna:

~~~text
Empresas.TimeZoneId
- required
- max length 100
~~~

### Empresas existentes

A migration deve preencher registros existentes com:

~~~text
America/Sao_Paulo
~~~

Não deixar `null`.

Não inferir timezone por endereço, nome da Empresa, usuário ou timezone do servidor.

### Seed técnico

A Empresa técnica criada por `HasData` deve possuir o mesmo timezone padrão.

Não editar migration histórica da FT002.

## Data operacional

A implementação deve ficar em uma camada compatível com o uso atual da Empresa Ativa.

Estrutura sugerida:

~~~text
src/Precificador.Core/Empresas/IDataOperacionalEmpresa.cs
src/Precificador.Web/Empresas/DataOperacionalEmpresa.cs
~~~

ou equivalente coerente com as dependências existentes.

A interface pode ficar no Core porque representa uma necessidade transversal de regra de negócio; a implementação pode ficar no Web porque usa o contexto request-scoped da Empresa Ativa.

Não adicionar EF Core à abstração.

## Comportamento sem Empresa Ativa

Não fazer fallback para timezone do servidor.

Se `IDataOperacionalEmpresa.Hoje` for solicitado sem Empresa Ativa/TimeZoneId válido, falhar explicitamente com `InvalidOperationException`.

As páginas de negócio já devem estar protegidas pela policy de Empresa Ativa; essa falha representa erro de programação/configuração, não fluxo normal do usuário.

## Relação com UC006

A MEL006 **não implementa o UC006**.

Após esta melhoria:

- a especificação do UC006 deve substituir “data local da aplicação” por “data operacional da Empresa”;
- Histórico e Detalhes devem receber a mesma data operacional por request;
- RN006 deve comparar `DataReferencia` com essa data;
- testes de UC006 não devem depender do timezone da máquina de CI.

Implementar MEL006 antes do UC006 evita introduzir e depois remover dependência de `DateTime.Now`.

## Relação com regras futuras

A mesma abstração deve ser reutilizada por futuras regras que dependam de “hoje” no contexto da Empresa.

Não antecipar agora:

- fechamento diário;
- agenda;
- vencimento;
- fiscal;
- horário comercial;
- timezone por usuário.

A unidade de negócio é a Empresa.

## Critérios de aceitação

### CA01 — Empresa possui timezone

Toda Empresa persistida possui `TimeZoneId` não nulo e válido.

### CA02 — Migration preserva Empresas existentes

Upgrade de banco existente mantém Empresas e atribui `America/Sao_Paulo`.

### CA03 — Seed técnico possui timezone

Empresa inicial criada em banco novo possui o timezone padrão.

### CA04 — Contexto ativo armazena timezone

Definir Empresa Ativa armazena Id, Nome e TimeZoneId.

### CA05 — Limpeza remove timezone

Logout/limpeza remove também o timezone ativo da sessão.

### CA06 — Login de empresa única propaga timezone

Empresa selecionada automaticamente após login define o TimeZoneId correto no contexto.

### CA07 — Seleção troca timezone

Selecionar outra Empresa passa a usar o TimeZoneId daquela Empresa.

### CA08 — Data operacional usa instante UTC + timezone da Empresa

O mesmo instante UTC pode produzir datas diferentes para Empresas em fusos diferentes.

### CA09 — Fronteira de meia-noite é correta

Para instante:

~~~text
2026-09-12T02:30:00Z
~~~

Empresa `America/Sao_Paulo` deve obter:

~~~text
11/09/2026
~~~

Empresa `UTC` deve obter:

~~~text
12/09/2026
~~~

### CA10 — Sem Empresa Ativa não há fallback

Solicitar data operacional sem contexto válido falha explicitamente.

### CA11 — Relógio é testável

Implementação usa `TimeProvider`; testes não alteram relógio do sistema.

### CA12 — Sem CRUD de timezone

Nenhuma nova tela administrativa de Empresa é criada.

### CA13 — Sem implementação do UC006

Nenhuma tela de histórico/preço vigente do UC006 é implementada nesta MEL.

## Matriz de testes fechada antes da implementação

### Unitários — Empresa

#### U1

~~~text
CA01_Empresa_valida_preserva_timezone
~~~

#### U2

~~~text
CA01_Timezone_vazio_ou_invalido_e_rejeitado
~~~

Cobrir vazio e identificador inexistente.

#### U3

~~~text
CA01_Timezone_acima_do_limite_e_rejeitado
~~~

Se limite for validado antes da resolução, manter teste focado.

### Integração — persistência

#### P1

~~~text
CA02_Migration_adiciona_timezone_e_preserva_empresa_existente
~~~

Migrar até estado anterior, criar/confirmar Empresa, aplicar migration e validar o valor padrão.

#### P2

~~~text
CA03_Banco_novo_cria_empresa_tecnica_com_timezone_padrao
~~~

#### P3

~~~text
CA01_Timezone_persiste_e_e_recuperado
~~~

### Testes focados de data operacional

Podem ficar no projeto de integração, pois ele referencia Web.

Usar `TimeProvider` controlado/falso local ao teste, sem nova dependência de pacote se não for necessária.

#### T1

~~~text
CA08_CA09_Mesmo_instante_UTC_respeita_timezone_da_empresa
~~~

Instante fixo:

~~~text
2026-09-12T02:30:00Z
~~~

Validar São Paulo = 11/09 e UTC = 12/09.

#### T2

~~~text
CA10_Sem_timezone_ativo_data_operacional_falha_sem_fallback
~~~

### Integração — contexto/autenticação

#### W1

~~~text
CA04_EmpresaContext_define_id_nome_e_timezone
~~~

#### W2

~~~text
CA05_Limpar_empresa_context_remove_id_nome_e_timezone
~~~

Atualizar a cobertura existente da MEL003 em vez de duplicar teste equivalente.

#### W3

~~~text
CA06_Login_com_empresa_unica_define_timezone_da_empresa
~~~

#### W4

~~~text
CA07_Selecao_de_empresa_atualiza_timezone_ativo
~~~

Usar duas Empresas com timezones distintos.

## Fora do escopo

- UI para editar timezone;
- catálogo completo de fusos;
- detectar timezone do navegador;
- timezone por usuário;
- cultura/moeda/idioma por Empresa;
- horário de verão customizado;
- NodaTime;
- serviço externo;
- UC006;
- UC007/UC008;
- MEL005;
- refatoração geral de autenticação.

## Definition of Done específica

MEL006 está concluída quando:

- Empresa possui TimeZoneId persistido e validado;
- migration evolutiva preserva banco existente;
- timezone padrão existe em um único ponto;
- EmpresaContext carrega/limpa TimeZoneId;
- login e seleção propagam timezone;
- `IDataOperacionalEmpresa` usa `TimeProvider`;
- não existe fallback silencioso para horário local do servidor;
- testes provam a fronteira São Paulo/UTC;
- teste de logout/contexto cobre limpeza do timezone;
- nenhuma tela administrativa nova;
- UC006 não foi implementado por antecipação;
- documentação RN006/UC006 é atualizada após implementação para usar data operacional da Empresa;
- build Release sem warnings novos;
- suíte completa verde;
- `melhorias.md` e este documento passam para Concluída após implementação.

## Mensagem de commit sugerida

~~~text
feat: adiciona data operacional por timezone da empresa
~~~
