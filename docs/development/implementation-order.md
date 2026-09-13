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
6. **UC005 — Registrar preço de Insumo** — implementado;
7. **UC006 — Consultar histórico de preços do insumo** — implementado.

Especificação UC006: [`../use-cases/UC006-consultar-historico-precos-insumo.md`](../use-cases/UC006-consultar-historico-precos-insumo.md).

Instrução Codex UC006: [`../codex/UC006-consultar-historico-precos-insumo.md`](../codex/UC006-consultar-historico-precos-insumo.md).

Objetivo: possuir catálogo e histórico de preços confiável, genérico e isolado por Empresa antes de precificar Produtos.

A MEL006 foi concluída e o UC006 consome `IDataOperacionalEmpresa` para definir vigência/futuro com a data operacional da Empresa, sem depender do timezone do servidor.

A revalidação anterior ao UC005 foi concluída pela RN040, e o UC005 implementou a restrição: após o primeiro preço, Nome, Marca e Unidade base tornam-se imutáveis.

O gate específico de referências de Ficha Técnica foi concluído na revalidação pós-UC013: RN048 protege Nome, Marca e Unidade base enquanto houver referência atual em Ficha.

## Etapa 2 — Produtos cadastrais

Todo Produto é tenant-owned.

Estado documental:

1. **UC007 — Cadastrar Produto** — implementado;
2. **UC008 — Listar e consultar Produtos** — implementado;
3. **UC009 — Editar Produto** — implementado;
4. **UC010 — Desativar e reativar Produto** — implementado.

UC011 e UC012 continuam pertencendo ao domínio Produtos, porém foram deslocados para depois da UC023. O registro comercial definido para a UC011 deve congelar Custo de referência, Margem de referência e Preço sugerido calculados pelo sistema; por isso não deve ser implementado antes de existir o cálculo completo.

Especificação UC007: [`../use-cases/UC007-cadastrar-produto.md`](../use-cases/UC007-cadastrar-produto.md).

Instrução Codex UC007: [`../codex/UC007-cadastrar-produto.md`](../codex/UC007-cadastrar-produto.md).

Especificação UC008: [`../use-cases/UC008-listar-consultar-produtos.md`](../use-cases/UC008-listar-consultar-produtos.md).

Instrução Codex UC008: [`../codex/UC008-listar-consultar-produtos.md`](../codex/UC008-listar-consultar-produtos.md).

Especificação UC009: [`../use-cases/UC009-editar-produto.md`](../use-cases/UC009-editar-produto.md).

Instrução Codex UC009: [`../codex/UC009-editar-produto.md`](../codex/UC009-editar-produto.md).

Especificação UC010: [`../use-cases/UC010-desativar-reativar-produto.md`](../use-cases/UC010-desativar-reativar-produto.md).

Instrução Codex UC010: [`../codex/UC010-desativar-reativar-produto.md`](../codex/UC010-desativar-reativar-produto.md).

A revalidação obrigatória do UC010 contra a master real pós-UC009 foi concluída; CA10/W6 está confirmado e a implementação está liberada.

O UC007 respeitou a fila serial do projeto, foi revisado e mergeado. A revisão obrigatória pós-UC007 do UC008 foi concluída contra o modelo real de Produto, e o UC008 foi implementado sem alteração de schema.

O UC009 foi implementado contra a master pós-UC008. Ele edita Nome, Categoria e Margem-alvo do Produto, preserva ownership/status, não altera schema e não antecipa UC010+.

O UC010 implementou o ciclo reversível de desativação/reativação. Produto inativo continua editável, a edição preserva `Ativo` e a cobertura Web usa `Produto.Desativar()` sem bypass técnico.

## Etapa 3 — Ficha técnica

A revalidação genérica da UC013 foi concluída: este incremento fica restrito a Rendimento e TempoAtivoMinutos. UC014 a UC017 continuam sujeitos às revalidações específicas antes da implementação.

A UC014 foi implementada após revalidação contra a implementação real da UC013. O modelo de ItemFichaTecnica permanece coerente; Quantidade usa parsing Web explícito pt-BR/invariant seguindo o aprendizado da UC013.

Especificação UC013: [`../use-cases/UC013-definir-base-ficha-tecnica.md`](../use-cases/UC013-definir-base-ficha-tecnica.md).

Instrução Codex UC013: [`../codex/UC013-definir-base-ficha-tecnica.md`](../codex/UC013-definir-base-ficha-tecnica.md).

Especificação UC014 revalidada: [`../use-cases/UC014-adicionar-insumo-ficha.md`](../use-cases/UC014-adicionar-insumo-ficha.md).

Instrução Codex UC014: [`../codex/UC014-adicionar-insumo-ficha.md`](../codex/UC014-adicionar-insumo-ficha.md).

Especificação UC015 revalidada: [`../use-cases/UC015-alterar-item-ficha.md`](../use-cases/UC015-alterar-item-ficha.md).

Instrução Codex UC015: [`../codex/UC015-alterar-item-ficha.md`](../codex/UC015-alterar-item-ficha.md).

Ordem prevista:

1. **UC013 — Definir rendimento e tempo ativo da Ficha Técnica** — implementado;
2. **UC014 — Adicionar Insumo à Ficha Técnica** — implementado;
3. **UC015 — Alterar item da Ficha Técnica** — pronto para implementação;
4. UC016 — Remover item da Ficha Técnica;
5. UC017 — Consultar Ficha Técnica e composição.

## Etapa 4 — Configurações de precificação

Antecipar UC026 e UC027 antes do motor completo, pois mão de obra, energia/equipamentos e o arredondamento do Preço sugerido dependem de configurações da Empresa.

1. UC026 — Consultar configurações de precificação da Empresa;
2. UC027 — Alterar configurações de precificação da Empresa.

## Etapa 5 — Motor de custo e preço sugerido

Executar UC018 a UC023 antes da decisão comercial de preço.

1. UC018 — Calcular custo atual dos itens do lote;
2. UC019 — Calcular perdas aplicáveis — revalidar antes de implementar;
3. UC020 — Calcular custo de mão de obra;
4. UC021 — Calcular custo de energia/equipamentos — revalidar antes de implementar;
5. UC022 — Calcular custo total e custo unitário;
6. UC023 — Calcular preço teórico e sugerido.

## Etapa 6 — Decisão comercial e histórico de preço

1. UC011 — Registrar Preço de prateleira preservando snapshot de precificação;
2. UC012 — Consultar histórico de precificação do Produto.

A UC011 receberá do usuário somente o Preço de prateleira. Data de referência, Custo de referência, Margem de referência e Preço sugerido são determinados pelo sistema. O histórico é append-only, admite múltiplos registros na mesma data para correções, não aceita data futura e permite novo registro para Produto inativo sem reativá-lo.

## Etapa 7 — Margem e detalhamento

1. UC024 — Calcular margem atual e situação;
2. UC025 — Consultar detalhamento da precificação.

## Etapa 8 — Dashboard

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

Implementar e revisar a **UC015 — Alterar item da Ficha Técnica** em `feat/uc015-editar-item-ficha`, usando a especificação revalidada e `docs/codex/UC015-alterar-item-ficha.md`. Após o merge, especificar/revalidar a UC016. A UC011 permanece bloqueada até a conclusão da UC023.
