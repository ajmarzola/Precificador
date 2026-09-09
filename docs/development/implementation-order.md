# Ordem Inicial de Implementação

A ordem prioriza dependências do domínio e entrega incremental. Não representa calendário rígido.

## Etapa 0 — Fundação técnica

- criar `Precificador.sln`;
- criar projetos de produção e teste;
- configurar referências;
- configurar .NET 10;
- configurar EF Core + SQLite;
- migration inicial quando o primeiro modelo persistente existir;
- logging padrão;
- seed mínimo de desenvolvimento;
- infraestrutura de testes.

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

## Próximo passo após esta fundação

Detalhar a **Fundação Técnica** como primeira instrução executável pelo Codex, sem iniciar ainda os UCs funcionais. Depois dela, preparar `UC001-cadastrar-insumo.md`.
