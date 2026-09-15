using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class UsoEquipamentoFichaConfiguration : IEntityTypeConfiguration<UsoEquipamentoFicha>
{
    public void Configure(EntityTypeBuilder<UsoEquipamentoFicha> builder)
    {
        builder.ToTable("UsosEquipamentosFicha");
        builder.HasKey(uso => uso.Id);
        builder.Property(uso => uso.EmpresaId).IsRequired();
        builder.Property(uso => uso.FichaTecnicaId).IsRequired();
        builder.Property(uso => uso.NomeEquipamento).HasMaxLength(120).IsRequired();
        builder.Property(uso => uso.NomeEquipamentoNormalizado).HasMaxLength(120).IsRequired();
        builder.Property(uso => uso.PotenciaKw).HasPrecision(18, 6).IsRequired();
        builder.Property(uso => uso.TempoUsoMinutos).IsRequired();
        builder.HasIndex(uso => new { uso.EmpresaId, uso.FichaTecnicaId, uso.NomeEquipamentoNormalizado }).IsUnique();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(uso => uso.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FichaTecnica>().WithMany().HasForeignKey(uso => uso.FichaTecnicaId).OnDelete(DeleteBehavior.Restrict);
    }
}
