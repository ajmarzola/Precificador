using System.Net;
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

public sealed class SituacaoInsumoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CA01_Detalhes_de_ativo_exibe_desativar_e_nao_reativar()
    {
        var id = await CriarInsumoAsync(1, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var pagina = await client.GetStringAsync($"/Insumos/Detalhes/{id}");

        Assert.Contains("Desativar", pagina);
        Assert.DoesNotContain("Reativar", pagina);
        Assert.Contains("Deseja desativar este insumo?", pagina);
    }

    [Fact]
    public async Task CA02_Detalhes_de_inativo_exibe_reativar_e_nao_desativar()
    {
        var id = await CriarInsumoAsync(1, ativo: false);
        using var client = await CriarClienteAutenticadoAsync();

        var pagina = await client.GetStringAsync($"/Insumos/Detalhes/{id}");

        Assert.Contains("Reativar", pagina);
        Assert.DoesNotContain("Desativar", pagina);
    }

    [Fact]
    public async Task CA06_Post_desativar_persiste_status_redireciona_e_exibe_sucesso()
    {
        var id = await CriarInsumoAsync(1, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await PostComTokenAsync(client, id, "Desativar");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Insumos/Detalhes/{id}", response.Headers.Location!.OriginalString);
        Assert.False((await ObterInsumoAsync(id, 1)).Ativo);
        Assert.Contains("Insumo desativado com sucesso.", await client.GetStringAsync(response.Headers.Location));
    }

    [Fact]
    public async Task CA07_Post_reativar_persiste_status_redireciona_e_exibe_sucesso()
    {
        var id = await CriarInsumoAsync(1, ativo: false);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await PostComTokenAsync(client, id, "Reativar");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Insumos/Detalhes/{id}", response.Headers.Location!.OriginalString);
        Assert.True((await ObterInsumoAsync(id, 1)).Ativo);
        Assert.Contains("Insumo reativado com sucesso.", await client.GetStringAsync(response.Headers.Location));
    }

    [Fact]
    public async Task CA08_CA09_Inativo_permanece_na_listagem_e_pode_ser_editado()
    {
        var id = await CriarInsumoAsync(1, ativo: false);
        using var client = await CriarClienteAutenticadoAsync();

        var listagem = await client.GetStringAsync("/Insumos");
        var edicao = await client.GetAsync($"/Insumos/Editar/{id}");

        Assert.Contains("Inativo", listagem);
        Assert.Contains($"/Insumos/Detalhes/{id}", listagem);
        Assert.Equal(HttpStatusCode.OK, edicao.StatusCode);
    }

    [Theory]
    [InlineData("Desativar")]
    [InlineData("Reativar")]
    public async Task CA11_Post_de_id_inexistente_retorna_404(string handler)
    {
        var id = await CriarInsumoAsync(1, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await PostComTokenAsync(client, 999999, handler, await ObterTokenAsync(client, id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Desativar", true)]
    [InlineData("Reativar", false)]
    public async Task CA12_Post_cross_tenant_retorna_404_e_preserva_registro(string handler, bool ativo)
    {
        var empresaDois = await CriarEmpresaAsync();
        var idOutroTenant = await CriarInsumoAsync(empresaDois, ativo);
        var idEmpresaAtiva = await CriarInsumoAsync(1, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await PostComTokenAsync(client, idOutroTenant, handler, await ObterTokenAsync(client, idEmpresaAtiva));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ativo, (await ObterInsumoAsync(idOutroTenant, empresaDois)).Ativo);
    }

    [Fact]
    public async Task CA13_Post_sem_antiforgery_e_rejeitado_sem_alterar_status()
    {
        var id = await CriarInsumoAsync(1, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await client.PostAsync($"/Insumos/Detalhes/{id}?handler=Desativar", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ObterInsumoAsync(id, 1)).Ativo);
    }

    private async Task<HttpResponseMessage> PostComTokenAsync(HttpClient client, int id, string handler, string? token = null) =>
        await client.PostAsync($"/Insumos/Detalhes/{id}?handler={handler}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token ?? await ObterTokenAsync(client, id)
        }));

    private static async Task<string> ObterTokenAsync(HttpClient client, int id)
    {
        var pagina = await (await client.GetAsync($"/Insumos/Detalhes/{id}")).Content.ReadAsStringAsync();
        return WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
    }

    private async Task<int> CriarEmpresaAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var empresa = Empresa.Criar($"Empresa {Guid.NewGuid():N}");
        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();
        return empresa.Id;
    }

    private async Task<int> CriarInsumoAsync(int empresaId, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Marca", "Observação");
        if (!ativo)
        {
            insumo.Desativar();
        }
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        return insumo.Id;
    }

    private async Task<Insumo> ObterInsumoAsync(int id, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.Insumos.AsNoTracking().SingleAsync(item => item.Id == id);
    }

    private async Task<HttpClient> CriarClienteAutenticadoAsync(int empresaId = 1)
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
        var response = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(await login.Content.ReadAsStringAsync(), "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
    }
}
