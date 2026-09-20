# MEL023 — Refinar equipamentos elétricos e navegação de tarifa na Ficha Técnica

- **Origem:** segunda rodada de testes manuais.
- **Classificação:** UX / semântica de energia / navegação.
- **Prioridade:** alta.
- **Estado:** Concluído.
- **Ordem na fila pendente:** 09.
- **Dependências:** MEL022 concluída; UC021 e UC027 concluídos.
- **Gate operacional:** antes da MEL021.
- **Alteração de regra de negócio:** não intencional; consolidar a regra vigente de energia e explicitar sua opcionalidade.
- **Alteração de domínio:** não.
- **Alteração de schema:** não.
- **Migration:** não.
- **Persistência de resultado calculado:** não.

## Objetivo

Tornar explícito na Ficha Técnica que o cadastro de equipamentos existe somente para **equipamentos elétricos cujo consumo de energia participa do custo do lote**, evitando que o usuário tente cadastrar ferramentas manuais sem potência elétrica.

Também criar um fluxo seguro para configurar TarifaEnergiaKwh diretamente a partir da Ficha quando houver equipamento elétrico e a tarifa estiver ausente, sem perder alterações válidas ainda não salvas no Rendimento.

A MEL023 não muda a fórmula de energia nem o modelo persistido criado pelo UC021.

## Problema observado

A Ficha atualmente apresenta:

~~~text
Equipamentos
Adicionar equipamento
~~~

e o cadastro exige:

~~~text
PotenciaKw > 0
TempoUsoMinutos > 0
~~~

A redação genérica induz a interpretação de que toda ferramenta usada no processo deve ser cadastrada.

Exemplo:

~~~text
descanso de panela em crochê
-> agulha de crochê
-> ferramenta necessária
-> não consome energia
-> não pertence a UsoEquipamentoFicha
~~~

O modelo vigente de UsoEquipamentoFicha representa exclusivamente uso elétrico para cálculo de energia.

A regra já implementada em RN014/RN056 e UC021 é:

~~~text
zero usos
=> CustoEnergiaLote = 0
=> componente completo
=> TarifaEnergiaKwh não é necessária
~~~

Portanto, esta MEL corrige principalmente semântica, orientação e navegação.

## Decisões centrais

### D1 — A seção representa somente equipamentos elétricos

Na Ficha Técnica, usar redação inequívoca:

~~~text
Equipamentos elétricos (opcional)
~~~

ou equivalente que preserve explicitamente os dois conceitos:

- elétrico;
- opcional.

O botão deve passar a comunicar a mesma semântica:

~~~text
Adicionar equipamento elétrico
~~~

As páginas Novo/Editar/Remover devem preferir “equipamento elétrico” em títulos, labels, mensagens e textos de confirmação quando isso melhorar a clareza.

Não renomear classes, tabelas ou propriedades persistidas apenas por UX.

### D2 — Ferramentas manuais não são cadastradas

Não cadastrar como UsoEquipamentoFicha itens sem consumo elétrico, por exemplo:

- agulha de crochê;
- tesoura manual;
- régua;
- espátula;
- colher;
- forma;
- panela sem consumo elétrico próprio.

Não criar registro artificial com PotenciaKw = 0 para representar ferramenta manual.

As regras do UC021 permanecem:

~~~text
PotenciaKw > 0
TempoUsoMinutos > 0
~~~

### D3 — Zero equipamentos elétricos é estado completo

Quando não houver UsoEquipamentoFicha:

~~~text
CustoEnergiaLote = 0
Completo = true
~~~

mesmo se TarifaEnergiaKwh = null.

A Ficha deve deixar claro que nenhum cadastro é necessário quando o processo não usa equipamento elétrico.

Redação sugerida:

~~~text
Nenhum equipamento elétrico adicionado.
Se este processo não utiliza equipamentos elétricos, não é necessário cadastrar nenhum.
~~~

Não mostrar aviso de tarifa ausente nesse cenário.

### D4 — Tarifa só é necessária quando existe equipamento elétrico

Com pelo menos um uso e tarifa ausente:

~~~text
há equipamento elétrico
TarifaEnergiaKwh = null
=> consumos em kWh continuam conhecidos
=> custos por uso ficam indisponíveis
=> CustoEnergiaLote = indisponível
=> componente incompleto
~~~

A Ficha deve mostrar:

~~~text
Tarifa de energia não configurada.
Configurar tarifa de energia
~~~

A ação leva para a edição das Configurações de Precificação com retorno à Ficha de origem.

Tarifa igual a zero continua sendo valor válido.

### D5 — Navegar para Configurações deve salvar primeiro a base válida da Ficha

A Ficha possui atualmente um único campo editável na base: Rendimento.

Se o usuário alterar o Rendimento e, antes de pressionar Salvar, escolher Configurar tarifa de energia, a alteração válida não deve ser descartada.

