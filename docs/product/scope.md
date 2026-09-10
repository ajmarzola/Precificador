# Escopo

## MVP

O MVP contempla:

- múltiplas empresas no mesmo sistema;
- autenticação de usuários;
- vínculo de usuários a uma ou mais empresas;
- seleção de Empresa Ativa e isolamento de dados por empresa;
- bootstrap do primeiro usuário e primeira empresa;
- cadastro administrativo de empresas/usuários e vínculos em UCs posteriores;
- cadastro, edição, consulta e desativação de insumos;
- histórico de preços de insumos;
- cálculo do custo unitário de compra;
- cadastro, edição, consulta e desativação de produtos;
- ficha técnica por lote/execução;
- rendimento do lote em unidades de venda;
- composição por insumos;
- custos de ingredientes/materiais, embalagens e consumíveis;
- perdas de material/processo quando aplicáveis;
- custo de mão de obra a partir de tempo ativo e valor/hora;
- custo de energia/recursos a partir de uso de equipamentos quando aplicável;
- cálculo do custo total por lote e por unidade;
- margem-alvo por produto;
- preço teórico e preço sugerido;
- preço de venda atual e histórico;
- margem atual;
- identificação de produtos abaixo da margem-alvo;
- precificação incompleta;
- dashboard de acompanhamento;
- configurações de precificação por empresa.

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
- permissões granulares/roles complexos;
- aplicativo móvel nativo;
- microserviços;
- integrações externas;
- BI avançado;
- previsão de demanda;
- cálculo automático de frete;
- autenticação externa/SSO;
- 2FA, salvo nova avaliação de segurança quando a publicação for definida.

## Decisões desta fase

- SQLite permanece autorizado;
- multiempresa usa banco/schema compartilhados com `EmpresaId`;
- ASP.NET Core Identity é o mecanismo de autenticação;
- publicação e eventual banco servidor serão reavaliados quando houver definição de hospedagem.

## Pós-MVP conhecido

Itens reconhecidos, mas não autorizados para implementação no MVP:

- preparações intermediárias reutilizáveis, como levain, requeijão, geleia, creme ou recheio, compondo outras fichas técnicas;
- rotina assistida de backup/restauração pela interface;
- gráficos históricos de custo e margem.

## Regra de controle de escopo

Uma necessidade nova deve ser avaliada, documentada e transformada em funcionalidade/caso de uso antes da implementação. Mudanças transversais não devem ser incluídas incidentalmente em outro caso de uso.
