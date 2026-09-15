# UC021 — Calcular custo de energia/equipamentos

- **Funcionalidades:** F003 — Ficha Técnica; F004 — Precificação
- **Dependências funcionais:** UC013 e UC027\n- **Base de revalidação:** UC020 já implementada
- **Schema:** sim
- **Persistência do custo:** não

## Objetivo

Registrar na Ficha os equipamentos elétricos usados para produzir um lote e calcular consumo/custo de energia de forma genérica para padaria, papelaria e outros segmentos.

Exemplos: forno, impressora, plotter, laminadora, prensa e máquina de corte.

## Modelo do MVP

Não criar cadastro global de patrimônio/equipamentos.

Criar apenas o uso atual do equipamento na Ficha:

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

Cada registro representa o **tempo total** de uso daquele equipamento no lote. Se houver fases distintas do mesmo equipamento, somar os tempos.

Um mesmo nome normalizado aparece no máximo uma vez por Ficha:

~~~text
UNIQUE (EmpresaId, FichaTecnicaId, NomeEquipamentoNormalizado)
~~~

Dois equipamentos físicos diferentes devem ter nomes diferentes, por exemplo `Forno 1` e `Forno 2`.

### Nome

- obrigatório;
- trim externo;
- whitespace interno reduzido a um espaço;
- máximo 120 caracteres;
- capitalização preservada;
- comparação por `ToUpperInvariant()`.

### Potência

~~~text
PotenciaKw > 0
~~~

- decimal;
- persistência `decimal(18,6)`;
- UI em kW;
- sem conversão automática W → kW.

### Tempo

~~~text
TempoUsoMinutos > 0
~~~

Inteiro em minutos.

Uso com tempo zero não deve existir.

## Estado atual da Ficha

O uso pode ser criado, editado ou removido fisicamente.

Edição altera somente:

- NomeEquipamento;
- PotenciaKw;
- TempoUsoMinutos.

Preservar Id, EmpresaId e FichaTecnicaId.

Atualização deve ser atômica: validar todos os candidatos antes de mutar a entidade.

Não há histórico de usos no MVP; snapshots comerciais futuros protegem decisões já registradas.

## Cálculo

Aplicar RN014:

~~~text
ConsumoKwh =
    PotenciaKw × (TempoUsoMinutos / 60m)

CustoEnergiaUso =
    ConsumoKwh × TarifaEnergiaKwh

CustoEnergiaLote =
    soma(CustoEnergiaUso)
~~~

Usar `decimal` e RN026. Não arredondar intermediários.

Criar calculadora pura em `Precificador.Core.Precificacao`, preferencialmente `CalculadoraCustoEnergia`.

Entrada conceitual:

~~~text
TarifaEnergiaKwh : decimal?
Usos: UsoId, PotenciaKw, TempoUsoMinutos
~~~

Saída:

~~~text
UsoId, ConsumoKwh, CustoEnergiaUso?
CustoEnergiaLote?
Completo
~~~

A calculadora não acessa EF/Web/tenant e rejeita defensivamente potência <=0, tempo <=0 e tarifa negativa.

## Null x zero

Usar `ConfiguracaoPrecificacaoEmpresa.TarifaEnergiaKwh`.

~~~text
zero usos + tarifa null
=> CustoEnergiaLote = 0
=> completo
~~~

Não existe consumo a tarifar.

~~~text
há usos + tarifa null
=> ConsumoKwh continua conhecido
=> CustoEnergiaUso = null
=> CustoEnergiaLote = null
=> incompleto
~~~

~~~text
tarifa = 0
=> custos = 0
=> completo
~~~

Nunca transformar tarifa ausente em zero.

## Configuração da Empresa

Ao carregar Ficha existente, preferir uma única projeção de `ConfiguracaoPrecificacaoEmpresa` com:

~~~text
ValorHoraTrabalho
TarifaEnergiaKwh
~~~

para alimentar UC020 e UC021.

Não duplicar consultas à mesma entidade sem necessidade.

Entidade 1:1 ausente:

~~~text
HTTP 404
~~~

Sem lazy-create.

## Persistência

Criar tabela:

~~~text
UsosEquipamentosFicha
~~~

Com:

- PK Id;
- EmpresaId obrigatório;
- FichaTecnicaId obrigatório;
- NomeEquipamento max 120;
- NomeEquipamentoNormalizado max 120;
- PotenciaKw decimal(18,6);
- TempoUsoMinutos int;
- índice único por Empresa/Ficha/Nome normalizado;
- FK Empresa RESTRICT;
- FK Ficha RESTRICT.

Adicionar:

- DbSet;
- configuração EF;
- Global Query Filter;
- proteção pelo guard de `IEntidadeEmpresa`;
- validação sync/async de que a Ficha pertence à mesma Empresa.

Migration:

~~~text
AddUsosEquipamentosFicha
~~~

ou equivalente.

Sem backfill, sem alterar Fichas existentes e sem editar migrations históricas.

## Web — manutenção

Seguir o padrão das páginas de Itens.

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Novo
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Editar/{usoId:int}
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Remover/{usoId:int}
~~~

Novo/Editar:

- Nome do equipamento;
- Potência (kW);
- Tempo de uso (minutos).

Produto inexistente/outro tenant => 404.

