using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

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
        Assert.Contains("R$ 0,013579", html);
        Assert.Contains("R$ 0,50", html);
        Assert.Contains("Custo base dos itens:", html);
        Assert.Contains("R$ 4,50", html);
        Assert.Contains("Observação contextual", html);
        Assert.Contains("Editar", html);
        Assert.Contains("Remover", html);
    }

    [Fact]
    public async Task MEL015_E1_E4_Post_web_preserva_valor_e_precificacao_economica_antes_e_depois_da_formatacao()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        var item = await ambiente.CriarItemAsync(1, ficha, insumo, 50m);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, $"/Insumos/Precos/Novo/{insumo}");
        var post = await client.PostAsync($"/Insumos/Precos/Novo/{insumo}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.QuantidadeCompra"] = "200",
            ["Input.PrecoCompra"] = "20,99",
            ["Input.DataReferencia"] = "2026-09-11"
        }));

        Assert.Equal(System.Net.HttpStatusCode.Redirect, post.StatusCode);
        var preco = await ambiente.ObterPrecoInsumoAsync(1, insumo);
        Assert.Equal(200m, preco.QuantidadeCompra);
        Assert.Equal(20.99m, preco.PrecoCompra);

        var precificacaoAtual = await ambiente.CalcularPrecificacaoAtualAsync(produto);
        Assert.NotNull(precificacaoAtual);
        var custoItem = Assert.Contains(item, precificacaoAtual!.Itens);
        Assert.Equal(0.10495m, custoItem.CustoUnitario);
        Assert.Equal(5.2475m, custoItem.CustoItem);

        var html = await ambiente.ObterFichaAsync(produto);
        Assert.Contains("R$ 0,10495", html);
        Assert.Contains("R$ 5,25", html);
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

    [Fact]
    public async Task UC022_W1_W2_W3_W11_W12_W13_W14_W15_W16_Compoe_e_recalcula_componentes_conhecidos()
    {
        await using var ambiente = await CriarAmbienteAsync();
        await ambiente.DefinirValorHoraAsync(1, 20m);
        await ambiente.DefinirTarifaAsync(1, 4m);
        var produto = await ambiente.CriarProdutoAsync(1, ativo: false);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m, tempo: 30);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        var item = await ambiente.CriarItemAsync(1, ficha, insumo, 2m, percentualPerda: 0.1m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 3m, Hoje);
        await ambiente.CriarUsoAsync(1, ficha, 1m, 30);

        AssertExibeCustoProduto(await ambiente.ObterFichaAsync(produto), "18,6", "9,3");

        await ambiente.CriarPrecoAsync(1, insumo, 1m, 5m, Hoje);
        AssertExibeCustoProduto(await ambiente.ObterFichaAsync(produto), "23", "11,5");

        await ambiente.DefinirPercentualPerdaAsync(1, item, 0.2m);
        await ambiente.DefinirValorHoraAsync(1, 40m);
        await ambiente.DefinirTarifaAsync(1, 2m);
        AssertExibeCustoProduto(await ambiente.ObterFichaAsync(produto), "33", "16,5");

        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);
        var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        var token = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync());
        var post = await client.PostAsync($"/Produtos/FichaTecnica/{produto}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Rendimento"] = "3",
            ["Input.TempoAtivoMinutos"] = "30"
        }));
        Assert.Equal(System.Net.HttpStatusCode.Redirect, post.StatusCode);
        AssertExibeCustoProduto(await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(post.Headers.Location!)), "33", "11");
        Assert.False(await ambiente.ProdutoAtivoAsync(produto, 1));
    }

    [Fact]
    public async Task UC022_W4_W5_W6_W7_W8_W9_W10_Impedimentos_nao_exibem_soma_parcial()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto, tempo: 30);
        var semPreco = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, semPreco, 2m, percentualPerda: 0.1m);
        await ambiente.CriarUsoAsync(1, ficha, 2m, 30);

        var html = await ambiente.ObterFichaAsync(produto);
        AssertExibeCustoProdutoIndisponivel(html);
        Assert.Contains("Há item(ns) sem preço vigente.", html);
        Assert.Contains("Há perda(s) sem custo base determinável.", html);
        Assert.Contains("Valor da hora de trabalho não configurado.", html);
        Assert.Matches("<td>2</td><td>30</td><td>1</td><td>—</td>", html);
        Assert.Matches("Custo de energia do lote:</strong>\\s*indisponível", html);
        Assert.Contains("Tarifa de energia não configurada.", html);
        Assert.Contains("Precificação incompleta.", html);

        await ambiente.CriarPrecoAsync(1, semPreco, 1m, 3m, Hoje);
        await ambiente.DefinirValorHoraAsync(1, 0m);
        await ambiente.DefinirTarifaAsync(1, 0m);
        AssertExibeCustoProduto(await ambiente.ObterFichaAsync(produto), "6,6", "6,6");

        var vazia = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, vazia);
        var fichaVazia = await ambiente.ObterFichaAsync(vazia);
        AssertExibeCustoProdutoIndisponivel(fichaVazia);
        Assert.Matches("Custo de perdas do lote:</strong>\\s*R\\$ 0,00", fichaVazia);
    }

    [Fact]
    public async Task UC022_W17_W18_W19_W20_Sem_ficha_post_invalido_e_tenant_preservam_estado()
    {
        await using var ambiente = await CriarAmbienteAsync();
        await ambiente.DefinirValorHoraAsync(1, 60m);
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m, tempo: 30);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, insumo, 2m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 5m, Hoje);
        var semFicha = await ambiente.CriarProdutoAsync(1, ativo: true);
        var fichasAntes = await ambiente.ContarFichasAsync(1);
        var estadoAntes = await ambiente.ObterEstadoFichaAsync(ficha, 1);

        Assert.DoesNotContain("Custo total do lote", await ambiente.ObterFichaAsync(semFicha));
        Assert.Equal(fichasAntes, await ambiente.ContarFichasAsync(1));
        AssertExibeCustoProduto(await ambiente.ObterFichaAsync(produto), "40", "20");

        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);
        var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        Assert.Equal(estadoAntes, await ambiente.ObterEstadoFichaAsync(ficha, 1));
        var token = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync());
        var post = await client.PostAsync($"/Produtos/FichaTecnica/{produto}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Rendimento"] = "0",
            ["Input.TempoAtivoMinutos"] = "120"
        }));
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(post);
        AssertExibeCustoProduto(html, "40", "20");
        Assert.Contains("value=\"120\"", html);
        Assert.Equal((2m, 30), await ambiente.ObterEstadoFichaAsync(ficha, 1));

        var empresaDois = await ambiente.CriarEmpresaAsync();
        var externo = await ambiente.CriarInsumoAsync(empresaDois, ativo: true);
        await ambiente.CriarPrecoAsync(empresaDois, externo, 1m, 999m, Hoje);
        AssertExibeCustoProduto(await ambiente.ObterFichaAsync(produto), "40", "20");
    }

    [Fact]
    public async Task UC023_W1_W2_W3_W4_W8_W9_W10_W11_W12_W13_Exibe_e_recalcula_precos_derivados()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: false, margemAlvo: .2m);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, insumo, 1m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 20m, Hoje);
        await ambiente.DefinirIncrementoAsync(1, .5m);

        AssertExibePrecoProduto(await ambiente.ObterFichaAsync(produto), "20%", "25", "25");

        await ambiente.CriarPrecoAsync(1, insumo, 1m, 20.2m, Hoje);
        AssertExibePrecoProduto(await ambiente.ObterFichaAsync(produto), "20%", "25,25", "25,5");

        await ambiente.DefinirIncrementoAsync(1, .25m);
        AssertExibePrecoProduto(await ambiente.ObterFichaAsync(produto), "20%", "25,25", "25,25");

        await ambiente.DefinirMargemAlvoAsync(1, produto, 0m);
        AssertExibePrecoProduto(await ambiente.ObterFichaAsync(produto), "0%", "20,2", "20,25");

        await ambiente.DefinirMargemPadraoAsync(1, .9m);
        await ambiente.DefinirReservaComercialAsync(1, .2m);
        AssertExibePrecoProduto(await ambiente.ObterFichaAsync(produto), "0%", "20,2", "20,25");
        Assert.False(await ambiente.ProdutoAtivoAsync(produto, 1));
    }

    [Fact]
    public async Task UC023_W5_W6_W7_W15_W16_W17_W18_Incompletude_preserva_motivos_e_teorico()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var completo = await ambiente.CriarProdutoAsync(1, ativo: true);
        var fichaCompleta = await ambiente.CriarFichaAsync(1, completo);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, fichaCompleta, insumo, 1m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 10m, Hoje);
        await ambiente.DefinirIncrementoAsync(1, null);

        var semIncremento = await ambiente.ObterFichaAsync(completo);
        AssertExibePrecoProduto(semIncremento, "30%", "14,2857", "indisponível");
        Assert.Contains("Incremento comercial não configurado.", semIncremento);
        Assert.Contains("Precificação incompleta.", semIncremento);

        var incompleto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var fichaIncompleta = await ambiente.CriarFichaAsync(1, incompleto, tempo: 1);
        var semPreco = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, fichaIncompleta, semPreco, 1m);
        await ambiente.CriarUsoAsync(1, fichaIncompleta, 1m, 1);
        var html = await ambiente.ObterFichaAsync(incompleto);
        AssertExibePrecoProduto(html, "30%", "indisponível", "indisponível");
        Assert.Contains("Há item(ns) sem preço vigente.", html);
        Assert.Contains("Valor da hora de trabalho não configurado.", html);
        Assert.Contains("Tarifa de energia não configurada.", html);

        var vazia = await ambiente.CriarProdutoAsync(1, ativo: true);
        await ambiente.CriarFichaAsync(1, vazia);
        AssertExibePrecoProduto(await ambiente.ObterFichaAsync(vazia), "30%", "indisponível", "indisponível");
    }

    [Fact]
    public async Task UC023_W14_W19_W20_Produto_sem_ficha_e_post_invalido_usam_somente_estado_persistido()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var semFicha = await ambiente.CriarProdutoAsync(1, ativo: true);
        var fichasAntes = await ambiente.ContarFichasAsync(1);
        var semFichaHtml = await ambiente.ObterFichaAsync(semFicha);
        Assert.DoesNotContain("Preço teórico", semFichaHtml);
        Assert.Equal(fichasAntes, await ambiente.ContarFichasAsync(1));

        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m, tempo: 0);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, insumo, 2m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 5m, Hoje);
        await ambiente.DefinirIncrementoAsync(1, .5m);
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
        AssertExibePrecoProduto(html, "30%", "7,1429", "7,5");
        Assert.Equal(antes, await ambiente.ObterEstadoFichaAsync(ficha, 1));
    }

    [Fact]
    public async Task UC023_W21_W22_Configuracao_respeita_tenant_e_ausencia_nao_cria_estado()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: true);
        var ficha = await ambiente.CriarFichaAsync(1, produto);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, insumo, 1m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 10m, Hoje);
        await ambiente.DefinirIncrementoAsync(1, .5m);
        var empresaDois = await ambiente.CriarEmpresaAsync();
        await ambiente.DefinirIncrementoAsync(empresaDois, 100m);
        AssertExibePrecoProduto(await ambiente.ObterFichaAsync(produto), "30%", "14,2857", "14,5");

        await ambiente.RemoverConfiguracaoAsync(1);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync($"/Produtos/FichaTecnica/{produto}")).StatusCode);
        Assert.Equal(0, await ambiente.ContarConfiguracoesAsync(1));
    }

    [Fact]
    public async Task UC025_W3_W5_W7_W13_W16_W28_W29_W32_W35_Detalhamento_projeta_estado_atual_sem_edicao()
    {
        await using var ambiente = await CriarAmbienteAsync();
        var produto = await ambiente.CriarProdutoAsync(1, ativo: false, margemAlvo: .2m);
        var ficha = await ambiente.CriarFichaAsync(1, produto, rendimento: 2m, tempo: 30);
        var insumo = await ambiente.CriarInsumoAsync(1, ativo: true);
        await ambiente.CriarItemAsync(1, ficha, insumo, 2m, percentualPerda: .1m);
        await ambiente.CriarPrecoAsync(1, insumo, 1m, 3m, Hoje);
        await ambiente.CriarUsoAsync(1, ficha, 1m, 30);
        await ambiente.DefinirValorHoraAsync(1, 20m);
        await ambiente.DefinirTarifaAsync(1, 4m);
        await ambiente.DefinirIncrementoAsync(1, .5m);
        var empresaDois = await ambiente.CriarEmpresaAsync();
        var produtoExterno = await ambiente.CriarProdutoAsync(empresaDois, ativo: true);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Precificacao/{produto}"));
        var externo = await client.GetAsync($"/Produtos/Precificacao/{produtoExterno}");

        foreach (var bloco in new[] { "Produto", "Estado da precificação", "Parâmetros usados", "Itens e perdas", "Mão de obra", "Equipamentos e energia", "Consolidação do custo", "Formação do preço", "Situação comercial atual", "Pendências da precificação" })
            Assert.Contains(bloco, pagina);
        Assert.Contains("Inativo", pagina);
        Assert.Contains("10%", pagina);
        Assert.Contains("1", pagina);
        Assert.Contains("Custo total do lote", pagina);
        Assert.Contains($"/Produtos/Detalhes/{produto}", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produto}", pagina);
        Assert.Contains($"/Produtos/Precos/Novo/{produto}", pagina);
        Assert.Contains($"/Produtos/Precos/Historico/{produto}", pagina);
        Assert.DoesNotContain("DescontoReferencia", pagina);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, externo.StatusCode);
    }

    private static void AssertExibeCustoProduto(string html, string lote, string unitario)
    {
        lote = FormatarMonetarioSeNecessario(lote);
        unitario = FormatarMonetarioSeNecessario(unitario);
        Assert.Matches($"Custo total do lote:</strong>\\s*{Regex.Escape(lote)}", html);
        Assert.Matches($"Custo unitário do produto:</strong>\\s*{Regex.Escape(unitario)}", html);
    }

    private static void AssertExibeCustoProdutoIndisponivel(string html) =>
        AssertExibeCustoProduto(html, "indisponível", "indisponível");

    private static void AssertExibePrecoProduto(string html, string margem, string teorico, string sugerido)
    {
        teorico = FormatarMonetarioSeNecessario(teorico);
        sugerido = FormatarMonetarioSeNecessario(sugerido);
        Assert.Matches($"Margem-alvo:</strong>\\s*{Regex.Escape(margem)}", html);
        Assert.Matches($"Preço teórico:</strong>\\s*{Regex.Escape(teorico)}", html);
        Assert.Matches($"Preço sugerido:</strong>\\s*{Regex.Escape(sugerido)}", html);
    }

    private static async Task<Ambiente> CriarAmbienteAsync()
    {
        var factory = new CustomWebApplicationFactory(Hoje);
        _ = factory.Services;
        return await Task.FromResult(new Ambiente(factory));
    }

    private static void AssertExibeCustoMaoDeObra(string html, string valor) =>
        Assert.Matches(
            $"Custo de mão de obra do lote:</strong>\\s*{Regex.Escape(FormatarMonetarioSeNecessario(valor))}",
            html);

    private static string FormatarMonetarioSeNecessario(string valor)
    {
        if (valor == "indisponível")
        {
            return valor;
        }

        if (valor.StartsWith("R$ ", StringComparison.Ordinal))
        {
            return valor;
        }

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var decimalSeparador = valor.Replace('.', ',');
        var convertido = decimal.Parse(decimalSeparador, NumberStyles.Number, cultura);
        return convertido.ToString("C2", cultura);
    }

    private sealed class Ambiente(CustomWebApplicationFactory factory) : IAsyncDisposable
    {
        private readonly IServiceScope scope = factory.Services.CreateScope();

        public WebTestContext Web { get; } = new(factory);

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

        public async Task<int> CriarItemAsync(int empresaId, int fichaId, int insumoId, decimal quantidade, string? observacao = null, decimal percentualPerda = 0m)
        {
            await using var context = CriarContexto(empresaId);
            var item = ItemFichaTecnica.Criar(empresaId, fichaId, insumoId, quantidade, observacao, percentualPerda);
            context.ItensFichaTecnica.Add(item);
            await context.SaveChangesAsync();
            return item.Id;
        }

        public async Task CriarUsoAsync(int empresaId, int fichaId, decimal potenciaKw, int tempoUsoMinutos)
        {
            await using var context = CriarContexto(empresaId);
            context.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(empresaId, fichaId, "Equipamento", potenciaKw, tempoUsoMinutos));
            await context.SaveChangesAsync();
        }

        public async Task DefinirPercentualPerdaAsync(int empresaId, int itemId, decimal percentualPerda)
        {
            await using var context = CriarContexto(empresaId);
            var item = await context.ItensFichaTecnica.SingleAsync(item => item.Id == itemId);
            item.AtualizarDados(item.Quantidade, item.Observacao, percentualPerda);
            await context.SaveChangesAsync();
        }

        public async Task CriarPrecoAsync(int empresaId, int insumoId, decimal quantidade, decimal preco, DateOnly data)
        {
            await using var context = CriarContexto(empresaId);
            context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, quantidade, preco, data));
            await context.SaveChangesAsync();
        }

        public async Task<PrecoInsumo> ObterPrecoInsumoAsync(int empresaId, int insumoId)
        {
            await using var context = CriarContexto(empresaId);
            return await context.PrecosInsumos.AsNoTracking()
                .SingleAsync(preco => preco.InsumoId == insumoId);
        }

        public async Task<ResultadoPrecificacaoProdutoAtual?> CalcularPrecificacaoAtualAsync(int produtoId)
        {
            await using var context = CriarContexto(1);
            var dataOperacionalEmpresa = scope.ServiceProvider.GetRequiredService<IDataOperacionalEmpresa>();
            var servico = new PrecificacaoProdutoAtual(context, dataOperacionalEmpresa);
            return await servico.CalcularAsync(produtoId);
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
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE ConfiguracoesPrecificacaoEmpresas SET ValorHoraTrabalho = @valorHora WHERE EmpresaId = @empresaId",
                SqlDecimalParameter.Criar("valorHora", valorHora, precision: 18, scale: 6),
                new SqlParameter("empresaId", empresaId));
        }

        public async Task DefinirTarifaAsync(int empresaId, decimal? tarifa)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE ConfiguracoesPrecificacaoEmpresas SET TarifaEnergiaKwh = @tarifa WHERE EmpresaId = @empresaId",
                SqlDecimalParameter.Criar("tarifa", tarifa, precision: 18, scale: 6),
                new SqlParameter("empresaId", empresaId));
        }

        public async Task DefinirIncrementoAsync(int empresaId, decimal? incremento)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE ConfiguracoesPrecificacaoEmpresas SET IncrementoComercial = @incremento WHERE EmpresaId = @empresaId",
                SqlDecimalParameter.Criar("incremento", incremento, precision: 18, scale: 6),
                new SqlParameter("empresaId", empresaId));
        }

        public async Task DefinirMargemPadraoAsync(int empresaId, decimal? margem)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE ConfiguracoesPrecificacaoEmpresas SET MargemPadrao = @margem WHERE EmpresaId = @empresaId",
                SqlDecimalParameter.Criar("margem", margem, precision: 9, scale: 6),
                new SqlParameter("empresaId", empresaId));
        }

        public async Task DefinirReservaComercialAsync(int empresaId, decimal reserva)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE ConfiguracoesPrecificacaoEmpresas SET ReservaComercialDesconto = @reserva WHERE EmpresaId = @empresaId",
                SqlDecimalParameter.Criar("reserva", reserva, precision: 9, scale: 6),
                new SqlParameter("empresaId", empresaId));
        }

        public async Task DefinirMargemAlvoAsync(int empresaId, int produtoId, decimal margem)
        {
            await using var context = CriarContexto(empresaId);
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Produtos SET MargemAlvo = @margem WHERE Id = @produtoId",
                SqlDecimalParameter.Criar("margem", margem, precision: 9, scale: 6),
                new SqlParameter("produtoId", produtoId));
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
