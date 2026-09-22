using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class CategoriaProdutoConfiguration : IEntityTypeConfiguration<CategoriaProduto>
{
    public void Configure(EntityTypeBuilder<CategoriaProduto> builder)
    {
        builder.ToTable("CategoriasProdutos");
        builder.HasKey(categoria => categoria.Id);
        builder.Property(categoria => categoria.Nome).IsRequired().HasMaxLength(80);
        builder.Property(categoria => categoria.NomeNormalizado).IsRequired().HasMaxLength(80);
        builder.Property(categoria => categoria.Ativo).IsRequired();
        builder.Property(categoria => categoria.EmpresaId).IsRequired();
        builder.Property(categoria => categoria.FormaCalculoDesgasteEquipamento).HasConversion<int>().IsRequired();
        builder.Property(categoria => categoria.ValorDesgasteEquipamento).HasPrecision(18, 6).IsRequired();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_CategoriasProdutos_FormaCalculoDesgasteEquipamento", "[FormaCalculoDesgasteEquipamento] IN (1, 2)");
            table.HasCheckConstraint("CK_CategoriasProdutos_ValorDesgasteEquipamento", "[ValorDesgasteEquipamento] >= 0");
        });
        builder.HasIndex(categoria => new { categoria.EmpresaId, categoria.NomeNormalizado }).IsUnique();
        builder.HasOne<Precificador.Core.Empresas.Empresa>()
            .WithMany()
            .HasForeignKey(categoria => categoria.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
