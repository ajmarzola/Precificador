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

public sealed class PrecoInsumoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CA01_Registrar_preco_exige_autenticacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Insumos/Precos/Novo/1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task CA02_Get_exibe_resumo_do_insumo_e_unidade_na_quantidade()
    {
        var id = await CriarInsumoAsync(1, Nome("Fita"), "Marca metro", UnidadeMedida.Metro, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await client.GetAsync($"/Insumos/Precos/Novo/{id}");
        var conteudo = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        response.EnsureSuccessStatusCode();
        Assert.Contains("Fita", conteudo);
        Assert.Contains("Marca metro", conteudo);
        Assert.Contains("Unidade base", conteudo);
        Assert.Contains(">m<", conteudo);
        Assert.Contains("Quantidade comprada (m)", conteudo);
        Assert.Contains("Ativo", conteudo);
    }

    [Fact]
    public async Task CA03_CA14_Post_valido_registra_preco_e_redireciona_com_sucesso()
    {
        var id = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarPrecoAsync(client, id, "1000", "12", "2026-09-11");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Insumos/Detalhes/{id}", response.Headers.Location!.OriginalString);
        var detalhe = WebUtility.HtmlDecode(await client.GetStringAsync(response.Headers.Location));
        Assert.Contains("Preço do insumo registrado com sucesso.", detalhe);

        var precos = await ObterPrecosAsync(id, 1);
        var preco = Assert.Single(precos);
        Assert.Equal(1, preco.EmpresaId);
        Assert.Equal(id, preco.InsumoId);
        Assert.Equal(1000m, preco.QuantidadeCompra);
        Assert.Equal(12m, preco.PrecoCompra);
        Assert.Equal(new DateOnly(2026, 9, 11), preco.DataReferencia);
    }

    [Theory]
    [MemberData(nameof(DadosInvalidosDePreco))]
    public async Task CA05_CA06_CA07_Post_invalido_nao_cria_preco(string quantidade, string preco, string data)
    {
        var id = await CriarInsumoAsync(1, Nome("Insumo inválido"), null, UnidadeMedida.Unidade, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarPrecoAsync(client, id, quantidade, preco, data);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await ObterPrecosAsync(id, 1));
    }

    [Theory]
    [InlineData("2020-01-01")]
    [InlineData("2026-09-11")]
    [InlineData("2099-01-01")]
    public async Task CA07_Datas_passada_atual_e_futura_sao_aceitas(string data)
    {
        var id = await CriarInsumoAsync(1, Nome("Preço datado"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarPrecoAsync(client, id, "1", "10", data);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Single(await ObterPrecosAsync(id, 1));
    }

    [Fact]
    public async Task CA11_Get_e_post_cross_tenant_retornam_404()
    {
        var empresaDois = await CriarEmpresaAsync();
        var idOutroTenant = await CriarInsumoAsync(empresaDois, Nome("Segredo"), "Outra", UnidadeMedida.Grama, ativo: true);
        var idEmpresaAtiva = await CriarInsumoAsync(1, Nome("Visível"), "Nossa", UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var get = await client.GetAsync($"/Insumos/Precos/Novo/{idOutroTenant}");
        var token = await ObterTokenPrecoAsync(client, idEmpresaAtiva);
        var post = await EnviarPrecoComTokenAsync(client, idOutroTenant, token, "1", "10", "2026-09-11");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        Assert.Empty(await ObterPrecosAsync(idOutroTenant, empresaDois));
    }

    [Fact]
    public async Task CA12_Insumo_inativo_recebe_preco_e_permanece_inativo()
    {
        var id = await CriarInsumoAsync(1, Nome("Inativo"), "Marca", UnidadeMedida.Grama, ativo: false);
        using var client = await CriarClienteAutenticadoAsync();

        var response = await EnviarPrecoAsync(client, id, "1000", "15", "2026-09-11");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Single(await ObterPrecosAsync(id, 1));
        Assert.False((await ObterInsumoAsync(id, 1)).Ativo);
    }

    [Fact]
    public async Task CA13_Detalhes_exibe_registrar_preco_para_ativo_e_inativo()
    {
        var ativo = await CriarInsumoAsync(1, Nome("Ativo"), null, UnidadeMedida.Grama, ativo: true);
        var inativo = await CriarInsumoAsync(1, Nome("Inativo"), null, UnidadeMedida.Grama, ativo: false);
        using var client = await CriarClienteAutenticadoAsync();

        var paginaAtivo = await client.GetStringAsync($"/Insumos/Detalhes/{ativo}");
        var paginaInativo = await client.GetStringAsync($"/Insumos/Detalhes/{inativo}");

        Assert.Contains($"/Insumos/Precos/Novo/{ativo}", paginaAtivo);
        Assert.Contains("Registrar preço", paginaAtivo);
        Assert.Contains($"/Insumos/Precos/Novo/{inativo}", paginaInativo);
        Assert.Contains("Registrar preço", paginaInativo);
    }

    [Fact]
    public async Task CA15_Get_edicao_com_historico_bloqueia_nome_marca_e_unidade()
    {
        var id = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, id, new DateOnly(2026, 9, 10));
        using var client = await CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Editar/{id}"));

        Assert.Contains("Nome, marca e unidade base não podem ser alterados porque este insumo já possui histórico de preços.", conteudo);
        Assert.Contains("readonly", ObterTag(conteudo, "input", "Input.Nome"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("readonly", ObterTag(conteudo, "input", "Input.Marca"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disabled", ObterTag(conteudo, "select", "Input.UnidadeBase"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("disabled", ObterTag(conteudo, "select", "Input.Categoria"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CA16_Post_manipulado_com_historico_nao_altera_campos_congelados()
    {
        var nomeOriginal = Nome("Farinha");
        var id = await CriarInsumoAsync(1, nomeOriginal, "Renata", UnidadeMedida.Grama, ativo: true, categoria: CategoriaInsumo.MateriaPrima, observacao: "Original");
        await CriarPrecoAsync(1, id, new DateOnly(2026, 9, 10));
        using var client = await CriarClienteAutenticadoAsync();
        var token = await ObterTokenEdicaoAsync(client, id);

        var response = await EnviarEdicaoComTokenAsync(
            client,
            id,
            token,
            "Açúcar adulterado",
            "Caputo adulterada",
            "Consumivel",
            "Metro",
            "Observação permitida");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var insumo = await ObterInsumoAsync(id, 1);
        Assert.Equal(nomeOriginal, insumo.Nome);
        Assert.Equal("Renata", insumo.Marca);
        Assert.Equal(UnidadeMedida.Grama, insumo.UnidadeBase);
        Assert.Equal(CategoriaInsumo.Consumivel, insumo.Categoria);
        Assert.Equal("Observação permitida", insumo.Observacao);
    }

    [Fact]
    public async Task CA15_Preco_futuro_tambem_bloqueia_campos_da_RN040()
    {
        var id = await CriarInsumoAsync(1, Nome("Preço futuro"), "Marca", UnidadeMedida.Metro, ativo: true);
        await CriarPrecoAsync(1, id, new DateOnly(2099, 1, 1));
        using var client = await CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Editar/{id}"));

        Assert.Contains("já possui histórico de preços", conteudo);
        Assert.Contains("readonly", ObterTag(conteudo, "input", "Input.Nome"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disabled", ObterTag(conteudo, "select", "Input.UnidadeBase"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CA17_Sem_historico_mantem_nome_marca_e_unidade_editaveis()
    {
        var id = await CriarInsumoAsync(1, Nome("Sem histórico"), "Marca antiga", UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Editar/{id}"));
        Assert.DoesNotContain("já possui histórico de preços", conteudo);
        Assert.DoesNotContain("readonly", ObterTag(conteudo, "input", "Input.Nome"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("disabled", ObterTag(conteudo, "select", "Input.UnidadeBase"), StringComparison.OrdinalIgnoreCase);

        var token = await ObterTokenEdicaoAsync(client, id);
        var novoNome = Nome("Alterado");
        var response = await EnviarEdicaoComTokenAsync(client, id, token, novoNome, "Marca nova", "Embalagem", "Metro", "Livre");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var insumo = await ObterInsumoAsync(id, 1);
        Assert.Equal(novoNome, insumo.Nome);
        Assert.Equal("Marca nova", insumo.Marca);
        Assert.Equal(UnidadeMedida.Metro, insumo.UnidadeBase);
    }

    public static IEnumerable<object[]> DadosInvalidosDePreco =>
    [
        ["0", "10", "2026-09-11"],
        ["-1", "10", "2026-09-11"],
        ["1", "0", "2026-09-11"],
        ["1", "-1", "2026-09-11"],
        ["1", "10", ""],
        ["1", "10", "data-invalida"]
    ];

    private async Task<HttpResponseMessage> EnviarPrecoAsync(HttpClient client, int id, string quantidade, string preco, string data) =>
        await EnviarPrecoComTokenAsync(client, id, await ObterTokenPrecoAsync(client, id), quantidade, preco, data);

    private static Task<HttpResponseMessage> EnviarPrecoComTokenAsync(HttpClient client, int id, string token, string quantidade, string preco, string data) =>
        client.PostAsync($"/Insumos/Precos/Novo/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.QuantidadeCompra"] = quantidade,
            ["Input.PrecoCompra"] = preco,
            ["Input.DataReferencia"] = data
        }));

    private static Task<HttpResponseMessage> EnviarEdicaoComTokenAsync(
        HttpClient client,
        int id,
        string token,
        string nome,
        string marca,
        string categoria,
        string unidade,
        string observacao) =>
        client.PostAsync($"/Insumos/Editar/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Nome"] = nome,
            ["Input.Marca"] = marca,
            ["Input.Categoria"] = categoria,
            ["Input.UnidadeBase"] = unidade,
            ["Input.Observacao"] = observacao
        }));

    private static async Task<string> ObterTokenPrecoAsync(HttpClient client, int id)
    {
        var pagina = await client.GetStringAsync($"/Insumos/Precos/Novo/{id}");
        return ExtrairToken(pagina);
    }

    private static async Task<string> ObterTokenEdicaoAsync(HttpClient client, int id)
    {
        var pagina = await client.GetStringAsync($"/Insumos/Editar/{id}");
        return ExtrairToken(pagina);
    }

    private static string ExtrairToken(string pagina) =>
        WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

    private static string ObterTag(string html, string tag, string nomeCampo) =>
        Regex.Match(html, $"<{tag}[^>]*name=\"{Regex.Escape(nomeCampo)}\"[^>]*>", RegexOptions.IgnoreCase).Value;

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

    private async Task<int> CriarInsumoAsync(
        int empresaId,
        string nome,
        string? marca,
        UnidadeMedida unidade,
        bool ativo,
        CategoriaInsumo categoria = CategoriaInsumo.MateriaPrima,
        string? observacao = null)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var insumo = Insumo.Criar(empresaId, nome, categoria, unidade, marca, observacao);
        if (!ativo)
        {
            insumo.Desativar();
        }

        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        return insumo.Id;
    }

    private async Task CriarPrecoAsync(int empresaId, int insumoId, DateOnly data)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, 1m, 10m, data));
        await context.SaveChangesAsync();
    }

    private async Task<Insumo> ObterInsumoAsync(int id, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.Insumos.AsNoTracking().SingleAsync(item => item.Id == id);
    }

    private async Task<List<PrecoInsumo>> ObterPrecosAsync(int insumoId, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.PrecosInsumos.AsNoTracking().Where(preco => preco.InsumoId == insumoId).ToListAsync();
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
            ["__RequestVerificationToken"] = ExtrairToken(await login.Content.ReadAsStringAsync()),
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
