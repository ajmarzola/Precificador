using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Empresas;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class IdentidadeVisualEmpresaConfiguration : IEntityTypeConfiguration<IdentidadeVisualEmpresa>
{
    public void Configure(EntityTypeBuilder<IdentidadeVisualEmpresa> builder)
    {
        builder.ToTable("IdentidadesVisuaisEmpresas", tabela =>
        {
            tabela.HasCheckConstraint("CK_IdentidadeVisual_Cor", "DATALENGTH([CorPrimaria]) = 14 AND [CorPrimaria] COLLATE Latin1_General_100_BIN2 LIKE '#[0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F]'");
            tabela.HasCheckConstraint("CK_IdentidadeVisual_Logo", "([LogoConteudo] IS NULL AND [LogoContentType] IS NULL) OR ([LogoConteudo] IS NOT NULL AND [LogoContentType] IS NOT NULL AND [LogoContentType] COLLATE Latin1_General_100_BIN2 IN ('image/png', 'image/jpeg'))");
            tabela.HasCheckConstraint("CK_IdentidadeVisual_Tamanho", "[LogoConteudo] IS NULL OR DATALENGTH([LogoConteudo]) BETWEEN 1 AND 524288");
        });
        builder.HasKey(x => x.EmpresaId);
        builder.Property(x => x.EmpresaId).ValueGeneratedNever();
        builder.Property(x => x.CorPrimaria).HasMaxLength(7).IsRequired();
        builder.Property(x => x.LogoConteudo).HasColumnType("varbinary(max)");
        builder.Property(x => x.LogoContentType).HasMaxLength(20);
        builder.HasOne<Empresa>().WithOne().HasForeignKey<IdentidadeVisualEmpresa>(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
