using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(produto => produto.Id);
        builder.Property(produto => produto.Nome).IsRequired().HasMaxLength(120);
        builder.Property(produto => produto.NomeNormalizado).IsRequired().HasMaxLength(120);
        builder.Property(produto => produto.Categoria).HasMaxLength(80);
        builder.Property(produto => produto.MargemAlvo).HasPrecision(9, 6).IsRequired();
        builder.Property(produto => produto.Ativo).IsRequired();
        builder.Property(produto => produto.EmpresaId).IsRequired();
        builder.HasIndex(produto => new { produto.EmpresaId, produto.NomeNormalizado }).IsUnique();
        builder.HasOne<Precificador.Core.Empresas.Empresa>()
            .WithMany()
            .HasForeignKey(produto => produto.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
