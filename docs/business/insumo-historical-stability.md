# Estabilidade cadastral do Insumo com histórico de preços

- **Status:** Aprovada
- **Data:** 2026-09-11
- **Regra normativa:** RN040 — Estabilidade cadastral do Insumo com histórico de preços
- **Casos afetados:** UC003, UC005 e futuramente UC014

## Contexto

O UC003 foi implementado antes da existência de preços de Insumo e permite editar Nome, Marca, Categoria, Unidade base e Observação.

O UC005 introduzirá histórico de preços. Cada registro de preço estará associado a um `InsumoId` e terá dados econômicos cuja interpretação depende dos dados cadastrais do Insumo.

Antes de criar esse histórico era necessário definir quais campos podem continuar mudando sem reescrever o significado de registros passados.

## Problema

Três campos têm impacto histórico diferente de Categoria e Observação:

### Nome

Nome participa, junto com Marca, da identidade funcional usada para reconhecer o Insumo.

Se um registro que possuía preços como **Farinha** for alterado posteriormente para **Açúcar**, o histórico antigo continuará ligado ao mesmo `InsumoId` e passará visualmente a parecer histórico de Açúcar.

### Marca

Marca representa variação comercial e já faz parte da identidade econômica definida pela RN029.

Permitir transformar um Insumo **Farinha + Renata** em **Farinha + Caputo** faria preços anteriores da Renata aparentarem pertencer à Caputo.

### Unidade base

A Unidade base atribui significado físico às quantidades utilizadas para calcular custo.

Exemplo:

```text
Unidade base = g
Quantidade compra = 1000
Preço compra = R$ 5,39
Custo unitário = 5,39 / 1000
```

Se o mesmo Insumo pudesse posteriormente mudar de `g` para `un`, o número histórico `1000` passaria a ser interpretado como mil unidades em vez de mil gramas. Isso altera a semântica do dado, não apenas sua apresentação.

## Decisão

Enquanto não houver nenhum registro de preço para o Insumo:

- Nome é editável;
- Marca é editável;
- Unidade base é editável;
- Categoria é editável;
- Observação é editável.

A partir do primeiro registro de preço, inclusive um registro com data futura:

- Nome torna-se imutável;
- Marca torna-se imutável;
- Unidade base torna-se imutável;
- Categoria continua editável;
- Observação continua editável.

A imutabilidade de Nome e Marca é integral, inclusive para alterações que manteriam o mesmo valor normalizado, como mudança apenas de capitalização ou espaçamento.

Uma mudança real de identidade ou unidade depois do início do histórico deve ser representada por **novo Insumo**. O Insumo antigo pode ser desativado, mas permanece associado ao seu histórico.

## Motivos da escolha

### 1. Preservar identidade econômica

Nome + Marca representam a identidade funcional/econômica do Insumo no modelo atual. O histórico deve continuar significando a mesma coisa ao longo do tempo.

### 2. Preservar a semântica matemática da unidade

Quantidade de compra e custo unitário só são interpretáveis corretamente se a unidade base usada na época continuar sendo a mesma.

### 3. Evitar reinterpretação retroativa

Um histórico confiável não deve mudar de significado porque o cadastro atual foi alterado depois.

### 4. Manter o modelo de preço simples no MVP

Com os campos históricos relevantes congelados, cada preço pode referenciar o `InsumoId` sem duplicar:

- Nome;
- Marca;
- Unidade base.

Isso reduz duplicação, risco de inconsistência e complexidade de consultas.

### 5. Usar o ciclo de vida já existente

O UC004 já permite desativar e reativar Insumos. Quando a identidade econômica realmente muda, o fluxo natural é:

```text
Insumo antigo + histórico
        ↓
    desativar

Novo Insumo correto
        ↓
novo histórico
```

Isso preserva rastreabilidade sem exigir versionamento cadastral no MVP.

### 6. Preferir uma regra inequívoca

Foi considerado permitir pequenas correções que preservassem `NomeNormalizado` ou `MarcaNormalizada`, como apenas capitalização.

A opção foi rejeitada no MVP porque criaria uma exceção conceitual e de interface para benefício pequeno. A fronteira “não possui preço / já possui preço” é mais simples de entender, testar e proteger no servidor.

## Alternativas avaliadas

### Permitir edição livre e mostrar o cadastro atual no histórico

**Rejeitada.**

Faria registros passados mudarem de significado conforme alterações posteriores no Insumo.

### Salvar snapshots de Nome, Marca e Unidade em cada preço

**Não adotada no MVP.**

Preservaria a apresentação histórica, porém duplicaria dados e exigiria decidir em cada consulta se deve ser exibido o snapshot ou o cadastro atual.

Snapshots podem ser introduzidos futuramente se surgir requisito real de auditoria temporal de apresentação.

### Versionar o cadastro do Insumo

**Não adotada no MVP.**

Resolveria o problema de forma completa, mas introduziria uma entidade/versionamento adicional antes de existir necessidade comprovada.

### Permitir apenas mudanças que preservem os valores normalizados

**Rejeitada no MVP.**

Seria tecnicamente segura para identidade, mas aumentaria regras condicionais na tela e no servidor sem resolver correções semânticas reais. Optou-se pela imutabilidade integral após o primeiro preço.

## Consequências

### Para o UC005

O UC005 deve:

- permitir registrar preço apenas para Insumo da Empresa Ativa;
- tornar verificável se o Insumo possui pelo menos um preço;
- aplicar a RN040 ao fluxo de edição;
- proteger a regra no servidor, não apenas por controles HTML;
- considerar qualquer preço, inclusive futuro, como início do histórico.

### Para o UC003

O fluxo existente de edição deverá passar a apresentar Nome, Marca e Unidade base como não editáveis quando houver histórico de preço.

Categoria e Observação permanecem editáveis.

A validação de servidor deve rejeitar tentativa manipulada de alterar os campos congelados.

### Para o modelo de preço

No MVP não é necessário snapshotar Nome, Marca e Unidade base em cada preço apenas para preservar a interpretação histórica.

### Para dados existentes

No momento desta decisão ainda não existe entidade de preço em produção no sistema, portanto não há histórico legado a migrar ou reconciliar.

### Para o UC014 / Ficha Técnica

Esta decisão fecha apenas o gate relacionado a **histórico de preços**.

Antes do UC014, deve ser reavaliado o caso de um Insumo que ainda não possui preços, mas já esteja referenciado por uma Ficha Técnica. Como a quantidade do item também depende da Unidade base, referências de ficha podem exigir uma regra adicional de estabilidade mesmo sem histórico de preço.

## Princípio resultante

> Dados cadastrais que definem a identidade econômica ou a unidade de interpretação de um histórico tornam-se estáveis quando esse histórico começa.
