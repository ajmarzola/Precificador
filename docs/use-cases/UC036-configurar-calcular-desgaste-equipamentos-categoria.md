# UC036 — Configurar e calcular custo de desgaste de equipamentos por Categoria

- **Funcionalidades:** F002 — Gestão de Produtos; F003 — Ficha Técnica; F004 — Precificação
- **Origem:** evolução do modelo de custo identificada após UC032
- **Estado:** Pronto
- **Prioridade:** antecipada
- **Gate operacional:** executar imediatamente após UC032 e antes da MEL021
- **Dependências funcionais:** UC032, UC018, UC022, UC023, UC024 e UC025
- **Alteração de domínio:** sim
- **Alteração de schema:** sim
- **Migration:** sim
- **Alteração da fórmula de custo:** sim
- **Persistência do custo calculado:** não
- **Snapshot comercial:** reutiliza o `CustoReferencia` final existente; não cria snapshot de componente

## Objetivo

Permitir que cada **Categoria de Produto** configure uma regra simplificada de apropriação de **desgaste de equipamentos** e incorporar esse valor ao custo atual do lote.

O desgaste representa uma estimativa de uso, depreciação e reposição de equipamentos necessários à produção daquela Categoria.

Exemplos:

~~~text
Categoria: Agenda
Forma de cálculo: Valor fixo por lote
Valor: R$ 1,50

=> CustoDesgasteEquipamentosLote = R$ 1,50
~~~

~~~text
Categoria: Pães Rústicos
Forma de cálculo: Percentual sobre os insumos
Valor: 5%
CustoBaseItens: R$ 10,00

=> CustoDesgasteEquipamentosLote = R$ 0,50
~~~

A UC036 deve:

- armazenar a configuração de desgaste na `CategoriaProduto`;
- permitir editar essa configuração na administração de Categorias;
- calcular o desgaste atual do lote;
- incorporar o novo componente ao `CustoLote`;
- refletir o novo custo no custo unitário, preço teórico, preço sugerido e margem atual;
- exibir a regra e o valor calculado na Ficha Técnica e no detalhamento da precificação;
- preservar snapshots comerciais históricos já registrados.

## Conceito — desgaste não é energia

O desgaste é **independente** do custo de energia elétrica da UC021/MEL023.

Não existe relação obrigatória entre:

~~~text
UsoEquipamentoFicha
e
CustoDesgasteEquipamentosLote
~~~

Portanto:

- uma Categoria pode possuir desgaste mesmo sem nenhum equipamento elétrico cadastrado na Ficha;
- uma Ficha pode possuir equipamentos elétricos e custo de energia mesmo com desgaste configurado em zero;
- desgaste pode representar equipamentos manuais, elétricos ou outros recursos duráveis;
- não criar vínculo entre Categoria e os nomes cadastrados em `UsoEquipamentoFicha`;
- não calcular desgaste por potência, tempo de uso ou tarifa de energia.

A UC036 é uma **apropriação simplificada por Categoria**, não um módulo contábil de depreciação patrimonial.

## Modelo da Categoria

Evoluir `CategoriaProduto` para conter:

~~~text
FormaCalculoDesgasteEquipamento : enum
ValorDesgasteEquipamento : decimal
~~~

Nome sugerido do enum:

~~~csharp
public enum FormaCalculoDesgasteEquipamento
{
    ValorFixoPorLote = 1,
    PercentualSobreInsumos = 2
}
~~~

Os nomes concretos podem sofrer ajuste mínimo se houver ganho claro de legibilidade, mas a semântica e os valores persistidos devem permanecer explícitos.

### Forma de cálculo

Valores válidos:

~~~text
ValorFixoPorLote
PercentualSobreInsumos
~~~

Não criar opção:

~~~text
Nenhum
Não configurado
Por unidade
Por equipamento
Por tempo
~~~

O valor zero já representa ausência de custo de desgaste quando desejado.

### Valor do desgaste

`ValorDesgasteEquipamento` possui semântica dependente da Forma:

