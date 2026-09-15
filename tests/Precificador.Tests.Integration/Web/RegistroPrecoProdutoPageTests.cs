using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class RegistroPrecoProdutoPageTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly WebTestContext web;
    public RegistroPrecoProdutoPageTests(CustomWebApplicationFactory factory) { this.factory = factory; web = new(factory); }

    [Fact]
    public async Task W1_W2_W3_W4_W5_W12_W13_W21_W22_W23_Get_e_post_respeitam_contrato_comercial()
    {
        var produto = await CriarProdutoPrecificavelAsync(1, ativo: false);
        using var anonimo = web.CriarCliente();
        Assert.Equal(HttpStatusCode.Redirect, (await anonimo.GetAsync($"/Produtos/Precos/Novo/{produto}")).StatusCode);
        using var client = await web.CriarClienteAutenticadoAsync();
        var get = await client.GetAsync($"/Produtos/Precos/Novo/{produto}"); var html = await get.Content.ReadAsStringAsync();
        get.EnsureSuccessStatusCode(); Assert.Contains("Preço de prateleira", html); Assert.DoesNotContain("MargemReferencia", html);
        Assert.Empty(await RegistrosAsync(produto, 1));
        var token = WebTestHtml.ExtrairTokenAntiforgery(html);
        var invalido = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "0"));
        Assert.Equal(HttpStatusCode.OK, invalido.StatusCode); Assert.Empty(await RegistrosAsync(produto, 1));
        token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}"));
        var valido = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "1"));
        Assert.Equal(HttpStatusCode.Redirect, valido.StatusCode); Assert.Equal($"/Produtos/Detalhes/{produto}", valido.Headers.Location!.OriginalString);
        var registro = Assert.Single(await RegistrosAsync(produto, 1)); Assert.Equal(1m, registro.PrecoPrateleira); Assert.False((await ProdutoAsync(produto, 1)).Ativo);
    }

    [Fact]
    public async Task W6_W7_W20_Request_manipulado_e_cross_tenant_nao_controlam_snapshot()
    {
        var produto = await CriarProdutoPrecificavelAsync(1, ativo: true); var empresaDois = await web.CriarEmpresaAsync(); var outro = await CriarProdutoPrecificavelAsync(empresaDois, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Produtos/Precos/Novo/{outro}")).StatusCode);
        var token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}"));
        var post = await client.PostAsync($"/Produtos/Precos/Novo/{outro}", Form(token, "10")); Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        post = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = token, ["Input.PrecoPrateleira"] = "10", ["EmpresaId"] = empresaDois.ToString(), ["MargemReferencia"] = "0" }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode); Assert.Equal(1, Assert.Single(await RegistrosAsync(produto, 1)).EmpresaId);
    }

    private static FormUrlEncodedContent Form(string token, string preco) => new(new Dictionary<string,string> { ["__RequestVerificationToken"] = token, ["Input.PrecoPrateleira"] = preco });
    private async Task<int> CriarProdutoPrecificavelAsync(int empresa, bool ativo)
    {
        using var scope = factory.Services.CreateScope(); var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var context = new PrecificadorDbContext(options, new Contexto(empresa));
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync(); configuracao.Atualizar(null, null, null, .01m, .1m);
        var produto = Produto.Criar(empresa, Guid.NewGuid().ToString(), .3m); if (!ativo) produto.Desativar(); context.Produtos.Add(produto); await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(empresa, produto.Id, 1m, 0); context.FichasTecnicas.Add(ficha); var insumo = Insumo.Criar(empresa, Guid.NewGuid().ToString(), CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade); context.Insumos.Add(insumo); await context.SaveChangesAsync();
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(empresa, ficha.Id, insumo.Id, 1m, null, 0m)); context.PrecosInsumos.Add(PrecoInsumo.Criar(empresa, insumo.Id, 1m, 10m, new DateOnly(2026,9,15))); await context.SaveChangesAsync(); return produto.Id;
    }
    private async Task<List<RegistroPrecoProduto>> RegistrosAsync(int produto, int empresa) { using var scope = factory.Services.CreateScope(); var o=scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(empresa)); return await c.RegistrosPrecosProdutos.Where(r=>r.ProdutoId==produto).ToListAsync(); }
    private async Task<Produto> ProdutoAsync(int id,int empresa) { using var scope=factory.Services.CreateScope(); var o=scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(empresa)); return await c.Produtos.SingleAsync(p=>p.Id==id); }
    private sealed class Contexto(int id) : IEmpresaContext { public int? EmpresaId=>id; public int EmpresaIdOuSentinela=>id; public string? TimeZoneId=>Empresa.TimeZoneIdPadrao; }
}
