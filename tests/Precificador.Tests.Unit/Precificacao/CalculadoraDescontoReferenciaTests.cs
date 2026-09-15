using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraDescontoReferenciaTests
{
    [Fact]
    public void U1_Reserva_dez_e_mais_dez_virgula_noventa_e_nove_retorna_null() =>
        Assert.Null(CalculadoraDescontoReferencia.Calcular(100m, 110.99m, .10m));

    [Fact]
    public void U2_Reserva_dez_e_mais_onze_retorna_um_por_cento() =>
        Assert.Equal(.01m, CalculadoraDescontoReferencia.Calcular(100m, 111m, .10m));

    [Fact]
    public void U3_Reserva_dez_e_mais_vinte_retorna_dez_por_cento() =>
        Assert.Equal(.10m, CalculadoraDescontoReferencia.Calcular(100m, 120m, .10m));

    [Fact]
    public void U4_Reserva_cinco_e_mais_cinco_virgula_noventa_e_nove_retorna_null() =>
        Assert.Null(CalculadoraDescontoReferencia.Calcular(100m, 105.99m, .05m));

    [Fact]
    public void U5_Reserva_cinco_e_mais_seis_retorna_um_por_cento() =>
        Assert.Equal(.01m, CalculadoraDescontoReferencia.Calcular(100m, 106m, .05m));

    [Fact]
    public void U6_Preco_de_prateleira_abaixo_do_sugerido_retorna_null() =>
        Assert.Null(CalculadoraDescontoReferencia.Calcular(100m, 99.99m, .10m));

    [Fact]
    public void U7_Reserva_zero_e_mais_um_retorna_um_por_cento() =>
        Assert.Equal(.01m, CalculadoraDescontoReferencia.Calcular(100m, 101m, 0m));

    [Fact]
    public void U8_Preco_sugerido_zero_retorna_null_sem_dividir_por_zero() =>
        Assert.Null(CalculadoraDescontoReferencia.Calcular(0m, 100m, .10m));

    [Fact]
    public void U9_Fronteira_de_alta_precisao_nao_arredonda_intermediario()
    {
        Assert.Null(CalculadoraDescontoReferencia.Calcular(100m, 110.999999m, .10m));
        Assert.Equal(.01m, CalculadoraDescontoReferencia.Calcular(100m, 111m, .10m));
    }

    [Fact]
    public void U10_Preco_sugerido_negativo_e_rejeitado() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraDescontoReferencia.Calcular(-.01m, 100m, .10m));

    [Theory]
    [InlineData(0)]
    [InlineData(-.01)]
    public void U11_Preco_prateleira_menor_ou_igual_a_zero_e_rejeitado(decimal precoPrateleira) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraDescontoReferencia.Calcular(100m, precoPrateleira, .10m));

    [Theory]
    [InlineData(-.01)]
    [InlineData(1)]
    public void U12_Reserva_negativa_ou_maior_igual_a_um_e_rejeitada(decimal reserva) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraDescontoReferencia.Calcular(100m, 120m, reserva));
}
