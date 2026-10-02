using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Produtos;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class ProdutoColecaoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task Gerenciamento_NN_categorias_inativos_detalhes_e_desvinculo_preservam_produto()
    {
        var dados = await PrepararAsync();
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var url = Url(dados.Colecao);
        var html = await HtmlAsync(client, url);
        Assert.Contains("Finalizada", html);
        Assert.Contains(dados.NomeInativo + " (inativo)", html);
        Assert.Contains(dados.NomeSemCategoria, html);
        Assert.Contains(dados.NomeOutraCategoria, html);
        Assert.DoesNotContain(dados.NomeAlheio, html);
        Assert.DoesNotContain("Input.EmpresaId", html);
        Assert.Contains("ainda não possui produtos", html);
        Assert.Contains("Este produto ainda não participa de nenhuma coleção.", await HtmlAsync(client, $"/Produtos/Detalhes/{dados.SemCategoria}"));
        foreach (var produto in new[] { dados.Inativo, dados.SemCategoria, dados.OutraCategoria, dados.MesmaCategoria })
            Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, url, "Vincular", produto, true)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, Url(dados.Futura), "Vincular", dados.SemCategoria, false)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, Url(dados.Aberta), "Vincular", dados.SemCategoria, true)).StatusCode);
        html = await HtmlAsync(client, url);
        Assert.Contains("Inativo", html);
        Assert.Contains("Sim", html);
        var select = html.Split("<select")[1].Split("</select>")[0];
        Assert.DoesNotContain(dados.NomeSemCategoria, select);
        var duplicada = await PostAsync(client, url, "Vincular", dados.SemCategoria, false);
        Assert.Equal(HttpStatusCode.OK, duplicada.StatusCode);
        Assert.Contains("Este produto já está vinculado à coleção.", await WebTestHtml.LerHtmlDecodificadoAsync(duplicada));
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, url, "DefinirDestaque", dados.SemCategoria, false)).StatusCode);
        html = await HtmlAsync(client, url);
        Assert.True(html.IndexOf(dados.NomeInativo, StringComparison.Ordinal) < html.LastIndexOf(dados.NomeSemCategoria, StringComparison.Ordinal));
        var detalhes = await HtmlAsync(client, $"/Produtos/Detalhes/{dados.SemCategoria}");
        Assert.Contains(dados.NomeColecao, detalhes);
        Assert.Contains("01/01/2020", detalhes);
        Assert.Contains("02/01/2020", detalhes);
        Assert.Contains("Finalizada", detalhes);
        Assert.Contains("Planejada", detalhes);
        Assert.Contains("Em andamento", detalhes);
        Assert.Contains("Em aberto", detalhes);
        Assert.Contains("Sim", detalhes); Assert.Contains("Não", detalhes);
        Assert.True(detalhes.IndexOf(dados.NomeFutura, StringComparison.Ordinal) < detalhes.IndexOf(dados.NomeColecao, StringComparison.Ordinal));
        Assert.True(detalhes.IndexOf(dados.NomeAberta, StringComparison.Ordinal) < detalhes.IndexOf(dados.NomeColecao, StringComparison.Ordinal));
        using (var scope = factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
            var produto = await db.Produtos.SingleAsync(x => x.Id == dados.SemCategoria);
            produto.Desativar(); await db.SaveChangesAsync();
            Assert.Equal(3, await db.ProdutosColecoes.CountAsync(x => x.ProdutoId == produto.Id));
            produto.Reativar(); produto.AtualizarDados(produto.Nome, .2m, dados.Categoria);
            var categoria = await db.CategoriasProdutos.SingleAsync(x => x.Id == dados.Categoria); categoria.Desativar();
            var categorias = await db.ColecoesProdutosCategorias.Where(x => x.ColecaoProdutoId == dados.Colecao).ToListAsync();
            db.RemoveRange(categorias);
            var colecao = await db.ColecoesProdutos.SingleAsync(x => x.Id == dados.Colecao);
            colecao.AtualizarDados(colecao.Nome, new(2020, 1, 1), null);
            await db.SaveChangesAsync();
            Assert.Equal(4, await db.ProdutosColecoes.CountAsync(x => x.ColecaoProdutoId == dados.Colecao));
        }
        detalhes = await HtmlAsync(client, $"/Produtos/Detalhes/{dados.SemCategoria}");
        Assert.DoesNotContain("02/01/2020", detalhes);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, url, "Desvincular", dados.SemCategoria)).StatusCode);
        using var verificacao = factory.Services.CreateScope();
        var contexto = verificacao.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.True(await contexto.Produtos.IgnoreQueryFilters().AnyAsync(x => x.Id == dados.SemCategoria));
        Assert.True(await contexto.ColecoesProdutos.IgnoreQueryFilters().AnyAsync(x => x.Id == dados.Colecao));
        Assert.Equal(2, await contexto.ProdutosColecoes.IgnoreQueryFilters().CountAsync(x => x.ProdutoId == dados.SemCategoria));
        Assert.False((await contexto.ProdutosColecoes.IgnoreQueryFilters().FirstAsync(x => x.ProdutoId == dados.SemCategoria && x.ColecaoProdutoId == dados.Futura)).Destaque);
    }

    [Fact]
    public async Task Seguranca_antiforgery_tenant_e_objetos_invalidos()
    {
        var dados = await PrepararAsync();
        using var anonimo = web.CriarCliente();
        Assert.Equal(HttpStatusCode.Redirect, (await anonimo.GetAsync(Url(dados.Colecao))).StatusCode);
        using var client = await web.CriarClienteAutenticadoAsync(1);
        using var outro = await web.CriarClienteAutenticadoAsync(dados.EmpresaAlheia);
        Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync(Url(dados.Colecao))).StatusCode);
        foreach (var handler in new[] { "Vincular", "DefinirDestaque", "Desvincular" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Url(dados.Colecao) + "?handler=" + handler,
                new FormUrlEncodedContent(new Dictionary<string, string> { ["produtoId"] = dados.SemCategoria.ToString() }))).StatusCode);
            var token = await WebTestHtml.ObterTokenAntiforgeryAsync(outro, "/Produtos/Colecoes");
            Assert.Equal(HttpStatusCode.NotFound, (await outro.PostAsync(Url(dados.Colecao) + "?handler=" + handler,
                new FormUrlEncodedContent(Formulario(token, dados.SemCategoria, true)))).StatusCode);
        }
        foreach (var id in new[] { 0, -1, 999999, dados.Alheio })
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Url(dados.Colecao), "Vincular", id)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, Url(dados.Colecao), "Vincular", dados.SemCategoria)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(client, Url(dados.Colecao), "DefinirDestaque", dados.Alheio, true)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(client, Url(dados.Colecao), "Desvincular", dados.Alheio)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var vinculo = await db.ProdutosColecoes.IgnoreQueryFilters().SingleAsync(x => x.ColecaoProdutoId == dados.Colecao);
        Assert.Equal(1, vinculo.EmpresaId); Assert.False(vinculo.Destaque);
        Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync($"/Produtos/Detalhes/{dados.SemCategoria}")).StatusCode);
    }

    [Fact]
    public async Task Filtro_colecao_sem_colecao_e_composicao_AND()
    {
        var dados = await PrepararAsync();
        using var client = await web.CriarClienteAutenticadoAsync(1);
        await PostAsync(client, Url(dados.Colecao), "Vincular", dados.Inativo);
        await PostAsync(client, Url(dados.Colecao), "Vincular", dados.SemCategoria);
        await PostAsync(client, Url(dados.Colecao), "Vincular", dados.OutraCategoria);
        var html = await HtmlAsync(client, $"/Produtos?colecao={dados.Colecao}");
        Assert.Contains(dados.NomeInativo, html); Assert.Contains(dados.NomeSemCategoria, html);
        Assert.DoesNotContain(dados.NomeMesmaCategoria, html); Assert.DoesNotContain(dados.NomeAlheio, html);
        Assert.Contains("Todas as coleções", html); Assert.Contains("Sem coleção", html);
        Assert.DoesNotContain(dados.NomeColecaoAlheia, html);
        var sem = await HtmlAsync(client, "/Produtos?colecao=sem-colecao");
        Assert.Contains(dados.NomeMesmaCategoria, sem); Assert.DoesNotContain(dados.NomeSemCategoria, sem);
        foreach (var valor in new[] { "0", "-1", "abc", "999999", dados.ColecaoAlheia.ToString() })
        {
            var vazio = await HtmlAsync(client, "/Produtos?colecao=" + valor);
            Assert.DoesNotContain(dados.NomeSemCategoria, vazio); Assert.DoesNotContain(dados.NomeAlheio, vazio);
        }
        var pesquisa = await HtmlAsync(client, $"/Produtos?colecao={dados.Colecao}&q={Uri.EscapeDataString(dados.NomeSemCategoria)}");
        Assert.Contains(dados.NomeSemCategoria, pesquisa); Assert.DoesNotContain(dados.NomeOutraCategoria, pesquisa);
        var categoria = await HtmlAsync(client, $"/Produtos?colecao={dados.Colecao}&categoria={dados.OutraCategoriaId}");
        Assert.Contains(dados.NomeOutraCategoria, categoria); Assert.DoesNotContain(dados.NomeSemCategoria, categoria);
        var incompleta = await HtmlAsync(client, $"/Produtos?colecao={dados.Colecao}&categoria=sem-categoria&filtro=precificacao-incompleta");
        Assert.Contains(dados.NomeSemCategoria, incompleta); Assert.DoesNotContain(dados.NomeInativo, incompleta);
        Assert.DoesNotContain(dados.NomeOutraCategoria, incompleta);
        var abaixo = await HtmlAsync(client, $"/Produtos?colecao={dados.Colecao}&filtro=abaixo-da-margem");
        Assert.DoesNotContain(dados.NomeSemCategoria, abaixo); Assert.DoesNotContain(dados.NomeInativo, abaixo);
        Assert.Contains(dados.NomeMesmaCategoria, await HtmlAsync(client, "/Produtos"));
    }

    [Fact]
    public async Task Colecao_compoe_com_margem_e_nao_altera_fotografia_de_precificacao()
    {
        var dados = await PrepararAsync();
        using var client = await web.CriarClienteAutenticadoAsync(1);
        using (var scope = factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
            var config = await db.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
            config.Atualizar(0, 0, .2m, .01m, .1m);
            var hoje = new DateOnly(2020, 1, 1);
            foreach (var id in new[] { dados.SemCategoria, dados.MesmaCategoria })
            {
                var produto = await db.Produtos.SingleAsync(x => x.Id == id);
                produto.AtualizarDados(produto.Nome, .5m, produto.CategoriaProdutoId);
                var ficha = FichaTecnica.Criar(1, id, 1);
                var insumo = Insumo.Criar(1, "Insumo " + Guid.NewGuid().ToString("N"), CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade);
                db.AddRange(ficha, insumo); await db.SaveChangesAsync();
                db.Add(ItemFichaTecnica.Criar(1, ficha.Id, insumo.Id, 1, null, 0));
                db.Add(PrecoInsumo.Criar(1, insumo.Id, 1, 10, hoje));
                db.Add(RegistroPrecoProduto.Criar(1, id, hoje, 10, .5m, 20, 12, .1m));
                await db.SaveChangesAsync();
            }
        }
        var antes = await HtmlAsync(client, $"/Produtos/Detalhes/{dados.SemCategoria}");
        await PostAsync(client, Url(dados.Colecao), "Vincular", dados.SemCategoria, true);
        await PostAsync(client, Url(dados.Colecao), "DefinirDestaque", dados.SemCategoria, false);
        var filtrado = await HtmlAsync(client, $"/Produtos?colecao={dados.Colecao}&categoria=sem-categoria&q={Uri.EscapeDataString(dados.NomeSemCategoria)}&filtro=abaixo-da-margem");
        Assert.Contains(dados.NomeSemCategoria, filtrado);
        Assert.DoesNotContain(dados.NomeMesmaCategoria, filtrado);
        var incompleta = await HtmlAsync(client, $"/Produtos?colecao={dados.Colecao}&filtro=precificacao-incompleta");
        Assert.DoesNotContain(dados.NomeSemCategoria, incompleta);
        var durante = await HtmlAsync(client, $"/Produtos/Detalhes/{dados.SemCategoria}");
        Assert.Equal(antes.Split("<dl")[2].Split("</dl>")[0], durante.Split("<dl")[2].Split("</dl>")[0]);
        await PostAsync(client, Url(dados.Colecao), "Desvincular", dados.SemCategoria);
        using var verificacao = factory.Services.CreateScope();
        var contexto = verificacao.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Single(await contexto.RegistrosPrecosProdutos.IgnoreQueryFilters().Where(x => x.ProdutoId == dados.SemCategoria).ToListAsync());
        Assert.Single(await contexto.FichasTecnicas.IgnoreQueryFilters().Where(x => x.ProdutoId == dados.SemCategoria).ToListAsync());
    }

    [Fact]
    public async Task Dois_POSTs_apos_precheck_persistem_uma_linha_sem_500()
    {
        var barreira = new BarreiraPreCheck();
        using var concorrente = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<PrecificadorDbContext>(options => options.AddInterceptors(barreira))));
        var webConcorrente = new WebTestContext(concorrente);
        using var primeiro = await webConcorrente.CriarClienteAutenticadoAsync(1);
        using var segundo = await webConcorrente.CriarClienteAutenticadoAsync(1);
        int colecaoId, produtoId;
        using (var preparacao = concorrente.Services.CreateScope())
        {
            var options = preparacao.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
            var produto = Produto.Criar(1, "Concorrente " + Guid.NewGuid().ToString("N"), .2m);
            var colecao = ColecaoProduto.Criar(1, "Concorrente " + Guid.NewGuid().ToString("N"), new(2020, 1, 1), null);
            db.AddRange(produto, colecao); await db.SaveChangesAsync();
            colecaoId = colecao.Id; produtoId = produto.Id;
        }
        var url = Url(colecaoId);
        var token1 = await WebTestHtml.ObterTokenAntiforgeryAsync(primeiro, url);
        var token2 = await WebTestHtml.ObterTokenAntiforgeryAsync(segundo, url);
        barreira.Ativa = true;
        var respostas = await Task.WhenAll(
            primeiro.PostAsync(url + "?handler=Vincular", new FormUrlEncodedContent(Formulario(token1, produtoId, true))),
            segundo.PostAsync(url + "?handler=Vincular", new FormUrlEncodedContent(Formulario(token2, produtoId, false))));
        barreira.Ativa = false;
        Assert.Equal(2, barreira.Chegadas);
        Assert.Single(respostas, x => x.StatusCode == HttpStatusCode.Redirect);
        var perdedora = Assert.Single(respostas, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Contains("Este produto já está vinculado à coleção.", await WebTestHtml.LerHtmlDecodificadoAsync(perdedora));
        using var scope = concorrente.Services.CreateScope();
        var verificacao = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(1, await verificacao.ProdutosColecoes.IgnoreQueryFilters().CountAsync(x => x.ProdutoId == produtoId && x.ColecaoProdutoId == colecaoId));
    }

    private sealed class BarreiraPreCheck : DbCommandInterceptor
    {
        private readonly ConcurrentDictionary<Guid, byte> contextos = new();
        private readonly TaskCompletionSource liberar = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Ativa { get; set; }
        public int Chegadas => contextos.Count;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (Ativa && command.CommandText.Contains("FROM [ProdutosColecoes] AS", StringComparison.Ordinal)
                && command.CommandText.Contains("CASE", StringComparison.Ordinal)
                && contextos.TryAdd(eventData.Context!.ContextId.InstanceId, 0))
            {
                if (contextos.Count == 2) liberar.TrySetResult();
                await liberar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }

    private static string Url(int id) => $"/Produtos/Colecoes/Produtos/{id}";
    private static async Task<string> HtmlAsync(HttpClient client, string url) =>
        await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(url));
    private static Dictionary<string, string> Formulario(string token, int produto, bool destaque) => new()
    {
        ["__RequestVerificationToken"] = token, ["Input.ProdutoId"] = produto.ToString(),
        ["Input.Destaque"] = destaque.ToString(), ["produtoId"] = produto.ToString(), ["destaque"] = destaque.ToString(),
        ["Input.EmpresaId"] = "999999", ["EmpresaId"] = "999999", ["ColecaoProdutoId"] = "999999"
    };
    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, string handler, int produto, bool destaque = false)
    {
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, url);
        return await client.PostAsync(url + "?handler=" + handler, new FormUrlEncodedContent(Formulario(token, produto, destaque)));
    }

    private async Task<Dados> PrepararAsync()
    {
        var empresa2 = await web.CriarEmpresaAsync();
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        var sufixo = Guid.NewGuid().ToString("N");
        var categoria = CategoriaProduto.Criar(1, "Categoria A " + sufixo, FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0);
        var outraCategoria = CategoriaProduto.Criar(1, "Categoria B " + sufixo, FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0);
        var colecao = ColecaoProduto.Criar(1, "Passada " + sufixo, new(2020, 1, 1), new(2020, 1, 2));
        var futura = ColecaoProduto.Criar(1, "Futura " + sufixo, new(2099, 1, 1), null);
        var aberta = ColecaoProduto.Criar(1, "Aberta " + sufixo, new(2020, 1, 1), null);
        db.AddRange(categoria, outraCategoria, colecao, futura, aberta); await db.SaveChangesAsync();
        db.Add(new ColecaoProdutoCategoria(colecao, categoria.Id));
        var inativo = Produto.Criar(1, "Inativo " + sufixo, .2m); inativo.Desativar();
        var sem = Produto.Criar(1, "SemCategoria " + sufixo, .2m);
        var outra = Produto.Criar(1, "OutraCategoria " + sufixo, .2m, outraCategoria.Id);
        var mesma = Produto.Criar(1, "MesmaCategoria " + sufixo, .2m, categoria.Id);
        db.AddRange(inativo, sem, outra, mesma); await db.SaveChangesAsync();
        await using var outro = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresa2));
        var alheio = Produto.Criar(empresa2, "Alheio " + sufixo, .2m);
        var alheia = ColecaoProduto.Criar(empresa2, "Alheia " + sufixo, new(2020, 1, 1), null);
        outro.AddRange(alheio, alheia); await outro.SaveChangesAsync();
        return new(colecao.Id, futura.Id, aberta.Id, inativo.Id, sem.Id, outra.Id, mesma.Id, alheio.Id, alheia.Id,
            empresa2, categoria.Id, outraCategoria.Id, colecao.Nome, futura.Nome, aberta.Nome, inativo.Nome, sem.Nome, outra.Nome, mesma.Nome, alheio.Nome, alheia.Nome);
    }
    private sealed record Dados(int Colecao, int Futura, int Aberta, int Inativo, int SemCategoria, int OutraCategoria, int MesmaCategoria,
        int Alheio, int ColecaoAlheia, int EmpresaAlheia, int Categoria, int OutraCategoriaId, string NomeColecao, string NomeFutura,
        string NomeAberta, string NomeInativo, string NomeSemCategoria, string NomeOutraCategoria, string NomeMesmaCategoria,
        string NomeAlheio, string NomeColecaoAlheia);
}