~~~text
ValorFixoPorLote
=> valor monetário do lote

PercentualSobreInsumos
=> fração decimal aplicada sobre CustoBaseItens
~~~

Exemplos persistidos:

~~~text
R$ 1,50
=> 1.50m

5%
=> 0.05m

125%
=> 1.25m
~~~

Validação comum:

~~~text
ValorDesgasteEquipamento >= 0
~~~

Não impor teto de 100% para o modo percentual.

Zero é válido nos dois modos.

## Precisão e persistência

Persistir:

~~~text
FormaCalculoDesgasteEquipamento int NOT NULL
ValorDesgasteEquipamento decimal(18,6) NOT NULL
~~~

Motivo do mesmo campo decimal para ambas as formas:

- valor fixo pode exigir precisão maior que centavos em cálculos internos;
- percentual é armazenado como fração;
- a Forma define a interpretação;
- evita dois campos mutuamente exclusivos.

Não arredondar o valor calculado antes das fronteiras de apresentação conforme RN026.

## Criação de Categoria após UC036

Toda nova Categoria deve nascer com configuração válida de desgaste.

O formulário deve pré-preencher:

~~~text
Forma de cálculo do desgaste = Valor fixo por lote
Valor do desgaste = 0,00
~~~

O usuário pode alterar antes de salvar.

Mesmo com defaults de interface, o domínio deve receber e validar explicitamente Forma + Valor na criação; não depender de estado inválido ou null.

Direção conceitual:

~~~csharp
CategoriaProduto.Criar(
    empresaId,
    nome,
    formaCalculoDesgasteEquipamento,
    valorDesgasteEquipamento)
~~~

## Edição de Categoria

A tela de edição passa a permitir:

~~~text
Nome
Forma de cálculo do desgaste
Valor do desgaste
~~~

É permitido alterar apenas a configuração de desgaste sem renomear a Categoria.

É permitido alterar a configuração de Categoria ativa ou inativa.

Uma alteração válida:

- preserva Id;
- preserva EmpresaId;
- preserva Ativo;
- altera o cálculo corrente de todos os Produtos atualmente vinculados;
- não modifica Produtos;
- não reativa Categoria inativa.

Preferir operação de domínio atômica para validar Nome + Forma + Valor antes de mutar estado.

## Migration

Criar migration SQL Server evolutiva sobre a master pós-UC032.

Não editar nem rebaselinear migrations históricas.

Adicionar às Categorias existentes:

~~~text
FormaCalculoDesgasteEquipamento
ValorDesgasteEquipamento
~~~

### Backfill obrigatório

Toda Categoria já existente deve receber:

~~~text
FormaCalculoDesgasteEquipamento = ValorFixoPorLote
ValorDesgasteEquipamento = 0
~~~

Consequências:

- nenhum Produto existente aumenta de custo apenas por aplicar a migration;
- todas as Categorias ficam em estado válido imediatamente;
- o usuário pode configurar valores reais posteriormente;
- não existe estado "configuração de desgaste ausente".

### Integridade no banco

Além das validações de domínio, proteger:

~~~text
FormaCalculoDesgasteEquipamento IN (1, 2)
ValorDesgasteEquipamento >= 0
~~~

por check constraints ou mecanismo equivalente do SQL Server.

Atualizar ModelSnapshot.

Os valores `Valor fixo por lote = 0` são regra de **backfill**, não um fallback permanente de persistência.

A migration pode usar estratégia temporária de default para viabilizar a alteração de schema, mas o modelo final não deve depender de default SQL para criar Categorias futuras. Após UC036, toda criação funcional deve fornecer Forma + Valor explicitamente pelo domínio.

## Produto sem Categoria

A Categoria continua opcional conforme UC032.

Produto com:

~~~text
CategoriaProdutoId = null
~~~

possui:

~~~text
CustoDesgasteEquipamentosLote = 0
ComponenteDesgasteCompleto = true
~~~

Não tornar a precificação incompleta apenas porque o Produto não possui Categoria.

Não criar Categoria artificial "Sem categoria".

## Categoria inativa

