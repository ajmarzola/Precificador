using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraMargemAtualTests
{
    [Fact]
    public void U1_Margem_acima_da_meta_fica_dentro() =>
        Assert.Equal(SituacaoMargemProduto.DentroDaMargem, CalculadoraMargemAtual.Calcular(60m, 100m, .30m).Situacao);

    [Fact]
    public void U2_Margem_abaixo_da_meta_fica_abaixo() =>
        Assert.Equal(SituacaoMargemProduto.AbaixoDaMargem, CalculadoraMargemAtual.Calcular(80m, 100m, .30m).Situacao);

    [Fact]
    public void U3_Margem_igual_a_meta_fica_dentro()
    {
        var resultado = CalculadoraMargemAtual.Calcular(70m, 100m, .30m);

        Assert.Equal(.30m, resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.DentroDaMargem, resultado.Situacao);
    }

    [Fact]
    public void U4_Custo_zero_produz_margem_um()
    {
        var resultado = CalculadoraMargemAtual.Calcular(0m, 100m, .30m);

        Assert.Equal(1m, resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.DentroDaMargem, resultado.Situacao);
    }

    [Fact]
    public void U5_Custo_maior_que_preco_produz_margem_negativa()
    {
        var resultado = CalculadoraMargemAtual.Calcular(120m, 100m, .30m);

        Assert.Equal(-.20m, resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.AbaixoDaMargem, resultado.Situacao);
    }

    [Fact]
    public void U6_Custo_null_fica_incompleto()
    {
        var resultado = CalculadoraMargemAtual.Calcular(null, 100m, .30m);

        Assert.Null(resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.Incompleto, resultado.Situacao);
    }

    [Fact]
    public void U7_Preco_null_fica_incompleto()
    {
        var resultado = CalculadoraMargemAtual.Calcular(10m, null, .30m);

        Assert.Null(resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.Incompleto, resultado.Situacao);
    }

    [Fact]
    public void U8_Custo_e_preco_null_ficam_incompletos()
    {
        var resultado = CalculadoraMargemAtual.Calcular(null, null, .30m);

        Assert.Null(resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.Incompleto, resultado.Situacao);
    }

    [Fact]
    public void U9_Margem_alvo_zero_e_valida()
    {
        var resultado = CalculadoraMargemAtual.Calcular(100m, 100m, 0m);

        Assert.Equal(0m, resultado.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.DentroDaMargem, resultado.Situacao);
    }

    [Fact]
    public void U10_Fronteira_nao_arredonda_antes_de_classificar()
    {
        var abaixo = CalculadoraMargemAtual.Calcular(70.000001m, 100m, .30m);
        var dentro = CalculadoraMargemAtual.Calcular(70m, 100m, .30m);

        Assert.Equal(.29999999m, abaixo.MargemAtual);
        Assert.Equal(SituacaoMargemProduto.AbaixoDaMargem, abaixo.Situacao);
        Assert.Equal(SituacaoMargemProduto.DentroDaMargem, dentro.Situacao);
    }

    [Fact]
    public void U11_Margem_alvo_negativa_rejeitada() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraMargemAtual.Calcular(1m, 2m, -.01m));

    [Fact]
    public void U12_Margem_alvo_maior_ou_igual_a_um_rejeitada()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraMargemAtual.Calcular(1m, 2m, 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraMargemAtual.Calcular(1m, 2m, 1.01m));
    }

    [Fact]
    public void U13_Custo_conhecido_negativo_rejeitado() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraMargemAtual.Calcular(-1m, 2m, .30m));

    [Fact]
    public void U14_Preco_conhecido_nao_positivo_rejeitado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraMargemAtual.Calcular(1m, 0m, .30m));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraMargemAtual.Calcular(1m, -1m, .30m));
    }
}
