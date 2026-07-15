# AGENTS.md

## Objetivo

Este repositório contém uma aplicação desenvolvida para fins pessoais. Implemente
soluções modernas, legíveis, testáveis e de fácil manutenção, aplicando de forma
pragmática os princípios KISS, Clean Code, SOLID e Clean Architecture.

Priorize a solução mais simples que atenda corretamente aos requisitos. Evite
abstrações prematuras, excesso de camadas, padrões desnecessários e complexidade
arquitetural sem benefício concreto. A arquitetura deve ser proporcional ao tamanho,
ao domínio e às necessidades reais do projeto.

## Autonomia de desenvolvimento

Trabalhe com autonomia para analisar, planejar, implementar, testar e validar as
tarefas solicitadas.

Quando houver pequenas ambiguidades, adote uma decisão técnica razoável, consistente
com o projeto, e registre a suposição no resumo final, em vez de interromper o trabalho
para solicitar confirmação.

Você pode, de forma independente:

- criar e organizar os arquivos necessários para a implementação;
- implementar funcionalidades completas dentro do escopo solicitado;
- realizar refatorações locais que melhorem clareza, testabilidade ou manutenção;
- criar e atualizar testes;
- corrigir erros diretamente relacionados à tarefa;
- executar builds, testes, lint, formatadores e validações locais;
- atualizar a documentação técnica relacionada à alteração;
- remover código temporário, duplicado ou comprovadamente não utilizado dentro do escopo.

Solicite aprovação antes de:

- alterar significativamente a arquitetura ou a estrutura geral da aplicação;
- substituir tecnologias ou frameworks existentes;
- adicionar dependências relevantes quando houver alternativa com os recursos atuais;
- modificar contratos públicos de APIs;
- realizar mudanças incompatíveis com versões anteriores;
- executar operações destrutivas ou irreversíveis;
- aplicar alterações em infraestrutura, ambientes externos ou bancos de dados reais;
- tomar decisões que dependam de regras de negócio não informadas e que possam mudar
  o comportamento esperado.

Ao concluir uma tarefa, informe as decisões tomadas, suposições adotadas, arquivos
alterados, validações executadas e eventuais limitações ou riscos restantes.

## Fluxo de trabalho

- Antes de editar, identifique a solução, os projetos envolvidos e o fluxo afetado.
- Mantenha as alterações restritas ao escopo solicitado, ainda que a implementação
  exija mudanças em múltiplos arquivos ou camadas.
- Para tarefas extensas, elabore um plano de execução antes de implementar. Só interrompa
  o trabalho para apresentar o plano ou solicitar confirmação quando houver decisão
  arquitetural relevante, risco elevado ou ambiguidade que possa alterar o comportamento esperado.
- Preserve compatibilidade retroativa, salvo quando a tarefa determinar o contrário
  ou quando a mudança incompatível tiver sido explicitamente aprovada.
- Não altere configurações de produção.
- Não modifique arquivos gerados automaticamente, salvo quando o fluxo oficial do projeto
  exigir sua regeneração.
- Não reformate arquivos não relacionados à tarefa.
- Corrija problemas adjacentes apenas quando forem diretamente necessários para concluir
  a tarefa ou quando a correção for pequena, segura e claramente relacionada.
- Ao concluir, apresente os arquivos modificados, decisões tomadas, suposições adotadas,
  validações executadas e riscos restantes.

## Segurança

- Nunca exponha secrets, connection strings, tokens, certificados ou chaves.
- Nunca coloque credenciais em código, logs, testes ou documentação.
- Nunca execute `terraform apply`, `terraform destroy`, comandos destrutivos do Azure
  ou operações de banco sem autorização explícita.
- Nunca execute `DELETE`, `UPDATE`, `DROP`, `TRUNCATE` ou migrations contra bancos reais.
- Não altere recursos Azure existentes apenas para corrigir um problema local.
- Não modifique pipelines de produção sem explicar o impacto.
- Não exponha variáveis de ambiente privadas ao bundle do navegador.
- Trate qualquer variável com prefixo público, como `NEXT_PUBLIC_`, como informação
  acessível pelo cliente.

## .NET e C#

- Respeite a versão de .NET e C# definida no projeto.
- Não atualize `TargetFramework` ou pacotes NuGet sem necessidade explícita.
- Preserve nullable reference types conforme a configuração existente.
- Prefira injeção de dependência e interfaces apenas quando houver benefício real.
- Propague `CancellationToken` em operações assíncronas quando aplicável.
- Não bloqueie código assíncrono usando `.Result` ou `.Wait()`.
- Evite capturar `Exception` sem tratamento ou contexto.
- Use logging estruturado, sem interpolar dados sensíveis.
- Preserve os padrões arquiteturais já adotados pelo projeto.

