using System.Globalization;
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
