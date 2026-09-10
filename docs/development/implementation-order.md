# Ordem Inicial de Implementação

A ordem prioriza dependências do domínio e entrega incremental.

## Etapa 0 — FT001 Fundação Técnica

Concluída.

## Etapa 0.5 — UC001 Cadastro básico de Insumo

Concluído.

## Etapa 0.6 — FT002 Fundação Multiempresa e Autenticação

Executar antes de qualquer evolução adicional de Insumos.

Objetivo:

- Empresa;
- Identity;
- UsuarioEmpresa;
- bootstrap inicial;
- login/logout;
- Empresa Ativa;
- isolamento centralizado;
- adicionar EmpresaId ao Insumo;
- preservar banco UC001.

Especificação: [`foundation-multiempresa-auth.md`](foundation-multiempresa-auth.md).

Instrução: [`../codex/FT002-fundacao-multiempresa-autenticacao.md`](../codex/FT002-fundacao-multiempresa-autenticacao.md).

## Etapa 1 — Evoluções de Insumo

Ordem:

1. UC001A — Marca e Observação, já especificado, mas depende agora da FT002;
2. UC001B — Generalizar classificação/unidades para os dois negócios; detalhar após inventário real de insumos;
3. UC002 a UC006.

UC002 não deve ser implementado antes dessas correções porque sua UI e consultas devem nascer tenant-aware e com vocabulário genérico.

## Etapa 2 — Produtos

UC007 a UC012. Todo Produto será tenant-owned.

## Etapa 3 — Ficha Técnica genérica

UC013 a UC017 devem ser revalidados antes da implementação para remover suposições específicas de panificação e detalhar uso de equipamento/perdas.

## Etapa 4 — Motor de precificação

UC018 a UC025. Revalidar especialmente UC019 (perdas) e UC021 (energia/equipamentos) antes da implementação.

## Etapa 5 — Configurações

UC026/UC027 passam a operar por Empresa.

## Etapa 6 — Dashboard

UC028 a UC030 sempre filtrados pela Empresa Ativa.

## Administração multiempresa

Adicionar ao backlog funcional, a detalhar antes da implementação:

- UC031 — Cadastrar/consultar empresas;
- UC032 — Cadastrar usuários e gerenciar vínculos usuário-empresa.

O login mínimo e o bootstrap pertencem à FT002 porque são pré-requisito de isolamento para todos os UCs seguintes.

## Regra de tamanho

Se um incremento não puder ser implementado, testado e revisado de forma pequena, dividir antes do Codex.

## Próximo passo

Implementar e revisar **FT002**. Não executar UC001A antes da FT002 estar mergeada.
