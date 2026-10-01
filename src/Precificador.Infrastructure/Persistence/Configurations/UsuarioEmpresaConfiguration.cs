using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Infrastructure.Autenticacao;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class UsuarioEmpresaConfiguration : IEntityTypeConfiguration<UsuarioEmpresa>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresa> builder)
    {
        builder.ToTable("UsuariosEmpresas", table =>
            table.HasCheckConstraint("CK_UsuariosEmpresas_Perfil", "[Perfil] IN (1, 2)"));
        builder.HasKey(vinculo => new { vinculo.UsuarioId, vinculo.EmpresaId });
        builder.Property(vinculo => vinculo.Ativo).IsRequired();
        builder.Property(vinculo => vinculo.Perfil).IsRequired();
        builder.HasOne<UsuarioAplicacao>().WithMany().HasForeignKey(vinculo => vinculo.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Precificador.Core.Empresas.Empresa>().WithMany().HasForeignKey(vinculo => vinculo.EmpresaId).OnDelete(DeleteBehavior.Cascade);
    }
}
