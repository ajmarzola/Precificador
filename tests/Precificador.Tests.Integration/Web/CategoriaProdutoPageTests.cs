using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

/// <summary>
/// Cobre a administração de Categorias de Produto (UC032): /Produtos/Categorias, /Novo e /Editar/{id},
/// além dos cenários do seletor de Categoria em Produtos/Novo e Produtos/Editar que envolvem Categoria inativa
/// e isolamento cross-tenant (complementa EditarProdutoPageTests.cs e ProdutoPageTests.cs).
/// </summary>
public sealed class CategoriaProdutoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task W1_Index_exige_autenticacao_e_lista_apenas_categorias_da_empresa_ativa_incluindo_inativas()
    {
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await anonimo.GetAsync("/Produtos/Categorias")).StatusCode);

        var ativaId = await CriarCategoriaAsync(1, NomeUnico("Papelaria"), ativo: true);
        var inativaId = await CriarCategoriaAsync(1, NomeUnico("Descontinuada"), ativo: false);
        var empresaDois = await web.CriarEmpresaAsync();
        var outroTenant = NomeUnico("Externa");
        await CriarCategoriaAsync(empresaDois, outroTenant, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos/Categorias"));

        Assert.Contains(await ObterNomeAsync(ativaId), conteudo);
        Assert.Contains(await ObterNomeAsync(inativaId), conteudo);
        Assert.Contains("Ativo", conteudo);
        Assert.Contains("Inativo", conteudo);
        Assert.DoesNotContain(outroTenant, conteudo);
    }

    [Fact]
    public async Task W2_Cadastro_valido_persiste_categoria_ativa_e_faz_PRG()
    {
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var nome = NomeUnico("Agendas");

        var response = await EnviarNovoAsync(client, $"  {nome}  ");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Produtos/Categorias", response.Headers.Location!.ToString());
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var categoria = await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(item => item.Nome == nome);
        Assert.Equal(1, categoria.EmpresaId);
        Assert.True(categoria.Ativo);
        Assert.Equal(nome.ToUpperInvariant(), categoria.NomeNormalizado);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Categoria de produto cadastrada com sucesso.", pagina);
        Assert.Contains(nome, pagina);
    }

    [Fact]
    public async Task W3_Cadastro_duplicado_case_insensitive_exibe_mensagem_amigavel_e_nao_persiste_segunda_categoria()
    {
        var nome = NomeUnico("Planners");
        await CriarCategoriaAsync(1, nome, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarNovoAsync(client, nome.ToUpperInvariant());
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe uma categoria de produto cadastrada com esse nome.", conteudo);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(1, await context.CategoriasProdutos.IgnoreQueryFilters().CountAsync(item => item.NomeNormalizado == nome.ToUpperInvariant()));
    }

    [Fact]
    public async Task W4_Cadastro_invalido_preserva_input_e_nao_persiste()
    {
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var quantidadeAntes = await ContarCategoriasFisicasAsync();

        var response = await EnviarNovoAsync(client, "   ");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal(quantidadeAntes, await ContarCategoriasFisicasAsync());
    }

    [Fact]
    public async Task W5_Edicao_valida_renomeia_e_preserva_situacao()
    {
        var id = await CriarCategoriaAsync(1, NomeUnico("Nome antigo"), ativo: false);
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var novoNome = NomeUnico("Nome novo");

        var response = await EnviarEdicaoAsync(client, id, $"  {novoNome}  ");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Produtos/Categorias", response.Headers.Location!.ToString());
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var categoria = await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(item => item.Id == id);
        Assert.Equal(novoNome, categoria.Nome);
        Assert.Equal(novoNome.ToUpperInvariant(), categoria.NomeNormalizado);
        Assert.False(categoria.Ativo);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Categoria de produto atualizada com sucesso.", pagina);
    }

    [Fact]
    public async Task W6_Edicao_cross_tenant_retorna_404_sem_alterar_registro()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var nomeOriginal = NomeUnico("Categoria outro tenant");
        var idOutroTenant = await CriarCategoriaAsync(empresaDois, nomeOriginal, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/Categorias/Editar/{idOutroTenant}");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var categoria = await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(item => item.Id == idOutroTenant);
        Assert.Equal(nomeOriginal, categoria.Nome);
    }

    [Fact]
    public async Task W7_Desativar_persiste_inatividade_sem_desvincular_produtos()
    {
        var categoriaId = await CriarCategoriaAsync(1, NomeUnico("Categoria a desativar"), ativo: true);
        var produtoId = await CriarProdutoComCategoriaAsync(1, NomeUnico("Produto vinculado"), categoriaId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await PostComTokenAsync(client, categoriaId, "Desativar");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.False((await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(item => item.Id == categoriaId)).Ativo);
        var produto = await context.Produtos.IgnoreQueryFilters().SingleAsync(item => item.Id == produtoId);
        Assert.Equal(categoriaId, produto.CategoriaProdutoId);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Categoria de produto desativada com sucesso.", pagina);
    }

    [Fact]
    public async Task W8_Reativar_persiste_atividade()
    {
        var categoriaId = await CriarCategoriaAsync(1, NomeUnico("Categoria a reativar"), ativo: false);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await PostComTokenAsync(client, categoriaId, "Reativar");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.True((await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(item => item.Id == categoriaId)).Ativo);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Categoria de produto reativada com sucesso.", pagina);
    }

    [Theory]
    [InlineData("Desativar", true)]
    [InlineData("Reativar", false)]
    public async Task W9_Post_sem_antiforgery_e_rejeitado_sem_alterar_estado(string handler, bool ativo)
    {
        var categoriaId = await CriarCategoriaAsync(1, NomeUnico("Categoria antiforgery"), ativo);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.PostAsync(
            $"/Produtos/Categorias?handler={handler}&id={categoriaId}",
            new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(ativo, (await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(item => item.Id == categoriaId)).Ativo);
    }

    [Fact]
    public async Task W15_W16_Editar_produto_preserva_categoria_inativa_atual_selecionada_e_permite_mante_la()
    {
        var categoriaId = await CriarCategoriaAsync(1, NomeUnico("Categoria inativa atual"), ativo: false);
        var produtoId = await CriarProdutoComCategoriaAsync(1, NomeUnico("Produto com categoria inativa"), categoriaId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Editar/{produtoId}"));
        Assert.Contains("selected", OpcaoDoValor(conteudo, categoriaId));

        var response = await EnviarEdicaoProdutoAsync(client, produtoId, NomeUnico("Produto renomeado"), categoriaId, "30");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(categoriaId, (await context.Produtos.IgnoreQueryFilters().SingleAsync(item => item.Id == produtoId)).CategoriaProdutoId);
    }

    [Fact]
    public async Task W17_Editar_produto_pode_remover_categoria()
    {
        var categoriaId = await CriarCategoriaAsync(1, NomeUnico("Categoria a remover"), ativo: true);
        var produtoId = await CriarProdutoComCategoriaAsync(1, NomeUnico("Produto remove categoria"), categoriaId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarEdicaoProdutoAsync(client, produtoId, NomeUnico("Produto sem categoria"), null, "30");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Null((await context.Produtos.IgnoreQueryFilters().SingleAsync(item => item.Id == produtoId)).CategoriaProdutoId);
    }

    [Fact]
    public async Task W19_Editar_produto_nao_pode_trocar_para_outra_categoria_inativa()
    {
        var categoriaAtualId = await CriarCategoriaAsync(1, NomeUnico("Categoria atual inativa"), ativo: false);
        var outraCategoriaInativaId = await CriarCategoriaAsync(1, NomeUnico("Outra categoria inativa"), ativo: false);
        var produtoId = await CriarProdutoComCategoriaAsync(1, NomeUnico("Produto troca invalida"), categoriaAtualId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarEdicaoProdutoAsync(client, produtoId, NomeUnico("Produto sem troca"), outraCategoriaInativaId, "30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(categoriaAtualId, (await context.Produtos.IgnoreQueryFilters().SingleAsync(item => item.Id == produtoId)).CategoriaProdutoId);
    }

    [Fact]
    public async Task W20_Post_invalido_recarrega_opcoes_do_seletor_preservando_selecao()
    {
        var categoriaId = await CriarCategoriaAsync(1, NomeUnico("Categoria preservada"), ativo: true);
        var produtoId = await CriarProdutoComCategoriaAsync(1, NomeUnico("Produto post invalido"), categoriaId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarEdicaoProdutoAsync(client, produtoId, "   ", categoriaId, "30");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("selected", OpcaoDoValor(conteudo, categoriaId));
    }

    [Fact]
    public async Task W24_Seletor_de_categoria_nao_expoe_categoria_de_outro_tenant()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var categoriaOutroTenant = NomeUnico("Categoria outro tenant seletor");
        var categoriaOutroTenantId = await CriarCategoriaAsync(empresaDois, categoriaOutroTenant, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos/Novo"));
        Assert.DoesNotContain(categoriaOutroTenant, conteudo);

        var produtoId = await CriarProdutoComCategoriaAsync(1, NomeUnico("Produto tenant um"), null);
        var response = await EnviarEdicaoProdutoAsync(client, produtoId, NomeUnico("Produto tenta cross tenant"), categoriaOutroTenantId, "30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Null((await context.Produtos.IgnoreQueryFilters().SingleAsync(item => item.Id == produtoId)).CategoriaProdutoId);
    }

    private async Task<HttpResponseMessage> EnviarNovoAsync(HttpClient client, string nome)
    {
        var respostaPagina = await client.GetAsync("/Produtos/Categorias/Novo");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);

        return await client.PostAsync("/Produtos/Categorias/Novo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina),
            ["Input.Nome"] = nome
        }));
    }

    private async Task<HttpResponseMessage> EnviarEdicaoAsync(HttpClient client, int id, string nome)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/Categorias/Editar/{id}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);

        return await client.PostAsync($"/Produtos/Categorias/Editar/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina),
            ["Input.Nome"] = nome
        }));
    }

    private async Task<HttpResponseMessage> PostComTokenAsync(HttpClient client, int id, string handler)
    {
        var pagina = await (await client.GetAsync("/Produtos/Categorias")).Content.ReadAsStringAsync();
        var token = WebTestHtml.ExtrairTokenAntiforgery(pagina);

        return await client.PostAsync($"/Produtos/Categorias?handler={handler}&id={id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
    }

    private async Task<HttpResponseMessage> EnviarEdicaoProdutoAsync(HttpClient client, int id, string nome, int? categoriaProdutoId, string margemPercentual)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/Editar/{id}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);

        return await client.PostAsync($"/Produtos/Editar/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina),
            ["Input.Nome"] = nome,
            ["Input.CategoriaProdutoId"] = categoriaProdutoId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["Input.MargemAlvoPercentual"] = margemPercentual
        }));
    }

    private async Task<int> CriarCategoriaAsync(int empresaId, string nome, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var categoria = CategoriaProduto.Criar(empresaId, nome);
        if (!ativo)
        {
            categoria.Desativar();
        }

        context.CategoriasProdutos.Add(categoria);
        await context.SaveChangesAsync();
        return categoria.Id;
    }

    private async Task<int> CriarProdutoComCategoriaAsync(int empresaId, string nome, int? categoriaProdutoId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, nome, 0.30m, categoriaProdutoId);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private async Task<string> ObterNomeAsync(int categoriaId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return (await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(item => item.Id == categoriaId)).Nome;
    }

    private async Task<int> ContarCategoriasFisicasAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return await context.CategoriasProdutos.IgnoreQueryFilters().CountAsync();
    }

    private static string OpcaoDoValor(string conteudo, int id)
    {
        var match = Regex.Match(conteudo, $"<option[^>]*value=\"{id}\"[^>]*>.*?</option>", RegexOptions.Singleline);
        Assert.True(match.Success, conteudo);
        return match.Value;
    }

    private static string NomeUnico(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";
}
