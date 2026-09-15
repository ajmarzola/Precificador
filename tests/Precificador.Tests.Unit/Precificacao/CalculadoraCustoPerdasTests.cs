using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraCustoPerdasTests
{
    [Fact]
    public void U1_Calcula_dez_porcento_sobre_custo_base_conhecido()
    {
        var resultado = CalculadoraCustoPerdas.Calcular([new(1, 0.10m, 100m)]);

        Assert.True(resultado.Completo);
        Assert.Equal(10m, Assert.Single(resultado.Itens).CustoPerdaItem);
        Assert.Equal(10m, resultado.CustoPerdasLote);
    }

    [Fact]
    public void U2_Soma_perdas_de_multiplos_itens_sem_recalcular_custo_base()
    {
        var resultado = CalculadoraCustoPerdas.Calcular([new(1, 0.10m, 100m), new(2, 0.25m, 12m)]);

        Assert.Equal(13m, resultado.CustoPerdasLote);
    }

    [Fact]
    public void U3_Perda_zero_sem_custo_base_e_zero_e_completa()
    {
        var resultado = CalculadoraCustoPerdas.Calcular([new(1, 0m, null)]);

        Assert.True(resultado.Completo);
        Assert.Equal(0m, Assert.Single(resultado.Itens).CustoPerdaItem);
        Assert.Equal(0m, resultado.CustoPerdasLote);
    }

    [Fact]
    public void U4_Perda_positiva_sem_custo_base_indisponibiliza_total()
    {
        var resultado = CalculadoraCustoPerdas.Calcular([new(1, 0.10m, null)]);

        Assert.False(resultado.Completo);
        Assert.Null(Assert.Single(resultado.Itens).CustoPerdaItem);
        Assert.Null(resultado.CustoPerdasLote);
    }

    [Fact]
    public void U5_Preserva_custo_conhecido_quando_outro_item_indisponibiliza_total()
    {
        var resultado = CalculadoraCustoPerdas.Calcular([new(1, 0.10m, 100m), new(2, 0.20m, null)]);

        Assert.Equal(10m, resultado.Itens.Single(item => item.ItemId == 1).CustoPerdaItem);
        Assert.Null(resultado.CustoPerdasLote);
    }

    [Fact]
    public void U6_Colecao_vazia_e_completa_com_total_zero()
    {
        var resultado = CalculadoraCustoPerdas.Calcular([]);

        Assert.True(resultado.Completo);
        Assert.Equal(0m, resultado.CustoPerdasLote);
    }

    [Fact]
    public void U7_Nao_arredonda_calculos_intermediarios()
    {
        var resultado = CalculadoraCustoPerdas.Calcular([new(1, 0.123456m, 0.502423m)]);

        Assert.Equal(0.062027133888m, resultado.CustoPerdasLote);
    }

    [Theory]
    [InlineData("-0.000001")]
    [InlineData("1")]
    public void U8_Rejeita_percentual_fora_do_intervalo(string percentual)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoPerdas.Calcular([
            new(1, decimal.Parse(percentual, System.Globalization.CultureInfo.InvariantCulture), 1m)]));
    }
}
