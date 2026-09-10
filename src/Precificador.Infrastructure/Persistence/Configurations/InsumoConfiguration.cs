using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Insumos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class InsumoConfiguration : IEntityTypeConfiguration<Insumo>
{
    public void Configure(EntityTypeBuilder<Insumo> builder)
    {
        builder.ToTable("Insumos");
        builder.HasKey(insumo => insumo.Id);
        builder.Property(insumo => insumo.Nome).IsRequired().HasMaxLength(120);
        builder.Property(insumo => insumo.NomeNormalizado).IsRequired().HasMaxLength(120);
        builder.Property(insumo => insumo.Categoria).IsRequired();
        builder.Property(insumo => insumo.UnidadeBase).IsRequired();
        builder.Property(insumo => insumo.Ativo).IsRequired();
        builder.Property(insumo => insumo.EmpresaId).IsRequired();
        builder.HasIndex(insumo => new { insumo.EmpresaId, insumo.NomeNormalizado }).IsUnique();
        builder.HasOne<Precificador.Core.Empresas.Empresa>()
            .WithMany()
            .HasForeignKey(insumo => insumo.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
