using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class AdministracaoAuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task System_admin_acessa_admin_sem_empresa_ativa_e_nao_acessa_area_tenant()
    {
        var web = new WebTestContext(factory);
        var admin = await web.CriarSystemAdminAsync();
        using var client = web.CriarCliente();

        var login = await web.LoginAsync(client, admin.Email, admin.Senha);
        Assert.Equal("/Admin", login.Headers.Location!.ToString());
        var pagina = await client.GetAsync("/Admin");
        var html = await pagina.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        Assert.Contains("Administração", html);
        Assert.DoesNotContain("Empresa ativa:", html);
        Assert.DoesNotContain("href=\"/Dashboard\"", html);
        Assert.DoesNotContain("href=\"/Insumos\"", html);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Insumos")).StatusCode);
        Assert.Equal("/Admin", (await client.GetAsync("/Empresas/Selecionar")).Headers.Location!.ToString());

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Empty(await context.UsuariosEmpresas.Where(v => v.UsuarioId == admin.Id).ToListAsync());
    }

    [Fact]
    public async Task Usuario_empresarial_nao_acessa_admin()
    {
        var web = new WebTestContext(factory);
        using var client = await web.CriarClienteAutenticadoAsync();
        var resposta = await client.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Conta/Login", resposta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Admin_anonimo_redireciona_para_login_e_return_url_tenant_nao_desvia_system_admin()
    {
        var web = new WebTestContext(factory);
        using (var anonimo = web.CriarCliente())
        {
            var resposta = await anonimo.GetAsync("/Admin");
            Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
            Assert.Contains("/Conta/Login", resposta.Headers.Location!.ToString());
        }

        var admin = await web.CriarSystemAdminAsync();
        using var client = web.CriarCliente();
        var login = await web.LoginAsync(client, admin.Email, admin.Senha, "/Produtos");
        Assert.Equal("/Admin", login.Headers.Location!.ToString());
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Admin");
        var logout = await client.PostAsync("/Conta/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/", logout.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin")).StatusCode);
    }
}
