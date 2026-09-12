using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class InsumoEmpresaForeignKeyTests
{
    [Fact]
    public async Task CA01_EmpresaId_inexistente_e_rejeitado_pela_fk_do_banco()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 999);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar(999, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var sqliteException = Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Equal(19, sqliteException.SqliteErrorCode);
        Assert.Equal(787, sqliteException.SqliteExtendedErrorCode);
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
