# MEL014 — Criar Manual do Usuário

- **Origem:** Review manual do MVP
- **Classificação:** Documentação de produto
- **Prioridade:** média
- **Dependência:** funcionalidades do MVP consolidadas
- **Estado:** Planejado

## Objetivo

Criar um manual voltado ao **usuário final**, separado da documentação técnica/de desenvolvimento, explicando passo a passo como executar os processos disponíveis no Precificador.

O manual deve privilegiar tarefas e decisões do usuário, não detalhes de implementação.

## Estrutura mínima

### Acesso

- primeiro acesso da instalação via Setup;
- Login;
- seleção de Empresa quando houver mais de um vínculo;
- Logout;
- comportamento esperado de Empresa Ativa.

### Configurações de precificação

- consultar configurações;
- alterar Valor da hora;
- alterar Tarifa de energia;
- definir Margem padrão;
- definir Incremento comercial;
- definir Reserva comercial;
- explicar, em linguagem de usuário, o efeito de cada configuração.

### Insumos

Passo a passo para:

- cadastrar;
- consultar/listar;
- editar;
- desativar/reativar;
- registrar preço;
- consultar histórico de preços;
- entender preço vigente, unidade base, marca e categoria.

### Produtos

Passo a passo para:

- cadastrar;
- consultar/listar;
- editar;
- desativar/reativar;
- definir Margem-alvo.

### Ficha Técnica

Passo a passo para:

- criar/alterar base da Ficha;
- definir Rendimento;
- definir Tempo ativo;
- adicionar/editar/remover Matéria-prima/Embalagem/Consumível;
- informar perda esperada;
- adicionar/editar/remover equipamento;
- interpretar custos exibidos.

### Precificação comercial

Passo a passo para:

- interpretar Custo do lote e Custo unitário;
- entender Preço teórico e Preço sugerido;
- registrar Preço de prateleira;
- consultar histórico de precificação;
- interpretar Margem atual e Situação;
- consultar o detalhamento completo da precificação;
- compreender estados `Incompleto`, `Abaixo da margem` e `Dentro da margem`.

### Dashboard e administração

Quando UC028–UC031 forem concluídas, incorporar:

- resumo de margens;
- filtros;
- produtos com precificação incompleta;
- administração de usuários e vínculos.

## Padrão de escrita

Cada processo deve conter, quando aplicável:

1. **Para que serve**;
2. **Pré-requisitos**;
3. **Caminho na interface**;
4. **Passo a passo**;
5. **Resultado esperado**;
6. **Mensagens/estados importantes**;
7. **Erros comuns e como corrigir**;
8. **Exemplo prático**.

## Formato

A especificação futura deve decidir o formato final, mantendo ao menos uma versão dentro do repositório para versionamento junto ao produto.

Pode posteriormente ser publicado também como PDF/site de ajuda, sem tornar formatos derivados a fonte normativa.

## Manutenção

O manual deve ser atualizado quando uma alteração funcional mudar o fluxo visível ao usuário.

O MEL013 continua sendo o guia de **execução técnica e desenvolvimento local**; MEL014 é documentação de **uso do produto**.
