using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Precificador.Tests.Integration.Web;

public sealed class AutenticacaoPagesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Rota_de_negocio_anônima_redireciona_para_login()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resposta = await client.GetAsync("/Insumos/Novo");
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Conta/Login", resposta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_invalido_exibe_erro_funcional()
    {
        using var client = factory.CreateClient();
        var resposta = await client.GetAsync("/Conta/Login");
        var pagina = await resposta.Content.ReadAsStringAsync();
        var token = Token(pagina);
        var post = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["Input.Email"] = "inexistente@teste.local", ["Input.Senha"] = "SenhaTeste1"
        }));
        var conteudo = await post.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        Assert.Contains("E-mail ou senha inválidos.", WebUtility.HtmlDecode(conteudo));
    }

    private static string Token(string pagina) => WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
}
