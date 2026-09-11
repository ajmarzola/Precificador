using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Autenticacao;

namespace Precificador.Infrastructure.Persistence;

public sealed class PrecificadorDbContext(
    DbContextOptions<PrecificadorDbContext> options,
    IEmpresaContext empresaContext)
    : IdentityDbContext<UsuarioAplicacao>(options)
{
    private readonly IEmpresaContext empresaContext = empresaContext;

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<PrecoInsumo> PrecosInsumos => Set<PrecoInsumo>();
    public DbSet<UsuarioEmpresa> UsuariosEmpresas => Set<UsuarioEmpresa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrecificadorDbContext).Assembly);
        modelBuilder.Entity<Insumo>().HasQueryFilter(insumo =>
            insumo.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<PrecoInsumo>().HasQueryFilter(preco =>
            preco.EmpresaId == empresaContext.EmpresaIdOuSentinela);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AplicarIsolamentoEmpresa();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AplicarIsolamentoEmpresa();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AplicarIsolamentoEmpresa()
    {
        var alteracoes = ChangeTracker.Entries<IEntidadeEmpresa>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var alteracao in alteracoes)
        {
            var empresaId = empresaContext.EmpresaId;
            if (!empresaId.HasValue)
            {
                throw new InvalidOperationException("Uma empresa ativa é necessária para alterar dados da empresa.");
            }

            if (alteracao.Entity.EmpresaId != empresaId.Value)
            {
                throw new InvalidOperationException("Não é permitido alterar dados de outra empresa.");
            }
        }
    }
}
