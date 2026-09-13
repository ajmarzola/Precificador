using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class SituacaoProdutoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CA01_Detalhes_de_produto_ativo_exibe_desativar_e_nao_reativar()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto ativo"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var pagina = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{id}"));

        Assert.Contains("Desativar", pagina);
        Assert.DoesNotContain("Reativar", pagina);
        Assert.Contains("Deseja desativar este produto?", pagina);
        Assert.Contains($"href=\"/Produtos/Editar/{id}\"", pagina);
    }

    [Fact]
    public async Task CA02_Detalhes_de_produto_inativo_exibe_reativar_e_nao_desativar()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto inativo"), null, 0.30m, ativo: false);
        using var client = await CriarClienteAutenticadoAsync(1);

        var pagina = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{id}"));

        Assert.Contains("Reativar", pagina);
        Assert.DoesNotContain("Desativar", pagina);
        Assert.Contains($"href=\"/Produtos/Editar/{id}\"", pagina);
    }

    [Fact]
    public async Task CA06_Post_desativar_persiste_status_faz_PRG_e_exibe_sucesso()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto desativar"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await PostComTokenAsync(client, id, "Desativar");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Produtos/Detalhes/{id}", response.Headers.Location!.ToString());
        Assert.False((await ObterProdutoAsync(id, 1)).Ativo);
        Assert.Contains("Produto desativado com sucesso.", await LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location)));
    }

    [Fact]
    public async Task CA07_Post_reativar_persiste_status_faz_PRG_e_exibe_sucesso()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto reativar"), null, 0.30m, ativo: false);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await PostComTokenAsync(client, id, "Reativar");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Produtos/Detalhes/{id}", response.Headers.Location!.ToString());
        Assert.True((await ObterProdutoAsync(id, 1)).Ativo);
        Assert.Contains("Produto reativado com sucesso.", await LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location)));
    }

    [Fact]
    public async Task CA08_CA09_Produto_inativo_permanece_na_listagem_e_detalhes()
    {
        var nome = NomeUnico("Produto listado inativo");
        var id = await CriarProdutoAsync(1, nome, "Papelaria", 0.255m, ativo: false);
        using var client = await CriarClienteAutenticadoAsync(1);

        var listagem = await LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));
        var detalhes = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{id}"));
        var linha = LinhaProduto(listagem, nome);

        Assert.Contains(nome, linha);
        Assert.Contains("Papelaria", linha);
        Assert.Contains("25,5%", linha);
        Assert.Contains("Inativo", linha);
        Assert.Contains($"/Produtos/Detalhes/{id}", linha);
        Assert.Contains("Situação", detalhes);
        Assert.Contains("Inativo", detalhes);
        Assert.Contains("Reativar", detalhes);
        Assert.Contains($"href=\"/Produtos/Editar/{id}\"", detalhes);
    }

    [Fact]
    public async Task CA10_Produto_inativo_permanece_editavel_sem_reativacao_implicita()
    {
        var nomeOriginal = NomeUnico("Produto editavel inativo");
        var id = await CriarProdutoAsync(1, nomeOriginal, "Original", 0.30m, ativo: false);
        using var client = await CriarClienteAutenticadoAsync(1);
        var novoNome = NomeUnico("Produto editado inativo");

        var detalhes = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{id}"));
        var getEditar = await client.GetAsync($"/Produtos/Editar/{id}");
        var postEditar = await EnviarEdicaoAsync(client, id, $"  {novoNome}  ", "  Atualizada  ", "25,5");

        Assert.Contains($"href=\"/Produtos/Editar/{id}\"", detalhes);
        Assert.Equal(HttpStatusCode.OK, getEditar.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, postEditar.StatusCode);
        Assert.Equal($"/Produtos/Detalhes/{id}", postEditar.Headers.Location!.ToString());

        var produto = await ObterProdutoAsync(id, 1);
        Assert.Equal(novoNome, produto.Nome);
        Assert.Equal(novoNome.ToUpperInvariant(), produto.NomeNormalizado);
        Assert.Equal("Atualizada", produto.Categoria);
        Assert.Equal(0.255m, produto.MargemAlvo);
        Assert.False(produto.Ativo);
    }

    [Theory]
    [InlineData("Desativar")]
    [InlineData("Reativar")]
    public async Task CA12_Post_de_id_inexistente_retorna_404(string handler)
    {
        var idExistente = await CriarProdutoAsync(1, NomeUnico("Produto token"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await PostComTokenAsync(client, 999999, handler, await ObterTokenAsync(client, idExistente));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Desativar", true)]
    [InlineData("Reativar", false)]
    public async Task CA13_Post_cross_tenant_retorna_404_e_preserva_status(string handler, bool ativo)
    {
        var empresaDois = await CriarEmpresaAsync();
        var idOutroTenant = await CriarProdutoAsync(empresaDois, NomeUnico("Produto cross tenant"), null, 0.30m, ativo);
        var idEmpresaAtiva = await CriarProdutoAsync(1, NomeUnico("Produto token tenant"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await PostComTokenAsync(client, idOutroTenant, handler, await ObterTokenAsync(client, idEmpresaAtiva));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ativo, (await ObterProdutoAsync(idOutroTenant, empresaDois)).Ativo);
    }

    [Theory]
    [InlineData("Desativar", true)]
    [InlineData("Reativar", false)]
    public async Task CA14_Post_sem_antiforgery_e_rejeitado_sem_alterar_status(string handler, bool ativo)
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto antiforgery"), null, 0.30m, ativo);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await client.PostAsync($"/Produtos/Detalhes/{id}?handler={handler}", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ativo, (await ObterProdutoAsync(id, 1)).Ativo);
    }

    [Fact]
    public async Task CA15_Get_nao_altera_situacao()
    {
        var idAtivo = await CriarProdutoAsync(1, NomeUnico("Produto get ativo"), null, 0.30m, ativo: true);
        var idInativo = await CriarProdutoAsync(1, NomeUnico("Produto get inativo"), null, 0.30m, ativo: false);
        using var client = await CriarClienteAutenticadoAsync(1);

        await client.GetAsync("/Produtos");
        await client.GetAsync($"/Produtos/Detalhes/{idAtivo}");
        await client.GetAsync($"/Produtos/Detalhes/{idInativo}");
        await client.GetAsync($"/Produtos/Editar/{idInativo}");

        Assert.True((await ObterProdutoAsync(idAtivo, 1)).Ativo);
        Assert.False((await ObterProdutoAsync(idInativo, 1)).Ativo);
    }

    private async Task<HttpResponseMessage> PostComTokenAsync(HttpClient client, int id, string handler, string? token = null) =>
        await client.PostAsync($"/Produtos/Detalhes/{id}?handler={handler}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token ?? await ObterTokenAsync(client, id)
        }));

    private static async Task<string> ObterTokenAsync(HttpClient client, int id)
    {
        var pagina = await (await client.GetAsync($"/Produtos/Detalhes/{id}")).Content.ReadAsStringAsync();
        return Token(pagina);
    }

    private static async Task<HttpResponseMessage> EnviarEdicaoAsync(
        HttpClient client,
        int id,
        string nome,
        string? categoria,
        string margemPercentual)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/Editar/{id}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);

        return await client.PostAsync($"/Produtos/Editar/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina),
            ["Input.Nome"] = nome,
            ["Input.Categoria"] = categoria ?? string.Empty,
            ["Input.MargemAlvoPercentual"] = margemPercentual
        }));
    }

    private async Task<int> CriarProdutoAsync(int empresaId, string nome, string? categoria, decimal margemAlvo, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, nome, margemAlvo, categoria);
        if (!ativo)
        {
            produto.Desativar();
        }

        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private async Task<Produto> ObterProdutoAsync(int id, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.Produtos.AsNoTracking().SingleAsync(produto => produto.Id == id);
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
        var response = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(await login.Content.ReadAsStringAsync()),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static async Task<string> LerHtmlDecodificadoAsync(HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return WebUtility.HtmlDecode(Encoding.UTF8.GetString(bytes));
    }

    private static string LinhaProduto(string conteudo, string nome)
    {
        var match = Regex.Match(conteudo, $"<tr>.*?{Regex.Escape(nome)}.*?</tr>", RegexOptions.Singleline);
        Assert.True(match.Success, conteudo);
        return match.Value;
    }

    private static string NomeUnico(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";

    private static string Token(string pagina) =>
        WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
