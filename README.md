# Precificador

Aplicação web para formação e acompanhamento de preços de produtos artesanais, com execução local e publicação controlada em Azure para piloto operacional interno.

O Precificador calcula custos a partir da ficha técnica dos produtos, dos preços históricos dos insumos e das configurações de produção. Também identifica produtos cuja margem atual ficou abaixo da margem-alvo após alterações de custos.

## Estado do projeto

Projeto reiniciado em setembro de 2026 com novo escopo. A implementação será conduzida incrementalmente por casos de uso pequenos, documentados e testados.

## Stack definida

- .NET 10 LTS
- ASP.NET Core Razor Pages
- Entity Framework Core
- SQL Server em desenvolvimento/testes
- Azure SQL Database na publicação Azure
- Bootstrap
- JavaScript apenas quando necessário
- xUnit para testes unitários e de integração

## Publicação Azure

A publicação inicial usa Azure App Service F1/Linux e Azure SQL Database Free offer, com custo alvo zero, Managed Identity e migrations explícitas de deploy. O procedimento reproduzível fica em [`infra/azure/`](infra/azure/README.md).

## Documentação

A documentação normativa do projeto está em [`docs/`](docs/README.md).

Para preparar o ambiente, executar a aplicação e validar as suítes, siga o [guia de execução e testes locais](docs/development/execucao-teste-local.md).

Antes de implementar qualquer alteração, consulte também [`AGENTS.md`](AGENTS.md), que define as regras de trabalho para agentes de código.

## Escopo

O Precificador é uma ferramenta de precificação. No MVP, não é um sistema de estoque, vendas, pedidos, clientes, PDV, financeiro ou contabilidade.
