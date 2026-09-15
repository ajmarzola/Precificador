using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class UsoEquipamentoFichaPersistenceTests
{
    [Fact]
    public async Task P1_P2_P3_MigrationEIndiceUnicoSaoEvolutivos()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1); await context.Database.MigrateAsync("20260915015959_AddConfiguracoesPrecificacaoEmpresa");
        var fichaUm = await CriarFichaAsync(context, 1); var fichaDois = await CriarFichaAsync(context, 1);
        var produtoAntes = await context.Produtos.SingleAsync(produto => produto.Id == 1);
        await context.Database.MigrateAsync();
        context.UsosEquipamentosFicha.AddRange(UsoEquipamentoFicha.Criar(1, fichaUm, "Forno", 1m, 10), UsoEquipamentoFicha.Criar(1, fichaDois, "Forno", 1m, 10)); await context.SaveChangesAsync();
        context.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(1, fichaUm, " forno ", 1m, 10));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var indices = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'index'").ToListAsync();
        Assert.Contains("IX_UsosEquipamentosFicha_EmpresaId_FichaTecnicaId_NomeEquipamentoNormalizado", indices);
        Assert.Equal(produtoAntes.Nome, (await context.Produtos.SingleAsync(produto => produto.Id == produtoAntes.Id)).Nome);
    }

    [Fact]
    public async Task P4_P5_P6_GqfEGuardProtegemFichaDeOutraEmpresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using (var um = CriarContexto(connection, 1)) { await um.Database.MigrateAsync(); um.Empresas.Add(Empresa.Criar("Dois")); await um.SaveChangesAsync(); }
        int fichaDois; await using (var dois = CriarContexto(connection, 2)) { fichaDois = await CriarFichaAsync(dois, 2); dois.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(2, fichaDois, "Forno", 1m, 10)); await dois.SaveChangesAsync(); }
        await using var contextoUm = CriarContexto(connection, 1); var fichaUm = await CriarFichaAsync(contextoUm, 1); Assert.Empty(await contextoUm.UsosEquipamentosFicha.ToListAsync());
        contextoUm.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(2, fichaDois, "Forno externo", 1m, 10)); await Assert.ThrowsAsync<InvalidOperationException>(() => contextoUm.SaveChangesAsync());
        contextoUm.ChangeTracker.Clear();
        contextoUm.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(1, fichaDois, "Forno", 1m, 10)); await Assert.ThrowsAsync<InvalidOperationException>(() => contextoUm.SaveChangesAsync());
        Assert.NotEqual(fichaUm, fichaDois);
    }

    [Fact]
    public async Task P7_P8_FksRestrictEPotenciaComPrecisaoSaoPreservadas()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); await using var context = CriarContexto(connection, 1); await context.Database.MigrateAsync();
        var ficha = await CriarFichaAsync(context, 1); var uso = UsoEquipamentoFicha.Criar(1, ficha, "Plotter", 0.123456m, 1); context.UsosEquipamentosFicha.Add(uso); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        Assert.Equal(0.123456m, (await context.UsosEquipamentosFicha.SingleAsync()).PotenciaKw); context.ChangeTracker.Clear();
        context.FichasTecnicas.Remove(await context.FichasTecnicas.SingleAsync()); await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        context.Empresas.Remove(await context.Empresas.SingleAsync()); await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task<int> CriarFichaAsync(PrecificadorDbContext context, int empresaId) { var produto = Produto.Criar(empresaId, Guid.NewGuid().ToString(), .3m); context.Produtos.Add(produto); await context.SaveChangesAsync(); var ficha = FichaTecnica.Criar(empresaId, produto.Id, 1m, 0); context.FichasTecnicas.Add(ficha); await context.SaveChangesAsync(); return ficha.Id; }
    private static PrecificadorDbContext CriarContexto(SqliteConnection c, int empresaId) => new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(c).Options, new ContextoEmpresa(empresaId));
    private sealed class ContextoEmpresa(int id) : IEmpresaContext { public int? EmpresaId => id; public int EmpresaIdOuSentinela => id; public string? TimeZoneId => Empresa.TimeZoneIdPadrao; }
}
