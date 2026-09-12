using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class PrecoInsumoConsultaTests
{
    private static readonly DateOnly DataOperacional = new(2026, 9, 11);

    [Fact]
    public async Task CA03_CA04_Consulta_ordena_por_data_e_id_e_seleciona_vigente()
    {
        await using var connection = await AbrirAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var insumo = await CriarInsumoAsync(context, 1);
        var passado = PrecoInsumo.Criar(1, insumo.Id, 1m, 8m, new DateOnly(2026, 9, 1));
        var atualMaisAntigo = PrecoInsumo.Criar(1, insumo.Id, 1m, 10m, DataOperacional);
        var atualMaisNovo = PrecoInsumo.Criar(1, insumo.Id, 1m, 12m, DataOperacional);
        var futuro = PrecoInsumo.Criar(1, insumo.Id, 1m, 15m, new DateOnly(2026, 9, 20));

        context.PrecosInsumos.AddRange(passado, atualMaisAntigo, atualMaisNovo, futuro);
        await context.SaveChangesAsync();

        var historico = await context.PrecosInsumos.AsNoTracking().ListarHistoricoAsync(insumo.Id);
        var vigente = await context.PrecosInsumos.AsNoTracking().SelecionarVigenteAsync(insumo.Id, DataOperacional);

        Assert.Equal([futuro.Id, atualMaisNovo.Id, atualMaisAntigo.Id, passado.Id], historico.Select(preco => preco.Id));
        Assert.Equal(atualMaisNovo.Id, vigente?.Id);
    }

    [Fact]
    public async Task CA09_Apenas_precos_futuros_nao_produzem_preco_vigente()
    {
        await using var connection = await AbrirAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var insumo = await CriarInsumoAsync(context, 1);
        context.PrecosInsumos.Add(PrecoInsumo.Criar(1, insumo.Id, 1m, 15m, new DateOnly(2026, 9, 20)));
        await context.SaveChangesAsync();

        var vigente = await context.PrecosInsumos.AsNoTracking().SelecionarVigenteAsync(insumo.Id, DataOperacional);

        Assert.Null(vigente);
    }

    [Fact]
    public async Task CA12_Query_filter_impede_historico_de_outro_tenant()
    {
        await using var connection = await AbrirAsync();
        await using var empresaUm = CriarContexto(connection, 1);
        await empresaUm.Database.MigrateAsync();
        empresaUm.Empresas.Add(Empresa.Criar("Empresa dois"));
        await empresaUm.SaveChangesAsync();

        await using var empresaDois = CriarContexto(connection, 2);
        var insumoOutroTenant = await CriarInsumoAsync(empresaDois, 2);
        empresaDois.PrecosInsumos.Add(PrecoInsumo.Criar(2, insumoOutroTenant.Id, 1m, 15m, DataOperacional));
        await empresaDois.SaveChangesAsync();

        var historico = await empresaUm.PrecosInsumos.AsNoTracking().ListarHistoricoAsync(insumoOutroTenant.Id);
        var vigente = await empresaUm.PrecosInsumos.AsNoTracking().SelecionarVigenteAsync(insumoOutroTenant.Id, DataOperacional);

        Assert.Empty(historico);
        Assert.Null(vigente);
    }

    private static async Task<SqliteConnection> AbrirAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static PrecificadorDbContext CriarContexto(SqliteConnection connection, int empresaId) =>
        new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(connection).Options, new ContextoEmpresa(empresaId));

    private static async Task<Insumo> CriarInsumoAsync(PrecificadorDbContext context, int empresaId)
    {
        var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        return insumo;
    }

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
