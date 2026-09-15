using Precificador.Core.Empresas;

namespace Precificador.Tests.Unit.Empresas;

public sealed class ConfiguracaoPrecificacaoEmpresaTests
{
    [Fact]
    public void U1_CriarPadrao_exige_empresa_valida()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ConfiguracaoPrecificacaoEmpresa.CriarPadrao(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConfiguracaoPrecificacaoEmpresa.CriarPadrao(-1));
    }

    [Fact]
    public void U2_CriarPadrao_deixa_parametros_opcionais_nulos()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        Assert.Null(configuracao.ValorHoraTrabalho);
        Assert.Null(configuracao.TarifaEnergiaKwh);
        Assert.Null(configuracao.MargemPadrao);
        Assert.Null(configuracao.IncrementoComercial);
    }

    [Fact]
    public void U3_CriarPadrao_define_reserva_comercial_default()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public void U4_DefinirEmpresa_nao_permite_reatribuicao_de_tenant()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        configuracao.DefinirEmpresa(1);
        Assert.Throws<InvalidOperationException>(() => configuracao.DefinirEmpresa(2));
    }
}
