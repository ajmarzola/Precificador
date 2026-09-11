using Precificador.Core.Insumos;

namespace Precificador.Tests.Unit.Insumos;

public sealed class PrecoInsumoTests
{
    [Fact]
    public void CA03_Criar_preco_valido_preserva_dados_e_tenant()
    {
        var preco = PrecoInsumo.Criar(7, 11, 1000m, 5.39m, new DateOnly(2026, 9, 11));

        Assert.Equal(7, preco.EmpresaId);
        Assert.Equal(11, preco.InsumoId);
        Assert.Equal(1000m, preco.QuantidadeCompra);
        Assert.Equal(5.39m, preco.PrecoCompra);
        Assert.Equal(new DateOnly(2026, 9, 11), preco.DataReferencia);
    }

    [Fact]
    public void CA04_Custo_unitario_divide_preco_por_quantidade_sem_arredondar() =>
        Assert.Equal(0.00539m, PrecoInsumo.Criar(1, 1, 1000m, 5.39m, new DateOnly(2026, 1, 1)).CustoUnitario);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CA05_Quantidade_zero_ou_negativa_e_rejeitada(decimal quantidade) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PrecoInsumo.Criar(1, 1, quantidade, 1m, new DateOnly(2026, 1, 1)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CA06_Preco_zero_ou_negativo_e_rejeitado(decimal preco) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PrecoInsumo.Criar(1, 1, 1m, preco, new DateOnly(2026, 1, 1)));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void CA03_Ids_de_empresa_e_insumo_devem_ser_positivos(int empresaId, int insumoId) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PrecoInsumo.Criar(empresaId, insumoId, 1m, 1m, new DateOnly(2026, 1, 1)));
}
