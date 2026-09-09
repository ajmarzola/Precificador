using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class InsumoPersistenceTests
{
    [Fact]
    public async Task Migrations_criam_tabela_e_indice_unico_em_banco_sqlite_vazio()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);

        await context.Database.MigrateAsync();

        var objetos = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type IN ('table', 'index')").ToListAsync();
        Assert.Contains("Insumos", objetos);
        Assert.Contains("IX_Insumos_NomeNormalizado", objetos);
    }

    [Fact]
    public async Task Insumo_valido_persiste_e_recupera_todos_os_valores()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar("  Copo   200 ml ", CategoriaInsumo.Embalagem, UnidadeMedida.Unidade));
        await context.SaveChangesAsync();

        var insumo = await context.Insumos.AsNoTracking().SingleAsync();
        Assert.Equal("Copo 200 ml", insumo.Nome);
        Assert.Equal("COPO 200 ML", insumo.NomeNormalizado);
        Assert.Equal(CategoriaInsumo.Embalagem, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Unidade, insumo.UnidadeBase);
        Assert.True(insumo.Ativo);
    }

    [Fact]
    public async Task Indice_unico_rejeita_nome_normalizado_duplicado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar("Farinha", CategoriaInsumo.Ingrediente, UnidadeMedida.Grama));
        await context.SaveChangesAsync();
        context.Insumos.Add(Insumo.Criar("farinha", CategoriaInsumo.Ingrediente, UnidadeMedida.Grama));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static PrecificadorDbContext CriarContexto(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(connection).Options);
}
