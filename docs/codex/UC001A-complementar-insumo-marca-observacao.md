# Instrução Codex — UC001A Marca e observação do insumo

> **NÃO EXECUTAR antes de FT002 estar implementada e mergeada.**

Implemente o UC001A somente após ler:

1. `AGENTS.md`;
2. `docs/use-cases/UC001A-complementar-insumo-marca-observacao.md`;
3. `docs/development/foundation-multiempresa-auth.md`;
4. `docs/architecture/architecture.md`;
5. `docs/business/business-rules.md`.

Branch:

```text
feat/uc001a-marca-observacao-insumo
```

## Objetivo

Adicionar Marca/MarcaNormalizada/Observacao ao Insumo já tenant-aware.

## Regras

- Marca opcional, max 80, normalização de espaços e caixa invariável;
- ausência: `Marca = null`, `MarcaNormalizada = ""`;
- Observação opcional max 1000, trim externo, whitespace-only -> null;
- preservar EmpresaId definido pela Empresa Ativa;
- não receber EmpresaId do formulário.

## Unicidade

Substituir índice da FT002:

```text
EmpresaId + NomeNormalizado
```

por:

```text
EmpresaId + NomeNormalizado + MarcaNormalizada
```

Validação web e banco devem respeitar a Empresa Ativa.

Mensagem funcional: `Já existe um insumo cadastrado com esse nome e marca.`

## Migration

Nova migration após FT002. Não editar migrations históricas. Preservar dados e EmpresaId existentes.

## Web

Atualizar `/Insumos/Novo` com Marca e Observação. Manter PRG. Não criar listagem.

## Testes

Cobrir regras originais de Marca/Observação e, obrigatoriamente:

- mesma Nome+Marca permitida em empresas diferentes;
- duplicidade rejeitada na mesma empresa;
- tenant isolation preservado;
- EmpresaId não é controlável pela request;
- cadastro sem Empresa Ativa não prossegue.

## Restrições

Não implementar UC001B, UC002+, CRUD de empresa/usuário, preços, Produto, Ficha Técnica ou mudanças de autenticação.

Ao concluir, marcar UC001A como Implementado e executar restore/build/test Release.
