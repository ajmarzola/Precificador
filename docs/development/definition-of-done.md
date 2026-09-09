# Definition of Done

Um caso de uso só está concluído quando todos os itens aplicáveis abaixo foram atendidos.

## Comportamento

- o comportamento solicitado foi implementado;
- os critérios de aceitação do UC foram atendidos;
- as regras de negócio referenciadas foram respeitadas;
- fluxos inválidos previstos foram tratados;
- nenhuma funcionalidade fora do escopo foi adicionada.

## Código

- o diff é pequeno e relacionado ao UC;
- não há refatoração oportunista sem necessidade;
- regras de negócio não foram duplicadas na UI;
- não foram criadas abstrações ou camadas sem justificativa;
- novos warnings relevantes não foram introduzidos.

## Persistência

Quando aplicável:

- alteração de esquema possui migration;
- migration é reproduzível a partir de banco limpo;
- não houve alteração manual de banco como substituto de migration;
- dados reais/sensíveis não foram adicionados ao repositório.

## Testes

- regras novas/alteradas possuem testes unitários;
- persistência relevante possui testes de integração com SQLite;
- golden cases são mantidos quando o motor de precificação for afetado;
- todos os testes existentes passam;
- testes não foram removidos/enfraquecidos para acomodar regressão.

## Validação técnica

- restore concluído;
- solution compila;
- suite completa de testes passa;
- aplicação inicia quando o UC envolve fluxo web;
- o diff final foi revisado.

## Documentação

- documento individual do UC está atualizado e com status coerente;
- regras de negócio afetadas foram atualizadas;
- funcionalidade afetada foi atualizada quando necessário;
- ADR foi criado/alterado se houve decisão arquitetural;
- documentação não descreve comportamento inexistente como implementado.

## Revisão

- PR descreve o UC implementado;
- mudanças não relacionadas foram removidas ou justificadas;
- pendências conhecidas ficam registradas como novo backlog, não escondidas no código.
