# Codex — MEL023 — Equipamentos elétricos / tarifa

Implemente exclusivamente a MEL023 conforme:

```text
docs/development/improvements/MEL023-equipamentos-eletricos-navegacao-tarifa.md
```

## Branch obrigatória

```text
fix/mel023-equipamentos-eletricos-tarifa
```

Nunca editar `master` diretamente.

## Objetivo

Refinar a semântica e a navegação da Ficha Técnica para deixar explícito que `UsoEquipamentoFicha` representa somente **equipamentos elétricos opcionais** usados no cálculo de energia.

Adicionar também o fluxo seguro:

```text
Ficha Técnica
-> salvar/validar Rendimento
-> Configurações de Precificação
-> configurar TarifaEnergiaKwh
-> retornar à Ficha
```

Não alterar fórmula de energia, domínio, schema ou migrations.

Não implementar MEL021 nem UC028.

## Invariantes

Preservar UC021, RN014 e RN056:

```text
ConsumoKwh =
    PotenciaKw × (TempoUsoMinutos / 60m)

CustoEnergiaUso =
    ConsumoKwh × TarifaEnergiaKwh
```

Regras:

- `PotenciaKw > 0`;
- `TempoUsoMinutos > 0`;
- zero usos => energia 0/completo, mesmo com tarifa null;
- uso + tarifa null => consumo conhecido, custo indisponível/incompleto;
- tarifa 0 => custo 0/completo;
- sem arredondamento intermediário;
- consumo/custo calculado não são persistidos.

## Semântica da Ficha

Alterar a apresentação para comunicar explicitamente:

```text
Equipamentos elétricos (opcional)
```

O botão deve usar redação equivalente a:

```text
Adicionar equipamento elétrico
```

Novo/Editar/Remover também devem preferir “equipamento elétrico” na UX.

Não renomear classes, entidades, tabelas ou propriedades persistidas apenas por apresentação.

Ferramentas manuais não entram em `UsoEquipamentoFicha`.

Exemplos fora do cadastro elétrico:

- agulha de crochê;
- tesoura manual;
- régua;
- espátula;
- colher;
- forma;
- panela sem consumo elétrico próprio.

Nunca usar potência zero para representar ferramenta manual.

## Zero equipamentos

Quando a Ficha não possuir usos elétricos:

- manter `CustoEnergiaLote = 0`;
- manter componente completo;
- não exigir tarifa;
- não mostrar pendência “Tarifa de energia não configurada”;
- explicar na UI que nenhum cadastro é necessário quando o processo não utiliza equipamento elétrico.

## Equipamento com tarifa ausente

Quando houver pelo menos um uso e `TarifaEnergiaKwh = null`:

- mostrar consumo em kWh normalmente;
- custo por uso permanece indisponível;
- custo de energia do lote permanece indisponível;
- exibir “Tarifa de energia não configurada”;
- exibir ação “Configurar tarifa de energia”.

Tarifa igual a zero é configuração válida e não deve mostrar a pendência.

## Salvar antes de navegar

A ação “Configurar tarifa de energia” **não pode ser um link GET direto** que descarte alterações da Ficha.

A Ficha atualmente edita somente:

```text
Rendimento
```

Criar handler POST equivalente a:

```text
OnPostConfigurarTarifaAsync
```

O comando pode usar `formaction` ou handler Razor Pages equivalente, mas deve submeter o mesmo formulário da Ficha.

Fluxo obrigatório:

1. receber o POST da Ficha;
2. resolver Produto/Ficha tenant-aware;
3. validar Rendimento usando a mesma regra/helper do POST normal;
4. se inválido:
   - HTTP 200/Page;
   - preservar Input;
   - preservar erros;
   - não persistir;
   - não navegar;
5. se válido:
   - criar/atualizar Ficha exatamente como no fluxo normal;
   - salvar;
   - redirecionar para `/Configuracoes/Precificacao/Editar`;
   - enviar `returnUrl` local da Ficha atual.

Evitar duplicar lógica divergente de persistência entre `OnPostAsync` e o novo handler.

Produto inativo continua suportado.

Antiforgery continua obrigatório.

## returnUrl em Configurações

Evoluir:

```text
/Configuracoes/Precificacao/Editar
```

para aceitar `returnUrl` opcional.

### Segurança obrigatória

Aceitar o retorno somente quando:

```text
Url.IsLocalUrl(returnUrl)
```

ou validação equivalente.

