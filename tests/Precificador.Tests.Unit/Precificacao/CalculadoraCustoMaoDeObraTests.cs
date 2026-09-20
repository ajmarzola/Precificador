using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraCustoMaoDeObraTests
{
    [Fact]
    public void U1_Custo_base_de_doze_com_dez_porcento_resulta_em_um_virgula_dois()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(12m, 0.10m);

        Assert.True(resultado.Completo);
        Assert.Equal(1.2m, resultado.CustoMaoDeObraLote);
    }

    [Fact]
    public void U2_Percentual_zero_resulta_em_zero_completo()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(12m, 0m);

        Assert.True(resultado.Completo);
        Assert.Equal(0m, resultado.CustoMaoDeObraLote);
    }

    [Fact]
    public void U3_Percentual_de_cem_porcento_e_valido()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(12m, 1m);

        Assert.Equal(12m, resultado.CustoMaoDeObraLote);
    }

    [Fact]
    public void U4_Percentual_de_duzentos_e_cinquenta_porcento_e_valido()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(12m, 2.5m);

        Assert.Equal(30m, resultado.CustoMaoDeObraLote);
    }

    [Fact]
    public void U5_Custo_base_indisponivel_resulta_em_calculo_incompleto()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(null, 0.10m);

        Assert.Null(resultado.CustoMaoDeObraLote);
        Assert.False(resultado.Completo);
    }

    [Fact]
    public void U6_Preserva_alta_precisao_sem_arredondamento_intermediario()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(12.345678m, 0.123456m);

        Assert.Equal(1.524148023168m, resultado.CustoMaoDeObraLote);
    }

    [Fact]
    public void U7_Rejeita_custo_base_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoMaoDeObra.Calcular(-0.01m, 0.10m));
    }

    [Fact]
    public void U8_Rejeita_percentual_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoMaoDeObra.Calcular(12m, -0.01m));
    }
}
