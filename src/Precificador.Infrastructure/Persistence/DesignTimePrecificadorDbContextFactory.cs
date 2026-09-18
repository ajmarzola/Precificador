using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Precificador.Core.Empresas;

namespace Precificador.Infrastructure.Persistence;

public sealed class DesignTimePrecificadorDbContextFactory : IDesignTimeDbContextFactory<PrecificadorDbContext>
{
    private const string ConnectionStringPadrao =
        "Server=(localdb)\\MSSQLLocalDB;Database=Precificador;Trusted_Connection=True;TrustServerCertificate=True;";

    public PrecificadorDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>()
            .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__Precificador") ?? ConnectionStringPadrao)
            .Options,
        new EmpresaContextVazio());

    private sealed class EmpresaContextVazio : IEmpresaContext
    {
        public int? EmpresaId => null;
        public int EmpresaIdOuSentinela => -1;
        public string? TimeZoneId => null;
    }
}
