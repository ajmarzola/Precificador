# Instrução Codex — UC017: Consultar Ficha Técnica e composição

## Tarefa

Implementar integralmente a UC017 conforme `docs/use-cases/UC017-consultar-ficha-composicao.md`.

Branch obrigatória:

~~~text
feat/uc017-consultar-ficha-composicao
~~~

Não editar/commitar/push direto em `master`. Não fazer merge da própria implementação.

## Precondições

Antes de alterar arquivos:

1. atualizar `master`;
2. criar/trocar para `feat/uc017-consultar-ficha-composicao`;
3. confirmar branch != master;
4. ler `AGENTS.md`;
5. ler `docs/development/backlog.md` e confirmar UC017 = `Pronto`;
6. ler UC013, UC014, UC015, UC016 e F003;
7. inspecionar `FichaTecnica.cshtml/.cs` e testes atuais de Item/Ficha.

Se UC017 não estiver `Pronto`, não implementar.

## Decisão obrigatória

Não criar nova página/rota de detalhes.

Evoluir somente:

~~~text
/Produtos/FichaTecnica/{id:int}
~~~

A página continua sendo simultaneamente superfície de leitura e manutenção da base/composição.

## Composição completa

A tabela deve exibir:

~~~text
Insumo
Marca
Quantidade
Unidade
Observação contextual
Situação
Ações
~~~

Regras:

- Nome e Marca separados;
- Marca nula => `—`;
- Observação contextual nula => `—`;
- preservar leitura de quebras de linha da Observação contextual;
- Situação = Ativo/Inativo;
- manter Editar e Remover;
- manter ordenação NomeNormalizado, MarcaNormalizada.

## Projeção Web

Expandir `ItemFichaResumo` para carregar no mínimo:

~~~text
Id
InsumoId
Nome
Marca
Quantidade
Unidade
Observacao
InsumoAtivo
~~~

Pode continuar formatando Quantidade com `ItemFichaTecnicaFormulario`.

Não usar `IgnoreQueryFilters`.

## Base e Produto

Preservar comportamento atual:

- Nome/Categoria/Situação do Produto;
- Rendimento;
- TempoAtivoMinutos;
- Produto inativo consultável;
- Produto sem Ficha continua na superfície de criação da base;
- GET sem Ficha não cria Ficha;
- Adicionar insumo somente com Ficha persistida.

## Ficha vazia

Preservar:

~~~text
Nenhum insumo adicionado.
~~~

e botão `Adicionar insumo` quando a Ficha existe.

## Observações

Na composição mostrar apenas:

~~~text
ItemFichaTecnica.Observacao
~~~

Não usar `Insumo.Observacao` como fallback.

## Escopo proibido

Não adicionar:

- preço vigente;
- custo unitário;
- custo de Item/lote;
- total;
- margem;
- status de precificação;
- filtros/pesquisa/paginação;
- migration;
- mudanças em domínio;
- nova rota de consulta;
- API.

## POST inválido da base

Preservar a composição completa ao renderizar erro de Rendimento/Tempo ativo, incluindo:

- Marca;
- Observação contextual;
- Situação;
- Editar;
- Remover.

Não mutar Ficha persistida.

## Testes obrigatórios

Atender W1-W16 da especificação.

Pontos de atenção:

- atualizar teste antigo da UC015 que hoje afirma que Observação contextual não aparece;
- provar Marca e Observação em colunas distintas;
- provar `—` para ausências;
- provar que Observação global do Insumo não aparece como contextual;
- provar Insumo/Produto inativos;
- provar ordenação determinística;
- provar isolamento por Ficha/tenant;
- provar GET sem mutação;
- provar POST inválido preservando composição completa;
- provar ausência de Custo/Preço/Total/Margem.

Não remover/enfraquecer testes de UC013–UC016.

## Validação

Executar:

~~~text
dotnet tool restore
dotnet restore Precificador.slnx
dotnet build Precificador.slnx --configuration Release --no-restore
dotnet test Precificador.slnx --configuration Release --no-build
~~~

## Documentação pós-implementação

Na PR de implementação:

- manter UC017 coerente;
- atualizar F003 apenas se necessário;
- alterar UC017 de `Pronto` para `Concluído` somente em `docs/development/backlog.md`;
- não reintroduzir Status em documentos individuais.

## Retorno obrigatório

Informar:

1. branch;
2. arquivos alterados;
3. campos finais da composição;
4. comportamento de Ficha inexistente/vazia;
5. confirmação de que custos/preços não foram antecipados;
6. testes W1-W16;
7. build/test;
8. URL da PR.

Commit sugerido:

~~~text
feat: completa consulta da ficha tecnica
~~~
