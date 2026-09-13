using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class ItemFichaTecnicaConfiguration : IEntityTypeConfiguration<ItemFichaTecnica>
{
    public void Configure(EntityTypeBuilder<ItemFichaTecnica> builder)
    {
        builder.ToTable("ItensFichaTecnica");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EmpresaId).IsRequired();
        builder.Property(item => item.FichaTecnicaId).IsRequired();
        builder.Property(item => item.InsumoId).IsRequired();
        builder.Property(item => item.Quantidade).HasPrecision(18, 6).IsRequired();
        builder.Property(item => item.Observacao).HasMaxLength(1000);
        builder.HasIndex(item => new { item.EmpresaId, item.FichaTecnicaId, item.InsumoId }).IsUnique();
        builder.HasOne<Precificador.Core.Empresas.Empresa>()
            .WithMany()
            .HasForeignKey(item => item.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FichaTecnica>()
            .WithMany()
            .HasForeignKey(item => item.FichaTecnicaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Insumo>()
            .WithMany()
            .HasForeignKey(item => item.InsumoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
