# F006 — Configurações de Precificação

## Objetivo

Centralizar parâmetros compartilhados pelos produtos **da mesma Empresa**.

## Configurações mínimas previstas

- valor da hora de trabalho;
- tarifa de energia em R$/kWh;
- margem padrão para novos produtos;
- incremento comercial de arredondamento.

Potência não é configuração global de forno: pertence ao Equipamento quando esse domínio for implementado.

## Comportamento

Alterar configuração da Empresa A afeta somente cálculos atuais da Empresa A.

Margem padrão é valor inicial de novo produto; cada produto mantém sua própria margem-alvo depois de criado.