Fluxo normativo:

1. usuário altera Rendimento;
2. aciona Configurar tarifa de energia;
3. a própria Ficha recebe POST;
4. o POST valida o Rendimento;
5. se inválido, não persiste, permanece na Ficha, preserva Input e apresenta erros;
6. se válido, cria ou atualiza a Ficha conforme o fluxo normal e salva;
7. redireciona para edição de Configurações com uma origem local segura;
8. ao salvar Configurações, retorna à Ficha;
9. o próximo GET recalcula energia com a tarifa atualizada.

Não duplicar regra de persistência da Ficha em caminhos divergentes. Reutilizar método/helper privado ou fluxo comum entre o POST normal e o POST de navegação quando apropriado.

## Implementação Web — Ficha Técnica

Rota existente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

### Formulário principal

Dar identidade explícita ao formulário da base, por exemplo:

~~~html
<form method="post" id="ficha-form">
~~~

O comando Configurar tarifa de energia, mesmo renderizado junto à seção de energia, deve submeter esse formulário.

Implementação sugerida:

~~~text
POST /Produtos/FichaTecnica/{id}?handler=ConfigurarTarifa
~~~

ou handler Razor Pages equivalente.

O botão pode usar o atributo HTML form para submeter o formulário principal sem mover toda a seção de energia para dentro dele.

Não usar link GET direto para Configurações quando isso puder descartar Rendimento ainda não salvo.

### Handler dedicado

Adicionar handler equivalente a OnPostConfigurarTarifaAsync.

Responsabilidades:

- resolver Produto tenant-aware pela rota;
- carregar estado atual necessário;
- validar Input.Rendimento com o mesmo helper já usado pelo POST normal;
- preservar Input/erros em falha;
- criar/atualizar Ficha exatamente como o POST normal em sucesso;
- executar SaveChangesAsync;
- construir returnUrl local apontando para a Ficha atual;
- redirecionar para /Configuracoes/Precificacao/Editar.

Não alterar Produto ativo/inativo.

Não criar configuração silenciosamente.

### Renderização do aviso

O comando de configuração aparece somente quando:

~~~text
PossuiFicha = true
E UsosEquipamentos.Count > 0
E CustoEnergiaCompleto = false
~~~

No modelo atual, isso corresponde ao cenário de tarifa necessária e ausente.

Não exibir Configurar tarifa de energia quando:

- não há Ficha;
- não há equipamentos elétricos;
- energia já está completa;
- tarifa zero está explicitamente configurada.

## Implementação Web — Configurações de Precificação

Rota existente:

~~~text
/Configuracoes/Precificacao/Editar
~~~

### returnUrl

Adicionar suporte opcional a returnUrl.

A origem deve ser aceita somente se local, usando validação equivalente a Url.IsLocalUrl.

Nunca redirecionar para URL absoluta externa ou protocol-relative externa.

Se returnUrl estiver ausente ou for inválida, preservar o comportamento atual:

~~~text
salvar
-> /Configuracoes/Precificacao
~~~

### GET

GET de edição:

- carrega configuração como hoje;
- preserva returnUrl local quando recebido;
- não altera dados;
- não cria configuração.

### POST válido

Depois de atualizar a configuração:

~~~text
se returnUrl local
    Redirect(returnUrl)
senão
    RedirectToPage("/Configuracoes/Precificacao")
~~~

A mensagem de sucesso pode ser preservada em TempData.

Ao retornar à Ficha, o GET deve recalcular a energia e deixar de exibir a pendência se a tarifa tiver sido configurada.

### POST inválido

POST inválido:

- permanece na edição;
- preserva os campos informados;
- preserva também returnUrl local;
- não altera banco.

### Cancelar

Quando houver returnUrl local, o comando Cancelar pode retornar diretamente à origem.

Sem origem local, mantém o comportamento atual de voltar à consulta de Configurações.

## Segurança de navegação

A MEL023 não pode introduzir open redirect.

Critérios:

- gerar a origem no servidor;
- aceitar apenas URL local;
- não confiar em host externo recebido por query/form;
- não concatenar domínio manualmente;
- não usar Redirect(returnUrl) antes de validar localidade.

Uma URL inválida deve ser ignorada e cair no destino padrão seguro.

## Regra de energia permanece inalterada

Preservar RN014:

~~~text
ConsumoKwh =
    PotenciaKw × (TempoUsoMinutos / 60m)

CustoEnergiaUso =
    ConsumoKwh × TarifaEnergiaKwh

CustoEnergiaLote =
    soma(CustoEnergiaUso)
~~~

Preservar RN056:

~~~text
zero usos
=> custo 0 / completo

há usos + tarifa null
=> consumo conhecido
=> custo indisponível / incompleto

