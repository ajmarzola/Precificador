using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraPrecoProdutoTests
{
    [Fact] public void U1_Formula_normal() { var r = CalculadoraPrecoProduto.Calcular(20m, .2m, .5m); Assert.Equal(25m, r.PrecoTeorico); Assert.Equal(25m, r.PrecoSugerido); }
    [Fact] public void U2_Margem_zero() => Assert.Equal(20m, CalculadoraPrecoProduto.Calcular(20m, 0m, .5m).PrecoTeorico);
    [Fact] public void U3_Custo_zero() { var r = CalculadoraPrecoProduto.Calcular(0m, .3m, .5m); Assert.True(r.Completo); Assert.Equal(0m, r.PrecoTeorico); Assert.Equal(0m, r.PrecoSugerido); }
    [Fact] public void U4_Arredonda_para_cima() => Assert.Equal(25.5m, CalculadoraPrecoProduto.Calcular(20.064m, .2m, .5m).PrecoSugerido);
    [Fact] public void U5_Multiplo_exato_nao_sobe() => Assert.Equal(25m, CalculadoraPrecoProduto.Calcular(20m, .2m, .5m).PrecoSugerido);
    [Fact] public void U6_Incremento_pequeno_sem_arredondamento_previo() => Assert.Equal(25.09m, CalculadoraPrecoProduto.Calcular(25.081m, 0m, .01m).PrecoSugerido);
    [Fact] public void U7_Custo_indisponivel() { var r = CalculadoraPrecoProduto.Calcular(null, .3m, .5m); Assert.False(r.Completo); Assert.Null(r.PrecoTeorico); Assert.Null(r.PrecoSugerido); }
    [Fact] public void U8_Incremento_nao_configurado_mantem_teorico() { var r = CalculadoraPrecoProduto.Calcular(10m, .2m, null); Assert.False(r.Completo); Assert.Equal(12.5m, r.PrecoTeorico); Assert.Null(r.PrecoSugerido); }
    [Fact] public void U9_Margem_proxima_de_cem_porcento_valida() => Assert.Equal(1000m, CalculadoraPrecoProduto.Calcular(1m, .999m, 1m).PrecoTeorico);
    [Fact] public void U10_Margem_negativa_rejeitada() => Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraPrecoProduto.Calcular(1m, -.01m, 1m));
    [Fact] public void U11_Margem_igual_a_um_rejeitada() => Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraPrecoProduto.Calcular(1m, 1m, 1m));
    [Fact] public void U12_Custo_negativo_rejeitado() => Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraPrecoProduto.Calcular(-1m, 0m, 1m));
    [Fact] public void U13_Incremento_zero_rejeitado() => Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraPrecoProduto.Calcular(1m, 0m, 0m));
    [Fact] public void U14_Incremento_negativo_rejeitado() => Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraPrecoProduto.Calcular(1m, 0m, -1m));
    [Fact] public void U15_Precisao_sem_arredondamento_intermediario() => Assert.Equal(25.09m, CalculadoraPrecoProduto.Calcular(20.0648m, .2m, .01m).PrecoSugerido);
    [Fact] public void U16_Resultado_completo_preserva_multiplo_e_limite() { var r = CalculadoraPrecoProduto.Calcular(20.064m, .2m, .5m); Assert.True(r.Completo); Assert.True(r.PrecoSugerido >= r.PrecoTeorico); Assert.Equal(0m, r.PrecoSugerido!.Value % .5m); }
}
