using System.Net;
using System.Text.RegularExpressions;

namespace Precificador.Tests.Integration.Web;

public sealed class HomePageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Home_anonima_apresenta_produto_fluxo_e_formulario_com_estrutura_responsiva()
    {
        using var client = new WebTestContext(factory).CriarCliente();
        var response = await client.GetAsync("/");
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(Regex.Matches(html, @"<h1\b[^>]*>"));
        Assert.Matches(@"<h1\b[^>]*>Precificador</h1>", html);
        Assert.Contains("Precifique com clareza. Acompanhe sua margem.", html);
        Assert.Contains("A decisão do preço de prateleira continua sendo sua.", html);
        Assert.Matches(@"<a[^>]*href=""/Conta/Login""[^>]*>Entrar</a>", html);
        Assert.Matches(@"<a[^>]*href=""#solicitar-acesso""[^>]*>Solicitar acesso</a>", html);
        foreach (var classe in new[] { "d-grid", "gap-3", "d-sm-flex", "row-cols-1", "row-cols-md-2", "row-cols-lg-3", "row-cols-md-3" })
            Assert.Matches($@"class=""[^""]*\b{Regex.Escape(classe)}\b[^""]*""", html);
        Assert.Equal(6, Regex.Matches(html, @"<article\b[^>]*class=""home-feature-card""").Count);
        foreach (var titulo in new[] { "Insumos e histórico de preços", "Ficha Técnica e custos", "Formação de preço", "Margem e revisão", "Histórico e explicabilidade", "Catálogo e multiempresa" })
            Assert.Matches($@"<h3\b[^>]*>{Regex.Escape(titulo)}</h3>", html);
        Assert.Matches(@"<h2\b[^>]*>Como funciona</h2>", html);
        foreach (var passo in new[] { "Centralize os dados", "Calcule com dados vigentes", "Decida com contexto" })
            Assert.Matches($@"<h3\b[^>]*>{Regex.Escape(passo)}</h3>", html);
        Assert.Contains("<section id=\"solicitar-acesso\"", html);
        Assert.Contains("O envio não cria uma conta nem concede acesso imediato.", html);
        Assert.Matches(@"<form[^>]*method=""post""[^>]*action=""/\?handler=SolicitarAcesso""", html);
        Assert.NotEmpty(WebTestHtml.ExtrairTokenAntiforgery(html));
        foreach (var campo in new[] { "NomeEmpresa", "NomeResponsavel", "EmailResponsavel", "Observacao" })
        {
            Assert.Contains($"name=\"Input.{campo}\"", html);
            Assert.Contains($"for=\"Input_{campo}\"", html);
            Assert.Contains($"data-valmsg-for=\"Input.{campo}\"", html);
        }
        Assert.DoesNotContain("style=", html);
        foreach (var rota in new[] { "/Insumos", "/Produtos", "/Configuracoes/Precificacao", "/Produtos/Novo", "/Admin" })
            Assert.DoesNotContain($"href=\"{rota}\"", html);
        Assert.DoesNotContain("Empresa ativa:", html);
        Assert.DoesNotContain("action=\"/Conta/Logout\"", html);
        Assert.Single(Regex.Matches(html, @"<meta\b[^>]*name=""description"""));
        Assert.Contains("content=\"Centralize insumos, fichas técnicas, custos, preços e margens para acompanhar a precificação de produtos artesanais.\"", html);
        Assert.Contains("<title>Formação e acompanhamento de preços - Precificador</title>", html);
    }

    [Fact]
    public async Task Login_sem_description_nao_recebe_meta_vazia_ou_generica()
    {
        var web = new WebTestContext(factory);
        await web.CriarUsuarioAsync(1);
        using var client = web.CriarCliente();
        var response = await client.GetAsync("/Conta/Login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("name=\"description\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Home_do_system_admin_redireciona_para_administracao()
    {
        var web = new WebTestContext(factory);
        var usuario = await web.CriarSystemAdminAsync();
        using var client = web.CriarCliente();
        await web.LoginAsync(client, usuario.Email, usuario.Senha);
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Admin", response.Headers.Location?.OriginalString);
    }
}
