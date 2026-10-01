using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precificador.Core.Acessos;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;

namespace Precificador.Infrastructure.Persistence.Configurations;

public sealed class SolicitacaoAcessoEmpresaConfiguration : IEntityTypeConfiguration<SolicitacaoAcessoEmpresa>
{
    public const string IndicePendente = "UX_SolicitacoesAcessoEmpresas_Pendente_Nome_Email";

    public void Configure(EntityTypeBuilder<SolicitacaoAcessoEmpresa> builder)
    {
        builder.ToTable("SolicitacoesAcessoEmpresas", table =>
        {
            table.HasCheckConstraint("CK_SolicitacoesAcessoEmpresas_Situacao", "[Situacao] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_SolicitacoesAcessoEmpresas_Decisao",
                "([Situacao] = 1 AND [EmpresaId] IS NULL AND [DataDecisaoUtc] IS NULL AND [DecididaPorUsuarioId] IS NULL AND [MotivoRecusa] IS NULL) OR " +
                "([Situacao] = 2 AND [EmpresaId] IS NOT NULL AND [DataDecisaoUtc] IS NOT NULL AND [DecididaPorUsuarioId] IS NOT NULL AND [MotivoRecusa] IS NULL) OR " +
                "([Situacao] = 3 AND [EmpresaId] IS NULL AND [DataDecisaoUtc] IS NOT NULL AND [DecididaPorUsuarioId] IS NOT NULL)");
        });
        builder.Property(x => x.MotivoRecusa).HasMaxLength(500);
        builder.Property(x => x.DecididaPorUsuarioId).HasMaxLength(450);
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UsuarioAplicacao>().WithMany().HasForeignKey(x => x.DecididaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NomeEmpresa).IsRequired().HasMaxLength(120);
        builder.Property(x => x.NomeEmpresaNormalizado).IsRequired().HasMaxLength(120);
        builder.Property(x => x.NomeResponsavel).IsRequired().HasMaxLength(120);
        builder.Property(x => x.EmailResponsavel).IsRequired().HasMaxLength(256);
        builder.Property(x => x.EmailResponsavelNormalizado).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Observacao).HasMaxLength(1000);
        builder.Property(x => x.DataSolicitacaoUtc).IsRequired();
        builder.Property(x => x.Situacao).IsRequired();
        builder.HasIndex(x => new { x.NomeEmpresaNormalizado, x.EmailResponsavelNormalizado })
            .IsUnique().HasFilter("[Situacao] = 1").HasDatabaseName(IndicePendente);
        builder.HasIndex(x => new { x.Situacao, x.DataSolicitacaoUtc });
    }
}
