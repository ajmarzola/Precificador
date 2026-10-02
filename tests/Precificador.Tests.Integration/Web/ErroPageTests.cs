using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

// Este host não migra nem usa banco: qualquer resolução do DbContext falha.
public sealed class ErroSemBancoFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<PrecificadorDbContext>();
            services.AddScoped<PrecificadorDbContext>(_ => throw new InvalidOperationException("Banco indisponível"));
            services.AddTransient<IStartupFilter, ErroTesteStartupFilter>();
        });
    }
}

internal sealed class ErroTesteStartupFilter : IStartupFilter
{
    public const string Detalhe = "SEGREDO-MEL025 SELECT * FROM Produtos; Server=privado;Password=secreta; C:\\dados\\banco usuario@privado.local EmpresaId=123";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        // Endpoints registrados somente no host de teste, dentro dos handlers reais.
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMethods("/__teste/excecao", ["GET", "POST"], (HttpContext context) => throw new InvalidOperationException(Detalhe));
            endpoints.MapGet("/__teste/asset.css", (HttpContext context) => throw new InvalidOperationException(Detalhe));
            endpoints.MapMethods("/__teste/status/{codigo:int}", ["GET", "POST"], (HttpContext context) =>
            {
                context.Response.StatusCode = int.Parse(context.Request.RouteValues["codigo"]!.ToString()!);
                return Task.CompletedTask;
            });
            endpoints.MapGet("/__teste/corpo", async context =>
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Resposta tratada");
            });
        });
    };
}

public sealed class ErroPageTests(ErroSemBancoFactory factory) : IClassFixture<ErroSemBancoFactory>
{
    private HttpClient Cliente(string accept = "text/html")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        return client;
    }

    [Fact]
    public async Task Rota_inexistente_retorna_404_generico_sem_scaffold()
    {
        using var client = Cliente();
        await Validar404(await client.GetAsync("/rota-inexistente"));
    }

    [Theory]
    [InlineData(400, "Solicitação inválida")]
    [InlineData(403, "Acesso não permitido")]
    [InlineData(404, "Página não encontrada")]
    [InlineData(405, "Operação não permitida")]
    [InlineData(500, "Não foi possível concluir sua solicitação")]
    [InlineData(503, "Serviço temporariamente indisponível")]
    [InlineData(418, "Não foi possível concluir a solicitação")]
    [InlineData(200, "Não foi possível concluir sua solicitação")]
    [InlineData(600, "Não foi possível concluir sua solicitação")]
    public async Task Pagina_direta_GET_POST_sem_token_preserva_status_e_cache(int codigo, string titulo)
    {
        using var client = Cliente();
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            using var request = new HttpRequestMessage(method, $"/Erro/{codigo}");
            var response = await client.SendAsync(request);
            Assert.Equal(codigo is >= 400 and <= 599 ? codigo : 500, (int)response.StatusCode);
            var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);
            Assert.Contains(titulo, html);
            Assert.Contains("href=\"/\">Voltar ao início", html);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Equal((int)response.StatusCode >= 500, html.Contains("Referência:"));
        }
    }

    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(405)]
    [InlineData(503)]
    public async Task Status_vazio_GET_POST_reexecuta_e_preserva_codigo(int codigo)
    {
        using var client = Cliente();
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            var response = await client.SendAsync(new HttpRequestMessage(method, $"/__teste/status/{codigo}"));
            Assert.Equal(codigo, (int)response.StatusCode);
            Assert.Contains("Voltar ao início", await WebTestHtml.LerHtmlDecodificadoAsync(response));
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Excecao_Production_retorna_500_com_referencia_sem_detalhes(string metodo)
    {
        using var client = Cliente();
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), "/__teste/excecao"));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);
        Assert.Contains("Não foi possível concluir sua solicitação", html);
        Assert.Matches("Referência:</strong> <code>[^<]+</code>", html);
        foreach (var segredo in new[] { "SEGREDO-MEL025", "SELECT", "Password", "C:\\dados", "usuario@privado.local", "EmpresaId", "InvalidOperationException", "Development Mode", "stack", "An error occurred" })
            Assert.DoesNotContain(segredo, html);
    }

    [Theory]
    [InlineData("/rota-inexistente", "application/json", 404)]
    [InlineData("/rota-inexistente", "text/html;q=0, application/json", 404)]
    [InlineData("/rota-inexistente", "*/*", 404)]
    [InlineData("/css/ausente.css", "text/html", 404)]
    [InlineData("/js/ausente.js", "text/html", 404)]
    [InlineData("/__teste/excecao", "application/json", 500)]
    [InlineData("/__teste/asset.css", "text/html", 500)]
    [InlineData("/__teste/status/503", "application/json", 503)]
    [InlineData("/Erro/500", "application/json", 500)]
    public async Task Conteudo_nao_HTML_nao_recebe_layout(string url, string accept, int status)
    {
        using var client = Cliente(accept);
        var response = await client.GetAsync(url);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.DoesNotContain("<html", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Corpo_proprio_nao_e_substituido()
    {
        using var client = Cliente();
        var response = await client.GetAsync("/__teste/corpo");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Resposta tratada", await response.Content.ReadAsStringAsync());
    }

    internal static async Task Validar404(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);
        Assert.Contains("Página não encontrada", html);
        Assert.Contains("A página ou o recurso solicitado não existe ou não está disponível.", html);
        Assert.Contains("href=\"/\">Voltar ao início", html);
        Assert.DoesNotContain("Development Mode", html);
        Assert.DoesNotContain("Referência:", html);
    }
}

