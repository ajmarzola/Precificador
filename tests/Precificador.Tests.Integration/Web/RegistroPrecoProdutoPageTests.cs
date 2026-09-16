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
        get.EnsureSuccessStatusCode(); Assert.Contains("Preço de prateleira", html); Assert.Contains("Custo unitário", html); Assert.Contains("Preço teórico", html); Assert.Contains("Preço sugerido", html); Assert.Contains("Inativo", html); Assert.Contains("R$ 10,00", html); Assert.Contains("R$ 14,29", html);
        Assert.Contains("name=\"Input.PrecoPrateleira\"", html); Assert.DoesNotContain("name=\"EmpresaId\"", html); Assert.DoesNotContain("name=\"MargemReferencia\"", html); Assert.DoesNotContain("name=\"DataReferencia\"", html); Assert.DoesNotContain("name=\"PrecoSugerido\"", html);
        Assert.Empty(await RegistrosAsync(produto, 1));
        var token = WebTestHtml.ExtrairTokenAntiforgery(html);
        var invalido = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "0"));
        Assert.Equal(HttpStatusCode.OK, invalido.StatusCode); Assert.Contains("value=\"0\"", await invalido.Content.ReadAsStringAsync()); Assert.Empty(await RegistrosAsync(produto, 1));
        token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}"));
        invalido = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "-1")); Assert.Equal(HttpStatusCode.OK, invalido.StatusCode); Assert.Empty(await RegistrosAsync(produto, 1));
        token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}"));
        var valido = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "1"));
        Assert.Equal(HttpStatusCode.Redirect, valido.StatusCode); Assert.Equal($"/Produtos/Detalhes/{produto}", valido.Headers.Location!.OriginalString);
        var registro = Assert.Single(await RegistrosAsync(produto, 1)); Assert.Equal(1m, registro.PrecoPrateleira); Assert.Equal(10m, registro.CustoReferencia); Assert.Equal(.3m, registro.MargemReferencia); Assert.Equal(14.29m, registro.PrecoSugerido); Assert.Equal(.1m, registro.ReservaComercialReferencia); Assert.NotEqual(default, registro.DataReferencia); Assert.False((await ProdutoAsync(produto, 1)).Ativo);
        token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}")); await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "2")); Assert.Equal(2, (await RegistrosAsync(produto, 1)).Count);
        var detalhes = await client.GetStringAsync($"/Produtos/Detalhes/{produto}"); Assert.Contains($"/Produtos/Precos/Novo/{produto}", detalhes); Assert.Contains("Registrar preço de prateleira", detalhes);
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

    [Fact]
    public async Task W1_W8_W9_W10_W23_Sem_empresa_ativa_e_precificacao_incompleta_nao_registram()
    {
        using var semEmpresa = web.CriarCliente(); var usuario = await web.CriarUsuarioAsync();
        await web.LoginAsync(semEmpresa, usuario.Email, usuario.Senha);
        var semContexto = await semEmpresa.GetAsync("/Produtos/Precos/Novo/1"); Assert.Equal(HttpStatusCode.Redirect, semContexto.StatusCode); Assert.Contains("/Conta/Login", semContexto.Headers.Location!.OriginalString);
        var semFicha = await CriarProdutoBasicoAsync(1); using var client = await web.CriarClienteAutenticadoAsync();
        var resposta = await client.GetAsync($"/Produtos/Precos/Novo/{semFicha}"); var token = WebTestHtml.ExtrairTokenAntiforgery(await resposta.Content.ReadAsStringAsync());
        var post = await client.PostAsync($"/Produtos/Precos/Novo/{semFicha}", Form(token, "12")); var html = await post.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, post.StatusCode); Assert.Contains("Precificação incompleta", html); Assert.Contains("value=\"12\"", html); Assert.Empty(await RegistrosAsync(semFicha, 1));
        var semPreco = await CriarProdutoComItemSemPrecoAsync(1); resposta = await client.GetAsync($"/Produtos/Precos/Novo/{semPreco}"); token = WebTestHtml.ExtrairTokenAntiforgery(await resposta.Content.ReadAsStringAsync()); post = await client.PostAsync($"/Produtos/Precos/Novo/{semPreco}", Form(token, "12")); Assert.Equal(HttpStatusCode.OK, post.StatusCode); Assert.Empty(await RegistrosAsync(semPreco, 1));
        var completo = await CriarProdutoPrecificavelAsync(1, true); await DefinirConfiguracaoAsync(1, null, .1m); resposta = await client.GetAsync($"/Produtos/Precos/Novo/{completo}"); token = WebTestHtml.ExtrairTokenAntiforgery(await resposta.Content.ReadAsStringAsync()); post = await client.PostAsync($"/Produtos/Precos/Novo/{completo}", Form(token, "12")); Assert.Equal(HttpStatusCode.OK, post.StatusCode); Assert.Empty(await RegistrosAsync(completo, 1));
    }

    [Fact]
    public async Task W14_W15_W16_W17_W18_W19_Snapshot_usa_estado_vigente_no_post_e_preserva_anterior()
    {
        await using var fabrica = new CustomWebApplicationFactory(new DateOnly(2030, 1, 2)); var teste = new RegistroPrecoProdutoPageTests(fabrica); var produto = await teste.CriarProdutoPrecificavelAsync(1, true);
        using var client = await teste.web.CriarClienteAutenticadoAsync(); var token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}"));
        await teste.AlterarEstadoAsync(produto, margem: .5m, incremento: .5m, reserva: .2m, precoInsumo: 20m);
        var post = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "50")); Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var primeiro = Assert.Single(await teste.RegistrosAsync(produto, 1)); Assert.Equal(new DateOnly(2030, 1, 2), primeiro.DataReferencia); Assert.Equal(20m, primeiro.CustoReferencia); Assert.Equal(.5m, primeiro.MargemReferencia); Assert.Equal(40m, primeiro.PrecoSugerido); Assert.Equal(.2m, primeiro.ReservaComercialReferencia);
        await teste.AlterarEstadoAsync(produto, reserva: .3m); token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}")); await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, "51"));
        var registros = await teste.RegistrosAsync(produto, 1); Assert.Equal(2, registros.Count); Assert.Equal(.2m, registros.Single(r => r.Id == primeiro.Id).ReservaComercialReferencia); Assert.Equal(.3m, registros.Single(r => r.Id != primeiro.Id).ReservaComercialReferencia);
    }

    [Fact]
    public async Task W20_W21_Configuracao_de_outra_empresa_nao_vaza_e_detalhes_tem_acao_em_ambos_status()
    {
        var ativo = await CriarProdutoPrecificavelAsync(1, true); var inativo = await CriarProdutoPrecificavelAsync(1, false); var empresaDois = await web.CriarEmpresaAsync(); await DefinirConfiguracaoAsync(empresaDois, 99m, .9m);
        using var client = await web.CriarClienteAutenticadoAsync(); var token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{ativo}")); await client.PostAsync($"/Produtos/Precos/Novo/{ativo}", Form(token, "15"));
        var registro = Assert.Single(await RegistrosAsync(ativo, 1)); Assert.Equal(.1m, registro.ReservaComercialReferencia);
        Assert.Contains("Registrar preço de prateleira", await client.GetStringAsync($"/Produtos/Detalhes/{ativo}")); Assert.Contains("Registrar preço de prateleira", await client.GetStringAsync($"/Produtos/Detalhes/{inativo}"));
    }

    [Theory]
    [InlineData("20,99", 20.99)]
    [InlineData("20.99", 20.99)]
    public async Task MEL015_Preco_prateleira_aceita_virgula_e_ponto(string informado, decimal esperado)
    {
        var produto = await CriarProdutoPrecificavelAsync(1, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync();

        var token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}"));
        var response = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, informado));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var registro = Assert.Single(await RegistrosAsync(produto, 1));
        Assert.Equal(esperado, registro.PrecoPrateleira);
    }

    [Theory]
    [InlineData("1.234,56")]
    [InlineData("1,234.56")]
    [InlineData("20,99,1")]
    public async Task MEL015_Preco_prateleira_ambiguo_e_rejeitado_preservando_texto(string informado)
    {
        var produto = await CriarProdutoPrecificavelAsync(1, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync();

        var token = WebTestHtml.ExtrairTokenAntiforgery(await client.GetStringAsync($"/Produtos/Precos/Novo/{produto}"));
        var response = await client.PostAsync($"/Produtos/Precos/Novo/{produto}", Form(token, informado));
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("O preço de prateleira deve ser um número válido.", html);
        Assert.Contains($"value=\"{informado}\"", html);
        Assert.Empty(await RegistrosAsync(produto, 1));
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
    private async Task<int> CriarProdutoBasicoAsync(int empresa) { using var s=factory.Services.CreateScope(); var o=s.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(empresa)); var p=Produto.Criar(empresa,Guid.NewGuid().ToString(),.3m); c.Produtos.Add(p); await c.SaveChangesAsync(); return p.Id; }
    private async Task<int> CriarProdutoComItemSemPrecoAsync(int empresa) { using var s=factory.Services.CreateScope(); var o=s.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(empresa)); var p=Produto.Criar(empresa,Guid.NewGuid().ToString(),.3m); c.Produtos.Add(p); await c.SaveChangesAsync(); var f=FichaTecnica.Criar(empresa,p.Id,1m,0); var i=Insumo.Criar(empresa,Guid.NewGuid().ToString(),CategoriaInsumo.MateriaPrima,UnidadeMedida.Unidade); c.FichasTecnicas.Add(f); c.Insumos.Add(i); await c.SaveChangesAsync(); c.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(empresa,f.Id,i.Id,1m,null,0m)); await c.SaveChangesAsync(); return p.Id; }
    private async Task DefinirConfiguracaoAsync(int empresa, decimal? incremento, decimal reserva) { using var s=factory.Services.CreateScope(); var o=s.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(empresa)); var x=await c.ConfiguracoesPrecificacaoEmpresas.SingleAsync(); x.Atualizar(null,null,null,incremento,reserva); await c.SaveChangesAsync(); }
    private async Task AlterarEstadoAsync(int produto, decimal? margem=null, decimal? incremento=null, decimal? reserva=null, decimal? precoInsumo=null) { using var s=factory.Services.CreateScope(); var o=s.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(1)); if(margem.HasValue){var p=await c.Produtos.SingleAsync(x=>x.Id==produto); p.AtualizarDados(p.Nome,margem.Value,p.Categoria);} if(incremento.HasValue||reserva.HasValue){var x=await c.ConfiguracoesPrecificacaoEmpresas.SingleAsync(); x.Atualizar(null,null,null,incremento ?? x.IncrementoComercial,reserva ?? x.ReservaComercialDesconto);} if(precoInsumo.HasValue){var ficha=await c.FichasTecnicas.SingleAsync(x=>x.ProdutoId==produto); var insumo=await c.ItensFichaTecnica.Where(x=>x.FichaTecnicaId==ficha.Id).Select(x=>x.InsumoId).SingleAsync(); c.PrecosInsumos.Add(PrecoInsumo.Criar(1,insumo,1m,precoInsumo.Value,new DateOnly(2030,1,2)));} await c.SaveChangesAsync(); }
    private async Task<List<RegistroPrecoProduto>> RegistrosAsync(int produto, int empresa) { using var scope = factory.Services.CreateScope(); var o=scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(empresa)); return await c.RegistrosPrecosProdutos.Where(r=>r.ProdutoId==produto).ToListAsync(); }
    private async Task<Produto> ProdutoAsync(int id,int empresa) { using var scope=factory.Services.CreateScope(); var o=scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(); await using var c=new PrecificadorDbContext(o,new Contexto(empresa)); return await c.Produtos.SingleAsync(p=>p.Id==id); }
    private sealed class Contexto(int id) : IEmpresaContext { public int? EmpresaId=>id; public int EmpresaIdOuSentinela=>id; public string? TimeZoneId=>Empresa.TimeZoneIdPadrao; }
}
