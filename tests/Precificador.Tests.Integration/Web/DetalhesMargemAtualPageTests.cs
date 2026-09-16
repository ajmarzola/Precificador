using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class DetalhesMargemAtualPageTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 15);

    [Fact]
    public async Task W1_W2_W3_Detalhes_mostra_resumo_completo_e_classifica_abaixo_igual_ou_dentro()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var dentro = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: 15m);
        var abaixo = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 16m, precoPrateleira: 20m);
        var igual = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 14m, precoPrateleira: 20m);

        var htmlDentro = await ambiente.ObterDetalhesAsync(dentro);

        AssertResumo(htmlDentro, "10", "15", "15/09/2026", "30%", "33,33%", "Dentro da margem");
        AssertResumo(await ambiente.ObterDetalhesAsync(abaixo), "16", "20", "15/09/2026", "30%", "20%", "Abaixo da margem");
        AssertResumo(await ambiente.ObterDetalhesAsync(igual), "14", "20", "15/09/2026", "30%", "30%", "Dentro da margem");
    }

    [Fact]
    public async Task W4_W5_W6_Sem_preco_ou_sem_ficha_mantem_incompleto_sem_apagar_preco()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var semPreco = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: null);
        var semFicha = await ambiente.CriarProdutoAsync(1, ativo: true, margemAlvo: .30m);
        await ambiente.CriarRegistroAsync(1, semFicha, Hoje, prateleira: 20m);

        var htmlSemPreco = await ambiente.ObterDetalhesAsync(semPreco);
        var htmlSemFicha = await ambiente.ObterDetalhesAsync(semFicha);

        Assert.Contains("Preço de prateleira atual: não definido.", htmlSemPreco);
        AssertResumo(htmlSemPreco, "10", "não definido.", null, "30%", "indisponível", "Incompleto");
        Assert.Contains("Preço de prateleira não definido.", htmlSemPreco);
        AssertResumo(htmlSemFicha, "indisponível", "20", "15/09/2026", "30%", "indisponível", "Incompleto");
        Assert.Contains("A ficha técnica não foi cadastrada.", htmlSemFicha);
    }

    [Fact]
    public async Task W7_W8_W9_Incremento_null_e_snapshots_historicos_nao_contaminam_margem()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoPrecificavelAsync(
            1,
            ativo: true,
            margemAlvo: .30m,
            custo: 10m,
            precoPrateleira: null);
        await ambiente.CriarRegistroAsync(1, produto, Hoje, prateleira: 20m, custoReferencia: 90m, margemReferencia: .90m);

        var html = await ambiente.ObterDetalhesAsync(produto);

        AssertResumo(html, "10", "20", "15/09/2026", "30%", "50%", "Dentro da margem");
        Assert.DoesNotContain("Incremento comercial não configurado.", html);
    }

    [Fact]
    public async Task W10_W11_W12_W13_Recalcula_por_custo_margem_alvo_preco_e_empate_por_id()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: 20m);
        AssertResumo(await ambiente.ObterDetalhesAsync(produto), "10", "20", "15/09/2026", "30%", "50%", "Dentro da margem");

        await ambiente.CriarPrecoInsumoAtualAsync(1, produto, 16m);
        AssertResumo(await ambiente.ObterDetalhesAsync(produto), "16", "20", "15/09/2026", "30%", "20%", "Abaixo da margem");

        await ambiente.DefinirMargemAlvoAsync(1, produto, .10m);
        AssertResumo(await ambiente.ObterDetalhesAsync(produto), "16", "20", "15/09/2026", "10%", "20%", "Dentro da margem");

        await ambiente.CriarRegistroAsync(1, produto, Hoje.AddDays(-1), prateleira: 12m);
        AssertResumo(await ambiente.ObterDetalhesAsync(produto), "16", "20", "15/09/2026", "10%", "20%", "Dentro da margem");

        await ambiente.CriarRegistroAsync(1, produto, Hoje, prateleira: 25m);
        AssertResumo(await ambiente.ObterDetalhesAsync(produto), "16", "25", "15/09/2026", "10%", "36%", "Dentro da margem");
    }

    [Fact]
    public async Task W14_W15_W16_Custo_zero_margem_negativa_e_produto_inativo()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var custoZero = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: 20m);
        await ambiente.DefinirPrecoInsumoAtualPorSqlAsync(1, custoZero, 0m);
        var margemNegativa = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 30m, precoPrateleira: 20m);
        var inativo = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: false, margemAlvo: .30m, custo: 10m, precoPrateleira: 20m);

        AssertResumo(await ambiente.ObterDetalhesAsync(custoZero), "0", "20", "15/09/2026", "30%", "100%", "Dentro da margem");
        AssertResumo(await ambiente.ObterDetalhesAsync(margemNegativa), "30", "20", "15/09/2026", "30%", "-50%", "Abaixo da margem");
        var htmlInativo = await ambiente.ObterDetalhesAsync(inativo);
        Assert.Contains("Inativo", htmlInativo);
        AssertResumo(htmlInativo, "10", "20", "15/09/2026", "30%", "50%", "Dentro da margem");
    }

    [Fact]
    public async Task W17_W18_Cross_tenant_e_dados_de_outra_empresa_nao_influenciam()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var empresaDois = await ambiente.CriarEmpresaAsync();
        var produtoEmpresaUm = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: null);
        var produtoEmpresaDois = await ambiente.CriarProdutoPrecificavelAsync(empresaDois, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: 99m);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        var crossTenant = await client.GetAsync($"/Produtos/Detalhes/{produtoEmpresaDois}");
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{produtoEmpresaUm}"));

        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        Assert.Contains("Preço de prateleira atual: não definido.", html);
    }

    [Fact]
    public async Task W19_W20_Get_nao_persiste_e_configuracao_ausente_preserva_preco_incompleto()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var semFicha = await ambiente.CriarProdutoAsync(1, ativo: true, margemAlvo: .30m);
        await ambiente.CriarRegistroAsync(1, semFicha, Hoje, prateleira: 20m);
        var fichasAntes = await ambiente.ContarFichasAsync(1);
        var configuracoesAntes = await ambiente.ContarConfiguracoesAsync(1);

        var htmlSemFicha = await ambiente.ObterDetalhesAsync(semFicha);

        AssertResumo(htmlSemFicha, "indisponível", "20", "15/09/2026", "30%", "indisponível", "Incompleto");
        Assert.Equal(fichasAntes, await ambiente.ContarFichasAsync(1));
        Assert.Equal(configuracoesAntes, await ambiente.ContarConfiguracoesAsync(1));

        var semConfiguracao = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: 20m);
        await ambiente.RemoverConfiguracaoAsync(1);
        var htmlSemConfiguracao = await ambiente.ObterDetalhesAsync(semConfiguracao);

        AssertResumo(htmlSemConfiguracao, "indisponível", "20", "15/09/2026", "30%", "indisponível", "Incompleto");
        Assert.Contains("As configurações de precificação não foram encontradas.", htmlSemConfiguracao);
        Assert.Equal(0, await ambiente.ContarConfiguracoesAsync(1));
    }

    [Fact]
    public async Task W21_W22_Ficha_tecnica_nao_regride_e_detalhes_nao_antecipa_uc025()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoPrecificavelAsync(1, ativo: true, margemAlvo: .30m, custo: 10m, precoPrateleira: 20m);
        await ambiente.DefinirIncrementoAsync(1, .5m);

        var ficha = await ambiente.ObterFichaAsync(produto);
        var detalhes = await ambiente.ObterDetalhesAsync(produto);

        Assert.Contains("Custo unitário do produto:", ficha);
        Assert.Contains("R$ 10,00", ficha);
        Assert.Contains("Preço teórico:", ficha);
        Assert.Contains("R$ 14,29", ficha);
        Assert.Contains("Preço sugerido:", ficha);
        Assert.Contains("R$ 14,50", ficha);
        Assert.DoesNotContain("Custo base dos itens:", detalhes);
        Assert.DoesNotContain("Custo de perdas do lote:", detalhes);
        Assert.DoesNotContain("Custo de mão de obra do lote:", detalhes);
        Assert.DoesNotContain("Custo de energia do lote:", detalhes);
        Assert.DoesNotContain("Preço teórico:", detalhes);
        Assert.DoesNotContain("Preço sugerido:", detalhes);
    }

    private static void AssertResumo(
        string html,
        string custo,
        string preco,
        string? data,
        string margemAlvo,
        string margemAtual,
        string situacao)
    {
        custo = FormatarMonetarioSeNecessario(custo);
        preco = FormatarMonetarioSeNecessario(preco);
        Assert.Matches($"Custo unitário atual</dt>\\s*<dd[^>]*>\\s*{Regex.Escape(custo)}", html);
        Assert.Matches($"Preço de prateleira atual</dt>\\s*<dd[^>]*>\\s*{Regex.Escape(preco)}", html);
        if (data is not null)
        {
            Assert.Matches($"Referência do preço</dt>\\s*<dd[^>]*>\\s*{Regex.Escape(data)}", html);
        }

        Assert.Matches($"Margem-alvo</dt>\\s*<dd[^>]*>\\s*{Regex.Escape(margemAlvo)}", html);
        Assert.Matches($"Margem atual</dt>\\s*<dd[^>]*>\\s*{Regex.Escape(margemAtual)}", html);
        Assert.Matches($"Situação</dt>\\s*<dd[^>]*>\\s*{Regex.Escape(situacao)}", html);
    }

    private static string FormatarMonetarioSeNecessario(string valor)
    {
        if (valor is "indisponível" or "não definido.")
        {
            return valor;
        }

        var cultura = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        var normalizado = valor.Replace('.', ',');
        var convertido = decimal.Parse(normalizado, System.Globalization.NumberStyles.Number, cultura);
        return convertido.ToString("C2", cultura);
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

        public async Task<int> CriarProdutoAsync(int empresaId, bool ativo, decimal margemAlvo)
        {
            await using var context = CriarContexto(empresaId);
            var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", margemAlvo);
            if (!ativo) produto.Desativar();
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
            return produto.Id;
        }

        public async Task<int> CriarProdutoPrecificavelAsync(int empresaId, bool ativo, decimal margemAlvo, decimal custo, decimal? precoPrateleira)
        {
            await using var context = CriarContexto(empresaId);
            var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", margemAlvo);
            if (!ativo) produto.Desativar();
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
            var ficha = FichaTecnica.Criar(empresaId, produto.Id, 1m, 0);
            var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade);
            context.FichasTecnicas.Add(ficha);
            context.Insumos.Add(insumo);
            await context.SaveChangesAsync();
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(empresaId, ficha.Id, insumo.Id, 1m, null, 0m));
            context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumo.Id, 1m, custo, Hoje));
            if (precoPrateleira is not null)
            {
                context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(empresaId, produto.Id, Hoje, custo, margemAlvo, 14.29m, precoPrateleira.Value, .10m));
            }

            await context.SaveChangesAsync();
            return produto.Id;
        }

        public async Task CriarRegistroAsync(
            int empresaId,
            int produtoId,
            DateOnly data,
            decimal prateleira,
            decimal custoReferencia = 10m,
            decimal margemReferencia = .30m)
        {
            await using var context = CriarContexto(empresaId);
            context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(empresaId, produtoId, data, custoReferencia, margemReferencia, 14.29m, prateleira, .10m));
            await context.SaveChangesAsync();
        }

        public async Task CriarPrecoInsumoAtualAsync(int empresaId, int produtoId, decimal custo)
        {
            await using var context = CriarContexto(empresaId);
            var ficha = await context.FichasTecnicas.SingleAsync(f => f.ProdutoId == produtoId);
            var insumoId = await context.ItensFichaTecnica.Where(i => i.FichaTecnicaId == ficha.Id).Select(i => i.InsumoId).SingleAsync();
            context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, 1m, custo, Hoje));
            await context.SaveChangesAsync();
        }

        public async Task DefinirPrecoInsumoAtualPorSqlAsync(int empresaId, int produtoId, decimal precoCompra)
        {
            await using var context = CriarContexto(empresaId);
            var ficha = await context.FichasTecnicas.SingleAsync(f => f.ProdutoId == produtoId);
            var insumoId = await context.ItensFichaTecnica.Where(i => i.FichaTecnicaId == ficha.Id).Select(i => i.InsumoId).SingleAsync();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE PrecosInsumos
                SET PrecoCompra = {precoCompra}
                WHERE InsumoId = {insumoId}
                """);
        }

        public async Task DefinirMargemAlvoAsync(int empresaId, int produtoId, decimal margem)
        {
            await using var context = CriarContexto(empresaId);
            var produto = await context.Produtos.SingleAsync(p => p.Id == produtoId);
            produto.AtualizarDados(produto.Nome, margem, produto.Categoria);
            await context.SaveChangesAsync();
        }

        public async Task DefinirIncrementoAsync(int empresaId, decimal? incremento)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ConfiguracoesPrecificacaoEmpresas
                SET IncrementoComercial = {incremento}
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

        public async Task<int> ContarFichasAsync(int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            return await context.FichasTecnicas.CountAsync();
        }

        public async Task<int> ContarConfiguracoesAsync(int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            return await context.ConfiguracoesPrecificacaoEmpresas.CountAsync();
        }

        public async Task<string> ObterDetalhesAsync(int produtoId)
        {
            using var client = await Web.CriarClienteAutenticadoAsync(1);
            return await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{produtoId}"));
        }

        public async Task<string> ObterFichaAsync(int produtoId)
        {
            using var client = await Web.CriarClienteAutenticadoAsync(1);
            return await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));
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
