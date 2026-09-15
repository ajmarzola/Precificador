using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class CalculadoraCustoItensTests
{
    [Fact]
    public void U1_Calcula_custo_do_item()
    {
        var resultado = CalculadoraCustoItens.Calcular([new(1, 2.5m, 3.2m)]);

        Assert.Equal(8m, Assert.Single(resultado.Itens).CustoItem);
    }

    [Fact]
    public void U2_Preserva_precisao_sem_arredondamento_intermediario()
    {
        var resultado = CalculadoraCustoItens.Calcular([new(1, 37m, 0.013579m)]);

        Assert.Equal(0.502423m, Assert.Single(resultado.Itens).CustoItem);
        Assert.Equal(0.502423m, resultado.CustoBaseItens);
    }

    [Fact]
    public void U3_Soma_itens_quando_todos_tem_custo()
    {
        var resultado = CalculadoraCustoItens.Calcular([new(1, 2m, 3m), new(2, 1.5m, 4m)]);

        Assert.True(resultado.Completo);
        Assert.Equal(12m, resultado.CustoBaseItens);
    }

    [Fact]
    public void U4_Custo_desconhecido_indisponibiliza_total_sem_apagar_conhecido()
    {
        var resultado = CalculadoraCustoItens.Calcular([new(1, 2m, 3m), new(2, 1m, null)]);

        Assert.False(resultado.Completo);
        Assert.Null(resultado.CustoBaseItens);
        Assert.Equal(6m, resultado.Itens.Single(item => item.ItemId == 1).CustoItem);
        Assert.Null(resultado.Itens.Single(item => item.ItemId == 2).CustoItem);
    }

    [Fact]
    public void U5_Colecao_vazia_e_incompleta_e_sem_total()
    {
        var resultado = CalculadoraCustoItens.Calcular([]);

        Assert.False(resultado.Completo);
        Assert.Null(resultado.CustoBaseItens);
        Assert.Empty(resultado.Itens);
    }
}
