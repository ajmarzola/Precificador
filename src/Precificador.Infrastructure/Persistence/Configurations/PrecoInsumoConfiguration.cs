using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Insumos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class PrecoInsumoConfiguration : IEntityTypeConfiguration<PrecoInsumo>
{
    public void Configure(EntityTypeBuilder<PrecoInsumo> builder)
    {
        builder.ToTable("PrecosInsumos");
        builder.HasKey(preco => preco.Id);
        builder.Property(preco => preco.EmpresaId).IsRequired();
        builder.Property(preco => preco.InsumoId).IsRequired();
        builder.Property(preco => preco.QuantidadeCompra).HasPrecision(18, 6).IsRequired();
        builder.Property(preco => preco.PrecoCompra).HasPrecision(18, 4).IsRequired();
        builder.Property(preco => preco.DataReferencia).IsRequired();
        builder.HasIndex(preco => new { preco.EmpresaId, preco.InsumoId, preco.DataReferencia });
        builder.HasOne<Precificador.Core.Empresas.Empresa>().WithMany().HasForeignKey(preco => preco.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Insumo>().WithMany().HasForeignKey(preco => preco.InsumoId).OnDelete(DeleteBehavior.Restrict);
    }
}
