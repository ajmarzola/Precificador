using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Precificador.Tests.Integration.Web;

public sealed class AutenticacaoPagesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Home_anônima_exibe_somente_navegação_pública()
    {
        var web = new WebTestContext(factory);
        await web.CriarUsuarioAsync(1);
        using var client = web.CriarCliente();

        var resposta = await client.GetAsync("/");
        var html = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("href=\"/Conta/Login\"", html);
        Assert.DoesNotContain("href=\"/Insumos\"", html);
        Assert.DoesNotContain("href=\"/Produtos\"", html);
        Assert.DoesNotContain("href=\"/Configuracoes/Precificacao\"", html);
        Assert.DoesNotContain("href=\"/Produtos/Novo\"", html);
        Assert.DoesNotContain("Empresa ativa:", html);
        Assert.DoesNotContain("action=\"/Conta/Logout\"", html);
    }

    [Fact]
    public async Task Primeiro_uso_redireciona_login_para_setup_e_conclui_bootstrap()
    {
        using var factoryIsolada = new CustomWebApplicationFactory();
        using var client = factoryIsolada.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginInicial = await client.GetAsync("/Conta/Login");
        Assert.Equal(HttpStatusCode.Redirect, loginInicial.StatusCode);
        Assert.Equal("/Setup", loginInicial.Headers.Location!.ToString());

        var setup = await client.GetAsync("/Setup");
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var token = WebTestHtml.ExtrairTokenAntiforgery(await setup.Content.ReadAsStringAsync());
        var post = await client.PostAsync("/Setup", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NomeEmpresa"] = "Empresa inicial",
            ["Input.Email"] = "admin@teste.local",
            ["Input.Senha"] = "SenhaTeste1",
            ["Input.ConfirmacaoSenha"] = "SenhaTeste1"
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("/Conta/Login", post.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Setup")).StatusCode);

        var loginDepoisDoSetup = await client.GetAsync("/Conta/Login");
        var html = await loginDepoisDoSetup.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, loginDepoisDoSetup.StatusCode);
        Assert.Contains("<form", html);
    }

    [Fact]
    public async Task Rota_de_negocio_anônima_redireciona_para_login()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resposta = await client.GetAsync("/Insumos/Novo");
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Conta/Login", resposta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Navegação_e_home_autenticadas_são_preservadas_e_login_redireciona_para_início()
    {
        var web = new WebTestContext(factory);
        using var client = await web.CriarClienteAutenticadoAsync();

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("href=\"/Insumos\"", html);
        Assert.Contains("href=\"/Produtos\"", html);
        Assert.Contains("href=\"/Configuracoes/Precificacao\"", html);
        Assert.Contains("href=\"/Produtos/Novo\"", html);
        Assert.Contains("Empresa ativa:", html);
        Assert.Contains("action=\"/Conta/Logout\"", html);
        Assert.DoesNotContain("href=\"/Conta/Login\"", html);

        var login = await client.GetAsync("/Conta/Login");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_invalido_exibe_erro_funcional()
    {
        var web = new WebTestContext(factory);
        await web.CriarUsuarioAsync(1);
        using var client = web.CriarCliente();

        var resposta = await client.GetAsync("/Conta/Login");
        var pagina = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("<form", pagina);
        Assert.Contains("href=\"/Conta/Login\"", pagina);
        Assert.DoesNotContain("href=\"/Insumos\"", pagina);
        Assert.DoesNotContain("href=\"/Produtos\"", pagina);
        Assert.DoesNotContain("href=\"/Configuracoes/Precificacao\"", pagina);

        var token = WebTestHtml.ExtrairTokenAntiforgery(pagina);
        var post = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Email"] = "inexistente@teste.local",
            ["Input.Senha"] = "SenhaTeste1"
        }));

        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        Assert.Contains("E-mail ou senha inválidos.", WebUtility.HtmlDecode(await post.Content.ReadAsStringAsync()));
    }
}
