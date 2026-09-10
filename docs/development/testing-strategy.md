# Estratégia de Testes

## Objetivo

Dar alta confiança às regras financeiras, persistência e isolamento entre empresas sem criar suíte de UI cara de manter.

## 1. Unitários

Obrigatórios para regras puras de domínio novas/alteradas.

Prioridades incluem normalização/identidade de entidades, custo, perda, rendimento, mão de obra, energia, margem e precificação.

Não testar internals do ASP.NET Core Identity como teste unitário próprio.

## 2. Integração de persistência

Usar SQLite real temporário ou `:memory:` com conexão mantida.

Cobrir especialmente:

- migrations e upgrades;
- FKs e índices únicos;
- históricos;
- filtros tenant-aware;
- rejeição de escrita cross-tenant;
- seleção de preço vigente;
- desativação.

Não usar EF InMemory como substituto de SQLite para esses riscos.

## 3. Testes de autenticação/multiempresa

Toda entidade tenant-owned deve possuir pelo menos cenários que provem:

- Empresa A não lê dados da Empresa B;
- Empresa A não altera dados da Empresa B por fluxo normal;
- mesma identidade de negócio pode coexistir em empresas diferentes quando permitido;
- ausência de Empresa Ativa não vaza dados;
- troca de Empresa Ativa altera corretamente o conjunto visível.

Autenticação deve ter smoke/integration para login, logout, rota protegida e vínculo usuário-empresa.

## 4. Web/smoke

Poucos e focados em fluxos críticos. Selenium/Playwright não é requisito geral.

## 5. Golden cases

Motor de precificação terá cenários canônicos dos negócios de referência. Os casos devem declarar a Empresa/contexto quando isso afetar configurações.

## 6. Dados de teste

- fictícios;
- nunca usar banco real do usuário;
- determinísticos;
- relógio controlado quando relevante;
- pelo menos duas empresas nos testes de isolamento.

## 7. Convenções

- xUnit;
- nomes explicam cenário/resultado;
- bug de regra confirmado ganha teste de regressão quando possível;
- critérios de segurança/isolamento têm prioridade equivalente às regras financeiras.
