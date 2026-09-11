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

public sealed class EditarInsumoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CA01_Edicao_exige_autenticacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Insumos/Editar/1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task CA02_Get_edicao_carrega_campos_funcionais_sem_campos_tecnicos()
    {
        var nome = Nome("Farinha");
        var id = await CriarInsumoAsync(1, nome, "Renata", "W 300", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await client.GetAsync($"/Insumos/Editar/{id}");
        var conteudo = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        response.EnsureSuccessStatusCode();
        Assert.Contains(nome, conteudo);
        Assert.Contains("Renata", conteudo);
        Assert.Contains("Matéria-prima", conteudo);
        Assert.Contains("W 300", conteudo);
        Assert.Contains("Salvar", conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("NomeNormalizado", conteudo);
        Assert.DoesNotContain("MarcaNormalizada", conteudo);
        Assert.DoesNotContain("Ativo", conteudo);
    }

    [Fact]
    public async Task CA04_Post_valido_atualiza_insumo_da_empresa_ativa_e_redireciona()
    {
        var id = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", "Original", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(client, id, "  Açúcar   cristal ", "Embalagem", "Unidade", "  União   Premium ", "  Nova observação  ");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Insumos/Detalhes/{id}", response.Headers.Location!.OriginalString);
        var conteudo = WebUtility.HtmlDecode(await (await client.GetAsync(response.Headers.Location)).Content.ReadAsStringAsync());
        Assert.Contains("Insumo atualizado com sucesso.", conteudo);
        var insumo = await ObterInsumoAsync(id, 1);
        Assert.Equal("Açúcar cristal", insumo.Nome);
        Assert.Equal("UNIÃO PREMIUM", insumo.MarcaNormalizada);
        Assert.Equal(CategoriaInsumo.Embalagem, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Unidade, insumo.UnidadeBase);
        Assert.Equal("Nova observação", insumo.Observacao);
        Assert.True(insumo.Ativo);
        Assert.Equal(1, insumo.EmpresaId);
    }

    [Fact]
    public async Task CA06_Post_sem_alterar_nome_marca_nao_detecta_o_proprio_registro_como_duplicado()
    {
        var nome = Nome("Farinha");
        var id = await CriarInsumoAsync(1, nome, "Renata", null, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(client, id, nome, "Consumivel", "Metro", "Renata", "Alterada");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var insumo = await ObterInsumoAsync(id, 1);
        Assert.Equal(CategoriaInsumo.Consumivel, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Metro, insumo.UnidadeBase);
        Assert.Equal("Alterada", insumo.Observacao);
    }

    [Fact]
    public async Task CA07_Post_para_nome_marca_de_outro_insumo_da_mesma_empresa_e_rejeitado()
    {
        var nomeDuplicado = Nome("Farinha");
        await CriarInsumoAsync(1, nomeDuplicado, "Renata", null, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        var editavel = await CriarInsumoAsync(1, Nome("Açúcar"), "União", "Original", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(client, editavel, $" {nomeDuplicado.ToUpperInvariant()} ", "Embalagem", "Unidade", " RENATA ", "Alterada");
        var conteudo = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe um insumo cadastrado com esse nome e marca.", conteudo);
        var insumo = await ObterInsumoAsync(editavel, 1);
        Assert.StartsWith("Açúcar", insumo.Nome);
        Assert.Equal("União", insumo.Marca);
        Assert.Equal("Original", insumo.Observacao);
    }

    [Fact]
    public async Task CA08_Post_para_combinacao_existente_apenas_em_outra_empresa_e_permitido()
    {
        var empresaDois = await CriarEmpresaAsync();
        var nome = Nome("Farinha");
        await CriarInsumoAsync(empresaDois, nome, "Renata", "Outra empresa", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        var editavel = await CriarInsumoAsync(1, Nome("Açúcar"), "União", null, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(client, editavel, nome, "Embalagem", "Unidade", "Renata", "Empresa um");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var empresaUm = await ObterInsumoAsync(editavel, 1);
        Assert.Equal(nome, empresaUm.Nome);
        Assert.Equal("Empresa um", empresaUm.Observacao);
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var contextoEmpresaDois = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaDois));
        var outroTenant = await contextoEmpresaDois.Insumos.SingleAsync();
        Assert.Equal("Outra empresa", outroTenant.Observacao);
    }

    [Fact]
    public async Task CA10_Post_com_dados_invalidos_nao_persiste_alteracoes()
    {
        var id = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", "Original", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(client, id, " ", "0", "Grama", "Marca", new string('a', 1001));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var insumo = await ObterInsumoAsync(id, 1);
        Assert.StartsWith("Farinha", insumo.Nome);
        Assert.Equal("Renata", insumo.Marca);
        Assert.Equal("Original", insumo.Observacao);
        Assert.Equal(CategoriaInsumo.MateriaPrima, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Grama, insumo.UnidadeBase);
    }

    [Fact]
    public async Task CA11_Get_e_post_de_id_inexistente_retornam_404()
    {
        var idExistente = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", null, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Insumos/Editar/999999")).StatusCode);
        var token = await ObterTokenAsync(client, idExistente);
        var post = await EnviarFormularioComTokenAsync(client, 999999, token, "Farinha", "MateriaPrima", "Grama", "Renata", null);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task CA12_Get_e_post_de_id_de_outro_tenant_retornam_404_sem_alterar_registro()
    {
        var empresaDois = await CriarEmpresaAsync();
        var idOutroTenant = await CriarInsumoAsync(empresaDois, Nome("Segredo"), "Outra", "Intacto", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        var idEmpresaAtiva = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", null, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Insumos/Editar/{idOutroTenant}")).StatusCode);
        var token = await ObterTokenAsync(client, idEmpresaAtiva);
        var post = await EnviarFormularioComTokenAsync(client, idOutroTenant, token, "Alterado", "Embalagem", "Unidade", "Marca", "Novo");
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        var insumoOutroTenant = await ObterInsumoAsync(idOutroTenant, empresaDois);
        Assert.StartsWith("Segredo", insumoOutroTenant.Nome);
        Assert.Equal("Outra", insumoOutroTenant.Marca);
        Assert.Equal("Intacto", insumoOutroTenant.Observacao);
    }

    [Fact]
    public async Task CA14_Detalhes_exibe_link_editar_do_registro()
    {
        var id = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", null, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        using var client = await CriarClienteAutenticadoAsync();

        var conteudo = await (await client.GetAsync($"/Insumos/Detalhes/{id}")).Content.ReadAsStringAsync();

        Assert.Contains($"/Insumos/Editar/{id}", conteudo);
        Assert.Contains("Editar", conteudo);
    }

    private async Task<HttpResponseMessage> EnviarFormularioAsync(HttpClient client, int id, string nome, string categoria, string unidadeBase, string? marca, string? observacao) =>
        await EnviarFormularioComTokenAsync(client, id, await ObterTokenAsync(client, id), nome, categoria, unidadeBase, marca, observacao);

    private static Task<HttpResponseMessage> EnviarFormularioComTokenAsync(HttpClient client, int id, string token, string nome, string categoria, string unidadeBase, string? marca, string? observacao) =>
        client.PostAsync($"/Insumos/Editar/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Nome"] = nome,
            ["Input.Categoria"] = categoria,
            ["Input.UnidadeBase"] = unidadeBase,
            ["Input.Marca"] = marca ?? string.Empty,
            ["Input.Observacao"] = observacao ?? string.Empty
        }));

    private static async Task<string> ObterTokenAsync(HttpClient client, int id)
    {
        var pagina = await (await client.GetAsync($"/Insumos/Editar/{id}")).Content.ReadAsStringAsync();
        return WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
    }

    private static string Nome(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";

    private async Task<int> CriarEmpresaAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var empresa = Empresa.Criar($"Empresa {Guid.NewGuid():N}");
        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();
        return empresa.Id;
    }

    private async Task<int> CriarInsumoAsync(int empresaId, string nome, string? marca, string? observacao, CategoriaInsumo categoria, UnidadeMedida unidade)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var insumo = Insumo.Criar(empresaId, nome, categoria, unidade, marca, observacao);
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
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var usuario = new UsuarioAplicacao { UserName = email, Email = email };
            Assert.True((await userManager.CreateAsync(usuario, senha)).Succeeded);
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaId, Ativo = true });
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var login = await client.GetAsync("/Conta/Login");
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(await login.Content.ReadAsStringAsync(), "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        return client;
    }

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
    }
}
