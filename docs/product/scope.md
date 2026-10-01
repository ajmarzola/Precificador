# Escopo

## MVP

O MVP contempla:

- múltiplas empresas no mesmo sistema;
- autenticação de usuários;
- vínculo de usuários a uma ou mais empresas;
- seleção de Empresa Ativa e isolamento de dados por empresa;
- bootstrap protegido do primeiro Administrador do Sistema, separado de Empresas;
- solicitação controlada de acesso, ativação/recuperação de conta por e-mail, administração de Empresas e gestão de usuários/vínculos;
- perfis Administrador/Operacional contextualizados por Empresa;
- cadastro, edição, consulta e desativação de insumos;
- histórico de preços de insumos;
- cálculo do custo unitário de compra;
- cadastro, edição, consulta e desativação de produtos;
- ficha técnica por lote/execução;
- rendimento do lote em unidades de venda;
- composição por insumos;
- custos de matérias-primas, embalagens e consumíveis;
- perdas de material/processo quando aplicáveis;
- custo de mão de obra a partir de percentual sobre o custo base dos insumos;
- custo de energia/recursos a partir de uso de equipamentos quando aplicável;
- cálculo do custo total por lote e por unidade;
- margem-alvo por produto;
- preço teórico e Preço sugerido;
- Preço de prateleira atual e histórico de snapshots de precificação;
- Desconto de referência derivado para decisões comerciais;
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
- matriz complexa de permissões por funcionalidade; roles globais adicionais além das necessárias à administração do sistema;
- aplicativo móvel nativo;
- microserviços;
- integrações externas de negócio, exceto a infraestrutura transacional de e-mail necessária a ativação/recuperação de conta;
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
