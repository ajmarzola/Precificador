# Generalização multiempresa e de produção

## Objetivo

Registrar a evolução do Precificador para atender empresas de produção artesanal com processos diferentes, sem transformar o sistema em ERP nem duplicar motores de cálculo.

## Multiempresa

Dados operacionais pertencem a uma Empresa. Usuários acessam somente empresas com vínculo ativo e trabalham no contexto de uma Empresa Ativa.

Entidades tenant-owned previstas:

- Insumo;
- histórico de preço do Insumo;
- Produto;
- Ficha Técnica e seus itens;
- histórico de preço de venda;
- configurações de precificação;
- equipamentos/recursos de produção quando modelados.

## Ficha Técnica genérica

A Ficha Técnica representa a composição e os recursos necessários para uma execução/lote de um produto.

Ela não deve representar especificamente uma receita de panificação.

Conceitos comuns:

- rendimento;
- materiais/insumos e quantidades;
- observação contextual por item;
- tempo ativo de trabalho;
- perdas de material/processo quando aplicáveis;
- uso de equipamentos quando aplicável.

## Equipamentos

`TempoForno` e `PotenciaFornoKw` não devem ser conceitos universais.

A evolução esperada é modelar uso de equipamento de forma genérica:

```text
Equipamento
- EmpresaId
- Nome
- PotenciaKw
- Ativo

UsoEquipamentoFicha
- FichaTecnicaId
- EquipamentoId
- TempoMinutos
```

Assim, forno, impressora, laminadora ou outro equipamento podem usar a mesma fórmula de energia sem tornar qualquer um obrigatório.

O modelo exato será fechado antes dos UCs de ficha técnica/energia.

## Perdas

Perda não deve ser atributo obrigatório específico de panificação no Produto.

Ela deve ser tratada como perda de material/processo quando aplicável. O local exato do percentual e regras de aplicação serão fechados antes do UC019, considerando os dois negócios.

## Mão de obra

Tempo ativo e valor/hora são conceitos gerais e permanecem no modelo de precificação.

## Insumos

A generalização revelou que `Ingrediente` como categoria e apenas `g/ml/un` como unidades não são suficientes para negócios não alimentícios.

Essa alteração será tratada separadamente no UC001B, baseada em uma lista real de insumos das empresas. A FT002 não deve inventar unidades antecipadamente.

## Configurações

Configurações deixam de ser globais da aplicação e passam a pertencer a uma Empresa.

Parâmetros específicos de equipamento pertencem ao Equipamento, não às configurações globais da empresa.
