# ADR-008 — Multiempresa em banco compartilhado

- **Status:** Aceito

## Contexto

O Precificador passa a atender mais de uma empresa, com isolamento obrigatório de dados. A aplicação permanece local-first e SQLite nesta fase.

## Decisão

Adotar multi-tenancy por **banco e schema compartilhados**, usando `EmpresaId` obrigatório como discriminator nas entidades pertencentes a uma empresa.

O isolamento de leitura será centralizado no EF Core por Global Query Filters ou mecanismo equivalente. Escritas terão validação central contra a Empresa Ativa.

Índices/restrições de unicidade de dados tenant-owned devem incluir `EmpresaId` quando a identidade for válida apenas dentro de uma empresa.

## Consequências

- mantém um único arquivo SQLite e uma única cadeia de migrations;
- simplifica operação local nesta fase;
- exige disciplina forte de tenant isolation;
- testes cross-tenant passam a ser obrigatórios;
- futura migração para banco servidor permanece possível;
- não existe isolamento físico entre bancos nesta fase.

## Alternativas rejeitadas agora

- um SQLite por empresa;
- schema por empresa;
- banco servidor antes da definição de publicação.
