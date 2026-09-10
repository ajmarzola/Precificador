# Escopo

## MVP

O MVP contempla:

- múltiplas empresas no mesmo sistema;
- autenticação de usuários;
- vínculo de usuários a uma ou mais empresas;
- seleção de Empresa Ativa;
- isolamento de dados por empresa;
- bootstrap do primeiro usuário e primeira empresa;
- cadastro administrativo de empresas e usuários/vínculos em UCs posteriores;
- cadastro, edição, consulta e desativação de insumos;
- histórico de preços de insumos;
- cálculo do custo unitário de compra;
- cadastro, edição, consulta e desativação de produtos;
- ficha técnica genérica por lote/execução;
- rendimento;
- composição por insumos/materiais;
- custos de materiais, embalagens e consumíveis;
- perdas de material/processo quando aplicáveis;
- custo de mão de obra a partir de tempo ativo e valor/hora;
- custo de energia por uso de equipamentos quando aplicável;
- cálculo de custo total e unitário;
- margem-alvo;
- preço teórico e sugerido;
- preço de venda atual e histórico;
- margem atual;
- identificação de produtos abaixo da margem;
- precificação incompleta;
- dashboard;
- configurações de precificação por empresa.

## Fora do MVP

- estoque/movimentação;
- compras e contas a pagar;
- vendas e contas a receber;
- pedidos;
- clientes;
- PDV;
- emissão fiscal;
- contabilidade;
- fluxo de caixa;
- permissões granulares/roles complexos;
- aplicativo móvel nativo;
- microserviços;
- BI avançado;
- previsão de demanda;
- cálculo automático de frete;
- autenticação externa/SSO;
- 2FA, salvo nova avaliação de segurança na publicação.

## Decisões desta fase

- SQLite permanece autorizado como persistência atual;
- multiempresa usa banco/schema compartilhados com `EmpresaId`;
- ASP.NET Core Identity é o mecanismo de autenticação;
- publicação e banco servidor serão reavaliados quando houver definição de hospedagem.

## Pós-MVP conhecido

- preparações intermediárias reutilizáveis, como levain, requeijão, geleia, creme ou recheio, compondo outras fichas técnicas;
- rotina assistida de backup/restauração;
- gráficos históricos avançados.

## Regra de controle de escopo

Necessidades novas devem ser avaliadas e documentadas antes da implementação. Mudanças transversais não devem ser escondidas dentro de um UC funcional existente.
