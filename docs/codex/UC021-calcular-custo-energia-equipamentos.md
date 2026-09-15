# Instrução Codex — UC021: Calcular custo de energia/equipamentos

## Tarefa

Implementar `docs/use-cases/UC021-calcular-custo-energia-equipamentos.md`.

Branch obrigatória:

~~~text
feat/uc021-custo-energia-equipamentos
~~~

Não trabalhar diretamente em `master` e não fazer merge da própria PR.

## Antes de editar

1. atualizar `master`;
2. criar/trocar para a branch acima;
3. confirmar UC021 = `Pronto` no backlog;
4. ler UC021, F003, F004, RN014/RN017/RN026/RN039/RN055/RN056;
5. inspecionar `ItemFichaTecnica`, páginas Itens Novo/Editar/Remover, `FichaTecnicaModel`, UC020 e `ConfiguracaoPrecificacaoEmpresa`.

## Modelo

Criar `UsoEquipamentoFicha : IEntidadeEmpresa`:

~~~text
Id
EmpresaId
FichaTecnicaId
NomeEquipamento
NomeEquipamentoNormalizado
PotenciaKw
TempoUsoMinutos
~~~

Não criar cadastro global `Equipamento`.

Regras:

- nome obrigatório, normalizado, máx. 120;
- potência > 0;
- tempo > 0;
- um NomeNormalizado por Ficha;
- edição atômica;
- EmpresaId/FichaTecnicaId imutáveis;
- remoção física permitida.

## Persistência

Criar:

- DbSet;
- configuration EF;
- GQF;
- índice único `(EmpresaId, FichaTecnicaId, NomeEquipamentoNormalizado)`;
- FKs Restrict para Empresa/Ficha;
- validação sync/async de referência Ficha/Empresa no DbContext.

Migration:

~~~text
AddUsosEquipamentosFicha
~~~

Não editar migrations históricas e não fazer backfill.

## Cálculo

Criar calculadora pura em Core.

~~~text
ConsumoKwh = PotenciaKw × (TempoUsoMinutos / 60m)
CustoEnergiaUso = ConsumoKwh × TarifaEnergiaKwh
CustoEnergiaLote = soma dos usos
~~~

Semântica:

~~~text
zero usos + tarifa null => custo 0/completo
usos + tarifa null => consumos conhecidos; custos null/incompleto
tarifa 0 => custos 0/completo
~~~

Sem arredondamento intermediário.

## Configuração

Usar `TarifaEnergiaKwh` da configuração tenant-aware.

Ao carregar Ficha existente, preferir uma única projeção de configuração para:

- ValorHoraTrabalho (UC020);
- TarifaEnergiaKwh (UC021).

Entidade 1:1 ausente => 404, sem lazy-create.

## Web — manutenção

Criar páginas no padrão de Itens:

~~~text
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Novo
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Editar/{usoId:int}
/Produtos/FichaTecnica/{produtoId:int}/Equipamentos/Remover/{usoId:int}
~~~

Campos de Novo/Editar:

- Nome do equipamento;
- Potência (kW);
- Tempo de uso (minutos).

Produto sem Ficha => redirecionar à Ficha com aviso.

Produto inativo pode manter usos.

Parsing de potência:

- vírgula => pt-BR;
- senão => invariant;
- decimal.TryParse + NumberStyles.Number.

Request não controla EmpresaId/FichaTecnicaId.

## Web — Ficha

Adicionar seção Equipamentos.

Tabela:

~~~text
Equipamento | Potência (kW) | Tempo (min) | Consumo (kWh) | Custo de energia | Ações
~~~

Adicionar ação `Adicionar equipamento`.

Resumo:

~~~text
Custo de energia do lote
~~~

Tarifa null com usos:

~~~text
indisponível
Tarifa de energia não configurada.
~~~

Sem usos:

~~~text
0
~~~

Preservar integralmente UC018 e UC020.

POST inválido da base deve recarregar energia pelo estado persistido sem sobrescrever Input.

## Não antecipar

Não implementar:

- cadastro global de ativos;
- depreciação/manutenção;
- gás/água;
- perdas;
- custo total/unitário;
- preço sugerido;
- snapshots.

## Testes

Atender D1–D8, U1–U8, P1–P8 e W1–W18 da UC021.

Riscos prioritários:

1. duplicidade por nome normalizado;
2. referência cross-tenant Ficha/Empresa;
3. divisão inteira;
4. tarifa null confundida com zero;
5. zero usos exigindo tarifa desnecessariamente;
6. arredondamento intermediário;
7. POST inválido usando estado não persistido;
8. regressão de UC018/UC020.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

Na PR, mover apenas UC021 de `Pronto` para `Concluído`.

## Retorno

Informar branch, arquivos/migration, modelo criado, calculadora, semântica null/zero, isolamento tenant, integração Web, testes, build/test e URL da PR.