tarifa = 0
=> custo 0 / completo
~~~

Não arredondar intermediários.

Não persistir consumo ou custo calculado.

## Relação com MEL022

MEL022 removeu TempoAtivoMinutos e ValorHoraTrabalho e deixou a base editável da Ficha apenas com Rendimento.

Não reintroduzir qualquer conceito de mão de obra por tempo.

## Relação com MEL017

A ajuda vigente de TarifaEnergiaKwh continua válida.

MEL023 pode ajustar o texto compartilhado para reforçar que a tarifa só é necessária quando a Ficha possui equipamento elétrico, se isso for necessário para manter coerência entre Configurações e Ficha.

## Relação com MEL021

MEL023 é gate da publicação Azure.

A publicação só deve ocorrer depois que a semântica de equipamentos elétricos e a navegação de tarifa estiverem estabilizadas e testadas.

MEL023 não deve antecipar qualquer provisionamento Azure.

## Documentação que deve ser alinhada na implementação

No mínimo:

- MEL023;
- backlog;
- UC021;
- F003;
- F004;
- F006, se a ajuda da tarifa for refinada;
- RN014/RN056 apenas se necessário para explicitar sem mudança de regra;
- UC027 apenas quanto ao retorno seguro da edição de Configurações.

## Critérios de aceitação

- **CA01:** seção da Ficha comunica explicitamente “equipamentos elétricos”.
- **CA02:** seção comunica explicitamente que o cadastro é opcional.
- **CA03:** botão de inclusão usa redação de equipamento elétrico.
- **CA04:** Novo/Editar/Remover não induzem cadastro de ferramenta manual.
- **CA05:** potência continua estritamente maior que zero.
- **CA06:** tempo de uso continua estritamente maior que zero.
- **CA07:** ferramenta manual não exige registro artificial.
- **CA08:** zero equipamentos mantém energia em 0/completo.
- **CA09:** zero equipamentos + tarifa null não mostra pendência de tarifa.
- **CA10:** com equipamento + tarifa null, consumo em kWh continua visível.
- **CA11:** com equipamento + tarifa null, custo por uso permanece indisponível.
- **CA12:** com equipamento + tarifa null, custo do lote permanece indisponível.
- **CA13:** nesse cenário aparece ação Configurar tarifa de energia.
- **CA14:** tarifa igual a zero continua válida e completa o componente.
- **CA15:** ação de configurar tarifa submete a Ficha antes de navegar.
- **CA16:** Rendimento válido alterado é persistido antes da navegação.
- **CA17:** Rendimento inválido impede navegação e não é persistido.
- **CA18:** POST inválido preserva Input e erros.
- **CA19:** navegação leva à edição de Configurações com origem local da Ficha.
- **CA20:** salvar Configurações retorna à Ficha quando a origem é válida.
- **CA21:** retorno à Ficha recalcula energia com a tarifa atualizada.
- **CA22:** POST inválido de Configurações preserva a origem.
- **CA23:** Cancelar pode retornar à origem local quando fornecida.
- **CA24:** returnUrl externa não é seguida.
- **CA25:** returnUrl inválida cai no destino padrão seguro.
- **CA26:** fluxo normal de Configurações sem returnUrl permanece inalterado.
- **CA27:** GETs continuam sem persistência/lazy-create.
- **CA28:** Produto inativo continua suportado.
- **CA29:** isolamento tenant permanece íntegro.
- **CA30:** antiforgery permanece obrigatório nos POSTs.
- **CA31:** nenhuma migration é criada.
- **CA32:** nenhuma fórmula de custo é alterada.
- **CA33:** nenhuma classe/tabela de domínio é renomeada apenas por UX.
- **CA34:** suíte unitária completa permanece verde.
- **CA35:** suíte de integração SQL Server/Web permanece verde.
- **CA36:** build Release permanece sem warnings novos relevantes.
- **CA37:** MEL021 não é antecipada.

## Matriz mínima de testes

### Web — Ficha / semântica

- **W1:** seção exibe Equipamentos elétricos e indicação de opcionalidade.
- **W2:** botão exibe Adicionar equipamento elétrico.
- **W3:** zero usos exibe mensagem de que nenhum equipamento elétrico foi adicionado.
- **W4:** zero usos + tarifa null mantém Custo de energia do lote = 0.
- **W5:** zero usos + tarifa null não exibe ação para configurar tarifa.

### Web — equipamento com tarifa

- **W6:** uso + tarifa configurada exibe consumo, custo por uso e total.
- **W7:** uso + tarifa zero exibe custos zero e componente completo.
- **W8:** uso + tarifa null mantém consumo conhecido.
- **W9:** uso + tarifa null mostra custos indisponíveis.
- **W10:** uso + tarifa null mostra Configurar tarifa de energia.

### Web — salvar antes de navegar

