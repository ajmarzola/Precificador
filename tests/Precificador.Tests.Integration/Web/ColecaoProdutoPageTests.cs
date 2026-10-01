using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class ColecaoProdutoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task Cadastro_lista_edicao_e_isolamento_funcionam_com_login_real()
    {
        var nome = "Colecao " + Guid.NewGuid().ToString("N");
        var nomeInativa = "Categoria " + Guid.NewGuid().ToString("N");
        var categoria = await CriarCategoriaAsync(1, nomeInativa, false);
        var ativa = await CriarCategoriaAsync(1, "Ativa " + Guid.NewGuid().ToString("N"), true);
        var empresa2 = await web.CriarEmpresaAsync();
        var alheia = await CriarCategoriaAsync(empresa2, "Alheia " + Guid.NewGuid().ToString("N"), true);
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var novo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos/Colecoes/Novo"));
        Assert.Contains("Ativa", novo);
        Assert.DoesNotContain("Alheia", novo);
        Assert.DoesNotContain(nomeInativa, novo);

        var invalida = await EnviarAsync(client, "/Produtos/Colecoes/Novo", nome, "2027-01-02", "2027-01-01", [ativa]);
        Assert.Equal(HttpStatusCode.OK, invalida.StatusCode);
        var comCategoriaAlheia = await EnviarAsync(client, "/Produtos/Colecoes/Novo", nome, "2027-01-02", "", [alheia]);
        Assert.Equal(HttpStatusCode.OK, comCategoriaAlheia.StatusCode);
        var comCategoriaInativa = await EnviarAsync(client, "/Produtos/Colecoes/Novo", nome, "2027-01-02", "", [categoria]);
        Assert.Equal(HttpStatusCode.OK, comCategoriaInativa.StatusCode);
        var cadastro = await EnviarAsync(client, "/Produtos/Colecoes/Novo", nome, "2027-01-02", "", [ativa, ativa]);
        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);
        Assert.Equal("/Produtos/Colecoes", cadastro.Headers.Location!.ToString());

        int id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var colecao = await db.ColecoesProdutos.IgnoreQueryFilters().SingleAsync(x => x.Nome == nome);
            id = colecao.Id;
            Assert.Equal(1, colecao.EmpresaId);
            Assert.Single(await db.ColecoesProdutosCategorias.IgnoreQueryFilters().Where(x => x.ColecaoProdutoId == id).ToListAsync());
        }
        var lista = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos/Colecoes"));
        Assert.Contains(nome, lista);
        Assert.Contains("Em aberto", lista);
        var rejeitada = await EnviarAsync(client, $"/Produtos/Colecoes/Editar/{id}", nome + " Novo", "2027-01-02", "2027-01-02", [ativa, categoria]);
        Assert.Equal(HttpStatusCode.OK, rejeitada.StatusCode);
        var edicao = await EnviarAsync(client, $"/Produtos/Colecoes/Editar/{id}", nome + " Novo", "2027-01-02", "2027-01-02", [ativa]);
        Assert.Equal(HttpStatusCode.Redirect, edicao.StatusCode);
        await DesativarCategoriaAsync(ativa);
        var paginaEdicao = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Colecoes/Editar/{id}"));
        Assert.Contains("(inativa)", paginaEdicao);
        Assert.Contains("checked", paginaEdicao);
        var mantida = await EnviarAsync(client, $"/Produtos/Colecoes/Editar/{id}", nome + " Novo", "2027-01-02", "", [ativa]);
        Assert.Equal(HttpStatusCode.Redirect, mantida.StatusCode);
        var removida = await EnviarAsync(client, $"/Produtos/Colecoes/Editar/{id}", nome + " Novo", "2027-01-02", "", []);
        Assert.Equal(HttpStatusCode.Redirect, removida.StatusCode);

        using var outroCliente = await web.CriarClienteAutenticadoAsync(empresa2);
        Assert.Equal(HttpStatusCode.NotFound, (await outroCliente.GetAsync($"/Produtos/Colecoes/Editar/{id}")).StatusCode);
        var paginaOutro = await (await outroCliente.GetAsync("/Produtos/Colecoes/Novo")).Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await outroCliente.PostAsync($"/Produtos/Colecoes/Editar/{id}", new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(paginaOutro), ["Input.Nome"] = "Alterada"
        }))).StatusCode);
    }

    [Fact]
    public async Task Duplicidade_e_antiforgery_sao_controlados()
    {
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var nome = "Colecao " + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarAsync(client, "/Produtos/Colecoes/Novo", nome, "2028-01-01", "", [])).StatusCode);
        var duplicada = await EnviarAsync(client, "/Produtos/Colecoes/Novo", nome.ToUpperInvariant(), "2028-01-01", "", []);
        Assert.Equal(HttpStatusCode.OK, duplicada.StatusCode);
        Assert.Contains("Já existe uma coleção", await WebTestHtml.LerHtmlDecodificadoAsync(duplicada));
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarAsync(client, "/Produtos/Colecoes/Novo", nome, "2029-01-01", "", [])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Produtos/Colecoes/Novo", new FormUrlEncodedContent(new Dictionary<string, string> { ["Input.Nome"] = nome }))).StatusCode);
    }

    [Fact]
    public async Task Lista_mostra_situacao_derivada_e_ordenacao_por_lancamento()
    {
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var sufixo = Guid.NewGuid().ToString("N");
        var antiga = "Antiga " + sufixo;
        var atual = "Atual " + sufixo;
        var futura = "Futura " + sufixo;
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarAsync(client, "/Produtos/Colecoes/Novo", antiga, "2020-01-01", "2020-12-31", [])).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarAsync(client, "/Produtos/Colecoes/Novo", atual, "2021-01-01", "", [])).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarAsync(client, "/Produtos/Colecoes/Novo", futura, "2099-01-01", "", [])).StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos/Colecoes"));
        Assert.True(html.IndexOf(futura, StringComparison.Ordinal) < html.IndexOf(atual, StringComparison.Ordinal));
        Assert.True(html.IndexOf(atual, StringComparison.Ordinal) < html.IndexOf(antiga, StringComparison.Ordinal));
        Assert.Contains($"{antiga}</td>", html);
        Assert.Contains("Finalizada", html);
        Assert.Contains("Em andamento", html);
        Assert.Contains("Planejada", html);
        Assert.Contains("—", html);
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpClient client, string url, string nome, string inicio, string fim, IReadOnlyList<int> categorias)
    {
        var html = await (await client.GetAsync(url)).Content.ReadAsStringAsync();
        var campos = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", WebTestHtml.ExtrairTokenAntiforgery(html)),
            new("Input.Nome", nome), new("Input.DataLancamento", inicio), new("Input.DataFinalizacao", fim),
            new("Input.EmpresaId", "999")
        };
        campos.AddRange(categorias.Select(id => new KeyValuePair<string,string>("Input.CategoriaProdutoIds", id.ToString())));
        return await client.PostAsync(url, new FormUrlEncodedContent(campos));
    }

    private async Task<int> CriarCategoriaAsync(int empresaId, string nome, bool ativa)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var categoria = CategoriaProduto.Criar(empresaId, nome, FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0);
        if (!ativa) categoria.Desativar();
        db.CategoriasProdutos.Add(categoria);
        await db.SaveChangesAsync();
        return categoria.Id;
    }

    private async Task DesativarCategoriaAsync(int id)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        var categoria = await db.CategoriasProdutos.SingleAsync(x => x.Id == id);
        categoria.Desativar();
        await db.SaveChangesAsync();
    }
}
