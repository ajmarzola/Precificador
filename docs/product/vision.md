# Visão do Produto

## Produto

**Precificador** é uma aplicação para formação, revisão e acompanhamento de preços de produtos artesanais.

## Problema

O custo dos produtos varia conforme o preço dos insumos. Em uma planilha, alterações são possíveis, mas tornam-se progressivamente difíceis de manter à medida que aumentam o número de insumos, fichas técnicas, produtos, históricos, empresas e usuários.

O sistema deve permitir atualizar um preço de insumo uma única vez e refletir esse novo custo em todos os produtos da mesma empresa que o utilizam, evidenciando produtos cuja margem real deixou de atender a margem-alvo.

## Objetivos

1. Centralizar cadastro e histórico de preços dos insumos por empresa.
2. Manter fichas técnicas de produtos com rendimento e recursos de produção.
3. Calcular o custo atual do produto a partir dos dados vigentes.
4. Calcular preço teórico e preço sugerido a partir da margem-alvo.
5. Comparar margem atual com margem-alvo.
6. Destacar produtos que precisam de revisão de preço.
7. Manter histórico suficiente para explicar mudanças de custo e preço.
8. Permitir múltiplas empresas e usuários com isolamento de acesso.
9. Permanecer simples de operar, evoluir, testar e fazer backup.

## Princípios

- **Local-first**: a fase atual deve funcionar sem serviço externo obrigatório.
- **SQLite nesta fase**: manter operação simples até a estratégia de publicação justificar banco servidor.
- **Isolamento por empresa**: dados operacionais de uma empresa não podem ser lidos ou alterados por usuário sem vínculo autorizado.
- **Fonte única para preços de insumos**: um registro de preço impacta todas as fichas relacionadas da mesma empresa.
- **Precisão antes de conveniência**: custo desconhecido deve ser sinalizado, nunca presumido como zero.
- **Histórico preservado**.
- **Explicabilidade**.
- **Escopo controlado**: o produto não deve crescer para um ERP por conveniência técnica.
- **Modelo produtivo generalizável**: regras comuns não devem assumir panificação quando podem atender outros processos artesanais.

## Perfil de uso do MVP

- múltiplas empresas no mesmo banco;
- usuários autenticados vinculados a uma ou mais empresas;
- moeda BRL;
- cultura principal pt-BR;
- uso inicialmente em desktop por navegador;
- dados persistidos em SQLite nesta fase.

## Evolução possível

A arquitetura deve permitir futura hospedagem web e troca de SQLite por banco servidor sem reescrever as regras de negócio. A decisão de publicação será tomada separadamente.
