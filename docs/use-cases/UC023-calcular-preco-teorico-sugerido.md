# UC023 — Calcular preço teórico e sugerido

- **Funcionalidade:** F004 — Precificação
- **Dependências funcionais:** UC022 e UC027
- **Base de revalidação:** UC022 e UC027 concluídas
- **Schema:** não
- **Persistência do resultado:** não
- **Regras principais:** RN017, RN019, RN020, RN021, RN025, RN026 e RN045
- **Relação com MEL009:** ReservaComercialDesconto não participa deste cálculo

## Objetivo

Calcular, para um Produto com custo unitário conhecido:

1. o **Preço teórico**, necessário para atingir exatamente a MargemAlvo do Produto;
2. o **Preço sugerido**, obtido pelo arredondamento comercial do Preço teórico para cima segundo o IncrementoComercial vigente da Empresa.

A UC023 completa o cálculo econômico anterior à decisão comercial do usuário.

Ela não registra preço de venda e não cria histórico. O registro comercial e seu snapshot pertencem ao UC011.

## Entradas do cálculo

A UC023 utiliza exclusivamente:

~~~text
CustoUnitarioProduto
MargemAlvo
IncrementoComercial
~~~

Origem:

~~~text
CustoUnitarioProduto => UC022
MargemAlvo           => Produto / RN045
IncrementoComercial  => ConfiguracaoPrecificacaoEmpresa / UC027
~~~

Não utilizar MargemPadrao, ReservaComercialDesconto ou PrecoPrateleira no cálculo.

### MargemAlvo x MargemPadrao

A margem usada no cálculo é sempre Produto.MargemAlvo.

MargemPadrao serve somente para pré-preencher novos Produtos. Alterá-la não altera Produto existente nem seu Preço teórico ou sugerido.

### ReservaComercialDesconto

Conforme MEL009, ReservaComercialDesconto não participa de PrecoTeorico ou PrecoSugerido.

A reserva será utilizada por UC011/UC012 na relação entre Preço sugerido e Preço de prateleira. Alterar a reserva comercial não recalcula Preço sugerido.

## Preço teórico

Aplicar RN020:

~~~text
PrecoTeorico = CustoUnitarioProduto / (1 - MargemAlvo)
~~~

Exemplo:

~~~text
CustoUnitarioProduto = 20,00
MargemAlvo = 20% = 0,20

PrecoTeorico = 20 / (1 - 0,20) = 25,00
~~~

O Preço teórico representa o valor exato necessário para que a margem sobre o preço seja igual à MargemAlvo.

## Margem válida

Aplicar RN019/RN045:

~~~text
0 <= MargemAlvo < 1
~~~

A calculadora deve validar defensivamente essa invariante.

## Preço sugerido

Aplicar RN021.

O Preço sugerido é o menor múltiplo de IncrementoComercial que seja maior ou igual ao Preço teórico.

~~~text
PrecoSugerido = Ceiling(PrecoTeorico / IncrementoComercial) * IncrementoComercial
~~~

Usar operação decimal. Não converter para double.

### Exemplo — arredondamento para cima

~~~text
PrecoTeorico = 25,08
IncrementoComercial = 0,50

25,08 / 0,50 = 50,16
Ceiling(50,16) = 51
PrecoSugerido = 25,50
~~~

### Preço já múltiplo do incremento

Se o Preço teórico já for exatamente múltiplo do incremento, ele não recebe um incremento adicional.

~~~text
PrecoTeorico = 25,00
IncrementoComercial = 0,50

PrecoSugerido = 25,00
~~~

Portanto:

~~~text
PrecoSugerido >= PrecoTeorico
~~~

mas não necessariamente PrecoSugerido > PrecoTeorico.

## Incremento comercial

Conforme RN025, quando configurado:

~~~text
IncrementoComercial > 0
~~~

null significa Não configurado e nunca deve ser transformado em zero.

## Semântica de completude

A UC023 possui dois níveis de resultado.

### Custo unitário indisponível

Se CustoUnitarioProduto = null:

~~~text
PrecoTeorico = null
PrecoSugerido = null
Completo = false
~~~

A UC023 não tenta reconstruir o custo.

