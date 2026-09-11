using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class ListarConsultarInsumosPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Area_de_insumos_exige_autenticacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var lista = await client.GetAsync("/Insumos");
        var detalhes = await client.GetAsync("/Insumos/Detalhes/1");
        Assert.Equal(HttpStatusCode.Redirect, lista.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, detalhes.StatusCode);
    }

    [Fact]
    public async Task Listagem_pesquisa_e_detalhes_respeitam_empresa_ativa()
    {
        var empresaDois = await CriarEmpresaAsync();
        var nomeFarinha = $"Farinha X {Guid.NewGuid():N}";
        var id = await CriarInsumoAsync(1, nomeFarinha, "Renata", "W 300", true);
        await CriarInsumoAsync(1, nomeFarinha, "Caputo", null, true);
        await CriarInsumoAsync(1, $"Copo {Guid.NewGuid():N}", null, null, false);
        var idOutroTenant = await CriarInsumoAsync(empresaDois, $"Segredo {Guid.NewGuid():N}", "Outra", null, true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var lista = await client.GetStringAsync("/Insumos");
        Assert.Equal(2, Regex.Matches(lista, Regex.Escape(nomeFarinha)).Count);
        Assert.Contains("Renata", lista);
        Assert.Contains("Caputo", lista);
        Assert.Contains("Inativo", lista);
        Assert.DoesNotContain("Segredo", lista);
        Assert.Contains("Cadastrar insumo", lista);
        Assert.Contains($"/Insumos/Detalhes/{id}", lista);

        var buscaNome = await client.GetStringAsync($"/Insumos?q=  farinha%20%20");
        Assert.Contains("Renata", buscaNome);
        Assert.Contains("Caputo", buscaNome);
        var buscaMarca = await client.GetStringAsync("/Insumos?q=rEnAtA");
        Assert.Contains("Renata", buscaMarca);
        Assert.DoesNotContain("Caputo", buscaMarca);
        var semResultado = await client.GetStringAsync("/Insumos?q=naoexiste");
        Assert.Contains("Nenhum insumo encontrado para a pesquisa.", semResultado);

        var detalhes = await client.GetAsync($"/Insumos/Detalhes/{id}");
        var conteudo = WebUtility.HtmlDecode(await LerComoUtf8Async(detalhes));
        detalhes.EnsureSuccessStatusCode();
        Assert.Contains("Nome", conteudo);
        Assert.Contains(nomeFarinha, conteudo);
        Assert.Contains("Marca", conteudo);
        Assert.Contains("Renata", conteudo);
        Assert.Contains("Categoria", conteudo);
        Assert.Contains("Matéria-prima", conteudo);
        Assert.Contains("Unidade base", conteudo);
        Assert.Contains("g", conteudo);
        Assert.Contains("Situação", conteudo);
        Assert.Contains("Ativo", conteudo);
        Assert.Contains("Observação", conteudo);
        Assert.Contains("W 300", conteudo);
        Assert.Contains("Voltar para insumos", conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("NomeNormalizado", conteudo);
        Assert.DoesNotContain("MarcaNormalizada", conteudo);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Insumos/Detalhes/{idOutroTenant}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Insumos/Detalhes/999999")).StatusCode);
    }

    [Fact]
    public async Task Empresa_ativa_sem_insumos_exibe_estado_vazio()
    {
        var empresaSemInsumos = await CriarEmpresaAsync();
        using var client = await CriarClienteAutenticadoAsync(empresaSemInsumos);
        var pagina = await client.GetStringAsync("/Insumos");
        Assert.Contains("insumos cadastrados para a empresa ativa", pagina);
        Assert.Contains("Cadastrar insumo", pagina);
    }

    private static async Task<string> LerComoUtf8Async(HttpResponseMessage response) => Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());

    private async Task<int> CriarEmpresaAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var empresa = Empresa.Criar($"Empresa {Guid.NewGuid():N}");
        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();
        return empresa.Id;
    }

    private async Task<int> CriarInsumoAsync(int empresaId, string nome, string? marca, string? observacao, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var opcoes = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(opcoes, new ContextoEmpresaTeste(empresaId));
        var insumo = Insumo.Criar(empresaId, nome, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, marca, observacao);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        if (!ativo)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Insumos SET Ativo = 0 WHERE Id = {insumo.Id}");
        }
        return insumo.Id;
    }

    private async Task<HttpClient> CriarClienteAutenticadoAsync(int empresaId)
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var usuario = new UsuarioAplicacao { UserName = email, Email = email };
            Assert.True((await users.CreateAsync(usuario, senha)).Succeeded);
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaId, Ativo = true });
            await context.SaveChangesAsync();
        }
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var login = await client.GetAsync("/Conta/Login");
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(await login.Content.ReadAsStringAsync()),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        return client;
    }

    private static string Token(string pagina) => WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
    }
}
