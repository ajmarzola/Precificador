# F006 — Configurações de Precificação

## Objetivo

Centralizar parâmetros compartilhados por vários produtos da mesma Empresa.

## Configurações mínimas

- percentual de mão de obra sobre os insumos;
- tarifa de energia em R$/kWh;
- margem padrão para novos produtos;
- incremento comercial de arredondamento;
- reserva comercial para desconto.

A potência do forno deixa de ser tratada como configuração global. Quando equipamentos forem modelados, potência será propriedade do equipamento correspondente.

## Modelo inicial — UC026

A configuração é uma entidade 1:1 por Empresa.

`PercentualMaoDeObra` é obrigatório e nasce em `0,10` (10%) conforme MEL022/RN013.

`TarifaEnergiaKwh`, `MargemPadrao` e `IncrementoComercial` não possuem defaults de negócio aprovados e começam como **Não configurado**.

A `ReservaComercialDesconto` nasce em `0,10` (10 p.p.) conforme MEL009/RN052.

UC026 introduz o modelo e consulta read-only. UC027 permite alteração tenant-aware dos cinco parâmetros e ativa o pré-preenchimento de Margem padrão em novos Produtos.

## Comportamento

Alterar uma configuração deve afetar os cálculos atuais dos produtos dependentes **somente da mesma Empresa**, sem necessidade de editar cada produto.

A margem padrão, quando configurada, pré-preenche novos Produtos; o usuário pode alterar o valor antes do cadastro e cada Produto mantém sua Margem-alvo própria depois de criado.

Os três parâmetros opcionais — Tarifa de energia, Margem padrão e Incremento comercial — podem ser limpos de volta para **Não configurado**. `PercentualMaoDeObra` é obrigatório e não pode ser limpo; zero é um valor explícito válido.

A reserva comercial não participa do Preço sugerido. Ela é congelada como referência nos registros comerciais de preço, preservando o histórico contra alterações posteriores da configuração.

## Explicabilidade — MEL017

Após MEL017, as telas de consulta e edição exibem ajuda contextual sempre visível para os cinco parâmetros.

A ajuda deve explicar o efeito real de cada configuração sem alterar regra de negócio:

- Percentual de mão de obra → percentual aplicado sobre o custo base dos insumos; pode superar 100% quando o trabalho artesanal justificar;
- Tarifa de energia → custo dos usos de equipamentos;
- Margem padrão → apenas pré-preenchimento de novos Produtos;
- Incremento comercial → múltiplo monetário que arredonda o Preço teórico para cima e forma o Preço sugerido;
- Reserva comercial → parcela reservada antes do cálculo do Desconto de referência, congelada nos novos registros comerciais.

A edição deve associar semanticamente cada input ao respectivo texto de ajuda, sem depender de `title`, hover ou JavaScript.

## Casos de uso

- [UC026 — Consultar configurações de precificação da Empresa](../use-cases/UC026-consultar-configuracoes-precificacao.md);
- [UC027 — Alterar configurações de precificação da Empresa](../use-cases/UC027-alterar-configuracoes-precificacao.md).

## Regras relacionadas

RN013, RN014, RN017, RN019, RN021, RN025, RN026, RN039, RN052, RN053 e RN054.