### Custo conhecido + incremento não configurado

Se o custo é conhecido e IncrementoComercial = null:

~~~text
PrecoTeorico = CustoUnitarioProduto / (1 - MargemAlvo)
PrecoSugerido = null
Completo = false
~~~

O Preço teórico continua determinável e deve permanecer visível. Não escondê-lo apenas porque o arredondamento comercial está ausente.

### Todos os dados conhecidos

Se CustoUnitarioProduto é conhecido, MargemAlvo é válida e IncrementoComercial é conhecido:

~~~text
PrecoTeorico = calculado
PrecoSugerido = calculado
Completo = true
~~~

## Null x zero

Custo conhecido igual a zero é diferente de custo desconhecido.

~~~text
CustoUnitarioProduto = 0
MargemAlvo = 0,30
IncrementoComercial = 0,50

=> PrecoTeorico = 0
=> PrecoSugerido = 0
=> Completo = true
~~~

UC023 não inventa preço mínimo.

IncrementoComercial igual a zero não é válido e deve ser rejeitado defensivamente.

## Precisão

Aplicar RN026.

Não arredondar CustoUnitarioProduto, 1 - MargemAlvo, PrecoTeorico ou a divisão pelo IncrementoComercial.

Exemplo:

~~~text
PrecoTeorico = 25,081
IncrementoComercial = 0,01

25,081 / 0,01 = 2508,1
Ceiling = 2509
PrecoSugerido = 25,09
~~~

Arredondar PrecoTeorico previamente para centavos poderia produzir resultado incorreto.

## Calculadora pura

Criar componente em Precificador.Core.Precificacao, preferencialmente CalculadoraPrecoProduto.

Entrada conceitual:

~~~text
decimal? CustoUnitarioProduto
decimal MargemAlvo
decimal? IncrementoComercial
~~~

Saída:

~~~text
decimal? PrecoTeorico
decimal? PrecoSugerido
bool Completo
~~~

Semântica:

~~~text
validar MargemAlvo
validar custo conhecido >= 0
validar incremento conhecido > 0

se CustoUnitarioProduto == null
    PrecoTeorico = null
    PrecoSugerido = null
    Completo = false
senão
    PrecoTeorico = CustoUnitarioProduto / (1 - MargemAlvo)

    se IncrementoComercial == null
        PrecoSugerido = null
        Completo = false
    senão
        PrecoSugerido = Ceiling(PrecoTeorico / IncrementoComercial) * IncrementoComercial
        Completo = true
~~~

A calculadora não acessa EF, HTTP, tenant, preços de Insumo, configuração, formatação ou persistência.

## Reuso do UC022

A UC023 deve consumir diretamente CustoUnitarioProduto já calculado por CalculadoraCustoProduto.

Não repetir CustoLote / Rendimento dentro da calculadora de preço e não recalcular UC018–UC021.

## Integração com Produto

A projeção atual do Produto usada pela Ficha deve passar a incluir MargemAlvo na mesma query existente.

A MargemAlvo usada é sempre a persistida no Produto.

## Integração com configuração

A consulta existente de ConfiguracaoPrecificacaoEmpresa, já usada para ValorHoraTrabalho e TarifaEnergiaKwh, deve ser ampliada para trazer IncrementoComercial na mesma projeção/query.

UC023 não deve criar uma segunda consulta de configuração.

Não carregar MargemPadrao ou ReservaComercialDesconto apenas para o cálculo desta UC.

## Integração Web

Evoluir a tela existente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

Não criar nova página de precificação neste UC.

Após o bloco de custo do Produto, apresentar:

~~~text
Margem-alvo
Preço teórico
Preço sugerido
~~~

Exemplo completo:

~~~text
Margem-alvo: 30%
Preço teórico: 14,2857
Preço sugerido: 14,50
~~~

Margem deve ser apresentada como percentual. Preços devem usar formatação monetária consistente sem alterar o valor interno.

## Incremento comercial ausente

Com custo conhecido e IncrementoComercial = null, apresentar:

~~~text
Margem-alvo: 30%
Preço teórico: <valor>
Preço sugerido: indisponível
Incremento comercial não configurado.
Precificação incompleta.
~~~

