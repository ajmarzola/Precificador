# Catálogo Inicial de Casos de Uso

O catálogo define backlog e dependências. UCs devem ser detalhados antes de implementação.

## Fundações

- FT001 — Fundação Técnica — implementada;
- FT002 — Fundação Multiempresa e Autenticação — próxima implementação.

## Insumos

| UC | Nome | Dependências |
|---|---|---|
| UC001 | Cadastrar insumo | FT001 — implementado |
| UC001A | Marca e observação do insumo | FT002 |
| UC001B | Generalizar classificação e unidades do insumo | FT002, UC001A |
| UC002 | Listar e consultar insumos | FT002, UC001A, UC001B |
| UC003 | Editar insumo | UC002 |
| UC004 | Desativar insumo | UC003 |
| UC005 | Registrar preço de insumo | UC001B |
| UC006 | Consultar histórico de preços | UC005 |

## Produtos

UC007 a UC012 permanecem no backlog e serão tenant-aware.

## Ficha técnica

UC013 a UC017 permanecem, mas devem ser revalidados antes da implementação conforme `product/multiempresa-generalizacao.md`.

## Precificação

UC018 a UC025 permanecem. UC019/UC021 exigem detalhamento atualizado de perdas/equipamentos antes da implementação.

## Configurações

UC026/UC027 operam por Empresa.

## Dashboard

UC028 a UC030 operam exclusivamente sobre a Empresa Ativa.

## Administração multiempresa

- UC031 — Cadastrar e consultar empresas — a detalhar;
- UC032 — Cadastrar usuários e gerenciar vínculos usuário-empresa — a detalhar.

O bootstrap, login/logout e seleção mínima de empresa são parte da FT002 por serem pré-requisito transversal de segurança.

## Pós-MVP

Preparações intermediárias reutilizáveis, backup UI e gráficos históricos avançados continuam não autorizados até nova decisão.
