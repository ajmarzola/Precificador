# Configuração de e-mail e acesso — UC040

O transporte usa SMTP via MailKit, separado do `ServicoConta`. Não há envio real nos testes automatizados. Sem configuração, `Email:Smtp:Enabled` assume `false`: a aplicação inicia normalmente e o POST de recuperação retorna 503 antes de consultar Identity.

## App Settings

No Azure, configurar os nomes abaixo em App Service Application Settings. Localmente, o caminho padrão documentado pela MEL013 é usar variáveis de ambiente. O projeto não possui `UserSecretsId` versionado; `user-secrets` só deve ser usado se essa capacidade for configurada explicitamente em trabalho separado. Não versionar credenciais.

| App Setting / variável | Finalidade |
|---|---|
| `Email__Smtp__Enabled` | Habilitar transporte (`true`/`false`) |
| `Email__Smtp__Host` | Servidor SMTP |
| `Email__Smtp__Port` | Porta de 1 a 65535 |
| `Email__Smtp__Security` | `StartTls`, `SslOnConnect` ou `None` |
| `Email__Smtp__UserName` | Usuário de autenticação, quando usado |
| `Email__Smtp__Password` | Segredo SMTP, junto com UserName |
| `Email__Smtp__FromAddress` | E-mail do remetente |
| `Email__Smtp__FromName` | Nome do remetente; padrão Precificador |
| `Aplicacao__UrlPublica` | URL absoluta canônica; HTTPS em Production |

As mesmas opções podem ser configuradas com `:` em configuração .NET. A barra final da URL é normalizada. Links nunca dependem do Host enviado pelo visitante. `None` é permitido somente em Development/Test; não existe fallback TLS inseguro nem alteração da validação de certificados. Configuração habilitada inválida impede o startup.

## Gmail no piloto

Uma conta Gmail dedicada pode usar `smtp.gmail.com`, porta `587`, `StartTls`, com UserName e FromAddress iguais ao endereço dessa conta. Password deve ser uma senha de app, nunca a senha normal Google. Domínio próprio não é necessário. Trocar o servidor/remetente altera apenas a configuração, sem código específico do Gmail.

## Banco e implantação

Aplicar `UC040_PersistirDataProtectionKeys` explicitamente antes de iniciar a nova versão fora de Development, pelo mecanismo de migrations existente. A identidade runtime precisa ler/inserir chaves em `DataProtectionKeys`; a aplicação não exige DDL em Production. As chaves são globais, sem EmpresaId ou filtro, e não contêm linhas de tokens. Preservar essa tabela entre reinicializações e manter ApplicationName `Precificador` para que links ainda válidos sobrevivam a cold start.

Ativação usa `ContaAtivacao` / `Precificador.AtivarConta` por 48 horas. Recuperação usa `ContaRecuperacaoSenha` por 1 hora. A alteração de credencial pelo Identity muda o security stamp e invalida os links anteriores. Nenhum fluxo autentica automaticamente.

O serviço interno `EnviarAtivacaoAsync` retorna `Enviado`, `NaoNecessario`, `Indisponivel` ou `Falhou`. Usuário com senha não recebe ativação. Falhas de transporte não alteram o usuário; na recuperação pública a resposta continua neutra. Logs de falha não incluem exceção SMTP, destinatário, token, URL, corpo ou credenciais.

A UC039 usa esse serviço após o commit da aprovação ou definição/substituição de Administrador. Usuário com senha recebe aviso de acesso liberado com link canônico `/Conta/Login`, sem reset. Recusa envia mensagem simples sem motivo interno. Falha ou indisponibilidade SMTP mantém a decisão e apresenta aviso administrativo; o detalhe da Empresa oferece reenvio para Administrador ativo sem senha. Ver [Administração global](administracao-global.md).

Não elevar o logging de requests/Identity/Data Protection a Debug/Trace nem habilitar sensitive data logging de EF em ambiente operacional: links e material criptográfico não devem aparecer em logs. A configuração versionada mantém `Microsoft.AspNetCore` em Warning.
