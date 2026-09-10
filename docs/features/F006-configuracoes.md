# F006 — Configurações de Precificação

## Objetivo

Centralizar parâmetros compartilhados por vários produtos **da mesma Empresa**.

## Configurações mínimas

- valor da hora de trabalho;
- tarifa de energia em R$/kWh;
- margem padrão para novos produtos;
- incremento comercial de arredondamento.

Potência não é mais tratada como configuração global de um forno universal; pertencerá ao Equipamento quando esse domínio for implementado.

## Comportamento

Alterar configuração da Empresa A deve afetar somente cálculos atuais dependentes da Empresa A.

A margem padrão vale como valor inicial de novos produtos; depois de criado, cada produto mantém sua margem-alvo própria.

## Regras relacionadas

RN013, RN014, RN019, RN021, RN025 e RN039.
