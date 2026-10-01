using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Precificador.Web.Email;

public enum SegurancaSmtp { StartTls, SslOnConnect, None }
public sealed class OpcoesSmtp
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public SegurancaSmtp Security { get; set; } = SegurancaSmtp.StartTls;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Precificador";
}
public sealed class OpcoesAplicacao
{
    public string UrlPublica { get; set; } = "";
}
public sealed class ValidacaoSmtp(IHostEnvironment ambiente) : IValidateOptions<OpcoesSmtp>
{
    public ValidateOptionsResult Validate(string? name, OpcoesSmtp options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;
        var valido = !string.IsNullOrWhiteSpace(options.Host) && options.Port is >= 1 and <= 65535
            && Enum.IsDefined(options.Security) && MailboxAddress.TryParse(options.FromAddress, out var remetente)
            && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(options.FromAddress)
            && (string.IsNullOrWhiteSpace(options.UserName) == string.IsNullOrWhiteSpace(options.Password))
            && (options.Security != SegurancaSmtp.None || ambiente.IsDevelopment() || ambiente.IsEnvironment("Test"));
        return valido ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail("Configuração SMTP inválida.");
    }
}
public sealed class ValidacaoAplicacao(IHostEnvironment ambiente, IOptions<OpcoesSmtp> smtp) : IValidateOptions<OpcoesAplicacao>
{
    public ValidateOptionsResult Validate(string? name, OpcoesAplicacao options)
    {
        if (!smtp.Value.Enabled && string.IsNullOrWhiteSpace(options.UrlPublica)) return ValidateOptionsResult.Success;
        var valido = Uri.TryCreate(options.UrlPublica, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
            && (!ambiente.IsProduction() || uri.Scheme == "https");
        return valido ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail("URL pública inválida.");
    }
}
public sealed record MensagemEmail(string Destinatario, string Assunto, string Texto, string Html);
public interface IEmailSenderAplicacao
{
    bool Disponivel { get; }
    Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default);
}
public sealed class SmtpEmailSender(IOptions<OpcoesSmtp> options) : IEmailSenderAplicacao
{
    public bool Disponivel => options.Value.Enabled;
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default)
    {
        if (!Disponivel) throw new InvalidOperationException("Transporte de e-mail indisponível.");
        var config = options.Value;
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
        email.To.Add(MailboxAddress.Parse(mensagem.Destinatario));
        email.Subject = mensagem.Assunto;
        email.Body = new BodyBuilder { TextBody = mensagem.Texto, HtmlBody = mensagem.Html }.ToMessageBody();
        using var client = new SmtpClient();
        var security = config.Security switch
        {
            SegurancaSmtp.StartTls => SecureSocketOptions.StartTls,
            SegurancaSmtp.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SegurancaSmtp.None => SecureSocketOptions.None,
            _ => throw new InvalidOperationException("Segurança SMTP inválida.")
        };
        await client.ConnectAsync(config.Host, config.Port, security, cancellationToken);
        if (!string.IsNullOrWhiteSpace(config.UserName))
            await client.AuthenticateAsync(config.UserName, config.Password, cancellationToken);
        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
