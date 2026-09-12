using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Precificador.Core.Empresas;

namespace Precificador.Infrastructure.Persistence;

public sealed class DesignTimePrecificadorDbContextFactory : IDesignTimeDbContextFactory<PrecificadorDbContext>
{
    public PrecificadorDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite("Data Source=precificador.db").Options,
        new EmpresaContextVazio());

    private sealed class EmpresaContextVazio : IEmpresaContext
    {
        public int? EmpresaId => null;
        public int EmpresaIdOuSentinela => -1;
        public string? TimeZoneId => null;
    }
}
