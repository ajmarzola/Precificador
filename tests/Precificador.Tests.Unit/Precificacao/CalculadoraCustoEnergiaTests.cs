using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraCustoEnergiaTests
{
    [Fact] public void Calcular_60Minutos() { var r = CalculadoraCustoEnergia.Calcular(2m, [new(1, 1.5m, 60)]); Assert.Equal(3m, r.CustoEnergiaLote); Assert.Equal(1.5m, r.Usos[0].ConsumoKwh); }
    [Fact] public void Calcular_FracaoSemArredondamento() { var r = CalculadoraCustoEnergia.Calcular(1m, [new(1, 0.3m, 1)]); Assert.Equal(0.005m, r.CustoEnergiaLote); }
    [Fact] public void Calcular_SemUsosETarifaNula_ECompletoComZero() { var r = CalculadoraCustoEnergia.Calcular(null, []); Assert.True(r.Completo); Assert.Equal(0m, r.CustoEnergiaLote); }
    [Fact] public void Calcular_UsosETarifaNula_MantemConsumo() { var r = CalculadoraCustoEnergia.Calcular(null, [new(1, 1m, 30)]); Assert.False(r.Completo); Assert.Null(r.CustoEnergiaLote); Assert.Equal(0.5m, r.Usos[0].ConsumoKwh); Assert.Null(r.Usos[0].CustoEnergiaUso); }
    [Fact] public void Calcular_TarifaZero_ECompleto() { var r = CalculadoraCustoEnergia.Calcular(0m, [new(1, 1m, 30)]); Assert.True(r.Completo); Assert.Equal(0m, r.CustoEnergiaLote); }
    [Fact] public void Calcular_EntradaInvalida_Rejeita() => Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraCustoEnergia.Calcular(1m, [new(1, 0m, 1)]));
}
