# Ordem Inicial de Implementação

A ordem prioriza dependências do domínio e entrega incremental. Não representa calendário rígido.

## Etapa 0 — Fundação técnica

FT001 concluída: solution, projetos, EF Core/SQLite, testes, logging e CI.

## Etapa 0.5 — Primeiro caso funcional

UC001 — Cadastrar Insumo — implementado.

## Etapa 0.6 — FT002 Multiempresa e Autenticação

FT002 concluída: Empresa, ASP.NET Core Identity, UsuarioEmpresa, bootstrap inicial, login/logout, Empresa Ativa, isolamento tenant-aware e `EmpresaId` em Insumo.

Especificação: [`foundation-multiempresa-auth.md`](foundation-multiempresa-auth.md).

Instrução Codex: [`../codex/FT002-fundacao-multiempresa-autenticacao.md`](../codex/FT002-fundacao-multiempresa-autenticacao.md).

## Etapa 1 — Insumos

Estado atual:

1. **UC001B — Generalizar categoria e unidades de insumo** — implementado: `MateriaPrima = 1`, `Metro = 4` e compatibilidade preservada;
2. **UC001A — Marca e Observação** — especificação revalidada após FT002/UC001B e pronta para implementação;
3. após UC001A, seguir UC002 a UC006 já usando identidade tenant-aware por Empresa + Nome + Marca.

Especificação UC001A: [`../use-cases/UC001A-complementar-insumo-marca-observacao.md`](../use-cases/UC001A-complementar-insumo-marca-observacao.md).

Instrução Codex UC001A: [`../codex/UC001A-complementar-insumo-marca-observacao.md`](../codex/UC001A-complementar-insumo-marca-observacao.md).

Objetivo: possuir catálogo e histórico de preços confiável, genérico e isolado por Empresa antes de precificar Produtos.

## Etapa 2 — Produtos

UC007 a UC012. Todo Produto será tenant-owned.

## Etapa 3 — Ficha técnica

UC013 a UC017 devem ser revalidados antes da implementação para adotar o modelo produtivo genérico registrado em `../product/multiempresa-generalizacao.md`.

## Etapa 4 — Motor de precificação

UC018 a UC025. Revalidar especialmente perdas e energia/equipamentos antes de implementar seus UCs.

## Etapa 5 — Configurações

UC026 e UC027 passam a operar por Empresa e podem ser antecipados quando necessários aos cálculos.

## Etapa 6 — Dashboard

UC028 a UC030 operam exclusivamente sobre a Empresa Ativa.

## Administração multiempresa

O backlog deverá detalhar posteriormente:

- cadastro/consulta de empresas;
- cadastro de usuários e gestão de vínculos usuário-empresa.

O bootstrap e login mínimos pertencem à FT002 porque são pré-requisitos transversais de isolamento.

## Melhorias não bloqueantes

Melhorias identificadas durante implementação e revisão que não bloqueiam as histórias principais ficam em [`melhorias.md`](melhorias.md) e podem ser executadas quando houver oportunidade técnica adequada.

## Regra de tamanho

Se um UC não puder ser implementado, testado e revisado como um incremento pequeno, deve ser dividido antes de ser enviado ao agente.

## Próximo passo

Implementar e revisar o **UC001A — Complementar cadastro de Insumo com Marca e Observação** usando a especificação revalidada. Não iniciar UC002 antes do UC001A estar concluído.
