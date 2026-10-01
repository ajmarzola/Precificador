using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Web.Email;

namespace Precificador.Web.Autenticacao;

public enum ResultadoEnvioConta { Enviado, NaoNecessario, Indisponivel, Falhou }

public sealed class ServicoConta(UserManager<UsuarioAplicacao> usuarios, IEmailSenderAplicacao sender,
    IOptions<OpcoesAplicacao> aplicacao, ILogger<ServicoConta> logger)
{
    public bool Disponivel => sender.Disponivel;

    public Task<ResultadoEnvioConta> EnviarAcessoLiberadoAsync(UsuarioAplicacao usuario, string empresa)
        => EnviarComunicacaoAsync(usuario.Email!, "Acesso liberado ao Precificador",
            $"Seu acesso à Empresa {empresa} foi liberado.\nEntre no Precificador usando sua credencial existente.\n{aplicacao.Value.UrlPublica.TrimEnd('/')}/Conta/Login");

    public Task<ResultadoEnvioConta> EnviarRecusaAsync(string email)
        => EnviarComunicacaoAsync(email, "Solicitação de acesso ao Precificador",
            "Sua solicitação de acesso ao Precificador foi analisada e não foi aprovada neste momento.");

    private async Task<ResultadoEnvioConta> EnviarComunicacaoAsync(string email, string assunto, string texto)
    {
        if (!Disponivel) return ResultadoEnvioConta.Indisponivel;
        try
        {
            await sender.EnviarAsync(new MensagemEmail(email, assunto, texto, $"<p>{HtmlEncoder.Default.Encode(texto)}</p>"));
            return ResultadoEnvioConta.Enviado;
        }
        catch (Exception)
        {
            logger.LogWarning("Envio de comunicação administrativa falhou.");
            return ResultadoEnvioConta.Falhou;
        }
    }

    public async Task<ResultadoEnvioConta> EnviarAtivacaoAsync(UsuarioAplicacao usuario)
    {
        if (await usuarios.HasPasswordAsync(usuario)) return ResultadoEnvioConta.NaoNecessario;
        if (!Disponivel || string.IsNullOrWhiteSpace(usuario.Email)) return ResultadoEnvioConta.Indisponivel;
        var token = await usuarios.GenerateUserTokenAsync(usuario, TokensConta.Ativacao, TokensConta.PurposeAtivacao);
        return await EnviarAsync(usuario, token, true);
    }

    public async Task<ResultadoEnvioConta> EnviarRecuperacaoAsync(UsuarioAplicacao usuario)
    {
        if (!await usuarios.HasPasswordAsync(usuario)) return await EnviarAtivacaoAsync(usuario);
        if (!Disponivel || string.IsNullOrWhiteSpace(usuario.Email)) return ResultadoEnvioConta.Indisponivel;
        return await EnviarAsync(usuario, await usuarios.GeneratePasswordResetTokenAsync(usuario), false);
    }

    private async Task<ResultadoEnvioConta> EnviarAsync(UsuarioAplicacao usuario, string token, bool ativacao)
    {
        var rota = ativacao ? "Ativar" : "RedefinirSenha";
        var link = QueryHelpers.AddQueryString(aplicacao.Value.UrlPublica.TrimEnd('/') + "/Conta/" + rota,
            new Dictionary<string, string?> { ["userId"] = usuario.Id, ["code"] = TokensConta.Codificar(token) });
        var assunto = ativacao ? "Ative seu acesso ao Precificador" : "Redefina sua senha do Precificador";
        var introducao = ativacao ? "Foi liberado acesso ao Precificador. Defina sua senha." : "Foi solicitada a recuperação de acesso. Defina sua nova senha.";
        var validade = ativacao ? "48 horas" : "1 hora";
        var final = $"Este link expira em {validade}. Se não esperava este e-mail, ignore-o.";
        try
        {
            await sender.EnviarAsync(new MensagemEmail(usuario.Email!, assunto, $"{introducao}\n{link}\n{final}",
                $"<p>{introducao}</p><p><a href=\"{HtmlEncoder.Default.Encode(link)}\">Definir senha</a></p><p>{final}</p>"));
            return ResultadoEnvioConta.Enviado;
        }
        catch (Exception)
        {
            // Não incluir a exceção: respostas SMTP podem conter destinatário ou corpo.
            logger.LogWarning("Envio de e-mail de acesso falhou.");
            return ResultadoEnvioConta.Falhou;
        }
    }
}
