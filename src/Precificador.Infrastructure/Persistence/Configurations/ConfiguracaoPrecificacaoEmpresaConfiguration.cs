using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Empresas;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracaoPrecificacaoEmpresaConfiguration : IEntityTypeConfiguration<ConfiguracaoPrecificacaoEmpresa>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoPrecificacaoEmpresa> builder)
    {
        builder.ToTable("ConfiguracoesPrecificacaoEmpresas");
        builder.HasKey(configuracao => configuracao.EmpresaId);
        builder.Property(configuracao => configuracao.EmpresaId).IsRequired();
        builder.Property(configuracao => configuracao.ValorHoraTrabalho).HasPrecision(18, 6);
        builder.Property(configuracao => configuracao.TarifaEnergiaKwh).HasPrecision(18, 6);
        builder.Property(configuracao => configuracao.MargemPadrao).HasPrecision(9, 6);
        builder.Property(configuracao => configuracao.IncrementoComercial).HasPrecision(18, 6);
        builder.Property(configuracao => configuracao.ReservaComercialDesconto)
            .HasPrecision(9, 6)
            .IsRequired()
            .HasDefaultValue(ConfiguracaoPrecificacaoEmpresa.ReservaComercialDescontoPadrao);
        builder.HasOne<Empresa>()
            .WithOne()
            .HasForeignKey<ConfiguracaoPrecificacaoEmpresa>(configuracao => configuracao.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasData(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1));
    }
}
