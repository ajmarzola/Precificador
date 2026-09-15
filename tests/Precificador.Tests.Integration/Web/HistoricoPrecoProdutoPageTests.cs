using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class HistoricoPrecoProdutoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task W1_Historico_exige_autenticacao_e_empresa_ativa()
    {
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.Redirect, (await anonimo.GetAsync("/Produtos/Precos/Historico/1")).StatusCode);

        using var semEmpresa = web.CriarCliente();
        var usuario = await web.CriarUsuarioAsync();
        await web.LoginAsync(semEmpresa, usuario.Email, usuario.Senha);

        var response = await semEmpresa.GetAsync("/Produtos/Precos/Historico/1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task W2_W3_Historico_exibe_resumo_snapshots_ordem_status_e_navegacao()
    {
        var produto = await CriarProdutoAsync(1, ativo: true, categoria: "Doces");
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 1), custo: 8m, margem: .20m, sugerido: 10m, prateleira: 12m, reserva: .10m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 15), custo: 9m, margem: .25m, sugerido: 20m, prateleira: 22m, reserva: .05m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 15), custo: 10m, margem: .30m, sugerido: 100m, prateleira: 111m, reserva: .10m);
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Precos/Historico/{produto}"));
        var linhas = LinhasPreco(conteudo);

        Assert.Contains("Histórico de precificação do produto", conteudo);
        Assert.Contains("Doces", conteudo);
        Assert.Contains("Ativo", conteudo);
        Assert.Contains("Preço de prateleira atual", conteudo);
        Assert.Contains("111", conteudo);
        Assert.Contains("15/09/2026", conteudo);
        Assert.Equal(3, linhas.Count);
        AssertLinha(linhas[0], "15/09/2026", "Atual", "10", "30%", "100", "111", "10%", "1%");
        AssertLinha(linhas[1], "15/09/2026", "Anterior", "9", "25%", "20", "22", "5%", "5%");
        AssertLinha(linhas[2], "01/09/2026", "Anterior", "8", "20%", "10", "12", "10%", "10%");
        Assert.Contains($"/Produtos/Precos/Novo/{produto}", conteudo);
        Assert.Contains("Registrar novo preço", conteudo);
        Assert.Contains($"/Produtos/Detalhes/{produto}", conteudo);
        Assert.Contains("Voltar ao produto", conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("ProdutoId", conteudo);
    }

    [Fact]
    public async Task W4_Estado_vazio_nao_usa_zero()
    {
        var produto = await CriarProdutoAsync(1, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Precos/Historico/{produto}"));

        Assert.Contains("Preço de prateleira atual: não definido.", conteudo);
        Assert.Contains("Nenhum preço de prateleira registrado para este produto.", conteudo);
        Assert.Empty(LinhasPreco(conteudo));
        Assert.DoesNotContain("0,00", conteudo);
        Assert.DoesNotContain("0.00", conteudo);
    }

    [Fact]
    public async Task W5_W6_Produto_inativo_e_consultavel_e_cross_tenant_retorna_404()
    {
        var inativo = await CriarProdutoAsync(1, ativo: false);
        await CriarRegistroAsync(1, inativo, new DateOnly(2026, 9, 15), 10m, .30m, 100m, 111m, .10m);
        var empresaDois = await web.CriarEmpresaAsync();
        var outroTenant = await CriarProdutoAsync(empresaDois, ativo: true);
        await CriarRegistroAsync(empresaDois, outroTenant, new DateOnly(2026, 9, 15), 10m, .30m, 100m, 111m, .10m);
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Precos/Historico/{inativo}"));
        var crossTenant = await client.GetAsync($"/Produtos/Precos/Historico/{outroTenant}");

        Assert.Contains("Inativo", conteudo);
        Assert.Contains($"/Produtos/Precos/Novo/{inativo}", conteudo);
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
    }

    [Fact]
    public async Task W7_W8_W9_Detalhes_exibe_link_e_preco_atual_para_ativo_inativo_e_estado_sem_registro()
    {
        var ativo = await CriarProdutoAsync(1, ativo: true);
        var inativo = await CriarProdutoAsync(1, ativo: false);
        var semRegistro = await CriarProdutoAsync(1, ativo: true);
        await CriarRegistroAsync(1, ativo, new DateOnly(2026, 9, 1), 10m, .30m, 100m, 120m, .10m);
        await CriarRegistroAsync(1, ativo, new DateOnly(2026, 9, 15), 10m, .30m, 100m, 111m, .10m);
        await CriarRegistroAsync(1, ativo, new DateOnly(2026, 9, 15), 10m, .30m, 100m, 112m, .10m);
        using var client = await web.CriarClienteAutenticadoAsync();

        var paginaAtivo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{ativo}"));
        var paginaInativo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{inativo}"));
        var paginaSemRegistro = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{semRegistro}"));

        Assert.Contains($"/Produtos/Precos/Historico/{ativo}", paginaAtivo);
        Assert.Contains("Histórico de precificação", paginaAtivo);
        Assert.Contains($"/Produtos/Precos/Historico/{inativo}", paginaInativo);
        Assert.Contains("Histórico de precificação", paginaInativo);
        Assert.Contains("Preço de prateleira atual", paginaAtivo);
        Assert.Contains("15/09/2026", paginaAtivo);
        Assert.Contains("112", paginaAtivo);
        Assert.DoesNotContain("01/09/2026", paginaAtivo);
        Assert.Contains("Preço de prateleira atual: não definido.", paginaSemRegistro);
        Assert.DoesNotContain("0,00", paginaSemRegistro);
    }

    [Fact]
    public async Task W10_W11_W12_W14_W15_Historico_deriva_desconto_pelo_snapshot_sem_arredondar()
    {
        var produto = await CriarProdutoAsync(1, ativo: true);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 1), 10m, .30m, 100m, 110.99m, .10m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 2), 10m, .30m, 100m, 111m, .10m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 3), 10m, .30m, 100m, 105.99m, .05m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 4), 10m, .30m, 100m, 106m, .05m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 5), 10m, .30m, 100m, 99m, .10m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 6), 10m, .30m, 0m, 100m, .10m);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 7), 10m, .30m, 100m, 110.999999m, .10m);
        using var client = await web.CriarClienteAutenticadoAsync();

        var linhas = LinhasPreco(await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Precos/Historico/{produto}")));

        AssertLinha(linhas[0], "07/09/2026", "Não aplicável");
        AssertLinha(linhas[1], "06/09/2026", "Não aplicável");
        AssertLinha(linhas[2], "05/09/2026", "Não aplicável");
        AssertLinha(linhas[3], "04/09/2026", "106", "1%");
        AssertLinha(linhas[4], "03/09/2026", "Não aplicável");
        AssertLinha(linhas[5], "02/09/2026", "111", "1%");
        AssertLinha(linhas[6], "01/09/2026", "Não aplicável");
    }

    [Fact]
    public async Task W13_W17_Reserva_atual_ou_ausente_nao_reinterpreta_historico()
    {
        var empresa = await web.CriarEmpresaAsync();
        var produto = await CriarProdutoAsync(empresa, ativo: true);
        await CriarRegistroAsync(empresa, produto, new DateOnly(2026, 9, 1), 10m, .30m, 100m, 111m, .10m);
        await AlterarReservaAtualAsync(empresa, .90m);
        await RemoverConfiguracaoAsync(empresa);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Precos/Historico/{produto}"));
        var linha = Assert.Single(LinhasPreco(conteudo));

        AssertLinha(linha, "10%", "1%");
    }

    [Fact]
    public async Task W16_Historico_e_somente_leitura()
    {
        var produto = await CriarProdutoAsync(1, ativo: true);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 15), 10m, .30m, 100m, 111m, .10m);
        using var client = await web.CriarClienteAutenticadoAsync();

        await client.GetAsync($"/Produtos/Precos/Historico/{produto}");
        await client.GetAsync($"/Produtos/Precos/Historico/{produto}");

        Assert.Equal(1, await ContarRegistrosAsync(1, produto));
    }

    [Fact]
    public async Task W18_W19_Historico_independe_da_precificacao_atual()
    {
        var produto = await CriarProdutoAsync(1, ativo: true);
        await CriarRegistroAsync(1, produto, new DateOnly(2026, 9, 15), 12.3456m, .30m, 14.29m, 20m, .10m);
        await AlterarProdutoAsync(1, produto, margem: .60m, categoria: "Atual alterada");
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Precos/Historico/{produto}"));
        var linha = Assert.Single(LinhasPreco(conteudo));

        AssertLinha(linha, "12,3456", "30%", "14,29", "20");
        Assert.Contains("Atual alterada", conteudo);
        Assert.DoesNotContain("60%", linha);
    }

    private static IReadOnlyList<string> LinhasPreco(string html) =>
        Regex.Matches(html, "<tr>\\s*<td>.*?</tr>", RegexOptions.Singleline)
            .Select(match => match.Value)
            .ToList();

    private static void AssertLinha(string linha, params string[] valores)
    {
        foreach (var valor in valores)
        {
            Assert.Contains(valor, linha);
        }
    }

    private async Task<int> CriarProdutoAsync(int empresaId, bool ativo, string? categoria = null)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", .30m, categoria);
        if (!ativo)
        {
            produto.Desativar();
        }

        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private async Task CriarRegistroAsync(
        int empresaId,
        int produtoId,
        DateOnly data,
        decimal custo,
        decimal margem,
        decimal sugerido,
        decimal prateleira,
        decimal reserva)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(empresaId, produtoId, data, custo, margem, sugerido, prateleira, reserva));
        await context.SaveChangesAsync();
    }

    private async Task AlterarReservaAtualAsync(int empresaId, decimal reserva)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(null, null, null, .01m, reserva);
        await context.SaveChangesAsync();
    }

    private async Task RemoverConfiguracaoAsync(int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        context.ConfiguracoesPrecificacaoEmpresas.RemoveRange(context.ConfiguracoesPrecificacaoEmpresas);
        await context.SaveChangesAsync();
    }

    private async Task AlterarProdutoAsync(int empresaId, int produtoId, decimal margem, string? categoria)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = await context.Produtos.SingleAsync(produto => produto.Id == produtoId);
        produto.AtualizarDados(produto.Nome, margem, categoria);
        await context.SaveChangesAsync();
    }

    private async Task<int> ContarRegistrosAsync(int empresaId, int produtoId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.RegistrosPrecosProdutos.CountAsync(registro => registro.ProdutoId == produtoId);
    }
}
