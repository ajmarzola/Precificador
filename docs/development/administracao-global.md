# Administração global — UC039

O SystemAdmin acessa `/Admin` sem selecionar Empresa. O dashboard mostra solicitações Pendentes e quantidades de Empresas reais Ativas, Suspensas e Encerradas. Solicitações e Empresas possuem listas e detalhes próprios; a área não lê dados comerciais dos tenants.

## Solicitações

`/Admin/Solicitacoes` inicia em Pendente, da mais antiga para a mais recente. Aprovada, Recusada e Todas ordenam da mais recente para a mais antiga. Observação e decisão ficam no detalhe.

Para aprovar, confirme o Nome final. O responsável e o e-mail vêm da solicitação persistida. Nome já cadastrado bloqueia a operação, sem vincular o solicitante à Empresa existente. E-mail de SystemAdmin também é bloqueado.

A aprovação cria Empresa real, configuração padrão e primeiro vínculo Ativo/Administrador, reutilizando a identidade por e-mail ou criando-a sem senha. Registra data UTC e SystemAdmin autenticado. Toda a unidade de banco é transacional, incluindo Identity. A configuração padrão usa um contexto limitado à nova Empresa e compartilha a transação; não seleciona Empresa para o SystemAdmin nem desliga isolamento.

Após commit, usuário sem senha recebe ativação pela UC040. Usuário com senha recebe aviso para entrar com sua credencial existente. Falha de e-mail preserva a aprovação e mostra aviso. Recusa registra motivo interno opcional de até 500 caracteres; a mensagem ao solicitante não inclui esse motivo. Decisões são terminais.

## Empresas

`/Admin/Empresas` lista somente Empresas reais, por Nome normalizado, com situação e quantidade de Administradores ativos. O seed técnico não aparece e não recebe ações. Empresa 1 historicamente renomeada permanece real no upgrade.

Empresa sem Administrador mostra aviso e permite Definir administrador. A ação pode criar identidade sem senha, reutilizar identidade, promover vínculo Operacional ou reativar vínculo inativo. Não há promoção automática do legado.

Substituir administrador exige selecionar um Admin ativo e outro e-mail. Promove/cria/reativa o novo vínculo antes de demover somente o selecionado para Operacional, mantendo-o ativo. Outros Admins, credenciais e vínculos de outras Empresas permanecem. Empresa Suspensa permite essa correção; Encerrada rejeita ações.

Suspender preserva dados e vínculos e bloqueia acesso operacional, inclusive sessões antigas pela policy. Reativar exige Administrador ativo. Encerrar exige confirmação explícita em POST, grava data UTC e preserva dados; não permite reabrir.

Reenviar ativação usa a UC040 para o vínculo Ativo/Administrador sem senha. Não duplica identidade/vínculo e trata Enviado, NaoNecessario, Indisponivel e Falhou.

## Persistência e validação

Aplicar `UC039_AdministracaoEmpresas` explicitamente fora de Development. A migration preserva histórico e key ring. O backfill técnico exige simultaneamente Id 1 e NomeNormalizado `EMPRESA INICIAL`. FKs de decisão usam Restrict; checks impedem decisão inconsistente e Empresa ativa encerrada. E-mail normalizado não nulo possui índice único Identity.

As transações de escrita UC039 usam Serializable e um lock SQL transacional `Precificador.UC039`, compartilhado entre instâncias. Somente a unidade curta de banco fica serializada; SMTP não segura esse lock. Conflitos esperados de criação Identity são repetidos com nova consulta em até três tentativas; nome de Empresa duplicado continua bloqueando aprovação.

Testes de domínio, SQL Server e HTTP cobrem clean DB, upgrade pós-UC040, seed intacto/renomeado, constraints, rollback, as três corridas concorrentes, antiforgery, separação de autoridade e lifecycle. A gestão cotidiana de usuários permanece na UC031, Planejado.
