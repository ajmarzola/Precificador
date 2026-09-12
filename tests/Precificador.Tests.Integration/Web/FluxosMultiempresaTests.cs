using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;
using Precificador.Web.Pages.Empresas;

namespace Precificador.Tests.Integration.Web;

public sealed class FluxosMultiempresaTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_valido_com_um_vinculo_seleciona_empresa_automaticamente()
    {
        var usuario = await CriarUsuarioAsync(1);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var resposta = await LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.ToString());
        var pagina = await client.GetStringAsync("/");
        Assert.Contains("Empresa ativa: Empresa inicial", pagina);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Insumos/Novo")).StatusCode);
    }

    [Fact]
    public async Task Login_com_multiplos_vinculos_direciona_para_selecao()
    {
        var empresaDois = await CriarEmpresaAsync("Empresa múltipla");
        var usuario = await CriarUsuarioAsync(1, empresaDois);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var resposta = await LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Empresas/Selecionar", resposta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Selecao_sem_vinculo_e_negada_e_troca_empresa_altera_contexto()
    {
        var empresaDois = await CriarEmpresaAsync("Empresa troca");
        var empresaNaoAutorizada = await CriarEmpresaAsync("Empresa bloqueada");
        var usuario = await CriarUsuarioAsync(1, empresaDois);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsync(client, usuario.Email, usuario.Senha);
        var selecao = await client.GetAsync("/Empresas/Selecionar");
        var token = Token(await selecao.Content.ReadAsStringAsync());
        var negada = await client.PostAsync("/Empresas/Selecionar", Form(token, empresaNaoAutorizada));
        Assert.Equal(HttpStatusCode.OK, negada.StatusCode);
        Assert.Contains("Empresa indispon", await negada.Content.ReadAsStringAsync());
        selecao = await client.GetAsync("/Empresas/Selecionar");
        token = Token(await selecao.Content.ReadAsStringAsync());
        var aceita = await client.PostAsync("/Empresas/Selecionar", Form(token, empresaDois));
        Assert.Equal(HttpStatusCode.Redirect, aceita.StatusCode);
        var inicio = await client.GetStringAsync("/");
        Assert.Contains("Empresa ativa: Empresa troca", inicio);
    }

    [Fact]
    public async Task CA07_Selecao_de_empresa_atualiza_timezone_ativo()
    {
        var empresaUtc = await CriarEmpresaAsync("Empresa UTC", "UTC");
        var usuario = await CriarUsuarioAsync(1, empresaUtc);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var session = new SessaoEmMemoria();
        var httpContext = new DefaultHttpContext
        {
            Session = session,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, usuario.Id)],
                "Teste"))
        };
        var empresaContext = new EmpresaContext(new HttpContextAccessor { HttpContext = httpContext });
        var pagina = new SelecionarModel(db, empresaContext)
        {
            EmpresaId = empresaUtc,
            PageContext = new PageContext { HttpContext = httpContext }
        };

        var resultado = await pagina.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal(empresaUtc, empresaContext.EmpresaId);
        Assert.Equal("Empresa UTC", empresaContext.Nome);
        Assert.Equal("UTC", empresaContext.TimeZoneId);
        Assert.True(session.TryGetValue(EmpresaContext.ChaveTimeZoneSession, out _));
    }

    [Fact]
    public async Task Logout_limpa_sessao_e_impede_acesso_operacional()
    {
        var usuario = await CriarUsuarioAsync(1);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsync(client, usuario.Email, usuario.Senha);
        var inicio = await client.GetAsync("/");
        var logout = await client.PostAsync("/Conta/Logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(await inicio.Content.ReadAsStringAsync()) }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        var protegido = await client.GetAsync("/Insumos/Novo");
        Assert.Equal(HttpStatusCode.Redirect, protegido.StatusCode);
        Assert.Contains("/Conta/Login", protegido.Headers.Location!.ToString());
    }

    private async Task<(string Id, string Email, string Senha)> CriarUsuarioAsync(params int[] empresas)
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var usuario = new UsuarioAplicacao { UserName = email, Email = email };
        Assert.True((await users.CreateAsync(usuario, senha)).Succeeded);
        foreach (var empresa in empresas) db.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresa, Ativo = true });
        await db.SaveChangesAsync();
        return (usuario.Id, email, senha);
    }

    private Task<int> CriarEmpresaAsync(string nome) => CriarEmpresaAsync(nome, Empresa.TimeZoneIdPadrao);

    private async Task<int> CriarEmpresaAsync(string nome, string timeZoneId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var empresa = Empresa.Criar(nome, timeZoneId);
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();
        return empresa.Id;
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string senha)
    {
        var pagina = await (await client.GetAsync("/Conta/Login")).Content.ReadAsStringAsync();
        return await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(pagina), ["Input.Email"] = email, ["Input.Senha"] = senha }));
    }

    private static FormUrlEncodedContent Form(string token, int empresaId) => new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["EmpresaId"] = empresaId.ToString() });
    private static string Token(string pagina) => WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

    private sealed class SessaoEmMemoria : ISession
    {
        private readonly Dictionary<string, byte[]> valores = [];

        public bool IsAvailable => true;
        public string Id => "sessao-em-memoria";
        public IEnumerable<string> Keys => valores.Keys;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Clear() => valores.Clear();
        public void Remove(string key) => valores.Remove(key);
        public void Set(string key, byte[] value) => valores[key] = value;
        public bool TryGetValue(string key, out byte[] value) => valores.TryGetValue(key, out value!);
    }
}
