using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

internal sealed class WebTestContext(CustomWebApplicationFactory factory)
{
    public HttpClient CriarCliente(bool manterCookies = true) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = manterCookies
        });

    public async Task<UsuarioTeste> CriarUsuarioAsync(params int[] empresaIds)
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var usuario = new UsuarioAplicacao { UserName = email, Email = email };

        Assert.True((await userManager.CreateAsync(usuario, senha)).Succeeded);
        foreach (var empresaId in empresaIds)
        {
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaId, Ativo = true });
        }

        await context.SaveChangesAsync();
        return new UsuarioTeste(usuario.Id, email, senha);
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

    public async Task<HttpClient> CriarClienteAutenticadoAsync(int empresaId = 1)
    {
        var usuario = await CriarUsuarioAsync(empresaId);
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
        return empresa.Id;
    }
}

internal sealed record UsuarioTeste(string Id, string Email, string Senha);
