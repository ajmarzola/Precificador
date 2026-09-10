# Estratégia de Testes

## Objetivo

Dar alta confiança às regras financeiras, persistência e isolamento entre empresas sem criar uma suíte de UI cara de manter.

## 1. Testes unitários

São obrigatórios para toda regra de negócio nova ou alterada.

Prioridade alta para normalização/identidade de domínio, custo unitário de insumo, seleção do preço vigente, custo da ficha, rendimento, mão de obra, energia, margem, preço e precificação incompleta.

Os testes devem evitar EF Core/HTTP quando o comportamento puder ser validado no núcleo.

Não criar testes unitários dos internals do ASP.NET Core Identity.

## 2. Testes de integração

Usar SQLite real temporário ou `:memory:` com conexão adequadamente mantida para riscos de persistência.

Cobrir, conforme aplicável:

- migrations e upgrades;
- FKs/índices;
- históricos;
- preço vigente;
- desativação;
- filtros tenant-aware;
- rejeição de escrita cross-tenant.

Não usar mocks de repositório ou EF InMemory como substitutos desses testes.

## 3. Testes multiempresa/autenticação

Toda entidade tenant-owned deve possuir cobertura suficiente para provar que:

- Empresa A não lê dados da Empresa B;
- Empresa A não altera dados da Empresa B por fluxo comum;
- ausência de Empresa Ativa não vaza dados;
- troca autorizada de Empresa altera o conjunto visível;
- a mesma identidade de negócio pode coexistir em empresas diferentes quando a regra permitir.

Autenticação deve possuir smoke/integration de bootstrap, login, logout, página protegida e seleção de empresa.

## 4. Testes web/smoke

Manter poucos e focados. No MVP não adotar Selenium/Playwright como requisito geral.

## 5. Golden cases

O motor de precificação terá cenários canônicos obtidos de dados conferidos. Casos futuros devem declarar o contexto de Empresa quando configurações/ownership forem relevantes.

## 6. Dados de teste

- usar dados fictícios;
- não depender do banco real do usuário;
- testes determinísticos;
- controlar datas relevantes;
- usar pelo menos duas empresas nos cenários de isolamento.

## 7. Convenções

- framework padrão: xUnit;
- nomes devem explicitar cenário e resultado esperado;
- cada bug de regra confirmado deve, quando possível, ganhar teste de regressão;
- critérios de isolamento têm prioridade equivalente às regras financeiras.
