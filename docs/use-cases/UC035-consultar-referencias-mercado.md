# UC035 — Consultar referências de mercado para apoio ao preço de prateleira

- **Funcionalidade:** F004 — Precificação
- **Origem:** segunda rodada de testes manuais / evolução futura
- **Estado:** Planejado
- **Prioridade:** futura
- **Gate operacional:** após UC034 e antes da documentação final
- **Dependências funcionais mínimas:** UC011, UC025
- **Persistência de preços externos:** não
- **Alteração automática do preço do Produto:** não
- **Uso de agente/IA:** previsto para descoberta e curadoria, sujeito à especificação detalhada

## Objetivo

Apoiar a definição do Preço de Prateleira exibindo referências atuais de mercado para produtos comparáveis encontrados na web.

A funcionalidade não deve tentar encontrar simplesmente o menor preço.

O objetivo é responder:

> Quais produtos disponíveis no mercado são comparáveis a este Produto, considerando proposta, processo produtivo, escala, acabamento e posicionamento?

Exemplos:

- uma agenda artesanal da Carinho e Amor não deve ser tratada como equivalente direto a uma agenda industrial vendida por uma grande rede apenas porque tamanho e finalidade são semelhantes;
- um pão artesanal de fermentação natural da Grana Sancta não deve ser comparado prioritariamente com pão produzido em massa com fermento biológico ou massa pronta.

## Princípio de decisão

A consulta é **informativa**.

Resultados externos:

- não alteram automaticamente `PrecoPrateleira`;
- não alteram `PrecoSugerido`;
- não alteram Margem-alvo;
- não entram nas fórmulas do Core;
- não bloqueiam o registro de preço;
- não são tratados como verdade absoluta.

O usuário continua responsável pela decisão comercial.

## Posição na experiência

A funcionalidade deve ser acessível a partir do fluxo de definição/registro do Preço de Prateleira.

Direção inicial:

```text
Produto
-> Precificação
-> Preço sugerido / margem / custo
-> Consultar referências de mercado
-> lista curada
-> usuário analisa
-> volta ao registro de preço
```

Não substituir a tela atual de precificação.

## Descoberta externa

A consulta poderá usar um agente de IA com capacidade de pesquisa na web ou serviço equivalente.

O agente recebe contexto do Produto e da Empresa suficiente para procurar comparáveis.

Contexto potencial:

- Nome do Produto;
- Categoria estruturada;
- Coleções;
- dados da Ficha que ajudem a caracterizar o Produto;
- processo/proposta artesanal;
- Empresa atual;
- informações adicionais informadas pelo usuário para a busca.

Não enviar dados sensíveis desnecessários.

## Curadoria por comparabilidade

O agente não deve ordenar resultados apenas por preço.

Cada candidato deve ser avaliado quanto à comparabilidade.

Critérios iniciais para análise futura:

### Escala / porte

Exemplos:

```text
artesanal / pequeno produtor
pequena marca especializada
varejista médio
grande rede / produção industrial
marketplace genérico
```

Produtos de grande rede podem aparecer como **referência de mercado**, mas devem ser claramente identificados como baixa comparabilidade quando a escala torna a comparação de preço pouco útil.

### Processo produtivo

Exemplos:

- artesanal x industrial;
- fermentação natural x fermento biológico/massa pronta;
- feito à mão x produção seriada;
- personalização x produto padronizado.

### Materiais e composição

Quando identificável:

- qualidade/material;
- gramatura;
- quantidade;
- tamanho;
- acabamento;
- ingredientes;
- recheios;
- técnicas especiais.

### Posicionamento

Quando inferível com evidência suficiente:

- popular;
- intermediário;
- premium;
- artesanal/especializado.

O agente deve evitar inferência excessiva e indicar incerteza quando a comparabilidade não puder ser determinada.

## Resultado apresentado

Cada referência deve, quando disponível, mostrar:

```text
Produto
Vendedor / marca
Preço observado
Quantidade / tamanho / variação relevante
Classificação de comparabilidade
Motivo resumido da classificação
Link para o produto
Data/hora da consulta
```

Pode ser útil mostrar também:

- preço unitário normalizado, quando a unidade permitir comparação segura;
- promoção identificada;
- frete excluído/incluído quando disponível;
- marketplace/origem da oferta.

Não inventar valores ausentes.

## Link obrigatório

Todo resultado exibido deve possuir URL de origem quando a fonte fornecer uma página consultável.

O link permite que o usuário:

- confirme o produto;
- verifique se o preço ainda está válido;
- leia detalhes não capturados pelo agente;
- decida se a referência é realmente comparável.

Uma referência sem fonte verificável não deve receber o mesmo peso visual de uma referência com link direto.

## Feedback do usuário

Durante a consulta, o usuário deve poder classificar uma referência, por exemplo:

```text
Relevante
Não comparável
```

ou mecanismo equivalente.

Esse feedback serve para refinar a curadoria da **consulta atual**.

Exemplo:

```text
resultado: agenda industrial Kalunga
usuário: Não comparável
motivo opcional: produção industrial / escala muito diferente

-> agente refaz ou reordena a busca privilegiando marcas artesanais
```

