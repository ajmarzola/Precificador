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
        builder.HasIndex(categoria => new { categoria.EmpresaId, categoria.NomeNormalizado }).IsUnique();
        builder.HasOne<Precificador.Core.Empresas.Empresa>()
            .WithMany()
            .HasForeignKey(categoria => categoria.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
