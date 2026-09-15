# Estratégia de Implementação

Este documento preserva o racional duradouro de sequenciamento do projeto. A fila operacional, os estados correntes e os gates de cada item ficam somente em [`backlog.md`](backlog.md).

## Princípios

- entregar incrementos pequenos, testáveis e revisáveis;
- priorizar dependências de domínio antes de telas de decisão comercial;
- evitar antecipar schema ou comportamento fora do UC/MEL em execução;
- manter dependências materiais no catálogo funcional e ordem operacional no backlog.

## Fundação

FT001 estabelece a base técnica do monólito local-first com EF Core, SQLite, testes, logging e CI.

FT002 estabelece Empresa, autenticação, vínculo usuário-empresa, Empresa Ativa e isolamento tenant-aware. O bootstrap/login mínimo pertence à fundação porque é pré-requisito transversal; CRUD administrativo completo será detalhado depois.

## Insumos Antes de Produtos

O domínio de Insumos vem antes de Produtos porque o motor de custo depende de catálogo, unidades, situação e histórico de preços confiáveis.

A sequência funcional de Insumos consolidou:

- cadastro e vocabulário de categoria/unidade;
- marca e observação;
- listagem, consulta, edição e situação;
- histórico append-only de preços;
- data operacional por Empresa para regras de vigência.

A RN040 protege Nome, Marca e Unidade base após o primeiro preço. A RN048 estende a proteção ao primeiro uso em Ficha, e a RN051 consolida a decisão de imutabilidade permanente.

## Produtos Antes da Precificação Comercial

Produto cadastral existe antes de Ficha Técnica e antes de preço comercial.

O cadastro de Produto mantém Nome, Categoria, Margem-alvo e situação. Preço de prateleira e histórico comercial ficam separados porque dependem do cálculo completo de custo e preço sugerido.

UC011 e UC012 permanecem no domínio Produtos, mas só devem ocorrer depois do cálculo até UC023, pois o registro comercial deve congelar Custo de referência, Margem de referência, Preço sugerido e Reserva comercial de referência.

## Ficha Técnica Antes do Motor de Custo

Ficha Técnica concentra a composição produtiva atual usada pelo motor de custo.

O núcleo da Ficha nasce com Rendimento e TempoAtivoMinutos. Depois entram Itens de Ficha e edição de Quantidade/Observação. A remoção de Item depende da MEL010 para garantir que remover a última referência não desbloqueie Nome, Marca ou Unidade base do Insumo.

## Configurações Antes do Motor Completo

Configurações de precificação da Empresa devem existir antes dos cálculos que dependem delas:

- custo de mão de obra;
- custo de energia/equipamentos;
- arredondamento do Preço sugerido;
- Reserva comercial do Desconto de referência definida pela MEL009.

Por isso UC026/UC027 antecedem o bloco completo de cálculo de custo e preço sugerido.

## Motor de Custo e Preço

O motor de custo deve evoluir em fatias que preservem explicabilidade:

- itens do lote;
- perdas aplicáveis;
- mão de obra;
- energia/equipamentos;
- custo total e unitário;
- preço teórico e sugerido.

Perdas e energia/equipamentos foram revalidadas antes da implementação para evitar modelagem específica demais para um único tipo de negócio. Energia foi generalizada por uso de equipamento; perdas foram generalizadas por Item da Ficha, com perda de saída representada pelo Rendimento.

## Decisão Comercial e Histórico

Depois do preço sugerido existir, o usuário registra somente o Preço de prateleira.

O sistema determina a data de referência e congela os valores calculados que explicam a decisão comercial. O histórico é append-only, admite correções por múltiplos registros na mesma data e não reinterpreta registros antigos quando ficha, custos ou configurações mudam.

## Dashboard

O dashboard depende de margem atual, preço, ficha e precificação incompleta. Ele opera exclusivamente sobre a Empresa Ativa e deve ser construído depois dos cálculos e consultas que alimentam seus indicadores.

## Melhorias

Melhorias não bloqueantes ficam catalogadas em [`melhorias.md`](melhorias.md). Quando uma melhoria vira pré-requisito obrigatório, ela deve aparecer na fila principal de [`backlog.md`](backlog.md).

## Regra de Tamanho

Se um UC não puder ser implementado, testado e revisado como um incremento pequeno, deve ser dividido antes de ser enviado ao agente.