Se um Produto já está vinculado a Categoria inativa:

- a Categoria continua válida para o Produto;
- sua configuração de desgaste continua sendo aplicada;
- alterar o desgaste da Categoria inativa afeta o cálculo corrente dos Produtos já vinculados;
- a Categoria continua não elegível para novas associações conforme UC032.

A situação `Ativo` da Categoria não participa da fórmula.

## Fórmula — valor fixo por lote

Aplicar:

~~~text
Forma = ValorFixoPorLote

CustoDesgasteEquipamentosLote =
    ValorDesgasteEquipamento
~~~

O valor é **por lote**, não por unidade produzida.

Exemplo:

~~~text
Categoria Agenda
Valor = R$ 1,50
Rendimento = 1
=> desgaste do lote = R$ 1,50
=> contribuição unitária = R$ 1,50

Rendimento = 5
=> desgaste do lote = R$ 1,50
=> contribuição unitária = R$ 0,30
~~~

A divisão por rendimento ocorre somente no cálculo final do custo unitário via UC022.

Não multiplicar o valor fixo pelo Rendimento.

## Fórmula — percentual sobre os insumos

Aplicar exclusivamente:

~~~text
Forma = PercentualSobreInsumos

CustoDesgasteEquipamentosLote =
    CustoBaseItens
    × ValorDesgasteEquipamento
~~~

A base é exatamente o `CustoBaseItens` da UC018.

Não entram na base:

- `CustoPerdasLote`;
- `CustoMaoDeObraLote`;
- `CustoEnergiaLote`;
- o próprio desgaste;
- Rendimento;
- Margem-alvo;
- Preço teórico;
- Preço sugerido;
- Preço de prateleira.

Isso evita dupla apropriação e qualquer cálculo circular.

### Percentual zero

Se:

~~~text
Forma = PercentualSobreInsumos
ValorDesgasteEquipamento = 0
~~~

então:

~~~text
CustoDesgasteEquipamentosLote = 0
ComponenteDesgasteCompleto = true
~~~

mesmo que `CustoBaseItens` esteja indisponível.

O custo total continuará incompleto se o próprio `CustoBaseItens` estiver indisponível; a regra apenas evita marcar o componente desgaste como desconhecido quando matematicamente seu resultado é zero.

### Percentual positivo com base indisponível

Se:

~~~text
ValorDesgasteEquipamento > 0
CustoBaseItens = null
~~~

então:

~~~text
CustoDesgasteEquipamentosLote = null
ComponenteDesgasteCompleto = false
~~~

Nunca substituir a base desconhecida por zero.

## Calculadora pura

Criar componente de domínio em `Precificador.Core.Precificacao`.

Nome sugerido:

~~~text
CalculadoraCustoDesgasteEquipamentos
~~~

Entrada conceitual:

~~~text
Configuração da Categoria?:
- Forma
- Valor

CustoBaseItens?
~~~

Resultado:

~~~text
CustoDesgasteEquipamentosLote : decimal?
Completo : bool
~~~

Sem Categoria equivale a ausência de configuração e deve retornar zero/completo.

A calculadora:

- não acessa EF;
- não conhece HTTP;
- não consulta Produto/Categoria;
- não arredonda;
- rejeita valor negativo;
- rejeita Forma inválida.

## Evolução do UC022 — custo total

Atualizar RN015 e UC022.

A fórmula passa a ser:

~~~text
CustoLote =
    CustoBaseItens
  + CustoPerdasLote
  + CustoMaoDeObraLote
  + CustoEnergiaLote
  + CustoDesgasteEquipamentosLote
~~~

`CalculadoraCustoProduto` deve receber o novo componente.

Completude:

~~~text
CustoBaseItens != null
E CustoPerdasLote != null
E CustoMaoDeObraLote != null
E CustoEnergiaLote != null
E CustoDesgasteEquipamentosLote != null

=> CustoLote conhecido
~~~

Zero conhecido continua válido.

Null continua significando indisponível.

Não apresentar soma parcial como custo total.