Proibido redirecionar para:

- URL absoluta externa;
- URL protocol-relative externa;
- host recebido do cliente;
- qualquer origem não local.

Nunca usar `Redirect(returnUrl)` sem validação prévia.

Se `returnUrl` estiver ausente ou inválida, manter o destino padrão atual:

```text
/Configuracoes/Precificacao
```

### GET

- carregar configuração como hoje;
- preservar somente returnUrl local;
- não persistir;
- não criar configuração.

### POST válido

- atualizar configuração normalmente;
- salvar;
- se returnUrl local válida, retornar a ela;
- senão, manter RedirectToPage para a consulta de Configurações.

### POST inválido

- preservar inputs;
- preservar returnUrl local;
- não persistir.

### Cancelar

- com returnUrl local válida, pode voltar à origem;
- sem retorno válido, mantém o comportamento atual.

Ao voltar à Ficha, o GET deve recalcular energia com a nova tarifa.

## Condição de exibição da ação

A ação “Configurar tarifa de energia” deve aparecer somente quando o cenário realmente exige tarifa:

```text
PossuiFicha
AND existe pelo menos um UsoEquipamentoFicha
AND energia está incompleta por tarifa ausente
```

Não mostrar quando:

- não existe Ficha;
- não existem equipamentos elétricos;
- tarifa está configurada;
- tarifa está configurada como zero.

## MEL017

Preservar a ajuda atual de TarifaEnergiaKwh.

Se necessário, ajustar a fonte compartilhada de ajuda para deixar claro que a tarifa só é necessária quando existem equipamentos elétricos.

Não duplicar texto de ajuda sem necessidade.

## Documentação obrigatória na implementação

Alinhar, no mínimo:

- MEL023;
- backlog;
- UC021;
- F003;
- F004;
- F006, se houver ajuste da ajuda;
- UC027 quanto ao retorno seguro;
- RN014/RN056 somente se necessário para explicitar a semântica já vigente.

Na PR de implementação:

- MEL023 -> Concluído;
- MEL021 continua sem implementação;
- nenhuma migration deve existir.

## Testes mínimos

### Ficha / semântica

- seção mostra Equipamentos elétricos + opcional;
- botão mostra Adicionar equipamento elétrico;
- zero usos + tarifa null => energia 0/completo;
- zero usos não mostra Configurar tarifa;
- uso + tarifa null mantém consumo conhecido;
- uso + tarifa null mostra custos indisponíveis e ação de configuração;
- tarifa zero => custo zero/completo.

### Salvar antes de navegar

- Rendimento válido alterado é persistido antes do redirect;
- Rendimento inválido não persiste nem navega;
- Input/erros são preservados;
- Produto inativo funciona;
- antiforgery é exigido.

### returnUrl

- GET aceita retorno local;
- POST válido retorna à Ficha;
- POST inválido preserva retorno;
- Cancelar pode retornar à origem;
- sem returnUrl mantém fluxo atual;
- URL absoluta externa não é seguida;
- URL protocol-relative externa não é seguida;
- retorno à Ficha recalcula energia com a tarifa salva.

### Regressão

Preservar:

- Novo/Editar/Remover equipamento;
- duplicidade de nome;
- parsing decimal MEL015;
- isolamento tenant;
- configuração ausente => 404 sem lazy-create;
- GET não persiste custo;
- suíte UC021/UC027 existente.

## Proibições

Não criar:

- migration;
- campo novo;
- entidade nova;
- catálogo global de equipamentos;
- ferramenta manual com potência 0;
- depreciação/manutenção/aquisição;
- água/gás/outros utilitários;
- mudança na fórmula de energia;
- mudança no modelo de mão de obra;
- código Azure;
- UC028+.

Se a implementação começar a exigir migration ou alteração de `CalculadoraCustoEnergia` para mudar regra, interrompa e reavalie contra a MEL023.

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
- suíte unitária completa verde;
- suíte integração SQL Server/Web completa verde;
- critérios CA01–CA37 revisados;
- matriz W1–W30 coberta ou justificada por regressão existente;
- nenhuma migration criada;
- MEL023 marcada Concluído;
- MEL021 continua sem implementação;
- UC028 continua Planejado.

Ao finalizar, informar:

- resumo das alterações;
- arquivos alterados;
- contagem final de testes;
- confirmação explícita de que não houve migration;
- eventuais critérios atendidos por testes já existentes em vez de novos testes.
