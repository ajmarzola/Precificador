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

    [Fact]
    public void UC027_U1_Atualizar_aceita_combinacao_valida_completa()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        configuracao.Atualizar(12.34m, 0.98m, 0.30m, 0.50m, 0.10m);

        Assert.Equal(12.34m, configuracao.ValorHoraTrabalho);
        Assert.Equal(0.98m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.30m, configuracao.MargemPadrao);
        Assert.Equal(0.50m, configuracao.IncrementoComercial);
        Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public void UC027_U2_Atualizar_aceita_null_nos_quatro_campos_opcionais()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        configuracao.Atualizar(null, null, null, null, 0.10m);

        Assert.Null(configuracao.ValorHoraTrabalho);
        Assert.Null(configuracao.TarifaEnergiaKwh);
        Assert.Null(configuracao.MargemPadrao);
        Assert.Null(configuracao.IncrementoComercial);
        Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public void UC027_U3_Atualizar_aceita_zero_em_hora_tarifa_margem_e_reserva()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        configuracao.Atualizar(0m, 0m, 0m, 0.01m, 0m);

        Assert.Equal(0m, configuracao.ValorHoraTrabalho);
        Assert.Equal(0m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0m, configuracao.MargemPadrao);
        Assert.Equal(0.01m, configuracao.IncrementoComercial);
        Assert.Equal(0m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public void UC027_U4_Atualizar_rejeita_hora_negativa()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => configuracao.Atualizar(-0.01m, null, null, null, 0.10m));
    }

    [Fact]
    public void UC027_U5_Atualizar_rejeita_tarifa_negativa()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => configuracao.Atualizar(null, -0.01m, null, null, 0.10m));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1")]
    public void UC027_U6_Atualizar_rejeita_margem_fora_da_faixa(string margem)
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            configuracao.Atualizar(
                null,
                null,
                decimal.Parse(margem, System.Globalization.CultureInfo.InvariantCulture),
                null,
                0.10m));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("0")]
    public void UC027_U7_Atualizar_rejeita_incremento_menor_ou_igual_a_zero_quando_informado(string incremento)
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            configuracao.Atualizar(
                null,
                null,
                null,
                decimal.Parse(incremento, System.Globalization.CultureInfo.InvariantCulture),
                0.10m));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1")]
    public void UC027_U8_Atualizar_rejeita_reserva_fora_da_faixa(string reserva)
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            configuracao.Atualizar(
                null,
                null,
                null,
                null,
                decimal.Parse(reserva, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void UC027_U9_Atualizar_invalido_preserva_estado_anterior_integralmente()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);
        configuracao.Atualizar(12.34m, 0.98m, 0.30m, 0.50m, 0.10m);

        Assert.Throws<ArgumentOutOfRangeException>(() => configuracao.Atualizar(20m, 2m, 0.20m, 0m, 0.15m));

        Assert.Equal(12.34m, configuracao.ValorHoraTrabalho);
        Assert.Equal(0.98m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.30m, configuracao.MargemPadrao);
        Assert.Equal(0.50m, configuracao.IncrementoComercial);
        Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public void UC027_U10_Atualizar_nao_altera_empresa_id()
    {
        var configuracao = ConfiguracaoPrecificacaoEmpresa.CriarPadrao(1);

        configuracao.Atualizar(12.34m, 0.98m, 0.30m, 0.50m, 0.10m);

        Assert.Equal(1, configuracao.EmpresaId);
    }
}