## Custo unitário, preço e margem

Nenhuma fórmula posterior muda conceitualmente.

O novo componente altera o `CustoLote`; por consequência, os cálculos existentes passam a refletir naturalmente:

~~~text
CustoUnitarioProduto
PrecoTeorico
PrecoSugerido
MargemAtual
SituacaoMargem
~~~

Não duplicar a regra de desgaste em UC023/UC024.

A cadeia permanece:

~~~text
CustoBaseItens
  + perdas
  + mão de obra
  + energia
  + desgaste
        ↓
CustoLote
        ↓
CustoUnitarioProduto
        ↓
Preço teórico / sugerido
        ↓
Margem atual / situação
~~~

## Orquestração atual

Evoluir `PrecificacaoProdutoAtual` para carregar junto ao Produto:

~~~text
CategoriaProdutoId
FormaCalculoDesgasteEquipamento?
ValorDesgasteEquipamento?
~~~

ou projeção equivalente.

Não fazer consulta N+1.

A mesma orquestração continua sendo usada por:

- Ficha Técnica;
- registro de Preço de prateleira;
- Detalhamento da precificação;
- cálculos atuais dependentes.

### Categoria presente

Usar a configuração da Categoria do mesmo tenant via GQF normal.

Não usar `IgnoreQueryFilters` no fluxo de cálculo.

### Categoria ausente

Calcular desgaste zero/completo.

### Categoria inconsistente/cross-tenant

A FK/guard da UC032 já impedem estado normal inconsistente.

Não criar fallback que consulte Categoria de outro tenant.

## Ficha Técnica

Evoluir:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Na área de custo, exibir o novo componente antes do Custo total do lote.

### Valor fixo

Exemplo:

~~~text
Desgaste de equipamentos
Forma: Valor fixo por lote
Valor configurado: R$ 1,50
Custo de desgaste do lote: R$ 1,50
~~~

### Percentual

Exemplo:

~~~text
Desgaste de equipamentos
Forma: Percentual sobre os insumos
Percentual configurado: 5%
Base: R$ 10,00
Custo de desgaste do lote: R$ 0,50
~~~

### Sem Categoria

Exibir sem criar aparência de erro:

~~~text
Custo de desgaste do lote: R$ 0,00
Produto sem categoria: nenhum desgaste por categoria aplicado.
~~~

### Percentual com base indisponível

Exibir:

~~~text
Custo de desgaste do lote: indisponível
~~~

e informação suficiente para compreender que a base dos insumos está indisponível.

Não criar botão de configuração da Categoria dentro da Ficha nesta UC.

## Detalhamento da precificação — UC025

Evoluir:

~~~text
/Produtos/Precificacao/{id:int}
~~~

Adicionar bloco ou informações de **Desgaste de equipamentos** contendo:

- Categoria;
- Forma de cálculo;
- Valor configurado;
- base usada quando percentual;
- Custo de desgaste do lote.

Na seção **Consolidação do custo**, acrescentar o novo componente antes de `Custo total do lote`.

Não esconder zero.

Não converter null em zero.

## Administração de Categorias — Web

Evoluir:

~~~text
/Produtos/Categorias
/Produtos/Categorias/Novo
/Produtos/Categorias/Editar/{id:int}
~~~

### Labels

Usar:

~~~text
Forma de cálculo do desgaste
Valor do desgaste
~~~

Opções da Forma:

~~~text
Valor fixo por lote
Percentual sobre os insumos
~~~

### Entrada de Valor

Usar um único campo.

Semântica de entrada:

~~~text
Forma = Valor fixo por lote
"1,50" => 1.50m

Forma = Percentual sobre os insumos
"5" => 0.05m
"12,5" => 0.125m
~~~

Aceitar vírgula conforme MEL015.

Não exigir que o usuário digite a fração decimal.

### Ajuda contextual

A interface deve explicar:

~~~text
Valor fixo por lote:
o valor informado é acrescentado uma vez ao custo total de cada lote.

Percentual sobre os insumos:
o percentual é calculado exclusivamente sobre o Custo base dos itens.
~~~