Produto sem Ficha => redirecionar para a Ficha com aviso para definir a base primeiro.

Produto inativo pode manter equipamentos sem reativação.

Request não controla EmpresaId ou FichaTecnicaId.

### Parsing de potência

Aceitar pelo menos:

~~~text
0,3
1,5
2.2
~~~

Padrão:

- contém vírgula => pt-BR;
- senão => invariant;
- `decimal.TryParse` + `NumberStyles.Number`.

Usar helper pequeno, por exemplo `UsoEquipamentoFichaFormulario`.

Duplicidade por nome normalizado deve gerar erro amigável antes de depender da exceção do índice.

## Web — Ficha

Adicionar seção **Equipamentos**.

Sem usos:

~~~text
Nenhum equipamento adicionado.
Custo de energia do lote: 0
~~~

Com usos, mostrar:

~~~text
Equipamento | Potência (kW) | Tempo (min) | Consumo (kWh) | Custo de energia | Ações
~~~

Ações:

- Editar;
- Remover;
- Adicionar equipamento.

Tarifa configurada: mostrar consumo, custo por uso e total.

Tarifa null com usos:

~~~text
Custo por uso: —
Custo de energia do lote: indisponível
Tarifa de energia não configurada.
~~~

Não esconder consumo em kWh conhecido.

## Integração com UC018/UC020

Itens, mão de obra e energia são componentes independentes.

Exemplo válido:

~~~text
CustoBaseItens = indisponível
CustoMaoDeObraLote = conhecido
CustoEnergiaLote = conhecido
~~~

Não somar componentes nesta UC. UC022 fará a composição.

Preservar integralmente as telas e comportamentos já existentes de UC018/UC020.

### POST inválido da base

Se o POST principal de Rendimento/TempoAtivo for inválido:

- preservar Input/erros;
- recarregar usos e energia pelo estado persistido;
- manter custos UC018/UC020 pelo estado persistido;
- não simular valor não salvo;
- não persistir custo.

## Recalculo

Mudanças em PotenciaKw, TempoUsoMinutos ou TarifaEnergiaKwh aparecem no próximo GET.

Nenhum consumo/custo calculado é persistido.

## Critérios essenciais

- uso tenant-owned ligado à Ficha correta;
- nome único por Ficha após normalização;
- potência/tempo estritamente positivos;
- edição atômica e ownership imutável;
- remoção afeta somente o estado atual;
- cálculo RN014 decimal e sem arredondamento intermediário;
- zero usos é custo zero/completo;
- tarifa null com usos torna custos indisponíveis, mantendo consumo conhecido;
- tarifa zero é válida;
- isolamento tenant e coerência Ficha/Empresa protegidos;
- configuração 1:1 ausente retorna 404;
- Produto inativo funciona;
- nenhum custo é persistido.

## Matriz de testes

### Domínio

- D1 criação válida/ownership;
- D2 normalização e limite de nome;
- D3 potência <=0 rejeitada;
- D4 tempo <=0 rejeitado;
- D5 edição preserva Id/Empresa/Ficha;
- D6 edição inválida é atômica;
- D7 reatribuição de Empresa rejeitada.

### Cálculo

- U1 60 min;
- U2 fração de hora;
- U3 múltiplos usos;
- U4 precisão sem arredondamento;
- U5 zero usos + tarifa null => 0/completo;
- U6 uso + tarifa null => consumo conhecido/custo indisponível;
- U7 tarifa zero;
- U8 entradas inválidas rejeitadas.

### Persistência

- P1 migration evolutiva;
- P2 índice único na mesma Ficha;
- P3 mesmo nome em Fichas diferentes;
- P4 GQF;
- P5 guard cross-tenant;
- P6 referência Ficha/Empresa inválida rejeitada;
- P7 FKs Restrict;
- P8 precisão de PotenciaKw.

### Web

- W1 adicionar exige Ficha;
- W2 Novo válido + PRG;
- W3 duplicidade amigável;
- W4 parsing/validação;
- W5 Editar preserva ownership;
- W6 Remover recalcula;
- W7 Produto inativo;
- W8 ids/cross-tenant protegidos;
- W9 zero usos => energia 0;
- W10 tarifa configurada => uso/total corretos;
- W11 tarifa null => consumo conhecido/custo indisponível;
- W12 tarifa zero;
- W13 alteração da tarifa reflete no GET;
- W14 configuração ausente => 404 sem criação;
- W15 UC018 incompleta não esconde energia;
- W16 UC020 incompleta não esconde energia;
- W17 POST inválido da base usa estado persistido;
- W18 GET não muta/persiste custo.

## Fora do escopo

- catálogo global de equipamentos/patrimônio;
- depreciação, manutenção, vida útil e aquisição;
- gás, água ou outros utilitários;
- equipamentos sem consumo elétrico;
- perdas — UC019;
- custo total/unitário — UC022;
- preço sugerido — UC023;
- snapshots comerciais.

## Gate

Revalidação concluída após UC020:

- Ficha/rota consolidadas;
- padrão Novo/Editar/Remover existente;
- TarifaEnergiaKwh já disponível e editável;
- carregamento tenant-aware de configuração já existe;
- não existe modelo anterior de equipamento a migrar.

UC021 está liberada para implementação.

## Branch sugerida

~~~text
feat/uc021-custo-energia-equipamentos
~~~
