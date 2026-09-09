# F006 — Configurações de Precificação

## Objetivo

Centralizar parâmetros globais usados por vários produtos.

## Configurações mínimas

- valor da hora de trabalho;
- tarifa de energia em R$/kWh;
- potência do forno em kW;
- margem padrão para novos produtos;
- incremento comercial de arredondamento.

## Comportamento

Alterar uma configuração deve afetar os cálculos atuais dos produtos dependentes sem necessidade de editar cada produto.

A margem padrão vale como valor inicial de novos produtos; depois de criado, cada produto mantém sua margem-alvo própria.

## Regras relacionadas

RN013, RN014, RN019, RN021 e RN025.
