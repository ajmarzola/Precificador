# MEL019 — Preencher Rendimento da Ficha Técnica com padrão 1

- **Origem:** Review MVP 2026-09-16 — teste autenticado
- **Classificação:** UX / default de formulário
- **Prioridade:** baixa
- **Dependência:** UC013
- **Estado:** Planejado

## Problema

Ao abrir uma Ficha Técnica ainda não cadastrada, o campo Rendimento inicia vazio.

Para a maioria dos primeiros cadastros, o valor inicial 1 reduz atrito e representa uma unidade/lote padrão neutro.

## Objetivo

Quando o Produto ainda não possuir Ficha Técnica:

~~~text
Rendimento = 1
~~~

como valor inicial do formulário.

## Regras

- o valor é apenas default de entrada;
- não criar Ficha automaticamente no GET;
- não persistir nada até o usuário salvar;
- o usuário pode substituir 1 por qualquer valor válido;
- Ficha já existente continua exibindo seu Rendimento persistido.

## Testes esperados

- Produto sem Ficha abre formulário com Rendimento 1;
- GET não cria registro;
- Produto com Ficha existente preserva seu valor;
- POST continua validando Rendimento > 0.
