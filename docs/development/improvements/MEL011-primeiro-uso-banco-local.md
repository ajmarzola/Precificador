# MEL011 — Tratar primeiro uso com banco local não migrado

- **Origem:** Review MVP 2026-09-16
- **Classificação:** correção de primeiro uso
- **Prioridade:** alta
- **Dependência:** FT002

## Problema

Ao executar a aplicação contra SQLite ainda não migrado, o Login pode falhar com exceção técnica:

~~~text
SQLite Error 1: 'no such table: AspNetUsers'
~~~

A aplicação atualmente não aplica migrations automaticamente por decisão arquitetural.

## Objetivo

Tornar o estado de banco não preparado explícito e diagnosticável, evitando que o primeiro contato do usuário/desenvolvedor seja uma exceção de infraestrutura sem orientação.

## Restrições

- não introduzir auto-migration no startup sem decisão arquitetural específica;
- não criar schema via `EnsureCreated`;
- não mascarar migration pendente como erro de credencial;
- considerar o risco do caminho relativo `Data Source=precificador.db`.

## A definir na especificação

- comportamento em Development e Production;
- detecção segura de banco/schema não preparado;
- mensagem/página de orientação;
- eventual exibição do caminho efetivo do SQLite em Development;
- testes para banco inexistente, vazio, desatualizado e migrado.

## Estado

Planejado.
