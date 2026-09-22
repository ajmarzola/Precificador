# Codex — UC036 — Configurar e calcular custo de desgaste de equipamentos por Categoria

Implemente exclusivamente a UC036 conforme:

```text
docs/use-cases/UC036-configurar-calcular-desgaste-equipamentos-categoria.md
```

## Branch obrigatória

```text
feat/uc036-desgaste-equipamentos-categoria
```

Nunca editar `master` diretamente.

## Objetivo

Adicionar à `CategoriaProduto` a configuração de desgaste de equipamentos e incorporar o novo componente ao custo atual do lote.

Não implementar Azure/MEL021.

## Modelo obrigatório

Adicionar enum equivalente a:

```csharp
FormaCalculoDesgasteEquipamento
{
    ValorFixoPorLote = 1,
    PercentualSobreInsumos = 2
}
```

Adicionar à Categoria:

```text
FormaCalculoDesgasteEquipamento
ValorDesgasteEquipamento
```

Persistência:

```text
Forma -> int NOT NULL
Valor -> decimal(18,6) NOT NULL
```

Validação:

```text
Forma válida
Valor >= 0
```

Percentual é fração:

```text
5% = 0.05
125% = 1.25
```

Não impor teto de 100%.

## Migration

Criar migration evolutiva após UC032.

Todas as Categorias existentes:

```text
Forma = ValorFixoPorLote
Valor = 0
```

Preservar:

- Id;
- EmpresaId;
- Nome/NomeNormalizado;
- Ativo;
- Produtos vinculados.

Adicionar integridade SQL para Forma válida e Valor não negativo.

Não editar migrations históricas.

O ValorFixo/0 das Categorias existentes é backfill de compatibilidade. Não deixar a criação futura depender silenciosamente de default SQL; a aplicação/domínio deve fornecer Forma + Valor explicitamente.

## Criação/edição de Categoria

Novo:

```text
Forma default visual = Valor fixo por lote
Valor default visual = 0,00
```

Após UC036, a criação de Categoria deve receber Forma + Valor explicitamente.

Editar:

- Nome;
- Forma;
- Valor;
- preservar Id/EmpresaId/Ativo;
- permitir Categoria inativa;
- alteração inválida não pode persistir estado parcial.

Preferir operação de domínio atômica para atualizar os três valores.

## Web

Labels:

```text
Forma de cálculo do desgaste
Valor do desgaste
```

Opções:

```text
Valor fixo por lote
Percentual sobre os insumos
```

Entrada:

```text
Valor fixo: "1,50" => 1.50
Percentual: "5" => 0.05
Percentual: "12,5" => 0.125
```

Usar parsing pt-BR compatível com MEL015.

GET de edição deve converter fração percentual para percentual humano.

Listagem deve resumir, por exemplo:

```text
R$ 1,50 por lote
5% sobre os insumos
```

Não introduzir dependência JS. Sufixo dinâmico R$/% é opcional.

## Cálculo puro

Criar `CalculadoraCustoDesgasteEquipamentos` ou nome equivalente no Core.

Regras exatas:

```text
sem Categoria
=> 0 / completo

ValorFixoPorLote
=> Valor / completo

PercentualSobreInsumos + Valor = 0
=> 0 / completo

PercentualSobreInsumos + Valor > 0 + CustoBaseItens conhecido
=> CustoBaseItens * Valor / completo

PercentualSobreInsumos + Valor > 0 + CustoBaseItens null
=> null / incompleto
```

Sem arredondamento intermediário.

Não usar:

- perdas;
- mão de obra;
- energia;
- rendimento;
- margem;
- preço;
- UsoEquipamentoFicha.

## Custo total

Alterar `CalculadoraCustoProduto`:

```text
CustoLote =
    CustoBaseItens
  + CustoPerdasLote
  + CustoMaoDeObraLote
  + CustoEnergiaLote
  + CustoDesgasteEquipamentosLote
```

Qualquer componente null torna CustoLote/CustoUnitario null.

Zero é valor conhecido.

Não duplicar a fórmula em PageModel.

## PrecificacaoProdutoAtual

Carregar a configuração da Categoria junto ao Produto/projeção equivalente, tenant-aware.

Não usar `IgnoreQueryFilters` no fluxo comum.

Calcular desgaste após `CustoBaseItens` estar disponível e antes de `CalculadoraCustoProduto`.

Expor no resultado pelo menos:

```text
FormaCalculoDesgasteEquipamento?
ValorDesgasteEquipamento?
CustoDesgasteEquipamentosLote?
```

ou estrutura equivalente suficiente para Ficha/UC025.

Produto sem Categoria deve produzir custo de desgaste zero, não null.

Categoria inativa vinculada continua válida para cálculo.

## Ficha Técnica

Exibir:

- Forma;
- Valor/percentual configurado;
- base quando percentual;
- Custo de desgaste do lote.

Sem Categoria:

```text
Custo de desgaste do lote: R$ 0,00
Produto sem categoria: nenhum desgaste por categoria aplicado.
```

Percentual positivo com base indisponível:

```text
Custo de desgaste do lote: indisponível
```

Não adicionar botão de edição da Categoria na Ficha.

## UC025 — detalhamento

Adicionar bloco/linhas de desgaste e incluir o componente na Consolidação do custo.

Não esconder zero.

Não transformar null em zero.

## Snapshots

Não alterar schema de `RegistroPrecoProduto`.

`CustoReferencia` já recebe `CustoUnitarioProduto` atual.

Depois da UC036:

- novos snapshots incluem desgaste indiretamente no custo final;
- snapshots antigos permanecem inalterados;
- histórico não recalcula componente antigo.

Adicionar testes de regressão para provar isso.

## Energia é independente

Não modificar:

- `UsoEquipamentoFicha`;
- `CalculadoraCustoEnergia`;
- TarifaEnergiaKwh;
- regras MEL023.

Desgaste não depende de equipamento elétrico cadastrado.

## Documentação obrigatória

Na implementação alinhar:

- RN015;
- RN017;
- RN043;
- criar RN057/RN058;
- F002;
- F003;
- F004;
- UC022;
- UC025;
- UC036 -> Concluído;
- catálogo;
- backlog;
- MEL021.

Após conclusão da UC036, MEL021 deixa de depender de código funcional pendente, mas **continua bloqueada externamente pela conta Azure**; não marcá-la Pronto enquanto o bloqueio externo persistir.

## Testes

Implementar a matriz da especificação.

Pontos críticos:

- migration de Categorias existentes -> ValorFixo/0;
- Categoria inativa preservada;
- valor fixo é do lote;
- percentual usa só `CustoBaseItens`;
- zero percentual com base null;
- percentual positivo com base null;
- Produto sem Categoria = zero;
- composição de cinco componentes;
- cálculo atual após alterar Categoria;
- snapshots antigos imutáveis;
- novos snapshots refletem novo custo;
- cross-tenant;
- parsing pt-BR de moeda e percentual.

Não trocar integração SQL Server por SQLite/InMemory.

## Validação final

Executar:

```text
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
```

Confirmar:

- 0 erros;
- nenhum warning novo relevante;
- unitários verdes;
- integração SQL Server/Web verde;
- zero migrations pendentes;
- nenhum código Azure;
- nenhum novo snapshot específico de desgaste;
- UC036 Concluído;
- MEL021 ainda bloqueada externamente.

Ao finalizar, informar:

- migration criada;
- campos/enum adicionados;
- fórmula implementada;
- tratamento de Categorias existentes;
- contagem de testes;
- confirmação de que snapshots históricos não foram reinterpretados.
