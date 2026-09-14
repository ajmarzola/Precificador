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
using Precificador.Web.Pages.Conta;
using Precificador.Web.Pages.Empresas;

namespace Precificador.Tests.Integration.Web;

public sealed class FluxosMultiempresaTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task Login_valido_com_um_vinculo_seleciona_empresa_automaticamente()
    {
        var usuario = await CriarUsuarioAsync(1);
        using var client = web.CriarCliente();
        var resposta = await LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.ToString());
        var pagina = await client.GetStringAsync("/");
        Assert.Contains("Empresa ativa: Empresa inicial", pagina);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Insumos/Novo")).StatusCode);
    }

    [Fact]
    public async Task CA06_Login_com_empresa_unica_define_timezone_da_empresa()
    {
        var empresaUtc = await CriarEmpresaAsync("Empresa login UTC", "UTC");
        var usuario = await CriarUsuarioAsync(empresaUtc);
        using var scope = factory.Services.CreateScope();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<UsuarioAplicacao>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var session = new SessaoEmMemoria();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            Session = session
        };
        signInManager.Context = httpContext;
        var empresaContext = new EmpresaContext(new HttpContextAccessor { HttpContext = httpContext });
        var pagina = new LoginModel(signInManager, userManager, db, empresaContext)
        {
            Input = new LoginModel.InputModel { Email = usuario.Email, Senha = usuario.Senha },
            PageContext = new PageContext { HttpContext = httpContext }
        };

        var resultado = await pagina.OnPostAsync();

        var redirect = Assert.IsType<LocalRedirectResult>(resultado);
        Assert.Equal("/", redirect.Url);
        Assert.Equal(empresaUtc, empresaContext.EmpresaId);
        Assert.Equal("Empresa login UTC", empresaContext.Nome);
        Assert.Equal("UTC", empresaContext.TimeZoneId);
        Assert.True(session.TryGetValue(EmpresaContext.ChaveTimeZoneSession, out _));
    }

    [Fact]
    public async Task Login_com_multiplos_vinculos_direciona_para_selecao()
    {
        var empresaDois = await CriarEmpresaAsync("Empresa múltipla");
        var usuario = await CriarUsuarioAsync(1, empresaDois);
        using var client = web.CriarCliente();
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
        using var client = web.CriarCliente();
        await LoginAsync(client, usuario.Email, usuario.Senha);
        var selecao = await client.GetAsync("/Empresas/Selecionar");
        var token = WebTestHtml.ExtrairTokenAntiforgery(await selecao.Content.ReadAsStringAsync());
        var negada = await client.PostAsync("/Empresas/Selecionar", Form(token, empresaNaoAutorizada));
        Assert.Equal(HttpStatusCode.OK, negada.StatusCode);
        Assert.Contains("Empresa indispon", await negada.Content.ReadAsStringAsync());
        selecao = await client.GetAsync("/Empresas/Selecionar");
        token = WebTestHtml.ExtrairTokenAntiforgery(await selecao.Content.ReadAsStringAsync());
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
        using var client = web.CriarCliente();
        await LoginAsync(client, usuario.Email, usuario.Senha);
        var inicio = await client.GetAsync("/");
        var logout = await client.PostAsync("/Conta/Logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(await inicio.Content.ReadAsStringAsync()) }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        var protegido = await client.GetAsync("/Insumos/Novo");
        Assert.Equal(HttpStatusCode.Redirect, protegido.StatusCode);
        Assert.Contains("/Conta/Login", protegido.Headers.Location!.ToString());
    }

    private Task<UsuarioTeste> CriarUsuarioAsync(params int[] empresas) => web.CriarUsuarioAsync(empresas);

    private Task<int> CriarEmpresaAsync(string nome) => web.CriarEmpresaAsync(nome);

    private Task<int> CriarEmpresaAsync(string nome, string timeZoneId) => web.CriarEmpresaAsync(nome, timeZoneId);

    private Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string senha) => web.LoginAsync(client, email, senha);

    private static FormUrlEncodedContent Form(string token, int empresaId) => new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["EmpresaId"] = empresaId.ToString() });
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
