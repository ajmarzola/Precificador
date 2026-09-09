# Instruções para agentes de código

Este arquivo define regras permanentes para qualquer agente que altere o repositório Precificador.

## 1. Fonte de verdade

Antes de implementar qualquer caso de uso, leia os documentos aplicáveis em `docs/`, especialmente:

- `docs/product/vision.md`
- `docs/product/scope.md`
- `docs/business/business-rules.md`
- `docs/business/pricing-model.md`
- `docs/architecture/architecture.md`
- o ADR relacionado à alteração
- o documento individual do caso de uso
- `docs/development/definition-of-done.md`
- `docs/development/testing-strategy.md`

Em caso de conflito, decisões mais específicas e ADRs aceitos prevalecem sobre descrições genéricas. Não invente requisitos para preencher lacunas.

## 2. Escopo da alteração

- Implemente somente o caso de uso solicitado e as mudanças estritamente necessárias para ele.
- Não faça refatorações oportunistas em áreas não relacionadas.
- Não acrescente funcionalidades não solicitadas.
- Não transforme o projeto em ERP: estoque, PDV, pedidos, clientes, financeiro e contabilidade estão fora do MVP.
- Se uma mudança arquitetural for realmente necessária, ela deve ser explicitada e registrada em ADR antes de ser tratada como padrão.

## 3. Arquitetura

A aplicação é um monólito web local-first.

Projetos de produção previstos:

- `Precificador.Web`
- `Precificador.Core`
- `Precificador.Infrastructure`

Projetos de teste podem existir separadamente e não contam no limite dos três projetos de produção.

Não introduza CQRS, MediatR, repository genérico, Unit of Work customizado, microserviços, mensageria ou camadas adicionais sem decisão arquitetural explícita.

## 4. Persistência

- Entity Framework Core é a abstração padrão de persistência.
- SQLite é o banco do MVP.
- Alterações de esquema devem ser feitas por migrations.
- Não altere manualmente o banco como mecanismo de evolução de esquema.
- Valores monetários e quantidades que exijam precisão devem usar tipos decimais apropriados; não use ponto flutuante binário para dinheiro.

## 5. Regras de negócio

- Regras financeiras devem residir no núcleo da aplicação e ser testáveis sem interface web.
- Não duplique fórmulas de precificação em PageModels, JavaScript ou consultas de apresentação.
- Custo desconhecido não significa custo zero.
- Preço de insumo é histórico; o valor atual é derivado dos registros de preço.
- O custo atual de um produto é calculado a partir dos dados atuais e não é um campo persistido no produto.

## 6. Testes

- Toda regra de negócio nova ou alterada deve possuir testes unitários.
- Fluxos de persistência relevantes devem possuir testes de integração com SQLite real temporário.
- Não substitua testes de integração de EF Core por mocks quando o comportamento da consulta/persistência fizer parte do risco testado.
- Todo teste existente deve continuar passando.
- Não remova ou enfraqueça testes apenas para obter build verde.

## 7. Documentação

A documentação é parte da entrega.

Ao concluir um caso de uso:

- mantenha o `.md` do caso de uso coerente com a implementação;
- atualize regras de negócio afetadas;
- atualize a documentação da funcionalidade quando o comportamento externo mudar;
- crie/atualize ADR somente quando houver uma decisão arquitetural relevante.

## 8. Qualidade e revisão

Antes de considerar uma alteração concluída:

- restaure dependências;
- compile a solution;
- execute todos os testes;
- confirme que não foram introduzidos warnings novos relevantes;
- verifique migrations quando houver alteração de banco;
- revise o diff e remova mudanças fora do escopo.

Consulte a Definition of Done completa em `docs/development/definition-of-done.md`.

## 9. Convenções

- Documentação e linguagem de domínio: português do Brasil.
- Termos técnicos consolidados do ecossistema .NET podem permanecer em inglês.
- Prefira código simples, explícito e fácil de manter.
- Um caso de uso deve produzir um diff pequeno e revisável.
- Não crie abstrações antecipando necessidades futuras sem evidência no escopo atual.
