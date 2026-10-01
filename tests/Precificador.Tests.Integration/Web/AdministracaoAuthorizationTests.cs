using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Core.Acessos;

namespace Precificador.Tests.Integration.Web;

public sealed class AdministracaoAuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData(PerfilUsuarioEmpresa.Operacional)]
    [InlineData(PerfilUsuarioEmpresa.Administrador)]
    public async Task Todas_rotas_UC039_negam_anonimo_e_perfis_empresariais(PerfilUsuarioEmpresa perfil)
    {
        var web = new WebTestContext(factory);
        var empresa = await web.CriarEmpresaAsync();
        int solicitacao;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var pedido = SolicitacaoAcessoEmpresa.Criar("Pedido " + Guid.NewGuid(), "Ana", "ana@teste.local", null, DateTimeOffset.UtcNow);
            db.SolicitacoesAcessoEmpresas.Add(pedido); await db.SaveChangesAsync(); solicitacao = pedido.Id;
        }
        var usuario = await web.CriarUsuarioAsync(perfil, empresa);
        using var tenant = web.CriarCliente(); await web.LoginAsync(tenant, usuario.Email, usuario.Senha);
        using var anonimo = web.CriarCliente();
        foreach (var url in new[] { "/Admin", "/Admin/Solicitacoes", $"/Admin/Solicitacoes/Detalhes/{solicitacao}", "/Admin/Empresas", $"/Admin/Empresas/Detalhes/{empresa}" })
        {
            foreach (var client in new[] { tenant, anonimo })
            {
                Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync(url)).StatusCode);
                var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, client == tenant ? "/Dashboard" : "/");
                Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(url + "?handler=Aprovar", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = token }))).StatusCode);
            }
        }
    }

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
