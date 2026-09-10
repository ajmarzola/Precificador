using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class IsolamentoEmpresaTests
{
    [Fact]
    public async Task Leitura_e_escrita_respeitam_empresa_ativa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var empresa1 = new ContextoEmpresa(1);
        await using (var contexto = CriarContexto(connection, empresa1))
        {
            await contexto.Database.MigrateAsync();
            contexto.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
            await contexto.SaveChangesAsync();
            contexto.Empresas.Add(Empresa.Criar("Empresa dois"));
            await contexto.SaveChangesAsync();
        }
        var empresa2 = new ContextoEmpresa(2);
        await using (var contexto = CriarContexto(connection, empresa2))
        {
            Assert.Empty(await contexto.Insumos.ToListAsync());
            contexto.Insumos.Add(Insumo.Criar(1, "Tentativa", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
            await Assert.ThrowsAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
        }
        await using var semEmpresa = CriarContexto(connection, new ContextoEmpresa(null));
        Assert.Empty(await semEmpresa.Insumos.ToListAsync());
    }

    private static PrecificadorDbContext CriarContexto(SqliteConnection connection, IEmpresaContext empresaContext) =>
        new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(connection).Options, empresaContext);

    private sealed class ContextoEmpresa(int? empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId ?? -1;
    }
}
