using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class UsoEquipamentoFichaPageTests
{
    [Fact]
    public async Task W1_W2_W3_W4_RotasCanonicas_NovoPrgDuplicidadeEParsing()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var semFicha = await ambiente.CriarProdutoAsync(1, true);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);
        var redirecionamento = await client.GetAsync($"/Produtos/FichaTecnica/{semFicha}/Equipamentos/Novo");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, redirecionamento.StatusCode);

        var produto = await ambiente.CriarProdutoAsync(1, true); await ambiente.CriarFichaAsync(1, produto);
        foreach (var (nome, potencia) in new[] { ("Forno", "0,3"), ("Prensa", "1,5"), ("Plotter", "2.2") })
        {
            var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Novo");
            Assert.Equal(System.Net.HttpStatusCode.OK, pagina.StatusCode);
            var post = await client.PostAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Novo", await ambiente.FormularioAsync(pagina, nome, potencia, "10"));
            Assert.Equal(System.Net.HttpStatusCode.Redirect, post.StatusCode);
        }
        Assert.Equal(3, await ambiente.ContarUsosAsync(1));
        var duplicado = await client.GetAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Novo");
        var respostaDuplicada = await client.PostAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Novo", await ambiente.FormularioAsync(duplicado, " forno ", "1", "10"));
        Assert.Contains("já foi adicionado", await WebTestHtml.LerHtmlDecodificadoAsync(respostaDuplicada));
        var paginaInvalida = await client.GetAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Novo");
        var invalido = await client.PostAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Novo", await ambiente.FormularioAsync(paginaInvalida, "", "0", "0"));
        var htmlInvalido = await WebTestHtml.LerHtmlDecodificadoAsync(invalido);
        Assert.Contains("obrigatório", htmlInvalido); Assert.Contains("maior que zero", htmlInvalido);
    }

    [Fact]
    public async Task W5_W6_W7_W8_EditarRemoverInativoECrossTenant()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var produto = await ambiente.CriarProdutoAsync(1, false); var ficha = await ambiente.CriarFichaAsync(1, produto); var uso = await ambiente.CriarUsoAsync(1, ficha, "Forno", 1m, 60);
        var empresaDois = await ambiente.Web.CriarEmpresaAsync(); var produtoDois = await ambiente.CriarProdutoAsync(empresaDois, true); var fichaDois = await ambiente.CriarFichaAsync(empresaDois, produtoDois); var usoDois = await ambiente.CriarUsoAsync(empresaDois, fichaDois, "Externo", 1m, 10);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);
        var editar = await client.GetAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Editar/{uso}"); Assert.Equal(System.Net.HttpStatusCode.OK, editar.StatusCode);
        var postEditar = await client.PostAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Editar/{uso}", await ambiente.FormularioAsync(editar, "Forno ajustado", "1,5", "30")); Assert.Equal(System.Net.HttpStatusCode.Redirect, postEditar.StatusCode);
        var usoAtualizado = await ambiente.DadosUsoAsync(uso, 1);
        Assert.Equal("Forno ajustado", usoAtualizado.Nome); Assert.Equal(1.5m, usoAtualizado.PotenciaKw); Assert.Equal(30, usoAtualizado.TempoUsoMinutos);
        Assert.Equal((1, ficha), await ambiente.OwnershipAsync(uso, 1)); Assert.False(await ambiente.ProdutoAtivoAsync(produto, 1));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Editar/{usoDois}")).StatusCode);
        var paginaPropria = await client.GetAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Editar/{uso}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.PostAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Editar/{usoDois}", await ambiente.FormularioAsync(paginaPropria, "Manipulado", "1", "1"))).StatusCode);
        var remover = await client.GetAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Remover/{uso}"); var postRemover = await client.PostAsync($"/Produtos/FichaTecnica/{produto}/Equipamentos/Remover/{uso}", await ambiente.TokenAsync(remover)); Assert.Equal(System.Net.HttpStatusCode.Redirect, postRemover.StatusCode);
        Assert.Equal(0, await ambiente.ContarUsosAsync(1));
    }

    private static void AssertExibeCustoEnergiaLote(string html, string valor) =>
        Assert.Matches($"Custo de energia do lote:</strong>\\s*{System.Text.RegularExpressions.Regex.Escape(FormatarMonetarioSeNecessario(valor))}", html);

    private static void AssertExibeUsoEnergia(string html, string equipamento, string consumo, string custo)
    {
        var custoEsperado = custo == "indisponível"
            ? "\\u2014"
            : System.Text.RegularExpressions.Regex.Escape(FormatarMonetarioSeNecessario(custo));
        Assert.Matches($"<td>{System.Text.RegularExpressions.Regex.Escape(equipamento)}</td>\\s*<td>[^<]*</td>\\s*<td>[^<]*</td>\\s*<td>{System.Text.RegularExpressions.Regex.Escape(consumo)}</td>\\s*<td>{custoEsperado}</td>", html);
    }

    [Fact]
    public async Task MEL023_W1_W2_W3_W4_W5_W7_W8_W9_W10_SemanticaEletricaEOpcionalidadeDaTarifa()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var produto = await ambiente.CriarProdutoAsync(1, true);
        var ficha = await ambiente.CriarFichaAsync(1, produto, 2m);
        await ambiente.TarifaAsync(1, null);

        var semUso = await ambiente.FichaAsync(produto);

        Assert.Contains("Equipamentos elétricos (opcional)", semUso);
        Assert.Contains("Adicionar equipamento elétrico", semUso);
        Assert.Contains("Nenhum equipamento elétrico adicionado.", semUso);
        Assert.Contains("não é necessário cadastrar nenhum", semUso);
        AssertExibeCustoEnergiaLote(semUso, "0");
        Assert.DoesNotContain("Configurar tarifa de energia", semUso);
        Assert.DoesNotContain("Tarifa de energia não configurada.", semUso);

        await ambiente.CriarUsoAsync(1, ficha, "Forno", 2m, 30);
        var semTarifa = await ambiente.FichaAsync(produto);

        Assert.Contains("Configurar tarifa de energia", semTarifa);
        Assert.Contains("Tarifa de energia não configurada.", semTarifa);
        AssertExibeUsoEnergia(semTarifa, "Forno", "1", "indisponível");
        AssertExibeCustoEnergiaLote(semTarifa, "indisponível");

        await ambiente.TarifaAsync(1, 0m);
        var tarifaZero = await ambiente.FichaAsync(produto);

        AssertExibeUsoEnergia(tarifaZero, "Forno", "1", "0");
        AssertExibeCustoEnergiaLote(tarifaZero, "0");
        Assert.DoesNotContain("Configurar tarifa de energia", tarifaZero);
        Assert.DoesNotContain("Tarifa de energia não configurada.", tarifaZero);
    }

    [Fact]
    public async Task MEL023_W11_W12_W13_W14_W15_ConfigurarTarifaSalvaRendimentoValidoERejeitaInvalido()
    {
        await using var ambiente = await Ambiente.CriarAsync();
        var produto = await ambiente.CriarProdutoAsync(1, false);
        var ficha = await ambiente.CriarFichaAsync(1, produto, 2m);
        await ambiente.CriarUsoAsync(1, ficha, "Forno", 1m, 60);
        await ambiente.TarifaAsync(1, null);
        using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1);

        var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        var post = await client.PostAsync(
            $"/Produtos/FichaTecnica/{produto}?handler=ConfigurarTarifa",
            await ambiente.FormularioFichaAsync(pagina, "3"));

        Assert.Equal(System.Net.HttpStatusCode.Redirect, post.StatusCode);
        Assert.StartsWith("/Configuracoes/Precificacao/Editar", post.Headers.Location!.ToString());
        Assert.Contains(Uri.EscapeDataString($"/Produtos/FichaTecnica/{produto}"), post.Headers.Location!.ToString());
        Assert.Equal(3m, await ambiente.EstadoAsync(ficha, 1));
        Assert.False(await ambiente.ProdutoAtivoAsync(produto, 1));

        var paginaInvalida = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        var invalido = await client.PostAsync(
            $"/Produtos/FichaTecnica/{produto}?handler=ConfigurarTarifa",
            await ambiente.FormularioFichaAsync(paginaInvalida, "0"));
        var htmlInvalido = await WebTestHtml.LerHtmlDecodificadoAsync(invalido);

        Assert.Equal(System.Net.HttpStatusCode.OK, invalido.StatusCode);
        Assert.Contains("O rendimento deve ser maior que zero.", htmlInvalido);
        Assert.Contains("Configurar tarifa de energia", htmlInvalido);
        Assert.Equal(3m, await ambiente.EstadoAsync(ficha, 1));

        var semToken = await client.PostAsync(
            $"/Produtos/FichaTecnica/{produto}?handler=ConfigurarTarifa",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["Input.Rendimento"] = "4" }));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, semToken.StatusCode);
        Assert.Equal(3m, await ambiente.EstadoAsync(ficha, 1));
    }

    [Fact]
    public async Task W9_W10_W11_W12_W13_W14_W15_W16_W17_W18_EnergiaMantemSemanticaEEstadoPersistido()
    {
        await using var ambiente = await Ambiente.CriarAsync(); var produto = await ambiente.CriarProdutoAsync(1, true); var ficha = await ambiente.CriarFichaAsync(1, produto, 2m);
        AssertExibeCustoEnergiaLote(await ambiente.FichaAsync(produto), "0");
        await ambiente.CriarUsoAsync(1, ficha, "Forno", 2m, 30); await ambiente.TarifaAsync(1, 3m);
        var configurada = await ambiente.FichaAsync(produto); AssertExibeUsoEnergia(configurada, "Forno", "1", "3"); AssertExibeCustoEnergiaLote(configurada, "3");
        await ambiente.TarifaAsync(1, null); var semTarifa = await ambiente.FichaAsync(produto); Assert.Contains("indisponível", semTarifa); Assert.Contains("Tarifa de energia não configurada.", semTarifa);
        AssertExibeUsoEnergia(semTarifa, "Forno", "1", "indisponível");
        await ambiente.TarifaAsync(1, 0m); AssertExibeCustoEnergiaLote(await ambiente.FichaAsync(produto), "0");
        await ambiente.TarifaAsync(1, 4m); AssertExibeCustoEnergiaLote(await ambiente.FichaAsync(produto), "4");
        await ambiente.PercentualMaoDeObraAsync(1, 0m); var independente = await ambiente.FichaAsync(produto); Assert.Contains("Custo base dos itens:</strong>", independente); Assert.Matches("Custo de mão de obra do lote:</strong>\\s*indisponível", independente); Assert.Contains("Custo de energia do lote:</strong> R$ 4,00", independente);
        var usosAntesGet = await ambiente.ContarUsosAsync(1); _ = await ambiente.FichaAsync(produto); Assert.Equal(usosAntesGet, await ambiente.ContarUsosAsync(1));
        var antes = await ambiente.EstadoAsync(ficha, 1); using var client = await ambiente.Web.CriarClienteAutenticadoAsync(1); var pagina = await client.GetAsync($"/Produtos/FichaTecnica/{produto}");
        var invalido = await client.PostAsync($"/Produtos/FichaTecnica/{produto}", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync()), ["Input.Rendimento"] = "0" }));
        AssertExibeCustoEnergiaLote(await WebTestHtml.LerHtmlDecodificadoAsync(invalido), "4"); Assert.Equal(antes, await ambiente.EstadoAsync(ficha, 1));
        await ambiente.RemoverConfiguracaoAsync(1); Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync($"/Produtos/FichaTecnica/{produto}")).StatusCode); Assert.Equal(0, await ambiente.ContarConfiguracoesAsync(1));
    }

    private static string FormatarMonetarioSeNecessario(string valor)
    {
        if (valor == "indisponível")
        {
            return valor;
        }

        var cultura = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        var normalizado = valor.Replace('.', ',');
        var convertido = decimal.Parse(normalizado, System.Globalization.NumberStyles.Number, cultura);
        return convertido.ToString("C2", cultura);
    }

    private sealed class Ambiente(CustomWebApplicationFactory factory) : IAsyncDisposable
    {
        private readonly IServiceScope scope = factory.Services.CreateScope(); public WebTestContext Web { get; } = new(factory);
        public static async Task<Ambiente> CriarAsync() { var factory = new CustomWebApplicationFactory(new DateOnly(2026, 9, 15)); _ = factory.Services; return await Task.FromResult(new Ambiente(factory)); }
        public async Task<int> CriarProdutoAsync(int empresa, bool ativo) { await using var c = Contexto(empresa); var p = Produto.Criar(empresa, Guid.NewGuid().ToString(), .3m); if (!ativo) p.Desativar(); c.Produtos.Add(p); await c.SaveChangesAsync(); return p.Id; }
        public async Task<int> CriarFichaAsync(int empresa, int produto, decimal rendimento = 1m) { await using var c = Contexto(empresa); var f = FichaTecnica.Criar(empresa, produto, rendimento); c.FichasTecnicas.Add(f); await c.SaveChangesAsync(); return f.Id; }
        public async Task<int> CriarUsoAsync(int empresa, int ficha, string nome, decimal potencia, int tempo) { await using var c = Contexto(empresa); var u = UsoEquipamentoFicha.Criar(empresa, ficha, nome, potencia, tempo); c.UsosEquipamentosFicha.Add(u); await c.SaveChangesAsync(); return u.Id; }
        public async Task<int> ContarUsosAsync(int empresa) { await using var c = Contexto(empresa); return await c.UsosEquipamentosFicha.CountAsync(); }
        public async Task<(int, int)> OwnershipAsync(int uso, int empresa) { await using var c = Contexto(empresa); var u = await c.UsosEquipamentosFicha.SingleAsync(x => x.Id == uso); return (u.EmpresaId, u.FichaTecnicaId); }
        public async Task<(string Nome, decimal PotenciaKw, int TempoUsoMinutos)> DadosUsoAsync(int uso, int empresa) { await using var c = Contexto(empresa); var u = await c.UsosEquipamentosFicha.SingleAsync(x => x.Id == uso); return (u.NomeEquipamento, u.PotenciaKw, u.TempoUsoMinutos); }
        public async Task<bool> ProdutoAtivoAsync(int produto, int empresa) { await using var c = Contexto(empresa); return (await c.Produtos.SingleAsync(x => x.Id == produto)).Ativo; }
        public async Task<string> FichaAsync(int produto) { using var client = await Web.CriarClienteAutenticadoAsync(1); return await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produto}")); }
        public Task<FormUrlEncodedContent> FormularioAsync(HttpResponseMessage pagina, string nome, string potencia, string tempo) => TokenAsync(pagina, new() { ["Input.NomeEquipamento"] = nome, ["Input.PotenciaKw"] = potencia, ["Input.TempoUsoMinutos"] = tempo });
        public Task<FormUrlEncodedContent> FormularioFichaAsync(HttpResponseMessage pagina, string rendimento) => TokenAsync(pagina, new() { ["Input.Rendimento"] = rendimento });
        public Task<FormUrlEncodedContent> TokenAsync(HttpResponseMessage pagina) => TokenAsync(pagina, new());
        private async Task<FormUrlEncodedContent> TokenAsync(HttpResponseMessage pagina, Dictionary<string, string> dados) { dados["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(await pagina.Content.ReadAsStringAsync()); return new FormUrlEncodedContent(dados); }
        public async Task TarifaAsync(int empresa, decimal? tarifa) { await using var c = Contexto(empresa); await c.Database.ExecuteSqlRawAsync("UPDATE ConfiguracoesPrecificacaoEmpresas SET TarifaEnergiaKwh = @tarifa WHERE EmpresaId = @empresaId", SqlDecimalParameter.Criar("tarifa", tarifa, precision: 18, scale: 6), new SqlParameter("empresaId", empresa)); }
        public async Task PercentualMaoDeObraAsync(int empresa, decimal percentual) { await using var c = Contexto(empresa); await c.Database.ExecuteSqlRawAsync("UPDATE ConfiguracoesPrecificacaoEmpresas SET PercentualMaoDeObra = @percentual WHERE EmpresaId = @empresaId", SqlDecimalParameter.Criar("percentual", percentual, precision: 9, scale: 6), new SqlParameter("empresaId", empresa)); }
        public async Task RemoverConfiguracaoAsync(int empresa) { await using var c = Contexto(empresa); await c.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ConfiguracoesPrecificacaoEmpresas WHERE EmpresaId = {empresa}"); }
        public async Task<int> ContarConfiguracoesAsync(int empresa) { await using var c = Contexto(empresa); return await c.ConfiguracoesPrecificacaoEmpresas.CountAsync(); }
        public async Task<decimal> EstadoAsync(int ficha, int empresa) { await using var c = Contexto(empresa); return (await c.FichasTecnicas.SingleAsync(x => x.Id == ficha)).Rendimento; }
        private PrecificadorDbContext Contexto(int empresa) => new(scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>(), new ContextoEmpresa(empresa));
        public ValueTask DisposeAsync() { scope.Dispose(); factory.Dispose(); return ValueTask.CompletedTask; }
    }
    private sealed class ContextoEmpresa(int empresa) : IEmpresaContext { public int? EmpresaId => empresa; public int EmpresaIdOuSentinela => empresa; public string? TimeZoneId => Empresa.TimeZoneIdPadrao; }
}