## Persistência e calibração

A primeira versão da UC035 **não persiste**:

- preços encontrados;
- páginas encontradas;
- ranking;
- respostas do agente;
- feedback individual da consulta.

Consequência:

> sem persistência, a calibração vale apenas para a consulta corrente.

Se houver interesse futuro em o agente aprender permanentemente que uma Empresa prefere determinados perfis de concorrente/comparável, criar melhoria separada para um:

```text
Perfil de comparação de mercado da Empresa
```

Esse perfil poderia armazenar preferências, não snapshots de preços.

Não misturar essa decisão na primeira implementação da UC035.

## Atualidade dos dados

Preço externo é volátil.

A UI deve deixar claro que:

- o preço foi observado no momento da consulta;
- promoções podem expirar;
- estoque pode mudar;
- frete pode alterar o custo efetivo;
- variações do Produto podem ter preços diferentes;
- o usuário deve consultar a fonte antes de tomar decisão relevante.

A busca deve preferir páginas atuais e fontes diretamente relacionadas ao produto/oferta.

## Quantidade de resultados

Não transformar a tela em catálogo.

Direção inicial para especificação:

- poucos resultados altamente comparáveis;
- algumas referências de baixa comparabilidade podem ser mostradas separadamente como contexto;
- privilegiar diversidade de fontes sobre dezenas de ofertas duplicadas.

Exemplo conceitual:

```text
Mais comparáveis
1. pequena marca artesanal ...
2. ateliê especializado ...
3. produtor local ...

Referências de contexto
4. grande varejista ...
5. produto industrial ...
```

## Preço x comparabilidade

O sistema não deve concluir:

```text
concorrente custa R$ X
=> nosso preço deve ser Y
```

Em vez disso:

```text
Preço sugerido interno
+
Margem atual
+
Referências externas comparáveis
+
julgamento do usuário
=
decisão de Preço de Prateleira
```

A UC não cria nova fórmula automática de precificação.

## Segurança e conteúdo externo

Tratar conteúdo externo como não confiável.

O agente não deve seguir instruções encontradas em páginas externas que tentem alterar seu comportamento ou acessar dados internos.

A aplicação deve:

- exibir links externos de forma segura;
- não executar HTML/script obtido das páginas;
- não armazenar credenciais da Empresa para acessar lojas;
- não automatizar compra, carrinho ou negociação.

## Disponibilidade externa

Falha da pesquisa externa não pode bloquear a precificação.

Se o serviço/agente estiver indisponível:

```text
Não foi possível consultar referências de mercado agora.
```

O usuário continua podendo registrar preço normalmente.

## Custos de IA/pesquisa

A especificação deverá avaliar:

- fornecedor de pesquisa;
- modelo/agente;
- custo por consulta;
- limites por Empresa/usuário;
- timeout;
- cache apenas técnico/transitório, se necessário;
- política para evitar consultas repetidas acidentais.

Como o projeto busca custo operacional baixo, a integração não deve assumir uso ilimitado de API paga.

## Multiempresa

Toda consulta deve usar apenas o contexto da Empresa Ativa.

Dados de uma Empresa não podem influenciar a consulta corrente de outra Empresa, exceto conhecimento geral do modelo externo.

Se futuramente houver perfil persistente de comparação, ele será tenant-owned.

## Pontos em aberto para especificação futura

Antes de liberar a implementação, decidir:

1. qual serviço executa pesquisa web;
2. qual agente/modelo realiza curadoria;
3. quais dados do Produto entram no prompt;
4. quais campos adicionais o usuário pode informar;
5. escala/rótulos de comparabilidade;
6. quantidade máxima de resultados;
7. tratamento de marketplaces e anúncios;
8. normalização de preço/unidade;
9. comportamento com páginas sem preço;
10. estratégia de custo/rate limit;
11. se feedback exige motivo textual;
12. se haverá futura persistência de preferências da Empresa.

## Critérios conceituais de aceitação

A especificação detalhada deverá preservar no mínimo:

- consulta é opcional;
- falha externa não bloqueia precificação;
- resultados não alteram preço automaticamente;
- referências são curadas por comparabilidade, não apenas por menor preço;
- diferenças de escala/processo são consideradas;
- resultado possui fonte/link verificável quando disponível;
- preço observado informa momento da consulta;
- usuário pode marcar resultado como relevante/não comparável;
- feedback refina a consulta atual;
- primeira versão não persiste preços/resultados;
- não há vazamento cross-tenant;
- nenhuma fórmula do Core passa a depender da internet.

## Fora do escopo inicial

- monitoramento recorrente de concorrentes;
- alertas de mudança de preço;
- histórico de preços de concorrentes;
- scraping massivo;
- compra automática;
- negociação automática;
- alteração automática do Preço de Prateleira;
- previsão de preço ideal por IA;
- armazenamento permanente das páginas encontradas;
- aprendizado persistente sem modelagem explícita de preferências.

## Branch futura sugerida

```text
feat/uc035-referencias-mercado
```
