# Instrução Codex — UC021: Calcular custo de energia/equipamentos

## Tarefa

Implementar `docs/use-cases/UC021-calcular-custo-energia-equipamentos.md`.

Branch:

~~~text
feat/uc021-custo-energia-equipamentos
~~~

Não trabalhar em `master` nem fazer merge da própria PR.

## Antes de editar

1. atualizar master e criar a branch;
2. confirmar UC021 = `Pronto`;
3. ler UC021, F003/F004, RN014/RN026/RN039/RN055/RN056;
4. inspecionar ItemFichaTecnica, páginas Itens, FichaTecnicaModel, UC020 e configuração da Empresa.

## Implementar

### Domínio

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

Sem cadastro global de Equipamento.

- nome obrigatório/normalizado/máx.120;
- potência >0;
- tempo >0;
- edição atômica;
- Empresa/Ficha imutáveis;
- remoção física permitida.

### Persistência

Adicionar DbSet/configuration/GQF/guard e validação sync+async Ficha/Empresa.

Índice único:

~~~text
EmpresaId + FichaTecnicaId + NomeEquipamentoNormalizado
~~~

FKs Restrict.

Migration:

~~~text
AddUsosEquipamentosFicha
~~~

Sem backfill/mudança de migrations antigas.

### Cálculo

Criar calculadora pura:

~~~text
ConsumoKwh = PotenciaKw × (TempoUsoMinutos / 60m)
CustoEnergiaUso = ConsumoKwh × TarifaEnergiaKwh
CustoEnergiaLote = soma
~~~

Semântica:

~~~text
zero usos + tarifa null => 0/completo
usos + tarifa null => consumo conhecido; custos null/incompleto
tarifa 0 => custo 0/completo
~~~

Sem arredondamento.

### Configuração

Usar TarifaEnergiaKwh tenant-aware.

Refatorar o carregamento da Ficha para preferencialmente consultar uma vez a configuração e alimentar UC020 + UC021.

Configuração 1:1 ausente => 404, sem lazy-create.

### Web

Criar:

~~~text
/Produtos/FichaTecnica/{produtoId}/Equipamentos/Novo
/Produtos/FichaTecnica/{produtoId}/Equipamentos/Editar/{usoId}
/Produtos/FichaTecnica/{produtoId}/Equipamentos/Remover/{usoId}
~~~

Seguir padrões das páginas Itens.

Potência aceita pt-BR/invariant por helper explícito.

Produto sem Ficha => redirect com aviso.

Produto inativo funciona.

Request não controla ownership.

Na Ficha, seção:

~~~text
Equipamento | Potência | Tempo | Consumo | Custo de energia | Ações
~~~

Sem usos => custo energia 0.

Com tarifa null => mostrar consumo, custo indisponível e mensagem.

Preservar UC018/UC020 e o retorno de POST inválido baseado no estado persistido.

## Não implementar

- catálogo patrimonial;
- depreciação/manutenção;
- gás/água;
- perdas;
- custo total/unitário;
- preço sugerido;
- snapshots.

## Testes

Atender D1–D7, U1–U8, P1–P8 e W1–W18.

Prioridades de revisão:

1. unicidade normalizada;
2. cross-tenant Ficha/Empresa;
3. divisão decimal;
4. tarifa null x zero;
5. zero usos sem tarifa;
6. precisão;
7. POST inválido;
8. regressão UC018/UC020.

## Validação

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
git diff --check
~~~

Na PR, mover somente UC021 de `Pronto` para `Concluído`.

Retornar arquivos/migration, modelo, calculadora, Web, testes, build/test e URL da PR.
