# ADR-013 — Empresa técnica e ciclo de vida administrativo

- **Status:** Aceito

## Contexto

A administração global introduzida pela UC039 precisa distinguir:

- a Empresa técnica criada historicamente pelo seed;
- Empresas reais;
- Empresa real ativa;
- Empresa real temporariamente suspensa;
- Empresa real encerrada.

O modelo atual já usa Empresa.Ativo em Login, seleção de Empresa e autorização tenant-aware. Substituir esse campo por um novo enum persistido criaria uma migração transversal desnecessária e aumentaria o risco de regressão.

Também existe legado em que a Empresa Id 1 pode ter sido renomeada para uma Empresa real antes da FT003.

## Decisão

### Empresa técnica

Adicionar:

~~~text
Empresa.EhTecnica : bool
~~~

Empresa.Criar cria EhTecnica=false.

Empresa.CriarTecnica cria EhTecnica=true.

Na migration da UC039, marcar técnica somente quando:

~~~text
Id = 1
AND NomeNormalizado = EMPRESA INICIAL
~~~

Uma Empresa 1 renomeada historicamente não é técnica.

A Empresa técnica:

- permanece no banco;
- não é hard-deletada;
- não aparece na administração de clientes;
- não recebe lifecycle/administração UC039.

### Ciclo de vida

Preservar:

~~~text
Empresa.Ativo
~~~

como gate operacional.

Adicionar:

~~~text
Empresa.EncerradaEmUtc : DateTimeOffset?
~~~

Derivar a situação:

~~~text
Ativa     -> Ativo=true  + EncerradaEmUtc=null
Suspensa  -> Ativo=false + EncerradaEmUtc=null
Encerrada -> Ativo=false + EncerradaEmUtc!=null
~~~

Não criar coluna de enum de situação.

### Suspensão

Suspensão é lógica e reversível.

Preserva Empresa, vínculos e dados.

### Encerramento

Encerramento é lógico e terminal dentro da UC039.

Preserva dados e não permite reativação.

Uma necessidade futura de reabertura deve ser especificada separadamente.

### Proteção de Administrador

Nova Empresa nasce com Administrador ativo.

Reativação exige Administrador ativo.

Empresa legada sem Administrador não recebe promoção automática; o SystemAdmin deve corrigir explicitamente.

## Consequências positivas

- Login/seleção/policies continuam usando Empresa.Ativo;
- pequena evolução de schema;
- suspensão funciona sem refatoração transversal;
- encerramento é distinguível de suspensão;
- histórico não é apagado;
- seed técnico deixa de aparecer como cliente;
- instalação antiga com Empresa 1 renomeada não é corrompida.

## Custos

- nova coluna EhTecnica;
- nova coluna EncerradaEmUtc;
- migration com backfill condicional;
- UI administrativa precisa trabalhar com situação derivada;
- invariantes precisam de constraint/testes.

## Alternativas rejeitadas

### Excluir o seed técnico

Rejeitado porque migrations/testes/histórico dependem dele e hard delete poderia destruir ou conflitar com instalações legadas.

### Marcar Id=1 sempre como técnico

Rejeitado porque instalações antigas podem ter transformado esse registro em Empresa real.

### Substituir Ativo por enum persistido

Rejeitado nesta etapa porque Ativo já é gate operacional consolidado em autenticação/autorização e a troca ampliaria o escopo sem benefício necessário.

### Hard delete no encerramento

Rejeitado porque Empresa possui dados históricos e relacionamentos tenant-owned que devem ser preservados.

## Relação com decisões anteriores

ADR-011 continua definindo os dois planos de autorização.

ADR-012 continua definindo ativação/recuperação e e-mail.

ADR-013 define somente a identidade técnica da Empresa e seu ciclo de vida administrativo global.