public sealed class ErroRecursoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task Pagina_protegida_anonima_continua_redirect_para_login()
    {
        using var client = web.CriarCliente();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        var response = await client.GetAsync("/Produtos");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Recurso_inexistente_cross_tenant_e_POST_recebem_mesmo_404_sem_dados_externos()
    {
        var empresaA = await web.CriarEmpresaAsync();
        var empresaB = await web.CriarEmpresaAsync();
        const string nomeExterno = "Produto sigiloso da Empresa B";
        int idB;
        using (var scope = factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaB));
            var produto = Produto.Criar(empresaB, nomeExterno, 0.3m);
            db.Produtos.Add(produto);
            await db.SaveChangesAsync();
            idB = produto.Id;
        }
        using var client = await web.CriarClienteAutenticadoAsync(empresaA);
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        var inexistente = await client.GetAsync("/Produtos/Detalhes/2147483647");
        var externo = await client.GetAsync($"/Produtos/Detalhes/{idB}");
        await ErroPageTests.Validar404(inexistente);
        await ErroPageTests.Validar404(externo);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(externo);
        Assert.Equal(await WebTestHtml.LerHtmlDecodificadoAsync(inexistente), html);
        Assert.DoesNotContain(nomeExterno, html);
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Produtos/Novo");
        await ErroPageTests.Validar404(await client.PostAsync("/Produtos/Detalhes/2147483647?handler=Desativar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token })));
        var postExterno = await client.PostAsync($"/Produtos/Detalhes/{idB}?handler=Desativar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        await ErroPageTests.Validar404(postExterno);
        Assert.Equal(html, await WebTestHtml.LerHtmlDecodificadoAsync(postExterno));
        var semToken = await client.PostAsync($"/Produtos/Detalhes/{idB}?handler=Desativar", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, semToken.StatusCode);
        Assert.Contains("Solicitação inválida", await WebTestHtml.LerHtmlDecodificadoAsync(semToken));
        using var scopeFinal = factory.Services.CreateScope();
        var optionsFinal = scopeFinal.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var dbFinal = new PrecificadorDbContext(optionsFinal, new ContextoEmpresaTeste(empresaB));
        Assert.True((await dbFinal.Produtos.SingleAsync(p => p.Id == idB)).Ativo);
    }

    [Fact]
    public async Task Development_preserva_diagnostico_e_404_amigavel()
    {
        using var development = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, ErroTesteStartupFilter>());
        });
        using var client = development.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        var response = await client.GetAsync("/__teste/excecao");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);
        Assert.DoesNotContain("Não foi possível concluir sua solicitação", html);
        Assert.Contains("InvalidOperationException", html);
        await ErroPageTests.Validar404(await client.GetAsync("/rota-inexistente"));
    }
}
