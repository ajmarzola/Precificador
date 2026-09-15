using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class FichaTecnicaCustoPageTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 11);

    [Fact]
    public async Task UC020_W1_W2_W3_W4_W5_Exibe_mao_de_obra_com_semantica_null_zero_e_fracao()
    {
        await using var ambiente = await CriarAmbienteAsync();
        await ambiente.DefinirValorHoraAsync(1, 40m);
        var normal = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, normal, tempo: 60);
        var fracao = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, fracao, tempo: 30);

        AssertExibeCustoMaoDeObra(await ambiente.ObterFichaAsync(normal), "40");
        AssertExibeCustoMaoDeObra(await ambiente.ObterFichaAsync(fracao), "20");

        await ambiente.DefinirValorHoraAsync(1, null);
        var semTempo = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, semTempo, tempo: 0);
        var semConfiguracao = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, semConfiguracao, tempo: 1);

        AssertExibeCustoMaoDeObra(await ambiente.ObterFichaAsync(semTempo), "0");
        var indisponivel = await ambiente.ObterFichaAsync(semConfiguracao);
        AssertExibeCustoMaoDeObra(indisponivel, "indisponível");
        Assert.Contains("Valor da hora de trabalho não configurado.", indisponivel);

        await ambiente.DefinirValorHoraAsync(1, 0m);
        AssertExibeCustoMaoDeObra(await ambiente.ObterFichaAsync(semConfiguracao), "0");
    }

    [Fact]
    public async Task UC020_W6_W7_W8_Reflete_configuracao_e_permanece_independente_de_status_e_uc018()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: false);
        await ambiente.CriarFichaAsync(1, produto, tempo: 30);
        await ambiente.DefinirValorHoraAsync(1, 10m);

        var primeira = await ambiente.ObterFichaAsync(produto);
        AssertExibeCustoMaoDeObra(primeira, "5");
        Assert.Contains("Custo base dos itens:", primeira);
        Assert.Contains("indisponível", primeira);
        Assert.Contains("Inativo", primeira);

        await ambiente.DefinirValorHoraAsync(1, 12m);
        AssertExibeCustoMaoDeObra(await ambiente.ObterFichaAsync(produto), "6");
        Assert.False(await ambiente.ProdutoAtivoAsync(produto, 1));
    }

    [Fact]
    public async Task UC020_W9_W10_Configuracao_de_outro_tenant_nao_vaza_e_ausente_retorna_404_sem_criar()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, produto, tempo: 60);
        await ambiente.DefinirValorHoraAsync(1, 10m);
        var empresaDois = await ambiente.CriarEmpresaAsync();
        await ambiente.DefinirValorHoraAsync(empresaDois, 99m);

        AssertExibeCustoMaoDeObra(await ambiente.ObterFichaAsync(produto), "10");

        await ambiente.RemoverConfiguracaoAsync(1);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync($"/Produtos/FichaTecnica/{produto}")).StatusCode);
        Assert.Equal(0, await ambiente.ContarConfiguracoesAsync(1));
    }

    [Fact]
    public async Task UC020_W11_W12_Get_nao_persiste_custo_e_post_invalido_usa_tempo_persistido()
    {
        await using var ambiente = await CriarAmbienteAsync();
        await ambiente.DefinirValorHoraAsync(1, 60m);
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m, tempo: 30);
        var antes = await ambiente.ObterEstadoFichaAsync(ficha, 1);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        var token = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync());
        var post = await client.PostAsync($"/Produtos/FichaTecnica/{produto}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Rendimento"] = "0",
            ["Input.TempoAtivoMinutos"] = "120"
        }));
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(post);

        AssertExibeCustoMaoDeObra(html, "30");
        Assert.Contains("value=\"120\"", html);
        Assert.Equal(antes, await ambiente.ObterEstadoFichaAsync(ficha, 1));
    }

    [Fact]
    public async Task UC020_W13_W14_W15_Post_valido_faz_prg_recalcula_e_formatacao_nao_perde_precisao()
    {
        await using var ambiente = await CriarAmbienteAsync();
        await ambiente.DefinirValorHoraAsync(1, 10m);
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var semFicha = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, produto, rendimento: 2m, tempo: 1);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        var token = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync());
        var post = await client.PostAsync($"/Produtos/FichaTecnica/{produto}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Rendimento"] = "2",
            ["Input.TempoAtivoMinutos"] = "30"
        }));

        Assert.Equal(System.Net.HttpStatusCode.Redirect, post.StatusCode);
        AssertExibeCustoMaoDeObra(await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(post.Headers.Location!)), "5");
        Assert.DoesNotContain("Custo de mão de obra do lote", await ambiente.ObterFichaAsync(semFicha));

        var umMinuto = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, umMinuto, tempo: 1);
        AssertExibeCustoMaoDeObra(await ambiente.ObterFichaAsync(umMinuto), "0,1667");
    }

    [Fact]
    public async Task W1_W2_W14_Exibe_custos_e_total_com_precisao_pt_br()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var primeiro = await ambiente.CriarInsumoAsync(1, ativo: true);
        var segundo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, primeiro, 37m, "Observação contextual");
        await ambiente.CriarItemAsync(1, ficha, segundo, 2m);
        await ambiente.CriarPrecoAsync(1, primeiro, 1000m, 13.579m, Hoje);
        await ambiente.CriarPrecoAsync(1, segundo, 1m, 2m, Hoje);

        var html = await ambiente.ObterFichaAsync(produto);

        Assert.Contains("Custo unitário", html);
        Assert.Contains("Custo do item", html);
        Assert.Contains("0,013579", html);
        Assert.Contains("0,5024", html);
        Assert.Contains("Custo base dos itens:", html);
        Assert.Contains("4,5024", html);
        Assert.Contains("Observação contextual", html);
        Assert.Contains("Editar", html);
        Assert.Contains("Remover", html);
    }

    [Fact]
    public async Task W3_W5_Preco_futuro_ou_apenas_futuro_nao_participa()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var comHistorico = await ambiente.CriarInsumoAsync(1, ativo: true);
        var apenasFuturo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, comHistorico, 1m);
        await ambiente.CriarItemAsync(1, ficha, apenasFuturo, 1m);
        await ambiente.CriarPrecoAsync(1, comHistorico, 1m, 3m, Hoje.AddDays(-1));
        await ambiente.CriarPrecoAsync(1, comHistorico, 1m, 99m, Hoje.AddDays(1));
        await ambiente.CriarPrecoAsync(1, apenasFuturo, 1m, 88m, Hoje.AddDays(1));

        var html = await ambiente.ObterFichaAsync(produto);

        Assert.Contains("3", html);
        Assert.Contains("Sem preço vigente", html);
        Assert.DoesNotContain(">99<", html);
        Assert.DoesNotContain(">88<", html);
        Assert.Contains("indisponível", html);
        Assert.Contains("Há item(ns) sem preço vigente.", html);
    }

    [Fact]
    public async Task W4_W6_W9_W10_Item_sem_preco_mantem_custos_conhecidos_e_inativos_calculaveis()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: false);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var inativoComPreco = await ambiente.CriarInsumoAsync(1, ativo: false);
        var semPreco = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, inativoComPreco, 2m);
        await ambiente.CriarItemAsync(1, ficha, semPreco, 1m);
        await ambiente.CriarPrecoAsync(1, inativoComPreco, 1m, 4m, Hoje);

        var html = await ambiente.ObterFichaAsync(produto);

        Assert.Contains("Inativo", html);
        Assert.Contains("8", html);
        Assert.Contains("Sem preço vigente", html);
        Assert.Contains("—", html);
        Assert.Contains("indisponível", html);
        Assert.False(await ambiente.ProdutoAtivoAsync(produto, 1));
        Assert.False(await ambiente.InsumoAtivoAsync(inativoComPreco, 1));
    }

    [Fact]
    public async Task W7_W8_W12_Ficha_vazia_e_produto_sem_ficha_nao_apresentam_zero_nem_mutam()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var comFicha = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, comFicha);
        var semFicha = await ambiente.CriarProdutoAsync(1, ativo: true);
        var antes = await ambiente.ContarFichasAsync(1);

        var vazia = await ambiente.ObterFichaAsync(comFicha);
        var ausente = await ambiente.ObterFichaAsync(semFicha);

        Assert.Contains("Custo base dos itens:", vazia);
        Assert.Contains("indisponível", vazia);
        Assert.Contains("A ficha não possui itens.", vazia);
        Assert.DoesNotContain("Custo base dos itens", ausente);
        Assert.Equal(antes, await ambiente.ContarFichasAsync(1));
    }

    [Fact]
    public async Task W11_W15_Tenant_nao_vaza_preco_e_post_invalido_preserva_calculo_sem_persistir()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m, tempo: 30);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, insumo, 2m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 5m, Hoje);
        var empresaDois = await ambiente.CriarEmpresaAsync();
        var externo = await ambiente.CriarInsumoAsync(empresaDois, ativo: true);
        await ambiente.CriarPrecoAsync(empresaDois, externo, 1m, 999m, Hoje);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        var token = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync());
        var post = await client.PostAsync($"/Produtos/FichaTecnica/{produto}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Rendimento"] = "0",
            ["Input.TempoAtivoMinutos"] = "30"
        }));
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(post);

        Assert.DoesNotContain("999", html);
        Assert.Contains("10", html);
        Assert.Contains("O rendimento deve ser maior que zero.", html);
        Assert.Equal(2m, await ambiente.RendimentoAsync(ficha, 1));
    }

    private static async Task<Ambiente> CriarAmbienteAsync()
    {
        var factory = new CustomWebApplicationFactory(Hoje);
        _ = factory.Services;
        return await Task.FromResult(new Ambiente(factory));
    }

    private static void AssertExibeCustoMaoDeObra(string html, string valor) =>
        Assert.Matches(
            $"Custo de mão de obra do lote:</strong>\\s*{Regex.Escape(valor)}",
            html);

    private sealed class Ambiente(CustomWebApplicationFactory factory) : IAsyncDisposable
    {
        private readonly IServiceScope scope = factory.Services.CreateScope();

        public WebTestContext Web { get; } = new(factory);

        public async Task<int> CriarEmpresaAsync() => await Web.CriarEmpresaAsync();

        public async Task<int> CriarProdutoAsync(int empresaId, bool ativo)
        {
            await using var context = CriarContexto(empresaId);
            var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", 0.3m);
            if (!ativo) produto.Desativar();
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
            return produto.Id;
        }

        public async Task<int> CriarFichaAsync(int empresaId, int produtoId, decimal rendimento = 1m, int tempo = 0)
        {
            await using var context = CriarContexto(empresaId);
            var ficha = FichaTecnica.Criar(empresaId, produtoId, rendimento, tempo);
            context.FichasTecnicas.Add(ficha);
            await context.SaveChangesAsync();
            return ficha.Id;
        }

        public async Task<int> CriarInsumoAsync(int empresaId, bool ativo)
        {
            await using var context = CriarContexto(empresaId);
            var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
            if (!ativo) insumo.Desativar();
            context.Insumos.Add(insumo);
            await context.SaveChangesAsync();
            return insumo.Id;
        }

        public async Task CriarItemAsync(int empresaId, int fichaId, int insumoId, decimal quantidade, string? observacao = null)
        {
            await using var context = CriarContexto(empresaId);
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(empresaId, fichaId, insumoId, quantidade, observacao));
            await context.SaveChangesAsync();
        }

        public async Task CriarPrecoAsync(int empresaId, int insumoId, decimal quantidade, decimal preco, DateOnly data)
        {
            await using var context = CriarContexto(empresaId);
            context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, quantidade, preco, data));
            await context.SaveChangesAsync();
        }

        public async Task<string> ObterFichaAsync(int produtoId)
        {
            using var client = await Web.CriarClienteAutenticadoAsync(1);
            return await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));
        }

        public async Task<int> ContarFichasAsync(int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            return await context.FichasTecnicas.CountAsync();
        }

        public async Task DefinirValorHoraAsync(int empresaId, decimal? valorHora)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ConfiguracoesPrecificacaoEmpresas
                SET ValorHoraTrabalho = {valorHora}
                WHERE EmpresaId = {empresaId}
                """);
        }

        public async Task RemoverConfiguracaoAsync(int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ConfiguracoesPrecificacaoEmpresas
                WHERE EmpresaId = {empresaId}
                """);
        }

        public async Task<int> ContarConfiguracoesAsync(int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            return await context.ConfiguracoesPrecificacaoEmpresas.CountAsync();
        }

        public async Task<(decimal Rendimento, int Tempo)> ObterEstadoFichaAsync(int fichaId, int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            var ficha = await context.FichasTecnicas.SingleAsync(item => item.Id == fichaId);
            return (ficha.Rendimento, ficha.TempoAtivoMinutos);
        }

        public async Task<bool> ProdutoAtivoAsync(int produtoId, int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            return (await context.Produtos.SingleAsync(item => item.Id == produtoId)).Ativo;
        }

        public async Task<bool> InsumoAtivoAsync(int insumoId, int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            return (await context.Insumos.SingleAsync(item => item.Id == insumoId)).Ativo;
        }

        public async Task<decimal> RendimentoAsync(int fichaId, int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            return (await context.FichasTecnicas.SingleAsync(item => item.Id == fichaId)).Rendimento;
        }

        private PrecificadorDbContext CriarContexto(int empresaId)
        {
            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            return new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        }

        public ValueTask DisposeAsync()
        {
            scope.Dispose();
            factory.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
