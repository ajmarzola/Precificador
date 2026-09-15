using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Produtos;

public sealed class RegistroPrecoProdutoTests
{
    [Fact]
    public void U1_Criacao_valida_preserva_todo_snapshot()
    {
        var registro = RegistroPrecoProduto.Criar(1, 2, new DateOnly(2026, 9, 15), 12.345678m, .3m, 17.7m, 15m, .1m);

        Assert.Equal(1, registro.EmpresaId);
        Assert.Equal(2, registro.ProdutoId);
        Assert.Equal(new DateOnly(2026, 9, 15), registro.DataReferencia);
        Assert.Equal(12.345678m, registro.CustoReferencia);
        Assert.Equal(.3m, registro.MargemReferencia);
        Assert.Equal(17.7m, registro.PrecoSugerido);
        Assert.Equal(15m, registro.PrecoPrateleira);
        Assert.Equal(.1m, registro.ReservaComercialReferencia);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void U2_Ids_invalidos_sao_rejeitados(int empresaId, int produtoId) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Criar(empresaId, produtoId));

    [Fact]
    public void U3_Data_default_e_rejeitada() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RegistroPrecoProduto.Criar(1, 1, default, 0m, 0m, 0m, 1m, 0m));

    [Theory]
    [InlineData(-.1)]
    [InlineData(0)]
    public void U4_Custo_negativo_e_rejeitado_mas_zero_e_valido(decimal custo)
    {
        if (custo < 0) Assert.Throws<ArgumentOutOfRangeException>(() => RegistroPrecoProduto.Criar(1, 1, new DateOnly(2026, 1, 1), custo, 0m, 0m, 1m, 0m));
        else Assert.Equal(0m, RegistroPrecoProduto.Criar(1, 1, new DateOnly(2026, 1, 1), custo, 0m, 0m, 1m, 0m).CustoReferencia);
    }

    [Theory]
    [InlineData(-.1)]
    [InlineData(1)]
    public void U5_U8_Fracoes_fora_da_faixa_sao_rejeitadas(decimal valor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegistroPrecoProduto.Criar(1, 1, new DateOnly(2026, 1, 1), 0m, valor, 0m, 1m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegistroPrecoProduto.Criar(1, 1, new DateOnly(2026, 1, 1), 0m, 0m, 0m, 1m, valor));
    }

    [Theory]
    [InlineData(-.1)]
    [InlineData(0)]
    public void U6_Preco_sugerido_negativo_e_rejeitado_mas_zero_e_valido(decimal preco)
    {
        if (preco < 0) Assert.Throws<ArgumentOutOfRangeException>(() => RegistroPrecoProduto.Criar(1, 1, new DateOnly(2026, 1, 1), 0m, 0m, preco, 1m, 0m));
        else Assert.Equal(0m, RegistroPrecoProduto.Criar(1, 1, new DateOnly(2026, 1, 1), 0m, 0m, preco, 1m, 0m).PrecoSugerido);
    }

    [Theory]
    [InlineData(-.1)]
    [InlineData(0)]
    public void U7_Preco_prateleira_deve_ser_positivo(decimal preco) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RegistroPrecoProduto.Criar(1, 1, new DateOnly(2026, 1, 1), 0m, 0m, 0m, preco, 0m));

    private static RegistroPrecoProduto Criar(int empresaId, int produtoId) =>
        RegistroPrecoProduto.Criar(empresaId, produtoId, new DateOnly(2026, 1, 1), 0m, 0m, 0m, 1m, 0m);
}
