# Escopo

## MVP

O MVP contempla:

- cadastro, edição, consulta e desativação de insumos;
- histórico de preços de insumos;
- cálculo do custo unitário de compra;
- cadastro, edição, consulta e desativação de produtos;
- ficha técnica por lote;
- rendimento do lote em unidades de venda;
- composição por insumos;
- custos de ingredientes, embalagens e consumíveis;
- percentual de perda de ingredientes por produto;
- custo de mão de obra a partir de tempo ativo e valor/hora;
- custo de energia a partir do tempo de forno, potência e tarifa;
- cálculo do custo total por lote e por unidade;
- margem-alvo por produto;
- preço teórico para atingir a margem-alvo;
- preço sugerido com política de arredondamento;
- preço de venda atual;
- margem atual;
- identificação de produtos abaixo da margem-alvo;
- indicação de produtos cuja precificação está incompleta por falta de dados;
- dashboard de acompanhamento;
- configurações globais de produção;
- histórico de alterações do preço de venda.

## Fora do MVP

Não fazem parte do MVP:

- estoque e movimentação de estoque;
- compras e contas a pagar;
- vendas e contas a receber;
- pedidos;
- cadastro de clientes;
- PDV;
- emissão fiscal;
- contabilidade;
- fluxo de caixa;
- autenticação e múltiplos usuários;
- permissões;
- aplicativo móvel nativo;
- sincronização em nuvem;
- microserviços;
- integrações externas;
- BI avançado;
- previsão de demanda;
- cálculo automático de frete.

## Pós-MVP conhecido

Itens reconhecidos, mas não autorizados para implementação no MVP:

- preparações intermediárias reutilizáveis, como levain, requeijão, geleia, creme ou recheio, compondo outras fichas técnicas;
- rotina assistida de backup/restauração pela interface;
- gráficos históricos de custo e margem;
- publicação web multi-dispositivo;
- troca de SQLite por banco servidor caso exista necessidade de acesso concorrente.

## Regra de controle de escopo

Uma necessidade nova deve ser avaliada, documentada e transformada em funcionalidade/caso de uso antes da implementação. Não deve ser incluída incidentalmente em outro caso de uso.
