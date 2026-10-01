using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Precificador.Web.Autenticacao;
using Precificador.Web.Email;

namespace Precificador.Tests.Integration.Web;

public sealed class ContaConfiguracaoTests
{
    [Theory]
    [InlineData("Production", SegurancaSmtp.None, false)]
    [InlineData("Development", SegurancaSmtp.None, true)]
    [InlineData("Test", SegurancaSmtp.None, true)]
    [InlineData("Production", SegurancaSmtp.StartTls, true)]
    [InlineData("Production", SegurancaSmtp.SslOnConnect, true)]
    public void Smtp_valida_ambiente_tls_e_campos(string ambiente, SegurancaSmtp security, bool valido)
    {
        var validator = new ValidacaoSmtp(new Ambiente(ambiente));
        var config = new OpcoesSmtp { Enabled = true, Host = "smtp.test", Port = 587, FromAddress = "sistema@teste.local", Security = security };
        Assert.Equal(valido, validator.Validate(null, config).Succeeded);
        config.Host = "";
        Assert.True(validator.Validate(null, config).Failed);
        config.Enabled = false;
        Assert.True(validator.Validate(null, config).Succeeded);
        Assert.False(new SmtpEmailSender(Options.Create(config)).Disponivel);
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("Port")]
    [InlineData("Security")]
    [InlineData("FromAddress")]
    [InlineData("UserName")]
    [InlineData("Password")]
    public void Smtp_incompleto_rejeitado(string campo)
    {
        var config = new OpcoesSmtp { Enabled = true, Host = "smtp.test", Port = 587, FromAddress = "sistema@teste.local" };
        switch (campo)
        {
            case "Host": config.Host = " "; break;
            case "Port": config.Port = 65536; break;
            case "Security": config.Security = (SegurancaSmtp)99; break;
            case "FromAddress": config.FromAddress = "invalido"; break;
            case "UserName": config.UserName = "usuario"; break;
            case "Password": config.Password = "segredo-teste"; break;
        }
        Assert.True(new ValidacaoSmtp(new Ambiente("Production")).Validate(null, config).Failed);
    }

    [Theory]
    [InlineData("Production", "http://publico.test", false)]
    [InlineData("Production", "https://publico.test/base/", true)]
    [InlineData("Development", "http://localhost:5000", true)]
    [InlineData("Production", "/relativa", false)]
    [InlineData("Production", "https://publico.test/?query=1", false)]
    public void Url_publica_absoluta_https_producao(string ambiente, string url, bool valido)
    {
        Assert.Equal(valido, new ValidacaoAplicacao(new Ambiente(ambiente), Options.Create(new OpcoesSmtp { Enabled = true }))
            .Validate(null, new OpcoesAplicacao { UrlPublica = url }).Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("%%%")]
    [InlineData("a")]
    [InlineData("_w")]
    public void Code_malformado_nao_lanca(string? code) => Assert.Null(TokensConta.Decodificar(code));

    [Fact]
    public void Code_roundtrip_e_validades_independentes()
    {
        const string token = "token+/=á";
        var code = TokensConta.Codificar(token);
        Assert.DoesNotContain("+", code);
        Assert.DoesNotContain("/", code);
        Assert.DoesNotContain("=", code);
        Assert.Equal(token, TokensConta.Decodificar(code));
        Assert.Equal(TimeSpan.FromHours(48), new OpcoesTokenAtivacao().TokenLifespan);
        Assert.Equal(TimeSpan.FromHours(1), new OpcoesTokenRecuperacao().TokenLifespan);
    }
    private sealed class Ambiente(string nome) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nome;
        public string ApplicationName { get; set; } = "Precificador";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
