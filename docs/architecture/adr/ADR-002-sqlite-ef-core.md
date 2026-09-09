# ADR-002 — SQLite com Entity Framework Core

- **Status:** Aceito
- **Data:** 2026-09-09

## Contexto

O MVP é local, de usuário único, deve ter custo operacional zero e exigir administração mínima. Também é desejável preservar a possibilidade de migração futura para um banco servidor.

## Decisão

Usar SQLite como banco do MVP e Entity Framework Core como mecanismo de acesso, mapeamento e migrations.

## Consequências

### Positivas

- banco em arquivo único;
- instalação simples;
- backup operacional simples;
- testes de integração rápidos com SQLite temporário;
- modelo de persistência compatível com futura troca de provider.

### Negativas

- limitações de concorrência tornam SQLite inadequado caso o sistema evolua para muitos usuários simultâneos;
- diferenças entre providers exigirão validação se houver migração futura.

## Regras derivadas

- migrations são obrigatórias para alterações de esquema;
- dados reais não devem ser commitados;
- testes de consultas relevantes devem usar SQLite real, não provider InMemory como substituto comportamental.