## ASP.NET

- Valide entradas nas fronteiras da aplicação.
- Não retorne entidades do banco diretamente pela API.
- Preserve códigos HTTP e contratos existentes.
- Considere autenticação, autorização e isolamento entre usuários.
- Evite lógica de negócio em controllers ou code-behind.

## Entity Framework e SQL Server

- Analise o SQL produzido por consultas LINQ sensíveis.
- Evite N+1, materialização antecipada e carregamento desnecessário.
- Não adicione `AsNoTracking` a fluxos que atualizam entidades.
- Não use funções sobre colunas indexadas sem considerar o plano de execução.
- Antes de sugerir índices, identifique consultas, cardinalidade e impacto de escrita.
- Scripts SQL devem ser idempotentes quando possível.
- Toda operação destrutiva deve possuir uma versão de inspeção ou `SELECT` equivalente.

## Angular e TypeScript

- Respeite a versão atual do Angular e as convenções existentes.
- Não introduza bibliotecas de estado ou UI sem necessidade técnica clara. Solicite aprovação quando a dependência for relevante ou alterar o padrão arquitetural do projeto.
- Preserve tipagem estrita.
- Evite `any`, subscriptions sem descarte e lógica excessiva em componentes.
- Prefira componentes pequenos, coesos e com responsabilidades claras.
- Evite efeitos colaterais em templates, pipes e getters executados frequentemente.
- Execute lint, testes e build após alterações relevantes.

## React e TypeScript

- Respeite a versão atual do React, TypeScript e as convenções já adotadas.
- Não migre componentes de classe para componentes funcionais sem necessidade.
- Não introduza bibliotecas de estado, formulários, UI ou data fetching sem necessidade técnica clara. Solicite aprovação quando a dependência for relevante ou alterar o padrão arquitetural do projeto.
- Preserve tipagem estrita e evite `any`, casts inseguros e tipos excessivamente genéricos.
- Prefira componentes funcionais, pequenos, coesos e com responsabilidades claras.
- Extraia lógica reutilizável para hooks apenas quando houver reutilização ou ganho real de clareza.
- Hooks devem ser chamados sempre no nível superior e nunca de forma condicional.
- Declare corretamente as dependências de `useEffect`, `useMemo` e `useCallback`.
- Não use `useEffect` para derivar estado que pode ser calculado durante a renderização.
- Evite sincronizar dois estados que representam a mesma informação.
- Não aplique `useMemo` ou `useCallback` por padrão; use-os apenas quando houver
  benefício mensurável ou necessidade referencial.
- Evite mutações diretas em props, state, objetos e arrays compartilhados.
- Use `key` estável e semântico em listas; não use índice quando a ordem puder mudar.
- Trate estados de carregamento, erro, vazio e sucesso explicitamente.
- Garanta acessibilidade básica: elementos semânticos, labels, foco, teclado e atributos ARIA
  quando realmente necessários.
- Não desative regras de lint sem explicar o motivo.
- Preserve o padrão existente de CSS, CSS Modules, styled-components, Tailwind ou outra solução.
- Crie ou atualize testes para mudanças de comportamento, especialmente em hooks,
  formulários, estados assíncronos e fluxos críticos.

## Next.js

- Respeite a versão do Next.js e verifique se o projeto utiliza App Router, Pages Router
  ou uma combinação dos dois.
- Não migre entre App Router e Pages Router sem autorização explícita.
- No App Router, prefira Server Components por padrão.
- Adicione `"use client"` apenas quando o componente realmente precisar de estado,
  efeitos, eventos do navegador ou APIs exclusivas do cliente.
- Mantenha a fronteira cliente-servidor o mais baixa e restrita possível.
- Não importe módulos exclusivos do servidor em Client Components.
- Não acesse banco de dados, filesystem, secrets ou serviços internos diretamente
  em código executado no navegador.
- Prefira buscar dados no servidor quando isso reduzir JavaScript no cliente e evitar
  exposição de credenciais.
- Respeite a estratégia existente de cache, revalidação e renderização.
- Não altere `revalidate`, `dynamic`, `fetchCache` ou políticas de cache sem analisar
  impacto funcional, custo e consistência dos dados.
- Diferencie corretamente conteúdo estático, SSR, ISR e renderização dinâmica.
- Em Route Handlers e API Routes, valide entrada, autenticação, autorização e limites
  antes de executar a regra de negócio.
