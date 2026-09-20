using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class DetalhamentoPrecificacaoPageTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 15);

    [Fact]
    public async Task W1_W2_W3_W4_W28_W35_Exige_contexto_isola_produto_e_oferece_navegacao_somente_leitura()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var ativo = await ambiente.CriarProdutoAsync(1, ativo: true);
        var inativo = await ambiente.CriarProdutoAsync(1, ativo: false);
        var empresaDois = await ambiente.CriarEmpresaAsync();
        var externo = await ambiente.CriarProdutoAsync(empresaDois, ativo: true);
        using var anonimo = ambiente.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var semEmpresa = ambiente.Web.CriarCliente();
        var usuario = await ambiente.Web.CriarUsuarioAsync();
        await ambiente.Web.LoginAsync(semEmpresa, usuario.Email, usuario.Senha);
        using var cliente = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        Assert.Equal(HttpStatusCode.Redirect, (await anonimo.GetAsync($"/Produtos/Precificacao/{ativo}")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await semEmpresa.GetAsync($"/Produtos/Precificacao/{ativo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/Produtos/Precificacao/{externo}")).StatusCode);
        var detalhesAtivo = await ambiente.LerAsync(cliente, $"/Produtos/Detalhes/{ativo}");
        var detalhesInativo = await ambiente.LerAsync(cliente, $"/Produtos/Detalhes/{inativo}");
        var paginaInativo = await ambiente.LerAsync(cliente, $"/Produtos/Precificacao/{inativo}");

        Assert.Contains($"/Produtos/Precificacao/{ativo}", detalhesAtivo);
        Assert.Contains($"/Produtos/Precificacao/{inativo}", detalhesInativo);
        Assert.Contains("Inativo", paginaInativo);
        Assert.Contains($"/Produtos/Detalhes/{inativo}", paginaInativo);
        Assert.Contains($"/Produtos/FichaTecnica/{inativo}", paginaInativo);
        Assert.Contains($"/Produtos/Precos/Novo/{inativo}", paginaInativo);
        Assert.Contains($"/Produtos/Precos/Historico/{inativo}", paginaInativo);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync($"/Produtos/Precificacao/{inativo}", new FormUrlEncodedContent([]))).StatusCode);
    }

    [Fact]
    public async Task W7_W8_W9_W10_W11_W12_W13_W14_W15_Incompletude_preserva_null_e_zeros_conhecidos()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m);
        var insumo = await ambiente.CriarInsumoAsync(1);
        await ambiente.CriarItemAsync(1, ficha, insumo, 2m, .1m);
        await ambiente.CriarUsoAsync(1, ficha, 2m, 30);
        var semPreco = await ambiente.ObterPrecificacaoAsync(produto);

        Assert.Contains("Sem preço vigente", semPreco);
        Assert.Contains("Custo base dos itens:</strong> indisponível", semPreco);
        Assert.Contains("Custo de perdas do lote:</strong> indisponível", semPreco);
        Assert.DoesNotContain("Valor da hora", semPreco);
        Assert.Contains("Percentual de mão de obra</dt><dd class=\"col-sm-9\">10%", semPreco);
        Assert.Contains("Custo de mão de obra do lote</dt><dd class=\"col-sm-9\">indisponível", semPreco);
        Assert.Contains("Consumo (kWh)", semPreco);
        Assert.Contains("Custo de energia do lote:</strong> indisponível", semPreco);

        var vazio = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, vazio);
        var fichaVazia = await ambiente.ObterPrecificacaoAsync(vazio);

        Assert.Contains("Nenhum insumo adicionado.", fichaVazia);
        Assert.Contains("Custo de perdas do lote:</strong> R$ 0,00", fichaVazia);
        Assert.Contains("Custo de mão de obra do lote</dt><dd class=\"col-sm-9\">indisponível", fichaVazia);
        Assert.Contains("Nenhum equipamento adicionado.", fichaVazia);
        Assert.Contains("Custo de energia do lote:</strong> R$ 0,00", fichaVazia);
    }

    [Fact]
    public async Task W16_W17_W18_W19_W20_W21_W26_Exibe_componentes_precos_e_pendencias_sem_soma_parcial()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true, margemAlvo: .2m);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m);
        var insumo = await ambiente.CriarInsumoAsync(1);
        await ambiente.CriarItemAsync(1, ficha, insumo, 2m, .1m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 3m, Hoje);
        await ambiente.CriarUsoAsync(1, ficha, 1m, 30);
        await ambiente.AtualizarConfiguracaoAsync(1, .10m, 4m, .5m);
        await ambiente.CriarRegistroPrecoProdutoAsync(1, produto, Hoje, 999m, .9m, 1000m, 15m);

        var completo = await ambiente.ObterPrecificacaoAsync(produto);

        Assert.Contains("15/09/2026", completo);
        Assert.Contains("10%", completo);
        Assert.Contains("Custo total do lote</dt><dd class=\"col-sm-9\">R$ 9,20", completo);
        Assert.Contains("Custo unitário do produto</dt><dd class=\"col-sm-9\">R$ 4,60", completo);
        Assert.Contains("Preço teórico</dt><dd class=\"col-sm-9\">R$ 5,75", completo);
        Assert.Contains("Preço sugerido</dt><dd class=\"col-sm-9\">R$ 6,00", completo);
        Assert.Contains("Margem atual</dt><dd class=\"col-sm-9\">69,33%", completo);
        Assert.Contains("Dentro da margem", completo);
        Assert.Contains("Nenhuma pendência de cálculo.", completo);

        await ambiente.AtualizarConfiguracaoAsync(1, .10m, 4m, null);
        var semIncremento = await ambiente.ObterPrecificacaoAsync(produto);
        Assert.Contains("Preço teórico</dt><dd class=\"col-sm-9\">R$ 5,75", semIncremento);
        Assert.Contains("Preço sugerido</dt><dd class=\"col-sm-9\">indisponível", semIncremento);
        Assert.Contains("Incremento comercial não configurado.", semIncremento);
    }

    [Fact]
    public async Task W22_W23_W24_W25_W27_W29_W30_Snapshots_ficha_e_configuracao_ausentes_nao_contaminam_ou_criam_estado()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var semFicha = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarRegistroPrecoProdutoAsync(1, semFicha, Hoje, 80m, .9m, 100m, 20m);
        var fichasAntes = await ambiente.ContarFichasAsync(1);

        var paginaSemFicha = await ambiente.ObterPrecificacaoAsync(semFicha);
        Assert.Contains("Ficha técnica: não cadastrada.", paginaSemFicha);
        Assert.Contains("A ficha técnica não foi cadastrada.", paginaSemFicha);
        Assert.Contains("20", paginaSemFicha);
        Assert.Contains("Custo unitário</dt><dd class=\"col-sm-9\">indisponível", paginaSemFicha);
        Assert.DoesNotContain("Desconto", paginaSemFicha);
        Assert.Equal(fichasAntes, await ambiente.ContarFichasAsync(1));

        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var insumo = await ambiente.CriarInsumoAsync(1);
        await ambiente.CriarItemAsync(1, ficha, insumo, 1m, 0m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 10m, Hoje);
        await ambiente.RemoverConfiguracaoAsync(1);

        var semConfiguracao = await ambiente.ObterPrecificacaoAsync(produto);
        Assert.Contains("As configurações de precificação não foram encontradas.", semConfiguracao);
        Assert.Contains("Rendimento</dt><dd class=\"col-sm-9\">1", semConfiguracao);
        Assert.Contains("Mão de obra sobre os insumos</dt><dd class=\"col-sm-9\">indisponível", semConfiguracao);
        Assert.Contains("Percentual de mão de obra</dt><dd class=\"col-sm-9\">indisponível", semConfiguracao);
        Assert.DoesNotContain("Mão de obra sobre os insumos</dt><dd class=\"col-sm-9\">0%", semConfiguracao);
        Assert.DoesNotContain("Percentual de mão de obra</dt><dd class=\"col-sm-9\">0%", semConfiguracao);
        Assert.Equal(0, await ambiente.ContarConfiguracoesAsync(1));
    }

    [Fact]
    public async Task W31_W32_Proximo_get_recalcula_e_nao_vaza_dados_de_outra_empresa()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var insumo = await ambiente.CriarInsumoAsync(1);
        await ambiente.CriarItemAsync(1, ficha, insumo, 1m, 0m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 10m, Hoje);
        await ambiente.AtualizarConfiguracaoAsync(1, 0m, 0m, .5m);
        var empresaDois = await ambiente.CriarEmpresaAsync();
        var produtoExterno = await ambiente.CriarProdutoAsync(empresaDois, ativo: true);
        var fichaExterna = await ambiente.CriarFichaAsync(empresaDois, produtoExterno);
        var insumoExterno = await ambiente.CriarInsumoAsync(empresaDois);
        await ambiente.CriarItemAsync(empresaDois, fichaExterna, insumoExterno, 1m, 0m);
        await ambiente.CriarPrecoAsync(empresaDois, insumoExterno, 1m, 999m, Hoje);

        Assert.Contains("14,5", await ambiente.ObterPrecificacaoAsync(produto));
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 20m, Hoje);
        var recalculado = await ambiente.ObterPrecificacaoAsync(produto);

        Assert.Contains("R$ 28,57", recalculado);
        Assert.DoesNotContain("999", recalculado);
    }

    private sealed class Ambiente(CustomWebApplicationFactory factory) : IAsyncDisposable
    {
        private readonly IServiceScope scope = factory.Services.CreateScope();
        public CustomWebApplicationFactory Factory { get; } = factory;
        public WebTestContext Web { get; } = new(factory);

        public static async Task<Ambiente> CriarAsync()
        {
            var factory = new CustomWebApplicationFactory(Hoje);
            _ = factory.Services;
            return await Task.FromResult(new Ambiente(factory));
        }

        public async Task<string> ObterPrecificacaoAsync(int produtoId)
        {
            using var client = await Web.CriarClienteAutenticadoAsync(1);
            return await LerAsync(client, $"/Produtos/Precificacao/{produtoId}");
        }

        public async Task<string> LerAsync(HttpClient client, string url) =>
            await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(url));

        public async Task<int> CriarEmpresaAsync() => await Web.CriarEmpresaAsync();

        public async Task<int> CriarProdutoAsync(int empresaId, bool ativo, decimal margemAlvo = .3m)
        {
            await using var context = CriarContexto(empresaId);
            var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", margemAlvo);
            if (!ativo) produto.Desativar();
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
            return produto.Id;
        }

        public async Task<int> CriarFichaAsync(int empresaId, int produtoId, decimal rendimento = 1m, int tempo = 0)
        {
            await using var context = CriarContexto(empresaId);
            var ficha = FichaTecnica.Criar(empresaId, produtoId, rendimento);
            context.FichasTecnicas.Add(ficha);
            await context.SaveChangesAsync();
            return ficha.Id;
        }

        public async Task<int> CriarInsumoAsync(int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade);
            context.Insumos.Add(insumo);
            await context.SaveChangesAsync();
            return insumo.Id;
        }

        public async Task CriarItemAsync(int empresaId, int fichaId, int insumoId, decimal quantidade, decimal percentualPerda)
        {
            await using var context = CriarContexto(empresaId);
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(empresaId, fichaId, insumoId, quantidade, null, percentualPerda));
            await context.SaveChangesAsync();
        }

        public async Task CriarUsoAsync(int empresaId, int fichaId, decimal potencia, int minutos)
        {
            await using var context = CriarContexto(empresaId);
            context.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(empresaId, fichaId, "Equipamento", potencia, minutos));
            await context.SaveChangesAsync();
        }

        public async Task CriarPrecoAsync(int empresaId, int insumoId, decimal quantidade, decimal preco, DateOnly data)
        {
            await using var context = CriarContexto(empresaId);
            context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, quantidade, preco, data));
            await context.SaveChangesAsync();
        }

        public async Task CriarRegistroPrecoProdutoAsync(int empresaId, int produtoId, DateOnly data, decimal custo, decimal margem, decimal sugerido, decimal prateleira)
        {
            await using var context = CriarContexto(empresaId);
            context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(empresaId, produtoId, data, custo, margem, sugerido, prateleira, .1m));
            await context.SaveChangesAsync();
        }

        public async Task AtualizarConfiguracaoAsync(int empresaId, decimal percentualMaoDeObra, decimal? tarifa, decimal? incremento)
        {
            await using var context = CriarContexto(empresaId);
            var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
            configuracao.Atualizar(percentualMaoDeObra, tarifa, null, incremento, .1m);
            await context.SaveChangesAsync();
        }

        public async Task RemoverConfiguracaoAsync(int empresaId)
        {
            await using var context = CriarContexto(empresaId);
            context.ConfiguracoesPrecificacaoEmpresas.RemoveRange(context.ConfiguracoesPrecificacaoEmpresas);
            await context.SaveChangesAsync();
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

        private PrecificadorDbContext CriarContexto(int empresaId) =>
            new(scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(), new ContextoEmpresaTeste(empresaId));

        public ValueTask DisposeAsync()
        {
            scope.Dispose();
            Factory.Dispose();
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
