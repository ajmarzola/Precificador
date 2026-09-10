# F006 — Configurações de Precificação

## Objetivo

Centralizar parâmetros compartilhados por vários produtos da mesma Empresa.

## Configurações mínimas

- valor da hora de trabalho;
- tarifa de energia em R$/kWh;
- margem padrão para novos produtos;
- incremento comercial de arredondamento.

A potência do forno deixa de ser tratada como configuração global. Quando equipamentos forem modelados, potência será propriedade do equipamento correspondente.

## Comportamento

Alterar uma configuração deve afetar os cálculos atuais dos produtos dependentes **somente da mesma Empresa**, sem necessidade de editar cada produto.

A margem padrão vale como valor inicial de novos produtos; depois de criado, cada produto mantém sua margem-alvo própria.

## Regras relacionadas

RN013, RN014, RN019, RN021, RN025 e RN039.
