using Precificador.Core.Precificacao;
using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraCustoDesgasteEquipamentosTests
{
    [Fact]
    public void Valor_fixo_e_por_lote() =>
        Assert.Equal(1.50m, CalculadoraCustoDesgasteEquipamentos.Calcular(FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 1.50m, null).CustoDesgasteEquipamentosLote);

    [Fact]
    public void Percentual_usa_apenas_custo_base() =>
        Assert.Equal(0.50m, CalculadoraCustoDesgasteEquipamentos.Calcular(FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, .05m, 10m).CustoDesgasteEquipamentosLote);

    [Fact]
    public void Percentual_zero_com_base_indisponivel_e_conhecido()
    {
        var resultado = CalculadoraCustoDesgasteEquipamentos.Calcular(FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, 0m, null);
        Assert.True(resultado.Completo);
        Assert.Equal(0m, resultado.CustoDesgasteEquipamentosLote);
    }

    [Fact]
    public void Percentual_positivo_com_base_indisponivel_e_incompleto()
    {
        var resultado = CalculadoraCustoDesgasteEquipamentos.Calcular(FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, .05m, null);
        Assert.False(resultado.Completo);
        Assert.Null(resultado.CustoDesgasteEquipamentosLote);
    }

    [Fact]
    public void Sem_categoria_e_zero_completo()
    {
        var resultado = CalculadoraCustoDesgasteEquipamentos.Calcular(null, null, null);
        Assert.True(resultado.Completo);
        Assert.Equal(0m, resultado.CustoDesgasteEquipamentosLote);
    }
}
