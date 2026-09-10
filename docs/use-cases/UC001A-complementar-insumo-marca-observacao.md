# UC001A — Complementar cadastro de insumo com marca e observação

- **Status:** Especificado — bloqueado pela FT002
- **Tipo:** ajuste do UC001
- **Funcionalidade:** F001 — Gestão de Insumos
- **Dependências:** UC001 e FT002 implementados
- **Próximo caso relacionado:** UC001B — Generalizar classificação e unidades

## Objetivo

Evoluir o Insumo tenant-aware para suportar Marca opcional, Observação técnica e identidade econômica por Nome + Marca **dentro da Empresa**.

## Modelo após o ajuste

```text
Insumo
- Id
- EmpresaId
- Nome
- NomeNormalizado
- Marca?
- MarcaNormalizada
- Observacao?
- Categoria
- UnidadeBase
- Ativo
```

As regras de Marca e Observação aprovadas anteriormente permanecem: Marca max 80, normalização de espaços/caixa; Observação max 1000, trim externo e whitespace-only -> null.

## Unicidade

A FT002 terá criado temporariamente:

```text
EmpresaId + NomeNormalizado
```

UC001A deve substituir por:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

Assim marcas distintas do mesmo item podem coexistir na mesma Empresa, e a mesma combinação pode existir em Empresas distintas.

## Interface

`/Insumos/Novo` continua usando a Empresa Ativa resolvida pelo servidor e adiciona Marca + Observação. `EmpresaId` nunca é input do usuário.

## Migration

Criar migration evolutiva após `AddMultiempresaIdentity`:

- adicionar Marca/MarcaNormalizada/Observacao;
- remover índice `(EmpresaId, NomeNormalizado)`;
- criar índice único `(EmpresaId, NomeNormalizado, MarcaNormalizada)`;
- preservar registros/EmpresaId existentes;
- não inferir marcas.

## Isolamento

Todos os fluxos e testes do UC001A devem respeitar o tenant filter/guard da FT002.

## Critérios adicionais multiempresa

- Empresa A e B podem possuir `Farinha / Renata` simultaneamente;
- dentro da Empresa A a mesma combinação é duplicada;
- tentativa de manipular EmpresaId pela request não altera ownership;
- cadastro é impossível sem Empresa Ativa válida.

## Testes

Além dos testes originais de Marca/Observação, incluir isolamento entre duas Empresas e unicidade composta tenant-aware.

## Fora do escopo

UC001B, UC002+, administração de empresas/usuários, preços, Produto/Ficha Técnica.

## Definition of Done

Marca/Observação implementadas, índice composto inclui EmpresaId, migration preserva FT002 e testes cross-tenant permanecem verdes.
