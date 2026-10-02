using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class ProdutoColecaoConfiguration : IEntityTypeConfiguration<ProdutoColecao>
{
    public void Configure(EntityTypeBuilder<ProdutoColecao> builder)
    {
        builder.ToTable("ProdutosColecoes");
        builder.HasKey(x => new { x.ProdutoId, x.ColecaoProdutoId });
        builder.HasIndex(x => new { x.EmpresaId, x.ColecaoProdutoId, x.Destaque });
        builder.HasIndex(x => new { x.EmpresaId, x.ProdutoId });
        builder.HasOne<Precificador.Core.Empresas.Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ColecaoProduto).WithMany().HasForeignKey(x => x.ColecaoProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}
