using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Empresas;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas");
        builder.HasKey(empresa => empresa.Id);
        builder.Property(empresa => empresa.Nome).IsRequired().HasMaxLength(120);
        builder.Property(empresa => empresa.NomeNormalizado).IsRequired().HasMaxLength(120);
        builder.Property(empresa => empresa.Ativo).IsRequired();
        builder.HasIndex(empresa => empresa.NomeNormalizado).IsUnique();
        builder.HasData(Empresa.CriarTecnica(1, "Empresa inicial"));
    }
}