- Não confie em dados vindos de `params`, `searchParams`, headers, cookies ou body.
- Preserve códigos HTTP, contratos de resposta e tratamento de erros.
- Use `redirect`, `notFound` e mecanismos equivalentes apenas nos contextos suportados.
- Utilize `next/link`, `next/image`, metadata e recursos nativos quando forem adequados,
  sem substituir componentes existentes sem necessidade.
- Defina dimensões ou proporção para imagens a fim de evitar layout shift.
- Não coloque secrets em `NEXT_PUBLIC_*`.
- Considere que variáveis `NEXT_PUBLIC_*` podem ser incorporadas ao bundle no build.
- Não leia variáveis de ambiente diretamente em componentes cliente, salvo as explicitamente públicas.
- Preserve middleware enxuto; evite consultas pesadas, acesso excessivo à rede e lógica
  de negócio complexa no middleware.
- Verifique compatibilidade com o runtime utilizado, como Node.js ou Edge Runtime.
- Não use APIs indisponíveis no Edge Runtime.
- Em Server Actions, valide novamente os dados e aplique autenticação e autorização;
  não trate a action como uma fronteira confiável.
- Após mutações, use a estratégia existente de invalidação, como `revalidatePath`,
  `revalidateTag` ou atualização local.
- Evite duplicar chamadas de dados entre `generateMetadata`, layouts e páginas quando
  o framework ou a aplicação já possuir estratégia de deduplicação.
- Preserve convenções de arquivos especiais, como `loading`, `error`, `not-found`,
  `layout`, `template` e `route`.
- Analise impacto em SEO, metadata, canonical, Open Graph e indexação quando alterar
  páginas públicas.
- Execute lint, testes e build de produção após alterações relevantes.

## Front-end: práticas gerais

- Respeite o gerenciador de pacotes e o arquivo de lock existentes.
- Não misture `npm`, `yarn`, `pnpm` ou `bun` no mesmo repositório.
- Não atualize dependências ou versões de Node sem necessidade técnica. Atualize o lockfile quando isso for consequência legítima de uma alteração de dependência aprovada ou necessária.
- Não instale pacotes para resolver problemas que possam ser tratados adequadamente com recursos já presentes. Dependências pequenas e claramente justificadas podem ser propostas, mas dependências relevantes exigem aprovação.
- Preserve aliases, estrutura de pastas e padrões de importação existentes.
- Evite imports circulares e dependências entre camadas inadequadas.
- Não faça chamadas HTTP diretamente em componentes quando o projeto já possuir
  uma camada de serviços, hooks ou clientes de API.
- Use tratamento consistente de erros, cancelamento e concorrência em chamadas assíncronas.
- Não armazene tokens sensíveis em `localStorage` sem que isso já faça parte da arquitetura
  e o risco tenha sido avaliado.
- Evite inserir HTML não confiável. O uso de `dangerouslySetInnerHTML` deve ser justificado
  e acompanhado de sanitização adequada.
- Preserve internacionalização, formatação de datas, moeda, fuso horário e locale existentes.
- Considere responsividade, acessibilidade e navegação por teclado em alterações visuais.
- Não altere contratos com APIs sem coordenar os impactos no back-end e nos consumidores.

## Terraform e Azure

- Execute `terraform fmt` e `terraform validate` quando disponíveis.
- `terraform plan` pode ser preparado, mas não aplique alterações automaticamente.
- Não altere state, backend, subscriptions, tenants ou resource groups sem autorização.
- Não recrie recursos para resolver diferenças simples de configuração.
- Trate alterações que forcem replacement como alto risco.
- Prefira Managed Identity e Key Vault a secrets em configuração.

## Testes e validação

Quando aplicável, execute:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes

npm run lint
npm test
npm run build

pnpm lint
pnpm test
pnpm build

yarn lint
yarn test
yarn build

terraform fmt -check
terraform validate
```

- Execute apenas os comandos compatíveis com o gerenciador e os scripts existentes.
- Crie ou atualize testes para mudanças de comportamento.
- Para React e Next.js, teste fluxos críticos, renderização condicional, formulários,
  autenticação, carregamento assíncrono e fronteiras cliente-servidor.
- Não declare sucesso se os testes não foram executados.
- Quando uma validação não puder ser executada, explique exatamente por quê.
- Não modifique testes apenas para fazê-los passar sem corrigir a causa real.
- Antes de concluir, revise o diff e remova logs, código temporário e comentários obsoletos.

## Critério de conclusão

Uma tarefa está concluída quando:

- o comportamento solicitado foi implementado;
- o escopo permaneceu restrito;
- build, testes e validações relevantes foram executados;
- o diff foi revisado;
- riscos, limitações e alterações de contrato foram informados;
- impactos em segurança, cache, SSR, SEO e infraestrutura foram considerados quando aplicáveis.