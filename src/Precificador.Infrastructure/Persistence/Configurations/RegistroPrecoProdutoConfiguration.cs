using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class RegistroPrecoProdutoConfiguration : IEntityTypeConfiguration<RegistroPrecoProduto>
{
    public void Configure(EntityTypeBuilder<RegistroPrecoProduto> builder)
    {
        builder.ToTable("RegistrosPrecosProdutos");
        builder.HasKey(registro => registro.Id);
        builder.Property(registro => registro.EmpresaId).IsRequired();
        builder.Property(registro => registro.ProdutoId).IsRequired();
        builder.Property(registro => registro.DataReferencia).IsRequired();
        builder.Property(registro => registro.CustoReferencia).HasPrecision(18, 6).IsRequired();
        builder.Property(registro => registro.MargemReferencia).HasPrecision(9, 6).IsRequired();
        builder.Property(registro => registro.PrecoSugerido).HasPrecision(18, 6).IsRequired();
        builder.Property(registro => registro.PrecoPrateleira).HasPrecision(18, 6).IsRequired();
        builder.Property(registro => registro.ReservaComercialReferencia).HasPrecision(9, 6).IsRequired();
        builder.HasIndex(registro => new { registro.EmpresaId, registro.ProdutoId, registro.DataReferencia });
        builder.HasOne<Precificador.Core.Empresas.Empresa>().WithMany().HasForeignKey(registro => registro.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Produto>().WithMany().HasForeignKey(registro => registro.ProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}
