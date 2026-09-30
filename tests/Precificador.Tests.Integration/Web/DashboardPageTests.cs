using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class DashboardPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task CA01_CA02_Dashboard_exige_autenticacao_e_empresa_ativa()
    {
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resposta = await anonimo.GetAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Conta/Login", resposta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CA05_a_CA26_Resume_ativos_com_valores_atuais_e_ordem_por_nome()
    {
        var dentro = await CriarProdutoPrecificavelAsync(1, "Zeta dentro", 10m, 20m, ativo: true);
        var abaixo = await CriarProdutoPrecificavelAsync(1, "Alfa abaixo", 10m, 5m, ativo: true);
        var inativo = await CriarProdutoPrecificavelAsync(1, "Inativo", 10m, 20m, ativo: false);
        var semMargem = await CriarProdutoSemFichaAsync(1, "Meio sem margem", 20m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Dashboard"));

        Assert.Contains("Produtos ativos</div><strong>3", conteudo);
        Assert.Contains("Abaixo da margem</div><strong>1", conteudo);
        Assert.Contains("Dentro da margem</div><strong>1", conteudo);
        Assert.Contains("Margem indisponível</div><strong>1", conteudo);
        Assert.Contains("R$ 10,00", LinhaProduto(conteudo, dentro.Nome));
        Assert.Contains("R$ 14,50", LinhaProduto(conteudo, dentro.Nome));
        Assert.Contains("50%", LinhaProduto(conteudo, dentro.Nome));
        Assert.Contains("Dentro da margem", LinhaProduto(conteudo, dentro.Nome));
        Assert.Contains("-100%", LinhaProduto(conteudo, abaixo.Nome));
        Assert.Contains("Abaixo da margem", LinhaProduto(conteudo, abaixo.Nome));
        Assert.Contains("indisponível", LinhaProduto(conteudo, semMargem.Nome));
        Assert.Contains("Margem indisponível", LinhaProduto(conteudo, semMargem.Nome));
        Assert.Contains($"/Produtos/Detalhes/{dentro.Id}", LinhaProduto(conteudo, dentro.Nome));
        Assert.DoesNotContain(inativo.Nome, conteudo);
        Assert.True(conteudo.IndexOf(abaixo.Nome, StringComparison.Ordinal) < conteudo.IndexOf(semMargem.Nome, StringComparison.Ordinal));
        Assert.True(conteudo.IndexOf(semMargem.Nome, StringComparison.Ordinal) < conteudo.IndexOf(dentro.Nome, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CA08_CA25_Conta_insumos_do_tenant_e_incremento_ausente_nao_invalida_margem()
    {
        var produto = await CriarProdutoPrecificavelAsync(1, "Produto incremento ausente", 10m, 20m, ativo: true, incrementoComercial: null);
        await CriarProdutoPrecificavelAsync(1, "Produto inativo", 10m, 20m, ativo: false, incrementoComercial: null);
        var empresaExterna = await web.CriarEmpresaAsync();
        var externo = await CriarProdutoPrecificavelAsync(empresaExterna, "Produto externo", 10m, 20m, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Dashboard"));
        var linha = LinhaProduto(conteudo, produto.Nome);

        Assert.Contains("Insumos ativos</div><strong>2", conteudo);
        Assert.Contains("indisponível", linha);
        Assert.Contains("50%", linha);
        Assert.Contains("Dentro da margem", linha);
        Assert.DoesNotContain(externo.Nome, conteudo);
    }

    [Fact]
    public async Task CA12_CA35_Estado_vazio_nao_escreve_e_mantem_contagem_de_insumos()
    {
        var empresa = await web.CriarEmpresaAsync();
        await CriarInsumoAsync(empresa, "Insumo ativo", ativo: true);
        await CriarInsumoAsync(empresa, "Insumo inativo", ativo: false);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Dashboard"));

        Assert.Contains("Produtos ativos</div><strong>0", conteudo);
        Assert.Contains("Insumos ativos</div><strong>1", conteudo);
        Assert.Contains("Abaixo da margem</div><strong>0", conteudo);
        Assert.Contains("Dentro da margem</div><strong>0", conteudo);
        Assert.Contains("Margem indisponível</div><strong>0", conteudo);
        Assert.Contains("Não há Produtos ativos para acompanhar.", conteudo);
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var verificacao = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresa));
        Assert.Equal(0, await verificacao.Produtos.CountAsync());
        Assert.Equal(2, await verificacao.Insumos.CountAsync());
    }

    private async Task<(int Id, string Nome)> CriarProdutoPrecificavelAsync(int empresaId, string nome, decimal custo, decimal preco, bool ativo, decimal? incrementoComercial = .50m)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(0m, 0m, null, incrementoComercial, .10m);
        var produto = Produto.Criar(empresaId, nome, .30m);
        if (!ativo) produto.Desativar();
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(empresaId, produto.Id, 1m);
        var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade);
        context.FichasTecnicas.Add(ficha);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(empresaId, ficha.Id, insumo.Id, 1m));
        context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumo.Id, 1m, custo, new DateOnly(2026, 9, 30)));
        context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(empresaId, produto.Id, new DateOnly(2026, 9, 30), custo, .30m, custo, preco, .10m));
        await context.SaveChangesAsync();
        return (produto.Id, nome);
    }

    private async Task<(int Id, string Nome)> CriarProdutoSemFichaAsync(int empresaId, string nome, decimal preco)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, nome, .30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(empresaId, produto.Id, new DateOnly(2026, 9, 30), 10m, .30m, 14.50m, preco, .10m));
        await context.SaveChangesAsync();
        return (produto.Id, nome);
    }

    private async Task CriarInsumoAsync(int empresaId, string nome, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var insumo = Insumo.Criar(empresaId, nome, CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade);
        if (!ativo) insumo.Desativar();
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
    }

    private static string LinhaProduto(string conteudo, string nome)
    {
        var match = Regex.Match(conteudo, $"<tr>.*?{Regex.Escape(nome)}.*?</tr>", RegexOptions.Singleline);
        Assert.True(match.Success, conteudo);
        return match.Value;
    }
}
