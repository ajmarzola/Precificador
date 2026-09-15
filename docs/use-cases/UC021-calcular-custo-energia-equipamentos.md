# UC021 — Calcular custo de energia/equipamentos

- **Funcionalidades:** F003 — Ficha Técnica; F004 — Precificação
- **Dependências funcionais:** UC013, UC020 e UC027
- **Alteração de schema:** sim
- **Persistência do custo:** não

## Objetivo

Permitir registrar, na Ficha Técnica, os equipamentos elétricos usados para produzir um lote e calcular o custo atual de energia de cada uso e do lote.

A UC021 deve continuar genérica para diferentes Empresas. Exemplos válidos:

- forno;
- impressora;
- plotter;
- laminadora;
- prensa;
- máquina de corte.

Não criar campos específicos de panificação.

## Decisão de modelagem do MVP

UC021 **não cria um cadastro global de equipamentos/patrimônio**.

O sistema precisa saber apenas quais equipamentos são usados por uma Ficha e seus parâmetros de consumo atuais.

Criar:

~~~text
UsoEquipamentoFicha : IEntidadeEmpresa
- Id : int
- EmpresaId : int
- FichaTecnicaId : int
- NomeEquipamento : string
- NomeEquipamentoNormalizado : string
- PotenciaKw : decimal
- TempoUsoMinutos : int
~~~

Cada registro representa o uso total de um equipamento naquela execução/lote.

Exemplo:

~~~text
Forno
PotenciaKw = 2,2
TempoUsoMinutos = 45
~~~

Se o mesmo forno tiver pré-aquecimento e assamento, somar os tempos e manter um único uso na Ficha.

Um catálogo global de ativos pode ser criado futuramente se houver necessidade de patrimônio, manutenção, depreciação ou compartilhamento administrativo. Isso não é necessário para o cálculo atual.

## Identidade do uso

Dentro da mesma Ficha, um equipamento aparece no máximo uma vez por nome normalizado.

Índice único:

~~~text
(EmpresaId, FichaTecnicaId, NomeEquipamentoNormalizado)
~~~

Se existirem dois equipamentos físicos diferentes, diferenciá-los pelo nome:

~~~text
Forno 1
Forno 2
~~~

## Nome do equipamento

Obrigatório.

Regras:

- remover whitespace externo;
- reduzir sequências internas de whitespace para um espaço;
- máximo 120 caracteres;
- preservar capitalização de exibição;
- `NomeEquipamentoNormalizado = NomeEquipamento.ToUpperInvariant()`.

Nome vazio/whitespace é inválido.

## Potência

~~~text
PotenciaKw > 0
~~~

Usar `decimal`.

Persistência recomendada:

~~~text
decimal(18,6)
~~~

A UI recebe potência em **kW**.

Exemplos:

~~~text
0,3
1,5
2.2
~~~

Não criar conversão automática W → kW neste UC.

Equipamentos sem consumo elétrico mensurável não pertencem a este componente.

## Tempo de uso

~~~text
TempoUsoMinutos > 0
~~~

Usar inteiro em minutos.

Não persistir uso com tempo zero. Se o equipamento não é usado no lote, não deve existir registro.

## Criação, edição e remoção

A Ficha é estado atual, não histórico.

### Criar

~~~text
UsoEquipamentoFicha.Criar(
    empresaId,
    fichaTecnicaId,
    nomeEquipamento,
    potenciaKw,
    tempoUsoMinutos)
~~~

### Editar

Permitir alterar:

- NomeEquipamento;
- PotenciaKw;
- TempoUsoMinutos.

Preservar:

- Id;
- EmpresaId;
- FichaTecnicaId.

A atualização deve validar todos os candidatos antes de mutar a entidade.

### Remover

Remoção física é permitida.

O registro representa somente o estado produtivo atual da Ficha. Histórico econômico será protegido pelos snapshots comerciais futuros.

## Fórmulas

Aplicar RN014.

Por uso:

~~~text
ConsumoKwh =
    PotenciaKw
    × (TempoUsoMinutos / 60m)
~~~

~~~text
CustoEnergiaUso =
    ConsumoKwh
    × TarifaEnergiaKwh
