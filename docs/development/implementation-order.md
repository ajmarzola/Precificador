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

O gate específico de referências de Ficha Técnica permanece para o UC014.

## Etapa 2 — Produtos

UC007 a UC012. Todo Produto será tenant-owned.

Estado documental:

1. **UC007 — Cadastrar Produto** — revalidado e pronto para implementação, próximo caso após merge do UC006;
2. **UC008 — Listar e consultar Produtos** — revalidado e pronto documentalmente; implementar somente após UC007;
3. UC009 — Editar Produto;
4. UC010 — Desativar Produto;
5. UC011 — Alterar preço de venda preservando histórico;
6. UC012 — Consultar histórico de preço de venda.

Especificação UC007: [`../use-cases/UC007-cadastrar-produto.md`](../use-cases/UC007-cadastrar-produto.md).

Instrução Codex UC007: [`../codex/UC007-cadastrar-produto.md`](../codex/UC007-cadastrar-produto.md).

Especificação UC008: [`../use-cases/UC008-listar-consultar-produtos.md`](../use-cases/UC008-listar-consultar-produtos.md).

Instrução Codex UC008: [`../codex/UC008-listar-consultar-produtos.md`](../codex/UC008-listar-consultar-produtos.md).

O UC007 não depende materialmente do UC006, mas sua implementação respeita a fila serial do projeto: primeiro concluir Insumos, depois iniciar Produtos. O UC008 permanece bloqueado para implementação até o UC007 estar implementado/revisado/mergeado.

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

Revisar e mergear o **UC006 — Consultar histórico de preços do insumo**. Após seu merge, a etapa de Insumos fica funcionalmente fechada e o **UC007 — Cadastrar produto** passa a ser o próximo caso de implementação. O gate separado do UC014 permanece para futuras referências de Ficha Técnica.
