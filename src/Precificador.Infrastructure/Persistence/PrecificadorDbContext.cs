using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;

namespace Precificador.Infrastructure.Persistence;

public sealed class PrecificadorDbContext(DbContextOptions<PrecificadorDbContext> options)
    : DbContext(options)
{
    public DbSet<Insumo> Insumos => Set<Insumo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrecificadorDbContext).Assembly);
    }
}
