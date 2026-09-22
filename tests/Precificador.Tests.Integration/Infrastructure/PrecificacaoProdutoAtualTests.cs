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
        ficha.AtualizarBase(2m);
        await context.SaveChangesAsync();

        var resultado = await CalcularAsync(context, produto.Id);

        Assert.Equal(Hoje, resultado!.DataOperacional);
        Assert.Equal(2m, resultado.Rendimento);
        Assert.Equal(0m, resultado.PercentualMaoDeObra);
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
        Assert.Null(resultadoSemFicha.PercentualMaoDeObra);

        var produto = await CriarProdutoPrecificavelAsync(context, margemAlvo: .30m, custoInsumo: 10m);
        context.ConfiguracoesPrecificacaoEmpresas.RemoveRange(context.ConfiguracoesPrecificacaoEmpresas);
        await context.SaveChangesAsync();
        var resultadoSemConfiguracao = await CalcularAsync(context, produto.Id);

        Assert.Equal(Hoje, resultadoSemConfiguracao!.DataOperacional);
        Assert.Equal(1m, resultadoSemConfiguracao.Rendimento);
        Assert.Null(resultadoSemConfiguracao.PercentualMaoDeObra);
        Assert.Null(resultadoSemConfiguracao.TarifaEnergiaKwh);
        Assert.Null(resultadoSemConfiguracao.IncrementoComercial);
        Assert.Equal(0, await context.ConfiguracoesPrecificacaoEmpresas.CountAsync());
    }

    [Fact]
    public async Task UC036_Produto_sem_categoria_e_fixo_por_lote_refletem_no_total_unitario_preco_e_margem()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(0m, 0m, null, .5m, .1m);
        var produto = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        var semCategoria = await CalcularAsync(context, produto.Id);
        Assert.Equal(0m, semCategoria!.CustoDesgasteEquipamentosLote);
        var resumoSemCategoria = await new ResumoPrecificacaoProdutosAtual(context, new DataOperacionalFixa(Hoje)).CalcularAsync([produto.Id]);
        Assert.Equal(10m, resumoSemCategoria[produto.Id].CustoUnitarioProduto);

        var categoria = CategoriaProduto.Criar(1, "Fixa", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 1.50m);
        context.CategoriasProdutos.Add(categoria);
        await context.SaveChangesAsync();
        produto.AtualizarDados(produto.Nome, produto.MargemAlvo, categoria.Id);
        var ficha = await context.FichasTecnicas.SingleAsync(f => f.ProdutoId == produto.Id);
        ficha.AtualizarBase(5m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var resultado = await CalcularAsync(context, produto.Id);
        Assert.Equal(1.50m, resultado!.CustoDesgasteEquipamentosLote);
        Assert.Equal(11.50m, resultado.CustoLote);
        Assert.Equal(2.30m, resultado.CustoUnitarioProduto);
        Assert.Equal(3.5m, resultado.PrecoSugerido);
        var resumoFixo = await new ResumoPrecificacaoProdutosAtual(context, new DataOperacionalFixa(Hoje)).CalcularAsync([produto.Id]);
        Assert.Equal(resultado.CustoUnitarioProduto, resumoFixo[produto.Id].CustoUnitarioProduto);
    }

    [Fact]
    public async Task UC036_Percentual_usa_somente_base_e_categoria_inativa_ou_alterada_reflete_no_calculo_atual()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(.10m, 0m, null, .5m, .1m);
        var produto = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(.10m, 0m, null, .5m, .1m);
        var categoria = CategoriaProduto.Criar(1, "Percentual", FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, .05m);
        categoria.Desativar();
        context.CategoriasProdutos.Add(categoria);
        await context.SaveChangesAsync();
        produto.AtualizarDados(produto.Nome, produto.MargemAlvo, categoria.Id);
        var item = await context.ItensFichaTecnica.SingleAsync();
        item.AtualizarDados(1m, null, .20m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var primeiro = await CalcularAsync(context, produto.Id);
        Assert.Equal(.50m, primeiro!.CustoDesgasteEquipamentosLote);
        Assert.Equal(13.50m, primeiro.CustoLote);
        var resumoCategoriaInativa = await new ResumoPrecificacaoProdutosAtual(context, new DataOperacionalFixa(Hoje)).CalcularAsync([produto.Id]);
        Assert.Equal(primeiro.CustoUnitarioProduto, resumoCategoriaInativa[produto.Id].CustoUnitarioProduto);
        var categoriaAtual = await context.CategoriasProdutos.IgnoreQueryFilters().SingleAsync(c => c.Id == categoria.Id);
        categoriaAtual.AtualizarDados(categoriaAtual.Nome, FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, .10m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var segundo = await CalcularAsync(context, produto.Id);
        Assert.Equal(1m, segundo!.CustoDesgasteEquipamentosLote);
        Assert.Equal(14m, segundo.CustoLote);
    }

    [Fact]
    public async Task MEL024_Resumo_em_lote_tem_paridade_e_preserva_estados_incompletos()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var completo = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        var negativo = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        negativo.Desativar();
        var semPreco = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        var itemSemPreco = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        var energiaIncompleta = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        var semFicha = Produto.Criar(1, "Sem ficha", .30m);
        context.Produtos.Add(semFicha);
        await context.SaveChangesAsync();
        context.RegistrosPrecosProdutos.AddRange(
            Registro(1, completo.Id, Hoje, 20m),
            Registro(1, negativo.Id, Hoje, 5m),
            Registro(1, itemSemPreco.Id, Hoje, 20m),
            Registro(1, energiaIncompleta.Id, Hoje, 20m),
            Registro(1, semFicha.Id, Hoje, 20m));
        var fichaItemSemPrecoId = await context.FichasTecnicas.Where(ficha => ficha.ProdutoId == itemSemPreco.Id).Select(ficha => ficha.Id).SingleAsync();
        var insumoItemSemPrecoId = await context.ItensFichaTecnica.Where(item => item.FichaTecnicaId == fichaItemSemPrecoId).Select(item => item.InsumoId).SingleAsync();
        var precoDoItem = await context.PrecosInsumos.SingleAsync(item => item.InsumoId == insumoItemSemPrecoId);
        context.PrecosInsumos.Remove(precoDoItem);
        var fichaEnergiaIncompletaId = await context.FichasTecnicas.Where(ficha => ficha.ProdutoId == energiaIncompleta.Id).Select(ficha => ficha.Id).SingleAsync();
        context.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(1, fichaEnergiaIncompletaId, "Equipamento", 1m, 60));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var ids = new[] { completo.Id, negativo.Id, semPreco.Id, itemSemPreco.Id, energiaIncompleta.Id, semFicha.Id };
        var lote = await new ResumoPrecificacaoProdutosAtual(context, new DataOperacionalFixa(Hoje)).CalcularAsync(ids);

        foreach (var id in ids)
        {
            var individual = await CalcularAsync(context, id);
            var resumo = lote[id];
            Assert.Equal(individual!.CustoUnitarioProduto, resumo.CustoUnitarioProduto);
            Assert.Equal(individual.PrecoPrateleiraAtual, resumo.PrecoPrateleiraAtual);
            Assert.Equal(individual.MargemAtual, resumo.MargemAtual);
            Assert.Equal(individual.SituacaoMargem, resumo.SituacaoMargem);
        }

        Assert.Equal(.5m, lote[completo.Id].MargemAtual);
        Assert.Equal(-1m, lote[negativo.Id].MargemAtual);
        Assert.Null(lote[semPreco.Id].MargemAtual);
        Assert.Null(lote[itemSemPreco.Id].CustoUnitarioProduto);
        Assert.Null(lote[itemSemPreco.Id].MargemAtual);
        Assert.Null(lote[energiaIncompleta.Id].CustoUnitarioProduto);
        Assert.Null(lote[energiaIncompleta.Id].MargemAtual);
        Assert.Equal(20m, lote[semFicha.Id].PrecoPrateleiraAtual);
        Assert.Null(lote[semFicha.Id].CustoUnitarioProduto);
    }

    [Fact]
    public async Task MEL024_Resumo_em_lote_tem_paridade_para_desgaste_fixo_percentual_e_categoria_inativa()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var fixa = CategoriaProduto.Criar(1, "Fixa", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 2m);
        var percentual = CategoriaProduto.Criar(1, "Percentual", FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, .10m);
        var inativa = CategoriaProduto.Criar(1, "Inativa", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 3m);
        inativa.Desativar();
        context.CategoriasProdutos.AddRange(fixa, percentual, inativa);
        await context.SaveChangesAsync();
        var produtoFixo = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        var produtoPercentual = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        var produtoInativo = await CriarProdutoPrecificavelAsync(context, .30m, 10m);
        produtoFixo.AtualizarDados(produtoFixo.Nome, produtoFixo.MargemAlvo, fixa.Id);
        produtoPercentual.AtualizarDados(produtoPercentual.Nome, produtoPercentual.MargemAlvo, percentual.Id);
        produtoInativo.AtualizarDados(produtoInativo.Nome, produtoInativo.MargemAlvo, inativa.Id);
        context.RegistrosPrecosProdutos.AddRange(
            Registro(1, produtoFixo.Id, Hoje, 20m),
            Registro(1, produtoPercentual.Id, Hoje, 20m),
            Registro(1, produtoInativo.Id, Hoje, 20m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var ids = new[] { produtoFixo.Id, produtoPercentual.Id, produtoInativo.Id };
        var lote = await new ResumoPrecificacaoProdutosAtual(context, new DataOperacionalFixa(Hoje)).CalcularAsync(ids);

        foreach (var id in ids)
        {
            var individual = await CalcularAsync(context, id);
            Assert.Equal(individual!.CustoUnitarioProduto, lote[id].CustoUnitarioProduto);
            Assert.Equal(individual.MargemAtual, lote[id].MargemAtual);
            Assert.Equal(individual.SituacaoMargem, lote[id].SituacaoMargem);
        }

        Assert.Equal(12m, lote[produtoFixo.Id].CustoUnitarioProduto);
        Assert.Equal(11m, lote[produtoPercentual.Id].CustoUnitarioProduto);
        Assert.Equal(13m, lote[produtoInativo.Id].CustoUnitarioProduto);
    }

    private static async Task<Produto> CriarProdutoPrecificavelAsync(
        PrecificadorDbContext context,
        decimal margemAlvo,
        decimal custoInsumo)
    {
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(0m, configuracao.TarifaEnergiaKwh, configuracao.MargemPadrao, configuracao.IncrementoComercial, configuracao.ReservaComercialDesconto);
        var produto = Produto.Criar(1, $"Produto {Guid.NewGuid():N}", margemAlvo);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(1, produto.Id, 1m);
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