É aceitável alterar sufixo visual entre `R$` e `%` com JavaScript simples, mas isso não é requisito funcional.

Não introduzir biblioteca JS.

### Validação Web

Mensagens sugeridas:

~~~text
Selecione uma forma de cálculo do desgaste válida.
Informe um valor de desgaste válido.
O valor do desgaste não pode ser negativo.
~~~

POST inválido:

- retorna 200;
- preserva Nome;
- preserva Forma;
- preserva Valor informado;
- não persiste alteração parcial.

### Cadastro

GET:

~~~text
Forma = Valor fixo por lote
Valor = 0,00
~~~

POST válido cria Categoria com configuração completa.

### Edição

GET converte:

~~~text
Valor fixo => moeda em pt-BR
Percentual => fração persistida para percentual de interface
~~~

Exemplo:

~~~text
0.05 persistido
=> "5" na tela
~~~

POST válido atualiza Nome + desgaste atomicamente.

Categoria inativa continua editável.

### Listagem

Adicionar coluna ou resumo:

~~~text
Desgaste de equipamentos
~~~

Exemplos:

~~~text
R$ 1,50 por lote
5% sobre os insumos
R$ 0,00 por lote
~~~

Preservar Nome, Situação e ações da UC032.

## Histórico comercial / snapshots

O UC011 já congela:

~~~text
CustoReferencia = CustoUnitarioProduto vigente
~~~

Após UC036, novos registros naturalmente passam a congelar um `CustoReferencia` que inclui desgaste.

Não adicionar a `RegistroPrecoProduto`:

- Forma de desgaste;
- Valor de desgaste;
- Custo de desgaste do lote.

Registros históricos existentes não devem ser recalculados.

Exemplo:

~~~text
Registro antigo:
CustoReferencia = R$ 20,00

Categoria alterada depois:
desgaste passa de R$ 0,00 para R$ 1,50

=> registro antigo continua R$ 20,00
=> cálculo atual muda
=> novo snapshot usa o novo custo atual
~~~

UC012 continua derivando histórico exclusivamente dos snapshots armazenados.

## Alteração imediata da configuração

A configuração de Categoria é **estado corrente**, não versionado.

Alterar Forma/Valor:

- afeta imediatamente todos os cálculos atuais dos Produtos vinculados;
- não altera Ficha Técnica persistida;
- não cria novo RegistroPrecoProduto;
- não altera snapshots antigos;
- não cria histórico da configuração.

Se histórico da configuração de desgaste for desejado no futuro, criar UC/MEL própria.

## Regras de negócio a adicionar/alterar na implementação

Adicionar preferencialmente:

~~~text
RN057 — Configuração de desgaste de equipamentos por Categoria
RN058 — Custo de desgaste de equipamentos do lote
~~~

E atualizar:

~~~text
RN015 — Custo do lote
RN017 — Precificação incompleta
RN043 — Categoria do Produto
~~~

### RN057 — direção normativa

Deve registrar:

- Categoria possui Forma + Valor;
- formas válidas;
- zero válido;
- percentual sem teto;
- existente migra para Valor fixo/zero;
- Produto sem Categoria => desgaste zero;
- Categoria inativa vinculada continua aplicando a regra.

### RN058 — direção normativa

Deve registrar exatamente:

~~~text
ValorFixoPorLote:
CustoDesgasteEquipamentosLote = Valor

PercentualSobreInsumos:
CustoDesgasteEquipamentosLote = CustoBaseItens × Valor
~~~

com as regras de zero/null desta especificação.

## Segurança e multiempresa

Preservar FT002 integralmente.

- Categoria é tenant-owned;
- configuração é parte da Categoria, portanto não recebe EmpresaId do request;
- GET/POST cross-tenant das páginas de Categoria continuam 404;
- cálculo usa GQF;
- não usar `IgnoreQueryFilters` para carregar Categoria no fluxo Web;
- alterar configuração de Categoria de outra Empresa continua bloqueado pelo guard central;
- nenhum dado da Empresa B pode entrar no desgaste da Empresa A.

