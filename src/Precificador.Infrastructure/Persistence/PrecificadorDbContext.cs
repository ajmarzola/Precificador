using Microsoft.EntityFrameworkCore;

namespace Precificador.Infrastructure.Persistence;

public sealed class PrecificadorDbContext(DbContextOptions<PrecificadorDbContext> options)
    : DbContext(options)
{
}
