using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraCustoProdutoTests
{
    [Fact]
    public void U1_Soma_os_quatro_componentes() =>
        Assert.Equal(18m, CalculadoraCustoProduto.Calcular(10m, 2m, 5m, 1m, 2m).CustoLote);

    [Fact]
    public void U2_Todos_os_componentes_zero_sao_completos()
    {
        var resultado = CalculadoraCustoProduto.Calcular(0m, 0m, 0m, 0m, 1m);

        Assert.True(resultado.Completo);
        Assert.Equal(0m, resultado.CustoLote);
        Assert.Equal(0m, resultado.CustoUnitarioProduto);
    }

    [Fact]
    public void U3_Perda_zero_participa_da_composicao() =>
        Assert.Equal(5m, CalculadoraCustoProduto.Calcular(4m, 0m, 1m, 0m, 1m).CustoLote);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void U4_U7_Componente_indisponivel_nao_vira_zero(int componenteIndisponivel)
    {
        decimal?[] componentes = [10m, 2m, 5m, 1m];
        componentes[componenteIndisponivel] = null;

        var resultado = CalculadoraCustoProduto.Calcular(componentes[0], componentes[1], componentes[2], componentes[3], 2m);

        Assert.False(resultado.Completo);
        Assert.Null(resultado.CustoLote);
        Assert.Null(resultado.CustoUnitarioProduto);
    }

    [Fact]
    public void U8_Multiplos_componentes_indisponiveis_nao_geram_soma_parcial()
    {
        var resultado = CalculadoraCustoProduto.Calcular(null, 2m, null, 1m, 2m);

        Assert.False(resultado.Completo);
        Assert.Null(resultado.CustoLote);
        Assert.Null(resultado.CustoUnitarioProduto);
    }

    [Fact]
    public void U9_Divide_pelo_rendimento_inteiro() =>
        Assert.Equal(6m, CalculadoraCustoProduto.Calcular(10m, 0m, 2m, 0m, 2m).CustoUnitarioProduto);

    [Fact]
    public void U10_Aceita_rendimento_fracionario() =>
        Assert.Equal(8m, CalculadoraCustoProduto.Calcular(10m, 0m, 2m, 0m, 1.5m).CustoUnitarioProduto);

    [Fact]
    public void U11_Nao_arredonda_soma_nem_divisao()
    {
        var resultado = CalculadoraCustoProduto.Calcular(0.502423m, 0.062027133888m, 0.1666666666666666666666666670m, 0.005m, 3m);

        Assert.Equal(0.7361168005546666666666666670m, resultado.CustoLote);
        Assert.Equal(0.2453722668515555555555555557m, resultado.CustoUnitarioProduto);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void U12_U13_Rejeita_rendimento_nao_positivo(decimal rendimento) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoProduto.Calcular(1m, 0m, 0m, 0m, rendimento));

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(0, -1, 0, 0)]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 0, -1)]
    public void U14_Rejeita_componente_conhecido_negativo(decimal baseItens, decimal perdas, decimal maoDeObra, decimal energia) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoProduto.Calcular(baseItens, perdas, maoDeObra, energia, 1m));
}
