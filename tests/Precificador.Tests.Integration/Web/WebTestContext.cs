using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autorizacao;

namespace Precificador.Tests.Integration.Web;

internal sealed class WebTestContext(CustomWebApplicationFactory factory)
{
    public HttpClient CriarCliente(bool manterCookies = true) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = manterCookies
        });

    public Task<UsuarioTeste> CriarUsuarioAsync(params int[] empresaIds) => CriarUsuarioAsync(PerfilUsuarioEmpresa.Operacional, empresaIds);

    public async Task<UsuarioTeste> CriarUsuarioAsync(PerfilUsuarioEmpresa perfil, params int[] empresaIds)
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        await GarantirSystemAdminAsync(userManager, roleManager);
        var usuario = new UsuarioAplicacao { UserName = email, Email = email };

        Assert.True((await userManager.CreateAsync(usuario, senha)).Succeeded);
        foreach (var empresaId in empresaIds)
        {
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaId, Ativo = true, Perfil = perfil });
        }

        await context.SaveChangesAsync();
        return new UsuarioTeste(usuario.Id, email, senha);
    }

    private static async Task GarantirSystemAdminAsync(UserManager<UsuarioAplicacao> userManager, RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(NomesAutorizacao.SystemAdmin))
            Assert.True((await roleManager.CreateAsync(new IdentityRole(NomesAutorizacao.SystemAdmin))).Succeeded);

        if (await userManager.GetUsersInRoleAsync(NomesAutorizacao.SystemAdmin) is { Count: > 0 }) return;

        var email = $"system-admin-{Guid.NewGuid():N}@teste.local";
        var usuario = new UsuarioAplicacao { UserName = email, Email = email };
        Assert.True((await userManager.CreateAsync(usuario, "SenhaTeste1")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(usuario, NomesAutorizacao.SystemAdmin)).Succeeded);
    }

    public async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string senha)
    {
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Conta/Login");
        return await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
    }

    public async Task<UsuarioTeste> CriarSystemAdminAsync()
    {
        var email = $"system-admin-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(NomesAutorizacao.SystemAdmin))
            Assert.True((await roleManager.CreateAsync(new IdentityRole(NomesAutorizacao.SystemAdmin))).Succeeded);
        var usuario = new UsuarioAplicacao { UserName = email, Email = email };
        Assert.True((await userManager.CreateAsync(usuario, senha)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(usuario, NomesAutorizacao.SystemAdmin)).Succeeded);
        return new UsuarioTeste(usuario.Id, email, senha);
    }

    public async Task<HttpClient> CriarClienteAutenticadoAsync(int empresaId = 1)
    {
        var usuario = await CriarUsuarioAsync(PerfilUsuarioEmpresa.Operacional, empresaId);
        var client = CriarCliente();
        var response = await LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    public async Task<int> CriarEmpresaAsync(string? nome = null, string? timeZoneId = null)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var empresa = timeZoneId is null
            ? Empresa.Criar(nome ?? $"Empresa {Guid.NewGuid():N}")
            : Empresa.Criar(nome ?? $"Empresa {Guid.NewGuid():N}", timeZoneId);
        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();

        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var contextoEmpresa = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresa.Id));
        contextoEmpresa.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresa.Id));
        await contextoEmpresa.SaveChangesAsync();
        return empresa.Id;
    }
}

internal sealed record UsuarioTeste(string Id, string Email, string Senha);
