# UC002 — Listar e consultar insumos

- **Status:** Especificado — aguarda FT002, UC001A e UC001B
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** FT002, UC001A e UC001B implementados

## Objetivo

Permitir que usuário autenticado visualize, pesquise e consulte o catálogo **somente da Empresa Ativa**.

## Listagem

Rota `/Insumos`.

Listar ativos e inativos da Empresa Ativa com Nome, Marca, Categoria, Unidade base, Situação e ação Consultar.

Observação permanece apenas nos detalhes.

Ordenar por NomeNormalizado, MarcaNormalizada e Id para desempate determinístico.

## Pesquisa

Query string `q`, parcial por Nome ou Marca, ignorando caixa/espaços acidentais e preservando acentos.

A consulta jamais deve usar `IgnoreQueryFilters` para montar a listagem.

## Detalhes

`/Insumos/Detalhes/{id:int}` exibe Nome, Marca, Categoria, Unidade, Situação e Observação.

Se o ID não existir **ou pertencer a outra Empresa**, responder 404. Não revelar existência cross-tenant com 403 ou mensagem diferenciada.

## Segurança

- exige autenticação e Empresa Ativa;
- PageModel não aceita EmpresaId de query/form;
- Global Query Filter da FT002 é a proteção padrão;
- `AsNoTracking` em leitura;
- nenhuma migration esperada.

## Critérios essenciais

- usuário da Empresa A vê somente Insumos A;
- pesquisa nunca retorna Insumo B;
- detalhes de ID da Empresa B retorna 404;
- troca autorizada para Empresa B altera o catálogo visível;
- ativos/inativos da própria empresa permanecem visíveis;
- mesma Nome/Marca em A e B não produz duplicidade visual no tenant atual;
- nenhuma escrita ocorre.

## Testes

Usar pelo menos duas empresas e validar listagem, busca e detalhes cross-tenant, além dos cenários funcionais já previstos de estado vazio, ordenação e 404.

## Fora do escopo

Edição, desativação, preço, histórico, paginação/filtros avançados, administração de empresa/usuário, Produto/Ficha Técnica.

## Persistência

Nenhuma migration. Se surgir mudança de schema, interromper e revisar o escopo.
