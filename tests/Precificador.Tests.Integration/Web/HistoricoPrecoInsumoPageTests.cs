using System.Net;
using System.Text;
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

public sealed class HistoricoPrecoInsumoPageTests(HistoricoPrecoInsumoPageTests.Factory fixture) : IClassFixture<HistoricoPrecoInsumoPageTests.Factory>
{
    private static readonly DateOnly DataOperacional = new(2026, 9, 11);
    private readonly CustomWebApplicationFactory factory = fixture.App;
    private readonly WebTestContext web = new(fixture.App);

    [Fact]
    public async Task CA01_Historico_exige_autenticacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Insumos/Precos/Historico/1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task CA02_Historico_exibe_resumo_do_insumo()
    {
        var id = await CriarInsumoAsync(1, Nome("Fita"), "Marca metro", UnidadeMedida.Metro, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await client.GetAsync($"/Insumos/Precos/Historico/{id}");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Contains("Fita", conteudo);
        Assert.Contains("Marca metro", conteudo);
        Assert.Contains("Unidade base", conteudo);
        Assert.Contains(">m<", conteudo);
        Assert.Contains("Situação", conteudo);
        Assert.Contains("Ativo", conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("InsumoId", conteudo);
        Assert.DoesNotContain("NomeNormalizado", conteudo);
        Assert.DoesNotContain("MarcaNormalizada", conteudo);
    }

    [Fact]
    public async Task CA03_CA04_CA05_CA06_Historico_exibe_ordem_e_status_corretos()
    {
        var id = await CriarInsumoAsync(1, Nome("Farinha"), "Renata", UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, id, 1000m, 9m, new DateOnly(2026, 9, 1));
        await CriarPrecoAsync(1, id, 1000m, 10m, DataOperacional);
        await CriarPrecoAsync(1, id, 1000m, 15m, new DateOnly(2026, 9, 20));
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Precos/Historico/{id}"));
        var linhas = LinhasPreco(conteudo);

        Assert.Equal(3, linhas.Count);
        AssertLinha(linhas[0], "20/09/2026", "Futuro");
        AssertLinha(linhas[1], "11/09/2026", "Vigente");
        AssertLinha(linhas[2], "01/09/2026", "Anterior");
    }

    [Fact]
    public async Task CA07_Mesma_data_marca_maior_id_como_vigente()
    {
        var id = await CriarInsumoAsync(1, Nome("Açúcar"), null, UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, id, 1m, 10m, DataOperacional);
        await CriarPrecoAsync(1, id, 1m, 12m, DataOperacional);
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Precos/Historico/{id}"));
        var linhas = LinhasPreco(conteudo);

        AssertLinha(linhas[0], "11/09/2026", "Vigente");
        Assert.Contains("R$ 12,00", linhas[0]);
        AssertLinha(linhas[1], "11/09/2026", "Anterior");
        Assert.Contains("R$ 10,00", linhas[1]);
    }

    [Fact]
    public async Task CA08_Sem_precos_exibe_estado_vazio_sem_custo_zero()
    {
        var id = await CriarInsumoAsync(1, Nome("Sem preço"), null, UnidadeMedida.Unidade, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Precos/Historico/{id}"));

        Assert.Contains("Sem preço vigente.", conteudo);
        Assert.Contains("Nenhum preço registrado para este insumo.", conteudo);
        Assert.DoesNotContain("0,00", conteudo);
        Assert.DoesNotContain("0.00", conteudo);
    }

    [Fact]
    public async Task CA09_Apenas_precos_futuros_exibe_sem_vigente_e_lista_futuros()
    {
        var id = await CriarInsumoAsync(1, Nome("Futuro"), null, UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, id, 1000m, 20m, new DateOnly(2026, 9, 20));
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Precos/Historico/{id}"));
        var linha = Assert.Single(LinhasPreco(conteudo));

        Assert.Contains("Sem preço vigente.", conteudo);
        AssertLinha(linha, "20/09/2026", "Futuro");
        Assert.DoesNotContain("Nenhum preço registrado para este insumo.", conteudo);
    }

    [Fact]
    public async Task CA10_Custo_unitario_preserva_precisao_na_apresentacao()
    {
        var id = await CriarInsumoAsync(1, Nome("Farinha precisa"), null, UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, id, 1000m, 5.39m, DataOperacional);
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Precos/Historico/{id}"));

        Assert.Contains("5,39", conteudo);
        Assert.Contains("1000 g", conteudo);
        Assert.Contains("0,00539", conteudo);
        Assert.Contains("Quantidade por embalagem", conteudo);
        Assert.Contains("Preço por embalagem", conteudo);
        Assert.DoesNotContain("<th>Quantidade</th>", conteudo);
        Assert.DoesNotContain("<th>Preço total</th>", conteudo);
        Assert.DoesNotContain("0,01", conteudo);
    }

    [Fact]
    public async Task CA11_Historico_de_inativo_e_consultavel_e_permite_registrar_novo_preco()
    {
        var id = await CriarInsumoAsync(1, Nome("Inativo"), "Marca", UnidadeMedida.Metro, ativo: false);
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Precos/Historico/{id}"));

        Assert.Contains("Inativo", conteudo);
        Assert.Contains($"/Insumos/Precos/Novo/{id}", conteudo);
        Assert.Contains("Registrar novo preço", conteudo);
    }

    [Fact]
    public async Task CA12_Historico_cross_tenant_retorna_404()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var idOutroTenant = await CriarInsumoAsync(empresaDois, Nome("Segredo"), "Outra", UnidadeMedida.Grama, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await client.GetAsync($"/Insumos/Precos/Historico/{idOutroTenant}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CA13_Detalhes_exibe_link_historico_para_ativo_e_inativo()
    {
        var ativo = await CriarInsumoAsync(1, Nome("Ativo"), null, UnidadeMedida.Grama, ativo: true);
        var inativo = await CriarInsumoAsync(1, Nome("Inativo"), null, UnidadeMedida.Grama, ativo: false);
        using var client = await web.CriarClienteAutenticadoAsync();

        var paginaAtivo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Detalhes/{ativo}"));
        var paginaInativo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Detalhes/{inativo}"));

        Assert.Contains($"/Insumos/Precos/Historico/{ativo}", paginaAtivo);
        Assert.Contains("Histórico de preços", paginaAtivo);
        Assert.Contains($"/Insumos/Precos/Historico/{inativo}", paginaInativo);
        Assert.Contains("Histórico de preços", paginaInativo);
    }

    [Fact]
    public async Task CA14_Detalhes_exibe_preco_vigente_sem_promover_preco_futuro()
    {
        var id = await CriarInsumoAsync(1, Nome("Detalhe vigente"), null, UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, id, 1000m, 10m, DataOperacional);
        await CriarPrecoAsync(1, id, 1000m, 15m, new DateOnly(2026, 9, 20));
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Detalhes/{id}"));

        Assert.Contains("Preço vigente", conteudo);
        Assert.Contains("11/09/2026", conteudo);
        Assert.Contains("R$ 10,00", conteudo);
        Assert.Contains("Quantidade por embalagem", conteudo);
        Assert.Contains("Preço por embalagem", conteudo);
        Assert.DoesNotContain(">Quantidade</dt>", conteudo);
        Assert.DoesNotContain(">Preço total</dt>", conteudo);
        Assert.DoesNotContain("20/09/2026", conteudo);
    }

    [Fact]
    public async Task CA15_Detalhes_sem_preco_vigente_exibe_estado_desconhecido_sem_zero()
    {
        var id = await CriarInsumoAsync(1, Nome("Detalhe futuro"), null, UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, id, 1000m, 15m, new DateOnly(2026, 9, 20));
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await client.GetStringAsync($"/Insumos/Detalhes/{id}"));

        Assert.Contains("Preço vigente", conteudo);
        Assert.Contains("Sem preço vigente.", conteudo);
        Assert.DoesNotContain("20/09/2026", conteudo);
        Assert.DoesNotContain("0,00", conteudo);
        Assert.DoesNotContain("0.00", conteudo);
    }

    private static IReadOnlyList<string> LinhasPreco(string html) =>
        Regex.Matches(html, "<tr>\\s*<td>.*?</tr>", RegexOptions.Singleline)
            .Select(match => match.Value)
            .ToList();

    private static void AssertLinha(string linha, string data, string status)
    {
        Assert.Contains(data, linha);
        Assert.Contains(status, linha);
    }
    private static string Nome(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";
    private async Task<int> CriarInsumoAsync(
        int empresaId,
        string nome,
        string? marca,
        UnidadeMedida unidade,
        bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var insumo = Insumo.Criar(empresaId, nome, CategoriaInsumo.MateriaPrima, unidade, marca);
        if (!ativo)
        {
            insumo.Desativar();
        }

        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        return insumo.Id;
    }

    private async Task CriarPrecoAsync(int empresaId, int insumoId, decimal quantidade, decimal precoCompra, DateOnly data)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, quantidade, precoCompra, data));
        await context.SaveChangesAsync();
    }
    public sealed class Factory : IDisposable
    {
        public CustomWebApplicationFactory App { get; } = new(DataOperacional);

        public void Dispose() => App.Dispose();
    }
}
