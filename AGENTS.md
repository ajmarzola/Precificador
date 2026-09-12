# Instruções para agentes de código

Este arquivo define regras permanentes para qualquer agente que altere o repositório Precificador.

## 0. Proteção obrigatória do fluxo Git

A branch `master` é protegida conceitualmente e **nunca deve receber edição, commit ou push direto de um agente**.

Antes de alterar qualquer arquivo, o agente deve obrigatoriamente:

1. identificar a branch atual;
2. confirmar que a branch atual **não é `master`**;
3. confirmar que está na branch definida pela instrução versionada da tarefa;
4. se estiver em `master`, criar/trocar para a branch da tarefa **antes da primeira edição**;
5. confirmar que a branch da tarefa parte da `master` esperada para aquela implementação.

Se não for possível criar/trocar para a branch correta, **não implementar a tarefa**. Reportar o impedimento sem modificar arquivos.

Regras permanentes:

- não editar arquivos enquanto estiver em `master`;
- não criar commit em `master`;
- não fazer push direto para `master`;
- não fazer merge da própria implementação em `master`;
- toda alteração de código, teste, migration ou documentação deve chegar à `master` por Pull Request;
- a branch indicada na instrução Codex prevalece sobre nomes improvisados;
- uma tarefa por branch/PR, salvo quando a especificação disser explicitamente o contrário;
- não reutilizar branch de tarefa já concluída para uma nova implementação;
- antes de concluir, confirmar novamente a branch atual e revisar o diff contra `master`.

A instrução individual da tarefa deve repetir a branch esperada, mas esta regra global continua válida mesmo que uma instrução individual seja omissa.

Fluxo normativo detalhado: `docs/development/workflow-codex.md`.

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

- confirme que a branch atual é a branch da tarefa e não `master`;
- restaure dependências;
- compile a solution;
- execute todos os testes;
- confirme que não foram introduzidos warnings novos relevantes;
- verifique migrations quando houver alteração de banco;
- revise o diff contra `master` e remova mudanças fora do escopo;
- deixe a entrega pronta para Pull Request;
- não faça merge em `master`.

Consulte a Definition of Done completa em `docs/development/definition-of-done.md`.

## 9. Convenções

- Documentação e linguagem de domínio: português do Brasil.
- Termos técnicos consolidados do ecossistema .NET podem permanecer em inglês.
- Prefira código simples, explícito e fácil de manter.
- Um caso de uso deve produzir um diff pequeno e revisável.
- Não crie abstrações antecipando necessidades futuras sem evidência no escopo atual.
