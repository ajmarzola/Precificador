# Ordem Inicial de Implementação

A ordem prioriza dependências do domínio e entrega incremental. Não representa calendário rígido.

## Etapa 0 — Fundação técnica

Especificação: [`foundation-technical.md`](foundation-technical.md).

- criar `Precificador.slnx`;
- criar projetos de produção e teste;
- configurar referências;
- configurar .NET 10;
- configurar EF Core + SQLite;
- configurar tool manifest com `dotnet-ef`;
- registrar logging padrão;
- configurar infraestrutura de testes e smoke tests;
- configurar GitHub Actions para restore/build/test;
- ajustar `.gitignore` e `.editorconfig`;
- não criar migration antes do primeiro modelo persistente;
- não criar seed vazio; iniciar seed de desenvolvimento quando existir a primeira entidade que o justifique.

Instrução Codex: [`../codex/FT001-fundacao-tecnica.md`](../codex/FT001-fundacao-tecnica.md).

## Etapa 1 — Insumos

UC001 a UC006.

Objetivo: possuir catálogo e histórico de preços confiável antes de tentar precificar produtos.

## Etapa 2 — Produtos

UC007 a UC012.

Objetivo: manter produtos, margem-alvo e preço praticado com histórico.

## Etapa 3 — Ficha técnica

UC013 a UC017.

Objetivo: representar um lote, rendimento, tempos e sua composição de insumos.

## Etapa 4 — Motor de precificação

UC018 a UC025.

Objetivo: implementar regras financeiras com forte cobertura unitária e golden cases.

## Etapa 5 — Configurações

UC026 e UC027 podem ser antecipados quando forem necessários aos UCs de mão de obra e energia. A dependência será resolvida antes desses cálculos.

## Etapa 6 — Dashboard

UC028 a UC030.

Objetivo: transformar os cálculos já estabilizados em visão operacional dos produtos que exigem atenção.

## Regra de tamanho

Se um UC não puder ser implementado, testado e revisado como um incremento pequeno, deve ser dividido antes de ser enviado ao agente.

## Próximo passo

Executar e revisar a **FT001 — Fundação Técnica**. Somente após sua aprovação, detalhar `UC001-cadastrar-insumo.md` e gerar a respectiva instrução para Codex.
