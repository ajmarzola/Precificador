using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ConfiguracaoPrecificacaoEmpresaPersistenceTests
{
    private const string MigrationAnterior = "20260914115007_AddInsumoIdentidadeConsolidada";

    [Fact]
    public async Task P1_Migration_em_banco_vazio_cria_tabela_e_configuracao_da_empresa_tecnica()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);

        await contexto.Database.MigrateAsync();

        var tabelas = await contexto.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();
        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        Assert.Contains("ConfiguracoesPrecificacaoEmpresas", tabelas);
        Assert.Equal(1, configuracao.EmpresaId);
    }

    [Fact]
    public async Task P2_P3_Upgrade_com_empresas_existentes_faz_backfill_com_nulls_e_reserva_default()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync(MigrationAnterior);
        await contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Empresas (Nome, NomeNormalizado, TimeZoneId, Ativo)
            VALUES ({"Empresa antiga"}, {"EMPRESA ANTIGA"}, {Empresa.TimeZoneIdPadrao}, {true})
            """);

        await contexto.Database.MigrateAsync();

        var configuracoes = await contexto.ConfiguracoesPrecificacaoEmpresas
            .IgnoreQueryFilters()
            .OrderBy(configuracao => configuracao.EmpresaId)
            .ToListAsync();
        Assert.Equal(2, configuracoes.Count);
        Assert.All(configuracoes, configuracao =>
        {
            Assert.Null(configuracao.ValorHoraTrabalho);
            Assert.Null(configuracao.TarifaEnergiaKwh);
            Assert.Null(configuracao.MargemPadrao);
            Assert.Null(configuracao.IncrementoComercial);
            Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
        });
    }

    [Fact]
    public async Task P4_PK_EmpresaId_impede_segunda_configuracao_para_mesma_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();

        contexto.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P5_FK_impede_configuracao_para_empresa_inexistente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 999);
        await contexto.Database.MigrateAsync();

        contexto.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(999));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P6_GQF_nao_retorna_configuracao_de_outra_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var contexto = CriarContexto(connection, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            await using var contextoEmpresaDois = CriarContexto(connection, 2);
            contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaDois.Id));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using var empresaUm = CriarContexto(connection, 1);
        await using var empresaDoisConsulta = CriarContexto(connection, 2);

        Assert.Equal(1, (await empresaUm.ConfiguracoesPrecificacaoEmpresas.SingleAsync()).EmpresaId);
        Assert.Equal(2, (await empresaDoisConsulta.ConfiguracoesPrecificacaoEmpresas.SingleAsync()).EmpresaId);
    }

    [Fact]
    public async Task P7_Guard_central_rejeita_escrita_cross_tenant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();
        contexto.Empresas.Add(Empresa.Criar("Empresa dois"));
        await contexto.SaveChangesAsync();

        contexto.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(2));

        await Assert.ThrowsAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P8_Roundtrip_preserva_precisao_decimal_e_nulls()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();
        await contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ConfiguracoesPrecificacaoEmpresas
            SET ValorHoraTrabalho = {12.345678m},
                TarifaEnergiaKwh = NULL,
                MargemPadrao = {0.075m},
                IncrementoComercial = {0.500001m},
                ReservaComercialDesconto = {0.125m}
            WHERE EmpresaId = {1}
            """);
        contexto.ChangeTracker.Clear();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();

        Assert.Equal(12.345678m, configuracao.ValorHoraTrabalho);
        Assert.Null(configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.075m, configuracao.MargemPadrao);
        Assert.Equal(0.500001m, configuracao.IncrementoComercial);
        Assert.Equal(0.125m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P1_Roundtrip_de_atualizacao_persiste_valores_e_nulls_com_precisao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(12.345678m, null, 0.075m, 0.500001m, 0.125m);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var atualizada = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        Assert.Equal(12.345678m, atualizada.ValorHoraTrabalho);
        Assert.Null(atualizada.TarifaEnergiaKwh);
        Assert.Equal(0.075m, atualizada.MargemPadrao);
        Assert.Equal(0.500001m, atualizada.IncrementoComercial);
        Assert.Equal(0.125m, atualizada.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P2_Alteracao_da_configuracao_de_uma_empresa_nao_altera_outra()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var contexto = CriarContexto(connection, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            await using var contextoEmpresaDois = CriarContexto(connection, empresaDois.Id);
            contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaDois.Id));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using (var contextoEmpresaUm = CriarContexto(connection, 1))
        {
            var configuracao = await contextoEmpresaUm.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
            configuracao.Atualizar(10m, 1m, 0.20m, 0.50m, 0.15m);
            await contextoEmpresaUm.SaveChangesAsync();
        }

        await using var contextoEmpresaDoisConsulta = CriarContexto(connection, 2);
        var configuracaoEmpresaDois = await contextoEmpresaDoisConsulta.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        Assert.Null(configuracaoEmpresaDois.ValorHoraTrabalho);
        Assert.Null(configuracaoEmpresaDois.TarifaEnergiaKwh);
        Assert.Null(configuracaoEmpresaDois.MargemPadrao);
        Assert.Null(configuracaoEmpresaDois.IncrementoComercial);
        Assert.Equal(0.10m, configuracaoEmpresaDois.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P3_Guard_central_rejeita_alteracao_tecnica_cross_tenant_de_configuracao_existente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var contexto = CriarContexto(connection, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            await using var contextoEmpresaDois = CriarContexto(connection, empresaDois.Id);
            contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(empresaDois.Id));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using (var contextoEmpresaDois = CriarContexto(connection, 2))
        {
            var configuracao = await contextoEmpresaDois.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
            configuracao.Atualizar(77m, 7m, 0.70m, 7m, 0.17m);
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using (var contextoEmpresaUm = CriarContexto(connection, 1))
        {
            var configuracaoDeOutraEmpresa = await contextoEmpresaUm.ConfiguracoesPrecificacaoEmpresas
                .IgnoreQueryFilters()
                .SingleAsync(configuracao => configuracao.EmpresaId == 2);
            configuracaoDeOutraEmpresa.Atualizar(11m, 1m, 0.10m, 1m, 0.05m);

            await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
        }

        await using var consultaEmpresaDois = CriarContexto(connection, 2);
        var configuracaoPreservada = await consultaEmpresaDois.ConfiguracoesPrecificacaoEmpresas
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(77m, configuracaoPreservada.ValorHoraTrabalho);
        Assert.Equal(7m, configuracaoPreservada.TarifaEnergiaKwh);
        Assert.Equal(0.70m, configuracaoPreservada.MargemPadrao);
        Assert.Equal(7m, configuracaoPreservada.IncrementoComercial);
        Assert.Equal(0.17m, configuracaoPreservada.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_P4_Atualizacao_nao_cria_segunda_configuracao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();
        var quantidadeAntes = await contexto.ConfiguracoesPrecificacaoEmpresas.CountAsync();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(10m, 1m, 0.20m, 0.50m, 0.15m);
        await contexto.SaveChangesAsync();

        Assert.Equal(quantidadeAntes, await contexto.ConfiguracoesPrecificacaoEmpresas.CountAsync());
    }

    [Fact]
    public async Task UC027_P5_Produtos_existentes_mantem_margem_apos_mudanca_de_margem_padrao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();
        var produto = Produto.Criar(1, "Produto existente", 0.30m);
        contexto.Produtos.Add(produto);
        await contexto.SaveChangesAsync();

        var configuracao = await contexto.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(null, null, 0.10m, null, 0.10m);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var produtoAtualizado = await contexto.Produtos.SingleAsync(item => item.Id == produto.Id);
        Assert.Equal(0.30m, produtoAtualizado.MargemAlvo);
    }

    private static PrecificadorDbContext CriarContexto(SqliteConnection connection, int empresaId) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(connection).Options,
        new ContextoEmpresa(empresaId));

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
