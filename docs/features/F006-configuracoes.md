# F006 — Configurações de Precificação

## Objetivo

Centralizar parâmetros compartilhados por vários produtos da mesma Empresa.

## Configurações mínimas

- valor da hora de trabalho;
- tarifa de energia em R$/kWh;
- margem padrão para novos produtos;
- incremento comercial de arredondamento;
- reserva comercial para desconto.

A potência do forno deixa de ser tratada como configuração global. Quando equipamentos forem modelados, potência será propriedade do equipamento correspondente.

## Modelo inicial — UC026

A configuração é uma entidade 1:1 por Empresa.

Os parâmetros `ValorHoraTrabalho`, `TarifaEnergiaKwh`, `MargemPadrao` e `IncrementoComercial` não possuem defaults de negócio aprovados e começam como **Não configurado**.

A `ReservaComercialDesconto` nasce em `0,10` (10 p.p.) conforme MEL009/RN052.

UC026 introduz o modelo e consulta read-only. UC027 permitirá alteração.

## Comportamento

Alterar uma configuração deve afetar os cálculos atuais dos produtos dependentes **somente da mesma Empresa**, sem necessidade de editar cada produto.

A margem padrão vale como valor inicial de novos produtos quando essa integração for habilitada; depois de criado, cada produto mantém sua margem-alvo própria.

Parâmetros ausentes não são tratados como zero; o cálculo futuro dependente permanece incompleto.

A reserva comercial não participa do Preço sugerido. Ela será congelada como referência em registros comerciais futuros pelo UC011.

## Casos de uso

- [UC026 — Consultar configurações de precificação da Empresa](../use-cases/UC026-consultar-configuracoes-precificacao.md);
- UC027 — Alterar configurações de precificação da Empresa.

## Regras relacionadas

RN013, RN014, RN017, RN019, RN021, RN025, RN026, RN039 e RN052.
