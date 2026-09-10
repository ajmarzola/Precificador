using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;
using Precificador.Infrastructure.Autenticacao;
using Microsoft.AspNetCore.Identity;

namespace Precificador.Tests.Integration.Web;

public sealed class NovoInsumoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Get_novo_insumo_retorna_sucesso_e_exibe_campos()
    {
        using var client = await CriarClienteAutenticadoAsync();

        var response = await client.GetAsync("/Insumos/Novo");
        var conteudo = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Nome", conteudo);
        Assert.Contains("Categoria", conteudo);
        Assert.Contains("Unidade base", conteudo);
    }

    [Fact]
    public async Task Post_valido_persiste_redireciona_e_exibe_mensagem_de_sucesso()
    {
        using var client = await CriarClienteAutenticadoAsync();
        var nome = $"Farinha {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, "Ingrediente", "Grama");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            Assert.True(await context.Insumos.IgnoreQueryFilters().AnyAsync(insumo => insumo.Nome == nome));
        }

        var paginaAposRedirect = await client.GetAsync(response.Headers.Location!);
        var conteudo = await paginaAposRedirect.Content.ReadAsStringAsync();
        Assert.Contains("Insumo cadastrado com sucesso.", conteudo);
    }

    [Fact]
    public async Task Post_invalido_nao_persiste()
    {
        using var client = await CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, "   ", "Ingrediente", "Grama");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task Post_com_nome_duplicado_nao_persiste_e_exibe_mensagem_funcional()
    {
        var nome = $"Açúcar {Guid.NewGuid():N}";
        using var client = await CriarClienteAutenticadoAsync();
        var primeiroCadastro = await EnviarFormularioAsync(client, nome, "Ingrediente", "Grama");
        Assert.Equal(HttpStatusCode.Redirect, primeiroCadastro.StatusCode);
        var response = await EnviarFormularioAsync(client, $"  {nome.ToUpperInvariant()}  ", "Ingrediente", "Grama");
        var conteudo = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe um insumo cadastrado com esse nome.", WebUtility.HtmlDecode(conteudo));
        Assert.Equal(1, await ContarInsumosAsync(nome));
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(HttpClient client, string nome, string categoria, string unidadeBase)
    {
        var respostaPagina = await client.GetAsync("/Insumos/Novo");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var token = Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
            ["Input.Nome"] = nome,
            ["Input.Categoria"] = categoria,
            ["Input.UnidadeBase"] = unidadeBase
        };

        return await client.PostAsync("/Insumos/Novo", new FormUrlEncodedContent(dados));
    }

    private async Task<int> ContarInsumosAsync(string? nome = null)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return nome is null
            ? await context.Insumos.IgnoreQueryFilters().CountAsync()
            : await context.Insumos.IgnoreQueryFilters().CountAsync(insumo => insumo.Nome == nome);
    }

    private async Task<HttpClient> CriarClienteAutenticadoAsync()
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var usuario = new UsuarioAplicacao { UserName = email, Email = email };
            Assert.True((await userManager.CreateAsync(usuario, senha)).Succeeded);
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = 1, Ativo = true });
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var login = await client.GetAsync("/Conta/Login");
        var pagina = await login.Content.ReadAsStringAsync();
        var token = Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        return client;
    }
}
