using Precificador.Core.FichasTecnicas;

namespace Precificador.Tests.Unit.FichasTecnicas;

public sealed class ItemFichaTecnicaTests
{
    [Fact]
    public void U1_Criar_item_valido_preserva_empresa_ficha_insumo_quantidade_e_observacao()
    {
        var item = ItemFichaTecnica.Criar(7, 11, 13, 1.25m, "  massa principal  ");

        Assert.Equal(7, item.EmpresaId);
        Assert.Equal(11, item.FichaTecnicaId);
        Assert.Equal(13, item.InsumoId);
        Assert.Equal(1.25m, item.Quantidade);
        Assert.Equal("massa principal", item.Observacao);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void U2_Quantidade_deve_ser_maior_que_zero(string quantidade)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ItemFichaTecnica.Criar(1, 1, 1, decimal.Parse(quantidade, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void U3_Observacao_opcional_normaliza_e_valida_limite()
    {
        Assert.Null(ItemFichaTecnica.Criar(1, 1, 1, 1m).Observacao);
        Assert.Null(ItemFichaTecnica.Criar(1, 1, 1, 1m, "   ").Observacao);
        Assert.Equal("linha 1\r\nlinha 2", ItemFichaTecnica.Criar(1, 1, 1, 1m, "  linha 1\r\nlinha 2  ").Observacao);

        Assert.Throws<ArgumentException>(() => ItemFichaTecnica.Criar(1, 1, 1, 1m, new string('a', 1001)));
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -1)]
    public void U4_Ids_de_empresa_ficha_e_insumo_devem_ser_positivos(int empresaId, int fichaId, int insumoId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemFichaTecnica.Criar(empresaId, fichaId, insumoId, 1m));
    }

    [Fact]
    public void U5_Item_nao_possui_unidade_propria()
    {
        var propriedades = typeof(ItemFichaTecnica).GetProperties().Select(propriedade => propriedade.Name);

        Assert.DoesNotContain("Unidade", propriedades);
        Assert.DoesNotContain("UnidadeBase", propriedades);
        Assert.DoesNotContain("UnidadeMedida", propriedades);
    }
}