Não apresentar Preço sugerido igual ao Preço teórico e não usar incremento arbitrário.

## Custo incompleto

Quando UC022 estiver incompleto:

~~~text
Preço teórico: indisponível
Preço sugerido: indisponível
~~~

Preservar os motivos específicos apresentados pelos componentes anteriores.

## Produto sem Ficha

Produto sem Ficha não possui Preço teórico ou sugerido calculável, não cria Ficha/configuração e não persiste cálculo.

## Ficha sem Itens

Ficha existente sem Itens continua incompleta por UC018/UC022; logo os dois preços permanecem indisponíveis.

## Produto inativo

Produto inativo continua calculável normalmente. UC023 não reativa Produto.

## POST válido da Ficha

Preservar PRG:

~~~text
POST válido
=> salvar Ficha
=> redirect
=> GET
=> recalcular custos
=> UC022
=> UC023
~~~

## POST inválido da Ficha

O Input inválido permanece na tela, mas CustoUnitarioProduto, PrecoTeorico e PrecoSugerido devem representar o estado persistido.

Não usar Rendimento ou TempoAtivo ainda não salvos.

## Recalculo atual

Os resultados são derivados e devem refletir no próximo GET alterações válidas em:

- qualquer dependência que altere CustoUnitarioProduto;
- MargemAlvo do Produto;
- IncrementoComercial da Empresa.

Alterar somente IncrementoComercial não altera PrecoTeorico, mas pode alterar PrecoSugerido.

Alterar somente MargemPadrao ou ReservaComercialDesconto não altera o cálculo de Produto existente.

## Configuração ausente

A configuração 1:1 da Empresa é invariante estabelecida por UC026. Se estiver ausente inesperadamente, preservar o tratamento atual, sem lazy-create no GET.

## Multiempresa

A UC023 não introduz entidade tenant-owned.

Produto e configuração devem continuar vindo da Empresa Ativa, sem IgnoreQueryFilters. A calculadora pura não recebe EmpresaId.

Empresa A nunca pode usar IncrementoComercial da Empresa B.

## Persistência

Nenhuma alteração de schema.

Não adicionar PrecoTeorico/PrecoSugerido a Produto ou Ficha, não criar tabela de cálculo, histórico, snapshot ou migration.

O snapshot de PrecoSugerido pertence ao UC011.

## Critérios de aceitação

- **CA01:** Preço teórico segue RN020.
- **CA02:** MargemAlvo usada é a do Produto, não MargemPadrao.
- **CA03:** Margem zero é válida.
- **CA04:** Margem inválida é rejeitada defensivamente pela calculadora.
- **CA05:** custo unitário indisponível torna ambos os preços indisponíveis.
- **CA06:** custo unitário zero é valor conhecido válido.
- **CA07:** IncrementoComercial null mantém Preço teórico disponível e Preço sugerido indisponível.
- **CA08:** IncrementoComercial conhecido deve ser maior que zero.
- **CA09:** Preço sugerido é o menor múltiplo do incremento maior ou igual ao Preço teórico.
- **CA10:** Preço teórico já múltiplo do incremento permanece inalterado.
- **CA11:** não existe arredondamento intermediário do Preço teórico.
- **CA12:** Preço sugerido nunca fica abaixo do Preço teórico.
- **CA13:** UC023 reutiliza CustoUnitarioProduto do UC022.
- **CA14:** mesma consulta de configuração existente é reutilizada para IncrementoComercial.
- **CA15:** alterar IncrementoComercial recalcula apenas Preço sugerido quando demais entradas não mudam.
- **CA16:** alterar MargemAlvo recalcula Preço teórico e sugerido.
- **CA17:** alterar MargemPadrao não altera preços de Produto existente.
- **CA18:** alterar ReservaComercialDesconto não altera Preço teórico ou sugerido.
- **CA19:** Produto inativo continua calculável.
- **CA20:** Produto sem Ficha não cria estado nem preços artificiais.
- **CA21:** Ficha sem Itens permanece sem preços calculáveis.
- **CA22:** POST inválido da Ficha usa estado persistido.
- **CA23:** nenhum resultado da UC023 é persistido.
- **CA24:** isolamento multiempresa permanece preservado.
- **CA25:** configuração inesperadamente ausente não é criada durante consulta.