## Migration e regressão

A migration da UC036 deve ser testada a partir da migration imediatamente anterior.

Cenários mínimos:

- banco sem Categorias;
- uma Categoria ativa;
- uma Categoria inativa;
- Categoria com Produtos vinculados;
- Categorias em Empresas distintas.

Após migration:

- todas possuem Forma = Valor fixo por lote;
- todas possuem Valor = 0;
- vínculos dos Produtos permanecem;
- situação Ativo/Inativo permanece;
- nomes/ids permanecem;
- zero migrations pendentes.

## Critérios de aceitação

- **CA01:** CategoriaProduto possui Forma e Valor de desgaste obrigatórios.
- **CA02:** Forma aceita somente Valor fixo por lote ou Percentual sobre os insumos.
- **CA03:** Valor negativo é rejeitado.
- **CA04:** zero é válido para ambas as Formas.
- **CA05:** percentual é armazenado como fração decimal.
- **CA06:** percentual acima de 100% é permitido.
- **CA07:** migration atribui Valor fixo/zero a todas as Categorias existentes.
- **CA08:** migration não altera vínculo/situação/nome de Categorias e Produtos.
- **CA09:** nova Categoria inicia o formulário em Valor fixo/zero.
- **CA10:** cadastro persiste Forma + Valor.
- **CA11:** edição altera configuração sem reativar Categoria inativa.
- **CA12:** edição inválida não persiste alteração parcial.
- **CA13:** listagem apresenta configuração de desgaste.
- **CA14:** Valor fixo por lote retorna exatamente o valor configurado.
- **CA15:** valor fixo não é multiplicado pelo Rendimento.
- **CA16:** percentual usa exclusivamente CustoBaseItens.
- **CA17:** percentual de 5% sobre R$ 10,00 resulta R$ 0,50.
- **CA18:** percentual zero resulta desgaste zero mesmo com base indisponível.
- **CA19:** percentual positivo + base indisponível resulta desgaste indisponível.
- **CA20:** Produto sem Categoria resulta desgaste zero/completo.
- **CA21:** Categoria inativa vinculada continua aplicando desgaste.
- **CA22:** energia e desgaste permanecem cálculos independentes.
- **CA23:** CustoLote soma o novo componente.
- **CA24:** CustoUnitarioProduto reflete o novo CustoLote.
- **CA25:** Preço teórico/sugerido refletem o novo custo sem fórmula duplicada.
- **CA26:** Margem atual/situação refletem o novo custo.
- **CA27:** Ficha Técnica exibe Forma/Valor/Custo de desgaste.
- **CA28:** detalhamento UC025 exibe o novo componente.
- **CA29:** valores conhecidos zero são exibidos como zero, não como indisponíveis.
- **CA30:** null não é convertido silenciosamente em zero.
- **CA31:** alteração de Categoria reflete no próximo cálculo atual.
- **CA32:** snapshots antigos permanecem inalterados.
- **CA33:** novo RegistroPrecoProduto congela CustoReferencia já incluindo desgaste.
- **CA34:** nenhum novo campo de snapshot de desgaste é criado.
- **CA35:** nenhum catálogo de equipamentos é criado.
- **CA36:** nenhuma regra usa PotenciaKw/Tempo/Tarifa para desgaste.
- **CA37:** GQF/guards continuam protegendo multiempresa.
- **CA38:** nenhuma implementação Azure é antecipada.
- **CA39:** build Release sem warnings novos relevantes.
- **CA40:** suíte unitária completa verde.
- **CA41:** suíte integração SQL Server/Web verde.

## Matriz mínima de testes

### Unitários — CategoriaProduto

- **U1:** criar Categoria com Valor fixo válido preserva Forma/Valor.
- **U2:** criar Categoria com percentual válido preserva fração.
- **U3:** Forma inválida é rejeitada.
- **U4:** Valor negativo é rejeitado.
- **U5:** zero é aceito nas duas Formas.
- **U6:** percentual >100% é aceito.
- **U7:** atualização válida de Nome/Forma/Valor é atômica.
- **U8:** atualização inválida preserva estado anterior.
- **U9:** desativar/reativar preserva configuração de desgaste.

