# Ordem Inicial de Implementação

A ordem prioriza dependências do domínio e entrega incremental. Não representa calendário rígido.

## Etapa 0 — Fundação técnica

FT001 concluída: solution, projetos, EF Core/SQLite, testes, logging e CI.

## Etapa 0.5 — Primeiro caso funcional

UC001 — Cadastrar Insumo — implementado.

## Etapa 0.6 — FT002 Multiempresa e Autenticação

FT002 concluída: Empresa, ASP.NET Core Identity, UsuarioEmpresa, bootstrap inicial, login/logout, Empresa Ativa, isolamento tenant-aware e `EmpresaId` em Insumo.

Especificação: [`foundation-multiempresa-auth.md`](foundation-multiempresa-auth.md).

## Etapa 1 — Insumos

Estado atual:

1. **UC001B — Generalizar categoria e unidades** — implementado;
2. **UC001A — Marca e Observação** — implementado, com unicidade `(EmpresaId, NomeNormalizado, MarcaNormalizada)`;
3. **UC002 — Listar e consultar Insumos** — implementado;
4. **UC003 — Editar Insumo** — implementado;
5. **UC004 — Desativar e reativar Insumo** — implementado;
6. próximo: detalhar/revalidar UC005 e depois seguir UC006.

Especificação UC004: [`../use-cases/UC004-desativar-reativar-insumo.md`](../use-cases/UC004-desativar-reativar-insumo.md).

Instrução Codex UC004: [`../codex/UC004-desativar-reativar-insumo.md`](../codex/UC004-desativar-reativar-insumo.md).

Objetivo: possuir catálogo e histórico de preços confiável, genérico e isolado por Empresa antes de precificar Produtos.

A revalidação anterior ao UC005 foi concluída pela RN040: após o primeiro preço, Nome, Marca e Unidade base tornam-se imutáveis. O UC005 deve implementar essa restrição junto ao histórico de preços.

O gate específico de referências de Ficha Técnica permanece para o UC014.

## Etapa 2 — Produtos

UC007 a UC012. Todo Produto será tenant-owned.

## Etapa 3 — Ficha técnica

UC013 a UC017 devem ser revalidados antes da implementação para adotar o modelo produtivo genérico registrado em `../product/multiempresa-generalizacao.md`.

UC014 também deve revalidar as restrições de edição dos dados de Insumo após existirem referências em fichas técnicas.

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

Detalhar/revalidar o **UC005 — Registrar preço de insumo**, usando a RN040 como contrato: o primeiro preço consolida Nome, Marca e Unidade base do Insumo. Preservar o gate separado do UC014 para futuras referências de Ficha Técnica.
