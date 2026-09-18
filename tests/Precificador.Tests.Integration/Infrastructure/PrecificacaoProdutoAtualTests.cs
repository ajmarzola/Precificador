using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Precificacao;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class PrecificacaoProdutoAtualTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 15);

    [Fact]
    public async Task P1_P2_Orquestracao_usa_registro_atual_por_data_e_id()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, new DateOnly(2026, 9, 1), prateleira: 15m));
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, Hoje, prateleira: 20m));
        await context.SaveChangesAsync();
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, Hoje, prateleira: 25m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.Equal(25m, resultado!.PrecoPrateleiraAtual);
        Assert.Equal(Hoje, resultado.DataReferenciaPrecoAtual);
        Assert.Equal(.60m, resultado.MargemAtual);
    }

    [Fact]
    public async Task P3_Preco_atual_retorna_mesmo_sem_ficha()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var produto = Produto.Criar(1, "Produto sem ficha", .30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, Hoje, prateleira: 20m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.Equal(20m, resultado!.PrecoPrateleiraAtual);
        Assert.Null(resultado.CustoUnitarioProduto);
        Assert.Null(resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.Incompleto, resultado.SituacaoMargem);
        Assert.Contains("A ficha técnica não foi cadastrada.", resultado.ImpedimentosMargemAtual);
    }

    [Fact]
    public async Task P4_P5_Usa_custo_e_margem_alvo_atuais_em_vez_de_snapshots()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, Hoje, custoReferencia: 90m, margemReferencia: .90m, prateleira: 20m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.Equal(10m, resultado!.CustoUnitarioProduto);
        Assert.Equal(.50m, resultado.MargemAtual);
        Assert.Equal(.30m, resultado.MargemAlvo);
        Assert.Equal(SituacaoMargemProduto.DentroDaMargem, resultado.SituacaoMargem);
    }

    [Fact]
    public async Task P6_Incremento_null_nao_torna_margem_incompleta()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, Hoje, prateleira: 20m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.False(resultado!.PrecoProdutoCompleto);
        Assert.Equal(.50m, resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.DentroDaMargem, resultado.SituacaoMargem);
        Assert.Contains("Incremento comercial não configurado.", resultado.Impedimentos);
        Assert.DoesNotContain("Incremento comercial não configurado.", resultado.ImpedimentosMargemAtual);
    }

    [Fact]
    public async Task P7_Gqf_impede_usar_registro_comercial_de_outra_empresa()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        context.Empresas.Add(Empresa.Criar("Empresa dois"));
        await context.SaveChangesAsync();
        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO RegistrosPrecosProdutos
                (EmpresaId, ProdutoId, DataReferencia, CustoReferencia, MargemReferencia, PrecoSugerido, PrecoPrateleira, ReservaComercialReferencia)
            VALUES
                (@empresaId, @produtoId, @dataReferencia, @custoReferencia, @margemReferencia, @precoSugerido, @precoPrateleira, @reservaComercialReferencia)
            """,
            new SqlParameter("empresaId", 2),
            new SqlParameter("produtoId", produto.Id),
            new SqlParameter("dataReferencia", System.Data.SqlDbType.Date) { Value = Hoje.ToDateTime(TimeOnly.MinValue) },
            SqlDecimalParameter.Criar("custoReferencia", 10m, precision: 18, scale: 6),
            SqlDecimalParameter.Criar("margemReferencia", 0.30m, precision: 9, scale: 6),
            SqlDecimalParameter.Criar("precoSugerido", 14.29m, precision: 18, scale: 6),
            SqlDecimalParameter.Criar("precoPrateleira", 99m, precision: 18, scale: 6),
            SqlDecimalParameter.Criar("reservaComercialReferencia", 0.10m, precision: 9, scale: 6));
        context.ChangeTracker.Clear();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.Null(resultado!.PrecoPrateleiraAtual);
        Assert.Null(resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.Incompleto, resultado.SituacaoMargem);
        Assert.Contains("Preço de prateleira não definido.", resultado.ImpedimentosMargemAtual);
    }

    [Fact]
    public async Task P8_Execucao_nao_persiste_resultado_derivado()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, Hoje, prateleira: 20m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.Equal(.50m, resultado!.MargemAtual);
        Assert.Equal(0, await context.SaveChangesAsync());
        Assert.Equal(1, await context.RegistrosPrecosProdutos.CountAsync());
        Assert.Equal(.30m, (await context.Produtos.SingleAsync(p => p.Id == produto.Id)).MargemAlvo);
    }

    [Fact]
    public async Task UC025_P1_P2_P3_Resultado_expoe_parametros_da_mesma_fotografia_e_preserva_zero()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(0m, 0m, null, .5m, .1m);
        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        var ficha = await context.FichasTecnicas.SingleAsync(f => f.ProdutoId == produto.Id);
        ficha.AtualizarBase(2m, 30);
        await context.SaveChangesAsync();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.Equal(Hoje, resultado!.DataOperacional);
        Assert.Equal(2m, resultado.Rendimento);
        Assert.Equal(30, resultado.TempoAtivoMinutos);
        Assert.Equal(0m, resultado.ValorHoraTrabalho);
        Assert.Equal(0m, resultado.TarifaEnergiaKwh);
        Assert.Equal(.5m, resultado.IncrementoComercial);
    }

    [Fact]
    public async Task UC025_P4_P5_Retorno_incompleto_preserva_apenas_entradas_conhecidas()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var semFicha = Produto.Criar(1, "Produto sem ficha UC025", .30m);
        context.Produtos.Add(semFicha);
        await context.SaveChangesAsync();
        var resultadoSemFicha = await CalcularAsync(context, semFicha.Id);

        Assert.Equal(Hoje, resultadoSemFicha!.DataOperacional);
        Assert.Null(resultadoSemFicha.Rendimento);
        Assert.Null(resultadoSemFicha.TempoAtivoMinutos);

        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        context.ConfiguracoesPrecificacaoEmpresas.RemoveRange(context.ConfiguracoesPrecificacaoEmpresas);
        await context.SaveChangesAsync();
        var resultadoSemConfiguracao = await CalcularAsync(context, produto.Id);

        Assert.Equal(Hoje, resultadoSemConfiguracao!.DataOperacional);
        Assert.Equal(1m, resultadoSemConfiguracao.Rendimento);
        Assert.Equal(0, resultadoSemConfiguracao.TempoAtivoMinutos);
        Assert.Null(resultadoSemConfiguracao.ValorHoraTrabalho);
        Assert.Null(resultadoSemConfiguracao.TarifaEnergiaKwh);
        Assert.Null(resultadoSemConfiguracao.IncrementoComercial);
        Assert.Equal(0, await context.ConfiguracoesPrecificacaoEmpresas.CountAsync());
    }

    private static async Task<Produto> CriarProdutoPrecificavelAsync(
        PrecificadorDbContext context,
        decimal margemAlvo,
        decimal custoInsumo)
    {
        var produto = Produto.Criar(1, $"Produto {Guid.NewGuid():N}", margemAlvo);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(1, produto.Id, 1m, 0);
        var insumo = Insumo.Criar(1, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade);
        context.FichasTecnicas.Add(ficha);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, ficha.Id, insumo.Id, 1m, null, 0m));
        context.PrecosInsumos.Add(PrecoInsumo.Criar(1, insumo.Id, 1m, custoInsumo, Hoje));
        await context.SaveChangesAsync();
        return produto;
    }

    private static RegistroPrecoProduto Registro(
        int empresaId,
        int produtoId,
        DateOnly data,
        decimal prateleira,
        decimal custoReferencia = 10m,
        decimal margemReferencia = .30m) =>
        RegistroPrecoProduto.Criar(empresaId, produtoId, data, custoReferencia, margemReferencia, 14.29m, prateleira, .10m);

    private static Task<ResultadoPrecificacaoProdutoAtual?> CalcularAsync(PrecificadorDbContext context, int produtoId) =>
        new PrecificacaoProdutoAtual(context, new DataOperacionalFixa(Hoje)).CalcularAsync(produtoId);

    private static async Task<PrecificadorDbContext> CriarContextoAsync(int empresaId)
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecificacaoProdutoAtual");
        return new PrecificadorDbContext(
            new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options,
            new Contexto(empresaId));
    }

    private sealed class DataOperacionalFixa(DateOnly hoje) : IDataOperacionalEmpresa
    {
        public DateOnly Hoje => hoje;
    }

    private sealed class Contexto(int id) : IEmpresaContext
    {
        public int? EmpresaId => id;
        public int EmpresaIdOuSentinela => id;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