~~~

Por lote:

~~~text
CustoEnergiaLote =
    soma(CustoEnergiaUso)
~~~

Usar divisão decimal.

## Tarifa de energia

Usar exclusivamente:

~~~text
ConfiguracaoPrecificacaoEmpresa.TarifaEnergiaKwh
~~~

da Empresa Ativa.

Semântica:

~~~text
null = Não configurado
0 = valor configurado válido
~~~

Não receber tarifa pelo request.

## Ficha sem usos de equipamento

Se não houver nenhum `UsoEquipamentoFicha`:

~~~text
CustoEnergiaLote = 0
Completo = true
~~~

inclusive quando:

~~~text
TarifaEnergiaKwh = null
~~~

Não há consumo a tarifar.

## Ficha com uso e tarifa ausente

Quando existe pelo menos um uso e:

~~~text
TarifaEnergiaKwh = null
~~~

então:

- `ConsumoKwh` de cada uso continua calculável;
- `CustoEnergiaUso` fica indisponível;
- `CustoEnergiaLote` fica indisponível;
- componente de energia fica incompleto.

Nunca substituir tarifa ausente por zero.

## Tarifa igual a zero

~~~text
TarifaEnergiaKwh = 0
~~~

é configuração válida.

Com usos existentes:

- consumo continua calculado;
- custos dos usos = 0;
- custo do lote = 0;
- componente completo.

## Precisão

Aplicar RN026.

Não arredondar:

- `TempoUsoMinutos / 60m`;
- `ConsumoKwh`;
- `CustoEnergiaUso`;
- `CustoEnergiaLote`.

Arredondamento/formatação ocorre somente na apresentação.

## Calculadora pura

Criar componente em:

~~~text
Precificador.Core.Precificacao
~~~

Nome sugerido:

~~~text
CalculadoraCustoEnergia
~~~

Entrada conceitual:

~~~text
TarifaEnergiaKwh : decimal?
Usos:
- UsoId
- PotenciaKw
- TempoUsoMinutos
~~~

Saída conceitual:

~~~text
Usos:
- UsoId
- ConsumoKwh
- CustoEnergiaUso : decimal?

CustoEnergiaLote : decimal?
Completo : bool
~~~

A calculadora:

- não acessa EF;
- não acessa Web;
- não resolve tenant;
- não formata strings;
- não persiste nada.

Defensivamente rejeitar potência <= 0, tempo <= 0 e tarifa negativa quando informada.

## Configuração 1:1

A página da Ficha já depende da configuração para UC020.

Após UC021, ao renderizar uma Ficha existente, preferir carregar uma única projeção de `ConfiguracaoPrecificacaoEmpresa` contendo:

~~~text
ValorHoraTrabalho
TarifaEnergiaKwh
~~~

e alimentar UC020 e UC021 com ela.

Não fazer duas consultas independentes à mesma configuração se uma única consulta resolver ambos os componentes.

Se a entidade 1:1 estiver ausente:

~~~text
HTTP 404
~~~

Não criar configuração silenciosamente e não confundir entidade ausente com tarifa null.

## Persistência

Criar tabela:

~~~text
UsosEquipamentosFicha
~~~

Campos:

- Id;
- EmpresaId;
- FichaTecnicaId;
- NomeEquipamento;
- NomeEquipamentoNormalizado;
- PotenciaKw;
- TempoUsoMinutos.

FKs:

~~~text
EmpresaId -> Empresas.Id             RESTRICT
FichaTecnicaId -> FichasTecnicas.Id  RESTRICT
~~~

Adicionar:

- `DbSet<UsoEquipamentoFicha>`;
- configuration EF;
- Global Query Filter por Empresa;
- proteção pelo guard central de `IEntidadeEmpresa`;
- validação de que a Ficha referenciada pertence à mesma Empresa.

A proteção de referência deve existir nos caminhos síncrono e assíncrono de `SaveChanges`, seguindo o padrão de `ItemFichaTecnica`.

### Migration

Criar migration evolutiva:

~~~text
AddUsosEquipamentosFicha
~~~

ou nome equivalente.

A migration:

