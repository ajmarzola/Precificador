using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraCustoMaoDeObraTests
{
    [Fact]
    public void U1_60_minutos_correspondem_a_uma_hora()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(60, 40m);

        Assert.True(resultado.Completo);
        Assert.Equal(40m, resultado.CustoMaoDeObraLote);
    }

    [Fact]
    public void U2_30_minutos_correspondem_a_meia_hora()
    {
        var resultado = CalculadoraCustoMaoDeObra.Calcular(30, 40m);

        Assert.Equal(20m, resultado.CustoMaoDeObraLote);
    }

    [Fact]
    public void U3_Um_minuto_preserva_precisao()
    {
        var minuto = CalculadoraCustoMaoDeObra.Calcular(1, 10m);

        Assert.Equal(0.1666666666666666666666666670m, minuto.CustoMaoDeObraLote);
    }

    [Fact]
    public void U4_Tempo_zero_com_valor_hora_null_e_completo()
    {
        var semTempo = CalculadoraCustoMaoDeObra.Calcular(0, null);

        Assert.Equal(0m, semTempo.CustoMaoDeObraLote);
        Assert.True(semTempo.Completo);
    }

    [Fact]
    public void U5_Tempo_positivo_com_valor_hora_null_e_incompleto()
    {
        var semConfiguracao = CalculadoraCustoMaoDeObra.Calcular(1, null);

        Assert.Null(semConfiguracao.CustoMaoDeObraLote);
        Assert.False(semConfiguracao.Completo);
    }

    [Fact]
    public void U6_Valor_hora_zero_e_valido_e_completo()
    {
        var horaZero = CalculadoraCustoMaoDeObra.Calcular(30, 0m);

        Assert.Equal(0m, horaZero.CustoMaoDeObraLote);
        Assert.True(horaZero.Completo);
    }

    [Fact]
    public void U7_Rejeita_tempo_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoMaoDeObra.Calcular(-1, 1m));
    }

    [Fact]
    public void U8_Rejeita_valor_hora_negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoMaoDeObra.Calcular(1, -1m));
    }
}
