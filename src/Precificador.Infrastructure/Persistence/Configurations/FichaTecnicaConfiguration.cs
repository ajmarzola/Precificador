using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class FichaTecnicaConfiguration : IEntityTypeConfiguration<FichaTecnica>
{
    public void Configure(EntityTypeBuilder<FichaTecnica> builder)
    {
        builder.ToTable("FichasTecnicas");
        builder.HasKey(ficha => ficha.Id);
        builder.Property(ficha => ficha.EmpresaId).IsRequired();
        builder.Property(ficha => ficha.ProdutoId).IsRequired();
        builder.Property(ficha => ficha.Rendimento).HasPrecision(18, 6).IsRequired();
        builder.HasIndex(ficha => new { ficha.EmpresaId, ficha.ProdutoId }).IsUnique();
        builder.HasOne<Precificador.Core.Empresas.Empresa>()
            .WithMany()
            .HasForeignKey(ficha => ficha.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Produto>()
            .WithMany()
            .HasForeignKey(ficha => ficha.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