- funciona em banco vazio;
- funciona sobre o banco atual;
- não cria usos retroativos;
- não altera Fichas existentes;
- não altera migrations históricas.

## Web — manutenção dos usos

Seguir o padrão das páginas de Itens.

### Adicionar

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Novo
~~~

Campos:

- Nome do equipamento;
- Potência (kW);
- Tempo de uso (minutos).

Se Produto não existir no tenant:

~~~text
404
~~~

Se Produto existir mas ainda não possuir Ficha:

- redirecionar para a Ficha;
- usar aviso equivalente a:
  `Defina a base da ficha técnica antes de adicionar equipamentos.`

Produto inativo pode receber uso de equipamento sem ser reativado.

### Editar

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Editar/{usoId:int}
~~~

Permitir editar somente Nome/Potência/Tempo.

Request não controla EmpresaId ou FichaTecnicaId.

### Remover

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Remover/{usoId:int}
~~~

GET confirma.

POST remove e faz PRG para a Ficha.

### Parsing decimal

Potência deve aceitar deterministicamente pelo menos:

~~~text
0,3
1,5
2.2
~~~

Seguir o padrão já consolidado:

- contém vírgula => `pt-BR`;
- caso contrário => cultura invariável;
- `decimal.TryParse` com `NumberStyles.Number`.

Criar helper pequeno, por exemplo:

~~~text
UsoEquipamentoFichaFormulario
~~~

Não depender implicitamente de `CurrentCulture`.

## Web — consulta da Ficha

Evoluir:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Adicionar seção **Equipamentos**.

Se não houver usos:

~~~text
Nenhum equipamento adicionado.
~~~

e disponibilizar ação:

~~~text
Adicionar equipamento
~~~

Quando houver usos, tabela recomendada:

~~~text
Equipamento | Potência (kW) | Tempo (min) | Consumo (kWh) | Custo de energia | Ações
~~~

Ações:

- Editar;
- Remover.

### Tarifa configurada

Mostrar consumo e custo de cada uso.

### Tarifa ausente

Mostrar consumo de cada uso e:

~~~text
Custo de energia: —
~~~

Resumo:

~~~text
Custo de energia do lote: indisponível
Tarifa de energia não configurada.
~~~

### Sem usos

Resumo:

~~~text
Custo de energia do lote: 0
~~~

Não mostrar mensagem de tarifa ausente.

## Relação com UC018 e UC020

Os três componentes são independentes.

Uma Ficha pode ter, simultaneamente:

~~~text
CustoBaseItens = indisponível
CustoMaoDeObraLote = conhecido
CustoEnergiaLote = conhecido
~~~

Não esconder componentes conhecidos porque outro componente está incompleto.

Não somar os três nesta UC.

A composição final pertence ao UC022.

## POST inválido da base da Ficha

O POST principal da Ficha continua editando somente Rendimento e TempoAtivo.

Se for inválido:

- preservar Input/erros;
- recarregar Itens/custos da UC018;
- recarregar mão de obra da UC020 com o estado persistido;
- recarregar usos/custos de energia com o estado persistido;
- não simular alterações não salvas;
- não persistir custo.

## Recalculo atual

Alterações em:

- PotenciaKw;
- TempoUsoMinutos;
- TarifaEnergiaKwh

refletem no próximo GET.

Nenhum custo é persistido.

## Fora do escopo

- cadastro global de equipamentos;
- patrimônio;
- depreciação;
- manutenção;
- vida útil;
- aquisição de máquinas;
- equipamentos não elétricos;
- gás, água ou outros utilitários;
- medição real de consumo;
- perdas — UC019;
- custo total/unitário — UC022;
- preço sugerido — UC023;
- snapshots comerciais.

## Critérios de aceitação

