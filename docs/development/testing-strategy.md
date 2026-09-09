# Estratégia de Testes

## Objetivo

Dar alta confiança às regras financeiras sem criar uma suíte de UI cara de manter.

## 1. Testes unitários

São obrigatórios para toda regra de negócio nova ou alterada.

Prioridade alta para:

- custo unitário de insumo;
- seleção do preço vigente;
- custo de item da ficha;
- perda de ingredientes;
- rendimento;
- mão de obra;
- energia;
- custo do lote;
- custo unitário do produto;
- margem;
- preço teórico;
- arredondamento comercial;
- identificação de produto abaixo da margem;
- precificação incompleta.

Os testes devem evitar dependências de EF Core e HTTP quando o comportamento puder ser validado no núcleo.

## 2. Testes de integração

Usar SQLite real temporário para comportamentos em que persistência e consulta fazem parte do risco, por exemplo:

- manter histórico ao registrar novo preço;
- recuperar o preço vigente correto;
- preservar histórico de preço de venda;
- desativação e filtros;
- migrations e restrições relevantes.

Não usar mocks de repositório como substitutos desses testes.

## 3. Testes web/smoke

Manter poucos e focados. Podem validar que páginas essenciais respondem e que fluxos críticos estão integrados.

No MVP, não adotar Selenium/Playwright como requisito geral. Automação E2E completa só deve ser adicionada se o custo de regressões de UI justificar sua manutenção.

## 4. Golden cases

O motor de precificação terá cenários canônicos obtidos da planilha de referência. Eles devem atravessar a composição completa do cálculo e proteger contra alterações involuntárias de fórmula.

Golden cases não substituem testes unitários das fórmulas; complementam-nos.

## 5. Dados de teste

- usar dados fictícios para testes comuns;
- não depender do banco real do usuário;
- testes devem ser determinísticos;
- datas relevantes devem ser controladas no teste, evitando dependência implícita do relógio atual.

## 6. Convenções

- framework padrão: xUnit;
- nomes devem explicitar cenário e resultado esperado;
- cada bug de regra confirmado deve, quando possível, ganhar teste de regressão antes ou junto da correção.
