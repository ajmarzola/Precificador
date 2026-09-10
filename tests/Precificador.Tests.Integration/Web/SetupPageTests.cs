using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class SetupPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Setup_cria_primeiro_usuario_vinculo_e_empresa_e_so_pode_ocorrer_uma_vez()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pagina = await client.GetAsync("/Setup");
        var token = Token(await pagina.Content.ReadAsStringAsync());
        var resposta = await client.PostAsync("/Setup", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NomeEmpresa"] = "Minha confeitaria",
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
            Assert.Equal("Minha confeitaria", (await db.Empresas.SingleAsync(empresa => empresa.Id == 1)).Nome);
            Assert.Single(await db.UsuariosEmpresas.ToListAsync());
        }
        var repetido = await client.GetAsync("/Setup");
        Assert.Equal(HttpStatusCode.NotFound, repetido.StatusCode);
    }

    private static string Token(string pagina) => WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
}
