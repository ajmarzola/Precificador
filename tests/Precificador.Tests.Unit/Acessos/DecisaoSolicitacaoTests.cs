using Precificador.Core.Acessos;

namespace Precificador.Tests.Unit.Acessos;

public sealed class DecisaoSolicitacaoTests
{
    private static SolicitacaoAcessoEmpresa Criar() => SolicitacaoAcessoEmpresa.Criar("Cliente", "Ana", "ana@teste.local", null, DateTimeOffset.UtcNow);
    [Fact]
    public void Aprovacao_registra_decisao_utc_e_e_terminal()
    {
        var item = Criar();
        Assert.Null(item.EmpresaId); Assert.Null(item.DataDecisaoUtc); Assert.Null(item.DecididaPorUsuarioId); Assert.Null(item.MotivoRecusa);
        var data = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.FromHours(-3));
        item.Aprovar(2, "admin", data);
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Aprovada, item.Situacao);
        Assert.Equal(2, item.EmpresaId); Assert.Equal("admin", item.DecididaPorUsuarioId);
        Assert.Equal(TimeSpan.Zero, item.DataDecisaoUtc!.Value.Offset); Assert.Equal(data, item.DataDecisaoUtc);
        Assert.Throws<InvalidOperationException>(() => item.Aprovar(3, "outro", data));
        Assert.Throws<InvalidOperationException>(() => item.Recusar("admin", data, null));
    }
    [Theory]
    [InlineData(null, null)] [InlineData("   ", null)] [InlineData("  motivo  ", "motivo")]
    public void Recusa_normaliza_motivo_e_e_terminal(string? motivo, string? esperado)
    {
        var item = Criar(); item.Recusar("admin", DateTimeOffset.UtcNow, motivo);
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Recusada, item.Situacao);
        Assert.Equal(esperado, item.MotivoRecusa); Assert.Null(item.EmpresaId);
        Assert.Throws<InvalidOperationException>(() => item.Aprovar(2, "admin", DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => item.Recusar("admin", DateTimeOffset.UtcNow, null));
    }
    [Fact]
    public void Decisao_invalida_nao_muta_solicitacao()
    {
        var item = Criar();
        Assert.Throws<ArgumentException>(() => item.Recusar("admin", DateTimeOffset.UtcNow, new string('x', 501)));
        Assert.Throws<ArgumentException>(() => item.Aprovar(2, " ", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => item.Aprovar(0, "admin", DateTimeOffset.UtcNow));
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Pendente, item.Situacao);
        Assert.Null(item.DataDecisaoUtc);
    }
}