- **W11:** alterar Rendimento válido + configurar tarifa persiste Rendimento e redireciona.
- **W12:** Rendimento inválido + configurar tarifa retorna HTTP 200 na Ficha, mostra erro e não persiste.
- **W13:** handler não altera Produto/tenant.
- **W14:** Produto inativo segue o mesmo fluxo.
- **W15:** antiforgery é exigido.

### Web — retorno de Configurações

- **W16:** GET de edição aceita returnUrl local.
- **W17:** POST válido com returnUrl local salva e retorna à Ficha.
- **W18:** POST inválido preserva returnUrl e inputs.
- **W19:** Cancelar com returnUrl local retorna à Ficha.
- **W20:** sem returnUrl, salvar continua indo para consulta de Configurações.
- **W21:** returnUrl absoluta externa não é seguida.
- **W22:** returnUrl protocol-relative externa não é seguida.
- **W23:** retorno à Ficha após salvar tarifa recalcula energia.

### Regressão UC021 / UC027

- **W24:** Novo/Editar/Remover de equipamento continuam tenant-aware.
- **W25:** duplicidade por nome continua protegida.
- **W26:** parsing decimal de potência permanece conforme MEL015.
- **W27:** Configuração ausente continua 404 sem lazy-create.
- **W28:** alterações de tarifa continuam isoladas por Empresa.
- **W29:** GET da Ficha não persiste custo.
- **W30:** suíte existente UC021/UC027 permanece verde.

## Testes unitários

Nenhuma nova regra de domínio é introduzida.

Novos testes unitários só são necessários se a implementação extrair lógica pura nova relevante. Não criar testes unitários apenas para textos constantes.

## Arquivos esperados

Não exaustivo:

~~~text
src/Precificador.Web/Pages/Produtos/FichaTecnica.cshtml
src/Precificador.Web/Pages/Produtos/FichaTecnica.cshtml.cs
src/Precificador.Web/Pages/Produtos/FichaTecnica/Equipamentos/Novo.cshtml
src/Precificador.Web/Pages/Produtos/FichaTecnica/Equipamentos/Editar.cshtml
src/Precificador.Web/Pages/Produtos/FichaTecnica/Equipamentos/Remover.cshtml
src/Precificador.Web/Pages/Configuracoes/Precificacao/Editar.cshtml
src/Precificador.Web/Pages/Configuracoes/Precificacao/Editar.cshtml.cs
tests/Precificador.Tests.Integration/Web/FichaTecnicaCustoPageTests.cs
tests/Precificador.Tests.Integration/Web/UsoEquipamentoFichaPageTests.cs
tests/Precificador.Tests.Integration/Web/ConfiguracaoPrecificacaoPageTests.cs
docs/development/improvements/MEL023-equipamentos-eletricos-navegacao-tarifa.md
docs/development/backlog.md
docs/use-cases/UC021-calcular-custo-energia-equipamentos.md
docs/features/F003-ficha-tecnica.md
docs/features/F004-precificacao.md
~~~

## Fora do escopo

- cadastrar ferramentas manuais;
- custo de aquisição de equipamento;
- depreciação;
- manutenção;
- vida útil;
- catálogo global de equipamentos;
- patrimônio;
- potência zero em UsoEquipamentoFicha;
- consumo de água;
- consumo de gás;
- outros utilitários;
- alterar fórmula de energia;
- tornar tarifa obrigatória para Ficha sem equipamento elétrico;
- migration/schema;
- renomear tabela/classe UsoEquipamentoFicha;
- alterar modelo de mão de obra;
- publicação Azure;
- UC028+.

## Definition of Done específica

MEL023 está concluída quando:

- a Ficha comunica claramente que equipamentos são elétricos e opcionais;
- ferramentas manuais deixam de ser sugeridas implicitamente pelo fluxo;
- zero usos continua energia 0/completo sem exigir tarifa;
- tarifa ausente com usos oferece navegação explícita para Configurações;
- a navegação salva primeiro um Rendimento válido;
- Rendimento inválido impede saída e preserva erros;
- edição de Configurações suporta retorno local seguro;
- salvar tarifa pode retornar diretamente à Ficha;
- nenhum open redirect é possível pelo novo fluxo;
- UC021/RN014/RN056 permanecem semanticamente inalterados;
- nenhuma migration é criada;
- documentação normativa é alinhada;
- build Release está verde;
- suíte unitária está verde;
- suíte integração SQL Server/Web está verde;
- backlog altera somente MEL023 de Pronto para Concluído na PR de implementação;
- MEL021 continua sem implementação até o merge da MEL023.

## Branch obrigatória para implementação

~~~text
fix/mel023-equipamentos-eletricos-tarifa
~~~

## Commit sugerido

~~~text
fix: refina equipamentos eletricos e navegacao de tarifa
~~~