- **CA01:** cria uso válido tenant-owned ligado à Ficha correta.
- **CA02:** nome é normalizado e único por Ficha.
- **CA03:** potência e tempo devem ser maiores que zero.
- **CA04:** edição é atômica e preserva Id/Empresa/Ficha.
- **CA05:** remoção elimina somente o uso atual.
- **CA06:** calcula consumo e custo pela RN014 sem arredondamento intermediário.
- **CA07:** vários usos somam o custo de energia do lote.
- **CA08:** zero usos => custo 0/completo mesmo com tarifa null.
- **CA09:** usos + tarifa null => consumos conhecidos, custos indisponíveis.
- **CA10:** tarifa 0 => custos 0/completo.
- **CA11:** alteração de tarifa reflete no próximo GET.
- **CA12:** Produto inativo pode manter usos.
- **CA13:** isolamento tenant e referência Ficha/Empresa são protegidos.
- **CA14:** configuração 1:1 ausente => 404 sem lazy-create.
- **CA15:** Produto sem Ficha não recebe uso.
- **CA16:** request não controla ownership.
- **CA17:** POST inválido da base preserva cálculo pelo estado persistido.
- **CA18:** energia permanece independente de UC018/UC020.
- **CA19:** nenhum custo é persistido.
- **CA20:** migration é evolutiva e preserva dados existentes.

## Matriz de testes

### Domínio — UsoEquipamentoFicha

- D1: criação válida;
- D2: nome normaliza espaços/capitalização de comparação;
- D3: nome vazio ou >120 rejeitado;
- D4: potência <=0 rejeitada;
- D5: tempo <=0 rejeitado;
- D6: edição válida preserva ownership;
- D7: edição inválida é atômica;
- D8: reatribuição de Empresa é rejeitada.

### Cálculo puro

- U1: 60 min calcula exatamente `PotenciaKw` em kWh;
- U2: 30 min calcula meia hora;
- U3: múltiplos usos somam;
- U4: precisão sem arredondamento intermediário;
- U5: zero usos + tarifa null => 0/completo;
- U6: uso + tarifa null => custo null/incompleto com consumo conhecido;
- U7: tarifa 0 => custo 0/completo;
- U8: entradas negativas/inválidas são rejeitadas.

### Persistência

- P1: migration preserva banco atual e cria tabela;
- P2: índice impede mesmo nome normalizado duas vezes na Ficha;
- P3: mesmo nome pode existir em Fichas diferentes;
- P4: GQF isola usos por Empresa;
- P5: guard rejeita escrita cross-tenant;
- P6: referência a Ficha de outra Empresa é rejeitada;
- P7: FKs usam Restrict;
- P8: precisão de PotenciaKw faz round-trip.

### Web

- W1: Novo exige Ficha existente;
- W2: adiciona uso válido e faz PRG;
- W3: duplicidade de nome na mesma Ficha é rejeitada amigavelmente;
- W4: parsing pt-BR/invariant e validações de potência/tempo;
- W5: Editar altera somente Nome/Potência/Tempo;
- W6: Remover exclui uso e recalcula;
- W7: Produto inativo aceita manutenção;
- W8: cross-tenant/manipulação de ids => 404/sem mutação;
- W9: Ficha sem usos mostra custo energia 0;
- W10: tarifa configurada mostra consumo, custo por uso e total;
- W11: tarifa null mostra consumo, custo indisponível e mensagem;
- W12: tarifa 0 mostra custo 0 sem mensagem de não configurada;
- W13: mudança de tarifa reflete dinamicamente;
- W14: configuração 1:1 ausente => 404 sem criação;
- W15: UC018 incompleta não esconde energia conhecida;
- W16: mão de obra incompleta não esconde energia conhecida;
- W17: POST inválido da base preserva usos/custos pelo estado persistido;
- W18: GET não persiste custo nem altera Ficha/usos/configuração.

## Gate

A revalidação pós-UC020 confirmou:

- Ficha Técnica e sua rota canônica estão consolidadas;
- padrão Novo/Editar/Remover de componentes da Ficha já existe;
- `TarifaEnergiaKwh` existe em `ConfiguracaoPrecificacaoEmpresa`;
- UC020 já introduziu carregamento tenant-aware da configuração na Ficha;
- não existe modelo anterior de Equipamento que precise ser migrado.

Não há gate técnico pendente.

## Branch sugerida

~~~text
feat/uc021-custo-energia-equipamentos
~~~

## Commit sugerido

~~~text
feat: calcula custo de energia por equipamento
~~~