### Unitários — cálculo de desgaste

- **U10:** Valor fixo retorna valor exato.
- **U11:** Valor fixo não depende de CustoBaseItens.
- **U12:** Percentual 5% × 10 = 0,50.
- **U13:** Percentual usa precisão decimal sem arredondamento intermediário.
- **U14:** Percentual zero + base null = zero/completo.
- **U15:** Percentual positivo + base null = null/incompleto.
- **U16:** sem Categoria = zero/completo.
- **U17:** Valor negativo/Forma inválida são rejeitados.

### Unitários — composição UC022

- **U18:** CustoLote soma os cinco componentes.
- **U19:** desgaste zero participa normalmente.
- **U20:** desgaste null torna CustoLote/CustoUnitario null.
- **U21:** rendimento continua dividindo somente o total final.
- **U22:** regressões anteriores de UC022 permanecem verdes com o novo parâmetro.

### Persistência / migration

- **P1:** migration adiciona as duas colunas com tipos/precisão esperados.
- **P2:** Categoria ativa existente recebe Valor fixo/zero.
- **P3:** Categoria inativa existente recebe Valor fixo/zero e permanece inativa.
- **P4:** Produto continua vinculado à mesma Categoria após migration.
- **P5:** Categorias de tenants distintos preservam ownership.
- **P6:** round-trip de valor fixo preserva precisão.
- **P7:** round-trip percentual preserva fração.
- **P8:** check rejeita Forma inválida.
- **P9:** check rejeita Valor negativo.
- **P10:** GQF/guard existentes continuam isolando configuração.
- **P11:** migration completa deixa zero pendências.

### Web — Categorias

- **W1:** Novo mostra Forma=Valor fixo e Valor=0,00.
- **W2:** cadastro Valor fixo 1,50 persiste 1.50.
- **W3:** cadastro Percentual 5 persiste 0.05.
- **W4:** vírgula percentual 12,5 persiste 0.125.
- **W5:** valor negativo retorna erro e não persiste.
- **W6:** Forma manipulada/inválida retorna erro.
- **W7:** edição carrega percentual 0.05 como 5.
- **W8:** edição troca Valor fixo -> Percentual corretamente.
- **W9:** edição troca Percentual -> Valor fixo corretamente.
- **W10:** Categoria inativa pode ter desgaste alterado sem reativação.
- **W11:** POST inválido preserva Nome/Forma/Valor.
- **W12:** listagem apresenta R$ por lote ou % sobre insumos.
- **W13:** GET/POST cross-tenant continuam 404.
- **W14:** antiforgery e PRG da UC032 permanecem íntegros.

### Web/orquestração — cálculo

- **W15:** Produto sem Categoria exibe desgaste R$ 0,00.
- **W16:** Valor fixo aparece na Ficha e integra CustoLote.
- **W17:** Valor fixo com rendimento 5 continua sendo valor do lote; unitário divide o total.
- **W18:** Percentual 5% usa exatamente CustoBaseItens.
- **W19:** perdas/mão de obra/energia não entram na base percentual.
- **W20:** percentual zero com item sem preço mantém desgaste zero, mas custo total continua incompleto pelo item.
- **W21:** percentual positivo com item sem preço mostra desgaste indisponível.
- **W22:** Categoria inativa vinculada continua calculando.
- **W23:** custo de energia zero/positivo não altera o desgaste.
- **W24:** alteração da Categoria reflete no próximo GET sem persistir custo.
- **W25:** detalhamento da precificação mostra Forma/Valor/Base/Custo.
- **W26:** consolidação mostra o novo componente.
- **W27:** preço teórico/sugerido usa novo custo.
- **W28:** margem atual usa novo custo.
- **W29:** Produto inativo continua calculável.
- **W30:** Empresa A não usa configuração de Categoria da Empresa B.
- **W31:** GET não persiste resultado de desgaste.

