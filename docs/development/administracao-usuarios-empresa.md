# Administração de usuários da Empresa

A área `/Usuarios` exige Administrador da Empresa Ativa. O menu Usuários aparece quando a policy `AdministradorEmpresa` autoriza o usuário. Operacionais mantêm acesso às páginas operacionais; SystemAdmin administra Empresas por `/Admin`.

A lista começa em Ativos e permite Inativos ou Todos. Exibe e-mail, perfil, situação do vínculo e credencial: Ativado quando há senha, Aguardando ativação quando não há. A ordem é vínculos ativos primeiro, depois e-mail normalizado. Filtro desconhecido retorna 400; detalhe sem vínculo local retorna 404, inclusive para identidade existente em outra Empresa.

Para adicionar, informe e-mail e perfil Operacional ou Administrador. A identidade existente é reutilizada sem alterar e-mail, senha, confirmação, security stamp, roles ou vínculos de outras Empresas. A identidade nova nasce sem senha. Vínculo inativo é reativado com o perfil escolhido; vínculo ativo recebe erro controlado e só muda de perfil pela ação específica. SystemAdmin não pode ser alvo de convite, reativação ou promoção.

No detalhe, vínculo ativo permite alterar perfil e desvincular. Desvincular define `Ativo=false`, preservando perfil e conta global. Reativar reutiliza o vínculo e preserva seu perfil. Reenviar ativação está disponível para vínculo ativo sem senha; o serviço trata envio, ativação não necessária, indisponibilidade e falha sem expor tokens.

O último Administrador ativo não pode ser demovido nem desvinculado. Quando há outro Administrador, a auto-demissão mantém a Empresa Ativa e redireciona ao Dashboard; o auto-desvínculo limpa imediatamente o contexto e redireciona à seleção de Empresa. A autoridade é reconsultada nas requisições seguintes.

`ServicoUsuariosEmpresa` serializa escritas por Empresa com transação Serializable e application lock SQL transacional. Revalida Empresa e perfil do ator após adquirir o lock. Corridas globais Identity repetem a tentativa somente para DuplicateEmail/DuplicateUserName ou violações dos índices EmailIndex/UserNameIndex da identidade. A unicidade no banco permanece a autoridade; erros não relacionados não são mascarados. Falha pré-commit reverte inclusive identidade recém-criada.

Adicionar e reativar comunicam após commit: usuário sem senha recebe ativação UC040; usuário com senha recebe aviso de acesso liberado. Falha/indisponibilidade SMTP preserva o vínculo e é informada na página. SMTP nunca ocorre dentro da transação ou lock.

Todos os POSTs usam antiforgery e InputModels restritos. EmpresaId vem exclusivamente da sessão validada no servidor. Não há tabela de convite, migration, edição de credencial global ou mudanças nas Coleções da UC033.

Os testes `UsuariosEmpresaPageTests` usam SQL Server real temporário, login e antiforgery reais, sender falso e interceptor para provocar corrida de e-mail e falha após persistência Identity. Os cenários concorrentes usam conexões/requests independentes; uma leitura por outra conexão e aquisição imediata do application lock durante o envio comprovam que SMTP ocorre após commit e liberação do lock.
