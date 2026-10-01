using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class ColecaoProdutoCategoriaConfiguration : IEntityTypeConfiguration<ColecaoProdutoCategoria>
{
    public void Configure(EntityTypeBuilder<ColecaoProdutoCategoria> builder)
    {
        builder.ToTable("ColecoesProdutosCategorias");
        builder.HasKey(x => new { x.ColecaoProdutoId, x.CategoriaProdutoId });
        builder.HasIndex(x => new { x.EmpresaId, x.CategoriaProdutoId });
        builder.HasOne<Precificador.Core.Empresas.Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ColecaoProduto).WithMany().HasForeignKey(x => x.ColecaoProdutoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CategoriaProduto).WithMany().HasForeignKey(x => x.CategoriaProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}
