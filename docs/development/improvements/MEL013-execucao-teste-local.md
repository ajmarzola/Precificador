# MEL013 — Documentar execução e teste local

- **Origem:** Review MVP 2026-09-16
- **Classificação:** Documentação / DX
- **Prioridade:** média
- **Dependência:** FT001, FT002

## Objetivo

Criar um guia único de preparação e uso do ambiente local.

## Conteúdo mínimo

- pré-requisitos;
- `dotnet tool restore`;
- restore/build;
- política de migrations por ambiente: automática em `Development` e explícita nos demais ambientes;
- comando `dotnet ef database update` com project/startup-project corretos para cenários em que a aplicação explícita seja necessária;
- localização/resolução do `precificador.db`;
- execução da aplicação;
- uso de `/Setup`;
- criação do primeiro usuário e Empresa;
- Login e Empresa Ativa;
- como identificar banco errado/não migrado;
- como resetar uma base exclusivamente de desenvolvimento/teste com segurança;
- política de não existir credencial padrão.

## Estado

Planejado.
