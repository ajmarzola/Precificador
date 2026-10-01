using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class SetupPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Setup_cria_primeiro_system_admin_sem_vinculo_e_sem_alterar_empresa_e_so_pode_ocorrer_uma_vez()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pagina = await client.GetAsync("/Setup");
        var token = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync());
        var resposta = await client.PostAsync("/Setup", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.ChaveConfiguracao"] = "chave-bootstrap-teste",
            ["Input.Email"] = "admin@teste.local",
            ["Input.Senha"] = "SenhaTeste1",
            ["Input.ConfirmacaoSenha"] = "SenhaTeste1"
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Conta/Login", resposta.Headers.Location!.ToString());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            Assert.Equal(1, await db.Users.CountAsync());
            Assert.Equal("Empresa inicial", (await db.Empresas.SingleAsync(empresa => empresa.Id == 1)).Nome);
            Assert.Empty(await db.UsuariosEmpresas.ToListAsync());
            var usuario = await db.Users.SingleAsync();
            Assert.Contains(await db.UserRoles.Join(db.Roles, membership => membership.RoleId, role => role.Id, (_, role) => role.Name).ToListAsync(), nome => nome == "SystemAdmin");
        }
        var repetido = await client.GetAsync("/Setup");
        Assert.Equal(HttpStatusCode.NotFound, repetido.StatusCode);
    }

    [Fact]
    public async Task Setup_chave_invalida_ou_post_sem_antiforgery_nao_persistem_efeitos()
    {
        using var factoryIsolada = new CustomWebApplicationFactory();
        using var client = factoryIsolada.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pagina = await client.GetAsync("/Setup");
        var token = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync());
        var invalida = await client.PostAsync("/Setup", Formulario(token, "chave-invalida", "novo@teste.local"));
        Assert.Equal(HttpStatusCode.OK, invalida.StatusCode);
        using (var scope = factoryIsolada.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            Assert.Empty(await db.Users.ToListAsync());
            Assert.Empty(await db.Roles.ToListAsync());
        }

        var semAntiforgery = await client.PostAsync("/Setup", Formulario(null, "chave-bootstrap-teste", "outro@teste.local"));
        Assert.Equal(HttpStatusCode.BadRequest, semAntiforgery.StatusCode);
    }

    [Fact]
    public async Task Usuario_empresarial_existente_nao_bloqueia_setup_nem_e_elevado_por_email()
    {
        using var factoryIsolada = new CustomWebApplicationFactory();
        string email;
        using (var scope = factoryIsolada.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            email = "empresarial@teste.local";
            Assert.True((await users.CreateAsync(new UsuarioAplicacao { UserName = email, Email = email }, "SenhaTeste1")).Succeeded);
        }
        using var client = factoryIsolada.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Setup")).StatusCode);
        var token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync("/Setup"));
        var repetido = await client.PostAsync("/Setup", Formulario(token, "chave-bootstrap-teste", email));
        Assert.Equal(HttpStatusCode.OK, repetido.StatusCode);
        using var verificacao = factoryIsolada.Services.CreateScope();
        var db = verificacao.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Empty(await db.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task Setup_sem_chave_esta_indisponivel_e_bootstrap_concorrente_cria_no_maximo_um_admin()
    {
        using (var semChave = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                   configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Bootstrap:SystemAdminKey"] = string.Empty }))))
        using (var client = semChave.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/Setup")).StatusCode);

        using var factoryIsolada = new CustomWebApplicationFactory();
        using var clienteUm = factoryIsolada.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var clienteDois = factoryIsolada.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokenUm = WebTestHtml.ExtrairTokenAntiforgery(await clienteUm.GetStringAsync("/Setup"));
        var tokenDois = WebTestHtml.ExtrairTokenAntiforgery(await clienteDois.GetStringAsync("/Setup"));
        var respostas = await Task.WhenAll(
            clienteUm.PostAsync("/Setup", Formulario(tokenUm, "chave-bootstrap-teste", "primeiro@teste.local")),
            clienteDois.PostAsync("/Setup", Formulario(tokenDois, "chave-bootstrap-teste", "segundo@teste.local")));
        Assert.Single(respostas, resposta => resposta.StatusCode == HttpStatusCode.Redirect);
        using var scope = factoryIsolada.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Single(await db.UserRoles.ToListAsync());
    }

    private static FormUrlEncodedContent Formulario(string? token, string chave, string email)
    {
        var valores = new Dictionary<string, string>
        {
            ["Input.ChaveConfiguracao"] = chave,
            ["Input.Email"] = email,
            ["Input.Senha"] = "SenhaTeste1",
            ["Input.ConfirmacaoSenha"] = "SenhaTeste1"
        };
        if (token is not null) valores["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(valores);
    }
}
