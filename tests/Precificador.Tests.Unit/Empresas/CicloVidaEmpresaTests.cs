using Precificador.Core.Empresas;

namespace Precificador.Tests.Unit.Empresas;

public sealed class CicloVidaEmpresaTests
{
    [Fact]
    public void Empresa_real_suspende_reativa_e_encerra_sem_reabrir()
    {
        var empresa = Empresa.Criar("Cliente");
        Assert.False(empresa.EhTecnica);
        Assert.Equal(SituacaoAdministrativaEmpresa.Ativa, empresa.SituacaoAdministrativa);
        empresa.Suspender();
        Assert.Equal(SituacaoAdministrativaEmpresa.Suspensa, empresa.SituacaoAdministrativa);
        Assert.Throws<InvalidOperationException>(() => empresa.Reativar(false));
        empresa.Reativar(true);
        Assert.True(empresa.Ativo);
        var tempo = new TempoFixo();
        empresa.Encerrar(tempo);
        Assert.False(empresa.Ativo);
        Assert.Equal(tempo.GetUtcNow(), empresa.EncerradaEmUtc);
        Assert.Equal(SituacaoAdministrativaEmpresa.Encerrada, empresa.SituacaoAdministrativa);
        Assert.Throws<InvalidOperationException>(() => empresa.Reativar(true));
        Assert.Throws<InvalidOperationException>(() => empresa.Suspender());
        Assert.Throws<InvalidOperationException>(() => empresa.Encerrar(tempo));
    }
    [Fact]
    public void Tecnica_rejeita_todas_as_acoes_administrativas()
    {
        var empresa = Empresa.CriarTecnica(1, "Empresa inicial");
        Assert.True(empresa.EhTecnica);
        Assert.Throws<InvalidOperationException>(() => empresa.Suspender());
        Assert.Throws<InvalidOperationException>(() => empresa.Reativar(true));
        Assert.Throws<InvalidOperationException>(() => empresa.Encerrar(TimeProvider.System));
    }
    private sealed class TempoFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);
    }
}
