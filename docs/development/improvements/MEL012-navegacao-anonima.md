# MEL012 — Adequar navegação para usuário anônimo

- **Origem:** Review MVP 2026-09-16
- **Classificação:** UX
- **Prioridade:** baixa
- **Dependência:** FT002

## Problema

O layout apresenta links operacionais como Insumos, Produtos e Configurações para usuário não autenticado.

As rotas permanecem protegidas; o problema é de experiência de navegação, não de segurança.

## Objetivo

Apresentar ao usuário anônimo somente ações compatíveis com seu estado de autenticação.

## Direção

- ocultar menus tenant-owned quando `User.Identity.IsAuthenticated != true`;
- manter Login/Setup conforme o estado aplicável;
- após autenticação, preservar navegação operacional atual;
- não substituir regras de autorização por ocultação de menu.

## Estado

Planejado.
