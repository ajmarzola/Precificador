# MEL018 — Redirecionar cadastros para os detalhes da entidade

- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Classificação:** UX / fluxo de trabalho
- **Prioridade:** média
- **Dependência:** MEL015, MEL016, UC001, UC007
- **Estado:** Planejado

## Problema

Após cadastrar com sucesso:

- um Insumo, o fluxo retorna à própria tela de cadastro;
- um Produto, o fluxo retorna à própria tela de cadastro.

Isso interrompe o fluxo natural de complementação da entidade recém-criada.

## Objetivo

~~~text
/Insumos/Novo
    -> /Insumos/Detalhes/{id}

/Produtos/Novo
    -> /Produtos/Detalhes/{id}
~~~

## Justificativa

Na tela de detalhes o usuário pode continuar imediatamente o processo.

### Insumo

- conferir cadastro;
- registrar preço;
- consultar preço vigente/histórico quando aplicável.

### Produto

- conferir cadastro;
- acessar Ficha Técnica;
- acompanhar precificação;
- registrar preço de prateleira quando disponível.

## Regras

- redirecionar somente após persistência bem-sucedida;
- preservar mensagem de sucesso via TempData;
- em validação/erro, permanecer no formulário;
- não alterar autorização/tenant isolation.

## Testes esperados

- POST válido de Insumo retorna redirect para Detalhes do novo Id;
- POST válido de Produto retorna redirect para Detalhes do novo Id;
- mensagem de sucesso continua disponível;
- POST inválido permanece no formulário.
