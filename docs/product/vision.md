# Visão do Produto

## Produto

**Precificador** é uma aplicação para formação, revisão e acompanhamento de preços de produtos artesanais.

## Problema

O custo dos produtos varia conforme o preço dos insumos. Em uma planilha, alterações são possíveis, mas tornam-se progressivamente difíceis de manter à medida que aumentam o número de insumos, fichas técnicas, produtos e históricos.

O sistema deve permitir atualizar um preço de insumo uma única vez e refletir esse novo custo em todos os produtos que o utilizam, evidenciando produtos cuja margem real deixou de atender a margem-alvo.

## Objetivos

1. Centralizar cadastro e histórico de preços dos insumos.
2. Manter fichas técnicas de produtos com rendimento e recursos de produção.
3. Calcular o custo atual do produto a partir dos dados vigentes.
4. Calcular preço teórico e preço sugerido a partir da margem-alvo.
5. Comparar margem atual com margem-alvo.
6. Destacar produtos que precisam de revisão de preço.
7. Manter histórico suficiente para explicar mudanças de custo e preço.
8. Permanecer simples de operar, evoluir, testar e fazer backup.

## Princípios

- **Local-first**: o MVP deve funcionar sem serviço externo obrigatório.
- **Custo operacional zero**: o uso local não deve exigir hospedagem ou banco pagos.
- **Fonte única para preços de insumos**: um registro de preço deve impactar todas as fichas relacionadas.
- **Precisão antes de conveniência**: custo desconhecido deve ser sinalizado, nunca presumido como zero.
- **Histórico preservado**: mudanças relevantes de preço não devem apagar a informação anterior.
- **Explicabilidade**: o usuário deve conseguir compreender de onde veio o custo calculado.
- **Escopo controlado**: o produto não deve crescer para um ERP por conveniência técnica.

## Perfil de uso do MVP

- usuário único/local;
- moeda BRL;
- cultura principal pt-BR;
- uso em desktop por navegador local;
- dados persistidos em SQLite.

## Evolução possível

A arquitetura deve permitir futura hospedagem web e troca de SQLite por um banco servidor sem reescrever as regras de negócio. Essa possibilidade não autoriza adicionar complexidade de infraestrutura antes de existir necessidade real.
