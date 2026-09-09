# Precificador

Aplicação local-first para formação e acompanhamento de preços de produtos artesanais.

O Precificador calcula custos a partir da ficha técnica dos produtos, dos preços históricos dos insumos e das configurações de produção. Também identifica produtos cuja margem atual ficou abaixo da margem-alvo após alterações de custos.

## Estado do projeto

Projeto reiniciado em setembro de 2026 com novo escopo. A implementação será conduzida incrementalmente por casos de uso pequenos, documentados e testados.

## Stack definida

- .NET 10 LTS
- ASP.NET Core Razor Pages
- Entity Framework Core
- SQLite
- Bootstrap
- JavaScript apenas quando necessário
- xUnit para testes unitários e de integração

## Documentação

A documentação normativa do projeto está em [`docs/`](docs/README.md).

Antes de implementar qualquer alteração, consulte também [`AGENTS.md`](AGENTS.md), que define as regras de trabalho para agentes de código.

## Escopo

O Precificador é uma ferramenta de precificação. No MVP, não é um sistema de estoque, vendas, pedidos, clientes, PDV, financeiro ou contabilidade.
