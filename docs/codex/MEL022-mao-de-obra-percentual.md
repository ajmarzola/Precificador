# Codex — MEL022 — Mão de obra percentual

Implemente exclusivamente a MEL022 conforme:

```text
docs/development/improvements/MEL022-mao-de-obra-percentual.md
```

## Branch obrigatória

```text
feat/mel022-mao-de-obra-percentual
```

Nunca editar `master` diretamente.

## Objetivo

Substituir integralmente:

```text
TempoAtivoMinutos × ValorHoraTrabalho
```

por:

```text
CustoBaseItens × PercentualMaoDeObra
```

Não implementar MEL023 nem publicação Azure.

## Decisões obrigatórias

### Percentual

Criar:

```text
ConfiguracaoPrecificacaoEmpresa.PercentualMaoDeObra
```

- `decimal` obrigatório;
- precisão EF `decimal(9,6)`;
- armazenado como fração;
- default `0.10m`;
- validação: `>= 0`;
- **não** limitar a <1;
- 100%, 150%, 250% são válidos.

Remover:

```text
ValorHoraTrabalho
```

### Ficha

Remover completamente:

```text
TempoAtivoMinutos
```

do domínio, EF, migration, UI, DTOs e testes ativos.

`FichaTecnica` passa a criar/atualizar apenas Rendimento como base.

MEL019 continua valendo para default visual de Rendimento=1 no GET sem Ficha.

### Calculadora

Preferir manter:

```text
CalculadoraCustoMaoDeObra
```

Nova entrada:

```text
decimal? custoBaseItens
decimal percentualMaoDeObra
```

Regras:

- custo null -> null/incompleto;
- custo conhecido × 0 -> 0/completo;
- percentual negativo -> exceção;
- percentual sem teto superior;
- custo negativo -> rejeição defensiva;
- sem arredondamento intermediário.

### Orquestração

Usar o `CustoBaseItens` produzido por UC018.

Não usar perdas/energia/rendimento na base.

Atualizar `ResultadoPrecificacaoProdutoAtual`:

remover:
- TempoAtivoMinutos;
- ValorHoraTrabalho.

adicionar:
- PercentualMaoDeObra.

### Configurações

UI:

```text
Mão de obra sobre os insumos (%)
```

Entrada humana:
- 10 -> 10%;
- 150 -> 150%;
- vírgula/ponto conforme MEL015;
- vazio inválido;
- negativo inválido;
- zero válido.

Atualizar ajuda MEL017 explicando que a base é CustoBaseItens e que >100% é permitido.

### Migration

Criar migration evolutiva SQL Server, sem rebaseline.

Up:
1. adicionar PercentualMaoDeObra NOT NULL decimal(9,6) default 0.10;
2. garantir 0.10 nas configurações existentes;
3. remover ValorHoraTrabalho;
4. remover TempoAtivoMinutos;
5. atualizar snapshot.

Não converter os valores antigos.

Down pode recriar:
- ValorHoraTrabalho nullable;
- TempoAtivoMinutos NOT NULL default 0;
- remover PercentualMaoDeObra.

Não é necessário recuperar dados antigos perdidos; documentar a irreversibilidade semântica.

### Histórico comercial

Não alterar `RegistroPrecoProduto`.

Não adicionar PercentualMaoDeObraReferencia.

Não recalcular registros históricos.

`CustoReferencia` existente continua sendo o snapshot final de custo.

## Documentação obrigatória na implementação

Alinhar, no mínimo:

- RN013;
- RN025;
- UC013;
- UC017;
- UC020;
- UC022;
- UC025;
- UC026;
- UC027;
- F003;
- F004;
- F006;
- MEL017;
- MEL019 com nota de supersessão do TempoAtivo.

Documentos históricos podem manter descrição antiga apenas se claramente marcados como superados pela MEL022.

## Testes mínimos

### Unitários

- 12 × 10% = 1.2;
- 12 × 0% = 0;
- 12 × 100% = 12;
- 12 × 250% = 30;
- null × 10% => incompleto;
- precisão alta;
- negativos rejeitados.

### Migration/persistência

- baseline MEL020 -> migration MEL022;
- existentes recebem 10%;
- colunas antigas removidas;
- nova coluna NOT NULL decimal(9,6);
- seed Empresa 1 = 10%;
- CriarPadrao nova Empresa = 10%;
- GQF e guard preservados.

### Web

- Ficha sem TempoAtivo;
- Configuração mostra/edita Percentual;
- vazio inválido;
- 150% válido;
- vírgula/ponto;
- cálculo na Ficha;
- item sem preço => mão de obra indisponível;
- detalhamento sem Tempo/ValorHora e com percentual/base;
- Produto inativo;
- isolamento tenant.

### Histórico

- registro antigo não muda;
- percentual atual não reinterpreta histórico;
- novo registro usa novo CustoReferencia atual.

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
- suíte SQL Server completa verde;
- MEL022 -> Concluído;
- MEL023 continua Planejado;
- MEL021 continua Especificado/bloqueada;
- nenhuma mudança Azure.
