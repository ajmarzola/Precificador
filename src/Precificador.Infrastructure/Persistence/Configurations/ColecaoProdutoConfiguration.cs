using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class ColecaoProdutoConfiguration : IEntityTypeConfiguration<ColecaoProduto>
{
    public void Configure(EntityTypeBuilder<ColecaoProduto> builder)
    {
        builder.ToTable("ColecoesProdutos", table => table.HasCheckConstraint("CK_ColecoesProdutos_Periodo", "[DataFinalizacao] IS NULL OR [DataFinalizacao] >= [DataLancamento]"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);
        builder.Property(x => x.NomeNormalizado).IsRequired().HasMaxLength(120);
        builder.Property(x => x.DataLancamento).HasColumnType("date").IsRequired();
        builder.Property(x => x.DataFinalizacao).HasColumnType("date");
        builder.HasIndex(x => new { x.EmpresaId, x.NomeNormalizado, x.DataLancamento }).IsUnique();
        builder.HasIndex(x => new { x.EmpresaId, x.DataLancamento });
        builder.HasOne<Precificador.Core.Empresas.Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