### Snapshot comercial

- **W32:** snapshot criado antes de alterar desgaste permanece inalterado.
- **W33:** após alterar desgaste, novo snapshot recebe novo CustoReferencia.
- **W34:** histórico continua usando snapshots e não recalcula desgaste antigo.
- **W35:** RegistroPrecoProduto não ganha campo específico de desgaste.

## Alterações esperadas

### Core

~~~text
Produtos/FormaCalculoDesgasteEquipamento.cs
Produtos/CategoriaProduto.cs
Precificacao/CalculadoraCustoDesgasteEquipamentos.cs
Precificacao/CalculadoraCustoProduto.cs
~~~

### Infrastructure

~~~text
CategoriaProdutoConfiguration.cs
migration UC036
PrecificadorDbContextModelSnapshot.cs
~~~

### Web

~~~text
Pages/Produtos/Categorias/*
Precificacao/PrecificacaoProdutoAtual.cs
Pages/Produtos/FichaTecnica.*
Pages/Produtos/Precificacao.*
~~~

Revisar páginas de preço/histórico somente para garantir regressão e snapshot correto; não duplicar fórmula.

## Documentação obrigatória na implementação

Atualizar no mínimo:

- `docs/business/business-rules.md` — RN015/RN017/RN043 + RN057/RN058;
- `docs/features/F002-produtos.md`;
- `docs/features/F003-ficha-tecnica.md`;
- `docs/features/F004-precificacao.md`;
- `docs/use-cases/UC022-calcular-custo-total-unitario.md`;
- `docs/use-cases/UC025-consultar-detalhamento-precificacao.md`;
- `docs/use-cases/UC036-configurar-calcular-desgaste-equipamentos-categoria.md`;
- `docs/use-cases/catalog.md`;
- `docs/development/backlog.md`;
- gate da MEL021.

UC011/UC012 podem receber nota de atualização somente se necessário para deixar explícito que `CustoReferencia` já incorpora o custo corrente completo; não alterar o contrato de snapshot.

## Fora do escopo

- catálogo de equipamentos;
- preço de compra de equipamento;
- vida útil;
- valor residual;
- depreciação contábil;
- depreciação por horas/ciclos;
- quantidade de equipamentos;
- vínculo Categoria -> equipamento;
- vínculo desgaste -> `UsoEquipamentoFicha`;
- cálculo por potência/tempo;
- rateio por unidade antes do UC022;
- histórico/versionamento da configuração da Categoria;
- snapshot específico do componente;
- tornar Categoria obrigatória;
- alterar regras de energia;
- alterar regras de mão de obra/perdas;
- Azure / MEL021;
- Dashboard / UC028+;
- Coleções / UC033–UC034.

## Definition of Done específica

UC036 está concluída quando:

- CategoriaProduto possui Forma + Valor de desgaste;
- migration preserva todas as Categorias e aplica Valor fixo/zero;
- criação/edição/listagem de Categoria administram a configuração;
- cálculo puro de desgaste cobre as duas Formas;
- Produto sem Categoria resulta em zero;
- Categoria inativa vinculada continua aplicando configuração;
- percentual usa somente CustoBaseItens;
- Valor fixo é por lote;
- UC022 soma o quinto componente;
- Ficha Técnica e UC025 exibem a regra/custo;
- UC023/UC024 refletem o novo custo sem duplicação;
- snapshots antigos permanecem congelados;
- novos snapshots usam o novo custo corrente;
- regras RN015/RN017/RN043/RN057/RN058 estão alinhadas;
- nenhuma funcionalidade Azure é implementada;
- MEL021 permanece bloqueada apenas pelo problema externo da conta após a conclusão desta UC;
- build Release está verde;
- unitários estão verdes;
- integração SQL Server/Web está verde;
- backlog marca UC036 como Concluído.

## Branch obrigatória para implementação

~~~text
feat/uc036-desgaste-equipamentos-categoria
~~~

## Commit sugerido

~~~text
feat: calcula desgaste de equipamentos por categoria
~~~