## Matriz de testes

### Unitários

- U1: fórmula normal.
- U2: margem zero.
- U3: custo zero.
- U4: arredondamento para cima.
- U5: múltiplo exato não sobe.
- U6: incremento pequeno sem arredondamento prévio.
- U7: custo indisponível.
- U8: incremento não configurado mantém teórico.
- U9: margem próxima de 100%, mas válida.
- U10: margem negativa rejeitada.
- U11: margem igual a 1 rejeitada.
- U12: custo negativo conhecido rejeitado.
- U13: incremento zero rejeitado.
- U14: incremento negativo rejeitado.
- U15: precisão sem arredondamento intermediário.
- U16: resultado completo preserva PrecoSugerido >= PrecoTeorico e múltiplo do incremento.

### Web

- W1: Ficha completa + incremento configurado exibe MargemAlvo, PrecoTeorico e PrecoSugerido corretos.
- W2: margem zero calcula corretamente.
- W3: preço teórico já múltiplo do incremento não sobe.
- W4: preço teórico entre múltiplos arredonda para cima.
- W5: custo incompleto deixa ambos os preços indisponíveis.
- W6: incremento null + custo completo mantém PrecoTeorico visível e PrecoSugerido indisponível.
- W7: incremento null apresenta mensagem específica.
- W8: mudança de IncrementoComercial recalcula PrecoSugerido sem alterar PrecoTeorico.
- W9: mudança de MargemAlvo recalcula ambos.
- W10: mudança de custo recalcula ambos.
- W11: mudança exclusiva de MargemPadrao não altera Produto existente nem preços.
- W12: mudança exclusiva de ReservaComercialDesconto não altera os preços.
- W13: Produto inativo continua calculável.
- W14: Produto sem Ficha não exibe preços nem cria Ficha.
- W15: Ficha vazia mantém preços indisponíveis.
- W16: Item sem preço vigente mantém preços indisponíveis.
- W17: mão de obra incompleta mantém preços indisponíveis.
- W18: energia incompleta mantém preços indisponíveis.
- W19: POST inválido usa estado persistido.
- W20: GET não persiste PrecoTeorico/PrecoSugerido.
- W21: IncrementoComercial da Empresa B não influencia Produto da Empresa A.
- W22: configuração 1:1 ausente preserva tratamento atual e não é criada.

## Alterações esperadas

### Core

Criar CalculadoraPrecoProduto e ResultadoCalculoPrecoProduto, ou nomes equivalentes claros.

### Web

Evoluir FichaTecnica.cshtml e FichaTecnica.cshtml.cs, adicionando MargemAlvo à projeção do Produto, IncrementoComercial à projeção existente da configuração, resultado da calculadora e apresentação.

### Infrastructure / Banco

Nenhuma alteração esperada. Nenhuma migration.

## Documentação

Atualizar F004, catálogo, backlog e esclarecer RN021 com a fórmula exata e o caso de múltiplo exato.

## Fora do escopo

- registrar Preço de prateleira;
- RegistroPrecoProduto;
- snapshots de custo/margem/preço;
- ReservaComercialReferencia;
- DescontoReferencia;
- histórico comercial;
- margem atual;
- comparação com Preço de prateleira;
- estados AbaixoDaMargem/DentroDaMargem;
- dashboard;
- impostos, comissão ou frete;
- markup alternativo;
- preço mínimo/promocional;
- arredondamento psicológico;
- alterar MargemAlvo/configurações;
- persistir PrecoTeorico/PrecoSugerido.

## Gate

UC022 está concluída e fornece CustoUnitarioProduto.

UC027 está concluída e fornece IncrementoComercial.

Produto já possui MargemAlvo.

RN020/RN021 definem o cálculo econômico, e MEL009 exclui ReservaComercialDesconto da UC023.

Não existe alteração estrutural pendente.

UC023 está liberada para implementação após o merge desta documentação.

## Branch sugerida

~~~text
feat/uc023-preco-teorico-sugerido
~~~

## Commit sugerido

~~~text
feat: calcula preco teorico e sugerido
~~~
