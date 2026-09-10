using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class NovoInsumoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Get_novo_insumo_retorna_sucesso_e_exibe_campos()
    {
        using var client = factory.CreateClient();

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
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var nome = $"Farinha {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, "Ingrediente", "Grama");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            Assert.True(await context.Insumos.AnyAsync(insumo => insumo.Nome == nome));
        }

        var paginaAposRedirect = await client.GetAsync(response.Headers.Location!);
        var conteudo = await paginaAposRedirect.Content.ReadAsStringAsync();
        Assert.Contains("Insumo cadastrado com sucesso.", conteudo);
    }

    [Fact]
    public async Task Post_invalido_nao_persiste()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, "   ", "Ingrediente", "Grama");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task Post_com_nome_duplicado_nao_persiste_e_exibe_mensagem_funcional()
    {
        var nome = $"Açúcar {Guid.NewGuid():N}";
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            context.Insumos.Add(Insumo.Criar(1, nome, CategoriaInsumo.Ingrediente, UnidadeMedida.Grama));
            await context.SaveChangesAsync();
        }

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
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
            ? await context.Insumos.CountAsync()
            : await context.Insumos.CountAsync(insumo => insumo.Nome == nome);
    }
}
