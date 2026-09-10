# Visão do Produto

## Produto

**Precificador** é uma aplicação para formação, revisão e acompanhamento de preços de produtos artesanais de diferentes empresas e processos produtivos.

## Problema

O custo dos produtos varia conforme preços de insumos, recursos e decisões de produção. Planilhas tornam-se progressivamente difíceis de manter à medida que aumentam insumos, fichas técnicas, produtos, históricos, empresas e usuários.

O sistema deve permitir atualizar um preço de insumo uma única vez dentro de uma empresa e refletir o novo custo em todos os produtos daquela empresa que o utilizam, preservando isolamento entre empresas.

## Objetivos

1. Centralizar cadastro e histórico de preços dos insumos por empresa.
2. Manter fichas técnicas genéricas com rendimento e recursos de produção.
3. Calcular custo atual do produto a partir dos dados vigentes da empresa.
4. Calcular preço teórico e sugerido a partir da margem-alvo.
5. Comparar margem atual com margem-alvo.
6. Destacar produtos que precisam de revisão.
7. Manter histórico suficiente para explicar mudanças de custo e preço.
8. Permitir múltiplos usuários e empresas com isolamento de acesso.
9. Permanecer simples de operar, evoluir, testar e fazer backup.

## Princípios

- **Local-first nesta fase**: funcionar sem serviço externo obrigatório.
- **Custo operacional baixo**: manter SQLite até a estratégia de publicação justificar banco servidor.
- **Isolamento por empresa**: nenhuma empresa pode consultar ou alterar dados de outra sem vínculo autorizado do usuário.
- **Fonte única para preços**: um preço de insumo impacta as fichas da mesma empresa.
- **Precisão antes de conveniência**: custo desconhecido é sinalizado, nunca presumido zero.
- **Histórico preservado**.
- **Explicabilidade**.
- **Modelo produtivo genérico**: regras comuns não devem assumir panificação quando podem atender outros processos.
- **Escopo controlado**: não crescer para ERP por conveniência técnica.

## Perfil de uso atual

- múltiplas empresas no mesmo banco;
- usuários autenticados e vinculados às empresas autorizadas;
- moeda BRL;
- cultura principal pt-BR;
- uso inicial em desktop pelo navegador;
- persistência em SQLite nesta fase.

## Evolução possível

A arquitetura deve permitir futura hospedagem web e troca de SQLite por banco servidor sem reescrever as regras de negócio. A estratégia de publicação será decidida separadamente.
