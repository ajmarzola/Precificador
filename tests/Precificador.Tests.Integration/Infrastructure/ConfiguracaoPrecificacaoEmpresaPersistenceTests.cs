using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ConfiguracaoPrecificacaoEmpresaPersistenceTests
{
    private const string MigracaoMel022 = "20260920172653_ReplaceHourlyLaborWithPercentage";

    [Fact]
    public async Task MEL022_P1_P5_P10_Migra_dados_existentes_da_MEL020_sem_colunas_legadas()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("MaoDeObraPercentual");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.GetService<IMigrator>().MigrateAsync("20260918122710_InitialSqlServer");

        await contexto.Database.ExecuteSqlRawAsync(
            "UPDATE ConfiguracoesPrecificacaoEmpresas SET ValorHoraTrabalho = 87.5 WHERE EmpresaId = 1");
        await contexto.Database.ExecuteSqlRawAsync(
            "INSERT INTO Empresas (Ativo, Nome, NomeNormalizado, TimeZoneId) VALUES (1, 'Empresa legada', 'EMPRESA LEGADA', 'America/Sao_Paulo')");
        var empresaLegadaId = await contexto.Database.SqlQueryRaw<int>(
            "SELECT Id AS Value FROM Empresas WHERE Nome = 'Empresa legada'").SingleAsync();
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO ConfiguracoesPrecificacaoEmpresas (EmpresaId, ValorHoraTrabalho) VALUES ({empresaLegadaId}, 125.25)");
        await contexto.Database.ExecuteSqlRawAsync(
            "INSERT INTO Produtos (EmpresaId, Nome, NomeNormalizado, MargemAlvo, Ativo) VALUES (1, 'Produto legado', 'PRODUTO LEGADO', 0.2, 1)");
        var produtoLegadoId = await contexto.Database.SqlQueryRaw<int>(
            "SELECT Id AS Value FROM Produtos WHERE Nome = 'Produto legado'").SingleAsync();
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO FichasTecnicas (EmpresaId, ProdutoId, Rendimento, TempoAtivoMinutos) VALUES (1, {produtoLegadoId}, 3, 95)");

        // A partir da MEL020 pode haver outras migrations pendentes além da MEL022 (ex.: UC032); o teste
        // migra explicitamente até a MEL022 para validar o contrato dela sem assumir que é a última do projeto.
        Assert.Contains(MigracaoMel022, await contexto.Database.GetPendingMigrationsAsync());
        await contexto.GetService<IMigrator>().MigrateAsync(MigracaoMel022);

        var percentualSeed = await contexto.Database.SqlQueryRaw<decimal>(
            "SELECT PercentualMaoDeObra AS Value FROM ConfiguracoesPrecificacaoEmpresas WHERE EmpresaId = 1").SingleAsync();
        var percentualLegado = await contexto.Database.SqlQueryRaw<decimal>(
            "SELECT PercentualMaoDeObra AS Value FROM ConfiguracoesPrecificacaoEmpresas WHERE EmpresaId = {0}", empresaLegadaId).SingleAsync();
        Assert.Equal(.10m, percentualSeed);
        Assert.Equal(.10m, percentualLegado);

        var colunasAntigas = await contexto.Database.SqlQueryRaw<string>(
            "SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE (TABLE_NAME = 'ConfiguracoesPrecificacaoEmpresas' AND COLUMN_NAME = 'ValorHoraTrabalho') OR (TABLE_NAME = 'FichasTecnicas' AND COLUMN_NAME = 'TempoAtivoMinutos')").ToListAsync();
        Assert.Empty(colunasAntigas);
        var definicaoPercentual = await contexto.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(DATA_TYPE, '(', NUMERIC_PRECISION, ',', NUMERIC_SCALE, ')/', IS_NULLABLE) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ConfiguracoesPrecificacaoEmpresas' AND COLUMN_NAME = 'PercentualMaoDeObra'").SingleAsync();
        Assert.Equal("decimal(9,6)/NO", definicaoPercentual);
        Assert.DoesNotContain(MigracaoMel022, await contexto.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task P1_Migration_em_banco_vazio_cria_tabela_e_configuracao_da_empresa_tecnica()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 1);

        await contexto.Database.MigrateAsync();

        var tabelas = await contexto.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables")
            .ToListAsync();
        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        Assert.Contains("ConfiguracoesPrecificacaoEmpresas", tabelas);
        Assert.Equal(1, configuracao.EmpresaId);
        Assert.Equal(0.10m, configuracao.PercentualMaoDeObra);
    }

    [Fact]
    public async Task P4_PK_EmpresaId_impede_segunda_configuracao_para_mesma_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();

        contexto.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P5_FK_impede_configuracao_para_empresa_inexistente()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 999);
        await contexto.Database.MigrateAsync();

        contexto.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(999));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P6_GQF_nao_retorna_configuracao_de_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using (var contexto = CriarContexto(connectionString, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            await using var contextoEmpresaDois = CriarContexto(connectionString, 2);
            contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaDois.Id));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using var empresaUm = CriarContexto(connectionString, 1);
        await using var empresaDoisConsulta = CriarContexto(connectionString, 2);

        Assert.Equal(1, (await empresaUm.ConfiguracoesPrecificacaoEmpresas.SingleAsync()).EmpresaId);
        Assert.Equal(2, (await empresaDoisConsulta.ConfiguracoesPrecificacaoEmpresas.SingleAsync()).EmpresaId);
    }

    [Fact]
    public async Task P7_Guard_central_rejeita_escrita_cross_tenant()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        contexto.Empresas.Add(Empresa.Criar("Empresa dois"));
        await contexto.SaveChangesAsync();

        contexto.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(2));

        await Assert.ThrowsAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P8_Roundtrip_preserva_precisao_decimal_e_nulls()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        await contexto.Database.ExecuteSqlRawAsync(
            """
            UPDATE ConfiguracoesPrecificacaoEmpresas
            SET PercentualMaoDeObra = @percentualMaoDeObra,
                TarifaEnergiaKwh = NULL,
                MargemPadrao = @margemPadrao,
                IncrementoComercial = @incrementoComercial,
                ReservaComercialDesconto = @reservaComercialDesconto
            WHERE EmpresaId = @empresaId
            """,
            SqlDecimalParameter.Criar("percentualMaoDeObra", 1.234567m, precision: 9, scale: 6),
            SqlDecimalParameter.Criar("margemPadrao", 0.075m, precision: 9, scale: 6),
            SqlDecimalParameter.Criar("incrementoComercial", 0.500001m, precision: 18, scale: 6),
            SqlDecimalParameter.Criar("reservaComercialDesconto", 0.125m, precision: 9, scale: 6),
            new SqlParameter("empresaId", 1));
        contexto.ChangeTracker.Clear();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();

        Assert.Equal(1.234567m, configuracao.PercentualMaoDeObra);
        Assert.Null(configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.075m, configuracao.MargemPadrao);
        Assert.Equal(0.500001m, configuracao.IncrementoComercial);
        Assert.Equal(0.125m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P1_Roundtrip_de_atualizacao_persiste_valores_e_nulls_com_precisao()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(1.234567m, null, 0.075m, 0.500001m, 0.125m);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var atualizada = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        Assert.Equal(1.234567m, atualizada.PercentualMaoDeObra);
        Assert.Null(atualizada.TarifaEnergiaKwh);
        Assert.Equal(0.075m, atualizada.MargemPadrao);
        Assert.Equal(0.500001m, atualizada.IncrementoComercial);
        Assert.Equal(0.125m, atualizada.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P2_Alteracao_da_configuracao_de_uma_empresa_nao_altera_outra()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using (var contexto = CriarContexto(connectionString, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            await using var contextoEmpresaDois = CriarContexto(connectionString, empresaDois.Id);
            contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaDois.Id));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using (var contextoEmpresaUm = CriarContexto(connectionString, 1))
        {
            var configuracao = await contextoEmpresaUm.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
            configuracao.Atualizar(0.10m, 1m, 0.20m, 0.50m, 0.15m);
            await contextoEmpresaUm.SaveChangesAsync();
        }

        await using var contextoEmpresaDoisConsulta = CriarContexto(connectionString, 2);
        var configuracaoEmpresaDois = await contextoEmpresaDoisConsulta.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        Assert.Equal(0.10m, configuracaoEmpresaDois.PercentualMaoDeObra);
        Assert.Null(configuracaoEmpresaDois.TarifaEnergiaKwh);
        Assert.Null(configuracaoEmpresaDois.MargemPadrao);
        Assert.Null(configuracaoEmpresaDois.IncrementoComercial);
        Assert.Equal(0.10m, configuracaoEmpresaDois.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P3_Guard_central_rejeita_alteracao_tecnica_cross_tenant_de_configuracao_existente()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using (var contexto = CriarContexto(connectionString, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            await using var contextoEmpresaDois = CriarContexto(connectionString, empresaDois.Id);
            contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaDois.Id));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using (var contextoEmpresaDois = CriarContexto(connectionString, 2))
        {
            var configuracao = await contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
            configuracao.Atualizar(0.77m, 7m, 0.70m, 7m, 0.17m);
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using (var contextoEmpresaUm = CriarContexto(connectionString, 1))
        {
            var configuracaoDeOutraEmpresa = await contextoEmpresaUm.ConfiguracoesPrecificacaoEmpresas
                .IgnoreQueryFilters()
                .SingleAsync(configuracao => configuracao.EmpresaId == 2);
            configuracaoDeOutraEmpresa.Atualizar(0.11m, 1m, 0.10m, 1m, 0.05m);

            await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
        }

        await using var consultaEmpresaDois = CriarContexto(connectionString, 2);
        var configuracaoPreservada = await consultaEmpresaDois.ConfiguracoesPrecificacaoEmpresas
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(0.77m, configuracaoPreservada.PercentualMaoDeObra);
        Assert.Equal(7m, configuracaoPreservada.TarifaEnergiaKwh);
        Assert.Equal(0.70m, configuracaoPreservada.MargemPadrao);
        Assert.Equal(7m, configuracaoPreservada.IncrementoComercial);
        Assert.Equal(0.17m, configuracaoPreservada.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P4_Atualizacao_nao_cria_segunda_configuracao()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        var quantidadeAntes = await contexto.ConfiguracoesPrecificacaoEmpresas.CountAsync();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(0.10m, 1m, 0.20m, 0.50m, 0.15m);
        await contexto.SaveChangesAsync();

        Assert.Equal(quantidadeAntes, await contexto.ConfiguracoesPrecificacaoEmpresas.CountAsync());
    }

    [Fact]
    public async Task UC027_P5_Produtos_existentes_mantem_margem_apos_mudanca_de_margem_padrao()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConfiguracaoPrecificacao");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        var produto = Produto.Criar(1, "Produto existente", 0.30m);
        contexto.Produtos.Add(produto);
        await contexto.SaveChangesAsync();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(0.10m, null, 0.10m, null, 0.10m);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var produtoAtualizado = await contexto.Produtos.SingleAsync(item => item.Id == produto.Id);
        Assert.Equal(0.30m, produtoAtualizado.MargemAlvo);
    }

    private static PrecificadorDbContext CriarContexto(string connectionString, int empresaId) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options,
        new ContextoEmpresa(empresaId));

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

